// The main form of the Buildings and Owners example (Delphi: examples/Delphi/Compound/Building/Mainform.pas): persons,
// buildings and residential buildings with their owners and residents.
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Bold;
using BuildingClasses;
using static BldOwn.Datamod;

namespace BldOwn;

/// <summary>The application's main form, as Delphi's global allform.</summary>
public static class Mainform
{
    public static Tallform allform { get; set; } = null!;
}

/// <summary>
/// All persons in a grid, with the current one's details; all buildings and all residential buildings on two pages, with
/// the current one's owners and residents; persons are dragged into owners and residents. Right-click a grid or list
/// for new, delete and show in own window; the buttons save, undo and redo, open and close the system and create the
/// database.
/// </summary>
public class Tallform : Form
{
    private readonly Container components = new();

    public readonly ContextMenuStrip BuildingPopup;
    public readonly ToolStripMenuItem newBuilding;
    public readonly ToolStripMenuItem Delete1;
    public readonly ToolStripMenuItem Showinownwindow2;
    public readonly ContextMenuStrip PersonPopup;
    public readonly ToolStripMenuItem NewPerson;
    public readonly ToolStripMenuItem DeletePerson;
    public readonly ToolStripMenuItem Showinownwindow1;
    public readonly ContextMenuStrip SingleLinkPopup;
    public readonly ToolStripMenuItem MenuItem5;
    public readonly ContextMenuStrip MultiLinkPopup;
    public readonly ToolStripMenuItem MenuItem1;
    public readonly ToolStripMenuItem ShowInOwnWindow3;

    public readonly TBoldListHandle blhAllPerson;
    public readonly TBoldListHandle blhAllBuilding;
    public readonly TBoldListHandle blhOwnedBuildingsHandle;
    public readonly TBoldListHandle blhOwners;
    public readonly TBoldExpressionHandle behHome;
    public readonly TBoldListHandle blhResidents;
    public readonly TBoldListHandle blhAllResidentialBuilding;
    public readonly TBoldListHandle blhOwners2;

    public readonly TBoldAsStringRenderer bsrRentPerResident;
    public readonly TBoldAsStringRenderer bsrResidentsTotalAssets;
    public readonly TBoldAsStringRenderer bsrAddress;
    public readonly TBoldAsStringRenderer HighRentRenderer;

    public readonly Label Label1;
    public readonly TBoldEdit PersonCount;
    public readonly TBoldGrid bgrPerson;
    public readonly CheckBox CheckBox1;
    public readonly CheckBox CheckBox2;
    public readonly GroupBox PersonGroup;
    public readonly TBoldEdit bedFirstName;
    public readonly TBoldEdit bedLastName;
    public readonly TBoldEdit bedAssets;
    public readonly TBoldCheckBox bcbRich;
    public readonly TBoldEdit bedPersonHome;
    public readonly TBoldListBox blbPersonOwnedBuildings;

    public readonly TabControl PageControl1;
    public readonly TabPage tabBuilding;
    public readonly TBoldGrid bgrBuilding;
    public readonly GroupBox GroupBox1;
    public readonly TBoldEdit bedAddress;
    public readonly TBoldListBox blbBuildingOwners;
    public readonly TRadioGroup rgNameRepresentation;
    public readonly TabPage tabResidentialBuilding;
    public readonly TBoldGrid bgrResidentialBuilding;
    public readonly GroupBox BuildingGroup;
    public readonly TBoldEdit bedAddress2;
    public readonly TBoldListBox blbBuildingOwners2;
    public readonly TBoldListBox blbBuildingResidents;
    public readonly Button btnChargeRent;

    public readonly Button btnUpdateDB;
    public readonly ProgressBar pbdbNotification;
    public readonly Button btnCheckpoint;
    public readonly Button btnUnDo;
    public readonly Button btnRedo;
    public readonly Button Button1;
    public readonly Button Button2;

    public readonly TBoldPersistenceProgressNotifier BoldPersistenceProgressNotifier1;
    public readonly TBoldActivateSystemAction BoldActivateSystemAction1;
    public readonly TBoldUpdateDBAction BoldUpdateDBAction1;

    /// <summary>Creates the database's tables (Delphi's BoldIBDatabaseAction1, for InterBase).</summary>
    public readonly TBoldGenerateSchemaAction BoldGenerateSchemaAction1;

