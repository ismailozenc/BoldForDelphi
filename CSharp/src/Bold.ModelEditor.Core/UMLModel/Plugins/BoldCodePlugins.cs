// Delphi: Source/UMLModel/Plugins/BoldCodePlugins.pas
// Port map: CSharp/port/units/BoldCodePlugins.md

using System.Drawing;
using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLIDLGenerator", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLIDLGenerator.GetMenuItemName", Note = "COM code generation is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLIDLGenerator.GetImageResourceName", Note = "COM code generation is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLIDLGenerator.GenerateCode", Note = "COM code generation is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLMIDLGenerator", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLMIDLGenerator.GetImageResourceName", Note = "COM code generation is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLMIDLGenerator.GetMenuItemName", Note = "COM code generation is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldCodePlugins", Symbol = "TUMLMIDLGenerator.GenerateCode", Note = "COM code generation is not ported (drop list).")]

namespace Bold;

/// <summary>Gives the model and its classes new GUIDs (all, or those that have none).</summary>
[Delphi(PortStatus.Ported)]
public class TUMLGUIDGenerator : TUMLPlugInFunction
{
    public TUMLGUIDGenerator(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        bool overwriteOld = Dialogs.MessageDlg("Keep Existing GUIDs?", TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == Controls.mrNo;
        static void NewGuid(TBoldUMLObject Element, string TaggedValueName, bool OverwriteOld)
        {
            if (OverwriteOld || Element.GetBoldTV(TaggedValueName).Trim() == "")
            {
                Element.SetBoldTV(TaggedValueName, BoldGUIDUtils.BoldCreateGUIDAsString(true));
            }
        }

        TBoldUMLObject umlModel = context.GetCurrentModelHandle().EnsuredUMLModel;
        NewGuid(umlModel, TAG_GUID, overwriteOld);
        foreach (TBoldUMLObject cls in umlModel.Classes)
        {
            NewGuid(cls, TAG_GUID, overwriteOld);
            NewGuid(cls, TAG_LISTGUID, overwriteOld);
        }
    }

    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginGenGUIDs";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Generate COM/IDL GUIDs";
    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified);
    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptTool;
}

/// <summary>Validates the model and, when it has no errors, generates code from it.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TUMLAbstractCodeGenerator : TUMLPlugInFunction
{
    protected TUMLAbstractCodeGenerator(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptTool;
    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;
    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified);

    [Delphi(PortStatus.Ported)]
    protected abstract void GenerateCode(TBoldGenerator Generator);

    /// <summary>The generator to generate with: C#, where Delphi's wrote Delphi.</summary>
    protected virtual TBoldGenerator CreateGenerator(TBoldTypeNameDictionary TypeNameDictionary) => new TBoldCSharpGenerator(TypeNameDictionary);

    /// <summary>Generates into the folder.</summary>
    [Delphi(PortStatus.Ported, Note = "Generates C# (TBoldCSharpGenerator), where Delphi's generator wrote Delphi.")]
    protected void Generate(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary, string Path)
    {
        using TBoldGenerator generator = CreateGenerator(TypeNameDictionary);
        generator.BaseFilePath = Path;
        generator.UseTypedLists = true;
        generator.MoldModel = MoldModel;
        GenerateCode(generator);
    }

    /// <summary>Validates the model and generates the code next to the model's file.</summary>
    [Delphi(PortStatus.Ported, Note = "Writes next to the model's file (TBoldModel.FileName), where Delphi wrote next to the unit the component was on.")]
    public override void Execute(IUMLModelPlugInContext context)
    {
        TBoldModel boldModel = context.GetCurrentModelHandle();
        using (var validator = new TBoldUMLModelValidator(boldModel.EnsuredUMLModel, null, TBoldUMLAbstractModelValidator.BoldDefaultValidatorSourceLanguage))
        {
            validator.Validate(boldModel.TypeNameDictionary);
            if (validator.HighestSeverity() == TSeverity.sError)
            {
                throw new EBold("Errors in model, can not generate code");
            }
        }

        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog("Code generation: " + MenuItemName);
        string folder = boldModel.FileName != "" ? System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(boldModel.FileName))! : Directory.GetCurrentDirectory();
        Generate(boldModel.MoldModel, boldModel.TypeNameDictionary, folder + System.IO.Path.DirectorySeparatorChar);
        log.EndLog();
    }
}

/// <summary>Generates the business classes.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLCodeGenerator : TUMLAbstractCodeGenerator
{
    public TUMLCodeGenerator(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Generate Code";
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginGenCodeImage";

    [Delphi(PortStatus.Ported, Note = "The C# generator writes the classes and the method stubs in one go.")]
    protected override void GenerateCode(TBoldGenerator Generator)
    {
        if (Generator is TBoldCSharpGenerator csharp)
        {
            csharp.Generate();
        }
        else
        {
            Generator.GenerateBusinessObjectCode();
            Generator.EnsureMethodImplementations();
        }
    }
}

/// <summary>Generates the persistence interfaces.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLPersistenceInterfaceGenerator : TUMLAbstractCodeGenerator
{
    public TUMLPersistenceInterfaceGenerator(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    /// <summary>A generator with Delphi's templates: the persistence interfaces are Delphi code, and only Delphi's templates have them.</summary>
    protected override TBoldGenerator CreateGenerator(TBoldTypeNameDictionary TypeNameDictionary)
    {
        TBoldDelphiTemplateManager.InstallTemplates();
        return new TBoldGenerator(TypeNameDictionary);
    }

    [Delphi(PortStatus.Ported)] protected override void GenerateCode(TBoldGenerator Generator) => Generator.GeneratePersistenceInterfaces();
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginGenPersistenceInterfaceImage";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Generate Persistence Interfaces";
}

/// <summary>Variables and initialization of BoldCodePlugins.</summary>
public static class BoldCodePlugins
{
    private static TUMLCodeGenerator? _CodeGenerator;
    private static TUMLPersistenceInterfaceGenerator? _PersistenceInterfaceGenerator;
    private static TUMLGUIDGenerator? _GUIDGenerator;

    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called when the plug-in list is first asked for; the IDL generators are not ported (COM).")]
    internal static void Initialize()
    {
        _CodeGenerator ??= new TUMLCodeGenerator(true);
        _PersistenceInterfaceGenerator ??= new TUMLPersistenceInterfaceGenerator(true);
        _GUIDGenerator ??= new TUMLGUIDGenerator(true);
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "Removes the plug-ins; the process ending does it otherwise.")]
    internal static void Finalize_()
    {
        _CodeGenerator?.Dispose();
        _PersistenceInterfaceGenerator?.Dispose();
        _GUIDGenerator?.Dispose();
        _CodeGenerator = null;
        _PersistenceInterfaceGenerator = null;
        _GUIDGenerator = null;
    }
}
