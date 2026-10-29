// Delphi: Source/BoldAwareGUI/BoldControls/BoldGridRTColEditor.pas
// Port map: CSharp/port/units/BoldGridRTColEditor.md

using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGridRTColEditor", Symbol = "TBoldExposedGrid", Note = "Opened the grid's protected members; the grid's columns and list element type are public.")]

namespace Bold;

/// <summary>
/// Edits a grid's columns while the program runs: pick a column to change its caption and expression (with the OCL
/// editor), add and delete columns; it shows whether the column has a renderer and what the renderer sets.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file.")]
public class TfrmRTColEditor : Form
{
    private TBoldCustomGrid? fGrid;
    private TBoldGridColumn? fCurrentGridColumn;
    private int fCurrentIndex;
    public readonly ListBox lbxColumns;
    public readonly Panel Panel1;
    public readonly Panel Panel2;
    public readonly Button cmdOK;
    public readonly Panel Panel3;
    public readonly TextBox tbxCaption;
    public readonly TextBox tbxExpression;
    public readonly Button cmdApply;
    public readonly Label Label1;
    public readonly Label Label2;
    public readonly Button cmdAddColumn;
    public readonly Button cmdDeleteColumn;
    public readonly Button cmdOCLEditor;
    public readonly GroupBox GroupBox1;
    public readonly CheckBox cbxRenderer;
    public readonly CheckBox cbxSetText;
    public readonly CheckBox cbxSetFont;
    public readonly CheckBox cbxSetColor;
    public readonly Label Label3;

    /// <summary>A column in the list: its caption and the column.</summary>
    private sealed record TColumnItem(string Caption, TBoldGridColumn Column)
    {
        public override string ToString() => Caption;
    }

