namespace Bold;

/// <summary>Delphi's TPersistent: an object that can copy itself from another (Assign) and has a name path.</summary>
public class TPersistent
{
    /// <summary>Copies Source into this object; raises EConvertError when Source cannot assign to it either.</summary>
    public virtual void Assign(TPersistent? Source)
    {
        if (Source is not null)
        {
            Source.AssignTo(this);
        }
        else
        {
            AssignError(null);
        }
    }

    /// <summary>Copies this object into Dest; raises unless overridden.</summary>
    protected virtual void AssignTo(TPersistent Dest) => Dest.AssignError(this);

    private void AssignError(TPersistent? Source) =>
        throw new EConvertError($"Cannot assign a {(Source is null ? "nil" : Source.GetType().Name)} to a {GetType().Name}");

    /// <summary>The owner in the name path, or null.</summary>
    protected virtual TPersistent? GetOwner() => null;

    /// <summary>The name used in the object inspector; the class name here.</summary>
    public virtual string GetNamePath() => GetType().Name;
}

/// <summary>Delphi's TCollectionItem: an item of a TCollection, with an index and a unique ID.</summary>
public class TCollectionItem : TPersistent
{
    private TCollection? fCollection;
    private int fID;

    public TCollectionItem(TCollection? Collection)
    {
        SetCollection(Collection);
    }

    /// <summary>The collection; setting it moves the item.</summary>
    public TCollection? Collection
    {
        get => fCollection;
        set => SetCollection(value);
    }

    private void SetCollection(TCollection? Value)
    {
        if (fCollection == Value)
        {
            return;
        }

        fCollection?.RemoveItem(this);
        Value?.InsertItem(this);
    }

    internal void SetCollectionField(TCollection? Value, int ID)
    {
        fCollection = Value;
        fID = ID;
    }

    public int ID => fID;

    /// <summary>The position in the collection, or -1.</summary>
    public virtual int Index
    {
        get => fCollection?.IndexOf(this) ?? -1;
        set
        {
            int current = Index;
            if (current >= 0 && current != value)
            {
                fCollection!.MoveItem(current, value);
                Changed(true);
            }
        }
    }

    /// <summary>The name shown in editors; the class name unless overridden.</summary>
    protected virtual string GetDisplayName() => GetType().Name;

    public string DisplayName => GetDisplayName();

    /// <summary>Tells the collection that this item (or, with AllItems, every item) changed.</summary>
    protected void Changed(bool AllItems) => fCollection?.Changed(AllItems ? null : this);

    protected override TPersistent? GetOwner() => fCollection;

    public override string GetNamePath() =>
        fCollection is not null ? $"{fCollection.GetNamePath()}[{Index}]" : GetType().Name;
}

/// <summary>Delphi's TCollection: an ordered list of items of one item class.</summary>
public class TCollection : TPersistent
{
    private readonly List<TCollectionItem> fItems = [];
    private readonly Type fItemClass;
    private int fNextID;
    private int fUpdateCount;

    public TCollection(Type ItemClass)
    {
        fItemClass = ItemClass;
    }

    public Type ItemClass => fItemClass;
    public int Count => fItems.Count;
    public int UpdateCount => fUpdateCount;

    public TCollectionItem this[int Index]
    {
        get => fItems[Index];
        set => fItems[Index].Assign(value);
    }

    /// <summary>A new item of the item class (whose constructor takes the collection), added at the end.</summary>
    public TCollectionItem Add() => (TCollectionItem)Activator.CreateInstance(fItemClass, this)!;

    public TCollectionItem Insert(int Index)
    {
        TCollectionItem result = Add();
        result.Index = Index;
        return result;
    }

    public void Clear()
    {
        if (fItems.Count == 0)
        {
            return;
        }

        BeginUpdate();
        try
        {
            while (fItems.Count > 0)
            {
                fItems[^1].Collection = null;
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    public void Delete(int Index) => fItems[Index].Collection = null;

    public void BeginUpdate() => fUpdateCount++;

    public void EndUpdate()
    {
        fUpdateCount--;
        Changed(null);
    }

    public TCollectionItem? FindItemID(int ID) => fItems.Find(i => i.ID == ID);

    public int IndexOf(TCollectionItem Item) => fItems.IndexOf(Item);

    /// <summary>Copies the items of another collection (of compatible item class).</summary>
    public override void Assign(TPersistent? Source)
    {
        if (Source is TCollection source)
        {
            BeginUpdate();
            try
            {
                Clear();
                for (int i = 0; i < source.Count; i++)
                {
                    Add().Assign(source[i]);
                }
            }
            finally
            {
                EndUpdate();
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    internal void InsertItem(TCollectionItem Item)
    {
        fItems.Add(Item);
        Item.SetCollectionField(this, fNextID++);
        Notify(Item, TCollectionNotification.cnAdded);
        Changed(null);
    }

    internal void RemoveItem(TCollectionItem Item)
    {
        Notify(Item, TCollectionNotification.cnExtracting);
        fItems.Remove(Item);
        Item.SetCollectionField(null, Item.ID);
        Changed(null);
    }

    internal void MoveItem(int CurIndex, int NewIndex)
    {
        TCollectionItem item = fItems[CurIndex];
        fItems.RemoveAt(CurIndex);
        fItems.Insert(Math.Clamp(NewIndex, 0, fItems.Count), item);
    }

    internal void Changed(TCollectionItem? Item)
    {
        if (fUpdateCount == 0)
        {
            Update(Item);
        }
    }

    /// <summary>Called after a change (Item null for many items); empty here.</summary>
    protected virtual void Update(TCollectionItem? Item)
    {
    }

    /// <summary>Called when an item is added or removed; empty here.</summary>
    protected virtual void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
    }

    public override string GetNamePath() => GetOwner() is { } owner ? owner.GetNamePath() + "." + GetType().Name : GetType().Name;
}

/// <summary>Delphi's TCollectionNotification.</summary>
public enum TCollectionNotification { cnAdded, cnExtracting, cnDeleting }

/// <summary>Delphi's TOwnedCollection: a collection that knows its owner.</summary>
public class TOwnedCollection(TPersistent? AOwner, Type ItemClass) : TCollection(ItemClass)
{
    private readonly TPersistent? fOwner = AOwner;

    protected override TPersistent? GetOwner() => fOwner;

    public TPersistent? Owner => fOwner;
}