    /// <summary>The control whose popup menu was opened; null when a menu item runs by its shortcut key.</summary>
    private Control? Currentpopupcomponent;

    public Tallform()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 428);
        Text = "Buildings and owners";
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += FormCloseQuery;

        // The handles: all persons and their details, all buildings and residential buildings and theirs.
        TBoldSystemHandle system = DataModule1.BoldSystemHandle1;
        blhAllPerson = new TBoldListHandle(components) { Name = "blhAllPerson", RootHandle = system, Expression = "Person.allInstances" };
        blhAllBuilding = new TBoldListHandle(components) { Name = "blhAllBuilding", RootHandle = system, Expression = "Building.allInstances" };
        blhOwnedBuildingsHandle = new TBoldListHandle(components) { Name = "blhOwnedBuildingsHandle", RootHandle = blhAllPerson, Expression = "ownedBuildings" };
        blhOwners = new TBoldListHandle(components) { Name = "blhOwners", RootHandle = blhAllBuilding, Expression = "owners" };
        behHome = new TBoldExpressionHandle(components) { Name = "behHome", RootHandle = blhAllPerson, Expression = "home" };
        blhAllResidentialBuilding = new TBoldListHandle(components)
        {
            Name = "blhAllResidentialBuilding",
            RootHandle = system,
            Expression = "Residential_Building.allInstances",
        };
        blhResidents = new TBoldListHandle(components) { Name = "blhResidents", RootHandle = blhAllResidentialBuilding, Expression = "residents" };
        blhOwners2 = new TBoldListHandle(components) { Name = "blhOwners2", RootHandle = blhAllResidentialBuilding, Expression = "owners" };

        // The renderers of this form.
        bsrRentPerResident = new TBoldAsStringRenderer(components)
        {
            OnGetAsString = bsrRentPerResidentGetAsString,
            OnSetAsString = bsrRentPerResidentSetAsString,
            OnValidateCharacter = bsrRentPerResidentValidateCharacter,
            OnValidateString = bsrRentPerResidentValidateString,
        };
        bsrRentPerResident.OnMayModify = bsrRentPerResidentMayModify;
        bsrRentPerResident.OnHoldsChangedValue = bsrRentPerResidentHoldsChangedValue;
        bsrRentPerResident.OnReleaseChangedValue = bsrRentPerResidentReleaseChangedValue;
        bsrRentPerResident.OnSubscribe = bsrRentPerResidentSubscribe;
        bsrResidentsTotalAssets = new TBoldAsStringRenderer(components) { OnGetAsString = bsrResidentsTotalAssetsGetAsString };
        bsrResidentsTotalAssets.OnSubscribe = bsrResidentsTotalAssetsSubscribe;
        bsrAddress = new TBoldAsStringRenderer(components) { OnSetFont = bsrAddressSetFont, OnSetColor = bsrAddressSetColor };
        HighRentRenderer = new TBoldAsStringRenderer(components) { OnSetFont = HighRentRendererSetFont, OnSetColor = HighRentRendererSetColor };

        // The popup menus.
        BuildingPopup = Popup(
            newBuilding = MenuItem("New ", Keys.Insert, newBuildingClick),
            Delete1 = MenuItem("Delete", Keys.Delete, DeleteCurrentObject),
            Showinownwindow2 = MenuItem("Show in own window", Keys.None, ShowInOwnWindow));
        PersonPopup = Popup(
            NewPerson = MenuItem("new", Keys.Insert, NewPersonClick),
            DeletePerson = MenuItem("Delete", Keys.Delete, DeleteCurrentObject),
            Showinownwindow1 = MenuItem("Show in own window", Keys.None, ShowInOwnWindow));
        SingleLinkPopup = Popup(MenuItem5 = MenuItem("Delete", Keys.Delete, SingleItemRemove));
        MultiLinkPopup = Popup(
            MenuItem1 = MenuItem("Delete", Keys.Delete, Remove),
            ShowInOwnWindow3 = MenuItem("Show in own window", Keys.None, ShowInOwnWindow));

        // All persons.
        Label1 = new Label { Name = "Label1", Text = "All Persons #:", Bounds = new Rectangle(8, 16, 90, 13) };
        PersonCount = new TBoldEdit { Name = "PersonCount", Bounds = new Rectangle(128, 16, 73, 21), ReadOnly = true };
        PersonCount.BoldHandle = blhAllPerson;
        PersonCount.BoldProperties.Expression = "Person.allInstances->size";
        bgrPerson = new TBoldGrid
        {
            Name = "bgrPerson",
            Bounds = new Rectangle(8, 40, 193, 97),
            AddNewAtEnd = false,
            BoldAutoColumns = false,
            BoldShowConstraints = false,
            ContextMenuStrip = PersonPopup,
        };
        bgrPerson.BoldHandle = blhAllPerson;
        bgrPerson.BoldProperties.InternalDrag = false;
        bgrPerson.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        Column(bgrPerson, "lastName + ', '+firstName", 87, "Full Name");
        Column(bgrPerson, "assets", 53);
        CheckBox1 = new CheckBox { Name = "CheckBox1", Text = "Rich only", Bounds = new Rectangle(8, 152, 72, 17) };
        CheckBox1.Click += CheckBox1Click;
        CheckBox2 = new CheckBox { Name = "CheckBox2", Text = "Alphabetically", Bounds = new Rectangle(80, 152, 97, 17) };
        CheckBox2.Click += CheckBox2Click;

        // The current person.
        PersonGroup = new GroupBox { Name = "PersonGroup", Text = "Person", Bounds = new Rectangle(208, 8, 385, 161) };
        bedFirstName = new TBoldEdit { Name = "bedFirstName", Bounds = new Rectangle(24, 32, 57, 21) };
        bedFirstName.BoldHandle = blhAllPerson;
        bedFirstName.BoldProperties.Expression = "firstName";
        bedLastName = new TBoldEdit { Name = "bedLastName", Bounds = new Rectangle(88, 32, 121, 21) };
        bedLastName.BoldHandle = blhAllPerson;
        bedLastName.BoldProperties.Expression = "lastName";
        bedLastName.BoldProperties.ApplyPolicy = TBoldApplyPolicy.bapChange;
        bedAssets = new TBoldEdit { Name = "bedAssets", Bounds = new Rectangle(24, 80, 65, 21) };
        bedAssets.BoldHandle = blhAllPerson;
        bedAssets.BoldProperties.Expression = "assets";
        bedAssets.BoldProperties.Renderer = DataModule1.NegativeRedRenderer;
        bcbRich = new TBoldCheckBox { Name = "bcbRich", Text = "Rich", Bounds = new Rectangle(104, 80, 57, 17) };
        bcbRich.BoldHandle = blhAllPerson;
        bcbRich.BoldProperties.Renderer = DataModule1.IsRichRenderer;
        bedPersonHome = new TBoldEdit { Name = "bedPersonHome", Bounds = new Rectangle(24, 120, 137, 21), ReadOnly = true, ContextMenuStrip = SingleLinkPopup };
        bedPersonHome.BoldHandle = behHome;
        bedPersonHome.BoldProperties.DropMode = TBoldDropMode.bdpReplace;
        bedPersonHome.BoldProperties.Expression = "address";
        blbPersonOwnedBuildings = new TBoldListBox { Name = "blbPersonOwnedBuildings", Bounds = new Rectangle(232, 32, 145, 113), ContextMenuStrip = MultiLinkPopup };
        blbPersonOwnedBuildings.BoldHandle = blhOwnedBuildingsHandle;
        blbPersonOwnedBuildings.BoldProperties.DropMode = TBoldDropMode.bdpInsert;
        blbPersonOwnedBuildings.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        blbPersonOwnedBuildings.BoldRowProperties.Expression = "address";
        PersonGroup.Controls.AddRange(
        [
            new Label { Text = "FirstName", Bounds = new Rectangle(24, 16, 60, 13) },
            new Label { Text = "LastName", Bounds = new Rectangle(88, 16, 60, 13) },
            new Label { Text = "Assets", Bounds = new Rectangle(24, 64, 60, 13) },
            new Label { Text = "Home Address", Bounds = new Rectangle(24, 104, 90, 13) },
            new Label { Text = "Owned Buildings", Bounds = new Rectangle(232, 16, 100, 13) },
            bedFirstName, bedLastName, bedAssets, bcbRich, bedPersonHome, blbPersonOwnedBuildings,
        ]);

        // All buildings, and the current one.
        PageControl1 = new TabControl { Name = "PageControl1", Bounds = new Rectangle(0, 176, 593, 193) };
        tabBuilding = new TabPage { Name = "tabBuilding", Text = "Buildings" };
        bgrBuilding = new TBoldGrid
        {
            Name = "bgrBuilding",
            Bounds = new Rectangle(8, 8, 269, 153),
            AddNewAtEnd = false,
            BoldAutoColumns = false,
            BoldShowConstraints = false,
            ContextMenuStrip = BuildingPopup,
        };
        bgrBuilding.BoldHandle = blhAllBuilding;
        bgrBuilding.BoldProperties.InternalDrag = false;
        bgrBuilding.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        Column(bgrBuilding, "address", 208).BoldProperties.Renderer = bsrAddress;
        GroupBox1 = new GroupBox { Name = "GroupBox1", Text = "Building", Bounds = new Rectangle(288, 8, 289, 153) };
        bedAddress = new TBoldEdit { Name = "bedAddress", Bounds = new Rectangle(16, 32, 121, 21) };
        bedAddress.BoldHandle = blhAllBuilding;
        bedAddress.BoldProperties.Expression = "address";
        blbBuildingOwners = new TBoldListBox { Name = "blbBuildingOwners", Bounds = new Rectangle(16, 72, 121, 65), ContextMenuStrip = MultiLinkPopup };
        blbBuildingOwners.BoldHandle = blhOwners;
        blbBuildingOwners.BoldProperties.InternalDrag = false;
        blbBuildingOwners.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        rgNameRepresentation = new TRadioGroup("Name representation", ["First Name", "Last Name", "Full Name"])
        {
            Name = "rgNameRepresentation",
            Bounds = new Rectangle(152, 64, 129, 81),
            ItemIndex = 1,
        };
        rgNameRepresentation.ItemIndexChanged += rgNameRepresentationClick;
        GroupBox1.Controls.AddRange(
        [
            new Label { Text = "Address", Bounds = new Rectangle(16, 16, 60, 13) },
            new Label { Text = "Owners", Bounds = new Rectangle(16, 56, 60, 13) },
            bedAddress, blbBuildingOwners, rgNameRepresentation,
        ]);
        tabBuilding.Controls.AddRange([bgrBuilding, GroupBox1]);

        // All residential buildings, and the current one.
        tabResidentialBuilding = new TabPage { Name = "tabResidentialBuilding", Text = "Residential Buildings" };
        bgrResidentialBuilding = new TBoldGrid
        {
            Name = "bgrResidentialBuilding",
            Bounds = new Rectangle(8, 8, 269, 153),
            AddNewAtEnd = false,
            BoldAutoColumns = false,
            BoldShowConstraints = false,
            ContextMenuStrip = BuildingPopup,
        };
        bgrResidentialBuilding.BoldHandle = blhAllResidentialBuilding;
        bgrResidentialBuilding.BoldProperties.InternalDrag = false;
        bgrResidentialBuilding.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        Column(bgrResidentialBuilding, "address", 79).BoldProperties.Renderer = bsrAddress;
        Column(bgrResidentialBuilding, "", 70, "Rent/Resident").BoldProperties.Renderer = bsrRentPerResident;
        Column(bgrResidentialBuilding, "if ocliskindof(Residential_Building) then totalRent else 0 endif", 37, "Rent").BoldProperties.Renderer = HighRentRenderer;
        Column(bgrResidentialBuilding, "residents.Assets->Sum ", 42, "tot $");
        BuildingGroup = new GroupBox { Name = "BuildingGroup", Text = "Building", Bounds = new Rectangle(288, 8, 289, 153) };
        bedAddress2 = new TBoldEdit { Name = "bedAddress2", Bounds = new Rectangle(16, 32, 121, 21) };
        bedAddress2.BoldHandle = blhAllResidentialBuilding;
        bedAddress2.BoldProperties.Expression = "address";
        blbBuildingOwners2 = new TBoldListBox { Name = "blbBuildingOwners2", Bounds = new Rectangle(16, 72, 121, 65), ContextMenuStrip = MultiLinkPopup };
        blbBuildingOwners2.BoldHandle = blhOwners2;
        blbBuildingOwners2.BoldProperties.DropMode = TBoldDropMode.bdpInsert;
        blbBuildingOwners2.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        blbBuildingResidents = new TBoldListBox { Name = "blbBuildingResidents", Bounds = new Rectangle(152, 72, 121, 65), ContextMenuStrip = MultiLinkPopup };
        blbBuildingResidents.BoldHandle = blhResidents;
        blbBuildingResidents.BoldProperties.DropMode = TBoldDropMode.bdpInsert;
        blbBuildingResidents.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        blbBuildingResidents.BoldRowProperties.Expression = "lastName";
        btnChargeRent = new Button { Name = "btnChargeRent", Text = "Charge Rent", Bounds = new Rectangle(200, 32, 77, 25) };
        btnChargeRent.Click += btnChargeRentClick;
        BuildingGroup.Controls.AddRange(
        [
            new Label { Text = "Address", Bounds = new Rectangle(16, 16, 60, 13) },
            new Label { Text = "Owners", Bounds = new Rectangle(16, 56, 60, 13) },
            new Label { Text = "Residents", Bounds = new Rectangle(152, 56, 60, 13) },
            bedAddress2, blbBuildingOwners2, blbBuildingResidents, btnChargeRent,
        ]);
        tabResidentialBuilding.Controls.AddRange([bgrResidentialBuilding, BuildingGroup]);
        PageControl1.TabPages.AddRange([tabBuilding, tabResidentialBuilding]);
        PageControl1.SelectedTab = tabResidentialBuilding;

        // Saving, the progress of the database, undo and redo, opening the system and creating the database.
        BoldActivateSystemAction1 = new TBoldActivateSystemAction(components)
        {
            Caption = "Open system",
            BoldSystemHandle = system,
            OpenCaption = "Open system",
            CloseCaption = "Close system",
            SaveQuestion = "There are dirty objects. Save them before exit?",
            SaveOnClose = TBoldSaveAction.saAsk,
            OnSystemOpened = BoldActivateSystemAction1SystemOpened,
        };
        BoldUpdateDBAction1 = new TBoldUpdateDBAction(components) { Caption = "Update DB", BoldSystemHandle = system };
        BoldGenerateSchemaAction1 = new TBoldGenerateSchemaAction(components)
        {
            Caption = "Create DB",
            BoldPersistenceHandleDB = DataModule1.BoldPersistenceHandleDB1,
        };
        btnUpdateDB = new Button { Name = "btnUpdateDB", Bounds = new Rectangle(4, 384, 89, 25) };
        TBoldActionLink.Link(btnUpdateDB, BoldUpdateDBAction1);
        pbdbNotification = new ProgressBar { Name = "pbdbNotification", Bounds = new Rectangle(104, 388, 105, 16), Visible = false };
        BoldPersistenceProgressNotifier1 = new TBoldPersistenceProgressNotifier(components)
        {
            ProgressBar = pbdbNotification,
            AnimationInterval = 100,
            PersistenceHandle = DataModule1.BoldPersistenceHandleDB1,
            MsgFetchObjects = "Fetching objects",
            MsgRetrieveIds = "Retrieving object ids",
            MsgUpdateDatabase = "Updating database",
        };
        btnCheckpoint = new Button { Name = "btnCheckpoint", Text = "Checkpoint", Bounds = new Rectangle(220, 384, 75, 25) };
        btnCheckpoint.Click += btnCheckpointClick;
        btnUnDo = new Button { Name = "btnUnDo", Text = "Undo", Bounds = new Rectangle(304, 372, 75, 25) };
        btnUnDo.Click += btnUnDoClick;
        btnRedo = new Button { Name = "btnRedo", Text = "Redo", Bounds = new Rectangle(304, 398, 75, 25) };
        btnRedo.Click += btnRedoClick;
        Button1 = new Button { Name = "Button1", Bounds = new Rectangle(404, 384, 89, 25) };
        TBoldActionLink.Link(Button1, BoldActivateSystemAction1);
        Button2 = new Button { Name = "Button2", Bounds = new Rectangle(504, 384, 89, 25) };
        TBoldActionLink.Link(Button2, BoldGenerateSchemaAction1);

        Controls.AddRange(
        [
            Label1, PersonCount, bgrPerson, CheckBox1, CheckBox2, PersonGroup, PageControl1,
            btnUpdateDB, pbdbNotification, btnCheckpoint, btnUnDo, btnRedo, Button1, Button2,
        ]);
        ResumeLayout(false);
        TResidential_Building.Evicted += Evicted;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TResidential_Building.Evicted -= Evicted;
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>A column of the grid on the expression, of the width, titled when a title is given.</summary>
    private static TBoldGridColumn Column(TBoldGrid grid, string expression, int width, string title = "")
    {
        TBoldGridColumn column = grid.AddColumn();
        column.BoldProperties.Expression = expression;
        column.Width = width;
        if (title != "")
        {
            column.Title.Caption = title;
        }

        return column;
    }

    private ContextMenuStrip Popup(params ToolStripItem[] items)
    {
        var menu = new ContextMenuStrip(components);
        menu.Items.AddRange(items);
        menu.Opening += PopupPopup;
        return menu;
    }

    private static ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler click)
    {
        var item = new ToolStripMenuItem(text) { ShortcutKeys = shortcut };
        item.Click += click;
        return item;
    }

    /// <summary>The message of the residential building whose resident ran out of money (Delphi's ShowMessage in ChargeRent).</summary>
    private static void Evicted(string message) => Dialogs.ShowMessage(message);

    private void newBuildingClick(object? Sender, EventArgs e)
    {
        Currentpopupcomponent = null;
        CurrentBuildingHandle().List!.AddNew();
    }

    private void NewPersonClick(object? Sender, EventArgs e)
    {
        Currentpopupcomponent = null;
        _ = new TPerson(null);
    }

    /// <summary>The control the menu item works on: the one whose popup menu opened, else (a shortcut key) the focused one.</summary>
    private Control? PopupComponent()
    {
        Control? result = Currentpopupcomponent ?? ActiveControl;
        Currentpopupcomponent = null;
        return result;
    }

    /// <summary>The current object of the grid or list box of the popup menu.</summary>
    private TBoldObject? CurrentObjectOfPopupComponent() => PopupComponent() switch
    {
        TBoldListBox { BoldHandle: { } handle } => handle.CurrentBoldObject,
        TBoldGrid { BoldHandle: { } handle } => handle.CurrentBoldObject,
        _ => null,
    };

    private void DeleteCurrentObject(object? Sender, EventArgs e) => CurrentObjectOfPopupComponent()?.Delete();

    /// <summary>Removes the current element of the list box from its list (an owner, a resident, an owned building).</summary>
    private void Remove(object? Sender, EventArgs e)
    {
        if (PopupComponent() is TBoldListBox { BoldHandle: { } handle })
        {
            handle.RemoveCurrentElement();
        }
    }

    /// <summary>Clears the link the edit shows (a person's home).</summary>
    private void SingleItemRemove(object? Sender, EventArgs e)
    {
        if (PopupComponent() is TBoldEdit { BoldHandle.Value: TBoldObjectReference reference })
        {
            reference.BoldObject = null;
        }
    }

    private void PopupPopup(object? Sender, CancelEventArgs e) => Currentpopupcomponent = (Sender as ContextMenuStrip)?.SourceControl;

    /// <summary>Unsaved changes are discarded when the user agrees to quit without them.</summary>
    private void FormCloseQuery(object? Sender, FormClosingEventArgs e)
    {
        if (!DataModule1.BoldSystemHandle1.Active)
        {
            return;
        }

        if (DataModule1.BoldSystemHandle1.System!.BoldDirty)
        {
            if (Dialogs.MessageDlg("You have unsaved changes, quit anyway?", TMsgDlgType.mtConfirmation,
                    DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == Bold.Controls.mrYes)
            {
                DataModule1.BoldSystemHandle1.System.Discard();
            }
            else
            {
                e.Cancel = true;
            }
        }
    }

    /// <summary>The current object of the grid or list box shows in its own form (a person in the form plugged in for persons).</summary>
    private void ShowInOwnWindow(object? Sender, EventArgs e)
    {
        if (CurrentObjectOfPopupComponent() is { } BoldObject)
        {
            BoldAFP.AutoFormProviderRegistry().FormForElement(BoldObject)?.Show();
        }
    }

    /// <summary>The rent each resident pays: "***" without a rent, "No Residents" without residents.</summary>
    private string bsrRentPerResidentGetAsString(TBoldElement? Element, int representation, string Expression)
    {
        string result = "";
        if (Element is TResidential_Building building)
        {
            if (building.M_TotalRent.IsNull)
            {
                result = "***";
            }
            else if (building.Residents.Count == 0)
            {
                result = "No Residents";
            }
            else
            {
                result = SysUtils.CurrToStr(building.TotalRent / building.Residents.Count);
            }
        }

        return result;
    }

    private void bsrRentPerResidentHoldsChangedValue(TBoldElement? Element, int representation, string Expression, TBoldSubscriber? subscriber)
    {
        if (Element is TResidential_Building building)
        {
            building.M_TotalRent.RegisterModifiedValueHolder(subscriber);
        }
    }

    private void bsrRentPerResidentReleaseChangedValue(TBoldElement? Element, int representation, string Expression, TBoldSubscriber? Subscriber)
    {
        if (Element is TResidential_Building building)
        {
            building.M_TotalRent.UnRegisterModifiedValueHolder(Subscriber);
        }
    }

    private void bsrRentPerResidentSubscribe(TBoldElement? Element, int representation, string Expression, TBoldSubscriber? Subscriber)
    {
        if (Element is TResidential_Building)
        {
            Element.SubscribeToExpression("totalRent", Subscriber, false);
            Element.SubscribeToExpression("residents", Subscriber, false);
        }
    }

    /// <summary>A new rent per resident sets the total rent.</summary>
    private void bsrRentPerResidentSetAsString(TBoldElement? Element, string value, int representation, string Expression)
    {
        if (Element is TResidential_Building building)
        {
            building.TotalRent = SysUtils.StrToCurr(value) * building.Residents.Count;
        }
    }

    /// <summary>The name shown for the owners: first name, last name or both.</summary>
    private void rgNameRepresentationClick(object? Sender, EventArgs e)
    {
        switch (rgNameRepresentation.ItemIndex)
        {
            case 0:
                blbBuildingOwners.BoldRowProperties.Expression = "firstName";
                break;
            case 1:
                blbBuildingOwners.BoldRowProperties.Expression = "lastName";
                break;
            case 2:
                blbBuildingOwners.BoldRowProperties.Expression = "firstName + ' ' + lastName";
                break;
        }
    }

    private void btnChargeRentClick(object? Sender, EventArgs e)
    {
        if (blhAllResidentialBuilding.CurrentBoldObject is TResidential_Building building)
        {
            building.ChargeRent();
        }
    }

    private void CheckBox1Click(object? Sender, EventArgs e) =>
        blhAllPerson.BoldFilter = CheckBox1.Checked ? DataModule1.IsRichFilter : null;

    private void CheckBox2Click(object? Sender, EventArgs e) =>
        blhAllPerson.BoldComparer = CheckBox2.Checked ? DataModule1.NameComparer : null;

    /// <summary>The rent per resident may change when there are residents to share it.</summary>
    private bool bsrRentPerResidentMayModify(TBoldElement element, int representation, string Expression, TBoldSubscriber? Subscriber) =>
        element is TResidential_Building building && building.Residents.Count > 0;

    private bool bsrRentPerResidentValidateCharacter(TBoldElement? element, string value, int representation, string Expression) =>
        value.Length > 0 && (char.IsAsciiDigit(value[0]) || value[0] is '-' or '+' or 'e' or 'E' || value[0] == SysUtils.FormatSettings.DecimalSeparator);

    private bool bsrRentPerResidentValidateString(TBoldElement? element, string value, int representation, string Expression) =>
        SysUtils.TryStrToCurr(value, out _);

    /// <summary>The sum of the residents' assets (a renderer the example declares but no column uses).</summary>
    private string bsrResidentsTotalAssetsGetAsString(TBoldElement? element, int representation, string Expression)
    {
        decimal Sum = 0;
        if (element is TResidential_Building building)
        {
            for (int i = 0; i < building.Residents.Count; i++)
            {
                Sum += building.Residents[i]!.Assets;
            }
        }

        return SysUtils.CurrToStr(Sum);
    }

    private void bsrResidentsTotalAssetsSubscribe(TBoldElement? element, int representation, string Expression, TBoldSubscriber? Subscriber)
    {
        if (element is TResidential_Building building)
        {
            building.SubscribeToExpression("residents", Subscriber, true);
            for (int i = 0; i < building.Residents.Count; i++)
            {
                building.Residents[i]!.SubscribeToExpression("assets", Subscriber, false);
            }
        }
    }

    /// <summary>An address with "Bold" in it has an aqua background.</summary>
    private void bsrAddressSetColor(TBoldElement? element, ref Color aColor, int representation, string Expression)
    {
        if (element is TBuilding building && building.Address.Contains("Bold", StringComparison.Ordinal))
        {
            aColor = Color.Aqua;
        }
    }

    /// <summary>An address with "Bold" in it is bold, with "Rose" red, with "Select" green.</summary>
    private void bsrAddressSetFont(TBoldElement? element, TFont aFont, int representation, string Expression)
    {
        if (element is TBuilding building)
        {
            string address = building.M_Address.AsString;
            if (address.Contains("Bold", StringComparison.Ordinal))
            {
                aFont.Style = aFont.Style + TFontStyle.fsBold;
            }

            if (address.Contains("Rose", StringComparison.Ordinal))
            {
                aFont.Color = Color.Red;
            }

            if (address.Contains("Select", StringComparison.Ordinal))
            {
                aFont.Color = Color.Green;
            }
        }
    }

    /// <summary>The page shown says which buildings a new one joins.</summary>
    private TBoldListHandle CurrentBuildingHandle() =>
        PageControl1.SelectedTab == tabBuilding ? blhAllBuilding : blhAllResidentialBuilding;

    /// <summary>A rent of 1500 or more on silver.</summary>
    private void HighRentRendererSetColor(TBoldElement? Element, ref Color AColor, int Representation, string Expression)
    {
        if (Element is TResidential_Building building)
        {
            AColor = building.TotalRent >= 1500 ? Color.Silver : Color.White;
        }
    }

    /// <summary>A rent of 1500 or more in red.</summary>
    private void HighRentRendererSetFont(TBoldElement? Element, TFont AFont, int Representation, string Expression)
    {
        if (Element is TResidential_Building building)
        {
            AFont.Color = building.TotalRent >= 1500 ? Color.Red : SystemColors.WindowText;
        }
    }

    /// <summary>
    /// The opened system records changes for the Undo and Redo buttons (a system's undo handler is off until enabled).
    /// Delphi's handler called Randomize, which .NET's Random does not need.
    /// </summary>
    private void BoldActivateSystemAction1SystemOpened(object? Sender) =>
        DataModule1.BoldSystemHandle1.System!.UndoHandlerInterface.Enabled = true;

    private void btnCheckpointClick(object? Sender, EventArgs e) =>
        DataModule1.BoldSystemHandle1.System?.UndoHandlerInterface.SetCheckPoint();

    private void btnUnDoClick(object? Sender, EventArgs e) =>
        DataModule1.BoldSystemHandle1.System?.UndoHandlerInterface.UndoLatest();

    private void btnRedoClick(object? Sender, EventArgs e) =>
        DataModule1.BoldSystemHandle1.System?.UndoHandlerInterface.RedoLatest();
}

