unit BoldAFPPluggableReg; { C# port: CSharp/src/Bold.WinForms/BoldAwareGUI/FormGen/BoldAFPPluggableReg.cs; symbols and status: CSharp/port/units/BoldAFPPluggableReg.md }

interface

procedure Register;

implementation

uses
  Classes,
  BoldAFPPluggable,
  BoldIDEConsts;

{$R *.res}

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_MISC, [TBoldPlaceableAFP]);
end;

end.
