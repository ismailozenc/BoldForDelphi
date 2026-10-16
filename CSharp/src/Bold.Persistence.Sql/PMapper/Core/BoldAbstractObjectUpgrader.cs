// Delphi: Source/PMapper/Core/BoldAbstractObjectUpgrader.pas
// Port map: CSharp/port/units/BoldAbstractObjectUpgrader.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldAbstractObjectUpgrader", Symbol = "TBoldObjectUpgraderConfigItemClass", Note = "A System.Type of the item class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldAbstractObjectUpgrader", Symbol = "TBoldObjectUpgraderConfigClass", Note = "A System.Type of the configuration class.")]

namespace Bold;

/// <summary>Which class (by expression name) to upgrade when its stored version is older than a given one.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgraderConfigurationItem(TCollection? Collection) : TBoldUniquelyNamedCollectionItemWithNameStorage(Collection)
{
    private int FUpgradeOlderThanVersion;

    [Delphi(PortStatus.Ported)]
    private void SetExpressionName(string Value) =>
        UniqueName = BoldNameExpander.BoldExpandName(Value, "", TExpansionType.xtExpression, -1, TBoldNationalCharConversion.nccDefault);

    [Delphi(PortStatus.Ported)] private void SetUpgradeOlderThanVersion(int Value) => FUpgradeOlderThanVersion = Value;
    [Delphi(PortStatus.Ported)] private TBoldObjectUpgraderConfiguration GetConfig() => (TBoldObjectUpgraderConfiguration)base.Collection!;

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() =>
        SysUtils.Format(BoldPMConsts.sUpgradeIfOlderThan, ExpressionName.Length != 0 ? ExpressionName : BoldPMConsts.sDisplayNameUnassigned, UpgradeOlderThanVersion);

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? source)
    {
        if (source is TBoldObjectUpgraderConfigurationItem item)
        {
            ExpressionName = item.ExpressionName;
            UpgradeOlderThanVersion = item.UpgradeOlderThanVersion;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectUpgraderConfiguration Config => GetConfig();
    [Delphi(PortStatus.Ported)] public string ExpressionName { get => GetUniqueName(); set => SetExpressionName(value); }
    [Delphi(PortStatus.Ported)] public int UpgradeOlderThanVersion { get => FUpgradeOlderThanVersion; set => SetUpgradeOlderThanVersion(value); }
}

/// <summary>The classes to upgrade, by expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgraderConfiguration : TBoldCollectionWithUniquelyNamedItems
{
    /// <summary>A configuration of TBoldObjectUpgraderConfigurationItem items.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectUpgraderConfiguration(TPersistent? Owner)
        : this(Owner, typeof(TBoldObjectUpgraderConfigurationItem))
    {
    }

    /// <summary>For subclasses with another item class (Delphi asks the virtual GetItemClass in the constructor).</summary>
    protected TBoldObjectUpgraderConfiguration(TPersistent? Owner, Type ItemClass)
        : base(Owner, ItemClass)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldObjectUpgraderConfigurationItem GetItems(int i) => (TBoldObjectUpgraderConfigurationItem)this[i];
    [Delphi(PortStatus.Ported)] private TPersistent? GetConfigOwner() => Owner;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectUpgraderConfigurationItem? GetItemByName(string ExpressionName) => (TBoldObjectUpgraderConfigurationItem?)base.ItemByName(ExpressionName);

    [Delphi(PortStatus.Ported, Note = "The item class is passed to the constructor in .NET; this reports it.")]
    protected virtual Type GetItemClass() => ItemClass;

    [Delphi(PortStatus.Ported)] public TPersistent? ConfigOwner => GetConfigOwner();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public new TBoldObjectUpgraderConfigurationItem? ItemByName(string ExpressionName) => GetItemByName(ExpressionName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectUpgraderConfiguration.items")]
    public new TBoldObjectUpgraderConfigurationItem this[int i] => (TBoldObjectUpgraderConfigurationItem)base[i];
}

/// <summary>Upgrades objects stored by older model versions, as configured.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractObjectUpgrader : TBoldMemoryManagedObject
{
    private readonly TBoldObjectUpgraderConfiguration fConfig;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractObjectUpgrader(TBoldObjectUpgraderConfiguration Config)
    {
        fConfig = Config;
    }

    [Delphi(PortStatus.Ported)] public abstract void UpgradeObjectById(TBoldObjectId ObjectId, IBoldQuery Query);

    /// <summary>Whether objects of the class stored at the version must be upgraded.</summary>
    [Delphi(PortStatus.Ported)]
    public bool NeedsManualUpdate(string ExpressionName, int Version) => Config.ItemByName(ExpressionName) is { } anItem && anItem.UpgradeOlderThanVersion > Version;

    [Delphi(PortStatus.Ported)] public abstract void StartTransaction();
    [Delphi(PortStatus.Ported)] public abstract void EndTransaction();
    [Delphi(PortStatus.Ported)] public abstract void FailTransaction();
    [Delphi(PortStatus.Ported)] public TBoldObjectUpgraderConfiguration Config => fConfig;
}
