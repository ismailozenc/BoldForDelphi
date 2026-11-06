using System.ComponentModel;

namespace Bold.Tests;

/// <summary>Models for the editor's tests: fixture models, model components, and small UML models built in code.</summary>
internal static class EditorSamples
{
    /// <summary>The Buildings and Owners model of Delphi Bold's examples.</summary>
    public const string BuildingModel = "BuildingsAndOwners.Building.bld";

    public static TMoldModel Fixture(string file = BuildingModel)
    {
        var lines = new TStringList();
        lines.LoadFromFile(FixturePaths.Model(file));
        return TMoldBLDRW.StringsToModel(lines);
    }

    /// <summary>A model component with a fixture model.</summary>
    public static TBoldModel ModelComponent(IContainer? components, string file = BuildingModel)
    {
        var model = new TBoldModel(components);
        model.LoadFromMoldModel(Fixture(file));
        return model;
    }

    /// <summary>A UML model of a fixture model, as the editor shows it (neither flattened nor boldified).</summary>
    public static TBoldUMLObject UMLModel(string file = BuildingModel) => TBoldModelConverter.MoldModelToUML(Fixture(file));

    /// <summary>The model flattened and boldified, as the validator wants it.</summary>
    public static TBoldUMLObject Prepared(TBoldUMLObject model)
    {
        if (!TBoldUMLSupport.IsFlattened(model))
        {
            TBoldUMLSupport.Flatten(model);
        }

        if (!TBoldUMLBoldify.IsBoldified(model))
        {
            new TBoldUMLBoldify().Boldify(model);
        }

        return model;
    }

    /// <summary>The default type name mappings, as a model component without a type name handle has them.</summary>
    public static TBoldTypeNameDictionary DefaultTypes() => new TBoldAbstractModel().TypeNameDictionary;

    /// <summary>A model "Shop" with a package "Sales" and a class "Customer" (persistent, with a string attribute "name").</summary>
    public static TBoldUMLObject Shop()
    {
        var graph = new TBoldUMLModelGraph();
        TBoldUMLObject model = graph.Create("Model");
        model.name = "Shop";
        TBoldUMLSupport.EnsureBoldTaggedValues(model);
        TBoldUMLObject package = NewElement(model, "Package", "Sales");
        TBoldUMLObject customer = NewElement(package, "Class", "Customer");
        customer.persistent = true;
        NewAttribute(customer, "name", "String");
        return model;
    }

    public static TBoldUMLObject NewElement(TBoldUMLObject owner, string metaClass, string name)
    {
        TBoldUMLObject element = owner.Graph.Create(metaClass);
        TBoldUMLSupport.EnsureBoldTaggedValues(element);
        element.namespace_ = owner;
        element.name = name;
        return element;
    }

    public static TBoldUMLObject NewAttribute(TBoldUMLObject cls, string name, string typeName)
    {
        TBoldUMLObject attribute = cls.Graph.Create("Attribute");
        TBoldUMLSupport.EnsureBoldTaggedValues(attribute);
        cls.AddRef("feature", attribute);
        attribute.name = name;
        attribute.typeName = typeName;
        attribute.persistent = cls.persistent;
        return attribute;
    }

    /// <summary>An association between two classes, with ends named after the roles.</summary>
    public static TBoldUMLObject NewAssociation(TBoldUMLObject owner, string name, TBoldUMLObject from, string fromRole, string fromMultiplicity,
        TBoldUMLObject to, string toRole, string toMultiplicity)
    {
        TBoldUMLObject association = NewElement(owner, "Association", name);
        association.persistent = true;
        foreach ((TBoldUMLObject type, string role, string multiplicity) in new[] { (from, fromRole, fromMultiplicity), (to, toRole, toMultiplicity) })
        {
            TBoldUMLObject end = owner.Graph.Create("AssociationEnd");
            TBoldUMLSupport.EnsureBoldTaggedValues(end);
            association.AddRef("connection", end);
            end.name = role;
            end.type_ = type;
            end.multiplicity = multiplicity;
            end.isNavigable = true;
        }

        return association;
    }

    /// <summary>The descriptions of the violations of a severity.</summary>
    public static string[] Violations(TBoldUMLObject model, TSeverity severity) =>
        [.. model.Graph.Violations.Where(v => v.Severity == severity).Select(v => v.Description)];
}

/// <summary>The editor, as a plug-in sees it: a model component and the element on show.</summary>
internal sealed class TestPlugInContext(TBoldModel model, TBoldUMLObject? element = null) : IUMLModelPlugInContext
{
    private bool fExecuting;

    public bool GetIsExecutingPlugin() => fExecuting;

    public void SetIsExecutingPlugin(bool Value) => fExecuting = Value;

    public TBoldModel GetCurrentModelHandle() => model;

    public TBoldUMLObject? GetCurrentElement() => element;
}

/// <summary>Answers the message dialogs while a test runs, and keeps what they said.</summary>
internal sealed class DialogAnswers : IDisposable
{
    public readonly List<string> Messages = [];

    public DialogAnswers(int answer)
        : this(_ => answer)
    {
    }

    public DialogAnswers(Func<string, int> answer)
    {
        Dialogs.MessageDlgHandler = (msg, _, _, _) =>
        {
            Messages.Add(msg);
            return answer(msg);
        };
    }

    public void Dispose() => Dialogs.MessageDlgHandler = null;
}
