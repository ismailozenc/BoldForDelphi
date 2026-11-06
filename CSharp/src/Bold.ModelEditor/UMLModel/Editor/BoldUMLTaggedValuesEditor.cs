// Delphi: Source/UMLModel/Editor/BoldUMLTaggedValuesEditor.pas
// Port map: CSharp/port/units/BoldUMLTaggedValuesEditor.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValuesEditor", Symbol = "TfrmBoldUMLTaggedValuesEditor.GetStringListType", Note = "Typed the derived list of tools in Bold's object space; the tools are a list of strings.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValuesEditor", Symbol = "TfrmBoldUMLTaggedValuesEditor.BoldAsStringRenderer1Subscribe", Note = "The UML graph tells the editor of every change.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValuesEditor", Symbol = "TfrmBoldUMLTaggedValuesEditor.DoMessage", Note = "Cut, copy and paste call the text box's methods, where Delphi sent Windows messages.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValuesEditor", Symbol = "initialization", Note = "The section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValuesEditor", Symbol = "WM_REFRESHGUI", Note = "The message that showed the editor again after a change; the editor posts the refresh with BeginInvoke.")]

namespace Bold;

/// <summary>
/// The tagged values of the element the model editor shows, a tab for each tool (the part of the tag before its dot):
/// the tags of the tool on the left, the value of the chosen one on the right.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Built in code; follows the editor's element, as Delphi's followed its handle.")]
public class TfrmBoldUMLTaggedValuesEditor : Form
{
    public readonly Panel Panel1;
    public readonly TabControl tcTools;
    public readonly ListBox lbTagName;
    public readonly Splitter Splitter1;
    public readonly TextBox bmValue;
    public readonly MenuStrip MainMenu1;
    public readonly ToolStripMenuItem Edit1;
    public readonly ToolStripMenuItem Copy1;
    public readonly ToolStripMenuItem Cut1;
    public readonly ToolStripMenuItem Paste1;
    public readonly ToolStripMenuItem mnuTools;
    public readonly ToolStripMenuItem Addtaggedvalue1;
    public readonly ToolStripMenuItem Deletetaggedvalue1;
    private readonly TBoldModelEditController? fController;
    private TBoldUMLObject? fRoot;
    private int PreviousSelectedTab;
    private bool fRefreshing;
    private bool fRefreshPosted;

