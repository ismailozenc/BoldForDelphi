// Delphi: Source/BoldAwareGUI/BoldControls/BoldDataSet.pas
// Port map: CSharp/port/units/BoldDataSet.md
using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>A data set over the persons of the Building model.</summary>
[NonParallelizable]
public class BoldDataSetTests
{
    private static TBoldDataSet People(TBoldListHandle handle)
    {
        var ds = new TBoldDataSet { Name = "People", BoldHandle = handle };
        ds.CreateDefaultFields();
        GuiSample.Display();
        return ds;
    }

    [Test]
    [Covers(typeof(TBoldDataSetFieldDescription), "*")]
    [Covers(typeof(TBoldDataSetFieldDescriptions), "*")]
    [Covers("BoldDataSet", "TBoldConvInfo")]
    public void Field_descriptions_take_their_type_and_size_from_the_expression()
    {
        using var s = new GuiSample();
        using var ds = People(s.List("Person.allInstances"));
        Assert.That(ds.FieldDescriptions.Count, Is.EqualTo(4), "a field per attribute of Person");
        TBoldDataSetFieldDescription first = ds.FieldDescriptions[0];
        Assert.That(first.Expression, Is.EqualTo("firstName"));
        Assert.That(first.EffectiveFieldName, Is.EqualTo("firstName"));
        Assert.That(first.FieldType, Is.EqualTo(TFieldType.ftString));
        Assert.That(first.FieldSize, Is.EqualTo(255));
        Assert.That(first.UseFieldSize, Is.True);
        Assert.That(first.Required, Is.False);
        Assert.That(first.RTInfo!.ExpressionName, Is.EqualTo("firstName"));
        Assert.That(first.FieldOfs, Is.EqualTo(0));
        Assert.That(first.Dataset, Is.SameAs(ds));
        Assert.That(first.FieldDescriptions, Is.SameAs(ds.FieldDescriptions));
        Assert.That(first.ListelementType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(first.DisplayName, Is.EqualTo("firstName = firstName"));
        Assert.That(ds.FieldDescriptions.Items(1).FieldSize, Is.EqualTo(25), "the attribute's length (Bold.Length)");
        Assert.That(ds.FieldDescriptions[1].BufferSize, Is.EqualTo(26));
        Assert.That(ds.FieldDescriptions[2].FieldType, Is.EqualTo(TFieldType.ftCurrency));
        Assert.That(ds.FieldDescriptions[3].FieldType, Is.EqualTo(TFieldType.ftBoolean));

        TBoldDataSetFieldDescription home = ds.FieldDescriptions.Add();
        home.Expression = "home.address";
        Assert.That(home.EffectiveFieldName, Is.EqualTo("home_address"));
        home.FieldName = "HomeAddress";
        Assert.That(home.EffectiveFieldName, Is.EqualTo("HomeAddress"));
        TBoldDataSetFieldDescription reference = ds.FieldDescriptions.Add();
        reference.BoldProperties = home.BoldProperties;
        Assert.That(reference.Expression, Is.EqualTo("home.address"));
        reference.Expression = "home";
        Assert.That(reference.FieldType, Is.EqualTo(TFieldType.ftInteger), "an object is a reference");
        TBoldDataSetFieldDescription copy = ds.FieldDescriptions.Add();
        copy.Assign(home);
        Assert.That(copy.FieldName, Is.EqualTo("HomeAddress"));
        Assert.That(copy.Expression, Is.EqualTo("home.address"));
        TBoldDataSetFieldDescription list = ds.FieldDescriptions.Add();
        list.Expression = "ownedBuildings";
        Assert.That(list.FieldType, Is.EqualTo(TFieldType.ftString));
        Assert.That(list.FieldSize, Is.EqualTo(40), "neither an attribute nor an object");
        TBoldDataSetFieldDescription wrong = ds.FieldDescriptions.Add();
        wrong.Expression = "nonsense";
        Assert.Throws<EBold>(() => _ = wrong.FieldType);

        int followers = ds.FieldDescriptions.FieldFollowers.Count;
        wrong.Dispose();
        Assert.That(ds.FieldDescriptions.Count, Is.EqualTo(8));
        Assert.That(ds.FieldDescriptions.FieldFollowers.Count, Is.EqualTo(followers - 1), "the field's follower goes with it");
        ds.FieldDescriptions.InvalidateFields();

        using var noHandle = new TBoldDataSet();
        TBoldDataSetFieldDescription orphan = noHandle.FieldDescriptions.Add();
        orphan.Expression = "x";
        Assert.Throws<EBold>(() => _ = orphan.FieldType, "no context");
        Assert.Throws<EBoldInternal>(() => _ = new TBoldDataSetFieldDescription(new TCollection(typeof(TBoldDataSetFieldDescription))));
    }

    [Test]
    [Covers(typeof(TBoldAbstractDataSet), "*")]
    [Covers(typeof(TBoldDataSet), "*")]
    [Covers(typeof(BoldDataSet), "*")]
    public void The_data_set_has_a_record_per_element_and_writes_fields_to_the_elements()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TPerson bob = s.NewPerson("Bob", "Jones");
        ann.Assets = 10.5m;
        ann.IsMarried = true;
        TBoldListHandle handle = s.List("Person.allInstances");
        using var ds = People(handle);
        int scrolls = 0, posts = 0;
        ds.AfterScroll = _ => scrolls++;
        ds.BeforePost = _ => posts++;
        ds.Open();
        GuiSample.Display();
        Assert.That(ds.Active, Is.True);
        Assert.That(ds.CanModify, Is.True);
        Assert.That(ds.RecordCount, Is.EqualTo(2));
        Assert.That(ds.FieldByName("firstName").AsString, Is.EqualTo("Ann"));
        Assert.That(ds.FieldByName("assets").AsCurrency, Is.EqualTo(10.5m));
        Assert.That(ds.FieldByName("isMarried").AsBoolean, Is.True);
        Assert.That(ds.ActiveBoldElement, Is.SameAs(ann));
        Assert.That(ds.GetFieldData(ds.FieldByName("firstName")), Is.True);
        Assert.That(ds.BoldRTInfo("lastName")!.ExpressionName, Is.EqualTo("lastName"));
        Assert.Throws<Exception>(() => ds.BoldRTInfo("nothing"));
        Assert.That(ds.BookmarkValid(ann), Is.True);
        Assert.That(ds.BookmarkValid("no element"), Is.False);
        using (Stream blob = ds.CreateBlobStream(ds.FieldByName("firstName"), TBoldBlobStreamMode.bmRead))
        {
            Assert.That(blob.Length, Is.EqualTo(0), "no blob");
        }

        ds.Next();
        Assert.That(scrolls, Is.GreaterThan(0));
        Assert.That(ds.ActiveBoldObject, Is.SameAs(bob));
        Assert.That(handle.CurrentElement, Is.SameAs(bob), "the handle follows the current record");

        ds.Edit();
        ds.FieldByName("firstName").AsString = "Robert";
        Assert.That(bob.FirstName, Is.EqualTo("Robert"), "a field is written to its element as it is set");
        ds.FieldByName("assets").AsCurrency = 3m;
        ds.FieldByName("isMarried").AsBoolean = true;
        ds.Post();
        Assert.That(posts, Is.EqualTo(1));
        Assert.That(bob.Assets, Is.EqualTo(3m));
        Assert.That(bob.IsMarried, Is.True);

        ds.Append();
        Assert.That(ds.ActiveBoldElement, Is.InstanceOf<TPerson>(), "a new record is a new element");
        ds.FieldByName("firstName").AsString = "Cid";
        ds.Post();
        Assert.That(ds.RecordCount, Is.EqualTo(3));
        Assert.That(((TPerson)ds.ActiveBoldElement!).FirstName, Is.EqualTo("Cid"));

        ds.Append();
        var discarded = (TPerson)ds.ActiveBoldElement!;
        ds.Cancel();
        Assert.That(discarded.BoldObjectIsDeleted, Is.True, "a cancelled new element is discarded");
        GuiSample.Display();
        Assert.That(ds.RecordCount, Is.EqualTo(3));

        Assert.That(ds.Locate("firstName", "ann", DelphiSet<TLocateOption>.Of(TLocateOption.loCaseInsensitive)), Is.True);
        Assert.That(ds.ActiveBoldElement, Is.SameAs(ann));
        Assert.That(ds.Locate("firstName", "Ro", DelphiSet<TLocateOption>.Of(TLocateOption.loPartialKey)), Is.True);
        Assert.That(ds.ActiveBoldElement, Is.SameAs(bob));
        Assert.That(ds.Locate("firstName;lastName", new object?[] { "Ann", "Smith" }, DelphiSet<TLocateOption>.Of()), Is.True);
        Assert.That(ds.Locate("SELF", bob.BoldObjectLocator, DelphiSet<TLocateOption>.Of()), Is.True);
        Assert.That(ds.ActiveBoldElement, Is.SameAs(bob));
        Assert.That(ds.Locate("firstName", "Zed", DelphiSet<TLocateOption>.Of()), Is.False);
        Assert.That(ds.Lookup("firstName", "Ann", "lastName"), Is.EqualTo("Smith"));
        Assert.That(ds.Lookup("firstName", "Zed", "lastName"), Is.Null);

        Assert.That(ds.Locate("firstName", "Cid", DelphiSet<TLocateOption>.Of()), Is.True);
        TPerson cid = (TPerson)ds.ActiveBoldElement!;
        ds.Delete();
        Assert.That(cid.BoldObjectIsDeleted, Is.True);
        Assert.That(ds.RecordCount, Is.EqualTo(2));

        ann.FirstName = "Anna";
        GuiSample.Display();
        ds.Locate("SELF", ann, DelphiSet<TLocateOption>.Of());
        Assert.That(ds.FieldByName("firstName").AsString, Is.EqualTo("Anna"), "a changed value shows");
        s.NewPerson("Dee");
        GuiSample.Display();
        Assert.That(ds.RecordCount, Is.EqualTo(3), "a new element shows");

        ds.Active = false;
        Assert.That(ds.Active, Is.False);
        Assert.That(ds.ActiveBoldElement, Is.Null);
        ds.Active = true;
        Assert.That(ds.RecordCount, Is.EqualTo(3));
    }

