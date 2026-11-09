// The desk's window: four grids on list handles that follow each other (the books of the current author, the loans of
// the current member), navigators, and buttons for lending, returning, undo, redo and saving.
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Bold;
using LibraryClasses;

namespace Library.Desk;

/// <summary>The library desk: authors and their books, members and their loans.</summary>
public sealed class DeskForm : Form
{
    private readonly Container components = new();
    private readonly LibraryData fData;

    // The handles: lists that follow the system, and the current object of the list they hang from.
    public readonly TBoldListHandle Authors;
    public readonly TBoldListHandle Books;
    public readonly TBoldListHandle Members;
    public readonly TBoldListHandle Loans;

    public readonly TBoldGrid AuthorGrid;
    public readonly TBoldGrid BookGrid;
    public readonly TBoldGrid MemberGrid;
    public readonly TBoldGrid LoanGrid;
    public readonly Button LendButton;
    public readonly Button ReturnButton;
    public readonly Button UndoButton;
    public readonly Button RedoButton;
    public readonly Button SaveButton;

    public DeskForm(LibraryData data)
    {
        fData = data;
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(960, 600);
        Text = $"Library desk: {data.Where}";
        StartPosition = FormStartPosition.CenterScreen;

        TBoldSystemHandle system = data.SystemHandle;
        Authors = new TBoldListHandle(components) { RootHandle = system, Expression = "Author.allInstances" };
        Books = new TBoldListHandle(components) { RootHandle = Authors, Expression = "books" };
        Members = new TBoldListHandle(components) { RootHandle = system, Expression = "Member.allInstances" };
        Loans = new TBoldListHandle(components) { RootHandle = Members, Expression = "loans" };

        // The grids: a column is an OCL expression on the row's object. A double click opens the object's auto form.
        AuthorGrid = Grid(Authors);
        Column(AuthorGrid, "name", "Author", 200);
        Column(AuthorGrid, "bookCount", "Books", 50);
        BookGrid = Grid(Books);
        Column(BookGrid, "title", "Title", 240);
        Column(BookGrid, "year", "Year", 50);
        Column(BookGrid, "copies", "Copies", 55);
        Column(BookGrid, "available", "Available", 65);
        MemberGrid = Grid(Members);
        MemberGrid.BoldShowConstraints = true;   // a column that marks a member who breaks a constraint (more than three loans)
        Column(MemberGrid, "name", "Member", 200);
        Column(MemberGrid, "loanCount", "Loans", 50);
        LoanGrid = Grid(Loans);
        Column(LoanGrid, "book.title", "Book", 240);
        Column(LoanGrid, "due", "Due", 90);
        Column(LoanGrid, "overdue", "Overdue", 65);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.Controls.Add(Group("Authors", AuthorGrid, Authors), 0, 0);
        layout.Controls.Add(Group("Books by the author", BookGrid, Books), 1, 0);
        layout.Controls.Add(Group("Members", MemberGrid, Members), 0, 1);
        layout.Controls.Add(Group("Loans of the member", LoanGrid, null), 1, 1);

        // The buttons: lending and returning are the desk's own; undo, redo and saving are Bold's actions, which enable
        // their buttons when there is something to do.
        LendButton = new Button { Text = "Lend the book to the member", AutoSize = true };
        LendButton.Click += LendClick;
        ReturnButton = new Button { Text = "Return the loan", AutoSize = true };
        ReturnButton.Click += ReturnClick;
        UndoButton = new Button { AutoSize = true };
        TBoldActionLink.Link(UndoButton, new TBoldUndoAction(components) { BoldSystemHandle = system });
        RedoButton = new Button { AutoSize = true };
        TBoldActionLink.Link(RedoButton, new TBoldRedoAction(components) { BoldSystemHandle = system });
        SaveButton = new Button { AutoSize = true };
        TBoldActionLink.Link(SaveButton, new TBoldUpdateDBAction(components) { Caption = "Save", BoldSystemHandle = system });
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(4) };
        buttons.Controls.AddRange([LendButton, ReturnButton, UndoButton, RedoButton, SaveButton]);

