unit BoldUMLModelHandleReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldUMLModelHandleReg.md }

interface

procedure Register;

implementation

uses
  SysUtils,
  Classes,
  BoldIDEConsts,
  BoldModel;

{$R BoldUMLModelHandleReg.res}

procedure RegisterComponentsOnPalette;
begin
  RegisterComponents(BOLDPAGENAME_HANDLES, [TBoldModel]);
end;

procedure RegisterEditors;
begin
end;

procedure Register;
begin
  RegisterComponentsOnPalette;
  RegisterEditors;
end;

end.
