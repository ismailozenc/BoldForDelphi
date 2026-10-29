using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[assembly: InternalsVisibleTo("Bold.WinForms.Tests")]

namespace Bold;

/// <summary>
/// Installs Bold's WinForms services, as the Delphi units' initialization sections did: the WinForms environment (its
/// display queue runs when the application is idle), the GUI handler that knows forms, message dialogs, the popup menu
/// and the handling of display errors. It runs when Bold.WinForms is first used; calling it again does nothing.
/// </summary>
public static class BoldWinForms
{
    private static bool fInitialized;

#pragma warning disable CA2255 // The library's initialization is what Delphi's unit initialization did.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void ModuleInitialize() => Initialize();

    /// <summary>Installs the WinForms environment and services (once).</summary>
    public static void Initialize()
    {
        if (fInitialized)
        {
            return;
        }

        fInitialized = true;
        BoldEnvironment.BoldInternalVCLConfiguration ??= new TBoldVCLEnvironmentConfiguration();
        BoldGUI.BoldGUIHandlerClass = typeof(TBoldWinFormsGUIHandler);
        BoldGUI.BoldPopupMenu ??= new ContextMenuStrip();
        BoldQueue.HandleException = (e, _) => Application.OnThreadException(e);
        Dialogs.MessageDlgHandler ??= ShowMessageDlg;
        // The image units' initialization: their view adapters.
        TBoldAbstractViewAdapter.RegisterViewAdapter(typeof(TBoldViewBitmapAdapter));
        TBoldAbstractViewAdapter.RegisterViewAdapter(typeof(TBoldViewJPEGAdapter));
    }

    /// <summary>A message box for Delphi's MessageDlg.</summary>
    private static int ShowMessageDlg(string Msg, TMsgDlgType DlgType, DelphiSet<TMsgDlgBtn> Buttons, int HelpCtx)
    {
        MessageBoxIcon icon = DlgType switch
        {
            TMsgDlgType.mtWarning => MessageBoxIcon.Warning,
            TMsgDlgType.mtError => MessageBoxIcon.Error,
            TMsgDlgType.mtInformation => MessageBoxIcon.Information,
            TMsgDlgType.mtConfirmation => MessageBoxIcon.Question,
            _ => MessageBoxIcon.None,
        };
        bool yes = Buttons.Contains(TMsgDlgBtn.mbYes), no = Buttons.Contains(TMsgDlgBtn.mbNo), cancel = Buttons.Contains(TMsgDlgBtn.mbCancel);
        MessageBoxButtons buttons =
            yes && no && cancel ? MessageBoxButtons.YesNoCancel :
            yes && no ? MessageBoxButtons.YesNo :
            Buttons.Contains(TMsgDlgBtn.mbRetry) && cancel ? MessageBoxButtons.RetryCancel :
            Buttons.Contains(TMsgDlgBtn.mbAbort) ? MessageBoxButtons.AbortRetryIgnore :
            cancel ? MessageBoxButtons.OKCancel :
            MessageBoxButtons.OK;
        return MessageBox.Show(Msg, Application.ProductName, buttons, icon) switch
        {
            DialogResult.OK => Controls.mrOk,
            DialogResult.Cancel => Controls.mrCancel,
            DialogResult.Abort => Controls.mrAbort,
            DialogResult.Retry => Controls.mrRetry,
            DialogResult.Ignore => Controls.mrIgnore,
            DialogResult.Yes => Controls.mrYes,
            DialogResult.No => Controls.mrNo,
            _ => Controls.mrNone,
        };
    }

    /// <summary>The Delphi font (with the text color) of a WinForms font and color.</summary>
    public static TFont ToTFont(Font font, Color color)
    {
        var result = new TFont { Name = font.Name, Size = (int)Math.Round(font.SizeInPoints), Color = color };
        var style = DelphiSet<TFontStyle>.Of();
        if (font.Bold)
        {
            style = style + TFontStyle.fsBold;
        }

        if (font.Italic)
        {
            style = style + TFontStyle.fsItalic;
        }

        if (font.Underline)
        {
            style = style + TFontStyle.fsUnderline;
        }

        if (font.Strikeout)
        {
            style = style + TFontStyle.fsStrikeOut;
        }

        result.Style = style;
        return result;
    }

    /// <summary>The WinForms font of a Delphi font (its color is the control's ForeColor).</summary>
    public static Font ToFont(TFont font)
    {
        FontStyle style = FontStyle.Regular;
        if (font.Style.Contains(TFontStyle.fsBold))
        {
            style |= FontStyle.Bold;
        }

        if (font.Style.Contains(TFontStyle.fsItalic))
        {
            style |= FontStyle.Italic;
        }

        if (font.Style.Contains(TFontStyle.fsUnderline))
        {
            style |= FontStyle.Underline;
        }

        if (font.Style.Contains(TFontStyle.fsStrikeOut))
        {
            style |= FontStyle.Strikeout;
        }

        return new Font(font.Name, font.Size, style, GraphicsUnit.Point);
    }

