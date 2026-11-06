// Delphi: Source/Common/Logging/BoldLogHandlerSimple.pas
// Port map: CSharp/port/units/BoldLogHandlerSimple.md
namespace Bold.Tests;

public class BoldLogHandlerSimpleTests
{
    [Test]
    [Covers(typeof(TBoldSimpleLogReceiver), "*")]
    public void The_receiver_keeps_the_lines_of_a_session()
    {
        using var receiver = new TBoldSimpleLogReceiver();
        IBoldLogReceiver log = receiver;
        log.StartLog("Import");
        log.LogHeader = "Reading the model";
        log.ProgressMax = 10;
        log.Progress = 5;
        log.ProgressStep();
        log.Log("A class", TBoldLogType.ltInfo);
        log.Show();
        log.Hide();
        log.Sync();
        log.ProcessInterruption();
        log.EndLog();
        Assert.That(Enumerable.Range(0, receiver.LogLines.Count).Select(i => receiver.LogLines[i]), Is.EqualTo(new[] { "Session: Import", "Reading the model", "A class" }));
        log.Clear();
        Assert.That(receiver.LogLines.Count, Is.Zero);
    }

    [Test]
    [NonParallelizable]
    [Covers(typeof(TBoldSimpleLogReceiver), nameof(TBoldSimpleLogReceiver.Log), nameof(TBoldSimpleLogReceiver.StartLog))]
    public void The_log_writes_to_the_receiver()
    {
        using var receiver = new TBoldSimpleLogReceiver();
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.RegisterLogReceiver(receiver);
        try
        {
            log.StartLog("Test");
            log.Log("from the log");
            log.EndLog();
        }
        finally
        {
            log.UnregisterLogReceiver(receiver);
        }

        Assert.That(receiver.LogLines.Text, Does.Contain("Session: Test").And.Contain("from the log"));
        log.Log("not any more");
        Assert.That(receiver.LogLines.Text, Does.Not.Contain("not any more"));
    }
}
