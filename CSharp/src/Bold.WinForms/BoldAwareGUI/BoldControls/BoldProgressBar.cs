// Delphi: Source/BoldAwareGUI/BoldControls/BoldProgressBar.pas
// Port map: CSharp/port/units/BoldProgressBar.md

using System.ComponentModel;
using System.Windows.Forms;

namespace Bold;

/// <summary>A progress bar that shows an integer element (the value of its handle); setting its position sets the element.</summary>
[Delphi(PortStatus.Ported, Note = "A WinForms ProgressBar; Position is its Value.")]
public class TBoldProgressBar : ProgressBar, IBoldOCLComponent
{
    /// <summary>Delphi raised its resource string sValueReadOnly, which is not in the sources.</summary>
    private const string sValueReadOnly = "The value is read only";

    private bool FEffectiveReadOnly;
    private bool FReadOnly;
    private TBoldIntegerFollowerController? FBoldProperties;
    private TBoldElementHandleFollower? fHandleFollower;

    [Delphi(PortStatus.Ported)]
    public TBoldProgressBar()
    {
        FBoldProperties = new TBoldIntegerFollowerController(this);
        FBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        FBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, FBoldProperties);
        AllowDrop = true;
        _ = new TBoldDragSupport(this, () => BoldProperties.DragMode != TBoldDragMode.bdgNone, DoStartDrag, DoEndDrag);
        TBoldPopupSupport.Attach(this, GetPopupMenu);
    }

    /// <summary>Applies or drops a pending change (as the apply policy says), then lets go of the follower.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldProgressBar.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fHandleFollower is not null)
        {
            switch (BoldProperties.ApplyPolicy)
            {
                case TBoldApplyPolicy.bapChange:
                case TBoldApplyPolicy.bapExit:
                    Follower.Apply();
                    break;
                case TBoldApplyPolicy.bapDemand:
                    Follower.DiscardChange();
                    break;
            }

            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref FBoldProperties);
        }

        base.Dispose(disposing);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (fHandleFollower is not null)
        {
            object match = (object?)FindForm() ?? this;
            fHandleFollower.MatchObject = match;
            fHandleFollower.Follower.MatchObject = match;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoStartDrag() => BoldProperties.StartDrag(Follower);

    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag() => BoldProperties.EndDrag();

    [Delphi(PortStatus.Ported, Symbol = "TBoldProgressBar.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            TBoldDragSupport.Accept(drgevent, BoldProperties.DropMode != TBoldDropMode.bdpNone && BoldProperties.DragOver(Follower, Follower.Element, 0));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldProgressBar.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            BoldProperties.DragDrop(Follower, Follower.Element, 0);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldDisplay(TBoldIntegerFollowerController Value) => FBoldProperties!.Assign(Value);

    /// <summary>The popup menu: the progress bar's own, else the controller's Bold popup for the handle's value.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the menu for a right click.")]
    protected virtual ContextMenuStrip? GetPopupMenu()
    {
        ContextMenuStrip? Result = ContextMenuStrip;
        if (Result is null && BoldHandle is not null)
        {
            Result = BoldProperties.Popup.GetMenu(this, BoldHandle.Value) as ContextMenuStrip;
        }

        return Result;
    }

    /// <summary>Shows the element's integer; the bar is read-only when asked to be or when the element cannot be changed.</summary>
    [Delphi(PortStatus.Ported, Note = "The position is kept between Minimum and Maximum (a WinForms progress bar raises outside them).")]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        int NewPosition = BoldProperties.GetCurrentAsInteger(Follower);
        if (Position != NewPosition)
        {
            base.Value = Math.Clamp(NewPosition, Minimum, Maximum);
        }

        FEffectiveReadOnly = FReadOnly || !BoldProperties.MayModify(Follower);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetContextType();

    [Delphi(PortStatus.Ported)]
    private string GetExpression() => BoldProperties.Expression;

    string IBoldOCLComponent.GetExpression() => GetExpression();

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Expression) => BoldProperties.Expression = Expression;

    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);

    [Delphi(PortStatus.Ported)]
    private int GetPosition() => base.Value;

    /// <summary>Sets the position and (when the bar may change it) the element; raises when read-only.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetPosition(int Value)
    {
        if (!EffectiveReadOnly)
        {
            BoldProperties.MayHaveChanged(Value, Follower);
            base.Value = Math.Clamp(Value, Minimum, Maximum);
        }
        else
        {
            throw new EBold(sValueReadOnly);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetReadOnly(bool Value)
    {
        if (FReadOnly != Value)
        {
            FReadOnly = Value;
            Follower.MarkValueOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariableList? GetVariableList() => BoldProperties.VariableList;

    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EffectiveReadOnly => FEffectiveReadOnly;

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldIntegerFollowerController BoldProperties
    {
        get => FBoldProperties!;
        set => SetBoldDisplay(value);
    }

    /// <summary>The position (the element's value); setting it sets the element.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Position
    {
        get => GetPosition();
        set => SetPosition(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => FReadOnly;
        set => SetReadOnly(value);
    }
}
