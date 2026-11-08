// Delphi: Source/ObjectSpace/IDE/AttributeWizard (the attribute wizard, which the generator replaces)
// Port map: CSharp/port/units/BoldAttributeWizard.md
using Bold.SourceGenerators;

namespace Bold.Tests;

/// <summary>The attribute generator run on sources: what it writes for each kind of attribute type, and what it reports.</summary>
public class BoldAttributeGeneratorTests
{
    private const string Color = """
        namespace Paint;

        [BoldValueSet("Color", ValuePrefix = "c")]
        [BoldValueSetValue("Red")]
        [BoldValueSetValue("Green", "Green", "Grün")]
        public partial class TBAColor : TBAValueSet { }
        """;

    private const string Money = """
        namespace Shop;

        [BoldAttributeType("Money", GenerateMapper = true)]
        [BoldAttributeField("Amount", typeof(decimal))]
        [BoldAttributeField("Currency", typeof(string))]
        [BoldAttributeField("Paid", typeof(bool))]
        [BoldAttributeField("When", typeof(System.DateTime))]
        [BoldAttributeField("Rate", typeof(double))]
        [BoldAttributeField("Count", typeof(int), Writable = false)]
        public partial class TBAMoney : TBoldAttribute { }
        """;

    [Test]
    [Covers("BoldAttributeWizard", "TAttributeWizard")]
    [Covers("BoldWAdmTemplates", "Tattrdatamodule")]
    [Covers("BoldWAValueSetForm1", "TValueSetForm1")]
    public void A_value_set_gets_its_enumeration_values_and_registration()
    {
        GeneratorRun run = GeneratorRun.Of(Color);
        Assert.That(run.GeneratorDiagnostics, Is.Empty);
        Assert.That(run.CompilationDiagnostics, Is.Empty, string.Join("\n", run.CompilationDiagnostics));
        string source = run.Source("TBAColor");
        Assert.That(source, Does.Contain("public enum TColorEnum"));
        Assert.That(source, Does.Contain("cRed,").And.Contain("cGreen,"));
        Assert.That(source, Does.Contain("public TColorEnum AsColor"));
        Assert.That(source, Does.Contain("values.Add((int)TColorEnum.cGreen, \"Green\", \"Grün\");"));
        Assert.That(source, Does.Contain("AddMemberTypeDescriptor(typeof(TBAColor)"));
        Assert.That(source, Does.Contain("mapping.NativeType = \"T<Name>Enum\";"));
        Assert.That(run.Sources.Keys, Is.EquivalentTo(new[] { "Paint.TBAColor.BoldAttribute.g.cs" }));
    }

    [Test]
    [Covers("BoldWACustomAttr", "TCustomAttribute")]
    [Covers("BoldWACustomAttr", "TPropertyInfo")]
    [Covers("BoldWACustomAttrForm1", "TCustomAttrForm1")]
    public void A_new_attribute_gets_its_fields_content_free_standing_value_and_mapper()
    {
        GeneratorRun run = GeneratorRun.Of(Money);
        Assert.That(run.GeneratorDiagnostics, Is.Empty);
        Assert.That(run.CompilationDiagnostics, Is.Empty, string.Join("\n", run.CompilationDiagnostics));
        string source = run.Source("TBAMoney");
        Assert.That(source, Does.Contain("public interface IBoldMoneyContent : global::Bold.IBoldNullableValue"));
        Assert.That(source, Does.Contain("public decimal AsAmount"));
        Assert.That(source, Does.Contain("public bool CanSetAmount(decimal Value"));
        Assert.That(source, Does.Contain("private string fCurrency = \"\";"));
        Assert.That(source, Does.Contain("public class TBAMoney_Proxy"));
        Assert.That(source, Does.Contain("public class TBFSMoney : global::Bold.TBoldFreeStandingNullableValue, IBoldMoneyContent"));
        Assert.That(source, Does.Contain("public partial class TBoldPMMoney : global::Bold.TBoldMemberDefaultMapper"));
        Assert.That(source, Does.Contain("protected override int GetColumnCount() => 6;"));
        Assert.That(source, Does.Contain("Param.AsCurrency = aValue.Amount;").And.Contain("aValue.When = Field.AsDateTime;").And.Contain("ftBoolean"));
        Assert.That(source, Does.Not.Contain("public bool CanSetCount"), "Count cannot be set");
        Assert.That(source, Does.Contain("public int AsCount").And.Not.Contain("set => SetAsCount(value);"));
    }

