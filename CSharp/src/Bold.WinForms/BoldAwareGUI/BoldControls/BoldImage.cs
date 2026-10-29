// Delphi: Source/BoldAwareGUI/BoldControls/BoldImage.pas
// Port map: CSharp/port/units/BoldImage.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldImage", Symbol = "TBoldImage.WMCopy", Note = "WndProc (see WMCut).")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldImage", Symbol = "TBoldImage.WMPaste", Note = "WndProc (see WMCut).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImage", Symbol = "TBoldImage.WMEraseBkgnd", Note = "Skipped erasing the background against flicker; the control paints everything in OnPaint (AllPaintingInWmPaint) and double buffers.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldImage", Symbol = "TBoldImage.GetPalette", Note = "Gave the picture's Windows GDI palette (HPALETTE) to realize; GDI+ draws without palettes.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImage", Symbol = "TBoldImage.OnResize", Note = "The control's Resize event.")]

namespace Bold;

/// <summary>
/// A control that shows a picture: the element (a blob) through its viewer controller, which picks the view adapter
/// for the content type. The picture can be stretched, scaled and centered; the clipboard keys cut, copy and paste
/// it (unless read-only), and a file can be loaded into it.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms control that paints itself.")]
public class TBoldImage : Control, IBoldOCLComponent
{
    private const int WM_CUT = 0x0300;
    private const int WM_COPY = 0x0301;
    private const int WM_PASTE = 0x0302;
    private const int WS_BORDER = 0x00800000;
    private const int WS_EX_CLIENTEDGE = 0x00000200;

    private TBoldViewerFollowerController? fBoldProperties;
    private TBoldElementHandleFollower? fHandleFollower;
    private bool fReadOnly;
    private BorderStyle fBorderStyle;
    private bool fAutoSize;
    private bool fDrawFocus;
    private string fContentTypeOnPaste = "";
    private TBoldStretchMode fStretchMode;
    private bool fCenter;
    private bool fQuickDraw;
    private double fScale;
    private Rectangle fDisplayRect;

