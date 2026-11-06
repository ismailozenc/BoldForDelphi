// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMILinkSupport.pas
// Port map: CSharp/port/units/BoldUMLXMILinkSupport.md
// The XML is read with System.Xml where Delphi used MSXML.

using System.Xml;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLXMILinkSupport", Symbol = "TIdIndex", Note = "TBoldXMIIObjectList finds items by id in a dictionary.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLXMILinkSupport", Symbol = "TIdIndex.ItemAsKeyString", Note = "The dictionary is keyed by the id.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLXMILinkSupport", Symbol = "TUUIdIndex", Note = "TBoldXMIIObjectList finds items by uuid in a dictionary.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLXMILinkSupport", Symbol = "TUUIdIndex.ItemAsKeyString", Note = "The dictionary is keyed by the uuid.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLXMILinkSupport", Symbol = "TBoldUMLElementArray.Items", Note = "The list's indexer.")]

namespace Bold;

/// <summary>An element's id in an XMI file: its xmi.id, its xmi.uuid, or both.</summary>
[Delphi(PortStatus.Ported)]
public record struct TXMIId(string Id, string UUId)
{
    public string Id { get; set; } = Id ?? "";
    public string UUId { get; set; } = UUId ?? "";
}

/// <summary>The elements of the UML graph one XMI element became.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLElementArray : List<TBoldUMLObject>
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLElementArray.Get")]
    public TBoldUMLObject Get(int Index) => this[Index];
}

/// <summary>An XMI id and the elements made of the XMI element that has it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMIObjectItem
{
    [Delphi(PortStatus.Ported)]
    public TBoldXMIObjectItem(TXMIId ID)
    {
        XMIId = ID;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMIObjectItem.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported)] public TXMIId XMIId { get; }
    [Delphi(PortStatus.Ported)] public TBoldUMLElementArray UMLobjects { get; } = [];
}

/// <summary>The elements made while importing, found by XMI id or uuid.</summary>
[Delphi(PortStatus.Ported, Note = "Holds the items in a list with two dictionaries, where Delphi used an indexable list with two hash indexes.")]
public class TBoldXMIIObjectList
{
    private readonly List<TBoldXMIObjectItem> fItems = [];
    private readonly Dictionary<string, TBoldXMIObjectItem> fById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TBoldXMIObjectItem> fByUUId = new(StringComparer.Ordinal);

    [Delphi(PortStatus.Ported)]
    public TBoldXMIIObjectList()
    {
    }

    [Delphi(PortStatus.Ported)]
    public TBoldUMLElementArray Add(TXMIId Id, TBoldUMLObject Obj1, TBoldUMLObject? Obj2 = null)
    {
        var newItem = new TBoldXMIObjectItem(Id);
        newItem.UMLobjects.Add(Obj1);
        if (Obj2 is not null)
        {
            newItem.UMLobjects.Add(Obj2);
        }

        fItems.Add(newItem);
        if (Id.Id != "")
        {
            fById.TryAdd(Id.Id, newItem);
        }

        if (Id.UUId != "")
        {
            fByUUId.TryAdd(Id.UUId, newItem);
        }

        return newItem.UMLobjects;
    }

    public int Count => fItems.Count;

    [Delphi(PortStatus.Ported)]
    private TBoldXMIObjectItem GetItem(int index) => fItems[index];

    [Delphi(PortStatus.Ported)]
    private TBoldXMIObjectItem? GetItemById(TXMIId XMIId)
    {
        TBoldXMIObjectItem? result = null;
        if (XMIId.Id != "")
        {
            fById.TryGetValue(XMIId.Id, out result);
        }

        if (result is null && XMIId.UUId != "")
        {
            fByUUId.TryGetValue(XMIId.UUId, out result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUMLElementArray? GetElementsById(TXMIId XMIId) => GetItemById(XMIId)?.UMLobjects;

    [Delphi(PortStatus.Ported)] public TBoldXMIObjectItem this[int index] => GetItem(index);
    [Delphi(PortStatus.Ported)] public TBoldXMIObjectItem? ItemsById(TXMIId XMIId) => GetItemById(XMIId);
    [Delphi(PortStatus.Ported)] public TBoldUMLElementArray? UMLElementsById(TXMIId XMIId) => GetElementsById(XMIId);
}

/// <summary>Functions of BoldUMLXMILinkSupport.</summary>
public static class BoldUMLXMILinkSupport
{
    [Delphi(PortStatus.Ported, Note = "Delphi wrote the id for a uuid too; this writes the uuid.")]
    public static string FormatId(TXMIId id)
    {
        string result = "";
        if (id.Id != "")
        {
            result = $"id:{id.Id} ";
        }

        if (id.UUId != "")
        {
            result = $"UUid:{id.UUId} ";
        }

        return result;
    }

    /// <summary>The first element of a node list, from the start.</summary>
    [Delphi(PortStatus.Ported)]
    public static XmlElement? FirstElementFromNodeList(IEnumerable<XmlNode> List) => List.OfType<XmlElement>().FirstOrDefault();

    [Delphi(PortStatus.Replaced, Note = "Node lists are enumerated; FirstElementFromNodeList and foreach over OfType<XmlElement>() take its place.")]
    public static XmlElement? NextElementFromNodeList(IEnumerator<XmlNode> List)
    {
        while (List.MoveNext())
        {
            if (List.Current is XmlElement element)
            {
                return element;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public static TXMIId GetXMIId(XmlElement UMLElementNode) =>
        new(UMLElementNode.GetAttribute("xmi.id"), UMLElementNode.GetAttribute("xmi.uuid"));

    [Delphi(PortStatus.Ported)]
    public static bool IsNonBlank(TXMIId XMIId) => XMIId.Id != "" || XMIId.UUId != "";

    [Delphi(PortStatus.Ported)]
    public static TXMIId GetXMIIdRef(XmlElement UMLElementNode) =>
        new(UMLElementNode.GetAttribute("xmi.idref"), UMLElementNode.GetAttribute("xmi.uuidref"));

    [Delphi(PortStatus.Ported)]
    public static TXMIId XMIIdforId(string id) => new(id, "");
}
