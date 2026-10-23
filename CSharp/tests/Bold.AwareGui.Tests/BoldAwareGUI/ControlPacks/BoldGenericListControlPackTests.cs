// Delphi: Source/BoldAwareGUI/ControlPacks/BoldGenericListControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldNodeControlPack.pas,
// Source/BoldAwareGUI/ControlPacks/BoldControllerListControlPack.pas
// Port map: CSharp/port/units/BoldGenericListControlPack.md
using BuildingClasses;

namespace Bold.Tests;

/// <summary>Lists whose rows come from parts, trees of node descriptions, and rows of several controllers.</summary>
[NonParallelizable]
public class BoldGenericListControlPackTests
{
    private static TBoldFollower Follow(TBoldElement? element, TBoldFollowerController controller)
    {
        var follower = new TBoldFollower(null, controller) { Element = element };
        GuiSample.Display();
        return follower;
    }

    private static string Text(TBoldFollower row) => ((TBoldStringFollowerController)row.Controller!).GetCurrentAsString(row);

    private static string[] Texts(TBoldFollower list) => [.. Enumerable.Range(0, list.SubFollowerCount).Select(i => Text(list.SubFollowers(i)!))];

    [Test]
    [Covers(typeof(TBoldControllerList), "*")]
    [Covers(typeof(TBoldControllerListAsFollowerListRenderer), "*")]
    public void A_controller_list_shows_the_element_through_each_controller()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada", "Lovelace");
        var columns = new TBoldControllerList(null);
        Assert.That((columns.DragMode, columns.DropMode), Is.EqualTo((TBoldDragMode.bdgSelection, TBoldDropMode.bdpInsert)));
        var first = new TBoldStringFollowerController(null) { Expression = "firstName" };
        var last = new TBoldStringFollowerController(null) { Expression = "lastName" };
        columns.Add(first);
        columns.Add(last);
        Assert.That(columns.Count, Is.EqualTo(2));
        Assert.That(columns[1], Is.SameAs(last));

        using TBoldFollower row = Follow(ada, columns);
        Assert.That(Texts(row), Is.EqualTo(new[] { "Ada", "Lovelace" }));
        ada.LastName = "Byron";
        GuiSample.Display();
        Assert.That(Texts(row), Is.EqualTo(new[] { "Ada", "Byron" }));

