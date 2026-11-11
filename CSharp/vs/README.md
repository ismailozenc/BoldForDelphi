# Bold for Visual Studio

A Visual Studio extension (VSIX) that puts Bold's tools in Visual Studio's menus, for Visual Studio 2022 and later
(Community, Professional and Enterprise). The command line tools stay as they are; the extension starts the same
programs.

## What it adds

| Where | What |
|---|---|
| **Tools > Bold > Model Editor** | Opens the Bold model editor, on the selected model when a `.bld` file is selected. |
| **Tools > Bold > Generate Classes from Model...** | Runs `bold-codegen` on the selected model, or on one you pick; the classes are written next to the model, and what the generator says shows in the Output window's **Bold** pane. |
| **Tools > Bold > Propagator Analyzer** | Starts the propagator analyzer. |
| **Tools > Bold > Port Browser** | Opens the [port browser](../tools/Bold.PortBrowser/README.md) on the repository of the open solution: the Delphi source and the C# port side by side, linked both ways. |
| **Show Delphi Counterpart**, in the code editor's right-click menu and on a `.cs` or `.pas` file in Solution Explorer | Opens the port browser at that file and the line of the cursor, with the counterpart beside it. |
| **Tools > Bold > User Guide**, **Tutorials** | Open the pages in Visual Studio. |
| A double click on a `.bld` file | Opens the model in the model editor. **Open With...** still offers Visual Studio's text editor. |
| Right click on a `.bld` file in Solution Explorer | **Open in Bold Model Editor** and **Generate Bold Classes**. |
| The WinForms designer's toolbox and component tray | Bold's controls and handles show with the pictures of Delphi's component palette. This comes with the `Bold.WinForms` and `Bold.Handles` packages, not with the extension. |

The commands are also in Visual Studio's command window and keyboard options, as `Bold.ModelEditor`,
`Bold.GenerateClasses`, `Bold.PropagatorAnalyzer`, `Bold.PortBrowser`, `Bold.UserGuide`, `Bold.Tutorials`,
`Bold.OpenModel`, `Bold.GenerateClassesOfModel`, `Bold.ShowCounterpart` and `Bold.ShowCounterpartOfFile`.

The model editor opens as a window of its own, started by Visual Studio. It cannot be a tab inside Visual Studio: it is
a .NET 10 WinForms program, and Visual Studio itself runs on .NET Framework.

## Install

Close Visual Studio, then, from the repository:

```
powershell -ExecutionPolicy Bypass -File CSharp\vs\build.ps1 -Install
```

[`build.ps1`](build.ps1) publishes Bold's programs, builds the extension with Visual Studio's MSBuild into
`CSharp\artifacts\vsix\Bold.VisualStudio.vsix`, and installs it. Without `-Install` it only builds; a double click on
the `.vsix` file installs it too, and the file can be copied to another machine. `-Uninstall` removes the extension, as
does **Extensions > Manage Extensions** in Visual Studio.

`CSharp\deploy\install.ps1` runs this for you when it finds Visual Studio (`-NoVisualStudio` leaves it out).

It needs Windows, the .NET 10 SDK, and Visual Studio with any workload; the Visual Studio SDK comes as NuGet packages.
The machine that runs the extension needs the .NET 10 Desktop Runtime for the model editor, which the .NET 10 SDK
includes.

## How it is made

| File | What |
|---|---|
| [`Bold.VisualStudio/BoldPackage.cs`](Bold.VisualStudio/BoldPackage.cs) | The package Visual Studio loads: the handlers of the menu commands. |
| [`Bold.VisualStudio/BoldPackage.vsct`](Bold.VisualStudio/BoldPackage.vsct) | The menus. |
| [`Bold.VisualStudio/BldEditorFactory.cs`](Bold.VisualStudio/BldEditorFactory.cs) | The editor of `.bld` files: it starts the model editor on the file. |
| [`Bold.VisualStudio/BoldTools.cs`](Bold.VisualStudio/BoldTools.cs) | Finds and starts Bold's programs in the extension's `tools` folder. It needs no Visual Studio; `tests/Bold.Examples.Tests` compiles it in and runs `bold-codegen` through it. |
| [`Bold.VisualStudio/source.extension.vsixmanifest`](Bold.VisualStudio/source.extension.vsixmanifest) | The extension's name, version, and the Visual Studio versions it installs into. |

The project is not in `Bold.slnx`: it is a .NET Framework library built with Visual Studio's MSBuild, since the VSIX
tasks and the menu compiler are Windows programs. `dotnet build CSharp/vs/Bold.VisualStudio` compiles the code alone, on
any system, as a check. To try the extension on programs built elsewhere, set the environment variable `BOLD_TOOLS` to
their folder before starting Visual Studio.

## Check it after a change

The extension was written without a Visual Studio at hand: its code compiles against the Visual Studio SDK and its
launcher is tested, but the VSIX build and the menus have to be checked in Visual Studio.

1. `powershell -ExecutionPolicy Bypass -File CSharp\vs\build.ps1 -Install` ends with "Installed".
2. In Visual Studio, **Tools > Bold** is there, and **Model Editor** opens the editor.
3. In a project with a `.bld` file: a double click opens the model in the editor, and the right-click menu has the two Bold commands, on `.bld` files only.
4. **Generate Bold Classes** writes the `.g.cs` files next to the model, and the Output window's Bold pane shows the generator's lines.
5. With `CSharp\Bold.slnx` open: **Tools > Bold > Port Browser** opens the browser on the repository, and **Show Delphi Counterpart** in a `.cs` file's right-click menu opens it at that line.
6. In a WinForms project that references `Bold.WinForms`, the toolbox lists the Bold controls with their pictures.

If the build fails, `CSharp\artifacts\vsix\logs\vsix.log` has MSBuild's output.
