// Delphi's action links (the Action property of controls and menu items, TActionLink) for WinForms. Not a Bold unit,
// so it has no port markers.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Bold;

/// <summary>
/// Links a control or a tool strip item (a menu item, a tool bar button) to an action, as setting Action on a Delphi
/// control or menu item did: the client shows the action's caption, enabled, visible and checked states (and its hint,
/// on a tool strip item), clicking it executes the action, and the action is updated when the application is idle, as
/// Delphi's action lists were. The link is the target the action sees: an <see cref="IActionClient"/> whose Action is
/// the action, and an <see cref="ISimpleTextTarget"/> whose text is the client's.
/// </summary>
public sealed class TBoldActionLink : IActionClient, ISimpleTextTarget, IDisposable
{
    private static readonly ConditionalWeakTable<Component, TBoldActionLink> fLinks = new();

    private readonly Component fClient;
    private TBasicAction? fAction;

    private TBoldActionLink(Component client, TBasicAction action)
    {
        fClient = client;
        fAction = action;
        switch (client)
        {
            case Control control:
                control.Click += ClientClick;
                break;
            case ToolStripItem item:
                item.Click += ClientClick;
                break;
        }

        client.Disposed += ClientDisposed;
        action.Changed += ActionChanged;
        action.Disposed += ActionDisposed;
        Application.Idle += ApplicationIdle;
        ShowAction();
    }

    /// <summary>Links the control to the action (in place of any action it had).</summary>
    public static TBoldActionLink Link(Control client, TBasicAction action) => Create(client, action);

    /// <summary>Links the menu item or tool bar button to the action (in place of any action it had).</summary>
    public static TBoldActionLink Link(ToolStripItem client, TBasicAction action) => Create(client, action);

    private static TBoldActionLink Create(Component client, TBasicAction action)
    {
        Unlink(client);
        var link = new TBoldActionLink(client, action);
        fLinks.AddOrUpdate(client, link);
        return link;
    }

    /// <summary>The action linked to the client; null when none.</summary>
    public static TBasicAction? ActionOf(Component client) => fLinks.TryGetValue(client, out TBoldActionLink? link) ? link.Action : null;

    /// <summary>The client no longer follows its action.</summary>
    public static void Unlink(Component client)
    {
        if (fLinks.TryGetValue(client, out TBoldActionLink? link))
        {
            link.Dispose();
        }
    }

    /// <summary>The control or tool strip item.</summary>
    public Component Client => fClient;

    /// <summary>The action; null once the link is gone.</summary>
    public TBasicAction? Action => fAction;

    /// <summary>The client's text.</summary>
    public string SimpleText
    {
        get => fClient switch
        {
            Control control => control.Text,
            ToolStripItem item => item.Text ?? "",
            _ => "",
        };
        set
        {
            switch (fClient)
            {
                case Control control:
                    control.Text = value;
                    break;
                case ToolStripItem item:
                    item.Text = value;
                    break;
            }
        }
    }

    /// <summary>Brings the action up to date for the client (as the idle loop does).</summary>
    public void Update() => fAction?.Update(this);

    /// <summary>Executes the action for the client (as a click does); true when something ran.</summary>
    public bool Execute() => fAction?.Execute(this) ?? false;

    /// <summary>The client shows the action's caption, enabled, visible and checked states, and hint.</summary>
    private void ShowAction()
    {
        if (fAction is not TCustomAction action)
        {
            return;
        }

        SimpleText = action.Caption;
        switch (fClient)
        {
            case Control control:
                control.Enabled = action.Enabled;
                control.Visible = action.Visible;
                switch (control)
                {
                    case CheckBox checkBox:
                        checkBox.Checked = action.Checked;
                        break;
                    case RadioButton radioButton:
                        radioButton.Checked = action.Checked;
                        break;
                }

                break;
            case ToolStripItem item:
                item.Enabled = action.Enabled;
                item.Visible = action.Visible;
                item.ToolTipText = action.Hint;
                switch (item)
                {
                    case ToolStripMenuItem menuItem:
                        menuItem.Checked = action.Checked;
                        break;
                    case ToolStripButton button:
                        button.Checked = action.Checked;
                        break;
                }

                break;
        }
    }

    private void ActionChanged(object? sender, EventArgs e) => ShowAction();

    private void ClientClick(object? sender, EventArgs e) => Execute();

    private void ClientDisposed(object? sender, EventArgs e) => Dispose();

    private void ActionDisposed(object? sender, EventArgs e) => Dispose();

    /// <summary>
    /// Updates the action; an exception goes to the application's handler, once (not on every idle turn) until an
    /// update succeeds again.
    /// </summary>
    private void ApplicationIdle(object? sender, EventArgs e)
    {
        try
        {
            Update();
            fUpdateFailed = false;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!fUpdateFailed)
            {
                fUpdateFailed = true;
                Application.OnThreadException(exception);
            }
        }
    }

    private bool fUpdateFailed;

    /// <summary>The client and the action go their own ways.</summary>
    public void Dispose()
    {
        Application.Idle -= ApplicationIdle;
        switch (fClient)
        {
            case Control control:
                control.Click -= ClientClick;
                break;
            case ToolStripItem item:
                item.Click -= ClientClick;
                break;
        }

        fClient.Disposed -= ClientDisposed;
        if (fAction is not null)
        {
            fAction.Changed -= ActionChanged;
            fAction.Disposed -= ActionDisposed;
            fAction = null;
        }

        if (fLinks.TryGetValue(fClient, out TBoldActionLink? current) && current == this)
        {
            fLinks.Remove(fClient);
        }
    }
}
