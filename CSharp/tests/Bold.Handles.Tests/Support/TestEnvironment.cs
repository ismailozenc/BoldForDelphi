namespace Bold.Tests;

/// <summary>
/// An environment installed as the custom one while it lives: an IContainer counts as a form, the root component can
/// be set, and design-time exceptions are recorded.
/// </summary>
internal sealed class TestEnvironment : TBoldFreestandingEnvironmentConfiguration
{
    public object? Root;
    public readonly List<object?> DesigntimeExceptions = [];

    public TestEnvironment()
    {
        BoldEnvironment.BoldInternalCustomConfiguration = this;
    }

    public override bool IsFormOrDataModule(object? Sender) => Sender is System.ComponentModel.IContainer;

    protected override object? GetRootComponent() => Root;

    public override void HandleDesigntimeException(object? Sender) => DesigntimeExceptions.Add(Sender);

    public override void Dispose()
    {
        BoldEnvironment.BoldInternalCustomConfiguration = null;
        base.Dispose();
    }
}

/// <summary>Records what is written to the Bold log while it lives.</summary>
internal sealed class LogCapture : IBoldLogReceiver, IDisposable
{
    public readonly List<string> Lines = [];
    public readonly List<string> Calls = [];

    public LogCapture()
    {
        BoldLogHandler.BoldLog().RegisterLogReceiver(this);
    }

    public string Text => string.Join("\n", Lines);

    public void SetProgress(int Value) => Calls.Add("progress");
    public void SetLogHeader(string Value) => Calls.Add("header " + Value);
    public void SetProgressMax(int Value) => Calls.Add("max");
    public void Clear() => Calls.Add("clear");
    public void Hide() => Calls.Add("hide");
    public void Log(string s, TBoldLogType LogType) => Lines.Add(s.Trim());
    public void ProgressStep() => Calls.Add("step");
    public void Show() => Calls.Add("show");
    public void StartLog(string SessionName) => Calls.Add("start " + SessionName);
    public void EndLog() => Calls.Add("end");
    public void Sync() => Calls.Add("sync");
    public void ProcessInterruption() => Calls.Add("interrupt?");

    public void Dispose() => BoldLogHandler.BoldLog().UnregisterLogReceiver(this);
}
