// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// The controllers that hold the values of lists and references (the link controllers are in BoldLinks).

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>Holds and changes the value of a list or reference member.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractController : TBoldMemoryManagedObject, IDisposable
{
    /// <summary>Frees what the controller owns; nothing here.</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Delphi(PortStatus.Ported)]
    protected internal void Changed(TBoldEvent Event, object?[] Args) => OwningMember.Changed(Event, Args);

    [Delphi(PortStatus.Ported)] protected internal abstract string GetStreamName();
    [Delphi(PortStatus.Ported)] protected internal abstract TBoldMember GetOwningMember();

    [Delphi(PortStatus.Ported)]
    protected internal TBoldObjectLocator? LocatorForID(TBoldObjectId? ObjectId) =>
        ObjectId is null ? null : OwningMember.BoldSystem!.EnsuredLocatorByID(ObjectId);

    [Delphi(PortStatus.Ported)]
    protected internal TBoldObjectLocator AssertedLocatorForID(TBoldObjectId? ObjectId)
    {
        if (ObjectId is null)
        {
            throw new EBoldInternal("Id is nil");
        }

        return LocatorForID(ObjectId)!;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void PreChange() => OwningMember.PreChange();

    [Delphi(PortStatus.Ported)]
    protected internal bool StartModify() => OwningMember.StartModify();

    [Delphi(PortStatus.Ported)]
    protected internal static TBoldAbstractController? GetControllerForMember(TBoldMember Member) => Member.GetController();

    /// <summary>A new value for the member in the old values (for optimistic locking), or nil when there is one.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal IBoldValue? NewValueInOptimisticLocking() =>
        TBoldAbstractOldValueHandler.NewValueInValueSpace(OwningMember, OwningMember.OwningObject!.BoldSystem.fOldValueHandler.OldValues);

    [Delphi(PortStatus.Ported)]
    protected internal void DbFetchOwningMember()
    {
        if (!OwningMember.OwningObject!.BoldObjectLocator!.BoldObjectID.IsStorable)
        {
            throw new EBoldInternal(SysUtils.Format("%s.DbFetchOwningMember: Member belongs to object with internal ID", OwningMember.BoldType!.AsString));
        }

        OwningMember.OwningObject.BoldSystem.SystemPersistenceHandler.FetchMember(OwningMember);
    }

    [Delphi(PortStatus.Ported)]
    protected internal void DbFetchClassForMember(TBoldTimeStampType TimeStamp) =>
        OwningMember.BoldSystem!.SystemPersistenceHandler.FetchClass((TBoldObjectList)OwningMember, TimeStamp);

    [Delphi(PortStatus.Ported)]
    protected internal void EndModify() => OwningMember.EndModify();

    [Delphi(PortStatus.Ported, Note = "Does nothing, as in Delphi (meant to become abstract).")]
    public virtual bool AssertIntegrity() => true;

    [Delphi(PortStatus.Ported)]
    public virtual void Unlink(TBoldObjectLocator? OldLocator, TBoldLinkUnlinkMode Mode)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual void LinkTo(TBoldObjectLocator? NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        Obj = null;
        return false;
    }

    [Delphi(PortStatus.Ported)] public string StreamName => GetStreamName();
    [Delphi(PortStatus.Ported)] public TBoldMember OwningMember => GetOwningMember();
}

/// <summary>Holds the elements of a list.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldListController : TBoldAbstractController
{
    private readonly TBoldList fOwningList;

    [Delphi(PortStatus.Ported)]
    protected TBoldListController(TBoldList OwningList)
    {
        fOwningList = OwningList;
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual TBoldElement CreateNew() => throw new EBold(sCannotCreateNewElement);

    [Delphi(PortStatus.Ported)]
    private TBoldSystem? GetBoldSystem() => OwningList.BoldSystem;

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetCanCreateNew() => false;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember GetOwningMember() => OwningList;

    [Delphi(PortStatus.Ported)]
    protected internal virtual string GetStringrepresentation()
    {
        OwningList.EnsureContentsCurrent();
        return Count.ToString();
    }

    [Delphi(PortStatus.Ported)] protected abstract int GetCount();
    [Delphi(PortStatus.Ported)] public abstract void AddElement(TBoldElement Element);
    [Delphi(PortStatus.Ported)] public abstract TBoldElement? GetElement(int index);
    [Delphi(PortStatus.Ported)] public abstract bool IncludesElement(TBoldElement Item);
    [Delphi(PortStatus.Ported)] public abstract int IndexOfElement(TBoldElement Item);
    [Delphi(PortStatus.Ported)] public abstract void InsertElement(int index, TBoldElement Element);
    [Delphi(PortStatus.Ported)] public abstract void Move(int CurrentIndex, int NewIndex);
    [Delphi(PortStatus.Ported)] public abstract void RemoveByIndex(int index);
    [Delphi(PortStatus.Ported)] public abstract void SetElement(int index, TBoldElement Value);

    [Delphi(PortStatus.Ported)] protected internal TBoldList OwningList => fOwningList;
    [Delphi(PortStatus.Ported)] public TBoldSystem? BoldSystem => GetBoldSystem();
    [Delphi(PortStatus.Ported)] public bool CanCreateNew => GetCanCreateNew();
    [Delphi(PortStatus.Ported)] public int Count => GetCount();
}

/// <summary>Holds the locators of an object list.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractObjectListController(TBoldObjectList aOwningList) : TBoldListController(aOwningList)
{
    [Delphi(PortStatus.Ported)]
    protected TBoldObjectList GetObjectList() => (TBoldObjectList)OwningList;

    [Delphi(PortStatus.Ported)] protected internal abstract TBoldMember_ProxyClass ProxyClass();

    [Delphi(PortStatus.Ported)]
    protected internal virtual void PrepareClear()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void AddElement(TBoldElement Element) => AddLocator(((TBoldObject)Element).BoldObjectLocator!);

    [Delphi(PortStatus.Ported)]
    public virtual TBoldMember AtTime(TBoldTimeStampType Time) =>
        throw new EBoldInternal(SysUtils.Format("%s.AtTime: Illegal call. Does not handle AtTime.", GetType().Name));

    [Delphi(PortStatus.Ported)]
    public virtual void Clear()
    {
        for (int I = Count - 1; I >= 0; I--)
        {
            RemoveByIndex(I);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void FreeContent()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldElement? GetElement(int index)
    {
        TBoldObjectLocator aLocator = GetLocator(index)!;
        aLocator.EnsureBoldObject();
        return aLocator.BoldObject;
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool HandlesAtTime() => false;

    [Delphi(PortStatus.Ported)]
    public override bool IncludesElement(TBoldElement Item) => IncludesLocator(((TBoldObject)Item).BoldObjectLocator);

    [Delphi(PortStatus.Ported)]
    public override int IndexOfElement(TBoldElement Item) => IndexOfLocator(((TBoldObject)Item).BoldObjectLocator);

    [Delphi(PortStatus.Ported)]
    public override void InsertElement(int index, TBoldElement Element) => InsertLocator(index, ((TBoldObject)Element).BoldObjectLocator!);

    [Delphi(PortStatus.Ported)]
    public override void SetElement(int index, TBoldElement Value) => SetLocator(index, ((TBoldObject)Value).BoldObjectLocator!);

    [Delphi(PortStatus.Ported)] public abstract void AddLocator(TBoldObjectLocator Element);
    [Delphi(PortStatus.Ported)] public abstract TBoldObjectLocator? GetLocator(int index);
    [Delphi(PortStatus.Ported)] public abstract TBoldObjectLocator? GetLocatorByQualifiersAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber);
    [Delphi(PortStatus.Ported)] public abstract bool IncludesLocator(TBoldObjectLocator? Item);
    [Delphi(PortStatus.Ported)] public abstract int IndexOfLocator(TBoldObjectLocator? Item);
    [Delphi(PortStatus.Ported)] public abstract void InsertLocator(int index, TBoldObjectLocator Element);
    [Delphi(PortStatus.Ported)] public abstract void MakeDbCurrent();
    [Delphi(PortStatus.Ported)] public abstract void SetLocator(int index, TBoldObjectLocator Value);

    [Delphi(PortStatus.Ported)] protected TBoldObjectList OwningObjectList => GetObjectList();
}

/// <summary>Holds the locator of an object reference.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractObjectReferenceController : TBoldAbstractController
{
    private readonly TBoldObjectReference fOwningReference;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractObjectReferenceController(TBoldObjectReference Owner)
    {
        fOwningReference = Owner;
    }

    /// <summary>The controller of the link object role that goes with this role (the member after it).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldAbstractObjectReferenceController ControllerForLinkRole()
    {
        TBoldMember LinkMember = OwningReference.OwningObject!.BoldMembers(OwningReference.BoldRoleRTInfo!.index + 1);
        return ((TBoldObjectReference)LinkMember).ReferenceController;
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldAbstractObjectReferenceController ControllerForMainRole()
    {
        TBoldMember LinkMember = OwningReference.OwningObject!.BoldMembers(OwningReference.BoldRoleRTInfo!.IndexOfMainRole);
        return ((TBoldObjectReference)LinkMember).ReferenceController;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember GetOwningMember() => OwningReference;

    [Delphi(PortStatus.Ported)]
    protected internal virtual bool MayUpdate() => true;

    /// <summary>The controller of the link object's role that points to the other end.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldAbstractObjectReferenceController OtherEndControllerForLinkObject(TBoldObject Obj)
    {
        TBoldMember OtherEndMember = Obj.BoldMembers(OwningReference.BoldRoleRTInfo!.OtherIndexInLinkClass);
        return ((TBoldObjectReference)OtherEndMember).ReferenceController;
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void PreDiscard()
    {
    }

    [Delphi(PortStatus.Ported)] protected internal abstract TBoldMember_ProxyClass ProxyClass();
    [Delphi(PortStatus.Ported)] public abstract TBoldObjectLocator? GetLocator();
    [Delphi(PortStatus.Ported)] public abstract void SetLocator(TBoldObjectLocator? NewLocator);
    [Delphi(PortStatus.Ported)] public abstract void MakeDbCurrent();

    [Delphi(PortStatus.Ported)] public TBoldObjectReference OwningReference => fOwningReference;
}

/// <summary>The controller of a free-standing or derived reference: just a locator, dropped when its object is destroyed.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectReferenceController : TBoldAbstractObjectReferenceController
{
    internal TBoldObjectLocator? fLocator;
    private TBoldPassthroughSubscriber? fSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectReferenceController(TBoldObjectReference Owner)
        : base(Owner)
    {
        fSubscriber = new TBoldPassthroughSubscriber(ObjectChangeReceive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectReferenceController.Destroy")]
    public override void Dispose()
    {
        fSubscriber?.Dispose();
        fSubscriber = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public void AssignContentValue(IBoldValue? Source)
    {
        if (Source is not IBoldObjectIdRef s)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        if (OwningReference is null)
        {
            throw new EBold(sObjectRefMustBePartOfObject);
        }

        fLocator = s.Id is null ? null : OwningReference.BoldSystem!.EnsuredLocatorByID(s.Id);
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator() => fLocator;

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRef;

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent() =>
        throw new EBoldInternal(SysUtils.Format("%s.MakeDBcurrent: This is only possible for link-controllers", GetType().Name));

    [Delphi(PortStatus.Ported)]
    private void ObjectChangeReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fLocator?.BoldObject && OriginalEvent == beDestroying)
        {
            fLocator = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SetLocator(TBoldObjectLocator? NewLocator)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningReference, "SetLocator", "");
        }

        fLocator = NewLocator;
        Changed(beValueChanged, [NewLocator]);
        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldObjectReference_Proxy);
}
