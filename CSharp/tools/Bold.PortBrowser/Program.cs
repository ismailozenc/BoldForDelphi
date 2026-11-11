// The port browser: the Delphi source and the C# port side by side, linked both ways.
//
//   Bold.PortBrowser [repository] [--open <.pas or .cs file>] [--line <n>] [--symbol <Name or Type.Member>] [--results <folder>]
//
// Without a repository it takes the one the current folder or the program is in, or the one it showed last.
using System.Windows.Forms;

namespace Bold.PortBrowser;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Options options = Options.Parse(args);
        using var form = new MainForm(options);
        bool shown = false;
        form.FormClosing += (_, _) => shown = form.ShowsBothSides;
        Application.Run(form);
        // With --close (the tests), the exit code says whether both sides showed code.
        return options.Close && !shown ? 1 : 0;
    }
}
