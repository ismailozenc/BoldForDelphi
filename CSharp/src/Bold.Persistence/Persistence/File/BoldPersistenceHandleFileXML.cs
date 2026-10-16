// Delphi: Source/Persistence/File/BoldPersistenceHandleFileXML.pas
// Port map: CSharp/port/units/BoldPersistenceHandleFileXML.md

using System.Xml;
using static Bold.PersistenceConsts;

namespace Bold;

/// <summary>A persistence handle that stores the objects in an XML file.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceHandleFileXML : TBoldAbstractPersistenceHandleFile
{
    public TBoldPersistenceHandleFileXML(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>An XML file controller for the file and model; raises without a model.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController() =>
        BoldModel is null
            ? throw new EBold(SysUtils.Format(sModelRequired, GetType().Name))
            : new TBoldPersistenceControllerFileXML(FileName, CacheData, BoldModel.MoldModel);
}

/// <summary>Reads and writes the value space as the XML document of the default XML streaming.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceControllerFileXML(string filename, bool CacheData, TMoldModel MoldModel)
    : TBoldPersistenceControllerFile(filename, CacheData, MoldModel)
{
    [Delphi(PortStatus.Ported)]
    protected override void ReadValueSpace()
    {
        var aMgr = new TBoldDefaultXMLStreamManager(TBoldDefaultXMLStreamerRegistry.MainStreamerRegistry(), MoldModel)
        {
            IgnorePersistenceState = true,
            PersistenceStatesToOverwrite = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsCurrent),
            PersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsModified,
                TBoldValuePersistenceState.bvpsCurrent),
        };
        var anXMLDoc = new XmlDocument();
        try
        {
            anXMLDoc.LoadXml(File.ReadAllText(FileName));
        }
        catch (XmlException)
        {
            throw new EBold(sXMLParseError);
        }

        TBoldXMLNode aNode = aMgr.GetRootNode(anXMLDoc, "ValueSpace");
        aMgr.ReadValueSpace(LocalValueSpace, aNode);
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteValueSpace()
    {
        var aMgr = new TBoldDefaultXMLStreamManager(TBoldDefaultXMLStreamerRegistry.MainStreamerRegistry(), MoldModel)
        {
            IgnorePersistenceState = true,
            PersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsModified,
                TBoldValuePersistenceState.bvpsCurrent),
        };
        var anXMLDoc = new XmlDocument();
        TBoldXMLNode aNode = aMgr.NewRootNode(anXMLDoc, "ValueSpace");
        var anIdList = new TBoldObjectIdList();
        LocalValueSpace.AllObjectIds(anIdList, true);
        aMgr.WriteValueSpace(LocalValueSpace, anIdList, null, aNode);
        File.WriteAllText(FileName, anXMLDoc.DocumentElement!.OuterXml + Environment.NewLine);
    }
}

/// <summary>The unit's file-type constants.</summary>
public static partial class BoldPersistenceHandleFileXML
{
    [Delphi(PortStatus.Ported)] public static string XML_LINKEXTENSION = ".xml";
    [Delphi(PortStatus.Ported)] public static string XML_LINKDESC = "XML file";
}
