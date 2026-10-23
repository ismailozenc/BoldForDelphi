// Delphi: Source/Handles/Core/BoldListHandle.pas
// Port map: CSharp/port/units/BoldListHandle.md

using System.ComponentModel;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>
/// The usual list handle: a cursor over the value of an OCL expression on the root's value, optionally filtered and
/// sorted. Inside it chains an expression handle, a filtered handle and a sorted handle.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandle : TBoldCursorHandle, IBoldOCLComponent
{
    private TBoldExpressionHandle? fExpressionHandle;
    private TBoldFilteredHandle? fFilterHandle;
    private TBoldSortedHandle? fSorterHandle;
    private string fMutableListExpression = "";

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldListHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fExpressionHandle = new TBoldExpressionHandle(null);
        FixupRoots();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldListHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fSorterHandle);
            SysUtils.FreeAndNil(ref fFilterHandle);
            SysUtils.FreeAndNil(ref fExpressionHandle);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)] private TBoldElementTypeInfo? GetContextType() => fExpressionHandle!.StaticRootType;
    [Delphi(PortStatus.Ported)] private TBoldComparer? GetComparer() => fSorterHandle?.BoldComparer;
    [Delphi(PortStatus.Ported)] private string GetExpression() => fExpressionHandle!.Expression;
    [Delphi(PortStatus.Ported)] private TBoldFilter? GetFilter() => fFilterHandle?.BoldFilter;

    /// <summary>Sets the comparer, adding or removing the inner sorted handle.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetComparer(TBoldComparer? Value)
    {
        if (Value == BoldComparer)
        {
            return;
        }

        if (Value is null)
        {
            SysUtils.FreeAndNil(ref fSorterHandle);
        }
        else
        {
            fSorterHandle ??= new TBoldSortedHandle(null);
            fSorterHandle.BoldComparer = Value;
            Value.FreeNotification(this);
        }

        FixupRoots();
    }

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Value)
    {
        if (Value != fExpressionHandle!.Expression)
        {
            fExpressionHandle.Expression = Value;
            StaticBoldTypeChanged();
        }
    }

    /// <summary>Sets the filter, adding or removing the inner filtered handle.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetFilter(TBoldFilter? Value)
    {
        if (Value == BoldFilter)
        {
            return;
        }

        if (Value is null)
        {
            SysUtils.FreeAndNil(ref fFilterHandle);
        }
        else
        {
            fFilterHandle ??= new TBoldFilteredHandle(null);
            fFilterHandle.BoldFilter = Value;
            Value.FreeNotification(this);
        }

        FixupRoots();
    }

    [Delphi(PortStatus.Ported)] private TBoldExternalVariableList? GetVariableList() => Variables?.VariableList;
    [Delphi(PortStatus.Ported)] private TBoldOclVariables? GetVariables() => fExpressionHandle!.Variables;

    /// <summary>Sets the variables; variables that use this handle would make a circle.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi passed three arguments for the four of its circular reference message, so the message itself failed; this passes four.")]
    private void SetVariables(TBoldOclVariables? Value)
    {
        if (Value is not null && Value.LinksToHandle(this))
        {
            throw new EBold(SysUtils.Format(sCircularReference, GetType().Name, Name, Value.Name, Name));
        }

        fExpressionHandle!.Variables = Value;
    }

    [Delphi(PortStatus.Ported)] private bool GetEvaluateInPS() => fExpressionHandle!.EvaluateInPS;

    [Delphi(PortStatus.Ported)]
    private void SetEvaluateInPS(bool Value)
    {
        if (Value != fExpressionHandle!.EvaluateInPS)
        {
            fExpressionHandle.EvaluateInPS = Value;
            MarkOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMutableListExpression(string Value) => fMutableListExpression = Value;

    /// <summary>Chains the inner handles: expression, then filter, then sorter; the last is this handle's root.</summary>
    [Delphi(PortStatus.Ported)]
    private void FixupRoots()
    {
        TBoldElementHandle NextHandle = fExpressionHandle!;
        fExpressionHandle!.Name = Name + "_expr"; // do not localize

        if (fFilterHandle is not null)
        {
            fFilterHandle.RootHandle = NextHandle;
            fFilterHandle.Name = Name + "_filt"; // do not localize
            NextHandle = fFilterHandle;
        }

        if (fSorterHandle is not null)
        {
            fSorterHandle.RootHandle = NextHandle;
            fSorterHandle.Name = Name + "_sort"; // do not localize
            NextHandle = fSorterHandle;
        }

        InternalRootHandle = NextHandle;
    }

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetContextType();
    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);
    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();
    string IBoldOCLComponent.GetExpression() => GetExpression();

    /// <summary>Forgets the filter or comparer when it goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (Operation == TOperation.opRemove && AComponent == BoldFilter)
        {
            BoldFilter = null;
        }

        if (Operation == TOperation.opRemove && AComponent == BoldComparer)
        {
            BoldComparer = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetSubscribe(bool Value)
    {
        fExpressionHandle!.Subscribe = Value;
        if (fFilterHandle is not null)
        {
            fFilterHandle.Subscribe = Value;
        }

        if (fSorterHandle is not null)
        {
            fSorterHandle.Subscribe = Value;
        }

        base.SetSubscribe(Value);
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetStaticSystemHandle(TBoldAbstractSystemHandle? Value)
    {
        base.SetStaticSystemHandle(Value);
        fExpressionHandle?.StaticSystemHandle = Value;
    }

    [Delphi(PortStatus.Ported)]
    protected override void Loaded()
    {
        base.Loaded();
        FixupRoots();
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementHandle? GetRootHandle() => fExpressionHandle?.RootHandle;

    [Delphi(PortStatus.Ported)]
    protected override void SetRootHandle(TBoldElementHandle? Value) => fExpressionHandle!.RootHandle = Value;

    /// <summary>
    /// A list that can be changed: the list itself when unfiltered and unsorted, else the list before the sorting, else
    /// the value of MutableListExpression on the root's value.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldList? GetMutableList()
    {
        // with a sorter or a filter the inherited result is surely not mutable... no need to ask
        TBoldList? result = fSorterHandle is not null || fFilterHandle is not null ? null : base.GetMutableList();

        using var ie = new TBoldIndirectElement();

        // try to get the value before the comparer
        if (result is null && fSorterHandle?.RootHandle?.Value is { } sortRootValue)
        {
            sortRootValue.GetAsList(ie);
            if (!ie.OwnsValue && ie.Value is not null && ie.Value.Mutable)
            {
                result = (TBoldList)ie.Value;
            }
        }

        // try to get the value given by MutableListExpression
        if (result is null && MutableListExpression != "" && RootHandle?.Value is { } rootValue)
        {
            ie.SetReferenceValue(rootValue.EvaluateExpressionAsDirectElement(MutableListExpression));
            ie.Value?.GetAsList(ie);
            if (!ie.OwnsValue && ie.Value is not null && ie.Value.Mutable)
            {
                result = (TBoldList)ie.Value;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void MarkOutOfDate()
    {
        fExpressionHandle!.MarkOutOfDate();
        fFilterHandle?.MarkOutOfDate();
        fSorterHandle?.MarkOutOfDate();
        base.MarkOutOfDate();
    }

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is not null && Component == Variables);

    [Delphi(PortStatus.Ported)]
    public TBoldComparer? BoldComparer
    {
        get => GetComparer();
        set => SetComparer(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFilter? BoldFilter
    {
        get => GetFilter();
        set => SetFilter(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Expression
    {
        get => GetExpression();
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOclVariables? Variables
    {
        get => GetVariables();
        set => SetVariables(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool EvaluateInPS
    {
        get => GetEvaluateInPS();
        set => SetEvaluateInPS(value);
    }

    [Delphi(PortStatus.Ported)]
    public string MutableListExpression
    {
        get => fMutableListExpression;
        set => SetMutableListExpression(value);
    }
}
