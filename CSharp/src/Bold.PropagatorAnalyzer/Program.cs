// The propagator analyzer as a program (Delphi's PropagatorAnalyzer.dpr); a thread log given on the command line is analyzed at once.

using System.Windows.Forms;

namespace Bold;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var form = new TForm1();
        if (args.Length > 0)
        {
            form.Edit1.Text = Path.GetFullPath(args[0]);
            form.Shown += (_, _) => form.Button1.PerformClick();
        }

        Application.Run(form);
    }
}
