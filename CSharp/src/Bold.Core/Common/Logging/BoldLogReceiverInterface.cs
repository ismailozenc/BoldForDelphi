// Delphi: Source/Common/Logging/BoldLogReceiverInterface.pas
// Port map: CSharp/port/units/BoldLogReceiverInterface.md

namespace Bold;

/// <summary>Receives log lines and progress: a log window, a file, a test recorder.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldLogReceiver
{
    [Delphi(PortStatus.Ported)] void SetProgress(int Value);
    [Delphi(PortStatus.Ported)] void SetLogHeader(string Value);
    [Delphi(PortStatus.Ported)] void SetProgressMax(int Value);
    [Delphi(PortStatus.Ported)] void Clear();
    [Delphi(PortStatus.Ported)] void Hide();
    [Delphi(PortStatus.Ported)] void Log(string s, TBoldLogType LogType);
    [Delphi(PortStatus.Ported)] void ProgressStep();
    [Delphi(PortStatus.Ported)] void Show();
    [Delphi(PortStatus.Ported)] void StartLog(string SessionName);
    [Delphi(PortStatus.Ported)] void EndLog();
    [Delphi(PortStatus.Ported)] void Sync();
    [Delphi(PortStatus.Ported)] void ProcessInterruption();
    [Delphi(PortStatus.Ported)] int ProgressMax { set => SetProgressMax(value); }
    [Delphi(PortStatus.Ported)] int Progress { set => SetProgress(value); }
    [Delphi(PortStatus.Ported)] string LogHeader { set => SetLogHeader(value); }
}
