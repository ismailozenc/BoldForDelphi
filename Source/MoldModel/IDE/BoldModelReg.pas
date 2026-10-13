unit BoldModelReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldModelReg.md }

interface

procedure Register;

implementation

uses
  SysUtils,
  DesignIntf,
  Classes,
  BoldUtils,
  BoldTypeNameHandle,
  BoldAbstractModel,
  BoldIDEConsts;

{.$R BoldModelReg.res}

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_MISC, [TBoldTypeNameHandle]);
end;
end.

