# Compiling and installing Bold for C#, step by step

For Windows 10 or 11. Every command runs in **PowerShell**, from the repository's root folder (the folder that holds `CSharp` and `Source`). Each step says what you should see, so you know when to go on.

| Part | Steps | You need it when |
| --- | --- | --- |
| A. Prepare the machine | 1 to 3 | Always |
| B. Compile and test | 4 to 6 | Always |
| C. Install | 7 to 9 | You want to use Bold in your own projects |
| D. Visual Studio | 10 to 12 | You want the Bold menus in Visual Studio |
| E. A first application | 13 | To see that it all works |
| F. Update and uninstall | 14, 15 | Later |

## A. Prepare the machine

### Step 1. Install the .NET 10 SDK

Download the **.NET 10 SDK** (x64) from <https://dotnet.microsoft.com/download/dotnet/10.0> and install it. Open a new PowerShell window and check:

```powershell
dotnet --version
```

**You should see** a version that starts with `10.0.`, such as `10.0.100`.

### Step 2. Install Git and, for part D, Visual Studio

- **Git:** <https://git-scm.com/download/win>. The defaults are fine.
- **Visual Studio 2022 or later, Community edition** (only for part D): <https://visualstudio.microsoft.com>. In its installer, tick the workload **.NET desktop development**.

### Step 3. Get the source

```powershell
git clone -b CSharp https://github.com/ismailozenc/BoldForDelphi.git
cd BoldForDelphi
```

**You should see** the folders `CSharp`, `Source`, `Doc` and `Images`. If you already have the repository:

```powershell
git checkout CSharp
git pull
```

## B. Compile and test

### Step 4. Restore the packages

```powershell
dotnet restore CSharp\Bold.slnx
```

This downloads the NuGet packages the build needs, so the machine must reach nuget.org. **You should see** no line with `error`.

### Step 5. Compile

```powershell
dotnet build CSharp\Bold.slnx --configuration Release --no-restore
```

It takes one to three minutes. **You should see**, at the end:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 6. Run the tests

```powershell
dotnet test CSharp\Bold.slnx --configuration Release --no-build
```

It takes about five minutes. The WinForms tests show windows off screen for a moment; leave the machine alone while they run. **You should see** one `Passed!` line for each of the 20 test projects, and no `Failed!` line.

To run one project only, name it:

```powershell
dotnet test CSharp\tests\Bold.WinForms.Tests --configuration Release --no-build
```

## C. Install

### Step 7. Close Visual Studio

The installer puts an extension into Visual Studio in step 8, and cannot do so while it is open.

### Step 8. Run the installer

```powershell
powershell -ExecutionPolicy Bypass -File CSharp\deploy\install.ps1 -NoBuild
```

`-NoBuild` uses what step 5 compiled. Leave it out to have the installer compile first. It does five things:

| What | Where it goes |
| --- | --- |
| Packs the libraries as NuGet packages | `CSharp\artifacts\packages` |
| Makes that folder a package source named `bold` | Your NuGet settings |
| Installs `bold-codegen` as a global tool | `%USERPROFILE%\.dotnet\tools` |
| Publishes the model editor, the propagator analyzer, the port browser and the two servers | `CSharp\artifacts\publish` |
| Makes `.bld` the model editor's file type, and installs the Visual Studio extension when Visual Studio is there | Your user settings; Visual Studio |

**You should see** the line `Bold for C# 1.0.0 is installed.`

To leave Visual Studio out, add `-NoVisualStudio`. To install only the packages and the tool, add `-NoPrograms`.

### Step 9. Check the installation

Open a **new** PowerShell window, so that it sees the new tool, then:

```powershell
dotnet nuget list source
```

**You should see** a source named `bold` that points at `...\CSharp\artifacts\packages`.

```powershell
bold-codegen CSharp\deploy\PackageSmoke\Library.bld --out $env:TEMP\bold-check
dir $env:TEMP\bold-check
```

**You should see** `Author.g.cs`, `Book.g.cs` and a few more files.

```powershell
CSharp\artifacts\publish\model-editor\Bold.ModelEditor.exe CSharp\examples\Library\Library.bld
```

**You should see** the model editor with the Library model: `Author`, `Book`, `Member` and `Loan` in its tree. A double click on any `.bld` file in Explorer now does the same.

## D. Visual Studio

Step 8 already did this when Visual Studio was installed. Do steps 10 and 11 when it reported a problem with the extension, or when you installed Visual Studio later.

### Step 10. Build and install the extension

Close Visual Studio, then:

```powershell
powershell -ExecutionPolicy Bypass -File CSharp\vs\build.ps1 -Install -NoBuild
```

**You should see** `Installed.` at the end, and the file `CSharp\artifacts\vsix\Bold.VisualStudio.vsix`.

If the build fails, the reason is in `CSharp\artifacts\vsix\logs\vsix.log`. This extension is new and was written without a Visual Studio to try it in, so this is the step most likely to need a fix. Send me the last 30 lines of that log.

