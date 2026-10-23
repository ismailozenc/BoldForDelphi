// Delphi: Source/BoldAwareGUI/ControlPacks/BoldListControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldListListControlPack.pas,
// Source/BoldAwareGUI/ControlPacks/BoldListHandleFollower.pas
// Port map: CSharp/port/units/BoldListControlPack.md
using BuildingClasses;

namespace Bold.Tests;

/// <summary>A list follower on a list handle: a row per element, each row a follower of the row controller.</summary>
[NonParallelizable]
public class BoldListControlPackTests
{
    private static string[] Rows(TBoldFollower list, TBoldStringFollowerController rows) =>
        [.. Enumerable.Range(0, list.SubFollowerCount).Select(i => rows.GetCurrentAsString(list.SubFollowers(i)!))];

    private static string[] Names(TBoldObjectList list) => [.. Enumerable.Range(0, list.Count).Select(i => ((TPerson)list[i]!).FirstName)];

    private sealed class Fixture : IDisposable
    {
        public readonly GuiSample Sample = new();
        public readonly TBoldListHandle People;
        public readonly TBoldStringFollowerController RowController = new(null) { Expression = "firstName", NilStringRepresentation = "-" };
        public readonly TBoldListAsFollowerListController ListController;
        public readonly TBoldListHandleFollower HandleFollower;
        public readonly List<string> Events = [];

        public Fixture(params string[] names)
        {
            foreach (string name in names)
            {
                Sample.NewPerson(name);
            }

            People = Sample.List("Person.allInstances->orderBy(firstName)");
            ListController = new TBoldListAsFollowerListController(null, RowController)
            {
                OnBeforeInsertItem = (i, _) => Events.Add("+" + i),
                OnAfterInsertItem = f => Events.Add("+" + f.index + "."),
                OnBeforeDeleteItem = f => Events.Add("-" + f.index),
                OnAfterDeleteItem = (i, _) => Events.Add("-" + i + "."),
            };
            HandleFollower = new TBoldListHandleFollower(null, ListController) { BoldHandle = People };
            GuiSample.Display();
        }

        public TBoldFollower List => HandleFollower.Follower;

        public string[] Rows() => BoldListControlPackTests.Rows(List, RowController);

        public void Dispose()
        {
            HandleFollower.Dispose();
            Sample.Dispose();
        }
    }

    [Test]
    [Covers(typeof(TBoldListHandleFollower), "*")]
    [Covers(typeof(TBoldListAsFollowerListController), "*")]
    [Covers(typeof(TBoldAbstractListAsFollowerListController), "*")]
    [Covers(typeof(TBoldListAsFollowerListRenderer), "*")]
    [Covers(typeof(TBoldAsFollowerListController), "*")]
    [Covers(typeof(TBoldAsFollowerListRenderer), "*")]
    [Covers(typeof(TBoldFollowerList), "*")]
    public void Rows_follow_the_list_as_it_changes()
    {
        using var f = new Fixture("Cy", "Ada", "Bob");
        Assert.That(f.List.Element, Is.SameAs(f.People.List));
        Assert.That(f.Rows(), Is.EqualTo(new[] { "Ada", "Bob", "Cy" }));
        Assert.That(f.Events, Is.EqualTo(new[] { "+0", "+0.", "+1", "+1.", "+2", "+2." }));
        Assert.That(f.List.SubFollowers(1)!.index, Is.EqualTo(1));
        Assert.That(f.List.SubFollowers(3), Is.Null);

        f.Events.Clear();
        f.Sample.NewPerson("Abe");
        GuiSample.Display();
        Assert.That(f.Rows(), Is.EqualTo(new[] { "Abe", "Ada", "Bob", "Cy" }));
        Assert.That(f.Events, Is.EqualTo(new[] { "+0", "+0." }), "only the new row is inserted");

        f.Events.Clear();
        var bob = (TPerson)f.People.List![2]!;
        bob.Delete();
        GuiSample.Display();
        Assert.That(f.Rows(), Is.EqualTo(new[] { "Abe", "Ada", "Cy" }));
        Assert.That(f.Events, Is.EqualTo(new[] { "-2", "-2." }), "only the gone row is deleted");

        ((TPerson)f.People.List![0]!).FirstName = "Zed";
        GuiSample.Display();
        Assert.That(f.Rows(), Is.EqualTo(new[] { "Ada", "Cy", "Zed" }));

        f.HandleFollower.BoldHandle = null;
        GuiSample.Display();
        Assert.That(f.List.Element, Is.Null);
        Assert.That(f.List.SubFollowerCount, Is.EqualTo(0));
        Assert.Throws<EBoldInternal>(() => f.HandleFollower.Apply());
        Assert.Throws<EBoldInternal>(() => f.HandleFollower.DiscardChange());
    }