    [Delphi(PortStatus.Ported)]
    public TBoldImage()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.Opaque, true);
        Width = 105;
        Height = 105;
        TabStop = true;
        BackColor = SystemColors.Window;
        fBoldProperties = new TBoldViewerFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fBorderStyle = BorderStyle.Fixed3D;
        fReadOnly = false;
        fAutoSize = false;
        fDrawFocus = true;
        fScale = 1.0;
        AllowDrop = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
        }

        base.Dispose(disposing);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (fHandleFollower is not null)
        {
            object match = (object?)FindForm() ?? this;
            fHandleFollower.MatchObject = match;
            fHandleFollower.Follower.MatchObject = match;
        }
    }

    /// <summary>A single border is a line; a 3D one is the sunken client edge (Delphi's bsSingle with Ctl3D).</summary>
    [Delphi(PortStatus.Ported, Note = "WinForms' BorderStyle: FixedSingle draws a line, Fixed3D the client edge.")]
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams Params = base.CreateParams;
            if (fBorderStyle == BorderStyle.Fixed3D)
            {
                Params.ExStyle |= WS_EX_CLIENTEDGE;
            }
            else if (fBorderStyle == BorderStyle.FixedSingle)
            {
                Params.Style |= WS_BORDER;
            }

            return Params;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldViewerFollowerController Value) => fBoldProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractViewAdapter? GetViewer() => BoldProperties.GetCurrentViewer(Follower);

    [Delphi(PortStatus.Ported)]
    private void SetViewer(TBoldAbstractViewAdapter? Value) => BoldProperties.MayHaveChanged(Value, Follower);

    [Delphi(PortStatus.Ported)]
    private void SetBorderStyle(BorderStyle Value)
    {
        if (fBorderStyle != Value)
        {
            fBorderStyle = Value;
            RecreateHandle();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetAutoSize(bool Value)
    {
        if (fAutoSize != Value)
        {
            fAutoSize = Value;
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDrawFocus(bool Value)
    {
        if (fDrawFocus != Value)
        {
            fDrawFocus = Value;
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetScale() => (int)Math.Round(fScale * 100);

    /// <summary>Scales the picture (in percent); the stretch mode becomes "to scale".</summary>
    [Delphi(PortStatus.Ported)]
    private void SetScale(int Value)
    {
        if (Value != fScale && Value > 0)
        {
            fScale = Value / 100.0;
            StretchMode = TBoldStretchMode.bsmStretchToScale;
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetStretchMode(TBoldStretchMode Value)
    {
        if (Value != fStretchMode)
        {
            fStretchMode = Value;
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.CMEnter")]
    protected override void OnEnter(EventArgs e)
    {
        if (fDrawFocus)
        {
            Invalidate(); // Draw the focus marker
        }

        base.OnEnter(e);
    }

    /// <summary>Leaving applies the change under the exit policy (the focus stays when that fails).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.CMExit")]
    protected override void OnLeave(EventArgs e)
    {
        try
        {
            if (Follower.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
            {
                Follower.Apply();
            }
        }
        catch (Exception)
        {
            Focus();
            throw;
        }

        if (fDrawFocus)
        {
            Invalidate(); // Erase the focus marker
        }

        base.OnLeave(e);
    }

    /// <summary>A click gives the image the focus.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.WMLButtonDown")]
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && TabStop && CanFocus)
        {
            Focus();
        }

        base.OnMouseDown(e);
    }

    /// <summary>The cut, copy and paste messages cut, copy and paste the picture.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.WMCut", Note = "Also WMCopy and WMPaste.")]
    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_CUT:
                CutToClipboard();
                break;
            case WM_COPY:
                CopyToClipboard();
                break;
            case WM_PASTE:
                PasteFromClipboard();
                break;
            default:
                base.WndProc(ref m);
                break;
        }
    }

    /// <summary>A new size draws the picture again (the Resize event is raised first).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.WMSize", Note = "OnSizeChanged; Delphi's OnResize is the Resize event.")]
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Invalidate();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.CMTextChanged")]
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        // FIXME Invalidate to redraw Caption  when there is no picture
    }

    /// <summary>Shift+Insert pastes, Ctrl+Insert copies, Shift+Delete cuts.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.KeyDown")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Insert:
                if (e.Shift)
                {
                    PasteFromClipboard();
                }
                else if (e.Control)
                {
                    CopyToClipboard();
                }

                break;
            case Keys.Delete:
                if (e.Shift)
                {
                    CutToClipboard();
                }

                break;
        }
    }

    /// <summary>Ctrl+X cuts, Ctrl+C copies, Ctrl+V pastes; Escape drops the change.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        switch (e.KeyChar)
        {
            case (char)24: // ^X
                CutToClipboard();
                break;
            case (char)3: // ^C
                CopyToClipboard();
                break;
            case (char)22: // ^V
                PasteFromClipboard();
                break;
            case BoldDefs.BOLDESC:
                Follower.DiscardChange();
                break;
        }
    }

    /// <summary>The rectangle the picture is drawn in, as the stretch mode, scale, auto size and centering say.</summary>
    [Delphi(PortStatus.Ported)]
    public void CalcDisplayRect()
    {
        TBoldAbstractViewAdapter? viewer = Viewer;
        int viewerWidth = viewer?.Width() ?? 0, viewerHeight = viewer?.Height() ?? 0;
        Rectangle CR = ClientRectangle;
        switch (fStretchMode)
        {
            case TBoldStretchMode.bsmNoStretch:
                fScale = 1.0;
                fDisplayRect = new Rectangle(0, 0, viewerWidth, viewerHeight);
                break;
            case TBoldStretchMode.bsmStretchProportional:
                if (viewerWidth > 0 && viewerHeight > 0)
                {
                    double ScaleX = (double)CR.Width / viewerWidth;
                    double ScaleY = (double)CR.Height / viewerHeight;
                    fScale = ScaleX < ScaleY ? ScaleX : ScaleY;
                }
                else
                {
                    fScale = 1.0;
                }

                fDisplayRect = new Rectangle(0, 0, (int)Math.Round(viewerWidth * fScale), (int)Math.Round(viewerHeight * fScale));
                break;
            case TBoldStretchMode.bsmStretchToFit:
                fScale = 1.0;
                fDisplayRect = new Rectangle(0, 0, CR.Width, CR.Height);
                break;
            case TBoldStretchMode.bsmStretchToScale:
                fDisplayRect = new Rectangle(0, 0, (int)Math.Round(viewerWidth * fScale), (int)Math.Round(viewerHeight * fScale));
                break;
        }

        if (AutoSize && viewer is not null && viewerWidth > 0 && viewerHeight > 0)
        {
            SetBounds(Left, Top, (int)Math.Round(viewerWidth * fScale), (int)Math.Round(viewerHeight * fScale));
        }

        if (Center)
        {
            fDisplayRect.Offset((CR.Width - fDisplayRect.Right) / 2, (CR.Height - fDisplayRect.Bottom) / 2);
        }
    }

    /// <summary>
    /// Paints the picture in its rectangle and the background around it; without a picture the name (in a designer);
    /// a frame when the image has the focus.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldImage.Paint", Note = "OnPaint.")]
    protected override void OnPaint(PaintEventArgs e)
    {
        var Canvas = new TBoldGraphicsCanvas(e.Graphics);
        Canvas.Brush.Color = BackColor;
        TBoldAbstractViewAdapter? viewer = fHandleFollower is not null ? Viewer : null;
        if (viewer is not null && !viewer.Empty())
        {
            CalcDisplayRect();
            viewer.Paint(Canvas, fDisplayRect);
            using var background = new Region(ClientRectangle);
            background.Exclude(fDisplayRect);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillRegion(brush, background);
        }
        else
        {
            // Draw Text if nothing to view
            Canvas.Font.Assign(BoldWinForms.ToTFont(Font, ForeColor));
            string S = DesignMode ? "(" + (Site?.Name ?? Name) + ")" : ""; //FIXME Some text in runtime?
            Rectangle R = ClientRectangle;
            int w = Canvas.TextWidth(S), h = Canvas.TextHeight(S);
            Canvas.TextRect(R, (R.Right - w) / 2, (R.Bottom - h) / 2, S);
        }

        // Draw Focus
        if (fDrawFocus && Focused && !DesignMode)
        {
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.WindowFrame, ButtonBorderStyle.Solid);
        }

        base.OnPaint(e);
    }

    /// <summary>The element changed: the picture draws again.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AfterMakeUptoDate(TBoldFollower Follower) => Invalidate();

    /// <summary>The picture may not change: the image is read-only, or the element cannot be changed.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool EffectiveReadOnly() => fReadOnly || !BoldProperties.MayModify(Follower);

    [Delphi(PortStatus.Ported)]
    public void CopyToClipboard() => Viewer?.CopyToClipboard();

    /// <summary>Copies the picture and (unless read-only) takes it away.</summary>
    [Delphi(PortStatus.Ported)]
    public void CutToClipboard()
    {
        if (Viewer is { } viewer)
        {
            viewer.CopyToClipboard();
            if (!EffectiveReadOnly())
            {
                Viewer = null;
                Invalidate();
                if (!Focused && BoldProperties.ApplyPolicy == TBoldApplyPolicy.bapExit)
                {
                    Follower.Apply();
                }
            }
        }
    }

    /// <summary>Pastes a picture (unless read-only), with the first view adapter that can paste the content type.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested GetViewer is a lambda.")]
    public void PasteFromClipboard()
    {
        if (!EffectiveReadOnly())
        {
            TBoldAbstractViewAdapter? aViewer = FirstViewer(adapter => adapter.CanPasteFromClipboard(fContentTypeOnPaste));
            if (aViewer is not null)
            {
                aViewer.PasteFromClipboard();
                Viewer = aViewer;
                Invalidate();
                if (!Focused && BoldProperties.ApplyPolicy == TBoldApplyPolicy.bapExit)
                {
                    Follower.Apply();
                }
            }
        }
    }

    /// <summary>Loads the file (unless read-only), with the first view adapter that can load it; raises for an unknown format.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested GetViewer is a lambda.")]
    public void LoadFromFile(string fileName)
    {
        if (!EffectiveReadOnly())
        {
            TBoldAbstractViewAdapter? aViewer = FirstViewer(adapter => adapter.CanLoadFromFile(fileName));
            if (aViewer is not null)
            {
                aViewer.LoadFromFile(fileName);
                Viewer = aViewer;
                Invalidate();
                if (!Focused && BoldProperties.ApplyPolicy == TBoldApplyPolicy.bapExit)
                {
                    Follower.Apply();
                }
            }
            else
            {
                throw new EBold(SysUtils.Format(sUnknownFileFormat, GetType().Name, fileName));
            }
        }
    }

    /// <summary>A new adapter of the first registered class that fits, or null.</summary>
    private static TBoldAbstractViewAdapter? FirstViewer(Func<TBoldAbstractViewAdapter, bool> fits)
    {
        for (int i = 0; i < TBoldAbstractViewAdapter.ViewAdapterClassCount(); i++)
        {
            var adapter = (TBoldAbstractViewAdapter)Activator.CreateInstance(TBoldAbstractViewAdapter.GetViewAdapterClass(i)!)!;
            if (fits(adapter))
            {
                return adapter;
            }

            (adapter as IDisposable)?.Dispose();
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetContextType();

    [Delphi(PortStatus.Ported)]
    private string GetExpression() => BoldProperties.Expression;

    string IBoldOCLComponent.GetExpression() => GetExpression();

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Expression) => BoldProperties.Expression = Expression;

    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);

    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariableList? GetVariableList() => BoldProperties.VariableList;

    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();

    /// <summary>The view adapter of the picture shown; setting it changes the element (under the apply policy).</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldAbstractViewAdapter? Viewer
    {
        get => GetViewer();
        set => SetViewer(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldViewerFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => fReadOnly;
        set => fReadOnly = value;
    }

    /// <summary>The control takes the picture's (scaled) size.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(true), DefaultValue(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override bool AutoSize
    {
        get => fAutoSize;
        set => SetAutoSize(value);
    }

    /// <summary>A frame shows when the image has the focus.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(true)]
    public bool DrawFocus
    {
        get => fDrawFocus;
        set => SetDrawFocus(value);
    }

    /// <summary>The content type a paste asks for (empty: any).</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue("")]
    public string ContentTypeOnPaste
    {
        get => fContentTypeOnPaste;
        set => fContentTypeOnPaste = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(TBoldStretchMode.bsmNoStretch)]
    public TBoldStretchMode StretchMode
    {
        get => fStretchMode;
        set => SetStretchMode(value);
    }

    /// <summary>The scale of the picture, in percent.</summary>
    [Delphi(PortStatus.Ported, Note = "Hides Control.Scale (a method that scales the control).")]
    [DefaultValue(100)]
    public new int Scale
    {
        get => GetScale();
        set => SetScale(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool Center
    {
        get => fCenter;
        set => fCenter = value;
    }

    [Delphi(PortStatus.Ported, Note = "Kept; Delphi used it only in a commented-out palette setting.")]
    [DefaultValue(false)]
    public bool QuickDraw
    {
        get => fQuickDraw;
        set => fQuickDraw = value;
    }

    [Delphi(PortStatus.Ported, Note = "WinForms' BorderStyle (Fixed3D for Delphi's bsSingle with Ctl3D).")]
    [DefaultValue(BorderStyle.Fixed3D)]
    public BorderStyle BorderStyle
    {
        get => fBorderStyle;
        set => SetBorderStyle(value);
    }
}
