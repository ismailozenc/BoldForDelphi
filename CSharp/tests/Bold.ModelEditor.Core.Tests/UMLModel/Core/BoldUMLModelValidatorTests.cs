// Delphi: Source/UMLModel/Core/BoldUMLModelValidator.pas, BoldUMLAbstractModelValidator.pas
// Port map: CSharp/port/units/BoldUMLModelValidator.md
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelValidatorTests
{
    private static TBoldSQLDataBaseConfig SQLite()
    {
        var config = new TBoldSQLDataBaseConfig();
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        return config;
    }

    private static TBoldUMLModelValidator Validate(TBoldUMLObject model, TBoldSQLDataBaseConfig? db = null,
        TBoldModelValidatorSourceLanguage language = TBoldModelValidatorSourceLanguage.mvslDelphi)
    {
        EditorSamples.Prepared(model);
        var validator = new TBoldUMLModelValidator(model, db, language);
        validator.Validate(EditorSamples.DefaultTypes());
        return validator;
    }

    private static string[] Errors(TBoldUMLObject model, TBoldSQLDataBaseConfig? db = null,
        TBoldModelValidatorSourceLanguage language = TBoldModelValidatorSourceLanguage.mvslDelphi)
    {
        using TBoldUMLModelValidator validator = Validate(model, db, language);
        return EditorSamples.Violations(model, TSeverity.sError);
    }

    private static TBoldUMLObject Class(TBoldUMLObject model, string name) => model.Classes.Single(c => c.name == name);

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), nameof(TBoldUMLModelValidator.Validate), nameof(TBoldUMLModelValidator.TypeNameDictionary), nameof(TBoldUMLModelValidator.Dispose))]
    [Covers(typeof(TBoldUMLAbstractModelValidator), "*")]
    [Covers(typeof(TBoldUMLModelValidator), "*")]
    [Covers(typeof(TBoldModelValidatorSourceLanguage))]
    [Covers("BoldUMLAbstractModelValidator", "BoldDefaultValidatorSourceLanguage")]
    [Covers("BoldUMLModelValidator", "beModelValidated")]
    public void A_sound_model_has_no_errors_and_the_model_hears_it_was_validated()
    {
        TBoldUMLObject model = EditorSamples.UMLModel();
        int validated = 0;
        model.Graph.ModelValidated += () => validated++;
        using (TBoldUMLModelValidator validator = Validate(model, SQLite()))
        {
            Assert.That(validator.Validator, Is.SameAs(model.Graph.Violations), "the violations are kept in the model");
            Assert.That(EditorSamples.Violations(model, TSeverity.sError), Is.Empty);
            Assert.That(validator.HighestSeverity(), Is.LessThan(TSeverity.sError));
            Assert.That(validator.UMLModel, Is.SameAs(model));
            Assert.That(validator.TypeNameDictionary, Is.Not.Null);
        }

        Assert.That(validated, Is.EqualTo(1));
        Assert.That(TBoldUMLAbstractModelValidator.BoldDefaultValidatorSourceLanguage, Is.EqualTo(TBoldModelValidatorSourceLanguage.mvslDelphi));
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), nameof(TBoldUMLModelValidator.Validate))]
    public void Only_a_flattened_and_boldified_model_is_validated()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        using var validator = new TBoldUMLModelValidator(model);
        Assert.That(() => validator.Validate(EditorSamples.DefaultTypes()), Throws.InstanceOf<EBoldInternal>());
        EditorSamples.Prepared(model);
        validator.Validate(null);
        Assert.That(EditorSamples.Violations(model, TSeverity.sError), Does.Contain("No TypeNameDictionary available"));
        validator.Validate(EditorSamples.DefaultTypes());
        Assert.That(EditorSamples.Violations(model, TSeverity.sError), Is.Empty, "validating again starts with no violations");
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), nameof(TBoldUMLModelValidator.Validate), "ValidateClass", "ValidateAttribute", "ValidateFeature", "ValidateNames", "ContainsClass")]
    public void Classes_and_attributes_need_names_that_are_their_own()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        EditorSamples.NewElement(model, "Class", "customer");
        EditorSamples.NewElement(model, "Class", "");
        EditorSamples.NewAttribute(customer, "", "String");
        EditorSamples.NewAttribute(customer, "rating", "Stars");
        Assert.That(Errors(model), Is.SupersetOf(new[]
        {
            "Duplicate class name \"Customer\"",
            "Class must have a name",
            "Member must have a name in \"Customer\"",
        }));
        Assert.That(model.Graph.Violations.Where(v => v.Description.Contains("rating")), Is.Empty, "a type the dictionary lacks has its default mapping");

        var stringsOnly = new TBoldTypeNameDictionary(null);
        TBoldTypeNameMapping mapping = stringsOnly.AddMapping();
        (mapping.ModelName, mapping.ExpressionName, mapping.DelphiName) = ("String", "String", "TBAString");
        using (var validator = new TBoldUMLModelValidator(model))
        {
            validator.Validate(stringsOnly);
        }

        Assert.That(EditorSamples.Violations(model, TSeverity.sError), Does.Contain("Attribute \"Customer.rating\" has unknown type (Stars)"));
        Assert.That(TBoldUMLModelValidator.ContainsClass(model, "CUSTOMER"), Is.True);
        Assert.That(TBoldUMLModelValidator.ContainsClass(model, "Order"), Is.False);
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), "ValidateNames", "ExpandedSourceName", "ExpandedExpressionName", "IsValidDelphiIdentifier", "IsValidCppIdentifier",
        "IsValidOCLIdentifier", "IsDelphiReservedWord", "IsCppReservedWord", "IsOCLReservedWord")]
    public void Names_must_do_for_the_source_code_and_OCL()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        EditorSamples.NewAttribute(customer, "begin", "String");
        TBoldUMLObject odd = EditorSamples.NewAttribute(customer, "odd", "String");
        odd.SetBoldTV(TAG_DELPHINAME, "9lives");
        odd.SetBoldTV(TAG_EXPRESSIONNAME, "and");
        string[] delphi = Errors(model);
        Assert.That(delphi, Does.Contain("Delphi name \"begin\" in \"Customer.begin\" is a reserved word"));
        Assert.That(delphi, Does.Contain("Expression name \"and\" in \"Customer.odd\" is a reserved word in OCL"));
        Assert.That(delphi.Where(e => e.Contains("9lives")), Is.Empty, "the name expander makes a name an identifier");
        Assert.That(Errors(model, null, TBoldModelValidatorSourceLanguage.mvslNone).Where(e => e.Contains("Delphi")), Is.Empty, "no source code, no source names");

        Assert.That(TBoldUMLModelValidator.IsValidDelphiIdentifier("_a1"), Is.True);
        Assert.That(TBoldUMLModelValidator.IsValidDelphiIdentifier("1a"), Is.False);
        Assert.That(TBoldUMLModelValidator.IsValidCppIdentifier("a_b"), Is.True);
        Assert.That(TBoldUMLModelValidator.IsValidOCLIdentifier("_a"), Is.False);
        Assert.That(TBoldUMLModelValidator.IsDelphiReservedWord("Property"), Is.True);
        Assert.That(TBoldUMLModelValidator.IsDelphiReservedWord("name"), Is.False);
        Assert.That(TBoldUMLModelValidator.IsCppReservedWord("if"), Is.True);
        Assert.That(TBoldUMLModelValidator.IsOCLReservedWord("implies"), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), "ValidateNames", "ExpandedDBName", "IsValidSQLIdentifier", "IsSQLReservedWord", "GetSQLReservedWordList", "SQLReservedWordList",
        "IsBoldReservedWord", "CheckDBStuff", "CheckAnsiSQLStuff", "NationalCharConversion")]
    public void With_a_database_the_table_and_column_names_are_checked()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        customer.SetBoldTV(TAG_TABLENAME, "Select");
        EditorSamples.NewAttribute(customer, "kind", "String").SetBoldTV(TAG_COLUMNNAME, "BOLD_TYPE");
        EditorSamples.NewAttribute(customer, "code", "String").SetBoldTV(TAG_COLUMNNAME, "size");
        Assert.That(Errors(model).Where(e => e.Contains("name \"")).Where(e => e.Contains("Table") || e.Contains("Column")), Is.Empty, "no database, no table names");
        Assert.That(Errors(model, SQLite()), Is.SupersetOf(new[]
        {
            "Table name \"Select\" in \"Customer\" is a reserved word in SQL",
            "Column name \"BOLD_TYPE\" in \"Customer.kind\" is a \"bold reserved\" word",
            "Column name \"size\" in \"Customer.code\" is a reserved word in SQL",
        }));
        Assert.That(TBoldUMLModelValidator.IsValidSQLIdentifier("$x_1"), Is.True);
        Assert.That(TBoldUMLModelValidator.IsBoldReservedWord("bold_id"), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), "ValidateAssociation", "ValidateAssociationEnd")]
    public void Associations_need_two_ends_with_classes_and_a_class_when_many_to_many()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        TBoldUMLObject order = EditorSamples.NewElement(model, "Class", "Order");
        order.persistent = true;
        EditorSamples.NewAssociation(model, "Places", customer, "customer", "0..1", order, "orders", "0..*");
        TBoldUMLObject twice = EditorSamples.NewAssociation(model, "Twice", customer, "same", "0..1", order, "same", "0..1");
        TBoldUMLObject composite = EditorSamples.NewAssociation(model, "Owns", customer, "owner", "0..*", order, "owned", "0..*");
        composite.connection[0].aggregation = TAggregationKind.akComposite;
        composite.connection[1].aggregation = TAggregationKind.akComposite;
        TBoldUMLObject single = EditorSamples.NewElement(model, "Association", "Lonely");
        TBoldUMLObject end = model.Graph.Create("AssociationEnd");
        single.AddRef("connection", end);
        end.name = "nobody";
        string[] errors = Errors(model);
        Assert.That(errors.Where(e => e.Contains("\"Places\"")), Is.Empty);
        Assert.That(errors, Does.Contain("AssociationEnds in Association Twice are both named same"));
        Assert.That(errors, Does.Contain("Association \"Owns\" has two ends that are aggregated"));
        Assert.That(errors, Does.Contain("Association \"Lonely\" must have two assocationEnds"));
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), "ValidateClass", "ValidateDuplicates")]
    public void Members_may_not_have_the_name_of_an_inherited_member()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        TBoldUMLObject premium = EditorSamples.NewElement(model, "Class", "Premium");
        premium.persistent = true;
        premium.SetFirstParent(customer);
        EditorSamples.NewAttribute(premium, "name", "String");
        Assert.That(Errors(model).Where(e => e.StartsWith("Duplicate member name \"name\"")), Is.Not.Empty);
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidator), "ValidateClass")]
    public void A_persistent_class_needs_a_persistent_superclass()
    {
        TBoldUMLObject model = EditorSamples.Shop();
        TBoldUMLObject customer = Class(model, "Customer");
        customer.persistent = false;
        TBoldUMLObject premium = EditorSamples.NewElement(model, "Class", "Premium");
        premium.persistent = true;
        premium.SetFirstParent(customer);
        Assert.That(Errors(model, SQLite()), Does.Contain("Persistent class \"Premium\" has non-persistent superclass"));
    }
}
