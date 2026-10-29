// Delphi: Source/BoldAwareGUI/FormGen/BoldAFPPluggable.pas
// Port map: CSharp/port/units/BoldAFPPluggable.md
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BuildingClasses;
using static Bold.Tests.BoldAFPDefaultTests;

namespace Bold.Tests;

/// <summary>A form of a person as the designer makes one: a reference handle in the components and an edit on it.</summary>
internal sealed class PersonForm : Form
{
    private readonly Container components = new();
    public readonly TBoldReferenceHandle PersonHandle;
    public readonly TBoldEdit NameEdit;

    public PersonForm()
    {
        PersonHandle = new TBoldReferenceHandle(components) { Name = "PersonHandle" };
        NameEdit = new TBoldEdit { Name = "NameEdit", Parent = this };
        NameEdit.BoldHandle = PersonHandle;
        NameEdit.BoldProperties.Expression = "firstName";
        Size = new Size(300, 200);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>A form with two reference handles: none is the form's handle.</summary>
internal sealed class TwoHandlesForm : Form
{
    private readonly Container components = new();

    public TwoHandlesForm()
    {
        _ = new TBoldReferenceHandle(components) { Name = "First" };
        _ = new TBoldReferenceHandle(components) { Name = "Second" };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>Forms of their own for objects: installable forms by class and placeable providers.</summary>
[NonParallelizable]
public class BoldAFPPluggableTests
{
    /// <summary>Removes every registration of the provider class.</summary>
    internal static void RemoveProviders(Type providerClass)
    {
        TBoldAutoFormProviderRegistry registry = BoldAFP.AutoFormProviderRegistry();
        for (int i = registry.Count - 1; i >= 0; i--)
        {
            if (registry.Holders(i).ProviderClass == providerClass)
            {
                registry.UnregisterProvider(providerClass);
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        // The unit initializes on first use; each test starts with its provider registered once.
        BoldAFPPluggable.InstallableFormsRegistry();
        RemoveProviders(typeof(TBoldInstallableAFP));
        BoldAFPPluggable.Initialization();
    }

    [TearDown]
    public void TearDown()
    {
        CloseAutoForms();
        RemoveProviders(typeof(TBoldInstallableAFP));
        RemoveProviders(typeof(TBoldRegisterAFP));
        BoldAFPPluggable.AllowDefaultForm = true;
        if (BoldAFPPluggable.InstallableFormsRegistry().DescriptorsByBOClassName("Person") is not null)
        {
            BoldAFPPluggable.InstallableFormsRegistry().UnRegisterInstallableFormsDescriptor("Person");
        }
    }

    [Test]
    [Covers(typeof(TBoldInstallableAFP), "*")]
    [Covers(typeof(TBoldPluggableAFP), "*")]
    [Covers(typeof(TBoldInstallableFormsRegistry), "*")]
    [Covers(typeof(TBoldInstallableFormsDescriptor), "*")]
    [Covers(typeof(TBoldHandleLocatorStyle))]
    [Covers(typeof(BoldAFPPluggable), "*")]
    public void An_installable_form_of_the_class_shows_the_object_in_its_handle()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        TBoldInstallableFormsRegistry registry = BoldAFPPluggable.InstallableFormsRegistry();
        registry.RegisterInstallableFormsDescriptor("Person", typeof(PersonForm), TBoldHandleLocatorStyle.hfByName, "PersonHandle");
        TBoldInstallableFormsDescriptor descriptor = registry.DescriptorsByBOClassName("person")!;
        Assert.That(descriptor.FormClass, Is.EqualTo(typeof(PersonForm)));
        Assert.That(descriptor.BOClassName, Is.EqualTo("Person"));
        Assert.That(descriptor.HandleName, Is.EqualTo("PersonHandle"));
        Assert.That(descriptor.HandleLocatorStyle, Is.EqualTo(TBoldHandleLocatorStyle.hfByName));

        var form = (PersonForm)ShowOffScreen(FormFor(ann));
        Assert.That(form.PersonHandle.Value, Is.SameAs(ann), "the handle found by name");
        Assert.That(form.NameEdit.Text, Is.EqualTo("Ann"));
        Assert.That(form.Size, Is.EqualTo(new Size(300, 200)), "the form keeps its size");
        Assert.That(FormFor(ann), Is.SameAs(form), "an open form is reused");
        form.Close();

        registry.UnRegisterInstallableFormsDescriptor("Person");
        Assert.That(registry.DescriptorsByBOClassName("Person"), Is.Null);
        registry.RegisterInstallableFormsDescriptor("Person", typeof(PersonForm), TBoldHandleLocatorStyle.hfByType, "");
        form = (PersonForm)ShowOffScreen(FormFor(ann));
        Assert.That(form.PersonHandle.Value, Is.SameAs(ann), "the form's only reference handle");
        form.Close();

        registry.UnRegisterInstallableFormsDescriptor("Person");
        registry.RegisterInstallableFormsDescriptor("Person", typeof(TwoHandlesForm), TBoldHandleLocatorStyle.hfByType, "");
        Assert.Throws<EBoldInternal>(() => FormFor(ann), "two reference handles: no handle");
        Assert.That(BoldAFPDefault.G_BoldActiveAutoForms.IndexOfElement(ann), Is.EqualTo(-1), "the form that went left the list");

        TResidential_Building elm = s.NewHouse("Elm Street");
        Form houseForm = ShowOffScreen(FormFor(elm));
        Assert.That(houseForm.Controls.Find("PageControl", true), Is.Not.Empty, "no form of its own: the default object form");
        houseForm.Close();

        BoldAFPPluggable.AllowDefaultForm = false;
        Assert.That(FormFor(elm), Is.Null, "no default forms");
    }

    [Test]
    [Covers(typeof(TBoldPlaceableAFP), "*")]
    [Covers(typeof(TBoldRegisterAFP), "*")]
    [Covers(typeof(TBoldGetFormClassEvent))]
    [Covers(typeof(TBoldRetrieveHandleEvent))]
    [Covers(typeof(TBoldMemberShouldBeDisplayedEvent))]
    public void A_placeable_provider_gives_the_form_class_the_handle_and_the_members_shown()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        TResidential_Building elm = s.NewHouse("Elm Street");
        var placeable = new TBoldPlaceableAFP
        {
            OnGetFormClass = (TBoldElement element, ref Type? result) =>
            {
                if (element is TPerson)
                {
                    result = typeof(PersonForm);
                }
            },
            OnRetrieveHandle = (Form form, ref TBoldReferenceHandle? result) => result = (form as PersonForm)?.PersonHandle,
            OnMemberShouldBeDisplayed = (TBoldMemberRTInfo member, ref bool show) => show = member.ExpressionName != "zipCode",
        };
        try
        {
            Assert.That(placeable.OnGetFormClass, Is.Not.Null);
            Assert.That(placeable.OnRetrieveHandle, Is.Not.Null);
            Assert.That(placeable.OnMemberShouldBeDisplayed, Is.Not.Null);

            var form = (PersonForm)ShowOffScreen(FormFor(ann));
            Assert.That(form.PersonHandle.Value, Is.SameAs(ann));

            Form houseForm = ShowOffScreen(FormFor(elm));
            Assert.That(houseForm.Controls.Find("Residential_BuildingaddressEdit", true), Is.Not.Empty);
            Assert.That(houseForm.Controls.Find("Residential_BuildingzipCodeEdit", true), Is.Empty, "the placeable provider hides the member");
            houseForm.Close();
        }
        finally
        {
            placeable.Dispose();
        }

        Form defaultForm = ShowOffScreen(FormFor(elm));
        Assert.That(defaultForm.Controls.Find("Residential_BuildingzipCodeEdit", true), Is.Not.Empty, "the provider went with the component");
    }
}
