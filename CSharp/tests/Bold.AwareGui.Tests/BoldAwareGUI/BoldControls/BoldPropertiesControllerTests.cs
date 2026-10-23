// Delphi: Source/BoldAwareGUI/BoldControls/BoldPropertiesController.pas, Source/BoldAwareGUI/BoldControls/BoldStringsPropertyController.pas
// Port map: CSharp/port/units/BoldPropertiesController.md
using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>Properties of other components driven by Bold expressions.</summary>
[NonParallelizable]
public class BoldPropertiesControllerTests
{
    /// <summary>A control-like component with properties of the kinds the controller sets.</summary>
    private sealed class Gadget : Component
    {
        public string Text { get; set; } = "";
        public int Count { get; set; } = -1;
        public bool Checked { get; set; }
        public TAlignment Alignment { get; set; }
        public double Weight { get; set; }
        public TStringList Lines { get; } = new();
        public List<object> Items { get; } = [];
        public TFont Font { get; } = new();
        public Gadget? Buddy { get; set; }
        public Gadget? Nobody { get; set; }
        public TCollection Columns { get; } = new(typeof(Column));

        public event EventHandler? Leave;

        public void DoLeave() => Leave?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Column(TCollection? collection) : TCollectionItem(collection)
    {
        public string Title { get; set; } = "";
    }

    private sealed class Fixture : IDisposable
    {
        public readonly GuiSample Sample = new();
        public readonly TPerson Ada;
        public readonly TBoldReferenceHandle Handle;
        public readonly Container Form = new();
        public readonly Gadget Gadget = new();
        public readonly TBoldPropertiesController Controller;

        public Fixture(string expression)
        {
            Ada = Sample.NewPerson("Ada", "Lovelace");
            Handle = Sample.Reference(Ada);
            Form.Add(Gadget, "Gadget");
            Controller = new TBoldPropertiesController(Form) { BoldHandle = Handle };
            Controller.BoldProperties.Expression = expression;
        }

        public TBoldDrivenProperty Drive(string property, IComponent? component = null)
        {
            TBoldDrivenProperty driven = Controller.DrivenProperties.Add();
            driven.VCLComponent = component ?? Gadget;
            driven.PropertyName = property;
            return driven;
        }

        public void Dispose()
        {
            Form.Dispose();
            Sample.Dispose();
        }
    }

    [Test]
    [Covers(typeof(TBoldPropertiesController), "*")]
    [Covers(typeof(TBoldDrivenProperty), "*")]
    [Covers(typeof(TBoldDrivenPropertyCollection), "*")]
    public void Driven_properties_follow_the_expression()
    {
        using var f = new Fixture("firstName");
        f.Drive("Text");
        GuiSample.Display();
        Assert.That(f.Gadget.Text, Is.EqualTo("Ada"));
        f.Ada.FirstName = "Augusta";
        GuiSample.Display();
        Assert.That(f.Gadget.Text, Is.EqualTo("Augusta"));
        Assert.That(f.Controller.DrivenProperties[0].DisplayName, Is.EqualTo("Gadget.Text (RO)"));
        Assert.That(f.Controller.DrivenProperties[0].PropertiesController, Is.SameAs(f.Controller));

        f.Handle.Value = null;
        GuiSample.Display();
        Assert.That(f.Gadget.Text, Is.EqualTo(""), "no element sets the nil representation");
    }

