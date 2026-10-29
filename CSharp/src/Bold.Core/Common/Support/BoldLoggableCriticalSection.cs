// Delphi: Source/Common/Support/BoldLoggableCriticalSection.pas
// Port map: CSharp/port/units/BoldLoggableCriticalSection.md

namespace Bold;

/// <summary>A critical section that writes each acquire and release to the thread log (see <see cref="BoldThreadSafeLog.BoldLogThread"/>).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLoggableCriticalSection : TCriticalSection
{
    private readonly string fName;

    [Delphi(PortStatus.Ported)]
    public TBoldLoggableCriticalSection(string name)
    {
        fName = name;
    }

    /// <summary>The name the thread log shows.</summary>
    public string Name => fName;

    [Delphi(PortStatus.Ported)]
    public override void Acquire()
    {
        BoldThreadSafeLog.BoldLogThread("L=" + fName);
        base.Acquire();
    }

    [Delphi(PortStatus.Ported)]
    public override void Release()
    {
        base.Release();
        BoldThreadSafeLog.BoldLogThread("U=" + fName);
    }
}
