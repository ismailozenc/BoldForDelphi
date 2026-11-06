// Delphi: Source/UMLModel/ModelLinks/XMI/BoldDTDParser.pas, BoldUMLDTDData.pas
// Port map: CSharp/port/units/BoldDTDParser.md
namespace Bold.Tests;

public class BoldDTDParserTests
{
    private const string SmallDtd = """
        <!-- A part of the UML 1.3 DTD -->
        <!ENTITY % XMI.element.att 'xmi.id ID #IMPLIED xmi.label CDATA #IMPLIED' >
        <!ENTITY % Foundation.Core.ModelElementFeatures 'Foundation.Core.ModelElement.name | Foundation.Core.ModelElement.visibility' >
        <!ELEMENT Foundation.Core.ModelElement.name (#PCDATA | XMI.reference)* >
        <!ELEMENT Foundation.Core.ModelElement.visibility EMPTY >
        <!ATTLIST Foundation.Core.ModelElement.visibility xmi.value (public | protected | private) #REQUIRED >
        <!ELEMENT Foundation.Core.Class (%Foundation.Core.ModelElementFeatures; | Foundation.Core.Class.isActive | XMI.extension)* >
        <!ATTLIST Foundation.Core.Class %XMI.element.att; >
        <!ELEMENT Foundation.Data_Types.Multiplicity (Foundation.Data_Types.Multiplicity.range, (XMI.extension)?)+ >
        <!ELEMENT XMI.extension ANY >
        """;

    [Test]
    [Covers(typeof(TBoldDTDParser), "*")]
    [Covers(typeof(TBoldDTDScanner), "*")]
    [Covers(typeof(TBoldDTDToken))]
    [Covers(typeof(TBoldDTDNameEvent))]
    [Covers(typeof(TBoldDTDEvent))]
    public void The_parser_tells_each_element_and_the_names_in_its_content()
    {
        var events = new List<string>();
        var parser = new TBoldDTDParser
        {
            OnStartElement = n => events.Add("<" + n),
            OnFoundCpName = n => events.Add(n),
            OnEndElement = () => events.Add(">"),
        };
        parser.Parse(SmallDtd);
        Assert.That(events, Is.EqualTo(new[]
        {
            "<Foundation.Core.ModelElement.name", ">",
            "<Foundation.Core.ModelElement.visibility", ">",
            "<Foundation.Core.Class", "Foundation.Core.ModelElement.name", "Foundation.Core.ModelElement.visibility", "Foundation.Core.Class.isActive", "XMI.extension", ">",
            "<Foundation.Data_Types.Multiplicity", "Foundation.Data_Types.Multiplicity.range", "XMI.extension", ">",
            "<XMI.extension", ">",
        }));
    }

    [Test]
    public void The_scanner_gives_the_tokens()
    {
        var scanner = new TBoldDTDScanner("<!ELEMENT a (#PCDATA)* > 'x' %y; ?+,|");
        var tokens = new List<TBoldDTDToken>();
        do
        {
            scanner.NextToken();
            tokens.Add(scanner.LastToken);
        }
        while (scanner.LastToken != TBoldDTDToken.dtEOF);

        Assert.That(tokens, Is.EqualTo(new[]
        {
            TBoldDTDToken.dtElementDecl, TBoldDTDToken.dtSpace, TBoldDTDToken.dtName, TBoldDTDToken.dtSpace, TBoldDTDToken.dtOpenParen,
            TBoldDTDToken.dtPCDataDecl, TBoldDTDToken.dtCloseParen, TBoldDTDToken.dtZeroOrMore, TBoldDTDToken.dtSpace, TBoldDTDToken.dtEndDecl,
            TBoldDTDToken.dtSpace, TBoldDTDToken.dtStringLiteral, TBoldDTDToken.dtSpace, TBoldDTDToken.dtPercent, TBoldDTDToken.dtName,
            TBoldDTDToken.dtSemicolon, TBoldDTDToken.dtSpace, TBoldDTDToken.dtOptional, TBoldDTDToken.dtOneOrMore, TBoldDTDToken.dtComma,
            TBoldDTDToken.dtChoice, TBoldDTDToken.dtEOF,
        }));
        Assert.That(() => scanner.NextToken(), Throws.InstanceOf<EBold>(), "past the end");
        Assert.That(() => new TBoldDTDParser().Parse("<!ELEMENT a (b c) >"), Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers(typeof(TBoldUMLDTDReader), "*")]
    [Covers(typeof(TdmUML13DTD), "*")]
    [Covers(typeof(BoldUMLDTDData), "*")]
    public void The_reader_gives_the_members_of_each_class()
    {
        string file = XmiSamples.Write(SmallDtd, "uml.dtd");
        TBoldUMLDTDReader reader = TBoldUMLDTDReader.CreateFromFile(file);
        Assert.That(reader.MembersOfClass("Foundation.Core.Class"),
            Is.EqualTo("Foundation.Core.ModelElement.name, Foundation.Core.ModelElement.visibility, Foundation.Core.Class.isActive, XMI.extension"));
        Assert.That(reader.MembersOfClass("Foundation.Core.ModelElement.name"), Is.EqualTo(""), "a member is not a class");
        string saved = XmiSamples.NewFile("table.txt");
        reader.SaveToFile(saved);
        Assert.That(File.ReadAllLines(saved), Has.Length.EqualTo(2), "the classes: names whose last part starts upper case");

        Assert.That(TBoldUMLDTDReader.Shared.MembersOfClass("Foundation.Core.Attribute"), Does.StartWith("Foundation.Core.ModelElement.name, "));
        Assert.That(TBoldUMLDTDReader.Shared.MembersOfClass("Foundation.Core.Attribute"), Does.Contain("Foundation.Core.Attribute.initialValue"));
        Assert.That(TdmUML13DTD.UML13DTD, Does.Contain("Foundation.Core.Class="));
        Assert.That(BoldUMLDTDData.dmUML13DTD, Is.Null);
    }
}
