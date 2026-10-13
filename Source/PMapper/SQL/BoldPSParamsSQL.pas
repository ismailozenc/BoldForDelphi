unit BoldPSParamsSQL; { C# port: CSharp/src/Bold.Persistence.Sql/PMapper/SQL/BoldPSParamsSQL.cs; symbols and status: CSharp/port/units/BoldPSParamsSQL.md }

interface

uses
  BoldDefs,
  BoldDBInterfaces,
  BoldPSParams;

type
  TBoldPSSQLParams = class(TBoldPSParams)
  private
    fDatabase: IBoldDataBase;
    fIgnoreUnknownTables: Boolean;
  public
    property Database: IBoldDataBase read fDatabase write fDatabase;
    property IgnoreUnknownTables: Boolean read fIgnoreUnknownTables write fIgnoreUnknownTables;
  end;


implementation

end.
