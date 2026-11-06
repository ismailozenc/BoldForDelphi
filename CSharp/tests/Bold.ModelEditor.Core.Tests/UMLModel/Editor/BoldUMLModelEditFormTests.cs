// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
using System.ComponentModel;
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelEditFormTests
{
    private Container fComponents = null!;
    private TBoldModel fModel = null!;
    private TBoldModelEditController fEditor = null!;

    [SetUp]
    public void SetUp()
    {
        fComponents = new Container();
        fModel = EditorSamples.ModelComponent(fComponents);
        fEditor = new TBoldModelEditController { ModelHandle = fModel };
    }

    [TearDown]
    public void TearDown()
    {
        fEditor.Dispose();
        fComponents.Dispose();
    }

    private TBoldUMLObject Model => fEditor.CurrentModel!;

    private TBoldUMLObject Class(string Name) => Model.Classes.Single(c => c.name == Name);

    private TBoldUMLObject Feature(string Class_, string Name) => this.Class(Class_).feature.Single(f => f.name == Name);

    private TBoldUMLObject Show(TBoldUMLObject Element)
    {
        fEditor.CurrentElement = Element;
        return Element;
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.ModelHandle), "SetModelHandle", nameof(TBoldModelEditController.CurrentModel),
        "GetCurrentModel", nameof(TBoldModelEditController.GetCurrentModelHandle), nameof(TBoldModelEditController.CurrentTypeNameDictionary),
        "GetCurrentTypeNameDictionary", nameof(TBoldModelEditController.EnsureTypesFromTypeNameHandle), nameof(TBoldModelEditController.StatusText))]
    public void The_model_is_on_show_and_needs_validating()
    {
        Assert.That(fEditor.CurrentModel, Is.SameAs(fModel.EnsuredUMLModel));
        Assert.That(fEditor.GetCurrentModelHandle(), Is.SameAs(fModel));
        Assert.That(fEditor.CurrentElement, Is.SameAs(fEditor.CurrentModel));
        Assert.That(fEditor.CurrentPage, Is.EqualTo(TBoldModelEditPage.Model));
        Assert.That(fEditor.ModelNeedsValidation, Is.True);
        Assert.That(fEditor.StatusText(), Is.EqualTo(("Model not validated", TSeverity.sNone)));
        Assert.That(fEditor.CurrentTypeNameDictionary, Is.SameAs(fModel.TypeNameDictionary));
        Assert.That(Model.AllOwnedElements.Where(e => e.IsKindOf("DataType")).Select(e => e.name), Does.Contain("Currency"), "the types of the type name dictionary");
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.CurrentElement), "SetCurrentElement", nameof(TBoldModelEditController.GetCurrentElement),
        nameof(TBoldModelEditController.GetCurrentClass), nameof(TBoldModelEditController.GetCurrentPackage))]
    public void The_page_follows_the_kind_of_element_on_show()
    {
        TBoldUMLObject person = Class("Person");
        int changes = 0;
        fEditor.CurrentElementChanged += () => changes++;
        Assert.That(Show(person) == person && fEditor.CurrentPage == TBoldModelEditPage.Class, Is.True);
        Assert.That(changes, Is.EqualTo(1));
        TBoldUMLObject attribute = person.feature.First(f => f.IsKindOf("Attribute"));
        Show(attribute);
        Assert.That((fEditor.CurrentPage, fEditor.GetCurrentClass(), fEditor.GetCurrentPackage()), Is.EqualTo((TBoldModelEditPage.Attribute, person, person.namespace_)));
        TBoldUMLObject end = Model.Associations.First().connection[0];
        Assert.That(TBoldModelEditController.PageFor(end), Is.EqualTo(TBoldModelEditPage.AssociationEnd));
        Assert.That(TBoldModelEditController.PageFor(end.association), Is.EqualTo(TBoldModelEditPage.Association));
        TBoldUMLObject qualifier = Model.Graph.Create("Attribute");
        end.AddRef("qualifier", qualifier);
        Assert.That(TBoldModelEditController.PageFor(qualifier), Is.EqualTo(TBoldModelEditPage.Qualifier));
        TBoldUMLObject operation = person.feature.FirstOrDefault(f => f.IsKindOf("Operation")) ?? person.Graph.Create("Operation");
        operation.owner ??= person;
        Assert.That(TBoldModelEditController.PageFor(operation), Is.EqualTo(TBoldModelEditPage.Operation));
        TBoldUMLObject parameter = operation.Graph.Create("Parameter");
        operation.AddRef("parameter", parameter);
        Assert.That(TBoldModelEditController.PageFor(parameter), Is.EqualTo(TBoldModelEditPage.Parameter));
        Assert.That(TBoldModelEditController.PageFor(EditorSamples.NewElement(Model, "Package", "P")), Is.EqualTo(TBoldModelEditPage.Package));
        Assert.That(TBoldModelEditController.PageFor(Model.AllOwnedElements.First(e => e.IsKindOf("DataType"))), Is.EqualTo(TBoldModelEditPage.DataType));
        Assert.That(TBoldModelEditController.PageFor(null), Is.EqualTo(TBoldModelEditPage.None));
        Show(Model);
        Assert.That((fEditor.GetCurrentClass(), fEditor.GetCurrentPackage()), Is.EqualTo(((TBoldUMLObject?)null, Model)));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Commands))]
    public void The_menu_offers_what_fits_the_element_on_show()
    {
        Show(Model);
        TBoldModelEditCommands commands = fEditor.Commands();
        Assert.That(commands.DeleteXxx, Is.Null, "the model is not deleted");
        Assert.That(commands.NewClass && commands.NewAssociation && commands.NewDatatype && commands.NewPackage && !commands.NewAttribute, Is.True);
        Show(EditorSamples.NewElement(Model, "Package", "Sales"));
        Assert.That(fEditor.Commands().DeleteXxx, Is.EqualTo("Delete Package Sales"));

        TBoldUMLObject person = Show(Class("Person"));
        commands = fEditor.Commands();
        Assert.That(commands.DeleteXxx, Is.EqualTo("Delete Class Person"));
        Assert.That(commands.InsertSuperclass && commands.AddSubclass && commands.NewAttribute && commands.NewOperation && commands.OverrideFrameworkMethods, Is.True);
        Assert.That(commands.NewAssociation || commands.NewParameter, Is.False);

        TBoldUMLObject operation = Show(fEditor.NewOperation());
        Assert.That(fEditor.Commands().NewParameter, Is.True);
        Assert.That(fEditor.Commands().OverrideInAllSubclasses, Is.False, "not virtual");
        operation.SetBoldTV(TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_VIRTUAL);
        Assert.That(fEditor.Commands().OverrideInAllSubclasses, Is.EqualTo(person.SubClasses.Count > 0));

        TBoldUMLObject end = Show(Model.Associations.First().connection[0]);
        Assert.That((fEditor.Commands().DeleteXxx, fEditor.Commands().NewQualifier), Is.EqualTo(((string?)null, true)));
        Show(end.association!);
        Assert.That(fEditor.Commands().DeleteXxx, Does.StartWith("Delete Association "));
        Show(end);
        TBoldUMLObject qualifier = Show(fEditor.NewQualifier());
        Assert.That(fEditor.Commands().NewAttribute, Is.False, "a qualifier has no class");
        Assert.That(fEditor.Commands().DeleteXxx, Is.EqualTo("Delete Attribute " + qualifier.name));
        fEditor.CurrentElement = null;
        Assert.That(fEditor.Commands().DeleteXxx, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.NewClass), nameof(TBoldModelEditController.NewAttribute), nameof(TBoldModelEditController.NewOperation),
        nameof(TBoldModelEditController.NewParameter), nameof(TBoldModelEditController.NewQualifier), nameof(TBoldModelEditController.NewAssociation),
        nameof(TBoldModelEditController.NewDatatype), nameof(TBoldModelEditController.NewPackage), nameof(TBoldModelEditController.InsertSuperclass),
        nameof(TBoldModelEditController.AddSubclass), nameof(TBoldModelEditController.CurrentModelIsBoldified), "GetCurrentModelIsBoldified")]
    public void New_elements_go_where_they_belong_with_names_of_their_own()
    {
        Assert.That(fEditor.CurrentModelIsBoldified, Is.True);
        TBoldUMLObject root = TBoldUMLBoldify.GetRootClass(Model)!;
        Show(Model);
        TBoldUMLObject first = fEditor.NewClass(), second = fEditor.NewClass();
        Assert.That((first.name, second.name), Is.EqualTo(("NewClass", "NewClass_1")));
        Assert.That(first.namespace_, Is.SameAs(Model));
        Assert.That((first.Superclass, first.persistent), Is.EqualTo((root, true)));

        Show(first);
        TBoldUMLObject attribute = fEditor.NewAttribute();
        Assert.That((attribute.owner, attribute.name, attribute.typeName, attribute.persistent), Is.EqualTo((first, "NewAttribute", "String", true)));
        TBoldUMLObject operation = fEditor.NewOperation();
        Assert.That((operation.owner, operation.name), Is.EqualTo((first, "NewOperation")));
        Show(operation);
        TBoldUMLObject parameter = fEditor.NewParameter();
        Assert.That((parameter.Ref("behavioralFeature"), parameter.name), Is.EqualTo((operation, "NewParameter")));
        Show(attribute);
        Assert.That(fEditor.NewClass().namespace_, Is.SameAs(Model), "beside the attribute's class");

        Show(Model);
        TBoldUMLObject association = fEditor.NewAssociation();
        Assert.That((association.name, association.persistent, association.namespace_), Is.EqualTo(("NewAssociation", true, Model)));
        Assert.That(association.connection.Select(e => (e.name, e.isNavigable)), Is.EqualTo(new[] { ("AssociationEnd0", true), ("AssociationEnd1", true) }));
        Show(association.connection[0]);
        TBoldUMLObject qualifier = fEditor.NewQualifier();
        Assert.That((qualifier.Ref("associationEnd"), qualifier.name, qualifier.owner), Is.EqualTo((association.connection[0], "NewQualifier", (TBoldUMLObject?)null)));

        Show(Model);
        TBoldUMLObject dataType = fEditor.NewDatatype();
        Assert.That((dataType.MetaClass, dataType.name, dataType.namespace_), Is.EqualTo(("DataType", "NewDataType", Model)));
        TBoldUMLObject package = fEditor.NewPackage();
        Assert.That((package.name, package.namespace_), Is.EqualTo(("NewPackage", Model)));
        Show(package);
        Assert.That(fEditor.NewClass().namespace_, Is.SameAs(package));
        Assert.That(fEditor.NewDatatype().namespace_, Is.SameAs(package));

        Show(first);
        TBoldUMLObject inserted = fEditor.InsertSuperclass();
        Assert.That((inserted.name, first.Superclass, inserted.Superclass, inserted.persistent), Is.EqualTo(("NewClass_3", inserted, root, true)));
        TBoldUMLObject sub = fEditor.AddSubclass();
        Assert.That((sub.Superclass, sub.namespace_, sub.name), Is.EqualTo((first, first.namespace_, "NewClass_4")));
        Show(Model);
        Assert.That(() => fEditor.AddSubclass(), Throws.Exception.With.Message.EqualTo("Unable to understand which class to use as superclass"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.DeleteXxx))]
    public void Deleting_keeps_the_root_class_the_ends_and_the_model()
    {
        TBoldUMLObject root = TBoldUMLBoldify.GetRootClass(Model)!;
        Show(Model);
        TBoldUMLObject top = fEditor.NewClass();
        Show(top);
        TBoldUMLObject middle = fEditor.AddSubclass();
        Show(middle);
        TBoldUMLObject bottom = fEditor.AddSubclass();
        Show(middle);
        fEditor.DeleteXxx();
        Assert.That(middle.Deleted, Is.True);
        Assert.That(bottom.Superclass, Is.SameAs(top), "its subclasses get its superclass");
        Assert.That(fEditor.CurrentElement, Is.SameAs(Model));

        Show(root);
        fEditor.DeleteXxx();
        Show(Model.Associations.First().connection[0]);
        fEditor.DeleteXxx();
        Show(Model);
        fEditor.DeleteXxx();
        Assert.That(root.Deleted || Model.Associations.First().connection.Count < 2 || Model.Deleted, Is.False);

        TBoldUMLObject attribute = Show(Feature("Person", "FirstName"));
        fEditor.DeleteXxx();
        Assert.That(attribute.Deleted, Is.True);
        Assert.That(fEditor.CurrentElement, Is.SameAs(Class("Person")));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Cut), nameof(TBoldModelEditController.Copy), nameof(TBoldModelEditController.Paste),
        nameof(TBoldModelEditController.UMLObjectToCopyOrMove), "GetUMLObjectToCopyOrMove", "SetUMLObjectToCopyOrMove",
        "GetCopyPasteRef", "GetElementToPaste", "PasteClass", "PasteFeature", "PasteParameter", "PasteAssociation", "PasteAssociationEnd")]
    [Covers(typeof(CutOrCopyKind))]
    public void Copying_pastes_a_copy_and_cutting_moves()
    {
        TBoldUMLObject person = Class("Person"), building = Class("Building");
        TBoldUMLObject firstName = Show(Feature("Person", "FirstName"));
        fEditor.Copy();
        Assert.That((fEditor.CutOrCopy, fEditor.UMLObjectToCopyOrMove), Is.EqualTo((CutOrCopyKind.cckCopy, firstName)));
        Show(person);
        fEditor.Paste();
        Assert.That(person.feature.Count(f => f.name.StartsWith("FirstName", StringComparison.Ordinal)), Is.EqualTo(2));
        Assert.That(person.feature.Select(f => f.name), Does.Contain("FirstName_1"));
        Show(building);
        fEditor.Paste();
        Assert.That(building.feature.Single(f => f.name == "FirstName").typeName, Is.EqualTo(firstName.typeName));

        Show(firstName);
        Assert.That(fEditor.Cut(), Is.True);
        Show(building);
        fEditor.Paste();
        Assert.That((firstName.owner, firstName.name), Is.EqualTo((building, "FirstName_1")), "moved, with a name of its own");

        Show(Model.Associations.First());
        Assert.That(fEditor.Cut(), Is.False, "an association is not cut");
        Show(Model);
        Assert.That(fEditor.Cut(), Is.False);
        Show(TBoldUMLBoldify.GetRootClass(Model)!);
        Assert.That(fEditor.Cut(), Is.False);

        Show(person);
        fEditor.Copy();
        Show(Model);
        fEditor.Paste();
        TBoldUMLObject copy = Model.Classes.Single(c => c.name == "Person_1");
        Assert.That(copy.Superclass, Is.SameAs(TBoldUMLBoldify.GetRootClass(Model)));
        Show(building);
        fEditor.Paste();
        Assert.That(Model.Classes.Single(c => c.name == "Person_2").Superclass, Is.SameAs(building), "pasted on a class it becomes a subclass");

        TBoldUMLObject association = Model.Associations.First();
        TBoldUMLObject end = association.connection[0];
        Show(end);
        fEditor.Copy();
        Show(copy);
        fEditor.Paste();
        TBoldUMLObject pasted = Model.Associations.Single(a => a != association && a.name.StartsWith(association.name, StringComparison.Ordinal));
        Assert.That(pasted.connection[0].type_, Is.SameAs(copy), "the copied end is the class's");
        Assert.That(pasted.connection[1].type_, Is.SameAs(association.connection[1].type_));

        Show(Model);
        TBoldUMLObject package = fEditor.NewPackage();
        Show(association);
        fEditor.Copy();
        Show(package);
        fEditor.Paste();
        Assert.That(package.ownedElement.Single(e => e.IsKindOf("Association")).connection.Count, Is.EqualTo(2));

        Show(building);
        TBoldUMLObject operation = fEditor.NewOperation();
        Show(operation);
        TBoldUMLObject parameter = fEditor.NewParameter();
        Show(parameter);
        fEditor.Copy();
        Show(operation);
        fEditor.Paste();
        Assert.That(operation.parameter.Select(p => p.name), Is.EqualTo(new[] { "NewParameter", "NewParameter_1" }));
        fEditor.CancelCut();
        Assert.That((fEditor.CutOrCopy, fEditor.UMLObjectToCopyOrMove), Is.EqualTo((CutOrCopyKind.cckNone, (TBoldUMLObject?)null)));
        fEditor.Paste();
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.CanDrop), nameof(TBoldModelEditController.Drop), "DropOnUMLClass", "DropOnUMLAssociation",
        "DropOnUMLAssociationEnd", "DropOnUMLAttribute", "DropOnUMLPackage", "DropOnUMLOperation", "DropOnUMLParameter", "DroppedOnUMLClass", "DroppedOnUMLOperation",
        "DroppedOnUMLParameter", "DroppedOnUMLPackage")]
    public void Elements_are_dropped_where_they_fit()
    {
        TBoldUMLObject person = Class("Person"), building = Class("Building"), root = TBoldUMLBoldify.GetRootClass(Model)!;
        TBoldUMLObject firstName = Feature("Person", "FirstName");
        TBoldUMLObject association = Model.Associations.First();
        TBoldUMLObject end = association.connection[0];
        Show(person);
        TBoldUMLObject operation = fEditor.NewOperation();
        Show(operation);
        TBoldUMLObject a = fEditor.NewParameter(), b = fEditor.NewParameter(), c = fEditor.NewParameter();
        Show(Model);
        TBoldUMLObject package = fEditor.NewPackage();

        Assert.That(fEditor.CanDrop(firstName, building) && fEditor.CanDrop(operation, building) && fEditor.CanDrop(end, building) && fEditor.CanDrop(person, building), Is.True);
        Assert.That(fEditor.CanDrop(root, building), Is.False, "not the root class");
        Assert.That(fEditor.CanDrop(person, package) && fEditor.CanDrop(package, Model), Is.True);
        Assert.That(fEditor.CanDrop(Model, package), Is.False);
        Assert.That(fEditor.CanDrop(a, operation) && fEditor.CanDrop(a, b), Is.True);
        Assert.That(fEditor.CanDrop(firstName, operation) || fEditor.CanDrop(person, firstName) || fEditor.CanDrop(person, association) || fEditor.CanDrop(person, end), Is.False);
        Assert.That(fEditor.CanDrop(null, person) || fEditor.CanDrop(person, null), Is.False);

        fEditor.Drop(firstName, building);
        Assert.That(firstName.owner, Is.SameAs(building));
        fEditor.Drop(person, building);
        Assert.That(person.Superclass, Is.SameAs(building));
        fEditor.Drop(end, Class("Residential Building"));
        Assert.That(end.otherEnd!.type_, Is.SameAs(Class("Residential Building")));
        fEditor.Drop(c, a);
        Assert.That(operation.parameter, Is.EqualTo(new[] { c, a, b }));
        fEditor.Drop(person, package);
        Assert.That(person.namespace_, Is.SameAs(package));
        Show(building);
        TBoldUMLObject other = fEditor.NewOperation();
        fEditor.Drop(a, other);
        Assert.That((a.Ref("behavioralFeature"), operation.parameter.Count), Is.EqualTo((other, 2)));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.GetMethodName), nameof(TBoldModelEditController.GetParamNames),
        nameof(TBoldModelEditController.GetParamTypes), nameof(TBoldModelEditController.GetReturnType), nameof(TBoldModelEditController.FindFrameworkMethod),
        nameof(TBoldModelEditController.FrameworkMethods))]
    public void Methods_are_read_from_their_Delphi_signatures()
    {
        static string[] Names(string m)
        {
            var list = new TStringList();
            TBoldModelEditController.GetParamNames(m, list);
            return [.. Enumerable.Range(0, list.Count).Select(i => list[i])];
        }

        static string[] Types(string m)
        {
            var list = new TStringList();
            TBoldModelEditController.GetParamTypes(m, list);
            return [.. Enumerable.Range(0, list.Count).Select(i => list[i])];
        }

        string query = TBoldModelEditController.FindFrameworkMethod("ReceiveQueryFromOwned");
        Assert.That(TBoldModelEditController.GetMethodName(query), Is.EqualTo("ReceiveQueryFromOwned"));
        Assert.That(Names(query), Is.EqualTo(new[] { "Originator", "OriginalEvent", "const Args", "Subscriber" }));
        Assert.That(Types(query), Is.EqualTo(new[] { "TObject", "TBoldEvent", "array of const", "TBoldSubscriber" }));
        Assert.That(TBoldModelEditController.GetReturnType(query), Is.EqualTo("Boolean"));

        string complete = TBoldModelEditController.FindFrameworkMethod("CompleteCreate");
        Assert.That((TBoldModelEditController.GetMethodName(complete), Names(complete).Length, TBoldModelEditController.GetReturnType(complete)),
            Is.EqualTo(("CompleteCreate", 0, "")));
        string mayDelete = TBoldModelEditController.FindFrameworkMethod("MayDelete");
        Assert.That((TBoldModelEditController.GetMethodName(mayDelete), TBoldModelEditController.GetReturnType(mayDelete)), Is.EqualTo(("MayDelete", "Boolean")));
        string set = TBoldModelEditController.FindFrameworkMethod("SetStringRepresentation");
        Assert.That((Types(set), TBoldModelEditController.GetReturnType(set)), Is.EqualTo((new[] { "TBoldRepresentation", "string" }, "")));
        Assert.That(TBoldModelEditController.FindFrameworkMethod("Nonsense"), Is.Empty);
        Assert.That(TBoldModelEditController.FrameworkMethods.Select(TBoldModelEditController.GetMethodName), Is.Unique.And.Not.Contains(""));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.OverrideFrameworkMethod), nameof(TBoldModelEditController.CreateParamsFromStringList),
        nameof(TBoldModelEditController.CreateFrameworkMethodItems))]
    public void A_framework_method_is_overridden_with_its_parameters()
    {
        TBoldUMLObject person = Show(Class("Person"));
        int before = TBoldModelEditController.CreateFrameworkMethodItems(person).Count;
        Assert.That(TBoldModelEditController.CreateFrameworkMethodItems(person), Does.Contain("ReceiveQueryFromOwned"));
        Assert.That(before, Is.EqualTo(13 - person.feature.Count(f => TBoldModelEditController.FindFrameworkMethod(f.name) != "")));
        TBoldUMLObject operation = fEditor.OverrideFrameworkMethod("ReceiveQueryFromOwned")!;
        Assert.That((operation.owner, operation.name, operation.GetBoldTV(TAG_DELPHIOPERATIONKIND)), Is.EqualTo((person, "ReceiveQueryFromOwned", TV_DELPHIOPERATIONKIND_OVERRIDE)));
        Assert.That(operation.parameter.Select(p => (p.name, p.typeName, p.kind, p.GetBoldTV(TAG_ISCONST))), Is.EqualTo(new[]
        {
            ("Originator", "TObject", TBoldParameterDirectionKind.pdIn, TV_FALSE),
            ("OriginalEvent", "TBoldEvent", TBoldParameterDirectionKind.pdIn, TV_FALSE),
            ("Args", "array of const", TBoldParameterDirectionKind.pdIn, TV_TRUE),
            ("Subscriber", "TBoldSubscriber", TBoldParameterDirectionKind.pdIn, TV_FALSE),
            ("return", "Boolean", TBoldParameterDirectionKind.pdReturn, TV_FALSE),
        }));
        Assert.That(TBoldModelEditController.CreateFrameworkMethodItems(person), Has.Count.EqualTo(before - 1).And.No.Contains("ReceiveQueryFromOwned"));

        var names = new TStringList();
        names.Add("var Count");
        var types = new TStringList();
        types.Add("Integer");
        TBoldModelEditController.CreateParamsFromStringList(names, types, operation, "");
        Assert.That(operation.parameter.Last().name, Is.EqualTo("Count"));
        Assert.That(operation.parameter.Last().kind, Is.EqualTo(TBoldParameterDirectionKind.pdInout));

        using var answers = new DialogAnswers(Controls.mrOk);
        Assert.That(fEditor.OverrideFrameworkMethod("Nonsense"), Is.Null);
        Assert.That(answers.Messages, Is.EqualTo(new[] { "Error: Could not find operation!" }));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.OverrideModelMethod), nameof(TBoldModelEditController.CreateModelMethodItems),
        "FindModelMethod", nameof(TBoldModelEditController.OverrideInAllSubclasses))]
    public void A_superclass_s_virtual_operation_is_overridden()
    {
        TBoldUMLObject building = Show(Class("Building"));
        TBoldUMLObject operation = fEditor.NewOperation();
        operation.name = "Describe";
        operation.SetBoldTV(TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_VIRTUAL);
        Show(operation);
        TBoldUMLObject parameter = fEditor.NewParameter();
        parameter.name = "Detail";
        parameter.typeName = "Integer";
        parameter.SetBoldTV(TAG_ISCONST, TV_TRUE);

        TBoldUMLObject residential = Show(Class("Residential Building"));
        var methods = new List<TBoldUMLObject>();
        TBoldModelEditController.GetAllOverrideableMethods(residential, methods);
        Assert.That(methods, Does.Contain(operation));
        Assert.That(fEditor.CreateModelMethodItems(residential), Does.Contain("Describe"));
        TBoldUMLObject overridden = fEditor.OverrideModelMethod("Describe")!;
        Assert.That((overridden.owner, overridden.GetBoldTV(TAG_DELPHIOPERATIONKIND)), Is.EqualTo((residential, TV_DELPHIOPERATIONKIND_OVERRIDE)));
        Assert.That(overridden.parameter.Select(p => (p.name, p.typeName, p.GetBoldTV(TAG_ISCONST))), Is.EqualTo(new[] { ("Detail", "Integer", TV_TRUE) }));
        Assert.That(fEditor.CreateModelMethodItems(residential), Does.Not.Contain("Describe"));

        overridden.Delete();
        Show(operation);
        fEditor.OverrideInAllSubclasses();
        Assert.That(residential.feature.Count(f => f.name == "Describe"), Is.EqualTo(1));
        Assert.That(building.feature.Count(f => f.name == "Describe"), Is.EqualTo(1));

        Show(residential);
        using var answers = new DialogAnswers(Controls.mrOk);
        Assert.That(fEditor.OverrideModelMethod("Nonsense"), Is.Null);
        Assert.That(answers.Messages, Is.EqualTo(new[] { "Error: Could not find operation!" }));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Boldifymodel), nameof(TBoldModelEditController.Flatten),
        nameof(TBoldModelEditController.BoldifyFlattenCaptions), nameof(TBoldModelEditController.EnsureFlattenedAndBoldified),
        nameof(TBoldModelEditController.EnsureUnFlattenedAndUnBoldified))]
    public void Boldifying_and_flattening_go_both_ways()
    {
        Assert.That(fEditor.BoldifyFlattenCaptions(), Is.EqualTo(("UnBoldify model", "UnFlatten package structure")));
        fEditor.Boldifymodel();
        Assert.That(TBoldUMLBoldify.IsBoldified(Model), Is.False);
        Assert.That(fEditor.BoldifyFlattenCaptions()!.Value.Boldify, Is.EqualTo("Boldify model"));
        fEditor.Flatten();
        Assert.That(TBoldUMLSupport.IsFlattened(Model), Is.False);
        Assert.That(fEditor.BoldifyFlattenCaptions()!.Value.Flatten, Is.EqualTo("Flatten package structure"));
        fEditor.EnsureFlattenedAndBoldified();
        Assert.That(TBoldUMLBoldify.IsBoldified(Model) && TBoldUMLSupport.IsFlattened(Model), Is.True);
        fEditor.EnsureUnFlattenedAndUnBoldified();
        Assert.That(TBoldUMLBoldify.IsBoldified(Model) || TBoldUMLSupport.IsFlattened(Model), Is.False);
        fEditor.Boldifymodel();
        Assert.That(TBoldUMLBoldify.IsBoldified(Model), Is.True);
        Assert.That(new TBoldModelEditController().BoldifyFlattenCaptions(), Is.Null);
    }

    [Test]
    public void A_change_that_fails_leaves_the_model_as_it_was()
    {
        string[] before = [.. Model.Classes.Select(c => c.name).Order()];
        Assert.That(() => fEditor.Transaction(() =>
        {
            Class("Person").Delete();
            throw new EBold("failed");
        }, true), Throws.InstanceOf<EBold>());
        Assert.That(Model.Classes.Select(c => c.name).Order(), Is.EqualTo(before));
        Assert.That(fModel.EnsuredUMLModel, Is.SameAs(Model), "the same model, read back");
        fEditor.Transaction(() => throw new EBold("failed"), false);
        Assert.That(Model.Classes.Select(c => c.name).Order(), Is.EqualTo(before));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.ModelNeedsValidation), "SetModelNeedsValidation", "ModelChangedRecieve", "ModelValidatedReceive",
        "SubscribeToModelChanges", nameof(TBoldModelEditController.IgnoreModelChanges), "SetIgnoreModelChanges", nameof(TBoldModelEditController.StatusText))]
    public void The_status_says_whether_the_model_was_validated_since_it_changed()
    {
        int statusChanges = 0;
        fEditor.StatusChanged += () => statusChanges++;
        TBoldUMLModelValidatorCallBack.Validate(fModel);
        Assert.That(fEditor.ModelNeedsValidation, Is.False);
        Assert.That(statusChanges, Is.GreaterThan(0));
        Assert.That(fEditor.StatusText().Text, Is.EqualTo("No errors in model").Or.StartWith("There are Hints"));

        fEditor.IgnoreModelChanges = true;
        Class("Person").name = "Human";
        Assert.That(fEditor.ModelNeedsValidation, Is.False, "ignored");
        fEditor.IgnoreModelChanges = false;
        Class("Human").name = "Person";
        Assert.That(fEditor.ModelNeedsValidation, Is.True);
        Assert.That(fEditor.StatusText().Text, Is.EqualTo("Model not validated").Or.StartWith("There are Hints"));

        EditorSamples.NewElement(Model, "Class", "Person");
        TBoldUMLModelValidatorCallBack.Validate(fModel);
        Assert.That(fEditor.StatusText(), Is.EqualTo(("There are Errors in this model, Select \"Tools\"->\"Check Consistency\" for further details.", TSeverity.sError)));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.OpenFile), nameof(TBoldModelEditController.SaveFileAs),
        nameof(TBoldModelEditController.GetIsExecutingPlugin), nameof(TBoldModelEditController.SetIsExecutingPlugin))]
    public void Files_are_read_and_written_through_the_link_of_their_extension()
    {
        TBoldUMLBldLink.EnsureRegistered();
        Assert.That(TBoldModelEditController.LinkForFile("x.BLD")!.LinkType, Is.EqualTo(typeof(TBoldUMLBldLink)), "whatever the case");
        Assert.That(TBoldModelEditController.LinkForFile("x.txt"), Is.Null);
        Assert.That(TBoldModelEditController.OpenFileFilter(), Does.Contain("*.bld").And.Not.Contain("*.*"));
        Assert.That(TBoldModelEditController.SaveFileFilter(), Does.Contain("*.bld").And.Not.Contain("*.*"));

        string file = Path.Combine(Path.GetTempPath(), $"bold-editor-{Guid.NewGuid():N}.bld");
        try
        {
            Class("Person").name = "Human";
            Assert.That(fEditor.SaveFileAs(file), Is.True);
            Assert.That(fModel.FileName, Is.EqualTo(file));
            Class("Human").name = "Person";
            Show(Class("Person"));
            Assert.That(fEditor.OpenFile(file), Is.True);
            Assert.That(Model.Classes.Select(c => c.name), Does.Contain("Human").And.Not.Contain("Person"));
            Assert.That(fEditor.CurrentElement, Is.SameAs(Model));
            Assert.That(fEditor.OpenFile(Path.ChangeExtension(file, ".txt")), Is.False);

            fEditor.IsExecutingPlugin = true;
            using (var answers = new DialogAnswers(Controls.mrOk))
            {
                Assert.That(fEditor.OpenFile(file) || fEditor.SaveFileAs(file), Is.False);
                Assert.That(answers.Messages, Is.EqualTo(new[] { "Already executing a plugin", "Already executing a plugin" }));
            }

            fEditor.SetIsExecutingPlugin(false);
            Assert.That(fEditor.GetIsExecutingPlugin(), Is.False);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.ExecPlugIn))]
    public void A_plug_in_runs_on_the_model_as_it_needs_it()
    {
        bool? boldified = null;
        var unboldified = new TUMLPlugIn("U", c => boldified = TBoldUMLBoldify.IsBoldified(c.GetCurrentModelHandle().EnsuredUMLModel), TPlugInType.ptTool, null, "",
            System.Drawing.Color.Teal, TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireUnBoldified), "U");
        fEditor.ExecPlugIn(unboldified);
        Assert.That(boldified, Is.False);
        var boldifiedPlugIn = new TUMLPlugIn("B", c => boldified = TBoldUMLBoldify.IsBoldified(c.GetCurrentModelHandle().EnsuredUMLModel), TPlugInType.ptTool, null, "",
            System.Drawing.Color.Teal, TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified), "B");
        fEditor.ExecPlugIn(boldifiedPlugIn);
        Assert.That(boldified, Is.True);
        Assert.That(fEditor.IsExecutingPlugin, Is.False);
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Clear), nameof(TBoldModelEditController.IsAdvancedMode), "SetIsAdvancedMode",
        nameof(TBoldModelEditController.PopulateComboBoxes))]
    public void Clearing_asks_first()
    {
        using (new DialogAnswers(Controls.mrNo))
        {
            fEditor.Clear();
        }

        Assert.That(Model.Classes, Is.Not.Empty);
        using (new DialogAnswers(Controls.mrYes))
        {
            fEditor.Clear();
        }

        Assert.That(Model.ownedElement.Count + Model.TaggedValues.Count, Is.Zero);
        Assert.That(fEditor.CurrentElement, Is.SameAs(Model));

        int changes = 0;
        fEditor.AdvancedModeChanged += () => changes++;
        fEditor.IsAdvancedMode = true;
        fEditor.IsAdvancedMode = true;
        Assert.That((fEditor.IsAdvancedMode, changes), Is.EqualTo((true, 1)));

        Dictionary<string, string[]> values = TBoldModelEditController.PopulateComboBoxes();
        Assert.That(values["bvhTableMapping"], Is.EqualTo(new[] { TV_TABLEMAPPING_OWN, TV_TABLEMAPPING_PARENT, TV_TABLEMAPPING_CHILDREN, TV_TABLEMAPPING_IMPORTED }));
        Assert.That(values["bvhAttributeKind"], Is.EqualTo(new[] { TV_ATTRIBUTEKIND_BOLD, TV_ATTRIBUTEKIND_DELPHI }));
        Assert.That(values["bvhMultiplicityValues"], Is.EqualTo(new[] { "0..1", "1..1", "0..*", "1..*" }));
        Assert.That(values["bvhDeleteActions"], Is.Not.Empty);
        Assert.That(values["bvhDelphiFunctionType"], Does.Contain(TV_DELPHIOPERATIONKIND_VIRTUAL));
        Assert.That(values["bvhDelphiProperty"], Is.Not.Empty);
        Assert.That(values["bvhOptimisticLocking"][0], Is.EqualTo(BoldDefs.DEFAULTNAME));
    }
}
