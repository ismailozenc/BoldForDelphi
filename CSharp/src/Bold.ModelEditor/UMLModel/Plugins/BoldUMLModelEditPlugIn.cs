// Delphi: Source/UMLModel/Plugins/BoldUMLModelEditPlugIn.pas
// Port map: CSharp/port/units/BoldUMLModelEditPlugIn.md
// The menu item and the tool button that run a plug-in; the plug-in framework is in Bold.ModelEditor.Core.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>A menu item that runs a plug-in.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLPlugInMenuItem : ToolStripMenuItem
{
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TUMLPlugIn? PlugIn { get; set; }
}

/// <summary>A tool button that runs a plug-in.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLPlugInToolButton : ToolStripButton
{
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TUMLPlugIn? PlugIn { get; set; }
}

/// <summary>The bitmaps of plug-ins.</summary>
public static class TUMLPlugInImages
{
    /// <summary>
    /// The plug-in's bitmap, embedded in its assembly by its image resource name, transparent where it has the plug-in's
    /// mask color; null when its assembly has none.
    /// </summary>
    public static Bitmap? ImageOf(TUMLPlugIn PlugIn)
    {
        using Stream? stream = PlugIn.Instance?.GetManifestResourceStream("Bold.PlugInImages." + PlugIn.ImageResourceName);
        if (stream is null)
        {
            return null;
        }

        var bitmap = new Bitmap(stream);
        bitmap.MakeTransparent(PlugIn.ImageMaskColor);
        return bitmap;
    }
}
