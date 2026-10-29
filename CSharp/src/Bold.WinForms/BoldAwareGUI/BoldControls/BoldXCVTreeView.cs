// Delphi: Source/BoldAwareGUI/BoldControls/BoldXCVTreeView.pas
// Port map: CSharp/port/units/BoldXCVTreeView.md

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldXCVTreeView", Symbol = "TBoldXCVTreeView.WMCopy", Note = "WndProc (see WMCut).")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldXCVTreeView", Symbol = "TBoldXCVTreeView.WMPaste", Note = "WndProc (see WMCut).")]

namespace Bold;

/// <summary>A Bold tree view whose cut, copy and paste messages raise events (or go to the label editor while editing).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXCVTreeView : TBoldTreeView
{
    private const int WM_CUT = 0x0300;
    private const int WM_COPY = 0x0301;
    private const int WM_PASTE = 0x0302;
    private const int TVM_GETEDITCONTROL = 0x1100 + 15;

    private TNotifyEvent? fOnCut;
    private TNotifyEvent? fOnCopy;
    private TNotifyEvent? fOnPaste;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private IntPtr EditControl() => IsHandleCreated ? SendMessage(Handle, TVM_GETEDITCONTROL, IntPtr.Zero, IntPtr.Zero) : IntPtr.Zero;

    [Delphi(PortStatus.Ported)]
    protected virtual void Copy() => fOnCopy?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    protected virtual void Cut() => fOnCut?.Invoke(this);

    /// <summary>The message goes to the label editor.</summary>
    [Delphi(PortStatus.Ported)]
    private void PassToEditor(Message Message) => PostMessage(EditControl(), Message.Msg, Message.WParam, Message.LParam);

    [Delphi(PortStatus.Ported)]
    protected virtual void Paste() => fOnPaste?.Invoke(this);

    /// <summary>Cut, copy and paste go to the label editor while a label is edited, else to the events.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldXCVTreeView.WMCut", Note = "Also WMCopy and WMPaste.")]
    protected override void WndProc(ref Message m)
    {
        if (m.Msg is WM_CUT or WM_COPY or WM_PASTE)
        {
            if (EditControl() != IntPtr.Zero)
            {
                PassToEditor(m);
            }
            else if (m.Msg == WM_CUT)
            {
                Cut();
            }
            else if (m.Msg == WM_COPY)
            {
                Copy();
            }
            else
            {
                Paste();
            }
        }

        base.WndProc(ref m);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TNotifyEvent? OnCut
    {
        get => fOnCut;
        set => fOnCut = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TNotifyEvent? OnCopy
    {
        get => fOnCopy;
        set => fOnCopy = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TNotifyEvent? OnPaste
    {
        get => fOnPaste;
        set => fOnPaste = value;
    }
}
