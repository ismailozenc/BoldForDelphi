// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// The locators: TBoldObjectLocator, the system's locator list and their hash indexes.

using System.Runtime.CompilerServices;
using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>Walks a system's locators.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLocatorListTraverser(TBoldIndexTraverser IndexTraverser) : TBoldIndexableListTraverser(IndexTraverser)
{
    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator GetLocator() => (TBoldObjectLocator)Item!;

    [Delphi(PortStatus.Ported)] public TBoldObjectLocator locator => GetLocator();
}

/// <summary>The locators of a system, owned by it and found by object id.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemLocatorList : TBoldUnOrderedIndexableList
{
    [Delphi(PortStatus.Ported)]
    public TBoldSystemLocatorList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_BoldObjectId, AddIndex(new TBoldLocatorIdHashIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetLocatorByID(TBoldObjectId ObjectID) => ((TBoldLocatorIdHashIndex)Indexes(IX_BoldObjectId)!).FindLocatorById(ObjectID);

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetObjectByID(TBoldObjectId ObjectID) => GetLocatorByID(ObjectID)?.BoldObject;

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexableListTraverserClass TraverserClass() => typeof(TBoldLocatorListTraverser);

    /// <summary>Gives the locator a new id (a copy of <paramref name="NewObjectID"/>), telling its object before and after.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateID(TBoldObjectLocator Locator, TBoldObjectId NewObjectID, bool AllowInternal = false)
    {
        if (!NewObjectID.IsStorable && !AllowInternal)
        {
            throw new EBoldInternal(SysUtils.Format("%s.UpdateId: Tried to update ID to an internal ID", GetType().Name));
        }

        Locator.BoldObject?.SendEvent(bePreUpdateId);
        RemoveFromAllIndexes(Locator);
        Locator.fBoldObjectID = NewObjectID.Clone();
        AddToAllIndexes(Locator);
        Locator.BoldObject?.SendEvent(bePostUpdateId);
    }

    [Delphi(PortStatus.Ported)]
    public new TBoldLocatorListTraverser CreateTraverser() => (TBoldLocatorListTraverser)base.CreateTraverser()!;

    [Delphi(PortStatus.Ported)] public TBoldObjectLocator? LocatorByID(TBoldObjectId ObjectID) => GetLocatorByID(ObjectID);
    [Delphi(PortStatus.Ported)] public TBoldObject? ObjectByID(TBoldObjectId ObjectID) => GetObjectByID(ObjectID);
}

/// <summary>
/// Stands for one object of a system, loaded or not: holds its id and, when loaded, the object. Single links stored in
/// other objects can be kept here while the object is not loaded.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectLocator : TBoldMemoryManagedObject, IDisposable
{
    private readonly TBoldSystem fBoldSystem;
    internal TBoldObject? fBoldObject;
    internal TBoldObjectId fBoldObjectID;
    internal TBoldObjectArray? fEmbeddedSingleLinks;

    /// <summary>A locator for (a copy of) the id, added to the system.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectLocator.CreateWithObjectId")]
    internal TBoldObjectLocator(TBoldSystem BoldSystem, TBoldObjectId BoldObjectID)
    {
        fBoldSystem = BoldSystem;
        fBoldObjectID = BoldObjectID.Clone();
        BoldSystem.Locators.Add(this);
    }

    /// <summary>A locator with a new internal id of the class, added to the system.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectLocator.CreateWithClassID")]
    internal TBoldObjectLocator(TBoldSystem BoldSystem, int TopSortedIndex, bool Exact)
    {
        fBoldSystem = BoldSystem;
        fBoldObjectID = new TBoldInternalObjectId(TopSortedIndex, Exact);
        BoldSystem.Locators.Add(this);
    }

    /// <summary>Raises when the object is still loaded.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectLocator.Destroy")]
    public void Dispose()
    {
        if (fBoldObject is not null)
        {
            throw new EBoldInternal(SysUtils.Format(sBoldObjectAssigned, GetType().Name));
        }

        fEmbeddedSingleLinks = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private string GetAsString() => fBoldObjectID.AsString;

    /// <summary>Fetches the object when it is not loaded.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureBoldObject()
    {
        if (fBoldObject is null)
        {
            FetchBoldObject();
        }
    }

    /// <summary>Drops the loaded object (which must be unloadable), keeping the links other objects need.</summary>
    [Delphi(PortStatus.Ported)]
    public void UnloadBoldObject()
    {
        if (BoldObject is not null)
        {
            BoldClearLastFailure();
            if (!BoldObject.CanUnload())
            {
                BoldRaiseLastFailure(null, "TBoldObjectLocator.UnloadBoldObject", "");
            }

            BoldSystem.MarkObjectClean(BoldObject);
            BoldObject.FreePublisherOf();
            BoldObject.FBoldObjectLocator = null;
            EmbeddedSingleLinksFromObject();
            FreeEmbeddedSingleLinksOfOtherEnd();
            TBoldObject obj = fBoldObject!;
            fBoldObject = null;
            obj.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void DiscardBoldObject() => BoldObject?.Discard();

    [Delphi(PortStatus.Ported)]
    private void FetchBoldObject()
    {
        if (!BoldObjectID.IsStorable)
        {
            throw new EBoldInternal(SysUtils.Format("%s.FetchBoldObject: Can't fetch Internal object", GetType().Name));
        }

        BoldSystem.fSystemPersistenceHandler.FetchObjectById(BoldObjectID);
    }

    [Delphi(PortStatus.Ported, Note = "Delphi also answers nil when called on a nil locator; C# callers use ?. for that.")]
    private TBoldObject? GetEnsuredBoldObject()
    {
        EnsureBoldObject();
        return BoldObject;
    }

    [Delphi(PortStatus.Ported, Note = "The identity hash of the locator, where Delphi uses its address.")]
    public uint Hash() => unchecked((uint)RuntimeHelpers.GetHashCode(this));

    [Delphi(PortStatus.Ported)]
    private bool GetObjectIsPersistent() => BoldObject is null || BoldObject.BoldPersistent;

    /// <summary>The locator of the same object at another time (versioned objects with default ids), else nil.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? AtTime(TBoldTimeStampType Time)
    {
        if (Time == BoldObjectID.TimeStamp)
        {
            return this;
        }

        if (BoldObjectID is TBoldDefaultID defaultId)
        {
            return BoldSystem.EnsuredLocatorByID(defaultId.CloneWithTimeStamp(Time));
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetEmbeddedSingleLinks(int EmbeddedIndex) =>
        fEmbeddedSingleLinks is not null && EmbeddedIndex < fEmbeddedSingleLinks.Count ? (TBoldObjectLocator?)fEmbeddedSingleLinks[EmbeddedIndex] : null;

    /// <summary>Keeps (or, with nil, drops) the locator a single link of the unloaded object points to.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetEmbeddedSingleLinks(int EmbeddedIndex, TBoldObjectLocator? Value)
    {
        fEmbeddedSingleLinks ??= new TBoldObjectArray(EmbeddedIndex + 1, default);
        TBoldObjectArray links = fEmbeddedSingleLinks;
        if (EmbeddedIndex >= links.Count)
        {
            links.Count = EmbeddedIndex + 1;
        }

        links[EmbeddedIndex] = Value;
        if (Value is null)
        {
            while (links.Count > 0 && links[links.Count - 1] is null)
            {
                links.Count--;
            }

            if (links.Count == 0)
            {
                fEmbeddedSingleLinks = null;
            }
            else if (links.Capacity > links.Count)
            {
                links.Capacity = links.Count;
            }
        }
    }

    /// <summary>Keeps the current single links of the object whose other end is loaded, before the object is unloaded.</summary>
    [Delphi(PortStatus.Ported)]
    private void EmbeddedSingleLinksFromObject()
    {
        TBoldObject obj = BoldObject!;
        for (int m = 0; m < obj.BoldClassTypeInfo.AllMembers.Count; m++)
        {
            int EmbeddedIndex = obj.BoldClassTypeInfo.AllMembers[m].EmbeddedLinkIndex;
            if (EmbeddedIndex != -1 && obj.BoldMemberAssigned(m))
            {
                var RoleRtInfo = (TBoldRoleRTInfo)obj.BoldClassTypeInfo.AllMembers[m];
                var ObjectReference = (TBoldObjectReference)obj.BoldMembers(m);
                if (ObjectReference.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent &&
                    RoleRtInfo.IndexOfOtherEnd != -1 &&
                    ObjectReference.Locator?.BoldObject is { } other &&
                    other.BoldMemberAssigned(RoleRtInfo.IndexOfOtherEnd))
                {
                    SetEmbeddedSingleLinks(EmbeddedIndex, ObjectReference.Locator);
                }
            }
        }
    }

    /// <summary>Gives the kept single links back to the newly loaded object, as invalid values with old values.</summary>
    [Delphi(PortStatus.Ported)]
    internal void EmbeddedSingleLinksToObject()
    {
        if (fEmbeddedSingleLinks is not null)
        {
            TBoldObject obj = BoldObject!;
            for (int m = 0; m < obj.BoldClassTypeInfo.AllMembers.Count; m++)
            {
                int EmbeddedIndex = obj.BoldClassTypeInfo.AllMembers[m].EmbeddedLinkIndex;
                if (EmbeddedIndex != -1 && GetEmbeddedSingleLinks(EmbeddedIndex) is { } link)
                {
                    var ObjRef = (TBoldObjectReference)obj.BoldMembers(m);
                    ((IBoldObjectIdRef)ObjRef.AsIBoldValue(TBoldDomainElementProxyMode.bdepInternalInitialize)!).SetFromId(link.BoldObjectID);
                    ObjRef.HasOldValues = true;
                }
            }

            fEmbeddedSingleLinks = null;
        }
    }

    /// <summary>Drops the single links that the objects at the other end of the object's link roles keep to it.</summary>
    [Delphi(PortStatus.Ported)]
    private void FreeEmbeddedSingleLinksOfOtherEnd()
    {
        TBoldObject obj = BoldObject!;
        for (int m = 0; m < obj.BoldClassTypeInfo.AllMembers.Count; m++)
        {
            if (obj.BoldClassTypeInfo.AllMembers[m] is TBoldRoleRTInfo { RoleType: TBoldRoleType.rtLinkRole } &&
                obj.BoldMemberAssigned(m) &&
                obj.BoldMembers(m).BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)
            {
                if (obj.BoldMembers(m) is TBoldObjectReference SingleLink)
                {
                    if (SingleLink.Locator?.fEmbeddedSingleLinks is not null)
                    {
                        SingleLink.Locator.SetEmbeddedSingleLinks(SingleLink.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.EmbeddedLinkIndex, null);
                    }
                }
                else
                {
                    var MultiLink = (TBoldObjectList)obj.BoldMembers(m);
                    for (int i = 0; i < MultiLink.Count; i++)
                    {
                        if (MultiLink.Locators(i)!.fEmbeddedSingleLinks is not null)
                        {
                            MultiLink.Locators(i)!.SetEmbeddedSingleLinks(MultiLink.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.EmbeddedLinkIndex, null);
                        }
                    }
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
    [Delphi(PortStatus.Ported)] public TBoldObject? BoldObject => fBoldObject;
    [Delphi(PortStatus.Ported)] public TBoldObjectId BoldObjectID => fBoldObjectID;
    [Delphi(PortStatus.Ported)] public TBoldSystem BoldSystem => fBoldSystem;
    [Delphi(PortStatus.Ported)] public TBoldObject? EnsuredBoldObject => GetEnsuredBoldObject();
    [Delphi(PortStatus.Ported)] public bool ObjectIsPersistent => GetObjectIsPersistent();
    [Delphi(PortStatus.Ported)] public TBoldObjectLocator? EmbeddedSingleLinks(int EmbeddedIndex) => GetEmbeddedSingleLinks(EmbeddedIndex);
}

/// <summary>Indexes locators by themselves (identity).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLocatorHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldObjectLocator ItemAsLocator(object? Item) => (TBoldObjectLocator)Item!;

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => ItemAsLocator(Item).Hash();

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ReferenceEquals(Key, ItemAsLocator(Item));

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => ((TBoldObjectLocator)Key).Hash();

    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? FindLocatorByLocator(TBoldObjectLocator BoldObjectLocator) => (TBoldObjectLocator?)Find(BoldObjectLocator);
}

/// <summary>Indexes locators by object id.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldLocatorIdHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldObjectLocator ItemAsLocator(object? Item) => (TBoldObjectLocator)Item!;

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => ItemAsLocator(Item).BoldObjectID.Hash;

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ((TBoldObjectId)Key).IsEqual(ItemAsLocator(Item).BoldObjectID);

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => ((TBoldObjectId)Key).Hash;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? FindLocatorById(TBoldObjectId BoldObjectId) => (TBoldObjectLocator?)Find(BoldObjectId);
}
