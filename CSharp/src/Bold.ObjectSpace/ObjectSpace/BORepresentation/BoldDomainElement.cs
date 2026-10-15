// Delphi: Source/ObjectSpace/BORepresentation/BoldDomainElement.pas
// Port map: CSharp/port/units/BoldDomainElement.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldDomainElement", Symbol = "TBoldDomainElementClass", Note = "Global using alias of System.Type, in BoldDomainElement.Aliases.cs.")]

namespace Bold;

/// <summary>Which side of a domain element a proxy gives access to.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldDomainElementProxyMode { bdepContents, bdepPMOut, bdepPMIn, bdepUnDo, bdepInternalInitialize, bdRemove }

/// <summary>
/// An element of the object space (a system, object, member or list) with an owner: events and queries it sends go
/// to its owner first.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldDomainElement : TBoldElement
{
    private TBoldDomainElement? FOwningElement;

    [Delphi(PortStatus.Ported)]
    protected TBoldDomainElement(TBoldDomainElement? OwningElement)
    {
        FOwningElement = OwningElement;
    }

    [Delphi(PortStatus.Ported)] protected abstract string GetDisplayName();
    [Delphi(PortStatus.Ported)] protected abstract bool GetBoldDirty();

    /// <summary>Whether the element allows a commit; true here.</summary>
    [Delphi(PortStatus.Ported)] protected virtual bool MayCommit() => true;

    /// <summary>Called with the events of owned elements; does nothing here.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void ReceiveEventFromOwned(object? Originator, TBoldEvent OriginalEvent)
    {
    }

    /// <summary>Called with the queries of owned elements; true (allow) here.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual bool ReceiveQueryFromOwned(object? Originator, TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) => true;

    /// <summary>Raises EBoldInternal naming the owner's class, this class and the problem.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void StateError(string S)
    {
        string MyOwnerClassname = OwningElement is not null ? OwningElement.GetType().Name + "." : "";
        throw new EBoldInternal(SysUtils.Format("%s.%s StateError: %s", MyOwnerClassname, GetType().Name, S));
    }

    /// <summary>An interface onto one side of the element (contents, persistence, undo); false when it has none.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the interface type where Delphi takes its GUID; the interface comes out as an object.")]
    public virtual bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        Obj = null;
        return false;
    }

    /// <summary>Whether the element and its subscribers allow a commit.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CanCommit() => MayCommit() && SendQuery(BoldSubscription.bqMayCommit, [], null);

    [Delphi(PortStatus.Ported)]
    public override void SendEvent(TBoldEvent OriginalEvent)
    {
        OwningElement?.ReceiveEventFromOwned(this, OriginalEvent);
        base.SendEvent(OriginalEvent);
    }

    [Delphi(PortStatus.Ported)]
    public override void SendExtendedEvent(TBoldEvent OriginalEvent, object?[] Args)
    {
        OwningElement?.ReceiveEventFromOwned(this, OriginalEvent);
        base.SendExtendedEvent(OriginalEvent, Args);
    }

    /// <summary>Asks the owner first, then the subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool SendQuery(TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber)
    {
        bool result = OwningElement is null || OwningElement.ReceiveQueryFromOwned(this, OriginalEvent, Args, Subscriber);
        return result && base.SendQuery(OriginalEvent, Args, Subscriber);
    }

    [Delphi(PortStatus.Ported)] public bool BoldDirty => GetBoldDirty();
    [Delphi(PortStatus.Ported)] public bool BoldPersistent => GetElementFlag(BoldElements.befPersistent);
    [Delphi(PortStatus.Ported)] public TBoldDomainElement? OwningElement => FOwningElement;
    [Delphi(PortStatus.Ported)] public string DisplayName => GetDisplayName();

    /// <summary>Sets the persistent flag; Delphi's subclasses set it directly through the protected flag access.</summary>
    internal void SetBoldPersistentFlag(bool Value) => SetElementFlag(BoldElements.befPersistent, Value);

    /// <summary>Sets the owner after construction: Delphi's members and objects call the inherited constructor late, from InitializeMember.</summary>
    internal void SetOwningElement(TBoldDomainElement? Value) => FOwningElement = Value;

    // Delphi code in BoldSystem reads and writes the flags, state and subscribers of other elements (same unit); C# only
    // allows that on instances of the calling class, so these give the same access within the assembly.
    internal bool GetElementFlagOf(TBoldElementFlag Flag) => GetElementFlag(Flag);

    internal void SetElementFlagOf(TBoldElementFlag Flag, bool Value) => SetElementFlag(Flag, Value);

    internal bool HasSubscribersOf => HasSubscribers;

    internal void FreePublisherOf() => FreePublisher();
}

/// <summary>An object giving one side (contents, persistence in or out, undo) of a domain element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDomainElement_Proxy : TBoldRefCountedObject
{
    private readonly TBoldDomainElementProxyMode fMode;
    private readonly TBoldDomainElement fProxedElement;

    [Delphi(PortStatus.Ported)]
    public TBoldDomainElement_Proxy(TBoldDomainElement ProxedElement, TBoldDomainElementProxyMode Mode)
    {
        fProxedElement = ProxedElement;
        fMode = Mode;
    }

    [Delphi(PortStatus.Ported)]
    protected void UnsupportedMode(TBoldDomainElementProxyMode Mode, string Func) =>
        throw new EBoldInternal(SysUtils.Format("%s.%s: Unsupported Mode: %d", GetType().Name, Func, (int)Mode));

    [Delphi(PortStatus.Ported)] public TBoldDomainElement ProxedElement => fProxedElement;
    [Delphi(PortStatus.Ported)] public TBoldDomainElementProxyMode Mode => fMode;
}

[Delphi(PortStatus.Ported)]
public class TBoldDomainElementCollectionTraverser : TBoldIndexableListTraverser
{
    public TBoldDomainElementCollectionTraverser(TBoldIndexTraverser IndexTraverser)
        : base(IndexTraverser)
    {
    }

    [Delphi(PortStatus.Ported)]
    private new TBoldDomainElement GetItem() => (TBoldDomainElement)base.Item!;

    [Delphi(PortStatus.Ported)]
    public new TBoldDomainElement Item => GetItem();
}

/// <summary>A set of domain elements (not owned) with fast membership tests.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDomainElementCollection : TBoldUnOrderedIndexableList
{
    private static int IX_DomainElementCollection = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldDomainElementCollection()
    {
        OwnsEntries = false;
        BoldIndexableList.SetIndexVariable(ref IX_DomainElementCollection, AddIndex(new TBoldDomainElementCollectionIndex()));
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexableListTraverserClass TraverserClass() => typeof(TBoldDomainElementCollectionTraverser);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldDomainElement item) => base.Add(item);

    [Delphi(PortStatus.Ported)]
    public bool Includes(TBoldDomainElement item) =>
        ((TBoldDomainElementCollectionIndex)Indexes(IX_DomainElementCollection)!).FindByElement(item) is not null;

    [Delphi(PortStatus.Ported)]
    public new TBoldDomainElementCollectionTraverser CreateTraverser() => (TBoldDomainElementCollectionTraverser)base.CreateTraverser()!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldDomainElementCollectionIndex")]
    private sealed class TBoldDomainElementCollectionIndex : TBoldObjectHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldDomainElementCollectionIndex.ItemAsKeyObject")]
        protected override object? ItemAsKeyObject(object? Item) => Item;

        internal object? FindByElement(object KeyObject) => FindByObject(KeyObject);
    }
}
