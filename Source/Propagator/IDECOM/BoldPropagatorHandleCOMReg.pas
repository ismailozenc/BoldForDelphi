unit BoldPropagatorHandleCOMReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldPropagatorHandleCOMReg.md }

interface

procedure Register;

implementation

{$R *.res}

uses
  Classes,
  BoldPropagatorHandleCOM,
  BoldIDEConsts;

procedure RegisterComponentsOnPalette;
begin
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldPropagatorHandleCom]);
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
