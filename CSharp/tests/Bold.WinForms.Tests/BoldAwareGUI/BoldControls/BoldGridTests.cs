// Delphi: Source/BoldAwareGUI/BoldControls/BoldGrid.pas
// Port map: CSharp/port/units/BoldGrid.md
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using BuildingClasses;

namespace Bold.Tests;

[NonParallelizable]
[Covers(typeof(TBoldCustomGrid), "*")]
[Covers(typeof(TBoldGridColumns), "*")]
[Covers(typeof(TBoldGridColumn), "*")]
[Covers(typeof(TBoldColumnTitle), "*")]
public class BoldGridTests
{
    /// <summary>A grid whose key handling and drawing a test can reach.</summary>
    private sealed class TestGrid : TBoldGrid
    {
        public int DrawnCells;

        public void PressKey(Keys key) => OnKeyDown(new KeyEventArgs(key));

        public override void DrawCell(int ACol, int aRow, Rectangle ARect, DataGridViewElementStates AState)
        {
            base.DrawCell(ACol, aRow, ARect, AState);
            DrawnCells++;
        }
    }

    private GuiSample Sample = null!;
    private TestGrid Grid = null!;
    private TestForm Form = null!;
    private TBoldListHandle People = null!;

    [SetUp]
    public void SetUp()
    {
        Sample = new GuiSample();
        Sample.NewPerson("Ann", "Smith");
        Sample.NewPerson("Bob", "Jones");
        Sample.NewPerson("Cid", "Brown");
        People = Sample.List("Person.allInstances->orderBy(firstName)");
        Grid = new TestGrid();
        Form = new TestForm(Grid);
    }

    [TearDown]
    public void TearDown()
    {
        Form.Dispose();
        Sample.Dispose();
    }

    private TBoldGridColumn Column(string expression, string title = "")
    {
        TBoldGridColumn column = Grid.AddColumn();
        column.BoldProperties.Expression = expression;
        if (title != "")
        {
            column.Title.Caption = title;
        }

        return column;
    }

    /// <summary>Shows the people's first names in column 1 and last names in column 2.</summary>
    private void ShowNames()
    {
        Grid.Columns[1].BoldProperties.Expression = "firstName";
        Column("lastName", "Last");
        Grid.BoldHandle = People;
        GuiSample.Display();
    }

    private string[] Cells(int col) => [.. Enumerable.Range(1, Grid.RowCount).Select(row => Grid.CellText(col, row))];

    private TPerson Person(int index) => (TPerson)People.List![index]!;

    [Test]
    public void A_new_grid_has_the_fixed_column_and_one_more_and_an_empty_row()
    {
        Assert.That(Grid.Columns.Count, Is.EqualTo(2));
        Assert.That(Grid.ColCount, Is.EqualTo(2));
        Assert.That(Grid.FixedCols, Is.EqualTo(1));
        Assert.That(Grid.Columns[0].BoldProperties.Renderer, Is.InstanceOf<TBoldFirstColumnRenderer>());
        Assert.That(Grid.Columns[0].Width, Is.EqualTo(17));
        Assert.That(Grid.HasGhostRow, Is.True);
        Assert.That(Grid.RowCount, Is.EqualTo(1));
        Assert.That(Grid.TitleRow, Is.EqualTo(0));
    }

    [Test]
    public void The_grid_shows_a_row_per_element_and_a_cell_per_column()
    {
        ShowNames();
        Assert.That(Grid.HasGhostRow, Is.False);
        Assert.That(Grid.RowCount, Is.EqualTo(3));
        Assert.That(Grid.CellText(1, 0), Is.EqualTo("firstName"), "a title is the expression until it is set");
        Assert.That(Grid.CellText(2, 0), Is.EqualTo("Last"));
        Assert.That(Cells(1), Is.EqualTo(new[] { "Ann", "Bob", "Cid" }));
        Assert.That(Cells(2), Is.EqualTo(new[] { "Smith", "Jones", "Brown" }));
        Assert.That(Grid.AsClipBoardText(), Is.EqualTo("firstName\tLast\t\r\nAnn\tSmith\t\r\nBob\tJones\t\r\nCid\tBrown\t\r\n"));
    }

