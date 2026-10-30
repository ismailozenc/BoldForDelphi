// Delphi: Source/ObjectSpace/BORepresentation/BoldExternalObjectSpaceEventHandler.pas
// Port map: CSharp/port/units/BoldExternalObjectSpaceEventHandler.md

using static Bold.BoldSubscription;

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldClassChangedEvent(TBoldObjectList TheClass);

[Delphi(PortStatus.Ported)]
public delegate void TBoldEmbeddedStateChangedEvent(TBoldObject? BoldObject);

[Delphi(PortStatus.Ported)]
public delegate void TBoldNonEmbeddedStateChangedEvent(TBoldMember BoldMember);

[Delphi(PortStatus.Ported)]
public delegate void TBoldConflictEvent(TBoldObject BoldObject);

[Delphi(PortStatus.Ported)]
public delegate void TBoldLockLostEvent(string LockName);

[Delphi(PortStatus.Ported)]
public delegate void TBoldDoDisconnectEvent(string aMessage, int RemainDisconnectedMSec);

/// <summary>
/// Brings other clients' changes into the object space: a changed class is invalidated, a changed object or member
/// is invalidated (a dirty object is a conflict), a deleted object is marked deleted. The events can be handled
/// instead. A disconnect message from the propagator disconnects the propagator handle.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalObjectSpaceEventHandler : TBoldAbstractDequeuer
{
    private TBoldSystemHandle? fBoldSystemHandle;
    private TBoldAbstractPropagatorHandle? fPropagatorHandle;
    private TBoldPassthroughSubscriber? fPTSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldExternalObjectSpaceEventHandler(System.ComponentModel.IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fPTSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalObjectSpaceEventHandler.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fPTSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetPropagatorHandle(TBoldAbstractPropagatorHandle? Value)
    {
        if (fPropagatorHandle != Value)
        {
            Subscribe(false);
            fPropagatorHandle = Value;
            Subscribe(true);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystemHandle(TBoldSystemHandle? aSystemHandle)
    {
        if (aSystemHandle != fBoldSystemHandle)
        {
            Subscribe(false);
            fBoldSystemHandle = aSystemHandle;
            Subscribe(true);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Also forgets a propagator handle that goes; Delphi subscribed to it but forgot only the system handle.")]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fBoldSystemHandle && RequestedEvent == beDestroying)
        {
            BoldSystemHandle = null;
        }
        else if (Originator == fPropagatorHandle && RequestedEvent == beDestroying)
        {
            PropagatorHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetObjectByID(TBoldDefaultID ObjectID) => fBoldSystemHandle!.System!.Locators.LocatorByID(ObjectID)?.BoldObject;

    [Delphi(PortStatus.Ported)]
    private void Subscribe(bool DoSubscribe)
    {
        if (DoSubscribe)
        {
            if (!IsDesigning)
            {
                fPropagatorHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
                fBoldSystemHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            }
        }
        else
        {
            fPTSubscriber?.CancelAllSubscriptions();
        }
    }

    /// <summary>Handles a message from the propagator: a "DISCONNECT:ms:message", or an object space event.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void HandleMessage(string aMsg)
    {
        if (fBoldSystemHandle is null)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sEventHandlerNotConnected, GetType().Name, Name));
        }

        if (fBoldSystemHandle.System is null)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sSystemHandleNotActive, GetType().Name, fBoldSystemHandle.Name));
        }

        if (aMsg.StartsWith("DISCONNECT:", StringComparison.Ordinal))
        {
            if (fPropagatorHandle is not null)
            {
                fPropagatorHandle.Connected = false;
            }

            if (OnDoDisconnect is not null)
            {
                string temp = aMsg[(aMsg.IndexOf(':') + 1)..];
                int colon = temp.IndexOf(':');
                OnDoDisconnect(temp[(colon + 1)..], SysUtils.StrToIntDef(colon < 0 ? "" : temp[..colon], -1));
            }
        }
        else
        {
            var ObjectID = new TBoldDefaultID();
            string ClassName = "", MemberName = "", LockName = "";
            TBoldObjectSpaceSubscriptionType SubsType =
                TBoldObjectSpaceExternalEvent.DecodeExternalEvent(aMsg, ref ClassName, ref MemberName, ref LockName, ObjectID);
            switch (SubsType)
            {
                case TBoldObjectSpaceSubscriptionType.bsClassChanged:
                    ClassChanged(ClassName);
                    break;
                case TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged:
                    EmbeddedStateOfObjectChanged(ObjectID);
                    break;
                case TBoldObjectSpaceSubscriptionType.bsObjectDeleted:
                    ObjectDeleted(ObjectID);
                    break;
                case TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged:
                    NonEmbeddedStateOfObjectChanged(MemberName, ObjectID);
                    break;
                case TBoldObjectSpaceSubscriptionType.bsLockLost:
                    LockLost(LockName);
                    break;
            }
        }
    }

    /// <summary>The class's list is invalidated (or OnClassChanged is called).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void ClassChanged(string ClassName)
    {
        TBoldObjectList? theClass = fBoldSystemHandle!.System!.ClassByExpressionName(ClassName);
        if (theClass is null)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sClassNotInSystem, ClassName));
        }

        if (OnClassChanged is not null)
        {
            OnClassChanged(theClass);
        }
        else
        {
            theClass.Invalidate();
        }
    }

    /// <summary>The object (if in memory) is invalidated, or is a conflict when dirty (or OnEmbeddedStateChanged is called).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void EmbeddedStateOfObjectChanged(TBoldDefaultID ObjectID)
    {
        TBoldObject? CurrObj = GetObjectByID(ObjectID);
        if (CurrObj is not null)
        {
            if (OnEmbeddedStateChanged is not null)
            {
                OnEmbeddedStateChanged(CurrObj);
            }
            else if (CurrObj.BoldDirty)
            {
                Conflict(CurrObj);
            }
            else
            {
                CurrObj.Invalidate();
            }
        }
        else if (HandleNilObjects)
        {
            OnEmbeddedStateChanged?.Invoke(null);
        }
    }

    /// <summary>The member (if its object is in memory) is invalidated (or OnNonEmbeddedStateChanged is called).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void NonEmbeddedStateOfObjectChanged(string MemberName, TBoldDefaultID ObjectID)
    {
        TBoldMember? CurrMember = GetObjectByID(ObjectID)?.BoldMemberByExpressionName(MemberName);
        if (CurrMember is not null)
        {
            if (OnNonEmbeddedStateChanged is not null)
            {
                OnNonEmbeddedStateChanged(CurrMember);
            }
            else
            {
                CurrMember.Invalidate();
            }
        }
    }

    /// <summary>The object (if in memory) is marked deleted, or is a conflict when dirty (or OnObjectDeleted is called).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void ObjectDeleted(TBoldDefaultID ObjectId)
    {
        TBoldObject? CurrObj = GetObjectByID(ObjectId);
        if (CurrObj is not null)
        {
            if (OnObjectDeleted is not null)
            {
                OnObjectDeleted(CurrObj);
            }
            else if (CurrObj.BoldDirty)
            {
                Conflict(CurrObj);
            }
            else
            {
                CurrObj.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepPMIn).BoldExistenceState = TBoldExistenceState.besDeleted;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void LockLost(string LockName) => OnLockLost?.Invoke(LockName);

    [Delphi(PortStatus.Ported)]
    protected void Conflict(TBoldObject BoldObject) => OnConflict?.Invoke(BoldObject);

    [Delphi(PortStatus.Ported)]
    protected override void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (Operation == TOperation.opRemove)
        {
            if (AComponent == fBoldSystemHandle)
            {
                fBoldSystemHandle = null;
            }
            else if (AComponent == fPropagatorHandle)
            {
                fPropagatorHandle = null;
            }
        }
    }

    [Delphi(PortStatus.Ported)] public bool HandleNilObjects { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldSystemHandle? BoldSystemHandle
    {
        get => fBoldSystemHandle;
        set => SetBoldSystemHandle(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldClassChangedEvent? OnClassChanged { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldEmbeddedStateChangedEvent? OnEmbeddedStateChanged { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldEmbeddedStateChangedEvent? OnObjectDeleted { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldNonEmbeddedStateChangedEvent? OnNonEmbeddedStateChanged { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldLockLostEvent? OnLockLost { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldConflictEvent? OnConflict { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldDoDisconnectEvent? OnDoDisconnect { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractPropagatorHandle? PropagatorHandle
    {
        get => fPropagatorHandle;
        set => SetPropagatorHandle(value);
    }
}