/// <summary>A group of radio buttons, one per item, and the index of the checked one (Delphi's TRadioGroup).</summary>
public sealed class TRadioGroup : GroupBox
{
    private readonly List<RadioButton> fButtons = [];
    private bool fSetting;

    public TRadioGroup(string caption, string[] items)
    {
        Text = caption;
        for (int i = 0; i < items.Length; i++)
        {
            var button = new RadioButton { Text = items[i], Bounds = new Rectangle(8, 16 + i * 20, 110, 18) };
            button.CheckedChanged += ButtonCheckedChanged;
            fButtons.Add(button);
            Controls.Add(button);
        }
    }

    /// <summary>Raised when the user checks another item.</summary>
    public event EventHandler? ItemIndexChanged;

    /// <summary>The index of the checked item; -1 when none.</summary>
    public int ItemIndex
    {
        get => fButtons.FindIndex(b => b.Checked);
        set
        {
            fSetting = true;
            try
            {
                for (int i = 0; i < fButtons.Count; i++)
                {
                    fButtons[i].Checked = i == value;
                }
            }
            finally
            {
                fSetting = false;
            }
        }
    }

    /// <summary>The buttons, in item order.</summary>
    public IReadOnlyList<RadioButton> Buttons => fButtons;

    private void ButtonCheckedChanged(object? sender, EventArgs e)
    {
        if (!fSetting && sender is RadioButton { Checked: true })
        {
            ItemIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
