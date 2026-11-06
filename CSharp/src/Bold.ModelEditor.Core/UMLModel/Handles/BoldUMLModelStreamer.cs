// Delphi: Source/UMLModel/Handles/BoldUMLModelStreamer.pas
// Port map: CSharp/port/units/BoldUMLModelStreamer.md

using System.Xml;

namespace Bold;

/// <summary>A UML model as a string, for a model component to keep it.</summary>
[Delphi(PortStatus.Ported, Note = "Streams the UML graph as XMI 2.1, where Delphi streamed its UML object space as Bold's XML value space.")]
public static class TUMLModelStreamer
{
    /// <summary>Reads the model from its string into the (empty) UML model.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the UML model to fill where Delphi took the object space and the UML metamodel's Mold model.")]
    public static void FillSystemFromString(TBoldUMLObject UMLModel, string UMLModelAsString)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml(UMLModelAsString);
        }
        catch (XmlException e)
        {
            throw new EBold("Error reading/parsing XML file", e);
        }

        new TBoldXMI2Importer(UMLModel).Import(document);
    }

    /// <summary>The model as a string.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the UML model where Delphi took the object space and the UML metamodel's Mold model.")]
    public static string SystemAsString(TBoldUMLObject UMLModel) => new TBoldXMI2Exporter().Export(UMLModel).OuterXml;
}
