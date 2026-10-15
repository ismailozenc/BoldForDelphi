// Delphi: Source/Common/Logging/BoldLogHandler.pas, BoldLogReceiverInterface.pas
// Port map: CSharp/port/units/BoldLogHandler.md, CSharp/port/units/BoldLogReceiverInterface.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldLogHandlerTests
{
    private sealed class Recorder : IBoldLogReceiver
    {
        public readonly List<string> Calls = [];
        public void SetProgress(int Value) => Calls.Add("progress " + Value);
        public void SetLogHeader(string Value) => Calls.Add("header " + Value);
        public void SetProgressMax(int Value) => Calls.Add("max " + Value);
        public void Clear() => Calls.Add("clear");
        public void Hide() => Calls.Add("hide");
        public void Log(string s, TBoldLogType LogType) => Calls.Add(LogType + " " + s);
        public void ProgressStep() => Calls.Add("step");
        public void Show() => Calls.Add("show");
        public void StartLog(string SessionName) => Calls.Add("start " + SessionName);
        public void EndLog() => Calls.Add("end");
        public void Sync() => Calls.Add("sync");
        public void ProcessInterruption() => Calls.Add("interrupt?");
    }

    [Test]
    [Covers(typeof(TBoldLogHandler), "*")]
    [Covers(typeof(TBoldLogReceiverSubscriber), "*")]
    [Covers(typeof(BoldLogHandler), "*")]
    [Covers(typeof(IBoldLogReceiver), "*")]
    [Covers("BoldLogHandler", "TBoldLogHandlerClass", "initialization", "finalization")]
    public void Receivers_get_every_log_event_until_unregistered()
    {
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        Assert.That(BoldLogHandler.BoldLog(), Is.SameAs(log));
        var r = new Recorder();
        log.RegisterLogReceiver(r);
        log.StartLog("session");
        log.LogIndent("a");
        log.LogFmt("b %d", [2], TBoldLogType.ltWarning);
        log.LogFmtIndent("c %s", ["x"]);
        log.LogDedent("d");
        log.LogFmtDedent("e", []);
        log.Dedent();
        log.Log("f");
        log.Indent();
        log.Log("g", TBoldLogType.ltError);
        log.ProgressMax = -5;
        log.Progress = 3;
        log.LogHeader = "H";
        log.ProgressStep();
        log.Clear();
        log.Show();
        log.Hide();
        log.Sync();
        Assert.That(log.ProcessInterruption(), Is.False);
        log.InterruptProcess();
        Assert.That(log.ProcessInterruption(), Is.True);
        Assert.That(log.ProcessInterruption(), Is.True);
        log.EndLog();
        IBoldLogReceiver asInterface = r;
        asInterface.Progress = 1;
        asInterface.ProgressMax = 2;
        asInterface.LogHeader = "x";

        Assert.That(r.Calls, Is.EqualTo(new[]
        {
            "ltSeparator ", "ltSeparator -={++++}=-", "ltSeparator ", "start session",
            "ltInfo a", "ltWarning   b 2", "ltInfo   c x", "ltInfo   d", "ltInfo e", "ltInfo f", "ltError   g",
            "max 0", "progress 3", "header H", "step", "clear", "show", "hide", "sync", "interrupt?",
            "ltInfo   Trying to abort process", "interrupt?", "ltInfo   Process stopped", "interrupt?",
            "end", "ltSeparator   ", "ltSeparator   -={++++}=-", "ltSeparator   ",
            "progress 1", "max 2", "header x",
        }));

        log.UnregisterLogReceiver(r);
        r.Calls.Clear();
        log.Log("after");
        Assert.That(r.Calls, Is.Empty, "unregistered receivers hear nothing more");
    }
}