    [Test]
    [Covers("BoldWASubClassForm1", "TSubClassForm1")]
    public void A_subclassed_attribute_gets_its_registration_and_its_superclass_mapping()
    {
        GeneratorRun run = GeneratorRun.Of("""
            [BoldAttributeType("Name", ModelName = "PersonName")]
            internal partial class TBAName : TBAString { }
            """);
        Assert.That(run.GeneratorDiagnostics, Is.Empty);
        Assert.That(run.CompilationDiagnostics, Is.Empty, string.Join("\n", run.CompilationDiagnostics));
        string source = run.Source("TBAName");
        Assert.That(source, Does.Contain("m.DelphiName == \"TBAString\""));
        Assert.That(source, Does.Contain("mapping.ModelName = \"PersonName\";"));
        Assert.That(source, Does.Not.Contain("interface"), "a subclass has its superclass's content");
        Assert.That(run.Sources.Keys, Is.EquivalentTo(new[] { "TBAName.BoldAttribute.g.cs" }), "in the global namespace");
    }

    [Test]
    public void A_mapper_is_left_out_unless_asked_for()
    {
        GeneratorRun run = GeneratorRun.Of("""
            [BoldAttributeType("Pair")]
            [BoldAttributeField("Left", typeof(object))]
            [BoldAttributeField("Right", typeof(object))]
            public partial class TBAPair : TBoldAttribute { }
            """, withPersistence: false);
        Assert.That(run.GeneratorDiagnostics, Is.Empty);
        Assert.That(run.CompilationDiagnostics, Is.Empty, string.Join("\n", run.CompilationDiagnostics));
        Assert.That(run.Source("TBAPair"), Does.Not.Contain("TBoldPMPair :").And.Contain("mapping.MapperName = \"TBoldPMPair\";"));
    }

    [TestCase("public class TBAColor : TBAValueSet { }", "BOLDGEN001")]
    [TestCase("public partial class TBAColor : TBAString { }", "BOLDGEN002")]
    [TestCase("public partial class TBAColor : TBAValueSet { }", "BOLDGEN003", false)]
    [TestCase("public partial class TBAColor : TBAValueSet { }", "BOLDGEN009", true, "1st")]
    [TestCase("public partial class TBAColor : TBAValueSet { }", "BOLDGEN010", true, "Red")]
    public void What_keeps_a_value_set_from_being_written_is_reported(string declaration, string id, bool withValue = true, string? second = null)
    {
        string values = withValue ? "[BoldValueSetValue(\"Red\")]\n" : "";
        if (second is not null)
        {
            values += $"[BoldValueSetValue(\"{second}\")]\n";
        }

        GeneratorRun run = GeneratorRun.Of($"[BoldValueSet(\"Color\")]\n{values}{declaration}");
        Assert.That(run.Ids, Is.EqualTo(new[] { id }));
        Assert.That(run.Sources, Is.Empty);
        Assert.That(BoldAttributeDiagnostics.All.Select(d => d.Id), Does.Contain(id));
    }

    [TestCase("[BoldAttributeType(\"Thing\")] public partial class TBAThing { }", "BOLDGEN004")]
    [TestCase("[BoldAttributeType(\"Thing\")] public partial class TBAThing : TBoldAttribute { }", "BOLDGEN005")]
    [TestCase("[BoldAttributeType(\"Thing\", GenerateMapper = true)] [BoldAttributeField(\"Size\", typeof(long))] public partial class TBAThing : TBoldAttribute { }", "BOLDGEN006")]
    [TestCase("public class Outer { [BoldAttributeType(\"Thing\")] public partial class TBAThing : TBAString { } }", "BOLDGEN008")]
    [TestCase("[BoldAttributeType(\"a thing\")] public partial class TBAThing : TBAString { }", "BOLDGEN009")]
    [TestCase("[BoldAttributeType(\"Thing\")] [BoldAttributeField(\"A\", typeof(int))] [BoldAttributeField(\"A\", typeof(int))] public partial class TBAThing : TBoldAttribute { }", "BOLDGEN010")]
    public void What_keeps_an_attribute_type_from_being_written_is_reported(string declaration, string id)
    {
        GeneratorRun run = GeneratorRun.Of(declaration);
        Assert.That(run.Ids, Is.EqualTo(new[] { id }));
        Assert.That(run.Sources, Is.Empty);
    }

    [Test]
    public void A_mapper_needs_the_SQL_persistence()
    {
        GeneratorRun run = GeneratorRun.Of(Money, withPersistence: false);
        Assert.That(run.Ids, Is.EqualTo(new[] { "BOLDGEN007" }));
        Assert.That(run.GeneratorDiagnostics[0].GetMessage(), Does.Contain("TBAMoney asks for a persistence mapper"));
    }

    [Test]
    public void Strings_are_written_as_CSharp_literals() =>
        Assert.That(BoldAttributeCode.Literal("a \"b\"\\\n\t\u0001"), Is.EqualTo("\"a \\\"b\\\"\\\\\\n\\t\\u0001\""));
}
