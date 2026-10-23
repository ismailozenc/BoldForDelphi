// Delphi: Source/BoldAwareGUI/BoldControls/BoldLabel.pas
// Port map: CSharp/port/units/BoldLabel.md

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.Loaded", Note = "Took the color read from the form as the shown color; TBoldControlLook keeps the given and the shown colors apart.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.CMParentColorChanged", Note = "Followed the parent's color; a WinForms label's BackColor is ambient (its parent's) until it is set.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.GetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.SetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.GetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.SetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel._FontChanged", Note = "OnFontChanged.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLabel", Symbol = "TBoldCustomLabel.GetEffectiveFont", Note = "EffectiveFont is the font the label shows.")]

namespace Bold;

/// <summary>A label that shows an element (the value of its handle, through its string controller's expression).</summary>
[Delphi(PortStatus.Ported, Note = "A WinForms Label.")]
public class TBoldCustomLabel : Label, IBoldOCLComponent
{
    private TBoldStringFollowerController? fBoldProperties;
    private TBoldElementHandleFollower? fHandleFollower;
    private readonly TBoldControlLook fLook;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomLabel()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fLook = new TBoldControlLook(this);
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomLabel.Destroy")]
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

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (fLook is not null && fLook.FontChanged() && fHandleFollower is not null)
        {
            Follower.MarkValueOutOfDate();
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (fLook is not null && fLook.BackColorChanged() && fHandleFollower is not null)
        {
            Follower.MarkValueOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private Color GetEffectiveColor() => base.BackColor;

    [Delphi(PortStatus.Ported)]
    private void SetEffectiveColor(Color v)
    {
        if (EffectiveColor != v)
        {
            base.BackColor = v;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldDisplay(TBoldStringFollowerController value) => fBoldProperties!.Assign(value);

    /// <summary>Shows the follower's string (in a designer: the renderer and expression), styled as the renderer says.</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        string newText;
        if (DesignMode)
        {
            // caption during design-time
            if (BoldProperties.Renderer is { } renderer)
            {
                newText = SysUtils.Format("%s.%s", renderer.Site?.Name ?? renderer.GetType().Name, BoldProperties.Expression);
            }
            else if (BoldProperties.Expression != "")
            {
                newText = BoldProperties.Expression;
            }
            else
            {
                newText = Site?.Name ?? Name;
            }
        }
        else
        {
            // Caption at run-time
            newText = BoldProperties.GetCurrentAsString(Follower);
        }

        if (Text != newText)
        {
            Text = newText;
        }

        fLook.ApplyFont((effective, font) => BoldProperties.SetFont(effective, font, Follower));
        fLook.ApplyColor(color =>
        {
            Color ec = color;
            BoldProperties.SetColor(ref ec, color, Follower);
            return ec;
        });
    }

    [Delphi(PortStatus.Ported)]
    private string GetText() => base.Text;

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

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomLabel.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            TBoldDragSupport.Accept(drgevent, BoldProperties.DropMode != TBoldDropMode.bdpNone && BoldProperties.DragOver(Follower, Follower.Element, 0));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomLabel.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            BoldProperties.DragDrop(Follower, Follower.Element, 0);
        }
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldDisplay(value);
    }

    /// <summary>The text shown (Delphi's Caption, which only the element sets).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption => GetText();

    /// <summary>The color the label was given; a renderer may show another (EffectiveColor).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomLabel.Color")]
    public override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EffectiveColor
    {
        get => GetEffectiveColor();
        set => SetEffectiveColor(value);
    }

    [Delphi(PortStatus.Ported, Note = "The WinForms font shown, which the renderer may have changed from Font.")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Font EffectiveFont => base.Font;

    /// <summary>The font the label was given; a renderer may show another (EffectiveFont).</summary>
    [Delphi(PortStatus.Ported)]
    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set => base.Font = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();
}

/// <summary>A Bold label, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLabel : TBoldCustomLabel
{
}
