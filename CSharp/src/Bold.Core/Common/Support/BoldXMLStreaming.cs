// Delphi: Source/Common/Support/BoldXMLStreaming.pas
// Port map: CSharp/port/units/BoldXMLStreaming.md

using System.Globalization;
using System.Xml;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldXMLStreaming", Symbol = "initialization", Note = "The unit's initialization section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldXMLStreaming", Symbol = "finalization", Note = "Freed the main registry; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Named objects shared by the nodes of one streaming operation.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldXMLStreamStateManager.Destroy", Note = "Freed the name list; the garbage collector does that in .NET.")]
public class TBoldXMLStreamStateManager : TBoldMemoryManagedObject
{
    private readonly TStringList fStateObjectList;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamStateManager()
    {
        fStateObjectList = new TStringList { Sorted = true };
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEmpty() => fStateObjectList.Count == 0;

    [Delphi(PortStatus.Ported)]
    public void AddObjectByName(string Name, object? Obj)
    {
        System.Diagnostics.Debug.Assert(fStateObjectList.IndexOf(Name) == -1, "There is already a stateobject with the name " + Name);
        fStateObjectList.AddObject(Name, Obj);
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveObjectByName(string Name)
    {
        int pos = fStateObjectList.IndexOf(Name);
        if (pos != -1)
        {
            fStateObjectList.Delete(pos);
        }
    }

    [Delphi(PortStatus.Ported)]
    public object? GetObjectByName(string Name)
    {
        int pos = fStateObjectList.IndexOf(Name);
        return pos != -1 ? fStateObjectList.Objects(pos) : null;
    }

    [Delphi(PortStatus.Ported)]
    public bool Empty => GetEmpty();
}

/// <summary>
/// An element of a Bold XML (SOAP-style) document, with typed reads and writes of its content and sub-elements. Floats use
/// '.' as decimal separator; dates and times are written as Year, Month, Day, Hour, Min, Sec and MSec sub-elements.
/// </summary>
[Delphi(PortStatus.Ported, Note = "System.Xml where Delphi uses MSXML.")]
public class TBoldXMLNode : TBoldMemoryManagedObject
{
    private const string BoldNodeName_Year = "Year";
    private const string BoldNodeName_Month = "Month";
    private const string BoldNodeName_Day = "Day";
    private const string BoldNodeName_Hour = "Hour";
    private const string BoldNodeName_Min = "Min";
    private const string BoldNodeName_Sec = "Sec";
    private const string BoldNodeName_MSec = "MSec";

    private readonly XmlElement fNode;
    private readonly TBoldXMLStreamManager fManager;
    private TBoldXMLStreamStateManager? fStateManager;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode(TBoldXMLStreamManager Manager, XmlElement Node, TBoldXMLStreamStateManager? StreamStateManager)
    {
        fNode = Node;
        fManager = Manager;
        fStateManager = StreamStateManager;
    }

    /// <summary>Writes the dynamic stream name as xsi:type when it differs from the static one.</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureType(string DynamicStreamName, string StaticStreamName)
    {
        if (DynamicStreamName != StaticStreamName)
        {
            BoldXMLStreaming.SetAttribute(fNode, BoldXMLStreaming.BoldSOAPTypeAttributeName, DynamicStreamName);
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetType(string StaticStreamName)
    {
        XmlAttribute? attr = fNode.GetAttributeNode(BoldXMLStreaming.BoldSOAPTypeAttributeName);
        return attr is not null ? attr.Value : StaticStreamName;
    }

    [Delphi(PortStatus.Ported)]
    private XmlDocument GetDocument() => fNode.OwnerDocument;

    [Delphi(PortStatus.Ported)]
    private XmlDocument Document => GetDocument();

    [Delphi(PortStatus.Ported)]
    private string GetAccessor() => fNode.Name;

    [Delphi(PortStatus.Ported)]
    private bool GetIsNull() => XMLDomElement.GetAttributeNode(BoldXMLStreaming.BoldSOAPNullAttributeName)?.Value == "1";

    /// <summary>The direct child element named <paramref name="Accessor"/>, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode? GetSubNode(string Accessor)
    {
        foreach (XmlNode node in fNode.ChildNodes)
        {
            if (node is XmlElement element && element.Name == Accessor)
            {
                return new TBoldXMLNode(fManager, element, fStateManager);
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsEmpty() => !XMLDomElement.HasChildNodes;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode NewSubNode(string Accessor)
    {
        XmlElement node = Document.CreateElement(Accessor);
        fNode.AppendChild(node);
        return new TBoldXMLNode(fManager, node, fStateManager);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode MakeNodeForElement(XmlElement Element) => new(Manager, Element, fStateManager);

    [Delphi(PortStatus.Ported)] public bool ReadBoolean() => ReadString() == "1";

    [Delphi(PortStatus.Ported)]
    public void ReadInterface(string StreamName, IBoldStreamable Item) =>
        fManager.Registry.GetInterfaceStreamer(GetType(StreamName)).ReadInterface(Item, this);

    [Delphi(PortStatus.Ported)] public int ReadInteger() => SysUtils.StrToIntDef(ReadString(), 0);

    /// <summary>A new object made by the streamer of the node's type and read from the node; null for a null node.</summary>
    [Delphi(PortStatus.Ported)]
    public object? ReadObject(string StreamName)
    {
        if (IsNull)
        {
            return null;
        }

        TBoldXMLObjectStreamer streamer = fManager.Registry.GetObjectStreamer(GetType(StreamName));
        object result = streamer.CreateObject();
        streamer.ReadObject(result, this);
        return result;
    }

    /// <summary>The text of the first child when it is a text node, else "".</summary>
    [Delphi(PortStatus.Ported)]
    public string ReadString() => fNode.FirstChild is XmlText text ? text.Value ?? "" : "";

    /// <summary>The element's text as a number with '.' decimals; 0 when it is not a number.</summary>
    [Delphi(PortStatus.Ported)]
    public double ReadFloat()
    {
        BoldXMLStreaming.PushFloatSettings();
        try
        {
            return SysUtils.TryStrToFloat(fNode.InnerText, out double value) ? value : 0;
        }
        finally
        {
            BoldXMLStreaming.PopFloatSettings();
        }
    }

    [Delphi(PortStatus.Ported, Note = "decimal where Delphi has Currency.")]
    public decimal ReadCurrency()
    {
        BoldXMLStreaming.PushFloatSettings();
        try
        {
            return decimal.TryParse(fNode.InnerText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
        }
        finally
        {
            BoldXMLStreaming.PopFloatSettings();
        }
    }

    private T ReadSub<T>(string accessor, Func<TBoldXMLNode, T> read) => read(GetSubNode(accessor)!);

    private void WriteSub(string accessor, Action<TBoldXMLNode> write) => write(NewSubNode(accessor));

    [Delphi(PortStatus.Ported)] public bool ReadSubNodeBoolean(string Accessor) => ReadSub(Accessor, n => n.ReadBoolean());
    [Delphi(PortStatus.Ported)] public int ReadSubNodeInteger(string Accessor) => ReadSub(Accessor, n => n.ReadInteger());
    [Delphi(PortStatus.Ported)] public object? ReadSubNodeObject(string Accessor, string StreamName) => ReadSub(Accessor, n => n.ReadObject(StreamName));
    [Delphi(PortStatus.Ported)] public string ReadSubNodeString(string Accessor) => ReadSub(Accessor, n => n.ReadString());
    [Delphi(PortStatus.Ported)] public double ReadSubNodeFloat(string Accessor) => ReadSub(Accessor, n => n.ReadFloat());

    [Delphi(PortStatus.Ported)] public void WriteBoolean(bool Value) => WriteString(Value ? "1" : "0");
    [Delphi(PortStatus.Ported)] public void WriteInteger(int Value) => WriteString(SysUtils.IntToStr(Value));

    [Delphi(PortStatus.Ported)]
    public void WriteInterface(string StaticStreamName, IBoldStreamable Item)
    {
        string dynamicStreamName = Item.StreamName;
        EnsureType(dynamicStreamName, StaticStreamName);
        fManager.Registry.GetInterfaceStreamer(dynamicStreamName).WriteInterface(Item, this);
    }

    /// <summary>Writes the object with the streamer of its stream name; null writes a null node.</summary>
    [Delphi(PortStatus.Ported)]
    public void WriteObject(string StaticStreamName, TBoldInterfacedObject? Obj)
    {
        if (Obj is not null)
        {
            string dynamicStreamName = ((IBoldStreamable)Obj).StreamName;
            EnsureType(dynamicStreamName, StaticStreamName);
            fManager.Registry.GetObjectStreamer(dynamicStreamName).WriteObject(Obj, this);
        }
        else
        {
            SetToNull();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void WriteString(string Value) => fNode.AppendChild(Document.CreateTextNode(Value));

    [Delphi(PortStatus.Ported)]
    public void WriteFloat(double value)
    {
        BoldXMLStreaming.PushFloatSettings();
        try
        {
            WriteString(SysUtils.FloatToStr(value));
        }
        finally
        {
            BoldXMLStreaming.PopFloatSettings();
        }
    }

    [Delphi(PortStatus.Ported, Note = "decimal where Delphi has Currency.")]
    public void WriteCurrency(decimal value)
    {
        BoldXMLStreaming.PushFloatSettings();
        try
        {
            WriteString(value.ToString("0.####", CultureInfo.InvariantCulture));
        }
        finally
        {
            BoldXMLStreaming.PopFloatSettings();
        }
    }

    [Delphi(PortStatus.Ported)] public void WriteSubNodeBoolean(string Accessor, bool Value) => WriteSub(Accessor, n => n.WriteBoolean(Value));
    [Delphi(PortStatus.Ported)] public void WriteSubNodeInteger(string Accessor, int Value) => WriteSub(Accessor, n => n.WriteInteger(Value));

    [Delphi(PortStatus.Ported)]
    public void WriteSubNodeObject(string Accessor, string StaticStreamName, TBoldInterfacedObject? Obj) =>
        WriteSub(Accessor, n => n.WriteObject(StaticStreamName, Obj));

    [Delphi(PortStatus.Ported)] public void WriteSubNodeString(string Accessor, string Value) => WriteSub(Accessor, n => n.WriteString(Value));
    [Delphi(PortStatus.Ported)] public void WriteSubNodeFloat(string Accessor, double Value) => WriteSub(Accessor, n => n.WriteFloat(Value));

    [Delphi(PortStatus.Ported)]
    public TDateTime ReadTime() => SysUtils.EncodeTime(ReadSubNodeInteger(BoldNodeName_Hour), ReadSubNodeInteger(BoldNodeName_Min),
        ReadSubNodeInteger(BoldNodeName_Sec), ReadSubNodeInteger(BoldNodeName_MSec));

    [Delphi(PortStatus.Ported)] public TDateTime ReadSubNodeTime(string Accessor) => ReadSub(Accessor, n => n.ReadTime());

    [Delphi(PortStatus.Ported)]
    public void WriteTime(TDateTime Value)
    {
        SysUtils.DecodeTime(Value, out int hour, out int min, out int sec, out int msec);
        WriteSubNodeInteger(BoldNodeName_Hour, hour);
        WriteSubNodeInteger(BoldNodeName_Min, min);
        WriteSubNodeInteger(BoldNodeName_Sec, sec);
        WriteSubNodeInteger(BoldNodeName_MSec, msec);
    }

    [Delphi(PortStatus.Ported)] public void WriteSubNodeTime(string Accessor, TDateTime Value) => WriteSub(Accessor, n => n.WriteTime(Value));

    [Delphi(PortStatus.Ported)]
    public TDateTime ReadDate() => SysUtils.EncodeDate(ReadSubNodeInteger(BoldNodeName_Year), ReadSubNodeInteger(BoldNodeName_Month),
        ReadSubNodeInteger(BoldNodeName_Day));

    [Delphi(PortStatus.Ported)] public TDateTime ReadSubNodeDate(string Accessor) => ReadSub(Accessor, n => n.ReadDate());

    [Delphi(PortStatus.Ported)]
    public void WriteDate(TDateTime Value)
    {
        SysUtils.DecodeDate(Value, out int year, out int month, out int day);
        WriteSubNodeInteger(BoldNodeName_Year, year);
        WriteSubNodeInteger(BoldNodeName_Month, month);
        WriteSubNodeInteger(BoldNodeName_Day, day);
    }

    [Delphi(PortStatus.Ported)] public void WriteSubNodeDate(string Accessor, TDateTime Value) => WriteSub(Accessor, n => n.WriteDate(Value));

    [Delphi(PortStatus.Ported)]
    public TDateTime ReadDateTime() => ReadDate() + ReadTime().TimeOfDay;

    [Delphi(PortStatus.Ported)] public TDateTime ReadSubNodeDateTime(string Accessor) => ReadSub(Accessor, n => n.ReadDateTime());

    [Delphi(PortStatus.Ported)]
    public void WriteDateTime(TDateTime Value)
    {
        WriteDate(Value);
        WriteTime(Value);
    }

    [Delphi(PortStatus.Ported)] public void WriteSubNodeDateTime(string Accessor, TDateTime Value) => WriteSub(Accessor, n => n.WriteDateTime(Value));

    /// <summary>The text, Base64-decoded when the dt attribute says binary.base64.</summary>
    [Delphi(PortStatus.Ported)]
    public string ReadData()
    {
        string dataString = ReadString();
        if (fNode.GetAttributeNode("dt")?.Value == "binary.base64")
        {
            string result = "";
            new TBase64().DecodeData(dataString, ref result);
            return result;
        }

        return dataString;
    }

    [Delphi(PortStatus.Ported)] public string ReadSubNodeData(string Accessor) => ReadSub(Accessor, n => n.ReadData());

    /// <summary>Writes the text; data with characters below space other than tab, CR and LF (or above 255) is Base64-encoded.</summary>
    [Delphi(PortStatus.Ported)]
    public void WriteData(string Value)
    {
        static bool IncludesIllegalChar(string value) =>
            value.Any(c => !(c == '\t' || c == BoldDefs.BOLDLF || c == BoldDefs.BOLDCR || (c >= ' ' && c <= (char)255)));

        string dataString = Value;
        if (IncludesIllegalChar(Value))
        {
            new TBase64().EncodeData(Value, ref dataString);
            XMLDomElement.SetAttribute("dt", "binary.base64");
        }

        WriteString(dataString);
    }

    [Delphi(PortStatus.Ported)] public void WriteSubNodeData(string Accessor, string Value) => WriteSub(Accessor, n => n.WriteData(Value));

    [Delphi(PortStatus.Ported)]
    public void AddStateObject(string Name, object? StateObject)
    {
        fStateManager ??= new TBoldXMLStreamStateManager();
        fStateManager.AddObjectByName(Name, StateObject);
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveStateObject(string Name)
    {
        if (fStateManager is not null)
        {
            fStateManager.RemoveObjectByName(Name);
            if (fStateManager.Empty)
            {
                fStateManager = null;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public object? GetStateObject(string Name) => fStateManager?.GetObjectByName(Name);

    [Delphi(PortStatus.Ported)] public TBoldXMLStreamManager Manager => fManager;
    [Delphi(PortStatus.Ported)] public XmlElement XMLDomElement => fNode;
    [Delphi(PortStatus.Ported)] public string Accessor => GetAccessor();
    [Delphi(PortStatus.Ported)] public bool IsNull => GetIsNull();

    [Delphi(PortStatus.Ported)]
    public void SetToNull() => BoldXMLStreaming.SetAttribute(XMLDomElement, BoldXMLStreaming.BoldSOAPNullAttributeName, "1");
}

/// <summary>Streamers by stream name; a registry falls back to its parent.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldXMLStreamerRegistry.Destroy", Note = "Freed the streamer list; the garbage collector does that in .NET.")]
public class TBoldXMLStreamerRegistry : TBoldMemoryManagedObject
{
    private static TBoldXMLStreamerRegistry? G_MainRegistry;

    private readonly TBoldXMLStreamerList fStreamers;
    private readonly TBoldXMLStreamerRegistry? fParentRegistry;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamerRegistry(TBoldXMLStreamerRegistry? Parent)
    {
        fStreamers = new TBoldXMLStreamerList();
        fParentRegistry = Parent;
    }

    /// <summary>The streamer named <paramref name="Name"/> here or in a parent; raises EBoldInternal when none has it.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldXMLStreamer GetStreamer(string Name) =>
        fStreamers.StreamerByName(Name) ?? fParentRegistry?.GetStreamerOrNull(Name)
        ?? throw new EBoldInternal(SysUtils.Format(BoldCommonConst.sStreamerNotFound, GetType().Name, Name));

    private TBoldXMLStreamer? GetStreamerOrNull(string name) => fStreamers.StreamerByName(name) ?? fParentRegistry?.GetStreamerOrNull(name);

    [Delphi(PortStatus.Ported)]
    internal TBoldXMLObjectStreamer GetObjectStreamer(string Name) => (TBoldXMLObjectStreamer)GetStreamer(Name);

    [Delphi(PortStatus.Ported)]
    internal TBoldXMLInterfaceStreamer GetInterfaceStreamer(string Name) => (TBoldXMLInterfaceStreamer)GetStreamer(Name);

    [Delphi(PortStatus.Ported)]
    public void RegisterStreamer(TBoldXMLStreamer Streamer) => fStreamers.Add(Streamer);

    [Delphi(PortStatus.Ported)]
    public static TBoldXMLStreamerRegistry MainStreamerRegistry() => G_MainRegistry ??= new TBoldXMLStreamerRegistry(null);
}

/// <summary>Opens the root node of a document for reading or creates it for writing.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLStreamManager : TBoldMemoryManagedObject
{
    private readonly TBoldXMLStreamerRegistry fRegistry;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamManager(TBoldXMLStreamerRegistry Registry)
    {
        fRegistry = Registry;
    }

    /// <summary>The root node; with an accessor, the root element must have that name.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode GetRootNode(XmlDocument? Document, string Accessor)
    {
        if (Document is null)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sStreamerNotConnected, GetType().Name, "GetRootNode"));
        }

        if (Document.DocumentElement is null)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sDocumentHasNoRootNode, GetType().Name));
        }

        if (Accessor != "" && Document.DocumentElement.Name != Accessor)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sWrongTagName, GetType().Name, Document.DocumentElement.Name, Accessor));
        }

        return new TBoldXMLNode(this, Document.DocumentElement, null);
    }

    /// <summary>Creates the root element of an empty document, with the xsi namespace and xml:space="preserve".</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldXMLNode NewRootNode(XmlDocument? Document, string Accessor)
    {
        if (Document is null)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sStreamerNotConnected, GetType().Name, "NewRootNode"));
        }

        if (Document.DocumentElement is not null)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sDocumentHasRootNode, GetType().Name));
        }

        XmlElement root = Document.CreateElement(Accessor);
        Document.AppendChild(root);
        var result = new TBoldXMLNode(this, root, null);
        result.XMLDomElement.SetAttribute("xmlns:xsi", "http://www.w3.org/1999/XMLSchema-instance");
        result.XMLDomElement.SetAttribute("xml:space", "preserve");
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamerRegistry Registry => fRegistry;
}

/// <summary>Reads and writes one stream name's values.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLStreamer : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    protected abstract string GetStreamName();

