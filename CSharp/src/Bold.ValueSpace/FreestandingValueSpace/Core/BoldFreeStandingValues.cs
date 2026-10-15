// Delphi: Source/FreestandingValueSpace/Core/BoldFreeStandingValues.pas
// Port map: CSharp/port/units/BoldFreeStandingValues.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldFreeStandingValues", Symbol = "TBoldFreeStandingElementClass", Note = "Global using alias of System.Type, in BoldFreeStandingValues.Aliases.cs.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldFreeStandingValues", Symbol = "PMemberList", Note = "TBoldFreeStandingObjectContents keeps its members in a managed array.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldFreeStandingValues", Symbol = "TMemberList", Note = "TBoldFreeStandingObjectContents keeps its members in a managed array.")]

namespace Bold;

/// <summary>A free-standing id list a value space can add ids to and remove ids from.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldFreeStandingIdList
{
    [Delphi(PortStatus.Ported)] void RemoveId(TBoldObjectId Id);
    [Delphi(PortStatus.Ported)] void AddId(TBoldObjectId Id);
}

/// <summary>A free-standing pair of parallel id lists (objects and link objects).</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldFreeStandingIdListPair
{
    [Delphi(PortStatus.Ported)] void RemoveId(TBoldObjectId Id);
    [Delphi(PortStatus.Ported)] void AddIds(TBoldObjectId Id1, TBoldObjectId Id2);
}

/// <summary>The base of everything in a free-standing value space; the factory creates them by stream name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFreeStandingElement : TBoldNonRefCountedObject
{
    /// <summary>Empty, so subclasses can initialize their fields in their own constructors.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingElement()
    {
    }
}