    [Test]
    [Covers(typeof(TBoldDrivenProperty), "*")]
    public void Values_are_converted_to_the_property_type()
    {
        using var f = new Fixture("firstName");
        f.Drive("Count");
        f.Drive("Checked");
        f.Drive("Alignment");
        f.Drive("Weight");
        f.Drive("Font.Size");
        f.Drive("Columns[1].Title");
        f.Drive("Lines");
        f.Drive("Items");
        f.Drive("Buddy");
        f.Drive("Nobody.Text");
        f.Drive("Misspelt");
        f.Gadget.Columns.Add();
        f.Gadget.Columns.Add();
        var buddy = new Gadget();
        f.Form.Add(buddy, "Buddy");

        f.Controller.BoldProperties.Expression = "'12'";
        f.Controller.DrivenProperties[1].PropertyName = "";
        f.Controller.DrivenProperties[2].PropertyName = "";
        f.Controller.DrivenProperties[3].PropertyName = "";
        f.Controller.DrivenProperties[8].PropertyName = "";
        GuiSample.Display();
        Assert.That(f.Gadget.Count, Is.EqualTo(12));
        Assert.That(f.Gadget.Font.Size, Is.EqualTo(12), "a path goes through object properties");
        Assert.That(((Column)f.Gadget.Columns[1]).Title, Is.EqualTo("12"), "and through collection items");
        Assert.That(f.Gadget.Lines.Text.Trim(), Is.EqualTo("12"));
        Assert.That(f.Gadget.Items, Is.EqualTo(new[] { "12" }), "a .NET list is filled like a string list");

        f.Controller.DrivenProperties[0].PropertyName = "";
        f.Controller.DrivenProperties[4].PropertyName = "";
        f.Controller.DrivenProperties[1].PropertyName = "Checked";
        f.Controller.DrivenProperties[2].PropertyName = "Alignment";
        f.Controller.DrivenProperties[3].PropertyName = "Weight";
        f.Controller.DrivenProperties[8].PropertyName = "Buddy";
        f.Controller.BoldProperties.Expression = "'Buddy'";
        f.Controller.DrivenProperties[1].PropertyName = "";
        f.Controller.DrivenProperties[2].PropertyName = "";
        f.Controller.DrivenProperties[3].PropertyName = "";
        GuiSample.Display();
        Assert.That(f.Gadget.Buddy, Is.SameAs(buddy), "an object property gets the owner's component with the value as name");

        f.Controller.DrivenProperties[8].PropertyName = "";
        f.Controller.DrivenProperties[1].PropertyName = "Checked";
        f.Controller.BoldProperties.Expression = "true";
        GuiSample.Display();
        Assert.That(f.Gadget.Checked, Is.True);

        f.Controller.DrivenProperties[1].PropertyName = "";
        f.Controller.DrivenProperties[2].PropertyName = "Alignment";
        f.Controller.BoldProperties.Expression = "'taCenter'";
        GuiSample.Display();
        Assert.That(f.Gadget.Alignment, Is.EqualTo(TAlignment.taCenter), "an enumeration is set by name");

        f.Controller.DrivenProperties[2].PropertyName = "";
        f.Controller.DrivenProperties[3].PropertyName = "Weight";
        f.Controller.BoldProperties.Expression = "2.5";
        GuiSample.Display();
        Assert.That(f.Gadget.Weight, Is.EqualTo(2.5));

        f.Controller.DrivenProperties[3].PropertyName = "Count";
        f.Controller.BoldProperties.Expression = "'many'";
        Assert.That(() => GuiSample.Display(), Throws.InstanceOf<Exception>().With.Message.Contains("Could not set the integer Count property to value many"));
        f.Controller.DrivenProperties[3].PropertyName = "Weight";
        f.Controller.BoldProperties.Expression = "'lots'"; // a changed controller displays again
        Assert.That(() => GuiSample.Display(), Throws.InstanceOf<Exception>().With.Message.Contains("Could not set the Weight property to value lots"));
    }

    [Test]
    [Covers(typeof(TBoldPropertiesController), "*")]
    [Covers(typeof(TBoldDrivenProperty), "*")]
    public void Without_an_element_numbers_are_zero_and_lists_show_their_elements()
    {
        using var f = new Fixture("firstName");
        f.Drive("Count");
        f.Handle.Value = null;
        GuiSample.Display();
        Assert.That(f.Gadget.Count, Is.EqualTo(0));

        TResidential_Building house = f.Sample.NewHouse("Main Street");
        house.Residents.Add(f.Ada);
        house.Residents.Add(f.Sample.NewPerson("Bob"));
        f.Handle.Value = house;
        f.Controller.BoldProperties.Expression = "residents";
        f.Controller.DrivenProperties[0].PropertyName = "Lines";
        GuiSample.Display();
        Assert.That(f.Gadget.Lines.Count, Is.EqualTo(2), "a list gives a line per element");
    }

