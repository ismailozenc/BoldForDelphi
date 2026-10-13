unit BoldHandlesPropagationReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldHandlesPropagationReg.md }

interface

procedure Register;

implementation

{.$R *.res}

uses
  SysUtils,
  BoldUtils,
  Classes,
  DesignIntf,
  BoldIDEConsts,
  BoldGuard,
  BoldAbstractPropertyEditors,
  BoldListenerHandle,
  BoldIDAdderHandle,
  BoldSnooperHandle,
  BoldAbstractDequeuer;

procedure RegisterComponentsOnPalette;
begin
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldListenerHandle]);
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldSnooperHandle]);
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldIDAdderHandle]);
end;

procedure RegisterEditors;
begin
  RegisterPropertyEditor(TypeInfo(TBoldAbstractDequeuer), TBoldListenerHandle, 'Dequeuer', TBoldComponentPropertyIndicateMissing); // do not localize
  RegisterPropertyEditor(TypeInfo(TBoldListenerHandle), TBoldIDAdderHandle, 'BoldListener', TBoldComponentPropertyIndicateMissing); // do not localize
end;

procedure Register;
begin
  RegisterComponentsOnPalette;
  RegisterEditors;
end;

end.
