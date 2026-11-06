// Delphi: Source/UMLModel/Editor/BoldDerivationExpressionsEditor.pas
// Port map: CSharp/port/units/BoldDerivationExpressionsEditor.md

using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>Edits a class's derivation expressions (one "member=expression" a line) in a memo.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code; the memo edits the tagged value's value, written back when the form closes.")]
public class TfrmBoldDerivationExpressionsEditor : Form
{
    public readonly Panel pnMain;
    public readonly Panel pnButtons;
    public readonly Button btClose;
    public readonly TextBox BoldMemo1;
    private readonly TBoldUMLObject fTaggedValue;

    public TfrmBoldDerivationExpressionsEditor(TBoldUMLObject TaggedValue)
    {
        fTaggedValue = TaggedValue;
        Text = "Expressions editor";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(353, 205);
        BoldMemo1 = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, AcceptsReturn = true, WordWrap = false, Text = TaggedValue["value"] };
        pnMain = new Panel { Dock = DockStyle.Fill };
        pnMain.Controls.Add(BoldMemo1);
        btClose = new Button { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(81, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(264, 8) };
        pnButtons = new Panel { Dock = DockStyle.Bottom, Height = 41 };
        pnButtons.Controls.Add(btClose);
        Controls.Add(pnMain);
        Controls.Add(pnButtons);
        AcceptButton = btClose;
        CancelButton = btClose;
        FormClosed += FormClose;
    }

    [Delphi(PortStatus.Ported, Note = "Writes the memo to the tagged value, as Delphi's Bold-aware memo did; WinForms frees the form.")]
    private void FormClose(object? Sender, FormClosedEventArgs e) => fTaggedValue["value"] = BoldMemo1.Text;

    /// <summary>Shows the editor of the tagged value, modally.</summary>
    [Delphi(PortStatus.Ported, Symbol = "ShowDerivationExpressionsEditor")]
    public static void ShowDerivationExpressionsEditor(TBoldUMLObject DerivExprTV)
    {
        using var frmBoldDerivationExpressionsEditor = new TfrmBoldDerivationExpressionsEditor(DerivExprTV);
        frmBoldDerivationExpressionsEditor.ShowDialog();
    }
}
