// Delphi: Source/BoldAwareGUI/FormGen/BoldAFPDefault.pas, BoldAFPUser.pas
// Port map: CSharp/port/units/BoldAFPDefault.md
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The default auto forms of objects, attributes, lists and systems, on the Building model.</summary>
[NonParallelizable]
public class BoldAFPDefaultTests
{
    private static readonly MethodInfo OnDragOverMethod = typeof(Control).GetMethod("OnDragOver", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo OnDoubleClickMethod = typeof(Control).GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>Shows the form off the visible screen and displays what is queued.</summary>
    internal static Form ShowOffScreen(Form? form)
    {
        Assert.That(form, Is.Not.Null);
        form!.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-2000, -2000);
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        GuiSample.Display();
        return form;
    }

    internal static T Find<T>(Control parent, string name)
        where T : Control => (T)parent.Controls.Find(name, true).Single();

    internal static Form? FormFor(TBoldElement element) => BoldAFP.AutoFormProviderRegistry().FormForElement(element);

    /// <summary>Closes and disposes the open auto forms (those a test opened by navigating, too).</summary>
    internal static void CloseAutoForms()
    {
        foreach (Form form in BoldAFPDefault.G_BoldActiveAutoForms.Select(entry => entry.Form).ToList())
        {
            form.Dispose();
        }
    }

    [TearDown]
    public void TearDown() => CloseAutoForms();

    private static string[] TabTexts(TabControl pages) => [.. pages.TabPages.Cast<TabPage>().Select(p => p.Text)];

