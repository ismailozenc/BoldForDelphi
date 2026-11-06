// Delphi: Source/UMLModel/Core/BoldUMLModelUpdater.pas
// Port map: CSharp/port/units/BoldUMLModelUpdater.md
using static Bold.BoldDefaultTaggedValues;
using static Bold.BoldUMLTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelUpdaterTests
{
    [Test]
    [Covers(typeof(TBoldUMLModelUpdater), "*")]
    public void The_old_Persistent_tagged_value_becomes_persistence()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = model.Classes.Single(c => c.name == "Customer");
        TBoldUMLObject name = customer.feature.Single();
        TBoldUMLObject note = EditorSamples.NewAttribute(customer, "note", "String");
        TBoldUMLObject untouched = EditorSamples.NewAttribute(customer, "code", "String");
        name.DeleteTaggedValue(TAG_PERSISTENCE);
        name.SetBoldTV(TV_PERSISTENT_OLD, "True");
        note.SetBoldTV(TV_PERSISTENT_OLD, "False");
        string before = untouched.GetTaggedValue(TAG_PERSISTENCE);

        TBoldUMLModelUpdater.UpdateModel(model);

        Assert.That(name.GetTaggedValue(TAG_PERSISTENCE), Is.EqualTo(TV_PERSISTENCE_PERSISTENT));
        Assert.That(note.GetTaggedValue(TAG_PERSISTENCE), Is.EqualTo(TV_PERSISTENCE_TRANSIENT));
        Assert.That(untouched.GetTaggedValue(TAG_PERSISTENCE), Is.EqualTo(before));
        Assert.That(name.FindTaggedValue(BoldDefs.BOLDTVPREFIX + TV_PERSISTENT_OLD), Is.Null);
        Assert.That(note.FindTaggedValue(BoldDefs.BOLDTVPREFIX + TV_PERSISTENT_OLD), Is.Null);
    }
}
