// Delphi: Source/Persistence/Core/BoldPersistenceController.pas
// Port map: CSharp/port/units/BoldPersistenceController.md

namespace Bold;

/// <summary>
/// What an object space needs from storage: fetch objects and ids, update, translate ids, reserve new ids. Persistence
/// controllers (file, SQL, remote, pass-through) implement it.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPersistenceController : TBoldSubscribableObject
{
    [Delphi(PortStatus.Ported)]
    public abstract void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList);

    [Delphi(PortStatus.Ported)]
    public abstract void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID);

    [Delphi(PortStatus.Ported)]
    public abstract void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID);

    [Delphi(PortStatus.Ported)]
    public abstract void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID);

    [Delphi(PortStatus.Ported)]
    public abstract void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList);

    [Delphi(PortStatus.Ported)]
    public abstract void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList);

    [Delphi(PortStatus.Ported)]
    public abstract void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList);

    /// <summary>Subscribes to the start and end of fetches, id fetches and updates.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void SubscribeToPeristenceEvents(TBoldSubscriber Subscriber)
    {
        AddSubscription(Subscriber, BoldSubscription.bpeStartFetch, BoldSubscription.bpeStartFetch);
        AddSubscription(Subscriber, BoldSubscription.bpeEndFetch, BoldSubscription.bpeEndFetch);
        AddSubscription(Subscriber, BoldSubscription.bpeStartUpdate, BoldSubscription.bpeStartUpdate);
        AddSubscription(Subscriber, BoldSubscription.bpeEndUpdate, BoldSubscription.bpeEndUpdate);
        AddSubscription(Subscriber, BoldSubscription.bpeStartFetchID, BoldSubscription.bpeStartFetchID);
        AddSubscription(Subscriber, BoldSubscription.bpeEndFetchID, BoldSubscription.bpeEndFetchID);
    }

    /// <summary>Whether multi links are stored in the object; false here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool MultilinksAreStoredInObject() => false;

    [Delphi(PortStatus.Ported)]
    public abstract void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList);

    /// <summary>The timestamp of a clock time; raises EBoldFeatureNotImplementedYet here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp) =>
        throw new EBoldFeatureNotImplementedYet(SysUtils.Format(PersistenceConsts.sPMTimeStampForTimeNotSupported, GetType().Name));

    [Delphi(PortStatus.Ported)]
    public virtual void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime) =>
        throw new EBoldFeatureNotImplementedYet(SysUtils.Format(PersistenceConsts.sPMTimeForTimeStampNotSupported, GetType().Name));
}

/// <summary>Unit-level constants of BoldPersistenceController.</summary>
public static partial class BoldPersistenceController
{
    /// <summary>The client id of no (or an invalid) client.</summary>
    [Delphi(PortStatus.Ported)] public const int NOTVALIDCLIENTID = -1;
}