    [Test]
    [Covers(typeof(TBoldDefaultFormProvider), "*")]
    [Covers(typeof(TBoldFormProviderForSingle), "*")]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), "*")]
    [Covers(typeof(TBoldUserFormProvider), "*")]
    [Covers(typeof(TAutoFormList), "*")]
    [Covers(typeof(TAutoFormListEntry), "*")]
    [Covers(typeof(BoldAFPDefault), "*")]
    public void An_object_form_has_a_tab_of_attributes_and_single_roles_a_history_tab_and_a_tab_per_multi_role()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TResidential_Building elm = s.NewHouse("Elm Street");
        ann.Home = elm;
        Form form = ShowOffScreen(FormFor(ann));

        var pages = Find<TabControl>(form, "PageControl");
        Assert.That(pages, Is.InstanceOf<TAFPPageControl>());
        Assert.That(TabTexts(pages), Is.EqualTo(new[] { "Person", "History", "OwnedBuildings" }), "the link role to a link class without members is not shown");
        Assert.That(form.Size, Is.EqualTo(new Size(440, form.Height)));

        var firstName = Find<TBoldEdit>(form, "PersonfirstNameEdit");
        Assert.That(firstName, Is.InstanceOf<TBoldAFEdit>());
        Assert.That(firstName.Text, Is.EqualTo("Ann"));
        Assert.That(firstName.BoldProperties.ApplyPolicy, Is.EqualTo(TBoldApplyPolicy.bapExit));
        Assert.That(Find<Label>(form, "Label1").Text, Is.EqualTo("FirstName:"));
        Assert.That(Find<TBoldEdit>(form, "PersonlastNameEdit").Text, Is.EqualTo("Smith"));

        var home = Find<TBoldEdit>(form, "PersonhomeEdit");
        Assert.That(home.Text, Is.EqualTo("Elm Street"), "a single link shows the first string attribute of the other end");
        Assert.That(home.ReadOnly, Is.True);
        Assert.That(home.BoldProperties.DropMode, Is.EqualTo(TBoldDropMode.bdpReplace));
        Assert.That(home.Parent!.Controls.OfType<TRoleButton>().Single().ContextMenuStrip, Is.InstanceOf<TRoleMenu>());

        Assert.That(form.Text, Does.StartWith("Person: "));
        Assert.That(Find<Button>(form, "CloseButton").Text, Is.EqualTo(BoldGuiResourceStrings.sClose));
        Assert.That(Find<Panel>(form, "ButtonPanel").Controls.OfType<TBoldDropTarget>().Single().IsDragSource, Is.True);

        ann.FirstName = "Anna";
        GuiSample.Display();
        Assert.That(firstName.Text, Is.EqualTo("Anna"));
        Assert.That(form.Text, Does.Contain("Person: "));

        Assert.That(FormFor(ann), Is.SameAs(form), "an open form of the element is reused");
        int index = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form);
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.Entries(index).Element, Is.SameAs(ann));
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.Forms(index), Is.SameAs(form));
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfElement(ann), Is.EqualTo(index));
    }

    [Test]
    [Covers(typeof(TBoldDefaultFormProvider), "*")]
    public void Closing_the_form_applies_the_active_edit_and_lets_the_form_go()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann));
        var edit = Find<TBoldEdit>(form, "PersonfirstNameEdit");
        form.ActiveControl = edit;
        edit.Text = "Bea";
        form.Close();
        Assert.That(ann.FirstName, Is.EqualTo("Bea"), "the edit applies as when it is left");
        Assert.That(form.IsDisposed, Is.True);
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form), Is.EqualTo(-1));

        Form second = ShowOffScreen(FormFor(ann));
        Assert.That(second, Is.Not.SameAs(form), "a closed form is not reused");
    }

    [Test]
    [Covers(typeof(TBoldDefaultFormProvider), "*")]
    [Covers(typeof(TAutoFormListEntry), nameof(TAutoFormListEntry.OnClose), nameof(TAutoFormListEntry.OnDragOver))]
    public void Dragging_over_a_form_brings_it_up_and_its_stored_handlers_run()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann));
        TAutoFormListEntry entry = BoldAFPDefault.G_BoldActiveAutoForms.Entries(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form));
        Assert.That(form.AllowDrop, Is.True);

        var args = new DragEventArgs(null, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);
        BoldGUI.BoldGUIHandler().DraggedObjects.Add(ann);
        try
        {
            OnDragOverMethod.Invoke(form, [args]);
            Assert.That(args.Effect, Is.EqualTo(DragDropEffects.None), "the form takes no drop");

            bool called = false;
            entry.OnDragOver = (_, e) =>
            {
                called = true;
                e.Effect = DragDropEffects.Copy;
            };
            OnDragOverMethod.Invoke(form, [args]);
            Assert.That(called, Is.True);
            Assert.That(args.Effect, Is.EqualTo(DragDropEffects.Copy), "the stored handler may accept");
        }
        finally
        {
            BoldGUI.BoldGUIHandler().DraggedObjects.Clear();
        }

        entry.OnClose = (_, e) => e.Cancel = true;
        form.Close();
        Assert.That(form.IsDisposed, Is.False, "the stored close handler cancelled the close");
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form), Is.Not.EqualTo(-1));
        entry.OnClose = null;
        form.Close();
        Assert.That(form.IsDisposed, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldFormProviderForSingle), "*")]
    [Covers(typeof(TBoldDefaultFormProvider), "*")]
    public void Deleting_the_object_lets_its_form_go()
    {
        using var s = new GuiSample();
        TPerson bob = s.NewPerson("Bob");
        Form form = ShowOffScreen(FormFor(bob));
        bob.Delete();
        Application.DoEvents();
        Assert.That(form.IsDisposed, Is.True);
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form), Is.EqualTo(-1));
    }

    [Test]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), "*")]
    public void A_multi_role_tab_loads_its_list_when_it_shows()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        ann.OwnedBuildings.Add(s.NewHouse("Elm Street"));
        ann.OwnedBuildings.Add(s.NewHouse("Oak Road"));
        Form form = ShowOffScreen(FormFor(ann));
        var pages = Find<TabControl>(form, "PageControl");
        var grid = Find<TBoldGrid>(form, "PersonownedBuildingsGrid");
        var handle = (TBoldListHandle)grid.BoldHandle!;
        Assert.That(handle.Enabled, Is.False, "the list waits for its tab");
        Assert.That(handle.Name, Is.EqualTo("PersonownedBuildingsHandle"));

        pages.SelectedIndex = 2;
        Application.DoEvents();
        GuiSample.Display();
        Assert.That(handle.Enabled, Is.True);
        Assert.That(grid.RowCount, Is.EqualTo(2));
        Assert.That(grid.Parent!.Controls.OfType<TBoldNavigator>().Single().BoldHandle, Is.SameAs(handle));
    }

    [Test]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), "*")]
    [Covers(typeof(BoldAFPDefault), nameof(BoldAFPDefault.BoldShowHistoryTabInAutoForms))]
    public void The_history_tab_says_when_the_class_is_not_versioned_and_can_be_turned_off()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann));
        TabPage history = Find<TabControl>(form, "PageControl").TabPages[1];
        Assert.That(history.Name, Is.EqualTo("tabObjectHistory"));
        Assert.That(history.Controls.OfType<Label>().Select(l => l.Text), Has.Some.Contains(BoldGuiResourceStrings.sObjectNotVersioned.Trim()));
        form.Dispose();

        BoldAFPDefault.BoldShowHistoryTabInAutoForms = false;
        try
        {
            Form without = ShowOffScreen(FormFor(ann));
            Assert.That(TabTexts(Find<TabControl>(without, "PageControl")), Is.EqualTo(new[] { "Person", "OwnedBuildings" }));
        }
        finally
        {
            BoldAFPDefault.BoldShowHistoryTabInAutoForms = true;
        }
    }

    [Test]
    [Covers(typeof(TRoleButton), "*")]
    [Covers(typeof(TRoleMenu), "*")]
    [Covers(typeof(TBoldAFEdit), "*")]
    public void The_role_button_s_menu_creates_unlinks_deletes_and_navigates()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann));
        var home = Find<TBoldEdit>(form, "PersonhomeEdit");
        TRoleButton button = home.Parent!.Controls.OfType<TRoleButton>().Single();
        var menu = (TRoleMenu)button.ContextMenuStrip!;
        Assert.That(menu.Items.Cast<ToolStripItem>().Select(i => i.Text), Is.EqualTo(new[]
        {
            BoldGuiResourceStrings.sNavigate, "", BoldGuiResourceStrings.sCreateNew, BoldGuiResourceStrings.sAddExisting, "",
            BoldGuiResourceStrings.sUnlink, BoldGuiResourceStrings.sDelete,
        }));

        menu.Adjust();
        Assert.That(menu.Items[0].Enabled, Is.False, "nothing to navigate to");
        Assert.That(menu.Items[2].Enabled, Is.True);
        Assert.That(menu.Items[3].Enabled, Is.False);
        Assert.That(menu.Items[5].Enabled, Is.False);
        Assert.That(menu.Items[6].Enabled, Is.False);

        menu.Items[2].PerformClick();
        Assert.That(ann.Home, Is.InstanceOf<TResidential_Building>(), "a new object of the other end's class");
        ann.Home!.Address = "New Street";
        GuiSample.Display();
        Assert.That(home.Text, Is.EqualTo("New Street"));
        menu.Adjust();
        Assert.That(menu.Items[0].Enabled, Is.True);
        Assert.That(menu.Items[2].Enabled, Is.False);
        Assert.That(menu.Items[5].Enabled, Is.True);
        Assert.That(menu.Items[6].Enabled, Is.True);

        button.CreateNewObject(button, EventArgs.Empty);
        Assert.That(s.Environment.Dialogs, Has.Member(BoldGuiResourceStrings.sClearLinkFirst), "the link is not empty");

        TResidential_Building newHome = ann.Home;
        s.Environment.DialogAnswer = Controls.mrNo;
        menu.Items[5].PerformClick();
        Assert.That(ann.Home, Is.SameAs(newHome), "not confirmed");
        s.Environment.DialogAnswer = Controls.mrYes;
        menu.Items[5].PerformClick();
        Assert.That(ann.Home, Is.Null);
        Assert.That(s.Environment.Dialogs, Has.Member(BoldGuiResourceStrings.sAreYouSureToUnlink));

        ann.Home = newHome;
        menu.Items[6].PerformClick();
        Assert.That(newHome.BoldObjectIsDeleted, Is.True);
        Assert.That(s.Environment.Dialogs, Has.Member(BoldGuiResourceStrings.sAreYouSureToDelete));

        TResidential_Building oak = s.NewHouse("Oak Road");
        ann.Home = oak;
        GuiSample.Display();
        button.PerformClick();
        int oakForm = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfElement(oak);
        Assert.That(oakForm, Is.Not.EqualTo(-1), "a click opens the related object's form");
        BoldAFPDefault.G_BoldActiveAutoForms.Forms(oakForm).Dispose();

        OnDoubleClickMethod.Invoke(home, [EventArgs.Empty]);
        oakForm = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfElement(oak);
        Assert.That(oakForm, Is.Not.EqualTo(-1), "a double click on the edit opens the related object's form");
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.Forms(oakForm).Text, Does.StartWith("Residential Building: "));
    }

    [Test]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), "*")]
    [Covers(typeof(TBoldDefaultFormProvider), "*")]
    public void Changes_applied_on_demand_have_apply_cancel_and_OK_buttons()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(new TBoldDefaultObjectAutoFormProvider(ann) { ApplyPolicy = TBoldApplyPolicy.bapDemand }.GenerateAutoForm());
        var edit = Find<TBoldEdit>(form, "PersonfirstNameEdit");
        Assert.That(edit.BoldProperties.ApplyPolicy, Is.EqualTo(TBoldApplyPolicy.bapDemand));
        Assert.That(form.AcceptButton, Is.SameAs(Find<Button>(form, "OKButton")));
        Assert.That(form.CancelButton, Is.SameAs(Find<Button>(form, "CancelButton")));

        edit.Text = "Bea";
        Find<Button>(form, "ApplyButton").PerformClick();
        Assert.That(ann.FirstName, Is.EqualTo("Bea"));

        edit.Text = "Cid";
        Find<Button>(form, "CancelButton").PerformClick();
        Assert.That(ann.FirstName, Is.EqualTo("Bea"), "cancel discards");
        Assert.That(form.IsDisposed, Is.True);

        form = ShowOffScreen(new TBoldDefaultObjectAutoFormProvider(ann) { ApplyPolicy = TBoldApplyPolicy.bapDemand }.GenerateAutoForm());
        Find<TBoldEdit>(form, "PersonfirstNameEdit").Text = "Dee";
        Find<Button>(form, "OKButton").PerformClick();
        Assert.That(ann.FirstName, Is.EqualTo("Dee"));
        Assert.That(form.IsDisposed, Is.True);

        form = ShowOffScreen(new TBoldDefaultObjectAutoFormProvider(ann).GenerateAutoForm());
        Find<Button>(form, "CloseButton").PerformClick();
        Assert.That(form.IsDisposed, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), nameof(TBoldDefaultObjectAutoFormProvider.RegisterClassTypeInfo),
        nameof(TBoldDefaultObjectAutoFormProvider.MemberRTInfoList), nameof(TBoldDefaultObjectAutoFormProvider.DisplaySingleRoleButtons))]
    public void A_form_can_show_an_object_as_another_class_with_other_members()
    {
        using var s = new GuiSample();
        TResidential_Building elm = s.NewHouse("Elm Street");
        TBoldClassTypeInfo building = s.System.BoldSystemTypeInfo.ClassTypeInfoByExpressionName("Building")!;
        var provider = new TBoldDefaultObjectAutoFormProvider(elm) { DisplaySingleRoleButtons = true };
        provider.RegisterClassTypeInfo(building);
        provider.MemberRTInfoList = building.AllMembers;
        Assert.That(provider.DisplaySingleRoleButtons, Is.True);
        Assert.That(provider.MemberRTInfoList, Is.SameAs(building.AllMembers));
        Form form = ShowOffScreen(provider.GenerateAutoForm());
        Assert.That(TabTexts(Find<TabControl>(form, "PageControl")), Is.EqualTo(new[] { "Building", "History", "Owners" }));
        Assert.That(Find<TBoldEdit>(form, "BuildingaddressEdit").Text, Is.EqualTo("Elm Street"));
        Assert.That(form.Text, Does.StartWith("Building: "));
    }

    [Test]
    [Covers(typeof(TBoldDefaultObjectAutoFormProvider), nameof(TBoldDefaultObjectAutoFormProvider.BoldAsStringRenderer1SetColor))]
    public void The_attribute_edits_show_members_that_cannot_change_in_the_inactive_caption_color()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Color color = Color.White;
        TBoldDefaultObjectAutoFormProvider.BoldAsStringRenderer1SetColor(null, ref color, 0, "firstName");
        Assert.That(color, Is.EqualTo(Color.White));
        TBoldDefaultObjectAutoFormProvider.BoldAsStringRenderer1SetColor(ann, ref color, 0, "firstName");
        Assert.That(color, Is.EqualTo(Color.White), "the first name can change");
        Assert.That(BoldAFPDefault.fReadOnlyStringRenderer.OnSetColor, Is.Not.Null);
    }

    [Test]
    [Covers(typeof(TBoldDefaultAttributeAutoFormProvider), "*")]
    public void An_attribute_form_has_the_attribute_s_name_and_an_edit()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann.M_FirstName));
        Assert.That(form.Controls.OfType<Label>().Single().Text, Is.EqualTo("FirstName"));
        Assert.That(form.Controls.OfType<TBoldEdit>().Single().Text, Is.EqualTo("Ann"));
    }

    [Test]
    [Covers(typeof(TBoldFormProviderForList), "*")]
    [Covers(typeof(TBoldDefaultObjectListAutoFormProvider), "*")]
    [Covers(typeof(TBoldDefaultAttributeListAutoFormProvider), "*")]
    [Covers(typeof(TBoldDefaultSystemAutoFormProvider), "*")]
    public void A_list_form_has_a_grid_and_a_navigator()
    {
        using var s = new GuiSample();
        s.NewPerson("Ann");
        s.NewPerson("Bob");
        TBoldObjectList persons = s.System.ClassByExpressionName("Person");
        Form form = ShowOffScreen(FormFor(persons));
        var grid = Find<TBoldGrid>(form, "BoldGrid");
        Assert.That(grid.BoldAutoColumns, Is.True);
        Assert.That(grid.RowCount, Is.EqualTo(2));
        Assert.That(((TBoldListHandle)grid.BoldHandle!).Name, Is.EqualTo("BoldListHandle"));
        Assert.That(Find<TBoldNavigator>(form, "BoldNavigator").BoldHandle, Is.SameAs(grid.BoldHandle));
        Assert.That(form.Text, Is.EqualTo(persons.BoldType!.ModelName));

        TBoldElement names = s.System.EvaluateExpressionAsNewElement("Person.allInstances.firstName")!;
        Form namesForm = ShowOffScreen(FormFor(names));
        Assert.That(Find<TBoldGrid>(namesForm, "BoldGrid").RowCount, Is.EqualTo(2));

        Form systemForm = ShowOffScreen(FormFor(s.System));
        Assert.That(systemForm.Text, Is.EqualTo(s.System.BoldType!.ModelName));
        Assert.That(Find<TBoldGrid>(systemForm, "BoldGrid"), Is.Not.Null);
    }

    [Test]
    [Covers(typeof(TAFPPageControl), "*")]
    public void Dragging_over_the_tabs_selects_the_tab_under_the_mouse()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        Form form = ShowOffScreen(FormFor(ann));
        var pages = (TAFPPageControl)Find<TabControl>(form, "PageControl");
        pages.ActivatePage(1, 0);
        Assert.That(pages.SelectedIndex, Is.EqualTo(0));
        int third = TextRenderer.MeasureText(pages.TabPages[0].Text, pages.Font).Width + 14 +
            TextRenderer.MeasureText(pages.TabPages[1].Text, pages.Font).Width + 14 + 3;
        pages.ActivatePage(third, 0);
        Assert.That(pages.SelectedIndex, Is.EqualTo(2));
        pages.ActivatePage(100000, 0);
        Assert.That(pages.SelectedIndex, Is.EqualTo(2), "beyond the tabs: the last");

        BoldGUI.BoldGUIHandler().DraggedObjects.Add(ann);
        try
        {
            Point strip = pages.PointToScreen(new Point(2, 2));
            var args = new DragEventArgs(null, 0, strip.X, strip.Y, DragDropEffects.Copy, DragDropEffects.Copy);
            OnDragOverMethod.Invoke(pages, [args]);
            Assert.That(args.Effect, Is.EqualTo(DragDropEffects.None));
            Assert.That(pages.SelectedIndex, Is.EqualTo(0));

            pages.SelectedIndex = 2;
            Point inside = pages.PointToScreen(new Point(2, pages.DisplayRectangle.Top + 10));
            OnDragOverMethod.Invoke(pages, [new DragEventArgs(null, 0, inside.X, inside.Y, DragDropEffects.Copy, DragDropEffects.Copy)]);
            Assert.That(pages.SelectedIndex, Is.EqualTo(2), "over a page, not a tab");
        }
        finally
        {
            BoldGUI.BoldGUIHandler().DraggedObjects.Clear();
        }
    }

    [Test]
    [Covers(typeof(TMethodButton), "*")]
    public void A_method_button_keeps_its_method_and_does_nothing_when_clicked()
    {
        using var s = new GuiSample();
        TBoldReferenceHandle handle = s.Reference(s.NewPerson("Ann"));
        using var result = new TextBox();
        var button = new TMethodButton(handle, 3, result);
        using var form = new TestForm(button);
        bool clicked = false;
        button.Click += (_, _) => clicked = true;
        button.PerformClick();
        Assert.That(clicked, Is.False);
        Assert.That(button.MethodNumber, Is.EqualTo(3));
        Assert.That(button.ObjectHandle, Is.SameAs(handle));
        Assert.That(button.ResultEditBox, Is.SameAs(result));
        Assert.That(button.ValueHolder, Is.Empty);
        button.Dispose();
        Assert.That(button.ValueHolder, Is.Null);
    }
}
