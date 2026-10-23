// Delphi: Source/Handles/Actions/BoldActions.pas, Source/Handles/Actions/BoldHandleAction.pas, Source/Handles/Actions/BoldListActions.pas,
// Source/Handles/Actions/BoldUndoActions.pas, Source/Handles/Actions/BoldActionDefs.pas
// Port map: CSharp/port/units/BoldActions.md
using BuildingClasses;

namespace Bold.Tests;

/// <summary>Actions on system and list handles: enabled when they can run, and what they do when executed.</summary>
[NonParallelizable]
public class BoldActionsTests
{
    /// <summary>The action's state after the GUI asks it to update for no target in particular.</summary>
    private static bool Enabled(TCustomAction action)
    {
        action.Update();
        return action.Enabled;
    }

    [Test]
    [Covers(typeof(BoldActionDefs), "*")]
    public void Database_action_defaults()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldActionDefs), "Source/Handles/Actions/BoldActionDefs.pas", "BoldPreferedDbPath");
        Assert.That(BoldActionDefs.BoldPreferedDbPath, Is.EqualTo(""));
    }

    [Test]
    [Covers(typeof(TBoldHandleAction), "*")]
    [Covers(typeof(TBoldSystemHandleAction), "*")]
    [Covers(typeof(TBoldUpdateDBAction), "*")]
    public void Update_db_saves_dirty_objects_and_forgets_a_disposed_handle()
    {
        using var storage = new GuiSample();
        using var s = new GuiSample(activate: false);
        s.SystemHandle.PersistenceHandle = new TBoldPersistenceHandleSystem(s.Components) { SystemHandle = storage.SystemHandle };
        var action = new TBoldUpdateDBAction(s.Components);
        Assert.That(action.Caption, Is.EqualTo(HandlesConst.sUpdateDB));
        Assert.That(action.HandlesTarget(null), Is.True);
        Assert.That(Enabled(action), Is.False, "no handle");

        action.BoldSystemHandle = s.SystemHandle;
        Assert.That(Enabled(action), Is.False, "the system is not active");
        s.SystemHandle.Active = true;
        Assert.That(Enabled(action), Is.False, "nothing to save");
        s.NewPerson("Ann");
        Assert.That(Enabled(action), Is.True);
        Assert.That(action.Execute(), Is.True);
        Assert.That(s.System.BoldDirty, Is.False);
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("1"));

        s.SystemHandle.Active = false;
        s.SystemHandle.Dispose();
        Assert.That(action.BoldSystemHandle, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldActivateSystemAction), "*")]
    [Covers(typeof(TBoldSaveAction))]
    public void Activate_system_opens_and_closes_and_deals_with_dirty_objects()
    {
        using var storage = new GuiSample();
        using var s = new GuiSample(activate: false);
        s.SystemHandle.PersistenceHandle = new TBoldPersistenceHandleSystem(s.Components) { SystemHandle = storage.SystemHandle };
        var opened = new List<string>();
        var action = new TBoldActivateSystemAction(s.Components) { Name = "Activate", OnSystemOpened = _ => opened.Add("open"), OnSystemClosed = _ => opened.Add("close") };
        Assert.That(action.Caption, Is.EqualTo(""), "without a handle the caption is the name, which it did not have when it was made");
        Assert.That(action.SaveQuestion, Is.EqualTo(HandlesConst.sThereAreDirtyObjects));
        action.BoldSystemHandle = s.SystemHandle;
        Assert.That(action.Caption, Is.EqualTo(HandlesConst.sOpenSystem));
        action.OpenCaption = "Open";
        action.CloseCaption = "Close";
        Assert.That(action.Caption, Is.EqualTo("Open"));

        action.Execute();
        Assert.That(s.SystemHandle.Active, Is.True);
        Assert.That(action.Caption, Is.EqualTo("Close"));
        Assert.That(opened.Last(), Is.EqualTo("open"), "the handle tells while it opens, and once it is open");

        s.NewPerson("Ann");
        action.SaveOnClose = TBoldSaveAction.saAsk;
        s.Environment.DialogAnswer = Controls.mrCancel;
        action.Execute();
        Assert.That(s.SystemHandle.Active, Is.True, "cancel keeps the system open");
        Assert.That(s.Environment.Dialogs, Is.EqualTo(new[] { HandlesConst.sThereAreDirtyObjects }));
        s.Environment.DialogAnswer = Controls.mrYes;
        action.Execute();
        Assert.That(s.SystemHandle.Active, Is.False);
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("1"), "yes saves");
        Assert.That(opened.Last(), Is.EqualTo("close"));

        action.Execute();
        s.NewPerson("Bob");
        s.Environment.DialogAnswer = Controls.mrNo;
        action.Execute();
        Assert.That(s.SystemHandle.Active, Is.False);
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("1"), "no discards");

        action.Execute();
        s.NewPerson("Cy");
        action.SaveOnClose = TBoldSaveAction.saFail;
        Assert.That(() => action.Execute(), Throws.InstanceOf<EBold>().With.Message.EqualTo(HandlesConst.sClosingWithDirtyObjects));
        action.SaveOnClose = TBoldSaveAction.saYes;
        action.Execute();
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("2"));

        action.Execute();
        s.NewPerson("Dan");
        action.SaveOnClose = TBoldSaveAction.saNo;
        action.Execute();
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("2"));

        action.BoldSystemHandle = null;
        Assert.That(action.Caption, Is.EqualTo("Activate"));
    }

    private sealed class StatusBar : IActionClient, ISimpleTextTarget
    {
        public TBasicAction? Action { get; set; }
        public string SimpleText { get; set; } = "";
    }

    [Test]
    [Covers(typeof(TBoldFailureDetectionAction), "*")]
    public void Failure_detection_shows_the_last_failure_on_its_status_bar()
    {
        using var s = new GuiSample();
        var action = new TBoldFailureDetectionAction(s.Components);
        var bar = new StatusBar();
        Assert.That(action.HandlesTarget(bar), Is.False, "only the control linked to the action");
        bar.Action = action;
        Assert.That(action.HandlesTarget(bar), Is.True);

        BoldSystem.SetBoldLastFailureReason(new TBoldFailureReason("Out of coffee", null));
        action.Update(bar);
        Assert.That(action.Caption, Is.EqualTo("Out of coffee"));
        Assert.That(bar.SimpleText, Is.EqualTo("Out of coffee"));
        BoldSystem.SetBoldLastFailureReason(null);
        action.Update(bar);
        Assert.That(bar.SimpleText, Is.EqualTo(""));
    }

    private sealed class ListFixture : IDisposable
    {
        public readonly GuiSample Sample = new();
        public readonly TResidential_Building House;
        public readonly TBoldListHandle Residents;

        public ListFixture()
        {
            House = Sample.NewHouse("Main Street");
            foreach (string name in new[] { "Ada", "Bob", "Cy" })
            {
                House.Residents.Add(Sample.NewPerson(name));
            }

            Residents = new TBoldListHandle(Sample.Components) { RootHandle = Sample.Reference(House), Expression = "residents" };
        }

        public string Current => ((TPerson)Residents.CurrentBoldObject!).FirstName;

        public string[] Names() => [.. Enumerable.Range(0, House.Residents.Count).Select(i => ((TPerson)House.Residents[i]!).FirstName)];

        public void Dispose() => Sample.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldListHandleAction), "*")]
    [Covers(typeof(TBoldListHandleNextAction), "*")]
    [Covers(typeof(TBoldListHandlePrevAction), "*")]
    [Covers(typeof(TBoldListHandleFirstAction), "*")]
    [Covers(typeof(TBoldListHandleLastAction), "*")]
    public void Navigation_actions_move_the_current_element()
    {
        using var f = new ListFixture();
        var next = new TBoldListHandleNextAction(f.Sample.Components);
        var prev = new TBoldListHandlePrevAction(f.Sample.Components);
        var first = new TBoldListHandleFirstAction(f.Sample.Components);
        var last = new TBoldListHandleLastAction(f.Sample.Components);
        Assert.That((next.Caption, prev.Caption, first.Caption, last.Caption), Is.EqualTo((HandlesConst.sNext, HandlesConst.sPrev, HandlesConst.sFirst, HandlesConst.sLast)));
        Assert.That(Enabled(next), Is.False, "no handle");
        foreach (TBoldListHandleAction action in new TBoldListHandleAction[] { next, prev, first, last })
        {
            action.BoldHandle = f.Residents;
        }

        Assert.That(f.Current, Is.EqualTo("Ada"));
        Assert.That((Enabled(next), Enabled(prev)), Is.EqualTo((true, false)));
        next.Execute();
        Assert.That(f.Current, Is.EqualTo("Bob"));
        last.Execute();
        Assert.That(f.Current, Is.EqualTo("Cy"));
        Assert.That((Enabled(next), Enabled(prev)), Is.EqualTo((false, true)));
        prev.Execute();
        Assert.That(f.Current, Is.EqualTo("Bob"));
        first.Execute();
        Assert.That(f.Current, Is.EqualTo("Ada"));

        f.House.Residents.Clear();
        Assert.That((Enabled(first), Enabled(last)), Is.EqualTo((false, false)), "an empty list has no first or last");
        f.Residents.RootHandle = null;
        Assert.That(Enabled(next), Is.False, "no list");
    }

    [Test]
    [Covers(typeof(TBoldListHandleDeleteAction), "*")]
    [Covers(typeof(TBoldListHandleAddNewAction), "*")]
    [Covers(typeof(TBoldListHandleMoveUpAction), "*")]
    [Covers(typeof(TBoldListHandleMoveDownAction), "*")]
    [Covers(typeof(TBoldDeleteMode))]
    public void Editing_actions_add_delete_and_move_elements()
    {
        using var f = new ListFixture();
        var up = new TBoldListHandleMoveUpAction(f.Sample.Components) { BoldHandle = f.Residents };
        var down = new TBoldListHandleMoveDownAction(f.Sample.Components) { BoldHandle = f.Residents };
        var add = new TBoldListHandleAddNewAction(f.Sample.Components) { BoldHandle = f.Residents };
        var delete = new TBoldListHandleDeleteAction(f.Sample.Components) { BoldHandle = f.Residents };
        Assert.That((up.Caption, down.Caption, add.Caption, delete.Caption), Is.EqualTo((HandlesConst.sMoveUp, HandlesConst.sMoveDown, HandlesConst.sAddNew, HandlesConst.sDelete)));

        Assert.That((Enabled(up), Enabled(down)), Is.EqualTo((false, true)));
        down.Execute();
        Assert.That(f.Names(), Is.EqualTo(new[] { "Bob", "Ada", "Cy" }));
        f.Residents.CurrentIndex = 1;
        up.Execute();
        Assert.That(f.Names(), Is.EqualTo(new[] { "Ada", "Bob", "Cy" }));

        Assert.That(Enabled(add), Is.True);
        add.Execute();
        Assert.That(f.House.Residents.Count, Is.EqualTo(4));

        f.Residents.CurrentIndex = 0;
        Assert.That(delete.BoldDeleteMode, Is.EqualTo(TBoldDeleteMode.dmDefault));
        Assert.That(Enabled(delete), Is.True);
        delete.Execute();
        Assert.That(f.House.Residents.Count, Is.EqualTo(3), "the default removes from the list");
        Assert.That(f.Sample.System.ClassByExpressionName("Person").Count, Is.EqualTo(4), "and keeps the object");

        f.Residents.CurrentIndex = 0;
        delete.BoldDeleteMode = TBoldDeleteMode.dmDelete;
        Assert.That(Enabled(delete), Is.True);
        delete.Execute();
        Assert.That(f.Sample.System.ClassByExpressionName("Person").Count, Is.EqualTo(3), "dmDelete deletes the object");

        f.Residents.CurrentIndex = 0;
        delete.BoldDeleteMode = TBoldDeleteMode.dmUnlinkAllAndDelete;
        delete.Execute();
        Assert.That(f.Sample.System.ClassByExpressionName("Person").Count, Is.EqualTo(2));

        delete.BoldDeleteMode = (TBoldDeleteMode)99;
        Assert.That(() => Enabled(delete), Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers(typeof(TBoldSetCheckPointAction), "*")]
    [Covers(typeof(TBoldUndoAction), "*")]
    [Covers(typeof(TBoldRedoAction), "*")]
    public void Undo_actions_set_check_points_undo_and_redo()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        var undoHandler = (TBoldUndoHandler)s.System.UndoHandler;
        undoHandler.SetEnabled(true);
        var checkPoint = new TBoldSetCheckPointAction(s.Components) { BoldSystemHandle = s.SystemHandle };
        var undo = new TBoldUndoAction(s.Components) { BoldSystemHandle = s.SystemHandle };
        var redo = new TBoldRedoAction(s.Components) { BoldSystemHandle = s.SystemHandle };
        Assert.That((checkPoint.Caption, undo.Caption, redo.Caption), Is.EqualTo((HandlesConst.sSetCheckPoint, HandlesConst.sUndo, HandlesConst.sRedo)));
        Assert.That(Enabled(checkPoint), Is.True);
        Assert.That((Enabled(undo), Enabled(redo)), Is.EqualTo((false, false)), "nothing to undo or redo");

        ada.FirstName = "Anna";
        checkPoint.Execute();
        ada.FirstName = "Annie";
        Assert.That(Enabled(undo), Is.True);
        undo.Execute();
        Assert.That(ada.FirstName, Is.EqualTo("Anna"));
        Assert.That(Enabled(redo), Is.True);
        redo.Execute();
        Assert.That(ada.FirstName, Is.EqualTo("Annie"));

        s.SystemHandle.Active = false;
        Assert.That((Enabled(checkPoint), Enabled(undo), Enabled(redo)), Is.EqualTo((false, false, false)), "a closed system has no undo");
    }
}
