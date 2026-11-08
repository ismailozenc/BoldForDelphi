// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
// The model editor's form: the tree of the model, the page of the element on show, the menus, the tool bar, the status
// bar and the plug-ins. What it does to the model is TBoldModelEditController's (Bold.ModelEditor.Core); its pages are
// built from TBoldModelEditPages there.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>The model editor's form.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code: Delphi's Bold-aware controls on the UML object space are WinForms controls on the UML graph, bound by TBoldModelEditPages; the tree is built from the model.")]
public class TBoldModelEditFrm : Form
{
    /// <summary>A node of the tree that groups elements: "Logical View", "Associations" or "DataTypes".</summary>
    public sealed record TTreeFolder(TBoldUMLObject Owner, string Caption);

    /// <summary>A page of the editor: the controls of its fields and its lists.</summary>
    private sealed class TPageView(TBoldModelEditPageInfo Info, Panel Panel, Label InfoLabel)
    {
        public TBoldModelEditPageInfo Info { get; } = Info;

        public Panel Panel { get; } = Panel;

        public Label InfoLabel { get; } = InfoLabel;

        public SplitContainer? Split { get; set; }

        /// <summary>The height the fields should have, applied when the page shows.</summary>
        public int? WantedSplit { get; set; }

        public GroupBox? Group { get; set; }

        public Dictionary<TBoldModelEditField, (Label? Label, Control Control, Button? Button)> Fields { get; } = [];

        public List<(TBoldModelEditGrid Grid, DataGridView View)> Grids { get; } = [];
    }

    private const string LogicalView = "Logical View";
    private const string AssociationsFolder = "Associations";
    private const string DataTypesFolder = "DataTypes";

    public readonly TBoldModelEditController Controller = new();
    public readonly MenuStrip MainMenu1;
    public readonly ToolStripMenuItem mnuFile;
    public readonly ToolStripMenuItem Clear1;
    public readonly ToolStripSeparator N1;
    public readonly ToolStripSeparator N4;
    public readonly ToolStripMenuItem OpenFile1;
    public readonly ToolStripMenuItem SaveFileAs1;
    public readonly ToolStripSeparator N8;
    public readonly ToolStripMenuItem Exit1;
    public readonly ToolStripMenuItem Edit1;
    public readonly ToolStripMenuItem Cut1;
    public readonly ToolStripMenuItem Copy1;
    public readonly ToolStripMenuItem Paste1;
    public readonly ToolStripMenuItem View1;
    public readonly ToolStripMenuItem mnuShowAttributes;
    public readonly ToolStripMenuItem mnuShowRoles;
    public readonly ToolStripMenuItem mnuShowOperations;
    public readonly ToolStripMenuItem Loggform1;
    public readonly ToolStripMenuItem mnuAdvanced;
    public readonly ToolStripMenuItem Model1;
    public readonly ToolStripMenuItem Deletexxx2;
    public readonly ToolStripSeparator N11;
    public readonly ToolStripMenuItem NewClass2;
    public readonly ToolStripMenuItem NewAssociation2;
    public readonly ToolStripMenuItem NewDataType2;
    public readonly ToolStripMenuItem AddSubclass2;
    public readonly ToolStripMenuItem InsertSuperclass1;
    public readonly ToolStripMenuItem NewAttribute2;
    public readonly ToolStripMenuItem NewOperation2;
    public readonly ToolStripMenuItem NewParameter2;
    public readonly ToolStripMenuItem NewQualifier2;
    public readonly ToolStripMenuItem Tools1;
    public readonly ToolStripMenuItem mnuConsistencycheck;
    public readonly ToolStripMenuItem N3;
    public readonly ToolStripSeparator N6;
    public readonly ToolStripSeparator N5;
    public readonly ToolStripMenuItem FetchDatatypes;
    public readonly ToolStripMenuItem Flatten;
    public readonly ToolStripMenuItem Boldifymodel1;
    public readonly ToolStripSeparator N2;
    public readonly ToolStripMenuItem Help1;
    public readonly ToolStripMenuItem Contents1;
    public readonly ContextMenuStrip popTree;
    public readonly ToolStripMenuItem DeleteXxx1;
    public readonly ToolStripSeparator N10;
    public readonly ToolStripMenuItem NewPackage1;
    public readonly ToolStripMenuItem NewClass1;
    public readonly ToolStripMenuItem NewAssociation1;
    public readonly ToolStripMenuItem NewDatatype1;
    public readonly ToolStripMenuItem AddSubclass1;
    public readonly ToolStripMenuItem InsertSuperclass2;
    public readonly ToolStripMenuItem NewAttribute1;
    public readonly ToolStripMenuItem NewOperation1;
    public readonly ToolStripMenuItem NewParameter1;
    public readonly ToolStripMenuItem NewQualifier1;
    public readonly ToolStripSeparator N9;
    public readonly ToolStripMenuItem mnuOverrideFrameworkMethods;
    public readonly ToolStripMenuItem mnuOverrideModelMethods;
    public readonly ToolStripMenuItem mnuOverrideInAllSubclasses;
    public readonly ToolStrip ToolBar1;
    public readonly ToolStripSeparator Separator1;
    public readonly ToolStripSeparator Separator2;
    public readonly ToolStripButton tbnFilterAttributes;
    public readonly ToolStripButton tbnFilterRoles;
    public readonly ToolStripButton tbnFilterOperations;
    public readonly ToolStripButton ToolButton1;
    public readonly StatusStrip StatusBar1;
    public readonly ToolStripStatusLabel StatusPanel0;
    public readonly ToolStripStatusLabel StatusPanel1;
    public readonly TreeView BoldTreeView1;
    public readonly Splitter Splitter2;
    public readonly Panel pcModelEdit;
    public readonly ImageList ilTreeView;
    public readonly ToolTip apeHintCatcher;
    public readonly TBoldLogFrame fLogframe;

    private readonly Dictionary<TBoldModelEditPage, TPageView> fPageViews = [];
    private IReadOnlyList<TUMLPlugIn> fPlugInList = [];
    private readonly HashSet<string> fExpanded = [];
    private readonly Bitmap imgHint;
    private readonly Bitmap imgWarning;
    private TreeNode? GreyNode;
    private TBoldUMLObject? fDragged;
    private bool fRefreshing;
    private bool fRefreshPosted;
    private bool fSelecting;
    private bool fTreeBuilt;

