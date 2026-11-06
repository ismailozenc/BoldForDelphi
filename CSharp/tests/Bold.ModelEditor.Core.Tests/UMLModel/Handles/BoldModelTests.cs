// Delphi: Source/UMLModel/Handles/BoldModel.pas, BoldUMLModelStreamer.pas
// Port map: CSharp/port/units/BoldModel.md
using System.ComponentModel;
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldModelTests
{
    private static string[] ClassNames(TBoldUMLObject model) => [.. model.Classes.Select(c => c.name).Order()];

    private static string[] ClassNames(TMoldModel model) => [.. Enumerable.Range(0, model.Classes.Count).Select(i => model.Classes[i].Name).Order()];

    [Test]
    [Covers(typeof(TBoldModel), "Create", nameof(TBoldModel.UMLModel), nameof(TBoldModel.EnsuredUMLModel), "GetUMLModel", "GetEnsuredUMLModel", "EnsureUMLModel",
        "MarkUMLModelExposed", "UMLModelExposed", "SubscribeToUMLModel", nameof(TBoldModel.Boldify), "SetBoldify", nameof(TBoldModel.EditableModel), nameof(TBoldModel.UMLModelMode))]
    [Covers(typeof(TBoldModel))]
    [Covers(typeof(TUMLModelMode))]
    [Covers("BoldModel", "initialization", "finalization")]
    public void The_UML_model_is_made_from_the_Mold_model_when_first_asked_for()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        Assert.That(model.UMLModel, Is.Null);
        Assert.That(model.UMLModelMode, Is.EqualTo(TUMLModelMode.ummNone));
        TBoldUMLObject uml = model.EnsuredUMLModel;
        Assert.That(model.UMLModel, Is.SameAs(uml));
        Assert.That(ClassNames(uml), Is.EqualTo(ClassNames(model.MoldModel)));
        Assert.That(uml.name, Is.EqualTo(model.MoldModel.Name));

        model.Boldify = new TBoldUMLBoldify { DefaultNavigableMultiplicity = "1..*" };
        Assert.That(model.Boldify.DefaultNavigableMultiplicity, Is.EqualTo("1..*"), "the component keeps its own boldify settings");
        model.EditableModel = true;
        Assert.That(model.EditableModel, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldModel), "EnsureMoldModelCurrent", "UMLModelChangedRecieve", "UnSubscribeToUMLModel", "GetUMLModelMoreCurrent", "UMLModelMoreCurrent")]
    public void A_change_to_the_UML_model_makes_the_Mold_model_again()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLObject uml = model.EnsuredUMLModel;
        bool boldified = TBoldUMLBoldify.IsBoldified(uml), flattened = TBoldUMLSupport.IsFlattened(uml);
        int changes = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, _, _) => changes++);
        model.AddSubscription(subscriber, BoldAbstractModel.beModelChanged, BoldAbstractModel.beModelChanged);

        TBoldUMLObject garage = EditorSamples.NewElement(uml, "Class", "Garage");
        garage.persistent = true;
        Assert.That(changes, Is.EqualTo(1));
        EditorSamples.NewAttribute(garage, "size", "Integer");
        Assert.That(changes, Is.EqualTo(1), "the model is changed until its Mold model is made again");

        Assert.That(model.MoldModel.Classes.ItemsByName("Garage"), Is.Not.Null);
        Assert.That(model.MoldModel.Classes.ItemsByName("Garage")!.Attributes.ItemsByName("size"), Is.Not.Null);
        Assert.That(TBoldUMLBoldify.IsBoldified(uml), Is.EqualTo(boldified), "the UML model is left as it was");
        Assert.That(TBoldUMLSupport.IsFlattened(uml), Is.EqualTo(flattened));
        Assert.That(model.MoldModel.TVByName(BOLDINTERALTVPREFIX + TV_MODELERRORS), Is.Empty);

        garage.name = "Shed";
        Assert.That(changes, Is.EqualTo(2), "it hears changes again once the Mold model is current");
        Assert.That(model.MoldModel.Classes.ItemsByName("Shed"), Is.Not.Null);
        subscriber.CancelAllSubscriptions();
    }

    [Test]
    [Covers(typeof(TBoldModel), "EnsureMoldModelCurrent")]
    public void The_errors_of_the_model_are_kept_in_a_tagged_value_of_the_Mold_model()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLObject uml = model.EnsuredUMLModel;
        EditorSamples.NewElement(uml, "Class", "Building");
        string errors = model.MoldModel.TVByName(BOLDINTERALTVPREFIX + TV_MODELERRORS);
        Assert.That(errors, Does.Contain("Duplicate class name"));
        Assert.That(uml.Graph.Violations.Any(v => v.Severity == TSeverity.sError), Is.True, "the violations stay in the UML model");
    }

    [Test]
    [Covers(typeof(TBoldModel), "EnsureMoldModelCurrent", "SubscribeToUMLModel", "UnSubscribeToUMLModel")]
    public void Loading_a_Mold_model_makes_the_UML_model_again()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLObject uml = model.EnsuredUMLModel;
        int changes = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, _, _) => changes++);
        model.AddSubscription(subscriber, BoldAbstractModel.beModelChanged, BoldAbstractModel.beModelChanged);
        TMoldModel other = EditorSamples.Fixture("BankingClasses.Delphi-Simple-ObjectSpace-Transaction.bld");
        model.LoadFromMoldModel(other);
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(model.EnsuredUMLModel, Is.SameAs(uml));
        Assert.That(ClassNames(uml), Is.EqualTo(ClassNames(other)));
        Assert.That(ClassNames(model.MoldModel), Is.EqualTo(ClassNames(other)));
        EditorSamples.NewElement(uml, "Class", "Vault");
        Assert.That(changes, Is.EqualTo(2), "the new UML model is heard");
        subscriber.CancelAllSubscriptions();
    }

    [Test]
    [Covers(typeof(TBoldModel), "UMLModelFromString", "UMLModelToString", "ReadUMLModelAsString", "WriteUMLModelAsString",
        "AssertDesignTime", "DefineProperties")]
    [Covers(typeof(TUMLModelStreamer), "*")]
    public void The_UML_model_is_kept_as_a_string()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        model.UMLModelMode = TUMLModelMode.ummRunTime;
        string kept = model.UMLModelAsString;
        Assert.That(kept, Does.Contain("xmi:XMI").And.Contain("Building"));

        var copy = new TBoldModel(components) { UMLModelMode = TUMLModelMode.ummRunTime, UMLModelAsString = kept };
        Assert.That(ClassNames(copy.EnsuredUMLModel), Is.EqualTo(ClassNames(model.EnsuredUMLModel)));
        Assert.That(ClassNames(copy.MoldModel), Is.EqualTo(ClassNames(model.MoldModel)));

        var designTime = new TBoldModel(components) { UMLModelMode = TUMLModelMode.ummDesignTime, UMLModelAsString = kept };
        Assert.That(() => designTime.EnsuredUMLModel, Throws.InstanceOf<EBold>(), "there is no design time at run time");
        Assert.That(() => TUMLModelStreamer.FillSystemFromString(new TBoldUMLModelGraph().Create("Model"), "<not xml"), Throws.InstanceOf<EBold>());
        TBoldUMLObject read = new TBoldUMLModelGraph().Create("Model");
        TUMLModelStreamer.FillSystemFromString(read, TUMLModelStreamer.SystemAsString(model.EnsuredUMLModel));
        Assert.That(ClassNames(read), Is.EqualTo(ClassNames(model.EnsuredUMLModel)));
    }

    [Test]
    [Covers(typeof(TBoldModel), nameof(TBoldModel.EnsureTypes))]
    public void The_UML_model_gets_a_data_type_for_each_type_name()
    {
        using var components = new Container();
        var model = new TBoldModel(components);
        TBoldUMLObject uml = model.EnsuredUMLModel;
        model.EnsureTypes();
        string[] types = [.. uml.AllOwnedElements.Where(e => e.MetaClass == "DataType").Select(e => e.name)];
        Assert.That(types, Is.SupersetOf(new[] { "String", "Integer", "Float", "Boolean", "DateTime", "Blob" }));
        Assert.That(types, Is.Unique);
        Assert.That(types, Does.Not.Contain(BoldDefs.DEFAULTNAME));
        model.EnsureTypes();
        Assert.That(uml.AllOwnedElements.Count(e => e.MetaClass == "DataType"), Is.EqualTo(types.Length), "a type is added once");
    }

    [Test]
    [Covers(typeof(TBoldModelList), "*")]
    [Covers(typeof(TBoldModel), "Dispose")]
    [Covers(typeof(BoldModelUnit), "*")]
    public void Every_model_component_is_in_the_list_of_models()
    {
        TBoldModelList list = BoldModelUnit.TheModelList();
        bool Listed(TBoldModel m) => Enumerable.Range(0, list.Count).Any(i => list[i] == m);
        var model = new TBoldModel();
        Assert.That(Listed(model), Is.True);
        model.Dispose();
        Assert.That(Listed(model), Is.False);
        Assert.That(BoldModelUnit.TheModelList(), Is.SameAs(list));
    }
}