    [Delphi(PortStatus.Ported)]
    public string StreamName => GetStreamName();
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLStreamerList : TBoldIndexableList
{
    private static int IX_XMLStreamerName = -1;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStreamerIndex")]
    private sealed class TBoldXMLStreamerIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStreamerIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldXMLStreamer)Item!).StreamName;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamerList()
    {
        BoldIndexableList.SetIndexVariable(ref IX_XMLStreamerName, AddIndex(new TBoldXMLStreamerIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldXMLStreamer? GetStreamerByName(string Name) =>
        (TBoldXMLStreamer?)((TBoldStringHashIndex)Indexes(IX_XMLStreamerName)!).FindByString(Name);

    [Delphi(PortStatus.Ported)]
    public TBoldXMLStreamer? StreamerByName(string Name) => GetStreamerByName(Name);
}

/// <summary>Streams objects; subclasses make, write and read them.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLObjectStreamer : TBoldXMLStreamer
{
    [Delphi(PortStatus.Ported)]
    public virtual void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ReadObject(object Obj, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    public abstract object CreateObject();
}

/// <summary>Streams values reached through IBoldStreamable.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLInterfaceStreamer : TBoldXMLStreamer
{
    [Delphi(PortStatus.Ported)]
    public virtual void WriteInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ReadInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
    }
}

/// <summary>Attribute names and float settings of BoldXMLStreaming.</summary>
public static partial class BoldXMLStreaming
{
    [Delphi(PortStatus.Ported)] public const string BoldSOAPNullAttributeName = "xsi:null";
    [Delphi(PortStatus.Ported)] public const string BoldSOAPTypeAttributeName = "xsi:type";

    private const string XsiNamespace = "http://www.w3.org/1999/XMLSchema-instance";

    /// <summary>Sets an attribute; an "xsi:" name gets the XML Schema instance namespace, as MSXML gives it.</summary>
    internal static void SetAttribute(XmlElement element, string name, string value)
    {
        if (name.StartsWith("xsi:", StringComparison.Ordinal))
        {
            XmlAttribute attr = element.OwnerDocument.CreateAttribute("xsi", name.Substring(4), XsiNamespace);
            attr.Value = value;
            element.SetAttributeNode(attr);
        }
        else
        {
            element.SetAttribute(name, value);
        }
    }

    [ThreadStatic] private static bool FloatSettingsPushed;
    [ThreadStatic] private static char oldDecimalSeparator;
    [ThreadStatic] private static char oldThousandSeparator;

    /// <summary>Sets '.' and ',' as decimal and thousand separators until PopFloatSettings; it does not nest.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void PushFloatSettings()
    {
        if (FloatSettingsPushed)
        {
            throw new EBold(BoldCommonConst.sCannotNestPushFloat);
        }

        FloatSettingsPushed = true;
        oldDecimalSeparator = SysUtils.FormatSettings.DecimalSeparator;
        SysUtils.FormatSettings.DecimalSeparator = '.';
        oldThousandSeparator = SysUtils.FormatSettings.ThousandSeparator;
        SysUtils.FormatSettings.ThousandSeparator = ',';
    }

    [Delphi(PortStatus.Ported)]
    internal static void PopFloatSettings()
    {
        if (!FloatSettingsPushed)
        {
            throw new EBold(BoldCommonConst.sPushNestMismatch);
        }

        SysUtils.FormatSettings.DecimalSeparator = oldDecimalSeparator;
        SysUtils.FormatSettings.ThousandSeparator = oldThousandSeparator;
        FloatSettingsPushed = false;
    }
}