    [Test]
    public void The_cells_and_rows_follow_the_list()
    {
        ShowNames();
        Person(1).FirstName = "Bea";
        GuiSample.Display();
        Assert.That(Cells(1), Is.EqualTo(new[] { "Ann", "Bea", "Cid" }));

        Sample.NewPerson("Dan", "White");
        GuiSample.Display();
        Assert.That(Grid.RowCount, Is.EqualTo(4));
        Assert.That(Cells(2), Is.EqualTo(new[] { "Smith", "Jones", "Brown", "White" }));
    }

    [Test]
    public void An_empty_list_shows_one_empty_row()
    {
        Grid.BoldHandle = Sample.List("Person.allInstances->select(p | p.firstName = 'Nobody')");
        GuiSample.Display();
        Assert.That(Grid.HasGhostRow, Is.True);
        Assert.That(Grid.RowCount, Is.EqualTo(1));
        Assert.That(Grid.CellText(1, 1), Is.EqualTo(""));
        Assert.That(Grid.CurrentBoldElement, Is.Null);
    }

    [Test]
    public void The_current_row_follows_the_handle_and_the_handle_follows_the_current_row()
    {
        ShowNames();
        Assert.That(Grid.Row, Is.EqualTo(1));
        Assert.That(Grid.Selected(0), Is.True, "the current row is selected");

        People.CurrentIndex = 2;
        GuiSample.Display();
        Assert.That(Grid.Row, Is.EqualTo(3));
        Assert.That(Grid.CurrentBoldElement, Is.SameAs(Person(2)));
        Assert.That(Grid.Selected(2), Is.True);
        Assert.That(Grid.Selected(0), Is.False);

        Grid.Row = 2;
        Assert.That(People.CurrentIndex, Is.EqualTo(1));
        Assert.That(Grid.Col, Is.EqualTo(1), "the fixed column never becomes the current one");
    }

    [Test]
    public void An_edited_cell_sets_its_value_when_the_editing_ends()
    {
        ShowNames();
        Grid.CurrentCell = Grid[1, 1];
        Assert.That(Grid.BeginEdit(true), Is.True);
        var editor = (TBoldInplaceEdit)Grid.EditingControl!;
        Assert.That(editor.Text, Is.EqualTo("Bob"));
        Assert.That(editor.MaxLength, Is.EqualTo(0).Or.GreaterThan(1000), "a first name has no length");
        editor.Text = "Bud";
        Assert.That(Grid.EndEdit(), Is.True);
        Assert.That(Person(1).FirstName, Is.EqualTo("Bud"));
        GuiSample.Display();
        Assert.That(Grid.CellText(1, 2), Is.EqualTo("Bud"));
    }

    [Test]
    public void The_editor_takes_no_more_than_a_string_attribute_s_length()
    {
        ShowNames();
        Grid.CurrentCell = Grid[2, 0];
        Assert.That(Grid.BeginEdit(true), Is.True);
        Assert.That(((TBoldInplaceEdit)Grid.EditingControl!).MaxLength, Is.EqualTo(25));
        Grid.CancelEdit();
    }

    [Test]
    public void A_read_only_column_is_not_edited()
    {
        ShowNames();
        Grid.Columns[1].ColReadOnly = true;
        Grid.CurrentCell = Grid[1, 0];
        Assert.That(Grid.BeginEdit(true), Is.False);
    }

    [Test]
    public void A_boolean_column_that_allows_it_shows_check_boxes_and_the_space_key_toggles_them()
    {
        Grid.Columns[1].BoldProperties.Expression = "isMarried";
        Grid.Columns[1].AllowCheckBox = true;
        Person(0).IsMarried = false;
        Grid.BoldHandle = People;
        GuiSample.Display();
        Assert.That(Grid.Columns[1].BoldProperties.Renderer, Is.InstanceOf<TBoldGridCheckBoxPainterRenderer>());

        Grid.Col = 1;
        Grid.Row = 1;
        Grid.PressKey(Keys.Space);
        Assert.That(Person(0).IsMarried, Is.True);
        GuiSample.Display();
        Grid.PressKey(Keys.Space);
        Assert.That(Person(0).M_IsMarried.IsNull || !Person(0).IsMarried, Is.True, "checked goes to null (or false)");
        Assert.That(Grid.BeginEdit(true), Is.False, "a check box column has no text editor");
    }

