// Delphi: Source/ObjectSpace/Core/BoldElements.pas
// Port map: CSharp/port/units/BoldElements.md
namespace Bold.Tests;

public class BoldElementsTests
{
    /// <summary>An evaluator that understands a few fixed expressions.</summary>
    private sealed class FakeEvaluator : TBoldEvaluator
    {
        public readonly List<string> Variables = [];
        public TBoldLookUpOclDefinition? Lookup;

        public override void DefineVariable(string VariableName, TBoldElement? VarValue, TBoldElementTypeInfo? VariableType, bool OwnValue) =>
            Variables.Add(VariableName);

        public override void Evaluate(string Ocl, TBoldElement? Root, TBoldSubscriber? Subscriber, bool ResubscribeAll, TBoldIndirectElement? resultElement,
            bool EvaluateInPS = false, TBoldExternalVariableList? VariableList = null)
        {
            switch (Ocl)
            {
                case "self":
                    resultElement?.SetReferenceValue(Root);
                    break;
                case "new":
                    resultElement?.SetOwnedValue(new Value(((Value)Root!).Type, "made"));
                    break;
                case "nil":
                    resultElement?.SetReferenceValue(null);
                    break;
                case "abort":
                    throw new EBoldOCLAbort("3:stopped") { Ocl = Ocl };
                case "error":
                    throw new EBoldOCLError("4:bad") { Ocl = Ocl };
                default:
                    throw new InvalidOperationException(Ocl);
            }
        }

        public override TBoldElementTypeInfo? ExpressionType(string Ocl, TBoldElementTypeInfo? Context, bool ReRaise, TBoldExternalVariableList? VariableList = null) => Context;

        public override void SetLookupOclDefinition(TBoldLookUpOclDefinition? value) => Lookup = value;
    }

    private sealed class SystemType(FakeEvaluator evaluator) : TBoldElementTypeInfo("System", "System", "TSystem", null)
    {
        protected override TBoldEvaluator? GetEvaluator() => evaluator;
        protected override TBoldElementTypeInfo? GetBoldType() => this;
        public override bool ConformsTo(TBoldElementTypeInfo? Element) => Element == this;
        public void SetKind(TBoldValueTypeSet kind) => SetValueType(kind);
    }

    private sealed class ValueType(SystemType system) : TBoldElementTypeInfo("Value", "value", "TValue", system)
    {
        protected override TBoldElementTypeInfo? GetBoldType() => null;
        public override bool ConformsTo(TBoldElementTypeInfo? Element) => Element == this;
    }

    /// <summary>A string value with its type, comparison, assignment and string representations.</summary>
    private sealed class Value(TBoldElementTypeInfo? type, string text) : TBoldElement
    {
        public string Text = text;
        public TBoldElementTypeInfo? Type => type;

        protected override TBoldElementTypeInfo? GetBoldType() => type;
        public override string GetStringRepresentation(TBoldRepresentation Representation) => Text;

        public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
        {
            if (!Mutable)
            {
                MutableError(Value);
            }

            Text = Value;
        }

        public override bool ValidateCharacter(char C, TBoldRepresentation Representation) => C != '!';

        public override int CompareToAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
        {
            if (BoldElement is not Value other)
            {
                CompareError(BoldElement);
                return 0;
            }

            if (CompareType == TBoldCompareType.ctAsDate)
            {
                CompareTypeError(CompareType, BoldElement);
            }

            return string.CompareOrdinal(Text, other.Text);
        }

        public override void Assign(TBoldElement? Source)
        {
            if (Source is Value v)
            {
                Text = v.Text;
            }
            else
            {
                AssignError(Source);
            }
        }