/// <summary>
/// A value space that lives on its own, outside any object space: object contents by id, as read from or written to a
/// persistence layer or a stream.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldFreeStandingValueSpace : TBoldFreeStandingElement, IBoldValueSpace, IDisposable
{
    private TBoldObjectIdList? fIdLIst;
    private TBoldFSObjectContentList? fObjectContentsList;
    private TBoldIDTranslationList? fTranslationList;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingValueSpace()
    {
        fObjectContentsList = new TBoldFSObjectContentList();
        fIdLIst = new TBoldObjectIdList();
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        SysUtils.FreeAndNil(ref fObjectContentsList);
        SysUtils.FreeAndNil(ref fIdLIst);
        SysUtils.FreeAndNil(ref fTranslationList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectIdList GetIdList() => fIdLIst ??= new TBoldObjectIdList();

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectIdList IdList => GetIdList();

    private TBoldFSObjectContentList Contents => fObjectContentsList!;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingObjectContents? GetFSObjectContentsByObjectId(TBoldObjectId ObjectId) =>
        Contents.FindObjectContentById(ObjectId);

    [Delphi(PortStatus.Ported)]
    public void RemoveFSObjectContentsByObjectId(TBoldObjectId ObjectId) =>
        Contents.Remove(GetFSObjectContentsByObjectId(ObjectId));

    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents? GetObjectContentsByObjectId(TBoldObjectId ObjectId) => GetFSObjectContentsByObjectId(ObjectId);

    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents GetEnsuredObjectContentsByObjectId(TBoldObjectId ObjectId) =>
        GetEnsuredFSObjectContentsByObjectId(ObjectId);

    [Delphi(PortStatus.Ported)]
    public bool GetHasContentsForId(TBoldObjectId ObjectId) => GetObjectContentsByObjectId(ObjectId) is not null;

    [Delphi(PortStatus.Ported)]
    public void EnsureObjectContents(TBoldObjectId ObjectId) => GetEnsuredFSObjectContentsByObjectId(ObjectId);

    [Delphi(PortStatus.Ported)]
    public void EnsureObjectId(TBoldObjectId ObjectId)
    {
        if (!IdList.IdInList(ObjectId))
        {
            IdList.Add(ObjectId);
        }
    }

    /// <summary>
    /// Records the translations and moves each object whose id was translated to its new id, in the id list and the
    /// contents index; then lets every value translate the ids it refers to.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void ApplyTranslationList(TBoldIDTranslationList IdTranslationList)
    {
        void DoOneObject(int IndexInList, TBoldFreeStandingObjectContents anObject)
        {
            TBoldObjectId? newId = IdTranslationList.TranslateToNewId(anObject.fObjectId);
            if (newId is not null && newId != anObject.fObjectId)
            {
                Contents.OwnsEntries = false;
                Contents[IndexInList] = null;
                Contents.OwnsEntries = true;
                IdList.ReplaceID(anObject.fObjectId!, newId);
                anObject.fObjectId = IdList.IDByID(newId);
                Contents[IndexInList] = anObject;
            }

            anObject.ApplyTranslationList(IdTranslationList);
        }

        fTranslationList ??= new TBoldIDTranslationList();
        for (int i = 0; i < IdTranslationList.Count; i++)
        {
            fTranslationList.AddTranslation(IdTranslationList.OldIds(i), IdTranslationList.NewIds(i));
        }

        for (int i = 0; i < Contents.Count; i++)
        {
            DoOneObject(i, Contents[i]!);
        }
    }

    void IBoldValueSpace.ApplytranslationList(TBoldIDTranslationList IdTranslationList) => ApplyTranslationList(IdTranslationList);

    /// <summary>Applies the loaded objects of <paramref name="ValueSpace"/>, creating contents as needed.</summary>
    [Delphi(PortStatus.Ported)]
    public void ApplyValueSpace(IBoldValueSpace ValueSpace, bool IgnorePersistenceState)
    {
        using TBoldObjectIdList anIdList = new();
        ValueSpace.AllObjectIds(anIdList, true);
        for (int i = 0; i < anIdList.Count; i++)
        {
            TBoldObjectId ObjectId = anIdList[i];
            TBoldFreeStandingObjectContents? ObjectContents = GetFSObjectContentsByObjectId(ObjectId);
            if (ObjectContents is null)
            {
                EnsureObjectContents(ObjectId);
                ObjectContents = GetFSObjectContentsByObjectId(ObjectId)!;
            }

            ObjectContents.ApplyObjectContents(ValueSpace.ObjectContentsByObjectId(ObjectId)!, true, IgnorePersistenceState);
        }
    }

    /// <summary>Does nothing, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExactifyIDs(TBoldIDTranslationList TranslationList)
    {
    }

    /// <summary>Adds every id (or only those with contents when <paramref name="OnlyLoaded"/>) to <paramref name="resultList"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void AllObjectIds(TBoldObjectIdList resultList, bool OnlyLoaded)
    {
        for (int i = 0; i < fIdLIst!.Count; i++)
        {
            if (OnlyLoaded)
            {
                if (GetObjectContentsByObjectId(fIdLIst[i]) is not null)
                {
                    resultList.Add(fIdLIst[i]);
                }
            }
            else
            {
                resultList.Add(fIdLIst[i]);
            }
        }
    }

    /// <summary>Drops the objects whose existence state is deleted, with their ids.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveDeletedObjects()
    {
        for (int i = Contents.Count - 1; i >= 0; i--)
        {
            TBoldFreeStandingObjectContents ObjectContents = Contents[i]!;
            if (ObjectContents.fBoldExistenceState == TBoldExistenceState.besDeleted)
            {
                TBoldObjectId? id = fIdLIst!.IDByID(ObjectContents.fObjectId!);
                Contents.Remove(ObjectContents);
                fIdLIst.Remove(id);
            }
        }
    }

    /// <summary>Copies values from <paramref name="ValueSpace"/> into the members this value space already has.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateOwnValuesFrom(IBoldValueSpace ValueSpace)
    {
        using TBoldObjectIdList anIdList = new();
        AllObjectIds(anIdList, true);
        for (int i = 0; i < anIdList.Count; i++)
        {
            TBoldObjectId ObjectId = anIdList[i];
            TBoldFreeStandingObjectContents OwnObjectContents = GetFSObjectContentsByObjectId(ObjectId)!;
            IBoldObjectContents? ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectId);
            if (ObjectContents is not null)
            {
                OwnObjectContents.UpdateObjectContentsFrom(ObjectContents);
            }
        }
    }

    /// <summary>Drops every object's contents; the ids stay.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveAllObjectContents()
    {
        using TBoldObjectIdList ObjectIds = new();
        AllObjectIds(ObjectIds, true);
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            RemoveFSObjectContentsByObjectId(ObjectIds[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void MarkAllObjectsAndMembersCurrent()
    {
        for (int ObjectIx = Contents.Count - 1; ObjectIx >= 0; ObjectIx--)
        {
            TBoldFreeStandingObjectContents ObjectContents = Contents[ObjectIx]!;
            ObjectContents.fBoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
            ObjectContents.MarkAllMembersCurrent();
        }
    }

    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetValueForIdAndMemberIndex(TBoldObjectId Id, int MemberIndex) =>
        GetFSObjectContentsByObjectId(Id)?.ValueByIndex(MemberIndex);

    /// <summary>The contents of the object, created (with its id added) when missing.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingObjectContents GetEnsuredFSObjectContentsByObjectId(TBoldObjectId ObjectId)
    {
        TBoldFreeStandingObjectContents? Result = GetFSObjectContentsByObjectId(ObjectId);
        if (Result is null)
        {
            TBoldObjectId? LocalId = IdList.IDByID(ObjectId);
            if (LocalId is null)
            {
                IdList.Add(ObjectId);
                LocalId = IdList.IDByID(ObjectId);
            }

            Result = new TBoldFreeStandingObjectContents { fObjectId = LocalId };
            Contents.Add(Result);
        }

        return Result;
    }
}

/// <summary>The values of one object in a free-standing value space, by member index.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFreeStandingObjectContents : TBoldFreeStandingElement, IBoldObjectContents, IBoldStreamable, IDisposable
{
    internal TBoldExistenceState fBoldExistenceState;
    internal TBoldValuePersistenceState fBoldPersistenceState;
    private string fGlobalId = "";
    private bool fIsReadOnly;
    private TBoldFreeStandingValue?[]? fMemberList;
    private int fMemberCount;
    internal TBoldObjectId? fObjectId;
    private TBoldTimeStampType fTimeStamp;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingObjectContents()
    {
        fBoldExistenceState = TBoldExistenceState.besExisting;
        fTimeStamp = -1;
    }

    [Delphi(PortStatus.Ported, Note = "Disposes the members; the member array is managed memory, not BoldMemoryManager's.")]
    public virtual void Dispose()
    {
        if (fMemberList is not null)
        {
            for (int i = 0; i < fMemberCount; i++)
            {
                fMemberList[i]?.Dispose();
            }

            fMemberList = null;
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId GetObjectId() => fObjectId!;

    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => BoldDefaultStreamNames.BOLDOBJECTCONTENTSNAME;

    [Delphi(PortStatus.Ported)]
    internal void ApplyTranslationList(TBoldIDTranslationList TranslationList)
    {
        for (int i = 0; i < MemberCount; i++)
        {
            fMemberList![i]?.ApplyTranslationList(TranslationList);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldExistenceState GetBoldExistenceState() => fBoldExistenceState;
    [Delphi(PortStatus.Ported)] public int GetBoldMemberCount() => fMemberCount;
    [Delphi(PortStatus.Ported)] public TBoldValuePersistenceState GetBoldPersistenceState() => fBoldPersistenceState;
    [Delphi(PortStatus.Ported)] public string GetGlobalId() => fGlobalId;

    /// <summary>Always true, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)] public bool GetIsModified() => true;

    [Delphi(PortStatus.Ported)] public bool GetIsReadOnly() => fIsReadOnly;

    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetValueByIndex(int I) => I < MemberCount ? fMemberList![I] : null;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingValue? GetFSValueByIndex(int i) => i < MemberCount ? fMemberList![i] : null;

    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetValueByMemberId(TBoldMemberID MemberId) => GetValueByIndex(MemberId.MemberIndex);

    [Delphi(PortStatus.Ported)] public TBoldTimeStampType GetTimeStamp() => fTimeStamp;
    [Delphi(PortStatus.Ported)] public void SetBoldExistenceState(TBoldExistenceState Value) => fBoldExistenceState = Value;
    [Delphi(PortStatus.Ported)] public void SetBoldPersistenceState(TBoldValuePersistenceState Value) => fBoldPersistenceState = Value;
    [Delphi(PortStatus.Ported)] public void SetGlobalId(string NewValue) => fGlobalId = NewValue;
    [Delphi(PortStatus.Ported)] public void SetIsReadOnly(bool NewValue) => fIsReadOnly = NewValue;
    [Delphi(PortStatus.Ported)] public void SetTimeStamp(TBoldTimeStampType NewValue) => fTimeStamp = NewValue;

    [Delphi(PortStatus.Ported)]
    public void MarkAllMembersCurrent()
    {
        for (int M = 0; M < MemberCount; M++)
        {
            if (fMemberList![M] is not null)
            {
                GetValueByIndex(M)!.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveMemberByIndex(int Index)
    {
        if (MemberCount > Index)
        {
            fMemberList![Index]?.Dispose();
            fMemberList[Index] = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureMember(TBoldMemberID MemberId, string StreamName) => EnsureMemberByIndex(MemberId.MemberIndex, StreamName);

    /// <summary>Grows the member array to hold <paramref name="Index"/> and creates the value there by stream name when missing.</summary>
    [Delphi(PortStatus.Ported, Note = "Grows a managed array where Delphi reallocates through BoldMemoryManager.")]
    public void EnsureMemberByIndex(int Index, string StreamName)
    {
        if (fMemberList is null)
        {
            fMemberList = new TBoldFreeStandingValue?[Index + 1];
            fMemberCount = Index + 1;
        }

        if (Index >= MemberCount)
        {
            Array.Resize(ref fMemberList, Index + 1);
            fMemberCount = Index + 1;
        }

        fMemberList[Index] ??= (TBoldFreeStandingValue)BoldFreeStandingValueFactories.FreeStandingValueFactory!.CreateInstance(StreamName);
    }

    /// <summary>
    /// Copies the existence and persistence state and, with <paramref name="ApplyValues"/>, every value. With
    /// <paramref name="IgnorePersistenceState"/> the values keep their own persistence state.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void ApplyObjectContents(IBoldObjectContents ObjectContents, bool ApplyValues, bool IgnorePersistenceState)
    {
        SetBoldExistenceState(ObjectContents.BoldExistenceState);
        SetBoldPersistenceState(ObjectContents.BoldPersistenceState);
        if (ApplyValues)
        {
            for (int i = 0; i < ObjectContents.MemberCount; i++)
            {
                IBoldValue? aValue = ObjectContents.ValueByIndex(i);
                if (aValue is not null)
                {
                    TBoldMemberID MemberId = new(i);
                    EnsureMember(MemberId, aValue.ContentName);
                    if (IgnorePersistenceState)
                    {
                        fMemberList![i]!.AssignContentValue(aValue);
                    }
                    else
                    {
                        fMemberList![i]!.AssignContent(aValue);
                    }
                }
            }
        }
    }

    /// <summary>Copies the states, and the values of the members this object already has.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateObjectContentsFrom(IBoldObjectContents ObjectContents)
    {
        SetBoldExistenceState(ObjectContents.BoldExistenceState);
        SetBoldPersistenceState(ObjectContents.BoldPersistenceState);
        for (int i = 0; i < MemberCount; i++)
        {
            IBoldValue? OwnValue = GetValueByIndex(i);
            IBoldValue? aValue = ObjectContents.ValueByIndex(i);
            if (OwnValue is not null && aValue is not null)
            {
                OwnValue.AssignContent(aValue);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public IBoldValue? ValueByIndex(int I) => GetValueByIndex(I);
    [Delphi(PortStatus.Ported)] public TBoldFreeStandingValue? FSValueByIndex(int I) => GetFSValueByIndex(I);

    [Delphi(PortStatus.Ported)]
    public TBoldExistenceState BoldExistenceState
    {
        get => GetBoldExistenceState();
        set => SetBoldExistenceState(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceState BoldPersistenceState
    {
        get => GetBoldPersistenceState();
        set => SetBoldPersistenceState(value);
    }

    [Delphi(PortStatus.Ported)] public int MemberCount => fMemberCount;
}

/// <summary>The base of the free-standing values: a persistence state and copying from another value.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldFreeStandingValue : TBoldFreeStandingElement, IBoldValue, IBoldStreamable, IDisposable
{
    private TBoldValuePersistenceState fBoldPersistenceState;

    [Delphi(PortStatus.Ported)]
    protected TBoldFreeStandingValue()
    {
        BoldPersistenceState = TBoldValuePersistenceState.bvpsInvalid;
    }

    /// <summary>Releases owned id lists; Delphi's values without a destructor have nothing to release.</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Delphi(PortStatus.Ported)] public string GetContentName() => GetStreamName();
    [Delphi(PortStatus.Ported)] public TBoldValuePersistenceState GetBoldPersistenceState() => fBoldPersistenceState;
    [Delphi(PortStatus.Ported)] public void SetBoldPersistenceState(TBoldValuePersistenceState Value) => fBoldPersistenceState = Value;
    [Delphi(PortStatus.Ported)] public abstract string GetStreamName();

    /// <summary>Does nothing; values holding ids translate them.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void ApplyTranslationList(TBoldIDTranslationList TranslationList)
    {
    }

    /// <summary>Copies the value of <paramref name="Source"/> without its persistence state.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void AssignContentValue(IBoldValue Source) =>
        throw new EBold(SysUtils.Format(FreeStandingValuesConst.sAbstractError, GetType().Name));

    /// <summary>Copies the value and the persistence state of <paramref name="Source"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssignContent(IBoldValue Source)
    {
        AssignContentValue(Source);
        BoldPersistenceState = Source.BoldPersistenceState;
    }

    [Delphi(PortStatus.Ported)] public string ContentName => GetContentName();

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceState BoldPersistenceState
    {
        get => fBoldPersistenceState;
        set => fBoldPersistenceState = value;
    }

    /// <summary>The exception Delphi raises when the source does not have the expected content interface.</summary>
    private protected EBold UnknownTypeOfSource() =>
        new(SysUtils.Format(FreeStandingValuesConst.sUnknownTypeOfSource, GetType().Name));
}

/// <summary>A free-standing value that can be null. A new value is not null.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldFreeStandingNullableValue : TBoldFreeStandingValue, IBoldNullableValue
{
    private bool fIsNull;

    [Delphi(PortStatus.Ported)] protected void SetToNonNull() => fIsNull = false;
    [Delphi(PortStatus.Ported)] public bool GetContentIsNull() => fIsNull;
    [Delphi(PortStatus.Ported)] public void SetContentToNull() => fIsNull = true;
}

[Delphi(PortStatus.Ported)]
public class TBFSInteger : TBoldFreeStandingNullableValue, IBoldIntegerContent
{
    private int fDataValue;

    [Delphi(PortStatus.Ported)] public int GetContentAsInteger() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsInteger(int NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Integer;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldIntegerContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsInteger(s.asInteger);
        }
    }

    [Delphi(PortStatus.Ported)]
    public int AsInteger { get => GetContentAsInteger(); set => SetContentAsInteger(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSString : TBoldFreeStandingNullableValue, IBoldStringContent
{
    private string fDataValue = "";

    [Delphi(PortStatus.Ported)] public string GetContentAsString() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsString(string NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_String;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldStringContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsString(s.asString);
        }
    }

    [Delphi(PortStatus.Ported)]
    public string AsString { get => GetContentAsString(); set => SetContentAsString(value); }
}

[Delphi(PortStatus.Ported, Note = "decimal where Delphi has Currency.")]
public class TBFSCurrency : TBoldFreeStandingNullableValue, IBoldCurrencyContent
{
    private decimal fDataValue;

    [Delphi(PortStatus.Ported)] public decimal GetContentAsCurrency() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsCurrency(decimal NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Currency;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldCurrencyContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsCurrency(s.asCurrency);
        }
    }

    [Delphi(PortStatus.Ported)]
    public decimal AsCurrency { get => GetContentAsCurrency(); set => SetContentAsCurrency(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSFloat : TBoldFreeStandingNullableValue, IBoldFloatContent
{
    private double fDataValue;

    [Delphi(PortStatus.Ported)] public double GetContentAsFloat() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsFloat(double NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Float;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldFloatContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsFloat(s.asFloat);
        }
    }

    [Delphi(PortStatus.Ported)]
    public double AsFloat { get => GetContentAsFloat(); set => SetContentAsFloat(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSBoolean : TBoldFreeStandingNullableValue, IBoldBooleanContent
{
    private bool fDataValue;

    [Delphi(PortStatus.Ported)] public bool GetContentAsBoolean() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsBoolean(bool NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Boolean;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldBooleanContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsBoolean(s.asBoolean);
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool AsBoolean { get => GetContentAsBoolean(); set => SetContentAsBoolean(value); }
}

/// <summary>The common store of the date, time and date-time values: one TDateTime.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBFSDateTimeAbstract : TBoldFreeStandingNullableValue
{
    private TDateTime fDataValue;

    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsDateTime() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsDateTime(TDateTime NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsTime() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsTime(TDateTime NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsDate() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsDate(TDateTime NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }
}

[Delphi(PortStatus.Ported)]
public class TBFSDateTime : TBFSDateTimeAbstract, IBoldDateTimeContent
{
    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_DateTime;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldDateTimeContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsDateTime(s.asDateTime);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TDateTime AsDateTime { get => GetContentAsDateTime(); set => SetContentAsDateTime(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSDate : TBFSDateTimeAbstract, IBoldDateContent
{
    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Date;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldDateContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsDate(s.asDate);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TDateTime AsDate { get => GetContentAsDate(); set => SetContentAsDate(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSTime : TBFSDateTimeAbstract, IBoldTimeContent
{
    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Time;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldTimeContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsTime(s.asTime);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TDateTime AsTime { get => GetContentAsTime(); set => SetContentAsTime(value); }
}

/// <summary>The common store of the blob values.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBFSBlobAbstract : TBoldFreeStandingNullableValue
{
    private string fDataValue = "";

    [Delphi(PortStatus.Ported)] public string GetContentAsBlob() => fDataValue;

    [Delphi(PortStatus.Ported)]
    public void SetContentAsBlob(string NewValue)
    {
        fDataValue = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Blob;

    [Delphi(PortStatus.Ported)]
    public string AsBlob { get => GetContentAsBlob(); set => SetContentAsBlob(value); }
}

[Delphi(PortStatus.Ported)]
public class TBFSBlob : TBFSBlobAbstract, IBoldBlobContent
{
    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldBlobContent s)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsBlob(s.asBlob);
        }
    }
}

/// <summary>A blob with a content type. Setting the content type makes the value non-null, as in Delphi.</summary>
[Delphi(PortStatus.Ported)]
public class TBFSTypedBlob : TBFSBlobAbstract, IBoldTypedBlob, IBoldBlobContent
{
    private string fContent = "";

    [Delphi(PortStatus.Ported)] public string GetContentTypeContent() => fContent;

    [Delphi(PortStatus.Ported)]
    public void SetContentTypeContent(string NewValue)
    {
        fContent = NewValue;
        SetToNonNull();
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_TypedBlob;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldBlobContent s || Source is not IBoldTypedBlob t)
        {
            throw UnknownTypeOfSource();
        }

        if (s.IsNull)
        {
            SetContentToNull();
        }
        else
        {
            SetContentAsBlob(s.asBlob);
        }

        SetContentTypeContent(t.ContentTypeContent);
    }

    [Delphi(PortStatus.Ported)]
    public string ContentTypeContent { get => GetContentTypeContent(); set => SetContentTypeContent(value); }
}

/// <summary>The common order number of the single-reference values.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBFSObjectIDRefAbstract : TBoldFreeStandingValue
{
    private int fOrderNo;

    [Delphi(PortStatus.Ported)] public int GetOrderNo() => fOrderNo;
    [Delphi(PortStatus.Ported)] public void SetOrderNo(int NewOrder) => fOrderNo = NewOrder;

    [Delphi(PortStatus.Ported)]
    public int OrderNo { get => GetOrderNo(); set => SetOrderNo(value); }
}

/// <summary>A single object reference; holds its own copy of the id.</summary>
[Delphi(PortStatus.Ported)]
public class TBFSObjectIDRef : TBFSObjectIDRefAbstract, IBoldObjectIdRef
{
    private TBoldObjectId? fObjectId;

    [Delphi(PortStatus.Ported)] public TBoldObjectId? GetId() => fObjectId;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdRef s)
        {
            throw UnknownTypeOfSource();
        }

        SetFromId(s.Id);
        OrderNo = s.OrderNo;
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRef;

    [Delphi(PortStatus.Ported)]
    protected internal override void ApplyTranslationList(TBoldIDTranslationList TranslationList)
    {
        if (fObjectId is not null)
        {
            TBoldObjectId? NewId = TranslationList.TranslateToNewId(fObjectId);
            if (NewId is not null && !NewId.IsEqual(fObjectId))
            {
                fObjectId = NewId.Clone();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        fObjectId = null;
        base.Dispose();
    }

    /// <summary>Stores a copy of <paramref name="Id"/> (kept when equal to the current id); null clears.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFromId(TBoldObjectId? Id)
    {
        if (Id is not null)
        {
            if (fObjectId is null || !Id.IsEqual(fObjectId))
            {
                fObjectId = Id.Clone();
            }
        }
        else
        {
            fObjectId = null;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId? Id => GetId();
}

/// <summary>A single reference with its link object, kept as an id list of at most two.</summary>
[Delphi(PortStatus.Ported)]
public class TBFSObjectIDRefPair : TBFSObjectIDRefAbstract, IBoldObjectIdRefPair
{
    private TBoldObjectIdList? fObjectIds;

    [Delphi(PortStatus.Ported)]
    private void EnsureIdList() => fObjectIds ??= new TBoldObjectIdList();

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetId1()
    {
        EnsureIdList();
        return fObjectIds!.Count > 0 ? fObjectIds[0] : null;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetId2()
    {
        EnsureIdList();
        return fObjectIds!.Count > 1 ? fObjectIds[1] : null;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdRefPair s)
        {
            throw UnknownTypeOfSource();
        }

        SetFromIds(s.Id1, s.Id2);
        OrderNo = s.OrderNo;
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair;

    [Delphi(PortStatus.Ported)]
    protected internal override void ApplyTranslationList(TBoldIDTranslationList TranslationList) =>
        fObjectIds?.ApplyTranslationList(TranslationList);

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fObjectIds);
        base.Dispose();
    }

    /// <summary>Stores copies of the ids; <paramref name="Id2"/> is ignored when <paramref name="Id1"/> is null.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFromIds(TBoldObjectId? Id1, TBoldObjectId? Id2)
    {
        EnsureIdList();
        while (fObjectIds!.Count > 0)
        {
            fObjectIds.RemoveByIndex(0);
        }

        if (Id1 is not null)
        {
            fObjectIds.Add(Id1);
            if (Id2 is not null)
            {
                fObjectIds.Add(Id2);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId? Id1 => GetId1();
    [Delphi(PortStatus.Ported)] public TBoldObjectId? Id2 => GetId2();
}

/// <summary>A multi reference: a list of ids.</summary>
[Delphi(PortStatus.Ported)]
public class TBFSObjectIdListref : TBoldFreeStandingValue, IBoldObjectIdListRef, IBoldFreeStandingIdList
{
    private TBoldObjectIdList? fIdLIst;

    [Delphi(PortStatus.Ported)] public TBoldObjectId GetIdList(int Index) => fIdLIst![Index];

    [Delphi(PortStatus.Ported)]
    public void RemoveId(TBoldObjectId Id)
    {
        int p = fIdLIst!.IndexByID(Id);
        if (p != -1)
        {
            fIdLIst.RemoveByIndex(p);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddId(TBoldObjectId Id)
    {
        EnsureList();
        fIdLIst!.Add(Id);
    }

    [Delphi(PortStatus.Ported)]
    private void EnsureList() => fIdLIst ??= new TBoldObjectIdList();

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdListRef s)
        {
            throw UnknownTypeOfSource();
        }

        EnsureList();
        fIdLIst!.Clear();
        for (int i = 0; i < s.Count; i++)
        {
            fIdLIst.Add(s.IdList(i));
        }
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;

    [Delphi(PortStatus.Ported)]
    protected internal override void ApplyTranslationList(TBoldIDTranslationList TranslationList) =>
        fIdLIst?.ApplyTranslationList(TranslationList);

    [Delphi(PortStatus.Ported)] public int GetCount() => fIdLIst?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fIdLIst);
        base.Dispose();
    }

    /// <summary>Replaces the list with a copy of <paramref name="IdList"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFromIdList(TBoldObjectIdList? IdList)
    {
        fIdLIst?.Dispose();
        fIdLIst = IdList!.Clone();
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId IdList(int Index) => GetIdList(Index);
    [Delphi(PortStatus.Ported)] public int Count => GetCount();
}

/// <summary>A multi reference with link objects: two parallel id lists.</summary>
[Delphi(PortStatus.Ported)]
public class TBFSObjectIdListrefPair : TBoldFreeStandingValue, IBoldObjectIdListRefPair, IBoldFreeStandingIdListPair
{
    private TBoldObjectIdList? fIdLIst1;
    private TBoldObjectIdList? fIdList2;

    [Delphi(PortStatus.Ported)] public TBoldObjectId GetIdList1(int Index) => fIdLIst1![Index];
    [Delphi(PortStatus.Ported)] public TBoldObjectId GetIdList2(int Index) => fIdList2![Index];

    [Delphi(PortStatus.Ported)]
    private void EnsureLists()
    {
        fIdLIst1 ??= new TBoldObjectIdList();
        fIdList2 ??= new TBoldObjectIdList();
    }

    /// <summary>Appends an object id and its link object id; the lists must exist, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddIds(TBoldObjectId Id1, TBoldObjectId Id2)
    {
        fIdLIst1!.Add(Id1);
        fIdList2!.Add(Id2);
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveId(TBoldObjectId Id)
    {
        int p = fIdLIst1!.IndexByID(Id);
        if (p != -1)
        {
            fIdLIst1.RemoveByIndex(p);
            fIdList2!.RemoveByIndex(p);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdListRefPair s)
        {
            throw UnknownTypeOfSource();
        }

        EnsureLists();
        fIdLIst1!.Clear();
        fIdList2!.Clear();
        for (int i = 0; i < s.Count; i++)
        {
            fIdLIst1.Add(s.IdList1(i));
            fIdList2.Add(s.IdList2(i));
        }
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair;

    [Delphi(PortStatus.Ported)]
    protected internal override void ApplyTranslationList(TBoldIDTranslationList TranslationList)
    {
        if (fIdLIst1 is not null)
        {
            // The two lists are either both assigned or both unassigned.
            fIdLIst1.ApplyTranslationList(TranslationList);
            fIdList2!.ApplyTranslationList(TranslationList);
        }
    }

    [Delphi(PortStatus.Ported)] public int GetCount() => fIdLIst1?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fIdLIst1);
        SysUtils.FreeAndNil(ref fIdList2);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public void SetFromIdLists(TBoldObjectIdList? IdList1, TBoldObjectIdList? IdList2)
    {
        fIdLIst1?.Dispose();
        fIdLIst1 = IdList1!.Clone();
        fIdList2?.Dispose();
        fIdList2 = IdList2!.Clone();
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId IdList1(int Index) => GetIdList1(Index);
    [Delphi(PortStatus.Ported)] public TBoldObjectId IdList2(int Index) => GetIdList2(Index);
    [Delphi(PortStatus.Ported)] public int Count => GetCount();
}

/// <summary>The object contents of a free-standing value space, owned, with a hash index by object id.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFSObjectContentList : TBoldIndexableList
{
    private static int IX_FSObjectContent = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldFSObjectContentList()
    {
        SetIndexCapacity(1);
        OwnsEntries = true;
        BoldIndexableList.SetIndexVariable(ref IX_FSObjectContent, AddIndex(new TBoldFSObjectContentHashIndex()));
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingObjectContents? FindObjectContentById(TBoldObjectId Id) =>
        (TBoldFreeStandingObjectContents?)((TBoldFSObjectContentHashIndex)Indexes(IX_FSObjectContent)!).FindById(Id);

    [Delphi(PortStatus.Ported)]
    private TBoldFreeStandingObjectContents? GetItems(int index) => (TBoldFreeStandingObjectContents?)base[index];

    [Delphi(PortStatus.Ported)]
    private void SetItems(int index, TBoldFreeStandingObjectContents? Value) => base[index] = Value;

    [Delphi(PortStatus.Ported, Symbol = "TBoldFSObjectContentList.Items")]
    public new TBoldFreeStandingObjectContents? this[int index]
    {
        get => GetItems(index);
        set => SetItems(index, value);
    }
}

/// <summary>Hashes object contents by their object id.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldFSObjectContentHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    private static TBoldObjectId ItemAsBoldObjectId(object? Item) => ((TBoldFreeStandingObjectContents)Item!).fObjectId!;

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => ItemAsBoldObjectId(Item).Hash;

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ((TBoldObjectId)Key).IsEqual(ItemAsBoldObjectId(Item));

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => ((TBoldObjectId)Key).Hash;

    [Delphi(PortStatus.Ported)]
    internal object? FindById(TBoldObjectId boldObjectId) => Find(boldObjectId);
}
