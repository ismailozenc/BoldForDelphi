// Delphi: Source/BoldAwareGUI/BoldControls/BoldEdit.pas
// Port map: CSharp/port/units/BoldEdit.md

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.WMPaint", Note = "Drew right and centered text when not focused; a WinForms TextBox aligns its text itself (TextAlign).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.GetTextMargins", Note = "The text margins for WMPaint, which the TextBox does not need.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.CreateParams", Note = "Made the edit multi-line so its text rectangle could leave room for the button; the TextBox's right margin does that (SetEditRect).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.Loaded", Note = "Took the color read from the form as the shown color; TBoldControlLook keeps the given and the shown colors apart.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.GetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.SetFont", Note = "The WinForms Font property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.GetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.SetColor", Note = "The WinForms BackColor property.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit._FontChanged", Note = "OnFontChanged.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEdit", Symbol = "TBoldCustomEdit.GetEffectiveFont", Note = "EffectiveFont is the font the TextBox shows.")]

namespace Bold;

/// <summary>
/// A text box that shows an element (the value of its handle, through its string controller's expression) and writes
/// what is typed back to it. It is read-only when the element cannot be changed; Escape drops the edit.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms TextBox.")]
public class TBoldCustomEdit : TextBox, IBoldOCLComponent
{
    private const int EM_SETMARGINS = 0xD3;
    private const int EC_RIGHTMARGIN = 0x2;

    private TBoldElementHandleFollower? fHandleFollower;
    private TBoldStringFollowerController? fBoldProperties;
    private bool fMyReadOnly;
    private readonly TBoldControlLook fLook;
    private TAlignment fAlignment;
    private bool fFocused;
    private TBoldComboButton? fButton;
    private TBoldEditButtonStyle fButtonStyle;
    private TNotifyEvent? fOnButtonClick;
    private bool fBeepOnInvalidKey;
    private int fMaxLength;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [Delphi(PortStatus.Ported)]
    public TBoldCustomEdit()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fLook = new TBoldControlLook(this);
        fBeepOnInvalidKey = true;
        base.ReadOnly = true;
        BackColor = SystemColors.Window;
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    /// <summary>Applies or drops a pending edit (as the apply policy says), then lets go of the follower.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.Destroy")]
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

