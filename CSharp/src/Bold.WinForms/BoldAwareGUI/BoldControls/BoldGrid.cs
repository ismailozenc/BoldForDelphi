// Delphi: Source/BoldAwareGUI/BoldControls/BoldGrid.pas
// Port map: CSharp/port/units/BoldGrid.md

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;
using static Bold.TBoldColumnValue;
using static Bold.TBoldCWAdjust;
using static Bold.TGridOption;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldColumnClass", Note = "A Type: TBoldGridColumn or a descendant.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TExposedBoldAsStringRenderer", Note = "Opened the renderer's protected methods to the grid; Bold.AwareGui shows its internal members to Bold.WinForms.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TExposedFollowerController", Note = "Opened the controller's protected members to the grid; Bold.AwareGui shows its internal members to Bold.WinForms.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldInplaceCombo.WndProc", Note = "Clicked the combo box on a choice when the column applies on change; WinForms raises SelectionChangeCommitted on every choice, which the editor handles.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldInplaceEdit.BoundsChanged", Note = "Set the editor's text margins; the DataGridView places and sizes its editing control.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldInplaceEdit.GetGrid", Note = "The Grid property reads the editing control's DataGridView.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldGridColumns.GetOwner", Note = "Gave the grid as the owner in the name path; a WinForms control is no TPersistent. Grid gives the grid.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldColumnTitle.Destroy", Note = "Freed the font; the garbage collector does that.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.CreateEditor", Note = "Made the grid's TBoldInplaceEdit; the grid's cells name it as their editing control (TBoldGridViewCell.EditType), and OnEditingControlShowing gives it the cell's font, text and limit.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.MouseMove", Note = "Started a drag from a data row when the mouse moved far enough (BoldDragAnywhere); TBoldDragSupport starts it (from the first column, or from any data row with BoldDragAnywhere).")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.WMChar", Note = "A check box column takes no typed characters: its cells start no editing (TBoldGridViewCell.KeyEntersEditMode, OnCellBeginEdit) and OnKeyPress drops the character.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.CellFollowers", Note = "An indexed property in Delphi; a method here.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.RowFollowers", Note = "An indexed property in Delphi; a method here.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.Selected", Note = "An indexed property in Delphi; a method here.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGrid", Symbol = "TBoldCustomGrid.CellText", Note = "An indexed property in Delphi; a method here.")]

namespace Bold;

/// <summary>Called instead of assigning the chosen element when a lookup column's choice changes.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldLookupChange(TBoldElement? DestElement, TBoldElement? NewValue);

/// <summary>The unit-level settings of BoldGrid.</summary>
public static class BoldGrid
{
    /// <summary>When true, every grid column whose expression is a Boolean shows a check box (not only those that allow it).</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldAllowCheckBoxInGrids = false;
}

/// <summary>The renderer of a grid's fixed first column: a mark on the current row and on each selected row.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFirstColumnRenderer : TBoldAsStringRenderer
{
    private readonly TBoldCustomGrid fGrid;

    /// <summary>The renderer of the grid's first column.</summary>
    public TBoldFirstColumnRenderer(TBoldCustomGrid Grid)
    {
        fGrid = Grid;
    }

    [Delphi(PortStatus.Ported, Note = "The marks are drawn when the canvas draws on a Graphics.")]
    protected internal override void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins)
    {
        base.DrawOnCanvas(Follower, Canvas, Rect, Alignment, Margins);
        Canvas.Brush.Color = fGrid.FixedColor;
        Canvas.FillRect(Rect);
        TBoldFollower? Row = Follower.OwningFollower;
        if (Row?.OwningFollower is { } List && List.CurrentIndex == Row.index)
        {
            DrawBitmap(Canvas, Rect.Left + 8, Rect.Top + 4, BoldCommonBitmaps.bmpBoldGridCurrent);
        }

        if (Row is { Selected: true })
        {
            DrawBitmap(Canvas, Rect.Left, Rect.Top + 4, BoldCommonBitmaps.bmpBoldGridSelected);
        }
    }

    /// <summary>Draws the bitmap at (X, Y) when the canvas draws on a Graphics (Delphi's Canvas.Draw).</summary>
    internal static void DrawBitmap(TCanvas Canvas, int X, int Y, Bitmap? Bitmap)
    {
        if (Bitmap is not null && Canvas is TBoldGraphicsCanvas graphicsCanvas)
        {
            graphicsCanvas.Graphics.DrawImage(Bitmap, X, Y, Bitmap.Width, Bitmap.Height);
        }
    }
}

/// <summary>
/// The renderer of a grid column that shows a Boolean as a check box: a click on the box or the space key toggles the
/// value (null, true, false; or the column's own states when it overrides them).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGridCheckBoxPainterRenderer : TBoldAsStringRenderer
{
    internal TBoldGridColumn? fColumn;

    /// <summary>The element the follower's expression gives, in <paramref name="ie"/>.</summary>
    [Delphi(PortStatus.Ported)]
    private static void GetCurrentElement(TBoldFollower Follower, TBoldIndirectElement ie)
    {
        var StringController = (TBoldStringFollowerController)Follower.AssertedController;
        Follower.Element?.EvaluateExpression(StringController.Expression, ie, false, StringController.VariableList);
    }

    /// <summary>The state of the check box: from the column when it overrides the states, else from the Boolean.</summary>
    [Delphi(PortStatus.Ported)]
    private TCheckBoxState GetCurrentCheckBoxState(TBoldFollower? Follower)
    {
        if (fColumn!.ColumnHasCheckBoxOverrides())
        {
            return fColumn.GetCurrentCheckBoxState(Follower!);
        }

        using var ie = new TBoldIndirectElement();
        if (Follower is not null)
        {
            GetCurrentElement(Follower, ie);
        }

        if (ie.Value is TBABoolean BoolValue)
        {
            return BoolValue.IsNull ? TCheckBoxState.cbGrayed : BoolValue.AsBoolean ? TCheckBoxState.cbChecked : TCheckBoxState.cbUnchecked;
        }

        return TCheckBoxState.cbGrayed;
    }

    /// <summary>The 12 pixel box at the bottom of the cell, placed as the column aligns.</summary>
    [Delphi(PortStatus.Ported)]
    private static Rectangle GetCheckBoxRect(Rectangle rect, TAlignment Alignment)
    {
        const int CheckBoxSize = 12;
        int Bottom = rect.Bottom - 3;
        int Top = Bottom - CheckBoxSize;
        int Left = Alignment switch
        {
            TAlignment.taCenter => rect.Left + (rect.Right - rect.Left - CheckBoxSize) / 2,
            TAlignment.taLeftJustify => rect.Left + 3,
            _ => rect.Right - 3 - CheckBoxSize,
        };
        return Rectangle.FromLTRB(Left, Top, Left + CheckBoxSize, Bottom);
    }

    /// <summary>A click in a cell of the column: on the box it toggles the value; the cell becomes the current one.</summary>
    [Delphi(PortStatus.Ported, Note = "The cell's left and right come from the grid's cell rectangle, where Delphi added up the column widths.")]
    public void CheckBoxClick(MouseButtons BUTTON, Keys Shift, int X, int Y, TBoldCustomGrid Grid, TAlignment Alignment)
    {
        Point GridCoord = Grid.MouseCoord(X, Y);
        Rectangle CheckBoxRect = GetCheckBoxRect(Grid.CellRect(GridCoord.X, GridCoord.Y), Alignment);
        if (GridCoord.Y >= Grid.FixedRows && GridCoord.Y < Grid.GridRowCount)
        {
            if (X >= CheckBoxRect.Left && X <= CheckBoxRect.Right)
            {
                ToggleValue(GridCoord, Grid);
            }

            if (GridCoord.X > 0 && GridCoord.X < Grid.Columns.Count)
            {
                Grid.Col = GridCoord.X;
                Grid.Row = GridCoord.Y;
            }
        }
    }

    /// <summary>The space key (alone) toggles the current cell's value.</summary>
    [Delphi(PortStatus.Ported)]
    public void KeyPress(ref Keys Key, Keys Shift, TBoldCustomGrid Grid)
    {
        var GridCoord = new Point(Grid.Col, Grid.Row);
        if (Key == Keys.Space && Shift == Keys.None)
        {
            ToggleValue(GridCoord, Grid);
            Key = Keys.None;
        }
    }

    /// <summary>Toggles the cell's value: null to false, false to true, true to null (or false when it may not be null).</summary>
    [Delphi(PortStatus.Ported)]
    private void ToggleValue(Point GridCoord, TBoldCustomGrid Grid)
    {
        TBoldFollower? CurrentFollower = Grid.CellFollowers(GridCoord.X, GridCoord.Y - Grid.FixedRows);
        if (CurrentFollower is null || !CurrentFollower.RendererData.MayModify)
        {
            return;
        }

        if (fColumn!.ColumnHasCheckBoxOverrides())
        {
            switch (fColumn.GetCurrentCheckBoxState(CurrentFollower))
            {
                case TCheckBoxState.cbGrayed:
                    fColumn.SetCurrentCheckBoxState(CurrentFollower, TCheckBoxState.cbChecked);
                    break;
                case TCheckBoxState.cbChecked:
                    fColumn.SetCurrentCheckBoxState(CurrentFollower, TCheckBoxState.cbUnchecked);
                    break;
                case TCheckBoxState.cbUnchecked:
                    fColumn.SetCurrentCheckBoxState(CurrentFollower, TCheckBoxState.cbChecked);
                    break;
            }
        }
        else
        {
            TCheckBoxState CheckBoxState = GetCurrentCheckBoxState(CurrentFollower);
            using var ie = new TBoldIndirectElement();
            GetCurrentElement(CurrentFollower, ie);
            if (ie.Value is TBABoolean BoolValue && BoolValue.CanModify())
            {
                switch (CheckBoxState)
                {
                    case TCheckBoxState.cbGrayed:
                        BoolValue.AsBoolean = false;
                        break;
                    case TCheckBoxState.cbUnchecked:
                        BoolValue.AsBoolean = true;
                        break;
                    case TCheckBoxState.cbChecked:
                        if (BoolValue.CanSetToNull(null))
                        {
                            BoolValue.SetToNull();
                        }
                        else
                        {
                            BoolValue.AsBoolean = false;
                        }

                        break;
                }
            }
        }
    }

    /// <summary>Draws the box (highlighted when the cell is the current one), with a tick or a gray fill.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins)
    {
        TCheckBoxState CheckBoxState = GetCurrentCheckBoxState(Follower);
        Color OrgPenColor = Canvas.Pen.Color;
        Color OrgBrushColor = Canvas.Brush.Color;

        var Grid = (TBoldCustomGrid)Follower.AssertedController.OwningComponent!;
        Point GridCoord = Grid.MouseCoord(Rect.Left + 1, Rect.Top + 1);
        Canvas.Brush.Color = GridCoord.X == Grid.Col && GridCoord.Y == Grid.Row ? SystemColors.Highlight : Grid.Color;
        Canvas.FillRect(Rect);

        Rectangle CheckBoxRect = GetCheckBoxRect(Rect, Alignment);
        Canvas.Brush.Color = Color.White;
        Canvas.FillRect(CheckBoxRect);
        Canvas.Pen.Color = Color.Gray;
        Canvas.Line(CheckBoxRect.Left, CheckBoxRect.Bottom, CheckBoxRect.Left, CheckBoxRect.Top);
        Canvas.Line(CheckBoxRect.Left, CheckBoxRect.Top, CheckBoxRect.Right, CheckBoxRect.Top);

        Canvas.Pen.Color = Color.Black;
        Canvas.Line(CheckBoxRect.Left + 1, CheckBoxRect.Bottom - 1, CheckBoxRect.Left + 1, CheckBoxRect.Top + 1);
        Canvas.Line(CheckBoxRect.Left + 1, CheckBoxRect.Top + 1, CheckBoxRect.Right - 1, CheckBoxRect.Top + 1);

        Canvas.Pen.Color = Color.LightGray;
        Canvas.Line(CheckBoxRect.Left + 1, CheckBoxRect.Bottom, CheckBoxRect.Right - 1, CheckBoxRect.Bottom);
        Canvas.Line(CheckBoxRect.Right - 1, CheckBoxRect.Bottom, CheckBoxRect.Right - 1, CheckBoxRect.Top);

        switch (CheckBoxState)
        {
            case TCheckBoxState.cbChecked:
                Canvas.Pen.Color = Color.Black;
                for (int i = 0; i <= 2; i++)
                {
                    Canvas.Line(CheckBoxRect.Left + 3, CheckBoxRect.Top + 5 + i, CheckBoxRect.Left + 5, CheckBoxRect.Top + 7 + i);
                    Canvas.Line(CheckBoxRect.Left + 5, CheckBoxRect.Top + 7 + i, CheckBoxRect.Left + 10, CheckBoxRect.Top + 2 + i);
                }

                break;
            case TCheckBoxState.cbGrayed:
                Canvas.Brush.Color = SystemColors.ControlLight;
                Canvas.FillRect(Rectangle.FromLTRB(CheckBoxRect.Left + 2, CheckBoxRect.Top + 2, CheckBoxRect.Right - 1, CheckBoxRect.Bottom - 1));
                break;
        }

        Canvas.Pen.Color = OrgPenColor;
        Canvas.Brush.Color = OrgBrushColor;
    }
}

/// <summary>The renderer of the constraint column: a mark when the object breaks a constraint or keeps them all; never editable.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldConstraintRenderer : TBoldAsStringRenderer
{
    [Delphi(PortStatus.Ported)]
    public override void DefaultMakeUptodateAndSetMayModifyAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        base.DefaultMakeUptodateAndSetMayModifyAndSubscribe(Element, RendererData, FollowerController, Subscriber);
        RendererData.MayModify = false;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins)
    {
        Canvas.FillRect(Rect);
        int RectMiddle = (Rect.Bottom - Rect.Top) / 2;
        string value = ((TBoldStringRendererData)Follower.RendererData).CurrentStringValue;
        Bitmap? bitmap = value == "Y" ? BoldCommonBitmaps.bmpBoldGridConstraint_false : value == "N" ? BoldCommonBitmaps.bmpBoldGridConstraint_true : null;
        if (bitmap is not null)
        {
            int IncValue = RectMiddle - bitmap.Height / 2;
            TBoldFirstColumnRenderer.DrawBitmap(Canvas, Rect.Left + 1, Rect.Top + IncValue, bitmap);
        }
    }
}

