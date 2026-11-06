// Delphi: Source/UMLModel/Editor/BoldUMLModelValidationForm.pas
// Port map: CSharp/port/units/BoldUMLModelValidationForm.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelValidationForm", Symbol = "TfrmValidation.Notification", Note = "The form hears the model component's Disposed event.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelValidationForm", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>Called with an element the user chose in the list of violations.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldUMLElementClickedEvent(Component? sender, TBoldUMLObject element);

/// <summary>Validates a model component's model.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldValidationCallBack(TBoldModel ModelHandle);

/// <summary>The violations a validator found in the model, the worst first; double-clicking one shows its element.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code; a grid of the UML graph's violations, shown again when the model is validated.")]
public class TfrmValidation : Form
{
    private static TfrmValidation? G_ValidationForm;

    /// <summary>The owner of validation forms made where no form is at hand (Delphi's G_ValidationFormDefaultOwner).</summary>
    [Delphi(PortStatus.Ported, Symbol = "G_ValidationFormDefaultOwner")]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static Form? G_ValidationFormDefaultOwner { get; set; }

    public readonly DataGridView BoldGrid1;
    public readonly StatusStrip StatusBar1;
    public readonly ToolStrip ToolBar1;
    public readonly ToolStripButton btReCheck;
    public readonly ToolStripButton btStayOnTop;
    public readonly ContextMenuStrip PopupMenu1;
    public readonly ToolStripMenuItem mnuCopyLogtoClipBoard;
    public readonly ToolStripMenuItem Copy1;
    public readonly ToolStripMenuItem Cut1;
    public readonly ToolStripMenuItem Paste1;
    private TBoldUMLElementClickedEvent? FOnElementClick;
    private TBoldModel? fModelComponent;
    private TBoldUMLModelGraph? fGraph;

    [Delphi(PortStatus.Ported, Symbol = "TfrmValidation.CreateWithModel", Note = "WinForms forms have no owner component; Owner, if a form, owns this one.")]
    public TfrmValidation(TBoldModel ModelComponent, Form? Owner)
    {
        Text = "Validation";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(603, 216);
        KeyPreview = true;
        if (Owner is { IsDisposed: false })
        {
            this.Owner = Owner;
        }

        BoldGrid1 = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        BoldGrid1.Columns.Add("Description", "Description");
        BoldGrid1.Columns.Add("Violation", "Violation");
        BoldGrid1.Columns.Add("Element", "Element");
        BoldGrid1.Columns[0].FillWeight = 300;
        StatusBar1 = new StatusStrip();
        btReCheck = new ToolStripButton("Recheck (F5)", null, btReCheckClick);
        btStayOnTop = new ToolStripButton("Stay on top", null, btStayOnTopClick) { CheckOnClick = true };
        ToolBar1 = new ToolStrip();
        ToolBar1.Items.AddRange([btReCheck, btStayOnTop]);
        mnuCopyLogtoClipBoard = new ToolStripMenuItem("Copy Log to ClipBoard", null, mnuCopyLogtoClipBoardClick);
        Cut1 = new ToolStripMenuItem("Cut", null, Cut1Click) { ShortcutKeys = Keys.Control | Keys.X };
        Copy1 = new ToolStripMenuItem("Copy", null, Copy1Click) { ShortcutKeys = Keys.Control | Keys.C };
        Paste1 = new ToolStripMenuItem("Paste", null, Paste1Click) { ShortcutKeys = Keys.Control | Keys.V };
        PopupMenu1 = new ContextMenuStrip();
        PopupMenu1.Items.AddRange([mnuCopyLogtoClipBoard, new ToolStripSeparator(), Cut1, Copy1, Paste1]);
        BoldGrid1.ContextMenuStrip = PopupMenu1;
        Controls.Add(BoldGrid1);
        Controls.Add(ToolBar1);
        Controls.Add(StatusBar1);
        BoldGrid1.CellDoubleClick += BoldGrid1DblClick;
        FormClosing += FormClose;
        KeyDown += FormKeyDown;
        fModelComponent = ModelComponent;
        fModelComponent.Disposed += ModelComponentDisposed;
        fGraph = ModelComponent.EnsuredUMLModel.Graph;
        fGraph.ModelValidated += BoldPlaceableSubscriber1Receive;
        ShowViolations();
    }

    [Delphi(PortStatus.Ported)]
    public TBoldModel? ModelComponent => fModelComponent;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLObject? UMLModel => GetUMLModel();

    [Delphi(PortStatus.Ported)]
    public TBoldUMLElementClickedEvent? OnElementClick => FOnElementClick;

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldValidationCallBack? ValidationProc { get; set; }

    [Delphi(PortStatus.Ported)]
    private TBoldUMLObject? GetUMLModel() => fModelComponent?.EnsuredUMLModel;

