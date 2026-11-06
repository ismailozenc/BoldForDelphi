// Delphi: Source/UMLModel/Core/BoldUMLOCLValidator.pas
// Port map: CSharp/port/units/BoldUMLOCLValidator.md
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldUMLOCLValidatorTests
{
    private static (TBoldUMLObject Model, TBoldUMLObject Customer) Shop()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        return (model, model.Classes.Single(c => c.name == "Customer"));
    }

    private static TBoldUMLObject Derived(TBoldUMLObject cls, string name, string typeName, string ocl)
    {
        TBoldUMLObject attribute = EditorSamples.NewAttribute(cls, name, typeName);
        attribute.derived = true;
        attribute.persistent = false;
        attribute.SetBoldTV(TAG_DERIVATIONOCL, ocl);
        return attribute;
    }

    private static string[] Check(TBoldUMLObject model, TSeverity severity = TSeverity.sError)
    {
        EditorSamples.Prepared(model);
        new TOCLValidityChecker(EditorSamples.DefaultTypes()).ValidateModel(model);
        return EditorSamples.Violations(model, severity);
    }

    [Test]
    [Covers(typeof(TOCLValidityChecker), nameof(TOCLValidityChecker.ValidateModel), nameof(TOCLValidityChecker.TypeNameDictionary), "ValidateClass", "ValidateAttribute",
        "ValidateExpression", "ExtractSystemTypeInfo", "GetOCLEvaluator", "OCLEvaluator", "SystemTypeInfo", "AttributeTypeInfoFromUMLAttribute", "ClearViolations", "AddViolation")]
    [Covers(typeof(TOCLValidityChecker), "*")]
    public void Derivation_expressions_must_parse_and_give_the_attribute_s_type()
    {
        (TBoldUMLObject model, TBoldUMLObject customer) = Shop();
        Derived(customer, "shout", "String", "name + '!'");
        Derived(customer, "bad", "String", "nmae");
        Derived(customer, "count", "Integer", "name");
        EditorSamples.NewAttribute(customer, "plain", "String").SetBoldTV(TAG_DERIVATIONOCL, "name");
        string[] errors = Check(model);
        Assert.That(errors, Has.Length.EqualTo(2), string.Join("\n", errors));
        Assert.That(errors[0], Does.StartWith("Invalid DerivationOCL: "));
        Assert.That(errors[1], Is.EqualTo("Invalid DerivationOCL:  Type mismatch: Expected Integer got String"));
        Assert.That(EditorSamples.Violations(model, TSeverity.sHint), Does.Contain("Derivation expression specified for non-derived attribute"));
        Assert.That(model.Graph.Violations.First(v => v.Severity == TSeverity.sError).ModelElement!.name, Is.EqualTo("bad"));

        new TOCLValidityChecker(EditorSamples.DefaultTypes()).ValidateModel(model);
        Assert.That(EditorSamples.Violations(model, TSeverity.sError), Has.Length.EqualTo(2), "validating again starts afresh");
    }

    [Test]
    [Covers(typeof(TOCLValidityChecker), "ValidateConstraints", "ValidateClass", "ValidateExpression")]
    public void Constraints_must_be_Boolean_and_string_representations_strings()
    {
        (TBoldUMLObject model, TBoldUMLObject customer) = Shop();
        TBoldUMLModelLinkSupport.StringToConstraints(customer, "named: name <> ''\r\nname");
        customer.SetBoldTV(TAG_DEFAULTSTRINGREPRESENTATION, "name.length");
        string[] errors = Check(model);
        Assert.That(errors, Is.EqualTo(new[]
        {
            "Invalid DefaultStringRepresentation:  Type mismatch: Expected String got Integer",
            "Invalid Constraint:  Type mismatch: Expected Boolean got String",
        }));
    }

    [Test]
    [Covers(typeof(TOCLValidityChecker), "ValidateAssociationEnd", "ValidateAssociation", "ElementTypeInfoFromUMLAssociationEnd")]
    public void Derived_association_ends_must_give_their_class()
    {
        (TBoldUMLObject model, TBoldUMLObject customer) = Shop();
        TBoldUMLObject order = EditorSamples.NewElement(model, "Class", "Order");
        order.persistent = true;
        TBoldUMLObject places = EditorSamples.NewAssociation(model, "Places", customer, "customer", "0..1", order, "orders", "0..*");
        TBoldUMLObject friends = EditorSamples.NewAssociation(model, "Friends", customer, "friendOf", "0..*", customer, "friends", "0..*");
        friends.derived = true;
        friends.persistent = false;
        friends.connection[1].SetBoldTV(TAG_DERIVATIONOCL, "Customer.allInstances");
        friends.connection[0].SetBoldTV(TAG_DERIVATIONOCL, "Order.allInstances");
        places.connection[1].SetBoldTV(TAG_DERIVATIONOCL, "Order.allInstances");
        string[] errors = Check(model);
        Assert.That(errors, Is.EqualTo(new[] { "Invalid DerivationOCL:  Type mismatch: Expected Collection(Customer) got Collection(Order)" }));
        Assert.That(EditorSamples.Violations(model, TSeverity.sHint), Does.Contain("Derivation expression specified for non-derived association"));
    }

    [Test]
    [Covers(typeof(TOCLValidityChecker), "ValidateDerivationExpressions")]
    public void A_subclass_may_derive_an_inherited_member_otherwise()
    {
        (TBoldUMLObject model, TBoldUMLObject customer) = Shop();
        Derived(customer, "title", "String", "name");
        EditorSamples.NewAttribute(customer, "code", "String");
        TBoldUMLObject premium = EditorSamples.NewElement(model, "Class", "Premium");
        premium.persistent = true;
        premium.SetFirstParent(customer);
        premium.SetBoldTV("DerivationExpressions", "title=name.toUpper\r\ncode=name");
        Assert.That(Check(model), Is.Empty);
        Assert.That(EditorSamples.Violations(model, TSeverity.sHint), Does.Contain("Derivation expression respecified for non-derived attribute"));

        premium.SetBoldTV("DerivationExpressions", "title=name.length");
        Assert.That(Check(model), Is.EqualTo(new[] { "Invalid DerivationExpression:  Type mismatch: Expected String got Integer" }));
    }
}
