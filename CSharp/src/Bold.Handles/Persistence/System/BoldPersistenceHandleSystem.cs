// Delphi: Source/Persistence/System/BoldPersistenceHandleSystem.pas
// Port map: CSharp/port/units/BoldPersistenceHandleSystem.md

using System.ComponentModel;

namespace Bold;

/// <summary>A persistence handle that stores objects in another running system (a system handle's).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceHandleSystem : TBoldPersistenceHandle
{
    private TBoldSystemHandle? fSystemHandle;

    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    public TBoldPersistenceHandleSystem(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceControllerSystem GetPersistenceControllerSystem() => (TBoldPersistenceControllerSystem)PersistenceController;

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController() => new TBoldPersistenceControllerSystem();

    /// <summary>Activating gives the controller the system handle's system.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        if (Value != Active && Value)
        {
            PersistenceControllerSystem.BoldSystem = SystemHandle!.System;
        }

        base.SetActive(Value);
    }

    [Delphi(PortStatus.Ported)] public TBoldPersistenceControllerSystem PersistenceControllerSystem => GetPersistenceControllerSystem();

    [Delphi(PortStatus.Ported)]
    public TBoldSystemHandle? SystemHandle
    {
        get => fSystemHandle;
        set => fSystemHandle = value;
    }
}
