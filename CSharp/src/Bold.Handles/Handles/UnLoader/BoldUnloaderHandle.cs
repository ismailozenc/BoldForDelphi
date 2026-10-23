// Delphi: Source/Handles/UnLoader/BoldUnloaderHandle.pas
// Port map: CSharp/port/units/BoldUnloaderHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// Runs an unloader on a system handle's system: a timer ticks it, and each tick it unloads objects that have not been
/// used for a while (MinAgeForUnload milliseconds).
/// </summary>
[Delphi(PortStatus.Ported, Note = "Derives from System.ComponentModel.Component where Delphi derives from TComponent; the timer ticks on the thread that made the handle, through its synchronization context.")]
public class TBoldUnloaderHandle : Component
{
    private const int breHandleDestroyed = 42;
    private const int breSystemChanged = 43;

    private bool fActive;
    private TBoldUnLoader? fUnloader;
    private TTimer? fTimer;
    private TBoldSystemHandle? fBoldSystemHandle;
    private TBoldPassthroughSubscriber? fSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldUnloaderHandle(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fUnloader = new TBoldUnLoader();
        fSubscriber = new TBoldPassthroughSubscriber(ReceiveFromSystemHandle);
        fTimer = new TTimer();
        fTimer.Enabled = true;
        TickIntervall = 1000;
        ScanPerTick = 1000;
        MinAgeForUnload = 300000;
        fTimer.OnTimer = Tick;
        Active = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUnloaderHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fSubscriber);
            SysUtils.FreeAndNil(ref fUnloader);
            SysUtils.FreeAndNil(ref fTimer);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)] private bool GetUnloadDelayedFetch() => Unloader.UnloadDelayedFetch;
    [Delphi(PortStatus.Ported)] private int GetMinAgeForUnload() => Unloader.MinAgeForUnload;
    [Delphi(PortStatus.Ported)] private int GetScanPerTick() => Unloader.ScanPerTick;

    [Delphi(PortStatus.Ported)]
    private void ReceiveFromSystemHandle(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breHandleDestroyed)
        {
            BoldSystemHandle = null;
        }
        else
        {
            PropagateToUnloder();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetActive(bool Value)
    {
        if (fActive != Value)
        {
            fActive = Value;
            PropagateToUnloder();
        }
    }

    [Delphi(PortStatus.Ported)] private void SetUnloadDelayedFetch(bool Value) => Unloader.UnloadDelayedFetch = Value;
    [Delphi(PortStatus.Ported)] private void SetMinAgeForUnload(int Value) => Unloader.MinAgeForUnload = Value;
    [Delphi(PortStatus.Ported)] private void SetScanPerTick(int Value) => Unloader.ScanPerTick = Value;

    [Delphi(PortStatus.Ported)] private TBoldUnLoader Unloader => fUnloader!;

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystemHandle(TBoldSystemHandle? Value)
    {
        if (Value != BoldSystemHandle)
        {
            fSubscriber!.CancelAllSubscriptions();
            fBoldSystemHandle = Value;
            if (Value is not null)
            {
                Value.AddSmallSubscription(fSubscriber, TBoldSmallEventSet.Of(beDestroying), breHandleDestroyed);
                Value.AddSmallSubscription(fSubscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breSystemChanged);
            }

            PropagateToUnloder();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void Tick(object? Sender) => Unloader.Tick();

    /// <summary>Gives the unloader the system handle's system and starts it, or stops it when the handle is inactive.</summary>
    [Delphi(PortStatus.Ported)]
    private void PropagateToUnloder()
    {
        if (Active)
        {
            fUnloader!.BoldSystem = BoldSystemHandle?.System;
            if (fUnloader.BoldSystem is not null)
            {
                fUnloader.Active = true;
            }
        }
        else
        {
            fUnloader!.Active = false;
        }
    }

    [Delphi(PortStatus.Ported)] private int GetTickInterval() => fTimer!.Interval;
    [Delphi(PortStatus.Ported)] private void SetTickInterval(int Value) => fTimer!.Interval = Value;
    [Delphi(PortStatus.Ported)] private TBoldInvalidateMemberEvent? GetOnMayInvalidate() => Unloader.OnMayInvalidate;
    [Delphi(PortStatus.Ported)] private TBoldUnloadObjectEvent? GetOnMayUnload() => Unloader.OnMayUnload;
    [Delphi(PortStatus.Ported)] private void SetOnMayInvalidate(TBoldInvalidateMemberEvent? Value) => Unloader.OnMayInvalidate = Value;
    [Delphi(PortStatus.Ported)] private void SetOnMayUnload(TBoldUnloadObjectEvent? Value) => Unloader.OnMayUnload = Value;

    [Delphi(PortStatus.Ported)]
    public int TickIntervall
    {
        get => GetTickInterval();
        set => SetTickInterval(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystemHandle? BoldSystemHandle
    {
        get => fBoldSystemHandle;
        set => SetBoldSystemHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public int ScanPerTick
    {
        get => GetScanPerTick();
        set => SetScanPerTick(value);
    }

    [Delphi(PortStatus.Ported)]
    public int MinAgeForUnload
    {
        get => GetMinAgeForUnload();
        set => SetMinAgeForUnload(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool UnloadDelayedFetch
    {
        get => GetUnloadDelayedFetch();
        set => SetUnloadDelayedFetch(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool Active
    {
        get => fActive;
        set => SetActive(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldInvalidateMemberEvent? OnMayInvalidate
    {
        get => GetOnMayInvalidate();
        set => SetOnMayInvalidate(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldUnloadObjectEvent? OnMayUnload
    {
        get => GetOnMayUnload();
        set => SetOnMayUnload(value);
    }
}