    [Test]
    public void With_auto_columns_the_grid_makes_a_column_per_attribute()
    {
        Grid.BoldAutoColumns = true;
        Grid.BoldHandle = People;
        GuiSample.Display();
        var type = (TBoldClassTypeInfo)People.ListElementType!;
        var expected = new List<string>();
        if (type.DefaultStringRepresentation != "")
        {
            expected.Add(type.ModelName);
        }

        for (int i = 0; i < type.AllMembers.Count; i++)
        {
            if (type.AllMembers[i].IsAttribute)
            {
                expected.Add(type.AllMembers[i].ModelName);
            }
        }

        Assert.That(expected, Does.Contain("FirstName").And.Contain("IsMarried"));
        Assert.That(Enumerable.Range(1, Grid.Columns.Count - 1).Select(c => Grid.Columns[c].Title.Caption), Is.EqualTo(expected));
        Assert.That(Grid.Columns[0].BoldProperties.Renderer, Is.InstanceOf<TBoldFirstColumnRenderer>(), "the fixed column stays");
        int firstName = expected.IndexOf("FirstName") + 1;
        Assert.That(Grid.Columns[firstName].BoldProperties.Expression, Is.EqualTo("firstName"));
        Assert.That(Cells(firstName), Is.EqualTo(new[] { "Ann", "Bob", "Cid" }));
    }

    [Test]
    [Covers(typeof(TBoldConstraintRenderer), "*")]
    public void With_constraints_shown_the_second_column_marks_broken_constraints()
    {
        var grid = new TestGrid { BoldShowConstraints = true };
        using var form = new TestForm(grid);
        Assert.That(grid.Columns.Count, Is.EqualTo(3));
        Assert.That(grid.Columns[1].Title.Caption, Is.EqualTo("§"));
        Assert.That(grid.Columns[1].BoldProperties.Expression, Is.EqualTo("constraints->exists(c|not c)"));
        Assert.That(grid.Columns[1].BoldProperties.Renderer, Is.InstanceOf<TBoldConstraintRenderer>());
        Assert.That(grid.Columns[1].ColReadOnly, Is.True);

        grid.Columns[2].BoldProperties.Expression = "firstName";
        grid.BoldHandle = People;
        GuiSample.Display();
        Assert.That(grid.CellFollowers(1, 0)!.RendererData.MayModify, Is.False);
        using var bitmap = new Bitmap(grid.Width, grid.Height);
        grid.DrawToBitmap(bitmap, new Rectangle(0, 0, grid.Width, grid.Height));
        Assert.That(grid.DrawnCells, Is.GreaterThanOrEqualTo(4 * grid.ColCount));
    }

    [Test]
    public void A_moved_column_takes_its_controller_and_its_width_along()
    {
        ShowNames();
        Grid.Columns[2].Width = 120;
        Grid.MoveColumn(2, 1);
        GuiSample.Display();
        Assert.That(Grid.Columns[1].BoldProperties.Expression, Is.EqualTo("lastName"));
        Assert.That(Grid.Columns[1].Width, Is.EqualTo(120));
        Assert.That(Cells(1), Is.EqualTo(new[] { "Smith", "Jones", "Brown" }));
        Assert.That(Cells(2), Is.EqualTo(new[] { "Ann", "Bob", "Cid" }));
    }

    [Test]
    public void A_deleted_column_leaves_the_grid()
    {
        ShowNames();
        Column("assets");
        Assert.That(Grid.ColCount, Is.EqualTo(4));
        Grid.DeleteColumn(1);
        GuiSample.Display();
        Assert.That(Grid.Columns.Count, Is.EqualTo(3));
        Assert.That(Grid.ColCount, Is.EqualTo(3));
        Assert.That(Cells(1), Is.EqualTo(new[] { "Smith", "Jones", "Brown" }));
    }

