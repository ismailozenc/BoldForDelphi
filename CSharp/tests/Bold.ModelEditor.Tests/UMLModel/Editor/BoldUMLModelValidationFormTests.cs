// Delphi: Source/UMLModel/Editor/BoldUMLModelValidationForm.pas
// Port map: CSharp/port/units/BoldUMLModelValidationForm.md
using System.ComponentModel;
using System.Windows.Forms;

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLModelValidationFormTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmValidation), ".ctor", nameof(TfrmValidation.EnsureValidationForm), nameof(TfrmValidation.Validate), "BoldGrid1DblClick",
        "BoldPlaceableSubscriber1Receive", nameof(TfrmValidation.ValidationProc), nameof(TfrmValidation.OnElementClick), "FormClose")]
    public void The_validation_form_lists_the_violations_and_shows_their_elements()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        model.EnsuredUMLModel.Classes.Single(c => c.name == "Person").name = "Building"; // two classes of one name
        TBoldUMLObject? clicked = null;
        TfrmValidation form = TfrmValidation.EnsureValidationForm(model, null, (_, element) => clicked = element);
        form.ValidationProc = TBoldUMLModelValidatorCallBack.Validate;
        form.Show();
        form.Validate();
        Assert.That(form.Violations, Is.Not.Empty);
        Assert.That(form.BoldGrid1.Rows.Count, Is.EqualTo(form.Violations.Count));
        Assert.That(form.Violations[0].Severity, Is.EqualTo(form.Violations.Max(v => v.Severity)), "the worst first");
        DataGridViewRow row = form.BoldGrid1.Rows.Cast<DataGridViewRow>().First(r => r.Tag is TBoldUMLViolation { ModelElement: not null });
        EditorForms.Raise(form.BoldGrid1, "OnCellDoubleClick", new DataGridViewCellEventArgs(0, row.Index));
        Assert.That(clicked, Is.SameAs(((TBoldUMLViolation)row.Tag!).ModelElement), "a double click shows the element");
        Assert.That(TfrmValidation.EnsureValidationForm(model, null, null), Is.SameAs(form), "one validation form for the model");
        form.Close();
        Assert.That((form.Visible, form.IsDisposed), Is.EqualTo((false, false)), "closing hides it");
    }
}
