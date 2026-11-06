// Delphi: Source/UMLModel/Plugins/BoldUMLPlugins.pas
// Port map: CSharp/port/units/BoldUMLPlugins.md

using System.Drawing;
using System.Reflection;

namespace Bold;

/// <summary>A plug-in of the model editor: a subclass says what it is and what it does.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TUMLPlugInFunction : IDisposable
{
    private TUMLPlugIn? fPlugIn;

    [Delphi(PortStatus.Ported)]
    protected TUMLPlugInFunction(bool InstallPlugin)
    {
        if (InstallPlugin)
        {
            Install();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TUMLPlugInFunction.Destroy")]
    public void Dispose()
    {
        if (fPlugIn is not null)
        {
            BoldUMLPlugInRegistry.RawList.Remove(fPlugIn);
            fPlugIn = null;
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] protected virtual string GetMenuItemName() => GetType().Name;
    [Delphi(PortStatus.Ported)] protected abstract TPlugInType GetPlugInType();

    [Delphi(PortStatus.Ported, Note = "The assembly of the plug-in's class, where Delphi gave the module handle.")]
    protected virtual Assembly GetInstance() => GetType().Assembly;

    [Delphi(PortStatus.Ported)] protected virtual string GetImageResourceName() => GetType().Name;
    [Delphi(PortStatus.Ported)] protected abstract Color GetImageMaskColor();
    [Delphi(PortStatus.Ported)] protected virtual TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of();

    /// <summary>Puts the plug-in in the editor's list.</summary>
    [Delphi(PortStatus.Ported)]
    public void Install()
    {
        fPlugIn = new TUMLPlugIn(MenuItemName, Execute, PlugInType, Instance, ImageResourceName, ImageMaskColor, Options, GetType().Name);
        BoldUMLPlugInRegistry.RawList.Add(fPlugIn);
    }

    [Delphi(PortStatus.Ported)] public string MenuItemName => GetMenuItemName();
    [Delphi(PortStatus.Ported)] public TPlugInType PlugInType => GetPlugInType();
    [Delphi(PortStatus.Ported)] public Assembly Instance => GetInstance();
    [Delphi(PortStatus.Ported)] public string ImageResourceName => GetImageResourceName();
    [Delphi(PortStatus.Ported)] public Color ImageMaskColor => GetImageMaskColor();
    [Delphi(PortStatus.Ported)] public TBoldUMLPluginOptions Options => GetOptions();

    /// <summary>The plug-in in the editor's list (none when not installed).</summary>
    public TUMLPlugIn? PlugIn => fPlugIn;

    [Delphi(PortStatus.Ported)]
    public abstract void Execute(IUMLModelPlugInContext context);
}

/// <summary>The import and export through the model's link.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TUMLLinkImporterExporter : TUMLPlugInFunction
{
    protected TUMLLinkImporterExporter(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    /// <summary>The model's UML model and the link that has the model component.</summary>
    [Delphi(PortStatus.Ported)]
    protected void Initialize(IUMLModelPlugInContext Context, out TBoldUMLObject UMLModel, out TBoldUMLModelLink UMLLink)
    {
        TBoldModel modelComponent = Context.GetCurrentModelHandle() ?? throw new EBold($"{GetType().Name}: Unable to find model component");
        UMLModel = modelComponent.EnsuredUMLModel;
        UMLLink = BoldUMLModelLinkUnit.BoldGetUMLModelLinkForComponent(modelComponent)
            ?? throw new EBold($"{GetType().Name}: There is no link component for {modelComponent}");
    }
}

/// <summary>Reads the model again through its link.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLLinkImporter : TUMLLinkImporterExporter
{
    public TUMLLinkImporter(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Import via Link";
    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptFile;
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginImportImage";
    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        if (Dialogs.MessageDlg("Are you sure you want to import?", TMsgDlgType.mtWarning, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) != Controls.mrYes)
        {
            return;
        }

        Initialize(context, out TBoldUMLObject umlModel, out TBoldUMLModelLink umlLink);
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        try
        {
            log.StartLog($"Importing Model From {umlLink.DisplayName}");
            umlLink.ImportModel(umlModel);
            log.EndLog();
        }
        catch (Exception E)
        {
            log.Log(E.Message);
            throw;
        }
    }
}

/// <summary>Writes the model through its link.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLLinkExporter : TUMLLinkImporterExporter
{
    public TUMLLinkExporter(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Export via Link";
    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptFile;
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginExportImage";
    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;
    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireUnBoldified);

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        if (Dialogs.MessageDlg("Are you sure you want to export?", TMsgDlgType.mtWarning, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) != Controls.mrYes)
        {
            return;
        }

        Initialize(context, out TBoldUMLObject umlModel, out TBoldUMLModelLink umlLink);
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        try
        {
            log.StartLog($"Exporting Model To {umlLink.DisplayName}");
            umlLink.ExportModel(umlModel);
        }
        catch (Exception E)
        {
            log.Log(E.Message);
            throw;
        }
    }
}

/// <summary>Brings a model of an older Bold up to date.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLModelUpdaterPlugIn : TUMLPlugInFunction
{
    public TUMLModelUpdaterPlugIn(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context) => TBoldUMLModelUpdater.UpdateModel(context.GetCurrentModelHandle().EnsuredUMLModel);

    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginModelUpdater";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Update model to latest version";
    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptTool;
}

/// <summary>Variables and initialization of BoldUMLPlugins.</summary>
public static class BoldUMLPlugins
{
    private static TUMLLinkExporter? _LinkExporter;
    private static TUMLLinkImporter? _LinkImporter;

    /// <summary>Installs the unit's plug-ins (Delphi's initialization; the updater was not installed there either).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called when the plug-in list is first asked for.")]
    internal static void Initialize()
    {
        _LinkExporter ??= new TUMLLinkExporter(true);
        _LinkImporter ??= new TUMLLinkImporter(true);
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "Removes the plug-ins; the process ending does it otherwise.")]
    internal static void Finalize_()
    {
        _LinkExporter?.Dispose();
        _LinkImporter?.Dispose();
        _LinkExporter = null;
        _LinkImporter = null;
    }
}
