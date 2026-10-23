// Delphi: Source/Common/Support/BoldNavigatorDefs.pas
// Port map: CSharp/port/units/BoldNavigatorDefs.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>A panel of navigator buttons, whose glyphs follow its image indexes.</summary>
[Delphi(PortStatus.Ported, Note = "A WinForms Panel; Delphi's is a TCustomPanel.")]
public abstract class TBoldNavigateBtnImageIndexOwner : Panel
{
    private TBoldNavigateBtn fFocusedButton;

    /// <summary>Gives the buttons the glyphs of their image indexes.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal abstract void FixButtonGlyphs();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldNavigateBtn FocusedButton
    {
        get => fFocusedButton;
        protected set => fFocusedButton = value;
    }
}

/// <summary>The image index of each navigator button in the navigator's image list; changing one refreshes the glyphs.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNavigateBtnImageIndex : TPersistent
{
    private int FnbNext;
    private int FnbInsert;
    private int FnbFirst;
    private int FnbPrior;
    private int FnbDelete;
    private int FnbLast;
    private int fnbMoveUp;
    private int fnbMoveDown;
    private readonly TBoldNavigateBtnImageIndexOwner fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldNavigateBtnImageIndex(TBoldNavigateBtnImageIndexOwner Owner)
    {
        fOwner = Owner;
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldNavigateBtnImageIndex source)
        {
            FnbNext = source.nbNext;
            FnbInsert = source.nbInsert;
            FnbFirst = source.nbFirst;
            FnbPrior = source.nbPrior;
            FnbDelete = source.nbDelete;
            FnbLast = source.nbLast;
            fnbMoveUp = source.nbMoveUp;
            fnbMoveDown = source.nbMoveDown;
        }
        else
        {
            base.Assign(Source);
        }
    }

    private void Set(ref int field, int value)
    {
        field = value;
        fOwner.FixButtonGlyphs();
    }

    [Delphi(PortStatus.Ported)] private void SetnbDelete(int Value) => Set(ref FnbDelete, Value);
    [Delphi(PortStatus.Ported)] private void SetnbFirst(int Value) => Set(ref FnbFirst, Value);
    [Delphi(PortStatus.Ported)] private void SetnbInsert(int Value) => Set(ref FnbInsert, Value);
    [Delphi(PortStatus.Ported)] private void SetnbLast(int Value) => Set(ref FnbLast, Value);
    [Delphi(PortStatus.Ported)] private void SetnbNext(int Value) => Set(ref FnbNext, Value);
    [Delphi(PortStatus.Ported)] private void SetnbPrior(int Value) => Set(ref FnbPrior, Value);
    [Delphi(PortStatus.Ported)] private void SetnbMoveUp(int Value) => Set(ref fnbMoveUp, Value);
    [Delphi(PortStatus.Ported)] private void SetnbMoveDown(int Value) => Set(ref fnbMoveDown, Value);

    [Delphi(PortStatus.Ported)] public int nbFirst { get => FnbFirst; set => SetnbFirst(value); }
    [Delphi(PortStatus.Ported)] public int nbPrior { get => FnbPrior; set => SetnbPrior(value); }
    [Delphi(PortStatus.Ported)] public int nbNext { get => FnbNext; set => SetnbNext(value); }
    [Delphi(PortStatus.Ported)] public int nbLast { get => FnbLast; set => SetnbLast(value); }
    [Delphi(PortStatus.Ported)] public int nbInsert { get => FnbInsert; set => SetnbInsert(value); }
    [Delphi(PortStatus.Ported)] public int nbDelete { get => FnbDelete; set => SetnbDelete(value); }
    [Delphi(PortStatus.Ported)] public int nbMoveUp { get => fnbMoveUp; set => SetnbMoveUp(value); }
    [Delphi(PortStatus.Ported)] public int nbMoveDown { get => fnbMoveDown; set => SetnbMoveDown(value); }
}

/// <summary>A button of a navigator: it repeats its click while held down (when allowed) and shows the navigator's focus.</summary>
[Delphi(PortStatus.Ported, Note = "A WinForms Button; Delphi's is a TSpeedButton.")]
public class TBoldNavButton : Button
{
    private const int InitRepeatPause = 400; // pause before repeat timer (ms)
    private const int RepeatPause = 100;

    private TBoldNavigateBtn FIndex;
    private TBoldNavButtonStyle FNavStyle = TBoldNavButtonStyle.Of();
    private System.Windows.Forms.Timer? FRepeatTimer;
    private bool fDown;

    public TBoldNavButton()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNavButton.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FRepeatTimer?.Dispose();
            FRepeatTimer = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>Starts the repeat timer when the button repeats.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldNavButton.MouseDown")]
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        fDown = mevent.Button == MouseButtons.Left;
        if (fDown && FNavStyle.Contains(TBoldNavButtonStyleElement.nsAllowTimer))
        {
            if (FRepeatTimer is null)
            {
                FRepeatTimer = new System.Windows.Forms.Timer();
                FRepeatTimer.Tick += TimerExpired;
            }

            FRepeatTimer.Interval = InitRepeatPause;
            FRepeatTimer.Enabled = true;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNavButton.MouseUp")]
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        fDown = false;
        if (FRepeatTimer is not null)
        {
            FRepeatTimer.Enabled = false;
        }
    }

    /// <summary>Clicks again while the button is held down over it.</summary>
    [Delphi(PortStatus.Ported)]
    private void TimerExpired(object? Sender, EventArgs e)
    {
        FRepeatTimer!.Interval = RepeatPause;
        if (fDown && Capture && ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            try
            {
                OnClick(EventArgs.Empty);
            }
            catch (Exception)
            {
                FRepeatTimer.Enabled = false;
                throw;
            }
        }
    }

    /// <summary>Draws the focus rectangle on the navigator's focused button while the navigator has the focus.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldNavButton.Paint")]
    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        if (Parent is TBoldNavigateBtnImageIndexOwner owner && owner.Focused && FIndex == owner.FocusedButton &&
            FNavStyle.Contains(TBoldNavButtonStyleElement.nsFocusRect))
        {
            Rectangle R = ClientRectangle;
            R.Inflate(-3, -3);
            if (fDown)
            {
                R.Offset(1, 1);
            }

            ControlPaint.DrawFocusRectangle(pevent.Graphics, R);
        }
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldNavigateBtn Index
    {
        get => FIndex;
        set => FIndex = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldNavButtonStyle NavStyle
    {
        get => FNavStyle;
        set => FNavStyle = value;
    }
}
