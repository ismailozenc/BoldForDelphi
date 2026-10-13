unit BoldXMLReg; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldXMLReg.md }

interface
uses
  Classes;

procedure Register;

implementation

uses
  BoldXMLProducers;

procedure Register;
begin
  RegisterComponents('Bold XML', [TBoldXMLProducer]); // do not localize
end;

end.

