// Delphi: Source/ObjectSpace/UtilsGUI/BoldOclPropEditor.pas
// Port map: CSharp/port/units/BoldOclPropEditor.md

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldCoreConsts;

[assembly: Delphi(PortStatus.Dropped, Unit = "BoldOclPropEditor", Symbol = "ModeType", Note = "Used only by code that is commented out in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclPropEditor", Symbol = "TBoldOclPropEditForm.WMGetMinMaxInfo", Note = "Set the smallest size of the form (525 by 264); the form's MinimumSize does that.")]

namespace Bold;

/// <summary>
/// Edits an OCL expression in a context: each change is checked (the parser's message, or the expression's type),
/// and a list offers the members of the type and the operations that apply to it, to add with a double click.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file; the list's images are drawn letters.")]
public class TBoldOclPropEditForm : Form
{
    private const string OCLInfoURL = "http://info.borland.com/techpubs/delphi/boldfordelphi/?Mech_OclAndSubscription.htm";
    private const string OCLInfoLocal = "doc\\oclinfo.html";

    private TBoldElementTypeInfo? fContext;
    private TBoldSystemTypeInfo? fSystemTypeInfo;
    private TBoldOcl? fOclEvaluator;
    private TBoldExternalVariableList? fVariables;
    private readonly ToolTip fHints = new();

    public bool ShowSyntaxErrors;
    public bool ShowTypes;
    public readonly Panel ExpBottomPanel;
    public readonly Panel TabControlPanel;
    public readonly TabControl ExprEditPageControl;
    public readonly TabPage ExpressionPage;
    public readonly Panel pnlLeftSide;
    public readonly Panel ExpParserPanel;
    public readonly Panel EditPanel;
    public readonly TextBox EditMemo;
    public readonly Panel pnlEditButtons;
    public readonly CheckBox Syntaxcb;
    public readonly Button Clear;
    public readonly Button RemoveLast;
    public readonly ImageList ImageList1;
    public readonly ContextMenuStrip PopupMenu1;
    public readonly ToolStripMenuItem Copymessagestoclipboard1;
    public readonly TextBox ParserMessages;
    public readonly Splitter Splitter1;
    public readonly Label ParserMsg;
    public readonly Splitter Splitter2;
    public readonly Panel pnlRightSide;
    public readonly ListBox SelectBox;
    public readonly Panel pnlShowTypes;
    public readonly CheckBox Typescb;
    public readonly PictureBox imgModelErrors;
    public readonly Panel pnlOKCancel;
    public readonly Button btnOK;
    public readonly Button btnCancel;
    public readonly Button btnShowInfo;

    /// <summary>An item of the list: its text and the OCL symbol of an operation.</summary>
    private sealed record TSelectItem(string Text, object? Data)
    {
        public override string ToString() => Text;
    }

