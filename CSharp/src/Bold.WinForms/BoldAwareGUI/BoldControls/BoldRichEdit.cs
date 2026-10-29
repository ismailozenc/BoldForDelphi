// Delphi: Source/BoldAwareGUI/BoldControls/BoldRichEdit.pas
// Port map: CSharp/port/units/BoldRichEdit.md

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit.GetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit.SetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit.GetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit.SetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit._FontChanged", Note = "OnFontChanged.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRichEdit", Symbol = "TBoldCustomRichEdit.GetEffectiveFont", Note = "EffectiveFont is the font the rich edit shows.")]

namespace Bold;

/// <summary>
/// A rich text box that shows an element and writes what is typed back to it. A string that is rich text (starts
/// with {\rtf) shows formatted; the text written back is the plain text, and FormattedText gives the rich text.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms RichTextBox.")]
public class TBoldCustomRichEdit : RichTextBox
{
    private TAlignment fAlignment;
    private TBoldStringFollowerController? fBoldProperties;
    private bool fFocused;
    private TBoldElementHandleFollower? fHandleFollower;
    private readonly TBoldControlLook fLook;
    private bool fMyReadOnly;
    private bool fDisplaying;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomRichEdit()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fLook = new TBoldControlLook(this);
        base.ReadOnly = true;
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.Destroy")]
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

    /// <summary>The text with its formatting, as rich text.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetFormattedText() => base.Rtf ?? "";

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            TBoldDragSupport.Accept(drgevent, BoldProperties.DropMode != TBoldDropMode.bdpNone && BoldProperties.DragOver(Follower, Follower.Element, 0));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
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

    /// <summary>The popup menu: the rich edit's own, else the controller's Bold popup for the handle's value.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the menu for a right click.")]
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
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        char Key = e.KeyChar;
        if (Key >= ' ' && !BoldProperties.ValidateCharacter(Key, Follower))
        {
            System.Media.SystemSounds.Beep.Play();
            e.Handled = true;
        }

        if (Key == BoldDefs.BOLDESC)
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

    /// <summary>A change of the text (not one the element made) may change the element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.Change", Note = "OnTextChanged; the text the element puts in is not written back (rich text would come back plain).")]
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (!DesignMode && !fDisplaying && fHandleFollower is not null)
        {
            BoldProperties.MayHaveChanged(Text, Follower);
        }
    }

    /// <summary>Shows the follower's string (rich text formatted), read-only and styled as the element and renderer say.</summary>
    [Delphi(PortStatus.Ported, Note = "A string that starts with {\\rtf is put in as rich text, as Delphi's rich edit control did with its text.")]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        string newText = BoldProperties.GetCurrentAsString(Follower);
        bool rich = newText.StartsWith(@"{\rtf", StringComparison.Ordinal);
        if (rich ? Rtf != newText : Text != newText)
        {
            fDisplaying = true;
            try
            {
                if (rich)
                {
                    base.Rtf = newText;
                }
                else
                {
                    base.Text = newText;
                }
            }
            finally
            {
                fDisplaying = false;
            }
        }

        base.ReadOnly = fMyReadOnly || !BoldProperties.MayModify(Follower);
        fLook.ApplyFont((effective, font) => BoldProperties.SetFont(effective, font, Follower));
        fLook.ApplyColor(color =>
        {
            Color ec = color;
            BoldProperties.SetColor(ref ec, color, Follower);
            return ec;
        });
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.CMEnter")]
    protected override void OnEnter(EventArgs e)
    {
        SetFocused(true);
        base.OnEnter(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.CMExit")]
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
    [DefaultValue(TAlignment.taLeftJustify)]
    public TAlignment Alignment
    {
        get => fAlignment;
        set => fAlignment = value;
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

    /// <summary>The color the rich edit was given; a renderer may show another (EffectiveColor).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomRichEdit.Color")]
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

    /// <summary>The font the rich edit was given; a renderer may show another (EffectiveFont).</summary>
    [Delphi(PortStatus.Ported)]
    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set => base.Font = value;
    }

    /// <summary>The text with its formatting, as rich text.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FormattedText => GetFormattedText();

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
}

/// <summary>A Bold rich edit, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRichEdit : TBoldCustomRichEdit
{
}
