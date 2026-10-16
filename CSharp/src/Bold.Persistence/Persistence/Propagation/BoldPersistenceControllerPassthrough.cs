// Delphi: Source/Persistence/Propagation/BoldPersistenceControllerPassthrough.pas
// Port map: CSharp/port/units/BoldPersistenceControllerPassthrough.md

using static Bold.BoldPMConsts;

namespace Bold;

/// <summary>A persistence controller that passes every call on to the next controller; subclasses intercept some of them.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceControllerPassthrough : TBoldPersistenceController
{
    private TBoldPersistenceController? fNextPersistenceController;

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceControllerPassthrough()
    {
    }

    /// <summary>The next controller; raises when none is set.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceController getNextPersistenceController() =>
        fNextPersistenceController ?? throw new EBold(SysUtils.Format(sNextControllerMissing, GetType().Name));

    [Delphi(PortStatus.Ported)]
    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        NextPersistenceController.PMExactifyIds(ObjectIdList, TranslationList);

    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID) =>
        NextPersistenceController.PMFetch(ObjectIdList, ValueSpace, MemberIdList, FetchMode, BoldClientID);

    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID) =>
        NextPersistenceController.PMFetchIDListWithCondition(ObjectIdList, ValueSpace, FetchMode, Condition, BoldClientID);

    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID) =>
        NextPersistenceController.PMUpdate(ObjectIdList, ValueSpace, Old_Values, Precondition, TranslationList, ref TimeStamp, BoldClientID);

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        NextPersistenceController.PMTranslateToGlobalIds(ObjectIdList, TranslationList);

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList) =>
        NextPersistenceController.PMTranslateToLocalIds(GlobalIdList, TranslationList);

    [Delphi(PortStatus.Ported)]
    public override void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList) =>
        NextPersistenceController.PMSetReadOnlyness(ReadOnlyList, WriteableList);

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToPeristenceEvents(TBoldSubscriber Subscriber) =>
        NextPersistenceController.SubscribeToPeristenceEvents(Subscriber);

    [Delphi(PortStatus.Ported)]
    public override void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        NextPersistenceController.ReserveNewIds(ValueSpace, ObjectIdList, TranslationList);

    [Delphi(PortStatus.Ported)]
    public override void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp) =>
        NextPersistenceController.PMTimestampForTime(ClockTime, ref Timestamp);

    [Delphi(PortStatus.Ported)]
    public override void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime) =>
        NextPersistenceController.PMTimeForTimestamp(Timestamp, ref ClockTime);

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceController NextPersistenceController
    {
        get => getNextPersistenceController();
        set => fNextPersistenceController = value;
    }
}
