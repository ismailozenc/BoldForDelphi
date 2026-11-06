// Delphi: Source/UMLModel/Editor/BoldUMLModelEdit.pas
// Port map: CSharp/port/units/BoldUMLModelEdit.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEdit", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>The model editor: a form for each model component, with the plug-ins on its menus and tool bar.</summary>
[Delphi(PortStatus.Ported, Note = "Not a component: it hears its forms' Disposed events, where Delphi had free notification.")]
public class TModelEdit : IDisposable
{
    private static TModelEdit? G_ModelEditor;
    private readonly List<TBoldModelEditFrm> fEditForms = [];

    [Delphi(PortStatus.Ported)]
    public TModelEdit()
    {
    }

    /// <summary>The model editor, made when first asked for.</summary>
    [Delphi(PortStatus.Ported, Symbol = "UMLModelEditor")]
    public static TModelEdit UMLModelEditor() => G_ModelEditor ??= new TModelEdit();

    [Delphi(PortStatus.Ported, Symbol = "UMLModelEditorAssigned")]
    public static bool UMLModelEditorAssigned() => G_ModelEditor is not null;

    /// <summary>The open forms.</summary>
    [Delphi(PortStatus.Ported)]
    private List<TBoldModelEditFrm> EditForms => fEditForms;

    /// <summary>The plug-ins: the built-in ones of Bold.ModelEditor.Core and the OCL validator.</summary>
    [Delphi(PortStatus.Ported, Note = "The plug-in registry of Bold.ModelEditor.Core, where Delphi's plug-in units added themselves to this list.")]
    public IReadOnlyList<TUMLPlugIn> PlugInList
    {
        get
        {
            BoldUMLPlugInRegistry.EnsureBuiltIns();
            TUMLOCLValidator.Initialize();
            return BoldUMLPlugInRegistry.PlugIns;
        }
    }

    /// <summary>Closes the forms.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TModelEdit.Destroy", Note = "The plug-ins stay in the registry, as Delphi freed them in their own units.")]
    public void Dispose()
    {
        while (EditForms.Count > 0)
        {
            EditForms[^1].Dispose(); // leaves the list through Notification
        }

        if (G_ModelEditor == this)
        {
            G_ModelEditor = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>The form that edits the model component, if one is open.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldModelEditFrm? FindFormForBoldModel(TBoldModel BoldModel) => EditForms.FirstOrDefault(f => f.ModelHandle == BoldModel);

    /// <summary>A form for the model component, with the plug-ins and the size and place it had last.</summary>
    [Delphi(PortStatus.Ported, Note = "The form edits the model component's UML graph; Delphi made the data module of its UML object space first.")]
    private TBoldModelEditFrm CreateFormForBoldModel(TBoldModel BoldModel)
    {
        var aForm = new TBoldModelEditFrm();
        aForm.Disposed += Notification;
        EditForms.Add(aForm);
        aForm.ModelHandle = BoldModel;
        aForm.PlugInList = PlugInList;
        LoadFormsettingsFromRegistry(aForm);
        return aForm;
    }

    /// <summary>A form went: it leaves the list.</summary>
    [Delphi(PortStatus.Ported, Note = "Hears the form's Disposed event.")]
    private void Notification(object? Sender, EventArgs e)
    {
        if (Sender is TBoldModelEditFrm form)
        {
            EditForms.Remove(form);
        }
    }

    /// <summary>Shows the form that edits the model component, made if there is none, and gives it.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the form.")]
    public TBoldModelEditFrm ShowEditFormForBoldModel(TBoldModel BoldModel)
    {
        TBoldModelEditFrm aForm = FindFormForBoldModel(BoldModel) ?? CreateFormForBoldModel(BoldModel);
        aForm.Show();
        return aForm;
    }

    /// <summary>The names of the splits between the fields and the lists of the pages, with their registry values.</summary>
    private static readonly (string ScrollBox, string Value)[] Splits =
    [
        ("sbModel", "ModelHeight"), ("sbClass", "ClassHeight"), ("sbOperation", "OperationHeight"),
        ("sbAssociation", "AssociationHeight"), ("sbAssociationEnd", "AssociationEndHeight"),
    ];

    /// <summary>Keeps the size and place of the form in the registry; errors are ignored.</summary>
    [Delphi(PortStatus.Ported, Note = "The heights of Delphi's scroll boxes are the split distances of the pages.")]
    public static void SaveFormsettingsToRegistry(TBoldModelEditFrm aForm)
    {
        try
        {
            using var BoldRegistry = new TBoldRegistry();
            BoldRegistry.OpenKey("\\UMLModel");
            Rectangle bounds = aForm.WindowState == FormWindowState.Normal ? aForm.Bounds : aForm.RestoreBounds;
            BoldRegistry.WriteInteger("Width", bounds.Width);
            BoldRegistry.WriteInteger("Height", bounds.Height);
            BoldRegistry.WriteInteger("Left", bounds.Left);
            BoldRegistry.WriteInteger("Top", bounds.Top);
            BoldRegistry.WriteInteger("TreeViewWidth", aForm.BoldTreeView1.Width);
            foreach ((string scrollBox, string value) in Splits)
            {
                BoldRegistry.WriteInteger(value, aForm.SplitDistance(scrollBox));
            }
        }
        catch (Exception e) when (IsRegistryError(e))
        {
            // Silently discard exceptions, as Delphi did.
        }
    }

    /// <summary>Gives the form the size and place it had last; errors are ignored.</summary>
    [Delphi(PortStatus.Ported, Note = "The form keeps its centered place when none was kept.")]
    private static void LoadFormsettingsFromRegistry(TBoldModelEditFrm aForm)
    {
        try
        {
            using var BoldRegistry = new TBoldRegistry();
            BoldRegistry.OpenKey("\\UMLModel");
            aForm.Width = BoldRegistry.ReadInteger("Width", 800);
            aForm.Height = BoldRegistry.ReadInteger("Height", 600);
            if (BoldRegistry.ValueExists("Left") && BoldRegistry.ValueExists("Top"))
            {
                var place = new Point(BoldRegistry.ReadInteger("Left", 0), BoldRegistry.ReadInteger("Top", 0));
                if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(place)))
                {
                    aForm.StartPosition = FormStartPosition.Manual;
                    aForm.Location = place;
                }
            }

            aForm.BoldTreeView1.Width = BoldRegistry.ReadInteger("TreeViewWidth", aForm.BoldTreeView1.Width);
            foreach ((string scrollBox, string value) in Splits)
            {
                aForm.SetSplitDistance(scrollBox, BoldRegistry.ReadInteger(value, aForm.SplitDistance(scrollBox)));
            }
        }
        catch (Exception e) when (IsRegistryError(e))
        {
            // Silently discard exceptions, as Delphi did.
        }
    }

    private static bool IsRegistryError(Exception e) =>
        e is IOException or UnauthorizedAccessException or System.Security.SecurityException or PlatformNotSupportedException or InvalidOperationException
            or ArgumentException;

    /// <summary>The editor goes with the process; this closes its forms first.</summary>
    [Delphi(PortStatus.Ported, Symbol = "finalization")]
    public static void Finalize_() => G_ModelEditor?.Dispose();
}
