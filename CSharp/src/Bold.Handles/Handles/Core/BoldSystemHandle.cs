// Delphi: Source/Handles/Core/BoldSystemHandle.pas
// Port map: CSharp/port/units/BoldSystemHandle.md

using System.ComponentModel;
using static Bold.BoldRegionDefinitions;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSystemHandle", Symbol = "TBoldSystemHandle.DefineProperties", Note = "Read the old TrackBold property from Delphi form files; there are no form files in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSystemHandle", Symbol = "TBoldSystemHandle.ReadTrackBold", Note = "Skipped the old TrackBold property of a Delphi form file.")]

namespace Bold;

/// <summary>
/// The handle that runs a system: activating it creates the system from the type info handle's model, with the
/// persistence handle's controller and the model's lock regions; deactivating it destroys the system.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemHandle : TBoldAbstractSystemHandle
{
    private const int brePersistenceHandleDestroying = 100;
    private const int breRegiondefinitionsDestroying = 101;
    private const int breRegionDefinitionClearing = 102;

    private TBoldSystem? fBoldSystem;
    private bool fStreamedActive;
    private bool fAutoActivate;
    private TBoldPersistenceHandle? fPersistenceHandle;
    private TBoldPassthroughSubscriber? fPersistenceHandleSubscriber;
    private TBoldPassthroughSubscriber? fRegionDefinitionSubscriber;
    private TBoldLookUpOclDefinition? fOnLookupOclDefinition;
    private TBoldOptimisticLockingFailedEvent? fOnOptimisticLockingFailed;
    private TNotifyEvent? FOnPreUpdate;
    private TBoldRegionFactory? fRegionFactory;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldSystemHandle(IContainer? owner = null)
        : base(owner)
    {
        fPersistenceHandleSubscriber = new TBoldPassthroughSubscriber(_ReceivePersistenceHandle);
        fRegionDefinitionSubscriber = new TBoldPassthroughSubscriber(_ReceiveRegionDefinitions);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSystemHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fBoldSystem);
            SysUtils.FreeAndNil(ref fPersistenceHandleSubscriber);
            SysUtils.FreeAndNil(ref fRegionDefinitionSubscriber);
            SysUtils.FreeAndNil(ref fRegionFactory);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Discards and closes a running system whose model, persistence handle or regions went away; raises when dirty
    /// objects were lost (in a designer the environment shows the error).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi read the dirty object count from the closed system for its message, which failed; this uses the count taken before.")]
    private void PanicShutDownSystem(string Message)
    {
        // if we are destroying, then we can just ignore this problem
        if (IsDestroying)
        {
            return;
        }

        try
        {
            int DirtyCount = System!.DirtyObjects.Count;
            System.Discard();
            Active = false;
            if (DirtyCount > 0)
            {
                throw new EBold(SysUtils.Format(sPanicShutDown, Message, BoldDefs.BOLDCRLF, DirtyCount));
            }
        }
        catch (Exception) when (BoldEnvironment.BoldEffectiveEnvironment().RunningInIDE)
        {
            BoldEnvironment.BoldEffectiveEnvironment().HandleDesigntimeException(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _ReceivePersistenceHandle(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == brePersistenceHandleDestroying)
        {
            PersistenceHandle = null;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Delphi tested for beRegionDefinitionClearing, the event, where the subscription asked for breRegionDefinitionClearing, so clearing the definitions went unnoticed; this tests for the requested event.")]
    private void _ReceiveRegionDefinitions(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent is breRegiondefinitionsDestroying or breRegionDefinitionClearing)
        {
            if (Active)
            {
                PanicShutDownSystem(sRegionDefinitionsRemovedFromRunningSystem);
            }

            SysUtils.FreeAndNil(ref fRegionFactory);
        }
    }

    [Delphi(PortStatus.Ported)] private bool GetAutoActivate() => fAutoActivate;

    /// <summary>Sets AutoActivate; while loading (outside a designer) it also activates the system, at Loaded.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetAutoActivate(bool Value)
    {
        if (Value != fAutoActivate)
        {
            fAutoActivate = Value;
            // During read AutoActivate means Activate!
            if (IsLoading && !IsDesigning)
            {
                Active = AutoActivate;
            }
        }
    }

    /// <summary>Sets the persistence handle; changing it under a running system shuts the system down.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetPersistenceHandle(TBoldPersistenceHandle? NewHandle)
    {
        if (NewHandle != fPersistenceHandle)
        {
            fPersistenceHandleSubscriber!.CancelAllSubscriptions();
            if (Active)
            {
                PanicShutDownSystem(sPersistenceHandleChangedOnRunningSystem);
            }

            fPersistenceHandle = NewHandle;
            fPersistenceHandle?.AddSmallSubscription(fPersistenceHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), brePersistenceHandleDestroying);
        }
    }

    [Delphi(PortStatus.Ported)] private bool GetPersistent() => PersistenceHandle is not null;

    [Delphi(PortStatus.Ported)]
    private void SetOnOptimisticLockingFailed(TBoldOptimisticLockingFailedEvent? Value)
    {
        fOnOptimisticLockingFailed = Value;
        if (Active)
        {
            System!.OnOptimisticLockingFailed = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetOnPreUpdate(TNotifyEvent? Value)
    {
        FOnPreUpdate = Value;
        if (Active)
        {
            fBoldSystem!.OnPreUpdate = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue() => fBoldSystem;

    /// <summary>Activates the system at the end of loading when Active or AutoActivate was set.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Loaded()
    {
        base.Loaded();
        try
        {
            if (fStreamedActive || (!IsDesigning && AutoActivate))
            {
                SetActive(true);
            }
        }
        catch (Exception) when (IsDesigning)
        {
            BoldEnvironment.BoldEffectiveEnvironment().HandleDesigntimeException(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldSystem? GetSystem() => fBoldSystem;

    /// <summary>
    /// Starts or stops the system. Starting needs a runnable type info; it activates the persistence handle and makes
    /// the region factory. While loading this only notes the value, and Loaded activates.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        // During read Activation is deferred to Loaded
        if (IsLoading)
        {
            fStreamedActive = Value;
            return;
        }

        if (Value == Active)
        {
            return;
        }

        if (Value)
        {
            if (SystemTypeInfoHandle is null)
            {
                throw new EBold(SysUtils.Format(sUnableToActivateSystemWithoutTypeInfoHandle, Name));
            }

            if (StaticSystemTypeInfo is null)
            {
                throw new EBold(sUnableToFindTypeInfoHandle);
            }

            if (!StaticSystemTypeInfo.SystemIsRunnable)
            {
                throw new EBold(SysUtils.Format(sUnableToActivateSystem, BoldDefs.BOLDCRLF, StaticSystemTypeInfo.InitializationLog.Text));
            }

            TBoldPersistenceController? PController = null;
            if (Persistent)
            {
                PersistenceHandle!.Active = true;
                PController = PersistenceHandle.PersistenceController;
            }

            if (SystemTypeInfoHandle.RegionDefinitions is { } regionDefinitions && fRegionFactory is null)
            {
                regionDefinitions.AddSmallSubscription(fRegionDefinitionSubscriber, TBoldSmallEventSet.Of(beDestroying), breRegiondefinitionsDestroying);
                regionDefinitions.AddSmallSubscription(fRegionDefinitionSubscriber, TBoldSmallEventSet.Of(beRegionDefinitionClearing), breRegionDefinitionClearing);
                fRegionFactory = new TBoldRegionFactory(regionDefinitions);
            }

            try
            {
                fBoldSystem = new TBoldSystem(null, StaticSystemTypeInfo, PController, fRegionFactory);
            }
            catch
            {
                fBoldSystem = null; // Make sure we're not active
                SysUtils.FreeAndNil(ref fRegionFactory);
                if (Persistent)
                {
                    PersistenceHandle!.Active = false;
                }

                throw;
            }

            fBoldSystem.Evaluator!.SetLookupOclDefinition(fOnLookupOclDefinition);
            fBoldSystem.IsDefault = IsDefault;
            fBoldSystem.OnPreUpdate = OnPreUpdate;
            fBoldSystem.OnOptimisticLockingFailed = OnOptimisticLockingFailed;
        }
        else
        {
            if (Persistent)
            {
                PersistenceHandle!.Active = false;
            }

            fBoldSystem!.EnsureCanDestroy(); // raises when the destruction is constrained
            SysUtils.FreeAndNil(ref fBoldSystem);
            SysUtils.FreeAndNil(ref fRegionFactory);
        }

        SendEvent(this, beValueIdentityChanged);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetActive() => fBoldSystem is not null;

    /// <summary>Restarts a running system on the new model (after discarding it), else tells subscribers the type changed.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void ModelChanged()
    {
        if (Active)
        {
            PanicShutDownSystem(sModelChangedOnRunningSystem);
        }

        bool WasActive = Active;
        Active = false;
        if (WasActive)
        {
            Active = true;
        }
        else
        {
            SendEvent(this, beValueIdentityChanged); // type change regarded as identity change
        }
    }

    /// <summary>Saves the system's dirty objects.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateDatabase()
    {
        if (System is null)
        {
            throw new EBold(SysUtils.Format(sCannotUpdateDatebaseWithoutSystem, GetType().Name, Name));
        }

        System.UpdateDatabase();
    }

    /// <summary>Sets how named OCL definitions are looked up, in the type info and the running system.</summary>
    [Delphi(PortStatus.Ported)]
    public void InstallOclDefinitionLookUp(TBoldLookUpOclDefinition? Value)
    {
        fOnLookupOclDefinition = Value;
        SystemTypeInfoHandle?.InstallOclDefinitionLookUp(Value);
        if (Active)
        {
            System!.Evaluator!.SetLookupOclDefinition(Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is not null && Component == PersistenceHandle);

    [Delphi(PortStatus.Ported)] public bool Persistent => GetPersistent();
    [Delphi(PortStatus.Ported)] public TBoldRegionFactory? RegionFactory => fRegionFactory;

    [Delphi(PortStatus.Ported)]
    public bool AutoActivate
    {
        get => GetAutoActivate();
        set => SetAutoActivate(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceHandle? PersistenceHandle
    {
        get => fPersistenceHandle;
        set => SetPersistenceHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnPreUpdate
    {
        get => FOnPreUpdate;
        set => SetOnPreUpdate(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOptimisticLockingFailedEvent? OnOptimisticLockingFailed
    {
        get => fOnOptimisticLockingFailed;
        set => SetOnOptimisticLockingFailed(value);
    }
}
