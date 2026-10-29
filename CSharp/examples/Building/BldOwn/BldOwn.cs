// The program of the Buildings and Owners example (Delphi: examples/Delphi/Compound/Building/BldOwn.dpr).
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BldOwn;

public static class Program
{
    /// <summary>
    /// Runs the example on the SQLite database file given (bldown.db in the current folder by default). The first time,
    /// create the database with Create DB, then open the system.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string databaseFile = args.Length > 0 ? args[0] : "bldown.db";
        using var dataModule = new TDataModule1(new SqliteConnection($"Data Source={databaseFile}"));
        Datamod.DataModule1 = dataModule;
        using var mainForm = new Tallform();
        Mainform.allform = mainForm;
        Application.Run(mainForm);
    }
}
