// Delphi: Source/ObjectSpace/Unloader/BoldUnloader.pas
// Port map: CSharp/port/units/BoldUnloader.md

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>Asks whether a member may be invalidated; set Invalidate to false to keep it.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldInvalidateMemberEvent(TBoldMember Member, ref bool Invalidate);

/// <summary>Asks whether an object may be unloaded; set Unload to false to keep it.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldUnloadObjectEvent(TBoldObject BoldObject, ref bool Unload);

/// <summary>
/// Frees memory a little at a time: each Tick scans some objects, invalidating untouched derived and delayed members
/// and unloading untouched clean objects nobody subscribes to; between scans it waits MinAgeForUnload ticks. Touching
/// an object or member between two scans keeps it.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUnLoader : TBoldMemoryManagedObject, IDisposable
{
    private TBoldSystem? fBoldSystem;
    private bool fUnloadDelayedFetch;
    private int fMinAgeForUnload;
    private int fScanPerTick;
    private bool fActive;
    private int fWaitCount;
    private TBoldLocatorListTraverser? fTraverser;
    private TBoldInvalidateMemberEvent? fOnMayInvalidate;
    private TBoldUnloadObjectEvent? fOnMayUnload;

    [Delphi(PortStatus.Ported, Symbol = "TBoldUnLoader.Destroy")]
    public void Dispose()
    {
        fTraverser?.Dispose();
        fTraverser = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Scans up to ScanPerTick locators.</summary>
    [Delphi(PortStatus.Ported)]
    private void Scan()
    {
        int Scanned = 0;
        while (!fTraverser!.EndOfList && Scanned < ScanPerTick)
        {
            TBoldObjectLocator Locator = fTraverser.locator;
            if (Locator.BoldObject is { BoldExistenceState: TBoldExistenceState.besExisting })
            {
                ScanObject(Locator.BoldObject);
            }

            fTraverser.Next();
            Scanned++;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ScanObject(TBoldObject BoldObject)
    {
        for (int m = 0; m < BoldObject.BoldMemberCount; m++)
        {
            if (BoldObject.BoldMemberAssigned(m))
            {
                TBoldMember Member = BoldObject.BoldMembers(m);
                if (!Member.Touched &&
                    ((Member.Derived && Member.BoldPersistenceState == TBoldValuePersistenceState.bvpsTransient) ||
                     (UnloadDelayedFetch && Member.BoldMemberRTInfo!.DelayedFetch && Member.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)))
                {
                    bool DoInvalidate = !Member.MemberHasSubscribers();
                    OnMayInvalidate?.Invoke(Member, ref DoInvalidate);
                    if (DoInvalidate)
                    {
                        Member.Invalidate();
                    }
                }
            }
        }

        if (BoldObject.Touched)
        {
            BoldObject.ClearTouched();
        }
        else if (BoldObject.BoldPersistent && !BoldObject.BoldDirty)
        {
            bool DoUnload = !BoldObject.ObjectHasSubscribers;
            OnMayUnload?.Invoke(BoldObject, ref DoUnload);
            if (DoUnload)
            {
                BoldObject.BoldObjectLocator!.UnloadBoldObject();
            }
        }
    }

    /// <summary>Activating needs a system and starts a scan; deactivating drops the scan.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetActive(bool Value)
    {
        if (Active != Value)
        {
            fActive = Value;
            if (Value)
            {
                if (BoldSystem is not null)
                {
                    StartScan();
                }
                else
                {
                    throw new EBold(SysUtils.Format(sNeedSystemToActivate, GetType().Name));
                }
            }
            else
            {
                fTraverser?.Dispose();
                fTraverser = null;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystem(TBoldSystem? Value)
    {
        Active = false;
        fBoldSystem = Value;
    }

    [Delphi(PortStatus.Ported)]
    private void StartScan()
    {
        System.Diagnostics.Debug.Assert(fTraverser is null);
        fTraverser = BoldSystem!.Locators.CreateTraverser();
    }

    [Delphi(PortStatus.Ported)]
    private void StartWait()
    {
        fTraverser?.Dispose();
        fTraverser = null;
        fWaitCount = 0;
    }

    /// <summary>Scans on, or counts a waiting tick and starts a new scan after MinAgeForUnload ticks.</summary>
    [Delphi(PortStatus.Ported)]
    public void Tick()
    {
        if (Active)
        {
            if (fTraverser is not null)
            {
                Scan();
                if (fTraverser.EndOfList)
                {
                    StartWait();
                }
            }
            else
            {
                fWaitCount++;
                if (fWaitCount > MinAgeForUnload)
                {
                    StartScan();
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystem? BoldSystem
    {
        get => fBoldSystem;
        set => SetBoldSystem(value);
    }

    [Delphi(PortStatus.Ported)] public int ScanPerTick { get => fScanPerTick; set => fScanPerTick = value; }
    [Delphi(PortStatus.Ported)] public int MinAgeForUnload { get => fMinAgeForUnload; set => fMinAgeForUnload = value; }
    [Delphi(PortStatus.Ported)] public bool UnloadDelayedFetch { get => fUnloadDelayedFetch; set => fUnloadDelayedFetch = value; }

    [Delphi(PortStatus.Ported)]
    public bool Active
    {
        get => fActive;
        set => SetActive(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldInvalidateMemberEvent? OnMayInvalidate { get => fOnMayInvalidate; set => fOnMayInvalidate = value; }
    [Delphi(PortStatus.Ported)] public TBoldUnloadObjectEvent? OnMayUnload { get => fOnMayUnload; set => fOnMayUnload = value; }
}
