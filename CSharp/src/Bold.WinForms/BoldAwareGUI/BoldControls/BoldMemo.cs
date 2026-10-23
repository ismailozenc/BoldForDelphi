// Delphi: Source/BoldAwareGUI/BoldControls/BoldMemo.pas
// Port map: CSharp/port/units/BoldMemo.md

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.Loaded", Note = "Took the color read from the form as the shown color; TBoldControlLook keeps the given and the shown colors apart.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.GetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.SetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.GetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.SetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo._FontChanged", Note = "OnFontChanged.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemo", Symbol = "TBoldCustomMemo.GetEffectiveFont", Note = "EffectiveFont is the font the memo shows.")]

namespace Bold;

/// <summary>A multi-line text box that shows an element and writes what is typed back to it.</summary>
[Delphi(PortStatus.Ported, Note = "A multi-line WinForms TextBox.")]
public class TBoldCustomMemo : TextBox, IBoldOCLComponent
{
    private TAlignment fAlignment;
    private TBoldStringFollowerController? fBoldProperties;
    private bool fFocused;
    private TBoldElementHandleFollower? fHandleFollower;
    private readonly TBoldControlLook fLook;
    private bool fMyReadOnly;
    private int fMaxLength;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomMemo()
    {
        Multiline = true;
        ScrollBars = ScrollBars.Vertical;
        AcceptsReturn = true;
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fLook = new TBoldControlLook(this);
        BackColor = SystemColors.Window;
        base.ReadOnly = true;
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    /// <summary>Applies or drops a pending edit (as the apply policy says), then lets go of the follower.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fHandleFollower is not null)
        {
            switch (BoldProperties.ApplyPolicy)
            {
                case TBoldApplyPolicy.bapChange:
                case TBoldApplyPolicy.bapExit:
                    try
                    {
                        Follower.Apply();
                    }
                    catch (Exception)
                    {
                        Follower.DiscardChange();
                    }

                    break;
                case TBoldApplyPolicy.bapDemand:
                    Follower.DiscardChange();
                    break;
            }

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

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            TBoldDragSupport.Accept(drgevent, BoldProperties.DropMode != TBoldDropMode.bdpNone && BoldProperties.DragOver(Follower, Follower.Element, 0));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            BoldProperties.DragDrop(Follower, Follower.Element, 0);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldDisplay(TBoldStringFollowerController value) => fBoldProperties!.Assign(value);

    [Delphi(PortStatus.Ported)]
    private void SetReadOnly(bool value)
    {
        if (fMyReadOnly != value)
        {
            fMyReadOnly = value;
            Follower.MarkValueOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEffectiveReadOnly() => base.ReadOnly;

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
    private void SetText(string value)
    {
        if (DesignMode || !EffectiveReadOnly)
        {
            base.Text = value;
        }
        else
        {
            throw new EBold(SysUtils.Format(sTextNotModifiable, GetType().Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetText() => base.Text;

    /// <summary>The popup menu: the memo's own, else the controller's Bold popup for the handle's value.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the menu for a right click; a TextBox shows its own menu when it has none.")]
    protected virtual ContextMenuStrip? GetPopupMenu()
    {
        ContextMenuStrip? Result = ContextMenuStrip;
        if (Result is null && BoldHandle is not null)
        {
            Result = BoldProperties.Popup.GetMenu(this, BoldHandle.Value) as ContextMenuStrip;
        }

        return Result;
    }

    /// <summary>Drops characters the element does not take (with a beep); Escape drops the edit.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        char Key = e.KeyChar;
        if (Key >= ' ' && !BoldProperties.ValidateCharacter(Key, Follower))
        {
            System.Media.SystemSounds.Beep.Play();
            e.Handled = true;
        }

        if (Key == (char)27)
        {
            Follower.DiscardChange();
            SelectAll();
            e.Handled = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetFocused(bool Value)
    {
        if (fFocused != Value)
        {
            fFocused = Value;
            if (fAlignment != TAlignment.taLeftJustify)
            {
                Invalidate();
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.Change")]
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (!DesignMode && fHandleFollower is not null)
        {
            BoldProperties.MayHaveChanged(Text, Follower);
        }
    }

    /// <summary>Shows the follower's string, read-only, styled and limited as the element and renderer say.</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        string newText = BoldProperties.GetCurrentAsString(Follower);
        if (Text != newText)
        {
            base.Text = newText;
        }

        base.ReadOnly = fMyReadOnly || !BoldProperties.MayModify(Follower);
        fLook.ApplyFont((effective, font) => BoldProperties.SetFont(effective, font, Follower));
        fLook.ApplyColor(color =>
        {
            Color ec = color;
            BoldProperties.SetColor(ref ec, color, Follower);
            return ec;
        });

        int EffectiveMaxLength = 0;
        int RendererDataMaxLength = ((TBoldStringRendererData)Follower.RendererData).MaxStringLength;
        if (RendererDataMaxLength != -1)
        {
            EffectiveMaxLength = RendererDataMaxLength;
        }

        if (MaxLength > 0 && MaxLength < EffectiveMaxLength)
        {
            EffectiveMaxLength = MaxLength;
        }

        base.MaxLength = EffectiveMaxLength > 0 ? EffectiveMaxLength : int.MaxValue;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.CMEnter")]
    protected override void OnEnter(EventArgs e)
    {
        SetFocused(true);
        base.OnEnter(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.CMExit")]
    protected override void OnLeave(EventArgs e)
    {
        if (Follower.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
        {
            Follower.Apply();
        }

        SetFocused(false);
        base.OnLeave(e);
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

    [Delphi(PortStatus.Ported)]
    [DefaultValue(TAlignment.taLeftJustify)]
    public TAlignment Alignment
    {
        get => fAlignment;
        set
        {
            fAlignment = value;
            TextAlign = BoldWinForms.ToHorizontalAlignment(value);
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

    /// <summary>The color the memo was given; a renderer may show another (EffectiveColor).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomMemo.Color")]
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

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EffectiveReadOnly => GetEffectiveReadOnly();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    /// <summary>The font the memo was given; a renderer may show another (EffectiveFont).</summary>
    [Delphi(PortStatus.Ported)]
    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set => base.Font = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public new bool ReadOnly
    {
        get => fMyReadOnly;
        set => SetReadOnly(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [AllowNull]
    public override string Text
    {
        get => GetText();
        set => SetText(value ?? "");
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(0)]
    public override int MaxLength
    {
        get => fMaxLength;
        set => fMaxLength = value;
    }
}

/// <summary>A Bold memo, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemo : TBoldCustomMemo
{
}
