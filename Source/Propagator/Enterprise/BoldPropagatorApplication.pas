unit BoldPropagatorApplication; { C# port: CSharp/src/Bold.Propagator.Server/Propagator/Enterprise/BoldPropagatorApplication.cs; symbols and status: CSharp/port/units/BoldPropagatorApplication.md }

interface
uses
  SysUtils,
  BoldUtils,
  BoldAdvancedPropagator,
  BoldPropagatorServer,
  dialogs,
  forms;

implementation

initialization
  try
    TBoldPropagatorServer.Instance.Initialize;
  except on E: Exception do
    begin
      showmessage(Format('%s', [E.Message]));
      Forms.Application.Terminate;
    end;
  end;
finalization
  if not IsLibrary then
    TBoldPropagatorServer.FreeSingleton;
end.
