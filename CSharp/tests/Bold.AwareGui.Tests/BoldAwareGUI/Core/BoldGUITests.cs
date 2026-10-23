// Delphi: Source/BoldAwareGUI/Core/BoldGUI.pas, Source/BoldAwareGUI/Core/BoldExceptionHandlers.pas,
// Source/BoldAwareGUI/Core/BoldGuiResourceStrings.pas
// Port map: CSharp/port/units/BoldGUI.md
using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

[NonParallelizable]
public class BoldGUITests
{
    private sealed class FormsHandler : TBoldGUIHandler
    {
        public override object? FindHostingForm(object? Component) => Component is IComponent c ? c.Site?.Container : null;
    }

    [Test]
    [Covers(typeof(TBoldGUIHandler), "*")]
    [Covers(typeof(BoldGUI), "*")]
    public void The_GUI_handler_is_made_from_its_class_on_first_use()
    {
        TBoldGUIHandler handler = BoldGUI.BoldGUIHandler();
        Assert.That(handler, Is.SameAs(BoldGUI.BoldGUIHandler()));
        Assert.That(handler.FindHostingForm(new Component()), Is.Null, "the plain handler knows no forms");
        Assert.That(handler.TryToFocusHostingForm(new Component()), Is.False);
        Assert.That(handler.TopmostParentControl(new Component()), Is.Null);
        handler.DoPopUp(null);

        using var form = new Container();
        var control = new Component();
        form.Add(control);
        Assert.That(handler.ComponentOwner(control), Is.SameAs(form), "a component's owner is its container");

        BoldGUI.BoldGUIHandlerClass = typeof(FormsHandler);
        try
        {
            Assert.That(BoldGUI.BoldGUIHandler(), Is.InstanceOf<FormsHandler>());
            Assert.That(BoldGUI.BoldGUIHandler().FindHostingForm(control), Is.SameAs(form));
        }
        finally
        {
            BoldGUI.BoldGUIHandlerClass = typeof(TBoldGUIHandler);
        }

        Assert.That(BoldGUI.BoldGUIHandler(), Is.Not.SameAs(handler), "a new class makes a new handler");
        BoldGUI.BoldGUIHandler().ActivateTargetFormOnDrop = true;
        Assert.That(BoldGUI.BoldGUIHandler().ActivateTargetFormOnDrop, Is.True);
        BoldGUI.BoldGUIHandler().ActivateTargetFormOnDrop = false;
    }

    [Test]
    [Covers(typeof(TBoldPopup), "*")]
    public void Popups_remember_the_element_and_control()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        var menu = new object();
        BoldGUI.BoldPopupMenu = menu;
        try
        {
            var popup = new TBoldPopup { Enable = false, InsertNew = true, Delete = TBoldPopupDeleteType.bpdDelete, Move = true };
            Assert.That(popup.GetMenu("control", ada), Is.SameAs(menu));
            Assert.That(BoldGUI.BoldGUIHandler().PopupElement, Is.SameAs(ada));
            Assert.That(BoldGUI.BoldGUIHandler().PopupControl, Is.EqualTo("control"));
            var copy = new TBoldPopup();
            copy.Assign(popup);
            Assert.That((copy.Enable, copy.InsertNew, copy.Delete, copy.Move), Is.EqualTo((false, true, TBoldPopupDeleteType.bpdDelete, true)));
            copy.Enable = true;
            Assert.That(copy.GetMenu(null, null), Is.Null, "the menu itself is not built yet, as in Delphi");
            Assert.That(() => copy.Assign(new TBoldStringFollowerController(null)), Throws.InstanceOf<Exception>(), "only a popup assigns to a popup");
        }
        finally
        {
            BoldGUI.BoldPopupMenu = null;
        }
    }

    [Test]
    [Covers(typeof(TBoldExceptionHandler), "*")]
    [Covers(typeof(TBoldApplyExceptionEvent))]
    [Covers(typeof(TBoldDisplayExceptionEvent))]
    public void Exception_handlers_catch_the_errors_of_the_controls_of_their_form()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        using var form = new Container();
        var control = new Component();
        form.Add(control);
        var handler = new TBoldExceptionHandler(form);
        Assert.That(handler.Owner, Is.SameAs(form));
        Assert.That(TBoldExceptionHandler.FindExceptionHandler(control), Is.SameAs(handler));
        Assert.That(TBoldExceptionHandler.FindExceptionHandler(new Component()), Is.Null, "a control on another form");

        var shown = new List<string>();
        handler.OnDisplayException = (e, c, el) => shown.Add(e.Message + (c == control ? " on control" : ""));
        var broken = new TBoldStringFollowerController(control) { Expression = "noSuchMember" };
        using var follower = new TBoldFollower(control, broken) { Element = ada };
        GuiSample.Display();
        Assert.That(shown, Has.Count.EqualTo(1));
        Assert.That(shown[0], Does.Contain("noSuchMember").And.EndsWith(" on control"));

        var failures = new List<string>();
        var names = new TBoldStringFollowerController(control) { Expression = "firstName", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using var name = new TBoldFollower(control, names) { Element = ada };
        GuiSample.Display();
        handler.OnApplyException = (TBoldApplyExceptionEvent)((Exception e, object? c, TBoldElement? el, ref bool discard) =>
        {
            failures.Add(e.Message);
            discard = false;
        });
        names.Renderer = new TBoldAsStringRenderer { OnSetAsString = (_, _, _, _) => throw new InvalidOperationException("read only today"), OnMayModify = (_, _, _, _) => true };
        GuiSample.Display();
        names.MayHaveChanged("Anna", name);
        name.Apply();
        Assert.That(failures, Is.EqualTo(new[] { "read only today" }));
        Assert.That(name.State, Is.EqualTo(TBoldFollowerState.bfsDirty), "the handler kept the change");

        handler.Dispose();
        Assert.That(TBoldExceptionHandler.FindExceptionHandler(control), Is.Null);
        Assert.That(() => name.Apply(), Throws.InstanceOf<InvalidOperationException>(), "without a handler the error goes on");
    }

    [Test]
    [Covers(typeof(BoldGuiResourceStrings), "*")]
    public void The_messages_are_the_Delphi_messages()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldGuiResourceStrings), "Source/BoldAwareGUI/Core/BoldGuiResourceStrings.pas");
    }
}
