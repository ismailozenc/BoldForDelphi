// Delphi: Source/BoldAwareGUI/BoldControls/BoldPageControl.pas
// Port map: CSharp/port/units/BoldPageControl.md

using System.ComponentModel;
using System.Windows.Forms;

namespace Bold;

/// <summary>A tab control whose shown page is the one named as the element's string (the value of its handle).</summary>
[Delphi(PortStatus.Ported, Note = "A WinForms TabControl; a page's name is its TabPage's Name.")]
public class TBoldPageControl : TabControl
{
    private TBoldElementHandleFollower? fHandleFollower;
    private TBoldStringFollowerController? fBoldProperties;

    [Delphi(PortStatus.Ported)]
    public TBoldPageControl()
    {
        fBoldProperties = new TBoldStringFollowerController(this);
        fHandleFollower = new TBoldElementHandleFollower(this, fBoldProperties);
        fBoldProperties.AfterMakeUptoDate = _Display;
        fBoldProperties.OnGetContextType = _GetContextType;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPageControl.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldStringFollowerController Value) => fBoldProperties!.Assign(Value);

    /// <summary>Shows the page named as the element's string (no page changes when none has that name).</summary>
    [Delphi(PortStatus.Ported)]
    private void _Display(TBoldFollower Follower)
    {
        string NewString = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
        for (int i = 0; i < TabPages.Count; i++)
        {
            if (TabPages[i].Name == NewString)
            {
                SelectedTab = TabPages[i];
                return;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? _GetContextType() => BoldHandle?.StaticBoldType;

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
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldProperties(value);
    }
}
