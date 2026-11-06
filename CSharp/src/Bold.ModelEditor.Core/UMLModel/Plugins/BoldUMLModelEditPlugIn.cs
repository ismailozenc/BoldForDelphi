// Delphi: Source/UMLModel/Plugins/BoldUMLModelEditPlugIn.pas
// Port map: CSharp/port/units/BoldUMLModelEditPlugIn.md
// The menu item and tool button that carry a plug-in are WinForms controls, in Bold.ModelEditor.

using System.Drawing;
using System.Reflection;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldUMLModelEditPlugIn", Symbol = "TBoldUMLPluginOptions", Note = "Global using alias of DelphiSet<TBoldUMLPluginOption>, in BoldUMLModelEditPlugIn.Aliases.cs.")]

namespace Bold;

/// <summary>What a plug-in is given: the model and the element the editor shows, and whether a plug-in runs.</summary>
[Delphi(PortStatus.Ported)]
public interface IUMLModelPlugInContext
{
    [Delphi(PortStatus.Ported)] bool GetIsExecutingPlugin();
    [Delphi(PortStatus.Ported)] void SetIsExecutingPlugin(bool Value);
    [Delphi(PortStatus.Ported)] TBoldModel GetCurrentModelHandle();
    [Delphi(PortStatus.Ported)] TBoldUMLObject? GetCurrentElement();

    [Delphi(PortStatus.Ported)]
    bool IsExecutingPlugin
    {
        get => GetIsExecutingPlugin();
        set => SetIsExecutingPlugin(value);
    }
}

[Delphi(PortStatus.Ported)]
public delegate void TUMLPluginExecuteEvent(IUMLModelPlugInContext context);

/// <summary>Where the editor puts a plug-in: the tools or the file menu.</summary>
[Delphi(PortStatus.Ported)]
public enum TPlugInType { ptTool, ptFile }

/// <summary>What a plug-in needs of the model: boldified (and flattened), or not.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldUMLPluginOption { poRequireBoldified, poRequireUnBoldified }

/// <summary>A plug-in of the model editor: its name in the menu, what it does, its image and what it needs.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLPlugIn
{
    [Delphi(PortStatus.Ported, Note = "The image comes from an assembly's resources where Delphi took a module handle; the mask colour is a System.Drawing.Color.")]
    public TUMLPlugIn(string MenuItemName, TUMLPluginExecuteEvent? OnExecute, TPlugInType PlugInType, Assembly? Instance,
        string ImageResourceName, Color ImageMaskColor, TBoldUMLPluginOptions Options, string PlugInClassName)
    {
        this.MenuItemName = MenuItemName;
        this.OnExecute = OnExecute;
        this.PlugInType = PlugInType;
        this.Instance = Instance;
        this.ImageResourceName = ImageResourceName;
        this.ImageMaskColor = ImageMaskColor;
        this.Options = Options;
        PluginClassName = PlugInClassName;
    }

    [Delphi(PortStatus.Ported)] protected TUMLPluginExecuteEvent? OnExecute { get; }
    [Delphi(PortStatus.Ported)] public string MenuItemName { get; }
    [Delphi(PortStatus.Ported)] public TPlugInType PlugInType { get; }
    [Delphi(PortStatus.Ported)] public TBoldUMLPluginOptions Options { get; }
    [Delphi(PortStatus.Ported)] public Assembly? Instance { get; }
    [Delphi(PortStatus.Ported)] public string ImageResourceName { get; }
    [Delphi(PortStatus.Ported)] public Color ImageMaskColor { get; }
    [Delphi(PortStatus.Ported)] public string PluginClassName { get; }

    /// <summary>Runs the plug-in unless one runs already.</summary>
    [Delphi(PortStatus.Ported)]
    public void GuardedExecute(IUMLModelPlugInContext Context)
    {
        if (Context.IsExecutingPlugin)
        {
            Dialogs.ShowMessage("Already executing a plugin");
            return;
        }

        Context.IsExecutingPlugin = true;
        try
        {
            UnGuardedExecute(Context);
        }
        finally
        {
            Context.IsExecutingPlugin = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void UnGuardedExecute(IUMLModelPlugInContext Context) => OnExecute?.Invoke(Context);

    public override string ToString() => MenuItemName;
}

/// <summary>
/// The plug-ins of the model editor. Delphi's plug-in units put their plug-ins in the editor's list when they were
/// loaded; here the built-in ones are put there when the list is first asked for.
/// </summary>
public static class BoldUMLPlugInRegistry
{
    private static readonly List<TUMLPlugIn> fPlugIns = [];
    private static bool fBuiltInsInstalled;

    /// <summary>Plug-in sets of other assemblies (the WinForms editor's) install themselves through this.</summary>
    public static event Action? InstallingBuiltIns;

    /// <summary>The plug-ins, the built-in ones installed.</summary>
    public static List<TUMLPlugIn> PlugIns
    {
        get
        {
            EnsureBuiltIns();
            return fPlugIns;
        }
    }

    /// <summary>The list without installing anything (for plug-ins that install or remove themselves).</summary>
    internal static List<TUMLPlugIn> RawList => fPlugIns;

    public static void EnsureBuiltIns()
    {
        if (fBuiltInsInstalled)
        {
            return;
        }

        fBuiltInsInstalled = true;
        BoldUMLPlugins.Initialize();
        BoldCodePlugins.Initialize();
        BoldDbPlugins.Initialize();
        InstallingBuiltIns?.Invoke();
    }
}
