// Delphi: Source/PMapper/SQL/BoldPSParamsSQL.pas
// Port map: CSharp/port/units/BoldPSParamsSQL.md

namespace Bold;

/// <summary>Storage creation parameters for SQL: the database, and whether to ignore tables the model does not know.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPSSQLParams : TBoldPSParams
{
    private IBoldDataBase? fDatabase;
    private bool fIgnoreUnknownTables;

    [Delphi(PortStatus.Ported)] public IBoldDataBase? Database { get => fDatabase; set => fDatabase = value; }
    [Delphi(PortStatus.Ported)] public bool IgnoreUnknownTables { get => fIgnoreUnknownTables; set => fIgnoreUnknownTables = value; }
}
