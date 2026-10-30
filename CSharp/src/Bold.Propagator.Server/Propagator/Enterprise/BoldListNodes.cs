// Delphi: Source/Propagator/Enterprise/BoldListNodes.pas
// Port map: CSharp/port/units/BoldListNodes.md

namespace Bold;

/// <summary>A node of a multi-indexed list: an index node (the head of a key's chain) or a link node.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMultiIndexedListNode
{
}

/// <summary>The head of the chain of link nodes that have one key in one index.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndexNode : TBoldMultiIndexedListNode
{
    [Delphi(PortStatus.Ported)]
    public TBoldIndexNode()
    {
        Next = null;
    }

    /// <summary>The first link node of the chain.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldAbstractLinkNode? Next { get; set; }
}

/// <summary>An index node for an event name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventNameIndexNode : TBoldIndexNode
{
    [Delphi(PortStatus.Ported)]
    public string EventName { get; set; } = "";
}

/// <summary>
/// A node that is linked into one chain per index: in each index its previous node is the node before it or the index
/// node of its key, its next node the node after it.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractLinkNode : TBoldMultiIndexedListNode
{
    [Delphi(PortStatus.Ported)]
    protected internal abstract TBoldMultiIndexedListNode? getNext(int Index);

    [Delphi(PortStatus.Ported)]
    protected internal abstract void setNext(int Index, TBoldMultiIndexedListNode? Value);

    [Delphi(PortStatus.Ported)]
    protected internal abstract TBoldMultiIndexedListNode? getPrevious(int Index);

    [Delphi(PortStatus.Ported)]
    protected internal abstract void setPrevious(int Index, TBoldMultiIndexedListNode? Value);

    /// <summary>The index node at the head of the node's chain in the index; null when the node is not in one.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldIndexNode? getIndexNode(int Index)
    {
        TBoldMultiIndexedListNode? aNode = this;
        while (aNode is TBoldAbstractLinkNode linkNode)
        {
            aNode = linkNode.Previous(Index);
        }

        return (TBoldIndexNode?)aNode;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract int getNumberOfIndices();

    /// <summary>Links <paramref name="NewNode"/> in after this node in the index.</summary>
    [Delphi(PortStatus.Ported)]
    public void InsertAfter(int Index, TBoldAbstractLinkNode NewNode)
    {
        NewNode.setPrevious(Index, this);
        NewNode.setNext(Index, Next(Index));
        (Next(Index) as TBoldAbstractLinkNode)?.setPrevious(Index, NewNode);
        setNext(Index, NewNode);
    }

    /// <summary>Links <paramref name="NewNode"/> in before this node in the index.</summary>
    [Delphi(PortStatus.Ported)]
    public void InsertBefore(int Index, TBoldAbstractLinkNode NewNode)
    {
        NewNode.setNext(Index, this);
        NewNode.setPrevious(Index, Previous(Index));
        if (Previous(Index) is TBoldAbstractLinkNode previous)
        {
            previous.setNext(Index, NewNode);
        }
        else
        {
            ((TBoldIndexNode)Previous(Index)!).Next = NewNode;
        }

        setPrevious(Index, NewNode);
    }

    /// <summary>Unlinks the node from its chains in every index.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Remove()
    {
        for (int Index = 0; Index < NumberOfIndices; Index++)
        {
            (Next(Index) as TBoldAbstractLinkNode)?.setPrevious(Index, Previous(Index));
            TBoldMultiIndexedListNode? Prev = Previous(Index);
            if (Prev is TBoldAbstractLinkNode prevLink)
            {
                prevLink.setNext(Index, Next(Index));
            }
            else if (Prev is TBoldIndexNode prevIndex)
            {
                prevIndex.Next = (TBoldAbstractLinkNode?)Next(Index);
            }

            setNext(Index, null);
            setPrevious(Index, null);
        }
    }

    /// <summary>The next node in the index.</summary>
    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name; setNext writes it.")]
    public TBoldMultiIndexedListNode? Next(int Index) => getNext(Index);

    /// <summary>The previous node in the index (a link node, or the index node of the key).</summary>
    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name; setPrevious writes it.")]
    public TBoldMultiIndexedListNode? Previous(int Index) => getPrevious(Index);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldIndexNode? IndexNode(int Index) => getIndexNode(Index);

    [Delphi(PortStatus.Ported)]
    public int NumberOfIndices => getNumberOfIndices();
}