    /// <summary>True when the two fonts look the same.</summary>
    internal static bool SameFont(Font a, Font b) =>
        a.Name == b.Name && Math.Abs(a.SizeInPoints - b.SizeInPoints) < 0.01 && a.Style == b.Style;

    /// <summary>The alignment of text in WinForms.</summary>
    public static HorizontalAlignment ToHorizontalAlignment(TAlignment alignment) => alignment switch
    {
        TAlignment.taRightJustify => HorizontalAlignment.Right,
        TAlignment.taCenter => HorizontalAlignment.Center,
        _ => HorizontalAlignment.Left,
    };

    /// <summary>The alignment of text in a label or cell.</summary>
    public static ContentAlignment ToContentAlignment(TAlignment alignment) => alignment switch
    {
        TAlignment.taRightJustify => ContentAlignment.MiddleRight,
        TAlignment.taCenter => ContentAlignment.MiddleCenter,
        _ => ContentAlignment.MiddleLeft,
    };
}

/// <summary>
/// The GUI handler of WinForms: the form of a control is the form it is on, focusing it activates that form, a
/// control's owner is its form and its topmost parent the top of its parent chain.
/// </summary>
public class TBoldWinFormsGUIHandler : TBoldGUIHandler
{
    public override object? FindHostingForm(object? Component) => Component is Control control ? control.FindForm() : null;

    public override bool TryToFocusHostingForm(object? Component)
    {
        if (FindHostingForm(Component) is Form form && form.Visible)
        {
            form.Activate();
            return true;
        }

        return false;
    }

    public override object? ComponentOwner(object Component) => Component switch
    {
        Form => null,
        Control control => control.FindForm(),
        _ => base.ComponentOwner(Component),
    };

    public override object? TopmostParentControl(object Component)
    {
        if (Component is not Control control)
        {
            return null;
        }

        while (control.Parent is not null)
        {
            control = control.Parent;
        }

        return control;
    }
}

/// <summary>A canvas that draws on a WinForms Graphics, for renderers.</summary>
public sealed class TBoldGraphicsCanvas(Graphics graphics) : TCanvas
{
    private const TextFormatFlags Flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

    /// <summary>The Graphics drawn on.</summary>
    public Graphics Graphics { get; } = graphics;

    private Font CurrentFont() => BoldWinForms.ToFont(Font);

    public override int TextWidth(string Text)
    {
        using Font font = CurrentFont();
        return TextRenderer.MeasureText(Graphics, Text, font, Size.Empty, Flags).Width;
    }

    public override int TextHeight(string Text)
    {
        using Font font = CurrentFont();
        return TextRenderer.MeasureText(Graphics, Text, font, Size.Empty, Flags).Height;
    }

    public override void TextRect(Rectangle Rect, int X, int Y, string Text)
    {
        FillRect(Rect);
        using Font font = CurrentFont();
        var bounds = Rectangle.Intersect(Rect, Rectangle.FromLTRB(X, Y, Math.Max(X, Rect.Right), Math.Max(Y, Rect.Bottom)));
        TextRenderer.DrawText(Graphics, Text, font, bounds, Font.Color, Flags);
    }

    public override void FillRect(Rectangle Rect)
    {
        using var brush = new SolidBrush(Brush.Color);
        Graphics.FillRectangle(brush, Rect);
    }

    public override void Line(int X1, int Y1, int X2, int Y2)
    {
        using var pen = new Pen(Pen.Color, Pen.Width);
        Graphics.DrawLine(pen, X1, Y1, X2, Y2);
    }
}

/// <summary>
/// The font and colors a Bold control was given (in the designer or in code) and the ones it shows, which renderers
/// may change for the element (Delphi's Font and Color against EffectiveFont and EffectiveColor).
/// </summary>
public sealed class TBoldControlLook
{
    private readonly Control fControl;
    private Font fFont;
    private Color fForeColor;
    private Color fBackColor;
    private bool fApplying;

    public TBoldControlLook(Control control)
    {
        fControl = control;
        fFont = control.Font;
        fForeColor = control.ForeColor;
        fBackColor = control.BackColor;
    }

    /// <summary>The font the control was given.</summary>
    public Font Font => fFont;

    /// <summary>The text color the control was given.</summary>
    public Color ForeColor => fForeColor;

    /// <summary>The background color the control was given.</summary>
    public Color BackColor => fBackColor;

