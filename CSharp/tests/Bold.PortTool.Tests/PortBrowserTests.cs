// The port browser's core (CSharp/tools/Bold.PortBrowser.Core) on this repository: the two trees, the counterpart of a
// file or a symbol in both directions, why something has no counterpart, search, and the colours of the code.
using Bold.PortBrowser;
using Bold.PortTool.Port;

namespace Bold.Tests;

public class PortBrowserTests
{
    private static readonly Lazy<PortIndex> Loaded = new(() => PortIndex.Load(RepoPaths.Root));

    private static PortIndex Index => Loaded.Value;

    private static BrowserNode Delphi(string name) => Index.FindByName(name, side: Side.Delphi) ?? throw new AssertionException($"no Delphi {name}");

    private static BrowserNode CSharp(string name) => Index.FindByName(name, side: Side.CSharp) ?? throw new AssertionException($"no C# {name}");

    [Test]
    public void Both_trees_have_their_folders_files_types_and_members()
    {
        Assert.That(Index.DelphiRoot.Children.Select(c => c.Name), Does.Contain("Source"));
        Assert.That(Index.CSharpRoot.Children.Select(c => c.Name), Is.SupersetOf(new[] { "src", "tests" }));
        BrowserNode src = Index.CSharpRoot.Children.Single(c => c.Name == "src");
        Assert.That(src.Children.Where(c => c.Kind == NodeKind.Project).Select(c => c.Name), Is.SupersetOf(new[] { "Bold.Core", "Bold.ObjectSpace", "Bold.WinForms" }));

        BrowserNode unit = Index.FindFile("Source/ObjectSpace/BORepresentation/BoldSystem.pas")!;
        Assert.That(unit.Path, Is.EqualTo("Source/ObjectSpace/BORepresentation/BoldSystem.pas"));
        Assert.That(unit.Kind, Is.EqualTo(NodeKind.File));
        BrowserNode system = unit.Children.Single(c => c.Name == "TBoldSystem");
        Assert.That(system.Kind, Is.EqualTo(NodeKind.Type));
        Assert.That(system.Children.Select(c => c.Name), Has.Some.StartsWith("UpdateDatabase"));
        Assert.That(system.Status, Is.Not.Null);
    }

    [Test]
    public void A_Delphi_unit_leads_to_its_CSharp_file_and_back()
    {
        BrowserNode unit = Index.FindFile("Source/Common/Core/BoldDefs.pas")!;
        IReadOnlyList<BrowserNode> files = Index.CounterpartsOf(unit);
        Assert.That(files.Select(f => f.File), Does.Contain("CSharp/src/Bold.Core/Common/Core/BoldDefs.cs"));
        Assert.That(files, Has.All.Property(nameof(BrowserNode.Side)).EqualTo(Side.CSharp));

        BrowserNode file = Index.FindFile("CSharp/src/Bold.Core/Common/Core/BoldDefs.cs")!;
        Assert.That(Index.CounterpartsOf(file), Is.EqualTo(new[] { unit }));
        Assert.That(file.Status, Is.EqualTo(unit.Status));
    }

    [Test]
    public void A_Delphi_symbol_leads_to_its_CSharp_declaration_and_back()
    {
        BrowserNode delphi = Delphi("TBoldSystem");
        Assert.That(delphi.File, Is.EqualTo("Source/ObjectSpace/BORepresentation/BoldSystem.pas"));
        BrowserNode csharp = Index.CounterpartsOf(delphi).First();
        Assert.That(csharp.Declaration!.Name, Is.EqualTo("TBoldSystem"));
        Assert.That(csharp.File, Does.StartWith("CSharp/src/Bold.ObjectSpace/"));
        Assert.That(Index.CounterpartsOf(csharp), Does.Contain(delphi));

        // A member: by "Type.Member", in both directions.
        BrowserNode method = Delphi("TBoldSystem.UpdateDatabase");
        Assert.That(method.Parent, Is.SameAs(delphi));
        BrowserNode ported = Index.CounterpartsOf(method).First();
        Assert.That(ported.Declaration!.FullName, Does.EndWith("TBoldSystem.UpdateDatabase"));
        Assert.That(Index.CounterpartsOf(ported), Does.Contain(method));

        // A constant of a unit is a member of the unit's static class.
        BrowserNode constant = Delphi("NO_CLASS");
        Assert.That(Index.CounterpartsOf(constant).Single().Declaration!.FullName, Is.EqualTo("BoldDefs.NO_CLASS"));
    }

