// Delphi: Source/Common/Support/BoldCollections.pas
// Port map: CSharp/port/units/BoldCollections.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldCollections", Symbol = "TBoldUniquelyNamedCollectionItemClass", Note = "A System.Type of the item class.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldCollections", Symbol = "TBoldCollectionWithUniquelyNamedItems.Destroy", Note = "Freed the name index; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Indexes collection items by their unique name (case insensitive).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUniqueNameItemIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldUniquelyNamedCollectionItem)Item!).UniqueName;
}

/// <summary>A collection item with a name that is unique in its collection; renaming to a used name raises.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldUniquelyNamedCollectionItem(TCollection? Collection) : TCollectionItem(Collection)
{
    [Delphi(PortStatus.Ported)]
    private void InternalSetUniqueName(string Value)
    {
        if (Value != UniqueName)
        {
            EnsureNameUnique(Value);
            this.Collection.ItemIndex.Remove(this);
            SetUniqueName(Value);
            this.Collection.ItemIndex.Add(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void EnsureNameUnique(string Value)
    {
        if (this.Collection.ItemByName(Value) is not null)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sDuplicateName, Value));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldCollectionWithUniquelyNamedItems GetCollection() => (TBoldCollectionWithUniquelyNamedItems)base.Collection!;

    [Delphi(PortStatus.Ported)] protected override string GetDisplayName() => UniqueName;
    [Delphi(PortStatus.Ported)] protected abstract string GetUniqueName();
    [Delphi(PortStatus.Ported)] protected abstract void SetUniqueName(string Value);

    [Delphi(PortStatus.Ported)]
    protected internal string UniqueName
    {
        get => GetUniqueName();
        set => InternalSetUniqueName(value);
    }

    [Delphi(PortStatus.Ported)] protected new TBoldCollectionWithUniquelyNamedItems Collection => GetCollection();

    [Delphi(PortStatus.Ported)]
    public override string GetNamePath() =>
        base.Collection is not null ? SysUtils.Format("%s.%s", base.Collection.GetNamePath(), UniqueName) : GetType().Name;
}

/// <summary>A uniquely named item that keeps its name in a field.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUniquelyNamedCollectionItemWithNameStorage(TCollection? Collection) : TBoldUniquelyNamedCollectionItem(Collection)
{
    private string fUniqueName = "";

    [Delphi(PortStatus.Ported)] protected override string GetUniqueName() => fUniqueName;
    [Delphi(PortStatus.Ported)] protected override void SetUniqueName(string Value) => fUniqueName = Value;
}

/// <summary>An owned collection of uniquely named items, found by name through an index rebuilt after changes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCollectionWithUniquelyNamedItems : TOwnedCollection
{
    private TBoldUniqueNameItemIndex? fItemIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldCollectionWithUniquelyNamedItems(TPersistent? AOwner, Type ItemClass)
        : base(AOwner, ItemClass)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUniquelyNamedCollectionItem? GetItemByName(string Name) => (TBoldUniquelyNamedCollectionItem?)ItemIndex.FindByString(Name);

    [Delphi(PortStatus.Ported)]
    private TBoldUniqueNameItemIndex GetItemIndex()
    {
        if (fItemIndex is null)
        {
            fItemIndex = new TBoldUniqueNameItemIndex();
            for (int i = 0; i < Count; i++)
            {
                fItemIndex.Add(this[i]);
            }
        }

        return fItemIndex;
    }

    /// <summary>Drops the index; it is rebuilt on the next lookup.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Update(TCollectionItem? Item) => fItemIndex = null;

    [Delphi(PortStatus.Ported)] protected internal TBoldUniqueNameItemIndex ItemIndex => GetItemIndex();

    [Delphi(PortStatus.Ported, Symbol = "TBoldCollectionWithUniquelyNamedItems.ItemByName", Note = "An indexed property in Delphi.")]
    public TBoldUniquelyNamedCollectionItem? ItemByName(string Name) => GetItemByName(Name);
}