### Step 11. Check the menus

Start Visual Studio.

1. Open the **Tools** menu. **You should see** a **Bold** submenu with Model Editor, Generate Classes from Model, Propagator Analyzer, User Guide and Tutorials.
2. Choose **Tools > Bold > Model Editor**. **You should see** the model editor open as its own window.
3. Open `CSharp\Bold.slnx`. In Solution Explorer, find `examples\Library\LibraryClasses` and right-click `Library.bld`. **You should see** **Open in Bold Model Editor** and **Generate Bold Classes**.
4. Double-click `Library.bld`. **You should see** the model in the model editor.

### Step 12. Check the designer

1. Create a new project: **Windows Forms App** (C#, .NET 10).
2. Right-click the project, choose **Manage NuGet Packages**, set the package source to `bold`, and install `Bold.WinForms`.
3. Build the project once, then open `Form1` in the designer and open the **Toolbox**.

**You should see** the Bold controls (`TBoldGrid`, `TBoldEdit`, `TBoldNavigator` and others) with their pictures. Handles such as `TBoldSystemHandle` and `TBoldListHandle` go to the tray under the form.

### Step 12a. Look at the port in the port browser

```powershell
CSharp\artifacts\publish\port-browser\Bold.PortBrowser.exe
```

Or, in Visual Studio, **Tools > Bold > Port Browser**. It takes a few seconds to read both code bases.

**You should see** the Delphi source in the left tree and the C# port in the right one. In the left tree open `Source\ObjectSpace\BORepresentation\BoldSystem.pas`, then `TBoldSystem`, and select `UpdateDatabase`: both code panes go to the method, the Delphi one at `procedure UpdateDatabase;` and the C# one at `public void UpdateDatabase()`. Select `Source\Common\Support\BoldGuard.pas`: the C# pane says it was replaced by `using` and the garbage collector. `CSharp\tools\Bold.PortBrowser\README.md` says what else it does.

## E. A first application

### Step 13. Build an application on the installed packages

In a folder outside the repository:

```powershell
dotnet new console -n Library
cd Library
dotnet add package Bold.Persistence.Sql
dotnet add package Bold.Handles
dotnet add package Microsoft.Data.Sqlite
```

Then follow **A first application** in the [user guide](USER_GUIDE.md): copy the model file, replace `Program.cs`, and run:

```powershell
dotnet run
```

**You should see** `1` and `The Dispossessed`.

To see more, run the [tutorials](TUTORIALS.md) from the repository's `CSharp` folder, starting with:

```powershell
cd CSharp
dotnet run --project examples\Library\Basics
dotnet run --project examples\Library\Desk
```

## F. Update and uninstall

### Step 14. Update to a newer revision

Close Visual Studio, then repeat steps 5, 6 and 8:

```powershell
git pull
dotnet build CSharp\Bold.slnx --configuration Release
dotnet test CSharp\Bold.slnx --configuration Release --no-build
powershell -ExecutionPolicy Bypass -File CSharp\deploy\install.ps1 -NoBuild
```

The installer removes the old packages from NuGet's cache, so your projects get the new ones on their next restore.

### Step 15. Uninstall

Close Visual Studio, then:

```powershell
powershell -ExecutionPolicy Bypass -File CSharp\deploy\install.ps1 -Uninstall
```

It removes the package source, `bold-codegen`, the cached packages, the `.bld` file type and the Visual Studio extension. The repository stays as it is.

## When something goes wrong

| What you see | Why | What to do |
| --- | --- | --- |
| `running scripts is disabled on this system` | PowerShell's execution policy | Start the script as shown, with `powershell -ExecutionPolicy Bypass -File ...` |
| `Bold for C# needs the .NET 10 SDK` | An older SDK is first on the path, or none | Install the .NET 10 SDK (step 1) and open a new window |
| `error NU1301` or `Unable to load the service index` | nuget.org cannot be reached | Check the network or proxy, then run step 4 again |
| `bold-codegen` is not recognized | The window was open before the tool was installed | Open a new PowerShell window |
| `VSIXInstaller answered 2004` or another number | Visual Studio is open, or it rejected the extension | Close every Visual Studio window and run step 10 again; a double click on the `.vsix` file shows the reason |
| `Visual Studio was not found` | No Visual Studio 2022 or later on the machine | Install it (step 2), or go on without part D |
| The Bold menu is missing in Visual Studio | The extension is disabled or did not load | Look in **Extensions > Manage Extensions > Installed** for "Bold for C#"; then run `devenv /log` and send me the lines with "Bold" from `%APPDATA%\Microsoft\VisualStudio\<version>\ActivityLog.xml` |
| The model editor does not start from Visual Studio | The .NET 10 Desktop Runtime is missing | It comes with the .NET 10 SDK: check step 1 |
| A WinForms test fails with a window in its name | Something took the focus while the tests ran | Run that test project again and leave the machine alone |
| Old behaviour after an update | A project still uses packages from NuGet's cache | Run step 8 again, then `dotnet restore --force` in your project |
