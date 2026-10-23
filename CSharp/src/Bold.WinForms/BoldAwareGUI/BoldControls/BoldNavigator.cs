// Delphi: Source/BoldAwareGUI/BoldControls/BoldNavigator.pas
// Port map: CSharp/port/units/BoldNavigator.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldNavigator", Symbol = "TBoldCustomNavigator.GetChildren", Note = "Kept the buttons out of the form file; the WinForms designer does not serialize controls a control makes itself.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldNavigator", Symbol = "TBoldCustomNavigator.WMGetDlgCode", Note = "IsInputKey takes the arrow keys.")]

namespace Bold;

/// <summary>
/// A row of buttons that moves through a list handle (first, prior, next, last), adds and deletes elements and moves
/// them up and down, each button enabled when its action can be done.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms Panel of buttons.")]
public class TBoldCustomNavigator : TBoldNavigateBtnImageIndexOwner
{
    private int ButtonWidth;
    private TBoldDeleteMode fBoldDeleteMode;
    private TBoldListAsFollowerListController? fBoldProperties;
    private TBoldStringFollowerController? fDummyController;
    private TBoldListHandleFollower? fHandleFollower;
    private bool fConfirmDelete;
    private bool fFlat;
    private readonly TStringList fHints = new();
    private TBoldNavClick? fOnNavClick;
    private TBoldButtonSet fVisibleButtons;
    private Point MinBtnSize;
    private TBoldNavigateBtnImageIndex fImageIndices;
    private ImageList? fImages;
    private string fDeleteQuestion;
    private string fRemoveQuestion;
    private string fUnlinkQuestion;

    /// <summary>The buttons, by what they do.</summary>
    protected readonly TBoldNavButton[] Buttons = new TBoldNavButton[8];

    [Delphi(PortStatus.Ported)]
    public TBoldCustomNavigator()
    {
        fImageIndices = new TBoldNavigateBtnImageIndex(this)
        {
            nbFirst = -1, nbPrior = -1, nbNext = -1, nbLast = -1, nbInsert = -1, nbDelete = -1, nbMoveUp = -1, nbMoveDown = -1,
        };
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        fVisibleButtons = TBoldButtonSet.Of(TBoldNavigateBtn.nbFirst, TBoldNavigateBtn.nbPrior, TBoldNavigateBtn.nbNext, TBoldNavigateBtn.nbLast, TBoldNavigateBtn.nbInsert, TBoldNavigateBtn.nbDelete);
        InitButtons();
        BorderStyle = BorderStyle.None;
        ButtonWidth = 0;
        FocusedButton = TBoldNavigateBtn.nbFirst;
        fConfirmDelete = true;
        fDummyController = new TBoldStringFollowerController(this);
        fBoldProperties = new TBoldListAsFollowerListController(this, fDummyController);
        fHandleFollower = new TBoldListHandleFollower(this, fBoldProperties);
        fBoldProperties.AfterMakeUptoDate = _AfterMakeUptoDate;
        fBoldProperties.BeforeMakeUptoDate = _BeforeMakeUptoDate;
        fDeleteQuestion = sDeleteQuestion;
        fUnlinkQuestion = sUnlinkQuestion;
        fRemoveQuestion = sRemoveQuestion;
        Size = new Size(8 * 24 + 1, 25);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fDummyController);
            SysUtils.FreeAndNil(ref fBoldProperties);
            fToolTip.Dispose();
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

    private static string DefaultHint(TBoldNavigateBtn button) => button switch
    {
        TBoldNavigateBtn.nbFirst => SNavHintFirst,
        TBoldNavigateBtn.nbPrior => SNavHintPrior,
        TBoldNavigateBtn.nbNext => SNavHintNext,
        TBoldNavigateBtn.nbLast => SNavHintLast,
        TBoldNavigateBtn.nbInsert => SNavHintNew,
        TBoldNavigateBtn.nbDelete => SNavHintDelete,
        TBoldNavigateBtn.nbMoveUp => SNavHintMoveUp,
        _ => SNavHintMoveDown,
    };