    [Test]
    [Covers(typeof(TBoldListHandleFollower), "*")]
    public void The_current_row_and_the_handle_index_follow_each_other()
    {
        using var f = new Fixture("Ada", "Bob", "Cy");
        Assert.That(f.HandleFollower.HandleIndexLock, Is.True);
        Assert.That(f.List.CurrentIndex, Is.EqualTo(0));

        f.HandleFollower.SetFollowerIndex(2);
        Assert.That(f.People.CurrentIndex, Is.EqualTo(2));
        Assert.That(f.ListController.GetListIndex(f.List), Is.EqualTo(2));

        f.People.CurrentIndex = 1;
        GuiSample.Display();
        Assert.That(f.List.CurrentIndex, Is.EqualTo(1));

        f.HandleFollower.HandleIndexLock = false;
        f.HandleFollower.SetFollowerIndex(0);
        Assert.That(f.People.CurrentIndex, Is.EqualTo(1), "without the lock the handle keeps its index");

        f.People.Dispose();
        Assert.That(f.HandleFollower.BoldHandle, Is.Null, "a disposed handle is forgotten");
    }

    [Test]
    [Covers(typeof(TBoldAbstractListAsFollowerListController), "*")]
    public void An_empty_row_can_come_first_or_last()
    {
        using var f = new Fixture("Ada", "Bob");
        f.ListController.NilElementMode = TBoldNilElementMode.neInsertFirst;
        GuiSample.Display();
        Assert.That(f.Rows(), Is.EqualTo(new[] { "-", "Ada", "Bob" }));
        Assert.That(f.List.SubFollowers(0)!.Element, Is.Null);
        Assert.That(f.ListController.ListIndex(0), Is.EqualTo(1));
        Assert.That(f.ListController.ListIndex(int.MaxValue), Is.EqualTo(int.MaxValue), "the end of the range stays as it is");
        Assert.That(f.ListController.ListIndexToIndex(f.List, 1), Is.EqualTo(0));
        f.HandleFollower.SetFollowerIndex(2);
        Assert.That(f.People.CurrentIndex, Is.EqualTo(1), "the empty row is not in the list");
        Assert.That(f.ListController.GetListIndex(f.List), Is.EqualTo(1));

        f.ListController.NilElementMode = TBoldNilElementMode.neAddLast;
        GuiSample.Display();
        Assert.That(f.Rows(), Is.EqualTo(new[] { "Ada", "Bob", "-" }));
        Assert.That(f.ListController.ListIndex(0), Is.EqualTo(0));
        Assert.That(f.ListController.ListIndexToIndex(f.List, 1), Is.EqualTo(1));

        var copy = new TBoldListAsFollowerListController(null, f.RowController) { DefaultDblClick = false, InternalDrag = false };
        Assert.That((copy.DragMode, copy.DropMode), Is.EqualTo((TBoldDragMode.bdgSelection, TBoldDropMode.bdpAppend)));
        copy.Assign(f.ListController);
        Assert.That((copy.DefaultDblClick, copy.InternalDrag, copy.NilElementMode), Is.EqualTo((true, true, TBoldNilElementMode.neAddLast)));
        Assert.That(copy.Renderer, Is.Null);
        copy.Renderer = new TBoldListAsFollowerListRenderer();
        Assert.That(copy.Renderer, Is.Not.Null);
        Assert.That(TBoldListAsFollowerListRenderer.DefaultRenderer(), Is.SameAs(TBoldListAsFollowerListRenderer.DefaultRenderer()));
    }

    [Test]
    [Covers(typeof(TBoldFollowerList), "*")]
    [Covers(typeof(TBoldAsFollowerListController), "*")]
    public void Rows_are_selected_one_by_one_or_by_range()
    {
        using var f = new Fixture("Ada", "Bob", "Cy", "Dan");
        var rows = (TBoldFollowerList)f.List.RendererData;
        Assert.That(rows.Count, Is.EqualTo(4));

        f.ListController.SelectAll(f.List, true);
        Assert.That(rows.SelectedCount, Is.EqualTo(4));
        f.ListController.SelectAll(f.List, false);
        Assert.That(rows.SelectedCount, Is.EqualTo(0));

        f.ListController.SetSelected(f.List, 1, true);
        f.ListController.SelectRange(f.List, 3);
        Assert.That(Enumerable.Range(0, 4).Select(i => f.ListController.GetSelected(f.List, i)), Is.EqualTo(new[] { false, true, true, true }));
        f.ListController.SelectRange(f.List, 0);
        Assert.That(Enumerable.Range(0, 4).Select(i => rows.Selected(i)), Is.EqualTo(new[] { true, true, false, false }), "a range runs from the last row selected one by one");
        f.ListController.ToggleSelected(f.List, 3);
        Assert.That(rows.Selected(3), Is.True);
        f.ListController.SetSelected(f.List, 9, true); // outside: ignored

        var selected = new TBoldObjectList();
        rows.AddSelectedToList(selected);
        Assert.That(selected.Count, Is.EqualTo(3));
        Assert.That(((TPerson)selected[2]!).FirstName, Is.EqualTo("Dan"));

        rows.CurrentItem = 2;
        Assert.That(rows.CurrentItem, Is.EqualTo(2));
    }

