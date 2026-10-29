using System.Drawing;
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>Buttons, check boxes and menu items linked to actions.</summary>
[NonParallelizable]
public class BoldActionLinkTests
{
    private static Form ShownForm(params Control[] controls)
    {
        var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
        form.Controls.AddRange(controls);
        form.Show();
        return form;
    }

    [Test]
    public void A_linked_button_shows_its_action_and_a_click_executes_it()
    {
        var button = new Button();
        using Form form = ShownForm(button);
        int executed = 0, updates = 0;
        using var action = new TAction { Caption = "&Go", Hint = "Goes" };
        action.OnExecute = _ => executed++;

        TBoldActionLink link = TBoldActionLink.Link(button, action);
        Assert.That(link.Client, Is.SameAs(button));
        Assert.That(link.Action, Is.SameAs(action));
        Assert.That(TBoldActionLink.ActionOf(button), Is.SameAs(action));
        Assert.That(button.Text, Is.EqualTo("&Go"));
        Assert.That(link.SimpleText, Is.EqualTo("&Go"));

        button.PerformClick();
        Assert.That(executed, Is.EqualTo(1));

        action.Enabled = false;
        Assert.That(button.Enabled, Is.False);
        Assert.That(link.Execute(), Is.False, "a disabled action does not execute");
        action.Enabled = true;
        action.Visible = false;
        Assert.That(button.Visible, Is.False);
        action.Visible = true;
        Assert.That(button.Visible, Is.True);

        // The action is updated when the application is idle (with the link as its target).
        action.OnUpdate = _ =>
        {
            updates++;
            action.Caption = "Went";
        };
        Application.RaiseIdle(EventArgs.Empty);
        Assert.That(updates, Is.EqualTo(1));
        Assert.That(button.Text, Is.EqualTo("Went"));
        link.Update();
        Assert.That(updates, Is.EqualTo(2));

        // An update that fails is reported once.
        var reported = new List<Exception>();
        void Report(object? sender, System.Threading.ThreadExceptionEventArgs e) => reported.Add(e.Exception);
        Application.ThreadException += Report;
        try
        {
            action.OnUpdate = _ => throw new InvalidOperationException("no update");
            Application.RaiseIdle(EventArgs.Empty);
            Application.RaiseIdle(EventArgs.Empty);
            Assert.That(reported, Has.Count.EqualTo(1));
        }
        finally
        {
            Application.ThreadException -= Report;
            action.OnUpdate = null;
        }
    }

    [Test]
    public void Check_boxes_and_menu_items_show_checked_and_hint_and_links_go_with_their_clients_and_actions()
    {
        var check = new CheckBox();
        var other = new Button();
        using Form form = ShownForm(check, other);
        using var menu = new ContextMenuStrip();
        var item = new ToolStripMenuItem();
        menu.Items.Add(item);
        int executed = 0;
        var action = new TAction { Caption = "Check", Hint = "Checks", Checked = true };
        action.OnExecute = _ => executed++;

        TBoldActionLink.Link(check, action);
        Assert.That(check.Checked, Is.True);
        action.Checked = false;
        Assert.That(check.Checked, Is.False);

        TBoldActionLink.Link(item, action);
        Assert.That(item.Text, Is.EqualTo("Check"));
        Assert.That(item.ToolTipText, Is.EqualTo("Checks"));
        Assert.That(item.Checked, Is.False);
        item.PerformClick();
        Assert.That(executed, Is.EqualTo(1));

        // Linking again replaces the action; unlinking leaves the client alone.
        var second = new TAction { Caption = "Second" };
        TBoldActionLink.Link(other, action);
        TBoldActionLink.Link(other, second);
        Assert.That(TBoldActionLink.ActionOf(other), Is.SameAs(second));
        action.Caption = "Changed";
        Assert.That(other.Text, Is.EqualTo("Second"), "the first action no longer shows");
        TBoldActionLink.Unlink(other);
        Assert.That(TBoldActionLink.ActionOf(other), Is.Null);
        second.Caption = "Again";
        Assert.That(other.Text, Is.EqualTo("Second"));

        // A link goes with its action, and with its client.
        action.Dispose();
        Assert.That(TBoldActionLink.ActionOf(check), Is.Null);
        Assert.That(TBoldActionLink.ActionOf(item), Is.Null);
        TBoldActionLink.Link(other, second);
        other.Dispose();
        Assert.That(TBoldActionLink.ActionOf(other), Is.Null);
        second.Dispose();
    }

    [Test]
    public void A_failure_detection_action_shows_the_last_failure_in_its_linked_control()
    {
        var status = new Label { Text = "old" };
        using Form form = ShownForm(status);
        using var action = new TBoldFailureDetectionAction();
        TBoldActionLink link = TBoldActionLink.Link(status, action);
        Assert.That(action.HandlesTarget(link), Is.True, "the link is the control linked to the action");
        Assert.That(action.HandlesTarget(new object()), Is.False);
        link.Update();
        Assert.That(status.Text, Is.EqualTo(BoldSystem.GetBoldLastFailureReason()?.Reason ?? ""));
    }
}
