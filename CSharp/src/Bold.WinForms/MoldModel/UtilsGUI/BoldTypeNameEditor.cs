// Delphi: Source/MoldModel/UtilsGUI/BoldTypeNameEditor.pas
// Port map: CSharp/port/units/BoldTypeNameEditor.md

using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>
/// Edits the mappings of a type name dictionary in a grid, a row per model type: add, delete, copy and move rows,
/// save and load the dictionary, and merge the default mappings in.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The grid is a DataGridView; its column headers are Delphi's fixed row 0, so Delphi's row r is grid row r - 1.")]
public class TBoldTypeNameEditorForm : Form
{
    private const int ColCount = 13;
    public readonly DataGridView StringGrid1;
    public readonly Button btnAdd;
    public readonly Button btnDelete;
    public readonly Button btnOK;
    public readonly Button btnCancel;
    public readonly Button btnDown;
    public readonly Button btnUp;
    public readonly MenuStrip MainMenu1;
    public readonly ToolStripMenuItem File1;
    public readonly ToolStripMenuItem SaveToFile1;
    public readonly SaveFileDialog SaveDialog1;
    public readonly OpenFileDialog OpenDialog1;
    public readonly ToolStripMenuItem LoadFromFile1;
    public readonly ContextMenuStrip PopupMenu1;
    public readonly ToolStripMenuItem Copyrow1;
    public readonly ToolStripMenuItem Edit1;
    public readonly ToolStripMenuItem MergeDefaultsmappings1;

    public TBoldTypeNameEditorForm()
    {
        SuspendLayout();
        Text = "TypeName Editor";
        ClientSize = new Size(1009, 400);
        MinimumSize = new Size(411, 200);
        MaximizeBox = false;
        MinimizeBox = false;

        Copyrow1 = new ToolStripMenuItem("Copy row", null, Copyrow1Click) { Name = "Copyrow1" };
        PopupMenu1 = new ContextMenuStrip();
        PopupMenu1.Items.Add(Copyrow1);
        StringGrid1 = new DataGridView
        {
            Name = "StringGrid1", Bounds = new Rectangle(0, 24, 1009, 336), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, ContextMenuStrip = PopupMenu1,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, SelectionMode = DataGridViewSelectionMode.CellSelect,
        };
        StringGrid1.RowTemplate.Height = 18;
        for (int i = 0; i < ColCount; i++)
        {
            StringGrid1.Columns.Add(new DataGridViewTextBoxColumn { SortMode = DataGridViewColumnSortMode.NotSortable, Width = 64 });
        }

        StringGrid1.RowCount = 1;
        btnUp = NewButton("btnUp", 8, 25, "▲", AnchorStyles.Left | AnchorStyles.Bottom, btnUpClick);
        btnDown = NewButton("btnDown", 40, 25, "▼", AnchorStyles.Left | AnchorStyles.Bottom, btnDownClick);
        btnAdd = NewButton("btnAdd", 665, 81, "&Add", AnchorStyles.Right | AnchorStyles.Bottom, btnAddClick);
        btnDelete = NewButton("btnDelete", 753, 81, "&Delete", AnchorStyles.Right | AnchorStyles.Bottom, btnDeleteClick);
        btnOK = NewButton("btnOK", 841, 81, "OK", AnchorStyles.Right | AnchorStyles.Bottom, null);
        btnOK.DialogResult = DialogResult.OK;
        btnCancel = NewButton("btnCancel", 929, 81, "Cancel", AnchorStyles.Right | AnchorStyles.Bottom, null);
        btnCancel.DialogResult = DialogResult.Cancel;
        AcceptButton = btnOK;

        SaveToFile1 = new ToolStripMenuItem("Save To File", null, SaveToFile1Click) { Name = "SaveToFile1" };
        LoadFromFile1 = new ToolStripMenuItem("Load From File", null, LoadFromFile1Click) { Name = "LoadFromFile1" };
        File1 = new ToolStripMenuItem("File") { Name = "File1" };
        File1.DropDownItems.AddRange([SaveToFile1, LoadFromFile1]);
        MergeDefaultsmappings1 = new ToolStripMenuItem("Merge Defaults mappings", null, MergeDefaultsmappings1Click) { Name = "MergeDefaultsmappings1" };
        Edit1 = new ToolStripMenuItem("Edit") { Name = "Edit1" };
        Edit1.DropDownItems.Add(MergeDefaultsmappings1);
        MainMenu1 = new MenuStrip { Name = "MainMenu1" };
        MainMenu1.Items.AddRange([File1, Edit1]);
        SaveDialog1 = new SaveFileDialog();
        OpenDialog1 = new OpenFileDialog();

        Controls.AddRange([StringGrid1, btnUp, btnDown, btnAdd, btnDelete, btnOK, btnCancel, MainMenu1]);
        MainMenuStrip = MainMenu1;
        ResumeLayout(false);
        PerformLayout();
        FormCreate(this, EventArgs.Empty);
    }

