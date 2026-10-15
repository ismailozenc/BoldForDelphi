// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// TBoldObject.

using System.Reflection;
using System.Runtime.ExceptionServices;
using static Bold.BoldCoreConsts;
using static Bold.BoldElements;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>
/// An object of the model: its class, existence and persistence state, and its members, created on first access. Lives
/// in one system and is found through its locator.
/// </summary>
/// <remarks>
/// Delphi creates objects of a class only known at run time through class references (TBoldObjectClass). Here that goes
/// through <see cref="CreateInstance"/>: subclasses (generated code) must have a constructor taking
/// (TBoldDomainElement? AOwningElement, bool Persistent) that calls this class's constructor.
/// </remarks>
[Delphi(PortStatus.Ported)]
public class TBoldObject : TBoldDomainElement
{
    /// <summary>The construction <see cref="CreateInstance"/> asks the next TBoldObject constructor on this thread to run.</summary>
    [ThreadStatic] private static Action<TBoldObject>? G_PendingConstruction;

    private TBoldClassTypeInfo fBoldClassTypeInfo = null!;

    /// <summary>The members, by index; Delphi keeps them (and the timestamp) in one block of memory.</summary>
    private TBoldMember?[]? fDynamicData;

    private TBoldTimeStampType fTimeStamp = -1;
    internal TBoldObjectLocator? FBoldObjectLocator;

    /// <summary>
    /// A new object of the class of this instance in the system (the default system when nil); when the system uses
    /// generated code, that class must be the generated class of a model class.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObject.Create")]
    public TBoldObject(TBoldDomainElement? AOwningElement, bool Persistent = true)
        : base(null)
    {
        Action<TBoldObject>? pending = G_PendingConstruction;
        G_PendingConstruction = null;
        if (pending is not null)
        {
            pending(this);
            return;
        }

        SetInternalState(unchecked((uint)BoldExistenceStateMask), BoldESShift, (uint)TBoldExistenceState.besNotCreated);
        SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsCurrent);
        TBoldSystem System;
        if (AOwningElement is null)
        {
            System = TBoldSystem.DefaultSystem() ?? throw new EBold(SysUtils.Format(sNoDefaultSystem, GetType().Name));
        }
        else if (AOwningElement is TBoldSystem s)
        {
            System = s;
        }
        else
        {
            throw new EBold(SysUtils.Format(sOwningElementMustBeSystem, GetType().Name));
        }

        TBoldClassTypeInfo? aClass = System.BoldSystemTypeInfo.TopSortedClasses.ItemsByObjectClass(GetType());
        if (aClass is null)
        {
            if (!System.BoldSystemTypeInfo.UseGeneratedCode)
            {
                throw new EBold(SysUtils.Format(sGeneratedCodeNotUsed + BoldDefs.BOLDCRLF + sGeneratedCode_HowToFix, GetType().Name));
            }

            throw new EBold(SysUtils.Format(sNoClassInformation, GetType().Name, GetType().Name));
        }

        if (aClass.IsLinkClass)
        {
            throw new EBold(SysUtils.Format(sCannotCreateAssociationClass, GetType().Name));
        }

