// Delphi: Source/BoldAwareGUI/BoldControls/BoldComboBox.pas
// Port map: CSharp/port/units/BoldComboBox.md

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.ChangeScale", Note = "Scaled the font with the form; WinForms scales fonts itself.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.CMParentColorChanged", Note = "Followed the parent's color; a WinForms control's BackColor is ambient until it is set.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.CMParentFontChanged", Note = "Followed the parent's font; a WinForms control's Font is ambient until it is set.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.ComboWndProc", Note = "Ignored clicks on the list of a read-only simple combo box; OnSelectionChangeCommitted puts a read-only combo box's text back.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TWinControlHack", Note = "Reached the parent's protected font and color; a WinForms control's font and back color are public and ambient.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.WMPaint", Note = "Only called inherited painting (own painting was never done).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.IsColorStored", Note = "The WinForms designer stores BackColor when it is not ambient.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.IsFontStored", Note = "The WinForms designer stores Font when it is not ambient.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.SetColor", Note = "The WinForms BackColor property (OnBackColorChanged).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.SetFont", Note = "The WinForms Font property (OnFontChanged).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.FontChanged", Note = "OnFontChanged.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldComboBox", Symbol = "TBoldCustomComboBox.GetEffectiveFont", Note = "EffectiveFont is the font the combo box shows.")]

namespace Bold;

/// <summary>The list controller of a combo box, with the empty row published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldComboListController : TBoldAbstractListAsFollowerListController
{
    /// <summary>A controller whose rows follow their elements with <paramref name="FollowerController"/>.</summary>
    public TBoldComboListController(object? aOwningComponent, TBoldFollowerController FollowerController)
        : base(aOwningComponent, FollowerController)
    {
    }

    public new TBoldNilElementMode NilElementMode
    {
        get => base.NilElementMode;
        set => base.NilElementMode = value;
    }
}

/// <summary>
/// A combo box that shows an element (through BoldProperties) and offers the elements of a list handle (through the
/// row properties). Choosing a row sets the text, the value, the reference handle's value or the list's current
/// index, as BoldSelectChangeAction says.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms ComboBox.")]
public class TBoldCustomComboBox : ComboBox, IBoldValidateableComponent
{
    private const int EM_SETREADONLY = 0x00CF;

    private TAlignment fAlignment;
    private TBoldElementHandleFollower? fHandleFollower;
    private TBoldListHandleFollower? fListHandleFollower;
    private TBoldStringFollowerController? fBoldProperties;
    private TBoldComboListController? fBoldListProperties;
    private TBoldStringFollowerController? fBoldRowProperties;
    private TBoldComboSelectChangeAction fBoldSelectChangeAction;
    private TBoldExpression fBoldSetValueExpression = "";
    private readonly TBoldControlLook fLook;
    private bool fEffectiveReadOnly;
    private bool fFocused;
    private bool fReadOnly;
    private TNotifyEvent? fOnSelectChanged;
    private bool fOnSelectChangedIsCalled;
    private bool fRestoring;
    private CharacterCasing fCharCase;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COMBOBOXINFO
    {
        public int cbSize;
        public RECT rcItem;
        public RECT rcButton;
        public int stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndItem;
        public IntPtr hwndList;
    }

