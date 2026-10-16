// Delphi: Source/Persistence/Core/BoldAbstractSnooper.pas
// Port map: CSharp/port/units/BoldAbstractSnooper.md

using static Bold.BoldPersistenceController;

namespace Bold;

/// <summary>
/// A pass-through controller that watches fetches and updates and collects what clients subscribe to (fetched
/// objects, links and classes) and the events an update causes; subclasses transmit them, for example to a propagator.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractSnooper : TBoldPersistenceControllerPassthrough
{
    private readonly TMoldModel fMoldModel;
    private readonly TStringList fEvents;
    private readonly TStringList fSubscriptions;
    private readonly TStringList fCancelledSubscriptions;
    private bool fModelSorted;
    private TBoldNotifyEventWithErrorMessage? FOnPropagatorFailure;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractSnooper(TMoldModel MoldModel)
    {
        fModelSorted = false;
        fMoldModel = MoldModel;
        fEvents = new TStringList { Sorted = true, Duplicates = TDuplicates.dupIgnore };
        fSubscriptions = new TStringList();
        fCancelledSubscriptions = new TStringList();
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldAbstractSnooper.Destroy", Note = "Freed the string lists; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected void GenerateNonEmbeddedStateChangedEvent(TBoldObjectId? OldID, TBoldObjectId? NewID, string NonEmbeddedLinkName)
    {
        if (OldID is not null && NewID is not null && !OldID.IsEqual(NewID))
        {
            Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", NonEmbeddedLinkName, "", OldID));
            Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", NonEmbeddedLinkName, "", NewID));
        }
        else if (OldID is not null && NewID is null)
        {
            Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", NonEmbeddedLinkName, "", OldID));
        }
        else if (NewID is not null && OldID is null)
        {
            Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", NonEmbeddedLinkName, "", NewID));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void SubscribeToNonEmbeddedStateChangedEvent(TBoldObjectId? Id, string NonEmbeddedLinkName)
    {
        if (Id is not null)
        {
            Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", NonEmbeddedLinkName, "", Id));
        }
    }

    /// <summary>The id a single link member refers to, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldObjectId? ObjectIdByMemberIndex(IBoldObjectContents? Object_Content, int MemberIndex) =>
        Object_Content?.ValueByIndex(MemberIndex) is IBoldObjectIdRef IDRef ? IDRef.GetId() : null;

    /// <summary>True for an embedded role; gives the name of the non-embedded role it is the other end of (the main role of a link role).</summary>
    [Delphi(PortStatus.Ported)]
    protected bool MemberIsEmbeddedSingleLink(TMoldMember MoldMember, ref string NonEmbeddedLinkName)
    {
        if (MoldMember is TMoldRole MoldRole && MoldRole.EffectiveEmbedded)
        {
            TMoldRole NonEmbeddedLink = MoldRole.OtherEnd!;
            if (NonEmbeddedLink.RoleType == TBoldRoleType.rtLinkRole)
            {
                NonEmbeddedLink = NonEmbeddedLink.MainRole!;
            }

            NonEmbeddedLinkName = NonEmbeddedLink.ExpandedExpressionName;
            return true;
        }

        return false;
    }

    /// <summary>True for an ordinary role that is not embedded; gives its name.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool MemberIsNonEmbeddedLink(TMoldMember MoldMember, ref string MemberName)
    {
        if (MoldMember is TMoldRole MoldRole && !MoldRole.EffectiveEmbedded && MoldRole.RoleType == TBoldRoleType.rtRole)
        {
            MemberName = MoldRole.ExpandedExpressionName;
            return true;
        }

        return false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractSnooper.ClassNameFromClassID")]
    protected string ClassNameFromClassID(int TopSortedIndex) => MoldModel.Classes[TopSortedIndex].ExpandedExpressionName;

    /// <summary>Events for the other ends of embedded links whose target changed between the old and new contents.</summary>
    [Delphi(PortStatus.Ported)]
    protected void NonEmbeddedStateOfObjectChanged(IBoldObjectContents? Object_Content, IBoldObjectContents? NewObject_Content, TMoldClass MoldClass)
    {
        int MemberCount = Object_Content?.GetBoldMemberCount() ?? NewObject_Content?.GetBoldMemberCount() ?? 0;
        string NonEmbeddedLinkName = "";
        for (int j = 0; j < MemberCount; j++)
        {
            if (MemberIsEmbeddedSingleLink(MoldClass.AllBoldMembers[j], ref NonEmbeddedLinkName))
            {
                GenerateNonEmbeddedStateChangedEvent(ObjectIdByMemberIndex(Object_Content, j), ObjectIdByMemberIndex(NewObject_Content, j), NonEmbeddedLinkName);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void ClearEvents()
    {
        Events.Clear();
        Subscriptions.Clear();
        CancelledSubscriptions.Clear();
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void EnsureDataBaseLock(TBoldClientID ClientID)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void ReleaseDataBaseLock(TBoldClientID ClientID)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected void DoPropagatorFailure(object Sender, string ErrorMessage) => FOnPropagatorFailure?.Invoke(Sender, ErrorMessage);

    /// <summary>A class-changed event for the class and each superclass.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AddClassEvents(int TopsortedIndex)
    {
        TMoldClass? MoldClass = MoldModel.Classes[TopsortedIndex];
        while (MoldClass is not null)
        {
            Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsClassChanged, MoldClass.ExpandedExpressionName, "", "", null));
            MoldClass = MoldClass.SuperClass;
        }
    }

    /// <summary>Fetches, then for a real client subscribes to the fetched objects' embedded state and non-embedded links.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID)
    {
        base.PMFetch(ObjectIdList, ValueSpace, MemberIdList, FetchMode, BoldClientID);
        if (BoldClientID == NOTVALIDCLIENTID)
        {
            return;
        }

        if (!fModelSorted)
        {
            MoldModel.EnsureTopSorted();
            fModelSorted = true;
        }

        string MemberName = "";
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            bool LoadingEmbedded = false;
            IBoldObjectContents? Object_Content = ValueSpace.GetObjectContentsByObjectId(ObjectIdList[i]);
            if (MemberIdList is not null)
            {
                if (Object_Content is not null)
                {
                    TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
                    for (int j = 0; j < MemberIdList.Count; j++)
                    {
                        int MemberIndex = MemberIdList[j].MemberIndex;
                        if (MemberIsNonEmbeddedLink(MoldClass.AllBoldMembers[MemberIndex], ref MemberName))
                        {
                            SubscribeToNonEmbeddedStateChangedEvent(ObjectIdList[i], MemberName);
                        }
                        else
                        {
                            LoadingEmbedded = true;
                        }
                    }
                }
            }
            else if (Object_Content is not null)
            {
                LoadingEmbedded = true;
                TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
                for (int j = 0; j < Object_Content.GetBoldMemberCount(); j++)
                {
                    if (!MoldClass.AllBoldMembers[j].EffectiveDelayedFetch && MemberIsNonEmbeddedLink(MoldClass.AllBoldMembers[j], ref MemberName))
                    {
                        SubscribeToNonEmbeddedStateChangedEvent(ObjectIdList[i], MemberName);
                    }
                }
            }

            if (LoadingEmbedded)
            {
                Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "", ObjectIdList[i]));
            }
        }

        TransmitEvents(BoldClientID);
    }

    /// <summary>Fetches, then for a real client subscribes to changes of the condition's class (plain class conditions only).</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID)
    {
        base.PMFetchIDListWithCondition(ObjectIdList, ValueSpace, FetchMode, Condition, BoldClientID);
        if (BoldClientID == NOTVALIDCLIENTID)
        {
            return;
        }

        if (Condition.GetType() == typeof(TBoldConditionWithClass))
        {
            int TopSortedIndex = ((TBoldConditionWithClass)Condition).TopSortedIndex;
            Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsClassChanged, ClassNameFromClassID(TopSortedIndex), "", "", null));
        }

        TransmitEvents(BoldClientID);
    }

    /// <summary>
    /// Gives new objects ids, records the events and subscriptions of the changes, updates (inside the database lock),
    /// then adds the events of changed embedded links and transmits.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        System.Diagnostics.Debug.Assert(MoldModel is not null, "Snooper has no Model");
        TBoldObjectIdList LocalObjectIdList = ObjectIdList.Clone();
        TBoldFreeStandingValueSpace? LocalOld_Values = null;
        try
        {
            if (Old_Values is null)
            {
                LocalOld_Values = new TBoldFreeStandingValueSpace();
                Old_Values = LocalOld_Values;
            }

            TranslationList ??= new TBoldIDTranslationList();
            ReserveNewIds(ValueSpace, LocalObjectIdList, TranslationList);
            ValueSpace.ApplytranslationList(TranslationList);
            LocalObjectIdList.ApplyTranslationList(TranslationList);

            if (BoldClientID != NOTVALIDCLIENTID)
            {
                string MemberName = "";
                for (int i = 0; i < LocalObjectIdList.Count; i++)
                {
                    IBoldObjectContents? Object_Content = ValueSpace.GetObjectContentsByObjectId(LocalObjectIdList[i]);
                    System.Diagnostics.Debug.Assert(Object_Content is not null, "Object does not exist");
                    if (Object_Content!.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
                    {
                        switch (Object_Content.BoldExistenceState)
                        {
                            case TBoldExistenceState.besExisting:
                                AddClassEvents(LocalObjectIdList[i].TopSortedIndex);
                                Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "",
                                    TranslationList.TranslateToNewId(LocalObjectIdList[i])));
                                break;
                            case TBoldExistenceState.besDeleted:
                                AddClassEvents(LocalObjectIdList[i].TopSortedIndex);
                                Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsObjectDeleted, "", "", "", LocalObjectIdList[i]));
                                CancelledSubscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "", LocalObjectIdList[i]));
                                TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
                                for (int j = 0; j < MoldClass.AllBoldMembers.Count; j++)
                                {
                                    MemberName = MoldClass.AllBoldMembers[j].ExpandedExpressionName;
                                    if (MemberIsNonEmbeddedLink(MoldClass.AllBoldMembers[j], ref MemberName))
                                    {
                                        CancelledSubscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "",
                                            MemberName, "", LocalObjectIdList[i]));
                                    }
                                }

                                break;
                        }
                    }
                    else
                    {
                        Events.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "", LocalObjectIdList[i]));
                    }
                }
            }

            EnsureDataBaseLock(BoldClientID);
            try
            {
                base.PMUpdate(LocalObjectIdList, ValueSpace, Old_Values, Precondition, null, ref TimeStamp, BoldClientID);
            }
            finally
            {
                ReleaseDataBaseLock(BoldClientID);
            }

            if ((Precondition is null || !Precondition.Failed) && BoldClientID != NOTVALIDCLIENTID)
            {
                for (int i = LocalObjectIdList.Count - 1; i >= 0; i--)
                {
                    IBoldObjectContents? Object_Content = Old_Values.GetObjectContentsByObjectId(LocalObjectIdList[i]);
                    TBoldObjectId? ObjectId = LocalObjectIdList[i];
                    IBoldObjectContents? NewObject_Content = ObjectId is not null ? ValueSpace.GetObjectContentsByObjectId(ObjectId) : null;
                    NonEmbeddedStateOfObjectChanged(Object_Content, NewObject_Content, MoldModel.Classes[LocalObjectIdList[i].TopSortedIndex]);
                }

                TransmitEvents(BoldClientID);
            }
        }
        finally
        {
            LocalOld_Values?.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract void TransmitEvents(TBoldClientID ClientID);

    [Delphi(PortStatus.Ported)] public TMoldModel MoldModel => fMoldModel;
    [Delphi(PortStatus.Ported)] public TStringList Events => fEvents;
    [Delphi(PortStatus.Ported)] public TStringList Subscriptions => fSubscriptions;
    [Delphi(PortStatus.Ported)] public TStringList CancelledSubscriptions => fCancelledSubscriptions;

    [Delphi(PortStatus.Ported)]
    public TBoldNotifyEventWithErrorMessage? OnPropagatorFailure
    {
        get => FOnPropagatorFailure;
        set => FOnPropagatorFailure = value;
    }
}
