unit BoldObjectUpgraderHandleReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldObjectUpgraderHandleReg.md }

interface

procedure Register;

implementation

{.$R *.res}

uses
  classes,
  BoldObjectUpgraderHandle,
  BoldIDEConsts;

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_PERSISTENCE, [TBoldObjectUpgraderHandle]);
end;

end.

