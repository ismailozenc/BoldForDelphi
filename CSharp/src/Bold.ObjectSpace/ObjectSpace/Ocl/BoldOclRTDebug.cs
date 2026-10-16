// Delphi: Source/ObjectSpace/Ocl/BoldOclRTDebug.pas
// Port map: CSharp/port/units/BoldOclRTDebug.md

namespace Bold;

/// <summary>A hook that can supply corrected OCL for expressions that fail at run time (set BoldOcl.BoldOCLRTDebugger).</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldOclRTDebugger : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    public abstract bool HasFixFor(string Ocl, TBoldElementTypeInfo? Context);

    [Delphi(PortStatus.Ported)]
    public abstract string GetFixFor(string Ocl, TBoldElementTypeInfo? Context);

    /// <summary>Records a failure; true when a fix is now available, so the evaluation is retried.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract bool AddFixFor(string Ocl, TBoldElementTypeInfo? Context, string ComponentPath, string Message);
}