    public TfrmRTColEditor()
    {
        SuspendLayout();
        Text = "Runtime Column Editor";
        ClientSize = new Size(368, 304);
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        lbxColumns = new ListBox { Name = "lbxColumns", Dock = DockStyle.Fill, IntegralHeight = false };
        lbxColumns.Click += lbxColumnsClick;
        cmdOK = new Button { Name = "cmdOK", Bounds = new Rectangle(0, 8, 81, 25), Text = "Done", DialogResult = DialogResult.OK };
        Panel2 = new Panel { Name = "Panel2", Dock = DockStyle.Right, Width = 88 };
        Panel2.Controls.Add(cmdOK);
        cmdAddColumn = new Button { Name = "cmdAddColumn", Bounds = new Rectangle(4, 8, 81, 25), Text = "&Add Column" };
        cmdAddColumn.Click += cmdAddColumnClick;
        cmdDeleteColumn = new Button { Name = "cmdDeleteColumn", Bounds = new Rectangle(92, 8, 81, 25), Text = "&Delete Column" };
        cmdDeleteColumn.Click += cmdDeleteColumnClick;
        Panel1 = new Panel { Name = "Panel1", Dock = DockStyle.Bottom, Height = 40 };
        Panel1.Controls.AddRange([cmdAddColumn, cmdDeleteColumn, Panel2]);

        Label1 = new Label { Name = "Label1", Location = new Point(8, 8), AutoSize = true, Text = "&Header Caption" };
        tbxCaption = new TextBox { Name = "tbxCaption", Bounds = new Rectangle(8, 24, 217, 21) };
        tbxCaption.TextChanged += PropertyChanged;
        tbxCaption.KeyPress += PropertyKeyPress;
        Label2 = new Label { Name = "Label2", Location = new Point(8, 56), AutoSize = true, Text = "&Expression" };
        tbxExpression = new TextBox { Name = "tbxExpression", Bounds = new Rectangle(8, 72, 197, 21) };
        tbxExpression.TextChanged += PropertyChanged;
        tbxExpression.KeyPress += PropertyKeyPress;
        cmdOCLEditor = new Button { Name = "cmdOCLEditor", Bounds = new Rectangle(204, 72, 21, 21), Text = "..." };
        cmdOCLEditor.Click += cmdOCLEditorClick;
        cmdApply = new Button { Name = "cmdApply", Bounds = new Rectangle(144, 104, 81, 25), Text = "&Apply", Enabled = false };
        cmdApply.Click += cmdApplyClick;
        cbxRenderer = new CheckBox { Name = "cbxRenderer", Bounds = new Rectangle(8, 16, 93, 17), Text = "&Has Renderer", ThreeState = true };
        Label3 = new Label { Name = "Label3", Location = new Point(24, 44), AutoSize = true, Text = "String Renderer" };
        Label3.Font = new Font(Label3.Font, FontStyle.Underline);
        cbxSetText = new CheckBox { Name = "cbxSetText", Bounds = new Rectangle(24, 60, 110, 17), Text = "&Get/Set string", ThreeState = true };
        cbxSetFont = new CheckBox { Name = "cbxSetFont", Bounds = new Rectangle(24, 76, 85, 17), Text = "Set &Font", ThreeState = true };
        cbxSetColor = new CheckBox { Name = "cbxSetColor", Bounds = new Rectangle(24, 92, 85, 17), Text = "Set &Color", ThreeState = true };
        GroupBox1 = new GroupBox { Name = "GroupBox1", Bounds = new Rectangle(8, 143, 217, 121), Text = "Renderer Info", Enabled = false };
        GroupBox1.Controls.AddRange([cbxRenderer, Label3, cbxSetText, cbxSetFont, cbxSetColor]);
        Panel3 = new Panel { Name = "Panel3", Dock = DockStyle.Right, Width = 232 };
        Panel3.Controls.AddRange([Label1, tbxCaption, Label2, tbxExpression, cmdOCLEditor, cmdApply, GroupBox1]);

        Controls.AddRange([lbxColumns, Panel3, Panel1]);
        AcceptButton = cmdOK;
        ResumeLayout(false);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldGridColumn? GetGridColumn() => fCurrentGridColumn;

    /// <summary>The column whose caption, expression and renderer show (none: all empty and grayed).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetGridColumn(TBoldGridColumn? Value)
    {
        fCurrentGridColumn = Value;
        if (CurrentGridColumn is { } column)
        {
            tbxCaption.Text = column.Title.Caption;
            tbxExpression.Text = column.BoldProperties.Expression;
            TBoldAsStringRenderer? renderer = column.BoldProperties.Renderer;
            cbxRenderer.Checked = renderer is not null;
            if (renderer is not null)
            {
                cbxSetText.Checked = renderer.OnSetAsString is not null && renderer.OnGetAsString is not null;
                cbxSetFont.Checked = renderer.OnSetFont is not null;
                cbxSetColor.Checked = renderer.OnSetColor is not null;
            }
            else
            {
                cbxSetText.Checked = false;
                cbxSetFont.Checked = false;
                cbxSetColor.Checked = false;
            }
        }
        else
        {
            tbxCaption.Text = "";
            tbxExpression.Text = "";
            cbxRenderer.CheckState = CheckState.Indeterminate;
            cbxSetText.CheckState = CheckState.Indeterminate;
            cbxSetFont.CheckState = CheckState.Indeterminate;
            cbxSetColor.CheckState = CheckState.Indeterminate;
        }

        cmdApply.Enabled = false;
    }

    [Delphi(PortStatus.Ported, Note = "The grid itself: its columns are public.")]
    private TBoldCustomGrid GetEGrid() => Grid!;

    /// <summary>The grid's columns, but the first (fixed) one.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetColumns()
    {
        for (int i = 1; i < Grid!.ColCount; i++)
        {
            lbxColumns.Items.Add(new TColumnItem(EGrid.Columns[i].Title.Caption, EGrid.Columns[i]));
        }
    }

    /// <summary>Edits the grid's columns in a modal form.</summary>
    [Delphi(PortStatus.Ported)]
    public void Execute(TBoldCustomGrid? BoldCustomGrid)
    {
        if (BoldCustomGrid is null)
        {
            throw new EBold(SysUtils.Format(sExecuteGridNotAssigned, GetType().Name));
        }

        Grid = BoldCustomGrid;
        CurrentGridColumn = null;
        GetColumns();
        ShowModalForm(this);
    }

    /// <summary>Shows the form modally (tests replace it).</summary>
    internal Func<Form, DialogResult> ShowModalForm { get; set; } = form => form.ShowDialog();

    [Delphi(PortStatus.Ported)]
    private void lbxColumnsClick(object? Sender, EventArgs e)
    {
        if (lbxColumns.SelectedItem is TColumnItem item)
        {
            CurrentGridColumn = item.Column;
            CurrentIndex = lbxColumns.SelectedIndex;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void PropertyChanged(object? Sender, EventArgs e) => cmdApply.Enabled = true;

    /// <summary>The column gets the caption and the expression.</summary>
    [Delphi(PortStatus.Ported)]
    private void cmdApplyClick(object? Sender, EventArgs e)
    {
        if (CurrentGridColumn is { } column)
        {
            column.Title.Caption = tbxCaption.Text;
            column.BoldProperties.Expression = tbxExpression.Text;
            lbxColumns.Items[CurrentIndex] = new TColumnItem(tbxCaption.Text, column);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void cmdAddColumnClick(object? Sender, EventArgs e)
    {
        Grid!.AddColumn();
        TBoldGridColumn column = EGrid.Columns[EGrid.ColCount - 1];
        column.Title.Caption = sNewColumn;
        lbxColumns.Items.Add(new TColumnItem(column.Title.Caption, column));
    }

    [Delphi(PortStatus.Ported)]
    private void cmdDeleteColumnClick(object? Sender, EventArgs e)
    {
        if (CurrentGridColumn is { } column)
        {
            column.Dispose();
            lbxColumns.Items.RemoveAt(CurrentIndex);
            CurrentGridColumn = null;
        }
    }

    /// <summary>Nothing can be typed without a column.</summary>
    [Delphi(PortStatus.Ported)]
    private void PropertyKeyPress(object? Sender, KeyPressEventArgs e)
    {
        if (CurrentGridColumn is null)
        {
            e.Handled = true;
        }
    }

    /// <summary>The OCL editor edits the expression in the context of the grid's elements.</summary>
    [Delphi(PortStatus.Ported)]
    private void cmdOCLEditorClick(object? Sender, EventArgs e)
    {
        using var editor = new TBoldOclPropEditForm();
        editor.Context = EGrid.GetHandleListElementType();
        editor.OclExpr = tbxExpression.Text;
        if (ShowModalForm(editor) == DialogResult.OK)
        {
            tbxExpression.Text = editor.OclExpr;
        }
    }

    [Delphi(PortStatus.Ported)]
    private int CurrentIndex
    {
        get => fCurrentIndex;
        set => fCurrentIndex = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldCustomGrid? Grid
    {
        get => fGrid;
        set => fGrid = value;
    }

    [Delphi(PortStatus.Ported, Note = "The grid itself (Delphi's typecast to TBoldExposedGrid).")]
    public TBoldCustomGrid EGrid => GetEGrid();

    [Delphi(PortStatus.Ported)]
    public TBoldGridColumn? CurrentGridColumn
    {
        get => GetGridColumn();
        set => SetGridColumn(value);
    }
}