        columns.Move(1, 0);
        GuiSample.Display();
        Assert.That(Texts(row), Is.EqualTo(new[] { "Byron", "Ada" }));
        columns.Remove(first);
        GuiSample.Display();
        Assert.That(Texts(row), Is.EqualTo(new[] { "Byron" }));
        columns.Add(new TBoldStringFollowerController(null) { Expression = "firstName" });
        columns.Delete(0);
        GuiSample.Display();
        Assert.That(Texts(row), Is.EqualTo(new[] { "Ada" }));
        Assert.That(TBoldControllerListAsFollowerListRenderer.DefaultRenderer(), Is.Not.Null);
    }

    /// <summary>A generic list on a house: its residents (a row each) and its address.</summary>
    private sealed class GenericFixture : IDisposable
    {
        public readonly GuiSample Sample = new();
        public readonly TResidential_Building House;
        public readonly TBoldStringFollowerController PersonRow = new(null) { Expression = "firstName" };
        public readonly TBoldStringFollowerController DefaultRow = new(null) { Expression = "" };
        public readonly List<string> Asked = [];
        public readonly TBoldGenericListController Controller;

        public GenericFixture()
        {
            House = Sample.NewHouse("Main Street");
            House.Residents.Add(Sample.NewPerson("Ada"));
            House.Residents.Add(Sample.NewPerson("Bob"));
            Controller = new TBoldGenericListController(null, ByName);
            TBoldGenericListPart residents = Controller.Parts.Add();
            residents.Name = "Residents";
            residents.ElementExpression = "residents";
            residents.InterpretAsList = true;
            TBoldGenericListPart address = Controller.Parts.Add();
            address.Name = "Address";
            address.ElementExpression = "address";
        }

        public TBoldFollowerController? ByName(string name)
        {
            Asked.Add(name);
            return name switch
            {
                "Person" => PersonRow,
                "<Default>" => DefaultRow,
                _ => null,
            };
        }

        public void Dispose() => Sample.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldGenericListController), "*")]
    [Covers(typeof(TBoldGenericListPart), "*")]
    [Covers(typeof(TBoldGenericListParts), "*")]
    [Covers(typeof(TBoldGenericAsListRenderer), "*")]
    [Covers(typeof(TBoldFollowerListWithOwnedList), "*")]
    public void Parts_give_the_rows_and_their_controllers()
    {
        using var g = new GenericFixture();
        Assert.That(g.Controller.CanHaveSubFollowers(), Is.True);
        Assert.That(g.Controller.FindPartByName("address"), Is.SameAs(g.Controller.Parts[1]), "names are found ignoring case");
        Assert.That(g.Controller.FindPartByName("nothing"), Is.Null);
        Assert.That(g.Controller.Parts[0].ControllerExpression, Does.Contain("<Default>"));

        using TBoldFollower list = Follow(g.House, g.Controller);
        Assert.That(Texts(list), Is.EqualTo(new[] { "Ada", "Bob", "Main Street" }));
        Assert.That(g.Asked, Does.Contain("Person"), "a person's type name picks the person controller");
        Assert.That(g.Asked, Does.Contain("<Default>"), "a string falls through to the default");

        g.House.Residents.Add(g.Sample.NewPerson("Cy"));
        GuiSample.Display();
        Assert.That(Texts(list), Is.EqualTo(new[] { "Ada", "Bob", "Cy", "Main Street" }));

        g.Controller.Parts[0].Enabled = false;
        GuiSample.Display();
        Assert.That(Texts(list), Is.EqualTo(new[] { "Main Street" }), "a disabled part gives no rows");
        g.Controller.Parts[0].Enabled = true;
        GuiSample.Display();
        Assert.That(list.SubFollowerCount, Is.EqualTo(4));

        TBoldGenericListPart count = g.Controller.Parts.Add();
        count.ElementExpression = "residents->size";
        GuiSample.Display();
        Assert.That(Texts(list).Last(), Is.EqualTo("3"), "a computed element is owned by the rows");

        g.Controller.Parts.Delete(2);
        GuiSample.Display();
        Assert.That(list.SubFollowerCount, Is.EqualTo(4));
    }

    [Test]
    [Covers(typeof(TBoldGenericListPart), "*")]
    [Covers(typeof(TBoldGenericListController), "*")]
    public void Part_events_replace_the_element_and_the_controller()
    {
        using var g = new GenericFixture();
        TBoldGenericListPart address = g.Controller.Parts[1];
        address.OnGetElement = (_, element, _, result, _) => result.SetReferenceValue(((TResidential_Building)element!).M_Address);
        address.OnGetFollowerController = (_, _, byName) => byName("<Default>");
        using TBoldFollower list = Follow(g.House, g.Controller);
        Assert.That(Texts(list).Last(), Is.EqualTo("Main Street"));

        address.OnGetFollowerController = (_, _, _) => throw new InvalidOperationException("no controller");
        g.Controller.Parts[1].ElementExpression = "address + '!'";
        Assert.That(() => GuiSample.Display(), Throws.InstanceOf<EBold>().With.Message.Contains("no controller").And.Message.Contains("occured when getting controller"));

        var copy = new TBoldGenericListController(null, g.ByName);
        copy.Assign(g.Controller);
        Assert.That(copy.Parts.Count, Is.EqualTo(2));
        Assert.That(copy.Parts[0].ElementExpression, Is.EqualTo("residents"));
        Assert.That(copy.Parts[0].InterpretAsList, Is.True);
        Assert.That(copy.Parts[1].OnGetFollowerController, Is.Not.Null);
        copy.Parts = g.Controller.Parts;
        Assert.That(copy.Parts.Count, Is.EqualTo(2));
        Assert.That(copy.Renderer, Is.Null);
        copy.Renderer = new TBoldGenericAsListRenderer();
        Assert.That(copy.Renderer, Is.Not.Null);
        Assert.That(TBoldGenericAsListRenderer.DefaultRenderer(), Is.Not.Null);

        g.Controller.OnGetContextType = () => g.Sample.System.BoldSystemTypeInfo.ClassTypeInfoByExpressionName("Residential_Building");
        Assert.That(g.Controller.Parts[0].ContextType, Is.SameAs(g.Controller.ContextType), "a part's context is its controller's");
        g.Controller.Dispose();
    }

    /// <summary>A tree of buildings with their residents: a node description for each class.</summary>
    private sealed class TreeFixture : IDisposable
    {
        public readonly GuiSample Sample = new();
        public readonly TBoldTreeFollowerController Tree = new(null);
        public readonly TBoldNodeDescription Building;
        public readonly TBoldNodeDescription Person;
        public readonly List<string> Changes = [];
        public readonly TPerson Ada;

        public TreeFixture()
        {
            TResidential_Building main = Sample.NewHouse("Main Street");
            Ada = Sample.NewPerson("Ada");
            main.Residents.Add(Ada);
            main.Residents.Add(Sample.NewPerson("Bob"));
            Sample.NewHouse("Side Street").Residents.Add(Sample.NewPerson("Cy"));

            TBoldGenericListPart root = Tree.Parts.Add();
            root.ElementExpression = "Residential_Building.allInstances->orderBy(address)";
            root.InterpretAsList = true;

            Building = Tree.NodeDescriptions.Add();
            Building.Name = "Residential_Building";
            Building.TextController.Expression = "address";
            Building.IconController.Expression = "1";
            TBoldGenericListPart residents = Building.ListController.Parts.Add();
            residents.ElementExpression = "residents->orderBy(firstName)";
            residents.InterpretAsList = true;

            Person = Tree.NodeDescriptions.Add();
            Person.Name = "Person";
            Person.TextController.Expression = "firstName";

            Tree.OnTextChanged = f => Changes.Add("text " + Text(f));
            Tree.OnIconChanged = f => Changes.Add("icon " + ((TBoldIntegerFollowerController)f.Controller!).GetCurrentAsInteger(f));
        }

        public void Dispose()
        {
            Tree.Dispose();
            Sample.Dispose();
        }
    }

    /// <summary>The text of a node follower and of its children, as "text(child, child)".</summary>
    private static string Node(TBoldFollower node)
    {
        TBoldFollower list = node.SubFollowers(BoldControlPackDefs.BoldNodeListIndex)!;
        string text = Text(node.SubFollowers(BoldControlPackDefs.BoldNodeTextIndex)!);
        return list.SubFollowerCount == 0 ? text : text + "(" + string.Join(", ", Enumerable.Range(0, list.SubFollowerCount).Select(i => Node(list.SubFollowers(i)!))) + ")";
    }

    [Test]
    [Covers(typeof(TBoldTreeFollowerController), "*")]
    [Covers(typeof(TBoldNodeDescriptions), "*")]
    [Covers(typeof(TBoldNodeDescription), "*")]
    [Covers(typeof(TBoldNodeFollowerController), "*")]
    public void A_tree_shows_each_node_through_its_description()
    {
        using var t = new TreeFixture();
        using TBoldFollower tree = Follow(t.Sample.System, t.Tree);
        Assert.That(Enumerable.Range(0, tree.SubFollowerCount).Select(i => Node(tree.SubFollowers(i)!)), Is.EqualTo(new[] { "Main Street(Ada, Bob)", "Side Street(Cy)" }));
        Assert.That(t.Changes, Does.Contain("text Main Street").And.Contain("icon 1").And.Contain("icon -1"), "a node without an icon expression shows -1");

        t.Ada.FirstName = "Abe";
        GuiSample.Display();
        Assert.That(Node(tree.SubFollowers(0)!), Is.EqualTo("Main Street(Abe, Bob)"));

        Assert.That(t.Tree.NodeDescriptions.FindByName("person"), Is.SameAs(t.Person));
        Assert.That(t.Tree.DefaultGetNodeFollowerControllerByName("Building"), Is.Null, "no description, no default");
        t.Tree.DefaultNodeDescriptionName = "Person";
        Assert.That(t.Tree.DefaultGetNodeFollowerControllerByName("<Default>"), Is.SameAs(t.Person.NodeFollowerController));
        Assert.That(() => t.Tree.DefaultNodeDescriptionName = "Nothing", Throws.InstanceOf<EBold>());
        t.Tree.DefaultNodeDescriptionName = "";
        Assert.That(t.Tree.DefaultNodeDescriptionName, Is.EqualTo(""));
        t.Tree.OnGetFollowerController = name => name == "Person" ? t.Building.NodeFollowerController : null;
        Assert.That(t.Tree.DoGetFollowerController("Person"), Is.SameAs(t.Building.NodeFollowerController), "the event replaces the lookup");
        t.Tree.OnGetFollowerController = null;
    }

    [Test]
    [Covers(typeof(TBoldNodeDescription), "*")]
    [Covers(typeof(TBoldNodeFollowerController), "*")]
    [Covers(typeof(TBoldNodeDescriptions), "*")]
    public void Node_descriptions_have_unique_names_and_copy_their_settings()
    {
        using var t = new TreeFixture();
        Assert.That(() => t.Person.Name = "residential_building", Throws.InstanceOf<EBold>(), "names are unique, ignoring case");
        t.Person.Name = "PERSON";
        Assert.That(t.Person.DisplayName, Is.EqualTo("PERSON"));
        Assert.That(t.Tree.NodeDescriptions.Add().DisplayName, Is.EqualTo(nameof(TBoldNodeDescription)), "a description without a name shows its class");

        t.Person.HideNodeWithNoChildren = true;
        Assert.That(t.Person.NodeFollowerController.HideNodeWithNoChildren, Is.True);
        Assert.That(t.Person.NodeFollowerController.GenericListFollowerController, Is.SameAs(t.Person.ListController));
        Assert.That(t.Person.NodeFollowerController.IconFollowerController, Is.SameAs(t.Person.IconController));
        Assert.That(t.Person.NodeFollowerController.TextFollowerController, Is.SameAs(t.Person.TextController));

        t.Tree.OnGetContextType = () => t.Sample.System.BoldSystemTypeInfo.ClassTypeInfoByExpressionName("Building");
        Assert.That(t.Person.NodeFollowerController.SystemTypeInfo, Is.SameAs(t.Sample.System.BoldSystemTypeInfo));
        Assert.That(t.Person.TextController.ContextType!.ExpressionName, Is.EqualTo("Person"), "a description's context type is the type it names");
        t.Person.ContextTypeName = "Residential_Building";
        Assert.That(t.Person.TextController.ContextType!.ExpressionName, Is.EqualTo("Residential_Building"));
        t.Tree.OnGetContextType = null;
        Assert.That(t.Person.TextController.ContextType, Is.Null);
        Assert.That(t.Person.NodeFollowerController.SystemTypeInfo, Is.Null);

        var copy = new TBoldTreeFollowerController(null);
        copy.Assign(t.Tree);
        Assert.That(copy.NodeDescriptions.Count, Is.EqualTo(3));
        TBoldNodeDescription person = copy.NodeDescriptions[1];
        Assert.That((person.Name, person.ContextTypeName, person.HideNodeWithNoChildren, person.TextController.Expression), Is.EqualTo(("PERSON", "Residential_Building", true, "firstName")));
        Assert.That(copy.NodeDescriptions[0].ListController.Parts.Count, Is.EqualTo(1));
        person.ListController = t.Building.ListController;
        person.IconController = t.Building.IconController;
        person.TextController = t.Building.TextController;
        Assert.That(person.IconController.Expression, Is.EqualTo("1"));
        copy.NodeDescriptions = t.Tree.NodeDescriptions;
        Assert.That(copy.NodeDescriptions.Count, Is.EqualTo(3));
        copy.Dispose();
    }
}
