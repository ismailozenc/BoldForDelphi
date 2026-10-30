// Delphi: Source/Propagator/Enterprise/BoldIndexList.pas
// Port map: CSharp/port/units/BoldIndexList.md
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldIndexList", Symbol = "TBoldIndexListClass", Note = "A System.Type of a TBoldIndexList subclass, created with its (IndexOrder) constructor.")]

namespace Bold;

/// <summary>
/// One index of a multi-indexed list: an index node per key, each heading the chain of the link nodes that have the
/// key. Keys are integers or strings (Delphi variants).
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldIndexList
{
    private readonly int fIndexOrder;

    /// <summary>An index that is the <paramref name="IndexOrder"/>th of its list (the link nodes' index for it).</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldIndexList(int IndexOrder)
    {
        fIndexOrder = IndexOrder;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldIndexNode? getItem(object Key);

    [Delphi(PortStatus.Ported)]
    protected abstract void setItem(object Key, TBoldIndexNode Value);

    [Delphi(PortStatus.Ported)]
    protected abstract int getCount();

    /// <summary>The index node of the key, created if it does not exist.</summary>
    [Delphi(PortStatus.Ported)]
    protected abstract TBoldIndexNode InsertINode(object Key);

    /// <summary>Unlinks and drops every link node of the index node's chain.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DeleteINode(TBoldIndexNode iNode)
    {
        if (iNode.Next is not null)
        {
            TBoldAbstractLinkNode aNode = iNode.Next;
            while (aNode.Next(IndexOrder) is TBoldAbstractLinkNode nextNode)
            {
                aNode.Remove();
                aNode = nextNode;
            }

            aNode.Remove();
            iNode.Next = null;
        }
    }

    /// <summary>Links <paramref name="NewNode"/> in first in the chain of the key.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void InsertNode(object Key, TBoldAbstractLinkNode NewNode)
    {
        TBoldIndexNode iNode = this[Key] ?? InsertINode(Key);
        NewNode.setPrevious(IndexOrder, iNode);
        NewNode.setNext(IndexOrder, iNode.Next);
        iNode.Next?.setPrevious(IndexOrder, NewNode);
        iNode.Next = NewNode;
    }

    /// <summary>Drops the index node of the key; subclasses that keep index nodes override it.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DeleteINodeByKey(object Key)
    {
    }

    /// <summary>Unlinks the link nodes of every key.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void DeleteNodes();

    /// <summary>Unlinks every link node that has the key (from all indexes) and drops the key's index node.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveKey(object Key)
    {
        TBoldIndexNode? iNode = this[Key];
        while (iNode?.Next is { } aNode)
        {
            aNode.Remove();
        }

        if (iNode is not null)
        {
            iNode.Next = null;
            DeleteINodeByKey(Key);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldIndexNode? this[object Key]
    {
        get => getItem(Key);
        set => setItem(Key, value!);
    }

    [Delphi(PortStatus.Ported)]
    public int Count => getCount();

    [Delphi(PortStatus.Ported)]
    public int IndexOrder => fIndexOrder;
}

/// <summary>A list of link nodes with several indexes: each node is in one chain per index, under its key there.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractMultiIndexedList
{
    private int fNumberOfIndices;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractMultiIndexedList()
    {
        fNumberOfIndices = 0;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldIndexList? getIndexList(int Index);

    [Delphi(PortStatus.Ported)]
    protected abstract void setIndexList(int Index, TBoldIndexList Value);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    protected TBoldIndexList Indices(int Index) => getIndexList(Index)!;

    /// <summary>Adds an index of the given TBoldIndexList subclass; returns its order.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the subclass's System.Type.")]
    public int AddIndex(Type IndexListClass)
    {
        setIndexList(fNumberOfIndices, (TBoldIndexList)Activator.CreateInstance(IndexListClass, fNumberOfIndices)!);
        int result = fNumberOfIndices;
        fNumberOfIndices++;
        return result;
    }

    /// <summary>Links the node in under its key in each index (the keys in index order).</summary>
    [Delphi(PortStatus.Ported)]
    public void AddNode(object[] Key, TBoldAbstractLinkNode NewNode)
    {
        for (int i = 0; i < NumberOfIndices; i++)
        {
            Indices(i).InsertNode(Key[i], NewNode);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveKey(int IndexOrder, object Key) => Indices(IndexOrder).RemoveKey(Key);

    [Delphi(PortStatus.Ported)]
    public int NumberOfIndices => fNumberOfIndices;
}
