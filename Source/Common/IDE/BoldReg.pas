unit BoldReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldReg.md }

interface

procedure Register;

implementation

uses
  DesignIntf,
  Classes,
  BoldPropertyEditors,
  BoldTemplateExpander,
  BoldIDEConsts;

{.$R *.res}

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_MISC, [TBoldTemplateHolder]);
  RegisterComponentEditor(TBoldTemplateHolder, TBoldStringListEditor);
end;

end.
