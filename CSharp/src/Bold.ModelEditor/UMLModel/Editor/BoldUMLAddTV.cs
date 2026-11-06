// Delphi: Source/UMLModel/Editor/BoldUMLAddTV.pas
// Port map: CSharp/port/units/BoldUMLAddTV.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLAddTV", Symbol = "frmBoldUMLAddTV", Note = "The form variable of Delphi's auto-created form; ShowAddTVDialog makes the form it shows.")]

namespace Bold;

/// <summary>The dialog that adds a tagged value: its tool, name and value.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code where Delphi's form came from its .dfm.")]
public class TfrmBoldUMLAddTV : Form
{
    public readonly GroupBox GroupBox1;
    public readonly Label Label1;
    public readonly Label Label2;
    public readonly Label Label3;
    public readonly TextBox edTool;
    public readonly TextBox edValue;
    public readonly TextBox edName;
    public readonly Button btOK;
    public readonly Button btCancel;

    public TfrmBoldUMLAddTV()
    {
        Text = "Bold UML tagged values editor";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(272, 150);
        GroupBox1 = new GroupBox { Text = " Add tagged value ", Location = new Point(8, 8), Size = new Size(256, 100) };
        Label1 = new Label { Text = "Tool", Location = new Point(8, 22), AutoSize = true };
        Label3 = new Label { Text = "Name", Location = new Point(8, 46), AutoSize = true };
        Label2 = new Label { Text = "Value", Location = new Point(8, 70), AutoSize = true };
        edTool = new TextBox { Location = new Point(64, 18), Width = 182 };
        edName = new TextBox { Location = new Point(64, 42), Width = 182 };
        edValue = new TextBox { Location = new Point(64, 66), Width = 182 };
        GroupBox1.Controls.AddRange([Label1, Label3, Label2, edTool, edName, edValue]);
        btOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(102, 116), Size = new Size(81, 25) };
        btCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(185, 116), Size = new Size(81, 25) };
        Controls.AddRange([GroupBox1, btOK, btCancel]);
        AcceptButton = btOK;
        CancelButton = btCancel;
        FormClosed += FormClose;
        Shown += FormShow;
    }

    /// <summary>What the user typed, kept as the dialog closes.</summary>
    public (string Tool, string TagName, string Value) Result { get; private set; }

    [Delphi(PortStatus.Ported, Note = "Keeps what was typed in Result, where Delphi kept it in unit variables.")]
    private void FormClose(object? Sender, FormClosedEventArgs e) => Result = (edTool.Text, edName.Text, edValue.Text);

    [Delphi(PortStatus.Ported)]
    private void FormShow(object? Sender, EventArgs e) => edName.Focus();

    /// <summary>The tag of a tool and a name: "Tool.Name", or the name alone for the default tool.</summary>
    public static string TagOf(string Tool, string TagName)
    {
        if (Tool == BoldDefs.DEFAULTNAME)
        {
            Tool = "";
        }

        return Tool == "" ? TagName : Tool + "." + TagName;
    }

    /// <summary>Asks for a tagged value and gives it to the element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "ShowAddTVDialog")]
    public static void ShowAddTVDialog(string SuggestedTool, TBoldUMLObject Owner)
    {
        using var frmBoldUMLAddTV = new TfrmBoldUMLAddTV();
        frmBoldUMLAddTV.edTool.Text = SuggestedTool;
        if (frmBoldUMLAddTV.ShowDialog() == DialogResult.OK)
        {
            (string Tool, string TagName, string Value) = frmBoldUMLAddTV.Result;
            Owner.SetTaggedValue(TagOf(Tool, TagName), Value);
        }
    }
}
