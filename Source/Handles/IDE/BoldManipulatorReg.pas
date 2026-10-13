unit BoldManipulatorReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldManipulatorReg.md }

interface

procedure Register;

implementation

uses
  SysUtils,
  BoldUtils,
  Classes,
  BoldDefs,
  BoldIDEConsts,
  BoldManipulators;

procedure RegisterComponentsOnPalette;
begin
  RegisterComponents(BOLDPAGENAME_MISC, [TBoldManipulator]);
end;

procedure Register;
begin
  RegisterComponentsOnPalette;
end;

end.
