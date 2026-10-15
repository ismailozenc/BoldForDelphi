// Delphi: Source/ValueSpace/Interfaces/BoldValueSpaceInterfaces.pas
// Port map: CSharp/port/units/BoldValueSpaceInterfaces.md

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldExistenceState { besNotCreated, besExisting, besDeleted }

/// <summary>Object contents by object id: what a persistence layer reads into and writes from.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldValueSpace
{
    [Delphi(PortStatus.Ported)] void AllObjectIds(TBoldObjectIdList resultList, bool OnlyLoaded);
    [Delphi(PortStatus.Ported)] void ApplytranslationList(TBoldIDTranslationList IdTranslationList);
    [Delphi(PortStatus.Ported)] void ApplyValueSpace(IBoldValueSpace ValueSpace, bool IgnorePersistenceState);
    [Delphi(PortStatus.Ported)] void EnsureObjectContents(TBoldObjectId ObjectId);
    [Delphi(PortStatus.Ported)] void EnsureObjectId(TBoldObjectId ObjectId);
    [Delphi(PortStatus.Ported)] void ExactifyIDs(TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] bool GetHasContentsForId(TBoldObjectId ObjectId);
    [Delphi(PortStatus.Ported)] IBoldObjectContents? GetObjectContentsByObjectId(TBoldObjectId ObjectId);
    [Delphi(PortStatus.Ported)] IBoldObjectContents GetEnsuredObjectContentsByObjectId(TBoldObjectId ObjectId);
    [Delphi(PortStatus.Ported)] bool HasContentsForId(TBoldObjectId ObjectId) => GetHasContentsForId(ObjectId);
    [Delphi(PortStatus.Ported)] IBoldObjectContents? ObjectContentsByObjectId(TBoldObjectId ObjectId) => GetObjectContentsByObjectId(ObjectId);
    [Delphi(PortStatus.Ported)] IBoldObjectContents EnsuredObjectContentsByObjectId(TBoldObjectId ObjectId) => GetEnsuredObjectContentsByObjectId(ObjectId);
}

/// <summary>The values of one object, with its existence, persistence state, global id, read-only flag and timestamp.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldObjectContents
{
    [Delphi(PortStatus.Ported)] void EnsureMember(TBoldMemberID MemberId, string ContentName);
    [Delphi(PortStatus.Ported)] TBoldExistenceState GetBoldExistenceState();
    [Delphi(PortStatus.Ported)] int GetBoldMemberCount();
    [Delphi(PortStatus.Ported)] TBoldValuePersistenceState GetBoldPersistenceState();
    [Delphi(PortStatus.Ported)] string GetGlobalId();
    [Delphi(PortStatus.Ported)] bool GetIsModified();
    [Delphi(PortStatus.Ported)] bool GetIsReadOnly();
    [Delphi(PortStatus.Ported)] TBoldObjectId GetObjectId();
    [Delphi(PortStatus.Ported)] IBoldValue? GetValueByIndex(int I);
    [Delphi(PortStatus.Ported)] IBoldValue? GetValueByMemberId(TBoldMemberID MemberId);
    [Delphi(PortStatus.Ported)] TBoldTimeStampType GetTimeStamp();
    [Delphi(PortStatus.Ported)] void SetBoldExistenceState(TBoldExistenceState Value);
    [Delphi(PortStatus.Ported)] void SetBoldPersistenceState(TBoldValuePersistenceState Value);
    [Delphi(PortStatus.Ported)] void SetGlobalId(string NewValue);
    [Delphi(PortStatus.Ported)] void SetIsReadOnly(bool NewValue);
    [Delphi(PortStatus.Ported)] void SetTimeStamp(TBoldTimeStampType NewValue);

    [Delphi(PortStatus.Ported)]
    TBoldExistenceState BoldExistenceState { get => GetBoldExistenceState(); set => SetBoldExistenceState(value); }

    [Delphi(PortStatus.Ported)]
    TBoldValuePersistenceState BoldPersistenceState { get => GetBoldPersistenceState(); set => SetBoldPersistenceState(value); }

    [Delphi(PortStatus.Ported)]
    string GlobalId { get => GetGlobalId(); set => SetGlobalId(value); }

    [Delphi(PortStatus.Ported)] bool IsModified => GetIsModified();

    [Delphi(PortStatus.Ported)]
    bool IsReadOnly { get => GetIsReadOnly(); set => SetIsReadOnly(value); }

    [Delphi(PortStatus.Ported)] IBoldValue? ValueByIndex(int I) => GetValueByIndex(I);
    [Delphi(PortStatus.Ported)] IBoldValue? ValueByMemberId(TBoldMemberID MemberId) => GetValueByMemberId(MemberId);
    [Delphi(PortStatus.Ported)] int MemberCount => GetBoldMemberCount();

    [Delphi(PortStatus.Ported)]
    TBoldTimeStampType TimeStamp { get => GetTimeStamp(); set => SetTimeStamp(value); }

    [Delphi(PortStatus.Ported)] TBoldObjectId ObjectId => GetObjectId();
}

/// <summary>Value space routines.</summary>
public static partial class BoldValueSpaceInterfaces
{
    [Delphi(PortStatus.Ported)] public const int BUSINESSCLASSESROOT_TOPSORTEDINDEX = 0;

    /// <summary>
    /// Copies objects (all of the source's when <paramref name="ObjectidList"/> is null) and members (all when
    /// <paramref name="MemberIdList"/> is null) from one value space to another, skipping values in the ignored states.
    /// With <paramref name="ForceCurrent"/> the copies are marked current.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldApplyPartialValueSpace(IBoldValueSpace DestVS, IBoldValueSpace SourceVS, TBoldObjectIdList? ObjectidList,
        TBoldMemberIdList? MemberIdList, bool ForceCurrent, TBoldValuePersistenceStateSet? PersistenceStatesToIgnore = null)
    {
        TBoldValuePersistenceStateSet ignore = PersistenceStatesToIgnore ?? TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid);
        TBoldObjectIdList listToCopy = ObjectidList ?? new TBoldObjectIdList();
        if (ObjectidList is null)
        {
            SourceVS.AllObjectIds(listToCopy, false);
        }

        for (int o = 0; o < listToCopy.Count; o++)
        {
            TBoldObjectId objectId = listToCopy[o];
            IBoldObjectContents? source = SourceVS.ObjectContentsByObjectId(objectId);
            if (source is null)
            {
                continue;
            }

            DestVS.EnsureObjectId(objectId);
            IBoldObjectContents dest = DestVS.EnsuredObjectContentsByObjectId(objectId);
            dest.BoldExistenceState = source.BoldExistenceState;
            dest.BoldPersistenceState = ForceCurrent ? TBoldValuePersistenceState.bvpsCurrent : source.BoldPersistenceState;

            void Copy(TBoldMemberID memberId, IBoldValue? sourceValue)
            {
                if (sourceValue is not null && !ignore.Contains(sourceValue.BoldPersistenceState))
                {
                    dest.EnsureMember(memberId, sourceValue.ContentName);
                    IBoldValue destValue = dest.ValueByMemberId(memberId)!;
                    destValue.AssignContent(sourceValue);
                    if (ForceCurrent)
                    {
                        destValue.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
                    }
                }
            }

            if (MemberIdList is not null)
            {
                for (int m = 0; m < MemberIdList.Count; m++)
                {
                    Copy(MemberIdList[m], source.ValueByMemberId(MemberIdList[m]));
                }
            }
            else
            {
                for (int m = 0; m < source.MemberCount; m++)
                {
                    Copy(new TBoldMemberID(m), source.ValueByIndex(m));
                }
            }
        }
    }
}