    /// <summary>The follower belongs to the form the edit is on (for applying the form's edits).</summary>
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        UpdateMatchObject();
    }

    private void UpdateMatchObject()
    {
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

    /// <summary>
    /// Accepts a dragged object for a reference handle without a root (it becomes the handle's value) when its type
    /// fits; otherwise the controller decides.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (!TBoldDragSupport.IsBoldDrag(drgevent))
        {
            return;
        }

        TBoldObjectList dragged = BoldGUI.BoldGUIHandler().DraggedObjects;
        bool accept;
        if (BoldProperties.DropMode == TBoldDropMode.bdpReplace && BoldHandle is TBoldRootedHandle rooted &&
            (BoldHandle.Value is null || BoldHandle.Value is TBoldObject) && rooted.RootHandle is null &&
            dragged.Count == 1 && dragged[0] is TBoldObject)
        {
            accept = BoldHandle.DynamicBoldType is null || dragged[0]!.BoldType!.ConformsTo(BoldHandle.DynamicBoldType);
        }
        else if (BoldProperties.DropMode == TBoldDropMode.bdpNone)
        {
            accept = false;
        }
        else
        {
            accept = BoldProperties.DragOver(Follower, Follower.Element, 0);
        }

        TBoldDragSupport.Accept(drgevent, accept);
    }

    /// <summary>Drops: a dragged object becomes a reference handle's value, or the controller drops on the element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (!TBoldDragSupport.IsBoldDrag(drgevent))
        {
            return;
        }

        TBoldObjectList dragged = BoldGUI.BoldGUIHandler().DraggedObjects;
        if (BoldProperties.DropMode == TBoldDropMode.bdpReplace && BoldHandle is TBoldReferenceHandle reference &&
            (BoldHandle.Value is null || BoldHandle.Value is TBoldObject) && dragged.Count == 1 && dragged[0] is TBoldObject draggedObject &&
            (BoldHandle.BoldType is null || draggedObject.BoldType!.ConformsTo(BoldHandle.BoldType)))
        {
            reference.Value = draggedObject;
        }
        else
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

    /// <summary>A new font given to the edit makes it show the element again (the renderer may change the font).</summary>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (fLook is not null && fLook.FontChanged() && fHandleFollower is not null)
        {
            Follower.MarkValueOutOfDate();
        }
    }

    /// <summary>A new color given to the edit makes it show the element again (the renderer may change the color).</summary>
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

    /// <summary>The popup menu: the edit's own, else the controller's Bold popup for the handle's value.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the menu for a right click; a TextBox shows its own menu when it has none.")]
    protected virtual ContextMenuStrip? GetPopupMenu()
    {
        ContextMenuStrip? Result = ContextMenuStrip;
        if (Result is null && BoldHandle is not null)
        {
            Result = BoldProperties.Popup.GetMenu(this, BoldHandle.Value) as ContextMenuStrip;
            if (Result is not null && Result.Items.Count == 0)
            {
                Result = null;
            }
        }

        return Result;
    }

    /// <summary>A key the element does not take: beeps when BeepOnInvalidKey.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void InvalidKey(char Key)
    {
        if (BeepOnInvalidKey)
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }

    /// <summary>Drops characters the element does not take; Ctrl+A selects all, Escape drops the edit.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        char Key = e.KeyChar;
        if (Key >= ' ' && !BoldProperties.ValidateCharacter(Key, Follower))
        {
            InvalidKey(Key);
            e.Handled = true;
        }
        else if (Key == '\u0001') // CTRL-A
        {
            SelectAll();
            e.Handled = true;
        }
        else if (Key == (char)27)
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

    /// <summary>What is typed may change the element: the follower is told.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.Change")]
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (!DesignMode && !EffectiveReadOnly && fHandleFollower is not null)
        {
            BoldProperties.MayHaveChanged(Text, Follower);
        }
    }

    /// <summary>Shows the follower's string, and makes the edit read-only, limited, colored and styled as the element and renderer say.</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        string newText = BoldProperties.GetCurrentAsString(Follower);
        if (Text != newText)
        {
            base.Text = newText;
        }

        base.ReadOnly = fMyReadOnly || !BoldProperties.MayModify(Follower) || CharacterCasing != CharacterCasing.Normal;
        fLook.ApplyFont((effective, font) => BoldProperties.SetFont(effective, font, Follower));

        int RendererDataMaxLength = ((TBoldStringRendererData)Follower.RendererData).MaxStringLength;
        int EffectiveMaxLength = RendererDataMaxLength != -1 ? RendererDataMaxLength : MaxLength;
        if (MaxLength > 0 && MaxLength < EffectiveMaxLength)
        {
            EffectiveMaxLength = MaxLength;
        }

        base.MaxLength = EffectiveMaxLength > 0 ? EffectiveMaxLength : 32767;

        fLook.ApplyColor(color =>
        {
            Color ec = color;
            BoldProperties.SetColor(ref ec, color, Follower);
            return ec;
        });
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.CMEnter")]
    protected override void OnEnter(EventArgs e)
    {
        SetFocused(true);
        base.OnEnter(e);
    }

    /// <summary>Leaving the edit applies the edit under the exit policy.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.CMExit")]
    protected override void OnLeave(EventArgs e)
    {
        if (Follower.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
        {
            Follower.Apply();
        }

        SetFocused(false);
        base.OnLeave(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.WMSize")]
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (fButton is not null)
        {
            SetEditRect();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.CMEnabledChanged")]
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (fButton is not null)
        {
            fButton.Enabled = Enabled;
        }
    }

    /// <summary>Makes the button (on the right of the text).</summary>
    [Delphi(PortStatus.Ported, Note = "One button; Delphi put a speed button in a control of its own.")]
    protected void ButtonCreate()
    {
        System.Diagnostics.Debug.Assert(fButton is null, "fButton is already created");
        fButton = new TBoldComboButton { Width = 17, Dock = DockStyle.Right, Cursor = Cursors.Default, TabStop = false, Enabled = Enabled };
        Controls.Add(fButton);
    }

    [Delphi(PortStatus.Ported)]
    protected void ButtonDestroy()
    {
        if (fButton is not null)
        {
            Controls.Remove(fButton);
            fButton.Dispose();
            fButton = null;
        }
    }

    /// <summary>The button's face for the style: a combo arrow, an ellipsis, or nothing (the application's own).</summary>
    [Delphi(PortStatus.Ported)]
    protected void ButtonSetGlyph()
    {
        switch (fButtonStyle)
        {
            case TBoldEditButtonStyle.bbsCombo:
                fButton!.Image = null;
                fButton.Text = "▾";
                break;
            case TBoldEditButtonStyle.bbsEllipsis:
                fButton!.Text = "";
                fButton.Image = BoldCommonBitmaps.bmpBOLDEDIT_ELLIPSIS;
                break;
            case TBoldEditButtonStyle.bbsCustom:
                fButton!.Text = "";
                fButton.Image = null;
                break;
        }
    }

    /// <summary>Makes the button when there is one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.CreateWnd")]
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateMatchObject();
        if (fButtonStyle != TBoldEditButtonStyle.bbsNone)
        {
            if (fButton is null)
            {
                ButtonCreate();
            }

            ButtonSetGlyph();
            SetEditRect();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.DestroyWnd")]
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (!RecreatingHandle)
        {
            ButtonDestroy();
        }

        base.OnHandleDestroyed(e);
    }

    /// <summary>Keeps the text clear of the button.</summary>
    [Delphi(PortStatus.Ported, Note = "Sets the TextBox's right margin, where Delphi set the edit's text rectangle.")]
    protected void SetEditRect()
    {
        if (IsHandleCreated)
        {
            int margin = fButton is not null && fButton.Visible ? fButton.Width + 2 : 0;
            SendMessage(Handle, EM_SETMARGINS, (IntPtr)EC_RIGHTMARGIN, (IntPtr)(margin << 16));
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetButtonStyle(TBoldEditButtonStyle Value)
    {
        if (Value != fButtonStyle)
        {
            fButtonStyle = Value;
            if (IsHandleCreated)
            {
                if (fButtonStyle == TBoldEditButtonStyle.bbsNone)
                {
                    if (fButton is not null)
                    {
                        fButton.Visible = false;
                    }
                }
                else
                {
                    if (fButton is null)
                    {
                        ButtonCreate();
                    }

                    ButtonSetGlyph();
                    fButton!.Visible = true;
                }

                SetEditRect();
                Invalidate();
            }
        }
    }

    /// <summary>The button, made (with the custom style) when there was none.</summary>
    [Delphi(PortStatus.Ported)]
    private Button GetButton()
    {
        if (fButton is null)
        {
            fButtonStyle = TBoldEditButtonStyle.bbsCustom;
            ButtonCreate();
        }

        return fButton!;
    }

    /// <summary>The button was clicked: OnButtonClick.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void ButtonClick() => fOnButtonClick?.Invoke(this);

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
    [DefaultValue(true)]
    public bool BeepOnInvalidKey
    {
        get => fBeepOnInvalidKey;
        set => fBeepOnInvalidKey = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button Button => GetButton();

    [Delphi(PortStatus.Ported, Note = "The button itself; Delphi's was a control holding the speed button.")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? ButtonControl => fButton;

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EffectiveReadOnly => GetEffectiveReadOnly();

    [Delphi(PortStatus.Ported, Note = "The WinForms font shown, which the renderer may have changed from Font.")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Font EffectiveFont => base.Font;

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EffectiveColor
    {
        get => GetEffectiveColor();
        set => SetEffectiveColor(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldDisplay(value);
    }

    /// <summary>Read-only by choice; the edit is also read-only when the element cannot be changed (EffectiveReadOnly).</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public new bool ReadOnly
    {
        get => fMyReadOnly;
        set => SetReadOnly(value);
    }

    /// <summary>The text shown; setting it raises when the edit is read-only.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [AllowNull]
    public override string Text
    {
        get => GetText();
        set => SetText(value ?? "");
    }

    /// <summary>The font the edit was given; a renderer may show another (EffectiveFont).</summary>
    [Delphi(PortStatus.Ported)]
    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set => base.Font = value;
    }

    /// <summary>The color the edit was given; a renderer may show another (EffectiveColor).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomEdit.Color")]
    public override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    /// <summary>The longest text the edit takes (0 for no limit); the element may allow less.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(0)]
    public override int MaxLength
    {
        get => fMaxLength;
        set => fMaxLength = value;
    }

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
    [DefaultValue(TBoldEditButtonStyle.bbsNone)]
    public TBoldEditButtonStyle ButtonStyle
    {
        get => fButtonStyle;
        set => SetButtonStyle(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(null)]
    public TNotifyEvent? OnButtonClick
    {
        get => fOnButtonClick;
        set => fOnButtonClick = value;
    }
}

/// <summary>A Bold edit box, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEdit : TBoldCustomEdit
{
}

/// <summary>The button of an edit box: a click focuses the edit, then clicks its button.</summary>
[Delphi(PortStatus.Ported, Note = "A Button; Delphi's was a speed button.")]
public class TBoldComboButton : Button
{
    /// <summary>Focuses the edit first; when it cannot take the focus nothing happens.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldComboButton.MouseDown")]
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (Parent is TBoldCustomEdit edit && !edit.Focused && edit.CanFocus)
        {
            edit.Focus();
            if (!edit.Focused)
            {
                return;
            }
        }

        base.OnMouseDown(mevent);
        (Parent as TBoldCustomEdit)?.ButtonClick();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldComboButton.MouseMove")]
    protected override void OnMouseMove(MouseEventArgs mevent) => base.OnMouseMove(mevent);
}
