// Delphi: Source/Common/Support/BoldTemplateExpander.pas
// Port map: CSharp/port/units/BoldTemplateExpander.md
using static Bold.BoldTemplateExpander;

namespace Bold.Tests;

public class BoldTemplateExpanderTests
{
    private const string CRLF = "\r\n";

    private static TBoldTemplateVariables Vars(params (string name, string value)[] values)
    {
        var variables = new TBoldTemplateVariables();
        foreach ((string name, string value) in values)
        {
            variables.SetVariable(name, value);
        }

        return variables;
    }

    [Test]
    [Covers(typeof(TBoldTemplateVariables), "*")]
    [Covers(typeof(TBoldTemplateVariable), "*")]
    [Covers("BoldTemplateExpander", "TBoldVariableFlags", "TBoldTemplatevariableIndex", "TBoldTemplatevariableIndex.ItemAsKeyString")]
    public void Variables_are_found_by_name_ignoring_case()
    {
        var variables = new TBoldTemplateVariables();
        variables.Add("Name", "Ann", DelphiSet<int>.Of(bvfPersistent));
        variables.Add("NAME", "Bob", DelphiSet<int>.Empty);
        Assert.That(variables.Values("name"), Is.EqualTo("Ann"), "Add keeps an existing variable");
        Assert.That(variables.Exists("nAmE"), Is.True);
        variables.Change("Name", "Cy");
        variables.Change("Other", "x");
        Assert.That((variables.Values("Name"), variables.Exists("Other")), Is.EqualTo(("Cy", false)), "Change does not add");
        variables.SetVariable("Other", "y");
        Assert.That(variables.Values("Other"), Is.EqualTo("y"), "SetVariable adds");
        variables.Remove("Other");
        Assert.That(variables.Values("Other"), Is.EqualTo(""), "a missing variable is empty");
        variables.ForceAdd("Name", "Dee", DelphiSet<int>.Empty);
        Assert.That(variables.Count, Is.EqualTo(2), "ForceAdd adds a second variable with the name");
        var variable = new TBoldTemplateVariable("V", "1", DelphiSet<int>.Of(bvfReadOnly));
        Assert.That((variable.Name, variable.Value, variable.Flags.Contains(bvfReadOnly)), Is.EqualTo(("V", "1", true)));
        Assert.That((BoldBooleanToTemplateVar(true), BoldBooleanToTemplateVar(false)), Is.EqualTo(("1", "0")));
        Assert.That(variables.ExpandString("$(NAME) on $(DATE)"), Is.EqualTo("Cy on " + SysUtils.DateToStr(DateTime.Now)));
    }

    [TestCase("Hi $(NAME)!", "Hi Ann!")]
    [TestCase("$(+NAME)$(-NAME)", "ANNann")]
    [TestCase("[$(<5NAME)][$(>5NAME)][$(<2NAME)]", "[Ann  ][  Ann][Ann]")]
    [TestCase("$(?ONE:yes,no) $(?ZERO:yes,no)", "yes no")]
    [TestCase("$(,ONE:a)$(,ZERO:b)", "a")]
    [TestCase("$(MISSING)|$(NESTED)", "|Ann")]
    [TestCase("a$(CRLF)b", "a\r\nb")]
    [TestCase("joined\\\r\nline", "joinedline")]
    [TestCase("no macro end $(NAME", "no macro end $(NAME")]
    [Covers("BoldTemplateExpander", "BoldExpandTemplate")]
    public void Macros_expand(string template, string expected)
    {
        TBoldTemplateVariables variables = Vars(("NAME", "Ann"), ("ONE", "1"), ("ZERO", "0"), ("NESTED", "$(NAME)"));
        Assert.That(BoldExpandTemplate(template, variables), Is.EqualTo(expected));
    }

