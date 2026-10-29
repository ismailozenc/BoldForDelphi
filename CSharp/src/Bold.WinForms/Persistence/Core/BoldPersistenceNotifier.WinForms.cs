// Delphi: Source/Persistence/Core/BoldPersistenceNotifier.pas
// Port map: CSharp/port/units/BoldPersistenceNotifier.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.PersistenceConsts;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPersistenceNotifier", Symbol = "TExposedWinControl", Note = "A WinForms control's Text is public.")]

namespace Bold;

/// <summary>
/// Shows the progress of the persistence layer: a progress bar steps per object fetched, created, updated or deleted,
/// an animation plays while the layer works, and a label or control shows what it is doing.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The persistence notifier's WinForms part, in Bold.WinForms. A picture box with an animated image (a GIF) stands in for Delphi's TAnimate: it plays while the picture box is enabled.")]
public class TBoldPersistenceProgressNotifier : TBoldAbstractPersistenceNotifier
{
    private PictureBox? FAnimation;
    private readonly System.Windows.Forms.Timer fAnimationTimer;
    private Control? FWinControl;
    private Label? fMsgLabel;
    private ProgressBar? FProgressBar;
    private string fMsgFetchObjects;
    private string fMsgRetrieveIds;
    private string fMsgUpdateDatabase;

    /// <summary>A notifier with the default messages; the animation stops 100 ms after the layer is done.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldPersistenceProgressNotifier(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
        fAnimationTimer = new System.Windows.Forms.Timer { Interval = 100 };
        fAnimationTimer.Tick += AnimationTurnOff;
        fMsgFetchObjects = sFetchingObjects;
        fMsgRetrieveIds = sRetrievingIDs;
        fMsgUpdateDatabase = sUpdatingDB;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Animation = null;
            WinControl = null;
            MsgLabel = null;
            ProgressBar = null;
            fAnimationTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The animation stops (the timer ran out after the layer was done).</summary>
    [Delphi(PortStatus.Ported)]
    private void AnimationTurnOff(object? Sender, EventArgs e)
    {
        if (Animation is not null)
        {
            Stop(Animation);
        }

        fAnimationTimer.Enabled = false;
    }

    /// <summary>The control is forgotten when it goes.</summary>
    [Delphi(PortStatus.Ported, Note = "The controls' Disposed events stand in for Delphi's free notifications.")]
    private void Notification(object? AComponent, EventArgs e)
    {
        if (AComponent == ProgressBar)
        {
            ProgressBar = null;
        }

        if (AComponent == Animation)
        {
            Animation = null;
        }

        if (AComponent == WinControl)
        {
            WinControl = null;
        }

        if (AComponent == MsgLabel)
        {
            MsgLabel = null;
        }
    }

    /// <summary>Watches the new control (and no longer the old one) to forget it when it goes.</summary>
    private void Watch(Component? oldValue, Component? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Disposed -= Notification;
        }