        DoInternalCreateNewWithClassAndSystem(aClass, System, Persistent);
    }

    /// <summary>
    /// Creates an object of <paramref name="ObjectClass"/> (TBoldObject or a subclass) whose constructor runs
    /// <paramref name="Construction"/> instead of its usual body; this is how Delphi's constructors called through a
    /// class reference are done here.
    /// </summary>
    protected internal static TBoldObject CreateInstance(TBoldObjectClass ObjectClass, Action<TBoldObject> Construction)
    {
        ConstructorInfo ctor = ObjectClass.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, [typeof(TBoldDomainElement), typeof(bool)]) ??
            throw new EBoldInternal(SysUtils.Format("%s has no constructor (TBoldDomainElement, Boolean)", ObjectClass.Name));
        G_PendingConstruction = Construction;
        try
        {
            return (TBoldObject)ctor.Invoke([null, true]);
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

    /// <summary>An object of the class for the locator of an existing object, to be filled with its fetched values.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldObject InternalCreateWithClassAndLocator(TBoldObjectClass ObjectClass, TBoldClassTypeInfo ClassTypeInfo, TBoldObjectLocator Locator) =>
        CreateInstance(ObjectClass, o =>
        {
            o.SetInternalState(unchecked((uint)BoldExistenceStateMask), BoldESShift, (uint)TBoldExistenceState.besExisting);
            o.SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsInvalid);
            o.SetOwningElement(Locator.BoldSystem);
            o.InitializeObject(Locator.BoldSystem, ClassTypeInfo, Locator, true);
            o.EndReCreate();
        });

    /// <summary>A new object of the class in the system (used by links to create link objects).</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldObject InternalCreateNewWithClassAndSystem(TBoldObjectClass ObjectClass, TBoldClassTypeInfo ClassTypeInfo, TBoldSystem aSystem, bool Persistent) =>
        CreateInstance(ObjectClass, o => o.DoInternalCreateNewWithClassAndSystem(ClassTypeInfo, aSystem, Persistent));

    /// <summary>The body of Delphi's InternalCreateNewWithClassAndSystem; on failure it rolls back and frees the object, as Delphi does.</summary>
    private void DoInternalCreateNewWithClassAndSystem(TBoldClassTypeInfo ClassTypeInfo, TBoldSystem aSystem, bool Persistent)
    {
        if (!aSystem.CanCreateObject(ClassTypeInfo))
        {
            BoldRaiseLastFailure(aSystem, "InternalCreateNewWithClassAndSystem", SysUtils.Format(sCannotCreateOfType, ClassTypeInfo.ExpressionName));
        }

        aSystem.StartTransaction(TBoldSystemTransactionMode.stmNormal);
        try
        {
            SetInternalState(unchecked((uint)BoldExistenceStateMask), BoldESShift, (uint)TBoldExistenceState.besNotCreated);
            SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)TBoldValuePersistenceState.bvpsCurrent);
            SetOwningElement(aSystem);
            InitializeObject(aSystem, ClassTypeInfo, null, Persistent);
            EndCreate();
            aSystem.CommitTransaction(TBoldSystemTransactionMode.stmNormal);
        }
        catch
        {
            aSystem.RollbackTransaction(TBoldSystemTransactionMode.stmNormal);
            Dispose();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldDeriveAndResubscribe? GetDeriveMethodForMember(TBoldMember Member)
    {
        if (Member.BoldMemberRTInfo!.IsDerived && Member.BoldMemberRTInfo.DeriveExpression != "")
        {
            return Member.CalculateDerivedMemberWithExpression;
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldReverseDerive? GetReverseDeriveMethodForMember(TBoldMember Member) => null;

    internal TBoldDeriveAndResubscribe? DeriveMethodForMember(TBoldMember Member) => GetDeriveMethodForMember(Member);

    internal TBoldReverseDerive? ReverseDeriveMethodForMember(TBoldMember Member) => GetReverseDeriveMethodForMember(Member);

    /// <summary>The member, created and initialized on first access (non-versioned members of old versions come from the latest version).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldMember GetBoldMembers(int index)
    {
        if (index < 0 || index >= BoldMemberCount)
        {
            throw new EBold(SysUtils.Format(sIndexOutOfRange, GetType().Name, "GetBoldMembers", index, BoldMemberCount));
        }

        TBoldMember? result = fDynamicData![index];
        if (result is null)
        {
            if (BoldObjectLocator!.BoldObjectID.TimeStamp != BoldDefs.BOLDMAXTIMESTAMP &&
                BoldClassTypeInfo.AllMembers[index].IsNonVersionedInVersionedClass)
            {
                result = AtTime(BoldDefs.BOLDMAXTIMESTAMP)!.BoldMembers(index);
            }
            else
            {
                result = CreateMemberByIndex(index);
                fDynamicData[index] = result;
                InitializeMember(result, BoldClassTypeInfo.AllMembers[index], GetElementFlag(befObjectWasCreatedNew));
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetBoldMemberAssigned(int Index)
    {
        if (Index < 0 || Index >= BoldMemberCount)
        {
            throw new EBold(SysUtils.Format(sIndexOutOfRange, GetType().Name, "GetBoldMemberAssigned", Index, BoldMemberCount));
        }

        return fDynamicData![Index] is not null;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetBoldObjectIsDeleted() => BoldExistenceState == TBoldExistenceState.besDeleted;

    /// <summary>Created and not yet saved, or transient.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetBoldObjectIsNew() =>
        (BoldExistenceState == TBoldExistenceState.besExisting && BoldPersistenceState == TBoldValuePersistenceState.bvpsModified) ||
        BoldPersistenceState == TBoldValuePersistenceState.bvpsTransient;

    [Delphi(PortStatus.Ported)]
    private int GetBoldMemberCount() => BoldClassTypeInfo.AllMembersCount;

    /// <summary>The class's default string representation (OCL), else the attribute at that index, else "id:class".</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        if (BoldClassTypeInfo.DefaultStringRepresentation != "")
        {
            return EvaluateExpressionAsString(BoldClassTypeInfo.DefaultStringRepresentation, BoldDefs.brDefault);
        }

        if (Representation < BoldMemberCount && BoldMembers(Representation) is TBoldAttribute)
        {
            return BoldMembers(Representation).AsString;
        }

        return SysUtils.Format("%s:%s", BoldObjectLocator!.AsString, BoldClassTypeInfo.ModelName);
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (BoldClassTypeInfo.DefaultStringRepresentation != "")
        {
            SubscribeToExpression(BoldClassTypeInfo.DefaultStringRepresentation, Subscriber, false);
        }
        else if (Representation < BoldMemberCount && BoldMembers(Representation) is TBoldAttribute attribute)
        {
            attribute.DefaultSubscribe(Subscriber, RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystem GetBoldSystem() => (TBoldSystem)OwningElement!;

    [Delphi(PortStatus.Ported)]
    private IBoldObjectContents GetAsIBoldObjectContents(TBoldDomainElementProxyMode Mode)
    {
        ProxyInterface(typeof(IBoldObjectContents), Mode, out object? result);
        return (IBoldObjectContents)result!;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMember CreateMemberByIndex(int Index)
    {
        TBoldMemberRTInfo MemberRTInfo = BoldClassTypeInfo.AllMembers[Index];
        return TBoldMember.InternalCreate(MemberRTInfo.MemberClass!, this, MemberRTInfo,
            GetElementFlag(befObjectWasCreatedNew) && !MemberRTInfo.IsDerived);
    }

    /// <summary>Sets the class and flags, allocates the members and connects to the locator (a new one for a new object).</summary>
    [Delphi(PortStatus.Ported)]
    private void InitializeObject(TBoldSystem System, TBoldClassTypeInfo ClassTypeInfo, TBoldObjectLocator? Locator, bool Persistent)
    {
        if (ClassTypeInfo.IsAbstract)
        {
            throw new EBold(SysUtils.Format(sCannotInstansiateAbstractClass, ClassTypeInfo.ExpressionName));
        }

        if (ClassTypeInfo.IsImported)
        {
            throw new EBold(SysUtils.Format(sCannotInstansiateImportedClass, ClassTypeInfo.ExpressionName));
        }

        fBoldClassTypeInfo = ClassTypeInfo;
        SetElementFlag(befObjectWasCreatedNew, Locator is null);
        SetElementFlag(befPersistent, System.BoldPersistent && ClassTypeInfo.Persistent && Persistent);
        SetElementFlag(befStoresTimeStamp, BoldClassTypeInfo.OptimisticLocking == TBoldOptimisticLockingMode.bolmTimeStamp);

        fDynamicData = new TBoldMember?[GetDynamicDataSize()];
        fTimeStamp = -1;

        if (Locator is not null)
        {
            FBoldObjectLocator = Locator;
            Locator.fBoldObject = this;
            Locator.EmbeddedSingleLinksToObject();
        }
        else
        {
            FBoldObjectLocator = new TBoldObjectLocator(System, ClassTypeInfo.TopSortedIndex, true);
            FBoldObjectLocator.fBoldObject = this;
        }

        if (ClassTypeInfo.ToBeRemoved)
        {
            ToBeRemovedClassAccessed();
        }
    }

    /// <summary>Sets the member's starting state, its initial value (new objects) and its deriver.</summary>
    [Delphi(PortStatus.Ported)]
    private void InitializeMember(TBoldMember Member, TBoldMemberRTInfo MemberRTInfo, bool IsNewObject)
    {
        if (IsNewObject)
        {
            if (Member.BoldPersistent)
            {
                Member.InitializeStateToModified();
            }
            else if (Member.Derived)
            {
                Member.InitializeStateToInvalid();
            }
            else
            {
                Member.InitializeStateToTransient();
            }
        }
        else
        {
            if (Member.BoldPersistent || Member.Derived ||
                (MemberRTInfo is TBoldRoleRTInfo role && (role.RoleType == TBoldRoleType.rtInnerLinkRole || role.RoleType == TBoldRoleType.rtLinkRole)))
            {
                Member.InitializeStateToInvalid();
            }
            else
            {
                Member.InitializeStateToTransient();
            }
        }

        if (IsNewObject && !MemberRTInfo.IsDerived)
        {
            Member.DoSetInitialValue();
        }

        if (MemberRTInfo.IsDerived && MemberRTInfo.DeriveExpression != "")
        {
            Member.Deriver.OnDeriveAndSubscribe = Member.CalculateDerivedMemberWithExpression;
        }

        if (MemberRTInfo.ToBeRemoved && !IsNewObject)
        {
            ToBeRemovedMemberAccessed(MemberRTInfo);
        }
    }

    /// <summary>Sends beMemberChanged for value changes of an existing object, and adds it to the system's NewModifiedList.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void ReceiveEventFromOwned(object? Originator, TBoldEvent OriginalEvent)
    {
        bool isValueEvent = beValueEvents.Contains(OriginalEvent);
        if (BoldExistenceState == TBoldExistenceState.besExisting && isValueEvent)
        {
            SendEvent(beMemberChanged);
        }

        if (isValueEvent && BoldSystem.NewModifiedList is not null && !(Originator is TBoldMember m && m.Derived))
        {
            BoldSystem.NewModifiedList.Add(this);
        }
    }

    /// <summary>
    /// Frees the members. An object that was never created removes its locator; an existing one must have been unloaded
    /// through its locator first.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObject.Destroy")]
    public override void Dispose()
    {
        if (BoldExistenceState == TBoldExistenceState.besNotCreated)
        {
            if (OwningElement is not null)
            {
                if (BoldObjectLocator is not null)
                {
                    TBoldObjectLocator aLocator = BoldObjectLocator;
                    aLocator.fBoldObject = null;
                    FBoldObjectLocator = null;
                    BoldSystem.Locators.Remove(aLocator);
                }

                BoldSystem.MarkObjectClean(this);
            }
        }
        else if (BoldObjectLocator is not null)
        {
            throw new EBold(SysUtils.Format(sIllegalDirectDestruction, GetType().Name));
        }

        PrepareToDestroy();

        if (fDynamicData is not null)
        {
            for (int i = 0; i < BoldMemberCount; i++)
            {
                fDynamicData[i]?.Dispose();
            }

            fDynamicData = null;
        }

        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
    }

    /// <summary>Clears the object's ordinary roles and invalidates its derived ones.</summary>
    [Delphi(PortStatus.Ported)]
    public void UnLinkAll()
    {
        for (int M = 0; M < BoldClassTypeInfo.AllMembers.Count; M++)
        {
            TBoldMemberRTInfo MemberRTInfo = BoldClassTypeInfo.AllMembers[M];
            if (MemberRTInfo.IsRole && MemberRTInfo.IsDerived)
            {
                if (GetBoldMemberAssigned(M))
                {
                    BoldMembers(M).Invalidate();
                }
            }
            else if (MemberRTInfo.IsMultiRole)
            {
                if (((TBoldRoleRTInfo)MemberRTInfo).RoleType == TBoldRoleType.rtRole && ((TBoldObjectList)BoldMembers(M)).Count != 0)
                {
                    ((TBoldObjectList)BoldMembers(M)).Clear();
                }
            }
            else if (MemberRTInfo.IsSingleRole)
            {
                if (((TBoldRoleRTInfo)MemberRTInfo).RoleType == TBoldRoleType.rtRole && ((TBoldObjectReference)BoldMembers(M)).Locator is not null)
                {
                    ((TBoldObjectReference)BoldMembers(M)).Clear();
                }
            }
        }
    }

    /// <summary>Makes the object dirty by marking one of its current, stored members dirty.</summary>
    [Delphi(PortStatus.Ported)]
    public void MarkObjectDirty()
    {
        if (BoldDirty)
        {
            return;
        }

        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (GetBoldMemberAssigned(i) && BoldMembers(i).BoldMemberRTInfo!.IsStoredInObject &&
                BoldMembers(i).BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)
            {
                BoldMembers(i).MarkMemberDirty();
                return;
            }
        }

        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (BoldMembers(i).BoldMemberRTInfo!.IsStoredInObject && BoldMembers(i).BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)
            {
                BoldMembers(i).MarkMemberDirty();
                return;
            }
        }

        throw new EBold(SysUtils.Format(sNoPersistentMembers, GetType().Name));
    }

    /// <summary>Invalidates the object and fetches it again.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReRead()
    {
        Invalidate();
        BoldSystem.SystemPersistenceHandler.FetchObjectById(BoldObjectLocator!.BoldObjectID);
    }

    /// <summary>True when no ordinary role other than the one at <paramref name="index"/> has links.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CheckLinks(int index)
    {
        for (int I = 0; I < BoldClassTypeInfo.AllMembers.Count; I++)
        {
            TBoldMemberRTInfo MemberRTInfo = BoldClassTypeInfo.AllMembers[I];
            if (I != index && !MemberRTInfo.IsDerived)
            {
                if (MemberRTInfo.IsMultiRole)
                {
                    if (((TBoldRoleRTInfo)MemberRTInfo).RoleType == TBoldRoleType.rtRole && ((TBoldObjectList)BoldMembers(I)).Count != 0)
                    {
                        return false;
                    }
                }
                else if (MemberRTInfo.IsSingleRole)
                {
                    if (((TBoldRoleRTInfo)MemberRTInfo).RoleType == TBoldRoleType.rtRole && ((TBoldObjectReference)BoldMembers(I)).Locator is not null)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// True when the object may be deleted: not read only, MayDelete, roles empty or cascading as their delete actions
    /// say (checking each object once), and the subscribers and the system agree.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private bool InternalCanDelete(TBoldDomainElementCollection CheckedObjects, bool Cascade)
    {
        static bool CheckEmpty(TBoldMember Member) => Member switch
        {
            TBoldObjectList list => list.Count == 0,
            TBoldObjectReference reference => reference.BoldObject is null,
            _ => false,
        };

        bool CascadeCanDelete(TBoldMember Member)
        {
            bool ok = true;
            if (Member is TBoldObjectList list)
            {
                list.EnsureObjects();
                for (int i = 0; i < list.Count; i++)
                {
                    ok = ok && list[i]!.InternalCanDelete(CheckedObjects, Cascade);
                }
            }
            else if (Member is TBoldObjectReference reference && reference.BoldObject is not null)
            {
                ok = reference.BoldObject.InternalCanDelete(CheckedObjects, Cascade);
            }

            return ok;
        }

        bool result = !IsReadOnly;

        if (CheckedObjects.Includes(this))
        {
            return result;
        }

        CheckedObjects.Add(this);

        if (!result)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sObjectIsreadOnly, this));
            return false;
        }

        result = MayDelete();
        if (!result)
        {
            return false;
        }

        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (BoldClassTypeInfo.AllMembers[i] is TBoldRoleRTInfo RoleRTInfo && (BoldSystem.BoldPersistent || GetBoldMemberAssigned(i)))
            {
                if (!RoleRTInfo.IsDerived && RoleRTInfo.RoleType == TBoldRoleType.rtRole)
                {
                    switch (RoleRTInfo.DeleteAction)
                    {
                        case TDeleteAction.daCascade:
                            result = !Cascade || (result && CascadeCanDelete(BoldMembers(i)));
                            break;
                        case TDeleteAction.daProhibit:
                            result = result && CheckEmpty(BoldMembers(i));
                            break;
                    }
                }
                else if (RoleRTInfo.RoleType == TBoldRoleType.rtLinkRole)
                {
                    result = result && CascadeCanDelete(BoldMembers(i));
                }

                if (!result)
                {
                    break;
                }
            }
        }

        if (result)
        {
            result = SendQuery(bqMayDelete, [], null) && BoldSystem.CanDeleteObject(this);
        }
        else
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sObjectHasRelations, this));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool CanDelete()
    {
        using var list = new TBoldDomainElementCollection();
        return InternalCanDelete(list, true);
    }

    /// <summary>Deletes the object in a transaction: cascades, clears or keeps each role as its delete action says.</summary>
    [Delphi(PortStatus.Ported)]
    public void Delete()
    {
        static void CascadeDelete(TBoldMember Member)
        {
            if (Member is TBoldObjectList list)
            {
                list.EnsureObjects();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    TBoldObject obj = list[i]!;
                    list.RemoveByIndex(i);
                    obj.Delete();
                }
            }
            else if (Member is TBoldObjectReference reference)
            {
                TBoldObject? obj = reference.BoldObject;
                reference.BoldObject = null;
                obj?.Delete();
            }
        }

        static void ClearRelation(TBoldMember Member)
        {
            if (Member is TBoldObjectList list)
            {
                list.Clear();
            }
            else if (Member is TBoldObjectReference reference)
            {
                reference.BoldObject = null;
            }
        }

        void DoDelete()
        {
            for (int i = 0; i < BoldMemberCount; i++)
            {
                if (BoldClassTypeInfo.AllMembers[i] is TBoldRoleRTInfo RoleRTInfo && !RoleRTInfo.IsDerived &&
                    (RoleRTInfo.RoleType == TBoldRoleType.rtRole || RoleRTInfo.RoleType == TBoldRoleType.rtInnerLinkRole))
                {
                    switch (RoleRTInfo.DeleteAction)
                    {
                        case TDeleteAction.daCascade:
                            CascadeDelete(BoldMembers(i));
                            break;
                        case TDeleteAction.daAllow:
                            ClearRelation(BoldMembers(i));
                            break;
                    }
                }
            }
        }

        BoldClearLastFailure();
        if (!StartDelete())
        {
            BoldRaiseLastFailure(this, "Delete", sPreconditionNotMet);
        }

        TBoldSystem system = BoldSystem;
        try
        {
            system.StartTransaction(TBoldSystemTransactionMode.stmNormal);
            DoDelete();
            EndDelete();
            system.CommitTransaction(TBoldSystemTransactionMode.stmNormal);
        }
        catch
        {
            FailDelete();
            system.RollbackTransaction(TBoldSystemTransactionMode.stmNormal);
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMember? FindBoldMemberByExpressionName(string Name)
    {
        int index = BoldMemberIndexByExpressionName(Name);
        return index == -1 ? null : BoldMembers(index);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMember GetBoldMemberByExpressionName(string Name) =>
        FindBoldMemberByExpressionName(Name) ?? throw new EBold(SysUtils.Format(sNoSuchMember, GetType().Name, Name));

    [Delphi(PortStatus.Ported)]
    private int GetBoldMemberIndexByExpressionName(string Name) => BoldClassTypeInfo.MemberRTInfoByExpressionName(Name)?.index ?? -1;

    /// <summary>Equal to itself, or to a reference that points to it.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (CompareType != TBoldCompareType.ctDefault)
        {
            throw new EBold(SysUtils.Format(sUnknownCompareType, GetType().Name, "IsEqualAs"));
        }

        if (BoldElement is TBoldObjectReference reference)
        {
            BoldElement = reference.BoldObject;
        }

        return BoldElement == this;
    }

    /// <summary>An immutable list with just this object.</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList)
    {
        var systemTypeInfo = (TBoldSystemTypeInfo)BoldType!.SystemTypeInfo!;
        TBoldList NewList = (TBoldList)TBoldMember.CreateWithTypeInfo(typeof(TBoldObjectList), systemTypeInfo.ListTypeInfoByElement(BoldType)!);
        NewList.Add(this);
        NewList.MakeImmutable();
        ResultList.SetOwnedValue(NewList);
    }

    /// <summary>Makes the new object existing (modified or transient) and runs CompleteCreate; undoes the state when that raises.</summary>
    [Delphi(PortStatus.Ported)]
    private void EndCreate()
    {
        SetBoldExistenceState(TBoldExistenceState.besExisting);
        SetBoldPersistenceState(BoldPersistent ? TBoldValuePersistenceState.bvpsModified : TBoldValuePersistenceState.bvpsTransient);
        try
        {
            CompleteCreate();
            SendEvent(beCompleteModify);
        }
        catch
        {
            SetBoldExistenceState(TBoldExistenceState.besNotCreated);
            SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);
            throw;
        }

        BoldSystem.AddToTransaction(this);
    }

    [Delphi(PortStatus.Ported)]
    private void EndReCreate()
    {
        SetBoldExistenceState(TBoldExistenceState.besExisting);
        SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);
        SendEvent(beObjectFetched);
    }

    [Delphi(PortStatus.Ported)]
    internal void EndUpdate(TBoldIDTranslationList? Translationlist) => SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);

    /// <summary>True, after taking a pessimistic lock and running PrepareDelete, when the existing object may be deleted.</summary>
    [Delphi(PortStatus.Ported)]
    private bool StartDelete()
    {
        if (BoldExistenceState != TBoldExistenceState.besExisting)
        {
            StateError("StartDelete");
        }

        bool result;
        using (var list = new TBoldDomainElementCollection())
        {
            result = InternalCanDelete(list, false);
        }

        if (result && BoldSystem.PessimisticLockHandler is not null)
        {
            result = BoldSystem.PessimisticLockHandler.LockElement(this);
        }

        if (result)
        {
            DoStartDelete();
        }

        return result;
    }

    /// <summary>Marks the object deleted; a new object becomes current (and is destroyed), a saved one modified.</summary>
    [Delphi(PortStatus.Ported)]
    private void EndDelete()
    {
        SetBoldExistenceState(TBoldExistenceState.besDeleted);
        switch (BoldPersistenceState)
        {
            case TBoldValuePersistenceState.bvpsModified:
                SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);
                break;
            case TBoldValuePersistenceState.bvpsCurrent:
                SetBoldPersistenceState(TBoldValuePersistenceState.bvpsModified);
                break;
        }

        if (BoldPersistenceState != TBoldValuePersistenceState.bvpsModified)
        {
            BoldSystem.DestroyObject(this);
        }

        BoldSystem.AddToTransaction(this);
    }

    [Delphi(PortStatus.Ported)]
    private void FailDelete()
    {
    }

    /// <summary>Modified, or existing with a dirty member.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetBoldDirty()
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
        {
            return true;
        }

        if (BoldExistenceState != TBoldExistenceState.besExisting)
        {
            return false;
        }

        if (!MemberModifiedKnown)
        {
            CalculateMemberModified();
        }

        return MemberModified;
    }

    [Delphi(PortStatus.Ported)]
    internal void MemberBecomingModified(TBoldMember BoldMember)
    {
        MemberModified = true;
        MemberModifiedKnown = true;
        BoldSystem.MarkObjectDirty(this);
    }

    [Delphi(PortStatus.Ported)]
    internal void MemberBecomingClean(TBoldMember BoldMember)
    {
        if (MemberModified)
        {
            MemberModifiedKnown = false;
            BoldSystem.MarkObjectPossiblyCleaner(this);
        }
    }

    /// <summary>Finds whether a member is dirty (or, for a new object, not created yet but stored in the object).</summary>
    [Delphi(PortStatus.Ported)]
    private void CalculateMemberModified()
    {
        MemberModifiedKnown = true;
        MemberModified = false;
        if (BoldPersistent)
        {
            for (int i = 0; i < BoldMemberCount; i++)
            {
                MemberModified = GetBoldMemberAssigned(i)
                    ? BoldMembers(i).BoldDirty
                    : GetElementFlag(befObjectWasCreatedNew) && BoldClassTypeInfo.AllMembers[i].IsStoredInObject && BoldSystem.PersistenceController is not null;
                if (MemberModified)
                {
                    return;
                }
            }
        }
    }

    /// <summary>Called when a new object has been created; nothing here, generated code overrides it.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void CompleteCreate()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void CompleteUpdate()
    {
    }

    [Delphi(PortStatus.Ported)] protected virtual bool MayUpdate() => true;

    [Delphi(PortStatus.Ported)]
    protected virtual void PrepareUpdate()
    {
    }

    [Delphi(PortStatus.Ported)] protected virtual bool MayDelete() => true;

    [Delphi(PortStatus.Ported)]
    protected virtual void PrepareDelete()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void DoStartDelete()
    {
        PrepareDelete();
        SendEvent(bePrepareDelete);
    }

    [Delphi(PortStatus.Ported)]
    internal void DoStartUpdate()
    {
        PrepareUpdate();
        PrepareUpdateMembers();
    }

    [Delphi(PortStatus.Ported)]
    internal bool GetIsModified() => BoldPersistenceState == TBoldValuePersistenceState.bvpsModified;

    /// <summary>False when a persistent member that must be saved may not be.</summary>
    [Delphi(PortStatus.Ported)]
    internal bool MayUpdateMembers()
    {
        for (int i = 0; i < BoldMemberCount; i++)
        {
            TBoldMember Member = BoldMembers(i);
            if (Member.BoldPersistent && (Member.BoldDirty || BoldSystem.BoldSystemTypeInfo.UpdateWholeObjects) && !Member.CanUpdate())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Finishes the update of each saved member.</summary>
    [Delphi(PortStatus.Ported)]
    internal void EndUpdateMembers(TBoldIDTranslationList? Translationlist)
    {
        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (GetBoldMemberAssigned(i))
            {
                TBoldMember Member = BoldMembers(i);
                if (Member.BoldPersistent && (BoldSystem.BoldSystemTypeInfo.UpdateWholeObjects || Member.BoldDirty))
                {
                    Member.EndUpdate(Translationlist);
                    Member.CompleteUpdate();
                }
            }
        }
    }

    /// <summary>Makes the fetched members (all non-delayed persistent ones when the list is nil) current when they were invalid.</summary>
    [Delphi(PortStatus.Ported)]
    internal void EndFetchMembers(TBoldMemberIdList? MemberIdList)
    {
        static void IfNecessaryEndFetchMember(TBoldMember? Member)
        {
            if (Member is not null && Member.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
            {
                Member.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
            }
        }

        if (MemberIdList is not null)
        {
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                IfNecessaryEndFetchMember(BoldMembers(MemberIdList[i].MemberIndex));
            }
        }
        else
        {
            for (int i = 0; i < BoldMemberCount; i++)
            {
                if (!BoldClassTypeInfo.AllMembers[i].DelayedFetch)
                {
                    TBoldMember aMember = BoldMembers(i);
                    if (aMember.BoldPersistent)
                    {
                        IfNecessaryEndFetchMember(aMember);
                    }
                }
            }
        }
    }

    /// <summary>Makes a transient object persistent (a link object only when both ends are).</summary>
    [Delphi(PortStatus.Ported)]
    public void BoldMakePersistent()
    {
        if (!BoldPersistent)
        {
            if (BoldPersistenceState != TBoldValuePersistenceState.bvpsTransient)
            {
                StateError("BoldMakePersistent");
            }

            if (!BoldClassTypeInfo.Persistent || !BoldSystem.BoldPersistent)
            {
                throw new EBold(SysUtils.Format(sCannotMakePersistent, GetType().Name));
            }

            if (BoldClassTypeInfo.IsLinkClass)
            {
                for (int i = BoldClassTypeInfo.FirstOwnMemberIndex; i < BoldMemberCount; i++)
                {
                    if (BoldMembers(i).BoldMemberRTInfo!.IsSingleRole &&
                        ((TBoldRoleRTInfo)BoldMembers(i).BoldMemberRTInfo!).RoleType == TBoldRoleType.rtInnerLinkRole &&
                        !((TBoldObjectReference)BoldMembers(i)).Locator!.ObjectIsPersistent)
                    {
                        throw new EBold(SysUtils.Format(sCannotMakeLinkPersistent, GetType().Name));
                    }
                }
            }

            SetElementFlag(befPersistent, true);
            SetBoldPersistenceState(TBoldValuePersistenceState.bvpsModified);
            for (int i = 0; i < BoldMemberCount; i++)
            {
                BoldMembers(i).ObjectBecomingPersistent();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void SetIsReadOnly(bool NewValue)
    {
        if (NewValue != IsReadOnly)
        {
            SetElementFlag(befObjectReadOnly, NewValue);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal TBoldTimeStampType GetTimeStamp() => BoldStoresTimeStamp ? fTimeStamp : -1;

    [Delphi(PortStatus.Ported)]
    internal void SetTimeStamp(TBoldTimeStampType NewValue)
    {
        if (BoldStoresTimeStamp)
        {
            fTimeStamp = NewValue;
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetObjectHasSubscribers()
    {
        bool result = HasSubscribers;
        int i = 0;
        while (!result && i < BoldMemberCount)
        {
            result = BoldMemberAssigned(i) && BoldMembers(i).HasSubscribersOf;
            i++;
        }

        return result;
    }

    /// <summary>Raises EBoldInternal with the object's existence and persistence state.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void StateError(string S) =>
        base.StateError(SysUtils.Format("%s (ExistenceState: %s PersistenceState: %s)", S, BoldExistenceState.ToString(), BoldPersistenceState.ToString()));

    [Delphi(PortStatus.Ported)]
    protected virtual void ToBeRemovedMemberAccessed(TBoldMemberRTInfo MemberRTInfo) =>
        BoldLogHandler.BoldLog().LogFmt(sToBeRemovedAccessed, [SysUtils.DateTimeToStr(SysUtils.Now()), BoldClassTypeInfo.ExpressionName, MemberRTInfo.ExpressionName]);

    [Delphi(PortStatus.Ported)]
    protected internal virtual void ToBeRemovedMemberModified(TBoldMemberRTInfo MemberRTInfo) =>
        BoldLogHandler.BoldLog().LogFmt(sToBeRemovedModified, [SysUtils.DateTimeToStr(SysUtils.Now()), BoldClassTypeInfo.ExpressionName, MemberRTInfo.ExpressionName]);

    [Delphi(PortStatus.Ported)]
    protected virtual void ToBeRemovedClassAccessed() =>
        BoldLogHandler.BoldLog().LogFmt(sToBeRemovedObjectAccessed, [SysUtils.DateTimeToStr(SysUtils.Now()), BoldClassTypeInfo.ExpressionName]);

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => fBoldClassTypeInfo;

    [Delphi(PortStatus.Ported)]
    public bool CanUpdate() => MayUpdate() && SendQuery(bqMayUpdate, [], null);

    [Delphi(PortStatus.Ported)]
    private bool GetIsReadOnly() => GetElementFlag(befObjectReadOnly);

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => GetType().Name;

    [Delphi(PortStatus.Ported)]
    protected override TBoldEvaluator? GetEvaluator() => BoldSystem.Evaluator;

    [Delphi(PortStatus.Ported)]
    internal bool CanUnload()
    {
        bool result = !BoldDirty;
        if (!result)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sObjectIsDirty, this));
        }

        return result;
    }

    /// <summary>Invalidates the persistent members, so they are fetched again; raises for a dirty object.</summary>
    [Delphi(PortStatus.Ported)]
    public void Invalidate()
    {
        if (BoldDirty)
        {
            throw new EBold(SysUtils.Format(sCannotInvalidateDirtyObject, GetType().Name));
        }

        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (BoldMembers(i).BoldPersistent)
            {
                BoldMembers(i).Invalidate();
            }
        }
    }

    /// <summary>Discards the changes: members go back to their saved values; a new object is destroyed, a deleted one comes back.</summary>
    [Delphi(PortStatus.Ported)]
    public void Discard()
    {
        if (BoldSystem.InTransaction())
        {
            throw new EBold(sCannotDiscardInTransaction);
        }

        if (!BoldObjectLocator!.BoldObjectID.IsStorable)
        {
            UnLinkAll();
        }

        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (BoldMemberAssigned(i))
            {
                BoldMembers(i).InternalDiscard();
            }
        }

        if (BoldObjectIsNew)
        {
            SetBoldExistenceState(TBoldExistenceState.besDeleted);
            SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);
            BoldSystem.DestroyObject(this);
        }
        else if (BoldPersistenceState == TBoldValuePersistenceState.bvpsModified && BoldExistenceState == TBoldExistenceState.besDeleted)
        {
            SetBoldExistenceState(TBoldExistenceState.besExisting);
            SetBoldPersistenceState(TBoldValuePersistenceState.bvpsCurrent);
        }
    }

    /// <summary>Changes the existence state, saving the old one for optimistic locking and rollback, and sends beObjectCreated or beObjectDeleted.</summary>
    [Delphi(PortStatus.Ported)]
    internal void SetBoldExistenceState(TBoldExistenceState Value)
    {
        if (Value != BoldExistenceState)
        {
            BoldSystem.fOldValueHandler.ObjectExistenceChange(this);
            BoldSystem.CopyObjectToRollBackBuffer(this);
            SetInternalState(unchecked((uint)BoldExistenceStateMask), BoldESShift, (uint)Value);
            if (BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
            {
                SendEvent(Value == TBoldExistenceState.besExisting ? beObjectCreated : beObjectDeleted);
            }
        }
    }

    /// <summary>Changes the persistence state and puts the object in or (possibly) out of the dirty list.</summary>
    [Delphi(PortStatus.Ported)]
    internal void SetBoldPersistenceState(TBoldValuePersistenceState Value)
    {
        TBoldValuePersistenceState OldValue = BoldPersistenceState;
        BoldSystem.fOldValueHandler.ObjectExistencePersistenceStateChange(this, Value);
        if (Value != OldValue)
        {
            SetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift, (uint)Value);
            if (Value == TBoldValuePersistenceState.bvpsModified)
            {
                BoldSystem.MarkObjectDirty(this);
            }
            else if (OldValue == TBoldValuePersistenceState.bvpsModified)
            {
                BoldSystem.MarkObjectPossiblyCleaner(this);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExistenceState GetBoldExistenceState() => (TBoldExistenceState)GetInternalState(unchecked((uint)BoldExistenceStateMask), BoldESShift);

    [Delphi(PortStatus.Ported)]
    private TBoldValuePersistenceState GetBoldPersistenceState() =>
        (TBoldValuePersistenceState)GetInternalState(unchecked((uint)BoldPersistenceStateMask), BoldPSShift);

    /// <summary>Gives the object as IBoldObjectContents.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldObjectContents))
        {
            Obj = new TBoldObject_Proxy(this, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObject? AtTime(TBoldTimeStampType Time) => BoldObjectLocator!.AtTime(Time)?.EnsuredBoldObject;

    [Delphi(PortStatus.Ported)]
    private TBoldTimeStampType GetBoldTime() => BoldObjectLocator!.BoldObjectID.TimeStamp;

    [Delphi(PortStatus.Ported)]
    private void PrepareUpdateMembers()
    {
        for (int i = 0; i < BoldMemberCount; i++)
        {
            if (GetBoldMemberAssigned(i) && BoldMembers(i).BoldDirty)
            {
                BoldMembers(i).PrepareUpdate();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetBoldObjectExists() => BoldExistenceState == TBoldExistenceState.besExisting;

    /// <summary>Checks, for generated code, that the member exists at that index and is of that class; raises when not.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool ValidateMember(string ObjectDelphiName, string MemberDelphiName, int GeneratedMemberIndex, TBoldMemberClass MemberClass)
    {
        var MemberRTInfo = (TBoldMemberRTInfo?)BoldClassTypeInfo.AllMembers.ItemsByDelphiName(MemberDelphiName);

        if (MemberRTInfo is null)
        {
            throw new EBold(SysUtils.Format(sValidate_NoSuchMember, ObjectDelphiName, MemberDelphiName));
        }

        if (GeneratedMemberIndex != -1 && MemberRTInfo.index != GeneratedMemberIndex)
        {
            throw new EBold(SysUtils.Format(sValidate_MemberIndexOutOfSynch, ObjectDelphiName, MemberDelphiName, GeneratedMemberIndex, MemberRTInfo.index));
        }

        TBoldMember Member = BoldMembers(MemberRTInfo.index);

        if (!MemberClass.IsInstanceOfType(Member))
        {
            throw new EBold(SysUtils.Format(sValidate_InvalidMemberType, ObjectDelphiName, MemberDelphiName, MemberClass.Name, Member.GetType().Name));
        }

        return true;
    }

    [Delphi(PortStatus.Ported, Note = "The number of member slots; Delphi gives the byte size of the block that also holds the timestamp.")]
    private int GetDynamicDataSize() => BoldMemberCount;

    /// <summary>Clears the touched flag of the object and its members.</summary>
    [Delphi(PortStatus.Ported)]
    public void ClearTouched()
    {
        if (Touched)
        {
            SetElementFlag(befTouched, false);
            for (int i = 0; i < BoldMemberCount; i++)
            {
                if (BoldMemberAssigned(i))
                {
                    BoldMembers(i).SetElementFlagOf(befTouched, false);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too.")]
    internal string GetGlobalId() => "";

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too.")]
    internal void SetGlobalId(string NewValue)
    {
    }

    [Delphi(PortStatus.Ported)]
    internal bool InDirtyList
    {
        get => GetElementFlag(befInDirtyList);
        set => SetElementFlag(befInDirtyList, value);
    }

    [Delphi(PortStatus.Ported)] internal bool IsReadOnly => GetIsReadOnly();

    [Delphi(PortStatus.Ported)]
    private bool MemberModified
    {
        get => GetElementFlag(befMemberModified);
        set => SetElementFlag(befMemberModified, value);
    }

    [Delphi(PortStatus.Ported)]
    private bool MemberModifiedKnown
    {
        get => GetElementFlag(befMemberModifiedKnown);
        set => SetElementFlag(befMemberModifiedKnown, value);
    }

    [Delphi(PortStatus.Ported)] private bool BoldStoresTimeStamp => GetElementFlag(befStoresTimeStamp);

    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo BoldClassTypeInfo => fBoldClassTypeInfo;
    [Delphi(PortStatus.Ported)] public TBoldExistenceState BoldExistenceState => GetBoldExistenceState();
    [Delphi(PortStatus.Ported)] public TBoldMember BoldMemberByExpressionName(string name) => GetBoldMemberByExpressionName(name);
    [Delphi(PortStatus.Ported)] public int BoldMemberCount => GetBoldMemberCount();
    [Delphi(PortStatus.Ported)] public int BoldMemberIndexByExpressionName(string name) => GetBoldMemberIndexByExpressionName(name);
    [Delphi(PortStatus.Ported)] public bool BoldMemberAssigned(int index) => GetBoldMemberAssigned(index);
    [Delphi(PortStatus.Ported)] public TBoldMember BoldMembers(int index) => GetBoldMembers(index);
    [Delphi(PortStatus.Ported)] public TBoldObjectLocator? BoldObjectLocator => FBoldObjectLocator;
    [Delphi(PortStatus.Ported)] public TBoldValuePersistenceState BoldPersistenceState => GetBoldPersistenceState();
    [Delphi(PortStatus.Ported)] public TBoldSystem BoldSystem => GetBoldSystem();
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType BoldTime => GetBoldTime();
    [Delphi(PortStatus.Ported)] public bool ObjectHasSubscribers => GetObjectHasSubscribers();
    [Delphi(PortStatus.Ported)] public IBoldObjectContents AsIBoldObjectContents(TBoldDomainElementProxyMode Mode) => GetAsIBoldObjectContents(Mode);
    [Delphi(PortStatus.Ported)] public bool BoldObjectIsNew => GetBoldObjectIsNew();
    [Delphi(PortStatus.Ported)] public bool BoldObjectIsDeleted => GetBoldObjectIsDeleted();
    [Delphi(PortStatus.Ported)] public bool BoldObjectExists => GetBoldObjectExists();
    [Delphi(PortStatus.Ported)] public bool Touched => GetElementFlag(befTouched);
}
