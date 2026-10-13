# Bold for C#

The C# port of Bold for Delphi, on .NET 10. The Delphi source in [`../Source`](../Source) is the specification; this folder holds the port, its tests and the tools that link the two.

| Where | What |
|---|---|
| [`Bold.slnx`](Bold.slnx) | The solution: every project below. |
| [`src/`](src) | One project per layer, from `Bold.Core` up to `Bold.ModelEditor`. A Delphi unit `Source/Common/Core/BoldDefs.pas` becomes `src/Bold.Core/Common/Core/BoldDefs.cs`. |
| [`tests/`](tests) | NUnit test projects, one per source project, with the same folder layout, and the test models in [`tests/Fixtures/Models`](tests/Fixtures/Models). |
| [`port/PORT_STATUS.md`](port/PORT_STATUS.md) | Port status of every Delphi unit, by milestone and project. |
| [`port/units/`](port/units) | One page per Delphi unit: every type, method, property and constant, linked to its Delphi line, its C# code and its tests, with a status. |
| [`port/port-map.json`](port/port-map.json) | Where each Delphi unit goes: project and milestone, or why it is not ported. |
| [`tools/Bold.PortTool`](tools/Bold.PortTool) | Reads both code bases, writes the status pages, keeps the links in the sources current, and checks them. |
| [`PORTING.md`](PORTING.md) | How we port: names, status markers, tests and the commands to run. |

## Build and test

```
dotnet build CSharp/Bold.slnx
dotnet test CSharp/Bold.slnx --logger trx --results-directory CSharp/TestResults
dotnet run --project CSharp/tools/Bold.PortTool -- report
```

`Bold.WinForms` and `Bold.ModelEditor` target `net10.0-windows`; build them on Windows. GitHub Actions builds and tests everything on Windows for every push ([`.github/workflows/csharp.yml`](../.github/workflows/csharp.yml)) and shows the port status on the job page.

## Where things came from

Every ported element carries a `[Delphi(PortStatus.…)]` attribute, and every C# file starts with the Delphi file it ports. In the other direction, every Delphi unit has a comment on its `unit` line naming its C# file, and every `FOLDER_INFO.md` in `Source` lists where its units went. [PORTING.md](PORTING.md) explains the details.
