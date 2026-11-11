// Bold for Visual Studio: the package that Visual Studio loads for Bold's menu commands and for .bld files.
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Bold.VisualStudio;

/// <summary>
/// The Bold package. Its menus are in BoldPackage.vsct: Tools > Bold, two commands on a model file in Solution
/// Explorer, and "Show Delphi Counterpart" in the code editor and on a source file. Each command starts one of Bold's programs from the extension's tools folder (BoldTools); the command line
/// tools stay as they are.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageIds.Package)]
[InstalledProductRegistration("#111", "#112", "1.0.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideEditorFactory(typeof(BldEditorFactory), 110, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
[ProvideEditorExtension(typeof(BldEditorFactory), BoldTools.ModelExtension, 50)]
[ProvideUIContextRule(PackageIds.BldSelected, name: "A Bold model is selected", expression: "bld",
    termNames: new[] { "bld" }, termValues: new[] { "HierSingleSelectionName:.bld$" })]
[ProvideUIContextRule(PackageIds.SourceSelected, name: "A source file is selected", expression: "source",
    termNames: new[] { "source" }, termValues: new[] { "HierSingleSelectionName:.(cs|pas)$" })]
public sealed class BoldPackage : AsyncPackage
{
    private static readonly Guid OutputPane = new("8c5f3a1d-7e2b-4d6c-9a0f-1b2c3d4e5f85");

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        RegisterEditorFactory(new BldEditorFactory());
        if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commands)
        {
            Add(commands, PackageIds.ModelEditor, () => Run(() => BoldTools.OpenModelEditor(SelectedModel())));
            Add(commands, PackageIds.OpenModel, () => Run(() => BoldTools.OpenModelEditor(SelectedModel())));
            Add(commands, PackageIds.PropagatorAnalyzer, () => Run(() => BoldTools.OpenPropagatorAnalyzer()));
            Add(commands, PackageIds.GenerateClasses, () => GenerateClasses(SelectedModel() ?? AskForModel()));
            Add(commands, PackageIds.GenerateClassesOfModel, () => GenerateClasses(SelectedModel()));
            Add(commands, PackageIds.PortBrowser, () => Run(() => BoldTools.OpenPortBrowser(SolutionFolder())));
            Add(commands, PackageIds.ShowCounterpart, ShowCounterpartOfDocument);
            Add(commands, PackageIds.ShowCounterpartOfFile, () => ShowCounterpart(SelectedFile(BoldTools.IsSource), 0));
            Add(commands, PackageIds.UserGuide, () => OpenPage(BoldTools.UserGuide));
            Add(commands, PackageIds.Tutorials, () => OpenPage(BoldTools.Tutorials));
        }
    }

    private static void Add(OleMenuCommandService commands, int id, Action execute) =>
        commands.AddCommand(new MenuCommand((_, _) => execute(), new CommandID(PackageIds.CommandSetGuid, id)));

    /// <summary>Does it, and shows what went wrong in a message box (a program that is not there, say).</summary>
    private static void Run(Action action)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            action();
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowError(e.Message);
        }
    }

    internal static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, message, "Bold for C#", OLEMSGICON.OLEMSGICON_WARNING,
            OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    /// <summary>The model file (.bld) that is selected in Solution Explorer, or else the one in the active document window; null when neither is one.</summary>
    private static string? SelectedModel()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return SelectedFile(BoldTools.IsModel);
    }

    /// <summary>The folder of the open solution, where the port browser looks for the repository; null without a solution.</summary>
    private static string? SolutionFolder()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (GetGlobalService(typeof(SDTE)) is EnvDTE.DTE dte)
        {
            try
            {
                string? solution = dte.Solution?.FullName;
                return string.IsNullOrEmpty(solution) ? null : Path.GetDirectoryName(solution);
            }
            catch (COMException)
            {
                // no solution
            }
        }

        return null;
    }

    /// <summary>The port browser at the file and line of the cursor in the active document, with its counterpart beside it.</summary>
    private static void ShowCounterpartOfDocument()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        string? file = null;
        int line = 0;
        if (GetGlobalService(typeof(SDTE)) is EnvDTE.DTE dte)
        {
            try
            {
                file = dte.ActiveDocument?.FullName;
                if (dte.ActiveDocument?.Selection is EnvDTE.TextSelection selection)
                {
                    line = selection.CurrentLine;
                }
            }
            catch (Exception e) when (e is COMException or ArgumentException)
            {
                // no active document that has a file
            }
        }

        ShowCounterpart(file, line);
    }

    private static void ShowCounterpart(string? file, int line)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!BoldTools.IsSource(file))
        {
            ShowError("The port browser shows the counterpart of a C# file (.cs) of the port or of a Delphi unit (.pas).");
            return;
        }

        Run(() => BoldTools.OpenPortBrowser(null, file, line));
    }

    /// <summary>The file that is selected in Solution Explorer, or else the one in the active document window, when it is one of the wanted kind; null otherwise.</summary>
    private static string? SelectedFile(Func<string?, bool> wanted)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (GetGlobalService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection selection &&
            ErrorHandler.Succeeded(selection.GetCurrentSelection(out IntPtr hierarchyPointer, out uint item, out _, out IntPtr containerPointer)))
        {
            try
            {
                if (hierarchyPointer != IntPtr.Zero && item != VSConstants.VSITEMID_NIL && item != VSConstants.VSITEMID_SELECTION &&
                    item != VSConstants.VSITEMID_ROOT && Marshal.GetObjectForIUnknown(hierarchyPointer) is IVsHierarchy hierarchy &&
                    ErrorHandler.Succeeded(hierarchy.GetCanonicalName(item, out string name)) && wanted(name) && File.Exists(name))
                {
                    return name;
                }
            }
            finally
            {
                if (hierarchyPointer != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPointer);
                }

                if (containerPointer != IntPtr.Zero)
                {
                    Marshal.Release(containerPointer);
                }
            }
        }

        if (GetGlobalService(typeof(SDTE)) is EnvDTE.DTE dte)
        {
            try
            {
                string? document = dte.ActiveDocument?.FullName;
                if (wanted(document) && File.Exists(document))
                {
                    return document;
                }
            }
            catch (Exception e) when (e is COMException or ArgumentException)
            {
                // no active document that has a file
            }
        }

        return null;
    }

    private static string? AskForModel()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Generate classes from a Bold model",
            Filter = "Bold models (*.bld)|*.bld|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>Runs bold-codegen on the model in the background; what it writes shows in the Output window's Bold pane.</summary>
    private void GenerateClasses(string? model)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (model is null)
        {
            return;
        }

        // What is typed in Visual Studio and not saved yet would not be in the file the generator reads.
        if (GetGlobalService(typeof(SDTE)) is EnvDTE.DTE dte)
        {
            try
            {
                dte.Documents.SaveAll();
            }
            catch (COMException)
            {
                // nothing open to save
            }
        }

        IVsOutputWindowPane? pane = Pane();
        pane?.Activate();
        Write(pane, $"Generating the classes of {model} into {BoldTools.ClassesFolder(model)}");
        _ = JoinableTaskFactory.RunAsync(async () =>
        {
            string result;
            try
            {
                int exitCode = await Task.Run(() => BoldTools.GenerateClasses(model, line => Write(pane, line)));
                result = exitCode == 0 ? "Bold: the classes are generated." : $"Bold: bold-codegen ended with exit code {exitCode}; see the Output window.";
            }
            catch (Exception e) when (e is IOException or InvalidOperationException)
            {
                result = "Bold: " + e.Message;
            }

            Write(pane, result);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            if (await GetServiceAsync(typeof(SVsStatusbar)) is IVsStatusbar statusBar)
            {
                statusBar.SetText(result);
            }
        });
    }

    /// <summary>Writes a line to the pane, from any thread: the generator's output arrives on its own.</summary>
#pragma warning disable VSTHRD010 // OutputStringThreadSafe is the pane's method for other threads
    private static void Write(IVsOutputWindowPane? pane, string line) => pane?.OutputStringThreadSafe(line + Environment.NewLine);
#pragma warning restore VSTHRD010

    /// <summary>The Bold pane of the Output window.</summary>
    private static IVsOutputWindowPane? Pane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (GetGlobalService(typeof(SVsOutputWindow)) is not IVsOutputWindow window)
        {
            return null;
        }

        Guid id = OutputPane;
        if (ErrorHandler.Failed(window.GetPane(ref id, out IVsOutputWindowPane pane)))
        {
            window.CreatePane(ref id, "Bold", fInitVisible: 1, fClearWithSolution: 0);
            window.GetPane(ref id, out pane);
        }

        return pane;
    }

    /// <summary>Opens one of the pages that come with the extension in Visual Studio.</summary>
    private void OpenPage(string file)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!File.Exists(file))
        {
            ShowError($"{Path.GetFileName(file)} is not in the extension's tools folder ({BoldTools.Folder}).");
            return;
        }

        VsShellUtilities.OpenDocument(this, file);
    }
}
