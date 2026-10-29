namespace Bold;

/// <summary>
/// Delphi's TCriticalSection (unit SyncObjs): a lock a thread can take more than once, as a Windows critical section.
/// Acquire and Release are virtual so subclasses can log them, as Bold's loggable critical section does.
/// </summary>
public class TCriticalSection : IDisposable
{
    private readonly object fLock = new();

    public virtual void Acquire() => Monitor.Enter(fLock);

    public virtual void Release() => Monitor.Exit(fLock);

    public void Enter() => Acquire();

    public void Leave() => Release();

    /// <summary>Takes the lock if no other thread holds it; true when taken.</summary>
    public bool TryEnter() => Monitor.TryEnter(fLock);

    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>The result of waiting on a <see cref="TEvent"/>, as Delphi's TWaitResult.</summary>
public enum TWaitResult { wrSignaled, wrTimeout, wrAbandoned, wrError }

/// <summary>
/// Delphi's TEvent and TSimpleEvent (unit SyncObjs): an event threads wait on. A manual-reset event stays signaled
/// until reset; an auto-reset event lets one waiter through and resets.
/// </summary>
public class TEvent : IDisposable
{
    private readonly EventWaitHandle fHandle;

    public TEvent(bool ManualReset = true, bool InitialState = false)
    {
        fHandle = new EventWaitHandle(InitialState, ManualReset ? EventResetMode.ManualReset : EventResetMode.AutoReset);
    }

    public WaitHandle Handle => fHandle;

    public void SetEvent() => fHandle.Set();

    public void ResetEvent() => fHandle.Reset();

    /// <summary>Waits up to <paramref name="Timeout"/> milliseconds (<see cref="Timeout.Infinite"/> waits for ever).</summary>
    public TWaitResult WaitFor(int Timeout = global::System.Threading.Timeout.Infinite)
    {
        try
        {
            return fHandle.WaitOne(Timeout) ? TWaitResult.wrSignaled : TWaitResult.wrTimeout;
        }
        catch (AbandonedMutexException)
        {
            return TWaitResult.wrAbandoned;
        }
        catch (ObjectDisposedException)
        {
            return TWaitResult.wrError;
        }
    }

    public virtual void Dispose()
    {
        fHandle.Dispose();
        GC.SuppressFinalize(this);
    }
}
