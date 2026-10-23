// Delphi: Source/Handles/Core/BoldExpressionHandle.pas
// Port map: CSharp/port/units/BoldExpressionHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>A rooted handle whose value is an OCL expression evaluated on the root's value, with optional variables.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExpressionHandle : TBoldRootedHandle, IBoldOCLComponent
{
    private const int breVariablesDestroyed = 200;

    private string fExpression = "";
    private TBoldOclVariables? FVariables;
    private TBoldPassthroughSubscriber? fVariablesSubscriber;
    private bool fEvaluateInPS;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldExpressionHandle(IContainer? owner = null)
        : base(owner)
    {
        fVariablesSubscriber = new TBoldPassthroughSubscriber(_VariablesReceive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldExpressionHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fVariablesSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void _VariablesReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == FVariables && RequestedEvent == breVariablesDestroyed)
        {
            FVariables = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Value)
    {
        if (Value != fExpression)
        {
            fExpression = Value;
            MarkSubscriptionOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)] private string GetExpression() => fExpression;

    /// <summary>Sets the variables; variables that use this handle would make a circle.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi passed three arguments for the four of its circular reference message, so the message itself failed; this passes four.")]
    private void SetVariables(TBoldOclVariables? Value)
    {
        if (Value != Variables)
        {
            if (Value is not null && Value.LinksToHandle(this))
            {
                throw new EBold(SysUtils.Format(sCircularReference, GetType().Name, Name, Value.Name, Name));
            }

            FVariables = Value;
            StaticBoldTypeChanged();
            fVariablesSubscriber!.CancelAllSubscriptions();
            Value?.AddSmallSubscription(fVariablesSubscriber, TBoldSmallEventSet.Of(beDestroying), breVariablesDestroyed);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldExternalVariableList? GetVariableList() => FVariables?.VariableList;

    [Delphi(PortStatus.Ported)]
    private void SetEvaluateInPS(bool Value)
    {
        if (Value != fEvaluateInPS)
        {
            fEvaluateInPS = Value;
            MarkSubscriptionOutOfDate();
        }
    }

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetStaticRootType();
    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);
    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();
    string IBoldOCLComponent.GetExpression() => GetExpression();

    /// <summary>The type of the expression on the root type.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType()
    {
        TBoldElementTypeInfo? rootType = StaticRootType;
        if (rootType is null)
        {
            return null;
        }

        return Variables is not null
            ? rootType.Evaluator!.ExpressionType(Expression, rootType, false, Variables.VariableList)
            : rootType.Evaluator!.ExpressionType(Expression, rootType, false);
    }

    /// <summary>Evaluates the expression on the root value into the result, subscribing to what it read and to the variables' handles.</summary>
    [Delphi(PortStatus.Ported, Note = "Raises EBold with the handle in the message and the evaluation error inside, where Delphi changed the message of the error.")]
    protected override void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        TBoldElement? RootValue = EffectiveRootValue();
        if (RootValue is not null)
        {
            TBoldExternalVariableList? vars = null;
            if (Variables is not null)
            {
                vars = Variables.VariableList;
                Variables.SubscribeToHandles(Subscriber);
            }

            try
            {
                RootValue.EvaluateAndSubscribeToExpression(Expression, Subscriber, ResultElement, false, EvaluateInPS, vars);
            }
            catch (Exception e)
            {
                throw new EBold(SysUtils.Format(sDeriveAndSubscribeFailed, GetType().Name, Name, e.Message), e);
            }
        }
        else
        {
            ResultElement.SetOwnedValue(null);
        }

        SubscribeToValue();
    }

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is not null && Component == Variables);

    [Delphi(PortStatus.Ported)]
    public string Expression
    {
        get => fExpression;
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOclVariables? Variables
    {
        get => FVariables;
        set => SetVariables(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool EvaluateInPS
    {
        get => fEvaluateInPS;
        set => SetEvaluateInPS(value);
    }
}
