// Opening a model file (.bld) in Visual Studio opens it in the Bold model editor.
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Bold.VisualStudio;

/// <summary>
/// The editor of .bld files. The model editor is a program of its own (a .NET 10 WinForms program, which cannot be loaded
/// into Visual Studio, a .NET Framework program), so the factory starts it on the file and tells Visual Studio that no
/// document window is to be opened. "Open With..." still offers Visual Studio's text editor for the file.
/// </summary>
[Guid(PackageIds.EditorFactory)]
public sealed class BldEditorFactory : IVsEditorFactory
{
    public int SetSite(IOleServiceProvider psp) => VSConstants.S_OK;

    public int Close() => VSConstants.S_OK;

    /// <summary>The factory has the primary view only; for the others (the code view, the text view) Visual Studio takes another editor.</summary>
    public int MapLogicalView(ref Guid rguidLogicalView, out string? pbstrPhysicalView)
    {
        pbstrPhysicalView = null;
        return rguidLogicalView == VSConstants.LOGVIEWID_Primary ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
    }

    public int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid,
        IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string? pbstrEditorCaption, out Guid pguidCmdUI,
        out int pgrfCDW)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppunkDocView = IntPtr.Zero;
        ppunkDocData = IntPtr.Zero;
        pbstrEditorCaption = null;
        pguidCmdUI = Guid.Empty;
        pgrfCDW = 0;
        if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
        {
            return VSConstants.E_INVALIDARG;
        }

        if (punkDocDataExisting != IntPtr.Zero)
        {
            // The file is open in another editor of Visual Studio (its text editor, say): that one keeps it.
            return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
        }

        try
        {
            BoldTools.OpenModelEditor(pszMkDocument);
        }
        catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            BoldPackage.ShowError(e.Message);
        }

        // Nothing opens in Visual Studio, and that is no error to report: the answer of a user who cancelled.
        return VSConstants.OLE_E_PROMPTSAVECANCELLED;
    }
}