    [Test]
    public void What_has_no_counterpart_says_why()
    {
        BrowserNode guard = Index.FindFile("Source/Common/Support/BoldGuard.pas")!;
        Assert.That(guard.Status, Is.EqualTo(Indicator.Replaced));
        Assert.That(Index.CounterpartsOf(guard), Is.Empty);
        Assert.That(Index.Describe(guard).Summary, Does.StartWith("Replaced: ").And.Contain("garbage collector"));
        Assert.That(Index.WhyNoCounterpart(guard), Does.Contain("There is no C# code for BoldGuard.pas").And.Contain("garbage collector"));

        BrowserNode guardClass = guard.Children.Single(c => c.Name == "TBoldGuard");
        Assert.That(Index.Describe(guardClass).Summary, Does.StartWith("Replaced: the unit is replaced"));

        BrowserNode dropped = Index.DelphiRoot.SelfAndDescendants().First(n => n.Kind == NodeKind.File && n.Unit!.IsDropped);
        Assert.That(Index.Describe(dropped).Summary, Does.StartWith("Not ported: "));
        Assert.That(Index.Describe(dropped).Facts.Select(f => f.Key), Does.Contain("Why not ported"));

        Assert.That(Index.WhyNoCounterpart(Delphi("TBoldSystem")), Is.Null, "it has one");
    }

    [Test]
    public void The_details_name_the_status_the_project_the_milestone_and_the_tests()
    {
        NodeDetails details = Index.Describe(Delphi("TBoldSystem"));
        string[] labels = [.. details.Facts.Select(f => f.Key)];
        Assert.That(labels, Is.SupersetOf(new[] { "Delphi", "Status", "C#", "Project", "Milestone", "Tests" }));
        Assert.That(details.Facts.First(f => f.Key == "Project").Value, Is.EqualTo("Bold.ObjectSpace"));

        NodeDetails unit = Index.Describe(Index.FindFile("Source/Common/Core/BoldDefs.pas")!);
        Assert.That(unit.Summary, Does.Contain("105 of 105 symbols done"));
        Assert.That(unit.Tests, Is.Not.Empty, "the tests that cover the unit's symbols");
        Assert.That(Index.NodeAt(unit.Tests[0].File, unit.Tests[0].Line), Is.Not.Null, "a test can be opened");
    }

    [Test]
    public void New_CSharp_code_is_marked_new()
    {
        BrowserNode desk = Index.FindFile("CSharp/tests/Bold.Examples.Tests/LibraryExamplesTests.cs")!;
        Assert.That(Index.WhyNoCounterpart(desk), Does.Contain("Delphi Bold had no tests"));
        Assert.That(Index.CSharpRoot.SelfAndDescendants().Count(n => n.IsNew), Is.GreaterThan(10));
        Assert.That(PortIndex.Matches(Index.CSharpRoot, null, newOnly: true), Is.True);
    }

    [Test]
    public void A_line_of_a_file_is_in_the_declaration_above_it()
    {
        BrowserNode method = Delphi("TBoldSystem.UpdateDatabase");
        Assert.That(Index.NodeAt(method.File!, method.Line), Is.SameAs(method));
        Assert.That(Index.NodeAt(method.File!, method.Symbol!.ImplLine!.Value + 2), Is.SameAs(method), "in its body");
        Assert.That(Index.NodeAt(method.File!, 1)!.Kind, Is.EqualTo(NodeKind.File));

        BrowserNode ported = Index.CounterpartsOf(method).First();
        Assert.That(Index.NodeAt(Index.FullPath(ported.File!), ported.Line), Is.SameAs(ported), "by its full path too");
    }

    [Test]
    public void Search_finds_names_on_both_sides_the_name_itself_first()
    {
        IReadOnlyList<BrowserNode> found = Index.Search("tboldsystem");
        Assert.That(found[0].Name, Is.EqualTo("TBoldSystem"));
        Assert.That(found.Select(n => n.Side).Distinct(), Is.EquivalentTo(new[] { Side.Delphi, Side.CSharp }));
        Assert.That(found.Select(n => n.Name), Has.Some.EqualTo("TBoldSystemHandle"));
        Assert.That(Index.Search("  "), Is.Empty);
        Assert.That(Index.Search("BoldDefs").Take(3).Select(n => n.Name), Is.SupersetOf(new[] { "BoldDefs.pas", "BoldDefs.cs" }));
    }

    [Test]
    public void Totals_count_the_symbols_of_a_folder_by_status()
    {
        Totals all = Index.TotalsOf(Index.DelphiRoot);
        Assert.That(all.Units, Is.EqualTo(Index.Analysis.Units.Count));
        Assert.That(all.Symbols, Is.EqualTo(all.ByStatus.Values.Sum()));
        Assert.That(all.Count(Indicator.NotStarted), Is.Zero, "the port is complete");
        Assert.That(all.ToString(), Does.Contain("units").And.Contain("% ported"));
        BrowserNode common = Index.DelphiRoot.Children.Single(c => c.Name == "Source").Children.Single(c => c.Name == "Common");
        Assert.That(Index.TotalsOf(common).Symbols, Is.InRange(1, all.Symbols - 1));
    }

    [Test]
    public void History_goes_back_and_forward()
    {
        var history = new History();
        BrowserNode a = Delphi("TBoldSystem"), b = Delphi("TBoldObject"), c = Delphi("TBoldMember");
        history.Visit(a);
        history.Visit(b);
        history.Visit(b);
        Assert.That(history.Back(), Is.SameAs(a));
        Assert.That(history.CanGoBack, Is.False);
        Assert.That(history.Forward(), Is.SameAs(b));
        history.Back();
        history.Visit(c);
        Assert.That(history.CanGoForward, Is.False, "a new visit forgets what was ahead");
        Assert.That(history.Back(), Is.SameAs(a));
    }

