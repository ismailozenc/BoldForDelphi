namespace Bold;

/// <summary>
/// Delphi's actions (TBasicAction, TCustomAction, TAction): a command with a caption and enabled, checked and
/// visible states that menu items and buttons follow. Execute runs OnExecute, or ExecuteTarget when the action handles
/// its target; Update runs OnUpdate, or UpdateTarget. A GUI layer links actions to its controls and calls Update when
/// idle.
/// </summary>
public class TBasicAction : System.ComponentModel.Component
{
    private TNotifyEvent? fOnExecute;
    private TNotifyEvent? fOnUpdate;

    public TBasicAction(System.ComponentModel.IContainer? AOwner = null)
    {
        AOwner?.Add(this);
    }

    /// <summary>The action's name: the designer's name for it, or the name given.</summary>
    public string Name
    {
        get => fName ?? Site?.Name ?? "";
        set => fName = value;
    }

    private string? fName;

    /// <summary>Raised when a property that controls show changes.</summary>
    public event EventHandler? Changed;

    public TNotifyEvent? OnExecute
    {
        get => fOnExecute;
        set
        {
            fOnExecute = value;
            Change();
        }
    }

    public TNotifyEvent? OnUpdate
    {
        get => fOnUpdate;
        set => fOnUpdate = value;
    }

    /// <summary>Tells linked controls to show the action again.</summary>
    protected void Change() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>True when the action works on the target (a control, or null for none).</summary>
    public virtual bool HandlesTarget(object? Target) => false;

    /// <summary>Brings the action's state up to date for the target.</summary>
    public virtual void UpdateTarget(object? Target)
    {
    }

    /// <summary>Does the action on the target.</summary>
    public virtual void ExecuteTarget(object? Target)
    {
    }

    /// <summary>Runs OnExecute, or ExecuteTarget(Target) when the action handles the target; true when something ran.</summary>
    public virtual bool Execute(object? Target = null)
    {
        if (fOnExecute is not null)
        {
            fOnExecute(this);
            return true;
        }

        if (HandlesTarget(Target))
        {
            ExecuteTarget(Target);
            return true;
        }

        return false;
    }

    /// <summary>Runs OnUpdate, or UpdateTarget(Target) when the action handles the target; true when something ran.</summary>
    public virtual bool Update(object? Target = null)
    {
        if (fOnUpdate is not null)
        {
            fOnUpdate(this);
            return true;
        }

        if (HandlesTarget(Target))
        {
            UpdateTarget(Target);
            return true;
        }

        return false;
    }
}

/// <summary>An action with the properties linked controls show.</summary>
public class TCustomAction : TBasicAction
{
    private string fCaption = "";
    private bool fEnabled = true;
    private bool fChecked;
    private bool fVisible = true;
    private string fHint = "";
    private int fImageIndex = -1;

    public TCustomAction(System.ComponentModel.IContainer? AOwner = null)
        : base(AOwner)
    {
    }

    public string Caption
    {
        get => fCaption;
        set => Set(ref fCaption, value);
    }

    public bool Enabled
    {
        get => fEnabled;
        set => Set(ref fEnabled, value);
    }

    public bool Checked
    {
        get => fChecked;
        set => Set(ref fChecked, value);
    }

    public bool Visible
    {
        get => fVisible;
        set => Set(ref fVisible, value);
    }

    public string Hint
    {
        get => fHint;
        set => Set(ref fHint, value);
    }

    public int ImageIndex
    {
        get => fImageIndex;
        set => Set(ref fImageIndex, value);
    }

    /// <summary>Updates the action first (as Delphi's TCustomAction.Execute does); a disabled action does not execute.</summary>
    public override bool Execute(object? Target = null)
    {
        Update(Target);
        return Enabled && base.Execute(Target);
    }

    private void Set<T>(ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            Change();
        }
    }
}

/// <summary>Delphi's TAction.</summary>
public class TAction : TCustomAction
{
    public TAction(System.ComponentModel.IContainer? AOwner = null)
        : base(AOwner)
    {
    }
}