    [DllImport("user32.dll")]
    private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO pcbi);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [Delphi(PortStatus.Ported)]
    public TBoldCustomComboBox()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = _AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextForBoldProperties;
        fBoldRowProperties = new TBoldStringFollowerController(this);
        fBoldRowProperties.AfterMakeUptoDate = _RowAfterMakeUptoDate;
        fBoldRowProperties.OnGetContextType = GetContextForBoldRowProperties;
        fBoldListProperties = new TBoldComboListController(this, fBoldRowProperties);
        fBoldListProperties.OnAfterInsertItem = _InsertItem;
        fBoldListProperties.OnAfterDeleteItem = _DeleteItem;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fListHandleFollower = new TBoldListHandleFollower(this, fBoldListProperties);
        fHandleFollower.PrioritizedQueuable = fListHandleFollower;
        fHandleFollower.StronglyDependedOfPrioritized = true;
        fLook = new TBoldControlLook(this);
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    /// <summary>Applies or drops a pending edit (as the apply policy says), then lets go of the followers.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fHandleFollower is not null)
        {
            switch (BoldProperties.ApplyPolicy)
            {
                case TBoldApplyPolicy.bapChange:
                case TBoldApplyPolicy.bapExit:
                    Follower.Apply();
                    break;
                case TBoldApplyPolicy.bapDemand:
                    Follower.DiscardChange();
                    break;
            }

            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fListHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
            SysUtils.FreeAndNil(ref fBoldListProperties);
            SysUtils.FreeAndNil(ref fBoldRowProperties);
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
            fListHandleFollower!.MatchObject = match;
            fListHandleFollower.Follower.MatchObject = match;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _InsertItem(TBoldFollower Follower) => Items.Insert(Follower.index, "");

    [Delphi(PortStatus.Ported)]
    private void _DeleteItem(int index, TBoldFollower OwningFollower) => Items.RemoveAt(index);

    /// <summary>Puts the row's string in the list, and shows the value again (its row may have gone and come back).</summary>
    [Delphi(PortStatus.Ported)]
    private void _RowAfterMakeUptoDate(TBoldFollower Follower)
    {
        int index = Follower.index;
        if (index > -1 && index < Items.Count)
        {
            Items[index] = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
        }

        Invalidate();
        // forces a redisplay of the edit-area, the windows component might go blank if the active row is removed and then reinserted
        fHandleFollower!.Follower.MarkValueOutOfDate();
    }

    /// <summary>Shows the value: its colors, read-only state, font and text.</summary>
    [Delphi(PortStatus.Ported)]
    private void _AfterMakeUptoDate(TBoldFollower Follower)
    {
        UpdateEffectiveColor();
        UpdateEffectiveReadOnly();
        UpdateEffectiveFont();
        string NewText = BoldProperties.GetCurrentAsString(Follower);
        if (base.Text != NewText)
        {
            SetEffectiveText(NewText);
        }
    }

    /// <summary>
    /// The text was typed or a row was chosen: the text may change the value (when the combo box sets text), and
    /// OnSelectChanged is raised unless the choice already raised it.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Called from OnTextUpdate (typing) and after a choice from the list.")]
    protected virtual void Change()
    {
        if (!DesignMode && !fRestoring && fHandleFollower is not null && !EffectiveReadOnly &&
            BoldSelectChangeAction == TBoldComboSelectChangeAction.bdscSetText && ComboAllowsTextEditing(BoldProperties, Follower))
        {
            BoldProperties.MayHaveChanged(Text, Follower);
        }

        if (!fOnSelectChangedIsCalled && !fRestoring)
        {
            fOnSelectChanged?.Invoke(this);
        }
    }

    /// <summary>Typing in the edit part calls Change.</summary>
    protected override void OnTextUpdate(EventArgs e)
    {
        Change();
        base.OnTextUpdate(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.CMEnter")]
    protected override void OnEnter(EventArgs e)
    {
        SetFocused(true);
        base.OnEnter(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.CMExit")]
    protected override void OnLeave(EventArgs e)
    {
        if (!Disposing && Follower.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
        {
            Follower.Apply();
        }

        SetFocused(false);
        base.OnLeave(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.CreateWnd")]
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        fEffectiveReadOnly = false;
        UpdateEffectiveReadOnly();
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            BoldProperties.DragDrop(Follower, Follower.Element, 0);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            TBoldDragSupport.Accept(drgevent, BoldProperties.DropMode != TBoldDropMode.bdpNone && BoldProperties.DragOver(Follower, Follower.Element, 0));
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (fLook is not null && fLook.FontChanged() && fHandleFollower is not null)
        {
            UpdateEffectiveFont();
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (fLook is not null && fLook.BackColorChanged() && fHandleFollower is not null)
        {
            UpdateEffectiveColor();
        }
    }

    [Delphi(PortStatus.Ported)]
    private Color GetEffectiveColor() => base.BackColor;

    /// <summary>The popup menu: the combo box's own, else the controller's Bold popup for the element.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual ContextMenuStrip? GetPopupMenu() =>
        ContextMenuStrip ?? (Follower.Element is not null ? BoldProperties.Popup.GetMenu(this, Follower.Element) as ContextMenuStrip : null);

    /// <summary>The text: typed (in an editable combo box) or the chosen row's; with no row chosen, the empty row's.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetText()
    {
        if (DropDownStyle != ComboBoxStyle.DropDownList)
        {
            return base.Text;
        }

        int I = SelectedIndex;
        if (I >= 0)
        {
            return Items[I]?.ToString() ?? "";
        }

        if (BoldListProperties.NilElementMode == TBoldNilElementMode.neInsertFirst && Items.Count > 0)
        {
            SelectedIndex = 0;
            return Items[0]?.ToString() ?? "";
        }

        if (BoldListProperties.NilElementMode == TBoldNilElementMode.neAddLast && Items.Count > 0)
        {
            SelectedIndex = Items.Count - 1;
            return Items[Items.Count - 1]?.ToString() ?? "";
        }

        return "";
    }

    /// <summary>A read-only combo box ignores the arrow keys.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.KeyDown")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (EffectiveReadOnly && e.KeyCode is Keys.Up or Keys.Down)
        {
            e.Handled = true;
        }
    }

    /// <summary>Drops characters the element does not take (when typing sets text); Escape drops the edit.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (fCharCase == CharacterCasing.Upper)
        {
            e.KeyChar = char.ToUpper(e.KeyChar, System.Globalization.CultureInfo.CurrentCulture);
        }
        else if (fCharCase == CharacterCasing.Lower)
        {
            e.KeyChar = char.ToLower(e.KeyChar, System.Globalization.CultureInfo.CurrentCulture);
        }

        base.OnKeyPress(e);
        char Key = e.KeyChar;
        if (Key >= ' ' && DropDownStyle != ComboBoxStyle.DropDownList &&
            BoldSelectChangeAction != TBoldComboSelectChangeAction.bdcsSetReference && !BoldProperties.ValidateCharacter(Key, Follower))
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

    /// <summary>Sets the handle; choosing a reference needs a reference handle.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? Value)
    {
        if (Value is not null && BoldSelectChangeAction == TBoldComboSelectChangeAction.bdcsSetReference && Value is not TBoldReferenceHandle)
        {
            throw new EBold(sHandleMustBeReferenceHandle);
        }

        fHandleFollower!.BoldHandle = Value;
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldListHandle(TBoldAbstractListHandle? Value) => fListHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldStringFollowerController Value) => fBoldProperties!.Assign(Value);

    [Delphi(PortStatus.Ported, Note = "A WinForms combo box has no casing of its own; typed characters are changed as they come.")]
    private void SetCharCase(CharacterCasing Value) => fCharCase = Value;

    /// <summary>Makes the edit part read-only or not.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetEffectiveReadOnly(bool Value)
    {
        if (Value != fEffectiveReadOnly)
        {
            fEffectiveReadOnly = Value;
            if (DropDownStyle != ComboBoxStyle.DropDownList && IsHandleCreated)
            {
                var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
                if (GetComboBoxInfo(Handle, ref info) && info.hwndItem != IntPtr.Zero)
                {
                    SendMessage(info.hwndItem, EM_SETREADONLY, (IntPtr)(Value ? 1 : 0), IntPtr.Zero);
                }
            }
        }
    }

    /// <summary>Shows the text: in a list the row with it (or the empty row), in an editable combo box the text itself.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetEffectiveText(string Value)
    {
        if (Value == GetText())
        {
            return;
        }

        fRestoring = true;
        try
        {
            if (DropDownStyle != ComboBoxStyle.DropDown)
            {
                int I;
                if (Value == "")
                {
                    I = BoldListProperties.NilElementMode switch
                    {
                        TBoldNilElementMode.neInsertFirst => 0,
                        TBoldNilElementMode.neAddLast => Items.Count - 1,
                        _ => -1,
                    };
                }
                else
                {
                    I = Items.IndexOf(Value);
                }

                if (I < Items.Count)
                {
                    SelectedIndex = I;
                }

                if (I >= 0)
                {
                    return;
                }
            }

            if (DropDownStyle != ComboBoxStyle.DropDownList)
            {
                base.Text = Value;
            }
        }
        finally
        {
            fRestoring = false;
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

    [Delphi(PortStatus.Ported)]
    private void SetReadOnly(bool Value)
    {
        if (fReadOnly != Value)
        {
            fReadOnly = Value;
            UpdateEffectiveReadOnly();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetRowProperties(TBoldStringFollowerController Value) => fBoldRowProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private void SetText(string Value)
    {
        if (!EffectiveReadOnly)
        {
            SetEffectiveText(Value);
        }
    }

    /// <summary>Shows the color the renderer gives for the value.</summary>
    [Delphi(PortStatus.Ported)]
    protected void UpdateEffectiveColor() =>
        fLook.ApplyColor(color =>
        {
            Color NewColor = color;
            BoldProperties.SetColor(ref NewColor, color, Follower);
            return NewColor;
        });

    /// <summary>Shows the font the renderer gives for the value.</summary>
    [Delphi(PortStatus.Ported)]
    protected void UpdateEffectiveFont() => fLook.ApplyFont((effective, font) => BoldProperties.SetFont(effective, font, Follower));

    /// <summary>Read-only when asked to be, or (when typing sets the text) when the value cannot be changed.</summary>
    [Delphi(PortStatus.Ported)]
    protected void UpdateEffectiveReadOnly()
    {
        if (BoldSelectChangeAction is TBoldComboSelectChangeAction.bdcsNone or TBoldComboSelectChangeAction.bdcsSetValue or TBoldComboSelectChangeAction.bdcsSetReference)
        {
            SetEffectiveReadOnly(fReadOnly);
        }
        else
        {
            SetEffectiveReadOnly(fReadOnly || !BoldProperties.MayModify(Follower));
        }
    }

    /// <summary>
    /// Choosing a row does what BoldSelectChangeAction says: sets the text (unless read-only), assigns the chosen element
    /// to the value (or to the BoldSetValueExpression of it), sets the reference handle, makes the element the list's
    /// current one, or nothing; then OnSelectChanged. When the choice itself is not kept, the value is shown again.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.WndProc", Note = "OnSelectionChangeCommitted, where Delphi caught the selection change notification.")]
    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        if (DesignMode)
        {
            base.OnSelectionChangeCommitted(e);
            return;
        }

        bool keepChoice = true;
        TBoldElement? LocalSelectedElement = SelectedElement;
        switch (BoldSelectChangeAction)
        {
            case TBoldComboSelectChangeAction.bdscSetText:
                keepChoice = !EffectiveReadOnly;
                break;
            case TBoldComboSelectChangeAction.bdcsSetValue:
                Follower.DiscardChange();
                if (BoldHandle?.Value is { } value)
                {
                    TBoldElement? ElementToAssignTo = BoldSetValueExpression.Trim() != ""
                        ? value.EvaluateExpressionAsDirectElement(BoldSetValueExpression)
                        : value;
                    // must check the element (and not the follower) since we might have a BoldSetValueExpression...
                    if (ElementToAssignTo is not null && ElementToAssignTo.Mutable &&
                        (ElementToAssignTo is not TBoldMember member || member.CanModify()))
                    {
                        try
                        {
                            ElementToAssignTo.Assign(LocalSelectedElement);
                        }
                        catch (Exception E)
                        {
                            bool Discard = false;
                            if (!HandleApplyException(E, ElementToAssignTo, ref Discard))
                            {
                                throw;
                            }

                            if (Discard)
                            {
                                Follower.DiscardChange();
                            }
                        }
                    }
                }

                keepChoice = false;
                break;
            case TBoldComboSelectChangeAction.bdcsSetReference:
                Follower.DiscardChange();
                if (BoldHandle is TBoldReferenceHandle reference)
                {
                    reference.Value = LocalSelectedElement;
                }

                break;
            case TBoldComboSelectChangeAction.bdcsSetListIndex:
                Follower.DiscardChange();
                if (BoldListHandle?.List is { } list)
                {
                    BoldListHandle.CurrentIndex = list.IndexOf(LocalSelectedElement);
                }

                keepChoice = false;
                break;
            case TBoldComboSelectChangeAction.bdcsNone:
                keepChoice = false;
                Invalidate();
                break;
        }

        fOnSelectChanged?.Invoke(this);
        base.OnSelectionChangeCommitted(e);
        fOnSelectChangedIsCalled = true;
        try
        {
            Change();
        }
        finally
        {
            fOnSelectChangedIsCalled = false;
        }

        if (!keepChoice && fHandleFollower is not null)
        {
            // Show the value again: the choice itself is not the value.
            BeginInvoke(new Action(() =>
            {
                if (fHandleFollower is not null)
                {
                    _AfterMakeUptoDate(Follower);
                }
            }));
        }
    }

    /// <summary>A read-only combo box shows its value again when its list closes.</summary>
    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        if (EffectiveReadOnly && fHandleFollower is not null)
        {
            _AfterMakeUptoDate(Follower); // Restore text
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldListProperties(TBoldComboListController Value) => fBoldListProperties!.Assign(Value);

    /// <summary>The element of the chosen row, or null.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetSelectedElement()
    {
        if (!ListFollower.Active)
        {
            return null;
        }

        var rows = (TBoldFollowerList)ListFollower.RendererData;
        return SelectedIndex >= 0 && SelectedIndex < rows.Count ? rows.Followers(SelectedIndex)!.Element : null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldListHandle() => fListHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetListFollower() => fListHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextForBoldProperties() => BoldHandle?.StaticBoldType;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextForBoldRowProperties() => BoldListHandle?.StaticBoldType;

    /// <summary>Checks the value, set-value and row expressions in their contexts.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix)
    {
        string name = Site?.Name ?? Name;
        // We want to evaluate everything. Thus suboptimized expressions.
        bool Result = ComponentValidator.ValidateExpressionInContext(BoldProperties.Expression, GetContextForBoldProperties(), SysUtils.Format("%s%s.BoldProperties", NamePrefix, name)); // Do not localize
        Result = ComponentValidator.ValidateExpressionInContext(BoldSetValueExpression, GetContextForBoldProperties(), SysUtils.Format("%s%s.BoldSetValueExpression", NamePrefix, name)) && Result; // Do not localize
        Result = ComponentValidator.ValidateExpressionInContext(BoldRowProperties.Expression, GetContextForBoldRowProperties(), SysUtils.Format("%s%s.BoldRowProperties", NamePrefix, name)) && Result; // Do not localize
        return Result;
    }

    /// <summary>Choosing a reference needs a reference handle.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetBoldSelectChangeAction(TBoldComboSelectChangeAction Value)
    {
        if (Value == TBoldComboSelectChangeAction.bdcsSetReference && BoldHandle is not null && BoldHandle is not TBoldReferenceHandle)
        {
            throw new EBold(sChangeActionCannotBeSetReference);
        }

        fBoldSelectChangeAction = Value;
    }

    /// <summary>Whether typing may edit the value; true here, for descendants to forbid.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool ComboAllowsTextEditing(TBoldStringFollowerController BoldProperties, TBoldFollower Follower) => true;

    /// <summary>Offers the error to the form's exception handler; true when it handled it.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool HandleApplyException(Exception E, TBoldElement? Elem, ref bool discard)
    {
        discard = false;
        TBoldExceptionHandler? ExceptionHandler = TBoldExceptionHandler.FindExceptionHandler(this);
        bool Result = ExceptionHandler is not null;
        if (Result)
        {
            ExceptionHandler!.HandleApplyException(E, this, Elem, ref discard, ref Result);
        }

        return Result;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(TAlignment.taLeftJustify)]
    public TAlignment Alignment
    {
        get => fAlignment;
        set => fAlignment = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower ListFollower => GetListFollower();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldAbstractListHandle? BoldListHandle
    {
        get => GetBoldListHandle();
        set => SetBoldListHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldComboListController BoldListProperties
    {
        get => fBoldListProperties!;
        set => SetBoldListProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldRowProperties
    {
        get => fBoldRowProperties!;
        set => SetRowProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(TBoldComboSelectChangeAction.bdscSetText)]
    public TBoldComboSelectChangeAction BoldSelectChangeAction
    {
        get => fBoldSelectChangeAction;
        set => SetBoldSelectChangeAction(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue("")]
    public TBoldExpression BoldSetValueExpression
    {
        get => fBoldSetValueExpression;
        set => fBoldSetValueExpression = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(null)]
    public TNotifyEvent? OnSelectChanged
    {
        get => fOnSelectChanged;
        set => fOnSelectChanged = value;
    }

    /// <summary>The casing of the text (Delphi's CharCase).</summary>
    [DefaultValue(CharacterCasing.Normal)]
    public CharacterCasing CharacterCasing
    {
        get => fCharCase;
        set => SetCharCase(value);
    }

    /// <summary>The color the combo box was given; a renderer may show another (EffectiveColor).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomComboBox.Color")]
    public override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EffectiveColor => GetEffectiveColor();

    [Delphi(PortStatus.Ported, Note = "The WinForms font shown, which the renderer may have changed from Font.")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Font EffectiveFont => base.Font;

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EffectiveReadOnly => fEffectiveReadOnly;

    /// <summary>The font the combo box was given; a renderer may show another (EffectiveFont).</summary>
    [Delphi(PortStatus.Ported)]
    [AllowNull]
    public override Font Font
    {
        get => base.Font;
        set => base.Font = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => fReadOnly;
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
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? SelectedElement => GetSelectedElement();
}

/// <summary>A Bold combo box, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldComboBox : TBoldCustomComboBox
{
}
