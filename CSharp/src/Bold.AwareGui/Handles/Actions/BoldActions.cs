// Delphi: Source/Handles/Actions/BoldActions.pas
// Port map: CSharp/port/units/BoldActions.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>What closing a system with dirty objects does: ask, save, discard, or raise.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldSaveAction { saAsk, saYes, saNo, saFail }

/// <summary>An action on a system handle.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemHandleAction : TBoldHandleAction
{
    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    public TBoldSystemHandleAction(IContainer? AOwner = null)
        : base(AOwner)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemHandle? GetBoldSystemHandle() => (TBoldSystemHandle?)BoldElementHandle;

    [Delphi(PortStatus.Ported)]
    protected virtual void SetBoldSystemHandle(TBoldSystemHandle? Value) => BoldElementHandle = Value;

    [Delphi(PortStatus.Ported)]
    public TBoldSystemHandle? BoldSystemHandle
    {
        get => GetBoldSystemHandle();
        set => SetBoldSystemHandle(value);
    }
}

/// <summary>An action whose caption is the reason of the last failure, for the control (a status bar) linked to it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFailureDetectionAction : TAction
{
    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    public TBoldFailureDetectionAction(IContainer? AOwner = null)
        : base(AOwner)
    {
    }

    /// <summary>Shows the last failure's reason as the caption (and as the target's text when it shows one line).</summary>
    [Delphi(PortStatus.Ported, Note = "A target that shows one line of text (ISimpleTextTarget) stands in for Delphi's TStatusBar.")]
    public override void UpdateTarget(object? Target)
    {
        base.UpdateTarget(Target);
        TBoldFailureReason? failure = BoldSystem.GetBoldLastFailureReason();
        Caption = failure is not null ? failure.Reason : "";

        // The statusbar seems to have a bug, it does not correctly update its caption...
        // even this does not make it work fully... Bug in VCL
        if (Target is ISimpleTextTarget statusBar)
        {
            statusBar.SimpleText = Caption;
        }
    }

    /// <summary>True for the control linked to this action.</summary>
    [Delphi(PortStatus.Ported, Note = "A target linked to an action (IActionClient) stands in for Delphi's TControl.")]
    public override bool HandlesTarget(object? Target) => Target is IActionClient control && control.Action == this;
}

/// <summary>Saves the dirty objects of the system; enabled when the system is active and has dirty objects.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUpdateDBAction : TBoldSystemHandleAction
{
    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldUpdateDBAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sUpdateDB;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldSystemHandle!.Active && BoldSystemHandle.System!.DirtyObjects.Count > 0;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldSystemHandle?.UpdateDatabase();
    }
}

