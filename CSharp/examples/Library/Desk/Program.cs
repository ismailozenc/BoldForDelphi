// Tutorial 4, a WinForms desk: the library's authors and books, members and loans in grids that follow the current
// object. It works on a database of its own, or on the library's server of tutorial 5 (the Network example's --serve).
//
//   dotnet run --project examples/Library/Desk [database file]          library-desk.db in the current folder by default
//   dotnet run --project examples/Library/Desk -- --server http://localhost:5000
namespace Library.Desk;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();   // before the first control
        int serverArgument = Array.IndexOf(args, "--server");
        string? server = serverArgument >= 0 && serverArgument + 1 < args.Length ? args[serverArgument + 1] : null;
        string databaseFile = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal) && argument != server)
            ?? "library-desk.db";
        using LibraryData data = server is null ? LibraryData.OnDatabase(databaseFile) : LibraryData.OnServer(server);
        using var form = new DeskForm(data);

        // --close is for the tests: the window closes itself once it has shown, and the exit code says whether it showed the library.
        int result = 0;
        if (args.Contains("--close"))
        {
            form.Shown += (_, _) =>
            {
                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    result = form.ShowsTheSampleLibrary() ? 0 : 1;
                    form.Close();
                };
                timer.Start();
            };
        }

        Application.Run(form);
        return result;
    }
}
