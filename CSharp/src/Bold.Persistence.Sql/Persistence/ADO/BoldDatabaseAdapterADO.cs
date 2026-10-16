// Delphi: Source/Persistence/ADO/BoldDatabaseAdapterADO.pas
// Port map: CSharp/port/units/BoldDatabaseAdapterADO.md

namespace Bold;

/// <summary>A database adapter for an ADO connection (any ADO.NET provider through TADOConnection).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDatabaseAdapterADO(System.ComponentModel.IContainer? aOwner = null) : TBoldAbstractDatabaseAdapter(aOwner)
{
    private TBoldADOConnection? fBoldDatabase;

    /// <summary>Tells subscribers and frees the database.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldDatabaseAdapterADO.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Changed();
            FreePublisher();
            fBoldDatabase = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)] private void SetDataBase(TADOConnection? Value) => InternalDatabase = Value;
    [Delphi(PortStatus.Ported)] private TADOConnection? GetDataBase() => (TADOConnection?)InternalDatabase;

    [Delphi(PortStatus.Ported)]
    protected override void ReleaseBoldDatabase()
    {
        fBoldDatabase?.ReleaseCachedObjects();
        fBoldDatabase = null;
    }

    /// <summary>The database for the connection, made on first use; raises without a connection.</summary>
    [Delphi(PortStatus.Ported)]
    protected override IBoldDataBase GetDataBaseInterface()
    {
        if (Connection is null)
        {
            throw new EBold(SysUtils.Format(ADOConsts.sAdapterNotConnected, GetType().Name));
        }

        return fBoldDatabase ??= new TBoldADOConnection(Connection, SQLDatabaseConfig);
    }

    [Delphi(PortStatus.Ported)] public TADOConnection? Connection { get => GetDataBase(); set => SetDataBase(value); }
}
