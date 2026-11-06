// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>The model editor's form on the Buildings and Owners model, used as a user would: the tree, the pages, the menus.</summary>
[NonParallelizable]
public class BoldUMLModelEditFormTests
{
    private Container fComponents = null!;
    private TBoldModel fModel = null!;
    private TBoldModelEditFrm fForm = null!;

    [SetUp]
    public void SetUp()
    {
        fComponents = new Container();
        fModel = EditorSamples.ModelComponent(fComponents);
        fForm = EditorForms.Open(fModel);
    }

    [TearDown]
    public void TearDown()
    {
        if (!fForm.IsDisposed)
        {
            fForm.Dispose();
        }

        fComponents.Dispose();
        EditorForms.CloseAll();
    }

    private TBoldModelEditController Controller => fForm.Controller;

    private TBoldUMLObject Model => Controller.CurrentModel!;

    private TBoldUMLObject Class(string name) => Model.Classes.Single(c => c.name == name);

    private TBoldUMLObject Feature(string className, string name) => Class(className).feature.Single(f => f.name == name);

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "FormCreate", "FormShow", nameof(TBoldModelEditFrm.GetNodeForElement), "BoldTreeView1Expanded")]
    public void The_tree_shows_the_model()
    {
        TreeNode root = fForm.BoldTreeView1.Nodes[0];
        Assert.That((root.Text, root.Tag), Is.EqualTo(("BuildingsAndOwners", (object)Model)));
        TreeNode logicalView = EditorForms.Node(fForm, "Logical View");
        Assert.That(root.IsExpanded && logicalView.IsExpanded, Is.True, "the model and its logical view are open");
        Assert.That(EditorForms.Texts(logicalView), Is.SupersetOf(new[] { "Building", "BusinessClassesRoot", "Person", "Residential Building", "Associations", "DataTypes" }));
        Assert.That(EditorForms.Node(fForm, "Logical View", "BusinessClassesRoot").ForeColor, Is.EqualTo(Color.Red), "the root class was made by boldifying");
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View", "Person")),
            Is.SupersetOf(new[] { "FirstName", "LastName", "Assets", "Home (Residential Building)", "OwnedBuildings (Building)", "CompleteCreate", "BorrowFrom" }));
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View", "Associations")), Is.SupersetOf(new[] { "Residence" }));
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View", "Associations", "Residence")),
            Is.EquivalentTo(new[] { "Residents (Person)", "Home (Residential Building)" }));
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View", "DataTypes")), Does.Contain("String"));
        Assert.That(fForm.GetNodeForElement(Class("Person")), Is.SameAs(EditorForms.Node(fForm, "Logical View", "Person")));
        Assert.That(fForm.BoldTreeView1.SelectedNode, Is.SameAs(root), "the model is on show");
        Assert.That(fForm.ActivePage, Is.EqualTo(TBoldModelEditPage.Model));
        Assert.That(EditorForms.Field<TextBox>(fForm, "tbxModelName").Text, Is.EqualTo("BuildingsAndOwners"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "BoldTreeView1Change", nameof(TBoldModelEditFrm.ApplyGUI))]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.CurrentElement))]
    public void Choosing_a_node_shows_its_page()
    {
        TBoldUMLObject person = Class("Person");
        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View", "Person"));
        Assert.That((Controller.CurrentElement, fForm.ActivePage), Is.EqualTo((person, TBoldModelEditPage.Class)));
        TextBox name = EditorForms.Field<TextBox>(fForm, "tbxClassName");
        Assert.That((name.Visible, name.Text), Is.EqualTo((true, "Person")));
        Assert.That(EditorForms.Field<CheckBox>(fForm, "bcbClassPersistent").Checked, Is.True);
        Assert.That(EditorForms.Field<ComboBox>(fForm, "cmbSuperClass").Text, Is.EqualTo("BusinessClassesRoot"));
        DataGridView attributes = EditorForms.Field<DataGridView>(fForm, "BoldGrid3");
        Assert.That(attributes.Rows.Count, Is.EqualTo(person.feature.Count(f => f.IsKindOf("Attribute"))));

        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View", "Person", "FirstName"));
        Assert.That((Controller.CurrentElement, fForm.ActivePage), Is.EqualTo((Feature("Person", "FirstName"), TBoldModelEditPage.Attribute)));
        Assert.That(EditorForms.Field<TextBox>(fForm, "tbxAttributeName").Text, Is.EqualTo("FirstName"));
        Assert.That(EditorForms.Field<ComboBox>(fForm, "cmbAttributeBoldType").Text, Is.EqualTo("String"));

        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View", "Associations"));
        Assert.That(Controller.CurrentElement, Is.SameAs(Model), "a folder shows the element it is in");
        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View", "Associations", "Residence"));
        Assert.That(fForm.ActivePage, Is.EqualTo(TBoldModelEditPage.Association));
        Assert.That(EditorForms.Field<TextBox>(fForm, "tbxAssociationName").Text, Is.EqualTo("Residence"));
        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View", "Person", "CompleteCreate"));
        Assert.That(fForm.ActivePage, Is.EqualTo(TBoldModelEditPage.Operation));
    }

    [Test]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.CurrentElement))]
    public void Editing_a_field_changes_the_model()
    {
        TBoldUMLObject person = Class("Person");
        EditorForms.Select(fForm, person);
        TextBox name = EditorForms.Field<TextBox>(fForm, "tbxClassName");
        name.Text = "Human";
        Modal.Raise(name, "OnKeyDown", new KeyEventArgs(Keys.Enter));
        Assert.That(person.name, Is.EqualTo("Human"));
        Assert.That(fForm.GetNodeForElement(person)!.Text, Is.EqualTo("Human"), "the tree shows the new name");

        CheckBox persistent = EditorForms.Field<CheckBox>(fForm, "bcbClassPersistent");
        Modal.Raise(persistent, "OnClick", EventArgs.Empty);
        Assert.That((person.persistent, persistent.Checked), Is.EqualTo((false, false)));
        Modal.Raise(persistent, "OnClick", EventArgs.Empty);
        Assert.That(person.persistent, Is.True);

        TBoldUMLObject firstName = Feature("Person", "FirstName");
        EditorForms.Select(fForm, firstName);
        ComboBox type = EditorForms.Field<ComboBox>(fForm, "cmbAttributeBoldType");
        Assert.That(type.Items.Cast<object>().Select(i => i.ToString()), Does.Contain("Integer"));
        type.SelectedItem = "Integer";
        EditorForms.Raise(type, "OnSelectionChangeCommitted", EventArgs.Empty);
        Assert.That(firstName.typeName, Is.EqualTo("Integer"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), nameof(TBoldModelEditFrm.JumpToElement), nameof(TBoldModelEditFrm.EnsureOwnerExpanded))]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.NewAttribute), nameof(TBoldModelEditController.NewOperation),
        nameof(TBoldModelEditController.NewParameter), nameof(TBoldModelEditController.NewClass), nameof(TBoldModelEditController.DeleteXxx))]
    public void The_commands_make_and_delete_elements()
    {
        TBoldUMLObject person = Class("Person");
        EditorForms.Select(fForm, person);
        Assert.That(fForm.NewAttribute1.Available && fForm.NewAttribute2.Enabled, Is.True);
        Assert.That(fForm.NewParameter1.Available || fForm.NewParameter2.Enabled, Is.False, "no parameter for a class");
        Assert.That(fForm.DeleteXxx1.Text, Is.EqualTo("Delete Class Person"));

        fForm.NewAttribute1.PerformClick();
        TBoldUMLObject attribute = Controller.CurrentElement!;
        Assert.That((attribute.IsKindOf("Attribute"), attribute.owner), Is.EqualTo((true, person)));
        Assert.That(fForm.BoldTreeView1.SelectedNode!.Tag, Is.SameAs(attribute), "the new attribute is on show");
        Assert.That(fForm.ActivePage, Is.EqualTo(TBoldModelEditPage.Attribute));

        Modal.Raise(fForm.BoldTreeView1, "OnKeyDown", new KeyEventArgs(Keys.Delete));
        Assert.That(person.feature, Does.Not.Contain(attribute));
        Assert.That(fForm.GetNodeForElement(attribute), Is.Null);
        Assert.That(Controller.CurrentElement, Is.SameAs(person), "the class is on show again");

        fForm.NewOperation2.PerformClick();
        TBoldUMLObject operation = Controller.CurrentElement!;
        Assert.That(operation.IsKindOf("Operation") && operation.owner == person, Is.True);
        Assert.That(fForm.NewParameter2.Enabled, Is.True);
        fForm.NewParameter2.PerformClick();
        Assert.That(operation.parameter, Has.Count.EqualTo(1));
        Assert.That(fForm.GetNodeForElement(operation.parameter[0]), Is.Not.Null);

        EditorForms.Select(fForm, EditorForms.Node(fForm, "Logical View"));
        fForm.NewClass2.PerformClick();
        TBoldUMLObject newClass = Controller.CurrentElement!;
        Assert.That(newClass.IsKindOf("Class") && Model.Classes.Contains(newClass), Is.True);
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View")), Does.Contain(newClass.name));
        fForm.Deletexxx2.PerformClick();
        Assert.That(Model.Classes, Does.Not.Contain(newClass));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "mnuShowAttributesClick", "mnuShowRolesClick", "mnuShowOperationsClick", "tbnFilterAttributesClick",
        "tbnFilterRolesClick", "tbnFilterOperationsClick")]
    public void The_view_filters_what_the_tree_shows()
    {
        string[] Person() => EditorForms.Texts(EditorForms.Node(fForm, "Logical View", "Person"));
        fForm.mnuShowAttributes.PerformClick();
        Assert.That((fForm.mnuShowAttributes.Checked, fForm.tbnFilterAttributes.Checked), Is.EqualTo((false, false)));
        Assert.That(Person(), Does.Not.Contain("FirstName").And.Contain("CompleteCreate"));
        fForm.tbnFilterOperations.PerformClick();
        Assert.That(Person(), Does.Not.Contain("CompleteCreate").And.Contain("Home (Residential Building)"));
        fForm.tbnFilterRoles.PerformClick();
        Assert.That(Person(), Is.Empty);
        fForm.tbnFilterAttributes.PerformClick();
        fForm.mnuShowOperations.PerformClick();
        fForm.mnuShowRoles.PerformClick();
        Assert.That(Person(), Is.SupersetOf(new[] { "FirstName", "CompleteCreate", "Home (Residential Building)" }));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "mnuAdvancedClick")]
    public void The_advanced_mode_shows_more_fields()
    {
        EditorForms.Select(fForm, Class("Person"));
        TextBox delphiName = EditorForms.Field<TextBox>(fForm, "tbxClassDelphiName");
        Assert.That(delphiName.Visible, Is.False);
        fForm.mnuAdvanced.PerformClick();
        Assert.That((Controller.IsAdvancedMode, delphiName.Visible), Is.EqualTo((true, true)));
        fForm.mnuAdvanced.PerformClick();
        Assert.That(delphiName.Visible, Is.False);
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), nameof(TBoldModelEditFrm.PlugInList), "SetPlugInList", "InstallPlugIn", "AddTool", "AddFiler", "AddMenuTool",
        "AddMenuFiler", "AddButtonTool", "AddButtonFiler", "NewPlugInMenuItem", "NewPlugInToolButton")]
    public void The_plug_ins_are_on_the_menus_and_the_tool_bar()
    {
        TUMLPlugIn[] shown = [.. fForm.PlugInList.Where(p => p.MenuItemName != "")];
        Assert.That(shown.Select(p => p.MenuItemName), Does.Contain("Validate OCL in model"));
        TUMLPlugInMenuItem[] tools = [.. fForm.Tools1.DropDownItems.OfType<TUMLPlugInMenuItem>()];
        TUMLPlugInMenuItem[] filers = [.. fForm.mnuFile.DropDownItems.OfType<TUMLPlugInMenuItem>()];
        Assert.That(tools.Select(i => i.PlugIn), Is.EquivalentTo(shown.Where(p => p.PlugInType == TPlugInType.ptTool)));
        Assert.That(filers.Select(i => i.PlugIn), Is.EquivalentTo(shown.Where(p => p.PlugInType == TPlugInType.ptFile)));
        Assert.That(tools.All(i => i.Text == i.PlugIn!.MenuItemName), Is.True);
        Assert.That(fForm.ToolBar1.Items.OfType<TUMLPlugInToolButton>().Select(b => b.PlugIn), Is.EquivalentTo(shown));
        Assert.That(tools.Single(i => i.Text == "Validate OCL in model").Image, Is.Not.Null, "the plug-in's bitmap");
        int separator = fForm.Tools1.DropDownItems.IndexOf(fForm.N5);
        Assert.That(fForm.Tools1.DropDownItems.IndexOf(tools[0]), Is.GreaterThan(separator), "the tools come after the editor's own");
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "PlugInMenuClick")]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.ExecPlugIn))]
    public void A_plug_in_runs_from_its_menu_item()
    {
        TUMLPlugInMenuItem validator = fForm.Tools1.DropDownItems.OfType<TUMLPlugInMenuItem>().Single(i => i.Text == "Validate OCL in model");
        using (new DialogAnswers(Controls.mrOk))
        {
            validator.PerformClick();
        }

        Assert.That(Application.OpenForms.OfType<TfrmValidation>().Single().Visible, Is.True, "the validation form shows what the plug-in found");
        Assert.That(Controller.IsExecutingPlugin, Is.False);
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "mnuConsistencycheckClick")]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.StatusText))]
    public void The_consistency_check_validates_the_model()
    {
        Assert.That(fForm.StatusPanel1.Text, Is.EqualTo("Model not validated"));
        using (new DialogAnswers(Controls.mrOk))
        {
            fForm.mnuConsistencycheck.PerformClick();
        }

        TfrmValidation validation = Application.OpenForms.OfType<TfrmValidation>().Single();
        Assert.That(validation.Visible, Is.True);
        Assert.That(validation.BoldGrid1.Rows.Count, Is.EqualTo(Model.Graph.Violations.Count));
        Assert.That(Controller.ModelNeedsValidation, Is.False);
        Assert.That(fForm.StatusPanel1.Text, Is.Not.EqualTo("Model not validated"));
        (string text, _) = Controller.StatusText();
        Assert.That(fForm.StatusPanel1.Text, Is.EqualTo(text));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "Tools1Click")]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Boldifymodel), nameof(TBoldModelEditController.BoldifyFlattenCaptions))]
    public void Unboldifying_takes_the_root_class_away()
    {
        fForm.Tools1.ShowDropDown();
        Assert.That((fForm.Boldifymodel1.Text, fForm.Flatten.Text), Is.EqualTo(("UnBoldify model", "UnFlatten package structure")));
        fForm.Tools1.HideDropDown();
        fForm.Boldifymodel1.PerformClick();
        Assert.That(TBoldUMLBoldify.IsBoldified(Model), Is.False);
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View")), Does.Not.Contain("BusinessClassesRoot"));
        fForm.Boldifymodel1.PerformClick();
        Assert.That(EditorForms.Texts(EditorForms.Node(fForm, "Logical View")), Does.Contain("BusinessClassesRoot"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "btModelConstraintEditorClick")]
    public void The_lists_on_a_page_show_and_choose_elements()
    {
        TBoldUMLObject person = Class("Person");
        EditorForms.Select(fForm, person);
        DataGridView attributes = EditorForms.Field<DataGridView>(fForm, "BoldGrid3");
        DataGridViewRow row = attributes.Rows.Cast<DataGridViewRow>().Single(r => r.Tag == Feature("Person", "LastName"));
        EditorForms.Raise(attributes, "OnCellDoubleClick", new DataGridViewCellEventArgs(-1, row.Index));
        Assert.That(Controller.CurrentElement, Is.SameAs(Feature("Person", "LastName")), "a double click shows the row's element");

        EditorForms.Select(fForm, person);
        fForm.mnuAdvanced.PerformClick(); // the constraints are an advanced field
        Button constraints = EditorForms.Field<Button>(fForm, "btClassConstraintEditor");
        constraints.PerformClick();
        TfrmBoldUMLConstraintEditor editor = Application.OpenForms.OfType<TfrmBoldUMLConstraintEditor>().Single();
        Assert.That(editor.CurrentElement, Is.SameAs(person));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), nameof(TBoldModelEditFrm.EditOclExpression), "btClassDefaultStringRepClick")]
    [Covers(typeof(TBoldUMLOclEditor), nameof(TBoldUMLOclEditor.EditOcl))]
    public void The_OCL_editor_edits_an_expression_of_the_model()
    {
        TBoldUMLObject person = Class("Person");
        EditorForms.Select(fForm, person);
        using (Modal.WhenShown<TBoldOclPropEditForm>(editor =>
        {
            Assert.That(editor.Context?.ExpressionName, Is.EqualTo("Person"), "the expression is on the class");
            editor.OclExpr = "'a person'";
            editor.btnOK.PerformClick();
        }))
        {
            EditorForms.Field<Button>(fForm, "btClassDefaultStringRep").PerformClick();
        }

        Assert.That(person.GetBoldTV(BoldDefaultTaggedValues.TAG_DEFAULTSTRINGREPRESENTATION), Is.EqualTo("'a person'"));
        Assert.That(EditorForms.Field<TextBox>(fForm, "edClassDefaultStringRep").Text, Is.EqualTo("'a person'"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "Notification", "FormDestroy")]
    public void The_form_goes_with_its_model_component()
    {
        fModel.Dispose();
        Assert.That(fForm.IsDisposed, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldModelEditFrm), "FormKeyDown")]
    [Covers(typeof(TBoldModelEditController), nameof(TBoldModelEditController.Cut))]
    public void Escape_forgets_what_was_cut()
    {
        TBoldUMLObject lastName = Feature("Person", "LastName");
        EditorForms.Select(fForm, lastName);
        Assert.That(Controller.Cut(), Is.True);
        Assert.That(Controller.UMLObjectToCopyOrMove, Is.SameAs(lastName));
        Modal.Raise(fForm, "OnKeyDown", new KeyEventArgs(Keys.Escape));
        Assert.That(Controller.UMLObjectToCopyOrMove, Is.Null);
    }
}
