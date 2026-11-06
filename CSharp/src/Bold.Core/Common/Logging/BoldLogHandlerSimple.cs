// Delphi: Source/Common/Logging/BoldLogHandlerSimple.pas
// Port map: CSharp/port/units/BoldLogHandlerSimple.md

namespace Bold;

/// <summary>A log receiver that keeps the lines it is given, for a program without a log form or a test.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSimpleLogReceiver : IBoldLogReceiver, IDisposable
{
    private TStringList? fLogLines;

    [Delphi(PortStatus.Ported)]
    private TStringList GetLogLines() => fLogLines ??= new TStringList();

    /// <summary>The lines logged.</summary>
    [Delphi(PortStatus.Ported)]
    public TStringList LogLines => GetLogLines();

    [Delphi(PortStatus.Ported, Symbol = "TBoldSimpleLogReceiver.Destroy")]
    public void Dispose()
    {
        fLogLines = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public void Clear() => LogLines.Clear();

    [Delphi(PortStatus.Ported)]
    public void EndLog()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void Hide()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void Log(string s, TBoldLogType LogType) => LogLines.Add(s);

    [Delphi(PortStatus.Ported)]
    public void ProgressStep()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void SetProgress(int Value)
    {
        // intentionally left blank
    }

    /// <summary>The header is logged as a line.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetLogHeader(string Value) => Log(Value, TBoldLogType.ltInfo);

    [Delphi(PortStatus.Ported)]
    public void SetProgressMax(int Value)
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void Show()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void StartLog(string SessionName) => LogLines.Add(SysUtils.Format(BoldCommonConst.sSessionStart, SessionName));

    [Delphi(PortStatus.Ported)]
    public void ProcessInterruption()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public void Sync()
    {
        // intentionally left blank
    }
}
