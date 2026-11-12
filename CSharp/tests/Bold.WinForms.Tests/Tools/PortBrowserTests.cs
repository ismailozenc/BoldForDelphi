// The port browser's window (CSharp/tools/Bold.PortBrowser) on this repository, in the test process and as a program.
// What it shows is tested without a window in tests/Bold.PortTool.Tests/PortBrowserTests.cs.
using System.Diagnostics;
using System.Windows.Forms;
using Bold.PortBrowser;

namespace Bold.Tests;

[NonParallelizable]
public class PortBrowserWindowTests
{
    /// <summary>Shows the browser started with the arguments, and waits until it has read the repository.</summary>
    private static MainForm Open(params string[] arguments)
    {
        var form = new MainForm(Options.Parse([RepoPaths.Root, .. arguments]));
        BoldAFPDefaultTests.ShowOffScreen(form);
        var waited = Stopwatch.StartNew();
        while (!form.Ready && waited.Elapsed < TimeSpan.FromMinutes(3))
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }

        Application.DoEvents();
        Assert.That(form.Ready, Is.True, "the repository was read");
        return form;
    }

    [Test]
    public void Started_at_a_Delphi_symbol_it_shows_the_code_of_both_sides()
    {
        using MainForm form = Open("--symbol", "TBoldSystem.UpdateDatabase");
        Assert.That(form.Text, Does.StartWith("Bold Port Browser - "));
        Assert.That(form.Current!.Side, Is.EqualTo(Side.Delphi));
        Assert.That(form.Current.Path, Is.EqualTo("Source/ObjectSpace/BORepresentation/BoldSystem.pas/TBoldSystem/UpdateDatabase"));
        Assert.That(form.ShowsBothSides, Is.True);
    }

    [Test]
    public void Started_at_a_line_of_a_CSharp_file_it_is_on_the_declaration_there()
    {
        BrowserNode ported;
        using (MainForm first = Open("--symbol", "TBoldSystem.UpdateDatabase"))
        {
            ported = first.Index!.CounterpartsOf(first.Current!).First();
        }

        using MainForm form = Open("--open", RepoPaths.Full(ported.File!), "--line", (ported.Line + 1).ToString());
        Assert.That(form.Current!.Side, Is.EqualTo(Side.CSharp));
        Assert.That(form.Current.Declaration!.FullName, Does.EndWith("TBoldSystem.UpdateDatabase"));
        Assert.That(form.ShowsBothSides, Is.True);
    }

    [Test]
    public void A_unit_that_was_replaced_shows_why_in_place_of_CSharp_code()
    {
        using MainForm form = Open("--open", RepoPaths.Full("Source/Common/Support/BoldGuard.pas"));
        Assert.That(form.Current!.Name, Is.EqualTo("BoldGuard.pas"));
        Assert.That(form.ShowsBothSides, Is.False, "there is no C# code to show");
        Assert.That(form.Index!.WhyNoCounterpart(form.Current), Does.Contain("garbage collector"));
    }
}

/// <summary>The port browser as a program, started as a user starts it: it shows a symbol on both sides and closes (its --close switch).</summary>
[NonParallelizable]
public class PortBrowserProgramTests
{
    [Test]
    public void The_program_shows_a_symbol_on_both_sides()
    {
        string program = Programs.Require("tools/Bold.PortBrowser", "Bold.PortBrowser");
        Process process = Programs.Start(program, RepoPaths.Root, "--symbol", "TBoldSystem.UpdateDatabase", "--close");
        try
        {
            Assert.That(process.WaitForExit(180_000), Is.True, Programs.OutputOf(process));
            Assert.That(process.ExitCode, Is.Zero, "both code panes showed a file:\n" + Programs.OutputOf(process));
        }
        finally
        {
            Programs.Stop(process);
        }
    }
}
