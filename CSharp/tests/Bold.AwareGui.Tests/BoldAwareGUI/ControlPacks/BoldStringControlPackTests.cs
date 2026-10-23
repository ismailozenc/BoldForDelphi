// Delphi: Source/BoldAwareGUI/ControlPacks/BoldStringControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldControlPack.pas,
// Source/BoldAwareGUI/ControlPacks/BoldElementHandleFollower.pas
// Port map: CSharp/port/units/BoldStringControlPack.md
using System.Drawing;
using BuildingClasses;
using static Bold.BoldControlPackDefs;

namespace Bold.Tests;

[NonParallelizable]
public class BoldStringControlPackTests
{
    [Test]
    [Covers(typeof(TBoldElementHandleFollower), "*")]
    [Covers(typeof(TBoldStringFollowerController), "*")]
    [Covers(typeof(TBoldStringRendererData), "*")]
    [Covers(typeof(TBoldFollowerSubscriber), "*")]
    public void A_handle_follower_shows_the_expression_and_follows_changes()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada", "Lovelace");
        TBoldReferenceHandle handle = s.Reference(ada);
        var controller = new TBoldStringFollowerController(null) { Expression = "firstName", NilStringRepresentation = "(none)" };
        using var handleFollower = new TBoldElementHandleFollower(null, controller) { BoldHandle = handle };
        TBoldFollower follower = handleFollower.Follower;

