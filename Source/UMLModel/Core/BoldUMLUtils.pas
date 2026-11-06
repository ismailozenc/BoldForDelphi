unit BoldUMLUtils; { C# port: CSharp/src/Bold.ModelEditor.Core/UMLModel/Core/BoldUMLUtils.cs; symbols and status: CSharp/port/units/BoldUMLUtils.md }

interface

uses
  BoldSQLDatabaseConfig,
  BoldModel;

function SQLDataBaseConfigforModel(BoldModel: TBoldModel): TBoldSQLDataBaseConfig;

implementation

uses
  BoldHandle,
  BoldAbstractPersistenceHandleDB;

function SQLDataBaseConfigforModel(BoldModel: TBoldModel): TBoldSQLDataBaseConfig;
var
  i: integer;
  temp: TBoldAbstractPersistenceHandleDB;
begin
  result := nil;
  for i := 0 to BoldHandle.BoldHandleList.Count - 1 do
    if (BoldHandleList[i] is TBoldAbstractPersistenceHandleDB) then
    begin
      temp := BoldHandleList[i] as TBoldAbstractPersistenceHandleDB;
      if (temp.BoldModel = BoldModel) then
      begin
        result := temp.SQLDataBaseConfig;
        break;
      end;
    end;
end;

end.

