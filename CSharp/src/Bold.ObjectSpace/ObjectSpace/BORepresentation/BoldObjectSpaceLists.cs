// Delphi: Source/ObjectSpace/BORepresentation/BoldObjectSpaceLists.pas
// Port map: CSharp/port/units/BoldObjectSpaceLists.md

using static Bold.BoldSubscription;

namespace Bold;

/// <summary>An indexable list that can index its objects by the values of some members (the qualifiers of a link).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectAttributeIndexList : TBoldIndexableList
{
    private static int IX_BoldQualifiers = -1;

    [Delphi(PortStatus.Ported)]
    private bool GetHasMembersIndex() => IX_BoldQualifiers != -1 && IndexCount > IX_BoldQualifiers && Indexes(IX_BoldQualifiers) is TBoldMembersHashIndex;

    [Delphi(PortStatus.Ported)]
    private TBoldMembersHashIndex GetMembersIndex() => (TBoldMembersHashIndex)Indexes(IX_BoldQualifiers)!;

    [Delphi(PortStatus.Ported)]
    private TBoldMembersHashIndex CreateMembersIndex(TBoldObjectList ObjectList, TBoldMemberRTInfoList MemberList) => new(this, ObjectList, MemberList);

    /// <summary>Drops the members index (a member value changed); it is built again on the next InitMembersIndex.</summary>
    [Delphi(PortStatus.Ported)]
    internal virtual void NotifyMemberIndexBad()
    {
        if (HasMembersIndex)
        {
            TBoldIndex? TempIndex = GetMembersIndex();
            RemoveAndFreeIndex(ref TempIndex, false);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void EnsureLazyCreateIndexes()
    {
    }

    /// <summary>Indexes the objects by the members (qualifiers).</summary>
    [Delphi(PortStatus.Ported)]
    public void InitMembersIndex(TBoldObjectList ObjectList, TBoldMemberRTInfoList MemberList)
    {
        EnsureLazyCreateIndexes();
        if (IX_BoldQualifiers != -1 && IndexCount > IX_BoldQualifiers)
        {
            SetIndex(IX_BoldQualifiers, CreateMembersIndex(ObjectList, MemberList));
        }
        else
        {
            BoldIndexableList.SetIndexVariable(ref IX_BoldQualifiers, AddIndex(CreateMembersIndex(ObjectList, MemberList)));
        }
    }

    [Delphi(PortStatus.Ported)] public bool HasMembersIndex => GetHasMembersIndex();

    /// <summary>The object whose members have the values in the list, subscribing to what decides it.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? GetLocatorByAttributesAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber) =>
        ((TBoldMembersHashIndex)Indexes(IX_BoldQualifiers)!).GetLocatorByAttributesAndSubscribe(MemberList, Subscriber);
}

/// <summary>An ordered list of locators that does not own them, with a lookup index built when it grows.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectLocatorList : TBoldObjectAttributeIndexList
{
    private static int IX_BoldObjectLocator = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocatorList()
    {
        SetIndexCapacity(1);
        OwnsEntries = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectLocatorList.CreateFromObjectList")]
    public TBoldObjectLocatorList(TBoldObjectList BoldObjectList)
        : this()
    {
        for (int I = 0; I < BoldObjectList.Count; I++)
        {
            Add(BoldObjectList.Locators(I));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetLocators(int index) => (TBoldObjectLocator?)base[index];

    [Delphi(PortStatus.Ported)]
    private void SetLocators(int index, TBoldObjectLocator? Value) => base[index] = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldLocatorHashIndex GetLocatorIndex()
    {
        if (UnorderedIndexCount == 0)
        {
            BoldIndexableList.SetIndexVariable(ref IX_BoldObjectLocator, AddIndex(new TBoldLocatorHashIndex()));
        }

        return (TBoldLocatorHashIndex)Indexes(IX_BoldObjectLocator)!;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocatorList Clone()
    {
        var result = new TBoldObjectLocatorList();
        for (int I = 0; I < Count; I++)
        {
            result.Add(this[I]);
        }

        return result;
    }

    /// <summary>True when the locator is in the list (through the index for lists over ten).</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetLocatorInList(TBoldObjectLocator? Locator)
    {
        if (Locator is null)
        {
            return false;
        }

        return Count > 10 ? LocatorIndex.FindLocatorByLocator(Locator) is not null : IndexOf(Locator) != -1;
    }

    [Delphi(PortStatus.Ported)]
    public void MergeObjectList(TBoldObjectList BoldObjectList)
    {
        for (int I = 0; I < BoldObjectList.Count; I++)
        {
            if (!LocatorInList(BoldObjectList.Locators(I)))
            {
                Add(BoldObjectList.Locators(I));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void FillObjectList(TBoldObjectList BoldObjectList)
    {
        for (int I = 0; I < Count; I++)
        {
            if (!BoldObjectList.LocatorInList(this[I]))
            {
                BoldObjectList.Add(this[I]!.BoldObject);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldObjectLocator? NewLocator) => base.Add(NewLocator);

    [Delphi(PortStatus.Ported)]
    protected override void EnsureLazyCreateIndexes()
    {
        base.EnsureLazyCreateIndexes();
        GetLocatorIndex();
    }

    [Delphi(PortStatus.Ported)]
    public void Ensure(TBoldObjectLocator NewLocator)
    {
        if (!LocatorInList(NewLocator))
        {
            Add(NewLocator);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldLocatorHashIndex LocatorIndex => GetLocatorIndex();

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectLocatorList.Locators")]
    public new TBoldObjectLocator? this[int index]
    {
        get => GetLocators(index);
        set => SetLocators(index, value);
    }

    [Delphi(PortStatus.Ported)] public bool LocatorInList(TBoldObjectLocator? Locator) => GetLocatorInList(Locator);
}

/// <summary>Indexes locators by the concatenated string values of some members of their objects; a member change drops the index.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMembersHashIndex : TBoldHashIndex
{
    private TList? FMemberIndexList;
    private TBoldPassthroughSubscriber? fMemberSubscriber;
    private readonly TBoldObjectAttributeIndexList fOwner;
    private readonly TBoldObjectList? fObjectList;

    [Delphi(PortStatus.Ported)]
    protected internal TBoldMembersHashIndex(TBoldObjectAttributeIndexList Owner, TBoldObjectList? ObjectList, TBoldMemberRTInfoList Members)
    {
        FMemberIndexList = new TList { Capacity = Members.Count };
        for (int i = 0; i < Members.Count; i++)
        {
            FMemberIndexList.Add(new TBoldMemberID(Members[i].index));
        }

        fMemberSubscriber = new TBoldPassthroughSubscriber(_receiveMemberChanged);
        fOwner = Owner;
        fObjectList = ObjectList;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldMembersHashIndex.Destroy")]
    public override void Dispose()
    {
        FMemberIndexList = null;
        fMemberSubscriber?.Dispose();
        fMemberSubscriber = null;
        base.Dispose();
    }

    private int MemberIndex(int i) => ((TBoldMemberID)FMemberIndexList![i]!).MemberIndex;

    /// <summary>Subscribes to the indexed members of the added object.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Add(object? Item)
    {
        base.Add(Item);
        if (Item is not null)
        {
            for (int i = 0; i < FMemberIndexList!.Count; i++)
            {
                ObjectFromItem(Item)!.BoldMembers(MemberIndex(i)).DefaultSubscribe(fMemberSubscriber);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _receiveMemberChanged(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        fObjectList?.SendEvent(beQualifierChanged);
        fOwner.NotifyMemberIndexBad();
    }

    [Delphi(PortStatus.Ported)]
    public override void Clear(bool DestroyObjects = false)
    {
        fMemberSubscriber?.CancelAllSubscriptions();
        base.Clear(DestroyObjects);
    }

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item)
    {
        string concatval = "";
        TBoldObject BoldObject = ObjectFromItem(Item)!;
        for (int i = 0; i < FMemberIndexList!.Count; i++)
        {
            concatval += BoldObject.BoldMembers(MemberIndex(i)).AsString;
        }

        return TBoldStringKey.HashString(concatval, TBoldStringCompareMode.bscCaseDependent);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item)
    {
        var MembersKey = (TBoldMembersKey)Key;
        TBoldObject BoldObject = ObjectFromItem(Item)!;
        bool result = FMemberIndexList!.Count == MembersKey.AttributeList.Count;
        int i = 0;
        while (result && i < FMemberIndexList.Count)
        {
            result = MembersKey.AttributeList[i].IsEqual(BoldObject.BoldMembers(MemberIndex(i)));
            i++;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => ((TBoldMembersKey)Key).Hash();

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectLocator? LocatorFromItem(object? Item) => (TBoldObjectLocator?)Item;

    [Delphi(PortStatus.Ported)]
    protected TBoldObject? ObjectFromItem(object? Item) => LocatorFromItem(Item)?.EnsuredBoldObject;

    /// <summary>The object whose members have the values in the list; subscribes to its members, or to qualifier changes when none.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? GetLocatorByAttributesAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber)
    {
        TBoldObjectLocator? result = LocatorFromItem(Find(new TBoldMembersKey(MemberList)));

        if (Subscriber is not null)
        {
            if (result is not null)
            {
                for (int i = 0; i < FMemberIndexList!.Count; i++)
                {
                    result.EnsuredBoldObject!.BoldMembers(MemberIndex(i)).DefaultSubscribe(Subscriber, breReSubscribe);
                }
            }
            else
            {
                fObjectList?.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beQualifierChanged), breReSubscribe);
            }
        }

        return result;
    }
}

/// <summary>The search key of a members hash index: the member values, hashed as their concatenated strings.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldMembersKey
{
    private readonly TBoldMemberList FAttributeList;
    private readonly uint FHash;

    [Delphi(PortStatus.Ported)]
    public TBoldMembersKey(TBoldMemberList Attributes)
    {
        FAttributeList = Attributes;
        string concatval = "";
        for (int i = 0; i < FAttributeList.Count; i++)
        {
            concatval += FAttributeList[i].AsString;
        }

        FHash = TBoldStringKey.HashString(concatval, TBoldStringCompareMode.bscCaseDependent);
    }

    [Delphi(PortStatus.Ported)] public TBoldMemberList AttributeList => FAttributeList;

    [Delphi(PortStatus.Ported)]
    public uint Hash() => FHash;
}