/// <summary>
/// Opens and closes the system; its caption says which it will do. Closing a system with dirty objects asks, saves,
/// discards or raises, as SaveOnClose says.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldActivateSystemAction : TBoldSystemHandleAction
{
    private const int breValueIdentityChanged = 45;

    private TBoldPassthroughSubscriber? fHandleIdentitySubscriber;
    private TNotifyEvent? fOnSystemClosed;
    private TNotifyEvent? fOnSystemActivated;
    private string FOpenCaption = "";
    private string FCloseCaption = "";
    private TBoldSaveAction FSaveOnClose;
    private string FSaveQuestion = "";

    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldActivateSystemAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        fHandleIdentitySubscriber = new TBoldPassthroughSubscriber(_Receive);
        FOpenCaption = sOpenSystem;
        FCloseCaption = sCloseSystem;
        FSaveQuestion = sThereAreDirtyObjects;
        UpdateCaption();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldActivateSystemAction.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleIdentitySubscriber);
        }

        base.Dispose(disposing);
    }

    /// <summary>Opens a closed system; closes an open one after dealing with its dirty objects as SaveOnClose says.</summary>
    [Delphi(PortStatus.Ported, Note = "Asks through Dialogs.MessageDlg, which the GUI toolkit supplies.")]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        if (BoldSystemHandle is not null)
        {
            bool Update = true;
            if (BoldSystemHandle.Active)
            {
                switch (FSaveOnClose)
                {
                    case TBoldSaveAction.saAsk:
                        if (BoldSystemHandle.System!.DirtyObjects.Count > 0)
                        {
                            switch (Dialogs.MessageDlg(FSaveQuestion, TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo, TMsgDlgBtn.mbCancel), 0))
                            {
                                case Controls.mrYes:
                                    BoldSystemHandle.System.UpdateDatabase();
                                    break;
                                case Controls.mrNo:
                                    BoldSystemHandle.System.Discard();
                                    break;
                                case Controls.mrCancel:
                                    Update = false;
                                    break;
                            }
                        }

                        break;
                    case TBoldSaveAction.saYes:
                        BoldSystemHandle.UpdateDatabase();
                        break;
                    case TBoldSaveAction.saNo:
                        BoldSystemHandle.System!.Discard();
                        break;
                    case TBoldSaveAction.saFail:
                        if (BoldSystemHandle.System!.DirtyObjects.Count > 0)
                        {
                            throw new EBold(sClosingWithDirtyObjects);
                        }

                        break;
                }
            }

            if (Update)
            {
                BoldSystemHandle.Active = !BoldSystemHandle.Active;
            }
        }
    }

    /// <summary>Follows the new handle's opening and closing.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetBoldSystemHandle(TBoldSystemHandle? Value)
    {
        base.SetBoldSystemHandle(Value);
        fHandleIdentitySubscriber!.CancelAllSubscriptions();
        BoldSystemHandle?.AddSmallSubscription(fHandleIdentitySubscriber, TBoldSmallEventSet.Of(beValueIdentityChanged, beDestroying), breValueIdentityChanged);
        UpdateCaption();
    }

    [Delphi(PortStatus.Ported)]
    private void SetCloseCaption(string Value)
    {
        FCloseCaption = Value;
        UpdateCaption();
    }

    [Delphi(PortStatus.Ported)]
    private void SetOpenCaption(string Value)
    {
        FOpenCaption = Value;
        UpdateCaption();
    }

    [Delphi(PortStatus.Ported)]
    protected void SystemActivated() => fOnSystemActivated?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    protected void SystemClosed() => fOnSystemClosed?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    private void TriggerSwitchEvent()
    {
        if (BoldSystemHandle is not null && BoldSystemHandle.Active)
        {
            SystemActivated();
        }
        else
        {
            SystemClosed();
        }
    }

    /// <summary>The close caption for an open system, the open caption for a closed one, the name without a handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void UpdateCaption()
    {
        if (BoldSystemHandle is not null)
        {
            Caption = BoldSystemHandle.Active ? CloseCaption : OpenCaption;
        }
        else
        {
            Caption = Name;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void _HandleSubscriberReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        base._HandleSubscriberReceive(Originator, OriginalEvent, RequestedEvent);
        if (RequestedEvent == breValueIdentityChanged)
        {
            UpdateCaption();
        }
    }

    /// <summary>The system opened or closed: the caption follows and the event goes off.</summary>
    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breValueIdentityChanged)
        {
            UpdateCaption();
            TriggerSwitchEvent();
        }
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnSystemOpened
    {
        get => fOnSystemActivated;
        set => fOnSystemActivated = value;
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnSystemClosed
    {
        get => fOnSystemClosed;
        set => fOnSystemClosed = value;
    }

    [Delphi(PortStatus.Ported)]
    public string OpenCaption
    {
        get => FOpenCaption;
        set => SetOpenCaption(value);
    }

    [Delphi(PortStatus.Ported)]
    public string CloseCaption
    {
        get => FCloseCaption;
        set => SetCloseCaption(value);
    }

    [Delphi(PortStatus.Ported)]
    public string SaveQuestion
    {
        get => FSaveQuestion;
        set => FSaveQuestion = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSaveAction SaveOnClose
    {
        get => FSaveOnClose;
        set => FSaveOnClose = value;
    }
}
