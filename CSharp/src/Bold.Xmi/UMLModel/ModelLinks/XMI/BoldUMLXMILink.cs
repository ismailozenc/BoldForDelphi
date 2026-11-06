// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMILink.pas
// Port map: CSharp/port/units/BoldUMLXMILink.md

namespace Bold;

/// <summary>Reads a model from an XMI 1.x file and writes one as XMI 1.0 (UML 1.3).</summary>
[Delphi(PortStatus.Ported)]
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

    [Delphi(PortStatus.Ported)]
    public override bool ExportModel(TBoldUMLObject UMLModel)
    {
        var exporter = new TBoldXMI10Exporter(this);
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog("Model Export");
        exporter.RawExport(new TBoldUMLMOFObjectAdapter(UMLModel, this));
        log.EndLog();
        return true;
    }

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
