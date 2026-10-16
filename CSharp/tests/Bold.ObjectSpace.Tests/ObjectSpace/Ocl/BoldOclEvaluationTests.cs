// Delphi: Source/ObjectSpace/Ocl/BoldOcl.pas, BoldOclLightWeightNodes.pas, BoldOclLightWeightNodeMaker.pas, BoldOclRTDebug.pas
// Port map: CSharp/port/units/BoldOcl.md
using System.Xml;

namespace Bold.Tests;

public class BoldOclEvaluationTests
{
    private static TBoldMember M(TBoldObject o, string name) => o.BoldMemberByExpressionName(name);

    private sealed class Variable(string name, TBoldElement value) : TBoldExternalVariable(name)
    {
        protected override TBoldElement? GetValue() => value;
        protected override TBoldElementTypeInfo? GetValueType() => value.BoldType;
    }

    /// <summary>Records the OCL condition of a fetch and returns no ids.</summary>
    private sealed class RecordingController : TBoldPersistenceControllerPassthrough
    {
        public TBoldCondition? Condition;

        public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
            TBoldClientID BoldClientID) => this.Condition = Condition;
    }

    private sealed class Debugger : TBoldOclRTDebugger
    {
        public readonly List<string> Failures = [];
        public override bool HasFixFor(string Ocl, TBoldElementTypeInfo? Context) => Ocl == "fixme";
        public override string GetFixFor(string Ocl, TBoldElementTypeInfo? Context) => "firstName";

        public override bool AddFixFor(string Ocl, TBoldElementTypeInfo? Context, string ComponentPath, string Message)
        {
            Failures.Add(Message);
            return false;
        }
    }

    [Test]
    [Covers(typeof(TBoldOcl), "Evaluate")]
    [Covers("BoldOcl", "TBoldOcl.AddVarsToEnv")]
    public void External_variables_are_bound()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        ((TBAInteger)M(ann, "age")).AsInteger = 30;
        var limit = (TBAInteger)TBoldMemberFactory.CreateMemberFromBoldType(s.SystemTypeInfo.AttributeTypeInfoByExpressionName("Integer"));
        limit.AsInteger = 25;
        var variables = new TBoldExternalVariableList();
        variables.Add(new Variable("limit", limit));
        Assert.That(ann.EvaluateExpressionAsString("age > limit", BoldDefs.brDefault, false, variables), Is.EqualTo("Y"));
        limit.AsInteger = 35;
        Assert.That(ann.EvaluateExpressionAsString("age > limit", BoldDefs.brDefault, false, variables), Is.EqualTo("N"), "variables are not cached in the tree");
        Assert.That(s.SystemTypeInfo.Evaluator!.ExpressionType("limit + 1", null, true, variables)!.ExpressionName, Is.EqualTo("Integer"));
    }

    [Test]
    [Covers(typeof(TBoldOclEvaluatorVisitor), "SubScribeToElem")]
    [Covers("BoldOclEvaluator", "MapResubscribe")]
    public void Evaluation_subscribes_to_what_the_result_depends_on()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        M(ann, "firstName").AsString = "Ann";
        int events = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, _, _) => events++);
        var result = new TBoldIndirectElement();
        ann.EvaluateAndSubscribeToExpression("firstName.length", subscriber, result);
        Assert.That(((TBAInteger)result.Value!).AsInteger, Is.EqualTo(3));
        M(ann, "firstName").AsString = "Anna";
        Assert.That(events, Is.GreaterThan(0), "a change of the name reaches the subscriber");
    }

    [Test]
    [Covers(typeof(TBoldOClDictionary), "*")]
    [Covers(typeof(TBoldOclEntry), "*")]
    [Covers("BoldOcl", "TOCLIndex", "TOCLIndex.ItemAsKeyString")]
    public void Parsed_expressions_are_cached()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson();
        M(ann, "firstName").AsString = "Ann";
        M(bob, "firstName").AsString = "Bob";
        Assert.That(ann.EvaluateExpressionAsString("firstName + '!'", BoldDefs.brDefault), Is.EqualTo("Ann!"));
        Assert.That(bob.EvaluateExpressionAsString("firstName + '!'", BoldDefs.brDefault), Is.EqualTo("Bob!"), "the cached tree is evaluated again");

        var dictionary = new TBoldOClDictionary();
        var entry = new TBoldOclEntry("1", null);
        dictionary.AddOcl(entry);
        Assert.That(dictionary.OclEntryByExpressionString("1"), Is.SameAs(entry));
        Assert.That(dictionary.OclEntryByExpressionString("2"), Is.Null);
        Assert.That(entry.firstSemanticPass, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldOclRTDebugger), "*")]
    [Covers("BoldOcl", "BoldOCLRTDebugger")]
    public void The_run_time_debugger_supplies_fixes_and_hears_failures()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        M(ann, "firstName").AsString = "Ann";
        var debugger = new Debugger();
        BoldOcl.BoldOCLRTDebugger = debugger;
        try
        {
            Assert.That(ann.EvaluateExpressionAsString("fixme", BoldDefs.brDefault), Is.EqualTo("Ann"));
            Assert.Throws<EBold>(() => ann.EvaluateExpressionAsString("nonsense", BoldDefs.brDefault));
            Assert.That(debugger.Failures, Has.Count.EqualTo(1));
        }
        finally
        {
            BoldOcl.BoldOCLRTDebugger = null;
        }
    }

    [Test]
    [Covers("BoldOcl", "BoldForceNelCompatibility", "BoldNelCompatibility", "BoldOCLAllowCapitalMembers", "TBoldOcl.LookupOclDefinition")]
    [Covers(typeof(TBoldOcl), "SetLookupOclDefinition")]
    public void Definitions_and_compatibility_switches()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        M(ann, "firstName").AsString = "Ann";
        TBoldEvaluator evaluator = ann.Evaluator!;
        Assert.Throws<EBold>(() => ann.EvaluateExpressionAsString("%name", BoldDefs.brDefault), "no definitions without a lookup");
        evaluator.SetLookupOclDefinition(name => name == "name" ? "firstName" : "");
        Assert.That(ann.EvaluateExpressionAsString("%name", BoldDefs.brDefault), Is.EqualTo("Ann"));

        bool nel = BoldOcl.BoldNelCompatibility, capital = BoldOcl.BoldOCLAllowCapitalMembers;
        try
        {
            BoldOcl.BoldForceNelCompatibility();
            Assert.That(BoldOcl.BoldNelCompatibility && BoldOcl.BoldOCLAllowCapitalMembers, Is.True);
            Assert.That(ann.EvaluateExpressionAsString("firstName.", BoldDefs.brDefault), Is.EqualTo("Ann"), "a trailing dot is ignored");
        }
        finally
        {
            BoldOcl.BoldNelCompatibility = nel;
            BoldOcl.BoldOCLAllowCapitalMembers = capital;
        }
    }

    [Test]
    [Covers(typeof(TBoldOLWNodeMaker), "*")]
    [Covers(typeof(TBoldOclCondition), "*")]
    [Covers("BoldOcl", "TBoldOcl.PSEvaluation")]
    public void Object_list_expressions_evaluate_in_the_persistence_layer()
    {
        var controller = new RecordingController();
        using var s = new ObjectSpaceSample(controller: _ => controller);
        TBoldObject ann = s.NewPerson();
        var result = new TBoldIndirectElement();
        s.System.EvaluateExpression("Person.allInstances->select(p | p.age > 26)", result, true);
        Assert.That(result.Value, Is.InstanceOf<TBoldObjectList>());
        var condition = (TBoldOclCondition)controller.Condition!;
        Assert.That(condition.OclExpr, Is.EqualTo("Person.allInstances->select(p | p.age > 26)"));
        Assert.That(condition.TopSortedIndex, Is.EqualTo(ann.BoldClassTypeInfo.TopSortedIndex));
        var select = (TBoldOLWIteration)condition.RootNode!;
        Assert.That(select.OperationName, Is.EqualTo("select"));
        Assert.That(select.LoopVar!.VariableName, Is.EqualTo("P"));
        Assert.That(((TBoldOLWOperation)select.Args[0]).OperationName, Is.EqualTo("allInstances"));
        Assert.That(((TBoldOLWTypeNode)((TBoldOLWOperation)select.Args[0]).Args[0]).TypeName, Is.EqualTo("Person"));
        var greater = (TBoldOLWOperation)select.Args[1];
        Assert.That(greater.OperationName, Is.EqualTo(">"));
        var age = (TBoldOLWMember)greater.Args[0];
        Assert.That((age.MemberName, age.IsBoolean), Is.EqualTo(("age", false)));
        Assert.That(((TBoldOLWVariableReference)age.MemberOf!).VariableBinding, Is.SameAs(select.LoopVar));
        Assert.That(((TBoldOLWIntLiteral)greater.Args[1]).IntValue, Is.EqualTo(26));

        var notAList = Assert.Throws<EBold>(() => s.System.EvaluateExpression("1 + 1", new TBoldIndirectElement(), true))!;
        Assert.That(notAList.InnerException, Is.InstanceOf<EBoldOCLError>());
    }

    [Test]
    [Covers("BoldOclLightWeightNodes", "TBoldXMLOCLConditionStreamer", "TBoldXMLOCLConditionStreamer.WriteObject", "TBoldXMLOCLConditionStreamer.ReadObject",
        "TBoldXMLOCLConditionStreamer.CreateObject", "TBoldXMLOCLConditionStreamer.GetStreamName", "TBoldXMLOLWNodeListStreamer",
        "TBoldXMLOLWNodeListStreamer.WriteObject", "TBoldXMLOLWNodeListStreamer.ReadObject", "TBoldXMLOLWIterationStreamer",
        "TBoldXMLOLWIterationStreamer.WriteObject", "TBoldXMLOLWIterationStreamer.ReadObject", "TBoldXMLOLWVariableReferenceStreamer",
        "TBoldXMLOLWVariableReferenceStreamer.WriteObject", "TBoldXMLOLWVariableReferenceStreamer.ReadObject", "WriteBindingToStream", "ReadBindingFromStream",
        "initialization")]
    [Covers(typeof(TBoldOLWNodeList), "*")]
    [Covers(typeof(TBoldOLWIteration), "*")]
    [Covers(typeof(TBoldOLWVariableBinding), "*")]
    public void OCL_conditions_stream_with_shared_bindings()
    {
        var loopVar = new TBoldOLWVariableBinding(3, "x", 2) { IsLoopVar = true };
        var iteration = new TBoldOLWIteration(1, "select", loopVar);
        iteration.Args.Add(new TBoldOLWTypeNode(0, "Person", 2));
        var compare = new TBoldOLWOperation(5, "=");
        compare.Args.Add(new TBoldOLWMember(7, "name", 1, new TBoldOLWVariableReference(7, loopVar), false));
        compare.Args.Add(new TBoldOLWStrLiteral(9, "Ann"));
        iteration.Args.Add(compare);
        var condition = new TBoldOclCondition { RootNode = iteration, OclExpr = "Person->select(x | x.name = 'Ann')", TopSortedIndex = 2 };
        condition.Env.Add(new TBoldOLWVariableBinding(0, "self", -1) { ExternalVarvalue = 42 });

        var manager = new TBoldXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry());
        var doc = new XmlDocument();
        manager.NewRootNode(doc, "Root").WriteObject("", condition);
        var reread = new XmlDocument();
        reread.LoadXml(doc.OuterXml);
        var r = (TBoldOclCondition)manager.GetRootNode(reread, "Root").ReadObject("")!;

        Assert.That((r.OclExpr, r.TopSortedIndex, r.Context.Count, r.Env.Count), Is.EqualTo((condition.OclExpr, 2, 0, 1)));
        Assert.That(r.Env[0] is TBoldOLWVariableBinding { VariableName: "self", ExternalVarvalue: "42" }, Is.True, "external values come back as strings");
        var rIteration = (TBoldOLWIteration)r.RootNode!;
        Assert.That((rIteration.Position, rIteration.OperationName, rIteration.LoopVar!.IsLoopVar), Is.EqualTo((1, "select", true)));
        Assert.That(((TBoldOLWTypeNode)rIteration.Args[0]).TypeName, Is.EqualTo("Person"));
        var rMember = (TBoldOLWMember)((TBoldOLWOperation)rIteration.Args[1]).Args[0];
        Assert.That(((TBoldOLWVariableReference)rMember.MemberOf!).VariableBinding, Is.SameAs(rIteration.LoopVar), "the reference shares the loop variable");
        Assert.That(((TBoldOLWStrLiteral)((TBoldOLWOperation)rIteration.Args[1]).Args[1]).StrValue, Is.EqualTo("Ann"));

        var external = new TBoldOLWVariableBinding(0, "v", -1);
        external.AddRef();
        Assert.Throws<EBold>(external.AddRef, "an external variable can be referenced once");
    }

    [Test]
    [Covers(typeof(TBoldOLWNodeVisitor), "*")]
    [Covers(typeof(TBoldOLWDateLiteral), "*")]
    [Covers(typeof(TBoldOLWTimeLiteral), "*")]
    [Covers(typeof(TBoldOLWFloatLiteral), "*")]
    [Covers(typeof(TBoldOLWEnumLiteral), "*")]
    [Covers(typeof(TBoldOLWListCoercion), "*")]
    public void Literals_accept_visitors_and_stream()
    {
        var list = new TBoldOLWNodeList();
        list.Add(new TBoldOLWDateLiteral(1, new DateTime(2001, 2, 3)));
        list.Add(new TBoldOLWTimeLiteral(2, SysUtils.EncodeTime(4, 5, 6, 0)));
        list.Add(new TBoldOLWFloatLiteral(3, 1.5));
        list.Add(new TBoldOLWEnumLiteral(4, "red") { Intvalue = 2 });
        list.Add(new TBoldOLWListCoercion(5, new TBoldOLWIntLiteral(6, 7)));
        list.TraverseList(new TBoldOLWNodeVisitor());
        Assert.That(list.IndexOf(list[2]), Is.EqualTo(2));

        var manager = new TBoldXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry());
        var doc = new XmlDocument();
        manager.NewRootNode(doc, "Root").WriteObject("", list);
        var r = (TBoldOLWNodeList)manager.GetRootNode(doc, "Root").ReadObject("")!;
        Assert.That(r.Count, Is.EqualTo(5));
        Assert.That(((TBoldOLWDateLiteral)r[0]).DateValue, Is.EqualTo(new DateTime(2001, 2, 3)));
        Assert.That(((TBoldOLWTimeLiteral)r[1]).TimeValue.TimeOfDay, Is.EqualTo(new TimeSpan(4, 5, 6)));
        Assert.That(((TBoldOLWFloatLiteral)r[2]).FloatValue, Is.EqualTo(1.5));
        Assert.That(r[3] is TBoldOLWEnumLiteral { Name: "red", Intvalue: 2 }, Is.True);
        Assert.That(((TBoldOLWIntLiteral)((TBoldOLWListCoercion)r[4]).Child!).IntValue, Is.EqualTo(7));
    }
}
