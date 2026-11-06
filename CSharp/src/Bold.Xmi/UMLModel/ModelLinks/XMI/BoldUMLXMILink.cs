// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMILink.pas
// Port map: CSharp/port/units/BoldUMLXMILink.md

namespace Bold;

/// <summary>The XMI a link writes.</summary>
public enum TBoldXMIVersion
{
    /// <summary>XMI 1.0 of UML 1.3, as Delphi Bold wrote it.</summary>
    xmi10,

    /// <summary>XMI 2.1 of UML 2.</summary>
    xmi21,
}

/// <summary>Reads a model from an XMI 1.x or 2.x file and writes one as XMI 1.0 (UML 1.3) or XMI 2.1 (UML 2).</summary>
[Delphi(PortStatus.Ported, Note = "Also reads and writes XMI 2 (XMIVersion), which Delphi did not.")]
public class TBoldUMLXMILink : TBoldUMLModelLink
{
    [Delphi(PortStatus.Ported, Symbol = "XMI_LINKEXTENSION")] public const string XMI_LINKEXTENSION = ".xmi";
    [Delphi(PortStatus.Ported, Symbol = "XMI_LINKDESC")] public const string XMI_LINKDESC = "XMI file";

    private string fFilename = "";

    public TBoldUMLXMILink(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
    }

    /// <summary>Registers the link in the list of links (Delphi's unit initialization).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called when the list of links is first made.")]
    internal static void Register(TBoldUMLModelLinkList List) =>
        List.AddLink(XMI_LINKEXTENSION, XMI_LINKDESC, new TBoldUMLModelLinkClass(typeof(TBoldUMLXMILink)));

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "The list goes with the process.")]
    internal static void Unregister(TBoldUMLModelLinkList List) => List.RemoveLink(new TBoldUMLModelLinkClass(typeof(TBoldUMLXMILink)));

    [Delphi(PortStatus.Ported, Note = "Writes XMI 2.1 when XMIVersion says so.")]
    public override bool ExportModel(TBoldUMLObject UMLModel)
    {
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog("Model Export");
        if (XMIVersion == TBoldXMIVersion.xmi21)
        {
            new TBoldXMI2Exporter().Export(UMLModel, FileName);
        }
        else
        {
            using var exporter = new TBoldXMI10Exporter(this);
            exporter.RawExport(new TBoldUMLMOFObjectAdapter(UMLModel, this));
        }

        log.EndLog();
        return true;
    }

    /// <summary>The XMI that ExportModel writes; reading takes any.</summary>
    public TBoldXMIVersion XMIVersion { get; set; } = TBoldXMIVersion.xmi10;

    [Delphi(PortStatus.Ported)]
    protected override bool GetCanExport() => true;

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => fFilename;

    [Delphi(PortStatus.Ported)]
    protected override string GetFileName() => fFilename;

    [Delphi(PortStatus.Ported)]
    protected override void SetFileName(string Value) => fFilename = Value;

    /// <summary>Clears the UML model and reads the file into it; false, with the error logged, when that fails.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi returned true after logging an error and rolling back; this returns false, and the graph may be partly filled.")]
    public override bool ImportModel(TBoldUMLObject UMLModel)
    {
        if (UMLModel is null)
        {
            throw new EBoldImport($"{GetType().Name}.ImportModel: Must have an UMLModel to import to.");
        }

        TBoldLogHandler log = BoldLogHandler.BoldLog();
        using var importer = new TBoldUMLXMIImporter(this, UMLModel);
        try
        {
            foreach (TBoldUMLObject element in UMLModel.AllOwnedElements.ToList())
            {
                element.Delete();
            }

            log.StartLog("Model import");
            importer.RawImport();
            log.EndLog();
            return true;
        }
        catch (Exception E)
        {
            log.LogFmt("Error: %s", [E.Message], TBoldLogType.ltError);
            LastError = E;
            return false;
        }
    }

    /// <summary>The error of the last import that failed.</summary>
    public Exception? LastError { get; private set; }

    [Delphi(PortStatus.Ported)] public bool ValidateInput { get; set; }
    [Delphi(PortStatus.Ported)] public bool TranslateRoseTaggedValues { get; set; }
}
