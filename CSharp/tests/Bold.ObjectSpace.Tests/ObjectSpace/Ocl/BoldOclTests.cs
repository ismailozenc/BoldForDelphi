// Delphi: Source/ObjectSpace/Ocl/BoldOcl.pas (with the parser, semantics, evaluator and symbol units)
// Port map: CSharp/port/units/BoldOcl.md
namespace Bold.Tests;

public class BoldOclTests
{
    private static string Eval(TBoldElement root, string ocl) => root.EvaluateExpressionAsString(ocl, BoldDefs.brDefault);

    private static TBoldMember M(TBoldObject o, string name) => o.BoldMemberByExpressionName(name);

    /// <summary>A town: Ann (30) and Bob (40) live in the house, Cy (25) in the flat; Ann's friends are Bob and Cy.</summary>
    private static (ObjectSpaceSample s, TBoldObject ann, TBoldObject bob, TBoldObject cy) Town()
    {
        var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), cy = s.NewPerson();
        TBoldObject house = s.NewBuilding(), flat = s.NewBuilding();
        M(house, "address").AsString = "House";
        M(flat, "address").AsString = "Flat";
        (string name, int age, TBoldObject home, TBoldObject p)[] people = [("Ann", 30, house, ann), ("Bob", 40, house, bob), ("Cy", 25, flat, cy)];
        foreach ((string name, int age, TBoldObject home, TBoldObject p) in people)
        {
            M(p, "firstName").AsString = name;
            ((TBAInteger)M(p, "age")).AsInteger = age;
            ((TBoldObjectReference)M(p, "home")).BoldObject = home;
        }

