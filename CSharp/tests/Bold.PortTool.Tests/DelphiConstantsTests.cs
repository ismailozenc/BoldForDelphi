using Bold.PortTool.Delphi;

namespace Bold.Tests;

public class DelphiConstantsTests
{
    private static object? Evaluate(string expression, Dictionary<string, object?>? names = null) =>
        DelphiConstants.EvaluateTokens(
            DelphiLexer.TokenizeText(expression).Where(t => t.Kind != TokenKind.End).ToList(),
            name => names?.GetValueOrDefault(name));

    [TestCase("1 + 2 * 3", 7L)]
    [TestCase("(1 + 2) * 3", 9L)]
    [TestCase("-5 + 2", -3L)]
    [TestCase("$FF", 255L)]
    [TestCase("7 div 2", 3L)]
    [TestCase("7 mod 2", 1L)]
    [TestCase("1 shl 4", 16L)]
    [TestCase("$F0 and $3C", 48L)]
    [TestCase("high(Integer)", 2147483647L)]
    [TestCase("low(SmallInt)", -32768L)]
    [TestCase("ord('A')", 65L)]
    [TestCase("MaxInt", 2147483647L)]
    public void Integer_expressions(string expression, long expected)
    {
        Assert.That(Evaluate(expression), Is.TypeOf<long>().And.EqualTo(expected), "integers stay integers");
    }

    [TestCase("'a' + 'b'", "ab")]
    [TestCase("#13#10", "\r\n")]
    [TestCase("#13 + #10", "\r\n")]
    [TestCase("chr(65)", "A")]
    [TestCase("'It''s'", "It's")]
    public void String_expressions(string expression, string expected)
    {
        Assert.That(Evaluate(expression), Is.EqualTo(expected));
    }

    [Test]
    public void Other_expressions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Evaluate("1.5 * 2"), Is.TypeOf<double>().And.EqualTo(3.0));
            Assert.That(Evaluate("7 / 2"), Is.TypeOf<double>().And.EqualTo(3.5));
            Assert.That(Evaluate("-(2 * 3)"), Is.TypeOf<long>().And.EqualTo(-6L));
            Assert.That(Evaluate("True and not False"), Is.EqualTo(true));
            Assert.That(Evaluate("PREFIX + '_ID'", new() { ["PREFIX"] = "<Prefix>" }), Is.EqualTo("<Prefix>_ID"));
            Assert.That(Evaluate("[1, 2]"), Is.Null, "sets are not evaluated");
            Assert.That(Evaluate("Unknown + 1"), Is.Null, "names from other units are not known");
        });
    }
}