        protected override TBoldElement? CloneIfPossible() => new Value(type, Text);
        public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = BoldSubscription.breReEvaluate) { }
        public override void GetAsList(TBoldIndirectElement ResultList) => ResultList.SetOwnedValue(new Value(type, "[" + Text + "]"));
        public TBoldEvaluator? EvaluatorOf() => GetEvaluator();
    }

    /// <summary>An element with no overrides: everything that should raise does.</summary>
    private sealed class Bare : TBoldElement
    {
        protected override TBoldElementTypeInfo? GetBoldType() => null;
        public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = BoldSubscription.breReEvaluate) { }
        public override void GetAsList(TBoldIndirectElement ResultList) { }
        public TBoldElement? Clone() => CloneIfPossible();
    }

    private sealed class Variable(string name, TBoldElement? value) : TBoldExternalVariable(name)
    {
        protected override TBoldElement? GetValue() => value;
        protected override TBoldElementTypeInfo? GetValueType() => value?.BoldType;
    }

    private sealed class OclComponent : IBoldOCLComponent
    {
        private string expression = "";
        public TBoldElementTypeInfo? GetContextType() => null;
        public void SetExpression(string Expression) => expression = Expression;
        public TBoldExternalVariableList? GetVariableList() => null;
        public string GetExpression() => expression;
    }

    [Test]
    [Covers(typeof(BoldElements), "*")]
    [Covers(typeof(TBoldValueTypeSet))]
    public void Constants_and_enums_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldElements), "Source/ObjectSpace/Core/BoldElements.pas");
        PortAssert.EnumsMatchDelphi("Source/ObjectSpace/Core/BoldElements.pas", typeof(TBoldValueTypeSet));
    }

    [Test]
    [Covers(typeof(BoldCoreConsts), "*")]
    public void Core_messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldCoreConsts), "Source/ObjectSpace/Core/BoldCoreConsts.pas");
    }

    [Test]
    [Covers(typeof(TBoldElement), nameof(TBoldElement.GetStringRepresentation), nameof(TBoldElement.SetStringRepresentation),
        nameof(TBoldElement.StringRepresentation), nameof(TBoldElement.AsString), nameof(TBoldElement.CompareToAs), nameof(TBoldElement.Assign),
        nameof(TBoldElement.ValidateCharacter), nameof(TBoldElement.ValidateString), nameof(TBoldElement.EnsureValidString),
        nameof(TBoldElement.ObserverMayModify), nameof(TBoldElement.ObserverMayModifyAsString), nameof(TBoldElement.SubscribeToStringRepresentation),
        nameof(TBoldElement.GetAsVariant), nameof(TBoldElement.SetAsVariant), nameof(TBoldElement.BoldType))]
    [Covers("BoldElements", "TBoldElement.Evaluator", "TBoldElement.GetEvaluator", "TBoldElement.CloneIfPossible", "TBoldElement.GetBoldType")]
    public void Base_element_raises_for_what_it_does_not_support()
    {
        using var bare = new Bare();
        var e = Assert.Throws<EBold>(() => _ = bare.AsString)!;
        Assert.That(e.Message, Is.EqualTo("Bare: representation not supported 1"));
        Assert.Throws<EBold>(() => bare.SetStringRepresentation(BoldDefs.brShort, "x"));
        Assert.Throws<EBold>(() => bare.StringRepresentation(BoldDefs.brLong));
        e = Assert.Throws<EBold>(() => bare.CompareTo(bare))!;
        Assert.That(e.Message, Is.EqualTo("Bare.CompareToAs: Abstract error"));
        e = Assert.Throws<EBold>(() => bare.Assign(bare))!;
        Assert.That(e.Message, Is.EqualTo("Bare.Assign does not support assigning from Bare"));
        e = Assert.Throws<EBold>(() => bare.Assign(null))!;
        Assert.That(e.Message, Is.EqualTo("Bare.Assign does not support assigning nil"));
        e = Assert.Throws<EBold>(() => _ = bare.Evaluator)!;
        Assert.That(e.Message, Is.EqualTo("Bare.GetEvaluator: Element has no type, can not get evaluator"));
        Assert.That(bare.BoldType, Is.Null);
        Assert.That(bare.Clone(), Is.Null, "elements cannot be cloned by default");
        Assert.That(bare.ValidateString("anything!", BoldDefs.brDefault), Is.True);
        Assert.That(bare.ObserverMayModify(this), Is.False);
        Assert.That(bare.ObserverMayModifyAsString(BoldDefs.brDefault, null), Is.False);
        bare.SubscribeToStringRepresentation(BoldDefs.brDefault, null);
        Assert.Throws<EBold>(() => bare.GetAsVariant());
        Assert.Throws<EBold>(() => bare.SetAsVariant(1));
    }

    [Test]
    [Covers(typeof(TBoldElement), nameof(TBoldElement.IsEqual), nameof(TBoldElement.IsEqualAs), nameof(TBoldElement.CompareTo),
        nameof(TBoldElement.MakeImmutable), nameof(TBoldElement.Mutable), nameof(TBoldElement.GetAsValue), nameof(TBoldElement.Dispose),
        nameof(TBoldElement.PrepareToDestroy), nameof(TBoldElement.DefaultSubscribe), nameof(TBoldElement.GetAsList))]
    [Covers("BoldElements", "TBoldElement.CompareError", "TBoldElement.AssignError", "TBoldElement.MutableError", "TBoldElement.CompareTypeError",
        "TBoldElement.GetMutable")]
    public void Elements_compare_assign_and_can_be_made_immutable()
    {
        using var a = new Value(null, "a");
        using var b = new Value(null, "b");
        Assert.That(a.CompareTo(b), Is.LessThan(0));
        Assert.That(a.IsEqual(new Value(null, "a")), Is.True);
        Assert.That(a.IsEqualAs(TBoldCompareType.ctAsString, b), Is.False);
        var e = Assert.Throws<EBold>(() => a.CompareTo(null))!;
        Assert.That(e.Message, Is.EqualTo("Value: Cannot compare to nil"));
        using var bare = new Bare();
        e = Assert.Throws<EBold>(() => a.CompareTo(bare))!;
        Assert.That(e.Message, Is.EqualTo("Value: Cannot compare with a Bare"));
        e = Assert.Throws<EBold>(() => a.CompareToAs(TBoldCompareType.ctAsDate, b))!;
        Assert.That(e.Message, Is.EqualTo("Value: Comparetype 'ctAsDate' not supported when comparing to Value"));
        Assert.Throws<EBold>(() => a.Assign(bare));

        a.Assign(b);
        Assert.That(a.AsString, Is.EqualTo("b"));
        a.SetAsVariant(12);
        Assert.That(a.GetAsVariant(), Is.EqualTo("12"));
        a.EnsureValidString("fine", BoldDefs.brDefault);
        Assert.Throws<EBoldAssertionFailed>(() => a.EnsureValidString("bad!", BoldDefs.brDefault));

        Assert.That(a.Mutable, Is.True);
        a.MakeImmutable();
        Assert.That(a.Mutable, Is.False);
        e = Assert.Throws<EBold>(() => a.AsString = "c")!;
        Assert.That(e.Message, Is.EqualTo("Value: Tried to change the value of an immutable element from '12' to 'c'"));

        using var holder = new TBoldIndirectElement();
        a.GetAsValue(holder);
        Assert.That(holder.Value, Is.SameAs(a));
        Assert.That(holder.OwnsValue, Is.False);
        a.GetAsList(holder);
        Assert.That(holder.Value!.AsString, Is.EqualTo("[12]"));

        int destroying = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, ev, _) => destroying += ev == BoldSubscription.beDestroying ? 1 : 0);
        a.AddSmallSubscription(subscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.breReEvaluate);
        a.Dispose();
        Assert.That(destroying, Is.EqualTo(1), "disposing tells the subscribers");
        Assert.That(a.Mutable, Is.True, "PrepareToDestroy makes the element mutable again");
    }

    [Test]
    [Covers(typeof(TBoldElement), nameof(TBoldElement.RegisterModifiedValueHolder), nameof(TBoldElement.UnRegisterModifiedValueHolder),
        nameof(TBoldElement.ModifiedValueHolder))]
    [Covers("BoldElements", "TBoldElement.GetModifiedValueHolder", "TBoldElement.SetModifiedValueHolder", "initialization", "finalization")]
    public void One_holder_at_a_time_may_modify_an_element()
    {
        using var a = new Value(null, "a");
        object first = new(), second = new();
        Assert.That(a.ModifiedValueHolder, Is.Null);
        a.RegisterModifiedValueHolder(first);
        a.RegisterModifiedValueHolder(first);
        Assert.That(a.ModifiedValueHolder, Is.SameAs(first));
        var e = Assert.Throws<EBold>(() => a.RegisterModifiedValueHolder(second))!;
        Assert.That(e.Message, Is.EqualTo("Value: Member already under modification"));
        a.UnRegisterModifiedValueHolder(second);
        Assert.That(a.ModifiedValueHolder, Is.Null, "any observer clears it, as in Delphi");
        a.RegisterModifiedValueHolder(second);
        Assert.That(a.ModifiedValueHolder, Is.SameAs(second));
    }

    [Test]
    [Covers(typeof(TBoldEvaluator), "*")]
    [Covers(typeof(TBoldElement), nameof(TBoldElement.EvaluateExpression), nameof(TBoldElement.EvaluateExpressionAsDirectElement),
        nameof(TBoldElement.EvaluateExpressionAsString), nameof(TBoldElement.EvaluateExpressionAsNewElement), nameof(TBoldElement.SubscribeToExpression),
        nameof(TBoldElement.EvaluateAndSubscribeToExpression), nameof(TBoldElement.Evaluator))]
    [Covers(typeof(TBoldElementTypeInfo), "*")]
    [Covers(typeof(TBoldLookUpOclDefinition))]
    public void Expressions_are_evaluated_by_the_system_types_evaluator()
    {
        var evaluator = new FakeEvaluator();
        using var system = new SystemType(evaluator);
        using var type = new ValueType(system);
        using var a = new Value(type, "a");
        Assert.That(a.EvaluatorOf(), Is.SameAs(evaluator), "a type asks its system type");
        Assert.That(type.SystemTypeInfo, Is.SameAs(system));
        Assert.That(type.ConformsTo(type), Is.True);
        Assert.That(type.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtList));
        system.SetKind(TBoldValueTypeSet.bvtSystem);
        Assert.That(system.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtSystem));
        Assert.That(system.Mutable, Is.False, "meta elements are immutable");
        Assert.Throws<EBoldFeatureNotImplementedYet>(() => type.GetAsList(new TBoldIndirectElement()), "needs TBoldTypeList (M3)");

        Assert.That(a.EvaluateExpressionAsDirectElement("self"), Is.SameAs(a));
        Assert.That(a.EvaluateExpressionAsDirectElement("new"), Is.Null, "a new result is not an existing element");
        Assert.That(a.EvaluateExpressionAsString("self", BoldDefs.brDefault), Is.EqualTo("a"));
        Assert.That(a.EvaluateExpressionAsString("nil", BoldDefs.brDefault), Is.EqualTo(""));

        TBoldElement made = a.EvaluateExpressionAsNewElement("new")!;
        Assert.That(made.AsString, Is.EqualTo("made"));
        Assert.That(made.Mutable, Is.False, "new results are made immutable");
        TBoldElement copy = a.EvaluateExpressionAsNewElement("self")!;
        Assert.That(copy, Is.Not.SameAs(a));
        Assert.That(copy.AsString, Is.EqualTo("a"));
        Assert.That(a.EvaluateExpressionAsNewElement("nil"), Is.Null);
        using var inPS = new TBoldIndirectElement();
        a.EvaluateExpression("new", inPS, true);
        Assert.That(inPS.Value!.Mutable, Is.True, "results evaluated in the persistence layer are left mutable");

        int resubscribe = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, _, requested) => resubscribe += requested == BoldSubscription.breReSubscribe ? 1 : 0);
        a.SubscribeToExpression("self", subscriber);
        a.Dispose();
        Assert.That(resubscribe, Is.EqualTo(1), "the subscriber resubscribes when the context goes away");

        using var b = new Value(type, "b");
        var e = Assert.Throws<EBold>(() => b.EvaluateExpression("abort", null))!;
        Assert.That(e.Message, Is.EqualTo("OCL Expression: abort Error: 3:stopped"));
        e = Assert.Throws<EBold>(() => b.EvaluateExpression("error", null))!;
        Assert.That(e.Message, Is.EqualTo("OCL Expression: error Error: 4:bad"));
        Assert.That(e.InnerException, Is.InstanceOf<EBoldOCLError>());

        evaluator.DefineVariable("x", null, null, false);
        Assert.That(evaluator.Variables, Is.EqualTo(new[] { "x" }));
        evaluator.SetLookupOclDefinition(name => name + "!");
        Assert.That(evaluator.Lookup!("def"), Is.EqualTo("def!"));
        Assert.That(evaluator.ExpressionType("x", type, false), Is.SameAs(type));
    }

    [Test]
    [Covers(typeof(TBoldMetaElement), "*")]
    public void Meta_elements_are_named_immutable_and_equal_only_to_themselves()
    {
        using var system = new SystemType(new FakeEvaluator());
        Assert.That((system.ModelName, system.ExpressionName, system.DelphiName), Is.EqualTo(("System", "System", "TSystem")));
        Assert.That(system.AsString, Is.EqualTo("System"));
        Assert.That(system.StringRepresentation(BoldDefs.brLong), Is.EqualTo("System"));
        using var other = new SystemType(new FakeEvaluator());
        Assert.That(system.IsEqual(other), Is.False);
        Assert.That(system.IsEqualAs(TBoldCompareType.ctAsString, system), Is.True);
        system.DefaultSubscribe(null);
        system.PrepareToDestroy();
        var e = Assert.Throws<EBold>(() => system.DefaultSubscribe(null))!;
        Assert.That(e.Message, Is.EqualTo("SystemType.DefaultSubscribe: Subscription on mutable MetaElements is not possible"));
    }

    [Test]
    [Covers(typeof(TBoldIndirectElement), "*")]
    public void Indirect_element_frees_only_what_it_owns()
    {
        var owned = new Value(null, "o");
        int destroyed = 0;
        owned.AddSmallSubscription(new TBoldPassthroughSubscriber((_, _, _) => destroyed++),
            TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.breReEvaluate);
        var referenced = new Value(null, "r");

        var holder = new TBoldIndirectElement();
        holder.SetOwnedValue(owned);
        Assert.That(holder.OwnsValue, Is.True);
        holder.SetOwnedValue(owned);
        Assert.That(destroyed, Is.EqualTo(0), "setting the same value again keeps it");
        var e = Assert.Throws<EBold>(() => holder.SetReferenceValue(owned))!;
        Assert.That(e.Message, Is.EqualTo("TBoldIndirectElement.SetReferenceValue: New value alread owned!"));
        holder.SetReferenceValue(referenced);
        Assert.That(destroyed, Is.EqualTo(1), "an owned value is freed when replaced");
        Assert.That(holder.OwnsValue, Is.False);
        holder.SetOwnedValue(null);
        Assert.That(holder.OwnsValue, Is.False, "owning nothing");

        var target = new TBoldIndirectElement();
        holder.SetOwnedValue(new Value(null, "t"));
        holder.TransferValue(target);
        Assert.That(holder.Value, Is.Null);
        Assert.That(target.OwnsValue, Is.True);
        Assert.That(target.Value!.AsString, Is.EqualTo("t"));
        holder.SetReferenceValue(referenced);
        holder.TransferValue(target);
        Assert.That(target.Value, Is.SameAs(referenced));
        Assert.That(target.OwnsValue, Is.False);
        holder.SetReferenceValue(referenced);
        holder.TransferValue(null);
        Assert.That(holder.Value, Is.Null);

        holder.SetOwnedValue(new Value(null, "kept"));
        TBoldElement kept = holder.RelinquishValue()!;
        Assert.That(holder.OwnsValue, Is.False);
        holder.Dispose();
        Assert.That(kept.AsString, Is.EqualTo("kept"));

        int destroyedOnDispose = 0;
        var last = new Value(null, "last");
        last.AddSmallSubscription(new TBoldPassthroughSubscriber((_, _, _) => destroyedOnDispose++),
            TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.breReEvaluate);
        var final = new TBoldIndirectElement();
        final.SetOwnedValue(last);
        final.Dispose();
        Assert.That(destroyedOnDispose, Is.EqualTo(1));
        target.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldExternalVariable), "*")]
    [Covers(typeof(TBoldExternalVariableList), "*")]
    [Covers(typeof(IBoldOCLComponent), "*")]
    [Covers(typeof(TBoldElementSubscribe))]
    [Covers(typeof(TBoldSubscribableComponentViaBoldElem))]
    [Covers("BoldElements", "TBoldValueTypes", "TBoldDirectElement", "TBoldElementClass")]
    public void Variables_components_and_aliases()
    {
        using var system = new SystemType(new FakeEvaluator());
        using var type = new ValueType(system);
        var v = new Value(type, "v");
        var list = new TBoldExternalVariableList();
        list.Add(new Variable("x", v));
        list.Add(new Variable("none", null));
        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list[0].Name, Is.EqualTo("x"));
        Assert.That(list[0].Value, Is.SameAs(v));
        Assert.That(list[0].ValueType, Is.SameAs(type));
        Assert.That(list[1].ValueType, Is.Null);

        IBoldOCLComponent c = new OclComponent();
        c.Expression = "self.name";
        Assert.That(c.Expression, Is.EqualTo("self.name"));
        Assert.That(c.ContextType, Is.Null);
        Assert.That(c.VariableList, Is.Null);

        TBoldElement? seen = null;
        TBoldElementSubscribe subscribe = (element, _) => seen = element;
        subscribe(v, new TBoldPassthroughSubscriber((TBoldEventHandler?)null));
        Assert.That(seen, Is.SameAs(v));

        using var component = new TBoldSubscribableComponentViaBoldElem();
        TBoldValueTypes kinds = TBoldValueTypes.Of(TBoldValueTypeSet.bvtAttr);
        Assert.That(kinds.Contains(TBoldValueTypeSet.bvtAttr), Is.True);
        TBoldDirectElement direct = v;
        TBoldElementClass cls = direct.GetType();
        Assert.That(cls, Is.EqualTo(typeof(Value)));
    }
}
