// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// TBoldObjectReference: single links and free-standing object references.

using static Bold.BoldCoreConsts;
using static Bold.BoldElements;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>A reference to one object: a single link of an object, or a free-standing reference. Its controller holds the value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectReference : TBoldMember
{
    internal TBoldAbstractObjectReferenceController? fObjectReferenceController;

    public TBoldObjectReference()
    {
    }

    protected TBoldObjectReference(Action<TBoldMember> Construction)
        : base(Construction)
    {
    }

    /// <summary>A free-standing reference to objects of the class (of the default system).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectReference.CreateTypedReference")]
    public TBoldObjectReference(TBoldObjectClass ObjectClass)
        : base(m => ((TBoldObjectReference)m).CreateTypedReference(ObjectClass))
    {
    }

    private void CreateTypedReference(TBoldObjectClass ObjectClass)
    {
        TBoldSystem? aSystem = TBoldSystem.DefaultSystem();
        InitializeMember(null, aSystem?.BoldSystemTypeInfo.TopSortedClasses.ItemsByObjectClass(ObjectClass));
        HasOldValues = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectReference.Destroy")]
    public override void Dispose()
    {
        PrepareToDestroy();
        fObjectReferenceController?.Dispose();
        fObjectReferenceController = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Mutable)
        {
            AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueChanged, beValueInvalid), RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetLocator()
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetLocator", "");
        }

        return ReferenceController.GetLocator();
    }

    /// <summary>The object, fetched when not loaded.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetBoldObject()
    {
        TBoldObjectLocator? aLocator = Locator;
        if (aLocator is null)
        {
            return null;
        }

        aLocator.EnsureBoldObject();
        return aLocator.BoldObject;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldAbstractController? GetController() => fObjectReferenceController;

    [Delphi(PortStatus.Ported)]
    private void SetLocator(TBoldObjectLocator? NewLocator)
    {
        EnsureContentsCurrent();
        InternalSetLocator(NewLocator);
    }

    [Delphi(PortStatus.Ported)]
    private void InternalSetLocator(TBoldObjectLocator? NewLocator)
    {
        if (NewLocator != ReferenceController.GetLocator())
        {
            BoldClearLastFailure();
            if (!CanSetLocator(NewLocator, null))
            {
                BoldRaiseLastFailure(this, "SetLocator", "");
            }

            ReferenceController.SetLocator(NewLocator);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldObject(TBoldObject? NewObject)
    {
        if (NewObject is not null)
        {
            Locator = NewObject.BoldObjectLocator;
        }
        else
        {
            Clear();
        }
    }

    /// <summary>Sets the reference to nil, when CanClear allows it.</summary>
    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
        BoldClearLastFailure();
        if (!CanClear(null))
        {
            BoldRaiseLastFailure(this, "Clear", "");
        }

        Locator = null;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => BoldObject?.GetStringRepresentation(Representation) ?? "";

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value) => BoldObject?.SetStringRepresentation(Representation, Value);

    [Delphi(PortStatus.Ported)]
    public override bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        TBoldElement? CompareElement = BoldElement;
        if (CompareElement is TBoldObjectReference reference)
        {
            CompareElement = reference.BoldObject;
        }

        if (BoldObject is not null)
        {
            if (CompareType != TBoldCompareType.ctDefault)
            {
                throw new EBold(SysUtils.Format(sUnknownCompareType, GetType().Name, "IsEqualAs"));
            }

            return BoldObject.IsEqualAs(CompareType, BoldElement);
        }

        return CompareElement is null;
    }

    /// <summary>An immutable list with the object (or empty).</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList)
    {
        var systemTypeInfo = (TBoldSystemTypeInfo)BoldType!.SystemTypeInfo!;
        var NewList = (TBoldObjectList)CreateWithTypeInfo(typeof(TBoldObjectList), systemTypeInfo.ListTypeInfoByElement(BoldType));
        if (BoldObject is not null)
        {
            NewList.Add(BoldObject);
        }

        NewList.MakeImmutable();
        ResultList.SetOwnedValue(NewList);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (ReferenceController.ProxyInterface(IId, Mode, out Obj))
        {
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Mutable)
        {
            AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueChanged), breReSubscribe);
        }

        BoldObject?.SubscribeToStringRepresentation(Representation, Subscriber, RequestedEvent);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => ReferenceController.StreamName;

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        switch (Source)
        {
            case null:
                BoldObject = null;
                break;
            case TBoldObject obj:
                BoldObject = obj;
                break;
            case TBoldObjectReference reference:
                Locator = reference.Locator;
                break;
            default:
                base.Assign(Source);
                break;
        }
    }

    /// <summary>Compares the objects: nil first, else as the objects compare.</summary>
    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (CompareType != TBoldCompareType.ctDefault)
        {
            return base.CompareToAs(CompareType, BoldElement);
        }

        TBoldObject? CompareObj = BoldElement switch
        {
            TBoldObject obj => obj,
            TBoldObjectReference reference => reference.BoldObject,
            null => null,
            _ => throw new EBold(SysUtils.Format(sUnknownObjectType, GetType().Name, BoldElement.GetType().Name)),
        };

        if (CompareObj == BoldObject)
        {
            return 0;
        }

        if (CompareObj is null)
        {
            return 1;
        }

        if (BoldObject is null)
        {
            return -1;
        }

        return BoldObject.CompareToAs(CompareType, CompareObj);
    }

    /// <summary>False for an object of another system or of a class the role does not allow; else asks the subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CanSetLocator(TBoldObjectLocator? NewLocator, TBoldSubscriber? Subscriber)
    {
        if (NewLocator is not null && OwningObject is not null && OwningObject.BoldSystem != NewLocator.BoldSystem)
        {
            return false;
        }

        return VerifyClass(NewLocator) && SendQuery(bqMaySetValue, [NewLocator], Subscriber);
    }

    /// <summary>True when the locator's class conforms to the reference's class (or could, when its class is not known exactly).</summary>
    [Delphi(PortStatus.Ported)]
    private bool VerifyClass(TBoldObjectLocator? aLocator)
    {
        if (BoldType is not null && aLocator is not null)
        {
            var AllowedClass = (TBoldClassTypeInfo)BoldType;
            TBoldClassTypeInfo KnownClass = ((TBoldSystemTypeInfo)BoldType.SystemTypeInfo!).TopSortedClasses[aLocator.BoldObjectID.TopSortedIndex];
            bool result;
            if (KnownClass.ConformsTo(AllowedClass))
            {
                result = true;
            }
            else if (aLocator.BoldObjectID.TopSortedIndexExact)
            {
                result = false;
            }
            else
            {
                result = AllowedClass.ConformsTo(KnownClass);
            }

            if (!result)
            {
                SetBoldLastFailureReason(new TBoldFailureReason(sCannotSetSuchReference, [KnownClass.ExpressionName, AllowedClass.ExpressionName], this));
            }

            return result;
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSet(TBoldObject NewObject, TBoldSubscriber? Subscriber) => CanSetLocator(NewObject.BoldObjectLocator, Subscriber);

    [Delphi(PortStatus.Ported)]
    internal override void MakeDbCurrent() => ReferenceController.MakeDbCurrent();

    /// <summary>Also creates the controller that fits the role: plain, direct or indirect single link, or a link object role.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        if (BoldMemberRTInfo is null || BoldMemberRTInfo.IsDerived)
        {
            fObjectReferenceController = new TBoldObjectReferenceController(this);
        }
        else
        {
            var role = (TBoldRoleRTInfo)BoldMemberRTInfo;
            if (role.RoleType == TBoldRoleType.rtInnerLinkRole)
            {
                fObjectReferenceController = new TBoldLinkObjectSingleLinkController(this);
            }
            else if (role.RoleType == TBoldRoleType.rtLinkRole)
            {
                fObjectReferenceController = new TBoldLinkObjectReferenceController(this);
            }
            else if (role.IsIndirect)
            {
                fObjectReferenceController = new TBoldIndirectSingleLinkController(this);
            }
            else
            {
                fObjectReferenceController = new TBoldDirectSingleLinkController(this);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal override void PreDiscard() => ReferenceController.PreDiscard();

    [Delphi(PortStatus.Ported)]
    public override bool ObserverMayModify(object? Observer) =>
        base.ObserverMayModify(Observer) && (BoldRoleRTInfo is null || BoldRoleRTInfo.RoleType == TBoldRoleType.rtRole);

    [Delphi(PortStatus.Ported)]
    private TBoldRoleRTInfo? GetBoldRoleRTInfo() => (TBoldRoleRTInfo?)BoldMemberRTInfo;

    [Delphi(PortStatus.Ported)]
    public bool CanClear(TBoldSubscriber? Subscriber) => SendQuery(bqMayClear, [], Subscriber);

    [Delphi(PortStatus.Ported)]
    internal override bool InternalMayUpdate() => ReferenceController.MayUpdate();

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => ReferenceController.ProxyClass();

    [Delphi(PortStatus.Ported)]
    internal override void AssignContentValueFromElement(TBoldElement? source)
    {
        var controller = (TBoldObjectReferenceController)ReferenceController;
        switch (source)
        {
            case null:
                controller.fLocator = null;
                break;
            case TBoldObject obj:
                controller.fLocator = obj.BoldObjectLocator;
                break;
            case TBoldObjectReference reference:
                controller.fLocator = reference.Locator;
                break;
            default:
                base.AssignContentValueFromElement(source);
                break;
        }
    }

    /// <summary>
    /// After a save, puts the new link into the old values of the object(s) at the other end, removing it from the old
    /// other end (direct single links and inner link roles stored in the object).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal override void AdjustOldValues(TBoldIDTranslationList? Translationlist)
    {
        void RemoveIdFromLink(TBoldObjectId LinkId, IBoldValue OldLink)
        {
            switch (OldLink)
            {
                case IBoldObjectIdRef IdRef:
                    IdRef.SetFromId(null);
                    break;
                case IBoldObjectIdRefPair IdRefPair:
                    IdRefPair.SetFromIds(null, null);
                    break;
                case IBoldFreeStandingIdList FreestandingList:
                    FreestandingList.RemoveId(LinkId);
                    break;
                case IBoldFreeStandingIdListPair FreestandingListPair:
                    FreestandingListPair.RemoveId(LinkId);
                    break;
                default:
                    throw new EBoldInternal(SysUtils.Format("%s.AdjustOldValues (RemoveIdFromLink): Unknown type of link", GetType().Name));
            }
        }

        void AddIdToLink(TBoldObjectId Id1, TBoldObjectId? Id2, IBoldValue OldLink)
        {
            TBoldObjectId? New(TBoldObjectId? id) => Translationlist is null ? id : Translationlist.TranslateToNewId(id);

            switch (OldLink)
            {
                case IBoldObjectIdRef IdRef:
                    IdRef.SetFromId(New(Id1));
                    break;
                case IBoldObjectIdRefPair IdRefPair:
                    IdRefPair.SetFromIds(New(Id1), New(Id2));
                    break;
                case IBoldFreeStandingIdList FreestandingList:
                    FreestandingList.AddId(New(Id1)!);
                    break;
                case IBoldFreeStandingIdListPair FreestandingListPair:
                    FreestandingListPair.AddIds(New(Id1)!, New(Id2)!);
                    break;
                default:
                    throw new EBoldInternal(SysUtils.Format("%s.AdjustOldValues (RemoveIdFromLink): Unknown type of link", GetType().Name));
            }
        }

        base.AdjustOldValues(Translationlist);

        IBoldValueSpace OldValues = OwningObject!.BoldSystem.fOldValueHandler.OldValues;
        TBoldRoleRTInfo role = BoldRoleRTInfo!;
        if (role.IsStoredInObject)
        {
            if (OldValue is IBoldObjectIdRef { Id: { } oldId })
            {
                IBoldValue? OldLink = OldValues.ObjectContentsByObjectId(oldId)?.ValueByIndex(role.IndexOfOtherEnd);
                if (OldLink is not null)
                {
                    RemoveIdFromLink(OwningObject.BoldObjectLocator!.BoldObjectID, OldLink);
                }
            }

            TBoldObjectLocator? locator = Locator;
            if (locator is not null)
            {
                IBoldValue? OldLink = OldValues.ObjectContentsByObjectId(locator.BoldObjectID)?.ValueByIndex(role.IndexOfOtherEnd);
                if (OldLink is not null)
                {
                    TBoldObjectId? RemoteId = null;
                    if (role.RoleType == TBoldRoleType.rtInnerLinkRole &&
                        OwningObject.BoldMembers(role.OtherIndexInLinkClass) is TBoldObjectReference OtherInnerLink &&
                        OtherInnerLink.Locator is not null)
                    {
                        RemoteId = OtherInnerLink.Locator.BoldObjectID;
                    }

                    AddIdToLink(OwningObject.BoldObjectLocator!.BoldObjectID, RemoteId, OldLink);
                }
            }
        }
    }

    /// <summary>Ordinary roles start as nil.</summary>
    [Delphi(PortStatus.Ported)]
    internal override void DoSetInitialValue()
    {
        if (BoldRoleRTInfo!.RoleType == TBoldRoleType.rtRole)
        {
            BoldObject = null;
        }
    }

    /// <summary>The id of the object at the other end in the old value, when that end is stored in the object.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObjectId? GetOldEmbeddingOtherEndId()
    {
        IBoldValue? OldRef = OldValue;
        if (OldRef is not null && BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.IsStoredInObject)
        {
            if (OldRef is IBoldObjectIdRef OldIdRef)
            {
                return OldIdRef.Id;
            }

            if (OldRef is IBoldObjectIdRefPair OldIdRefPair)
            {
                return OldIdRefPair.Id1;
            }
        }

        return null;
    }

    /// <summary>True when the value refers to the same object as this reference.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool IsEqualToValue(IBoldValue Value)
    {
        TBoldObjectId? ValueId = Value switch
        {
            IBoldObjectIdRef IdRef => IdRef.Id,
            IBoldObjectIdRefPair IdRefPair => IdRefPair.Id1,
            _ => throw new EBoldInternal("Internal error"),
        };

        TBoldObjectLocator? locator = Locator;
        if (locator is not null && ValueId is not null)
        {
            return locator.BoldObjectID.IsEqual(ValueId);
        }

        return locator is null && ValueId is null;
    }

    [Delphi(PortStatus.Ported)] internal TBoldAbstractObjectReferenceController ReferenceController => fObjectReferenceController!;

    [Delphi(PortStatus.Ported)]
    public TBoldObject? BoldObject
    {
        get => GetBoldObject();
        set => SetBoldObject(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldRoleRTInfo? BoldRoleRTInfo => GetBoldRoleRTInfo();

    [Delphi(PortStatus.Ported)]
    public TBoldObjectLocator? Locator
    {
        get => GetLocator();
        set => SetLocator(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId? OldEmbeddingOtherEndId => GetOldEmbeddingOtherEndId();

    [Delphi(PortStatus.Ported)]
    public bool HasOldValues
    {
        get => GetElementFlag(befHasOldValues);
        set => SetElementFlag(befHasOldValues, value);
    }
}
