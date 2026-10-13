unit BoldComReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldComReg.md }

interface

procedure Register;

implementation
{$R BoldComReg.Res}
uses
  Classes,
  DesignIntf,
  BoldComServerHandles,
  BoldComClientHandles,
  BoldComEditors,
  BoldIDEConsts,
  BoldObjectNamePropertyEditor;

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_COM,
    [TBoldComServerHandle,
     TBoldComServerObjectHandle,
     TBoldComConnectionHandle,
     TBoldComClientObjectHandle]);

  RegisterComponentEditor(TBoldComServerHandle, TBoldComServerHandleComponentEditor);

  RegisterPropertyEditor(TypeInfo(string), TBoldComExportHandle,
    'ServerClass', TBoldComServerClassPropertyEditor); // do not localize
  RegisterPropertyEditor(TypeInfo(String), TBoldComClientObjectHandle, 'Objectname', TBoldObjectNameProperty); // do not localize
end;

end.
