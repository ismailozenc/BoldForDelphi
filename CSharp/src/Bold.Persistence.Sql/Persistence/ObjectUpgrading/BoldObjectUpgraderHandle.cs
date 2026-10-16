// Delphi: Source/Persistence/ObjectUpgrading/BoldObjectUpgraderHandle.pas
// Port map: CSharp/port/units/BoldObjectUpgraderHandle.md
// The handle of the default object upgrader, over a DB persistence handle and the runtime type info. The type info is
// set directly instead of through a TBoldSystemTypeInfoHandle, so Bold.Persistence.Sql does not need Bold.Handles.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldObjectUpgraderHandle", Symbol = "TBoldObjectUpgraderHandle.SystemTypeInfoHandle", Note = "The SystemTypeInfo property takes the runtime type info directly.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldObjectUpgraderHandle", Symbol = "TBoldObjectUpgraderHandle.SetSystemTypeInfoHandle", Note = "The SystemTypeInfo property takes the runtime type info directly.")]

namespace Bold;

/// <summary>Unit-level constants of BoldObjectUpgraderHandle.</summary>
public static class BoldObjectUpgraderHandle
{
    internal const int breSystemTypeInfoHandleDestroying = 100;
    internal const int brePersistenceHandleDestroying = 101;
    internal const int breSystemTypeInfoHandleChanged = 102;
    internal const int brePersistenceHandleDeactivating = 103;
}

/// <summary>A handle with a TBoldObjectUpgrader that upgrades objects through a DB persistence handle.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgraderHandle : TBoldAbstractObjectUpgraderHandle
{
    private TBoldAbstractPersistenceHandleDB? FPersistenceHandle;
    private TBoldSystemTypeInfo? fSystemTypeInfo;
    private readonly TBoldPassthroughSubscriber fComponentSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldObjectUpgraderHandle(System.ComponentModel.IContainer? owner = null) : base(owner)
    {
        fComponentSubscriber = new TBoldPassthroughSubscriber(_ReceiveComponentEvents);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectUpgraderHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fComponentSubscriber.Dispose();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected override Type ConfigClass() => typeof(TBoldObjectUpgraderConfigurationWithEvent);

    [Delphi(PortStatus.Ported)]
    protected override TBoldAbstractObjectUpgrader CreateObjectUpgrader() => new TBoldObjectUpgrader(Config, GetSystemTypeInfo, GetPersistenceController);

    [Delphi(PortStatus.Ported)]
    private TBoldObjectUpgrader GetObjectUpgrader() => (TBoldObjectUpgrader)base.ObjectUpgrader;

    [Delphi(PortStatus.Ported)]
    private TBoldUpgradeEvent? GetOnUpgradeObject() => ObjectUpgrader.OnUpgradeObject;

    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceControllerDefault? GetPersistenceController() => PersistenceHandle?.PersistenceControllerDefault;

    [Delphi(PortStatus.Ported, Note = "Returns the type info set on the handle.")]
    private TBoldSystemTypeInfo? GetSystemTypeInfo() => fSystemTypeInfo;

    /// <summary>Subscribes to the persistence handle being destroyed or deactivated.</summary>
    [Delphi(PortStatus.Ported, Note = "There is no type info handle to subscribe to.")]
    private void PlaceComponentSubscriptions()
    {
        fComponentSubscriber.CancelAllSubscriptions();
        if (FPersistenceHandle is not null)
        {
            FPersistenceHandle.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldObjectUpgraderHandle.brePersistenceHandleDestroying);
            FPersistenceHandle.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDeactivating), BoldObjectUpgraderHandle.brePersistenceHandleDeactivating);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetOnUpgradeObject(TBoldUpgradeEvent? Value) => ObjectUpgrader.OnUpgradeObject = Value;

    [Delphi(PortStatus.Ported)]
    private void SetPersistenceHandle(TBoldAbstractPersistenceHandleDB? Value)
    {
        FPersistenceHandle = Value;
        PlaceComponentSubscriptions();
    }

    /// <summary>Sets the type info; the upgrader's object space is released, as when the type info handle changed.</summary>
    private void SetSystemTypeInfo(TBoldSystemTypeInfo? Value)
    {
        if (fSystemTypeInfo != Value)
        {
            fSystemTypeInfo = Value;
            ObjectUpgrader.ReleaseBoldSystem();
        }
    }

    /// <summary>Forgets a destroyed persistence handle; releases the upgrader's object space.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveComponentEvents(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == BoldObjectUpgraderHandle.brePersistenceHandleDestroying)
        {
            PersistenceHandle = null;
        }

        if (RequestedEvent is BoldObjectUpgraderHandle.breSystemTypeInfoHandleDestroying or BoldObjectUpgraderHandle.brePersistenceHandleDestroying
            or BoldObjectUpgraderHandle.breSystemTypeInfoHandleChanged or BoldObjectUpgraderHandle.brePersistenceHandleDeactivating)
        {
            ObjectUpgrader.ReleaseBoldSystem();
        }
    }

    [Delphi(PortStatus.Ported)] public new TBoldObjectUpgrader ObjectUpgrader => GetObjectUpgrader();
    [Delphi(PortStatus.Ported)] public TBoldAbstractPersistenceHandleDB? PersistenceHandle { get => FPersistenceHandle; set => SetPersistenceHandle(value); }

    /// <summary>The runtime type info of the upgrader's object space (a TBoldSystemTypeInfoHandle in Delphi).</summary>
    public TBoldSystemTypeInfo? SystemTypeInfo { get => fSystemTypeInfo; set => SetSystemTypeInfo(value); }

    [Delphi(PortStatus.Ported)] public TBoldUpgradeEvent? OnUpgradeObject { get => GetOnUpgradeObject(); set => SetOnUpgradeObject(value); }
}