    /// <summary>The control's font changed: true when it was given a new one (not a renderer's).</summary>
    public bool FontChanged()
    {
        if (fApplying)
        {
            return false;
        }

        fFont = fControl.Font;
        fForeColor = fControl.ForeColor;
        return true;
    }

    /// <summary>The control's background color changed: true when it was given a new one (not a renderer's).</summary>
    public bool BackColorChanged()
    {
        if (fApplying)
        {
            return false;
        }

        fBackColor = fControl.BackColor;
        return true;
    }

    /// <summary>Lets the renderer change the given font (and text color) and shows the result.</summary>
    public void ApplyFont(Action<TFont, TFont> setFont)
    {
        TFont wanted = BoldWinForms.ToTFont(fFont, fForeColor);
        TFont effective = BoldWinForms.ToTFont(fControl.Font, fControl.ForeColor);
        setFont(effective, wanted);
        fApplying = true;
        try
        {
            Font font = BoldWinForms.ToFont(effective);
            if (BoldWinForms.SameFont(font, fControl.Font))
            {
                font.Dispose();
            }
            else
            {
                fControl.Font = font;
            }

            if (fControl.ForeColor != effective.Color)
            {
                fControl.ForeColor = effective.Color;
            }
        }
        finally
        {
            fApplying = false;
        }
    }

    /// <summary>Lets the renderer change the given background color and shows the result.</summary>
    public void ApplyColor(Func<Color, Color> setColor)
    {
        Color effective = setColor(fBackColor);
        if (fControl.BackColor != effective)
        {
            fApplying = true;
            try
            {
                fControl.BackColor = effective;
            }
            finally
            {
                fApplying = false;
            }
        }
    }
}

/// <summary>What a Bold control drags: the GUI handler's dragged objects.</summary>
public sealed class TBoldDragObject
{
    /// <summary>The control the drag started from.</summary>
    public required Control Source { get; init; }
}

/// <summary>
/// Drag and drop for a Bold control: a drag starts when the mouse moves far enough with the left button down (when
/// the controller drags), and the control accepts drops when its controller does.
/// </summary>
public sealed class TBoldDragSupport
{
    private readonly Control fControl;
    private readonly Func<bool> fCanDrag;
    private readonly Action fStartDrag;
    private readonly Action fEndDrag;
    private Rectangle fDragBox = Rectangle.Empty;

    /// <summary>Drag support for the control: canDrag says whether a drag may start, startDrag fills the dragged objects, endDrag clears them.</summary>
    public TBoldDragSupport(Control control, Func<bool> canDrag, Action startDrag, Action endDrag)
    {
        fControl = control;
        fCanDrag = canDrag;
        fStartDrag = startDrag;
        fEndDrag = endDrag;
        control.MouseDown += MouseDown;
        control.MouseMove += MouseMove;
        control.MouseUp += (_, _) => fDragBox = Rectangle.Empty;
    }

    private void MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && fCanDrag())
        {
            Size size = SystemInformation.DragSize;
            fDragBox = new Rectangle(new Point(e.X - size.Width / 2, e.Y - size.Height / 2), size);
        }
        else
        {
            fDragBox = Rectangle.Empty;
        }
    }

    private void MouseMove(object? sender, MouseEventArgs e)
    {
        if ((e.Button & MouseButtons.Left) != 0 && fDragBox != Rectangle.Empty && !fDragBox.Contains(e.X, e.Y))
        {
            fDragBox = Rectangle.Empty;
            fStartDrag();
            try
            {
                if (BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0)
                {
                    fControl.DoDragDrop(new TBoldDragObject { Source = fControl }, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                }
            }
            finally
            {
                fEndDrag();
            }
        }
    }

    /// <summary>True when the drag carries Bold's dragged objects.</summary>
    public static bool IsBoldDrag(DragEventArgs e) => e.Data?.GetDataPresent(typeof(TBoldDragObject)) == true;

    /// <summary>Sets the drag's effect: a copy when accepted, none otherwise.</summary>
    public static void Accept(DragEventArgs e, bool accept) =>
        e.Effect = accept ? (e.AllowedEffect & DragDropEffects.Copy) != 0 ? DragDropEffects.Copy : e.AllowedEffect & (DragDropEffects.Move | DragDropEffects.Link) : DragDropEffects.None;
}

/// <summary>Shows a Bold popup menu on a right click when the control has no menu of its own.</summary>
internal static class TBoldPopupSupport
{
    public static void Attach(Control control, Func<ContextMenuStrip?> getMenu) =>
        control.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && control.ContextMenuStrip is null && getMenu() is { Items.Count: > 0 } menu)
            {
                menu.Show(control, e.Location);
            }
        };
}
