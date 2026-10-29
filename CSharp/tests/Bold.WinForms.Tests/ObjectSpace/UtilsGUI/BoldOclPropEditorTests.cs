// Delphi: Source/ObjectSpace/UtilsGUI/BoldOclPropEditor.pas, BoldOCLGraphicRTDebug.pas
// Port map: CSharp/port/units/BoldOclPropEditor.md
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>The OCL expression editor and the run-time OCL debugger, on the Building model.</summary>
[NonParallelizable]
public class BoldOclPropEditorTests
{
    private static string[] Items(TBoldOclPropEditForm form) => [.. form.SelectBox.Items.Cast<object>().Select(i => i.ToString()!)];

    [Test]
    [Covers(typeof(TBoldOclPropEditForm), nameof(TBoldOclPropEditForm.Context), nameof(TBoldOclPropEditForm.OclExpr), nameof(TBoldOclPropEditForm.SystemTypeInfo),
        nameof(TBoldOclPropEditForm.OclEvaluator), nameof(TBoldOclPropEditForm.Variables), "SetContext", "SetOclExpr", "GetOclExpr", "CheckOcl", "MemberHelp",
        "OperationHelp", "CleanParserMsg", "GetOclEvaluator", "SetVariables", "EditMemoChange", "EditMemoEnter", "SelectBoxDblClick", "SelectBoxKeyPress",
        "ClearClick", "RemoveLastClick", "SyntaxcbClick", "TypescbClick", "SetSelection", "EditMemoMouseDown", "EditMemoKeyPress", "EditMemoKeyUp",
        "OKClick", "CancelClick", "SelectBoxDrawItem", "SelectBoxKeyDown", "Copymessagestoclipboard1Click", "Dispose")]
    public void The_editor_checks_the_expression_and_offers_members_and_operations()
    {
        using var s = new GuiSample();
        TBoldClassTypeInfo person = s.SystemHandle.StaticSystemTypeInfo!.ClassTypeInfoByExpressionName("Person")!;
        var form = new TBoldOclPropEditForm();
        BoldAFPDefaultTests.ShowOffScreen(form);
        form.Context = person;
        Assert.That(form.Text, Does.EndWith("Person"));
        Assert.That(form.SystemTypeInfo, Is.SameAs(person.SystemTypeInfo));
        Assert.That(form.ParserMessages.Text, Does.Contain(BoldCoreConsts.sEmptyExpression));
        Assert.That(Items(form), Does.Contain("M | firstName "), "the members of the context");
        Assert.That(Items(form), Does.Contain("A | home "));

        form.OclExpr = "firstName";
        Assert.That(form.OclExpr, Is.EqualTo("firstName"));
        Assert.That(form.ParserMessages.Text, Does.Contain(BoldCoreConsts.sSyntaxOK).And.Contain("String"));
        Assert.That(Items(form), Has.Some.StartsWith("O | .").And.Some.Contains("«"), "operations with argument placeholders");

        Modal.Raise(form.Typescb, "OnClick", EventArgs.Empty);
        Assert.That(form.ShowTypes, Is.True);

        form.Clear.PerformClick();
        Assert.That(form.EditMemo.Text, Is.Empty);
        Assert.That(Items(form), Does.Contain("M | firstName: String"), "with types");
        form.SelectBox.SelectedIndex = Items(form).ToList().IndexOf("A | home: Residential_Building");
        Modal.Raise(form.SelectBox, "OnDoubleClick", EventArgs.Empty);
        Assert.That(form.EditMemo.Text, Is.EqualTo("home"));
        form.SelectBox.SelectedIndex = Items(form).ToList().IndexOf("M | .address: String");
        Modal.Raise(form.SelectBox, "OnKeyPress", new KeyPressEventArgs('\r'));
        Assert.That(form.EditMemo.Text, Is.EqualTo("home.address"));

        form.RemoveLast.PerformClick();
        Assert.That(form.EditMemo.Text, Is.EqualTo("home"));

        form.EditMemo.Text = "firstName.nonsense";
        Assert.That(form.ParserMessages.Lines, Has.Some.Contains("-------"), "the error is pointed at");
        Modal.Raise(form.Syntaxcb, "OnClick", EventArgs.Empty);
        Assert.That(form.ShowSyntaxErrors, Is.True);
        form.EditMemo.Text = "firstName->";
        Assert.That(form.ParserMessages.Text, Is.Not.Empty);

        form.EditMemo.Text = "x->select(«Boolean»)";
        form.EditMemo.SelectionStart = 13;
        form.EditMemo.SelectionLength = 0;
        Modal.Raise(form.EditMemo, "OnKeyUp", new KeyEventArgs(Keys.Right));
        Assert.That(form.EditMemo.SelectedText, Is.EqualTo("«Boolean»"), "the caret in a placeholder selects it");
        Modal.Raise(form.EditMemo, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
        Modal.Raise(form.EditMemo, "OnEnter", EventArgs.Empty);

        form.Variables = null;
        Assert.That(form.Variables, Is.Null);
        Assert.That(form.OclEvaluator, Is.Not.Null);

        form.EditMemo.Text = "";
        form.SelectBox.SelectedIndex = 0;
        Modal.Raise(form.SelectBox, "OnKeyDown", new KeyEventArgs(Keys.F1));
        Assert.That(form.SelectBoxHelpContext, Is.EqualTo(0), "a member has no help context");
        form.SelectBox.Refresh();
        form.Copymessagestoclipboard1.PerformClick();
        Assert.That(Clipboard.GetText(), Does.StartWith("Context: Person"));

        Modal.Raise(form.EditMemo, "OnKeyPress", new KeyPressEventArgs('\u001B'));
        Assert.That(form.DialogResult, Is.EqualTo(DialogResult.Cancel));
        form.btnOK.PerformClick();
        Assert.That(form.DialogResult, Is.EqualTo(DialogResult.OK));
        form.btnCancel.PerformClick();
        Assert.That(form.DialogResult, Is.EqualTo(DialogResult.Cancel));
        form.Dispose();

        using var noContext = new TBoldOclPropEditForm();
        noContext.Context = null;
        Assert.That(noContext.ParserMessages.Text, Does.Contain(BoldCoreConsts.sNoContextNoSupport));
    }

    [Test]
    [Covers(typeof(TBoldOclGraphicRTDebugger), "*")]
    public void The_debugger_keeps_the_user_s_fix_of_an_expression()
    {
        using var s = new GuiSample();
        TBoldClassTypeInfo person = s.SystemHandle.StaticSystemTypeInfo!.ClassTypeInfoByExpressionName("Person")!;
        using var debugger = new TBoldOclGraphicRTDebugger();
        Assert.That(debugger.HasFixFor("fristName", person), Is.False);
        Assert.That(debugger.GetFixFor("fristName", person), Is.EqualTo("fristName"));

        debugger.ShowFixForm = form =>
        {
            Assert.That(form.OclExpr, Is.EqualTo("fristName"));
            Assert.That(form.Text, Is.EqualTo(BoldCoreConsts.sIllegalExpressionEncountered));
            form.EditMemo.Text = "firstName";
            return DialogResult.OK;
        };
        Assert.That(debugger.AddFixFor("fristName", person, "Form1.Edit1", "no such member"), Is.True);
        Assert.That(s.Environment.Dialogs, Has.Some.Contains("fristName"), "the user is told");
        Assert.That(BoldOcl.BoldOCLRTDebugger, Is.SameAs(debugger));
        Assert.That(debugger.HasFixFor("fristName", person), Is.True);
        Assert.That(debugger.GetFixFor("fristName", person), Is.EqualTo("firstName"));
        Assert.That(debugger.HasFixFor("fristName", null), Is.False, "fixes are for the context");

        debugger.ShowFixForm = form =>
        {
            form.EditMemo.Text = "lastName";
            return DialogResult.OK;
        };
        Assert.That(debugger.AddFixFor("fristName", person, "Form1.Edit1", "again"), Is.True);
        Assert.That(debugger.GetFixFor("fristName", person), Is.EqualTo("lastName"), "a new fix replaces the old");

        debugger.ShowFixForm = _ => DialogResult.Cancel;
        Assert.That(debugger.AddFixFor("other", null, "", ""), Is.False);
        BoldOcl.BoldOCLRTDebugger = null;
    }
}