    private static Button NewButton(string name, int left, int width, string caption, AnchorStyles anchor, EventHandler? click)
    {
        var result = new Button { Name = name, Bounds = new Rectangle(left, 368, width, 25), Text = caption, Anchor = anchor };
        if (click is not null)
        {
            result.Click += click;
        }

        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SaveDialog1.Dispose();
            OpenDialog1.Dispose();
            PopupMenu1.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Delphi's RowCount: the rows with the title row.</summary>
    private int RowCount
    {
        get => StringGrid1.RowCount + 1;
        set => StringGrid1.RowCount = Math.Max(0, value - 1);
    }

    /// <summary>Delphi's Row: the current row (1 for the first mapping).</summary>
    private int Row
    {
        get => (StringGrid1.CurrentCell?.RowIndex ?? 0) + 1;
        set => StringGrid1.CurrentCell = StringGrid1[Math.Max(0, StringGrid1.CurrentCell?.ColumnIndex ?? 0), value - 1];
    }

    /// <summary>Delphi's Cells[ACol, ARow]: row 0 is the title row.</summary>
    public string Cells(int ACol, int ARow) =>
        ARow == 0 ? StringGrid1.Columns[ACol].HeaderText : StringGrid1[ACol, ARow - 1].Value as string ?? "";

    /// <summary>Sets Delphi's Cells[ACol, ARow].</summary>
    public void SetCells(int ACol, int ARow, string Value)
    {
        if (ARow == 0)
        {
            StringGrid1.Columns[ACol].HeaderText = Value;
        }
        else
        {
            StringGrid1[ACol, ARow - 1].Value = Value;
        }
    }

    /// <summary>The column titles.</summary>
    [Delphi(PortStatus.Ported, Note = "Called by the constructor (Delphi's OnCreate).")]
    private void FormCreate(object? Sender, EventArgs e)
    {
        string[] titles =
        [
            "ModelName", "ExpressionName", "DelphiName", "ContentName", "PMapper", "Accessor", "NativeType", "UnitName", // do not localize
            "ComType", "IDLType", "ValueInterface", "VI-accessor", "VI-NativeType", // do not localize
        ];
        for (int i = 0; i < titles.Length; i++)
        {
            SetCells(i, 0, titles[i]);
        }
    }

    /// <summary>A new row at the end with the name patterns, made current.</summary>
    [Delphi(PortStatus.Ported)]
    private void btnAddClick(object? Sender, EventArgs e)
    {
        RowCount++;
        StringGrid1.CurrentCell = StringGrid1[0, RowCount - 2];
        SetCells(2, RowCount - 1, "TXX<Name>"); // do not localize
        SetCells(3, RowCount - 1, "<Name>"); // do not localize
        SetCells(4, RowCount - 1, "TBoldPM<Name>"); // do not localize
        SetCells(5, RowCount - 1, "as<Name>"); // do not localize
        SetCells(6, RowCount - 1, "T<Name>"); // do not localize
        SetCells(10, RowCount - 1, "IBold<Name>Content"); // do not localize
        StringGrid1.Focus();
    }

    /// <summary>Deletes the current row (the last mapping stays).</summary>
    [Delphi(PortStatus.Ported)]
    private void btnDeleteClick(object? Sender, EventArgs e)
    {
        if (RowCount > 2)
        {
            StringGrid1.Rows.RemoveAt(Row - 1);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btnDownClick(object? Sender, EventArgs e) => MoveRow(1);

    /// <summary>Swaps the current row with the one in the direction, which becomes current.</summary>
    [Delphi(PortStatus.Ported)]
    private void MoveRow(int Direction)
    {
        if (Row + Direction >= 1 && Row + Direction < RowCount)
        {
            int OldPos = Row;
            string[] OldRow = [.. Enumerable.Range(0, ColCount).Select(i => Cells(i, OldPos))];
            for (int i = 0; i < ColCount; i++)
            {
                SetCells(i, OldPos, Cells(i, OldPos + Direction));
            }

            for (int i = 0; i < ColCount; i++)
            {
                SetCells(i, OldPos + Direction, OldRow[i]);
            }

            Row = OldPos + Direction;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btnUpClick(object? Sender, EventArgs e) => MoveRow(-1);

    /// <summary>A new row with the current row's names (all but the model and expression names).</summary>
    [Delphi(PortStatus.Ported)]
    private void Copyrow1Click(object? Sender, EventArgs e)
    {
        int OldPos = Row;
        btnAddClick(this, e);
        for (int i = 2; i < ColCount; i++)
        {
            SetCells(i, Row, Cells(i, OldPos));
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SaveToFile1Click(object? Sender, EventArgs e)
    {
        if (SaveDialog1.ShowDialog(this) == DialogResult.OK)
        {
            var Dictionary = new TBoldTypeNameDictionary(null);
            SaveToDictionary(Dictionary);
            Dictionary.SaveToFile(SaveDialog1.FileName);
        }
    }

    /// <summary>A row per mapping of the dictionary.</summary>
    [Delphi(PortStatus.Ported)]
    public void LoadFromDictionary(TBoldTypeNameDictionary Dictionary)
    {
        StringGrid1.Rows.Clear();
        RowCount = Dictionary.Count + 1;
        for (int i = 0; i < Dictionary.Count; i++)
        {
            TBoldTypeNameMapping mapping = Dictionary.Mapping(i);
            string[] values =
            [
                mapping.ModelName, mapping.ExpressionName, mapping.DelphiName, mapping.ContentsName, mapping.MapperName, mapping.Accessor,
                mapping.NativeType, mapping.UnitNameText, mapping.ComType, mapping.IDLType, mapping.ValueInterface,
                mapping.ValueInterfaceAccessor, mapping.ValueInterfaceNativeType,
            ];
            for (int c = 0; c < values.Length; c++)
            {
                SetCells(c, i + 1, values[c]);
            }
        }
    }

    /// <summary>The dictionary's mappings become the rows.</summary>
    [Delphi(PortStatus.Ported)]
    public void SaveToDictionary(TBoldTypeNameDictionary Dictionary)
    {
        StringGrid1.EndEdit();
        Dictionary.Clear();
        for (int i = 1; i < RowCount; i++)
        {
            TBoldTypeNameMapping mapping = Dictionary.AddMapping();
            mapping.ModelName = Cells(0, i);
            mapping.ExpressionName = Cells(1, i);
            mapping.DelphiName = Cells(2, i);
            mapping.ContentsName = Cells(3, i);
            mapping.MapperName = Cells(4, i);
            mapping.Accessor = Cells(5, i);
            mapping.NativeType = Cells(6, i);
            mapping.UnitNameText = Cells(7, i);
            mapping.ComType = Cells(8, i);
            mapping.IDLType = Cells(9, i);
            mapping.ValueInterface = Cells(10, i);
            mapping.ValueInterfaceAccessor = Cells(11, i);
            mapping.ValueInterfaceNativeType = Cells(12, i);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void LoadFromFile1Click(object? Sender, EventArgs e)
    {
        if (OpenDialog1.ShowDialog(this) == DialogResult.OK)
        {
            var Dictionary = new TBoldTypeNameDictionary(null);
            Dictionary.LoadFromFile(OpenDialog1.FileName);
            LoadFromDictionary(Dictionary);
        }
    }

    /// <summary>
    /// The default mappings replace the rows of their model names (the abstract types, which have none, by expression
    /// name); the others are added.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void MergeDefaultsmappings1Click(object? Sender, EventArgs e)
    {
        var DefaultMappings = new TBoldTypeNameDictionary(null);
        var CurrentMappings = new TBoldTypeNameDictionary(null);
        DefaultMappings.AddDefaultMappings();
        SaveToDictionary(CurrentMappings);
        for (int i = 0; i < DefaultMappings.Count; i++)
        {
            TBoldTypeNameMapping? Mapping = null;
            if (DefaultMappings.Mapping(i).ModelName != "")
            {
                Mapping = CurrentMappings.ExactMappingForModelName(DefaultMappings.Mapping(i).ModelName);
            }
            else
            {
                // our abstract types does not have model names. Fortuantely,
                for (int j = 0; j < CurrentMappings.Count; j++)
                {
                    if (string.Equals(DefaultMappings.Mapping(i).ExpressionName, CurrentMappings.Mapping(j).ExpressionName, StringComparison.OrdinalIgnoreCase))
                    {
                        Mapping = CurrentMappings.Mapping(j);
                        break;
                    }
                }
            }

            DefaultMappings.Mapping(i).AssignTo(Mapping ?? CurrentMappings.AddMapping());
        }

        LoadFromDictionary(CurrentMappings);
    }
}
