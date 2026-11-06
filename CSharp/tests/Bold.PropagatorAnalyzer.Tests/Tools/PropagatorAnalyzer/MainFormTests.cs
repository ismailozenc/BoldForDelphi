// Delphi: Tools/PropagatorAnalyzer/MainForm.pas
// Port map: CSharp/port/units/MainForm.md
using System.Windows.Forms;

// WinForms controls live on a single-threaded apartment thread.
[assembly: Apartment(ApartmentState.STA)]

namespace Bold.Tests;

/// <summary>The propagator analyzer finds the lock orders of a thread log that can deadlock.</summary>
[NonParallelizable]
public class MainFormTests
{
    /// <summary>Two threads that take A and B in opposite orders, and a third that takes C alone.</summary>
    private static readonly string[] Log =
    [
        "13:45:30 ID=Dequeuer:TID=1",
        "13:45:30 ID=Enqueuer:TID=2",
        "13:45:31 L=A:TID=1",
        "13:45:31 L=B[x]:TID=1",
        "13:45:31 U=B:TID=1",
        "13:45:31 U=A:TID=1",
        "1:45:32 PM L=B:TID=2",
        "1:45:32 PM L=A:TID=2",
        "1:45:32 PM U=A:TID=2",
        "1:45:32 PM U=B:TID=2",
        "13:45:33 L=C:TID=3",
        "13:45:33 U=C:TID=3",
    ];

    private static TForm1 ShowForm()
    {
        var form = new TForm1 { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000) };
        form.Show();
        Application.DoEvents();
        return form;
    }

    [Test]
    [Covers("MainForm", "TThreadAction")]
    public void The_actions_have_the_Delphi_values()
    {
        PortAssert.EnumsMatchDelphi("Tools/PropagatorAnalyzer/MainForm.pas", typeof(TThreadAction));
    }

    [Test]
    [Covers(typeof(TThreadInfo), ".ctor", nameof(TThreadInfo.ProcessLock), nameof(TThreadInfo.ProcessUnLock), nameof(TThreadInfo.Resources),
        nameof(TThreadInfo.ThreadName), nameof(TThreadInfo.Thread), nameof(TThreadInfo.Dispose))]
    public void A_thread_holds_its_locks_in_the_order_it_took_them()
    {
        using var thread = new TThreadInfo(7) { ThreadName = "Worker" };
        thread.ProcessLock("A");
        thread.ProcessLock("A"); // taken again: still once
        thread.ProcessLock("B");
        Assert.That((thread.Thread, thread.Resources.Count, thread.Resources[0], thread.Resources[1]), Is.EqualTo((7, 2, "A", "B")));
        thread.ProcessUnLock("A"); // not the last taken
        Assert.That(thread.Resources.Count, Is.EqualTo(2));
        thread.ProcessUnLock("B");
        thread.ProcessUnLock("A");
        Assert.That(thread.Resources.Count, Is.Zero);
    }

    [Test]
    [Covers(typeof(TForm1), "FormCreate", "GetThreadInfo", "AddDependentLock", "CheckDependency", "ClearData", "FormDestroy")]
    public void Opposite_lock_orders_are_marked()
    {
        using TForm1 form = ShowForm();
        string[] report = form.Analyze(Log);
        Assert.That(report, Is.EqualTo(new[]
        {
            "A",
            "               ! B",
            "B",
            "               ! A",
            "---",
            "3:C",
            "Dequeuer:A",
            "Dequeuer:A->B",
            "Enqueuer:B",
            "Enqueuer:B->A",
        }));
        Assert.That(form.Analyze(["13:45:31 L=A:TID=1", "13:45:31 L=B:TID=1"]), Does.Contain("               * B"), "analyzing again starts afresh");
    }

    [Test]
    [Covers(typeof(TForm1), "Button1Click")]
    public void Analyze_reads_the_log_file_of_the_edit()
    {
        string file = Path.Combine(Path.GetTempPath(), $"analyzer-{Guid.NewGuid():N}.thread");
        File.WriteAllLines(file, Log);
        try
        {
            using TForm1 form = ShowForm();
            form.Edit1.Text = file;
            form.Button1.PerformClick();
            Assert.That(form.Memo1.Lines, Does.Contain("               ! A").And.Contain("Enqueuer:B->A"));
            Assert.That(form.ProgressBar1.Visible, Is.False);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    [Covers(typeof(TForm1), "Button1Click")]
    public void The_thread_log_of_the_port_is_analyzed()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"analyzer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string threadLog = Path.Combine(folder, "BoldPropagator.thread");
        BoldThreadSafeLog.BoldInitLog(Path.Combine(folder, "log.txt"), Path.Combine(folder, "errors.txt"), threadLog, 0);
        try
        {
            var first = new TBoldLoggableCriticalSection("Queue");
            var second = new TBoldLoggableCriticalSection("Subscriptions");
            void TakeBoth(TBoldLoggableCriticalSection outer, TBoldLoggableCriticalSection inner, string name)
            {
                BoldThreadSafeLog.BoldLogThread("ID=" + name);
                outer.Acquire();
                inner.Acquire();
                inner.Release();
                outer.Release();
            }

            var one = new Thread(() => TakeBoth(first, second, "One"));
            one.Start();
            one.Join();
            var other = new Thread(() => TakeBoth(second, first, "Other"));
            other.Start();
            other.Join();
        }
        finally
        {
            BoldThreadSafeLog.BoldDoneLog();
        }

        try
        {
            using TForm1 form = ShowForm();
            form.Edit1.Text = threadLog;
            form.Button1.PerformClick();
            Assert.That(form.Memo1.Lines, Does.Contain("               ! Subscriptions").And.Contain("               ! Queue"), string.Join("\n", File.ReadAllLines(threadLog)));
            Assert.That(form.Memo1.Lines, Does.Contain("One:Queue->Subscriptions").And.Contain("Other:Subscriptions->Queue"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