    [Test]
    [Covers(typeof(TBoldListAsFollowerListRenderer), "*")]
    [Covers(typeof(TBoldFollowerList), "*")]
    public void Only_the_active_rows_get_their_elements()
    {
        using var f = new Fixture("Ada", "Bob", "Cy", "Dan", "Eve");
        var rows = (TBoldFollowerList)f.List.RendererData;
        f.ListController.SetActiveRange(f.List, 1, 2);
        Assert.That((rows.FirstActive, rows.LastActive), Is.EqualTo((1, 2)));
        Assert.That(Enumerable.Range(0, 5).Select(i => f.List.SubFollowers(i)!.Active), Is.EqualTo(new[] { false, true, true, false, false }));

        f.Sample.NewPerson("Fay");
        GuiSample.Display();
        Assert.That(rows.Count, Is.EqualTo(6));
        Assert.That(f.List.SubFollowers(4)!.ElementValid, Is.False, "a row outside the range waits for its element");
        Assert.That(f.List.SubFollowers(4)!.Element, Is.Null);
        f.ListController.SetActiveRange(f.List, 3, 5, 4);
        Assert.That(f.List.SubFollowers(4)!.ElementValid, Is.True, "activating the row gives it its element");
        Assert.That(((TPerson)f.List.SubFollowers(4)!.Element!).FirstName, Is.EqualTo("Eve"));
        GuiSample.Display();
        Assert.That(f.RowController.GetCurrentAsString(f.List.SubFollowers(5)!), Is.EqualTo("Fay"));
    }

    [Test]
    [Covers(typeof(TBoldListAsFollowerListRenderer), "*")]
    public void Selected_rows_drag_to_another_list()
    {
        using var f = new Fixture("Ada", "Bob", "Cy");
        TResidential_Building house = f.Sample.NewHouse("Main Street");
        TPerson zed = f.Sample.NewPerson("Zed");
        house.Residents.Add(zed);
        GuiSample.Display();
        f.ListController.SetSelected(f.List, 0, true);
        f.ListController.SetSelected(f.List, 2, true);

        f.ListController.StartDrag(f.List);
        Assert.That(BoldGUI.BoldGUIHandler().DraggedObjects.Count, Is.EqualTo(2));
        Assert.That(() => f.ListController.StartDrag(f.List), Throws.InstanceOf<EBold>());
        Assert.That(f.ListController.DragOver(f.List, house.M_Residents, 0), Is.True);
        f.ListController.DropMode = TBoldDropMode.bdpInsert;
        f.ListController.DragDrop(f.List, house.M_Residents, 0);
        Assert.That(Names(house.Residents), Is.EqualTo(new[] { "Ada", "Cy", "Zed" }), "inserted at the drop index");
        f.ListController.EndDrag();

        f.ListController.SelectAll(f.List, false);
        f.ListController.SetSelected(f.List, 3, true); // Zed
        f.ListController.StartDrag(f.List);
        f.ListController.DragDrop(f.List, house.M_Residents, 0);
        Assert.That(Names(house.Residents), Is.EqualTo(new[] { "Zed", "Ada", "Cy" }), "an object already in the list moves");
        f.ListController.EndDrag();

        f.ListController.SetSelected(f.List, 3, false);
        f.ListController.SetSelected(f.List, 1, true); // Bob
        f.ListController.StartDrag(f.List);
        f.ListController.DropMode = TBoldDropMode.bdpAppend;
        f.ListController.DragDrop(f.List, house.M_Residents, 0);
        Assert.That(((TPerson)house.Residents[3]!).FirstName, Is.EqualTo("Bob"));
        f.ListController.DropMode = TBoldDropMode.bdpReplace;
        Assert.That(() => f.ListController.DragDrop(f.List, house.M_Residents, 0), Throws.InstanceOf<EBoldFeatureNotImplementedYet>());
        f.ListController.EndDrag();
        Assert.That(f.ListController.DragOver(f.List, house.M_Residents, 0), Is.False, "nothing is dragged");
    }
}
