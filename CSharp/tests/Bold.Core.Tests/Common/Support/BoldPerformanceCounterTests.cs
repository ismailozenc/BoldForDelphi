// Delphi: Source/Common/Support/BoldPerformanceCounter.pas
// Port map: CSharp/port/units/BoldPerformanceCounter.md
namespace Bold.Tests;

public class BoldPerformanceCounterTests
{
    private static void Spin(TimeSpan time)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.Elapsed < time)
        {
        }
    }

    [Test]
    [Covers("BoldPerformanceCounter", "TBoldPerformanceCounterState")]
    public void The_state_enum_has_the_Delphi_values()
    {
        PortAssert.EnumsMatchDelphi("Source/Common/Support/BoldPerformanceCounter.pas", typeof(TBoldPerformanceCounterState));
    }

    [Test]
    [Covers(typeof(TBoldPerformanceCounter), ".ctor", nameof(TBoldPerformanceCounter.Start), nameof(TBoldPerformanceCounter.Stop), nameof(TBoldPerformanceCounter.Seconds),
        nameof(TBoldPerformanceCounter.PercentOfOwner), nameof(TBoldPerformanceCounter.ChildByName), nameof(TBoldPerformanceCounter.Children),
        nameof(TBoldPerformanceCounter.ChildCount), nameof(TBoldPerformanceCounter.Reset), "GetSeconds", "GetPercentOfOwner", "GetChildByName",
        "GetChildren", "GetChildCount", "StartFromChild", "StopFromChild")]
    public void Counters_add_up_their_children_s_time()
    {
        using var root = new TBoldPerformanceCounter(null, "Root");
        TBoldPerformanceCounter fetch = root.ChildByName("Fetch");
        Assert.That(root.ChildByName("fetch"), Is.SameAs(fetch), "names in any case");
        TBoldPerformanceCounter update = root.ChildByName("Update");
        Assert.That((root.ChildCount, root.Children(0), root.Children(1)), Is.EqualTo((2, fetch, update)));

        fetch.Start();
        Spin(TimeSpan.FromMilliseconds(30));
        update.Start(); // the root keeps running while a child runs
        Spin(TimeSpan.FromMilliseconds(30));
        fetch.Stop();
        update.Stop();
        Assert.That(fetch.Seconds, Is.GreaterThanOrEqualTo(0.059));
        Assert.That(update.Seconds, Is.GreaterThanOrEqualTo(0.029));
        Assert.That(root.Seconds, Is.GreaterThanOrEqualTo(fetch.Seconds).And.LessThan(fetch.Seconds + update.Seconds), "the root ran once, while either ran");
        Assert.That(fetch.PercentOfOwner, Is.InRange(50.0, 100.0));
        Assert.That(root.PercentOfOwner, Is.EqualTo(100), "a counter without an owner");

        root.Reset();
        Assert.That((root.Seconds, fetch.Seconds, update.Seconds), Is.EqualTo((0.0, 0.0, 0.0)));
    }

    [Test]
    [Covers(typeof(TBoldPerformanceCounter), nameof(TBoldPerformanceCounter.AsString), nameof(TBoldPerformanceCounter.WriteToFile), "GetAsString", nameof(TBoldPerformanceCounter.Dispose))]
    public void A_counter_reports_itself_and_its_children()
    {
        var root = new TBoldPerformanceCounter(null, "Root");
        TBoldPerformanceCounter child = root.ChildByName("Child");
        child.Start();
        child.Stop();
        string[] lines = root.AsString.Split(BoldDefs.BOLDCRLF, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[0], Does.StartWith("ROOT").And.Contain("calls:     1"));
        Assert.That(lines[1], Does.StartWith("CHILD").And.Contain("percent of ROOT"));
        string file = Path.Combine(Path.GetTempPath(), $"counter-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.That(root.WriteToFile(file), Is.True);
            Assert.That(File.ReadAllText(file), Does.Contain("CHILD"));
        }
        finally
        {
            File.Delete(file);
        }

        child.Dispose();
        Assert.That(root.ChildCount, Is.Zero, "a counter that goes leaves its owner");
        root.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldPerformanceCounter), nameof(TBoldPerformanceCounter.BoldMainPerformanceCounter), nameof(TBoldPerformanceCounter.Finalize_))]
    public void There_is_a_main_counter()
    {
        Assert.That(TBoldPerformanceCounter.BoldMainPerformanceCounter(), Is.SameAs(TBoldPerformanceCounter.BoldMainPerformanceCounter()));
        TBoldPerformanceCounter.Finalize_();
        Assert.That(TBoldPerformanceCounter.BoldMainPerformanceCounter().ChildCount, Is.Zero, "made again");
    }
}