    /// <summary>The form, with the expression page shown without its tab and the edit focused.</summary>
    [Delphi(PortStatus.Ported, Note = "No owner.")]
    public TBoldOclPropEditForm()
    {
        SuspendLayout();
        Text = "OCL Expression Editor";
        Size = new Size(644, 357);
        MinimumSize = new Size(525, 264);
        StartPosition = FormStartPosition.CenterScreen;

        ImageList1 = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
        foreach ((char letter, Color color) in new[] { ('M', Color.SteelBlue), ('C', Color.SeaGreen), ('A', Color.DarkOrange), ('E', Color.MediumPurple), ('O', Color.Firebrick) })
        {
            ImageList1.Images.Add(LetterImage(letter, color));
        }

        // The bottom panel: the model errors light and the buttons.
        imgModelErrors = new PictureBox
        {
            Name = "imgModelErrors", Bounds = new Rectangle(8, 0, 49, 33), Cursor = Cursors.Hand, SizeMode = PictureBoxSizeMode.Zoom,
            Image = SystemIcons.Warning.ToBitmap(), BackColor = Color.Transparent, Visible = false,
        };
        fHints.SetToolTip(imgModelErrors, "The model contains errors, click here to show details");
        imgModelErrors.Click += imgModelErrorsClick;
        btnShowInfo = new Button { Name = "btnShowInfo", Bounds = new Rectangle(23, 2, 75, 25), Text = "Show Info" };
        btnShowInfo.Click += InfoBrowserClick;
        btnOK = new Button { Name = "btnOK", Bounds = new Rectangle(106, 2, 81, 25), Text = "OK" };
        btnOK.Click += OKClick;
        btnCancel = new Button { Name = "btnCancel", Bounds = new Rectangle(194, 2, 81, 25), Text = "Cancel" };
        btnCancel.Click += CancelClick;
        pnlOKCancel = new Panel { Name = "pnlOKCancel", Dock = DockStyle.Right, Width = 286 };
        pnlOKCancel.Controls.AddRange([btnOK, btnCancel, btnShowInfo]);
        ExpBottomPanel = new Panel { Name = "ExpBottomPanel", Dock = DockStyle.Bottom, Height = 33 };
        ExpBottomPanel.Controls.AddRange([imgModelErrors, pnlOKCancel]);

        // The edit and the parser's messages on the left.
        EditMemo = new TextBox { Name = "EditMemo", Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, AcceptsReturn = true };
        EditMemo.TextChanged += EditMemoChange;
        EditMemo.Enter += EditMemoEnter;
        EditMemo.KeyPress += EditMemoKeyPress;
        EditMemo.KeyUp += EditMemoKeyUp;
        EditMemo.MouseDown += EditMemoMouseDown;
        EditPanel = new Panel { Name = "EditPanel", Dock = DockStyle.Fill };
        EditPanel.Controls.Add(EditMemo);
        ParserMsg = new Label { Name = "ParserMsg", Location = new Point(1, 4), AutoSize = true, Text = "Parser Message:" };
        Syntaxcb = new CheckBox { Name = "Syntaxcb", Bounds = new Rectangle(1, 19, 180, 17), Text = "Show Original Parser &Errors" };
        Syntaxcb.Click += SyntaxcbClick;
        RemoveLast = new Button { Name = "RemoveLast", Bounds = new Rectangle(253, 4, 81, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = "&Remove Last" };
        RemoveLast.Click += RemoveLastClick;
        Clear = new Button { Name = "Clear", Bounds = new Rectangle(345, 4, 81, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = "&Clear" };
        Clear.Click += ClearClick;
        pnlEditButtons = new Panel { Name = "pnlEditButtons", Dock = DockStyle.Top, Height = 37, Width = 427 };
        pnlEditButtons.Controls.AddRange([ParserMsg, Syntaxcb, RemoveLast, Clear]);
        Copymessagestoclipboard1 = new ToolStripMenuItem("Copy messages to clipboard", null, Copymessagestoclipboard1Click) { Name = "Copymessagestoclipboard1" };
        PopupMenu1 = new ContextMenuStrip();
        PopupMenu1.Items.Add(Copymessagestoclipboard1);
        ParserMessages = new TextBox
        {
            Name = "ParserMessages", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
            TabStop = false, BackColor = SystemColors.Control, Font = new Font("Courier New", 8.25f), ContextMenuStrip = PopupMenu1,
        };
        ExpParserPanel = new Panel { Name = "ExpParserPanel", Dock = DockStyle.Bottom, Height = 114 };
        ExpParserPanel.Controls.AddRange([ParserMessages, pnlEditButtons]);
        Splitter2 = new Splitter { Name = "Splitter2", Dock = DockStyle.Bottom, Height = 6 };
        pnlLeftSide = new Panel { Name = "pnlLeftSide", Dock = DockStyle.Fill };
        pnlLeftSide.Controls.AddRange([EditPanel, Splitter2, ExpParserPanel]);

        // The list of members and operations on the right.
        SelectBox = new ListBox { Name = "SelectBox", Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 17, Sorted = true, IntegralHeight = false };
        SelectBox.DoubleClick += SelectBoxDblClick;
        SelectBox.DrawItem += SelectBoxDrawItem;
        SelectBox.KeyDown += SelectBoxKeyDown;
        SelectBox.KeyPress += SelectBoxKeyPress;
        Typescb = new CheckBox { Name = "Typescb", Bounds = new Rectangle(1, 0, 159, 17), Text = "&Show Types" };
        Typescb.Click += TypescbClick;
        pnlShowTypes = new Panel { Name = "pnlShowTypes", Dock = DockStyle.Bottom, Height = 17 };
        pnlShowTypes.Controls.Add(Typescb);
        pnlRightSide = new Panel { Name = "pnlRightSide", Dock = DockStyle.Right, Width = 185 };
        pnlRightSide.Controls.AddRange([SelectBox, pnlShowTypes]);
        Splitter1 = new Splitter { Name = "Splitter1", Dock = DockStyle.Right, Width = 6 };

        ExpressionPage = new TabPage { Name = "ExpressionPage", Text = "Expression" };
        ExpressionPage.Controls.AddRange([pnlLeftSide, Splitter1, pnlRightSide]);
        ExprEditPageControl = new TabControl { Name = "ExprEditPageControl", Dock = DockStyle.Fill };
        ExprEditPageControl.TabPages.Add(ExpressionPage);
        TabControlPanel = new Panel { Name = "TabControlPanel", Dock = DockStyle.Fill, Padding = new Padding(5) };
        TabControlPanel.Controls.Add(ExprEditPageControl);
        Controls.AddRange([TabControlPanel, ExpBottomPanel]);
        ResumeLayout(false);

        // ExpressionPage.TabVisible := false: the page shows without a tab.
        ExprEditPageControl.Appearance = TabAppearance.FlatButtons;
        ExprEditPageControl.ItemSize = new Size(0, 1);
        ExprEditPageControl.SizeMode = TabSizeMode.Fixed;
        ExprEditPageControl.SelectedTab = ExpressionPage;
        ActiveControl = EditMemo;
        fOclEvaluator = null;
    }

    /// <summary>A small image with the letter (the kind of an item of the list).</summary>
    private static Bitmap LetterImage(char letter, Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using Graphics g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, 1, 1, 14, 14);
        using var font = new Font("Arial", 7.5f, FontStyle.Bold);
        TextRenderer.DrawText(g, letter.ToString(), font, new Rectangle(1, 1, 14, 14), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        return bitmap;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclPropEditForm.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (fOclEvaluator as IDisposable)?.Dispose();
            fOclEvaluator = null;
            ImageList1.Dispose();
            PopupMenu1.Dispose();
            fHints.Dispose();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void CleanParserMsg() => ParserMessages.Clear();

    /// <summary>The evaluator of the system's OCL, made on first use.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldOcl GetOclEvaluator() => fOclEvaluator ??= new TBoldOcl(SystemTypeInfo!, null);

    /// <summary>The context: the caption names it, the system is its system (or the default system's), and the expression is checked again.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetContext(TBoldElementTypeInfo? Context)
    {
        TBoldSystemTypeInfo? OldSystemTypeInfo = SystemTypeInfo;
        fContext = Context;
        if (Context is not null)
        {
            fSystemTypeInfo = Context.SystemTypeInfo as TBoldSystemTypeInfo;
            Text = "OCL expression editor - Context: " + Context.AsString; // do not localize
        }
        else
        {
            Text = "OCL expression editor - Context: <unknown>"; // do not localize
            CleanParserMsg();
            ParserMessages.AppendText(sNoContextNoSupport + Environment.NewLine);
        }

        if (SystemTypeInfo is null && TBoldSystem.DefaultSystem() is { } defaultSystem)
        {
            fSystemTypeInfo = defaultSystem.BoldType as TBoldSystemTypeInfo;
        }

        if (OldSystemTypeInfo != fSystemTypeInfo)
        {
            (fOclEvaluator as IDisposable)?.Dispose();
            fOclEvaluator = fSystemTypeInfo is not null ? new TBoldOcl(fSystemTypeInfo, null) : null;
        }

        EditMemoChange(null, EventArgs.Empty);
        imgModelErrors.Visible = SystemTypeInfo is not null && SystemTypeInfo.InitializationLog.Count > 0;
    }

    /// <summary>Adds the expression's lines to the edit and checks it.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetOclExpr(string Expr)
    {
        var lines = new List<string>(EditMemo.Lines);
        if (Expr != "")
        {
            lines.AddRange(Expr.Split(["\r\n", "\r", "\n"], StringSplitOptions.None));
        }

        EditMemo.Lines = [.. lines];
        int i = EditMemo.Text.Length;
        EditMemo.SelectionStart = Math.Max(0, i - 2);
        EditMemoChange(null, EventArgs.Empty);
    }

    /// <summary>The edit's lines, separated by carriage returns.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetOclExpr() => string.Join(BoldDefs.BOLDCR, EditMemo.Lines);

    /// <summary>The type of the expression in the context, with the parser's message (null when it does not parse).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? CheckOcl(string Ocl)
    {
        void ShowError(string Ocl, string ErrorPointer, string message)
        {
            var lines = new List<string>(Ocl.Split(["\r\n", "\r", "\n"], StringSplitOptions.None));
            int i = 0;
            while (i < lines.Count - 1 && ErrorPointer.Length > lines[i].Length)
            {
                ErrorPointer = ErrorPointer.Remove(0, Math.Min(ErrorPointer.Length, lines[i].Length + 2));
                i++;
            }

            lines.Add(message);
            if (ErrorPointer.Length > 10)
            {
                ErrorPointer = ErrorPointer.Remove(ErrorPointer.Length - 9, 7);
                ErrorPointer = ErrorPointer.Insert(ErrorPointer.Length - 1, "-------");
            }
            else
            {
                ErrorPointer += "-------";
            }

            lines.Insert(Math.Min(i + 1, lines.Count), ErrorPointer);
            ParserMessages.Lines = [.. lines];
        }

        CleanParserMsg();
        if (Ocl == "")
        {
            ParserMessages.AppendText(sEmptyExpression + Environment.NewLine);
            return Context;
        }

        TBoldElementTypeInfo? Result;
        try
        {
            Result = OclEvaluator.ExpressionType(Ocl, Context, true, Variables);
        }
        catch (EBoldOCLAbort e)
        {
            if (e.Message.Contains("SSYacc", StringComparison.Ordinal) || e.Message.Contains("SSLex", StringComparison.Ordinal)) // do not localize
            {
                if (ShowSyntaxErrors)
                {
                    ShowError(e.Ocl, e.ErrorPointer(), e.Message);
                }
                else
                {
                    ParserMessages.AppendText(sSyntaxError + Environment.NewLine);
                }
            }
            else
            {
                ShowError(e.Ocl, e.ErrorPointer(), e.Message);
            }

            return null;
        }
        catch (EBoldOCLInternalError e)
        {
            e.Ocl = "Internal Error: " + e.Ocl; // do not localize
            ShowError(e.Ocl, e.ErrorPointer(), e.Message);
            return null;
        }
        catch (EBoldOCLError e)
        {
            ShowError(e.Ocl, e.ErrorPointer(), e.Message);
            return null;
        }
        catch (Exception e)
        {
            ParserMessages.AppendText(e.Message + Environment.NewLine);
            return null;
        }

        ParserMessages.Lines =
        [
            sSyntaxOK, "",
            Result is not null ? SysUtils.Format(sCurrentTypeIsX, Result.AsString) : sCurrentTypeIsUnknown,
        ];
        ParserMessages.SelectionStart = 0;
        ParserMessages.SelectionLength = 0;
        return Result;
    }

    /// <summary>The operations that apply to the expression's type (or to its elements'), with their arguments.</summary>
    [Delphi(PortStatus.Ported)]
    private void OperationHelp(string Ocl)
    {
        if (Ocl == "")
        {
            return;
        }

        TBoldElementTypeInfo? ExpressionType = CheckOcl(Ocl);
        if (ExpressionType is null)
        {
            return;
        }

        TBoldSymbolDictionary? table = OclEvaluator.SymbolTable;
        if (table is not null)
        {
            for (int i = 0; i < table.Count; i++)
            {
                TBoldOclSymbol Symbol = table.Symbols(i);
                TBoldElementTypeInfo? first = Symbol.FormalArguments(0);
                if ((first is null || ExpressionType.ConformsTo(first) ||
                     (ExpressionType is TBoldListTypeInfo list && list.ListElementTypeInfo is not null && list.ListElementTypeInfo.ConformsTo(first))) &&
                    Symbol.IsPostfix)
                {
                    string Addition = "O | " + (Symbol.isDotNotation ? "." : "->") + Symbol.SymbolName; // do not localize
                    if (Symbol.NumberOfArgs > 1)
                    {
                        Addition += "(";
                        for (int j = 1; j < Symbol.NumberOfArgs; j++)
                        {
                            Addition += Symbol.FormalArguments(j) switch
                            {
                                TBoldListTypeInfo { ListElementTypeInfo: { } element } => "«List<" + element.ExpressionName + ">»,", // do not localize
                                TBoldListTypeInfo => "«List<AnyArg>»,", // do not localize
                                { } argument => "«" + argument.ExpressionName + "»,",
                                null => "«AnyArg»,", // do not localize
                            };
                        }

                        Addition = Addition[..^1] + ")";
                    }

                    SelectBox.Items.Add(new TSelectItem(Addition + " ", Symbol));
                }
            }
        }
    }

    /// <summary>The members of the expression's class (or of its elements'), or the classes of a system.</summary>
    [Delphi(PortStatus.Ported)]
    private void MemberHelp(string Ocl)
    {
        TBoldElementTypeInfo? ExpressionType = CheckOcl(Ocl);
        if (ExpressionType is null)
        {
            return;
        }

        if (ExpressionType is TBoldListTypeInfo list)
        {
            ExpressionType = list.ListElementTypeInfo;
        }

        string Prefix = Ocl != "" ? "." : "";
        if (ExpressionType is TBoldClassTypeInfo ClassInfo)
        {
            for (int I = 0; I < ClassInfo.AllMembers.Count; I++)
            {
                if (ClassInfo.AllMembers[I] is TBoldAttributeRTInfo Attr)
                {
                    string Exprname = "M | " + Prefix + Attr.ExpressionName; // do not localize
                    SelectBox.Items.Add(ShowTypes ? Exprname + ": " + Attr.BoldType!.ExpressionName : Exprname + " ");
                }
                else if (ClassInfo.AllMembers[I] is TBoldRoleRTInfo Role && Role.IsNavigable)
                {
                    string ExprName = "A | " + Prefix + Role.ExpressionName; // do not localize
                    if (ShowTypes)
                    {
                        SelectBox.Items.Add(Role.IsMultiRole
                            ? ExprName + ": List<" + Role.ClassTypeInfoOfOtherEnd!.ExpressionName + ">" // do not localize
                            : ExprName + ": " + Role.ClassTypeInfoOfOtherEnd!.ExpressionName);
                    }
                    else
                    {
                        SelectBox.Items.Add(ExprName + " ");
                    }
                }
            }
        }
        else if (ExpressionType is TBoldSystemTypeInfo SystemInfo)
        {
            for (int i = 0; i < SystemInfo.TopSortedClasses.Count; i++)
            {
                TBoldClassTypeInfo classType = SystemInfo.TopSortedClasses[i];
                SelectBox.Items.Add(ShowTypes ? "C | " + classType.ExpressionName + ": " + classType.DelphiName : "C | " + classType.ExpressionName + " "); // do not localize
            }
        }
    }

    /// <summary>The chosen member or operation is added to the end of the expression.</summary>
    [Delphi(PortStatus.Ported)]
    private void SelectBoxDblClick(object? Sender, EventArgs e)
    {
        if (Context is null || SelectBox.Items.Count == 0)
        {
            return;
        }

        if (SelectBox.SelectedIndex == -1)
        {
            SelectBox.SelectedIndex = 0;
        }

        string Addition = SelectBox.Items[SelectBox.SelectedIndex]!.ToString()!;
        int bar = Addition.IndexOf(" | ", StringComparison.Ordinal);
        if (bar != -1)
        {
            Addition = Addition[(bar + 3)..];
        }

        int space = Addition.IndexOf(' ');
        Addition = space > 0 ? Addition[..space] : "";
        if (Addition.EndsWith(':'))
        {
            Addition = Addition[..^1];
        }

        string[] lines = EditMemo.Lines;
        if (lines.Length == 0)
        {
            lines = [Addition];
        }
        else
        {
            lines[^1] += Addition;
        }

        EditMemo.Lines = lines;
        EditMemoChange(Sender, e);
    }

    [Delphi(PortStatus.Ported)]
    private void EditMemoEnter(object? Sender, EventArgs e) => EditMemoChange(Sender, e);

    [Delphi(PortStatus.Ported)]
    private void SelectBoxKeyPress(object? Sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == '\r')
        {
            SelectBoxDblClick(Sender, e);
        }
    }

    /// <summary>The expression is checked and the list filled again.</summary>
    [Delphi(PortStatus.Ported)]
    private void EditMemoChange(object? Sender, EventArgs e)
    {
        if (Context is null)
        {
            return;
        }

        string Ocl = EditMemo.Text.Trim();
        SelectBox.Items.Clear();
        MemberHelp(Ocl);
        OperationHelp(Ocl);
    }

    [Delphi(PortStatus.Ported)]
    private void ClearClick(object? Sender, EventArgs e)
    {
        EditMemo.Clear();
        EditMemoChange(Sender, e);
    }

    /// <summary>Removes the last step of the expression (back to its last dot, arrow or space).</summary>
    [Delphi(PortStatus.Ported)]
    private void RemoveLastClick(object? Sender, EventArgs e)
    {
        // rewrite this entire procedure...
        string line = EditMemo.Text.Trim();
        if (line == "")
        {
            return;
        }

        while (line != "" && line[^1] is not ('.' or ' ') && !line.EndsWith("->", StringComparison.Ordinal))
        {
            line = line[..^1];
        }

        if (line != "")
        {
            if (line[^1] == '.')
            {
                line = line[..^1];
            }
            else if (line.EndsWith("->", StringComparison.Ordinal))
            {
                line = line[..^2];
            }
        }

        EditMemo.Text = line.Trim();
    }

    [Delphi(PortStatus.Ported)]
    private void SyntaxcbClick(object? Sender, EventArgs e)
    {
        ShowSyntaxErrors = Syntaxcb.Checked;
        EditMemoChange(Sender, e);
    }

    /// <summary>Shows the types in the list, keeping the list's position.</summary>
    [Delphi(PortStatus.Ported)]
    private void TypescbClick(object? Sender, EventArgs e)
    {
        int OldIndex = SelectBox.SelectedIndex;
        int OldTop = SelectBox.TopIndex;
        ShowTypes = Typescb.Checked;
        EditMemoChange(Sender, e);
        SelectBox.SelectedIndex = OldIndex < SelectBox.Items.Count ? OldIndex : -1;
        SelectBox.TopIndex = Math.Min(OldTop, Math.Max(0, SelectBox.Items.Count - 1));
    }

    /// <summary>The caret in an argument placeholder («Type») selects the placeholder.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetSelection()
    {
        if (EditMemo.SelectionLength == 0)
        {
            string t = EditMemo.Text;
            int i = EditMemo.SelectionStart;
            while (i > 0 && t[i - 1] is not ('«' or '»'))
            {
                i--;
            }

            if (i == 0 || t[i - 1] == '»')
            {
                return;
            }

            int j = i + 1;
            while (j <= t.Length && t[j - 1] is not ('«' or '»'))
            {
                j++;
            }

            if (j > t.Length || t[j - 1] == '«')
            {
                return;
            }

            EditMemo.SelectionStart = i - 1;
            EditMemo.SelectionLength = j - i + 1;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void EditMemoMouseDown(object? Sender, MouseEventArgs e) => SetSelection();

    [Delphi(PortStatus.Ported)]
    private void EditMemoKeyPress(object? Sender, KeyPressEventArgs e)
    {
        SetSelection();
        if (e.KeyChar == '\u001B')
        {
            DialogResult = DialogResult.Cancel;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void OKClick(object? Sender, EventArgs e) => DialogResult = DialogResult.OK;

    [Delphi(PortStatus.Ported)]
    private void CancelClick(object? Sender, EventArgs e) => DialogResult = DialogResult.Cancel;

    [Delphi(PortStatus.Ported)]
    private void EditMemoKeyUp(object? Sender, KeyEventArgs e) => SetSelection();

    /// <summary>An item: the image of its kind and its text without the kind.</summary>
    [Delphi(PortStatus.Ported, Note = "The list's DrawItem event.")]
    private void SelectBoxDrawItem(object? Control, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= SelectBox.Items.Count)
        {
            return;
        }

        string StringfromSelect = SelectBox.Items[e.Index]!.ToString()!;
        int image = StringfromSelect.Length > 0 ? StringfromSelect[0] switch
        {
            'M' => 0, // Member
            'C' => 1, // Class
            'A' => 2, // Assoc/role
            'E' => 3, // Method
            'O' => 4, // Operation (OCL)
            _ => -1,
        } : -1;
        if (image >= 0)
        {
            ImageList1.Draw(e.Graphics, e.Bounds.Left + 2, e.Bounds.Top, image);
        }

        int bar = StringfromSelect.IndexOf(" | ", StringComparison.Ordinal);
        if (bar != -1)
        {
            StringfromSelect = StringfromSelect[(bar + 3)..];
        }

        TextRenderer.DrawText(e.Graphics, StringfromSelect, e.Font ?? Font, new Point(ImageList1.ImageSize.Width + e.Bounds.Left + 2, e.Bounds.Top), e.ForeColor, TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }

    /// <summary>F1 on an operation takes its help context.</summary>
    [Delphi(PortStatus.Ported, Note = "WinForms controls have no help context; it is kept in SelectBoxHelpContext.")]
    private void SelectBoxKeyDown(object? Sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1 && SelectBox.SelectedIndex != -1)
        {
            SelectBoxHelpContext = (SelectBox.Items[SelectBox.SelectedIndex] as TSelectItem)?.Data is TBoldOclSymbol symbol ? symbol.HelpContext : 0;
        }
    }

    /// <summary>The help context of the operation chosen with F1 (Delphi's SelectBox.HelpContext).</summary>
    public int SelectBoxHelpContext { get; private set; }

    [Delphi(PortStatus.Ported)]
    private void SetVariables(TBoldExternalVariableList? Value)
    {
        fVariables = Value;
        EditMemoChange(null, EventArgs.Empty);
    }

    /// <summary>The context, the expression and the parser's messages to the clipboard.</summary>
    [Delphi(PortStatus.Ported)]
    private void Copymessagestoclipboard1Click(object? Sender, EventArgs e)
    {
        string s = Context is not null ? "Context: " + Context.ExpressionName + BoldDefs.BOLDCRLF : ""; // do not localize
        Clipboard.SetText(s + "OCL Expression: " + BoldDefs.BOLDCRLF + EditMemo.Text + BoldDefs.BOLDCRLF + BoldDefs.BOLDCRLF + // do not localize
            "Parser messages: " + BoldDefs.BOLDCRLF + ParserMessages.Text); // do not localize
    }

    [Delphi(PortStatus.Ported)]
    private void imgModelErrorsClick(object? Sender, EventArgs e) =>
        Dialogs.ShowMessage(SysUtils.Format(sModelContainsErrors, BoldDefs.BOLDCRLF, BoldDefs.BOLDCRLF, SystemTypeInfo!.InitializationLog.Text));

    /// <summary>Opens the OCL documentation: the installed one, else the web page.</summary>
    [Delphi(PortStatus.Ported, Note = "Opens it with the shell (Process.Start), as Delphi's ShellExecute.")]
    private void InfoBrowserClick(object? Sender, EventArgs e)
    {
        string basePath = BoldRegistry.GetBoldBasePath();
        string URL = basePath != "" && File.Exists(basePath + "\\" + OCLInfoLocal) ? basePath + "\\" + OCLInfoLocal : OCLInfoURL;
        Process.Start(new ProcessStartInfo(URL) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Maximized });
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOcl OclEvaluator => GetOclEvaluator();

    [Delphi(PortStatus.Ported)]
    public TBoldElementTypeInfo? Context
    {
        get => fContext;
        set => SetContext(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystemTypeInfo? SystemTypeInfo => fSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    public string OclExpr
    {
        get => GetOclExpr();
        set => SetOclExpr(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldExternalVariableList? Variables
    {
        get => fVariables;
        set => SetVariables(value);
    }
}