        var friends = (TBoldObjectList)M(ann, "friends");
        friends.Add(bob);
        friends.Add(cy);
        return (s, ann, bob, cy);
    }

    [Test]
    [Covers(typeof(TBoldOcl), "Evaluate", "DefineVariable")]
    [Covers("BoldOcl", "TBoldOcl.SyntacticParse", "TBoldOcl.SemanticCheck", "OclScannerTable", "OclParserTable", "initialization")]
    [Covers(typeof(AYaccClass), "Reduce", "StackElement")]
    [Covers(typeof(TBoldOclEvaluatorVisitor), "VisitTBoldOclOperation", "VisitTBoldOclIntLiteral", "VisitTBoldOclStrLiteral", "VisitTBoldOclNumericLiteral")]
    [Covers(typeof(TBoldOclSemanticsVisitor), "Traverse", "TraverseList", "DeduceBoldType", "CheckArgumentType")]
    public void Literals_and_arithmetic_evaluate()
    {
        using var s = new ObjectSpaceSample();
        Assert.That(Eval(s.System, "1 + 2 * 3"), Is.EqualTo("7"));
        Assert.That(Eval(s.System, "(1 + 2) * 3"), Is.EqualTo("9"));
        Assert.That(Eval(s.System, "7 div 2"), Is.EqualTo("3"));
        Assert.That(Eval(s.System, "7 mod 2"), Is.EqualTo("1"));
        Assert.That(Eval(s.System, "(-5).abs"), Is.EqualTo("5"));
        Assert.That(Eval(s.System, "'ab' + 'cd'"), Is.EqualTo("abcd"));
        Assert.That(Eval(s.System, "'Hello'.length"), Is.EqualTo("5"));
        Assert.That(Eval(s.System, "'Hello'.toUpper"), Is.EqualTo("HELLO"));
        Assert.That(Eval(s.System, "'Hello'.subString(2, 3)"), Is.EqualTo("el"));
        Assert.That(Eval(s.System, "'7'.pad(3, '0')"), Is.EqualTo("007"));
        Assert.That(Eval(s.System, "2.5.round"), Is.EqualTo("2"));
        Assert.That(Eval(s.System, "1 < 2"), Is.EqualTo("Y"));
        Assert.That(Eval(s.System, "if 1 > 2 then 'a' else 'b' endif"), Is.EqualTo("b"));
        Assert.That(Eval(s.System, "true and not false"), Is.EqualTo("Y"));
        Assert.That(Eval(s.System, @"'it\'s'"), Is.EqualTo("it's"), "a quote is escaped with a backslash");
        Assert.That(Eval(s.System, "#2001-02-03.formatDateTime('yyyy/mm/dd')"), Is.EqualTo("2001/02/03"));
    }

    [Test]
    [Covers(typeof(TBoldOclEvaluatorVisitor), "VisitTBoldOclCollectionLiteral", "VisitTBoldOclIteration")]
    [Covers("BoldOclSymbolImplementations", "TBOS_Sum.Evaluate", "TBOS_Size.Evaluate", "TBOS_Select.Evaluate", "TBOS_collect.Evaluate", "TBOS_includes.Evaluate")]
    public void Collections_and_iterations_evaluate()
    {
        using var s = new ObjectSpaceSample();
        Assert.That(Eval(s.System, "Sequence{1..4}->sum"), Is.EqualTo("10"));
        Assert.That(Eval(s.System, "Sequence{1, 2, 3}->size"), Is.EqualTo("3"));
        Assert.That(Eval(s.System, "Sequence{1..6}->select(x | x mod 2 = 0)->sum"), Is.EqualTo("12"));
        Assert.That(Eval(s.System, "Sequence{1..3}->collect(x | x * x)->sum"), Is.EqualTo("14"));
        Assert.That(Eval(s.System, "Sequence{1..3}->includes(2)"), Is.EqualTo("Y"));
        Assert.That(Eval(s.System, "Sequence{1..3}->forAll(x | x > 0)"), Is.EqualTo("Y"));
        Assert.That(Eval(s.System, "Sequence{1..3}->exists(x | x > 2)"), Is.EqualTo("Y"));
        Assert.That(Eval(s.System, "Sequence{3, 1, 2}->orderby(x | x)->first"), Is.EqualTo("1"));
        Assert.That(Eval(s.System, "Sequence{3, 1, 2}->orderdescending(x | x)->first"), Is.EqualTo("3"));
        Assert.That(Eval(s.System, "Sequence{1, 2}->union(Sequence{3})->size"), Is.EqualTo("3"));
        Assert.That(Eval(s.System, "Sequence{1, 2, 3}->at(2)"), Is.EqualTo("2"));
        Assert.That(Eval(s.System, "Sequence{1, 2, 3}->last"), Is.EqualTo("3"));
    }

    [Test]
    [Covers(typeof(TBoldOclEvaluatorVisitor), "VisitTBoldOclMember", "VisitTBoldOclVariableReference")]
    [Covers(typeof(TBoldOclSemanticsVisitor), "VisitTBoldOclMember", "VisitTBoldOclTypeNode", "TestForOperation")]
    [Covers("BoldOclSymbolImplementations", "TBOS_AllInstances.Evaluate", "TBOS_orderby.Init")]
    public void Model_navigation_evaluates()
    {
        (ObjectSpaceSample s, TBoldObject ann, TBoldObject bob, TBoldObject cy) = Town();
        using (s)
        {
            Assert.That(Eval(ann, "firstName"), Is.EqualTo("Ann"));
            Assert.That(Eval(ann, "home.address"), Is.EqualTo("House"));
            Assert.That(Eval(ann, "self.friends->size"), Is.EqualTo("2"));
            Assert.That(Eval(ann, "friends.age->sum"), Is.EqualTo("65"));
            Assert.That(Eval(ann, "home.residents->size"), Is.EqualTo("2"));
            Assert.That(Eval(s.System, "Person.allInstances->size"), Is.EqualTo("3"));
            Assert.That(Eval(s.System, "Person.allInstances->select(age > 26)->size"), Is.EqualTo("2"));
            Assert.That(Eval(s.System, "Person.allInstances->orderby(age)->first.firstName"), Is.EqualTo("Cy"));
            Assert.That(Eval(s.System, "Person.allInstances.firstName->includes('Bob')"), Is.EqualTo("Y"), "an implicit collect");
            Assert.That(Eval(bob, "friendOf->includes(self)"), Is.EqualTo("N"));
            Assert.That(Eval(cy, "self.oclIsKindOf(Person)"), Is.EqualTo("Y"));
            Assert.That(Eval(cy, "self.oclType.typename"), Is.EqualTo("Person"));
        }
    }

    [Test]
    [Covers(typeof(TBoldOcl), "ExpressionType", "RTInfo")]
    public void Expression_types_and_member_info()
    {
        using var s = new ObjectSpaceSample();
        TBoldClassTypeInfo person = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!;
        TBoldEvaluator evaluator = s.SystemTypeInfo.Evaluator!;
        Assert.That(evaluator.ExpressionType("age + 1", person, true)!.ExpressionName, Is.EqualTo("Integer"));
        Assert.That(evaluator.ExpressionType("friends", person, true), Is.InstanceOf<TBoldListTypeInfo>());
        Assert.That(evaluator.ExpressionType("", person, true), Is.SameAs(person));
        Assert.That(evaluator.ExpressionType("nonsense", person, false), Is.Null);
        Assert.That(((TBoldRTEvaluator)evaluator).RTInfo("home", person, true)!.ExpressionName, Is.EqualTo("home"));
    }

    [Test]
    [Covers(typeof(EBoldOCLAbort), "FixError")]
    public void Errors_carry_positions()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        // Element evaluation wraps OCL errors in EBold, with the expression; the evaluator raises them positioned.
        var unknown = Assert.Throws<EBold>(() => Eval(p, "nonsense"))!;
        Assert.That(unknown.InnerException, Is.InstanceOf<EBoldOCLAbort>());
        Assert.That(unknown.Message, Does.Contain("nonsense is not a member of Person"));
        TBoldEvaluator evaluator = s.SystemTypeInfo.Evaluator!;
        var paren = Assert.Throws<EBoldOCLAbort>(() => evaluator.Evaluate("age + (1 + 2", p, null, false, null))!;
        Assert.That(paren.Ocl, Is.EqualTo("age + (1 + 2"));
        Assert.That(paren.Position, Is.EqualTo(6), "the unmatched parenthesis");
        Assert.Throws<EBoldOclRunTimeError>(() => evaluator.Evaluate("1 / 0", s.System, null, false, new TBoldIndirectElement()));
    }
}
