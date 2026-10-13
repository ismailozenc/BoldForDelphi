using Bold.PortTool.Delphi;

namespace Bold.Tests;

public class DelphiLexerTests
{
    private static List<string> Texts(string source, IEnumerable<string>? defines = null) =>
        DelphiLexer.TokenizeText(source, defines: defines).Where(t => t.Kind != TokenKind.End).Select(t => t.Text).ToList();

    [Test]
    public void Comments_of_all_three_kinds_are_dropped()
    {
        Assert.That(Texts("a { brace } b (* paren *) c // line\nd"), Is.EqualTo(new[] { "a", "b", "c", "d" }));
    }

    [Test]
    public void Strings_join_quoted_parts_and_character_codes()
    {
        var token = DelphiLexer.TokenizeText("'It''s'#13#10'ok'#$41").First();
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(TokenKind.String));
            Assert.That(token.Text, Is.EqualTo("It's\r\nokA"));
        });
    }

    [Test]
    public void Two_character_symbols_and_numbers_are_single_tokens()
    {
        Assert.That(Texts("a := $FF .. 1.5e3 <> b <= c"), Is.EqualTo(new[] { "a", ":=", "$FF", "..", "1.5e3", "<>", "b", "<=", "c" }));
    }

    [Test]
    public void Conditional_code_follows_the_defines()
    {
        const string source = "{$IFDEF BOLD_DELPHI} delphi {$ELSE} other {$ENDIF} {$IFNDEF BOLD_BCB} notbcb {$ENDIF}";
        Assert.Multiple(() =>
        {
            Assert.That(Texts(source), Is.EqualTo(new[] { "delphi", "notbcb" }));
            Assert.That(Texts(source, ["BOLD_BCB"]), Is.EqualTo(new[] { "other" }));
        });
    }

    [Test]
    public void Nested_conditions_and_defines_in_the_source_are_honoured()
    {
        const string source = "{$DEFINE X} {$IFDEF X} {$IFDEF Y} y {$ELSE} noty {$ENDIF} x {$ENDIF} {$UNDEF X} {$IFDEF X} gone {$ENDIF}";
        Assert.That(Texts(source), Is.EqualTo(new[] { "noty", "x" }));
    }

    [Test]
    public void Lines_are_counted_across_comments_and_strings()
    {
        var tokens = DelphiLexer.TokenizeText("a\n{ one\ntwo }\nb 'x'\n\nc");
        Assert.That(tokens.Where(t => t.Kind != TokenKind.End).Select(t => t.Line), Is.EqualTo(new[] { 1, 4, 4, 6 }));
    }

    [Test]
    public void Include_files_are_expanded_with_their_own_file_name_and_lines()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bold-lexer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Part.INC"), "included\nsecond");
            string unit = Path.Combine(directory, "Main.pas");
            File.WriteAllText(unit, "before\n{$I part.inc}\nafter");
            var (tokens, warnings) = DelphiLexer.Tokenize(unit);
            var words = tokens.Where(t => t.Kind != TokenKind.End).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(warnings, Is.Empty);
                Assert.That(words.Select(t => t.Text), Is.EqualTo(new[] { "before", "included", "second", "after" }));
                Assert.That(Path.GetFileName(words[2].File), Is.EqualTo("Part.INC"), "the include file is found ignoring case");
                Assert.That(words[2].Line, Is.EqualTo(2));
                Assert.That(words[3].Line, Is.EqualTo(3));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
