// Delphi: Source/UMLModel/ModelLinks/Bld/BoldUMLBldLink.pas
// Port map: CSharp/port/units/BoldUMLBldLink.md

namespace Bold;

/// <summary>Reads a UML model from, and writes one to, a .bld file (Bold's own format, the Mold model's text).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLBldLink : TBoldUMLModelLink
{
    [Delphi(PortStatus.Ported, Symbol = "Bld_LINKEXTENSION")] public const string Bld_LINKEXTENSION = ".bld";
    [Delphi(PortStatus.Ported, Symbol = "Bld_LINKDESC")] public const string Bld_LINKDESC = "Bold Proprietary Format";

    private string fFilename = "";

    public TBoldUMLBldLink(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
    }

    /// <summary>Registers the link in the list of links (Delphi's unit initialization).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called by EnsureRegistered, which the editor calls.")]
    private static void Register(TBoldUMLModelLinkList List)
    {
        if (List.LinkDescriptorByLinkClass(new TBoldUMLModelLinkClass(typeof(TBoldUMLBldLink))) is null)
        {
            List.AddLink(Bld_LINKEXTENSION, Bld_LINKDESC, new TBoldUMLModelLinkClass(typeof(TBoldUMLBldLink)));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "The list goes with the process.")]
    private static void Unregister(TBoldUMLModelLinkList List) => List.RemoveLink(new TBoldUMLModelLinkClass(typeof(TBoldUMLBldLink)));

    /// <summary>Puts the link in the list of links.</summary>
    public static void EnsureRegistered() => Register(BoldUMLModelLinkUnit.BoldUMLModelLinkList());

    /// <summary>Writes the model's Mold model.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ExportModel(TBoldUMLObject UMLModel)
    {
        TMoldModel moldModel = TBoldModelConverter.UMLModelToMold(UMLModel);
        var modelAsStrings = new TStringList();
        TMoldBLDRW.ModelToStrings(moldModel, modelAsStrings);
        modelAsStrings.SaveToFile(FileName);
        return true;
    }

    [Delphi(PortStatus.Ported)] protected override bool GetCanExport() => true;
    [Delphi(PortStatus.Ported)] protected override string GetDisplayName() => fFilename;
    [Delphi(PortStatus.Ported)] protected override string GetFileName() => fFilename;
    [Delphi(PortStatus.Ported)] protected override void SetFileName(string Value) => fFilename = Value;

    /// <summary>Clears the UML model and reads the file's Mold model into it; false, with the error logged, when that fails.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi returned true after logging an error and rolling back; this returns false.")]
    public override bool ImportModel(TBoldUMLObject UMLModel)
    {
        if (UMLModel is null)
        {
            throw new EBoldImport($"{GetType().Name}.ImportModel: Must have an UMLModel to import to.");
        }

        TBoldLogHandler log = BoldLogHandler.BoldLog();
        try
        {
            log.LogFmtIndent("Start Import Bold Proprietary Model File: %s", [FileName]);
            TMoldModel moldModel = TMoldBLDRW.ModelFromFile(FileName);
            TBoldModelConverter.MoldModelToUML(moldModel, UMLModel);
            log.LogDedent("Done importing model");
            log.EndLog();
            return true;
        }
        catch (Exception E)
        {
            log.LogFmt("Error: %s", [E.Message], TBoldLogType.ltError);
            return false;
        }
    }
}