        GuiSample.Display();
        Assert.That(follower.Element, Is.SameAs(ada));
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsCurrent));
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Ada"));
        Assert.That(((TBoldStringRendererData)follower.RendererData).OldStringValue, Is.EqualTo("Ada"));
        Assert.That(controller.MayModify(follower), Is.True);

        ada.FirstName = "Augusta";
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsValueOutOfDate), "a change makes the follower out of date");
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Augusta"));

        handle.Value = s.NewPerson("Grace");
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Grace"));

        handle.Value = null;
        GuiSample.Display();
        Assert.That(follower.Element, Is.Null);
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("(none)"), "no element shows the nil representation");

        Assert.Throws<EBoldInternal>(() => handleFollower.Apply());
        Assert.Throws<EBoldInternal>(() => handleFollower.DiscardChange());
    }

    [Test]
    [Covers(typeof(TBoldFollower), "*")]
    [Covers(typeof(TBoldSingleFollowerController), "*")]
    [Covers(typeof(TBoldFollowerController), "*")]
    public void Edits_make_the_follower_dirty_until_applied_or_discarded()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        var controller = new TBoldStringFollowerController(null) { Expression = "firstName", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using var handleFollower = new TBoldElementHandleFollower(null, controller) { BoldHandle = s.Reference(ada) };
        TBoldFollower follower = handleFollower.Follower;
        GuiSample.Display();

        controller.MayHaveChanged("Ada", follower);
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsCurrent), "the same string is no change");
        controller.MayHaveChanged("Anna", follower);
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsDirty));
        Assert.That(ada.FirstName, Is.EqualTo("Ada"), "nothing is written before apply");
        follower.Apply();
        Assert.That(ada.FirstName, Is.EqualTo("Anna"));
        GuiSample.Display();
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsCurrent));

        controller.MayHaveChanged("Bea", follower);
        follower.DiscardChange();
        GuiSample.Display();
        Assert.That(ada.FirstName, Is.EqualTo("Anna"));
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Anna"), "discarding shows the element's value again");

        controller.MayHaveChanged("Cleo", follower);
        GuiSample.ApplyAll();
        Assert.That(ada.FirstName, Is.EqualTo("Cleo"), "the queue applies pending edits");
    }

    [Test]
    [Covers(typeof(TBoldAsStringRenderer), "*")]
    [Covers(typeof(TBoldRenderer), "*")]
    [Covers(typeof(TBoldSingleRenderer), "*")]
    public void Renderer_events_replace_the_default_steps()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada", "Lovelace");
        var setTo = new List<string>();
        var renderer = new TBoldAsStringRenderer
        {
            OnGetAsString = (e, _, _) => ((TPerson)e!).FirstName + " " + ((TPerson)e!).LastName,
            OnSetAsString = (_, v, _, _) => setTo.Add(v),
            OnValidateCharacter = (_, v, _, _) => v != "#",
            OnValidateString = (_, v, _, _) => v.Length > 1,
            OnSubscribe = (e, _, _, sub) => e!.SubscribeToExpression("firstName", sub),
            OnMayModify = (_, _, _, _) => true,
        };
        var controller = new TBoldStringFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using var handleFollower = new TBoldElementHandleFollower(null, controller) { BoldHandle = s.Reference(ada) };
        TBoldFollower follower = handleFollower.Follower;
        GuiSample.Display();

        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Ada Lovelace"));
        Assert.That(controller.GetAsString(ada), Is.EqualTo("Ada Lovelace"));
        Assert.That(controller.ValidateCharacter('#', follower), Is.False);
        Assert.That(controller.ValidateCharacter('a', follower), Is.True);
        Assert.That(controller.ValidateString("x", follower), Is.False);

        ada.FirstName = "Augusta";
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Augusta Lovelace"), "OnSubscribe decides what the follower follows");

        controller.MayHaveChanged("Countess", follower);
        follower.Apply();
        Assert.That(setTo, Is.EqualTo(new[] { "Countess" }));
        GuiSample.Display();

        controller.MayHaveChanged("x", follower);
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsDirty));
        Assert.That(() => follower.Apply(), Throws.InstanceOf<Exception>(), "a string that does not validate is not applied");
        Assert.That(follower.State, Is.Not.EqualTo(TBoldFollowerState.bfsDirty), "a failed apply discards the change");
    }

    [Test]
    [Covers(typeof(TBoldAsStringRenderer), "*")]
    public void Fonts_colors_and_drawing_go_through_the_renderer()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        var renderer = new TBoldAsStringRenderer
        {
            OnSetFont = (_, font, _, _) => font.Style = DelphiSet<TFontStyle>.Of(TFontStyle.fsBold),
            OnSetColor = (_, ref color, _, _) => color = Color.Red,
        };
        var controller = new TBoldStringFollowerController(null) { Expression = "firstName", Renderer = renderer };
        using var handleFollower = new TBoldElementHandleFollower(null, controller) { BoldHandle = s.Reference(ada) };
        GuiSample.Display();

        var effective = new TFont();
        controller.SetFont(effective, new TFont(), handleFollower.Follower);
        Assert.That(effective.Style.Contains(TFontStyle.fsBold), Is.True);
        Color color = Color.White;
        controller.SetColor(ref color, Color.White, handleFollower.Follower);
        Assert.That(color, Is.EqualTo(Color.Red));

        var canvas = new RecordingCanvas();
        controller.DrawOnCanvas(handleFollower.Follower, canvas, new Rectangle(0, 0, 100, 20), TAlignment.taLeftJustify, new Point(2, 2));
        Assert.That(canvas.Texts, Has.Some.Contains("Ada"));
    }

    [Test]
    [Covers(typeof(TBoldFollowerController), "*")]
    public void Controllers_copy_their_settings_and_tell_their_followers_of_changes()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada", "Lovelace");
        var controller = new TBoldStringFollowerController(null) { Expression = "firstName" };
        using var handleFollower = new TBoldElementHandleFollower(null, controller) { BoldHandle = s.Reference(ada) };
        GuiSample.Display();

        var other = new TBoldStringFollowerController(null) { Expression = "lastName", ApplyPolicy = TBoldApplyPolicy.bapChange, DragMode = TBoldDragMode.bdgSelection };
        controller.Assign(other);
        Assert.That(controller.Expression, Is.EqualTo("lastName"));
        Assert.That(controller.ApplyPolicy, Is.EqualTo(TBoldApplyPolicy.bapChange));
        Assert.That(controller.DragMode, Is.EqualTo(TBoldDragMode.bdgSelection));
        Assert.That(handleFollower.Follower.State, Is.EqualTo(TBoldFollowerState.bfsSubscriptionOutOfDate), "a changed controller makes its followers resubscribe");
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsString(handleFollower.Follower), Is.EqualTo("Lovelace"));

        Assert.That(() => controller.Assign(new TBoldIntegerFollowerController(null)), Throws.InstanceOf<Exception>(), "only a controller of the same class assigns");

        int before = 0, after = 0;
        controller.BeforeMakeUptoDate = _ => before++;
        controller.AfterMakeUptoDate = _ => after++;
        ada.LastName = "Byron";
        GuiSample.Display();
        Assert.That((before, after), Is.EqualTo((1, 1)));
    }

    [Test]
    [Covers(typeof(TBoldGUIHandler), "*")]
    public void Dragging_a_followed_object_and_dropping_it_on_a_link()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        TResidential_Building house = s.NewHouse("Main Street");
        var controller = new TBoldStringFollowerController(null) { Expression = "firstName", DragMode = TBoldDragMode.bdgSelection, DropMode = TBoldDropMode.bdpReplace };
        using var person = new TBoldElementHandleFollower(null, controller) { BoldHandle = s.Reference(ada) };
        var addresses = new TBoldStringFollowerController(null) { Expression = "address", DragMode = TBoldDragMode.bdgSelection };
        using var building = new TBoldElementHandleFollower(null, addresses) { BoldHandle = s.Reference(house) };
        GuiSample.Display();
        TBoldGUIHandler gui = BoldGUI.BoldGUIHandler();

        controller.StartDrag(person.Follower);
        Assert.That(gui.DraggedObjects.Count, Is.EqualTo(1));
        Assert.That(gui.DraggedObjectsAssignable(house.M_Residents, TBoldDropMode.bdpAppend), Is.True);
        Assert.That(gui.DraggedObjectsAssignable(ada.M_Home, TBoldDropMode.bdpReplace), Is.False, "a person is not a building");
        Assert.That(gui.DraggedObjectsAssignable(house.M_Address, TBoldDropMode.bdpReplace), Is.False, "an attribute takes no objects");
        Assert.That(gui.DraggedObjectsAssignable(null, TBoldDropMode.bdpReplace), Is.False);
        Assert.That(controller.DragOver(person.Follower, house.M_Residents, 0), Is.True);
        Assert.That(() => controller.StartDrag(person.Follower), Throws.InstanceOf<EBold>(), "a drag starts with no dragged objects");
        controller.EndDrag();
        Assert.That(gui.DraggedObjects.Count, Is.EqualTo(0));

        addresses.StartDrag(building.Follower);
        Assert.That(controller.DragOver(person.Follower, ada.M_Home, 0), Is.True);
        controller.DragDrop(person.Follower, ada.M_Home, 0);
        Assert.That(ada.Home, Is.SameAs(house), "dropping a building on the home link sets it");
        addresses.EndDrag();
    }

    /// <summary>A canvas that records what is drawn.</summary>
    private sealed class RecordingCanvas : TCanvas
    {
        public readonly List<string> Texts = [];

        public override void TextRect(Rectangle Rect, int X, int Y, string Text) => Texts.Add(Text);

        public override void FillRect(Rectangle Rect)
        {
        }

        public override int TextWidth(string Text) => Text.Length * 6;

        public override int TextHeight(string Text) => 12;

        public override void Line(int X1, int Y1, int X2, int Y2)
        {
        }
    }
}
