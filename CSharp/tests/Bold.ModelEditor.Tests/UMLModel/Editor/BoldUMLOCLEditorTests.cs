// Delphi: Source/UMLModel/Editor/BoldUMLOCLEditor.pas
// Port map: CSharp/port/units/BoldUMLOCLEditor.md
using System.ComponentModel;

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLOCLEditorTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TBoldUMLOclEditor), nameof(TBoldUMLOclEditor.BoldUMLOclEditor_), ".ctor")]
    public void An_expression_is_on_its_class_or_on_the_type_of_its_attribute()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLObject person = model.EnsuredUMLModel.Classes.Single(c => c.name == "Person");
        TBoldUMLOclEditor editor = TBoldUMLOclEditor.BoldUMLOclEditor_();
        Assert.That(TBoldUMLOclEditor.BoldUMLOclEditor_(), Is.SameAs(editor));
        Assert.That(editor.ContextType(model, person)?.ExpressionName, Is.EqualTo("Person"));
        Assert.That(editor.ContextType(model, person.feature.Single(f => f.name == "FirstName"))?.ExpressionName, Is.EqualTo("String"));
        Assert.That(() => editor.ContextType(model, model.EnsuredUMLModel), Throws.InstanceOf<EBold>().With.Message.Contains("Unsupported context type"));
    }
}