    [Test]
    [Covers("BoldTemplateExpander", "BoldExpandTemplate")]
    public void Loops_repeat_with_the_index_and_find_outer_variables()
    {
        TBoldTemplateVariables variables = Vars(("CLASSCOUNT", "2"), ("CLASS.0", "A"), ("CLASS.1", "B"), ("UNIT", "U"),
            ("MEMBERCOUNT.0", "1"), ("MEMBERCOUNT.1", "2"), ("MEMBER.0.0", "a0"), ("MEMBER.1.0", "b0"), ("MEMBER.1.1", "b1"));
        string template = "$(LOOPCLASSCOUNT)$(UNIT).$(CLASS)[$(LOOPMEMBERCOUNT)$(MEMBER),$(ENDLOOPMEMBERCOUNT)]$(ENDLOOPCLASSCOUNT)";
        Assert.That(BoldExpandTemplate(template, variables), Is.EqualTo("U.A[a0,]U.B[b0,b1,]"));
        Assert.That(variables.Values("CLASSCOUNT"), Is.EqualTo("2"), "the loop variable gets its count back");
        Assert.Throws<Exception>(() => BoldExpandTemplate("$(LOOPCLASSCOUNT)x", variables), "an unterminated loop");
    }

    [Test]
    [Covers("BoldTemplateExpander", "BoldExpandTemplate")]
    public void Cases_keep_the_lines_of_the_value_or_else()
    {
        string template = "$(CASEKIND)\\" + CRLF + "Attribute|attr $(NAME)" + CRLF + "Role|role" + CRLF + "Role|more" + CRLF + "ELSE|other" + CRLF + "$(ENDCASEKIND)\\" + CRLF + "end";
        Assert.That(BoldExpandTemplate(template, Vars(("KIND", "attribute"), ("NAME", "x"))), Is.EqualTo("attr x" + CRLF + "end"), "case values ignore case");
        Assert.That(BoldExpandTemplate(template, Vars(("KIND", "Role"))), Is.EqualTo("role" + CRLF + "more" + CRLF + "end"));
        Assert.That(BoldExpandTemplate(template, Vars(("KIND", "Method"))), Is.EqualTo("other" + CRLF + "end"));
        Assert.Throws<Exception>(() => BoldExpandTemplate("$(CASEKIND)x|y", Vars()), "an unterminated case");
    }

    [Test]
    [Covers("BoldTemplateExpander", "BoldExpandTemplate")]
    public void Macros_define_variables()
    {
        TBoldTemplateVariables variables = Vars(("NAME", "Ann"));
        Assert.That(BoldExpandTemplate("$(MACROSTART Greeting)Hi $(NAME)$(MACROEND Greeting)<$(Greeting)>", variables), Is.EqualTo("<Hi Ann>"));
        Assert.Throws<Exception>(() => BoldExpandTemplate("$(MACROSTART X)y", variables), "an unterminated macro");
    }

    [Test]
    [Covers(typeof(TBoldTemplateHolder), "*")]
    [Covers(typeof(TBoldTemplateList), "*")]
    public void Holders_expand_their_template_and_file_name()
    {
        using var holder = new TBoldTemplateHolder { FileName = "$(UNIT).pas", Description = "Main unit" };
        holder.Template.Text = "unit $(UNIT);" + CRLF + CRLF + "end." + CRLF + CRLF + "  " + CRLF;
        Assert.That(holder.Template.Count, Is.EqualTo(3), "trailing empty lines are dropped");
        holder.Variables.SetVariable("UNIT", "Town");
        Assert.That(holder.ExpandedFileName, Is.EqualTo("Town.pas"));
        Assert.That(holder.ExpandedTemplate.Text, Is.EqualTo("unit Town;" + CRLF + CRLF + "end." + CRLF));
        Assert.That(holder.LastExpansion, Is.SameAs(holder.ExpandedTemplate));
        Assert.That(holder.Description, Is.EqualTo("Main unit"));
        var copy = new TStringList { Text = "x" };
        holder.Template = copy;
        Assert.That(holder.Template.Text, Is.EqualTo("x\r\n"));
        Assert.That(holder.Template, Is.Not.SameAs(copy), "the template is copied, not shared");
        var list = new TBoldTemplateList();
        list.Add(holder);
        Assert.That(list[0], Is.SameAs(holder));
    }
}
