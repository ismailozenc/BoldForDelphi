// Delphi: Source/BoldAwareGUI/BoldControls/BoldListBox.pas
// Port map: CSharp/port/units/BoldListBox.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldListBox", Symbol = "TBoldCustomListBox.CNDrawItem", Note = "Drew the background of no row and the focus of the current row; OnDrawItem does that.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldListBox", Symbol = "TBoldCustomListBox.Selected", Note = "An indexed property in Delphi; GetSelected and SetSelected.")]

namespace Bold;

/// <summary>
/// A list box that shows a list handle's list, a row per element through the row controller; its current row follows
/// the handle's current element (when locked to it), and a double click opens a form for the element.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms ListBox, drawn by the rows' renderer.")]
public class TBoldCustomListBox : ListBox, IBoldOCLComponent
{
    private TAlignment FAlignment;
    private TBoldListHandleFollower? fHandleFollower;
    private TBoldListAsFollowerListController? fBoldProperties;
    private TBoldStringFollowerController? fBoldRowProperties;
    private TCanvas? fCanvas;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomListBox()
    {
        fBoldRowProperties = new TBoldStringFollowerController(this);
        fBoldRowProperties.AfterMakeUptoDate = _RowAfterMakeUptoDate;
        fBoldRowProperties.OnGetContextType = GetContextType;
        fBoldProperties = new TBoldListAsFollowerListController(this, fBoldRowProperties);
        fBoldProperties.OnAfterInsertItem = _InsertItem;
        fBoldProperties.OnAfterDeleteItem = _DeleteItem;
        fBoldProperties.BeforeMakeUptoDate = _BeforeMakeUptoDate;
        fBoldProperties.AfterMakeUptoDate = _AfterMakeUptoDate;
        fHandleFollower = new TBoldListHandleFollower(this, fBoldProperties);
        DrawMode = DrawMode.OwnerDrawVariable;
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
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
        }
    }

    /// <summary>Fetches the whole list, and holds the drawing until the rows are up to date.</summary>
    [Delphi(PortStatus.Ported)]
    private void _BeforeMakeUptoDate(TBoldFollower Follower)
    {
        // Will fetch all
        if (BoldHandle?.List is { } list)
        {
            list.EnsureRange(0, list.Count - 1);
        }

        BeginUpdate();
    }

    /// <summary>Selects the current row (or nothing), and draws again.</summary>
    [Delphi(PortStatus.Ported)]
    private void _AfterMakeUptoDate(TBoldFollower Follower)
    {
        int index = Follower.CurrentIndex;
        if (index < Items.Count)
        {
            base.SelectedIndex = index;
        }

        if (index == -1)
        {
            fBoldProperties!.SelectAll(Follower, false);
        }
        else
        {
            InternalSetSelected(index, true);
        }

        EndUpdate();
    }

    [Delphi(PortStatus.Ported)]
    private void SetAlignment(TAlignment Value)
    {
        if (Value != FAlignment)
        {
            FAlignment = Value;
            // Enough to invalidate drawing surface
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private int GetItemIndex() => base.SelectedIndex;

    /// <summary>Makes the row current (and the handle's current element, when locked to it).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetItemIndex(int Value)
    {
        fHandleFollower!.SetFollowerIndex(Value);
        base.SelectedIndex = Value; // FIXME;
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldListAsFollowerListController Value) => fBoldProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private void SetRowProperties(TBoldStringFollowerController Value) => fBoldRowProperties!.Assign(Value);

    /// <summary>The popup menu: the list box's own, else the controller's Bold popup.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual ContextMenuStrip? GetPopupMenu() => ContextMenuStrip ?? BoldProperties.Popup.GetMenu(null, null) as ContextMenuStrip;

    /// <summary>Selects the row as the keys say: alone, a range from the last selected (Shift), or toggled (Ctrl).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetSelection(int aRow, Keys Shift)
    {
        if (aRow == -1)
        {
            return;
        }

        bool shift = (Shift & Keys.Shift) != 0, ctrl = (Shift & Keys.Control) != 0;
        bool multiSelect = SelectionMode is SelectionMode.MultiSimple or SelectionMode.MultiExtended;

        // Clear previous selection, Select one item
        if (!(shift || ctrl) || !multiSelect)
        {
            fBoldProperties!.SelectAll(Follower, false);
            fBoldProperties.SetSelected(Follower, aRow, true);
        }

        // Select range from first selected item
        if (shift && multiSelect)
        {
            fBoldProperties!.SelectRange(Follower, aRow);
        }

        // Toggle selection on current item
        if (ctrl && multiSelect)
        {
            fBoldProperties!.ToggleSelected(Follower, aRow);
        }

        Invalidate();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.MouseDown")]
    protected override void OnMouseDown(MouseEventArgs e)
    {
        SetSelection(ItemIndex, ModifierKeys);
        base.OnMouseDown(e);
    }

    /// <summary>A click makes the clicked row current.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.Click")]
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        fHandleFollower!.SetFollowerIndex(ItemIndex);
    }

    /// <summary>A double click opens a form for the current element (when the controller does that by default).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.DblClick", Note = "The DoubleClick event is raised first.")]
    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        if (BoldProperties.DefaultDblClick && CurrentBoldElement is { } element)
        {
            BoldAFP.AutoFormProviderRegistry().FormForElement(element)?.Show();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    /// <summary>Accepts what the controller accepts on the mutable list at the row under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            bool internalDrag = drgevent.Data?.GetData(typeof(TBoldDragObject)) is TBoldDragObject drag && drag.Source == this;
            bool accept = BoldProperties.DropMode != TBoldDropMode.bdpNone && (!internalDrag || BoldProperties.InternalDrag) &&
                BoldProperties.DragOver(Follower, MutableList, IndexFromPoint(PointToClient(new Point(drgevent.X, drgevent.Y))));
            TBoldDragSupport.Accept(drgevent, accept);
        }
    }

    /// <summary>Drops on the mutable list at the row under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            BoldProperties.DragDrop(Follower, MutableList, IndexFromPoint(PointToClient(new Point(drgevent.X, drgevent.Y))));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetCurrentBoldElement() => ItemIndex >= 0 ? Follower.SubFollowers(ItemIndex)?.Element : null;

    [Delphi(PortStatus.Ported)]
    private TBoldList? GetBoldList() => BoldHandle?.List;

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetCurrentBoldObject() => CurrentBoldElement as TBoldObject;

    /// <summary>Puts the row's string in the list box.</summary>
    [Delphi(PortStatus.Ported)]
    private void _RowAfterMakeUptoDate(TBoldFollower Follower)
    {
        int index = Follower.index;
        if (index > -1 && index < Items.Count) //FIXME: How come index sometimes is > Items.Count?
        {
            Items[index] = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _InsertItem(TBoldFollower Follower) => Items.Insert(Follower.index, "");

    [Delphi(PortStatus.Ported)]
    private void _DeleteItem(int index, TBoldFollower OwningFollower) => Items.RemoveAt(index);

    [Delphi(PortStatus.Ported)]
    public new void SetSelected(int index, bool V)
    {
        base.SetSelected(index, V);
        InternalSetSelected(index, V);
    }

    [Delphi(PortStatus.Ported)]
    public new bool GetSelected(int index) => base.GetSelected(index);

    [Delphi(PortStatus.Ported)]
    private int GetSelectedCount() => SelectedIndices.Count;

    /// <summary>Arrow and page keys select as the mouse does (without Ctrl: the keyboard cannot select apart).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.KeyUp")]
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if ((int)e.KeyCode >= 33 && (int)e.KeyCode <= 40) //PGUP..DOWN
        {
            SetSelection(ItemIndex, e.Modifiers & ~Keys.Control);
        }

        base.OnKeyUp(e);
    }

    /// <summary>The row's font and colors, from the row renderer; a selected row is highlighted.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultSetFontAndColor(int Index)
    {
        TBoldFollower? SubFollower = Follower.SubFollowers(Index);
        if (fCanvas is null || SubFollower is null)
        {
            return;
        }

        BoldRowProperties.SetFont(fCanvas.Font, BoldWinForms.ToTFont(Font, ForeColor), SubFollower);
        Color ec = BackColor;
        BoldRowProperties.SetColor(ref ec, BackColor, SubFollower);
        fCanvas.Brush.Color = ec;
        // Selected state yields default highlight colors
        if (SubFollower.Selected)
        {
            fCanvas.Brush.Color = SystemColors.Highlight;
            fCanvas.Font.Color = SystemColors.HighlightText;
        }
    }

    /// <summary>Draws the row through the row renderer.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DefaultDrawItem(int Index, Rectangle Rect)
    {
        if (fCanvas is not null && Follower.SubFollowers(Index) is { } row)
        {
            BoldRowProperties.DrawOnCanvas(row, fCanvas, Rect, Alignment, new Point(2, 0));
        }
    }

    /// <summary>Measures the row with its font.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.MeasureItem")]
    protected override void OnMeasureItem(MeasureItemEventArgs e)
    {
        TBoldFollower? row = e.Index >= 0 && Follower.Active ? Follower.SubFollowers(e.Index) : null;
        if (row is not null)
        {
            var canvas = new TBoldGraphicsCanvas(e.Graphics);
            BoldRowProperties.SetFont(canvas.Font, BoldWinForms.ToTFont(Font, ForeColor), row);
            string S = ((TBoldStringFollowerController)row.Controller!).GetCurrentAsString(row);
            e.ItemHeight = S == "" ? 2 + canvas.TextHeight("Wg") : canvas.TextHeight(S);
        }
        else
        {
            e.ItemHeight = Font.Height + 2;
        }

        // Now allow user to remeasure, using updated height-value
        base.OnMeasureItem(e);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.WMSize")]
    protected override void OnResize(EventArgs e)
    {
        // Redraw when resising if aligment is not taLeftJustify
        base.OnResize(e);
        if (Alignment != TAlignment.taLeftJustify)
        {
            Invalidate();
        }
    }

    /// <summary>Draws the row (its font and colors first), then the focus rectangle on the current row.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomListBox.DrawItem", Note = "Also does what Delphi's CNDrawItem did: the background of no row and the focus of the current one. The DrawItem event is raised after the default drawing.")]
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        fCanvas = new TBoldGraphicsCanvas(e.Graphics);
        try
        {
            fCanvas.Font.Assign(BoldWinForms.ToTFont(Font, ForeColor));
            fCanvas.Brush.Color = BackColor;
            if (e.Index >= 0 && Follower.Active && e.Index < Follower.SubFollowerCount)
            {
                DefaultSetFontAndColor(e.Index);
                DefaultDrawItem(e.Index, e.Bounds);
            }
            else
            {
                fCanvas.FillRect(e.Bounds);
            }

            if (BoldHandle is not null && e.Index >= 0 && e.Index == Follower.CurrentIndex)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);
            }

            base.OnDrawItem(e);
        }
        finally
        {
            fCanvas = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetContextType();

    [Delphi(PortStatus.Ported)]
    private string GetExpression() => BoldRowProperties.Expression;

    string IBoldOCLComponent.GetExpression() => GetExpression();

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Expression) => BoldRowProperties.Expression = Expression;

    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);

    [Delphi(PortStatus.Ported)]
    private bool GetBoldHandleIndexLock() => fHandleFollower!.HandleIndexLock;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandleIndexLock(bool Value) => fHandleFollower!.HandleIndexLock = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldList? GetMutableList() => BoldHandle?.MutableList;

    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariableList? GetVariableList() => BoldProperties.VariableList;

    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();

    /// <summary>Selects or unselects the row in the follower list (alone, unless the list box selects several).</summary>
    [Delphi(PortStatus.Ported)]
    private void InternalSetSelected(int index, bool v)
    {
        if (fBoldProperties!.GetSelected(Follower, index) != v)
        {
            if (v && SelectionMode is SelectionMode.One or SelectionMode.None)
            {
                fBoldProperties.SelectAll(Follower, false);
            }

            fBoldProperties.SetSelected(Follower, index, v);
        }
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [DefaultValue(TAlignment.taLeftJustify)]
    public TAlignment Alignment
    {
        get => FAlignment;
        set => SetAlignment(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(true)]
    public bool BoldHandleIndexLock
    {
        get => GetBoldHandleIndexLock();
        set => SetBoldHandleIndexLock(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldList? BoldList => GetBoldList();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldListAsFollowerListController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldRowProperties
    {
        get => fBoldRowProperties!;
        set => SetRowProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldObject? CurrentBoldObject => GetCurrentBoldObject();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? CurrentBoldElement => GetCurrentBoldElement();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemIndex
    {
        get => GetItemIndex();
        set => SetItemIndex(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedCount => GetSelectedCount();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldList? MutableList => GetMutableList();
}

/// <summary>A Bold list box, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListBox : TBoldCustomListBox
{
}
