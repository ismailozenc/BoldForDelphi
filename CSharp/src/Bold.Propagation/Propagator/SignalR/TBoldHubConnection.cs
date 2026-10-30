using System.Collections.Concurrent;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Bold;

/// <summary>
/// A small SignalR client: SignalR's JSON hub protocol over a WebSocket, with no negotiation (an ASP.NET Core SignalR
/// server accepts WebSocket connections straight at the hub's URL). It does what the propagator hub needs: blocking
/// invocations with results, calls from the hub to handlers, keep-alive pings and a notification when the connection
/// closes. It does not reconnect; a closed connection is reported through <see cref="OnClosed"/>, and a new connection
/// is made for a new start.
/// </summary>
/// <remarks>
/// It uses only the .NET class library, so the client libraries need no package. Handlers run on the thread that reads
/// the connection; they should not block.
/// </remarks>
public sealed class TBoldHubConnection : IDisposable
{
    private const char RecordSeparator = '\u001e';
    private const int InvocationMessage = 1;
    private const int CompletionMessage = 3;
    private const int PingMessage = 6;
    private const int CloseMessage = 7;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ClientWebSocket fSocket = new();
    private readonly SemaphoreSlim fSendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>> fPending = new();
    private readonly ConcurrentDictionary<string, Action<JsonElement[]>> fHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource fStop = new();
    private readonly StringBuilder fReceived = new();
    private readonly Decoder fDecoder = Encoding.UTF8.GetDecoder();
    private readonly byte[] fReceiveBuffer = new byte[16 * 1024];
    private int fNextInvocationId;
    private int fClosed;
    private volatile bool fDisposing;
    private Task? fReceiving;
    private Timer? fPing;

    /// <summary>A connection to the hub at <paramref name="Url"/> (http, https, ws or wss).</summary>
    public TBoldHubConnection(string Url)
    {
        this.Url = WebSocketUri(Url);
    }

    /// <summary>The hub's WebSocket address.</summary>
    public Uri Url { get; }

    /// <summary>How long an invocation waits for its result, in milliseconds (30 seconds by default).</summary>
    public int InvokeTimeOut { get; set; } = 30000;

    /// <summary>How often a ping is sent so the server keeps the connection, in milliseconds (15 seconds by default).</summary>
    public int KeepAliveInterval { get; set; } = 15000;

    /// <summary>Called once when the connection closes, with the error if there was one; not when it is disposed.</summary>
    public Action<Exception?>? OnClosed { get; set; }

    /// <summary>True from a successful start until the connection closes.</summary>
    public bool Connected => fReceiving is not null && Volatile.Read(ref fClosed) == 0;

    /// <summary>The WebSocket address for a hub's address: http becomes ws and https wss.</summary>
    public static Uri WebSocketUri(string Url)
    {
        var builder = new UriBuilder(Url);
        builder.Scheme = builder.Scheme switch
        {
            "http" => "ws",
            "https" => "wss",
            _ => builder.Scheme,
        };
        return builder.Uri;
    }

    /// <summary>Connects and makes the protocol handshake; raises when the hub cannot be reached in <paramref name="TimeOut"/> milliseconds.</summary>
    public void Start(int TimeOut = 30000)
    {
        if (fReceiving is not null)
        {
            throw new EBold("TBoldHubConnection: already started.");
        }

        using var timeOut = CancellationTokenSource.CreateLinkedTokenSource(fStop.Token);
        timeOut.CancelAfter(TimeOut);
        try
        {
            fSocket.ConnectAsync(Url, timeOut.Token).GetAwaiter().GetResult();
            SendText("{\"protocol\":\"json\",\"version\":1}" + RecordSeparator, timeOut.Token);
            string? handshake = ReadRecordAsync(timeOut.Token).GetAwaiter().GetResult();
            if (handshake is null)
            {
                throw new EBold($"The hub at {Url} closed the connection during the handshake.");
            }

            using JsonDocument response = JsonDocument.Parse(handshake);
            if (response.RootElement.TryGetProperty("error", out JsonElement error))
            {
                throw new EBold($"The hub at {Url} refused the handshake: {error.GetString()}");
            }
        }
        catch (OperationCanceledException) when (!fStop.IsCancellationRequested)
        {
            throw new EBold($"The hub at {Url} did not answer in {TimeOut} ms.");
        }
        catch (WebSocketException e)
        {
            throw new EBold($"Could not connect to the hub at {Url}: {e.Message}");
        }

        fReceiving = Task.Run(ReceiveLoop);
        fPing = new Timer(_ => Ping(), null, KeepAliveInterval, KeepAliveInterval);
    }

    /// <summary>Calls <paramref name="Handler"/> with the arguments of each call of <paramref name="Target"/> by the hub.</summary>
    public void On(string Target, Action<JsonElement[]> Handler) => fHandlers[Target] = Handler;

