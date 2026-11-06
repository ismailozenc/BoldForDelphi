// Delphi: Source/UMLModel/Editor/BoldUMLConstraintEditor.pas
// Port map: CSharp/port/units/BoldUMLConstraintEditor.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLConstraintEditor", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>The constraints of an element: a list of their names and bodies, to add to and remove from.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code; a grid of the element's constraints in the UML graph.")]
public class TfrmBoldUMLConstraintEditor : Form
{
    private static TfrmBoldUMLConstraintEditor? G_ConstraintEditor;

    public readonly ToolStrip ToolBar1;
    public readonly ToolStripButton btAdd;
    public readonly ToolStripButton btRemove;
    public readonly DataGridView BoldGrid1;
    public readonly ContextMenuStrip PopupMenu2;
    public readonly ToolStripMenuItem mnuCut;
    public readonly ToolStripMenuItem mnuCopy;
    public readonly ToolStripMenuItem mnuPaste;
    private TBoldUMLObject? fCurrentElement;
    private bool fShowing;

    public TfrmBoldUMLConstraintEditor()
    {
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        ClientSize = new Size(390, 221);
        btAdd = new ToolStripButton("Add constraint", null, btAddClick) { ToolTipText = "Add constraint" };
        btRemove = new ToolStripButton("Remove constraint", null, btRemoveClick) { ToolTipText = "Remove constraint" };
        ToolBar1 = new ToolStrip();
        ToolBar1.Items.AddRange([btAdd, btRemove]);
        BoldGrid1 = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersWidth = 24,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        BoldGrid1.Columns.Add("name", "Name");
        BoldGrid1.Columns.Add("body", "Body");
        BoldGrid1.Columns[1].FillWeight = 250;
        mnuCut = new ToolStripMenuItem("Cut", null, mnuCutClick) { ShortcutKeys = Keys.Control | Keys.X };
        mnuCopy = new ToolStripMenuItem("Copy", null, mnuCopyClick) { ShortcutKeys = Keys.Control | Keys.C };
        mnuPaste = new ToolStripMenuItem("Paste", null, mnuPasteClick) { ShortcutKeys = Keys.Control | Keys.V };
        PopupMenu2 = new ContextMenuStrip();
        PopupMenu2.Items.AddRange([mnuCut, mnuCopy, mnuPaste]);
        BoldGrid1.ContextMenuStrip = PopupMenu2;
        Controls.Add(BoldGrid1);
        Controls.Add(ToolBar1);
        BoldGrid1.CellValueChanged += GridCellValueChanged;
        FormClosing += FormClose;
        Deactivate += FormDeactivate;
    }

    /// <summary>The element whose constraints are on show.</summary>
    public TBoldUMLObject? CurrentElement => fCurrentElement;

    /// <summary>The constraints of the element.</summary>
    public List<TBoldUMLObject> Constraints => fCurrentElement?.constraint ?? [];

    /// <summary>Shows the constraint editor for an element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "ShowTheConstraintEditor")]
    public static TfrmBoldUMLConstraintEditor ShowTheConstraintEditor(TBoldUMLObject aUMLElement, Form? Owner)
    {
        if (G_ConstraintEditor is null || G_ConstraintEditor.IsDisposed)
        {
            G_ConstraintEditor = new TfrmBoldUMLConstraintEditor();
            if (Owner is { IsDisposed: false })
            {
                G_ConstraintEditor.Owner = Owner;
            }
        }

        G_ConstraintEditor.InitElements(aUMLElement);
        G_ConstraintEditor.Show();
        return G_ConstraintEditor;
    }

    [Delphi(PortStatus.Ported, Symbol = "EnsureFreeConstraintEditor")]
    public static void EnsureFreeConstraintEditor()
    {
        G_ConstraintEditor?.Dispose();
        G_ConstraintEditor = null;
    }

    /// <summary>Closing hides the form.</summary>
    [Delphi(PortStatus.Ported)]
    private void FormClose(object? Sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void InitElements(TBoldUMLObject CurrentElement)
    {
        fCurrentElement = CurrentElement;
        Text = "Constraints editor - " + CurrentElement.qualifiedName;
        ShowConstraints();
    }

    private void ShowConstraints()
    {
        fShowing = true;
        try
        {
            BoldGrid1.Rows.Clear();
            foreach (TBoldUMLObject constraint in Constraints)
            {
                int row = BoldGrid1.Rows.Add(constraint.name, constraint.body);
                BoldGrid1.Rows[row].Tag = constraint;
            }
        }
        finally
        {
            fShowing = false;
        }
    }

    private void GridCellValueChanged(object? Sender, DataGridViewCellEventArgs e)
    {
        if (fShowing || e.RowIndex < 0 || BoldGrid1.Rows[e.RowIndex].Tag is not TBoldUMLObject constraint)
        {
            return;
        }

        string value = BoldGrid1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
        if (e.ColumnIndex == 0)
        {
            constraint.name = value;
        }
        else
        {
            constraint.body = value;
        }
    }

    /// <summary>Adds a constraint to the element, in the element's namespace (or the element, a namespace itself).</summary>
    [Delphi(PortStatus.Ported)]
    private void btAddClick(object? Sender, EventArgs e)
    {
        if (fCurrentElement is not { } Element)
        {
            return;
        }

        TBoldUMLObject NewConstraint = Element.Graph.Create("Constraint");
        TBoldUMLSupport.EnsureBoldTaggedValues(NewConstraint);
        NewConstraint.AddRef("constrainedElement", Element);
        NewConstraint.namespace_ = Element.IsKindOf("Namespace") ? Element : Element.namespace_;
        NewConstraint.name = TBoldUMLSupport.UniqueName(NewConstraint, "NewConstraint");
        ShowConstraints();
    }

    /// <summary>Removes the constraint chosen.</summary>
    [Delphi(PortStatus.Ported)]
    private void btRemoveClick(object? Sender, EventArgs e)
    {
        if (BoldGrid1.CurrentRow?.Tag is TBoldUMLObject CurrConstr)
        {
            CurrConstr.Delete();
            ShowConstraints();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnuCutClick(object? Sender, EventArgs e) => (BoldGrid1.EditingControl as TextBoxBase)?.Cut();

    [Delphi(PortStatus.Ported)]
    private void mnuCopyClick(object? Sender, EventArgs e)
    {
        if (BoldGrid1.EditingControl is TextBoxBase text)
        {
            text.Copy();
        }
        else if (BoldGrid1.GetClipboardContent() is { } content)
        {
            Clipboard.SetDataObject(content);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnuPasteClick(object? Sender, EventArgs e) => (BoldGrid1.EditingControl as TextBoxBase)?.Paste();

    /// <summary>Leaving the form ends the edit of the cell.</summary>
    [Delphi(PortStatus.Ported)]
    private void FormDeactivate(object? Sender, EventArgs e) => BoldGrid1.EndEdit();
}
