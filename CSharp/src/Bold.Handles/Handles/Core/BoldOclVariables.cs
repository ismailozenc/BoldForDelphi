// Delphi: Source/Handles/Core/BoldOclVariables.pas
// Port map: CSharp/port/units/BoldOclVariables.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclVariables", Symbol = "TBoldVariableTupleList.GetOwner", Note = "Gave the owning component for the object inspector's name path; OwningDefinition gives it.")]

namespace Bold;

/// <summary>An OCL variable whose value is a handle's value (or, for a list handle, optionally its whole list).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldHandleBasedExternalVariable : TBoldExternalVariable, IDisposable
{
    internal TBoldElementHandle? fHandle;
    private readonly bool fUseListElement;
    private TBoldPassthroughSubscriber? fhandleSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldHandleBasedExternalVariable(string Name, TBoldElementHandle? Handle, bool UseListElement)
        : base(Name)
    {
        fHandle = Handle;
        fUseListElement = UseListElement;
        fhandleSubscriber = new TBoldPassthroughSubscriber(_ReceiveHandleEvent);
        fHandle?.AddSmallSubscription(fhandleSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldHandleBasedExternalVariable.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fhandleSubscriber);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private void _ReceiveHandleEvent(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == beDestroying && Originator == fHandle)
        {
            fHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue()
    {
        if (fHandle is null)
        {
            return null;
        }

        return fUseListElement && fHandle is TBoldAbstractListHandle listHandle ? listHandle.List : fHandle.Value;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetValueType()
    {
        if (fHandle is null)
        {
            return null;
        }

        return fUseListElement && fHandle is TBoldAbstractListHandle listHandle ? listHandle.StaticListType : fHandle.StaticBoldType;
    }
}

/// <summary>
/// A set of named OCL variables, each the value of a handle; expression handles and list handles that use it can say
/// "self.name = myVariable" in their expressions.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclVariables : TBoldSubscribableComponent
{
    private TBoldVariableTupleList? fVariableTupleList;
    private TBoldExternalVariableList? fVariableList;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldOclVariables(IContainer? Owner = null)
    {
        Owner?.Add(this);
        fVariableList = null;
        fVariableTupleList = new TBoldVariableTupleList(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclVariables.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fVariableList);
            fVariableTupleList?.Clear();
            fVariableTupleList = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetVariableTupleList(TBoldVariableTupleList Value) => fVariableTupleList = Value;

    /// <summary>Drops the variable list (the next use builds it again) and tells subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    internal void VariablesChanged()
    {
        SysUtils.FreeAndNil(ref fVariableList);
        SendEvent(this, beValueChanged);
    }

    /// <summary>The variables as the evaluator takes them, built on first use; later uses update each variable's handle.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariableList GetVariableList()
    {
        bool NewList = fVariableList is null;
        fVariableList ??= new TBoldExternalVariableList();
        for (int i = 0; i < Variables.Count; i++)
        {
            if (NewList)
            {
                fVariableList.Add(new TBoldHandleBasedExternalVariable(Variables[i].VariableName, Variables[i].BoldHandle, Variables[i].EffectiveUseListElement));
            }
            else
            {
                ((TBoldHandleBasedExternalVariable)fVariableList[i]).fHandle = Variables[i].BoldHandle;
            }
        }

        return fVariableList;
    }

    /// <summary>Asks to be told (breReSubscribe) when the value of any variable's handle changes identity.</summary>
    [Delphi(PortStatus.Ported)]
    public void SubscribeToHandles(TBoldSubscriber? Subscriber)
    {
        if (Subscriber is null)
        {
            return;
        }

        for (int i = 0; i < Variables.Count; i++)
        {
            Variables[i].BoldHandle?.AddSubscription(Subscriber, beValueIdentityChanged, breReSubscribe);
        }
    }

    /// <summary>True when a variable uses the handle, directly or through its roots.</summary>
    [Delphi(PortStatus.Ported)]
    public bool LinksToHandle(TBoldElementHandle Handle)
    {
        bool result = false;
        for (int i = 0; i < Variables.Count; i++)
        {
            result = result || Variables[i].LinksToHandle(Handle);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldExternalVariableList VariableList => GetVariableList();

    [Delphi(PortStatus.Ported)]
    public TBoldVariableTupleList Variables
    {
        get => fVariableTupleList!;
        set => SetVariableTupleList(value);
    }
}

/// <summary>The variables of a TBoldOclVariables: a collection of (name, handle) tuples with unique, valid names.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldVariableTupleList : TCollection
{
    private readonly TBoldOclVariables fOwningDefinition;

    [Delphi(PortStatus.Ported)]
    public TBoldVariableTupleList(TBoldOclVariables aOwningDefinition)
        : base(typeof(TBoldVariableTuple))
    {
        fOwningDefinition = aOwningDefinition;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldVariableTuple GetItems(int Index) => (TBoldVariableTuple)base[Index];

    /// <summary>Frees a tuple that leaves the list, as Delphi freed a deleted collection item.</summary>
    protected override void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
        base.Notify(Item, Action);
        if (Action == TCollectionNotification.cnExtracting && Item is TBoldVariableTuple tuple)
        {
            tuple.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)] internal TBoldOclVariables OwningDefinition => fOwningDefinition;

    [Delphi(PortStatus.Ported)]
    public bool NameIsUnique(string Name)
    {
        for (int i = 0; i < Count; i++)
        {
            if (SysUtils.AnsiCompareStr(Name, this[i].VariableName) == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when the name has only letters, digits and underscores.</summary>
    [Delphi(PortStatus.Ported)]
    public bool NameIsValid(string Name)
    {
        foreach (char c in Name)
        {
            if (!(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_' or >= '0' and <= '9'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The first free name of Variable1, Variable2...</summary>
    [Delphi(PortStatus.Ported)]
    public string GetUniqueName()
    {
        int i = 1;
        string result;
        do
        {
            result = "Variable" + SysUtils.IntToStr(i); // do not localize
            i++;
        }
        while (!NameIsUnique(result));
        return result;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldVariableTupleList.Items", Note = "The default indexed property in Delphi.")]
    public new TBoldVariableTuple this[int Index] => GetItems(Index);
}

/// <summary>One OCL variable: a name and the handle that gives its value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldVariableTuple : TCollectionItem, IDisposable
{
    private string FVariableName = "";
    private TBoldElementHandle? FBoldHandle;
    private TBoldPassthroughSubscriber? fBoldHandleSubscriber;
    private bool FUseListElement;

    /// <summary>Adds the tuple to the collection with a new unique name.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldVariableTuple(TCollection? Collection)
        : base(Collection)
    {
        Changed();
        VariableName = ((TBoldVariableTupleList)Collection!).GetUniqueName();
        fBoldHandleSubscriber = new TBoldPassthroughSubscriber(_ReceiveHandleEvent);
    }

    /// <summary>Cancels the handle subscription and tells the variables they changed; called when the tuple leaves its list.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldVariableTuple.Destroy", Note = "Also tells the variables they changed, so a deleted variable leaves the variable list; Delphi kept it until the next change.")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fBoldHandleSubscriber);
        if (Collection is TBoldVariableTupleList tupleList)
        {
            tupleList.OwningDefinition.VariablesChanged();
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private void _ReceiveHandleEvent(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == BoldHandle && RequestedEvent == beDestroying)
        {
            BoldHandle = null;
        }
    }

    /// <summary>Sets the handle; a handle that uses these variables itself would make a circle.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi passed three arguments for the four of its circular reference message, so the message itself failed; this passes four.")]
    private void SetBoldHandle(TBoldElementHandle? Value)
    {
        if (Value != FBoldHandle)
        {
            if (Value is not null && Value.RefersToComponent(TupleList.OwningDefinition))
            {
                throw new EBold(SysUtils.Format(sCircularReference, GetType().Name, TupleList.OwningDefinition.Name, Value.Name, TupleList.OwningDefinition.Name));
            }

            FBoldHandle = Value;
            fBoldHandleSubscriber?.CancelAllSubscriptions();
            Value?.AddSmallSubscription(fBoldHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetVariableName(string Value)
    {
        if (FVariableName != Value)
        {
            if (!((TBoldVariableTupleList)Collection!).NameIsUnique(Value))
            {
                throw new EBold(SysUtils.Format(sNameNotUnique, Value));
            }

            if (!((TBoldVariableTupleList)Collection!).NameIsValid(Value))
            {
                throw new EBold(sIllegalCharsInName);
            }

            FVariableName = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetUseListElement(bool Value)
    {
        if (Value != FUseListElement)
        {
            FUseListElement = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void Changed() => ((TBoldVariableTupleList)Collection!).OwningDefinition.VariablesChanged();

    [Delphi(PortStatus.Ported)] private TBoldVariableTupleList GetTupleList() => (TBoldVariableTupleList)Collection!;

    [Delphi(PortStatus.Ported)]
    private bool GetEffectiveUseListElement() => UseListElement && BoldHandle is TBoldAbstractListHandle;

    /// <summary>"name: handle", with " (list)" when the variable is the whole list.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName()
    {
        string result = VariableName;
        result += BoldHandle is not null ? ": " + BoldHandle.Name : ": Not Connected"; // do not localize
        if (EffectiveUseListElement)
        {
            result += " (list)"; // do not localize
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldVariableTuple SourceTuple)
        {
            VariableName = SourceTuple.VariableName;
            UseListElement = SourceTuple.UseListElement;
            BoldHandle = SourceTuple.BoldHandle;
        }
        else
        {
            base.Assign(Source);
        }
    }

    /// <summary>True when the variable's handle is the handle or is rooted in it.</summary>
    [Delphi(PortStatus.Ported)]
    public bool LinksToHandle(TBoldElementHandle Handle)
    {
        bool result = BoldHandle is not null && Handle == BoldHandle;
        if (!result && BoldHandle is TBoldRootedHandle rooted)
        {
            result = rooted.IsRootLinkedTo(Handle);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldVariableTupleList TupleList => GetTupleList();
    [Delphi(PortStatus.Ported)] public bool EffectiveUseListElement => GetEffectiveUseListElement();

    [Delphi(PortStatus.Ported)]
    public TBoldElementHandle? BoldHandle
    {
        get => FBoldHandle;
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public string VariableName
    {
        get => FVariableName;
        set => SetVariableName(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool UseListElement
    {
        get => FUseListElement;
        set => SetUseListElement(value);
    }
}
