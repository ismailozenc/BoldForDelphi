// Delphi: Source/Extensions/OLLE/Core/BoldOLLEController.pas
// Port map: CSharp/port/units/BoldOLLEController.md

namespace Bold;

/// <summary>
/// The persistence handle of the OLLE system: it uses the database of the application's persistence handle, with
/// a configuration of its own (a copy of the application's, with system tables named OLLE_ rather than BOLD_).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPHandleMimic : TBoldAbstractPersistenceHandleDB
{
    internal TBoldAbstractPersistenceHandleDB? fPHandle;
    private TBoldSQLDataBaseConfig? fSQLDatabaseConfig;

    /// <summary>A handle in the container when one is given (Delphi's owner).</summary>
    public TBoldPHandleMimic(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPHandleMimic.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fSQLDatabaseConfig);
        }

        base.Dispose(disposing);
    }

    /// <summary>The database of the application's persistence handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected override IBoldDataBase? GetDataBaseInterface() => fPHandle!.DatabaseInterface;

    /// <summary>The handle's own configuration, made when first asked for.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldSQLDataBaseConfig GetSQLDatabaseConfig() => fSQLDatabaseConfig ??= new TBoldSQLDataBaseConfig();
}

/// <summary>
/// Runs the OLLE system for an application: the system of the information objects, in the application's database
/// when persistent (the default) and in memory only when not.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Disposable: disposing closes the OLLE system and disposes the data module, which Delphi never freed.")]
public class TBoldOLLEController : IDisposable
{
    private readonly TdmOLL fOlleDM;
    private readonly TBoldPHandleMimic fMimicPHandle;

    /// <summary>The OLLE system on the database of the application's persistence handle.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOLLEController(TBoldAbstractPersistenceHandleDB PersistenceHandle)
    {
        fOlleDM = new TdmOLL(null);
        fMimicPHandle = new TBoldPHandleMimic(fOlleDM.Components) { Name = "BoldPHandleMimic" };
        fMimicPHandle.fPHandle = PersistenceHandle;
        fMimicPHandle.BoldModel = fOlleDM.BoldModel1;
        fMimicPHandle.SQLDatabaseConfig.AssignConfig(PersistenceHandle.SQLDatabaseConfig);
        fMimicPHandle.SQLDatabaseConfig.SystemTablePrefix = "OLLE"; // do not localize
        fOlleDM.BoldObjectInfoSystem.PersistenceHandle = fMimicPHandle;
    }

    /// <summary>Creates the OLLE tables in the application's database, leaving its other tables alone.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateDatabase() => fMimicPHandle.CreateDataBaseSchema(true);

    /// <summary>The running OLLE system (started when first asked for).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldSystem GetOLLESystem()
    {
        fOlleDM.BoldObjectInfoSystem.Active = true;
        return fOlleDM.BoldObjectInfoSystem.System!;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetPersistent() => fOlleDM.BoldObjectInfoSystem.PersistenceHandle is not null;

    /// <summary>Makes the OLLE system persistent or not; not while it runs.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetPersistent(bool Value)
    {
        if (Value != Persistent)
        {
            if (fOlleDM.BoldObjectInfoSystem.Active)
            {
                throw new EBold(SysUtils.Format(OlleConsts.sCannotChangePersistenceWhenActive, GetType().Name));
            }

            fOlleDM.BoldObjectInfoSystem.PersistenceHandle = Value ? fMimicPHandle : null;
        }
    }

    /// <summary>Closes the OLLE system (its information objects are saved as the handlers go) and disposes the data module.</summary>
    public void Dispose()
    {
        TBoldSystemHandle systemHandle = fOlleDM.BoldObjectInfoSystem;
        if (systemHandle.Active)
        {
            systemHandle.System!.Discard();
            systemHandle.Active = false;
        }

        fOlleDM.Dispose();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldSystem OLLESystem => GetOLLESystem();

    [Delphi(PortStatus.Ported)]
    public bool Persistent
    {
        get => GetPersistent();
        set => SetPersistent(value);
    }
}
