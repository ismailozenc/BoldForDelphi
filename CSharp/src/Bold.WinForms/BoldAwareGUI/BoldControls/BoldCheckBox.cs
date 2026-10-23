// Delphi: Source/BoldAwareGUI/BoldControls/BoldCheckBox.pas
// Port map: CSharp/port/units/BoldCheckBox.md

using System.ComponentModel;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

namespace Bold;

/// <summary>
/// A check box that shows a boolean element (grayed for null) and sets it when clicked, unless the element cannot be
/// changed.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms CheckBox; it toggles itself (AutoCheck is off) only when it may change the element.")]
public class TBoldCustomCheckBox : CheckBox, IBoldOCLComponent
{
    private TBoldCheckBoxStateFollowerController? fBoldProperties;
    private bool fEffectiveReadOnly;
    private TBoldElementHandleFollower? fHandleFollower;
    private bool fMyReadOnly;
    private bool fSettingState;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomCheckBox()
    {
        AutoCheck = false;
        fBoldProperties = new TBoldCheckBoxStateFollowerController(this);
        fBoldProperties.OnGetContextType = GetContextType;
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
    }

    /// <summary>Applies or drops a pending change (as the apply policy says), then lets go of the follower.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomCheckBox.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fHandleFollower is not null)
        {
            switch (BoldProperties.ApplyPolicy)
            {
                case TBoldApplyPolicy.bapChange:
                case TBoldApplyPolicy.bapExit:
                    try
                    {
                        Follower.Apply();
                    }
                    catch (Exception)
                    {
                        Follower.DiscardChange();
                    }

                    break;
                case TBoldApplyPolicy.bapDemand:
                    Follower.DiscardChange();
                    break;
            }

            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
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
    protected virtual bool GetChecked() => State == TCheckBoxState.cbChecked;

    [Delphi(PortStatus.Ported)]
    private void SetReadOnly(bool value)
    {
        if (fMyReadOnly != value)
        {
            fMyReadOnly = value;
            Follower.MarkValueOutOfDate();
        }
    }

    private static TCheckBoxState FromCheckState(CheckState state) => state switch
    {
        CheckState.Checked => TCheckBoxState.cbChecked,
        CheckState.Indeterminate => TCheckBoxState.cbGrayed,
        _ => TCheckBoxState.cbUnchecked,
    };

    private static CheckState ToCheckState(TCheckBoxState state) => state switch
    {
        TCheckBoxState.cbChecked => CheckState.Checked,
        TCheckBoxState.cbGrayed => CheckState.Indeterminate,
        _ => CheckState.Unchecked,
    };

    [Delphi(PortStatus.Ported)]
    private TCheckBoxState GetState() => FromCheckState(CheckState);

    /// <summary>Sets the state; raises when the check box may not change the element.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetState(TCheckBoxState v)
    {
        if (v != State)
        {
            if (!EffectiveReadOnly)
            {
                CheckState = ToCheckState(v);
            }
            else
            {
                throw new EBold(SysUtils.Format(sStateNotModifiable, GetType().Name));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldDisplay(TBoldCheckBoxStateFollowerController value) => fBoldProperties!.Assign(value);

    /// <summary>Shows the element's state; the check box is read-only when asked to be or when the element cannot be changed.</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        fSettingState = true;
        try
        {
            CheckState = ToCheckState(BoldProperties.GetCurrentAsCheckBoxState(Follower));
        }
        finally
        {
            fSettingState = false;
        }

        fEffectiveReadOnly = fMyReadOnly || !BoldProperties.MayModify(Follower);
    }

    /// <summary>A new state may change the element: the follower is told (and drops the change when that fails).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomCheckBox.CMChanged")]
    protected override void OnCheckStateChanged(EventArgs e)
    {
        try
        {
            if (!DesignMode && !fSettingState && fHandleFollower is not null)
            {
                BoldProperties.MayHaveChanged(State, Follower);
            }

            base.OnCheckStateChanged(e);
        }
        catch (Exception)
        {
            Follower.DiscardChange();
            throw;
        }
    }

    /// <summary>Leaving the check box applies the change under the exit policy.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomCheckBox.CMExit")]
    protected override void OnLeave(EventArgs e)
    {
        if (Follower.Controller?.ApplyPolicy == TBoldApplyPolicy.bapExit)
        {
            Follower.Apply();
        }

        base.OnLeave(e);
    }

    /// <summary>A click toggles the check box (unchecked, grayed when allowed, checked) unless it is read-only.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomCheckBox.CNCommand", Note = "OnClick with AutoCheck off, where Delphi took the button's click notification.")]
    protected override void OnClick(EventArgs e)
    {
        if (!EffectiveReadOnly)
        {
            CheckState = CheckState switch
            {
                CheckState.Unchecked => ThreeState ? CheckState.Indeterminate : CheckState.Checked,
                CheckState.Indeterminate => CheckState.Checked,
                _ => CheckState.Unchecked,
            };
        }

        base.OnClick(e);
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
    private TBoldExternalVariableList? GetVariableList() => BoldProperties.VariableList;

    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower Follower => GetFollower();

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldCheckBoxStateFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldDisplay(value);
    }

    /// <summary>True when the state is checked (the element sets it).</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool Checked => GetChecked();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EffectiveReadOnly => fEffectiveReadOnly;

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => fMyReadOnly;
        set => SetReadOnly(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TCheckBoxState State
    {
        get => GetState();
        set => SetState(value);
    }
}

/// <summary>A Bold check box, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCheckBox : TBoldCustomCheckBox
{
}
