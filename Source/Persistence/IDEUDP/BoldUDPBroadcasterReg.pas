unit BoldUDPBroadcasterReg; { C# port: not ported (IDE registration); see CSharp/port/units/BoldUDPBroadcasterReg.md }

interface

procedure Register;

implementation

uses
  Classes,
  BoldIDEConsts,
  BoldUDPModificationBroadCaster;

{$R BoldUDPBroadCaster.res}

procedure Register;
begin
  RegisterComponents(BOLDPAGENAME_OSS_CMS, [TBoldUDPModificationBroadcaster]);
end;

end.