        Controls.Add(layout);
        Controls.Add(buttons);
        ResumeLayout(false);
        FormClosing += AskToSave;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private static TBoldGrid Grid(TBoldListHandle handle)
    {
        var grid = new TBoldGrid { BoldAutoColumns = false, AddNewAtEnd = false, Dock = DockStyle.Fill };
        grid.BoldHandle = handle;
        grid.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        return grid;
    }

    /// <summary>A column on the expression. A new grid has its fixed column and one empty column, which the first column takes.</summary>
    private static TBoldGridColumn Column(TBoldGrid grid, string expression, string title, int width)
    {
        TBoldGridColumn column = grid.Columns.Count == 2 && grid.Columns[1].BoldProperties.Expression == "" && string.IsNullOrEmpty(grid.Columns[1].Title.Caption)
            ? grid.Columns[1]
            : grid.AddColumn();
        column.BoldProperties.Expression = expression;
        column.Title.Caption = title;
        column.Width = width;
        return column;
    }

    /// <summary>A titled box with the grid, and a navigator over the list when one is given (first, previous, next, last, new, delete).</summary>
    private static GroupBox Group(string title, TBoldGrid grid, TBoldListHandle? navigated)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill };
        group.Controls.Add(grid);
        if (navigated is not null)
        {
            group.Controls.Add(new TBoldNavigator { BoldHandle = navigated, Dock = DockStyle.Top });
        }

        return group;
    }

    /// <summary>The text the grid shows in the column of the expression, in the row (row 0 is the titles).</summary>
    public static string Cell(TBoldGrid grid, string expression, int row)
    {
        for (int column = 0; column < grid.Columns.Count; column++)
        {
            if (grid.Columns[column].BoldProperties.Expression == expression)
            {
                return grid.CellText(column, row);
            }
        }

        return "";
    }

    /// <summary>Whether the window shows the sample library a new database starts with (for the tests).</summary>
    public bool ShowsTheSampleLibrary() =>
        !AuthorGrid.HasGhostRow && Cell(AuthorGrid, "name", 1) == "Ursula K. Le Guin" && Cell(BookGrid, "title", 1) == "A Wizard of Earthsea"
        && Cell(MemberGrid, "name", 1) == "Ann" && Cell(LoanGrid, "book.title", 1) == "Solaris";

    /// <summary>The current member borrows the current book (Member.Borrow, in LibraryClasses).</summary>
    private void LendClick(object? sender, EventArgs e)
    {
        if (Members.CurrentBoldObject is TMember member && Books.CurrentBoldObject is TBook book)
        {
            fData.System.UndoHandlerInterface.SetCheckPoint();
            if (member.Borrow(book) is null)
            {
                Dialogs.ShowMessage($"No copy of {book.Title} is left.");
            }
        }
    }

    /// <summary>The current loan ends: the book comes back.</summary>
    private void ReturnClick(object? sender, EventArgs e)
    {
        if (Loans.CurrentBoldObject is TLoan loan)
        {
            fData.System.UndoHandlerInterface.SetCheckPoint();
            loan.Delete();
        }
    }

    /// <summary>Unsaved changes are saved or dropped as the user says; Cancel keeps the window open.</summary>
    private void AskToSave(object? sender, FormClosingEventArgs e)
    {
        if (!fData.SystemHandle.Active || !fData.System.BoldDirty)
        {
            return;
        }

        int answer = Dialogs.MessageDlg("Save the changes?", TMsgDlgType.mtConfirmation,
            DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo, TMsgDlgBtn.mbCancel), 0);
        if (answer == Bold.Controls.mrYes)
        {
            fData.System.UpdateDatabase();
        }
        else if (answer == Bold.Controls.mrNo)
        {
            fData.System.Discard();
        }
        else
        {
            e.Cancel = true;
        }
    }
}
