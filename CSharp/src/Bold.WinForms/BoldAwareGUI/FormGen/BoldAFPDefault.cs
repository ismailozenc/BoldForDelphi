// Delphi: Source/BoldAwareGUI/FormGen/BoldAFPDefault.pas
// Port map: CSharp/port/units/BoldAFPDefault.md

using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldAFPDefault", Symbol = "TBoldEditClass", Note = "A Type: TBoldEdit or a descendant.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldAFPDefault", Symbol = "TBoldExposedCustomGrid", Note = "Used only by FillColumns, which is commented out in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAFPDefault", Symbol = "TExposedGrid", Note = "Opened CreateDefaultColumns to the history tab; it is protected internal in the grid.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAFPDefault", Symbol = "finalization", Note = "Unregistered the providers and freed the form list and the renderer at exit; they end with the process.")]

namespace Bold;

/// <summary>
/// The default form provider: a form of 440 by 360 that closes (and goes) when it is closed. A second request for a
/// form of the same element brings up the open form instead of making another.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultFormProvider : TBoldUserFormProvider
{
    private TBoldApplyPolicy fApplyPolicy;
    private TBoldOrientation fOrientation;
    private bool fReusing;

    [Delphi(PortStatus.Ported)]
    public TBoldDefaultFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
        fApplyPolicy = TBoldApplyPolicy.bapExit;
    }

    /// <summary>Dragging Bold objects over an auto form brings it to the front; the form takes no drop unless its own handler accepts.</summary>
    [Delphi(PortStatus.Ported, Note = "The form's DragOver event; the form allows drops so that WinForms raises it.")]
    private static void DefaultFormDragOver(object? Sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.None;
        if (Sender is Form form)
        {
            if (BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0)
            {
                form.BringToFront();
            }

            int index = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form);
            if (index != -1 && BoldAFPDefault.G_BoldActiveAutoForms.Entries(index).OnDragOver is { } StoredDragOverEvent)
            {
                StoredDragOverEvent(Sender, e);
            }
        }
    }

    /// <summary>
    /// The active control applies its edit (as when it is left), the stored close handler runs, and the form leaves the
    /// list of auto forms unless the close is cancelled.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The form's FormClosing event. Closing a form shown with Show disposes it (Delphi's caFree). The form stays in the list when the close is cancelled.")]
    private static void DefaultFormOnClose(object? Sender, FormClosingEventArgs e)
    {
        if (Sender is Form form)
        {
            if (form.ActiveControl is { } active)
            {
                fOnLeave.Invoke(active, [EventArgs.Empty]);
            }

            int index = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form);
            if (index != -1)
            {
                BoldAFPDefault.G_BoldActiveAutoForms.Entries(index).OnClose?.Invoke(Sender, e);
                if (!e.Cancel)
                {
                    BoldAFPDefault.G_BoldActiveAutoForms.RemoveByForm(form);
                }
            }
        }
    }

    /// <summary>Control.OnLeave, which Delphi reached with Perform(CM_EXIT).</summary>
    private static readonly MethodInfo fOnLeave = typeof(Control).GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>The form of the handle goes (later) when the handle's object is deleted or destroyed.</summary>
    [Delphi(PortStatus.Ported)]
    private protected static void DefaultReceiveObjectGone(object? Sender)
    {
        if (BoldOwnedComponents.OwnerOf(Sender as IComponent) is Form form)
        {
            BoldOwnedComponents.Release(form);
        }
    }

    /// <summary>Makes the form unless an open one is reused.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoGenerateForm()
    {
        if (!Reusing)
        {
            base.DoGenerateForm();
        }
    }

    /// <summary>Makes the form and adds it to the list of auto forms (it leaves the list when it goes).</summary>
    [Delphi(PortStatus.Ported, Note = "A disposed form leaves the list; in Delphi a form freed without closing stayed in it.")]
    protected override void EnsureForm()
    {
        base.EnsureForm();
        if (Form is { } form)
        {
            BoldAFPDefault.G_BoldActiveAutoForms.AddPair(form, Element);
            form.Disposed += FormDisposed;
            PostEnsureForm();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Delphi placed the handle on a form designer (DesignInfo); WinForms components have no such place.")]
    protected override void EnsureHandle()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type? GetFormClass() => typeof(Form);

    [Delphi(PortStatus.Ported)]
    protected override Control? GetTarget() => Form;

    /// <summary>The form at the default position, 440 by 360.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void PostEnsureForm()
    {
        Form form = Form!;
        form.StartPosition = FormStartPosition.WindowsDefaultLocation;
        form.Bounds = new Rectangle(0, 0, 440, 360);
    }

    /// <summary>The form's close and drag-over go through the default handlers.</summary>
    [Delphi(PortStatus.Ported, Note = "WinForms events are multicast: the form's own handlers stay subscribed, so there is no handler to store; the entry's handlers are run by the default ones when set.")]
    protected override void PostGenerateAutoForm()
    {
        Form form = Form!;
        form.FormClosing -= DefaultFormOnClose;
        form.DragOver -= DefaultFormDragOver;
        form.FormClosing += DefaultFormOnClose;
        form.DragOver += DefaultFormDragOver;
        form.AllowDrop = true;
    }

    /// <summary>A form that goes without closing (released, or given up for want of a handle) leaves the list too.</summary>
    private static void FormDisposed(object? sender, EventArgs e)
    {
        if (sender is Form form && BoldAFPDefault.G_BoldActiveAutoForms.IndexOfForm(form) != -1)
        {
            BoldAFPDefault.G_BoldActiveAutoForms.RemoveByForm(form);
        }
    }

    /// <summary>An open auto form of the element is brought up (restored when minimized) and reused.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void PreGenerateAutoForm()
    {
        int index = BoldAFPDefault.G_BoldActiveAutoForms.IndexOfElement(Element);
        fReusing = index != -1;
        if (Reusing)
        {
            Form form = BoldAFPDefault.G_BoldActiveAutoForms.Forms(index);
            Form = form;
            form.BringToFront();
            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldApplyPolicy ApplyPolicy
    {
        get => fApplyPolicy;
        set => fApplyPolicy = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldOrientation Orientation
    {
        get => fOrientation;
        set => fOrientation = value;
    }

    /// <summary>True when an open form of the element is reused.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool Reusing => fReusing;
}

/// <summary>A form with a grid and a navigator on a list handle rooted in the reference handle.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFormProviderForList : TBoldDefaultFormProvider
{
    private TBoldGrid? fBoldGrid;
    private TBoldAbstractListHandle? fListHandle;

    public TBoldFormProviderForList(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle GetlistHandle()
    {
        if (fListHandle is null)
        {
            fListHandle = new TBoldListHandle(BoldOwnedComponents.Of(Form!));
            fListHandle.RootHandle = BoldHandle;
        }

        return fListHandle;
    }

    /// <summary>A reference handle typed as the list's elements; the form goes when the value goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureHandle()
    {
        var handle = new TBoldReferenceHandle(BoldOwnedComponents.Of(Form!));
        BoldHandle = handle;
        TBoldElementTypeInfo DesignTimeContext = Element.BoldType!;
        if (DesignTimeContext is TBoldListTypeInfo listType)
        {
            DesignTimeContext = listType.ListElementTypeInfo!;
        }

        handle.StaticValueTypeName = DesignTimeContext.ExpressionName;
        handle.OnObjectDeleted = DefaultReceiveObjectGone;
        handle.OnValueDestroyed = DefaultReceiveObjectGone;
        base.EnsureHandle();
    }

    /// <summary>The grid (with automatic columns) and the navigator on the list; the caption is the type's model name.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void PreEnsureComponents()
    {
        base.PreEnsureComponents();
        fBoldGrid = new TBoldGrid
        {
            Name = "BoldGrid", // do not localize
            BoldShowConstraints = BoldAFPDefault.BoldShowConstraintsInAutoFormGrids,
            BoldAutoColumns = true,
            BoldHandle = ListHandle,
            Dock = DockStyle.Fill,
            Parent = Target,
        };
        _ = new TBoldNavigator
        {
            Parent = Target,
            Dock = DockStyle.Bottom,
            BoldHandle = ListHandle,
            Name = "BoldNavigator", // do not localize
        };
        Form!.Text = Element.BoldType!.ModelName;
        ListHandle.Name = "BoldListHandle"; // do not localize
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldGrid? BoldGrid => fBoldGrid;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractListHandle ListHandle => GetlistHandle();
}

/// <summary>The form of a system (the list form).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultSystemAutoFormProvider : TBoldFormProviderForList
{
    public TBoldDefaultSystemAutoFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }
}

/// <summary>The form of a list of attributes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultAttributeListAutoFormProvider : TBoldFormProviderForList
{
    public TBoldDefaultAttributeListAutoFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }
}

/// <summary>The form of a list of objects.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultObjectListAutoFormProvider : TBoldFormProviderForList
{
    public TBoldDefaultObjectListAutoFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }
}

/// <summary>A form on a reference handle typed as the element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFormProviderForSingle : TBoldDefaultFormProvider
{
    public TBoldFormProviderForSingle(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    /// <summary>A reference handle typed as the element; the form goes when the value goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureHandle()
    {
        var handle = new TBoldReferenceHandle(BoldOwnedComponents.Of(Form!));
        BoldHandle = handle;
        handle.StaticValueTypeName = Element.BoldType!.ExpressionName;
        handle.OnObjectDeleted = DefaultReceiveObjectGone;
        handle.OnValueDestroyed = DefaultReceiveObjectGone;
        base.EnsureHandle();
    }
}

/// <summary>The form of an attribute: its name and an edit.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultAttributeAutoFormProvider : TBoldFormProviderForSingle
{
    public TBoldDefaultAttributeAutoFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    /// <summary>A label with the attribute's model name and an edit of its value.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureComponents()
    {
        int Box1NextTop = BoldAFPDefault.CONTROLMARGIN;
        var label = new Label
        {
            Top = BoldAFPDefault.CONTROLMARGIN,
            Left = BoldAFPDefault.CONTROLMARGIN,
            AutoSize = true,
            Parent = Target,
            Text = ModelNameOrDefault("DataValue"), // do not localize
        };
        Box1NextTop += label.Height;
        _ = new TBoldEdit
        {
            Top = Box1NextTop,
            Left = BoldAFPDefault.CONTROLMARGIN,
            BoldHandle = BoldHandle,
            Parent = Target,
        };
        Box1NextTop += BoldAFPDefault.EDITHEIGHT;
        Form form = Form!;
        form.ClientSize = new Size(form.ClientSize.Width, Box1NextTop + BoldAFPDefault.EDITHEIGHT * 2 + BoldAFPDefault.CONTROLMARGIN);
    }
}

/// <summary>
/// The form of an object: a tab with an edit per attribute and single role (a combo box for a value set), a tab per
/// multi role with a grid and a navigator, a history tab, and a button panel with a drag source and the buttons of
/// the apply policy. The caption is the class's model name and the object's string.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultObjectAutoFormProvider : TBoldFormProviderForSingle
{
    private bool fClassTypeInfoOverride;
    private TBoldClassTypeInfo? fClassTypeInfo;
    private TabControl? fPageControl;
    private TBoldMemberRTInfoList? fMemberRTInfoList;
    private Panel? fButtonPanel;
    private TBoldDropTarget? fDragSource;
    protected bool fDisplaySingleRoleButtons;

    public TBoldDefaultObjectAutoFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    /// <summary>The member of the other end's class to show a single link with: its first string attribute, else its first attribute (none when the class has a default string).</summary>
    [Delphi(PortStatus.Ported)]
    private string GetGoodStringRepresentationForSingleLink(TBoldMemberRTInfo MemberRTInfo)
    {
        string result = "";
        TBoldClassTypeInfo ClassTypeInfo = ((TBoldRoleRTInfo)MemberRTInfo).ClassTypeInfoOfOtherEnd!;
        if (ClassTypeInfo.DefaultStringRepresentation == "")
        {
            for (int i = 0; i < ClassTypeInfo.AllMembers.Count; i++)
            {
                if (ClassTypeInfo.AllMembers[i].BoldType!.ExpressionName == "String") // do not localize
                {
                    return ClassTypeInfo.AllMembers[i].ExpressionName;
                }
            }

            for (int i = 0; i < ClassTypeInfo.AllMembers.Count; i++)
            {
                if (ClassTypeInfo.AllMembers[i] is TBoldAttributeRTInfo)
                {
                    return ClassTypeInfo.AllMembers[i].ExpressionName;
                }
            }
        }

        return result;
    }

    /// <summary>The widest model name of the attributes and single roles, in the form's font.</summary>
    [Delphi(PortStatus.Ported)]
    private int GetLargestWidth()
    {
        int result = 0;
        for (int i = 0; i < MemberRTInfoList.Count; i++)
        {
            TBoldMemberRTInfo member = MemberRTInfoList[i];
            if (member.IsAttribute || member.IsSingleRole)
            {
                int w = TextRenderer.MeasureText(member.ModelName, Form!.Font).Width;
                if (w > result)
                {
                    result = w;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfoList GetMemberRTInfoList() => fMemberRTInfoList ??= BoldObject.BoldClassTypeInfo.AllMembers;

    [Delphi(PortStatus.Ported)]
    private void SetMemberRTInfoList(TBoldMemberRTInfoList Value) => fMemberRTInfoList = Value;

    /// <summary>The page control, made on first use; it fills the form.</summary>
    [Delphi(PortStatus.Ported, Note = "A TabControl. It is brought to the front so that it fills what the docked button panel leaves (WinForms docks in reverse order).")]
    private TabControl GetPageControl()
    {
        if (fPageControl is null)
        {
            fPageControl = BoldUtils.BoldRunningAsDesignTimePackage ? new TabControl() : new TAFPPageControl();
            fPageControl.Parent = Target;
            fPageControl.Name = "PageControl"; // do not localize
            fPageControl.Dock = DockStyle.Fill;
            fPageControl.BringToFront();
        }

        return fPageControl;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject GetBoldObject()
    {
        System.Diagnostics.Debug.Assert(Element is TBoldObject, "Attempting to invoke object form generator with no object in sight!");
        return (TBoldObject)Element;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldClassTypeInfo GetClassTypeInfo() => ClassTypeInfoOverride ? fClassTypeInfo! : (TBoldClassTypeInfo)ElementTypeInfo;

    [Delphi(PortStatus.Ported)]
    private static string MakeComponentName(string Postfix, TBoldClassTypeInfo ClassTypeInfo, TBoldMemberRTInfo? MemberRTInfo)
    {
        string MemberName = MemberRTInfo is not null ? MemberRTInfo.ExpressionName : "";
        return SysUtils.Format("%s%s%s", ClassTypeInfo.ExpressionName, MemberName, Postfix); // do not localize
    }

    [Delphi(PortStatus.Ported, Note = "A TabPage.")]
    private TabPage CreateTabSheet(string Caption, string Name)
    {
        var result = new TabPage { Text = Caption, Name = Name };
        PageControl.TabPages.Add(result);
        return result;
    }

    [Delphi(PortStatus.Ported, Note = "A Panel that scrolls (AutoScroll).")]
    private Panel CreateScrollBox(Control aParent) => new()
    {
        Name = MakeComponentName("ScrollBox", ClassTypeInfo, null), // do not localize
        Parent = aParent,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        AutoScroll = true,
    };

    /// <summary>The form's caption: the class's model name and the object's string.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldCaptionController CreateCaptionController()
    {
        var result = new TBoldCaptionController();
        BoldOwnedComponents.Of(Form!).Add(result, MakeComponentName("CaptionController", ClassTypeInfo, null)); // do not localize
        result.BoldHandle = BoldHandle;
        result.BoldProperties.Expression = "'" + ClassTypeInfo.ModelName + ": ' + self.asstring"; // do not localize
        result.TrackControl = Form;
        return result;
    }

    /// <summary>The panel at the bottom with the drag source and, for a historic object, its time stamp.</summary>
    [Delphi(PortStatus.Ported)]
    private Panel CreateButtonPanel()
    {
        var result = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = BoldAFPDefault.PANELHEIGHT,
            BorderStyle = BorderStyle.None,
            Parent = Form,
            Name = "ButtonPanel", // do not localize
            Text = "",
        };
        fDragSource = CreateDragSource(result);
        TBoldObjectId id = BoldObject.BoldObjectLocator!.BoldObjectID;
        if (id.TimeStamp != BoldDefs.BOLDMAXTIMESTAMP)
        {
            _ = new Label
            {
                Parent = result,
                Left = fDragSource.Left + fDragSource.Width + 15,
                Top = 10,
                AutoSize = true,
                Text = SysUtils.Format(sHistoricObject, id.TimeStamp),
            };
        }

        return result;
    }

    /// <summary>
    /// Enables the list handle of a tab when the tab is shown, shows it, and lets the grid's columns adjust their
    /// widths (up to 100).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Runs when the tab page becomes visible (Delphi's OnShow).")]
    private void ActivateTabSheetHandle(object? Sender)
    {
        // note, when this method executes, the autoform provider is most likely destroyed, we must not access it.
        if (Sender is TabPage ATabSheet)
        {
            if (BoldOwnedComponents.All(ATabSheet).OfType<TBoldListHandle>().FirstOrDefault() is { } listHandle)
            {
                listHandle.Enabled = true;
            }

            TBoldQueueable.DisplayAll();
            foreach (TBoldGrid grid in ATabSheet.Controls.OfType<TBoldGrid>())
            {
                for (int j = 2; j < grid.Columns.Count; j++)
                {
                    TBoldGridColumn Col = grid.Columns[j];
                    Col.CWAdjust = TBoldCWAdjustSet.Of(TBoldCWAdjust.caAllowGrow, TBoldCWAdjust.caAllowShrink, TBoldCWAdjust.caIncludeTitle);
                    if (Col.Width > 100)
                    {
                        Col.Width = 100;
                    }
                }
            }
        }
    }

    /// <summary>Runs the handler when the tab page becomes visible (Delphi's TTabSheet.OnShow).</summary>
    private static void OnShow(TabPage page, Action<object?> handler) =>
        page.VisibleChanged += (sender, _) =>
        {
            if (page.Visible)
            {
                handler(sender);
            }
        };

    /// <summary>The newest version first.</summary>
    [Delphi(PortStatus.Ported)]
    private int CompareObjectsOnTimeStamp(TBoldElement? Item1, TBoldElement? Item2) =>
        ((TBoldObject)Item2!).BoldObjectLocator!.BoldObjectID.TimeStamp - ((TBoldObject)Item1!).BoldObjectLocator!.BoldObjectID.TimeStamp;

    /// <summary>
    /// The versions of the object (newest first): the change points of the object, and with the check box ticked, of
    /// its non-embedded roles to versioned classes.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void DeriveObjectHistory(Component Sender, TBoldElement? RootValue, TBoldIndirectElement ResultElement, TBoldSubscriber? Subscriber)
    {
        if (RootValue is null)
        {
            return;
        }

        var RootObject = (TBoldObject)RootValue;
        var DerivedHandle = (TObjectHistoryHandle)Sender;
        var idList = new TBoldObjectIdList();
        var memberIdList = new TBoldMemberIdList();
        var aCond = new TBoldChangePointCondition { IdList = idList, MemberIdList = memberIdList };
        idList.Add(RootObject.BoldObjectLocator!.BoldObjectID);
        if (DerivedHandle.Tag == 1)
        {
            for (int i = 0; i < ClassTypeInfo.AllMembers.Count; i++)
            {
                if (ClassTypeInfo.AllMembers[i].IsRole && !ClassTypeInfo.AllMembers[i].IsStoredInObject &&
                    ((TBoldRoleRTInfo)ClassTypeInfo.AllMembers[i]).ClassTypeInfoOfOtherEnd!.Versioned)
                {
                    memberIdList.Add(new TBoldMemberID(i));
                }
            }
        }

        aCond.StartTime = 0;
        aCond.EndTime = BoldDefs.BOLDMAXTIMESTAMP;

        TBoldObjectList ObjectList;
        if (ResultElement.Value is null)
        {
            TBoldListTypeInfo ListType = RootObject.BoldClassTypeInfo.ListTypeInfo!;
            ObjectList = (TBoldObjectList)TBoldMemberFactory.CreateMemberFromBoldType(ListType);
            ResultElement.SetOwnedValue(ObjectList);
        }
        else
        {
            ObjectList = (TBoldObjectList)ResultElement.Value;
            ObjectList.Clear();
        }

        RootObject.BoldSystem!.GetAllWithCondition(ObjectList, aCond);
        ObjectList.Sort(CompareObjectsOnTimeStamp);
    }

    /// <summary>The check box toggles whether the history includes the changes in non-embedded links.</summary>
    [Delphi(PortStatus.Ported)]
    private void ChangeNonEmbeddedLinksInclusion(object? Sender, EventArgs e)
    {
        // this toggles the tag of the DerivedHandle between 0 and 1;
        if ((Sender as Control)?.FindForm() is { } form &&
            BoldOwnedComponents.Find(form, "bdhObjectHistory") is TObjectHistoryHandle DerivedHandle) // do not localize
        {
            DerivedHandle.Tag = 1 - DerivedHandle.Tag;
            DerivedHandle.MarkOutOfDate();
        }
    }

    /// <summary>A Close button, or Apply, Cancel and OK buttons when changes are applied on demand.</summary>
    [Delphi(PortStatus.Ported)]
    private void CreateButtons()
    {
        Form form = Form!;
        Panel panel = ButtonPanel!;
        // Create button set
        switch (ApplyPolicy)
        {
            case TBoldApplyPolicy.bapChange:
            case TBoldApplyPolicy.bapExit:
            {
                var aButton = new Button { Parent = panel, Text = sClose };
                aButton.Click += CloseButtonClick;
                aButton.Left = form.ClientSize.Width - aButton.Width - BoldAFPDefault.CONTROLMARGIN;
                aButton.Top = BoldAFPDefault.CONTROLMARGIN / 2;
                aButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                aButton.Name = "CloseButton"; // do not localize
                break;
            }

            case TBoldApplyPolicy.bapDemand:
            {
                // Apply button
                var aButton = new Button { Text = sApply, Parent = panel };
                aButton.Left = panel.Width - aButton.Width;
                aButton.Top = BoldAFPDefault.CONTROLMARGIN / 2;
                aButton.Anchor = AnchorStyles.Right;
                aButton.Click += ApplyButtonClick;
                aButton.Name = "ApplyButton"; // do not localize

                // Cancel button
                aButton = new Button { Text = sCancel, Parent = panel };
                form.CancelButton = aButton;
                aButton.Left = panel.Width - (aButton.Width * 2) - BoldAFPDefault.CONTROLMARGIN;
                aButton.Top = BoldAFPDefault.CONTROLMARGIN / 2;
                aButton.Anchor = AnchorStyles.Right;
                aButton.Click += CancelButtonClick;
                aButton.Name = "CancelButton"; // do not localize

                // OK button
                aButton = new Button { Text = sOK, Parent = panel };
                form.AcceptButton = aButton;
                aButton.Left = panel.Width - (aButton.Width * 3) - (BoldAFPDefault.CONTROLMARGIN * 2);
                aButton.Top = BoldAFPDefault.CONTROLMARGIN / 2;
                aButton.Anchor = AnchorStyles.Right;
                aButton.Click += OKButtonClick;
                aButton.Name = "OKButton"; // do not localize
                break;
            }

            default:
                throw new EBold(sIllegalApplyPolicy);
        }
    }

    /// <summary>A drop target on the reference handle that drags the object (it takes no drops).</summary>
    [Delphi(PortStatus.Ported, Note = "The DROPTARGETSOURCE bitmap is embedded in Bold.WinForms (Images/DropTargetSource.bmp in the repository).")]
    private TBoldDropTarget CreateDragSource(Control aParent)
    {
        var result = new TBoldDropTarget
        {
            Parent = aParent,
            BackColor = Color.Transparent,
            Image = DragSourcePicture(),
            IsDragSource = true,
            IsDropTarget = false,
            Width = 20,
            Height = 25,
            Top = 5,
            Left = 5,
            BoldHandle = BoldHandle,
        };
        return result;
    }

    /// <summary>The drag source's picture, transparent where its lower left pixel's color is (as Delphi's transparent image).</summary>
    private static Bitmap? DragSourcePicture()
    {
        using Stream? stream = typeof(TBoldDefaultObjectAutoFormProvider).Assembly.GetManifestResourceStream("Bold.Images.DropTargetSource.bmp"); // do not localize
        if (stream is null)
        {
            return null;
        }

        var bitmap = new Bitmap(stream);
        bitmap.MakeTransparent(bitmap.GetPixel(0, bitmap.Height - 1));
        return bitmap;
    }

    /// <summary>Read-only members show in the inactive caption text color.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void BoldAsStringRenderer1SetColor(TBoldElement? Element, ref Color AColor, TBoldRepresentation Representation, TBoldExpression Expression)
    {
        if (Element is not null)
        {
            using var Result = new TBoldIndirectElement();
            Element.EvaluateExpression(Expression, Result);
            if (Result.Value is TBoldMember member && !member.CanModify())
            {
                AColor = SystemColors.InactiveCaptionText;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void CloseButtonClick(object? Sender, EventArgs e)
    {
        System.Diagnostics.Debug.Assert(Sender is Button);
        ((Button)Sender!).FindForm()?.Close();
    }

    /// <summary>The form's changes are discarded and the form closes.</summary>
    [Delphi(PortStatus.Ported)]
    private static void CancelButtonClick(object? Sender, EventArgs e)
    {
        System.Diagnostics.Debug.Assert(Sender is Button);
        Form? form = ((Button)Sender!).FindForm();
        TBoldQueueable.DiscardChangeAllMatching(form);
        form?.Close();
    }

    /// <summary>The form's changes are applied and the form closes.</summary>
    [Delphi(PortStatus.Ported)]
    private static void OKButtonClick(object? Sender, EventArgs e)
    {
        ApplyButtonClick(Sender, e);
        System.Diagnostics.Debug.Assert(Sender is Button);
        ((Button)Sender!).FindForm()?.Close();
    }

    /// <summary>The form's changes are applied.</summary>
    [Delphi(PortStatus.Ported)]
    private static void ApplyButtonClick(object? Sender, EventArgs e)
    {
        System.Diagnostics.Debug.Assert(Sender is Button);
        TBoldQueueable.ApplyAllMatching(((Button)Sender!).FindForm());
    }

    /// <summary>Navigable roles are shown, except link roles to link classes without members of their own.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool MemberShouldBeDisplayed(TBoldMemberRTInfo Member)
    {
        bool result;
        if (Member is TBoldRoleRTInfo Role)
        {
            result = Role.IsNavigable;
            // suppress linkroles if the linkobjects does not have any modelled members
            if (Role.RoleType == TBoldRoleType.rtLinkRole && Role.ClassTypeInfoOfOtherEnd!.AllMembers.Count == 2)
            {
                result = false;
            }
        }
        else
        {
            result = true;
        }

        return result;
    }

    /// <summary>The form at most 90% of the screen, the button panel, the tabs and the caption.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureComponents()
    {
        Rectangle screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1024, 768);
        Form!.MaximumSize = new Size((int)Math.Round(screen.Width * 0.9), (int)Math.Round(screen.Height * 0.9));

        ButtonPanel = CreateButtonPanel();
        CreateButtons();
        EnsureSingleMemberControls();
        EnsureObjectVersionControls();
        EnsureMultiRoleMemberControls();
        CreateMethodButtonsForClass();

        CreateCaptionController();
    }

    /// <summary>
    /// A history tab: for a versioned class, a grid of the object's versions (newest first, with their time stamps) and
    /// a check box to include the changes in non-embedded links; otherwise a note that the object is not versioned.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureObjectVersionControls()
    {
        if (ClassTypeInfo.Versioned || BoldAFPDefault.BoldShowHistoryTabInAutoForms)
        {
            TabPage TabSheet = CreateTabSheet("History", "tabObjectHistory"); // do not localize
            if (ClassTypeInfo.Versioned)
            {
                OnShow(TabSheet, ActivateTabSheetHandle);

                var DerivedHandle = new TObjectHistoryHandle(BoldOwnedComponents.Of(Form!))
                {
                    Name = "bdhObjectHistory", // do not localize
                    RootHandle = BoldHandle,
                    OnDeriveAndSubscribe = DeriveObjectHistory,
                    ValueTypeName = "Collection(" + ClassTypeInfo.ExpressionName + ")", // do not localize
                };

                var listHandle = new TBoldListHandle(BoldOwnedComponents.Of(TabSheet)) // must be tabsheet for "ActivateTabSheetHandle" to work
                {
                    Enabled = false,
                    RootHandle = DerivedHandle,
                    Expression = "",
                    Name = "blhObjectHistory", // do not localize
                };

                var Panel = new Panel
                {
                    Parent = TabSheet,
                    Height = 24,
                    Dock = DockStyle.Bottom,
                    Text = "",
                    BorderStyle = BorderStyle.None,
                };
                var CheckBox = new CheckBox
                {
                    Parent = Panel,
                    Name = "cbIncludeNonembeddedlinksInHistory", // do not localize
                    Text = sIncludeChangesInNonEmbeddedLinks,
                    Left = 4,
                    Top = 4,
                };
                CheckBox.Click += ChangeNonEmbeddedLinksInclusion;
                CheckBox.Width = TextRenderer.MeasureText(CheckBox.Text, Form!.Font).Width + 20;

                var g = new TBoldGrid
                {
                    EnableColAdjust = true,
                    Dock = DockStyle.Fill,
                    BoldHandle = listHandle,
                    BoldShowConstraints = BoldAFPDefault.BoldShowConstraintsInAutoFormGrids,
                };
                g.CreateDefaultColumns();
                TBoldGridColumn TimeStampColumn = g.Columns.Add();

                if (((TBoldSystemTypeInfo)ClassTypeInfo.SystemTypeInfo!).UseClockLog)
                {
                    TimeStampColumn.BoldProperties.Expression = "objectTimeStamp.TimeStampToTime"; // do not localize
                }
                else
                {
                    TimeStampColumn.BoldProperties.Expression = "objectTimeStamp"; // do not localize
                }

                TimeStampColumn.Title.Caption = "Timestamp"; // do not localize
                TimeStampColumn.Index = 2;

                g.Name = "bgrObjectHistory"; // do not localize
                g.Parent = TabSheet;
                g.BringToFront(); // fills what the docked panel leaves
            }
            else
            {
                _ = new Label
                {
                    Parent = TabSheet,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Text = BoldDefs.BOLDCRLF + sObjectNotVersioned,
                };
                var note = new Label
                {
                    Parent = TabSheet,
                    Dock = DockStyle.Bottom,
                    AutoSize = false,
                    Text = SysUtils.Format("%s%s%s", sToDisableTab_1, BoldDefs.BOLDCRLF, sToDisableTab_2),
                };
                note.Height = (note.Font.Height * 2) + 6;
            }
        }
    }

    /// <summary>A tab per displayed multi role: a grid and a navigator on a list handle that is enabled when the tab shows.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureMultiRoleMemberControls()
    {
        for (int i = 0; i < MemberRTInfoList.Count; i++)
        {
            TBoldMemberRTInfo Member = MemberRTInfoList[i];
            if (Member.IsMultiRole && MemberShouldBeDisplayed(Member))
            {
                TabPage TabSheet = CreateTabSheet(Member.ModelName, MakeComponentName("Tab", ClassTypeInfo, Member)); // do not localize

                var listHandle = new TBoldListHandle(BoldOwnedComponents.Of(TabSheet)); // must be tabsheet for "ActivateTabSheetHandle" to work
                listHandle.Enabled = false;
                OnShow(TabSheet, ActivateTabSheetHandle);

                listHandle.RootHandle = BoldHandle;
                listHandle.Expression = Member.ExpressionName;
                listHandle.Name = MakeComponentName("Handle", ClassTypeInfo, Member); // do not localize
                TBoldElementTypeInfo ValueType = Member.BoldType!;
                if (ValueType is TBoldListTypeInfo listType)
                {
                    ValueType = listType.ListElementTypeInfo!;
                }

                listHandle.RootTypeName = ValueType.ExpressionName;
                listHandle.StaticSystemHandle = BoldHandle!.StaticSystemHandle;

                _ = new TBoldGrid
                {
                    EnableColAdjust = true,
                    Dock = DockStyle.Fill,
                    BoldAutoColumns = true,
                    BoldShowConstraints = BoldAFPDefault.BoldShowConstraintsInAutoFormGrids,
                    Name = MakeComponentName("Grid", ClassTypeInfo, Member), // do not localize
                    Parent = TabSheet,
                    BoldHandle = listHandle,
                };
                _ = new TBoldNavigator
                {
                    Dock = DockStyle.Bottom,
                    BoldHandle = listHandle,
                    Parent = TabSheet,
                };
            }
        }
    }

    /// <summary>
    /// The object's tab: a label and an edit per displayed attribute and single role (a combo box for a value set). A
    /// single role's edit is read-only, takes a dropped object and has a button with the role's menu.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi also moved the non-visual components to the bottom of the form in a designer (DesignInfo), which WinForms components do not have.")]
    protected void EnsureSingleMemberControls()
    {
        Type EditorClass = BoldUtils.BoldRunningAsDesignTimePackage ? typeof(TBoldEdit) : typeof(TBoldAFEdit);

        TabPage TabSheet = CreateTabSheet(ClassTypeInfo.ModelName, MakeComponentName("Tab", ClassTypeInfo, null)); // do not localize
        Panel ScrollBox = CreateScrollBox(TabSheet);

        int MaxLabelWidth = GetLargestWidth();
        int Box1NextLeft = BoldAFPDefault.CONTROLMARGIN;
        int Box1NextTop = BoldAFPDefault.CONTROLMARGIN;
        TBoldSystemTypeInfo SystemTypeInfo = (TBoldSystemTypeInfo)ClassTypeInfo.SystemTypeInfo!;

        for (int i = 0; i < MemberRTInfoList.Count; i++)
        {
            TBoldMemberRTInfo Member = MemberRTInfoList[i];
            if (MemberShouldBeDisplayed(Member) && !Member.IsMultiRole)
            {
                var label = new Label
                {
                    Text = Member.ModelName + ":",
                    Name = SysUtils.Format("Label%d", i + 1), // do not localize
                    AutoSize = false,
                    TextAlign = ContentAlignment.TopLeft,
                };
                label.Width = MaxLabelWidth + TextRenderer.MeasureText(":", label.Font).Width; // wide enough for the colon
                if ((label.Width + 131 + Box1NextLeft) > ScrollBox.ClientSize.Width - 2 * BoldAFPDefault.CONTROLMARGIN && Orientation == TBoldOrientation.orHorizontal)
                {
                    Box1NextTop += 24;
                    Box1NextLeft = 10;
                }

                label.Left = Box1NextLeft;
                label.Top = Box1NextTop + 4;
                label.Parent = ScrollBox;
                Box1NextLeft += label.Width + 10;

                if (Member.BoldType!.ConformsTo(SystemTypeInfo.AttributeTypeInfoByExpressionName("ValueSet"))) // do not localize
                {
                    var combo = new TBoldComboBox { Parent = ScrollBox, Left = Box1NextLeft, Top = Box1NextTop };
                    combo.Name = MakeComponentName("BoldComboBox", ClassTypeInfo, Member); // do not localize
                    combo.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    combo.Width = ScrollBox.Width - Box1NextLeft - 8;

                    var ValueSetHandle = new TBoldListHandle(BoldOwnedComponents.Of(Form!))
                    {
                        RootHandle = BoldHandle,
                        Expression = Member.BoldType.ExpressionName + ".allInstances", // do not localize
                    };
                    combo.BoldHandle = BoldHandle;
                    combo.BoldProperties.Expression = Member.ExpressionName;
                    combo.BoldListHandle = ValueSetHandle;
                    if (Orientation == TBoldOrientation.orHorizontal)
                    {
                        Box1NextLeft += combo.Width + 20;
                    }
                    else
                    {
                        Box1NextLeft = BoldAFPDefault.CONTROLMARGIN;
                    }

                    if (Orientation != TBoldOrientation.orHorizontal)
                    {
                        Box1NextTop += 24;
                    }
                }
                else
                {
                    var BoldEdit = (TBoldEdit)Activator.CreateInstance(EditorClass)!;
                    BoldEdit.Parent = ScrollBox;
                    BoldEdit.Left = Box1NextLeft;
                    BoldEdit.Top = Box1NextTop;
                    BoldEdit.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    BoldEdit.Width = ScrollBox.Width - Box1NextLeft - 8;
                    BoldEdit.Name = MakeComponentName("Edit", ClassTypeInfo, Member); // do not localize
                    if (Member.IsSingleRole)
                    {
                        var ExpressionHandle = new TBoldExpressionHandle(BoldOwnedComponents.Of(Form!))
                        {
                            RootHandle = BoldHandle,
                            Expression = Member.ExpressionName,
                            Name = MakeComponentName("Handle", ClassTypeInfo, Member), // do not localize
                        };
                        BoldEdit.BackColor = SystemColors.InactiveCaptionText;
                        BoldEdit.ReadOnly = true;
                        BoldEdit.BoldHandle = ExpressionHandle;
                        BoldEdit.BoldProperties.DragMode = TBoldDragMode.bdgSelection;
                        BoldEdit.BoldProperties.DropMode = TBoldDropMode.bdpReplace;
                        BoldEdit.BoldProperties.Expression = GetGoodStringRepresentationForSingleLink(Member);
                        BoldEdit.BoldProperties.ApplyPolicy = ApplyPolicy;
                        BoldEdit.Width -= 20;
                        var RoleButton = new TRoleButton { Parent = ScrollBox, Text = "...", Top = BoldEdit.Top, Width = 20 };
                        RoleButton.Left = ScrollBox.Width - 8 - RoleButton.Width; //TimeWarp 07/20/01
                        RoleButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                        RoleButton.Height = BoldEdit.Height;
                        RoleButton.BoldHandle = BoldEdit.BoldHandle;
                        RoleButton.ContextMenuStrip = new TRoleMenu(RoleButton);
                    }
                    else
                    {
                        BoldEdit.BoldHandle = BoldHandle;
                        BoldEdit.BoldProperties.Expression = Member.ExpressionName;
                        BoldEdit.BoldProperties.ApplyPolicy = ApplyPolicy;
                        BoldEdit.BoldProperties.Renderer = BoldAFPDefault.fReadOnlyStringRenderer;
                    }

                    if (Orientation == TBoldOrientation.orHorizontal)
                    {
                        Box1NextLeft += BoldEdit.Width + 20;
                    }
                    else
                    {
                        Box1NextLeft = BoldAFPDefault.CONTROLMARGIN;
                    }

                    if (Orientation != TBoldOrientation.orHorizontal)
                    {
                        Box1NextTop += 24;
                    }
                }
            }
        }

        Form!.Height = Box1NextTop + 100 + BoldAFPDefault.PANELHEIGHT < BoldAFPDefault.MINFORMHEIGHT
            ? BoldAFPDefault.MINFORMHEIGHT
            : Box1NextTop + 100 + BoldAFPDefault.PANELHEIGHT;
    }

    [Delphi(PortStatus.Ported, Note = "Its body is commented out in Delphi (buttons for the class's methods): it makes nothing.")]
    protected void CreateMethodButtonsForClass()
    {
    }

    /// <summary>The form shows the class instead of the object's own class.</summary>
    [Delphi(PortStatus.Ported)]
    public void RegisterClassTypeInfo(TBoldClassTypeInfo ClassTypeInfo)
    {
        fClassTypeInfoOverride = true;
        fClassTypeInfo = ClassTypeInfo;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldObject BoldObject => GetBoldObject();

    [Delphi(PortStatus.Ported)]
    protected TBoldClassTypeInfo ClassTypeInfo => GetClassTypeInfo();

    [Delphi(PortStatus.Ported)]
    protected TabControl PageControl => GetPageControl();

    [Delphi(PortStatus.Ported)]
    protected bool ClassTypeInfoOverride => fClassTypeInfoOverride;

    [Delphi(PortStatus.Ported)]
    protected Panel? ButtonPanel
    {
        get => fButtonPanel;
        set => fButtonPanel = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldDropTarget? DragSource => fDragSource;

    /// <summary>The members shown; the object's class's members unless set.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldMemberRTInfoList MemberRTInfoList
    {
        get => GetMemberRTInfoList();
        set => SetMemberRTInfoList(value);
    }

    /// <summary>The apply policy of the edits and of the buttons (exit by default).</summary>
    public new TBoldApplyPolicy ApplyPolicy
    {
        get => base.ApplyPolicy;
        set => base.ApplyPolicy = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool DisplaySingleRoleButtons
    {
        get => fDisplaySingleRoleButtons;
        set => fDisplaySingleRoleButtons = value;
    }

    /// <summary>The history's derived handle, with Delphi's TComponent.Tag that says whether non-embedded links count.</summary>
    private sealed class TObjectHistoryHandle(IContainer Owner) : TBoldDerivedHandle(Owner)
    {
        public int Tag { get; set; }
    }
}

/// <summary>A button for a method of the class; Delphi's method call is commented out, so a click does nothing.</summary>
[Delphi(PortStatus.Ported)]
public class TMethodButton : Button
{
    private readonly TBoldRootedHandle fObjectHandle;
    private readonly int fMethodNumber;
    private readonly TextBox? fResultEditBox;
    private List<object>? fValueHolder;

    [Delphi(PortStatus.Ported, Symbol = "TMethodButton.CreateWithInfo", Note = "No owner; the button goes with its parent.")]
    public TMethodButton(TBoldRootedHandle ObjectHandle, int Number, TextBox? ResultEditBox)
    {
        fObjectHandle = ObjectHandle;
        fMethodNumber = Number;
        fResultEditBox = ResultEditBox;
        fValueHolder = [];
    }

    [Delphi(PortStatus.Ported, Symbol = "TMethodButton.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fValueHolder = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported, Symbol = "TMethodButton.Click", Note = "Its body is commented out in Delphi and does not call inherited: the Click event is not raised.")]
    protected override void OnClick(EventArgs e)
    {
    }

    [Delphi(PortStatus.Ported)]
    public int MethodNumber => fMethodNumber;

    [Delphi(PortStatus.Ported)]
    public TBoldRootedHandle ObjectHandle => fObjectHandle;

    [Delphi(PortStatus.Ported)]
    public TextBox? ResultEditBox => fResultEditBox;

    [Delphi(PortStatus.Ported, Note = "A list of the argument handles and edits.")]
    public List<object>? ValueHolder => fValueHolder;
}

/// <summary>The button beside a single role's edit: a press shows the role's menu, a click (by keyboard) opens the related object's form.</summary>
[Delphi(PortStatus.Ported)]
public class TRoleButton : Button
{
    /// <summary>The handle on the single link.</summary>
    internal TBoldElementHandle? BoldHandle;

    [Delphi(PortStatus.Ported)]
    public TRoleButton()
    {
        Click += NavigateToSingleRole;
    }

    [Delphi(PortStatus.Ported, Symbol = "TRoleButton.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ContextMenuStrip?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>A press shows the menu at the mouse (and no click follows).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TRoleButton.MouseDown", Note = "OnMouseDown; as in Delphi, the button's own handling (and the MouseDown event) is skipped.")]
    protected override void OnMouseDown(MouseEventArgs e) => ContextMenuStrip?.Show(MousePosition);

    /// <summary>Opens the form of the related object.</summary>
    [Delphi(PortStatus.Ported)]
    public void NavigateToSingleRole(object? Sender, EventArgs e)
    {
        if (BoldHandle?.Value is TBoldObjectReference reference && reference.BoldObject is { } anObject)
        {
            BoldAFP.AutoFormProviderRegistry().FormForElement(anObject)?.Show();
        }
    }

    /// <summary>Unlinks the related object, when the user confirms.</summary>
    [Delphi(PortStatus.Ported)]
    public void UnlinkObject(object? Sender, EventArgs e)
    {
        if (BoldHandle?.Value is TBoldObjectReference ObjectReference && ObjectReference.BoldObject is not null &&
            Dialogs.MessageDlg(sAreYouSureToUnlink, TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == global::Bold.Controls.mrYes)
        {
            ObjectReference.BoldObject = null;
        }
    }

    /// <summary>Deletes the related object, when the user confirms.</summary>
    [Delphi(PortStatus.Ported)]
    public void DeleteObject(object? Sender, EventArgs e)
    {
        if (BoldHandle?.Value is TBoldObjectReference ObjectReference && ObjectReference.BoldObject is { } related &&
            Dialogs.MessageDlg(sAreYouSureToDelete, TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == global::Bold.Controls.mrYes)
        {
            related.Delete();
        }
    }

    /// <summary>Links a new object of the other end's class (when it has no subclasses and the link is empty).</summary>
    [Delphi(PortStatus.Ported)]
    public void CreateNewObject(object? Sender, EventArgs e)
    {
        if (BoldHandle?.Value is TBoldObjectReference ObjectReference)
        {
            if (ObjectReference.BoldObject is not null)
            {
                Dialogs.ShowMessage(sClearLinkFirst);
            }
            else
            {
                TBoldClassTypeInfo otherEnd = ObjectReference.BoldRoleRTInfo!.ClassTypeInfoOfOtherEnd!;
                if (!otherEnd.HasSubclasses)
                {
                    ObjectReference.BoldObject = ObjectReference.BoldSystem!.CreateNewObjectByExpressionName(otherEnd.ExpressionName);
                }
                else
                {
                    Dialogs.ShowMessage(sCreatingNewNotSupported);
                }
            }
        }
    }
}

/// <summary>The menu of a role button: navigate, create new, add existing (disabled), unlink and delete.</summary>
[Delphi(PortStatus.Ported, Note = "A ContextMenuStrip.")]
public class TRoleMenu : ContextMenuStrip
{
    private readonly ToolStripMenuItem fmnuDelete;
    private readonly ToolStripMenuItem fmnuNavigate;
    private readonly ToolStripMenuItem fmnuUnlink;
    private readonly ToolStripMenuItem fmnuCreateNew;
    private readonly TRoleButton fRoleButton;

    [Delphi(PortStatus.Ported, Note = "The owner is the role button.")]
    public TRoleMenu(TRoleButton AOwner)
    {
        fRoleButton = AOwner;
        Opening += AdjustEnabled;

        fmnuNavigate = new ToolStripMenuItem(sNavigate);
        fmnuNavigate.Click += fRoleButton.NavigateToSingleRole;
        Items.Add(fmnuNavigate);

        Items.Add(new ToolStripSeparator());

        fmnuCreateNew = new ToolStripMenuItem(sCreateNew);
        fmnuCreateNew.Click += fRoleButton.CreateNewObject;
        Items.Add(fmnuCreateNew);

        Items.Add(new ToolStripMenuItem(sAddExisting) { Enabled = false });

        Items.Add(new ToolStripSeparator());

        fmnuUnlink = new ToolStripMenuItem(sUnlink);
        fmnuUnlink.Click += fRoleButton.UnlinkObject;
        Items.Add(fmnuUnlink);

        fmnuDelete = new ToolStripMenuItem(sDelete);
        fmnuDelete.Click += fRoleButton.DeleteObject;
        Items.Add(fmnuDelete);
    }

    /// <summary>Enables the items that apply to the link as it is.</summary>
    [Delphi(PortStatus.Ported, Note = "The menu's Opening event (Delphi's OnPopup).")]
    private void AdjustEnabled(object? sender, CancelEventArgs e)
    {
        if (fRoleButton.BoldHandle?.Value is TBoldObjectReference ObjectReference)
        {
            fmnuDelete.Enabled = ObjectReference.BoldObject is not null && ObjectReference.BoldObject.CanDelete();
            fmnuNavigate.Enabled = ObjectReference.BoldObject is not null;
            fmnuCreateNew.Enabled = ObjectReference.BoldObject is null && ObjectReference.CanModify();
            fmnuUnlink.Enabled = ObjectReference.CanModify() && ObjectReference.CanClear(null) && ObjectReference.BoldObject is not null;
        }
    }

    /// <summary>Raises Opening, as a menu shown by the user does (for code and tests that adjust the items).</summary>
    internal void Adjust() => AdjustEnabled(this, new CancelEventArgs());
}

/// <summary>An edit whose double click opens the form of the object its link refers to.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldAFEdit : TBoldEdit
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldAFEdit.DblClick", Note = "OnDoubleClick; as in Delphi, the DoubleClick event is not raised.")]
    protected override void OnDoubleClick(EventArgs e)
    {
        if (BoldHandle?.Value is TBoldObjectReference reference && reference.BoldObject is { } related)
        {
            BoldAFP.AutoFormProviderRegistry().FormForElement(related)?.Show();
        }
    }
}

/// <summary>A tab control whose tabs come up when Bold objects are dragged over them.</summary>
[Delphi(PortStatus.Ported, Note = "A TabControl that allows drops, so that WinForms raises DragOver.")]
internal class TAFPPageControl : TabControl
{
    public TAFPPageControl()
    {
        AllowDrop = true;
    }

    /// <summary>The width of an image on a tab.</summary>
    [Delphi(PortStatus.Ported)]
    protected int ImageDelta() => ImageList?.ImageSize.Width ?? 0;

    /// <summary>Selects the tab at x (from the widths of the tabs' captions).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void ActivatePage(int x, int y)
    {
        int p = -1;
        int Page = -1;
        while (p < x && Page < TabPages.Count - 1)
        {
            Page++;
            p += TextRenderer.MeasureText(TabPages[Page].Text, Font).Width + 14 + ImageDelta();
        }

        if (Page >= 0)
        {
            SelectedTab = TabPages[Page];
        }
    }

    /// <summary>Takes no drop; dragging Bold objects over the tabs selects the tab under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TAFPPageControl.DragOver", Note = "OnDragOver; as in Delphi, the DragOver event is not raised. Only the tab strip selects a tab: in Delphi the pages covered the rest.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        drgevent.Effect = DragDropEffects.None;
        if (BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0)
        {
            Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
            Rectangle pages = DisplayRectangle;
            bool overTabs = Alignment switch
            {
                TabAlignment.Bottom => p.Y >= pages.Bottom,
                TabAlignment.Left => p.X < pages.Left,
                TabAlignment.Right => p.X >= pages.Right,
                _ => p.Y < pages.Top,
            };
            if (overTabs)
            {
                ActivatePage(p.X, p.Y);
            }
        }
    }
}

/// <summary>An open auto form and its element, with the form's own close and drag-over handlers.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TAutoFormListEntry : TBoldMemoryManagedObject
{
    private readonly Form fForm;
    private readonly TBoldElement fElement;

    [Delphi(PortStatus.Ported)]
    public TAutoFormListEntry(Form Form, TBoldElement Element)
    {
        fForm = Form;
        fElement = Element;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElement Element => fElement;

    [Delphi(PortStatus.Ported)]
    public Form Form => fForm;

    [Delphi(PortStatus.Ported, Note = "A FormClosingEventHandler (setting Cancel is Delphi's caNone).")]
    public FormClosingEventHandler? OnClose { get; set; }

    [Delphi(PortStatus.Ported)]
    public DragEventHandler? OnDragOver { get; set; }
}

/// <summary>The open auto forms with their elements.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TAutoFormList : List<TAutoFormListEntry>
{
    [Delphi(PortStatus.Ported)]
    private Form GetForms(int index) => Entries(index).Form;

    [Delphi(PortStatus.Ported)]
    private TAutoFormListEntry GetEntries(int index) => this[index];

    [Delphi(PortStatus.Ported)]
    public int IndexOfForm(object? anObject) => FindIndex(entry => entry.Form == anObject);

    [Delphi(PortStatus.Ported)]
    public int IndexOfElement(object? anObject) => FindIndex(entry => entry.Element == anObject);

    [Delphi(PortStatus.Ported)]
    public void AddPair(Form Form, TBoldElement Element) => Add(new TAutoFormListEntry(Form, Element));

    [Delphi(PortStatus.Ported)]
    public void RemoveByForm(Form Form) => RemoveAt(IndexOfForm(Form));

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public Form Forms(int index) => GetForms(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TAutoFormListEntry Entries(int index) => GetEntries(index);
}

/// <summary>Unit-level variables and the initialization of BoldAFPDefault.</summary>
public static class BoldAFPDefault
{
    internal const int BOXMARGIN = 8;
    internal const int BOXSPACING = 2;
    internal const int CONTROLMARGIN = 10;
    internal const int LISTBOXLABELHEIGHT = 18;
    internal const int MAXFORMHEIGHT = 450;
    internal const int MINFORMHEIGHT = 150;
    internal const int EDITHEIGHT = 20;
    internal const int EDITWIDTH = 121;
    internal const int LISTBOXHEIGHT = 97;
    internal const int LISTBOXWIDTH = 121;
    internal const int PANELHEIGHT = 35;

    /// <summary>The grids of auto forms show a constraint column.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldShowConstraintsInAutoFormGrids = true;

    /// <summary>Object forms have a history tab (for a class that is not versioned, a note).</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldShowHistoryTabInAutoForms = true;

    /// <summary>The open auto forms.</summary>
    internal static readonly TAutoFormList G_BoldActiveAutoForms = new();

    /// <summary>The renderer of the attribute edits: read-only members in the inactive caption text color.</summary>
    internal static readonly TBoldAsStringRenderer fReadOnlyStringRenderer = new() { OnSetColor = TBoldDefaultObjectAutoFormProvider.BoldAsStringRenderer1SetColor };

    /// <summary>Registers the default providers: objects, systems, attributes, lists of attributes and lists of objects.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called by BoldWinForms.Initialize; the form list and the renderer are made with the class.")]
    internal static void Initialization()
    {
        TBoldAutoFormProviderRegistry registry = BoldAFP.AutoFormProviderRegistry();
        registry.RegisterProvider(TBoldValueTypeSet.bvtClass, typeof(TBoldObject), typeof(TBoldDefaultObjectAutoFormProvider));
        registry.RegisterProvider(TBoldValueTypeSet.bvtSystem, typeof(TBoldSystem), typeof(TBoldDefaultSystemAutoFormProvider));
        registry.RegisterProvider(TBoldValueTypeSet.bvtAttr, typeof(TBoldMember), typeof(TBoldDefaultAttributeAutoFormProvider));
        registry.RegisterListProvider(TBoldValueTypeSet.bvtAttr, typeof(TBoldMember), typeof(TBoldDefaultAttributeListAutoFormProvider));
        registry.RegisterListProvider(TBoldValueTypeSet.bvtClass, typeof(TBoldObjectList), typeof(TBoldDefaultObjectListAutoFormProvider));
    }
}
