// Delphi: Source/Persistence/Propagation/BoldIDAdder.pas
// Port map: CSharp/port/units/BoldIDAdder.md

namespace Bold;

/// <summary>
/// A persistence controller that puts the client's id (its listener's) on the calls it passes on, so the snooper
/// further down knows which client fetches and updates. It resumes a suspended listener.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldIDAdder : TBoldPersistenceControllerPassthrough
{
    [Delphi(PortStatus.Ported)]
    public TBoldIDAdder()
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldClientID GetClientID() => Listener?.BoldClientID ?? BoldDefs.NOTVALIDCLIENTID;

    private void ResumeListener()
    {
        if (Listener is { Suspended: true } listener)
        {
            listener.Resume();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID)
    {
        ResumeListener();
        base.PMFetch(ObjectIdList, ValueSpace, MemberIdList, FetchMode, this.BoldClientID);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID)
    {
        ResumeListener();
        base.PMFetchIDListWithCondition(ObjectIdList, ValueSpace, FetchMode, Condition, this.BoldClientID);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        ResumeListener();
        base.PMUpdate(ObjectIdList, ValueSpace, Old_Values, Precondition, TranslationList, ref TimeStamp, this.BoldClientID);
    }

    [Delphi(PortStatus.Ported)] public TBoldClientID BoldClientID => GetClientID();
    [Delphi(PortStatus.Ported)] public TBoldListenerThread? Listener { get; set; }
}
