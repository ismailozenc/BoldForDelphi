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
/// The Bold package. Its menus are in BoldPackage.vsct: Tools > Bold, and two commands on a model file in Solution
/// Explorer. Each command starts one of Bold's programs from the extension's tools folder (BoldTools); the command line
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
        if (GetGlobalService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection selection &&
            ErrorHandler.Succeeded(selection.GetCurrentSelection(out IntPtr hierarchyPointer, out uint item, out _, out IntPtr containerPointer)))
        {
            try
            {
                if (hierarchyPointer != IntPtr.Zero && item != VSConstants.VSITEMID_NIL && item != VSConstants.VSITEMID_SELECTION &&
                    item != VSConstants.VSITEMID_ROOT && Marshal.GetObjectForIUnknown(hierarchyPointer) is IVsHierarchy hierarchy &&
                    ErrorHandler.Succeeded(hierarchy.GetCanonicalName(item, out string name)) && BoldTools.IsModel(name) && File.Exists(name))
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
                if (BoldTools.IsModel(document) && File.Exists(document))
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
