// Delphi: Source/BoldAwareGUI/BoldControls/BoldCaptionController.pas
// Port map: CSharp/port/units/BoldCaptionController.md

using System.ComponentModel;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldCaptionController", Symbol = "TWinControlWithCaption", Note = "Reached a control's protected caption; a WinForms control's Text is public.")]

namespace Bold;

/// <summary>
/// A component that puts an element's string (the value of its handle, through its string controller) in another
/// control's text: a form's caption, a group box's title.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms component; the tracked control is any control (its Text).")]
public class TBoldCustomCaptionController : Component, IBoldOCLComponent
{
    private TBoldStringFollowerController? fBoldProperties;
    private string fCaption = "";
    private TBoldElementHandleFollower? fHandleFollower;
    private Control? fTrackControl;

    [Delphi(PortStatus.Ported)]
    public TBoldCustomCaptionController()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fBoldProperties.AfterMakeUptoDate = AfterMakeUptoDate;
        fBoldProperties.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        TrackControl = null;
    }

    /// <summary>A controller among the container's components (a form's).</summary>
    public TBoldCustomCaptionController(IContainer container)
        : this()
    {
        container.Add(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomCaptionController.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
            fTrackControl = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private string GetTrackedCaption() => fTrackControl?.Text ?? "";

    [Delphi(PortStatus.Ported)]
    private void SetTrackedCaption(string s)
    {
        if (fTrackControl is not null)
        {
            fTrackControl.Text = s;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetCaption(string s)
    {
        if (s != fCaption || s != TrackedCaption)
        {
            fCaption = s;
            TrackedCaption = Caption; //TrackedCaption ensures valid fTrackControl
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetTrackControl(Control? Control)
    {
        if (Control != fTrackControl)
        {
            fTrackControl = Control;
            Caption = Caption; //Update caption if new Control;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value) => fHandleFollower!.BoldHandle = value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldDisplay(TBoldStringFollowerController value) => fBoldProperties!.Assign(value);

    /// <summary>The caption is the follower's string (in a designer: the renderer and expression).</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        string newText;
        if (DesignMode)
        {
            if (BoldProperties.Renderer is { } renderer)
            {
                newText = SysUtils.Format("%s.%s", renderer.Site?.Name ?? renderer.GetType().Name, BoldProperties.Expression);
            }
            else if (BoldProperties.Expression != "")
            {
                newText = BoldProperties.Expression;
            }
            else
            {
                newText = Site?.Name ?? "";
            }
        }
        else
        {
            newText = BoldProperties.GetCurrentAsString(Follower);
        }

        Caption = newText;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

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
    private string TrackedCaption
    {
        get => GetTrackedCaption();
        set => SetTrackedCaption(value);
    }

    /// <summary>The caption shown in the tracked control.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption
    {
        get => fCaption;
        set => SetCaption(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldDisplay(value);
    }

    /// <summary>The control whose text is the caption.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public Control? TrackControl
    {
        get => fTrackControl;
        set => SetTrackControl(value);
    }
}

/// <summary>A Bold caption controller, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCaptionController : TBoldCustomCaptionController
{
    public TBoldCaptionController()
    {
    }

    /// <summary>A controller among the container's components (a form's).</summary>
    public TBoldCaptionController(IContainer container)
        : base(container)
    {
    }
}
