namespace Bold;

/// <summary>
/// Delphi's TTimer: calls OnTimer every Interval milliseconds on the thread that set it going, through that thread's
/// synchronization context (the UI thread in a WinForms application). Without a synchronization context it does not
/// fire, as a VCL timer does not fire without a message loop.
/// </summary>
public sealed class TTimer : IDisposable
{
    private bool fEnabled = true;
    private int fInterval = 1000;
    private TNotifyEvent? fOnTimer;
    private Timer? fTimer;
    private SynchronizationContext? fContext;
    private bool fDisposed;

    public bool Enabled
    {
        get => fEnabled;
        set
        {
            fEnabled = value;
            UpdateTimer();
        }
    }

    /// <summary>The time between ticks in milliseconds; 0 stops the timer.</summary>
    public int Interval
    {
        get => fInterval;
        set
        {
            fInterval = value;
            UpdateTimer();
        }
    }

    public TNotifyEvent? OnTimer
    {
        get => fOnTimer;
        set
        {
            fOnTimer = value;
            UpdateTimer();
        }
    }

    /// <summary>True when the timer is running (enabled, with an interval, an OnTimer and a synchronization context).</summary>
    public bool Running => fTimer is not null;

    public void Dispose()
    {
        fDisposed = true;
        fTimer?.Dispose();
        fTimer = null;
    }

    private void UpdateTimer()
    {
        fTimer?.Dispose();
        fTimer = null;
        if (fDisposed || !fEnabled || fInterval <= 0 || fOnTimer is null)
        {
            return;
        }

        fContext = SynchronizationContext.Current;
        if (fContext is not null)
        {
            SynchronizationContext context = fContext;
            fTimer = new Timer(_ => context.Post(_ => DoTimer(), null), null, fInterval, fInterval);
        }
    }

    private void DoTimer()
    {
        if (!fDisposed && fEnabled)
        {
            fOnTimer?.Invoke(this);
        }
    }
}
