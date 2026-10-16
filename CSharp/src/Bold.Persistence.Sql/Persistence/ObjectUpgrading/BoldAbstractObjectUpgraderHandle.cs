// Delphi: Source/Persistence/ObjectUpgrading/BoldAbstractObjectUpgraderHandle.pas
// Port map: CSharp/port/units/BoldAbstractObjectUpgraderHandle.md
// A handle with an object upgrader and its configuration (which classes to upgrade below which model version).

namespace Bold;

/// <summary>A handle that makes an object upgrader on first use and keeps its configuration.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractObjectUpgraderHandle(System.ComponentModel.IContainer? Owner = null) : TBoldHandle(Owner)
{
    private TBoldObjectUpgraderConfiguration? fConfig;
    private TBoldAbstractObjectUpgrader? fObjectUpgrader;

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractObjectUpgraderHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fObjectUpgrader = null;
            fConfig = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported, Note = "The configuration has no owner here (the handle is not a TPersistent in .NET).")]
    private TBoldObjectUpgraderConfiguration GetConfig() => fConfig ??= (TBoldObjectUpgraderConfiguration)Activator.CreateInstance(ConfigClass(), [null])!;

    [Delphi(PortStatus.Ported)]
    private void SetConfig(TBoldObjectUpgraderConfiguration Value)
    {
        Config.Clear();
        for (int i = 0; i < Value.Count; i++)
        {
            Config.Add().Assign(Value[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractObjectUpgrader GetObjectUpgrader() => fObjectUpgrader ??= CreateObjectUpgrader();

    [Delphi(PortStatus.Ported)]
    protected virtual Type ConfigClass() => typeof(TBoldObjectUpgraderConfiguration);

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldAbstractObjectUpgrader CreateObjectUpgrader();

    [Delphi(PortStatus.Ported)]
    protected override object? GetHandledObject() => ObjectUpgrader;

    [Delphi(PortStatus.Ported)] public TBoldAbstractObjectUpgrader ObjectUpgrader => GetObjectUpgrader();
    [Delphi(PortStatus.Ported)] public TBoldObjectUpgraderConfiguration Config { get => GetConfig(); set => SetConfig(value); }
}
