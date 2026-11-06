// Delphi: Source/UMLModel/Core/BoldUMLModelSupport.pas
// Port map: CSharp/port/units/BoldUMLModelSupport.md
// Boldify and its undoing, flattening of packages and back, unique names, and copies of elements, on the UML graph.
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelSupportTests
{
    /// <summary>A model with a package Sales holding Customer and Order, an unnamed many-to-many association between
    /// them, an attribute Customer.name and an operation Customer.Rename.</summary>
    private static (TBoldUMLObject Model, TBoldUMLObject Customer, TBoldUMLObject Order, TBoldUMLObject Association) Sample()
    {
        var graph = new TBoldUMLModelGraph();
        TBoldUMLObject model = graph.Create("Model");
        model.name = "Shop";
        TBoldUMLObject package = graph.Create("Package");
        package.name = "Sales";
        package.namespace_ = model;
        TBoldUMLObject customer = graph.Create("Class");
        customer.name = "Customer";
        customer.namespace_ = package;
        TBoldUMLObject order = graph.Create("Class");
        order.name = "Order";
        order.namespace_ = package;
        TBoldUMLObject name = graph.Create("Attribute");
        name.name = "name";
        customer.AddRef("feature", name);
        name.typeName = "String";
        TBoldUMLObject rename = graph.Create("Operation");
        rename.name = "Rename";
        customer.AddRef("feature", rename);
        TBoldUMLObject parameter = graph.Create("Parameter");
        parameter.name = "NewName";
        rename.AddRef("parameter", parameter);
        parameter.typeName = "String";
        TBoldUMLObject association = graph.Create("Association");
        association.namespace_ = package;
        foreach (TBoldUMLObject type in new[] { customer, order })
        {
            TBoldUMLObject end = graph.Create("AssociationEnd");
            association.AddRef("connection", end);
            end.type_ = type;
            end.multiplicity = "0..*";
        }

        return (model, customer, order, association);
    }

    [Test]
    [Covers(typeof(TBoldUMLBoldify), "*")]
    [Covers(typeof(TBoldUMLSupport), nameof(TBoldUMLSupport.EnsureBoldTaggedValues), nameof(TBoldUMLSupport.EnsureBoldTaggedValuesInModel),
        nameof(TBoldUMLSupport.ManipulateAllOwnedElements), nameof(TBoldUMLSupport.EnsuredTaggedValue), nameof(TBoldUMLSupport.RemoveTaggedValue),
        nameof(TBoldUMLSupport.UniqueName), nameof(TBoldUMLSupport.AllModelParts), nameof(TBoldUMLSupport.UMLModelNameToUMLName))]
    public void Boldify_makes_a_root_class_names_and_link_classes_and_UnBoldify_takes_them_back()
    {
        var (model, customer, order, association) = Sample();
        var boldify = new TBoldUMLBoldify { PluralSuffix = "s" };
        Assert.That(TBoldUMLBoldify.IsBoldified(model), Is.False);
        boldify.Boldify(model);
        Assert.That(TBoldUMLBoldify.IsBoldified(model), Is.True);

        TBoldUMLObject root = TBoldUMLBoldify.GetRootClass(model)!;
        Assert.That(root.name, Is.EqualTo("BusinessClassesRoot"));
        Assert.That(TBoldUMLBoldify.IsAutoCreated(root), Is.True);
        Assert.That(customer.Superclass, Is.SameAs(root));
        Assert.That(customer.GetBoldTV(TAG_TABLENAME), Is.EqualTo("<Name>"), "Bold's tagged values with their defaults");
        Assert.That(association.connection.Select(e => e.name), Is.EqualTo(new[] { "Customers", "Orders" }), "ends named by their classes, with the plural suffix");
        Assert.That(association.name, Is.EqualTo("CustomerOrder"));
        Assert.That(association.class_, Is.Not.Null, "a many-to-many association gets a link class");
        Assert.That(association.class_!.name, Is.EqualTo("CustomerOrder"));
        Assert.That(association.class_.Superclass, Is.SameAs(root));

        TBoldUMLBoldify.UnBoldify(model);
        Assert.That(TBoldUMLBoldify.IsBoldified(model), Is.False);
        Assert.That(model.Classes.Select(c => c.name), Is.EquivalentTo(new[] { "Customer", "Order" }), "the root and link classes are gone");
        Assert.That(customer.generalization, Is.Empty);
        Assert.That(association.name, Is.EqualTo(""));
        Assert.That(association.connection.Select(e => e.name), Is.EqualTo(new[] { "", "" }));
        Assert.That(model.GetBoldTV(TAG_ROOTCLASS), Is.EqualTo("BusinessClassesRoot"), "the root class is named");
        Assert.That(() => TBoldUMLBoldify.GetRootClass(model), Throws.InstanceOf<EBold>());

        var copy = new TBoldUMLBoldify();
        copy.Assign(boldify);
        Assert.That(copy.PluralSuffix, Is.EqualTo("s"));
        Assert.That(copy.DefaultNavigableMultiplicity, Is.EqualTo("0..1"));
        Assert.That(() => copy.Assign("x"), Throws.InstanceOf<EBold>());
    }

    [Test]
    public void Boldify_defaults_multiplicities_and_makes_derived_and_embedded_settings_fit()
    {
        var (model, customer, _, association) = Sample();
        TBoldUMLObject end = association.connection[0];
        end.multiplicity = "";
        end.isNavigable = false;
        TBoldUMLObject other = association.connection[1];
        other.multiplicity = "";
        TBoldUMLObject attribute = customer.feature[0];
        attribute.derived = true;
        new TBoldUMLBoldify().Boldify(model);
        Assert.That(end.multiplicity, Is.EqualTo("0..*"), "a non-navigable end");
        Assert.That(other.multiplicity, Is.EqualTo("0..1"), "a navigable end");
        Assert.That(end.name, Is.EqualTo("x_Order_Customer"), "a non-navigable end's implicit name");
        Assert.That(attribute.persistent, Is.False, "a derived attribute is transient");
        Assert.That(association.class_, Is.Null, "a one-to-many association needs no link class");

        TBoldUMLBoldify.UnBoldify(model);
        Assert.That(end.multiplicity, Is.EqualTo(""));
        Assert.That(attribute.persistent, Is.True);
    }

    [Test]
    public void A_model_whose_root_class_has_a_superclass_cannot_be_boldified()
    {
        var (model, customer, order, _) = Sample();
        model.SetBoldTV(TAG_ROOTCLASS, "Order");
        order.SetFirstParent(customer);
        Assert.That(() => new TBoldUMLBoldify().Boldify(model), Throws.InstanceOf<EBold>().With.Message.EqualTo(UMLConsts.sCannotBoldifyIfRootClassHasSuperClass));
    }

    [Test]
    [Covers(typeof(TBoldUMLSupport), "*")]
    public void Flattening_moves_classes_to_the_model_and_unflattening_puts_them_back()
    {
        var (model, customer, _, association) = Sample();
        TBoldUMLObject package = customer.namespace_!;
        TBoldUMLSupport.Flatten(model);
        Assert.That(TBoldUMLSupport.IsFlattened(model), Is.True);
        Assert.That(customer.namespace_, Is.SameAs(model));
        Assert.That(association.namespace_, Is.SameAs(model));
        Assert.That(customer.GetTaggedValue(BOLDINTERALTVPREFIX + TAG_UNFLATTENEDNAMESPACE), Is.EqualTo("Shop.Sales"));

        package.Delete();
        TBoldUMLSupport.UnFlatten(model);
        Assert.That(TBoldUMLSupport.IsFlattened(model), Is.False);
        Assert.That(customer.namespace_!.name, Is.EqualTo("Sales"), "the package is made again");
        Assert.That(customer.namespace_.namespace_, Is.SameAs(model));
        Assert.That(association.namespace_, Is.SameAs(customer.namespace_));
        Assert.That(customer.taggedValue(BOLDINTERALTVPREFIX + TAG_UNFLATTENEDNAMESPACE), Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldUMLSupport), nameof(TBoldUMLSupport.AddQualifier), nameof(TBoldUMLSupport.AddToNamespace),
        nameof(TBoldUMLSupport.AddToolId), nameof(TBoldUMLSupport.GetToolId), nameof(TBoldUMLSupport.StripToolId),
        nameof(TBoldUMLSupport.ElementForToolId), nameof(TBoldUMLSupport.RelinkSpecializations))]
    public void Names_are_made_unique_and_tool_ids_are_kept()
    {
        var (model, customer, order, association) = Sample();
        TBoldUMLObject third = model.Graph.Create("Class");
        TBoldUMLSupport.AddToNamespace(customer.namespace_!, third, "Customer");
        Assert.That(third.name, Is.EqualTo("Customer_1"));
        TBoldUMLObject qualifier = model.Graph.Create("Attribute");
        TBoldUMLSupport.AddQualifier(association.connection[0], qualifier, "key");
        Assert.That(qualifier.name, Is.EqualTo("key"));
        TBoldUMLObject second = model.Graph.Create("Attribute");
        TBoldUMLSupport.AddQualifier(association.connection[0], second, "key");
        Assert.That(second.name, Is.EqualTo("key_1"));

        TBoldUMLSupport.AddToolId(customer, "ID-1");
        Assert.That(TBoldUMLSupport.GetToolId(customer), Is.EqualTo("ID-1"));
        Assert.That(TBoldUMLSupport.ElementForToolId(model, "ID-1"), Is.SameAs(customer));
        TBoldUMLSupport.StripToolId(model);
        Assert.That(TBoldUMLSupport.GetToolId(customer), Is.EqualTo(""));

        // Order gets a parent; Customer's child Third takes Customer's parents when Customer is relinked
        TBoldUMLObject top = model.Graph.Create("Class");
        TBoldUMLSupport.AddToNamespace(model, top, "Top");
        customer.SetFirstParent(top);
        third.SetFirstParent(customer);
        TBoldUMLSupport.RelinkSpecializations(customer);
        Assert.That(third.Superclass, Is.SameAs(top));
        Assert.That(customer.specialization, Is.Empty);
    }

    [Test]
    [Covers(typeof(TBoldCopyAndClone), "*")]
    [Covers(typeof(TBoldCopyMode))]
    [Covers(typeof(TUMLModelElementManipulator))]
    [Covers(typeof(TBoldUMLOperationSupport), "*")]
    public void An_operation_is_overridden_in_the_subclasses_with_a_deep_copy()
    {
        var (model, customer, order, _) = Sample();
        TBoldUMLObject rename = customer.feature[1];
        TBoldUMLSupport.AddToolId(rename, "OP");
        order.SetFirstParent(customer);
        TBoldUMLObject sub = model.Graph.Create("Class");
        TBoldUMLSupport.AddToNamespace(model, sub, "SpecialOrder");
        sub.SetFirstParent(order);

        TBoldUMLOperationSupport.OverrideInAllSubclasses(customer, rename);
        TBoldUMLObject copy = order.feature.Single();
        Assert.That(copy, Is.Not.SameAs(rename));
        Assert.That(copy.name, Is.EqualTo("Rename"));
        Assert.That(copy.parameter.Single().typeName, Is.EqualTo("String"), "the parameters are copied");
        Assert.That(copy.parameter.Single(), Is.Not.SameAs(rename.parameter.Single()));
        Assert.That(copy.GetBoldTV(TAG_DELPHIOPERATIONKIND), Is.EqualTo(TV_DELPHIOPERATIONKIND_OVERRIDE));
        Assert.That(TBoldUMLSupport.GetToolId(copy), Is.EqualTo(""), "the tool id is not copied");
        Assert.That(sub.feature.Single().name, Is.EqualTo("Rename"));
        Assert.That(customer.feature.Count(f => f.name == "Rename"), Is.EqualTo(1), "the class itself has it already");

        var types = new TStringList { "string" };
        Assert.That(TBoldUMLOperationSupport.ClassHasOperation(order, "rename", types), Is.True, "names and types are compared ignoring case");
        Assert.That(TBoldUMLOperationSupport.ClassHasOperation(order, "rename", new TStringList { "Integer" }), Is.False);

        TBoldUMLObject shallow = TBoldCopyAndClone.BoldClone(customer.feature[0], TBoldCopyMode.bcmShallow, false)!;
        Assert.That(shallow.type_, Is.SameAs(customer.feature[0].type_), "a shallow copy shares what it refers to");
        Assert.That(shallow.owner, Is.SameAs(customer), "and its owner, whose other end holds many");
        Assert.That(customer.feature, Does.Contain(shallow));
        Assert.That(TBoldCopyAndClone.BoldClone(null, TBoldCopyMode.bcmDeep, false), Is.Null);
    }
}