    [Test]
    public void Delphi_code_is_coloured()
    {
        const string code = "unit A; // note\n{ block }\n{$IFDEF X}\nconst S = 'it''s'; N = $FF;\nbegin end.";
        string Text(CodeSpan span) => code.Substring(span.Start, span.Length);
        var spans = CodeHighlighter.Spans(code, CodeLanguage.Delphi);
        Assert.That(spans.Where(s => s.Color == CodeColor.Keyword).Select(Text), Is.EqualTo(new[] { "unit", "const", "begin", "end" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Comment).Select(Text), Is.EqualTo(new[] { "// note", "{ block }" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Directive).Select(Text), Is.EqualTo(new[] { "{$IFDEF X}" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.String).Select(Text), Is.EqualTo(new[] { "'it''s'" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Number).Select(Text), Is.EqualTo(new[] { "$FF" }));
    }

    [Test]
    public void CSharp_code_is_coloured()
    {
        const string code = "#nullable enable\npublic class A /* c */ { string s = \"a\\\"b\"; var v = @\"x\"\"y\"; char c = '\\''; int n = 0x1F; }";
        string Text(CodeSpan span) => code.Substring(span.Start, span.Length);
        var spans = CodeHighlighter.Spans(code, CodeLanguage.CSharp);
        Assert.That(spans.Where(s => s.Color == CodeColor.Keyword).Select(Text), Is.EqualTo(new[] { "public", "class", "string", "var", "char", "int" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.String).Select(Text), Is.EqualTo(new[] { "\"a\\\"b\"", "@\"x\"\"y\"", "'\\''" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Comment).Select(Text), Is.EqualTo(new[] { "/* c */" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Directive).Select(Text), Is.EqualTo(new[] { "#nullable enable" }));
        Assert.That(spans.Where(s => s.Color == CodeColor.Number).Select(Text), Is.EqualTo(new[] { "0x1F" }));
    }

    [Test]
    public void The_code_as_rich_text_keeps_its_lines_and_numbers_them()
    {
        string rtf = CodeHighlighter.ToRtf("begin\r\n  x := '{a}\\';\r\nend.", CodeLanguage.Delphi);
        Assert.That(rtf, Does.StartWith(@"{\rtf1").And.EndWith("}"));
        Assert.That(rtf.Split(@"\par").Length, Is.EqualTo(3), "a paragraph a line");
        Assert.That(rtf, Does.Contain("    1  ").And.Contain("    3  "));
        Assert.That(rtf, Does.Contain(@"'\{a\}\\'"), "braces and backslashes are escaped");
        Assert.That(CodeHighlighter.ToRtf("é", CodeLanguage.Plain, lineNumbers: false), Does.Contain(@"\u233?"));

        // Every source of the repository's largest unit becomes rich text.
        BrowserNode unit = Index.FindFile("Source/ObjectSpace/BORepresentation/BoldSystem.pas")!;
        string text = Index.ReadText(unit.File!);
        Assert.That(CodeHighlighter.ToRtf(text, CodeLanguage.Delphi).Split(@"\par").Length, Is.EqualTo(CodeHighlighter.Lines(text).Length));
    }

    [TestCase("  System.UpdateDatabase(nil);", 12, false, "UpdateDatabase")]
    [TestCase("  System.UpdateDatabase(nil);", 12, true, "System.UpdateDatabase")]
    [TestCase("  System.UpdateDatabase(nil);", 4, true, "System")]
    [TestCase("  x := 42;", 8, false, null)]
    [TestCase("  x := 42;", 5, false, null)]
    public void The_name_under_the_cursor(string line, int column, bool withOwner, string? name) =>
        Assert.That(CodeHighlighter.NameAt(line, column, withOwner), Is.EqualTo(name));

    [Test]
    public void A_name_read_in_code_is_found_from_where_it_was_read()
    {
        // In BoldSystem.pas "TBoldObject" is the class of that unit; read in C# it is the C# class.
        BrowserNode inDelphi = Delphi("TBoldSystem");
        Assert.That(Index.FindByName("tboldobject", inDelphi)!.Side, Is.EqualTo(Side.Delphi), "Delphi names are not case sensitive");
        BrowserNode inCSharp = Index.CounterpartsOf(inDelphi).First();
        Assert.That(Index.FindByName("TBoldObject", inCSharp)!.Side, Is.EqualTo(Side.CSharp));
        Assert.That(Index.FindByName("NoSuchNameAnywhere", inDelphi), Is.Null);
        Assert.That(PortIndex.FindRepository(Path.GetTempPath()), Is.Null);
        Assert.That(PortIndex.FindRepository(RepoPaths.Full("CSharp/src")), Is.EqualTo(Index.Root));
    }
}
