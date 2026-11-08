// The user guide's WinForms window on the packages alone. With --close it closes itself once it has been shown, so that
// deploy/check.sh can see it start and end without anyone at the screen.
using System.ComponentModel;
using Bold;
using Microsoft.Data.Sqlite;

// The project's root namespace, where WinForms generates ApplicationConfiguration, as in a new WinForms project.
namespace WinFormsSmoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();   // before the first control
        string databaseFile = Path.Combine(Path.GetTempPath(), $"bold-winforms-smoke-{Guid.NewGuid():N}.db");
        using var components = new Container();

        // The model, the database and the system handle, as in the first application.
        var model = new TBoldAbstractModel(components) { Name = "Library" };
        var lines = new TStringList();
        lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Library.bld"));
        model.ReadModel(lines);
        var connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile};Pooling=False"));
        var adapter = new TBoldDatabaseAdapterADO(components) { Connection = connection };
        adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
        database.CreateDataBaseSchema();
        var typeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model, UseGeneratedCode = false };
        var systemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = database, Active = true };
        TBoldObject author = systemHandle.System!.CreateNewObjectByExpressionName("Author");
        author.BoldMemberByExpressionName("name").AsString = "Ursula K. Le Guin";

        // The window of the guide.
        var authors = new TBoldListHandle(components) { RootHandle = systemHandle, Expression = "Author.allInstances" };
        var books = new TBoldListHandle(components) { RootHandle = authors, Expression = "books" };

        var form = new Form { Text = "Library", Width = 640, Height = 480 };
        var bookGrid = new TBoldGrid { BoldHandle = books, BoldAutoColumns = true, Dock = DockStyle.Fill, Parent = form };
        var authorGrid = new TBoldGrid { BoldHandle = authors, Dock = DockStyle.Left, Width = 240, Parent = form };
        TBoldGridColumn name = authorGrid.Columns[1];   // a new grid has its fixed column and one empty column
        name.BoldProperties.Expression = "name";
        name.Title.Caption = "Author";
        var navigator = new TBoldNavigator { BoldHandle = authors, Dock = DockStyle.Top, Parent = form };
        var save = new Button { Text = "Save", Dock = DockStyle.Bottom, Parent = form };
        TBoldActionLink.Link(save, new TBoldUpdateDBAction(components) { BoldSystemHandle = systemHandle });

        int result = 0;
        if (args.Contains("--close"))
        {
            form.Shown += (_, _) =>
            {
                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    // The author shows in the grid's first row once the followers have been displayed (row 0 is the title).
                    result = !authorGrid.HasGhostRow && authorGrid.CellText(1, 1) == "Ursula K. Le Guin" ? 0 : 1;
                    form.Close();
                };
                timer.Start();
            };
        }

        Application.Run(form);
        systemHandle.System!.Discard();
        systemHandle.Active = false;
        connection.Close();
        return result;
    }
}
