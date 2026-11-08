// Delphi: Source/Extensions/OLLE/Core/BoldOLLEHandles.pas
// Port map: CSharp/port/units/BoldOLLEHandles.md

namespace Bold;

/// <summary>
/// OLLE as a component: the OLLE controller on the application's persistence handle, and the handlers that lend
/// this application's objects (own objects) and take in objects lent to it (foreign objects).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLLEHandle : TBoldHandle
{
    private TBoldOLLEController? fOLLEController;
    private TBoldOwnObjectHandler? fOwnObjectHandler;
    private TBoldForeignObjectHandler? fForeignObjectHandler;

    /// <summary>A handle in the container when one is given (Delphi's owner).</summary>
    public TBoldOLLEHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOLLEHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fOLLEController);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldForeignObjectHandler GetForeignObjectHandler()
    {
        if (fForeignObjectHandler is null)
        {
            fForeignObjectHandler = new TBoldForeignObjectHandler();
            fForeignObjectHandler.OllSystem = OLLEController.OLLESystem;
            fForeignObjectHandler.PController = ApplicationPersistenceHandle!.PersistenceControllerDefault;
        }

        return fForeignObjectHandler;
    }

    /// <summary>The OLLE controller.</summary>
    [Delphi(PortStatus.Ported)]
    protected override object? GetHandledObject() => GetOLLEController();

    [Delphi(PortStatus.Ported)]
    private TBoldOLLEController GetOLLEController() => fOLLEController ??= new TBoldOLLEController(ApplicationPersistenceHandle!);

    [Delphi(PortStatus.Ported)]
    private TBoldOwnObjectHandler GetOwnObjectHandler()
    {
        if (fOwnObjectHandler is null)
        {
            fOwnObjectHandler = new TBoldOwnObjectHandler();
            fOwnObjectHandler.OllSystem = OLLEController.OLLESystem;
            fOwnObjectHandler.PController = ApplicationPersistenceHandle!.PersistenceControllerDefault;
        }

        return fOwnObjectHandler;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetPersistent() => GetOLLEController().Persistent;

    /// <summary>Nothing, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Loaded()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetPersistent(bool Value) => GetOLLEController().Persistent = Value;

    [Delphi(PortStatus.Ported)]
    public bool Persistent
    {
        get => GetPersistent();
        set => SetPersistent(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLLEController OLLEController => GetOLLEController();
    [Delphi(PortStatus.Ported)] public TBoldOwnObjectHandler OwnObjectHandler => GetOwnObjectHandler();
    [Delphi(PortStatus.Ported)] public TBoldForeignObjectHandler ForeignObjectHandler => GetForeignObjectHandler();

    /// <summary>The persistence handle of the application, whose database holds the OLLE tables too.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldAbstractPersistenceHandleDB? ApplicationPersistenceHandle { get; set; }
}
