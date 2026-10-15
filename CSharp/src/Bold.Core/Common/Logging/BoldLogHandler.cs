// Delphi: Source/Common/Logging/BoldLogHandler.pas
// Port map: CSharp/port/units/BoldLogHandler.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldLogHandler", Symbol = "TBoldLogHandlerClass", Note = "Global using alias of System.Type, in BoldLogHandler.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldLogHandler", Symbol = "initialization", Note = "Empty in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLogHandler", Symbol = "finalization", Note = "Freed the global log; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// The log: sends each log line, progress step and session start as a subscription event, so any number of receivers
/// (registered with <see cref="RegisterLogReceiver"/>) show it. Keeps an indent and an interruption flag.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldLogHandler : TBoldSubscribableObject
{
    private int fIndent;
    private bool fInterrupted;
    private bool fInterruptHandled;

    [Delphi(PortStatus.Ported)]
    public TBoldLogHandler()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose() => base.Dispose();

    [Delphi(PortStatus.Ported)]
    private void SetProgress(int Value) => SendExtendedEvent(BoldLogHandler.bleSetProgress, [Value]);

    [Delphi(PortStatus.Ported)]
    private void SetLogHeader(string Value) => SendExtendedEvent(BoldLogHandler.bleSetLogHeader, [Value]);

    /// <summary>Sends the maximum, never below zero.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetProgressMax(int Value) => SendExtendedEvent(BoldLogHandler.bleSetProgressMax, [BoldMath.MaxIntValue(0, Value)]);

    /// <summary>Two spaces per indent level.</summary>
    [Delphi(PortStatus.Ported)]
    protected string IndentSpaces() => SysUtils.StringOfChar(' ', fIndent << 1);

    /// <summary>Sends the line, indented, with its type.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Log(string s, TBoldLogType LogType = TBoldLogType.ltInfo) =>
        SendExtendedEvent(BoldLogHandler.bleLog, [IndentSpaces() + s, (int)LogType]);

    [Delphi(PortStatus.Ported)]
    public void LogFmt(string s, object?[] Args, TBoldLogType LogType = TBoldLogType.ltInfo) => Log(SysUtils.Format(s, Args), LogType);

    [Delphi(PortStatus.Ported)]
    public void LogIndent(string s, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        Log(s, LogType);
        Indent();
    }

    [Delphi(PortStatus.Ported)]
    public void LogDedent(string s, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        Dedent();
        Log(s, LogType);
    }

    [Delphi(PortStatus.Ported)]
    public void LogFmtIndent(string s, object?[] Args, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        LogFmt(s, Args, LogType);
        Indent();
    }

    [Delphi(PortStatus.Ported)]
    public void LogFmtDedent(string s, object?[] Args, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        Dedent();
        LogFmt(s, Args, LogType);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Clear() => SendEvent(BoldLogHandler.bleClear);

    [Delphi(PortStatus.Ported)]
    public virtual void Hide() => SendEvent(BoldLogHandler.bleHide);

    [Delphi(PortStatus.Ported)]
    public virtual void Show() => SendEvent(BoldLogHandler.bleShow);

    /// <summary>Resets the indent and the interruption, writes a separator and starts the session.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void StartLog(string SessionName)
    {
        fIndent = 0;
        fInterrupted = false;
        fInterruptHandled = false;
        Separator();
        SendExtendedEvent(BoldLogHandler.bleStartLog, [SessionName]);
    }

    [Delphi(PortStatus.Ported)]
    public void EndLog()
    {
        SendEvent(BoldLogHandler.bleEndLog);
        Separator();
    }

    [Delphi(PortStatus.Ported)]
    public void Indent() => fIndent++;

    [Delphi(PortStatus.Ported)]
    public void Dedent()
    {
        fIndent--;
        if (fIndent < 0)
        {
            fIndent = 0;
        }
    }

    /// <summary>An empty line, a separator line and an empty line.</summary>
    [Delphi(PortStatus.Ported)]
    public void Separator()
    {
        Log("", TBoldLogType.ltSeparator);
        Log("-={++++}=-", TBoldLogType.ltSeparator);
        Log("", TBoldLogType.ltSeparator);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ProgressStep() => SendEvent(BoldLogHandler.bleProgressStep);

    [Delphi(PortStatus.Ported)]
    public virtual void Sync() => SendEvent(BoldLogHandler.bleSync);

    /// <summary>Subscribes a new receiver subscriber for <paramref name="LogReceiver"/> to the global log.</summary>
    [Delphi(PortStatus.Ported, Note = "Subscribes to the global log (BoldLog), as Delphi does, whichever handler it is called on.")]
    public void RegisterLogReceiver(IBoldLogReceiver LogReceiver) => _ = new TBoldLogReceiverSubscriber(LogReceiver);

    [Delphi(PortStatus.Ported)]
    public void UnregisterLogReceiver(IBoldLogReceiver LogReceiver) => SendExtendedEvent(BoldLogHandler.bleRemoveReceiver, [LogReceiver]);

    /// <summary>Asks the running process to stop; it checks with <see cref="ProcessInterruption"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void InterruptProcess()
    {
        Log(BoldCommonConst.sTryingToAbort);
        fInterrupted = true;
        fInterruptHandled = false;
    }

    /// <summary>Lets receivers process input; true when the process was asked to stop (logged once).</summary>
    [Delphi(PortStatus.Ported)]
    public bool ProcessInterruption()
    {
        SendEvent(BoldLogHandler.bleProcessInterruption);
        bool result = fInterrupted;
        if (result)
        {
            if (!fInterruptHandled)
            {
                Log(BoldCommonConst.sProcessStopped);
            }

            fInterruptHandled = true;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public int ProgressMax { set => SetProgressMax(value); }
    [Delphi(PortStatus.Ported)] public int Progress { set => SetProgress(value); }
    [Delphi(PortStatus.Ported)] public string LogHeader { set => SetLogHeader(value); }
}

/// <summary>Passes the log's events to one receiver; frees itself when the log goes or the receiver is unregistered.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLogReceiverSubscriber : TBoldSubscriber
{
    private readonly IBoldLogReceiver fReceiver;

    [Delphi(PortStatus.Ported)]
    public TBoldLogReceiverSubscriber(IBoldLogReceiver Receiver)
    {
        fReceiver = Receiver;
        BoldLogHandler.BoldLog().AddSmallSubscription(this, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), 0);
        BoldLogHandler.BoldLog().AddSmallSubscription(this, TBoldSmallEventSet.Range(BoldLogHandler.bleFirst, BoldLogHandler.bleLast), 0);
    }

    /// <summary>Nothing: the events come through <see cref="ReceiveExtended"/>.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void ReceiveExtended(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        static string GetString(object? value) =>
            value as string ?? throw new Exception(BoldCommonConst.sUnknownTypeInGetString);

        switch (OriginalEvent)
        {
            case BoldLogHandler.bleSetProgress: fReceiver.SetProgress((int)Args[0]!); break;
            case BoldLogHandler.bleSetLogHeader: fReceiver.SetLogHeader(GetString(Args[0])); break;
            case BoldLogHandler.bleSetProgressMax: fReceiver.SetProgressMax((int)Args[0]!); break;
            case BoldLogHandler.bleLog: fReceiver.Log(GetString(Args[0]), (TBoldLogType)(int)Args[1]!); break;
            case BoldLogHandler.bleStartLog: fReceiver.StartLog(GetString(Args[0])); break;
            case BoldLogHandler.bleRemoveReceiver:
                if (ReferenceEquals(Args[0], fReceiver))
                {
                    Dispose();
                }

                break;
            case BoldSubscription.beDestroying: Dispose(); break;
            case BoldLogHandler.bleClear: fReceiver.Clear(); break;
            case BoldLogHandler.bleHide: fReceiver.Hide(); break;
            case BoldLogHandler.bleProgressStep: fReceiver.ProgressStep(); break;
            case BoldLogHandler.bleShow: fReceiver.Show(); break;
            case BoldLogHandler.bleSync: fReceiver.Sync(); break;
            case BoldLogHandler.bleEndLog: fReceiver.EndLog(); break;
            case BoldLogHandler.bleProcessInterruption: fReceiver.ProcessInterruption(); break;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetHandlesExtendedEvents() => true;
}

/// <summary>The global log and its event numbers.</summary>
public static partial class BoldLogHandler
{
    internal const int bleFirst = 1;
    internal const int bleSetProgress = 1;
    internal const int bleSetLogHeader = 2;
    internal const int bleSetProgressMax = 3;
    internal const int bleClear = 4;
    internal const int bleHide = 5;
    internal const int bleLog = 6;
    internal const int bleProgressStep = 7;
    internal const int bleShow = 8;
    internal const int bleStartLog = 9;
    internal const int bleEndLog = 10;
    internal const int bleProcessInterruption = 11;
    internal const int bleRemoveReceiver = 12;
    internal const int bleSync = 13;
    internal const int bleLast = 13;

    private static TBoldLogHandler? G_BoldLog;

    /// <summary>The class created for the global log; set it before the first <see cref="BoldLog"/> call to use a subclass.</summary>
    internal static TBoldLogHandlerClass G_BoldLogHandlerClass = typeof(TBoldLogHandler);

    /// <summary>The global log, created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldLogHandler BoldLog() => G_BoldLog ??= (TBoldLogHandler)Activator.CreateInstance(G_BoldLogHandlerClass)!;
}
