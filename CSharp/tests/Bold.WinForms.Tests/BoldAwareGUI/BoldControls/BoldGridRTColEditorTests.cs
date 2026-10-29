// Delphi: Source/BoldAwareGUI/BoldControls/BoldGridRTColEditor.pas
// Port map: CSharp/port/units/BoldGridRTColEditor.md
using System.Windows.Forms;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>Editing a grid's columns while the program runs.</summary>
[NonParallelizable]
public class BoldGridRTColEditorTests
{
    [Test]
    [Covers(typeof(TfrmRTColEditor), "*")]
    public void The_column_editor_changes_adds_and_deletes_columns()
    {
        using var s = new GuiSample();
        s.NewPerson("Ann", "Smith");
        var grid = new TBoldGrid();
        using var host = new TestForm(grid);
        grid.BoldHandle = s.List("Person.allInstances");
        grid.AddColumn();
        grid.Columns[1].BoldProperties.Expression = "firstName";
        grid.Columns[1].Title.Caption = "First";
        GuiSample.Display();
        int columns = grid.ColCount;
        bool ran = false;

        using var editor = new TfrmRTColEditor();
        Assert.Throws<EBold>(() => editor.Execute(null));
        editor.ShowModalForm = f =>
        {
            if (f is TBoldOclPropEditForm ocl)
            {
                Assert.That(ocl.Context!.ExpressionName, Is.EqualTo("Person"));
                Assert.That(ocl.OclExpr, Is.EqualTo("firstName"));
                ocl.EditMemo.Text = "lastName";
                return DialogResult.OK;
            }

            var e = (TfrmRTColEditor)f;
            BoldAFPDefaultTests.ShowOffScreen(e);
            Assert.That(e.Grid, Is.SameAs(grid));
            Assert.That(e.EGrid, Is.SameAs(grid));
            Assert.That(e.lbxColumns.Items.Count, Is.EqualTo(columns - 1), "every column but the fixed one");
            Assert.That(e.cbxRenderer.CheckState, Is.EqualTo(CheckState.Indeterminate), "no column");
            var typed = new KeyPressEventArgs('a');
            Modal.Raise(e.tbxCaption, "OnKeyPress", typed);
            Assert.That(typed.Handled, Is.True, "nothing to type into");

            e.lbxColumns.SelectedIndex = 0;
            Modal.Raise(e.lbxColumns, "OnClick", EventArgs.Empty);
            Assert.That(e.CurrentGridColumn, Is.SameAs(grid.Columns[1]));
            Assert.That(e.tbxCaption.Text, Is.EqualTo("First"));
            Assert.That(e.tbxExpression.Text, Is.EqualTo("firstName"));
            Assert.That(e.cbxRenderer.Checked, Is.False);
            Assert.That(e.cmdApply.Enabled, Is.False);

            e.tbxCaption.Text = "Last";
            Assert.That(e.cmdApply.Enabled, Is.True);
            e.cmdOCLEditor.PerformClick();
            Assert.That(e.tbxExpression.Text, Is.EqualTo("lastName"));
            e.cmdApply.PerformClick();
            Assert.That(grid.Columns[1].Title.Caption, Is.EqualTo("Last"));
            Assert.That(grid.Columns[1].BoldProperties.Expression, Is.EqualTo("lastName"));
            Assert.That(e.lbxColumns.Items[0]!.ToString(), Is.EqualTo("Last"));

            e.cmdAddColumn.PerformClick();
            Assert.That(grid.ColCount, Is.EqualTo(columns + 1));
            Assert.That(e.lbxColumns.Items[^1]!.ToString(), Is.EqualTo(BoldGuiResourceStrings.sNewColumn));
            e.lbxColumns.SelectedIndex = e.lbxColumns.Items.Count - 1;
            Modal.Raise(e.lbxColumns, "OnClick", EventArgs.Empty);
            e.cmdDeleteColumn.PerformClick();
            Assert.That(grid.ColCount, Is.EqualTo(columns));
            Assert.That(e.CurrentGridColumn, Is.Null);

            grid.Columns[1].BoldProperties.Renderer = new TBoldAsStringRenderer { OnSetColor = (TBoldElement? _, ref System.Drawing.Color _, int _, string _) => { } };
            e.lbxColumns.SelectedIndex = 0;
            Modal.Raise(e.lbxColumns, "OnClick", EventArgs.Empty);
            Assert.That(e.cbxRenderer.Checked, Is.True);
            Assert.That(e.cbxSetColor.Checked, Is.True);
            Assert.That(e.cbxSetFont.Checked, Is.False);
            Assert.That(e.cbxSetText.Checked, Is.False);
            e.Hide();
            ran = true;
            return DialogResult.OK;
        };
        editor.Execute(grid);
        Assert.That(ran, Is.True);
    }
}
