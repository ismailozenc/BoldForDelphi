// Delphi: Source/UMLModel/Core/BoldUMLModelConverter.pas, UMLConsts.pas
// Port map: CSharp/port/units/BoldUMLModelConverter.md
namespace Bold.Tests;

public class BoldUMLModelConverterTests
{
    [Test]
    [Covers(typeof(TBoldModelConverter), "*")]
    [Covers(typeof(UMLConsts), "*")]
    public void A_Mold_model_converted_to_UML_and_back_is_the_same_model()
    {
        foreach (string file in new[] { "BuildingsAndOwners.Building.bld", "AssociationClassExampleClasses.Delphi-Simple-ObjectSpace-AssociationClass.bld", "BoldUMLModel.bld" })
        {
            TMoldModel original = XmiSamples.LoadFixture(file);
            TBoldUMLObject uml = TBoldModelConverter.MoldModelToUML(original);
            TMoldModel back = TBoldModelConverter.UMLModelToMold(uml);
            // the UML model names its root class in a tagged value, as Delphi's did
            Assert.That(back.TVByName("Bold.RootClass"), Is.EqualTo(original.RootClass.Name), file);
            Assert.That(ModelDump.Of(back).Where(l => !l.StartsWith("model TV Bold.RootClass=")),
                Is.EqualTo(ModelDump.Of(original).Where(l => !l.StartsWith("model TV Bold.RootClass="))), file);
        }
    }

    [Test]
    public void The_UML_model_has_the_Mold_model_s_elements()
    {
        TMoldModel original = XmiSamples.LoadFixture("BuildingsAndOwners.Building.bld");
        TBoldUMLObject uml = TBoldModelConverter.MoldModelToUML(original);
        TBoldUMLObject person = uml.Classes.Single(c => c.name == "Person");
        Assert.That(person.feature.Where(f => f.MetaClass == "Attribute").Select(f => f.name), Does.Contain("FirstName"));
        Assert.That(person.Superclass!.name, Is.EqualTo("BusinessClassesRoot"));
        TBoldUMLObject ownership = uml.Associations.Single(a => a.name == "Ownership");
        Assert.That(ownership.connection.Select(e => e.type_!.name), Is.EquivalentTo(new[] { "Person", "Building" }));
        Assert.That(ownership.class_!.name, Is.EqualTo("Ownership"), "the link class");
        Assert.That(TBoldUMLBoldify.IsBoldified(uml), Is.True, "the Mold model was boldified");

        // a second conversion into the same UML model replaces what it had
        TBoldModelConverter.MoldModelToUML(original, uml);
        Assert.That(uml.Classes.Count(c => c.name == "Person"), Is.EqualTo(1));
    }
}
