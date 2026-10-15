// Delphi: Source/MoldModel/Handles/BoldAbstractModel.pas, BoldTypeNameHandle.pas
// Port map: CSharp/port/units/BoldAbstractModel.md, CSharp/port/units/BoldTypeNameHandle.md
namespace Bold.Tests;

public class BoldAbstractModelTests
{
    [Test]
    [Covers(typeof(TBoldTypeNameHandle), "*")]
    public void Type_name_handle_has_default_mappings_and_copies_on_set()
    {
        using var handle = new TBoldTypeNameHandle();
        Assert.That(handle.Dictionary.MappingForModelName("Integer"), Is.Not.Null);
        var other = new TBoldTypeNameDictionary(null);
        other.AddMapping().ModelName = "Only";
        handle.Dictionary = other;
        Assert.That(handle.Dictionary, Is.Not.SameAs(other), "the mappings are copied");
        Assert.That(handle.Dictionary.Count, Is.EqualTo(1));
        Assert.That(handle.Dictionary.Mapping(0).ModelName, Is.EqualTo("Only"));
        handle.Dictionary = handle.Dictionary;
        Assert.That(handle.Dictionary.Count, Is.EqualTo(1));
    }

    [Test]
    [Covers(typeof(TBoldAbstractModel), "*")]
    [Covers(typeof(BoldAbstractModel), "*")]
    public void Model_component_keeps_a_raw_and_a_trimmed_model()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldAbstractModel), "Source/MoldModel/Handles/BoldAbstractModel.pas");
        using var model = new TBoldAbstractModel();
        Assert.That(model.MoldModel.Classes.Count, Is.EqualTo(1), "starts with an empty model (its root class)");
        Assert.That(model.TypeNameDictionary.MappingForModelName("String"), Is.Not.Null, "implicit dictionary with defaults");
        Assert.That(model.MoldModel.TypeNameDictionary, Is.SameAs(model.TypeNameDictionary));

        var lines = new TStringList();
        TMoldBLDRW.ModelToStrings(TMoldBLDRW.ModelFromFile(FixturePaths.Model("BuildingsAndOwners.Building.bld")), lines);
        model.ReadModel(lines);
        Assert.That(model.RawMoldModel, Is.Not.SameAs(model.MoldModel));
        Assert.That(model.MoldModel.Classes.Count, Is.EqualTo(model.RawMoldModel.Classes.Count));
        var written = new TStringList();
        model.WriteModel(written);
        Assert.That(written.ToStringArray(), Is.EqualTo(lines.ToStringArray()));

        using var handle = new TBoldTypeNameHandle();
        model.TypeNameHandle = handle;
        Assert.That(model.TypeNameDictionary, Is.SameAs(handle.Dictionary));
        Assert.That(model.MoldModel.TypeNameDictionary, Is.SameAs(handle.Dictionary));
        Assert.That(model.RawMoldModel.TypeNameDictionary, Is.SameAs(handle.Dictionary));
        model.TypeNameHandle = null;
        Assert.That(model.TypeNameDictionary, Is.Not.SameAs(handle.Dictionary));
        model.UpdateDesigner();

        string className = model.MoldModel.Classes[1].Name;
        string attributeLine = Enumerable.Range(0, model.MoldModel.Classes.Count).Select(i => model.MoldModel.Classes[i])
            .Where(c => c.Attributes.Count > 0).Select(c => c.Name + "." + c.Attributes[0].Name).First();
        string association = model.MoldModel.Associations[0].Name;
        string file = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(file, [className, attributeLine, association, "NoSuchThing"]);
            model.ToBeRemovedInfoFileName = "  " + file + "  ";
            Assert.That(model.ToBeRemovedInfoFileName, Is.EqualTo(file));
            model.ReadModel(lines);
            Assert.That(model.MoldModel.Classes.ItemsByName(className)!.EvolutionState, Is.EqualTo(TBoldEvolutionState.esToBeRemoved));
            Assert.That(model.MoldModel.Associations.ItemsByName(association)!.EvolutionState, Is.EqualTo(TBoldEvolutionState.esToBeRemoved));
            Assert.That(model.RawMoldModel.Classes.ItemsByName(className)!.EvolutionState, Is.EqualTo(TBoldEvolutionState.esNormal), "only the working model");
            string outFile = file + ".out";
            model.WriteToBeRemovedInfoToFile(outFile);
            Assert.That(File.ReadAllLines(outFile), Is.EquivalentTo(new[] { className, attributeLine, association }));
            File.Delete(outFile);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
