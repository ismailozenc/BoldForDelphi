// Delphi: Source/Handles/XML/BoldXMLProducers.pas
// Port map: CSharp/port/units/BoldXMLProducers.md

using System.ComponentModel;
using System.Xml;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldXMLProducers", Symbol = "TBoldXMLProducerOptions", Note = "DelphiSet<TBoldXMLProducerOption>.")]

namespace Bold;

/// <summary>What an XML element for a Bold element holds: its id string, its value.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldXMLProducerOption { xpoIncludeIdString, xpoIncludeValue }

/// <summary>Builds the XML document of a producer from the request parameters.</summary>
[Delphi(PortStatus.Ported, Note = "Takes a System.Xml document where Delphi takes an MSXML IXMLDomDocument.")]
public delegate void TBoldProduceEvent(TBoldStringList paramList, XmlDocument DomDoc);

/// <summary>
/// Produces XML documents of Bold elements: each element becomes an XML element named after its member or type, with
/// its id string (from the manipulator) as an attribute and its value as text.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Builds System.Xml documents where Delphi builds MSXML documents.")]
public abstract class TBoldAbstractXMLProducer : TBoldSubscribableComponent
{
    private TBoldManipulator? FBoldManipulator;
    private string FXMLElementBoldIDName;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldAbstractXMLProducer(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        FXMLElementBoldIDName = BoldXMLProducers.DEFAULT_XMLELEMENT_BOLDIDNAME;
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldManipulator(TBoldManipulator? Value)
    {
        if (Value != FBoldManipulator)
        {
            Value?.FreeNotification(this);
            FBoldManipulator = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void Produce(TBoldStringList paramList, XmlDocument DomDoc);

    /// <summary>Forgets the manipulator when it goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (AComponent == FBoldManipulator && Operation == TOperation.opRemove)
        {
            FBoldManipulator = null;
        }
    }

    /// <summary>A new document with the XML declaration, filled by Produce.</summary>
    [Delphi(PortStatus.Ported)]
    protected XmlDocument getDocument(TBoldStringList paramList)
    {
        var doc = new XmlDocument();
        XmlProcessingInstruction ProcessingInstruction = doc.CreateProcessingInstruction("xml", "version=\"1.0\""); // do not localize
        doc.AppendChild(ProcessingInstruction);
        Produce(paramList, doc);
        return doc;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldManipulator? BoldManipulator
    {
        get => FBoldManipulator;
        set => SetBoldManipulator(value);
    }

    [Delphi(PortStatus.Ported)]
    protected string XMLElementBoldIDName
    {
        get => FXMLElementBoldIDName;
        set => FXMLElementBoldIDName = value;
    }

    [Delphi(PortStatus.Ported)]
    public string getDocumentAsString(TBoldStringList paramList) => getDocument(paramList).OuterXml;

    /// <summary>
    /// Adds an XML element for the Bold element under the parent: named by the tag (or the element's default tag), with
    /// the id string attribute and the value as text, as the options say.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public XmlElement AddDomElementForBoldElement(XmlElement DomParentElement, TBoldElement BoldElement, string Mapping = "", string Tag = "",
        DelphiSet<TBoldXMLProducerOption>? Options = null)
    {
        Options ??= BoldXMLProducers.xpoDefault;
        if (BoldManipulator is null)
        {
            throw new EBold(SysUtils.Format(sManipulatorNotAssigned, GetType().Name, "AddDomElementForBoldElement")); // do not localize
        }

        string IdString = "";
        string Value = BoldManipulator.GetValueAndId(BoldElement, ref IdString, Mapping);
        string ElementTag = SysUtils.Trim(Tag) == "" ? BoldManipulator.DefaultTagForElement(BoldElement) : Tag;
        XmlDocument DomDoc = DomParentElement.OwnerDocument;
        XmlElement Result = DomDoc.CreateElement(ElementTag);
        if (Options.Contains(TBoldXMLProducerOption.xpoIncludeIdString))
        {
            Result.SetAttribute(XMLElementBoldIDName, IdString);
        }

        if (Options.Contains(TBoldXMLProducerOption.xpoIncludeValue))
        {
            Result.InnerText = Value;
        }

        DomParentElement.AppendChild(Result);
        return Result;
    }

    /// <summary>
    /// Adds XML elements for the named members of the object ("Mapping.Member" uses a mapping, named as the tag), or for
    /// all its attributes when no names are given.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void AddDomElementsForBoldObjectAttributes(XmlElement DomParentElement, TBoldObject BoldObject, string[] BoldMemberNames,
        DelphiSet<TBoldXMLProducerOption>? Options = null)
    {
        Options ??= BoldXMLProducers.xpoDefault;
        if (BoldMemberNames.Length == 0)
        {
            for (int i = 0; i < BoldObject.BoldMemberCount; i++)
            {
                if (BoldObject.BoldMembers(i) is TBoldAttribute)
                {
                    AddDomElementForBoldElement(DomParentElement, BoldObject.BoldMembers(i), "", "", Options);
                }
            }

            return;
        }

        foreach (string memberName in BoldMemberNames)
        {
            int position = SysUtils.Pos(".", memberName);
            string MapperName;
            string MemberName;
            if (position != 0)
            {
                MapperName = SysUtils.Copy(memberName, 0, position - 1);
                MemberName = SysUtils.Copy(memberName, position + 1, SysUtils.MaxInt);
            }
            else
            {
                MapperName = "";
                MemberName = memberName;
            }

            AddDomElementForBoldElement(DomParentElement, BoldObject.BoldMemberByExpressionName(MemberName), MapperName, MapperName, Options);
        }
    }

    /// <summary>Adds an XML element with the tag, text and the "name=value" attributes under the parent.</summary>
    [Delphi(PortStatus.Ported, Note = "An attribute's value is the value of its name=value line; Delphi set the whole line.")]
    public XmlElement AddDomElement(XmlElement DomParentElement, string XMLTag, string XMLValue = "", TBoldStringList? XMLAttributes = null)
    {
        System.Diagnostics.Debug.Assert(DomParentElement is not null, SysUtils.Format("%s.AddDomElement: cannot pass a nil argument \"DomParentElement\"", GetType().Name));
        XmlDocument DomDoc = DomParentElement.OwnerDocument;
        XmlElement Result = DomDoc.CreateElement(XMLTag);
        DomParentElement.AppendChild(Result);
        Result.InnerText = XMLValue;
        if (XMLAttributes is not null)
        {
            for (int i = 0; i < XMLAttributes.Count; i++)
            {
                Result.SetAttribute(XMLAttributes.Names(i), XMLAttributes.ValueFromIndex(i));
            }
        }

        return Result;
    }
}

/// <summary>An XML producer whose OnProduce event builds the document.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLProducer : TBoldAbstractXMLProducer
{
    private TBoldProduceEvent? FOnProduce;

    /// <summary>Adds the producer to <paramref name="AOwner"/> when given.</summary>
    public TBoldXMLProducer(IContainer? AOwner = null)
        : base(AOwner)
    {
    }

    [Delphi(PortStatus.Ported, Note = "Protected, as the abstract method it overrides; Delphi made the override public.")]
    protected override void Produce(TBoldStringList paramList, XmlDocument DomDoc) => FOnProduce?.Invoke(paramList, DomDoc);

    /// <summary>The manipulator that gives id strings and values (published here; protected in the abstract producer).</summary>
    public new TBoldManipulator? BoldManipulator
    {
        get => base.BoldManipulator;
        set => base.BoldManipulator = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldProduceEvent? OnProduce
    {
        get => FOnProduce;
        set => FOnProduce = value;
    }
}

/// <summary>Unit-level constants of BoldXMLProducers.</summary>
public static partial class BoldXMLProducers
{
    [Delphi(PortStatus.Ported)] public static readonly DelphiSet<TBoldXMLProducerOption> xpoDefault = DelphiSet<TBoldXMLProducerOption>.Of(TBoldXMLProducerOption.xpoIncludeIdString, TBoldXMLProducerOption.xpoIncludeValue);
    [Delphi(PortStatus.Ported)] public const string DEFAULT_XMLELEMENT_BOLDIDNAME = "BoldID";
}