    /// <summary>The validation form of the model component; another model's form goes.</summary>
    [Delphi(PortStatus.Ported, Symbol = "EnsureValidationForm")]
    public static TfrmValidation EnsureValidationForm(TBoldModel ModelComponent, Form? Owner, TBoldUMLElementClickedEvent? OnElementClick)
    {
        if (G_ValidationForm is not null && (G_ValidationForm.IsDisposed || G_ValidationForm.ModelComponent != ModelComponent))
        {
            if (!G_ValidationForm.IsDisposed)
            {
                G_ValidationForm.Dispose();
            }

            G_ValidationForm = null;
        }

        G_ValidationForm ??= new TfrmValidation(ModelComponent, Owner);
        if (OnElementClick is not null)
        {
            G_ValidationForm.FOnElementClick = OnElementClick;
        }

        return G_ValidationForm;
    }

    /// <summary>The violations of the model, the worst first.</summary>
    public IReadOnlyList<TBoldUMLViolation> Violations =>
        fGraph is null ? [] : [.. fGraph.Violations.OrderByDescending(v => v.Severity)];

    private void ShowViolations()
    {
        BoldGrid1.Rows.Clear();
        foreach (TBoldUMLViolation violation in Violations)
        {
            int row = BoldGrid1.Rows.Add(violation.Description, SeverityText(violation.Severity), violation.ModelElement?.name ?? "");
            BoldGrid1.Rows[row].Tag = violation;
        }

        StatusBar1.Items.Clear();
        StatusBar1.Items.Add(new ToolStripStatusLabel($"{BoldGrid1.Rows.Count} violations"));
    }

    private static string SeverityText(TSeverity Severity) => Severity switch
    {
        TSeverity.sHint => "Hint",
        TSeverity.sWarning => "Warning",
        TSeverity.sError => "Error",
        _ => "None",
    };

    [Delphi(PortStatus.Ported)]
    private void BoldGrid1DblClick(object? Sender, DataGridViewCellEventArgs e)
    {
        if (OnElementClick is not null && e.RowIndex >= 0 && BoldGrid1.Rows[e.RowIndex].Tag is TBoldUMLViolation { ModelElement: { } element })
        {
            OnElementClick(this, element);
        }
    }

    /// <summary>Closing hides the form.</summary>
    [Delphi(PortStatus.Ported)]
    private void FormClose(object? Sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void FormKeyDown(object? Sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            Validate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btStayOnTopClick(object? Sender, EventArgs e) => TopMost = btStayOnTop.Checked;

    [Delphi(PortStatus.Ported)]
    private void btReCheckClick(object? Sender, EventArgs e) => Validate();

    /// <summary>Validates the model with the validation procedure; says so when it is sound.</summary>
    [Delphi(PortStatus.Ported)]
    public new void Validate()
    {
        if (ValidationProc is null)
        {
            throw new EBold("No validator registered");
        }

        ValidationProc(ModelComponent!);
        ShowViolations();
        if (UMLModel!.Graph.HighestSeverity() == TSeverity.sNone)
        {
            Dialogs.ShowMessage("Model validated OK!");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnuCopyLogtoClipBoardClick(object? Sender, EventArgs e) =>
        Clipboard.SetText(string.Join(Environment.NewLine, Violations.Select(v => $"{v.Description}\t{SeverityText(v.Severity)}\t{v.ModelElement?.name}")) + " ");

    [Delphi(PortStatus.Ported)]
    private void Cut1Click(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Cut();

    [Delphi(PortStatus.Ported)]
    private void Copy1Click(object? Sender, EventArgs e)
    {
        if (ActiveControl is TextBoxBase text)
        {
            text.Copy();
        }
        else if (BoldGrid1.GetClipboardContent() is { } content)
        {
            Clipboard.SetDataObject(content);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void Paste1Click(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Paste();

    /// <summary>The model was validated: the list shows the violations, and the form shows when there are any.</summary>
    [Delphi(PortStatus.Ported, Note = "Hears the UML graph's ModelValidated event.")]
    private void BoldPlaceableSubscriber1Receive()
    {
        if (IsDisposed)
        {
            return;
        }

        ShowViolations();
        if (BoldGrid1.Rows.Count > 0)
        {
            Show();
        }
    }

    private void ModelComponentDisposed(object? sender, EventArgs e) => fModelComponent = null;

    [Delphi(PortStatus.Ported, Symbol = "TfrmValidation.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (G_ValidationForm == this)
            {
                G_ValidationForm = null;
            }

            if (fGraph is not null)
            {
                fGraph.ModelValidated -= BoldPlaceableSubscriber1Receive;
                fGraph = null;
            }

            if (fModelComponent is not null)
            {
                fModelComponent.Disposed -= ModelComponentDisposed;
            }
        }

        base.Dispose(disposing);
    }
}
