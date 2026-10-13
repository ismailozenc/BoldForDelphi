unit BoldPersistenceHandleReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldPersistenceHandleReg.md }

interface

procedure Register;

implementation

uses
  SysUtils,
  DesignIntf,
  BoldAbstractPropertyEditors,
  BoldAbstractModel,
  BoldPersistenceHandle,
  BoldPersistenceHandlePassthrough;

procedure Register;
begin
  RegisterPropertyEditor(TypeInfo(TBoldAbstractModel), TBoldPersistenceHandle, 'BoldModel', TBoldComponentPropertyIndicateMissing); // do not localize
  RegisterPropertyEditor(TypeInfo(TBoldPersistenceHandle), TBoldPersistenceHandlePassthrough, 'NextPersistenceHandle', TBoldComponentPropertyIndicateMissing); // do not localize
end;

end.