    [Test]
    [Covers(typeof(TBoldDrivenProperty), "*")]
    [Covers(typeof(TBoldPropertiesController), "*")]
    public void Leaving_the_component_writes_its_value_back()
    {
        using var f = new Fixture("firstName");
        TBoldDrivenProperty text = f.Drive("Text");
        GuiSample.Display();
        f.Gadget.Text = "Anna";
        f.Gadget.DoLeave();
        Assert.That(f.Ada.FirstName, Is.EqualTo("Ada"), "a read-only property is not written back");

        text.ReadOnly = false;
        Assert.That(text.DisplayName, Does.EndWith("(RW)"));
        f.Gadget.DoLeave();
        Assert.That(f.Ada.FirstName, Is.EqualTo("Anna"), "leaving applies with the exit policy");

        var copy = f.Controller.DrivenProperties.Add();
        copy.Assign(text);
        Assert.That((copy.VCLComponent, copy.PropertyName, copy.ReadOnly), Is.EqualTo(((IComponent?)f.Gadget, "Text", false)));
        f.Controller.DrivenProperties.Delete(1);

        f.Gadget.Dispose();
        Assert.That(text.VCLComponent, Is.Null, "a disposed component is forgotten");
        Assert.That(text.PropertyName, Is.EqualTo(""));
        Assert.That(text.DisplayName, Is.EqualTo("<No Comp>.<No Prop> (RW)"));

        var other = new TBoldPropertiesController();
        other.BoldProperties = f.Controller.BoldProperties;
        other.DrivenProperties = f.Controller.DrivenProperties;
        Assert.That(other.BoldProperties.Expression, Is.EqualTo("firstName"));
        Assert.That(other.DrivenProperties.Count, Is.EqualTo(1));
        Assert.That(other.BoldHandle, Is.Null);
        other.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldStringsPropertyController), "*")]
    [Covers(typeof(TBoldControlFollowerEvent))]
    [Covers(typeof(TBoldControlSubFollowerEvent))]
    public void A_strings_property_gets_a_line_per_element()
    {
        using var s = new GuiSample();
        s.NewPerson("Bob");
        s.NewPerson("Ada");
        TBoldListHandle people = s.List("Person.allInstances->orderBy(firstName)");
        using var form = new Container();
        var gadget = new Gadget();
        form.Add(gadget, "Gadget");
        var events = new List<string>();
        var controller = new TBoldStringsPropertyController(form)
        {
            Name = "Names",
            BoldHandle = people,
            VCLComponent = gadget,
            PropertyName = "Lines",
            OnListBeforeMakeUptoDate = (_, _) => events.Add("before"),
            OnListAfterMakeUptoDate = (_, _) => events.Add("after"),
            OnListBeforeInsertItem = (_, i, _) => events.Add("+" + i),
            OnListAfterInsertItem = (_, r) => events.Add("+" + r.index + "."),
            OnListBeforeDeleteItem = (_, r) => events.Add("-" + r.index),
            OnListAfterDeleteItem = (_, i, _) => events.Add("-" + i + "."),
            OnItemBeforeMakeUpToDate = (_, _) => events.Add("item"),
            OnItemAfterMakeUpToDate = (_, _) => events.Add("item."),
        };
        controller.BoldRowProperties.Expression = "firstName";
        Assert.That(controller.HandleFollower.BoldHandle, Is.SameAs(people));
        GuiSample.Display();
        Assert.That(gadget.Lines.Text.Split(SysUtils.sLineBreak, StringSplitOptions.RemoveEmptyEntries), Is.EqualTo(new[] { "Ada", "Bob" }));
        Assert.That(gadget.Lines.Objects(0), Is.InstanceOf<TBoldFollower>(), "each line holds its row follower");
        Assert.That(events, Does.Contain("before").And.Contain("after").And.Contain("+1.").And.Contain("item."));

        s.NewPerson("Abe");
        GuiSample.Display();
        Assert.That(gadget.Lines.Strings(0), Is.EqualTo("Abe"));
        ((TPerson)people.List![2]!).Delete();
        GuiSample.Display();
        Assert.That(gadget.Lines.Count, Is.EqualTo(2));
        Assert.That(events, Does.Contain("-2").And.Contain("-2."));

        var items = new TBoldStringsPropertyController(form) { Name = "Items", BoldHandle = people, VCLComponent = gadget, PropertyName = "Items" };
        items.BoldRowProperties.Expression = "firstName";
        items.BoldProperties.NilElementMode = TBoldNilElementMode.neAddLast;
        GuiSample.Display();
        Assert.That(gadget.Items, Is.EqualTo(new[] { "Abe", "Ada", "" }), "a .NET list works as the strings");

        gadget.Items.Add("intruder");
        s.NewPerson("Cy");
        Assert.That(() => GuiSample.Display(), Throws.InstanceOf<EBold>().With.Message.Contains("\"Items\" changed by other means"));

        var copy = new TBoldStringsPropertyController();
        copy.BoldProperties = controller.BoldProperties;
        copy.BoldRowProperties = controller.BoldRowProperties;
        copy.BoldProperties = items.BoldProperties;
        Assert.That(copy.BoldProperties.NilElementMode, Is.EqualTo(TBoldNilElementMode.neAddLast));
        Assert.That(copy.BoldRowProperties.Expression, Is.EqualTo("firstName"));
        copy.Dispose();

        gadget.Dispose();
        Assert.That(controller.VCLComponent, Is.Null, "a disposed component is forgotten");
        Assert.That(controller.PropertyName, Is.EqualTo(""));
        controller.BoldHandle = null;
        Assert.That(controller.BoldHandle, Is.Null);
    }
}