    private readonly ToolTip fToolTip = new();

    /// <summary>Gives the buttons their hints: the defaults, replaced by the non-empty lines of Hints in button order.</summary>
    [Delphi(PortStatus.Ported, Note = "The hints show in a ToolTip.")]
    private void InitHints()
    {
        foreach (TBoldNavigateBtn button in Enum.GetValues<TBoldNavigateBtn>())
        {
            fToolTip.SetToolTip(Buttons[(int)button], DefaultHint(button));
        }

        for (int I = 0; I < fHints.Count && I < Buttons.Length; I++)
        {
            if (fHints.Strings(I) != "")
            {
                fToolTip.SetToolTip(Buttons[I], fHints.Strings(I));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetFlat(bool Value)
    {
        if (fFlat != Value)
        {
            fFlat = Value;
            foreach (TBoldNavButton button in Buttons)
            {
                button.FlatStyle = Value ? FlatStyle.Flat : FlatStyle.Standard;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetHints(TStrings Value)
    {
        fHints.Assign(Value);
        InitHints();
    }

    [Delphi(PortStatus.Ported)]
    private void SetVisible(TBoldButtonSet Value)
    {
        fVisibleButtons = Value;
        foreach (TBoldNavigateBtn I in Enum.GetValues<TBoldNavigateBtn>())
        {
            Buttons[(int)I].Visible = fVisibleButtons.Contains(I);
        }

        int W = Width, H = Height;
        AdjustSizeXY(ref W, ref H);
        if (W != Width || H != Height)
        {
            base.SetBoundsCore(Left, Top, W, H, BoundsSpecified.Size);
        }

        Invalidate();
    }

    /// <summary>Shares the width among the visible buttons (at least their smallest size); the others go out of sight.</summary>
    [Delphi(PortStatus.Ported)]
    private void AdjustSizeXY(ref int W, ref int H)
    {
        if (Buttons[0] is null)
        {
            return;
        }

        int Count = 0;
        foreach (TBoldNavButton button in Buttons)
        {
            if (fVisibleButtons.Contains(button.Index))
            {
                Count++;
            }
        }

        if (Count == 0)
        {
            Count++;
        }

        int MinW = Count * MinBtnSize.X;
        if (W < MinW)
        {
            W = MinW;
        }

        if (H < MinBtnSize.Y)
        {
            H = MinBtnSize.Y;
        }

        ButtonWidth = W / Count;
        int Temp = Count * ButtonWidth;
        if (Dock == DockStyle.None)
        {
            W = Temp;
        }

        int X = 0;
        int Remain = W - Temp;
        Temp = Count / 2;
        foreach (TBoldNavButton button in Buttons)
        {
            if (fVisibleButtons.Contains(button.Index))
            {
                int Space = 0;
                if (Remain != 0)
                {
                    Temp -= Remain;
                    if (Temp < 0)
                    {
                        Temp += Count;
                        Space = 1;
                    }
                }

                button.SetBounds(X, 0, ButtonWidth + Space, H);
                X += ButtonWidth + Space;
            }
            else
            {
                button.SetBounds(W + 1, 0, ButtonWidth, H);
            }
        }
    }

    /// <summary>Keeps the size a whole number of buttons wide.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.SetBounds")]
    protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
    {
        int W = width, H = height;
        AdjustSizeXY(ref W, ref H);
        base.SetBoundsCore(x, y, W, H, specified);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.WMSize")]
    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        // check for minimum size
        int W = Width, H = Height;
        AdjustSizeXY(ref W, ref H);
        if (W != Width || H != Height)
        {
            base.SetBoundsCore(Left, Top, W, H, BoundsSpecified.Size);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ClickHandler(object? Sender, EventArgs e) => BtnClick(((TBoldNavButton)Sender!).Index);

    /// <summary>A pressed button becomes the focused one (and the navigator takes the focus when it can).</summary>
    [Delphi(PortStatus.Ported)]
    private void BtnMouseDown(object? Sender, MouseEventArgs e)
    {
        TBoldNavigateBtn OldFocus = FocusedButton;
        FocusedButton = ((TBoldNavButton)Sender!).Index;
        if (TabStop && !Focused && CanFocus)
        {
            Focus();
        }
        else if (TabStop && Focused && OldFocus != FocusedButton)
        {
            Buttons[(int)OldFocus].Invalidate();
            Buttons[(int)FocusedButton].Invalidate();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.WMSetFocus")]
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Buttons[(int)FocusedButton].Invalidate();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.WMKillFocus")]
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Buttons[(int)FocusedButton].Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    /// <summary>Left and right move the focus between the visible buttons; space clicks the focused one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.KeyDown")]
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        TBoldNavigateBtn OldFocus = FocusedButton;
        TBoldNavigateBtn High = TBoldNavigateBtn.nbMoveDown, Low = TBoldNavigateBtn.nbFirst;
        switch (e.KeyCode)
        {
            case Keys.Right:
            {
                TBoldNavigateBtn NewFocus = FocusedButton;
                do
                {
                    if (NewFocus < High)
                    {
                        NewFocus++;
                    }
                }
                while (NewFocus != High && !Buttons[(int)NewFocus].Visible);

                if (NewFocus != FocusedButton)
                {
                    FocusedButton = NewFocus;
                    Buttons[(int)OldFocus].Invalidate();
                    Buttons[(int)FocusedButton].Invalidate();
                }

                break;
            }

            case Keys.Left:
            {
                TBoldNavigateBtn NewFocus = FocusedButton;
                do
                {
                    if (NewFocus > Low)
                    {
                        NewFocus--;
                    }
                }
                while (NewFocus != Low && !Buttons[(int)NewFocus].Visible);

                if (NewFocus != FocusedButton)
                {
                    FocusedButton = NewFocus;
                    Buttons[(int)OldFocus].Invalidate();
                    Buttons[(int)FocusedButton].Invalidate();
                }

                break;
            }

            case Keys.Space:
                if (Buttons[(int)FocusedButton].Enabled)
                {
                    Buttons[(int)FocusedButton].PerformClick();
                }

                break;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.CMEnabledChanged")]
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        SetActiveButtons();
    }

    /// <summary>Sizes the buttons and gives them their hints, glyphs and enabled states.</summary>
    [Delphi(PortStatus.Ported, Note = "Runs when the navigator's handle is made, after the designer set its properties.")]
    protected virtual void Loaded()
    {
        int W = Width, H = Height;
        AdjustSizeXY(ref W, ref H);
        if (W != Width || H != Height)
        {
            base.SetBoundsCore(Left, Top, W, H, BoundsSpecified.Size);
        }

        InitHints();
        FixButtonGlyphs();
        SetActiveButtons();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Loaded();
    }

    /// <summary>
    /// Does the button's action on the handle; deleting asks first (when ConfirmDelete) and removes, unlinks or deletes
    /// as the delete mode and the list say. Then OnClick.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested Delete is a local function.")]
    public void BtnClick(TBoldNavigateBtn index)
    {
        if (Visible && Enabled && CanFocus)
        {
            Focus();
        }

        TBoldAbstractListHandle handle = BoldHandle!;

        void Delete(bool AskFirst)
        {
            string RoleName = "";
            TBoldDeleteMode EffectiveDeleteMode = BoldDeleteMode;
            TBoldRoleRTInfo? RoleRTInfo = handle.ObjectList?.BoldMemberRTInfo as TBoldRoleRTInfo;
            if (RoleRTInfo is not null)
            {
                RoleName = RoleRTInfo.ExpressionName;
            }

            TBoldElement Element = handle.Value!;
            string ElementName = Element.BoldType is not null ? Element.BoldType.ExpressionName : Element.GetType().Name;
            if (BoldDeleteMode == TBoldDeleteMode.dmDefault)
            {
                // Delete from classlists, remove from other lists
                EffectiveDeleteMode = handle.ObjectList?.OwningElement is TBoldSystem ? TBoldDeleteMode.dmDelete : TBoldDeleteMode.dmRemoveFromList;
            }

            bool unlink = false;
            if (EffectiveDeleteMode == TBoldDeleteMode.dmRemoveFromList && RoleRTInfo is not null)
            {
                // linkobjects will be deleted... other objects will be unlinked
                if (RoleRTInfo.RoleType == TBoldRoleType.rtLinkRole)
                {
                    EffectiveDeleteMode = TBoldDeleteMode.dmDelete;
                }
                else
                {
                    unlink = true;
                }
            }

            string query = fDeleteQuestion;
            if (EffectiveDeleteMode == TBoldDeleteMode.dmRemoveFromList)
            {
                query = unlink ? fUnlinkQuestion : fRemoveQuestion;
            }

            bool PerformOp = !AskFirst ||
                Dialogs.MessageDlg(SysUtils.Format(query, ElementName, Element.AsString, RoleName), TMsgDlgType.mtConfirmation,
                    DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbOK, TMsgDlgBtn.mbCancel), 0) != Bold.Controls.mrCancel;
            if (PerformOp)
            {
                switch (EffectiveDeleteMode)
                {
                    case TBoldDeleteMode.dmRemoveFromList:
                        handle.RemoveCurrentElement();
                        break;
                    case TBoldDeleteMode.dmDelete:
                    case TBoldDeleteMode.dmUnlinkAllAndDelete:
                        if (handle.CurrentBoldObject is { } Obj)
                        {
                            Obj.BoldSystem!.StartTransaction();
                            try
                            {
                                if (EffectiveDeleteMode == TBoldDeleteMode.dmUnlinkAllAndDelete)
                                {
                                    Obj.UnLinkAll();
                                }

                                Obj.Delete();
                                Obj.BoldSystem.CommitTransaction();
                            }
                            catch (Exception)
                            {
                                Obj.BoldSystem.RollbackTransaction();
                                throw;
                            }
                        }

                        break;
                }
            }
        }

        switch (index)
        {
            case TBoldNavigateBtn.nbPrior:
                handle.Prior();
                break;
            case TBoldNavigateBtn.nbNext:
                handle.Next();
                break;
            case TBoldNavigateBtn.nbFirst:
                handle.First();
                break;
            case TBoldNavigateBtn.nbLast:
                handle.Last();
                break;
            case TBoldNavigateBtn.nbInsert:
                handle.CurrentIndex = handle.List!.IndexOf(handle.MutableList!.AddNew());
                break;
            case TBoldNavigateBtn.nbDelete:
                Delete(fConfirmDelete); //FIXME Localize
                break;
            case TBoldNavigateBtn.nbMoveUp:
                handle.List!.Move(handle.CurrentIndex, handle.CurrentIndex - 1);
                break;
            case TBoldNavigateBtn.nbMoveDown:
                handle.List!.Move(handle.CurrentIndex, handle.CurrentIndex + 1);
                break;
        }

        if (!DesignMode)
        {
            fOnNavClick?.Invoke(this, index);
        }
    }

    /// <summary>Makes the buttons: the visible ones in a row, prior and next repeating while held down.</summary>
    [Delphi(PortStatus.Ported)]
    private void InitButtons()
    {
        MinBtnSize = new Point(20, 18);
        int X = 0;
        foreach (TBoldNavigateBtn I in Enum.GetValues<TBoldNavigateBtn>())
        {
            var Btn = new TBoldNavButton
            {
                FlatStyle = fFlat ? FlatStyle.Flat : FlatStyle.Standard,
                Index = I,
                Visible = fVisibleButtons.Contains(I),
                Enabled = true,
                NavStyle = TBoldNavButtonStyle.Of(TBoldNavButtonStyleElement.nsFocusRect),
            };
            Btn.SetBounds(X, 0, MinBtnSize.X, MinBtnSize.Y);
            Btn.Click += ClickHandler;
            Btn.MouseDown += BtnMouseDown;
            Controls.Add(Btn);
            Buttons[(int)I] = Btn;
            X += MinBtnSize.X;
        }

        FixButtonGlyphs();
        InitHints();
        Buttons[(int)TBoldNavigateBtn.nbPrior].NavStyle = Buttons[(int)TBoldNavigateBtn.nbPrior].NavStyle + TBoldNavButtonStyleElement.nsAllowTimer;
        Buttons[(int)TBoldNavigateBtn.nbNext].NavStyle = Buttons[(int)TBoldNavigateBtn.nbNext].NavStyle + TBoldNavButtonStyleElement.nsAllowTimer;
    }

    /// <summary>Enables each button when its action can be done on the handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetActiveButtons()
    {
        TBoldAbstractListHandle? handle = BoldHandle;
        bool EnabledAndHandle = Enabled && handle is not null;
        Button(TBoldNavigateBtn.nbFirst).Enabled = EnabledAndHandle && handle!.HasPrior;
        Button(TBoldNavigateBtn.nbPrior).Enabled = Button(TBoldNavigateBtn.nbFirst).Enabled;
        Button(TBoldNavigateBtn.nbNext).Enabled = EnabledAndHandle && handle!.HasNext;
        Button(TBoldNavigateBtn.nbLast).Enabled = Button(TBoldNavigateBtn.nbNext).Enabled;
        Button(TBoldNavigateBtn.nbInsert).Enabled = EnabledAndHandle && VisibleButtons.Contains(TBoldNavigateBtn.nbInsert) &&
            handle!.MutableList is not null && handle.MutableList.CanCreateNew;
        Button(TBoldNavigateBtn.nbDelete).Enabled = EnabledAndHandle && VisibleButtons.Contains(TBoldNavigateBtn.nbDelete) &&
            handle!.CurrentBoldObject is not null && MapMinus(handle.CurrentBoldObject.CanDelete());
        Button(TBoldNavigateBtn.nbMoveUp).Enabled = EnabledAndHandle && VisibleButtons.Contains(TBoldNavigateBtn.nbMoveUp) &&
            handle!.CurrentBoldObject is not null && handle.List!.CanMove(handle.CurrentIndex, handle.CurrentIndex - 1);
        Button(TBoldNavigateBtn.nbMoveDown).Enabled = EnabledAndHandle && VisibleButtons.Contains(TBoldNavigateBtn.nbMoveDown) &&
            handle!.CurrentBoldObject is not null && handle.List!.CanMove(handle.CurrentIndex, handle.CurrentIndex + 1);
    }

    private TBoldNavButton Button(TBoldNavigateBtn button) => Buttons[(int)button];

    [Delphi(PortStatus.Ported)]
    private void SetBoldListHandle(TBoldAbstractListHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldListHandle() => fHandleFollower?.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private void _AfterMakeUptoDate(TBoldFollower Follower) => SetActiveButtons();

    /// <summary>Whether the delete mode allows deleting, given whether the object can be deleted.</summary>
    [Delphi(PortStatus.Ported)]
    private bool MapMinus(bool CanDeleteObject) => BoldDeleteMode switch
    {
        TBoldDeleteMode.dmDefault or TBoldDeleteMode.dmRemoveFromList => true,
        TBoldDeleteMode.dmDelete => CanDeleteObject,
        TBoldDeleteMode.dmUnlinkAllAndDelete => true,
        _ => throw new EBold(SysUtils.Format(sUnknownDeleteMode, GetType().Name)),
    };

    /// <summary>Copies the image indexes of another navigator.</summary>
    [Delphi(PortStatus.Ported)]
    public void Assign(object? Source)
    {
        if (Source is TBoldCustomNavigator navigator)
        {
            fImageIndices.Assign(navigator.ImageIndices);
        }
    }

    /// <summary>Gives each button its image from the image list, else Bold's bitmap (the enabled half of it).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void FixButtonGlyphs()
    {
        foreach (TBoldNavButton? Btn in Buttons)
        {
            if (Btn is null || fImageIndices is null)
            {
                continue;
            }

            int Index = Btn.Index switch
            {
                TBoldNavigateBtn.nbFirst => ImageIndices.nbFirst,
                TBoldNavigateBtn.nbPrior => ImageIndices.nbPrior,
                TBoldNavigateBtn.nbNext => ImageIndices.nbNext,
                TBoldNavigateBtn.nbLast => ImageIndices.nbLast,
                TBoldNavigateBtn.nbInsert => ImageIndices.nbInsert,
                TBoldNavigateBtn.nbDelete => ImageIndices.nbDelete,
                TBoldNavigateBtn.nbMoveUp => ImageIndices.nbMoveUp,
                TBoldNavigateBtn.nbMoveDown => ImageIndices.nbMoveDown,
                _ => -1,
            };
            if (Images is not null && Index >= 0 && Index < Images.Images.Count)
            {
                Btn.Image = Images.Images[Index];
            }
            else
            {
                Bitmap? glyph = Btn.Index switch
                {
                    TBoldNavigateBtn.nbFirst => BoldCommonBitmaps.bmpBoldNavigatorFirst,
                    TBoldNavigateBtn.nbPrior => BoldCommonBitmaps.bmpBoldNavigatorPrior,
                    TBoldNavigateBtn.nbNext => BoldCommonBitmaps.bmpBoldNavigatorNext,
                    TBoldNavigateBtn.nbLast => BoldCommonBitmaps.bmpBoldNavigatorLast,
                    TBoldNavigateBtn.nbInsert => BoldCommonBitmaps.bmpBoldNavigatorInsert,
                    TBoldNavigateBtn.nbDelete => BoldCommonBitmaps.bmpBoldNavigatorDelete,
                    TBoldNavigateBtn.nbMoveUp => BoldCommonBitmaps.bmpBoldNavigatorMoveUp,
                    _ => BoldCommonBitmaps.bmpBoldNavigatorMoveDown,
                };
                // A glyph wider than high holds an enabled and a disabled image side by side; WinForms grays the enabled one.
                Btn.Image = glyph is not null && glyph.Width > glyph.Height
                    ? glyph.Clone(new Rectangle(0, 0, glyph.Width / 2, glyph.Height), glyph.PixelFormat)
                    : glyph;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetImages(ImageList? Value)
    {
        fImages = Value;
        FixButtonGlyphs();
    }

    /// <summary>Only the current element needs fetching.</summary>
    [Delphi(PortStatus.Ported)]
    private void _BeforeMakeUptoDate(TBoldFollower Follower)
    {
        if (BoldHandle is not null)
        {
            fBoldProperties!.SetActiveRange(Follower, BoldHandle.CurrentIndex, BoldHandle.CurrentIndex);
        }
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(TBoldDeleteMode.dmDefault)]
    public TBoldDeleteMode BoldDeleteMode
    {
        get => fBoldDeleteMode;
        set => fBoldDeleteMode = value;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldListHandle();
        set => SetBoldListHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(true)]
    public bool ConfirmDelete
    {
        get => fConfirmDelete;
        set => fConfirmDelete = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool Flat
    {
        get => fFlat;
        set => SetFlat(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TStrings Hints
    {
        get => fHints;
        set => SetHints(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldButtonSet VisibleButtons
    {
        get => fVisibleButtons;
        set => SetVisible(value);
    }

    /// <summary>A button was clicked (after its action).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomNavigator.OnClick")]
    [DefaultValue(null)]
    public TBoldNavClick? OnNavClick
    {
        get => fOnNavClick;
        set => fOnNavClick = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldNavigateBtnImageIndex ImageIndices
    {
        get => fImageIndices;
        set => fImageIndices = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(null)]
    public ImageList? Images
    {
        get => fImages;
        set => SetImages(value);
    }

    [Delphi(PortStatus.Ported)]
    public string DeleteQuestion
    {
        get => fDeleteQuestion;
        set => fDeleteQuestion = value;
    }

    [Delphi(PortStatus.Ported)]
    public string UnlinkQuestion
    {
        get => fUnlinkQuestion;
        set => fUnlinkQuestion = value;
    }

    [Delphi(PortStatus.Ported)]
    public string RemoveQuestion
    {
        get => fRemoveQuestion;
        set => fRemoveQuestion = value;
    }
}

/// <summary>A Bold navigator, with its properties published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNavigator : TBoldCustomNavigator
{
}