    public TBoldModelEditFrm()
    {
        SuspendLayout();
        Text = "Bold UML Model Editor";
        ClientSize = new Size(829, 600);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        Clear1 = MenuItem("&Clear", Clear1Click);
        N1 = new ToolStripSeparator { Name = "N1" };
        N4 = new ToolStripSeparator { Name = "N4" };
        OpenFile1 = MenuItem("&Open File", OpenFile1Click, "Open");
        SaveFileAs1 = MenuItem("Save File &As...", SaveFileAs1Click, "Save");
        N8 = new ToolStripSeparator { Name = "N8" };
        Exit1 = MenuItem("&Exit", Exit1Click, "Exit");
        mnuFile = new ToolStripMenuItem("&File") { Name = "mnuFile" };
        mnuFile.DropDownItems.AddRange([Clear1, N1, N4, OpenFile1, SaveFileAs1, N8, Exit1]);

        Cut1 = MenuItem("Cu&t", Cut1Click, "Cut", Keys.Control | Keys.X);
        Copy1 = MenuItem("&Copy", Copy1Click, "Copy", Keys.Control | Keys.C);
        Paste1 = MenuItem("&Paste", Paste1Click, "Paste", Keys.Control | Keys.V);
        Edit1 = new ToolStripMenuItem("&Edit") { Name = "Edit1" };
        Edit1.DropDownItems.AddRange([Cut1, Copy1, Paste1]);

        mnuShowAttributes = MenuItem("&Attributes", mnuShowAttributesClick, "Attribute");
        mnuShowRoles = MenuItem("&Roles", mnuShowRolesClick, "AssociationEnd");
        mnuShowOperations = MenuItem("&Operations", mnuShowOperationsClick, "Operation");
        mnuShowAttributes.Checked = mnuShowRoles.Checked = mnuShowOperations.Checked = true;
        Loggform1 = MenuItem("Log form", Loggform1Click, null, Keys.Control | Keys.L);
        mnuAdvanced = MenuItem("A&dvanced", mnuAdvancedClick, null, Keys.Control | Keys.A);
        View1 = new ToolStripMenuItem("&View") { Name = "View1" };
        View1.DropDownItems.AddRange([mnuShowAttributes, mnuShowRoles, mnuShowOperations, new ToolStripSeparator(), Loggform1, new ToolStripSeparator(), mnuAdvanced]);

        Deletexxx2 = MenuItem("&Delete xxx", DeleteXxx1Click, "Delete");
        N11 = new ToolStripSeparator { Name = "N11" };
        NewClass2 = MenuItem("New &Class", NewClass1Click, null, Keys.Control | Keys.Alt | Keys.C);
        NewAssociation2 = MenuItem("New &Association", NewAssociation1Click, null, Keys.Control | Keys.Alt | Keys.N);
        NewDataType2 = MenuItem("New DataType", NewDatatype1Click, null, Keys.Control | Keys.Alt | Keys.D);
        AddSubclass2 = MenuItem("Add Subclass", AddSubclass1Click, null, Keys.Control | Keys.Alt | Keys.S);
        InsertSuperclass1 = MenuItem("Insert Superclass", InsertSuperclass1Click);
        NewAttribute2 = MenuItem("New A&ttribute", NewAttribute1Click, null, Keys.Control | Keys.Alt | Keys.A);
        NewOperation2 = MenuItem("New &Operation", NewOperation1Click, null, Keys.Control | Keys.Alt | Keys.O);
        NewParameter2 = MenuItem("New &Parameter", NewParameter1Click, null, Keys.Control | Keys.Alt | Keys.P);
        NewQualifier2 = MenuItem("New &Qualifier", NewQualifier1Click, null, Keys.Control | Keys.Alt | Keys.Q);
        Model1 = new ToolStripMenuItem("&Model") { Name = "Model1" };
        Model1.DropDownItems.AddRange([Deletexxx2, N11, NewClass2, NewAssociation2, NewDataType2, AddSubclass2, InsertSuperclass1, NewAttribute2, NewOperation2, NewParameter2, NewQualifier2]);
        Model1.DropDownOpening += popTreePopup;

        mnuConsistencycheck = MenuItem("&Consistency Check", mnuConsistencycheckClick);
        N3 = MenuItem("&Edit tagged values", N3Click, null, Keys.Control | Keys.T);
        N6 = new ToolStripSeparator { Name = "N6" };
        N5 = new ToolStripSeparator { Name = "N5" };
        FetchDatatypes = MenuItem("Fetch data types from TypeNameHandle", EnsureTypesFromTypeNameHandle);
        Flatten = MenuItem("Flatten package structure", FlattenClick);
        Boldifymodel1 = MenuItem("xxx", Boldifymodel1Click);
        N2 = new ToolStripSeparator { Name = "N2" };
        Tools1 = new ToolStripMenuItem("&Tools") { Name = "Tools1" };
        Tools1.DropDownItems.AddRange([mnuConsistencycheck, N3, N6, N5, FetchDatatypes, Flatten, Boldifymodel1, N2]);
        Tools1.DropDownOpening += Tools1Click;

        Contents1 = MenuItem("&Contents", Contents1Click, "Help");
        Help1 = new ToolStripMenuItem("&Help") { Name = "Help1" };
        Help1.DropDownItems.Add(Contents1);

        MainMenu1 = new MenuStrip { Name = "MainMenu1" };
        MainMenu1.Items.AddRange([mnuFile, Edit1, View1, Model1, Tools1, Help1]);
        MainMenuStrip = MainMenu1;

        DeleteXxx1 = MenuItem("Delete xxx", DeleteXxx1Click);
        DeleteXxx1.ShortcutKeyDisplayString = "Del";
        N10 = new ToolStripSeparator { Name = "N10" };
        NewPackage1 = MenuItem("New Package", NewPackage1Click);
        NewClass1 = MenuItem("New Class", NewClass1Click);
        NewAssociation1 = MenuItem("New Association", NewAssociation1Click);
        NewDatatype1 = MenuItem("New Datatype", NewDatatype1Click);
        AddSubclass1 = MenuItem("Add Subclass", AddSubclass1Click);
        InsertSuperclass2 = MenuItem("Insert Superclass", InsertSuperclass1Click);
        NewAttribute1 = MenuItem("New Attribute", NewAttribute1Click);
        NewOperation1 = MenuItem("New Operation", NewOperation1Click);
        NewParameter1 = MenuItem("New Parameter", NewParameter1Click);
        NewQualifier1 = MenuItem("New Qualifier", NewQualifier1Click);
        N9 = new ToolStripSeparator { Name = "N9" };
        mnuOverrideFrameworkMethods = new ToolStripMenuItem("Override Framework Methods") { Name = "mnuOverrideFrameworkMethods" };
        mnuOverrideModelMethods = new ToolStripMenuItem("Override Model methods") { Name = "mnuOverrideModelMethods" };
        mnuOverrideInAllSubclasses = MenuItem("Override in all subclasses", mnuOverrideInAllSubclassesClick);
        popTree = new ContextMenuStrip { Name = "popTree" };
        popTree.Items.AddRange([DeleteXxx1, N10, NewPackage1, NewClass1, NewAssociation1, NewDatatype1, AddSubclass1, InsertSuperclass2, NewAttribute1,
            NewOperation1, NewParameter1, NewQualifier1, N9, mnuOverrideFrameworkMethods, mnuOverrideModelMethods, mnuOverrideInAllSubclasses]);
        popTree.Opening += (_, _) => popTreePopup(popTree, EventArgs.Empty);

        Separator1 = new ToolStripSeparator { Name = "Separator1" };
        Separator2 = new ToolStripSeparator { Name = "Separator2" };
        tbnFilterAttributes = new ToolStripButton("&Attributes", EditorImage("Attribute"), tbnFilterAttributesClick)
        {
            Name = "tbnFilterAttributes", CheckOnClick = false, Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = "Show Attributes",
        };
        tbnFilterRoles = new ToolStripButton("Roles", EditorImage("AssociationEnd"), tbnFilterRolesClick)
        {
            Name = "tbnFilterRoles", Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = "Show Roles",
        };
        tbnFilterOperations = new ToolStripButton("Operations", EditorImage("Operation"), tbnFilterOperationsClick)
        {
            Name = "tbnFilterOperations", Checked = true, DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = "Show Operations",
        };
        ToolButton1 = new ToolStripButton(":-)", null, ToolButton1Click) { Name = "ToolButton1", Visible = false };
        ToolBar1 = new ToolStrip { Name = "ToolBar1", ImageScalingSize = new Size(16, 16) };
        ToolBar1.Items.AddRange([Separator1, Separator2, tbnFilterAttributes, tbnFilterRoles, tbnFilterOperations, ToolButton1]);

        imgHint = new Bitmap(SystemIcons.Information.ToBitmap(), 16, 16);
        imgWarning = new Bitmap(SystemIcons.Warning.ToBitmap(), 16, 16);
        StatusPanel0 = new ToolStripStatusLabel { Name = "StatusPanel0", AutoSize = false, Width = 20 };
        StatusPanel1 = new ToolStripStatusLabel { Name = "StatusPanel1", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        StatusBar1 = new StatusStrip { Name = "StatusBar1" };
        StatusBar1.Items.AddRange([StatusPanel0, StatusPanel1]);
        StatusBar1.ShowItemToolTips = true;
        foreach (ToolStripStatusLabel panel in new[] { StatusPanel0, StatusPanel1 })
        {
            panel.DoubleClickEnabled = true;
            panel.DoubleClick += mnuConsistencycheckClick;
            panel.ToolTipText = "Doubleclick here to validate model";
        }

        ilTreeView = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
        foreach (string image in new[] { "Model", "Package", "Class", "Attribute", "Operation", "Parameter", "Association", "Associations", "AssociationEnd", "DataType" })
        {
            ilTreeView.Images.Add(image, EditorImage(image) ?? new Bitmap(16, 16));
        }

        BoldTreeView1 = new TreeView
        {
            Name = "BoldTreeView1", Dock = DockStyle.Left, Width = 220, ImageList = ilTreeView, HideSelection = false, LabelEdit = true, AllowDrop = true,
            ContextMenuStrip = popTree,
        };
        Splitter2 = new Splitter { Name = "Splitter2", Dock = DockStyle.Left, Width = 4 };
        pcModelEdit = new Panel { Name = "pcModelEdit", Dock = DockStyle.Fill };
        apeHintCatcher = new ToolTip();
        fLogframe = new TBoldLogFrame();

        Controls.AddRange([pcModelEdit, Splitter2, BoldTreeView1, fLogframe, ToolBar1, StatusBar1, MainMenu1]);

        BoldTreeView1.AfterSelect += BoldTreeView1Change;
        BoldTreeView1.KeyDown += BoldTreeView1KeyDown;
        BoldTreeView1.AfterExpand += BoldTreeView1Expanded;
        BoldTreeView1.AfterExpand += (_, e) => fExpanded.Add(KeyOf(e.Node!));
        BoldTreeView1.AfterCollapse += (_, e) => fExpanded.Remove(KeyOf(e.Node!));
        BoldTreeView1.ItemDrag += BoldTreeView1StartDrag;
        BoldTreeView1.DragOver += BoldTreeView1DragOver;
        BoldTreeView1.DragDrop += BoldTreeView1DragDrop;
        BoldTreeView1.BeforeLabelEdit += BoldTreeView1BeforeLabelEdit;
        BoldTreeView1.AfterLabelEdit += BoldTreeView1AfterLabelEdit;
        BoldTreeView1.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                BoldTreeView1.SelectedNode = e.Node; // right-click selects
            }
        };
        Splitter2.SplitterMoving += Splitter2CanResize;
        KeyDown += FormKeyDown;
        FormClosed += FormClose;
        Activated += FormActivate;
        Deactivate += FormDeactivate;
        Shown += FormShow;
        Controller.CurrentElementChanged += CurrentElementChanged;
        Controller.ModelChanged += _ => PostRefresh();
        Controller.StatusChanged += UpdateStatusbar;
        Controller.AdvancedModeChanged += RefreshPage;
        ResumeLayout(false);
        PerformLayout();
        FormCreate();
    }

    private static ToolStripMenuItem MenuItem(string Text, EventHandler Click, string? Image = null, Keys Shortcut = Keys.None)
    {
        var item = new ToolStripMenuItem(Text, Image is null ? null : EditorImage(Image), Click);
        if (Shortcut != Keys.None)
        {
            item.ShortcutKeys = Shortcut;
        }

        return item;
    }

    /// <summary>A bitmap of the editor, embedded from the repository's Images; null when there is none of the name.</summary>
    public static Bitmap? EditorImage(string Name)
    {
        using Stream? stream = typeof(TBoldModelEditFrm).Assembly.GetManifestResourceStream("Bold.EditorImages." + Name);
        if (stream is null)
        {
            return null;
        }

        var bitmap = new Bitmap(stream);
        bitmap.MakeTransparent();
        return bitmap;
    }

    /// <summary>The model component whose model the form edits (the controller's).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldModel? ModelHandle
    {
        get => Controller.ModelHandle;
        set => SetModelHandle(value!);
    }

    /// <summary>The plug-ins on the menus and the tool bar.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PlugInList")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TUMLPlugIn> PlugInList
    {
        get => fPlugInList;
        set => SetPlugInList(value);
    }

    /// <summary>The split of a page between its fields and its lists, by the name of Delphi's scroll box ("sbClass").</summary>
    public SplitContainer? ScrollBox(string Name) => SplitView(Name)?.Split;

    private TPageView? SplitView(string Name) => fPageViews.Values.FirstOrDefault(v => v.Split?.Name == Name);

    /// <summary>The height of the fields of a page above its lists, by the name of Delphi's scroll box ("sbClass").</summary>
    public int SplitDistance(string Name) => SplitView(Name) is { Split: { } split } view ? view.WantedSplit ?? split.SplitterDistance : 0;

    /// <summary>Sets the height of the fields of a page above its lists; it applies when the page shows.</summary>
    public void SetSplitDistance(string Name, int Distance)
    {
        if (SplitView(Name) is { } view && Distance > 0)
        {
            view.WantedSplit = Distance;
            ApplyWantedSplit(view);
        }
    }

    private static void ApplyWantedSplit(TPageView View)
    {
        if (View.Split is { } split && View.WantedSplit is int wanted && View.Panel.Visible &&
            wanted >= split.Panel1MinSize && wanted + split.SplitterWidth + split.Panel2MinSize <= split.Height)
        {
            split.SplitterDistance = wanted;
            View.WantedSplit = null;
        }
    }

    /// <summary>The page on show.</summary>
    public TBoldModelEditPage ActivePage => Controller.CurrentPage;

    private void SetModelHandle(TBoldModel Value)
    {
        if (Controller.ModelHandle is { } old)
        {
            old.Disposed -= Notification;
        }

        Controller.ModelHandle = Value;
        Value.Disposed += Notification;
        TfrmValidation.EnsureValidationForm(Value, this, JumpToElement);
        fTreeBuilt = false;
        RefreshAll();
    }

    /// <summary>The model component went: so does the form.</summary>
    [Delphi(PortStatus.Ported, Note = "Hears the model component's Disposed event.")]
    private void Notification(object? Sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            Dispose();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Builds the pages from TBoldModelEditPages; the combo boxes' values are the pages'.")]
    private void FormCreate()
    {
        TBoldUMLBldLink.EnsureRegistered();
        foreach (TBoldModelEditPageInfo page in TBoldModelEditPages.All)
        {
            fPageViews[page.Page] = BuildPage(page);
        }

        SetCheckBoxHints();
        UpdateStatusbar();
    }

    [Delphi(PortStatus.Ported)]
    private void FormShow(object? Sender, EventArgs e)
    {
        RefreshAll();
        BoldTreeView1.Focus();
    }

    [Delphi(PortStatus.Ported, Note = "WinForms frees the closed form; what is being edited is applied first.")]
    private void FormClose(object? Sender, FormClosedEventArgs e) => ApplyGUI();

    [Delphi(PortStatus.Ported, Note = "The form's Dispose: saves its settings and lets the constraint editor go.")]
    private void FormDestroy()
    {
        TModelEdit.SaveFormsettingsToRegistry(this);
        if (TfrmValidation.G_ValidationFormDefaultOwner == this)
        {
            TfrmValidation.G_ValidationFormDefaultOwner = null;
        }

        FreeItemsInMenu(mnuOverrideFrameworkMethods);
        FreeItemsInMenu(mnuOverrideModelMethods);
        TfrmBoldUMLConstraintEditor.EnsureFreeConstraintEditor();
        if (Controller.ModelHandle is { } model)
        {
            model.Disposed -= Notification;
        }

        Controller.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FormDestroy();
            imgHint.Dispose();
            imgWarning.Dispose();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported, Note = "The validation forms made where no form is at hand are this form's.")]
    private void FormActivate(object? Sender, EventArgs e) => TfrmValidation.G_ValidationFormDefaultOwner = this;

    [Delphi(PortStatus.Ported)]
    private void FormDeactivate(object? Sender, EventArgs e) => ApplyGUI();

    /// <summary>Escape forgets what was cut.</summary>
    [Delphi(PortStatus.Ported)]
    private void FormKeyDown(object? Sender, KeyEventArgs e)
    {
        if (Controller.UMLObjectToCopyOrMove is not null && e.KeyCode == Keys.Escape)
        {
            UngreyNode();
            Controller.CancelCut();
        }
    }

    /// <summary>Applies what is being edited in the page.</summary>
    [Delphi(PortStatus.Ported, Note = "Validates the control being edited, which writes its value, where Delphi applied the Bold-aware controls' queue.")]
    public void ApplyGUI() => Validate();

    /// <summary>A control's long hint shows in the status bar.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the hint of the control the mouse is over.")]
    private void apeHintCatcherHint(string Hint) => StatusPanel1.Text = Hint;

    /// <summary>The hint of each check box is its caption.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetCheckBoxHints()
    {
        foreach (TPageView view in fPageViews.Values)
        {
            foreach ((TBoldModelEditField field, (Label? _, Control control, Button? _)) in view.Fields)
            {
                if (control is CheckBox box && field.Hint == "")
                {
                    apeHintCatcher.SetToolTip(box, box.Text.Replace("&", "", StringComparison.Ordinal));
                }
            }
        }
    }

    // the pages

    private TPageView BuildPage(TBoldModelEditPageInfo Info)
    {
        var panel = new Panel { Name = Info.Name, Dock = DockStyle.Fill, Visible = false };
        var infoCaption = new Label { Text = Info.InfoCaption, AutoSize = true, Location = new Point(6, 6) };
        var infoLabel = new Label { AutoSize = true, Location = new Point(90, 6), Font = new Font(Font, FontStyle.Bold) };
        var header = new Panel { Dock = DockStyle.Top, Height = 26 };
        header.Controls.AddRange([infoCaption, infoLabel]);
        var view = new TPageView(Info, panel, infoLabel);

        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, Padding = new Padding(4) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        var checks = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        TableLayoutPanel? groupTable = null;
        foreach (TBoldModelEditField field in Info.Fields)
        {
            Control control = MakeFieldControl(field);
            Label? label = null;
            Button? button = null;
            if (field.Hint != "")
            {
                apeHintCatcher.SetToolTip(control, field.Hint);
                control.MouseEnter += (_, _) => apeHintCatcherHint(field.Hint);
            }

            if (field.Group is not null)
            {
                if (groupTable is null)
                {
                    groupTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
                    groupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                    groupTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    view.Group = new GroupBox { Name = field.Group, Text = "Attribute kind Delphi features", AutoSize = true, Dock = DockStyle.Fill };
                    view.Group.Controls.Add(groupTable);
                }

                if (control is CheckBox)
                {
                    groupTable.Controls.Add(control);
                    groupTable.SetColumnSpan(control, 2);
                }
                else
                {
                    label = new Label { Text = field.Caption, AutoSize = true, Anchor = AnchorStyles.Left };
                    groupTable.Controls.AddRange([label, control]);
                }
            }
            else if (control is CheckBox)
            {
                checks.Controls.Add(control);
            }
            else
            {
                label = new Label { Text = field.Caption, AutoSize = true, Anchor = AnchorStyles.Left };
                table.Controls.Add(label);
                table.Controls.Add(control);
                if (field.Button is not null)
                {
                    button = new Button { Name = field.Button, Text = "…", Width = 26, Height = 22 };
                    string name = field.Button;
                    button.Click += (_, _) => FieldButtonClick(name);
                    table.Controls.Add(button);
                }
                else
                {
                    table.Controls.Add(new Label { AutoSize = true });
                }
            }

            if (label is not null)
            {
                label.Click += (_, _) => control.Focus();
            }

            view.Fields[field] = (label, control, button);
        }

        table.Controls.Add(checks);
        table.SetColumnSpan(checks, 3);
        if (view.Group is not null)
        {
            table.Controls.Add(view.Group);
            table.SetColumnSpan(view.Group, 3);
        }

        var fields = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        fields.Controls.Add(table);
        if (Info.Grids.Count > 0)
        {
            var tabs = new TabControl { Name = $"pc{Info.Page}Tabs", Dock = DockStyle.Fill };
            foreach (TBoldModelEditGrid grid in Info.Grids)
            {
                var page = new TabPage(grid.Caption.Replace("&", "", StringComparison.Ordinal)) { Name = TabSheetName(grid.Name) };
                DataGridView gridView = MakeGrid(grid);
                page.Controls.Add(gridView);
                tabs.TabPages.Add(page);
                view.Grids.Add((grid, gridView));
            }

            string splitName = Info.Page switch
            {
                TBoldModelEditPage.Model => "sbModel",
                TBoldModelEditPage.Class => "sbClass",
                TBoldModelEditPage.Operation => "sbOperation",
                TBoldModelEditPage.Association => "sbAssociation",
                TBoldModelEditPage.AssociationEnd => "sbAssociationEnd",
                _ => "sb" + Info.Page,
            };
            view.Split = new SplitContainer { Name = splitName, Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, FixedPanel = FixedPanel.Panel1 };
            view.WantedSplit = 300;
            view.Split.Panel1.Controls.Add(fields);
            view.Split.Panel2.Controls.Add(tabs);
            panel.Controls.AddRange([view.Split, header]);
        }
        else
        {
            panel.Controls.AddRange([fields, header]);
        }

        pcModelEdit.Controls.Add(panel);
        return view;
    }

    /// <summary>The name of the tab sheet of a grid in Delphi's form (the grid has a name of its own).</summary>
    private static string TabSheetName(string GridName) => GridName switch
    {
        "BoldGrid1" => "TabSheet1",
        "BoldGrid2" => "TabSheet2",
        "BoldGrid3" => "TabSheet3",
        "BoldGrid4" => "TabSheet4",
        "BoldGrid5" => "TabSheet5",
        "BoldGrid6" => "TabSheet6",
        "BoldGrid7" => "TabSheet7",
        "BoldGrid9" => "TabSheet8",
        "BoldGrid8" => "TabSheet11",
        "BoldGrid10" => "TabSheet12",
        _ => "ts" + GridName,
    };

    private Control MakeFieldControl(TBoldModelEditField Field)
    {
        switch (Field.Kind)
        {
            case TBoldModelEditFieldKind.CheckBox:
                var check = new CheckBox { Name = Field.Name, Text = Field.Caption, AutoSize = true, AutoCheck = false };
                check.Click += (_, _) => CheckClicked(Field);
                return check;
            case TBoldModelEditFieldKind.Choice:
            case TBoldModelEditFieldKind.EditableChoice:
                var combo = new ComboBox
                {
                    Name = Field.Name, Dock = DockStyle.Fill,
                    DropDownStyle = Field.Kind == TBoldModelEditFieldKind.Choice ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
                };
                combo.SelectionChangeCommitted += (_, _) => Commit(Field, combo.SelectedItem as string ?? combo.Text);
                if (Field.Kind == TBoldModelEditFieldKind.EditableChoice)
                {
                    combo.Validated += (_, _) => Commit(Field, combo.Text);
                }

                return combo;
            default:
                var box = new TextBox { Name = Field.Name, Dock = DockStyle.Fill };
                box.Validated += (_, _) => Commit(Field, box.Text);
                box.KeyDown += (_, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        Commit(Field, box.Text);
                        e.SuppressKeyPress = true;
                    }
                };
                return box;
        }
    }

    private DataGridView MakeGrid(TBoldModelEditGrid Grid)
    {
        var view = new DataGridView
        {
            Name = Grid.Name, Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersWidth = 24,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        };
        foreach (TBoldModelEditField column in Grid.Columns)
        {
            DataGridViewColumn gridColumn = column.Kind == TBoldModelEditFieldKind.CheckBox
                ? new DataGridViewCheckBoxColumn { ThreeState = false }
                : new DataGridViewTextBoxColumn();
            gridColumn.Name = column.Name;
            gridColumn.HeaderText = column.Caption;
            gridColumn.ReadOnly = column.ReadOnly;
            view.Columns.Add(gridColumn);
        }

        view.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (view.IsCurrentCellDirty && view.CurrentCell is DataGridViewCheckBoxCell)
            {
                view.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        view.CellValueChanged += (_, e) => GridCellChanged(Grid, view, e);
        view.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex < 0 || view.Columns[e.ColumnIndex].ReadOnly) && view.Rows[e.RowIndex].Tag is TBoldUMLObject element)
            {
                JumpToElement(this, element);
            }
        };
        return view;
    }

    private TPageView? CurrentPageView => fPageViews.GetValueOrDefault(Controller.CurrentPage);

    /// <summary>Writes a field's value to the element on show.</summary>
    private void Commit(TBoldModelEditField Field, string Value)
    {
        if (fRefreshing || Field.Set is not { } set || Controller.CurrentElement is not { } element || Field.Get(element) == Value)
        {
            return;
        }

        Guarded(() => set(element, Value));
    }

    private void CheckClicked(TBoldModelEditField Field)
    {
        if (fRefreshing || Field.SetChecked is not { } setChecked || Controller.CurrentElement is not { } element || !Field.MayModify(element))
        {
            return;
        }

        bool? state = Field.GetChecked(element);
        Guarded(() => setChecked(element, !(state ?? false)));
    }

    private void GridCellChanged(TBoldModelEditGrid Grid, DataGridView View, DataGridViewCellEventArgs e)
    {
        if (fRefreshing || e.RowIndex < 0 || e.ColumnIndex < 0 || View.Rows[e.RowIndex].Tag is not TBoldUMLObject row)
        {
            return;
        }

        TBoldModelEditField column = Grid.Columns[e.ColumnIndex];
        object? value = View.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        if (column.Kind == TBoldModelEditFieldKind.CheckBox)
        {
            if (column.SetChecked is { } setChecked)
            {
                Guarded(() => setChecked(row, value is true));
            }
        }
        else if (column.Set is { } set)
        {
            Guarded(() => set(row, value?.ToString() ?? ""));
        }
    }

    private void Guarded(Action Change)
    {
        try
        {
            Change();
        }
        catch (Exception e) when (e is EBold or ArgumentException or InvalidOperationException)
        {
            Dialogs.ShowMessage(e.Message);
        }

        RefreshAll();
    }

    /// <summary>Shows the page of the element on show and selects its node.</summary>
    private void CurrentElementChanged()
    {
        foreach ((TBoldModelEditPage page, TPageView view) in fPageViews)
        {
            view.Panel.Visible = page == Controller.CurrentPage;
            ApplyWantedSplit(view);
        }

        RefreshPage();
        UpdateMenus();
        if (!fSelecting && Controller.CurrentElement is { } element && ElementOf(BoldTreeView1.SelectedNode) != element)
        {
            JumpToElement(this, element);
        }
    }

    /// <summary>Shows the values of the element on show in its page.</summary>
    public void RefreshPage()
    {
        if (CurrentPageView is not { } view || Controller.CurrentElement is not { } element || IsDisposed)
        {
            return;
        }

        fRefreshing = true;
        try
        {
            view.InfoLabel.Text = view.Info.Info(element);
            foreach ((TBoldModelEditField field, (Label? label, Control control, Button? button)) in view.Fields)
            {
                bool shows = field.Shows(Controller, element);
                control.Visible = shows;
                label?.Visible = shows;
                button?.Visible = shows;
                if (!shows)
                {
                    continue;
                }

                control.Enabled = field.IsEnabled(element);
                if (field.Name == "cmbSuperClass")
                {
                    control.Enabled = TBoldModelEditPages.pcModelEditChange(element);
                }

                if (control.ContainsFocus && control is not CheckBox)
                {
                    continue; // the user is typing in it
                }

                switch (control)
                {
                    case CheckBox check:
                        bool? state = field.GetChecked(element);
                        check.ThreeState = state is null;
                        check.CheckState = state is null ? CheckState.Indeterminate : state.Value ? CheckState.Checked : CheckState.Unchecked;
                        check.Enabled = control.Enabled && field.SetChecked is not null && field.MayModify(element);
                        break;
                    case ComboBox combo:
                        IReadOnlyList<string> choices = field.Choices(Controller, element);
                        if (!combo.Items.Cast<object>().Select(i => i.ToString()).SequenceEqual(choices))
                        {
                            combo.Items.Clear();
                            combo.Items.AddRange([.. choices]);
                        }

                        string value = field.Get(element);
                        if (combo.DropDownStyle == ComboBoxStyle.DropDownList)
                        {
                            combo.SelectedIndex = IndexOf(choices, value);
                        }
                        else
                        {
                            combo.Text = value;
                        }

                        combo.Enabled = control.Enabled && field.Set is not null;
                        break;
                    case TextBox box:
                        box.Text = field.Get(element);
                        box.ReadOnly = field.Set is null;
                        break;
                }
            }

            if (view.Group is { } group)
            {
                group.Visible = TBoldModelEditPages.ShowAttribKindFeaturesRecieve(element);
            }

            foreach ((TBoldModelEditGrid grid, DataGridView gridView) in view.Grids)
            {
                if (!gridView.IsCurrentCellInEditMode)
                {
                    FillGrid(grid, gridView, element);
                }
            }
        }
        finally
        {
            fRefreshing = false;
        }
    }

    private static int IndexOf(IReadOnlyList<string> Items, string Value)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == Value)
            {
                return i;
            }
        }

        return -1;
    }

    private static void FillGrid(TBoldModelEditGrid Grid, DataGridView View, TBoldUMLObject Owner)
    {
        View.Rows.Clear();
        foreach (TBoldUMLObject row in Grid.Rows(Owner))
        {
            object[] values = [.. Grid.Columns.Select(c => c.Kind == TBoldModelEditFieldKind.CheckBox ? (object)(c.GetChecked(row) ?? false) : c.Get(row))];
            int index = View.Rows.Add(values);
            View.Rows[index].Tag = row;
        }
    }

    /// <summary>Shows the tree, the page and the status of the model as it is now.</summary>
    public void RefreshAll()
    {
        if (IsDisposed)
        {
            return;
        }

        RefreshTree();
        RefreshPage();
        UpdateStatusbar();
    }

    private void PostRefresh()
    {
        if (fRefreshPosted || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        fRefreshPosted = true;
        BeginInvoke((MethodInvoker)(() =>
        {
            fRefreshPosted = false;
            RefreshAll();
        }));
    }

    // the tree

    private static string LocalKey(object? Tag) => Tag switch
    {
        TBoldUMLObject element => "E" + element.LocalIndex,
        TTreeFolder folder => "F" + folder.Caption,
        _ => "?",
    };

    private static string KeyOf(TreeNode Node) => (Node.Parent is { } parent ? KeyOf(parent) + "/" : "") + LocalKey(Node.Tag);

    private static IEnumerable<TBoldUMLObject> Sorted(IEnumerable<TBoldUMLObject> Elements) => Elements.OrderBy(e => e.name, StringComparer.CurrentCulture);

    private static string NodeText(TBoldUMLObject Element) =>
        Element.IsKindOf("AssociationEnd") ? $"{Element.name} ({Element.type_?.name})" : Element.name;

    private static string ImageOf(object? Tag) => Tag switch
    {
        TTreeFolder { Caption: AssociationsFolder } => "Associations",
        TTreeFolder { Caption: DataTypesFolder } => "DataType",
        TTreeFolder => "Package",
        TBoldUMLObject e when e.IsKindOf("Model") => "Model",
        TBoldUMLObject e when e.IsKindOf("Package") => "Package",
        TBoldUMLObject e when e.IsKindOf("Class") => "Class",
        TBoldUMLObject e when e.IsKindOf("Attribute") => "Attribute",
        TBoldUMLObject e when e.IsKindOf("Operation") => "Operation",
        TBoldUMLObject e when e.IsKindOf("Parameter") => "Parameter",
        TBoldUMLObject e when e.IsKindOf("Association") => "Association",
        TBoldUMLObject e when e.IsKindOf("AssociationEnd") => "AssociationEnd",
        _ => "DataType",
    };

    private static TreeNode NewNode(TreeNodeCollection Nodes, object Tag, string Text)
    {
        var node = new TreeNode(Text) { Tag = Tag, ImageKey = ImageOf(Tag), SelectedImageKey = ImageOf(Tag) };
        if (Tag is TBoldUMLObject element && TBoldModelEditPages.bsrRedOnAutocreatedSetColor(element))
        {
            node.ForeColor = Color.Red;
        }

        Nodes.Add(node);
        return node;
    }

    private void AddChildren(TreeNode Node)
    {
        switch (Node.Tag)
        {
            case TTreeFolder { Caption: LogicalView } folder:
                AddPackageContents(Node, folder.Owner);
                break;
            case TTreeFolder { Caption: AssociationsFolder } folder:
                foreach (TBoldUMLObject association in Sorted(folder.Owner.ownedElement.Where(IsAssociation)))
                {
                    AddChildren(NewNode(Node.Nodes, association, NodeText(association)));
                }

                break;
            case TTreeFolder { Caption: DataTypesFolder } folder:
                foreach (TBoldUMLObject dataType in Sorted(folder.Owner.ownedElement.Where(e => e.IsKindOf("DataType"))))
                {
                    NewNode(Node.Nodes, dataType, dataType.name);
                }

                break;
            case TBoldUMLObject e when e.IsKindOf("Model"):
                AddChildren(NewNode(Node.Nodes, new TTreeFolder(e, LogicalView), LogicalView));
                break;
            case TBoldUMLObject e when e.IsKindOf("Package"):
                AddPackageContents(Node, e);
                break;
            case TBoldUMLObject e when e.IsKindOf("Class"):
                if (mnuShowAttributes.Checked)
                {
                    foreach (TBoldUMLObject attribute in Sorted(e.feature.Where(f => f.IsKindOf("Attribute"))))
                    {
                        NewNode(Node.Nodes, attribute, attribute.name);
                    }
                }

                if (mnuShowRoles.Checked)
                {
                    foreach (TBoldUMLObject role in Sorted(e.associationEnd.Select(end => end.otherEnd).OfType<TBoldUMLObject>().Where(end => end.isNavigable)))
                    {
                        AddChildren(NewNode(Node.Nodes, role, NodeText(role)));
                    }
                }

                if (mnuShowOperations.Checked)
                {
                    foreach (TBoldUMLObject operation in Sorted(e.feature.Where(f => f.IsKindOf("Operation"))))
                    {
                        AddChildren(NewNode(Node.Nodes, operation, operation.name));
                    }
                }

                break;
            case TBoldUMLObject e when e.IsKindOf("Association"):
                if (mnuShowRoles.Checked)
                {
                    foreach (TBoldUMLObject end in e.connection)
                    {
                        AddChildren(NewNode(Node.Nodes, end, NodeText(end)));
                    }
                }

                break;
            case TBoldUMLObject e when e.IsKindOf("AssociationEnd"):
                foreach (TBoldUMLObject qualifier in e.qualifier)
                {
                    NewNode(Node.Nodes, qualifier, qualifier.name);
                }

                break;
            case TBoldUMLObject e when e.IsKindOf("Operation"):
                foreach (TBoldUMLObject parameter in e.parameter)
                {
                    NewNode(Node.Nodes, parameter, parameter.name);
                }

                break;
        }
    }

    private static bool IsAssociation(TBoldUMLObject Element) => Element.IsKindOf("Association") && !Element.IsKindOf("Class");

    private void AddPackageContents(TreeNode Node, TBoldUMLObject Package)
    {
        foreach (TBoldUMLObject package in Sorted(Package.ownedElement.Where(e => e.IsKindOf("Package"))))
        {
            AddChildren(NewNode(Node.Nodes, package, package.name));
        }

        foreach (TBoldUMLObject cls in Sorted(Package.ownedElement.Where(e => e.IsKindOf("Class"))))
        {
            AddChildren(NewNode(Node.Nodes, cls, cls.name));
        }

        if (Package.ownedElement.Any(IsAssociation))
        {
            AddChildren(NewNode(Node.Nodes, new TTreeFolder(Package, AssociationsFolder), AssociationsFolder));
        }

        if (Package.ownedElement.Any(e => e.IsKindOf("DataType")))
        {
            AddChildren(NewNode(Node.Nodes, new TTreeFolder(Package, DataTypesFolder), DataTypesFolder));
        }
    }

    private static IEnumerable<TreeNode> AllNodes(TreeNodeCollection Nodes)
    {
        foreach (TreeNode node in Nodes)
        {
            yield return node;
            foreach (TreeNode child in AllNodes(node.Nodes))
            {
                yield return child;
            }
        }
    }

    /// <summary>Builds the tree of the model again, keeping what was expanded and selected.</summary>
    public void RefreshTree()
    {
        if (Controller.TreeRoot is not { } root)
        {
            BoldTreeView1.Nodes.Clear();
            return;
        }

        string? selected = BoldTreeView1.SelectedNode is { } node ? KeyOf(node) : null;
        fSelecting = true;
        BoldTreeView1.BeginUpdate();
        try
        {
            BoldTreeView1.Nodes.Clear();
            TreeNode model = NewNode(BoldTreeView1.Nodes, root, root.name);
            AddChildren(model);
            if (!fTreeBuilt)
            {
                fTreeBuilt = true;
                fExpanded.Add(KeyOf(model));
                if (model.Nodes.Count > 0)
                {
                    fExpanded.Add(KeyOf(model.Nodes[0]));
                }
            }

            foreach (TreeNode each in AllNodes(BoldTreeView1.Nodes).ToList())
            {
                if (fExpanded.Contains(KeyOf(each)))
                {
                    each.Expand();
                }
            }

            TreeNode? select = selected is null ? null : AllNodes(BoldTreeView1.Nodes).FirstOrDefault(n => KeyOf(n) == selected);
            select ??= Controller.CurrentElement is { } current ? GetNodeForElement(current) : null;
            BoldTreeView1.SelectedNode = select ?? model;
            if (Controller.UMLObjectToCopyOrMove is { } cut && Controller.CutOrCopy == CutOrCopyKind.cckCut)
            {
                GreyNode = GetNodeForElement(cut);
                GreyNode?.ForeColor = SystemColors.GrayText;
            }
        }
        finally
        {
            BoldTreeView1.EndUpdate();
            fSelecting = false;
        }
    }

    /// <summary>The first node of the element (with the caption, if one is given).</summary>
    [Delphi(PortStatus.Ported)]
    public TreeNode? GetNodeForElement(TBoldUMLObject element, string Caption = "") =>
        AllNodes(BoldTreeView1.Nodes).FirstOrDefault(n => n.Tag == element && (Caption == "" || Caption == n.Text));

    /// <summary>Expands the nodes down to the element's node.</summary>
    [Delphi(PortStatus.Ported, Note = "Expands the parents of the element's node; the tree holds the whole model.")]
    public void EnsureOwnerExpanded(TBoldUMLObject element)
    {
        for (TreeNode? node = GetNodeForElement(element)?.Parent; node is not null; node = node.Parent)
        {
            node.Expand();
        }
    }

    /// <summary>Shows the element: its node is selected.</summary>
    [Delphi(PortStatus.Ported)]
    public void JumpToElement(System.ComponentModel.Component? sender, TBoldUMLObject element)
    {
        EnsureOwnerExpanded(element);
        if (GetNodeForElement(element) is { } node)
        {
            BoldTreeView1.SelectedNode = node;
            node.EnsureVisible();
        }
        else if (Controller.CurrentElement != element)
        {
            Controller.CurrentElement = element; // not in the tree (filtered out, or not shown yet): its page shows
        }
    }

    [Delphi(PortStatus.Ported)]
    private void BoldTreeView1Change(object? Sender, TreeViewEventArgs e)
    {
        if (fSelecting)
        {
            return;
        }

        ApplyGUI();
        fSelecting = true;
        try
        {
            Controller.CurrentElement = ElementOf(e.Node);
        }
        finally
        {
            fSelecting = false;
        }
    }

    /// <summary>Delete deletes the element; F2 edits the name of its node.</summary>
    [Delphi(PortStatus.Ported)]
    private void BoldTreeView1KeyDown(object? Sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && !(BoldTreeView1.SelectedNode?.IsEditing ?? false))
        {
            DeleteXxx1Click(Sender, EventArgs.Empty);
            e.Handled = true;
        }

        if (e.KeyCode == Keys.F2 && BoldTreeView1.SelectedNode is { IsEditing: false } node)
        {
            node.BeginEdit();
            e.Handled = true;
        }
    }

    /// <summary>The first node is selected when the tree first expands, so that a page shows.</summary>
    [Delphi(PortStatus.Ported)]
    private void BoldTreeView1Expanded(object? Sender, TreeViewEventArgs e)
    {
        if (BoldTreeView1.SelectedNode is null && BoldTreeView1.Nodes.Count > 0)
        {
            BoldTreeView1.SelectedNode = BoldTreeView1.Nodes[0];
        }
    }

    private void BoldTreeView1BeforeLabelEdit(object? Sender, NodeLabelEditEventArgs e) =>
        e.CancelEdit = e.Node?.Tag is not TBoldUMLObject element || element.IsKindOf("AssociationEnd") || element.IsKindOf("DataType");

    private void BoldTreeView1AfterLabelEdit(object? Sender, NodeLabelEditEventArgs e)
    {
        if (e.Label is { } label && e.Node?.Tag is TBoldUMLObject element)
        {
            Guarded(() => element.name = label);
        }
    }

    [Delphi(PortStatus.Ported, Note = "WinForms' drag and drop carries the element, where Delphi's drag object and GUI handler did.")]
    private void BoldTreeView1StartDrag(object? Sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode { Tag: TBoldUMLObject element })
        {
            fDragged = element;
            BoldTreeView1.DoDragDrop(element, DragDropEffects.Move);
            BoldTreeView1EndDrag();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void BoldTreeView1EndDrag() => fDragged = null;

    private TBoldUMLObject? NodeElementAt(int X, int Y) => ElementOf(BoldTreeView1.GetNodeAt(BoldTreeView1.PointToClient(new Point(X, Y))));

    /// <summary>The element of a node: a folder's is the element it belongs to (the model or a package).</summary>
    public static TBoldUMLObject? ElementOf(TreeNode? Node) => Node?.Tag switch
    {
        TBoldUMLObject element => element,
        TTreeFolder folder => folder.Owner,
        _ => null,
    };

    private void BoldTreeView1DragOver(object? Sender, DragEventArgs e) =>
        e.Effect = Controller.CanDrop(fDragged, NodeElementAt(e.X, e.Y)) ? DragDropEffects.Move : DragDropEffects.None;

    private void BoldTreeView1DragDrop(object? Sender, DragEventArgs e)
    {
        if (fDragged is { } dragged && NodeElementAt(e.X, e.Y) is { } target && Controller.CanDrop(dragged, target))
        {
            Guarded(() => Controller.Drop(dragged, target));
        }
    }

    [Delphi(PortStatus.Ported, Note = "Nothing to accept: a WinForms splitter always resizes.")]
    private void Splitter2CanResize(object? Sender, SplitterEventArgs e)
    {
    }

    // the menus

    /// <summary>The menus offer what fits the element on show (TBoldModelEditController.Commands).</summary>
    private void popTreePopup(object? Sender, EventArgs e) => UpdateMenus();

    private void UpdateMenus()
    {
        TBoldModelEditCommands c = Controller.Commands();
        DeleteXxx1.Text = c.DeleteXxx ?? "Delete";
        Deletexxx2.Text = DeleteXxx1.Text;
        SetAvailable(DeleteXxx1, Deletexxx2, c.DeleteXxx is not null);
        SetAvailable(N10, N11, c.N10);
        SetAvailable(NewPackage1, null, c.NewPackage);
        SetAvailable(NewClass1, NewClass2, c.NewClass);
        SetAvailable(NewAssociation1, NewAssociation2, c.NewAssociation);
        SetAvailable(NewDatatype1, NewDataType2, c.NewDatatype);
        SetAvailable(AddSubclass1, AddSubclass2, c.AddSubclass);
        SetAvailable(InsertSuperclass2, InsertSuperclass1, c.InsertSuperclass);
        SetAvailable(NewAttribute1, NewAttribute2, c.NewAttribute);
        SetAvailable(NewOperation1, NewOperation2, c.NewOperation);
        SetAvailable(NewParameter1, NewParameter2, c.NewParameter);
        SetAvailable(NewQualifier1, NewQualifier2, c.NewQualifier);
        SetAvailable(N9, null, c.N9);
        SetAvailable(mnuOverrideFrameworkMethods, null, c.OverrideFrameworkMethods);
        SetAvailable(mnuOverrideModelMethods, null, c.OverrideModelMethods);
        SetAvailable(mnuOverrideInAllSubclasses, null, c.OverrideInAllSubclasses);
        if (c.OverrideFrameworkMethods && Controller.GetCurrentClass() is { } cls)
        {
            FillOverrideMenu(mnuOverrideFrameworkMethods, TBoldModelEditController.CreateFrameworkMethodItems(cls));
            FillOverrideMenu(mnuOverrideModelMethods, Controller.CreateModelMethodItems(cls));
        }
    }

    /// <summary>
    /// A command of the tree's menu and its twin on the model menu are there, and their shortcuts work, when the
    /// command fits the element on show (Available, not Visible: a menu that is not open shows nothing).
    /// </summary>
    private static void SetAvailable(ToolStripItem Popup, ToolStripItem? Main, bool Available)
    {
        Popup.Available = Available;
        Popup.Enabled = Available;
        if (Main is not null)
        {
            Main.Available = Available;
            Main.Enabled = Available;
        }
    }

    private void FillOverrideMenu(ToolStripMenuItem Menu, List<string> Names)
    {
        FreeItemsInMenu(Menu);
        foreach (string name in Names)
        {
            Menu.DropDownItems.Add(new ToolStripMenuItem(name, null, OnOverrideMenuItemClick));
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void FreeItemsInMenu(ToolStripMenuItem InMenu)
    {
        while (InMenu.DropDownItems.Count > 0)
        {
            ToolStripItem item = InMenu.DropDownItems[0];
            InMenu.DropDownItems.RemoveAt(0);
            item.Dispose();
        }
    }

    [Delphi(PortStatus.Ported, Note = "The items of the override menus go as their menu is filled again.")]
    private static void FreeRefsInList(List<ToolStripItem> List)
    {
        foreach (ToolStripItem item in List)
        {
            item.Dispose();
        }

        List.Clear();
    }

    [Delphi(PortStatus.Ported)]
    private static void ClearMenuItems(ToolStripMenuItem MenuItem) => MenuItem.DropDownItems.Clear();

    /// <summary>An override menu's item: the method of its name is overridden in the class on show.</summary>
    [Delphi(PortStatus.Ported)]
    private void OnOverrideMenuItemClick(object? Sender, EventArgs e)
    {
        if (Sender is ToolStripMenuItem { OwnerItem: ToolStripMenuItem parent } item)
        {
            TBoldUMLObject? made = parent == mnuOverrideFrameworkMethods ? Controller.OverrideFrameworkMethod(item.Text ?? "")
                : parent == mnuOverrideModelMethods ? Controller.OverrideModelMethod(item.Text ?? "") : null;
            ShowElement(made);
        }
    }

    /// <summary>Shows the model as it is now, and the element made, if any.</summary>
    private void ShowElement(TBoldUMLObject? Made)
    {
        RefreshAll();
        if (Made is not null)
        {
            JumpToElement(this, Made);
        }
    }

    private void Run(Func<TBoldUMLObject> Command)
    {
        ApplyGUI();
        TBoldUMLObject? made = null;
        Guarded(() => made = Command());
        ShowElement(made);
    }

    private void DeleteXxx1Click(object? Sender, EventArgs e)
    {
        if (BoldTreeView1.SelectedNode?.IsEditing ?? false)
        {
            return;
        }

        Guarded(Controller.DeleteXxx);
        ShowElement(Controller.CurrentElement);
    }

    private void NewClass1Click(object? Sender, EventArgs e) => Run(Controller.NewClass);

    private void NewAssociation1Click(object? Sender, EventArgs e) => Run(Controller.NewAssociation);

    private void NewDatatype1Click(object? Sender, EventArgs e) => Run(Controller.NewDatatype);

    private void NewPackage1Click(object? Sender, EventArgs e) => Run(Controller.NewPackage);

    private void AddSubclass1Click(object? Sender, EventArgs e) => Run(Controller.AddSubclass);

    private void InsertSuperclass1Click(object? Sender, EventArgs e) => Run(Controller.InsertSuperclass);

    private void NewAttribute1Click(object? Sender, EventArgs e) => Run(Controller.NewAttribute);

    private void NewOperation1Click(object? Sender, EventArgs e) => Run(Controller.NewOperation);

    private void NewParameter1Click(object? Sender, EventArgs e) => Run(Controller.NewParameter);

    private void NewQualifier1Click(object? Sender, EventArgs e) => Run(Controller.NewQualifier);

    private void mnuOverrideInAllSubclassesClick(object? Sender, EventArgs e)
    {
        Guarded(Controller.OverrideInAllSubclasses);
    }

    private void Clear1Click(object? Sender, EventArgs e)
    {
        Controller.Clear();
        RefreshAll();
    }

    [Delphi(PortStatus.Ported, Note = "The edit menu's cut: the tree cuts the element (TBoldModelEditController.Cut), a text its selection.")]
    private void Cut1Click(object? Sender, EventArgs e)
    {
        switch (FocusedControl())
        {
            case TreeView:
                if (Controller.Cut() && BoldTreeView1.SelectedNode is { } node)
                {
                    UngreyNode();
                    GreyNode = node;
                    GreyNode.ForeColor = SystemColors.GrayText;
                }

                break;
            case TextBoxBase text:
                text.Cut();
                break;
        }
    }

    [Delphi(PortStatus.Ported, Note = "The tree copies the element (TBoldModelEditController.Copy), a text its selection.")]
    private void Copy1Click(object? Sender, EventArgs e)
    {
        switch (FocusedControl())
        {
            case TreeView:
                Controller.Copy();
                break;
            case TextBoxBase text:
                text.Copy();
                break;
        }
    }

    [Delphi(PortStatus.Ported, Note = "The tree pastes the element cut or copied (TBoldModelEditController.Paste), a text the clipboard.")]
    private void Paste1Click(object? Sender, EventArgs e)
    {
        switch (FocusedControl())
        {
            case TreeView:
                Guarded(Controller.Paste);
                UngreyNode();
                break;
            case TextBoxBase text:
                text.Paste();
                break;
        }
    }

    private void UngreyNode()
    {
        GreyNode?.ForeColor = Color.Empty;
        GreyNode = null;
    }

    private Control? FocusedControl()
    {
        Control? control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
        {
            control = inner;
        }

        if (control is DataGridView { EditingControl: { } editing })
        {
            control = editing;
        }

        return control;
    }

    [Delphi(PortStatus.Ported)]
    private void mnuShowAttributesClick(object? Sender, EventArgs e)
    {
        mnuShowAttributes.Checked = !mnuShowAttributes.Checked;
        tbnFilterAttributes.Checked = mnuShowAttributes.Checked;
        RefreshTree();
    }

    [Delphi(PortStatus.Ported)]
    private void mnuShowRolesClick(object? Sender, EventArgs e)
    {
        mnuShowRoles.Checked = !mnuShowRoles.Checked;
        tbnFilterRoles.Checked = mnuShowRoles.Checked;
        RefreshTree();
    }

    [Delphi(PortStatus.Ported)]
    private void mnuShowOperationsClick(object? Sender, EventArgs e)
    {
        mnuShowOperations.Checked = !mnuShowOperations.Checked;
        tbnFilterOperations.Checked = mnuShowOperations.Checked;
        RefreshTree();
    }

    [Delphi(PortStatus.Ported)]
    private void tbnFilterAttributesClick(object? Sender, EventArgs e) => mnuShowAttributesClick(mnuShowAttributes, e);

    [Delphi(PortStatus.Ported)]
    private void tbnFilterRolesClick(object? Sender, EventArgs e) => mnuShowRolesClick(mnuShowRoles, e);

    [Delphi(PortStatus.Ported)]
    private void tbnFilterOperationsClick(object? Sender, EventArgs e) => mnuShowOperationsClick(mnuShowOperations, e);

    [Delphi(PortStatus.Ported)]
    private void mnuAdvancedClick(object? Sender, EventArgs e)
    {
        mnuAdvanced.Checked = !mnuAdvanced.Checked;
        Controller.IsAdvancedMode = mnuAdvanced.Checked;
    }

    [Delphi(PortStatus.Ported)]
    private void Loggform1Click(object? Sender, EventArgs e) => BoldLogHandler.BoldLog().Show();

    /// <summary>The boldify and flatten commands say what they will do.</summary>
    [Delphi(PortStatus.Ported)]
    private void Tools1Click(object? Sender, EventArgs e)
    {
        if (Controller.BoldifyFlattenCaptions() is { } captions)
        {
            Boldifymodel1.Text = captions.Boldify;
            Flatten.Text = captions.Flatten;
        }
    }

    private void Boldifymodel1Click(object? Sender, EventArgs e)
    {
        Cursor.Current = Cursors.WaitCursor;
        Controller.Boldifymodel();
        RefreshAll();
    }

    private void FlattenClick(object? Sender, EventArgs e)
    {
        Controller.Flatten();
        RefreshAll();
    }

    private void EnsureTypesFromTypeNameHandle(object? Sender, EventArgs e)
    {
        Controller.EnsureTypesFromTypeNameHandle();
        RefreshAll();
    }

    /// <summary>Validates the model in the validation form.</summary>
    [Delphi(PortStatus.Ported)]
    private void mnuConsistencycheckClick(object? Sender, EventArgs e)
    {
        ApplyGUI();
        Controller.EnsureFlattenedAndBoldified();
        TfrmValidation ValidationForm = TfrmValidation.EnsureValidationForm(Controller.GetCurrentModelHandle(), this, JumpToElement);
        ValidationForm.ValidationProc = TBoldUMLModelValidatorCallBack.Validate;
        ValidationForm.Show();
        ValidationForm.Validate();
        Controller.ModelNeedsValidation = false;
        RefreshAll();
    }

    [Delphi(PortStatus.Ported, Note = "Opens the tagged values editor on the element on show; it follows the editor's element.")]
    private void N3Click(object? Sender, EventArgs e)
    {
        var TVEditor = new TfrmBoldUMLTaggedValuesEditor(Controller) { Owner = this };
        TVEditor.Show();
    }

    [Delphi(PortStatus.Ported, Note = "Delphi opened its help file, which is not ported.")]
    private void Contents1Click(object? Sender, EventArgs e) =>
        Dialogs.ShowMessage("Bold UML Model Editor. The model editor's guide is in the README of the C# port (CSharp/README.md).");

    [Delphi(PortStatus.Ported, Note = "Empty, as Delphi's (it opened a debugger that is not ported).")]
    private void ToolButton1Click(object? Sender, EventArgs e)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void Exit1Click(object? Sender, EventArgs e)
    {
        ApplyGUI();
        Close();
    }

    /// <summary>Hides the form, after applying what is being edited.</summary>
    [Delphi(PortStatus.Ported, Note = "Hides the form, where Delphi returned caHide for its caller to hide it.")]
    public void CloseThisForm()
    {
        ApplyGUI();
        Hide();
    }

    private void OpenFile1Click(object? Sender, EventArgs e)
    {
        ApplyGUI();
        using var odOpenModel = new OpenFileDialog { Filter = TBoldModelEditController.OpenFileFilter() };
        if (odOpenModel.ShowDialog(this) == DialogResult.OK)
        {
            Cursor.Current = Cursors.WaitCursor;
            Controller.OpenFile(odOpenModel.FileName);
            RefreshAll();
        }
    }

    private void SaveFileAs1Click(object? Sender, EventArgs e)
    {
        ApplyGUI();
        using var sdSaveModel = new SaveFileDialog { Filter = TBoldModelEditController.SaveFileFilter(), FileName = ModelHandle?.FileName ?? "" };
        if (sdSaveModel.ShowDialog(this) == DialogResult.OK)
        {
            Cursor.Current = Cursors.WaitCursor;
            Controller.SaveFileAs(sdSaveModel.FileName);
            RefreshAll();
        }
    }

    // the buttons beside fields

    private void FieldButtonClick(string Button)
    {
        ApplyGUI();
        switch (Button)
        {
            case "btInterfaceUses":
                btInterfaceUsesClick(this, EventArgs.Empty);
                break;
            case "btImplementationUses":
                btImplementationUsesClick(this, EventArgs.Empty);
                break;
            case "btShowDerivationExpressionsEditor":
                btShowDerivationExpressionsEditorClick(this, EventArgs.Empty);
                break;
            case "btClassDefaultStringRep":
                btClassDefaultStringRepClick(this, EventArgs.Empty);
                break;
            case "btAttributeShowDerivExprEditor":
                btAttributeShowDerivExprEditorClick(this, EventArgs.Empty);
                break;
            case "btAssoEndShowDeriExprEditor":
                btAssoEndShowDeriExprEditorClick(this, EventArgs.Empty);
                break;
            default:
                btModelConstraintEditorClick(this, EventArgs.Empty);
                break;
        }

        RefreshAll();
    }

    [Delphi(PortStatus.Ported)]
    private void btInterfaceUsesClick(object? Sender, EventArgs e) =>
        new TfrmUsesEditor("Interface Uses Editor", Controller.CurrentModel!, BoldDefs.BOLDTVPREFIX + TAG_INTERFACEUSES) { Owner = this }.Show();

    [Delphi(PortStatus.Ported)]
    private void btImplementationUsesClick(object? Sender, EventArgs e) =>
        new TfrmUsesEditor("Implementation Uses Editor", Controller.CurrentModel!, BoldDefs.BOLDTVPREFIX + TAG_IMPLEMENTATIONUSES) { Owner = this }.Show();

    [Delphi(PortStatus.Ported)]
    private void btModelConstraintEditorClick(object? Sender, EventArgs e)
    {
        if (Controller.CurrentElement is { } element)
        {
            TfrmBoldUMLConstraintEditor.ShowTheConstraintEditor(element, this);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btShowDerivationExpressionsEditorClick(object? Sender, EventArgs e)
    {
        if (Controller.GetCurrentClass() is { } cls)
        {
            TfrmBoldDerivationExpressionsEditor.ShowDerivationExpressionsEditor(cls.EnsuredTaggedValue(BoldDefs.BOLDTVPREFIX + TAG_DERIVATIONEXPRESSIONS));
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btAttributeShowDerivExprEditorClick(object? Sender, EventArgs e)
    {
        if (Controller.CurrentElement is { } attribute && attribute.owner is { } owner)
        {
            EditOclExpression(attribute, TAG_DERIVATIONOCL, owner);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btAssoEndShowDeriExprEditorClick(object? Sender, EventArgs e)
    {
        if (Controller.CurrentElement is not { } end)
        {
            return;
        }

        if (end.otherEnd?.type_ is { } type)
        {
            EditOclExpression(end, TAG_DERIVATIONOCL, type);
        }
        else
        {
            Dialogs.ShowMessage("This association is not complete... the other end is not connected");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btClassDefaultStringRepClick(object? Sender, EventArgs e)
    {
        if (Controller.CurrentElement is { } cls)
        {
            EditOclExpression(cls, TAG_DEFAULTSTRINGREPRESENTATION, cls);
        }
    }

    /// <summary>Edits an OCL expression of the element, in the OCL editor, with the model boldified and flattened.</summary>
    [Delphi(PortStatus.Ported)]
    public void EditOclExpression(TBoldUMLObject Element, string TaggedValue, TBoldUMLObject Context)
    {
        Controller.EnsureFlattenedAndBoldified();
        string res = TBoldUMLOclEditor.BoldUMLOclEditor_().EditOcl(Controller.GetCurrentModelHandle(), Context, Element.GetBoldTV(TaggedValue));
        Element.SetBoldTV(TaggedValue, res);
    }

    // the plug-ins

    [Delphi(PortStatus.Ported)]
    private void SetPlugInList(IReadOnlyList<TUMLPlugIn> PlugInList)
    {
        fPlugInList = PlugInList;
        foreach (TUMLPlugIn plugIn in PlugInList)
        {
            InstallPlugIn(plugIn);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void InstallPlugIn(TUMLPlugIn PlugIn)
    {
        if (PlugIn.PlugInType == TPlugInType.ptTool)
        {
            AddTool(PlugIn);
        }
        else
        {
            AddFiler(PlugIn);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void AddTool(TUMLPlugIn PlugIn)
    {
        AddMenuTool(NewPlugInMenuItem(PlugIn));
        AddButtonTool(NewPlugInToolButton(PlugIn));
    }

    [Delphi(PortStatus.Ported)]
    private void AddFiler(TUMLPlugIn PlugIn)
    {
        AddMenuFiler(NewPlugInMenuItem(PlugIn));
        AddButtonFiler(NewPlugInToolButton(PlugIn));
    }

    /// <summary>A tool goes on the tools menu, after its first separators.</summary>
    [Delphi(PortStatus.Ported)]
    private void AddMenuTool(ToolStripMenuItem? MenuItem)
    {
        if (MenuItem is not null)
        {
            Tools1.DropDownItems.Insert(Math.Min(4, Tools1.DropDownItems.Count), MenuItem);
        }
    }

    /// <summary>A filer goes on the file menu, after Clear.</summary>
    [Delphi(PortStatus.Ported)]
    private void AddMenuFiler(ToolStripMenuItem? MenuItem)
    {
        if (MenuItem is not null)
        {
            mnuFile.DropDownItems.Insert(Math.Min(2, mnuFile.DropDownItems.Count), MenuItem);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Before the second separator of the tool bar.")]
    private void AddButtonTool(ToolStripButton? ToolButton)
    {
        if (ToolButton is not null)
        {
            ToolBar1.Items.Insert(ToolBar1.Items.IndexOf(Separator2), ToolButton);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Before the first separator of the tool bar.")]
    private void AddButtonFiler(ToolStripButton? ToolButton)
    {
        if (ToolButton is not null)
        {
            ToolBar1.Items.Insert(ToolBar1.Items.IndexOf(Separator1), ToolButton);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TUMLPlugInMenuItem? NewPlugInMenuItem(TUMLPlugIn PlugIn)
    {
        if (PlugIn.MenuItemName == "")
        {
            return null;
        }

        var Result = new TUMLPlugInMenuItem { Text = PlugIn.MenuItemName, PlugIn = PlugIn, Image = TUMLPlugInImages.ImageOf(PlugIn) };
        Result.Click += PlugInMenuClick;
        return Result;
    }

    /// <summary>A tool button with the plug-in's bitmap; without one, its name shows.</summary>
    [Delphi(PortStatus.Ported, Note = "Shows the plug-in's name when it has no bitmap, where Delphi said it could not find it.")]
    private TUMLPlugInToolButton? NewPlugInToolButton(TUMLPlugIn PlugIn)
    {
        if (PlugIn.MenuItemName == "")
        {
            return null;
        }

        Bitmap? image = TUMLPlugInImages.ImageOf(PlugIn);
        var result = new TUMLPlugInToolButton
        {
            PlugIn = PlugIn, Image = image, ToolTipText = PlugIn.MenuItemName, Text = PlugIn.MenuItemName,
            DisplayStyle = image is null ? ToolStripItemDisplayStyle.Text : ToolStripItemDisplayStyle.Image,
        };
        result.Click += PlugInButtonClick;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void PlugInMenuClick(object? Sender, EventArgs e)
    {
        if (Sender is TUMLPlugInMenuItem { PlugIn: { } plugIn })
        {
            ExecPlugIn(plugIn);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void PlugInButtonClick(object? Sender, EventArgs e)
    {
        if (Sender is TUMLPlugInToolButton { PlugIn: { } plugIn })
        {
            ExecPlugIn(plugIn);
        }
    }

    /// <summary>Runs a plug-in (TBoldModelEditController.ExecPlugIn), after applying what is being edited.</summary>
    public void ExecPlugIn(TUMLPlugIn PlugIn)
    {
        ApplyGUI();
        Cursor.Current = Cursors.WaitCursor;
        try
        {
            Controller.ExecPlugIn(PlugIn);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
            RefreshAll();
        }
    }

    /// <summary>The status bar says whether the model was validated and what validating found.</summary>
    private void UpdateStatusbar()
    {
        (string text, TSeverity severity) = Controller.StatusText();
        StatusPanel1.Text = text;
        StatusPanel0.Image = severity switch
        {
            TSeverity.sHint => imgHint,
            TSeverity.sWarning or TSeverity.sError => imgWarning,
            _ => null,
        };
    }
}