    /// <summary>Calls the hub's method and waits for its result; raises when the hub reports an error or does not answer in time.</summary>
    public T? Invoke<T>(string Target, params object?[] Arguments)
    {
        JsonElement? result = InvokeCore(Target, Arguments);
        return result is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } value ? value.Deserialize<T>(JsonOptions) : default;
    }

    /// <summary>Calls the hub's method and waits for it to complete.</summary>
    public void Invoke(string Target, params object?[] Arguments) => InvokeCore(Target, Arguments);

    private JsonElement? InvokeCore(string Target, object?[] Arguments)
    {
        if (!Connected)
        {
            throw new EBold($"The connection to the hub at {Url} is closed.");
        }

        string id = Interlocked.Increment(ref fNextInvocationId).ToString(CultureInfo.InvariantCulture);
        var completion = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        fPending[id] = completion;
        try
        {
            var message = new Dictionary<string, object?>
            {
                ["type"] = InvocationMessage,
                ["invocationId"] = id,
                ["target"] = Target,
                ["arguments"] = Arguments,
            };
            SendText(JsonSerializer.Serialize(message, JsonOptions) + RecordSeparator, fStop.Token);
            return completion.Task.WaitAsync(TimeSpan.FromMilliseconds(InvokeTimeOut)).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            throw new EBold($"The hub at {Url} did not answer {Target} in {InvokeTimeOut} ms.");
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            throw new EBold($"The connection to the hub at {Url} is closed: {e.Message}");
        }
        finally
        {
            fPending.TryRemove(id, out _);
        }
    }

    private void Ping()
    {
        try
        {
            if (Connected)
            {
                SendText("{\"type\":6}" + RecordSeparator, fStop.Token);
            }
        }
        catch (Exception)
        {
            // A failed ping means the connection is going; the receive loop reports it.
        }
    }

    private void SendText(string Text, CancellationToken Token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Text);
        fSendLock.Wait(Token);
        try
        {
            fSocket.SendAsync(bytes, WebSocketMessageType.Text, true, Token).GetAwaiter().GetResult();
        }
        finally
        {
            fSendLock.Release();
        }
    }

    /// <summary>The next message (without its record separator); null when the server closed the connection.</summary>
    private async Task<string?> ReadRecordAsync(CancellationToken Token)
    {
        while (true)
        {
            string received = fReceived.ToString();
            int end = received.IndexOf(RecordSeparator, StringComparison.Ordinal);
            if (end >= 0)
            {
                fReceived.Remove(0, end + 1);
                return received[..end];
            }

            WebSocketReceiveResult result = await fSocket.ReceiveAsync(fReceiveBuffer, Token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            char[] chars = new char[fDecoder.GetCharCount(fReceiveBuffer, 0, result.Count)];
            fDecoder.GetChars(fReceiveBuffer, 0, result.Count, chars, 0);
            fReceived.Append(chars);
        }
    }

    private async Task ReceiveLoop()
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                string? record = await ReadRecordAsync(fStop.Token).ConfigureAwait(false);
                if (record is null || !HandleMessage(record, ref error))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (fStop.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            error = e;
        }

        Close(error);
    }

    /// <summary>Handles one message from the hub; false for a close message.</summary>
    private bool HandleMessage(string Record, ref Exception? Error)
    {
        using JsonDocument document = JsonDocument.Parse(Record);
        JsonElement root = document.RootElement;
        switch (root.GetProperty("type").GetInt32())
        {
            case InvocationMessage:
                string target = root.GetProperty("target").GetString() ?? "";
                JsonElement[] arguments = root.TryGetProperty("arguments", out JsonElement args) ? [.. args.EnumerateArray().Select(a => a.Clone())] : [];
                if (fHandlers.TryGetValue(target, out Action<JsonElement[]>? handler))
                {
                    try
                    {
                        handler(arguments);
                    }
                    catch (Exception e)
                    {
                        BoldThreadSafeLog.BoldLogError("TBoldHubConnection: the handler of %s failed: %s", target, e.Message);
                    }
                }

                break;
            case CompletionMessage:
                string id = root.GetProperty("invocationId").GetString() ?? "";
                if (fPending.TryRemove(id, out TaskCompletionSource<JsonElement?>? completion))
                {
                    if (root.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String)
                    {
                        completion.TrySetException(new EBold($"The hub failed: {error.GetString()}"));
                    }
                    else
                    {
                        completion.TrySetResult(root.TryGetProperty("result", out JsonElement result) ? result.Clone() : null);
                    }
                }

                break;
            case CloseMessage:
                if (root.TryGetProperty("error", out JsonElement closeError) && closeError.ValueKind == JsonValueKind.String)
                {
                    Error = new EBold($"The hub closed the connection: {closeError.GetString()}");
                }

                return false;
            case PingMessage:
            default:
                break;
        }

        return true;
    }

    private void Close(Exception? Error)
    {
        if (Interlocked.Exchange(ref fClosed, 1) == 1)
        {
            return;
        }

        fPing?.Dispose();
        foreach (string id in fPending.Keys)
        {
            if (fPending.TryRemove(id, out TaskCompletionSource<JsonElement?>? completion))
            {
                completion.TrySetException(new EBold($"The connection to the hub at {Url} closed.", Error));
            }
        }

        if (!fDisposing)
        {
            OnClosed?.Invoke(Error);
        }
    }

    /// <summary>Closes the connection (without calling <see cref="OnClosed"/>).</summary>
    public void Dispose()
    {
        fDisposing = true;
        if (fSocket.State == WebSocketState.Open)
        {
            try
            {
                using var timeOut = new CancellationTokenSource(2000);
                fSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeOut.Token).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Closing anyway.
            }
        }

        fStop.Cancel();
        try
        {
            fReceiving?.Wait(2000);
        }
        catch (AggregateException)
        {
        }

        Close(null);
        fSocket.Dispose();
        fStop.Dispose();
        fSendLock.Dispose();
    }
}
