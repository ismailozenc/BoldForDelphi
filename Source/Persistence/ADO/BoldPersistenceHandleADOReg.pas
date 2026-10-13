unit BoldPersistenceHandleADOReg; { C# port: not ported (IDE registration); see CSharp/port/units/BoldPersistenceHandleADOReg.md }

interface

procedure Register;

implementation

{$R BoldPersistenceHandleADO.res}

uses
  SysUtils,
  Classes,
  BoldDatabaseAdapterADO,
  BoldPersistenceHandleADO,
  BoldIDEConsts;

procedure Register;
begin
	{$WARNINGS OFF}
  RegisterComponents(BOLDPAGENAME_DEPRECATED, [TBoldPersistenceHandleADO]);
  {$WARNINGS ON}
  RegisterComponents(BOLDPAGENAME_PERSISTENCE, [TBoldDatabaseAdapterADO]);
end;

end.
