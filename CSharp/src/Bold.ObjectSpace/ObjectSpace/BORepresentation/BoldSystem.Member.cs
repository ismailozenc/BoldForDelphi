// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// TBoldMember, TBoldAttribute, the failure reasons and the member factory.

using System.Reflection;
using System.Runtime.ExceptionServices;
using static Bold.BoldCoreConsts;
using static Bold.BoldElements;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>
/// A value of an object (attribute, single or multi link), or a free-standing value or list: persistence state,
/// derivation, the May... queries, modification bracketing (StartModify, EndModify) and the value as IBoldValue.
/// </summary>
/// <remarks>
/// Delphi creates members of a class only known at run time through class references (TBoldMemberClass). Here that
/// goes through <see cref="CreateInstance"/>: subclasses must have a public parameterless constructor (Delphi's Create).
/// </remarks>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMember : TBoldDomainElement
{
    /// <summary>The construction <see cref="CreateInstance"/> asks the next TBoldMember constructor on this thread to run.</summary>
    [ThreadStatic] private static Action<TBoldMember>? G_PendingConstruction;

    /// <summary>The member's RT info when owned by an object, else its type info.</summary>
    internal TBoldMetaElement? fBoldMetaType;

    /// <summary>A member owned by nothing, with the type info its class stands for (Delphi's Create).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldMember.Create")]
    protected TBoldMember()
        : base(null)
    {
        Action<TBoldMember>? pending = G_PendingConstruction;
        G_PendingConstruction = null;
        if (pending is not null)
        {
            pending(this);
        }
        else
        {
            InitializeMember(null, null);
        }
    }

    /// <summary>For subclass constructors that initialize differently (Delphi's named constructors): runs <paramref name="Construction"/> instead of InitializeMember(nil, nil).</summary>
    protected TBoldMember(Action<TBoldMember> Construction)
        : base(null)
    {
        G_PendingConstruction = null;
        Construction(this);
    }

    /// <summary>
    /// Creates a member of <paramref name="MemberClass"/> whose constructor runs <paramref name="Construction"/> instead
    /// of its usual body; this is how Delphi's constructors called through a class reference are done here.
    /// </summary>
    protected internal static TBoldMember CreateInstance(TBoldMemberClass MemberClass, Action<TBoldMember> Construction)
    {
        ConstructorInfo ctor = MemberClass.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes) ??
            throw new EBoldInternal(SysUtils.Format("%s has no parameterless constructor", MemberClass.Name));
        G_PendingConstruction = Construction;
        try
        {
            return (TBoldMember)ctor.Invoke(null);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
        finally
        {
            G_PendingConstruction = null;
        }
    }

    /// <summary>A free-standing member of the class with the type info.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the member class, as Delphi calls it on a class reference.")]
    public static TBoldMember CreateWithTypeInfo(TBoldMemberClass MemberClass, TBoldElementTypeInfo? ElementTypeInfo) =>
        CreateInstance(MemberClass, m => m.InitializeMember(null, ElementTypeInfo));

    /// <summary>A member of the class for the object and RT info.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the member class, as Delphi calls it on a class reference.")]
    internal static TBoldMember InternalCreate(TBoldMemberClass MemberClass, TBoldObject OwningObject, TBoldMemberRTInfo? MemberRTInfo, bool SetInitialValue) =>
        CreateInstance(MemberClass, m =>
        {
            m.fBoldMetaType = MemberRTInfo;
            m.OwnedByObject = true;
            m.InitializeMember(OwningObject, MemberRTInfo?.BoldType);
        });

    [Delphi(PortStatus.Ported)]
    protected internal virtual string GetStreamName() =>
        throw new EBoldInternal(SysUtils.Format("%s.GetStreamName: Method is abstract, please implement", GetType().Name));

    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfo? GetBoldMemberRTInfo() => OwnedByObject ? (TBoldMemberRTInfo?)fBoldMetaType : null;

    [Delphi(PortStatus.Ported, Symbol = "TBoldMember.Destroy")]
    public override void Dispose()
    {
        PrepareToDestroy();
        if (HasDeriver)
        {
            G_ExternalDerivers.SetReferencedObjects(this, null);
        }

        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetOwningObject() => OwnedByObject ? (TBoldObject?)OwningElement : null;

    [Delphi(PortStatus.Ported)]
    private TBoldSystem? GetBoldSystem()
    {
        if (OwnedByObject)
        {
            return ((TBoldObject)OwningElement!).BoldSystem;
        }

        return (TBoldSystem?)OwningElement;
    }

    /// <summary>Derives the member from its OCL derivation expression, subscribing to what it read.</summary>
    [Delphi(PortStatus.Ported)]
    internal void CalculateDerivedMemberWithExpression(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        using var ie = new TBoldIndirectElement();
        try
        {
            OwningObject!.EvaluateAndSubscribeToExpression(BoldMemberRTInfo!.DeriveExpression, Subscriber, ie, false);
            if (ie.Value is TBoldMember member)
            {
                member.EnsureContentsCurrent();
            }

            AssignContentValueFromElement(ie.Value);
        }
        catch (Exception e)
        {
            string s = SysUtils.Format(sFailedToDerive + BoldDefs.BOLDCRLF + sOCLExpressionError, DisplayName, BoldType!.AsString, BoldMemberRTInfo!.DeriveExpression, e.Message);
            if (ie.Value is not null && !ie.Value.BoldType!.ConformsTo(BoldType))
            {
                s = SysUtils.Format(s + BoldDefs.BOLDCRLF + sPossiblyBadConformance, ie.Value.BoldType.AsString, BoldType.AsString);
            }

            throw new EBold(s, e);
        }
    }

    /// <summary>Gives the member as IBoldValue (or IBoldStreamable) through its proxy class.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldValue) || IId == typeof(IBoldStreamable))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldValue/IBoldStreamable");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ObserverMayModify(object? Observer) => CanModify() && (ModifiedValueHolder is null || ModifiedValueHolder == Observer);

    /// <summary>An immutable member list with just this member.</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList)
    {
        var NewList = new TBoldMemberList();
        NewList.fBoldMetaType = ((TBoldSystemTypeInfo)BoldType!.SystemTypeInfo!).ListTypeInfoByElement(BoldType);
        NewList.Add(this);
        NewList.MakeImmutable();
        NewList.MakeContentsImmutable();
        ResultList.SetOwnedValue(NewList);
    }

    [Delphi(PortStatus.Ported)]
    public override void GetAsValue(TBoldIndirectElement ResultElement) => ResultElement.SetOwnedValue(Clone());

    /// <summary>True, after taking a pessimistic lock, recording the old value for undo and running PrepareModify, when the member may change.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual bool StartModify()
    {
        bool result = CanModify();
        if (result && BoldSystem is not null && OwningObject is not null && BoldSystem.PessimisticLockHandler is not null && !BoldMemberRTInfo!.IsDerived)
        {
            result = BoldSystem.PessimisticLockHandler.LockElement(this);
        }

        if (result)
        {
            if (OwningObject is not null && !BoldSystem!.InTransaction() && StoreInUndo())
            {
                BoldSystem.UndoHandler.HandleMember(OwningObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents), BoldMemberRTInfo!.index,
                    AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
            }

            DoStartModify();
        }

        return result;
    }

    /// <summary>Marks a stored member modified, reverse derives a derived one, runs CompleteModify and adds the member to the transaction.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void EndModify()
    {
        if ((BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent || BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid) &&
            BoldMemberRTInfo is not null &&
            (BoldMemberRTInfo.IsStoredInObject ||
             (BoldMemberRTInfo.IsMultiRole && BoldSystem!.PersistenceController is not null && BoldSystem.PersistenceController.MultilinksAreStoredInObject())))
        {
            BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        }

        if (Derived && !BoldDeriver.bdsIsDeriving.Contains(DeriverState))
        {
            Deriver.ReverseDerive();
        }

        CompleteModify();
        SendEvent(beCompleteModify);
        if (OwningObject is not null)
        {
            BoldSystem!.AddToTransaction(OwningObject);
            BoldSystem.AddToTransaction(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void FailModify()
    {
    }

    [Delphi(PortStatus.Ported)]
    internal void EndUpdate(TBoldIDTranslationList? Translationlist)
    {
        AdjustOldValues(Translationlist);
        BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
    }

    [Delphi(PortStatus.Ported)]
    internal virtual void AdjustOldValues(TBoldIDTranslationList? Translationlist)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetBoldDirty() => BoldPersistenceState == TBoldValuePersistenceState.bvpsModified;

    [Delphi(PortStatus.Ported)] protected internal virtual bool MayUpdate() => true;
    [Delphi(PortStatus.Ported)] protected internal virtual bool MayModify() => true;

    [Delphi(PortStatus.Ported)]
    protected internal virtual void PrepareModify()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void CompleteModify()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void CompleteUpdate()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void PrepareUpdate()
    {
    }

    /// <summary>Saves the old value for optimistic locking, runs PrepareModify and sends bePrepareModify.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void DoStartModify()
    {
        OwningObject?.BoldSystem.fOldValueHandler.MemberValuePreChange(this);

        PrepareModify();
        SendEvent(bePrepareModify);
        if (BoldMemberRTInfo is { ToBeRemoved: true } && OwningObject is not null)
        {
            OwningObject.ToBeRemovedMemberModified(BoldMemberRTInfo);
        }
    }

    /// <summary>True when the member is mutable, not read only, derivable back, not history, and MayModify and the subscribers agree.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CanModify()
    {
        if (!Mutable)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sMemberIsImmutable, this));
            return false;
        }

        if (IsReadOnly)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sMemberIsreadOnly, this));
            return false;
        }

        if (Derived && !(BoldDeriver.bdsIsDeriving.Contains(DeriverState) || Deriver.CanReverseDerive))
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sMemberIsreadOnlyDerived, this));
            return false;
        }

        if (OwningObject is not null && OwningObject.BoldTime != BoldDefs.BOLDMAXTIMESTAMP)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sMemberIsHistory, this));
            return false;
        }

        return MayModify() && SendQuery(bqMayModify, [], null);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanUpdate() => InternalMayUpdate() && MayUpdate() && SendQuery(bqMayUpdate, [], null);

    /// <summary>Fetches or derives an invalid member, and marks the member and its object touched.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureContentsCurrent()
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid && !GetElementFlag(befEnsuringCurrent))
        {
            SetElementFlag(befEnsuringCurrent, true);
            try
            {
                if (Derived)
                {
                    if (DeriverState != TBoldDeriverState.bdsCurrent)
                    {
                        Deriver.EnsureCurrent();
                    }

                    if (!BoldDeriver.bdsIsDeriving.Contains(DeriverState))
                    {
                        BoldPersistenceState = TBoldValuePersistenceState.bvpsTransient;
                    }
                }
                else
                {
                    MakeDbCurrent();
                }
            }
            finally
            {
                SetElementFlag(befEnsuringCurrent, false);
            }
        }

        if (!GetElementFlag(befTouched))
        {
            SetElementFlag(befTouched, true);
            OwningObject?.SetElementFlagOf(befTouched, true);
        }
    }

    /// <summary>Drops a current or transient value (derived or persistent) so it is fetched or derived again.</summary>
    [Delphi(PortStatus.Ported)]
    public void Invalidate()
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
        {
            StateError("Invalidate");
        }

        if (BoldMemberRTInfo is { IsDerived: false } && BoldPersistenceState == TBoldValuePersistenceState.bvpsTransient)
        {
            throw new EBold(SysUtils.Format(sCannotInvalidateTransient, GetType().Name, DisplayName));
        }

        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent || BoldPersistenceState == TBoldValuePersistenceState.bvpsTransient)
        {
            BoldPersistenceState = TBoldValuePersistenceState.bvpsInvalid;
            if (HasDeriver)
            {
                Deriver.MarkOutOfdate();
            }

            FreeContent();
            SendEvent(beValueInvalid);
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool CanRead(TBoldSubscriber? Subscriber) => SendQuery(bqMayRead, [], Subscriber);

    [Delphi(PortStatus.Ported)]
    internal void InitializeStateToModified() =>
        SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsModified);

    [Delphi(PortStatus.Ported)]
    internal void InitializeStateToInvalid() =>
        SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsInvalid);

    [Delphi(PortStatus.Ported)]
    internal void InitializeStateToTransient() =>
        SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsTransient);

    [Delphi(PortStatus.Ported)]
    protected internal TBoldValuePersistenceState GetBoldPersistenceState() =>
        (TBoldValuePersistenceState)GetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift);

    /// <summary>Changes the persistence state, saving the old value, keeping a direct single link's old value, and telling the object.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetBoldPersistenceState(TBoldValuePersistenceState Value)
    {
        TBoldValuePersistenceState OldPState = BoldPersistenceState;
        if (Value != OldPState)
        {
            OwningObject?.BoldSystem.fOldValueHandler.MemberPersistenceStatePreChange(this, Value);
            SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)Value);
            if (OwnedByObject && BoldMemberRTInfo!.IsSingleRole && !Derived && this is TBoldObjectReference reference &&
                !reference.BoldRoleRTInfo!.IsIndirect && Value == TBoldValuePersistenceState.bvpsInvalid)
            {
                reference.HasOldValues = ((TBoldDirectSingleLinkController)reference.fObjectReferenceController!).GetLocator() is not null;
            }

            if (!Derived && OwningObject is not null)
            {
                if (Value == TBoldValuePersistenceState.bvpsModified)
                {
                    OwningObject.MemberBecomingModified(this);
                }
                else if (OldPState == TBoldValuePersistenceState.bvpsModified)
                {
                    OwningObject.MemberBecomingClean(this);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal virtual void MakeDbCurrent()
    {
    }

    /// <summary>The member's deriver, created on first use with the derive methods the object gives.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldEventPluggedDeriver GetDeriver()
    {
        var result = (TBoldMemberDeriver?)G_ExternalDerivers.ReferencedObjects(this);
        if (result is null)
        {
            result = new TBoldMemberDeriver(this);
            if (OwningObject is not null)
            {
                result.OnDeriveAndSubscribe = OwningObject.DeriveMethodForMember(this);
                result.OnReverseDerive = OwningObject.ReverseDeriveMethodForMember(this);
            }

            result.OnNotifyOutOfdate = _NotifyOutOfDate;
            G_ExternalDerivers.SetReferencedObjects(this, result);
            SetElementFlag(befHasDeriver, true);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void _NotifyOutOfDate() => Invalidate();

    /// <summary>Raises EBoldInternal with the member's name and persistence state.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void StateError(string S)
    {
        string MemberName = BoldMemberRTInfo is not null ? "(" + BoldMemberRTInfo.ExpressionName + ") " : "";
        base.StateError(SysUtils.Format("%s %s(PersistenceState: %s)", S, MemberName, BoldPersistenceState.ToString()));
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldEvaluator? GetEvaluator() => IsPartOfSystem ? BoldSystem!.Evaluator : base.GetEvaluator();

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() =>
        OwnedByObject ? ((TBoldMemberRTInfo)fBoldMetaType!).BoldType : (TBoldElementTypeInfo?)fBoldMetaType;

    /// <summary>A transient, free-standing copy of the member.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldMember Clone()
    {
        TBoldMember result = BoldType is not null
            ? TBoldMemberFactory.CreateMemberFromBoldType(BoldType)
            : CreateInstance(GetType(), m => m.InitializeMember(null, null));
        try
        {
            result.BoldPersistenceState = TBoldValuePersistenceState.bvpsTransient;
            result.Assign(this);
        }
        catch
        {
            result.Dispose();
            throw;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetIsPartOfSystem() => OwnedByObject || OwningElement is TBoldSystem;

    [Delphi(PortStatus.Ported)]
    private bool GetIsReadOnly(TBoldElementFlag Flag) =>
        GetElementFlag(Flag) || (BoldMemberRTInfo is { IsStoredInObject: true } && OwningObject!.IsReadOnly);

    /// <summary>Sets the owner, flags and type; the constructors call it (Delphi calls the inherited constructor from here).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        SetOwningElement(AOwningElement);

        if (OwnedByObject)
        {
            SetElementFlag(befDerived, BoldMemberRTInfo!.IsDerived);
            SetElementFlag(befPersistent, AOwningElement is not null && AOwningElement.BoldPersistent && BoldMemberRTInfo.Persistent);
            if (Derived)
            {
                DeriverState = TBoldDeriverState.bdsSubscriptionOutOfDate;
            }
        }
        else
        {
            SetElementFlag(befPersistent, false);
            fBoldMetaType = ElementTypeInfo ?? GetElementTypeInfoForType();
        }
    }

    /// <summary>Drops a modified value (to invalid, or current for an unsaved object) or resets a transient one to its initial value.</summary>
    [Delphi(PortStatus.Ported)]
    internal void InternalDiscard()
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsModified || BoldPersistenceState == TBoldValuePersistenceState.bvpsTransient)
        {
            PreDiscard();
            switch (BoldPersistenceState)
            {
                case TBoldValuePersistenceState.bvpsModified:
                    BoldPersistenceState = OwningObject!.BoldObjectLocator!.BoldObjectID.IsStorable
                        ? TBoldValuePersistenceState.bvpsInvalid
                        : TBoldValuePersistenceState.bvpsCurrent;
                    FreeContent();
                    SendEvent(beValueInvalid);
                    break;
                case TBoldValuePersistenceState.bvpsTransient:
                    if (!Derived)
                    {
                        DoSetInitialValue();
                    }

                    break;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Discard()
    {
        if (!OwningObject!.BoldObjectLocator!.BoldObjectID.IsStorable)
        {
            throw new EBold(SysUtils.Format(sCannotDiscardUnsavedSingleLinks, GetType().Name));
        }

        InternalDiscard();
    }

    [Delphi(PortStatus.Ported)]
    internal virtual void PreDiscard()
    {
    }

    [Delphi(PortStatus.Ported)]
    private IBoldValue GetAsIBoldValue(TBoldDomainElementProxyMode Mode)
    {
        ProxyInterface(typeof(IBoldValue), Mode, out object? result);
        return (IBoldValue)result!;
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual TBoldAbstractController? GetController() => null;

    [Delphi(PortStatus.Ported)]
    internal void MarkMemberDirty() => BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;

    [Delphi(PortStatus.Ported)]
    internal void ObjectBecomingPersistent()
    {
        if (BoldMemberRTInfo!.Persistent)
        {
            SetElementFlag(befPersistent, true);
            BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        }
    }

    [Delphi(PortStatus.Ported)]
    internal virtual bool InternalMayUpdate() => true;

    /// <summary>Saves the value in the system's rollback area before a change.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void PreChange()
    {
        if (OwningObject is not null && !BoldMemberRTInfo!.IsDerived)
        {
            OwningObject.BoldSystem.CopyMemberToRollBackBuffer(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal virtual void DoSetInitialValue()
    {
    }

    /// <summary>The same member of the object at another time.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual TBoldMember AtTime(TBoldTimeStampType Time) =>
        OwningObject is not null ? OwningObject.AtTime(Time)!.BoldMembers(BoldMemberRTInfo!.index) : this;

    [Delphi(PortStatus.Ported)]
    protected internal virtual void FreeContent()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldElement? CloneIfPossible() => Clone();

    /// <summary>Creates the member's proxy for the mode and gives it as the interface; raises when the proxy class lacks it.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool RetrieveProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj, string InterfaceName)
    {
        TBoldMember_ProxyClass proxyClass = ProxyClass();
        object proxy = Activator.CreateInstance(proxyClass, this, Mode)!;
        if (!IId.IsInstanceOfType(proxy))
        {
            throw new EBoldInternal(SysUtils.Format(sProxyClassDidNotImplementInterface, proxyClass.Name, GetType().Name, InterfaceName));
        }

        Obj = proxy;
        return true;
    }

    /// <summary>The proxy class that gives this member as IBoldValue.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal abstract TBoldMember_ProxyClass ProxyClass();

    /// <summary>Sets the content (not through the May... checks) from a member's value; derived members get their value this way.</summary>
    [Delphi(PortStatus.Ported)]
    internal virtual void AssignContentValueFromElement(TBoldElement? source)
    {
        if (source is null)
        {
            return;
        }

        if (source is TBoldMember member)
        {
            AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(member.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValuefromElement"));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldDeriverState GetDeriverState() => (TBoldDeriverState)GetInternalState(unchecked((uint)BoldDerivationStateMask), BoldDSShift);

    [Delphi(PortStatus.Ported)]
    private void SetDeriverState(TBoldDeriverState value) => SetInternalState(unchecked((uint)BoldDerivationStateMask), BoldDSShift, (uint)value);

    /// <summary>The value the member had in the database, kept for optimistic locking, if any.</summary>
    [Delphi(PortStatus.Ported)]
    private IBoldValue? GetOldvalue()
    {
        if (OwningObject is not null && BoldSystem is not null)
        {
            IBoldObjectContents? ObjectContents = BoldSystem.fOldValueHandler.OldValues.GetObjectContentsByObjectId(OwningObject.BoldObjectLocator!.BoldObjectID);
            return ObjectContents?.ValueByIndex(BoldMemberRTInfo!.index);
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public bool MemberHasSubscribers() => HasSubscribers;

    /// <summary>True for the members undo records: non-derived attributes, ordinary direct single links and inner link roles.</summary>
    [Delphi(PortStatus.Ported)]
    public bool StoreInUndo()
    {
        if (BoldMemberRTInfo is null || BoldMemberRTInfo.IsDerived)
        {
            return false;
        }

        if (BoldMemberRTInfo.IsAttribute)
        {
            return true;
        }

        var role = (TBoldRoleRTInfo)BoldMemberRTInfo;
        return (role.RoleType == TBoldRoleType.rtRole && !(role.IsMultiRole || role.IsIndirect)) || role.RoleType == TBoldRoleType.rtInnerLinkRole;
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool IsEqualToValue(IBoldValue Value) => throw new EBold(sIsEqualToValueNotImplemented);

    [Delphi(PortStatus.Ported)]
    public void Refetch() => MakeDbCurrent();

    [Delphi(PortStatus.Ported)]
    internal virtual TBoldElementTypeInfo? GetElementTypeInfoForType() => null;

    [Delphi(PortStatus.Ported)]
    internal TBoldSystem? FindASystem() => IsPartOfSystem ? BoldSystem : TBoldSystem.DefaultSystem();

    /// <summary>Sends the event, unless the member is invalid.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void Changed(TBoldEvent Event, object?[] Args)
    {
        if (!IsInvalid())
        {
            SendExtendedEvent(Event, Args);
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool IsInvalid() => BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid;

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() =>
        BoldMemberRTInfo is not null ? BoldMemberRTInfo.ClassTypeInfo.ExpressionName + "." + BoldMemberRTInfo.ExpressionName : GetType().Name;

    [Delphi(PortStatus.Ported)]
    internal bool HasDeriver
    {
        get => GetElementFlag(befHasDeriver);
        set => SetElementFlag(befHasDeriver, value);
    }

    [Delphi(PortStatus.Ported)]
    internal bool OwnedByObject
    {
        get => GetElementFlag(befOwnedByObject);
        set => SetElementFlag(befOwnedByObject, value);
    }

    [Delphi(PortStatus.Ported)]
    internal bool IsReadOnly
    {
        get => GetIsReadOnly(befMemberReadOnly);
        set => SetElementFlag(befMemberReadOnly, value);
    }

    [Delphi(PortStatus.Ported)]
    internal TBoldDeriverState DeriverState
    {
        get => GetDeriverState();
        set => SetDeriverState(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfo? BoldMemberRTInfo => GetBoldMemberRTInfo();
    [Delphi(PortStatus.Ported)] public TBoldSystem? BoldSystem => GetBoldSystem();

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceState BoldPersistenceState
    {
        get => GetBoldPersistenceState();
        set => SetBoldPersistenceState(value);
    }

    [Delphi(PortStatus.Ported)] public bool Derived => GetElementFlag(befDerived);
    [Delphi(PortStatus.Ported)] public TBoldEventPluggedDeriver Deriver => GetDeriver();
    [Delphi(PortStatus.Ported)] public bool IsPartOfSystem => GetIsPartOfSystem();
    [Delphi(PortStatus.Ported)] public TBoldObject? OwningObject => GetOwningObject();
    [Delphi(PortStatus.Ported)] public IBoldValue AsIBoldValue(TBoldDomainElementProxyMode Mode) => GetAsIBoldValue(Mode);
    [Delphi(PortStatus.Ported)] public IBoldValue? OldValue => GetOldvalue();
    [Delphi(PortStatus.Ported)] public bool Touched => GetElementFlag(befTouched);
}

/// <summary>Gives a member as IBoldValue: the base of each member class's proxy.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMember_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldDomainElement_Proxy(aProxedElement, aMode), IBoldValue, IBoldStreamable
{
    [Delphi(PortStatus.Ported)]
    private TBoldMember GetProxedMember() => (TBoldMember)ProxedElement;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractController? GetProxedController() => ProxedMember.GetController();

    [Delphi(PortStatus.Ported)] protected TBoldMember ProxedMember => GetProxedMember();
    [Delphi(PortStatus.Ported)] protected TBoldAbstractController? ProxedController => GetProxedController();

    [Delphi(PortStatus.Ported)]
    public string GetContentName() => ProxedMember.GetStreamName();

    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => ProxedMember.GetStreamName();

    /// <summary>Sets the content and takes the source's persistence state.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssignContent(IBoldValue Source)
    {
        AssignContentValue(Source);
        ProxedMember.SetBoldPersistenceState(Source.BoldPersistenceState);
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void AssignContentValue(IBoldValue Source);

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceState GetBoldPersistenceState() => ProxedMember.BoldPersistenceState;

    [Delphi(PortStatus.Ported)]
    public void SetBoldPersistenceState(TBoldValuePersistenceState Value) => ProxedMember.SetBoldPersistenceState(Value);
}

/// <summary>Creates free-standing members from types.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberFactory : TBoldMemoryManagedObject
{
    /// <summary>A member of the class the type stands for: its list class, attribute class, or a reference for a class type.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldMember CreateMemberFromBoldType(TBoldElementTypeInfo? BoldType)
    {
        TBoldMemberClass? MemberClass = BoldType switch
        {
            TBoldListTypeInfo list => list.ListClass,
            TBoldAttributeTypeInfo attribute => attribute.AttributeClass,
            TBoldClassTypeInfo => typeof(TBoldObjectReference),
            null => throw new EBold(SysUtils.Format(sInvalidBoldType_Nil, nameof(TBoldMemberFactory))),
            _ => throw new EBold(SysUtils.Format(sInvalidBoldType, nameof(TBoldMemberFactory), BoldType.GetType().Name)),
        };

        if (MemberClass is null)
        {
            throw new EBold(SysUtils.Format(sDelphiClassNotInstalled, nameof(TBoldMemberFactory), BoldType.ExpressionName, BoldType.DelphiName));
        }

        return TBoldMember.CreateWithTypeInfo(MemberClass, BoldType);
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldMember CreateMemberFromExpressionName(TBoldSystemTypeInfo SystemTypeInfo, string Name)
    {
        TBoldElementTypeInfo BoldType = SystemTypeInfo.ElementTypeInfoByExpressionName(Name) ??
            throw new EBold(SysUtils.Format(sUnableToFindType, nameof(TBoldMemberFactory), Name));
        return CreateMemberFromBoldType(BoldType);
    }
}

/// <summary>An attribute: a member with a value that can be null.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAttribute : TBoldMember
{
    protected TBoldAttribute()
    {
    }

    protected TBoldAttribute(Action<TBoldMember> Construction)
        : base(Construction)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAttributeRTInfo? GetBoldAttributeRTInfo() => (TBoldAttributeRTInfo?)BoldMemberRTInfo;

    /// <summary>The attribute type with this class's Delphi name in the system the attribute belongs to (or the default system).</summary>
    [Delphi(PortStatus.Ported)]
    internal override TBoldElementTypeInfo? GetElementTypeInfoForType() => FindASystem()?.BoldSystemTypeInfo.AttributeTypeInfoByDelphiName(GetType().Name);

    [Delphi(PortStatus.Ported)]
    private bool GetIsNull()
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetIsNull", "");
        }

        return ContentIsNull;
    }

    [Delphi(PortStatus.Ported)]
    internal override void MakeDbCurrent()
    {
        if (!OwningObject!.BoldObjectLocator!.BoldObjectID.IsStorable)
        {
            throw new EBoldInternal(SysUtils.Format(sObjectIDIsInternal, BoldType!.AsString));
        }

        OwningObject.BoldSystem.SystemPersistenceHandler.FetchMember(this);
    }

    [Delphi(PortStatus.Ported)]
    private void NullFailure()
    {
        if (GetBoldLastFailureReason() is null)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sNullValueNotAllowed, this));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal abstract void AssignValue(IBoldValue Source);

    [Delphi(PortStatus.Ported)]
    protected internal abstract void AssignContentValue(IBoldValue Source);

    /// <summary>Sets the initial value of the model: null for "&lt;NULL&gt;", else the value as a string.</summary>
    [Delphi(PortStatus.Ported)]
    internal override void DoSetInitialValue()
    {
        if (BoldAttributeRTInfo is { HasInitialValue: true })
        {
            string InitialValue = BoldAttributeRTInfo.InitialValue;
            if (SysUtils.CompareText(InitialValue, "<NULL>") == 0)
            {
                SetToNull();
            }
            else
            {
                try
                {
                    AsString = InitialValue;
                }
                catch (Exception e)
                {
                    throw new EBold(SysUtils.Format(sIllegalInitialValue, InitialValue, BoldAttributeRTInfo.AsString, BoldDefs.BOLDCRLF, e.Message), e);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal bool GetContentIsNull() => GetElementFlag(befIsNull);

    [Delphi(PortStatus.Ported)]
    protected static bool EitherIsNull(TBoldAttribute Attribute1, TBoldAttribute Attribute2) => Attribute1.IsNull || Attribute2.IsNull;

    [Delphi(PortStatus.Ported)]
    protected internal void EnsureNotNull()
    {
        if (IsNull)
        {
            throw new EBoldAccessNullValue(SysUtils.Format(sNullValueAccess, DisplayName));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void FormatFailure(string value, string ExpectedDataType) =>
        SetBoldLastFailureReason(new TBoldFailureReason(sFailure_Invalid, [value, ExpectedDataType], this));

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldMemberRTInfo!.StreamName;

    [Delphi(PortStatus.Ported)]
    protected int NullBiggest(TBoldElement? BoldElement) => -1 * NullSmallest(BoldElement);

    /// <summary>Compares by nullness only: equal when both or neither are null, else null first.</summary>
    [Delphi(PortStatus.Ported)]
    protected int NullSmallest(TBoldElement? BoldElement)
    {
        if (BoldElement is TBoldAttribute other)
        {
            if (!(IsNull ^ other.IsNull))
            {
                return 0;
            }

            return IsNull ? -1 : 1;
        }

        CompareError(BoldElement);
        return 0;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void SetContentToNull()
    {
        if (!ContentIsNull)
        {
            SetElementFlag(befIsNull, true);
            Changed(beValueChanged, []);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void SetToNonNull() => SetElementFlag(befIsNull, false);

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        CompareTypeError(CompType, BoldElement!);
        return 0;
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
    public bool CanSetToNull(TBoldSubscriber? Subscriber)
    {
        bool result = (BoldAttributeRTInfo is null || BoldAttributeRTInfo.AllowNull) && SendQuery(bqMaySetToNull, [], Subscriber);
        if (!result)
        {
            NullFailure();
        }

        return result;
    }

    /// <summary>True when the value equals this attribute's (compared through a member of the same type).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool IsEqualToValue(IBoldValue Value)
    {
        TBoldMember MemberOfSameType = BoldType is not null
            ? TBoldMemberFactory.CreateMemberFromBoldType(BoldType)
            : CreateInstance(GetType(), m => m.InitializeMember(null, null));
        try
        {
            MemberOfSameType.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(Value);
            MemberOfSameType.BoldPersistenceState = TBoldValuePersistenceState.bvpsTransient;
            return IsEqual(MemberOfSameType);
        }
        finally
        {
            MemberOfSameType.Dispose();
        }
    }

    /// <summary>Makes a free-standing attribute mutable again, to be reused.</summary>
    [Delphi(PortStatus.Ported)]
    public void RecycleValue()
    {
        if (OwningObject is not null)
        {
            throw new EBold(sCannotRecyclePartOfObject);
        }

        PrepareToDestroy();
        SetElementFlag(befImmutable, false);
    }

    /// <summary>Sets the attribute to null (when allowed and it is not null already).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void SetToNull()
    {
        BoldClearLastFailure();
        if (!CanSetToNull(null))
        {
            BoldRaiseLastFailure(this, "SetToNull", "");
        }

        if (!IsNull)
        {
            if (!StartModify())
            {
                BoldRaiseLastFailure(this, "SetToNull", sStartModifyPreconditionNotMet);
            }

            try
            {
                SetContentToNull();
                EndModify();
            }
            catch
            {
                FailModify();
                throw;
            }
        }
    }

    /// <summary>Nil sets null (or the empty value when null is not allowed).</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is null)
        {
            if (BoldAttributeRTInfo is null || BoldAttributeRTInfo.AllowNull)
            {
                SetToNull();
            }
            else
            {
                SetEmptyValue();
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldNullableValue))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldNullableValue");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate) =>
        DefaultSubscribe(Subscriber, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public virtual void SetEmptyValue() => throw new EBold(SysUtils.Format(sMethodNotImplemented, GetType().Name, "SetEmptyValue"));

    [Delphi(PortStatus.Ported)] protected bool ContentIsNull => GetContentIsNull();
    [Delphi(PortStatus.Ported)] public TBoldAttributeRTInfo? BoldAttributeRTInfo => GetBoldAttributeRTInfo();
    [Delphi(PortStatus.Ported)] public bool IsNull => GetIsNull();
}

/// <summary>Gives an attribute as IBoldNullableValue.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAttribute_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldNullableValue
{
    [Delphi(PortStatus.Ported)]
    private TBoldAttribute GetProxedAttribute() => (TBoldAttribute)ProxedElement;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source) => ProxedAttribute.AssignContentValue(Source);

    [Delphi(PortStatus.Ported)]
    public void SetContentToNull() => ProxedAttribute.SetContentToNull();

    [Delphi(PortStatus.Ported)]
    public bool GetContentIsNull() => ProxedAttribute.GetContentIsNull();

    [Delphi(PortStatus.Ported, Note = "Delphi implements IBoldNullableValue by delegation to this property; here the proxy implements it and forwards.")]
    protected TBoldAttribute ProxedAttribute => GetProxedAttribute();
}

/// <summary>Why a May... query or operation failed; BoldRaiseLastFailure puts it in the exception.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFailureReason : IDisposable
{
    private string fMessageFormatStr = "";
    private TBoldDomainElement? fOriginator;
    private readonly string fReason;
    private TBoldPassthroughSubscriber? fSubscriber;

    /// <summary>A reason, forgetting the originator when it is destroyed.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldFailureReason.create")]
    public TBoldFailureReason(string reason, TBoldDomainElement? Originator)
    {
        fReason = reason;
        fOriginator = Originator;
        if (fOriginator is not null)
        {
            fSubscriber = new TBoldPassthroughSubscriber(ReceiveOriginatorDestroy);
            fOriginator.AddSmallSubscription(fSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFailureReason.CreateFmt")]
    public TBoldFailureReason(string Reason, object?[] args, TBoldDomainElement? Originator)
        : this(SysUtils.Format(Reason, args), Originator)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFailureReason.Destroy")]
    public virtual void Dispose()
    {
        fSubscriber?.Dispose();
        fSubscriber = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private void ReceiveOriginatorDestroy(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (OriginalEvent == beDestroying && Originator == this.Originator)
        {
            fOriginator = null;
        }
    }

    /// <summary>The exception to raise with the message; subclasses give their own exception class.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual EBoldFailure GetException(string Msg) => new(Msg);

    [Delphi(PortStatus.Ported)]
    public string MessageFormatStr
    {
        get => fMessageFormatStr;
        set => fMessageFormatStr = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldDomainElement? Originator => fOriginator;
    [Delphi(PortStatus.Ported)] public string Reason => fReason;
}

/// <summary>Raised when an operation is not allowed; carries the failure reason.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldFailure(string? message = null, Exception? innerException = null) : EBold(message, innerException)
{
    [Delphi(PortStatus.Replaced, Symbol = "EBoldFailure.Destroy", Note = "Freed the reason; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public TBoldFailureReason? ReasonObject { get; set; }
}

/// <summary>Raised when an operation failed for some objects; holds them in a list.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldOperationFailedForObjectList : EBold
{
    private readonly TBoldObjectList FObjectList;

    [Delphi(PortStatus.Ported)]
    public EBoldOperationFailedForObjectList(string msg, object?[] args, TBoldObjectIdList IdList, TBoldSystem System)
        : base(SysUtils.Format(msg, args))
    {
        TBoldClassTypeInfo RootClassTypeInfo = System.BoldSystemTypeInfo.TopSortedClasses[0];
        FObjectList = (TBoldObjectList)TBoldMemberFactory.CreateMemberFromBoldType(RootClassTypeInfo.ListTypeInfo);
        FObjectList.FillFromIDList(IdList, System);
    }

    [Delphi(PortStatus.Replaced, Symbol = "EBoldOperationFailedForObjectList.Destroy", Note = "Freed the list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public TBoldObjectList ObjectList => FObjectList;
}

/// <summary>The deriver of a derived member; keeps its state in the member's flags.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldMemberDeriver(TBoldMember DerivedMember) : TBoldEventPluggedDeriver(DerivedMember)
{
    [Delphi(PortStatus.Ported)]
    protected override void SetInternalDeriverState(TBoldDeriverState Value) => ((TBoldMember)DerivedObject!).DeriverState = Value;

    [Delphi(PortStatus.Ported)]
    protected override TBoldDeriverState GetInternalDeriverState() => ((TBoldMember)DerivedObject!).DeriverState;
}