    [Test]
    public void Down_on_the_last_row_adds_an_element_when_the_grid_adds_new_at_end()
    {
        TBoldListHandle all = Sample.List("Person.allInstances");
        Grid.BoldHandle = all;
        Grid.AddNewAtEnd = true;
        GuiSample.Display();
        Grid.Row = Grid.RowCount; // the last row (the title row is row 0)
        Grid.PressKey(Keys.Down);
        GuiSample.Display();
        Assert.That(all.Count, Is.EqualTo(4));
        Assert.That(Grid.RowCount, Is.EqualTo(4));
    }

    [Test]
    public void A_lookup_column_offers_its_handle_s_elements_and_sets_the_choice()
    {
        Sample.NewHouse("Oak Road");
        TResidential_Building elm = Sample.NewHouse("Elm Street");
        TBoldGridColumn column = Grid.Columns[1];
        column.BoldProperties.Expression = "home";
        column.LookupHandle = Sample.List("Residential_Building.allInstances->orderBy(address)");
        column.LookUpProperties.Expression = "address";
        Grid.BoldHandle = People;
        GuiSample.Display();

        Grid.CurrentCell = Grid[1, 0];
        Assert.That(Grid.BeginEdit(true), Is.True);
        Assert.That(Grid.LookUpEditorActive, Is.True);
        ComboBox combo = ((TBoldInplaceEdit)Grid.EditingControl!).Combo;
        Assert.That(combo.Visible, Is.True);
        Assert.That(combo.Items.Cast<object>().Select(item => item.ToString()), Is.EqualTo(new[] { "Elm Street", "Oak Road" }));
        Assert.That(combo.SelectedIndex, Is.EqualTo(-1), "Ann has no home");

        combo.SelectedIndex = 0;
        typeof(ComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(combo, [EventArgs.Empty]);
        Assert.That(Person(0).Home, Is.SameAs(elm));
        Assert.That(combo.Visible, Is.False);
    }

    [Test]
    public void A_title_s_values_are_the_grid_s_until_set()
    {
        TBoldColumnTitle title = Grid.Columns[1].Title;
        Grid.Columns[1].BoldProperties.Expression = "firstName";
        Assert.That(title.Caption, Is.EqualTo("firstName"));
        Assert.That(title.Color, Is.EqualTo(Grid.FixedColor));
        Assert.That(title.Alignment, Is.EqualTo(TAlignment.taLeftJustify));

        title.Caption = "First name";
        title.Alignment = TAlignment.taCenter;
        Assert.That(Grid.Columns[1].AssignedValues.Contains(TBoldColumnValue.cvTitleCaption), Is.True);
        Assert.That(title.IsCaptionStored(), Is.True);
        Assert.That(title.Caption, Is.EqualTo("First name"));

        title.RestoreDefaults();
        Assert.That(title.Caption, Is.EqualTo("firstName"));
        Assert.That(title.Alignment, Is.EqualTo(TAlignment.taLeftJustify));
        Assert.That(title.IsCaptionStored(), Is.False);
    }

    [Test]
    public void A_column_is_made_only_in_a_grid_s_columns()
    {
        Assert.Throws<EBold>(() => _ = new TBoldGridColumn(new TCollection(typeof(TBoldGridColumn))));
    }

    [Test]
    [Covers(typeof(TBoldFirstColumnRenderer), "*")]
    [Covers(typeof(TBoldGridCheckBoxPainterRenderer), "*")]
    public void The_grid_draws_its_cells_through_the_renderers()
    {
        ShowNames();
        TBoldGridColumn married = Column("isMarried");
        married.AllowCheckBox = true;
        People.CurrentIndex = 1;
        GuiSample.Display();
        Assert.That(married.BoldProperties.Renderer, Is.InstanceOf<TBoldGridCheckBoxPainterRenderer>());

        using var bitmap = new Bitmap(Grid.Width, Grid.Height);
        Grid.DrawToBitmap(bitmap, new Rectangle(0, 0, Grid.Width, Grid.Height));
        Assert.That(Grid.DrawnCells, Is.GreaterThanOrEqualTo(4 * Grid.ColCount), "the titles and the rows are drawn");
    }
}
