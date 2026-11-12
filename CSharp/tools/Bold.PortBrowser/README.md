# Bold Port Browser

The Delphi source and the C# port side by side, linked both ways: select a file or a symbol on one side, and the other
side shows its counterpart, with what the port did with it and why. It reads the sources each time it starts, so it
shows them as they are now.

```
dotnet run --project CSharp/tools/Bold.PortBrowser                       from anywhere in the repository (Windows)
Bold.PortBrowser.exe [repository] [--open <.pas or .cs file>] [--line <n>] [--symbol <Name or Type.Member>] [--results <folder>]
```

Without a repository it takes the one the current folder or the program is in, or the one it showed last. In Visual
Studio it is **Tools > Bold > Port Browser**, and **Show Delphi Counterpart** in the code editor's right-click menu
([`../../vs`](../../vs)).

## The window

| Part | What it shows |
|---|---|
| Left tree | The Delphi source: folders, units, types and members. |
| Right tree | The C# port: `src` and `tests`, projects, folders, files, types and members. |
| Middle | The code of both sides, read-only, with line numbers and colours. The line of the selection is selected. |
| Details | What the port did with the selection: status, the note on its `[Delphi]` marker, project, milestone, and why it was dropped or what replaced it. |
| Tests | The tests that cover the selection; a double click opens the test. |
| Counterparts | Every counterpart, when there is more than one (a type split over files, a property and its accessors). |
| Found | What **Find** found. |

A dot before a name is its port status:

| Dot | Status | Meaning |
|---|---|---|
| green, checked | Verified | Ported or replaced, and every test that covers it passed |
| light green | Ported | Ported with the same behaviour; no passing test covers it yet |
| blue | Replaced | A .NET feature or a new design does the job |
| yellow | Partial | Part of the behaviour is ported |
| orange | Stub | Declared with the Delphi name; throws `NotImplementedException` |
| red | Failing | A test that covers it failed |
| grey | Not started | No C# yet |
| dark, with a dash | Dropped | Not ported on purpose |
| purple star | New in C# | C# code with no Delphi counterpart |

"Verified" needs test results: run `dotnet test CSharp/Bold.slnx --logger trx --results-directory CSharp/TestResults`
first, or give a folder with `--results`. Without results, verified symbols show as ported.

## Working with it

| To | Do |
|---|---|
| See a unit's port | Select the `.pas` file in the left tree: its `.cs` file opens beside it. |
| See how a method was ported | Select it in either tree, or click in its code: both sides go to it. |
| Follow a name in the code | Put the cursor on it and press **F12**, or double-click it: the browser goes to its declaration and counterpart. |
| Find something by name | Type part of the name in **Find** (Ctrl+F) and press Enter. |
| See what was not ported, and why | Set **Show** to Dropped or Replaced: the trees keep only those. Select one; the C# pane says why there is no code. |
| See what is new in C# | Set **Show** to New in C#. |
| Go back | **Back** and **Forward**, or Alt+Left and Alt+Right. |
| Edit the code | **Open in Visual Studio** opens the selected file at its line. The browser itself changes nothing. |
| Read the unit's status page | **Status Page** opens `CSharp/port/units/<Unit>.md`. |
| Read the sources again | **Reload**, or F5. |

## How it is made

`Bold.PortBrowser.Core` has everything but the window: `PortIndex` builds the two trees on the port tool's analysis
(`Bold.PortTool`: the `[Delphi]` markers, `port-map.json` and the test results) and finds counterparts, declarations by
line and names; `CodeHighlighter` colours the code. `tests/Bold.PortTool.Tests/PortBrowserTests.cs` tests it on this
repository, on every platform. The window is in this project; `tests/Bold.WinForms.Tests/Tools/PortBrowserTests.cs`
shows it and starts the program.

It links by symbol, not by line: Delphi and C# differ too much for a line by line comparison to mean anything.