    [Test]
    [Covers(typeof(TBoldAbstractDataSet), nameof(TBoldAbstractDataSet.TryToOpen), "AutoOpen", "Loaded", "LayoutChanged", "TypeMayHaveChanged", "TypeHasChanged")]
    public void A_data_set_that_opens_by_itself_opens_after_loading_and_on_a_new_type()
    {
        using var s = new GuiSample();
        s.NewPerson("Ann");
        TBoldListHandle persons = s.List("Person.allInstances");
        var ds = new TBoldDataSet();
        ISupportInitialize init = ds;
        init.BeginInit();
        ds.BoldHandle = persons;
        ds.AutoOpen = true;
        Assert.That(ds.Active, Is.False, "not while loading");
        init.EndInit();
        Assert.That(ds.FieldDescriptions.Count, Is.EqualTo(4), "default fields after loading");
        Assert.That(ds.Active, Is.True);
        Assert.That(ds.RecordCount, Is.EqualTo(1));
        Assert.That(ds.IsLayoutChanged, Is.False);

        persons.Expression = "Residential_Building.allInstances";
        s.NewHouse("Elm Street");
        GuiSample.Display();
        ds.CreateDefaultFields();
        Assert.That(ds.FieldDescriptions[0].Expression, Is.EqualTo("zipCode"), "the fields of the new type");
        Assert.That(ds.Active, Is.True, "it opened again");
        Assert.That(ds.FieldByName("address").AsString, Is.EqualTo("Elm Street"));
        ds.Dispose();
    }
}
