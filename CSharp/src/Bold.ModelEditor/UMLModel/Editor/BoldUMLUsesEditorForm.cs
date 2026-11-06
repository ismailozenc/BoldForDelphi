// Delphi: Source/UMLModel/Editor/BoldUMLUsesEditorForm.pas
// Port map: CSharp/port/units/BoldUMLUsesEditorForm.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLUsesEditorForm", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>Edits a list of units (a model's interface or implementation uses) in a memo.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code; the memo edits a tagged value of the element, where Delphi's showed an expression of it.")]
public class TfrmUsesEditor : Form
{
    public readonly Panel pnButtons1;
    public readonly Panel pnButtons2;
    public readonly Button btnOK;
    public readonly Button btnCancel;
    public readonly StatusStrip StatusBar1;
    public readonly Panel pnText;
    public readonly TextBox BoldMemo1;
    public readonly ContextMenuStrip PopupMenu1;
    public readonly ToolStripMenuItem mnuCut;
    public readonly ToolStripMenuItem mnuCopy;
    public readonly ToolStripMenuItem mnuPaste;
    private readonly TBoldUMLObject fRoot;
    private readonly string fTag;

    [Delphi(PortStatus.Ported, Symbol = "TfrmUsesEditor.CreateEditorWithParams", Note = "Takes the element and the tag of the value to edit; Show it after.")]
    public TfrmUsesEditor(string Title, TBoldUMLObject Root, string Tag)
    {
        fRoot = Root;
        fTag = Tag;
        Text = Title;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(227, 210);
        BoldMemo1 = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, AcceptsReturn = true, Text = Root.GetTaggedValue(Tag) };
        pnText = new Panel { Dock = DockStyle.Fill };
        pnText.Controls.Add(BoldMemo1);
        btnOK = new Button { Text = "OK", Location = new Point(10, 8), Size = new Size(81, 25) };
        btnCancel = new Button { Text = "Cancel", Location = new Point(97, 8), Size = new Size(81, 25) };
        pnButtons2 = new Panel { Dock = DockStyle.Right, Width = 185 };
        pnButtons2.Controls.AddRange([btnOK, btnCancel]);
        pnButtons1 = new Panel { Dock = DockStyle.Bottom, Height = 41 };
        pnButtons1.Controls.Add(pnButtons2);
        StatusBar1 = new StatusStrip();
        mnuCut = new ToolStripMenuItem("Cut", null, mnuCutClick) { ShortcutKeys = Keys.Control | Keys.X };
        mnuCopy = new ToolStripMenuItem("Copy", null, mnuCopyClick) { ShortcutKeys = Keys.Control | Keys.C };
        mnuPaste = new ToolStripMenuItem("Paste", null, mnuPasteClick) { ShortcutKeys = Keys.Control | Keys.V };
        PopupMenu1 = new ContextMenuStrip();
        PopupMenu1.Items.AddRange([mnuCut, mnuCopy, mnuPaste]);
        BoldMemo1.ContextMenuStrip = PopupMenu1;
        Controls.Add(pnText);
        Controls.Add(pnButtons1);
        Controls.Add(StatusBar1);
        AcceptButton = btnOK;
        CancelButton = btnCancel;
        btnOK.Click += btnOKClick;
        btnCancel.Click += btnCancelClick;
        FormClosed += FormClose;
    }

    [Delphi(PortStatus.Ported)]
    private void btnCancelClick(object? Sender, EventArgs e) => Close();

    [Delphi(PortStatus.Ported, Note = "WinForms frees a closed modeless form.")]
    private void FormClose(object? Sender, FormClosedEventArgs e)
    {
    }

    /// <summary>Writes the units back to the element and closes.</summary>
    [Delphi(PortStatus.Ported)]
    private void btnOKClick(object? Sender, EventArgs e)
    {
        fRoot.SetTaggedValue(fTag, BoldMemo1.Text);
        Close();
    }

    [Delphi(PortStatus.Ported)]
    private void mnuCutClick(object? Sender, EventArgs e) => BoldMemo1.Cut();

    [Delphi(PortStatus.Ported)]
    private void mnuCopyClick(object? Sender, EventArgs e) => BoldMemo1.Copy();

    [Delphi(PortStatus.Ported)]
    private void mnuPasteClick(object? Sender, EventArgs e) => BoldMemo1.Paste();
}
