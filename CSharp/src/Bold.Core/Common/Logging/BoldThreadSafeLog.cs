// Delphi: Source/Common/Logging/BoldThreadSafeLog.pas
// Port map: CSharp/port/units/BoldThreadSafeLog.md
using System.Text;

namespace Bold;

/// <summary>
/// The log of the propagator and its threads: a general log, an error log and, when named, a thread log that records
/// every acquire and release of a <see cref="TBoldLoggableCriticalSection"/>. Writing is safe from any thread.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldLogger : IDisposable
{
    private TFileLogging? fLog;
    private TFileLogging? fErrorLog;
    private TFileLogging? fThreadLog;

    [Delphi(PortStatus.Ported)]
    public TBoldLogger(string LogFileName, string ErrorLogFileName, string ThreadLogFileName, int MaxLogFileSize)
    {
        fLog = new TFileLogging(LogFileName, MaxLogFileSize, true);
        fErrorLog = new TFileLogging(ErrorLogFileName, MaxLogFileSize, true) { IncludeThreadId = true };
        if (ThreadLogFileName != "")
        {
            fThreadLog = new TFileLogging(ThreadLogFileName, MaxLogFileSize, true)
            {
                IncludeThreadId = true,
                IncludeDate = false,
                ShortThreadId = true,
            };
        }
        else
        {
            fThreadLog = null;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLogger.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fLog);
        SysUtils.FreeAndNil(ref fErrorLog);
        SysUtils.FreeAndNil(ref fThreadLog);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public void Log(string Msg) => fLog?.Trace(Msg);

    [Delphi(PortStatus.Ported)]
    public void LogError(string Msg) => fErrorLog?.Trace(Msg);

    [Delphi(PortStatus.Ported)]
    public void LogFmt(string Msg, params object?[] Args) => fLog?.Trace(Msg, Args);

    [Delphi(PortStatus.Ported)]
    public void LogErrorFmt(string Msg, params object?[] Args) => fErrorLog?.Trace(Msg, Args);

    [Delphi(PortStatus.Ported, Note = "Does nothing without a thread log, where Delphi failed on the missing log.")]
    public void LogThread(string Msg) => fThreadLog?.Trace(Msg);
}

/// <summary>
/// A log file that lines are appended to, each with the time and optionally the thread id. When the file grows past
/// the maximum size it is emptied and starts again.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TFileLogging : IDisposable
{
    private FileStream? fFileStream;
    private readonly string fFileName;
    private readonly long fMaxSize;
    private TCriticalSection? fLocker;
    private bool fOpen;

    [Delphi(PortStatus.Ported, Symbol = "TFileLogging.create")]
    public TFileLogging(string FileName, long MaxSize, bool Open)
    {
        fFileName = FileName;
        IncludeDate = true;
        fMaxSize = MaxSize;
        fLocker = new TCriticalSection();
        IncludeThreadId = false;
        fLocker.Acquire();
        try
        {
            this.Open = Open;
        }
        finally
        {
            fLocker.Release();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TFileLogging.Destroy")]
    public void Dispose()
    {
        if (fLocker is null)
        {
            return;
        }

        fLocker.Acquire();
        try
        {
            CloseStream();
        }
        finally
        {
            fLocker.Release();
        }

        SysUtils.FreeAndNil(ref fLocker);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public string FileName => fFileName;

    [Delphi(PortStatus.Ported)]
    public bool Open
    {
        get => fOpen;
        set => SetOpen(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool IncludeThreadId { get; set; }

    [Delphi(PortStatus.Ported)]
    public bool IncludeDate { get; set; }

    [Delphi(PortStatus.Ported)]
    public bool ShortThreadId { get; set; }

    /// <summary>Opens the file for appending, creating it if needed; false when it cannot be opened.</summary>
    [Delphi(PortStatus.Ported)]
    private bool OpenStream()
    {
        try
        {
            fFileStream = new FileStream(fFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Appends a line: the date and time (or only the time), the entry and, if asked for, the thread id.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TFileLogging.Trace(string)", Note = "Writes the line as UTF-8; Unicode Delphi wrote only the first half of its bytes.")]
    public void Trace(string Entry)
    {
        TCriticalSection? locker = fLocker;
        if (locker is null)
        {
            return;
        }

        locker.Acquire();
        try
        {
            if (Open && fFileStream is not null)
            {
                if (fMaxSize > 0 && fFileStream.Length > fMaxSize)
                {
                    FlushStream();
                }

                string line = IncludeDate ? SysUtils.DateTimeToStr(DateTime.Now) : SysUtils.TimeToStr(DateTime.Now);
                line = line + " " + Entry;
                if (IncludeThreadId)
                {
                    line += ShortThreadId
                        ? SysUtils.Format(":TID=%d", Environment.CurrentManagedThreadId)
                        : SysUtils.Format(" (ThreadID=%d)", Environment.CurrentManagedThreadId);
                }

                line += BoldDefs.BOLDCRLF;
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                fFileStream.Write(bytes, 0, bytes.Length);
                fFileStream.Flush();
            }
        }
        finally
        {
            locker.Release();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TFileLogging.Trace(string,array of const)")]
    public void Trace(string Msg, params object?[] Args) => Trace(SysUtils.Format(Msg, Args));

    [Delphi(PortStatus.Ported)]
    private void CloseStream()
    {
        fFileStream?.Dispose();
        fFileStream = null;
    }

    /// <summary>Empties the file.</summary>
    [Delphi(PortStatus.Ported)]
    private void FlushStream()
    {
        fFileStream!.SetLength(0);
        fFileStream.Position = 0;
    }

    [Delphi(PortStatus.Ported)]
    private void SetOpen(bool Value)
    {
        fLocker!.Acquire();
        try
        {
            if (Value != fOpen)
            {
                fOpen = Value;
                if (Value)
                {
                    fOpen = OpenStream();
                }
                else
                {
                    CloseStream();
                }
            }
        }
        finally
        {
            fLocker.Release();
        }
    }
}

/// <summary>The global propagator log: set up with <see cref="BoldInitLog"/>; logging does nothing until then.</summary>
public static class BoldThreadSafeLog
{
    private static volatile bool LogThreadActivities;
    private static TBoldLogger? Logger;

    [Delphi(PortStatus.Ported)]
    public static void BoldInitLog(string LogFilename, string ErrorLogFileName, string ThreadLogFileName, int MaxLogFileSize)
    {
        LogThreadActivities = ThreadLogFileName != "";
        Logger = new TBoldLogger(LogFilename, ErrorLogFileName, ThreadLogFileName, MaxLogFileSize);
    }

    [Delphi(PortStatus.Ported, Note = "Stops thread logging too.")]
    public static void BoldDoneLog()
    {
        LogThreadActivities = false;
        Interlocked.Exchange(ref Logger, null)?.Dispose();
    }

    [Delphi(PortStatus.Ported, Symbol = "BoldLog(string)")]
    public static void BoldLog(string Msg) => Logger?.Log(Msg);

    [Delphi(PortStatus.Ported, Symbol = "BoldLog(string,array of const)")]
    public static void BoldLog(string Msg, params object?[] Args) => Logger?.LogFmt(Msg, Args);

    [Delphi(PortStatus.Ported, Symbol = "BoldLogError(string)")]
    public static void BoldLogError(string Msg) => Logger?.LogError(Msg);

    [Delphi(PortStatus.Ported, Symbol = "BoldLogError(string,array of const)")]
    public static void BoldLogError(string Msg, params object?[] Args) => Logger?.LogErrorFmt(Msg, Args);

    [Delphi(PortStatus.Ported)]
    public static void BoldLogThread(string Msg)
    {
        if (LogThreadActivities)
        {
            Logger?.LogThread(Msg);
        }
    }
}