/// <summary>The columns of a grid; the grid has a column per item, and column 0 is the fixed one.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGridColumns : TCollection
{
    private readonly TBoldCustomGrid fGrid;

    [Delphi(PortStatus.Ported, Note = "ColumnClass is TBoldGridColumn or a descendant.")]
    public TBoldGridColumns(TBoldCustomGrid theGrid, Type ColumnClass)
        : base(ColumnClass)
    {
        fGrid = theGrid;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldGridColumn GetColumn(int index) => (TBoldGridColumn)base[index];

    [Delphi(PortStatus.Ported)]
    internal void MoveColumn(int FromIndex, int ToIndex) => GetColumn(FromIndex).Index = ToIndex;

    [Delphi(PortStatus.Ported)]
    private void SetColumn(int index, TBoldGridColumn Value) => GetColumn(index).Assign(Value);

    /// <summary>A new column at the end.</summary>
    [Delphi(PortStatus.Ported)]
    public new TBoldGridColumn Add()
    {
        BeginUpdate();
        try
        {
            return (TBoldGridColumn)base.Add();
        }
        finally
        {
            EndUpdate();
        }
    }

    /// <summary>The grid gets a column per item (at least one), and draws again.</summary>
    [Delphi(PortStatus.Ported, Note = "Invalidates the grid where Delphi refreshed it at once.")]
    protected override void Update(TCollectionItem? Item)
    {
        if (fGrid.fDestroying)
        {
            return;
        }

        if (fGrid.ColCount != Count)
        {
            fGrid.ColCount = Math.Max(1, Count);
        }

        if (Item is not null)
        {
            fGrid.ReallyInvalidateCol(Item.Index);
        }
        else
        {
            fGrid.Invalidate();
        }
    }

    internal void UpdateColumn(TBoldGridColumn Column) => Update(Column);

    /// <summary>A column leaving the collection is destroyed (its controller goes with it).</summary>
    protected override void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
        base.Notify(Item, Action);
        if (Action == TCollectionNotification.cnExtracting && Item is TBoldGridColumn column)
        {
            column.Destroying();
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldCustomGrid Grid => fGrid;

    [Delphi(PortStatus.Ported, Symbol = "TBoldGridColumns.Items")]
    public new TBoldGridColumn this[int index]
    {
        get => GetColumn(index);
        set => SetColumn(index, value);
    }
}

/// <summary>
/// The title of a grid column: alignment, caption, color and font, each the grid's default (the expression as the
/// caption, the fixed color, the title font) until set.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Borrowed from Delphi's TDBGrid.")]
public class TBoldColumnTitle : TPersistent
{
    private TAlignment fAlignment;
    private string fCaption = "";
    private Color FColor;
    private readonly TBoldGridColumn fColumn;
    private readonly TFont fFont;
    private ContextMenuStrip? fPopupMenu;

    [Delphi(PortStatus.Ported)]
    public TBoldColumnTitle(TBoldGridColumn Column)
    {
        fColumn = Column;
        fFont = new TFont();
        fFont.Assign(DefaultFont());
        fFont.OnChange = _FontChanged;
    }

    /// <summary>Copies the title's values that its column set.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldColumnTitle S)
        {
            TBoldColumnValues AssignedValues = S.fColumn.fAssignedValues;
            if (AssignedValues.Contains(cvTitleAlignment))
            {
                Alignment = S.Alignment;
            }

            if (AssignedValues.Contains(cvTitleColor))
            {
                Color = S.Color;
            }

            if (AssignedValues.Contains(cvTitleCaption))
            {
                Caption = S.Caption;
            }

            if (AssignedValues.Contains(cvTitleFont))
            {
                Font = S.Font;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void Changed() => fColumn.Grid.InvalidateGridCell(fColumn.Index, 0);

    [Delphi(PortStatus.Ported)]
    public TAlignment DefaultAlignment() => TAlignment.taLeftJustify;

    [Delphi(PortStatus.Ported)]
    public Color DefaultColor() => fColumn.Grid is { } Grid ? Grid.FixedColor : SystemColors.Control;

    [Delphi(PortStatus.Ported)]
    public TFont DefaultFont() => fColumn.Grid is { } Grid ? Grid.TitleFont : fColumn.Font;

    [Delphi(PortStatus.Ported)]
    public string DefaultCaption() => fColumn.BoldProperties?.Expression ?? "";

    [Delphi(PortStatus.Ported)]
    private void _FontChanged(object? Sender)
    {
        fColumn.fAssignedValues = fColumn.AreFontsEqual((TFont)Sender!, DefaultFont())
            ? fColumn.fAssignedValues.Exclude(cvTitleFont)
            : fColumn.fAssignedValues.Include(cvTitleFont);
        Changed();
    }

    [Delphi(PortStatus.Ported)]
    private TAlignment GetAlignment() => fColumn.fAssignedValues.Contains(cvTitleAlignment) ? fAlignment : DefaultAlignment();

    [Delphi(PortStatus.Ported)]
    private Color GetColor() => fColumn.fAssignedValues.Contains(cvTitleColor) ? FColor : DefaultColor();

    [Delphi(PortStatus.Ported)]
    private string GetCaption() => fColumn.fAssignedValues.Contains(cvTitleCaption) ? fCaption : DefaultCaption();

    /// <summary>The font: the default one (copied in) until the title's own is set.</summary>
    [Delphi(PortStatus.Ported)]
    private TFont GetFont()
    {
        if (!fColumn.fAssignedValues.Contains(cvTitleFont))
        {
            TFont Default = DefaultFont();
            if (!fColumn.AreFontsEqual(Default, fFont))
            {
                TNotifyEvent? SavedOnChange = fFont.OnChange;
                fFont.OnChange = null;
                fFont.Assign(Default);
                fFont.OnChange = SavedOnChange;
            }
        }

        return fFont;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsAlignmentStored() => fColumn.fAssignedValues.Contains(cvTitleAlignment) && fAlignment != DefaultAlignment();

    [Delphi(PortStatus.Ported)]
    public bool IsColorStored() => fColumn.fAssignedValues.Contains(cvTitleColor) && FColor != DefaultColor();

    [Delphi(PortStatus.Ported)]
    public bool IsFontStored() => fColumn.fAssignedValues.Contains(cvTitleFont);

    [Delphi(PortStatus.Ported)]
    public bool IsCaptionStored() => fColumn.fAssignedValues.Contains(cvTitleCaption) && fCaption != DefaultCaption();

    [Delphi(PortStatus.Ported)]
    protected void RefreshDefaultFont()
    {
        if (fColumn.fAssignedValues.Contains(cvTitleFont))
        {
            return;
        }

        TNotifyEvent? SavedOnChange = fFont.OnChange;
        fFont.OnChange = null;
        try
        {
            fFont.Assign(DefaultFont());
        }
        finally
        {
            fFont.OnChange = SavedOnChange;
        }
    }

    /// <summary>Back to the defaults: none of the title's values are its own.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void RestoreDefaults()
    {
        bool FontAssigned = fColumn.fAssignedValues.Contains(cvTitleFont);
        fColumn.fAssignedValues = fColumn.fAssignedValues - TBoldColumnValues.Of(cvTitleColor, cvTitleCaption, cvTitleAlignment, cvTitleFont);
        fCaption = "";
        RefreshDefaultFont();
        // If font was assigned, changing it back to default may affect grid title
        // height, and title height changes require layout and redraw of the grid.
        fColumn.DoChanged(FontAssigned);
    }

    [Delphi(PortStatus.Ported)]
    private void SetAlignment(TAlignment Value)
    {
        fColumn.fAssignedValues = Value == DefaultAlignment()
            ? fColumn.fAssignedValues.Exclude(cvTitleAlignment)
            : fColumn.fAssignedValues.Include(cvTitleAlignment);
        fAlignment = Value;
        Changed();
    }

    [Delphi(PortStatus.Ported)]
    private void SetColor(Color Value)
    {
        fColumn.fAssignedValues = Value == DefaultColor()
            ? fColumn.fAssignedValues.Exclude(cvTitleColor)
            : fColumn.fAssignedValues.Include(cvTitleColor);
        FColor = Value;
        Changed();
    }

    [Delphi(PortStatus.Ported)]
    private void SetFont(TFont Value) => fFont.Assign(Value);

    [Delphi(PortStatus.Ported)]
    protected virtual void SetCaption(string Value)
    {
        fColumn.fAssignedValues = Value == DefaultCaption()
            ? fColumn.fAssignedValues.Exclude(cvTitleCaption)
            : fColumn.fAssignedValues.Include(cvTitleCaption);
        fCaption = Value;
        Changed();
    }

    [Delphi(PortStatus.Ported, Note = "Delphi asked the menu to tell the grid when it was freed, which the grid did not use.")]
    private void SetPopupMenu(ContextMenuStrip? Value) => fPopupMenu = Value;

    [Delphi(PortStatus.Ported)]
    public TAlignment Alignment
    {
        get => GetAlignment();
        set => SetAlignment(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Caption
    {
        get => GetCaption();
        set => SetCaption(value);
    }

    [Delphi(PortStatus.Ported)]
    public Color Color
    {
        get => GetColor();
        set => SetColor(value);
    }

    [Delphi(PortStatus.Ported)]
    public TFont Font
    {
        get => GetFont();
        set => SetFont(value);
    }

    /// <summary>The menu a right click on the title shows; without one the grid's default title menu shows.</summary>
    [Delphi(PortStatus.Ported)]
    public ContextMenuStrip? PopupMenu
    {
        get => fPopupMenu;
        set => SetPopupMenu(value);
    }
}

/// <summary>
/// A column of a Bold grid: its controller (BoldProperties) gives each row's cell, and it has a title, a font, a
/// color, an alignment and a width. With a lookup handle, editing a cell offers the handle's elements in a combo box;
/// a Boolean column may show check boxes.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGridColumn : TCollectionItem, IDisposable
{
    private const int breColumnLookUpHandleDestroyed = 101;

    internal TBoldGridCheckBoxPainterRenderer? fCheckBoxPainterRenderer;
    private bool fAllowCheckBox;
    private TBoldAbstractListHandle? fLookupHandle;
    private TBoldNilElementMode fNilElementMode;
    private TBoldStringFollowerController? fLookUpProperties;
    private TBoldLookupChange? fOnLookupChange;
    private TBoldPassthroughSubscriber? fLookupHandleSubscriber;
    private TAlignment fAlignment;
    internal TBoldColumnValues fAssignedValues = TBoldColumnValues.Of();
    private readonly TBoldStringFollowerController fBoldProperties;
    private Color fColor;
    private TBoldCWAdjustSet fCWAdjust = TBoldCWAdjustSet.Of();
    private bool fDefaultPopupMenu;
    private TFont? FFont;
    private readonly TBoldCustomGrid fGrid;
    private bool fReadOnly;
    private TBoldColumnTitle? FTitle;
    private bool fUserDraw;

    /// <summary>A column of the grid whose columns the collection is; a column cannot be made in another collection.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldGridColumn(TCollection theCollection)
        : base(CheckedCollection(theCollection))
    {
        fGrid = ((TBoldGridColumns)theCollection).Grid;
        fBoldProperties = new TBoldStringFollowerController(fGrid);
        fColor = fGrid.Color;
        fBoldProperties.AfterMakeUptoDate = fGrid._AfterMakeCellUptoDate;
        fBoldProperties.OnGetContextType = fGrid.GetHandleStaticType;
        fGrid.ColumnsProperties.Add(fBoldProperties);
        FTitle = new TBoldColumnTitle(this);
        FFont = new TFont
        {
            OnChange = _FontChanged,
        };
        DefaultTitlePopupMenu = true;
        fLookUpProperties = new TBoldStringFollowerController(fGrid);
        fLookUpProperties.OnGetContextType = GetLookupContext;
        fOnLookupChange = null;
        fNilElementMode = TBoldNilElementMode.neNone;
        fLookupHandleSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    private static TCollection CheckedCollection(TCollection theCollection) =>
        theCollection is TBoldGridColumns
            ? theCollection
            : throw new EBold(SysUtils.Format(sCannotCreateColumnOutsideCollection, nameof(TBoldGridColumn)));

    /// <summary>Takes the column out of its grid (and destroys it, as Delphi's Free did).</summary>
    public void Dispose()
    {
        Collection = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>The column leaves the grid: its controller goes, and its lookup subscriptions.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldGridColumn.Destroy", Note = "Runs when the column leaves its collection (Dispose, Delete or Clear).")]
    internal void Destroying()
    {
        if (fGrid.fFixedColumn == this)
        {
            fGrid.fFixedColumn = null;
        }

        if (Index >= 0 && Index < fGrid.ColumnsProperties.Count)
        {
            fGrid.ColumnsProperties.Delete(Index);
        }

        FTitle = null;
        FFont = null;
        SysUtils.FreeAndNil(ref fLookUpProperties);
        SysUtils.FreeAndNil(ref fLookupHandleSubscriber);
    }

    /// <summary>Copies another column's controller, title and the values it set.</summary>
    [Delphi(PortStatus.Ported, Note = "Mainly from Delphi's DBGrids.")]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldGridColumn SourceCol)
        {
            Collection?.BeginUpdate();
            try
            {
                RestoreDefaults();
                BoldProperties = SourceCol.BoldProperties;
                if (SourceCol.AssignedValues.Contains(cvColor))
                {
                    Color = SourceCol.Color;
                }

                if (SourceCol.AssignedValues.Contains(cvWidth))
                {
                    Width = SourceCol.Width;
                }

                if (SourceCol.AssignedValues.Contains(cvFont))
                {
                    Font = SourceCol.Font;
                }

                if (SourceCol.AssignedValues.Contains(cvAlignment))
                {
                    Alignment = SourceCol.Alignment;
                }

                Title = SourceCol.Title;
            }
            finally
            {
                Collection?.EndUpdate();
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    internal void DoChanged(bool AllItems) => Changed(AllItems);

    [Delphi(PortStatus.Ported)]
    private void SetFont(TFont Value)
    {
        FFont!.Assign(Value);
        fAssignedValues = fAssignedValues.Include(cvFont);
        Changed(false);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldStringFollowerController? Value)
    {
        if (Value is not null)
        {
            fBoldProperties.Assign(Value);
        }
    }

    /// <summary>Back to the defaults: none of the column's values (nor its title's) are its own.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void RestoreDefaults()
    {
        bool FontAssigned = fAssignedValues.Contains(cvFont);
        FTitle!.RestoreDefaults();
        fAssignedValues = TBoldColumnValues.Of();
        RefreshDefaultFont();
        Changed(FontAssigned);
    }

    [Delphi(PortStatus.Ported)]
    protected void RefreshDefaultFont()
    {
        if (fAssignedValues.Contains(cvFont) || FFont is null)
        {
            return;
        }

        TNotifyEvent? SavedOnChange = FFont.OnChange;
        FFont.OnChange = null;
        try
        {
            FFont.Assign(DefaultFont());
        }
        finally
        {
            FFont.OnChange = SavedOnChange;
        }
    }

    /// <summary>The grid's font.</summary>
    [Delphi(PortStatus.Ported)]
    public TFont DefaultFont() => fGrid.GridFont();

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName()
    {
        string Result = FTitle?.Caption ?? "";
        return Result == "" ? base.GetDisplayName() : Result;
    }

    [Delphi(PortStatus.Ported)]
    private void SetTitle(TBoldColumnTitle V)
    {
        if (V != FTitle)
        {
            FTitle!.Assign(V);
            fGrid.InvalidateGridCell(Index, 0);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColor(Color C)
    {
        if (Color != C)
        {
            fColor = C;
            Changed(true);
        }
    }

    /// <summary>The font changed: it is the column's own unless it is the grid's.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi also assigned the font to itself, which only raised the change again.")]
    private void _FontChanged(object? Sender)
    {
        fAssignedValues = AreFontsEqual((TFont)Sender!, DefaultFont())
            ? fAssignedValues.Exclude(cvTitleFont)
            : fAssignedValues.Include(cvTitleFont);
        fGrid.ReallyInvalidateCol(Index);
    }

    [Delphi(PortStatus.Ported)]
    private void SetWidth(int Value) => fGrid.SetColWidth(Index, Value);

    [Delphi(PortStatus.Ported)]
    private int GetWidth() => fGrid.GetColWidth(Index);

    [Delphi(PortStatus.Ported)]
    private void SetCWAdjust(TBoldCWAdjustSet V)
    {
        if (V != fCWAdjust)
        {
            fCWAdjust = V;
            fGrid.AdjustCol(Index);
        }
    }

    /// <summary>The font: the grid's (copied in) until the column's own is set.</summary>
    [Delphi(PortStatus.Ported)]
    private TFont GetFont()
    {
        if (!fAssignedValues.Contains(cvTitleFont) && FFont is not null)
        {
            TFont Default = DefaultFont();
            if (!AreFontsEqual(Default, FFont))
            {
                TNotifyEvent? SavedOnChange = FFont.OnChange;
                FFont.OnChange = null;
                FFont.Assign(Default);
                FFont.OnChange = SavedOnChange;
            }
        }

        return FFont!;
    }

    [Delphi(PortStatus.Ported, Note = "Compares the name, size, style and color (a TFont of the port has no charset, height or pitch).")]
    internal bool AreFontsEqual(TFont Font1, TFont Font2) =>
        Font1.Color == Font2.Color && Font1.Name == Font2.Name && Font1.Size == Font2.Size && Font1.Style == Font2.Style;

    /// <summary>Moves the column (and its controller); the grid draws again.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void SetIndex(int Value)
    {
        Value = Math.Clamp(Value, 0, Math.Max(0, (Collection?.Count ?? 1) - 1));
        if (Value != Index)
        {
            fGrid.ColumnsProperties.Move(Index, Value);
            base.Index = Value;
        }

        fGrid.Invalidate(); // Fixes a bug in Borland grid to invalidate col that does not handle scrolled grids
    }

    public override int Index
    {
        get => base.Index;
        set => SetIndex(value);
    }

    /// <summary>Gives the column the check box renderer when its expression is a Boolean (and check boxes are allowed).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetCheckBoxRendererIfAppropriate()
    {
        if (BoldProperties.Renderer is null && fGrid.BoldHandle?.ListElementType is { } ListElementTypeInfo &&
            (AllowCheckBox || BoldGrid.BoldAllowCheckBoxInGrids) &&
            ListElementTypeInfo.SystemTypeInfo is TBoldSystemTypeInfo SystemTypeInfo)
        {
            TBoldAttributeTypeInfo? BooleanTypeInfo = SystemTypeInfo.AttributeTypeInfoByExpressionName("Boolean"); // do not localize
            TBoldElementTypeInfo? ResultTypeInfo = SystemTypeInfo.Evaluator?.ExpressionType(BoldProperties.Expression, ListElementTypeInfo, false, BoldProperties.VariableList);
            if ((ResultTypeInfo is not null && ResultTypeInfo.ConformsTo(BooleanTypeInfo)) || ColumnHasCheckBoxOverrides())
            {
                fCheckBoxPainterRenderer = new TBoldGridCheckBoxPainterRenderer
                {
                    fColumn = this,
                };
                BoldProperties.Renderer = fCheckBoxPainterRenderer;
            }
        }
    }

    /// <summary>True when the column gives the check box states itself (GetCurrentCheckBoxState, SetCurrentCheckBoxState).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual bool ColumnHasCheckBoxOverrides() => false;

    /// <summary>The check box state of the cell; asked only when ColumnHasCheckBoxOverrides.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual TCheckBoxState GetCurrentCheckBoxState(TBoldFollower Follower) => TCheckBoxState.cbGrayed;

    /// <summary>Sets the cell's check box state; asked only when ColumnHasCheckBoxOverrides.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetCurrentCheckBoxState(TBoldFollower Follower, TCheckBoxState NewValue)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetLookupContext() => LookupHandle?.StaticBoldType;

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breColumnLookUpHandleDestroyed)
        {
            LookupHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetLookupHandle(TBoldAbstractListHandle? Value)
    {
        if (Value != LookupHandle)
        {
            fLookupHandleSubscriber?.CancelAllSubscriptions();
            fLookupHandle = Value;
            fLookupHandle?.AddSmallSubscription(fLookupHandleSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), breColumnLookUpHandleDestroyed);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetLookUpProperties(TBoldStringFollowerController? Value)
    {
        if (Value is not null)
        {
            fLookUpProperties?.Assign(Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldColumnValues AssignedValues => fAssignedValues;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomGrid Grid => fGrid;

    [Delphi(PortStatus.Ported)]
    public TAlignment Alignment
    {
        get => fAlignment;
        set => fAlignment = value;
    }

    /// <summary>The controller of the column's cells.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    public Color Color
    {
        get => fColor;
        set => SetColor(value);
    }

    /// <summary>The column's cells are not edited.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ColReadOnly
    {
        get => fReadOnly;
        set => fReadOnly = value;
    }

    /// <summary>How the column's width follows its contents (when the grid scrolls, or the user clicks without moving).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldCWAdjustSet CWAdjust
    {
        get => fCWAdjust;
        set => SetCWAdjust(value);
    }

    /// <summary>A right click on the title shows the grid's title menu (when the title has no menu of its own).</summary>
    [Delphi(PortStatus.Ported)]
    public bool DefaultTitlePopupMenu
    {
        get => fDefaultPopupMenu;
        set => fDefaultPopupMenu = value;
    }

    [Delphi(PortStatus.Ported)]
    public TFont Font
    {
        get => GetFont();
        set => SetFont(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldColumnTitle Title
    {
        get => FTitle!;
        set => SetTitle(value);
    }

    /// <summary>The grid's OnDrawCell draws the column's cells.</summary>
    [Delphi(PortStatus.Ported)]
    public bool UserDraw
    {
        get => fUserDraw;
        set => fUserDraw = value;
    }

    [Delphi(PortStatus.Ported)]
    public int Width
    {
        get => GetWidth();
        set => SetWidth(value);
    }

    /// <summary>The column may show check boxes (when its expression is a Boolean).</summary>
    [Delphi(PortStatus.Ported)]
    public bool AllowCheckBox
    {
        get => fAllowCheckBox;
        set => fAllowCheckBox = value;
    }

    /// <summary>The controller of the strings that the lookup combo box shows.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController LookUpProperties
    {
        get => fLookUpProperties!;
        set => SetLookUpProperties(value);
    }

    /// <summary>Whether the lookup combo box offers no element, first or last.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldNilElementMode NilElementMode
    {
        get => fNilElementMode;
        set => fNilElementMode = value;
    }

    /// <summary>The elements a cell of the column may be set to, offered in a combo box when the cell is edited.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldAbstractListHandle? LookupHandle
    {
        get => fLookupHandle;
        set => SetLookupHandle(value);
    }

    /// <summary>Called with the cell's element and the chosen one instead of assigning the choice.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldLookupChange? OnLookupChange
    {
        get => fOnLookupChange;
        set => fOnLookupChange = value;
    }
}

/// <summary>A DataGridView column of a Bold grid: its cells are edited in a TBoldInplaceEdit and drawn by the grid.</summary>
public class TBoldGridViewColumn : DataGridViewColumn
{
    public TBoldGridViewColumn()
        : base(new TBoldGridViewCell())
    {
        SortMode = DataGridViewColumnSortMode.NotSortable;
        MinimumWidth = 2;
    }
}

/// <summary>A DataGridView cell of a Bold grid: edited in a TBoldInplaceEdit, except in a check box column.</summary>
public class TBoldGridViewCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(TBoldInplaceEdit);

    public override bool KeyEntersEditMode(KeyEventArgs e) =>
        (DataGridView is not TBoldCustomGrid grid || !grid.ColumnIsCheckBox(ColumnIndex)) && base.KeyEntersEditMode(e);
}

/// <summary>
/// A grid that shows a list handle's list, a row per element and a column per column controller; its current row
/// follows the handle's current element (when locked to it). Column 0 is fixed: it marks the current and the selected
/// rows, and a drag starting there drags the selected elements. Cells are edited as their controllers allow; a lookup
/// column offers its lookup handle's elements in a combo box, and a Boolean column may show check boxes.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms DataGridView in virtual mode, drawn by the columns' renderers; Bold keeps the values and the selection.")]
[Designer("System.Windows.Forms.Design.ControlDesigner, System.Design")]
public class TBoldCustomGrid : DataGridView, IBoldValidateableComponent
{
    private const int DefaultColWidthValue = 64;

    private TBoldFirstColumnRenderer? fFirstColumnRenderer;
    private bool fAddNewAtEnd;
    private bool fAlwaysShowSelected;
    private TBoldControllerList? fBoldColumnsProperties;
    private TBoldListAsFollowerListController? fBoldProperties;
    private TBoldGridColumns? FColumns;
    private TBoldElementTypeInfo? fCurrentListElementType;
    private bool fEnableColAdjust;
    private TBoldListHandleFollower? fHandleFollower;
    private bool fBoldAutoColumns;
    private TBoldDrawCellEvent? fOndrawCell;
    private TNotifyEvent? fOnTopLeftChanged;
    private bool fRangeSelect;
    private TFont? fTitleFont;
    private Point fLastMouseDownScreenCoord;
    private Point fLastMouseDownGridCoord;
    private bool fBoldShowConstraints;
    private int fInvalidateFrom;
    private bool fMakingListUpToDate;
    private bool fHasGhostRow;
    private TSelectCellEvent? FOnSelectCell;
    private bool fIsEnsuringFixedCol;
    private bool fLookUpEditorActive;
    internal TBoldGridColumn? fFixedColumn;
    private bool fAutoSelectNewRows;
    private int fSubFollowerCountBeforeMakeUpToDate;
    private int fLastInsertedRowIndex;
    private TBoldElement? fEditedElementBeforeMakeUpToDate;
    private bool fBoldDragAnywhere;
    private bool fIsDragging;
    private bool fIsMultiSelecting;
    private Keys fLastMouseDownShiftState;

    // The DataGridView side: Delphi's TCustomGrid state.
    private TGridOptions fOptions;
    private int fFixedCols;
    private int fCol = 1;
    private Color fFixedColor = SystemColors.Control;
    private int fDefaultColWidth = DefaultColWidthValue;
    private TCanvas? fCanvas;
    private TBoldInplaceEdit? fInplaceEditor;
    private ContextMenuStrip? fDefaultTitlePopup;
    private bool fSyncingColumns;
    private bool fColumnMovePending;
    private bool fLoaded;
    internal bool fDestroying;

    [Delphi(PortStatus.Ported, Note = "The DataGridView is virtual: the grid draws every cell; rows are added for the list (or one empty row).")]
    public TBoldCustomGrid()
    {
        VirtualMode = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        RowHeadersVisible = false;
        base.MultiSelect = false;
        BackgroundColor = SystemColors.Window;
        DoubleBuffered = true;

        fFirstColumnRenderer = new TBoldFirstColumnRenderer(this);
        fBoldColumnsProperties = new TBoldControllerList(this);
        fBoldProperties = new TBoldListAsFollowerListController(this, fBoldColumnsProperties);
        fBoldProperties.OnAfterInsertItem = _InsertRow;
        fBoldProperties.OnAfterDeleteItem = _DeleteRow;
        fBoldProperties.AfterMakeUptoDate = _AfterMakeListUptoDate;
        fBoldProperties.BeforeMakeUptoDate = _BeforeMakeListUpToDate;
        fBoldProperties.OnGetContextType = GetHandleStaticType;
        fHandleFollower = new TBoldListHandleFollower(this, fBoldProperties);
        FColumns = CreateColumns();
        Options = TGridOptions.Of(goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRangeSelect, goEditing, goColMoving, goColSizing);
        AlwaysShowSelected = true;
        fTitleFont = BoldWinForms.ToTFont(Font, ForeColor);
        fTitleFont.OnChange = _FontChanged;
        fIsEnsuringFixedCol = false;
        fLastMouseDownScreenCoord = new Point(-1, -1);
        fLastMouseDownGridCoord = new Point(-1, -1);
        EnsureOneFixedCol();
        fHasGhostRow = true;
        GridRowCount = FixedRows + 1;
        AllowDrop = true;
        _ = new TBoldDragSupport(this, CanStartDrag, DoStartDrag, DoEndDrag);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && !fDestroying)
        {
            if (EditorMode)
            {
                EditStop();
            }

            fDestroying = true;
            fFixedColumn = null;
            SysUtils.FreeAndNil(ref fHandleFollower);
            FColumns?.Clear();
            FColumns = null;
            SysUtils.FreeAndNil(ref fBoldProperties);
            SysUtils.FreeAndNil(ref fBoldColumnsProperties);
            fTitleFont = null;
            SysUtils.FreeAndNil(ref fFirstColumnRenderer);
            SysUtils.FreeAndNil(ref fDefaultTitlePopup);
        }

        base.Dispose(disposing);
    }

    internal TBoldControllerList ColumnsProperties => fBoldColumnsProperties!;

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

    // ---- Delphi's TCustomGrid on the DataGridView ----

    /// <summary>The grid's columns, the fixed one included (Delphi's ColCount); setting it adds or removes columns at the end.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ColCount
    {
        get => base.ColumnCount;
        set => SetColCount(value);
    }

    private void SetColCount(int Value)
    {
        bool wasSyncing = fSyncingColumns;
        fSyncingColumns = true;
        try
        {
            while (base.Columns.Count < Value)
            {
                base.Columns.Add(new TBoldGridViewColumn { Width = fDefaultColWidth });
            }

            while (base.Columns.Count > Value)
            {
                base.Columns.RemoveAt(base.Columns.Count - 1);
            }

            // As Delphi's grid: there is always a column more than the fixed ones.
            if (Value <= fFixedCols)
            {
                fFixedCols = Math.Max(0, Value - 1);
            }

            ApplyFixedCols();
        }
        finally
        {
            fSyncingColumns = wasSyncing;
        }
    }

    /// <summary>The grid's rows, the title row included (Delphi's RowCount).</summary>
    internal int GridRowCount
    {
        get => base.RowCount + FixedRows;
        set => base.RowCount = Math.Max(0, value - FixedRows);
    }

    /// <summary>The fixed columns at the left (0 or 1): they do not scroll, and their cells are not edited.</summary>
    [DefaultValue(1)]
    public int FixedCols
    {
        get => fFixedCols;
        set
        {
            fFixedCols = Math.Max(0, value);
            ApplyFixedCols();
        }
    }

    private void ApplyFixedCols()
    {
        for (int i = 0; i < base.Columns.Count; i++)
        {
            DataGridViewColumn column = base.Columns[i];
            bool isFixed = i < fFixedCols;
            column.Frozen = isFixed;
            column.ReadOnly = isFixed;
            column.Resizable = isFixed ? DataGridViewTriState.False : DataGridViewTriState.NotSet;
        }
    }

    /// <summary>The fixed rows at the top: 1 when the title row shows.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FixedRows
    {
        get => ColumnHeadersVisible ? 1 : 0;
        set => ColumnHeadersVisible = value > 0;
    }

    /// <summary>The color of the fixed cells (the first column and the titles).</summary>
    public Color FixedColor
    {
        get => fFixedColor;
        set
        {
            fFixedColor = value;
            Invalidate();
        }
    }

    /// <summary>The background of the cells.</summary>
    public Color Color
    {
        get => DefaultCellStyle.BackColor;
        set => DefaultCellStyle.BackColor = value;
    }

    /// <summary>The width of a new column.</summary>
    [DefaultValue(DefaultColWidthValue)]
    public int DefaultColWidth
    {
        get => fDefaultColWidth;
        set => fDefaultColWidth = value;
    }

    internal int GetColWidth(int Col) => Col >= 0 && Col < base.Columns.Count ? base.Columns[Col].Width : fDefaultColWidth;

    internal void SetColWidth(int Col, int Width)
    {
        if (Col >= 0 && Col < base.Columns.Count)
        {
            base.Columns[Col].Width = Math.Max(Width, base.Columns[Col].MinimumWidth);
        }
    }

    /// <summary>The current column.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Col
    {
        get => CurrentCell?.ColumnIndex ?? fCol;
        set
        {
            fCol = value;
            if (CurrentCell is { } cell && cell.ColumnIndex != value && value >= 0 && value < base.ColumnCount)
            {
                CurrentCell = this[value, cell.RowIndex];
            }
        }
    }

    /// <summary>The current row, counting the title row (Delphi's Row).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Row
    {
        get => CurrentCell is { } cell ? cell.RowIndex + FixedRows : FixedRows;
        set
        {
            int r = value - FixedRows;
            if (r >= 0 && r < base.RowCount && base.ColumnCount > 0 && (CurrentCell is null || CurrentCell.RowIndex != r))
            {
                CurrentCell = this[Math.Clamp(Col, 0, base.ColumnCount - 1), r];
            }
        }
    }

    /// <summary>The first row shown below the title row (Delphi's TopRow).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TopRow => Math.Max(0, FirstDisplayedScrollingRowIndex) + FixedRows;

    /// <summary>The first column shown right of the fixed ones (Delphi's LeftCol).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LeftCol
    {
        get => FirstDisplayedScrollingColumnIndex < 0 ? FixedCols : FirstDisplayedScrollingColumnIndex;
        set
        {
            if (value >= FixedCols && value < base.ColumnCount && !base.Columns[value].Frozen && FirstDisplayedScrollingColumnIndex != value)
            {
                FirstDisplayedScrollingColumnIndex = value;
            }
        }
    }

    /// <summary>The rows shown whole below the title row.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int VisibleRowCount => DisplayedRowCount(false);

    /// <summary>The cell an editor shows on (Delphi's EditorMode).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EditorMode
    {
        get => IsCurrentCellInEditMode;
        set
        {
            if (value)
            {
                ShowEditor();
            }
            else
            {
                HideEditor();
            }
        }
    }

    /// <summary>Starts editing the current cell (when it may be edited).</summary>
    public void ShowEditor()
    {
        if (!IsCurrentCellInEditMode && CurrentCell is not null)
        {
            BeginEdit(false);
        }
    }

    /// <summary>Ends editing the current cell.</summary>
    public void HideEditor()
    {
        if (IsCurrentCellInEditMode)
        {
            EndEdit();
        }
    }

    /// <summary>The editor of the grid's cells, once a cell was edited.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldInplaceEdit? InplaceEditor => EditingControl as TBoldInplaceEdit ?? fInplaceEditor;

    /// <summary>The cell (column, grid row) at the point, or (-1, -1) (Delphi's MouseCoord).</summary>
    public Point MouseCoord(int X, int Y)
    {
        HitTestInfo hit = HitTest(X, Y);
        return hit.Type switch
        {
            DataGridViewHitTestType.Cell => new Point(hit.ColumnIndex, hit.RowIndex + FixedRows),
            DataGridViewHitTestType.ColumnHeader => new Point(hit.ColumnIndex, TitleRow),
            _ => new Point(-1, -1),
        };
    }

    /// <summary>The rectangle of the cell (column, grid row) in the grid.</summary>
    public Rectangle CellRect(int ACol, int ARow)
    {
        if (ACol < 0 || ACol >= base.ColumnCount)
        {
            return Rectangle.Empty;
        }

        int r = ARow - FixedRows;
        return r >= -1 && r < base.RowCount ? GetCellDisplayRectangle(ACol, r, false) : Rectangle.Empty;
    }

    /// <summary>Draws the cell (column, grid row) again.</summary>
    internal void InvalidateGridCell(int ACol, int ARow)
    {
        if (ACol < 0 || ACol >= base.ColumnCount)
        {
            return;
        }

        int r = ARow < FixedRows ? -1 : ARow - FixedRows;
        if (r < base.RowCount)
        {
            InvalidateCell(ACol, r);
        }
    }

    /// <summary>Draws the grid row again.</summary>
    internal void InvalidateGridRow(int ARow)
    {
        int r = ARow - FixedRows;
        if (r >= 0 && r < base.RowCount)
        {
            InvalidateRow(r);
        }
    }

    /// <summary>The grid's font, as a Delphi font (with the text color).</summary>
    internal TFont GridFont() => BoldWinForms.ToTFont(Font, ForeColor);

    /// <summary>The canvas the grid draws on while a cell paints.</summary>
    protected TCanvas? Canvas => fCanvas;

    // ---- BoldGrid ----

    /// <summary>Makes column 0 the fixed column (adding columns as needed: at least one more than the fixed one).</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi waited while the form was read; the designer's code has run before the handle is made.")]
    private void EnsureOneFixedCol()
    {
        bool FirstIsOK() =>
            Columns.Count > 0 &&
            (Columns[0].BoldProperties.Renderer is null || Columns[0].BoldProperties.Renderer == fFirstColumnRenderer) &&
            (Columns[0].BoldProperties.Expression == "" || Columns[0].BoldProperties.Expression == "''");

        if (FixedCols == 0)
        {
            fFixedColumn = null;
        }

        if (fFixedColumn is null && !fIsEnsuringFixedCol)
        {
            fIsEnsuringFixedCol = true;
            // there must be atleast one column more than the fixed column
            while (Columns.Count < 2)
            {
                if (!FirstIsOK())
                {
                    Columns.Insert(0);
                }
                else
                {
                    Columns.Add();
                }
            }

            // see if the existing first column can be used as our fixed column...
            if (!FirstIsOK())
            {
                Columns.Insert(0);
            }

            fFixedColumn = Columns[0];
            // make this column the fixed column.
            fFixedColumn.BoldProperties.Expression = "";
            fFixedColumn.Title.Caption = ""; // Clear the title
            fFixedColumn.Color = FixedColor;
            if (!DesignMode)
            {
                fFixedColumn.BoldProperties.Renderer = fFirstColumnRenderer;
            }

            FixedCols = 1;
            SetColWidth(0, 17);

            fIsEnsuringFixedCol = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldHandle() => fHandleFollower?.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldList? GetBoldList() => BoldHandle?.List;

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetCurrentBoldElement() => CurrentCellFollower?.Element;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TGridOptions GetOptions() => fRangeSelect ? fOptions.Include(goRangeSelect) : fOptions;

    [Delphi(PortStatus.Ported)]
    private bool GetSelected(int DataRow) =>
        DataRow >= 0 && DataRow < Follower.SubFollowerCount && (RowFollowers(DataRow)?.Selected ?? false);

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private void SetColumns(TBoldGridColumns Value) => Columns.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private void SetController(TBoldListAsFollowerListController Value) => fBoldProperties!.Assign(Value);

    /// <summary>The options; a range selection is the grid's own (Bold's selected rows), not the DataGridView's.</summary>
    [Delphi(PortStatus.Ported, Note = "The other options set the DataGridView's: lines, column moving and sizing, row selection, editing and tabbing.")]
    private void SetOptions(TGridOptions val)
    {
        fRangeSelect = val.Contains(goRangeSelect);
        fOptions = val.Exclude(goRangeSelect);
        AllowUserToOrderColumns = fOptions.Contains(goColMoving);
        AllowUserToResizeColumns = fOptions.Contains(goColSizing);
        AllowUserToResizeRows = fOptions.Contains(goRowSizing);
        SelectionMode = fOptions.Contains(goRowSelect) ? DataGridViewSelectionMode.FullRowSelect : DataGridViewSelectionMode.CellSelect;
        EditMode = fOptions.Contains(goAlwaysShowEditor) ? DataGridViewEditMode.EditOnEnter : DataGridViewEditMode.EditOnKeystrokeOrF2;
        StandardTab = !fOptions.Contains(goTabs);
        bool vert = fOptions.Contains(goVertLine), horz = fOptions.Contains(goHorzLine);
        CellBorderStyle = vert && horz ? DataGridViewCellBorderStyle.Single
            : vert ? DataGridViewCellBorderStyle.SingleVertical
            : horz ? DataGridViewCellBorderStyle.SingleHorizontal
            : DataGridViewCellBorderStyle.None;
    }

    /// <summary>
    /// Selects the row as the keys say: alone (unless already selected and not forced), a range from the last selected
    /// (Shift), or toggled (Ctrl, unless toggles are ignored).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal void SetSelection(int aRow, Keys Shift, bool ForceClearOfOtherRows, bool IgnoreToggles)
    {
        if (aRow == -1)
        {
            return;
        }

        bool shift = (Shift & Keys.Shift) != 0, ctrl = (Shift & Keys.Control) != 0;
        fIsMultiSelecting = MultiSelect && (shift || ctrl);

        // Clear previous selection, Select one item
        if (!(shift || ctrl) || !MultiSelect)
        {
            if (!(Follower.SubFollowers(aRow)?.Selected ?? false) || ForceClearOfOtherRows)
            {
                BoldProperties.SelectAll(Follower, false);
                BoldProperties.SetSelected(Follower, aRow, true);
            }
        }

        // Select range from first selected item
        if (shift && MultiSelect)
        {
            BoldProperties.SelectRange(Follower, aRow);
        }

        // Toggle selection on current item
        if (ctrl && MultiSelect && !IgnoreToggles)
        {
            BoldProperties.ToggleSelected(Follower, aRow);
        }

        // At this point we would rather have invalidated col 0,
        // but that does not yield desired redraw WHEN THE GRID SCROLLS.
        Invalidate();
        AdjustActiveRange();
    }

    /// <summary>The list's element type changed: new default columns (with BoldAutoColumns), and check boxes where they fit.</summary>
    [Delphi(PortStatus.Ported)]
    private void TypeMayHaveChanged()
    {
        if (BoldEnvironment.BoldEffectiveEnvironment().RunningInIDE && (BoldHandle?.List is not { Count: > 0 }))
        {
            return; // only update at runtime if there are values, avoids update on every UML model change.
        }

        TBoldElementTypeInfo? NewListElementType = GetHandleListElementType();
        if (NewListElementType != fCurrentListElementType)
        {
            fCurrentListElementType = NewListElementType;
            if (BoldAutoColumns)
            {
                CreateDefaultColumns();
            }
        }

        for (int i = 0; i < Columns.Count; i++)
        {
            Columns[i].SetCheckBoxRendererIfAppropriate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TFont CellFont(TBoldGridColumn Column) => Column.Font ?? GridFont();

    /// <summary>The cell's string (the row is made active first); empty beyond the list.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetString(int GridCol, int DataRow)
    {
        if (BoldHandle is null || DataRow >= BoldHandle.Count)
        {
            return "";
        }

        EnsureRowActive(DataRow);
        // if a cell's controller has not be created yet then you'll get an AV (ex: setting the CWAdjust flag)
        TBoldFollower? cell = CellFollowers(GridCol, DataRow);
        return cell is { Controller: TBoldStringFollowerController controller } ? controller.GetCurrentAsString(cell) : "";
    }

    /// <summary>A cell is highlighted when it is the selected cell of a selected row (all selected rows while selecting several).</summary>
    [Delphi(PortStatus.Ported, Note = "The grid has the focus when it or its editor has.")]
    private bool HighlightCell(DataGridViewElementStates AState, int aRow)
    {
        bool RowSelected = Follower.SubFollowers(DataRow(aRow))?.Selected ?? false;
        if (fIsMultiSelecting)
        {
            return RowSelected;
        }

        return (AState & DataGridViewElementStates.Selected) != 0 && RowSelected && (AlwaysShowSelected || ContainsFocus);
    }

    /// <summary>The cell's follower is up to date: the editor shows its string (when on it), and the cell draws again.</summary>
    [Delphi(PortStatus.Ported)]
    internal void _AfterMakeCellUptoDate(TBoldFollower Follower)
    {
        int DisplayGridRow = GridRow(Follower.OwningFollower!.index);
        int DisplayCol = Follower.index;
        if (EditorMode && InplaceEditor is { } editor && Row == DisplayGridRow && Col == DisplayCol)
        {
            editor.Text = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
        }

        if (DisplayGridRow >= TopRow && DisplayGridRow <= TopRow + VisibleRowCount + 1)
        {
            InvalidateGridCell(DisplayCol, DisplayGridRow);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _DeleteRow(int index, TBoldFollower owningFollower)
    {
        if (index < fInvalidateFrom)
        {
            fInvalidateFrom = index;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _InsertRow(TBoldFollower Follower)
    {
        if (Follower.index < fInvalidateFrom)
        {
            fInvalidateFrom = Follower.index;
        }

        fLastInsertedRowIndex = Follower.index;
    }

    /// <summary>Fits the column's width to its shown strings (and title), as its CWAdjust allows.</summary>
    [Delphi(PortStatus.Ported)]
    internal void AdjustCol(int Col)
    {
        if (Col < 0 || Col >= Columns.Count || Columns[Col].CWAdjust.IsEmpty || !IsHandleCreated)
        {
            return;
        }

        using Graphics graphics = CreateGraphics();
        var Canvas = new TBoldGraphicsCanvas(graphics);
        Canvas.Font.Assign(GridFont());
        int TextW = 0;
        if (!HasGhostRow)
        {
            for (int I = TopRow; I <= TopRow + Math.Min(VisibleRowCount, Follower.SubFollowerCount) - 1; I++)
            {
                TextW = Math.Max(TextW, Canvas.TextWidth(GetString(Col, DataRow(I))) + 4);
            }
        }

        if (Columns[Col].CWAdjust.Contains(caIncludeTitle))
        {
            TextW = Math.Max(TextW, Canvas.TextWidth(Columns[Col].Title.Caption) + 4);
        }

        if (Columns[Col].CWAdjust.Contains(caAllowGrow) && TextW > GetColWidth(Col))
        {
            SetColWidth(Col, TextW);
        }

        if (Columns[Col].CWAdjust.Contains(caAllowShrink) && TextW < GetColWidth(Col))
        {
            SetColWidth(Col, TextW);
        }
    }

    /// <summary>The title menu: a check item per width adjustment, a separator and a close item.</summary>
    [Delphi(PortStatus.Ported, Note = "The grid has its own menu, where Delphi shared one between the grids.")]
    private ContextMenuStrip? DefaultTitlePopup(int Col)
    {
        if (!Columns[Col].DefaultTitlePopupMenu)
        {
            return null;
        }

        if (fDefaultTitlePopup is null)
        {
            fDefaultTitlePopup = new ContextMenuStrip();
            foreach (TBoldCWAdjust adjust in Enum.GetValues<TBoldCWAdjust>())
            {
                var M = new ToolStripMenuItem(adjust.ToString())
                {
                    Name = "__mnuBoldGridCWA" + adjust, // do not translate
                    Tag = (int)adjust,
                };
                M.Click += DefaultTitlePopupOnClick;
                fDefaultTitlePopup.Items.Add(M);
            }

            fDefaultTitlePopup.Items.Add(new ToolStripSeparator { Name = "__mnuBoldGridSeparator" }); // do not translate
            fDefaultTitlePopup.Items.Add(new ToolStripMenuItem(sClosePopup) { Name = "__mnuBoldGridCancel" }); // do not translate
        }

        ContextMenuStrip Result = fDefaultTitlePopup;
        Result.Tag = Col;
        for (int I = 0; I < Result.Items.Count; I++)
        {
            if (Result.Items[I] is ToolStripMenuItem item && item.Tag is int adjust)
            {
                item.Checked = Columns[Col].CWAdjust.Contains((TBoldCWAdjust)adjust);
            }
        }

        return Result;
    }

    /// <summary>A click on an adjustment of the title menu turns it on or off for the menu's column.</summary>
    [Delphi(PortStatus.Ported)]
    private void DefaultTitlePopupOnClick(object? Sender, EventArgs e)
    {
        if (Sender is ToolStripMenuItem item)
        {
            item.Checked = !item.Checked;
            if (item.Name is { } name && name.StartsWith("__mnuBoldGridCWA", StringComparison.Ordinal) && // do not translate
                item.Owner?.Tag is int col && col < Columns.Count && item.Tag is int adjust)
            {
                TBoldGridColumn column = Columns[col];
                column.CWAdjust = item.Checked
                    ? column.CWAdjust.Include((TBoldCWAdjust)adjust)
                    : column.CWAdjust.Exclude((TBoldCWAdjust)adjust);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFollower? GetCellFollower(int ListCol, int DataRow)
    {
        TBoldFollower? RowFollower = GetRowFollower(DataRow);
        return RowFollower is not null && ListCol >= 0 && ListCol < RowFollower.SubFollowerCount ? RowFollower.SubFollowers(ListCol) : null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFollower? GetCurrentCellFollower() => Col < FixedCols ? null : CellFollowers(Col, DataRow(Row));

    [Delphi(PortStatus.Ported)]
    private bool GetMultiSelect() => Options.Contains(goRangeSelect);

    [Delphi(PortStatus.Ported)]
    private TBoldFollower? GetRowFollower(int DataRow) =>
        fHandleFollower is not null && DataRow < Follower.SubFollowerCount ? Follower.SubFollowers(DataRow) : null;

    [Delphi(PortStatus.Ported)]
    private void InvalidateFromRow(int DisplayDataRow)
    {
        for (int I = GridRow(DisplayDataRow); I <= Math.Min(GridRowCount - 1, TopRow + VisibleRowCount + 1); I++)
        {
            InvalidateGridRow(I);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetCurrentRow(int DataRow)
    {
        Row = GridRow(Math.Max(0, DataRow)); // Need to make sure we never focus on row 0
        ReallyInvalidateCol(0); // Need to redraw first column FIXME let renderer to this too
    }

    [Delphi(PortStatus.Ported)]
    private void SetMultiSelect(bool V) => Options = V ? Options.Include(goRangeSelect) : Options.Exclude(goRangeSelect);

    /// <summary>Shows the title's menu (or the grid's title menu) at the point.</summary>
    [Delphi(PortStatus.Ported)]
    private void TitleMenuPopup(Point GridCoord, int X, int Y)
    {
        ContextMenuStrip? ThePopupMenu = Columns[GridCoord.X].Title.PopupMenu ?? DefaultTitlePopup(GridCoord.X);
        ThePopupMenu?.Show(this, new Point(X, Y));
    }

    [Delphi(PortStatus.Ported)]
    private void SetTitleFont(TFont Value) => fTitleFont!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private bool GetBoldHandleIndexLock() => fHandleFollower!.HandleIndexLock;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandleIndexLock(bool Value) => fHandleFollower!.HandleIndexLock = Value;

    /// <summary>Checks the grid's context and each column's expression in it.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix)
    {
        TBoldElementTypeInfo? Context = GetHandleStaticType();
        bool result = ComponentValidator.ValidateExpressionInContext("", Context, SysUtils.Format("%s%s", NamePrefix, Name)); // do not localize
        if (Context is not null)
        {
            for (int i = 0; i < Columns.Count; i++)
            {
                result = ComponentValidator.ValidateExpressionInContext(
                    Columns[i].BoldProperties.Expression,
                    Context,
                    SysUtils.Format("%s%s.Column[%d]", NamePrefix, Name, i)) && result; // do not localize
            }
        }

        return result;
    }

    bool IBoldValidateableComponent.ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix) =>
        ValidateComponent(ComponentValidator, NamePrefix);

    /// <summary>True when the column shows check boxes.</summary>
    [Delphi(PortStatus.Ported)]
    internal bool ColumnIsCheckBox(int col) => FColumns is not null && col >= 0 && col < Columns.Count && Columns[col].fCheckBoxPainterRenderer is not null;

    [Delphi(PortStatus.Ported)]
    private TBoldList? GetMutableList() => BoldHandle?.MutableList;

    [Delphi(PortStatus.Ported)]
    private bool GetShowTitleRow() => FixedRows == 1;

    [Delphi(PortStatus.Ported)]
    private void SetShowTitleRow(bool Value) => FixedRows = Value ? 1 : 0;

    [Delphi(PortStatus.Ported)]
    private int GetTitleRow() => ShowTitleRow ? 0 : -1;

    /// <summary>The rows to keep active: those shown and one more each way, widened to the selected rows.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetActiveRange(out int FirstActive, out int LastActive)
    {
        FirstActive = DataRow(TopRow) - 1;
        LastActive = DataRow(TopRow + VisibleRowCount) + 1;
        // extend range to include all selected elements
        for (int i = 0; i <= FirstActive - 1; i++)
        {
            if (Selected(i))
            {
                FirstActive = i;
                break;
            }
        }

        for (int i = DataRow(GridRowCount - 1); i >= LastActive + 1; i--)
        {
            if (Selected(i))
            {
                LastActive = i;
                break;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private new bool MultiSelect
    {
        get => GetMultiSelect();
        set => SetMultiSelect(value);
    }

    /// <summary>Makes the row active (widening the active range to it) and displays it.</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureRowActive(int DataRow)
    {
        if (fHandleFollower is not null && DataRow >= 0 && BoldHandle is not null && DataRow < BoldHandle.Count)
        {
            if (RowFollowers(DataRow) is not { Active: true })
            {
                GetActiveRange(out int FirstActive, out int LastActive);
                if (DataRow < FirstActive)
                {
                    FirstActive = DataRow;
                }
                else if (DataRow > LastActive)
                {
                    LastActive = DataRow;
                }

                BoldProperties.SetActiveRange(Follower, FirstActive, LastActive, 10);
                // this can fail if the FollowerHandle is invalid...
                Follower.EnsureDisplayable();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DisplayAvailableFollowers() => Follower.EnsureDisplayable();

    [Delphi(PortStatus.Ported)]
    private string GetCellText(int col, int row) => row < FixedRows ? Columns[col].Title.Caption : GetString(col, DataRow(row));

    /// <summary>The title font changed: the titles draw again.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi stopped taking the parent's font; a WinForms control's font is its own once set. The titles draw again.")]
    private void _FontChanged(object? Sender) => Invalidate();

    /// <summary>
    /// The list is up to date: a row per element (or one empty row), the new row selected (AutoSelectNewRows), the editor
    /// hidden when its row went, the current row shown and selected.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void _AfterMakeListUptoDate(TBoldFollower Follower)
    {
        if (fInvalidateFrom <= DataRow(Row))
        {
            EditorMode = false;
        }

        fHasGhostRow = Follower.SubFollowerCount == 0;
        GridRowCount = HasGhostRow ? FixedRows + 1 : Follower.SubFollowerCount + FixedRows;

        // if exactly one row has been inserted, then select it.
        if (AutoSelectNewRows && fSubFollowerCountBeforeMakeUpToDate == Follower.SubFollowerCount - 1 && BoldHandle is not null)
        {
            // perhaps this is a bit overkill, but it works. anyone with a better suggestion?
            BoldHandle.CurrentIndex = fLastInsertedRowIndex;
            Follower.CurrentIndex = fLastInsertedRowIndex;
            SetCurrentRow(Follower.CurrentIndex);
        }

        if (fEditedElementBeforeMakeUpToDate is not null &&
            (Follower.CurrentIndex == -1 || RowFollowers(Follower.CurrentIndex) is not { } current ||
             fEditedElementBeforeMakeUpToDate != current.Element))
        {
            EditorMode = false;
        }

        if (fSubFollowerCountBeforeMakeUpToDate == 0 && Follower.SubFollowerCount > 0 && Follower.SubFollowers(Follower.CurrentIndex) is { } first)
        {
            first.Selected = true;
        }

        AdjustActiveRange();
        if (fInvalidateFrom != int.MaxValue)
        {
            InvalidateFromRow(fInvalidateFrom);
        }

        // setting the currentRow will reset the LeftCol
        if (GridRow(Follower.CurrentIndex) < GridRowCount)
        {
            int OldLeftCol = LeftCol;
            SetCurrentRow(Follower.CurrentIndex);
            LeftCol = OldLeftCol;
        }

        if (!fIsMultiSelecting && Follower.SubFollowers(Follower.CurrentIndex) is { Selected: false })
        {
            BoldProperties.SelectAll(Follower, false);
            BoldProperties.SetSelected(Follower, Follower.CurrentIndex, true);
        }

        fMakingListUpToDate = false;
    }

    /// <summary>The list is about to change: the type is checked, and what the grid needs afterwards is noted.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void _BeforeMakeListUpToDate(TBoldFollower Follower)
    {
        TypeMayHaveChanged(); // IMPROVEME, subscribe to listidentitychanged instead.
        fMakingListUpToDate = true;
        AdjustActiveRange();
        fInvalidateFrom = int.MaxValue;
        fSubFollowerCountBeforeMakeUpToDate = Follower.SubFollowerCount;
        fEditedElementBeforeMakeUpToDate = EditorMode ? RowFollowers(DataRow(Row))?.Element : null;
    }

    /// <summary>Fits the column to its contents and title once (whatever its CWAdjust).</summary>
    [Delphi(PortStatus.Ported)]
    protected void AutoAdjustCol(int Col)
    {
        if (Col > 0 && Col < Columns.Count)
        {
            TBoldCWAdjustSet CWA = Columns[Col].CWAdjust;
            Columns[Col].CWAdjust = TBoldCWAdjustSet.Of(caAllowGrow, caAllowShrink, caIncludeTitle);
            Columns[Col].CWAdjust = CWA;
        }
    }

    /// <summary>The editor takes the key when the cell's controller accepts the character.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual bool CanEditAcceptKey(char KEY) =>
        CurrentCellFollower is { } cell && ((TBoldStringFollowerController)cell.Controller!).ValidateCharacter(KEY, cell);

    /// <summary>The current cell's value may be changed (its controller allows it, and the column is not read only).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool CanEditModify() =>
        CurrentCellFollower is { } cell && cell.AssertedController.MayModify(cell) && !Columns[Col].ColReadOnly;

    /// <summary>Delphi's inherited CanEditShow: the grid edits, does not select rows, and the cell is no fixed one.</summary>
    private bool InheritedCanEditShow() =>
        fOptions.Contains(goEditing) && !fOptions.Contains(goRowSelect) && !DesignMode && Col >= FixedCols && Row >= FixedRows;

    /// <summary>The current cell may show an editor: it can be changed, or its column has a lookup handle.</summary>
    [Delphi(PortStatus.Ported, Note = "Asked when a cell would start editing (OnCellBeginEdit).")]
    protected virtual bool CanEditShow()
    {
        bool Result = InheritedCanEditShow() && CurrentCellFollower is not null && !Columns[Col].ColReadOnly;
        // editable if we have a write-allowing renderer or a lookup-handle
        Result = Result && (CurrentCellFollower!.RendererData.MayModify || Columns[Col].LookupHandle is not null);
        LookUpEditorActive = Result && Columns[Col].LookupHandle is not null;
        return Result;
    }

    [Delphi(PortStatus.Ported)]
    protected bool CanEditShowForCustomEditors() => InheritedCanEditShow() && CurrentCellFollower is not null;

    /// <summary>The handle's current element follows the current row.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.Click", Note = "OnCurrentCellChanged: Delphi's grid clicked whenever the current cell moved. Not while the grid follows the list.")]
    protected override void OnCurrentCellChanged(EventArgs e)
    {
        if (fHandleFollower is not null && !fMakingListUpToDate && CurrentCell is not null)
        {
            fCol = CurrentCell.ColumnIndex;
            fHandleFollower.SetFollowerIndex(DataRow(Row));
        }

        base.OnCurrentCellChanged(e);
    }

    /// <summary>A column was moved: the column (and its controller) moves, and the grid draws again.</summary>
    [Delphi(PortStatus.Ported, Note = "Called when the user dragged a column to a new place, or by MoveColumn.")]
    protected virtual void ColumnMoved(int FromIndex, int ToIndex)
    {
        Columns.MoveColumn(FromIndex, ToIndex);
        // Redraw affected columns
        for (int Col = Math.Min(FromIndex, ToIndex); Col <= Math.Max(FromIndex, ToIndex); Col++)
        {
            Columns.UpdateColumn(Columns[Col]);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.ColWidthsChanged", Note = "OnColumnWidthChanged.")]
    protected override void OnColumnWidthChanged(DataGridViewColumnEventArgs e)
    {
        base.OnColumnWidthChanged(e);
        // By including TopLeftChanged, [caAllowGrow, caAllowShrink]
        // effectively freezes the column width
        //  TopLeftChanged;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldGridColumns CreateColumns() => new(this, ColumnClass());

    /// <summary>
    /// Columns for the list's element type: for a class, its default string and each attribute; for an attribute or a
    /// list type, one column; otherwise the type and the string of each element.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void CreateDefaultColumns()
    {
        TBoldElementTypeInfo? ListElementType = GetHandleListElementType();
        DeleteAllColumns();
        bool UsedFirstCol = false;
        EnsureConstraintColumn();

        TBoldGridColumn GetEmptyCol()
        {
            TBoldGridColumn result = UsedFirstCol ? AddColumn() : Columns[ColCount - 1];
            UsedFirstCol = true;
            return result;
        }

        if (ListElementType is TBoldClassTypeInfo ClassTypeInfo)
        {
            if (ClassTypeInfo.DefaultStringRepresentation != "")
            {
                TBoldGridColumn Column = GetEmptyCol();
                Column.BoldProperties.Expression = "";
                Column.Title.Caption = ClassTypeInfo.ModelName;
            }

            for (int i = 0; i < ClassTypeInfo.AllMembers.Count; i++)
            {
                if (ClassTypeInfo.AllMembers[i].IsAttribute)
                {
                    TBoldGridColumn Column = GetEmptyCol();
                    Column.BoldProperties.Expression = ClassTypeInfo.AllMembers[i].ExpressionName;
                    Column.Title.Caption = ClassTypeInfo.AllMembers[i].ModelName;
                }
            }
        }
        else if (ListElementType is TBoldAttributeTypeInfo AttributeTypeInfo)
        {
            GetEmptyCol().Title.Caption = AttributeTypeInfo.ModelName;
        }
        else if (ListElementType is TBoldListTypeInfo)
        {
            GetEmptyCol().Title.Caption = sCaptionClassName;
        }

        if (Columns.Count == 2)
        {
            AddColumn();
            Columns[Columns.Count - 1].Dispose();
        }

        EnsureOneFixedCol();
        if (Columns.Count == 1)
        {
            TBoldGridColumn TypeColumn = AddColumn();
            TypeColumn.BoldProperties.Expression = "self.oclType"; //do not localize
            TypeColumn.Title.Caption = sCaptionType;
            TBoldGridColumn StringColumn = AddColumn();
            StringColumn.BoldProperties.Expression = "";
            StringColumn.Title.Caption = sCaptionAsString;
        }

        Col = BoldShowConstraints ? 2 : 1;
    }

    /// <summary>The list row of a grid row (-1 for the empty row of an empty list).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal int DataRow(int GridRow) => HasGhostRow ? -1 : GridRow - FixedRows;

    /// <summary>The grid row of a list row.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal int GridRow(int Datarow) => Datarow + FixedRows;

    /// <summary>A double click on a data row opens a form for the current element (when the controller does that by default).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.DblClick", Note = "The DoubleClick event is raised first.")]
    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        if (fHandleFollower is not null && fLastMouseDownGridCoord.Y >= FixedRows && BoldProperties.DefaultDblClick && CurrentBoldElement is { } element)
        {
            BoldAFP.AutoFormProviderRegistry().FormForElement(element)?.Show();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void DefaultColumns() => CreateDefaultColumns();

    /// <summary>Leaves only the fixed column and one more.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DeleteAllColumns()
    {
        while (Columns.Count > 1)
        {
            Columns[Columns.Count - 1].Dispose();
        }

        if (Columns.Count == 0)
        {
            AddColumn();
        }

        // ensure column 0
        EnsureOneFixedCol();
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag()
    {
        BoldProperties.EndDrag();
        fIsDragging = false;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag()
    {
        fIsDragging = true;
        BoldProperties.StartDrag(Follower);
    }

    /// <summary>A drag starts from the first column of a data row, or from any data row with BoldDragAnywhere.</summary>
    private bool CanStartDrag() =>
        fHandleFollower is not null && BoldProperties.DragMode != TBoldDragMode.bdgNone && fLastMouseDownGridCoord.Y >= FixedRows &&
        (fLastMouseDownGridCoord.X == FixedCols - 1 || BoldDragAnywhere);

    /// <summary>Accepts what the controller accepts on the mutable list at the row under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (fHandleFollower is not null && TBoldDragSupport.IsBoldDrag(drgevent))
        {
            bool internalDrag = drgevent.Data?.GetData(typeof(TBoldDragObject)) is TBoldDragObject drag && drag.Source == this;
            Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
            bool accept = BoldProperties.DropMode != TBoldDropMode.bdpNone && (!internalDrag || BoldProperties.InternalDrag) &&
                BoldProperties.DragOver(Follower, MutableList, MouseCoord(p.X, p.Y).Y);
            TBoldDragSupport.Accept(drgevent, accept);
        }
    }

    /// <summary>Drops on the mutable list at the row under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (fHandleFollower is not null && TBoldDragSupport.IsBoldDrag(drgevent))
        {
            Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
            BoldProperties.DragDrop(Follower, MutableList, MouseCoord(p.X, p.Y).Y);
        }
    }

    /// <summary>
    /// Moving keys end the editing (Shift selects several); Delete clears the cell; Down on the last row adds an
    /// element (AddNewAtEnd); in a check box column the space key toggles the value.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.KeyDown")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (fHandleFollower is null)
        {
            base.OnKeyDown(e);
            return;
        }

        Keys KEY = e.KeyCode;
        bool RowMovementKey = KEY is Keys.Left or Keys.Right or Keys.Up or Keys.PageUp or Keys.Down or Keys.PageDown or Keys.Home or Keys.End or Keys.Tab;
        if (RowMovementKey)
        {
            HideEditor();
            if (e.Shift && MultiSelect && KEY != Keys.Tab)
            {
                fIsMultiSelecting = true;
            }
        }

        if (CanEditShow() && KEY == Keys.Delete && e.Modifiers == Keys.None)
        {
            ShowEditor();
            if (InplaceEditor is { } editor && EditorMode)
            {
                editor.Text = "";
            }

            SetEditText(Col, Row, "");
        }

        if (Row == GridRowCount - 1 && KEY == Keys.Down)
        {
            if (AddNewAtEnd && BoldProperties.NilElementMode != TBoldNilElementMode.neAddLast && BoldHandle?.List is { } list)
            {
                list.AddNew();
                Follower.EnsureDisplayable(); // Force control to get in sync with Object Layer
            }
            else
            {
                KEY = Keys.None; // Avoid walking below last row
            }
        }

        if (KEY == Keys.None)
        {
            e.Handled = true;
        }
        else if (!RowMovementKey && ColumnIsCheckBox(Col))
        {
            Columns[Col].fCheckBoxPainterRenderer!.KeyPress(ref KEY, e.Modifiers, this);
            e.Handled = true;
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    /// <summary>Esc throws away the current cell's change and ends the editing; a check box column takes no characters.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.KeyPress")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (ColumnIsCheckBox(Col))
        {
            e.Handled = true;
            return;
        }

        if (e.KeyChar == BoldDefs.BOLDESC)
        {
            CurrentCellFollower?.DiscardChange();
            HideEditor();
            e.KeyChar = BoldDefs.BOLDNULL;
            e.Handled = true;
        }

        base.OnKeyPress(e);
    }

    /// <summary>Esc while editing throws away the cell's change (the grid then ends the editing), as Delphi's editor passed Esc to the grid.</summary>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if ((keyData & Keys.KeyCode) == Keys.Escape && IsCurrentCellInEditMode && fHandleFollower is not null)
        {
            CurrentCellFollower?.DiscardChange();
        }

        return base.ProcessDialogKey(keyData);
    }

    /// <summary>The moving keys select the new current row (without Ctrl: the keyboard cannot select apart).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.KeyUp")]
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (fHandleFollower is not null && (int)e.KeyCode >= 33 && (int)e.KeyCode <= 40 && !EditorMode) //PGUP..DOWN
        {
            //FIXME: It *is* possible to make non-consecutive selections with keyboard.
            // I think the algorithm has to be rewritten to accommodate this.
            // Also: Check how Delphi/Windows implements keyboard selections (keys/combinations)
            SetSelection(DataRow(Row), e.Modifiers & ~Keys.Control, true, true); // Call setselection with currentrow and shiftstate
        }

        base.OnKeyUp(e);
    }

    /// <summary>The fixed column, the types' check boxes and the constraint column, once the designer's code has run.</summary>
    [Delphi(PortStatus.Ported, Note = "Runs once, when the grid's handle is made.")]
    protected virtual void Loaded()
    {
        EnsureOneFixedCol();
        if (!DesignMode)
        {
            TypeMayHaveChanged();
        }

        EnsureConstraintColumn();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!fLoaded && fHandleFollower is not null)
        {
            fLoaded = true;
            Loaded();
        }
    }

    /// <summary>
    /// A click on a check box toggles it; on the top left cell selects all rows; a right click on a title shows its menu;
    /// a click on a data row makes it current and selects it.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.MouseDown", Note = "A drag from the first column starts in TBoldDragSupport. A click on the first column of a selected row keeps the selection (Delphi passed the grid row as the list row).")]
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (fHandleFollower is null)
        {
            base.OnMouseDown(e);
            return;
        }

        Keys Shift = ModifierKeys;
        fLastMouseDownGridCoord = MouseCoord(e.X, e.Y);
        fLastMouseDownScreenCoord = e.Location;

        int x = fLastMouseDownGridCoord.X;
        if (Options.Contains(goEditing) && e.Button == MouseButtons.Left && x >= 0 && Columns.Count > x &&
            Columns[x].fCheckBoxPainterRenderer is { } CheckBoxRenderer)
        {
            if (fLastMouseDownGridCoord.Y >= FixedRows)
            {
                if (e.Clicks == 2)
                {
                    OnDoubleClick(EventArgs.Empty);
                }
                else if (!Columns[x].ColReadOnly)
                {
                    CheckBoxRenderer.CheckBoxClick(e.Button, Shift, e.X, e.Y, this, Columns[x].Alignment);
                }
            }

            return;
        }

        base.OnMouseDown(e);
        // Top Left cell marks all rows
        // FIXME FIXEDROW handling
        if (fLastMouseDownGridCoord.Y != -1 && fLastMouseDownGridCoord.Y == TitleRow)
        {
            if (fLastMouseDownGridCoord.X == FixedCols - 1)
            {
                BoldProperties.SelectAll(Follower, true);
                ReallyInvalidateCol(FixedCols - 1); // FIXME InvalidateCol doesn't invalidate last when scrolling
                AdjustActiveRange();
            }
            else if (e.Button == MouseButtons.Right && x >= 0)
            {
                TitleMenuPopup(fLastMouseDownGridCoord, e.X, e.Y);
                fLastMouseDownScreenCoord = new Point(-1, -1);
            }
        }
        else if (e.Button == MouseButtons.Left && fLastMouseDownGridCoord.Y >= FixedRows) //mark clicked row; if clicking outside datacells, y is -1
        {
            Row = fLastMouseDownGridCoord.Y;
            // if the click is in column 0 and it is already selected, don't reselect anything
            if (!(fLastMouseDownGridCoord.X == FixedCols - 1 && Selected(DataRow(fLastMouseDownGridCoord.Y))))
            {
                SetSelection(DataRow(Row), Shift, false, true); // SetSelection Invalidates entire grid
            }
        }

        fLastMouseDownShiftState = Shift;
    }

    /// <summary>A click without moving fits the column (EnableColAdjust); the left button selects the current row as the keys say.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.MouseUp")]
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (fHandleFollower is null)
        {
            return;
        }

        if (fEnableColAdjust && fLastMouseDownScreenCoord.X == e.X)
        {
            AutoAdjustCol(MouseCoord(e.X, e.Y).X);
        }

        if (e.Button == MouseButtons.Left)
        {
            if (!fIsDragging)
            {
                SetSelection(DataRow(Row), ModifierKeys, true, false); // Call setselection with currentrow and shiftstate
            }
            else if (!Selected(DataRow(Row)) && (fLastMouseDownShiftState & Keys.Control) != 0)
            {
                // starting a drag on a nonselected row with ctrl pressed should select the row
                SetSelection(DataRow(Row), fLastMouseDownShiftState, true, false);
            }
        }

        fLastMouseDownScreenCoord = new Point(-1, -1);
    }

    /// <summary>The grid scrolled: the active rows follow, the columns fit, then OnTopLeftChanged.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.TopLeftChanged", Note = "OnScroll; the Scroll event is raised first.")]
    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);
        if (fHandleFollower is null)
        {
            return;
        }

        AdjustActiveRange();
        for (int Col = FixedCols; Col < Columns.Count; Col++)
        {
            AdjustCol(Col);
        }

        fOnTopLeftChanged?.Invoke(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomGrid.Resize")]
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (fHandleFollower is not null && !fMakingListUpToDate)
        {
            AdjustActiveRange();
        }
    }

    /// <summary>Adds the constraint column (second) when the grid shows constraints.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureConstraintColumn()
    {
        if (BoldShowConstraints && !DesignMode)
        {
            TBoldGridColumn column = AddColumn();
            column.Index = 1;
            column.BoldProperties.Expression = "constraints->exists(c|not c)"; // do not localize
            column.Width = (BoldCommonBitmaps.bmpBoldGridSelected?.Width ?? 12) + 3;
            column.Title.Caption = "§";
            column.BoldProperties.Renderer = new TBoldConstraintRenderer();
            column.ColReadOnly = true;
        }
    }

    /// <summary>The editing of a cell ended: the cell's change is applied when its controller applies on exit.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void EditStop()
    {
        TBoldFollower? CellFollower = CurrentCellFollower;
        // if the grid is changed under our feet (for example because it is sorted, and we just changed the sort order)
        // then ignore the edit stop
        if (CellFollower?.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
        {
            CellFollower.Apply();
        }
    }

    /// <summary>The longest text the editor takes: a string attribute's length, or no limit (0).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual int GetEditLimit()
    {
        int result = 0;
        // set the maxlength of the editor;
        TBoldElement? El = InplaceEditor?.GetDestElement(CurrentCellFollower, Columns[Col]);
        if (El is TBAString s && s.BoldAttributeRTInfo is { } info)
        {
            result = info.Length;
        }

        return Math.Max(result, 0);
    }

    [Delphi(PortStatus.Ported, Note = "A Type: TBoldGridColumn or a descendant.")]
    protected virtual Type ColumnClass() => typeof(TBoldGridColumn);

    /// <summary>Asked before a cell becomes the current one; OnSelectCell may refuse.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool SelectCell(int ACol, int ARow)
    {
        bool Result = true;
        FOnSelectCell?.Invoke(this, ACol, ARow, ref Result);
        return Result;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldInplaceEdit? GetEditor() => InplaceEditor;

    /// <summary>Makes the cell followers of the top row ensure their expressions for all active rows at once.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureActiveCellFollowerExpressions()
    {
        if (Follower.SubFollowerCount > 0)
        {
            for (int i = FixedCols; i < ColCount && i < Columns.Count; i++)
            {
                if (Columns[i].BoldProperties.SupportsMultiEnsure && CellFollowers(i, TopRow) is { } cell)
                {
                    cell.EnsureMulti();
                }
            }
        }
    }

    /// <summary>The lookup combo box's choice goes to the cell's element (or to the column's OnLookupChange).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void LookUpChange(object? sender, TBoldElement? DestElement, TBoldGridColumn? EditColumn)
    {
        int aIndex = -1;
        if (LookUpEditorActive && GetEditor() is { } editor)
        {
            aIndex = editor.Combo.SelectedIndex;
        }

        LookUpEditorActive = true;
        if (aIndex > -1 && DestElement is not null && GetEditor()?.Combo.Items[aIndex] is TBoldInplaceEdit.TBoldLookupItem item)
        {
            if (EditColumn?.OnLookupChange is { } change)
            {
                change(DestElement, item.Element);
            }
            else
            {
                DestElement.Assign(item.Element);
            }
        }
    }

    /// <summary>Adds a column at the end.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldGridColumn AddColumn()
    {
        Columns.BeginUpdate();
        try
        {
            return Columns.Add();
        }
        finally
        {
            Columns.EndUpdate();
        }
    }

    /// <summary>Makes the rows shown (and the selected ones) active, and their cells' expressions ensured.</summary>
    [Delphi(PortStatus.Ported)]
    public void AdjustActiveRange()
    {
        if (fHandleFollower is not null)
        {
            GetActiveRange(out int FirstActive, out int LastActive);
            BoldProperties.SetActiveRange(Follower, FirstActive, LastActive, 10);
            EnsureActiveCellFollowerExpressions();
        }
    }

    /// <summary>Deletes the column (while more than two are left).</summary>
    [Delphi(PortStatus.Ported, Note = "Deletes the Bold column (the grid's columns follow); Delphi's grid moved the column to the end and dropped its last.")]
    public void DeleteColumn(int ACol)
    {
        if (ColCount > 2 && ACol >= 0 && ACol < Columns.Count)
        {
            Columns[ACol].Dispose();
        }
    }

    /// <summary>
    /// Draws a cell: the title row with the column's title; a data row through the column's controller (its font and
    /// color; highlighted when HighlightCell says), or by OnDrawCell for a column the user draws; beyond the list the
    /// grid's color. Fixed cells get a raised edge, the selected cell a focus rectangle.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Draws on the grid's canvas while a cell paints (OnCellPainting). The state is the DataGridView's: Selected for the current cell, Frozen for a fixed one. A row that is not yet displayable gets the grid's color.")]
    public virtual void DrawCell(int ACol, int aRow, Rectangle ARect, DataGridViewElementStates AState)
    {
        TCanvas? Canvas = this.Canvas;
        if (Canvas is null || fHandleFollower is null)
        {
            return;
        }

        int aListRow = DataRow(aRow);
        if (ACol > Columns.Count - 1)
        {
            return;
        }

        TBoldGridColumn DrawColumn = Columns[ACol];
        if ((aListRow >= Follower.SubFollowerCount || aListRow < 0) && aRow != TitleRow)
        {
            Canvas.Brush.Color = Color;
            Canvas.FillRect(new Rectangle(ARect.X, ARect.Y, ARect.Width + 1, ARect.Height + 1));
            return;
        }

        if (aRow == TitleRow) // Title row
        {
            if (DrawColumn.Title is { } title) // Attempts to redraw before column is done creating
            {
                Canvas.Font.Assign(title.Font);
                Canvas.Brush.Color = title.Color;
                TBoldAsStringRenderer.DrawStringOnCanvas(Canvas, ARect, title.Alignment, new Point(1, 1), title.Caption);
            }
        }
        else if (RowFollowers(aListRow) is { } RowFollower && RowFollower.Displayable() && CellFollowers(ACol, aListRow) is { } CellFollower)
        {
            // Render font and color
            DrawColumn.BoldProperties.SetFont(Canvas.Font, CellFont(DrawColumn), CellFollower);
            Color cl = DrawColumn.Color;
            DrawColumn.BoldProperties.SetColor(ref cl, DrawColumn.Color, CellFollower);

            if (HighlightCell(AState, aRow))
            {
                Canvas.Brush.Color = SystemColors.Highlight;
                Canvas.Font.Color = SystemColors.HighlightText;
            }
            else
            {
                Canvas.Brush.Color = cl;
            }

            if (DrawColumn.UserDraw)
            {
                fOndrawCell?.Invoke(DrawColumn, Canvas, ACol, aRow, ARect, AState);
            }
            else
            {
                DrawColumn.BoldProperties.DrawOnCanvas(CellFollower, Canvas, ARect, DrawColumn.Alignment, new Point(2, 2));
            }
        }
        else
        {
            Canvas.Brush.Color = Color;
            Canvas.FillRect(ARect);
        }

        if (Canvas is TBoldGraphicsCanvas graphicsCanvas)
        {
            bool isFixed = (AState & DataGridViewElementStates.Frozen) != 0 || aRow < FixedRows;
            Border3DSide sides = 0;
            if (Options.Contains(goFixedVertLine))
            {
                sides |= Border3DSide.Right | Border3DSide.Left;
            }

            if (Options.Contains(goFixedHorzLine))
            {
                sides |= Border3DSide.Bottom | Border3DSide.Top;
            }

            if (isFixed && sides != 0)
            {
                ControlPaint.DrawBorder3D(graphicsCanvas.Graphics, ARect, Border3DStyle.RaisedInner, sides);
            }

            if (!DesignMode && (AState & DataGridViewElementStates.Selected) != 0 &&
                !(Options.Contains(goEditing) && Options.Contains(goAlwaysShowEditor)) && !Options.Contains(goRowSelect))
            {
                ControlPaint.DrawFocusRectangle(graphicsCanvas.Graphics, ARect);
            }
        }
    }

    /// <summary>Paints a cell through DrawCell (the Bold state of the cell: Selected for the current one, Frozen for a fixed one).</summary>
    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        base.OnCellPainting(e);
        if (e.Handled || e.ColumnIndex < 0 || fHandleFollower is null || fDestroying || e.Graphics is not { } graphics)
        {
            return;
        }

        int aRow = e.RowIndex < 0 ? TitleRow : GridRow(e.RowIndex);
        if (aRow == -1)
        {
            return;
        }

        DataGridViewElementStates state = DataGridViewElementStates.None;
        if (e.RowIndex >= 0 && CurrentCell is { } current && current.RowIndex == e.RowIndex &&
            (current.ColumnIndex == e.ColumnIndex || Options.Contains(goRowSelect)))
        {
            state |= DataGridViewElementStates.Selected;
        }

        if (e.ColumnIndex < FixedCols)
        {
            state |= DataGridViewElementStates.Frozen;
        }

        Rectangle bounds = e.CellBounds;
        var rect = new Rectangle(bounds.X, bounds.Y, Math.Max(0, bounds.Width - 1), Math.Max(0, bounds.Height - 1));
        fCanvas = new TBoldGraphicsCanvas(graphics);
        try
        {
            fCanvas.Font.Assign(GridFont());
            DrawCell(e.ColumnIndex, aRow, rect, state);
        }
        finally
        {
            fCanvas = null;
        }

        e.Paint(e.ClipBounds, DataGridViewPaintParts.Border);
        e.Handled = true;
    }

    /// <summary>The value of a cell, for the DataGridView (tool tips, copying): the shown string when the cell is displayed.</summary>
    protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
    {
        base.OnCellValueNeeded(e);
        if (e.Value is null && fHandleFollower is not null && !fDestroying &&
            CellFollowers(e.ColumnIndex, DataRow(GridRow(e.RowIndex))) is { } cell && cell.Displayable() &&
            cell.Controller is TBoldStringFollowerController controller)
        {
            e.Value = controller.GetCurrentAsString(cell);
        }
    }

    /// <summary>A cell starts editing only when CanEditShow (and not in a check box column).</summary>
    protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e)
    {
        base.OnCellBeginEdit(e);
        if (!e.Cancel && (fHandleFollower is null || ColumnIsCheckBox(e.ColumnIndex) || !CanEditShow()))
        {
            e.Cancel = true;
        }
    }

    /// <summary>The editor shows: the cell's text, font and limit, read only unless the value may change; the lookup combo box.</summary>
    protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
    {
        base.OnEditingControlShowing(e);
        if (e.Control is TBoldInplaceEdit editor && fHandleFollower is not null)
        {
            fInplaceEditor = editor;
            editor.Text = GetEditText(Col, Row);
            editor.MaxLength = GetEditLimit();
            editor.ReadOnly = !CanEditModify();
            editor.ChangedPos(true);
        }
    }

    /// <summary>The editing ended: the change is applied (EditStop) unless a lookup was dropped.</summary>
    protected override void OnCellEndEdit(DataGridViewCellEventArgs e)
    {
        base.OnCellEndEdit(e);
        if (fHandleFollower is not null && !fDestroying)
        {
            fInplaceEditor?.ChangedPos(false);
        }
    }

    /// <summary>A cell that is not the fixed column becomes the current one; OnSelectCell may refuse.</summary>
    protected override bool SetCurrentCellAddressCore(int columnIndex, int rowIndex, bool setAnchorCellAddress, bool validateCurrentCell, bool throughMouseClick)
    {
        if (columnIndex >= 0 && rowIndex >= 0 && FColumns is not null && !fDestroying)
        {
            if (columnIndex < FixedCols && base.ColumnCount > FixedCols)
            {
                columnIndex = Math.Clamp(Math.Max(fCol, FixedCols), FixedCols, base.ColumnCount - 1);
            }

            if (!SelectCell(columnIndex, rowIndex + FixedRows))
            {
                return false;
            }
        }

        return base.SetCurrentCellAddressCore(columnIndex, rowIndex, setAnchorCellAddress, validateCurrentCell, throughMouseClick);
    }

    /// <summary>The user moved a column: once the move is over, the Bold column follows (ColumnMoved).</summary>
    protected override void OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)
    {
        base.OnColumnDisplayIndexChanged(e);
        if (!fSyncingColumns && !fDestroying && FColumns is not null && !fColumnMovePending && IsHandleCreated)
        {
            fColumnMovePending = true;
            BeginInvoke(new Action(ColumnsReordered));
        }
    }

    /// <summary>
    /// The grid's columns go back to their places (a cell's place is its column's), the widths moving with the moved
    /// column; then ColumnMoved moves the Bold column.
    /// </summary>
    private void ColumnsReordered()
    {
        fColumnMovePending = false;
        int n = base.Columns.Count;
        int[] order = new int[n];
        foreach (DataGridViewColumn column in base.Columns)
        {
            order[column.DisplayIndex] = column.Index;
        }

        int p = 0;
        while (p < n && order[p] == p)
        {
            p++;
        }

        if (p == n)
        {
            return;
        }

        int q = n - 1;
        while (order[q] == q)
        {
            q--;
        }

        int FromIndex = order[p] == q ? q : p;
        int ToIndex = order[p] == q ? p : q;
        int[] widths = new int[n];
        for (int i = 0; i < n; i++)
        {
            widths[i] = base.Columns[order[i]].Width;
        }

        fSyncingColumns = true;
        try
        {
            for (int i = 0; i < n; i++)
            {
                base.Columns[i].DisplayIndex = i;
            }

            for (int i = 0; i < n; i++)
            {
                base.Columns[i].Width = widths[i];
            }

            ApplyFixedCols();
        }
        finally
        {
            fSyncingColumns = false;
        }

        ColumnMoved(FromIndex, ToIndex);
    }

    [Delphi(PortStatus.Ported, Note = "Delphi left this empty: the run time column editor is not part of BoldGrid.")]
    public void EditColumns()
    {
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElementTypeInfo? GetHandleStaticType() => BoldHandle?.StaticBoldType;

    [Delphi(PortStatus.Ported)]
    public TBoldElementTypeInfo? GetHandleListElementType() => BoldHandle?.ListElementType;

    /// <summary>The text the editor starts with (the cell's string), and the editor gets the column's font.</summary>
    [Delphi(PortStatus.Ported, Note = "Called when the editor shows (OnEditingControlShowing); the DataGridView sizes the editor.")]
    public virtual string GetEditText(int GridCol, int GridRow)
    {
        if (InplaceEditor is { } Editor && Col < Columns.Count)
        {
            Editor.Font = BoldWinForms.ToFont(CellFont(Columns[Col]));
        }

        return GetString(GridCol, DataRow(GridRow));
    }

    /// <summary>Moves the column (with its width) to another place.</summary>
    [Delphi(PortStatus.Ported, Note = "Moves the column's width with it, then ColumnMoved, as Delphi's grid did.")]
    public void MoveColumn(int FromIndex, int ToIndex)
    {
        if (FromIndex < 0 || FromIndex >= ColCount || ToIndex < 0 || ToIndex >= ColCount || FromIndex == ToIndex)
        {
            return;
        }

        var widths = new List<int>();
        for (int i = 0; i < ColCount; i++)
        {
            widths.Add(GetColWidth(i));
        }

        int moved = widths[FromIndex];
        widths.RemoveAt(FromIndex);
        widths.Insert(ToIndex, moved);
        for (int i = 0; i < widths.Count; i++)
        {
            SetColWidth(i, widths[i]);
        }

        ColumnMoved(FromIndex, ToIndex);
    }

    /// <summary>The editor's text changed: the cell's value may have changed.</summary>
    [Delphi(PortStatus.Ported, Note = "Called for each change of the editor's text (Delphi's OnChange).")]
    public virtual void SetEditText(int GridCol, int GridRow, string Value)
    {
        if (!DesignMode && EditorMode && CurrentCellFollower is { } cell) // CHECKME heeded?
        {
            ((TBoldStringFollowerController)cell.AssertedController).MayHaveChanged(Value, cell);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Invalidates the whole grid, as Delphi did (it could not invalidate a scrolled column alone).")]
    public void ReallyInvalidateCol(int Column) => Invalidate();

    /// <summary>Displays every row's cells (selecting all for a moment, so that all are active), then selects the current row again.</summary>
    [Delphi(PortStatus.Ported)]
    public void DisplayAllCells()
    {
        BoldProperties.SelectAll(Follower, true);
        Invalidate();
        AdjustActiveRange();
        EnsureActiveCellFollowerExpressions(); // TODO: Remove? AdjustActiveRange already calls this method, why call it again??
        Follower.EnsureDisplayable();
        BoldProperties.SelectAll(Follower, false);
        BoldProperties.SetSelected(Follower, DataRow(Row), true);
    }

    /// <summary>The grid's text: a line per row (the title row first), the cells of the data columns each ended by a tab.</summary>
    [Delphi(PortStatus.Ported)]
    public string AsClipBoardText()
    {
        // as the grid is optimizing the active followers, we need to activate them manually
        ActivateAllCells();
        var Result = new System.Text.StringBuilder();
        for (int row = 0; row < GridRowCount; row++)
        {
            for (int col = FixedCols; col < ColCount; col++)
            {
                Result.Append(CellText(col, row)).Append('\t');
            }

            Result.Append(BoldDefs.BOLDCRLF);
        }

        return Result.ToString();
    }

    /// <summary>Makes every row active and displays them.</summary>
    [Delphi(PortStatus.Ported)]
    public void ActivateAllCells()
    {
        BoldProperties.SetActiveRange(Follower, 0, GridRowCount - 1);
        DisplayAvailableFollowers();
    }

    /// <summary>The follower of a cell (a column of a list row), or null.</summary>
    public TBoldFollower? CellFollowers(int GridCol, int DataRow) => GetCellFollower(GridCol, DataRow);

    /// <summary>The follower of a list row, or null.</summary>
    public TBoldFollower? RowFollowers(int DataRow) => GetRowFollower(DataRow);

    /// <summary>True when the list row is selected.</summary>
    public bool Selected(int DataRow) => GetSelected(DataRow);

    /// <summary>The text of a cell (column, grid row): the title in the title row, else the cell's string.</summary>
    public string CellText(int col, int row) => GetCellText(col, row);

    /// <summary>A drag may start from any data row (not only the first column).</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(false)]
    public bool BoldDragAnywhere
    {
        get => fBoldDragAnywhere;
        set => fBoldDragAnywhere = value;
    }

    /// <summary>Down on the last row adds an element to the list.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(false)]
    public bool AddNewAtEnd
    {
        get => fAddNewAtEnd;
        set => fAddNewAtEnd = value;
    }

    /// <summary>The selected cell is highlighted even when the grid does not have the focus.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(true)]
    public bool AlwaysShowSelected
    {
        get => fAlwaysShowSelected;
        set => fAlwaysShowSelected = value;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(true)]
    public bool BoldHandleIndexLock
    {
        get => GetBoldHandleIndexLock();
        set => SetBoldHandleIndexLock(value);
    }

    /// <summary>The grid makes its columns from the list's element type whenever it changes.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(false)]
    public bool BoldAutoColumns
    {
        get => fBoldAutoColumns;
        set => fBoldAutoColumns = value;
    }

    /// <summary>A second column marks the objects that break a constraint.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(false)]
    public bool BoldShowConstraints
    {
        get => fBoldShowConstraints;
        set => fBoldShowConstraints = value;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldList? BoldList => GetBoldList();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldListAsFollowerListController BoldProperties
    {
        get => fBoldProperties!;
        set => SetController(value);
    }

    /// <summary>The Bold columns (the grid's columns follow them); column 0 is the fixed one.</summary>
    [Delphi(PortStatus.Ported, Note = "Set in code: the designer does not write a TCollection.")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new TBoldGridColumns Columns
    {
        get => FColumns!;
        set => SetColumns(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? CurrentBoldElement => GetCurrentBoldElement();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower? CurrentCellFollower => GetCurrentCellFollower();

    /// <summary>A click on a cell without moving fits its column to its contents.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool EnableColAdjust
    {
        get => fEnableColAdjust;
        set => fEnableColAdjust = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    /// <summary>Draws the cells of the columns the user draws (UserDraw).</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldDrawCellEvent? OnDrawCell
    {
        get => fOndrawCell;
        set => fOndrawCell = value;
    }

    /// <summary>Called when the grid scrolled.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TNotifyEvent? OnTopLeftChanged
    {
        get => fOnTopLeftChanged;
        set => fOnTopLeftChanged = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TGridOptions Options
    {
        get => GetOptions();
        set => SetOptions(value);
    }

    /// <summary>The font of the titles.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TFont TitleFont
    {
        get => fTitleFont!;
        set => SetTitleFont(value);
    }

    /// <summary>The list is empty: the grid shows one empty row.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasGhostRow => fHasGhostRow;

    [Delphi(PortStatus.Ported)]
    [DefaultValue(true)]
    public bool ShowTitleRow
    {
        get => GetShowTitleRow();
        set => SetShowTitleRow(value);
    }

    /// <summary>The grid row of the titles, or -1.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TitleRow => GetTitleRow();

    /// <summary>Asked before a cell becomes the current one.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TSelectCellEvent? OnSelectCell
    {
        get => FOnSelectCell;
        set => FOnSelectCell = value;
    }

    /// <summary>The cell being edited shows the lookup combo box.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LookUpEditorActive
    {
        get => fLookUpEditorActive;
        set => fLookUpEditorActive = value;
    }

    /// <summary>A row added alone becomes the current one.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(false)]
    public bool AutoSelectNewRows
    {
        get => fAutoSelectNewRows;
        set => fAutoSelectNewRows = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldList? MutableList => GetMutableList();
}

/// <summary>A Bold grid; its properties are public in TBoldCustomGrid.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGrid : TBoldCustomGrid
{
}

/// <summary>
/// The editor of a Bold grid's cells: checks typed characters with the cell's controller, Esc throws away the change;
/// for a lookup column a combo box over the cell offers the lookup handle's elements.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The DataGridView's editing text box.")]
public class TBoldInplaceEdit : DataGridViewTextBoxEditingControl
{
    private const int breDestElementDestroyed = 100;
    private const int WM_CHAR = 0x0102;

    private ComboBox? fCombo;
    private int fInitialComboIndex;
    private TBoldElement? fDestElement;
    private TBoldPassthroughSubscriber? fDestElementSubscriber;
    private bool fComboAborting;
    private TBoldGridColumn? fEditColumn;

    /// <summary>A choice of the lookup combo box: the string shown and the element.</summary>
    internal sealed class TBoldLookupItem(string Text, TBoldElement? Element)
    {
        public TBoldElement? Element { get; } = Element;

        public override string ToString() => Text;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [Delphi(PortStatus.Ported)]
    public TBoldInplaceEdit()
    {
        fDestElementSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldInplaceEdit.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fCombo);
            SysUtils.FreeAndNil(ref fDestElementSubscriber);
            fDestElement = null;
        }

        fEditColumn = null;
        base.Dispose(disposing);
    }

    /// <summary>The grid the editor edits in.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldCustomGrid Grid => (TBoldCustomGrid)EditingControlDataGridView!;

    /// <summary>A typed character clears the other selected rows; one the cell's controller refuses beeps; Esc throws away the change.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldInplaceEdit.KeyPress", Note = "Checks characters from the space up (Delphi checked #32..#255), the grid's CanEditAcceptKey first as Delphi's editor did.")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        TBoldCustomGrid Grid = this.Grid;
        // when editing, clear all other selected rows
        Grid.SetSelection(Grid.DataRow(Grid.Row), Keys.None, true, false);
        char Key = e.KeyChar;
        if (Key >= ' ' && (!Grid.CanEditAcceptKey(Key) ||
            (Grid.CurrentCellFollower is { } cell && !Grid.Columns[Grid.Col].BoldProperties.ValidateCharacter(Key, cell))))
        {
            System.Media.SystemSounds.Beep.Play();
            e.KeyChar = BoldDefs.BOLDNULL;
            e.Handled = true;
        }

        if (Key == BoldDefs.BOLDESC)
        {
            Grid.CurrentCellFollower?.DiscardChange();
            SelectAll();
            e.KeyChar = BoldDefs.BOLDNULL;
            e.Handled = true;
        }
    }

    /// <summary>The text changed: the grid's SetEditText (the cell's value may have changed).</summary>
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (EditingControlDataGridView is TBoldCustomGrid grid)
        {
            grid.SetEditText(grid.Col, grid.Row, Text);
        }
    }

    /// <summary>
    /// The editor shows (the lookup combo box shows over it) or hides: the change is applied (EditStop), or, when the
    /// lookup was dropped, the cell shows its value again.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Called by the grid when the editing starts (Showing) and ends, where Delphi caught the editor's moves (WM_WINDOWPOSCHANGED).")]
    internal void ChangedPos(bool Showing)
    {
        if (!Showing)
        {
            TBoldCustomGrid Grid = this.Grid;
            if (fComboAborting)
            {
                Grid.CurrentCellFollower?.MarkValueOutOfDate();
            }
            else
            {
                Grid.EditStop();
            }

            if (fCombo is not null)
            {
                fCombo.Visible = false;
            }

            Grid.LookUpEditorActive = false;
            if (Grid.ContainsFocus && Grid.CanFocus)
            {
                Grid.Focus();
            }
        }
        else
        {
            InitCombo();
        }
    }

    /// <summary>For a lookup column: the combo box over the cell, with the lookup handle's elements and the cell's value chosen.</summary>
    [Delphi(PortStatus.Ported, Note = "Placed over the cell being edited.")]
    protected void InitCombo()
    {
        TBoldCustomGrid Grid = this.Grid;
        fEditColumn = Grid.Columns[Grid.Col];
        if (Grid.LookUpEditorActive && fEditColumn.LookupHandle is { } LookupHandle && Grid.CurrentCell is { } current)
        {
            ComboBox Combo = this.Combo;
            Combo.Parent = Grid;
            Combo.Items.Clear();
            TBoldFollower? CellFollower = Grid.CellFollowers(Grid.Col, Grid.Row - Grid.FixedRows);
            fDestElement = GetDestElement(CellFollower, fEditColumn);
            fDestElementSubscriber!.CancelAllSubscriptions();
            fDestElement?.AddSmallSubscription(fDestElementSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), breDestElementDestroyed);

            TBoldElement? CellValue = fDestElement;
            if (CellValue is TBoldObjectReference reference)
            {
                CellValue = reference.BoldObject;
            }

            fInitialComboIndex = -1;
            if (fEditColumn.NilElementMode == TBoldNilElementMode.neInsertFirst)
            {
                fInitialComboIndex = 0;
                Combo.Items.Add(new TBoldLookupItem(fEditColumn.LookUpProperties.NilStringRepresentation, null));
            }

            LookupHandle.List?.EnsureRange(0, LookupHandle.Count - 1);
            for (int i = 0; i < LookupHandle.Count; i++)
            {
                TBoldElement? ListElement = LookupHandle.List![i];
                Combo.Items.Add(new TBoldLookupItem(fEditColumn.LookUpProperties.GetAsString(ListElement), ListElement));
                if (CellValue == ListElement || (CellValue is TBoldAttribute && CellValue.IsEqual(ListElement)))
                {
                    fInitialComboIndex = Combo.Items.Count - 1;
                }
            }

            if (fEditColumn.NilElementMode == TBoldNilElementMode.neAddLast)
            {
                if (fInitialComboIndex == -1)
                {
                    fInitialComboIndex = Combo.Items.Count;
                }

                Combo.Items.Add(new TBoldLookupItem(fEditColumn.LookUpProperties.NilStringRepresentation, null));
            }

            Rectangle cellRect = Grid.GetCellDisplayRectangle(current.ColumnIndex, current.RowIndex, false);
            Combo.SetBounds(cellRect.Left - 1, cellRect.Top - 1, cellRect.Width + 3, cellRect.Height);
            Combo.SelectedIndex = fInitialComboIndex;
            Combo.Visible = true;
            Combo.BringToFront();
        }
    }

    /// <summary>
    /// The element a lookup choice goes to: the cell's element when the column has OnLookupChange, else the element
    /// the column's expression gives (when it is an existing one).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal TBoldElement? GetDestElement(TBoldFollower? CellFollower, TBoldGridColumn Column)
    {
        TBoldElement? result = null;
        if (CellFollower?.Element is { } element)
        {
            // if the event is active, then return the element in the grid, otherwise
            // see if we get any useful element from the cell.
            if (Column.OnLookupChange is not null)
            {
                result = element;
            }
            else
            {
                using var ie = new TBoldIndirectElement();
                element.EvaluateExpression(Column.BoldProperties.Expression, ie, false, Column.BoldProperties.VariableList);
                if (ie.Value is not null && !ie.OwnsValue)
                {
                    result = ie.Value;
                }
            }
        }

        return result;
    }

    /// <summary>A choice was made (or the combo box was left): it goes to the cell (unless dropped), and the combo box hides.</summary>
    [Delphi(PortStatus.Ported)]
    private void ComboChange(object? sender, EventArgs e)
    {
        if (!fComboAborting)
        {
            Grid.LookUpChange(sender, fDestElement, fEditColumn);
        }

        HideCombo();
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breDestElementDestroyed)
        {
            fDestElement = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void HideCombo() => Combo.Visible = false;

    /// <summary>The lookup combo box (made on first use): a drop down list; a choice, Enter or leaving it takes the choice, Esc drops it.</summary>
    [Delphi(PortStatus.Ported)]
    private ComboBox GetCombo()
    {
        if (fCombo is null)
        {
            fCombo = new TBoldInplaceCombo(this)
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false,
            };
            fCombo.SelectionChangeCommitted += ComboChange; // OnChange and OnClick
            fCombo.KeyDown += ComboKeyDown;
            fCombo.Leave += ComboChange; // OnExit
        }

        return fCombo;
    }

    /// <summary>A key while the combo box shows: Esc hides it, any other goes to the combo box.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldInplaceEdit.WMKeyDown")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (fCombo is { Visible: true })
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideCombo();
                base.OnKeyDown(e);
                Grid.Focus();
            }
            else
            {
                fCombo.Focus();
                SendMessage(fCombo.Handle, WM_CHAR, e.KeyValue, IntPtr.Zero);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    /// <summary>Enter takes the combo box's choice; Esc drops it (the combo box goes back to the cell's value).</summary>
    [Delphi(PortStatus.Ported)]
    protected void ComboKeyDown(object? Sender, KeyEventArgs e)
    {
        if (Sender == fCombo)
        {
            switch (e.KeyCode)
            {
                case Keys.Return:
                    ComboChange(this, EventArgs.Empty);
                    OnKeyDown(e);
                    Grid.Focus();
                    break;
                case Keys.Escape:
                    Combo.SelectedIndex = fInitialComboIndex;
                    fComboAborting = true;
                    try
                    {
                        HideCombo();
                    }
                    finally
                    {
                        fComboAborting = false;
                    }

                    OnKeyDown(e);
                    Grid.Focus();
                    break;
            }
        }
    }

    /// <summary>The lookup combo box.</summary>
    [Delphi(PortStatus.Ported)]
    public ComboBox Combo => GetCombo();
}

/// <summary>The combo box of a grid's lookup editor.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldInplaceCombo : ComboBox
{
    private readonly TBoldInplaceEdit fInplaceEdit;

    [Delphi(PortStatus.Ported, Symbol = "TBoldInplaceCombo.createWithInplaceEditor")]
    public TBoldInplaceCombo(TBoldInplaceEdit InplaceEdit)
    {
        fInplaceEdit = InplaceEdit;
    }

    /// <summary>The editor the combo box belongs to.</summary>
    public TBoldInplaceEdit InplaceEdit => fInplaceEdit;
}
