// Delphi: Source/Common/Logging/BoldThreadSafeLog.pas, Source/Common/Support/BoldLoggableCriticalSection.pas
// Port map: CSharp/port/units/BoldThreadSafeLog.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldThreadSafeLogTests
{
    private string folder = null!;

    [SetUp]
    public void CreateFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), $"boldlog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
    }

    [TearDown]
    public void DeleteFolder()
    {
        BoldThreadSafeLog.BoldDoneLog();
        Directory.Delete(folder, true);
    }

    private string File(string name) => Path.Combine(folder, name);

    private static string[] Lines(string file) => System.IO.File.ReadAllLines(file);

    [Test]
    [Covers(typeof(BoldThreadSafeLog), "*")]
    [Covers(typeof(TBoldLogger), "*")]
    [Covers(typeof(TBoldLoggableCriticalSection), "*")]
    public void The_log_writes_messages_errors_and_lock_activity_to_their_files()
    {
        BoldThreadSafeLog.BoldLog("not logged before the log is set up");
        BoldThreadSafeLog.BoldInitLog(File("log.txt"), File("errors.txt"), File("threads.txt"), 0);
        BoldThreadSafeLog.BoldLog("plain");
        BoldThreadSafeLog.BoldLog("%d clients", 3);
        BoldThreadSafeLog.BoldLogError("failed");
        BoldThreadSafeLog.BoldLogError("failed %s", "again");
        using (var section = new TBoldLoggableCriticalSection("Queue"))
        {
            Assert.That(section.Name, Is.EqualTo("Queue"));
            section.Acquire();
            section.Release();
        }

        BoldThreadSafeLog.BoldDoneLog();
        BoldThreadSafeLog.BoldLog("not logged after");

        string[] log = Lines(File("log.txt"));
        Assert.That(log, Has.Length.EqualTo(2));
        Assert.That(log[0], Does.EndWith(" plain"));
        Assert.That(log[1], Does.EndWith(" 3 clients"));
        string[] errors = Lines(File("errors.txt"));
        Assert.That(errors[0], Does.Contain(" failed (ThreadID="), "errors name the thread");
        Assert.That(errors[1], Does.Contain(" failed again (ThreadID="));
        string[] threads = Lines(File("threads.txt"));
        Assert.That(threads, Has.Length.EqualTo(2));
        Assert.That(threads[0], Does.Match(@" L=Queue:TID=\d+$"), "the thread log has the short thread id");
        Assert.That(threads[1], Does.Match(@" U=Queue:TID=\d+$"));
        Assert.That(threads[0], Does.Not.Contain(DateTime.Now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)), "and the time only");
    }

    [Test]
    public void Without_a_thread_log_lock_activity_is_not_logged()
    {
        BoldThreadSafeLog.BoldInitLog(File("log.txt"), File("errors.txt"), "", 0);
        using var section = new TBoldLoggableCriticalSection("Quiet");
        section.Acquire();
        section.Release();
        BoldThreadSafeLog.BoldDoneLog();
        Assert.That(System.IO.File.Exists(File("threads.txt")), Is.False);
        using var logger = new TBoldLogger(File("own.txt"), File("own-errors.txt"), "", 0);
        Assert.DoesNotThrow(() => logger.LogThread("nowhere to go"));
    }

    [Test]
    [Covers(typeof(TFileLogging), "*")]
    public void A_file_log_appends_lines_closes_and_starts_again_past_its_size()
    {
        string file = File("trace.txt");
        System.IO.File.WriteAllText(file, "earlier\r\n");
        using (var trace = new TFileLogging(file, 0, true))
        {
            Assert.That(trace.FileName, Is.EqualTo(file));
            Assert.That((trace.Open, trace.IncludeDate, trace.IncludeThreadId, trace.ShortThreadId), Is.EqualTo((true, true, false, false)));
            trace.Trace("first");
            trace.Trace("%s and %d", "second", 2);
            trace.Open = false;
            trace.Trace("not written while closed");
            trace.Open = true;
            trace.IncludeThreadId = true;
            trace.Trace("third");
        }

        string[] lines = Lines(file);
        Assert.That(lines[0], Is.EqualTo("earlier"), "lines are appended");
        Assert.That(lines[1], Does.StartWith(SysUtils.DateTimeToStr(DateTime.Now)[..4]));
        Assert.That(lines[1], Does.EndWith(" first"));
        Assert.That(lines[2], Does.EndWith(" second and 2"));
        Assert.That(lines[3], Does.Match(@" third \(ThreadID=\d+\)$"));
        Assert.That(lines, Has.Length.EqualTo(4));

        string small = File("small.txt");
        using (var trace = new TFileLogging(small, 30, true))
        {
            trace.Trace("a line that is longer than thirty bytes");
            trace.Trace("next");
        }

        Assert.That(Lines(small), Has.Length.EqualTo(1), "past the maximum size the file starts again");
        Assert.That(Lines(small)[0], Does.EndWith(" next"));

        using var nowhere = new TFileLogging(Path.Combine(folder, "missing", "x.txt"), 0, true);
        Assert.That(nowhere.Open, Is.False, "a file that cannot be opened leaves the log closed");
        Assert.DoesNotThrow(() => nowhere.Trace("lost"));
    }
}
