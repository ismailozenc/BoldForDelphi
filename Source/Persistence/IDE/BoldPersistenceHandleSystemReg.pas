unit BoldPersistenceHandleSystemReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldPersistenceHandleSystemReg.md }

interface

procedure Register;

implementation
{.$R BoldPersistenceHandleSystem.res}
uses
  SysUtils,
  Classes,
  BoldIdeConsts,
  BoldPersistenceHandleSystem;

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_PERSISTENCE, [TBoldPersistenceHandleSystem]);
end;

end.