        if (newValue is not null)
        {
            newValue.Disposed += Notification;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetAnimation(PictureBox? Value)
    {
        Watch(FAnimation, Value);
        FAnimation = Value;
    }

    [Delphi(PortStatus.Ported)]
    private int GetAnimationInterval() => fAnimationTimer.Interval;

    [Delphi(PortStatus.Ported)]
    private void SetAnimationInterval(int Value) => fAnimationTimer.Interval = Value;

    /// <summary>Shows the message in the control and the label.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetMessage(string s)
    {
        if (WinControl is not null)
        {
            WinControl.Text = s;
            WinControl.Refresh();
        }

        if (MsgLabel is not null)
        {
            MsgLabel.Text = s;
            MsgLabel.Refresh();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMsgLabel(Label? Value)
    {
        Watch(fMsgLabel, Value);
        fMsgLabel = Value;
    }

    /// <summary>The progress bar steps by one.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetProgressBar(ProgressBar? Value)
    {
        Watch(FProgressBar, Value);
        FProgressBar = Value;
        if (FProgressBar is not null)
        {
            FProgressBar.Step = 1;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetWinControl(Control? Value)
    {
        Watch(FWinControl, Value);
        FWinControl = Value;
    }

    /// <summary>Whether the animation has frames to play (an animated image).</summary>
    private static bool HasFrames(PictureBox animation) => animation.Image is Image image && ImageAnimator.CanAnimate(image);

    /// <summary>Plays the animation (a picture box animates its image while it is enabled).</summary>
    private static void Play(PictureBox animation) => animation.Enabled = true;

    private static void Stop(PictureBox animation) => animation.Enabled = false;

    /// <summary>The layer is done: the progress bar hides, the animation stops shortly, and the message clears.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EndEvent()
    {
        if (ProgressBar is not null)
        {
            ProgressBar.Visible = false;
            ProgressBar.Maximum = 0;
            ProgressBar.Refresh();
        }

        if (Animation is not null)
        {
            fAnimationTimer.Enabled = true;
            Animation.Refresh();
        }

        SetMessage("");
    }

    /// <summary>The layer starts on Count objects: the progress bar shows, from zero, and the animation plays.</summary>
    [Delphi(PortStatus.Ported)]
    protected void StartEvent(int Count)
    {
        if (ProgressBar is not null)
        {
            ProgressBar.Visible = true;
            ProgressBar.Value = 0;
            ProgressBar.Maximum = Count;
            ProgressBar.Refresh();
        }

        if (Animation is not null)
        {
            fAnimationTimer.Enabled = false;
            if (!Animation.Enabled && HasFrames(Animation))
            {
                Play(Animation);
            }

            Animation.Refresh();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void StepProgress()
    {
        if (ProgressBar is not null)
        {
            ProgressBar.PerformStep();
            ProgressBar.Refresh();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void StepAnimation()
    {
        if (Animation is not null)
        {
            fAnimationTimer.Enabled = false;
            if (HasFrames(Animation))
            {
                Play(Animation);
            }

            Animation.Refresh();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void EndFetch(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.EndFetch(Originator, OriginalEvent, RequestedEvent, Args);
        EndEvent();
    }

    [Delphi(PortStatus.Ported)]
    protected override void EndUpdate(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.EndUpdate(Originator, OriginalEvent, RequestedEvent, Args);
        EndEvent();
    }

    [Delphi(PortStatus.Ported)]
    protected override void EndFetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.EndFetchID(Originator, OriginalEvent, RequestedEvent, Args);
        EndEvent();
    }

    /// <summary>An update of the objects in the list (the first argument) starts.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void StartUpdate(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.StartUpdate(Originator, OriginalEvent, RequestedEvent, Args);
        StartEvent(((TBoldObjectIdList)Args[0]!).Count);
        SetMessage(MsgUpdateDatabase);
    }

    /// <summary>A fetch of the objects in the list (the first argument) starts.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void StartFetch(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.StartFetch(Originator, OriginalEvent, RequestedEvent, Args);
        StartEvent(((TBoldObjectIdList)Args[0]!).Count);
        SetMessage(MsgFetchObjects);
    }

    [Delphi(PortStatus.Ported)]
    protected override void UpdateObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.UpdateObject(Originator, OriginalEvent, RequestedEvent, Args);
        StepProgress();
    }

    [Delphi(PortStatus.Ported)]
    protected override void DeleteObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.DeleteObject(Originator, OriginalEvent, RequestedEvent, Args);
        StepProgress();
    }

    [Delphi(PortStatus.Ported)]
    protected override void CreateObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.CreateObject(Originator, OriginalEvent, RequestedEvent, Args);
        StepProgress();
    }

    [Delphi(PortStatus.Ported)]
    protected override void FetchObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.FetchObject(Originator, OriginalEvent, RequestedEvent, Args);
        StepProgress();
    }

    [Delphi(PortStatus.Ported)]
    protected override void FetchMember(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.FetchMember(Originator, OriginalEvent, RequestedEvent, Args);
        StepAnimation();
    }

    [Delphi(PortStatus.Ported)]
    protected override void StartFetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        base.StartFetchID(Originator, OriginalEvent, RequestedEvent, Args);
        StepAnimation();
        SetMessage(MsgRetrieveIds);
    }

    [Delphi(PortStatus.Ported)]
    protected override void FetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        base.FetchID(Originator, OriginalEvent, RequestedEvent, Args);

    /// <summary>The animation: a picture box with an animated image, playing while it is enabled.</summary>
    [Delphi(PortStatus.Ported, Note = "A PictureBox where Delphi has a TAnimate.")]
    public PictureBox? Animation
    {
        get => FAnimation;
        set => SetAnimation(value);
    }

    /// <summary>A control whose text shows the message.</summary>
    [Delphi(PortStatus.Ported)]
    public Control? WinControl
    {
        get => FWinControl;
        set => SetWinControl(value);
    }

    /// <summary>A label that shows the message.</summary>
    [Delphi(PortStatus.Ported)]
    public Label? MsgLabel
    {
        get => fMsgLabel;
        set => SetMsgLabel(value);
    }

    [Delphi(PortStatus.Ported)]
    public ProgressBar? ProgressBar
    {
        get => FProgressBar;
        set => SetProgressBar(value);
    }

    /// <summary>How long the animation plays on after the layer is done (milliseconds).</summary>
    [Delphi(PortStatus.Ported)]
    public int AnimationInterval
    {
        get => GetAnimationInterval();
        set => SetAnimationInterval(value);
    }

    [Delphi(PortStatus.Ported)]
    public string MsgFetchObjects
    {
        get => fMsgFetchObjects;
        set => fMsgFetchObjects = value;
    }

    [Delphi(PortStatus.Ported)]
    public string MsgRetrieveIds
    {
        get => fMsgRetrieveIds;
        set => fMsgRetrieveIds = value;
    }

    [Delphi(PortStatus.Ported)]
    public string MsgUpdateDatabase
    {
        get => fMsgUpdateDatabase;
        set => fMsgUpdateDatabase = value;
    }
}