    public TfrmBoldUMLTaggedValuesEditor(TBoldModelEditController? Controller = null)
    {
        Text = "Bold UML tagged values editor";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(368, 376);
        lbTagName = new ListBox { Dock = DockStyle.Left, Width = 121, IntegralHeight = false };
        Splitter1 = new Splitter { Dock = DockStyle.Left, Width = 6 };
        bmValue = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, AcceptsReturn = true };
        tcTools = new TabControl { Dock = DockStyle.Top, Height = 24 };
        Panel1 = new Panel { Dock = DockStyle.Fill };
        Panel1.Controls.Add(bmValue);
        Panel1.Controls.Add(Splitter1);
        Panel1.Controls.Add(lbTagName);
        Panel1.Controls.Add(tcTools);
        Copy1 = new ToolStripMenuItem("Copy", null, Copy1Click) { ShortcutKeys = Keys.Control | Keys.C };
        Cut1 = new ToolStripMenuItem("Cut", null, Cut1Click) { ShortcutKeys = Keys.Control | Keys.X };
        Paste1 = new ToolStripMenuItem("Paste", null, Paste1Click) { ShortcutKeys = Keys.Control | Keys.V };
        Edit1 = new ToolStripMenuItem("&Edit");
        Edit1.DropDownItems.AddRange([Copy1, Cut1, Paste1]);
        Addtaggedvalue1 = new ToolStripMenuItem("&Add tagged value", null, btAddTVClick);
        Deletetaggedvalue1 = new ToolStripMenuItem("&Delete tagged value", null, btDeleteTVClick);
        mnuTools = new ToolStripMenuItem("&Tools");
        mnuTools.DropDownItems.AddRange([Addtaggedvalue1, Deletetaggedvalue1]);
        MainMenu1 = new MenuStrip();
        MainMenu1.Items.AddRange([Edit1, mnuTools]);
        MainMenuStrip = MainMenu1;
        Controls.Add(Panel1);
        Controls.Add(MainMenu1);
        tcTools.SelectedIndexChanged += tcToolsChange;
        lbTagName.SelectedIndexChanged += (_, _) => ShowValue();
        bmValue.Validated += (_, _) => ApplyValue();
        FormClosed += FormClose;
        fController = Controller;
        FormCreate();
    }

    /// <summary>The element whose tagged values are on show.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldUMLObject? Root
    {
        get => fRoot;
        set
        {
            if (fRoot != value)
            {
                fRoot = value;
                PostRefresh();
            }
        }
    }

    /// <summary>The tool of a tag: the part before its dot, or "&lt;Default&gt;".</summary>
    [Delphi(PortStatus.Ported)]
    public static string GetToolName(string tag)
    {
        int p = tag.IndexOf('.');
        return p < 0 ? BoldDefs.DEFAULTNAME : tag[..p];
    }

    /// <summary>The name of a tag after its tool.</summary>
    [Delphi(PortStatus.Ported)]
    public static string GetTagName(string tag)
    {
        int p = tag.IndexOf('.');
        return p < 0 ? tag : tag[(p + 1)..];
    }

    /// <summary>The tools of the element's tagged values, in the order of the tagged values.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the tools; there is nothing to subscribe to.")]
    public static List<string> bdhAllToolsDeriveAndSubscribe(TBoldUMLObject? RootValue)
    {
        var result = new List<string>();
        foreach (TBoldUMLObject taggedValue in RootValue?.TaggedValues ?? [])
        {
            string tool = GetToolName(taggedValue["tag"]);
            if (!result.Contains(tool))
            {
                result.Add(tool);
            }
        }

        return result;
    }

    /// <summary>The element's tagged values of a tool, by tag.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the tagged values, ordered by tag as Delphi's list handle ordered them.")]
    public static List<TBoldUMLObject> bdhSelectedTVsDeriveAndSubscribe(TBoldUMLObject? RootValue, string Tool) =>
        [.. (RootValue?.TaggedValues ?? []).Where(t => GetToolName(t["tag"]) == Tool).OrderBy(t => t["tag"], StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The tagged values of the tool on show.</summary>
    public List<TBoldUMLObject> SelectedTVs => tcTools.SelectedIndex < 0 ? [] : bdhSelectedTVsDeriveAndSubscribe(fRoot, tcTools.TabPages[tcTools.SelectedIndex].Text);

    /// <summary>The tagged value chosen.</summary>
    public TBoldUMLObject? CurrentTV => lbTagName.SelectedIndex >= 0 && lbTagName.SelectedIndex < SelectedTVs.Count ? SelectedTVs[lbTagName.SelectedIndex] : null;

    [Delphi(PortStatus.Ported)]
    private void FormClose(object? Sender, FormClosedEventArgs e) => Dispose();

    [Delphi(PortStatus.Ported)]
    private void CreateTabs()
    {
        fRefreshing = true;
        try
        {
            tcTools.TabPages.Clear();
            foreach (string tool in bdhAllToolsDeriveAndSubscribe(fRoot))
            {
                tcTools.TabPages.Add(tool);
            }

            if (tcTools.TabCount > 0)
            {
                tcTools.SelectedIndex = PreviousSelectedTab < tcTools.TabCount ? PreviousSelectedTab : 0;
            }
        }
        finally
        {
            fRefreshing = false;
        }

        tcToolsChange(null, EventArgs.Empty);
    }

    [Delphi(PortStatus.Ported)]
    private void tcToolsChange(object? Sender, EventArgs e)
    {
        if (fRefreshing)
        {
            return;
        }

        string? selected = CurrentTV?["tag"];
        lbTagName.BeginUpdate();
        lbTagName.Items.Clear();
        foreach (TBoldUMLObject taggedValue in SelectedTVs)
        {
            lbTagName.Items.Add(BoldAsStringRenderer1GetAsString(taggedValue));
        }

        lbTagName.EndUpdate();
        int index = SelectedTVs.FindIndex(t => t["tag"] == selected);
        lbTagName.SelectedIndex = index >= 0 ? index : (lbTagName.Items.Count > 0 ? 0 : -1);
        ShowValue();
    }

    /// <summary>A tagged value shows by its name, without its tool.</summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldAsStringRenderer1GetAsString(TBoldUMLObject? Element) => Element is null ? "" : GetTagName(Element["tag"]);

    private void ShowValue()
    {
        TBoldUMLObject? taggedValue = CurrentTV;
        bmValue.Enabled = taggedValue is not null;
        bmValue.Text = taggedValue?["value"] ?? "";
    }

    private void ApplyValue()
    {
        if (CurrentTV is { } taggedValue && taggedValue["value"] != bmValue.Text)
        {
            taggedValue["value"] = bmValue.Text;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Follows the model editor's element and the model's changes.")]
    private void PlaceSubscriptions()
    {
        if (fController is not null)
        {
            fController.CurrentElementChanged -= RecieveRootChanged;
            fController.CurrentElementChanged += RecieveRootChanged;
            fController.ModelChanged -= ModelChanged;
            fController.ModelChanged += ModelChanged;
        }
    }

    private void ModelChanged(TBoldUMLObject Element)
    {
        if (Element.IsKindOf("TaggedValue") || Element == fRoot)
        {
            PostRefresh();
        }
    }

    /// <summary>The element on show changed: the form shows the new one's tagged values when idle.</summary>
    [Delphi(PortStatus.Ported)]
    private void RecieveRootChanged()
    {
        Root = fController?.CurrentElement;
        PostRefresh();
    }

    private void PostRefresh()
    {
        if (fRefreshPosted || IsDisposed)
        {
            return;
        }

        if (IsHandleCreated)
        {
            fRefreshPosted = true;
            BeginInvoke((MethodInvoker)(() =>
            {
                fRefreshPosted = false;
                RefreshGUI();
            }));
        }
        else
        {
            RefreshGUI();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Runs from BeginInvoke where Delphi posted WM_REFRESHGUI.")]
    public void RefreshGUI()
    {
        if (IsDisposed)
        {
            return;
        }

        CreateTabs();
        Text = fRoot is null ? "Tagged values" : $"Tagged values for the {fRoot.MetaClass.ToLowerInvariant()} {fRoot.name}.";
    }

    [Delphi(PortStatus.Ported)]
    private void FormCreate()
    {
        PlaceSubscriptions();
        Root = fController?.CurrentElement;
        RefreshGUI();
    }

    [Delphi(PortStatus.Ported, Note = "Asks with the add dialog and shows the new value.")]
    private void btAddTVClick(object? Sender, EventArgs e)
    {
        if (fRoot is null)
        {
            return;
        }

        PreviousSelectedTab = tcTools.SelectedIndex;
        TfrmBoldUMLAddTV.ShowAddTVDialog(tcTools.SelectedIndex >= 0 ? tcTools.TabPages[tcTools.SelectedIndex].Text : "Bold", fRoot);
        RefreshGUI();
    }

    [Delphi(PortStatus.Ported)]
    private void btDeleteTVClick(object? Sender, EventArgs e)
    {
        PreviousSelectedTab = tcTools.SelectedIndex;
        if (fRoot is not null && CurrentTV is { } taggedValue)
        {
            fRoot.DeleteTaggedValue(taggedValue["tag"]);
            RefreshGUI();
        }
    }

    /// <summary>Read only, the tools menu goes and the lists are greyed.</summary>
    [Delphi(PortStatus.Ported)]
    public void SwitchMode(bool ReadOnly)
    {
        mnuTools.Visible = !ReadOnly;
        bmValue.ReadOnly = ReadOnly;
        lbTagName.BackColor = ReadOnly ? SystemColors.InactiveCaption : SystemColors.Window;
        bmValue.BackColor = ReadOnly ? SystemColors.InactiveCaption : SystemColors.Window;
    }

    [Delphi(PortStatus.Ported)]
    private void Copy1Click(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Copy();

    [Delphi(PortStatus.Ported)]
    private void Cut1Click(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Cut();

    [Delphi(PortStatus.Ported)]
    private void Paste1Click(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Paste();

    /// <summary>Shows the tagged values of an element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "ShowTaggedValuesEditor", Note = "Shows the element's tagged values, which Delphi's did not set.")]
    public static TfrmBoldUMLTaggedValuesEditor ShowTaggedValuesEditor(TBoldUMLObject UMLElement)
    {
        var editor = new TfrmBoldUMLTaggedValuesEditor { Root = UMLElement };
        editor.RefreshGUI();
        editor.Show();
        return editor;
    }

    [Delphi(PortStatus.Ported, Symbol = "TfrmBoldUMLTaggedValuesEditor.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fController is not null)
        {
            fController.CurrentElementChanged -= RecieveRootChanged;
            fController.ModelChanged -= ModelChanged;
        }

        base.Dispose(disposing);
    }
}
