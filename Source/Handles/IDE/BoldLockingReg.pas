unit BoldLockingReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldLockingReg.md }

interface

procedure Register;

implementation

uses
  SysUtils,
  BoldUtils,
  Classes,
  BoldDefs,
  BoldIDEConsts,
  BoldLockingHandles;

{.$R *.res}

procedure RegisterComponentsOnPalette;
begin
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldLockingHandle]);
end;

procedure Register;
begin
  RegisterComponentsOnPalette;
end;

end.
