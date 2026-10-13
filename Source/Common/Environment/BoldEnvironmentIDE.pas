unit BoldEnvironmentIDE; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldEnvironmentIDE.md }

interface

implementation

uses
  BoldEnvironmentVCL,
  BoldEnvironment;

initialization
  BoldInternalRunningInIDE := true;
end.

