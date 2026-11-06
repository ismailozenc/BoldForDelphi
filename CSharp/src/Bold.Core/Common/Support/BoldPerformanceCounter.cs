// Delphi: Source/Common/Support/BoldPerformanceCounter.pas
// Port map: CSharp/port/units/BoldPerformanceCounter.md

using System.Diagnostics;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPerformanceCounter", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldPerformanceCounterState { bpcsStopped, bpcsStarted }

/// <summary>
/// Measures the time spent in parts of a program: a tree of named counters, each started and stopped around the code
/// it measures; a counter runs while any of its children runs.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Times with Stopwatch, where Delphi called QueryPerformanceCounter.")]
public class TBoldPerformanceCounter : IDisposable
{
    private static TBoldPerformanceCounter? G_MainPerformanceCounter;
    private int fNumberOfChildrenRunning;
    private readonly TStringList fChildren = new();
    private TBoldPerformanceCounter? fOwner;
    private readonly string fName;
    private long fAccumulatedTime;
    private long fStart;
    private int fCalls;

    /// <summary>A counter, a child of the owner when there is one.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldPerformanceCounter(TBoldPerformanceCounter? Owner, string Name)
    {
        fNumberOfChildrenRunning = 0;
        fOwner = Owner;
        fName = Name.ToUpperInvariant();
        Owner?.fChildren.AddObject(Name, this);
    }

    /// <summary>The counter of the program, made when first asked for.</summary>
    [Delphi(PortStatus.Ported, Symbol = "BoldMainPerformanceCounter")]
    public static TBoldPerformanceCounter BoldMainPerformanceCounter() =>
        G_MainPerformanceCounter ??= new TBoldPerformanceCounter(null, "BoldMainPerformanceCounter");

    /// <summary>Leaves the owner, and the children go with the counter.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldPerformanceCounter.Destroy")]
    public void Dispose()
    {
        if (fOwner is not null)
        {
            for (int i = 0; i < fOwner.ChildCount; i++)
            {
                if (fOwner.Children(i) == this)
                {
                    fOwner.fChildren.Delete(i);
                    break;
                }
            }
        }

        for (int i = ChildCount - 1; i >= 0; i--)
        {
            TBoldPerformanceCounter child = Children(i);
            child.fOwner = null;
            child.Dispose();
        }

        fChildren.Clear();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private int GetChildCount() => fChildren.Count;

    [Delphi(PortStatus.Ported)]
    public int ChildCount => GetChildCount();

    [Delphi(PortStatus.Ported)]
    private TBoldPerformanceCounter GetChildren(int index) => (TBoldPerformanceCounter)fChildren.Objects(index)!;

    /// <summary>The child at the index.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldPerformanceCounter Children(int index) => GetChildren(index);

    /// <summary>The child of the name (in any case), made when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldPerformanceCounter GetChildByName(string Name)
    {
        int i = fChildren.IndexOf(Name.ToUpperInvariant());
        return i == -1 ? new TBoldPerformanceCounter(this, Name) : Children(i);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPerformanceCounter ChildByName(string name) => GetChildByName(name);

    /// <summary>Sets the time and the calls of the counter and its children to zero.</summary>
    [Delphi(PortStatus.Ported)]
    public bool Reset()
    {
        fAccumulatedTime = 0;
        fCalls = 0;
        for (int i = 0; i < ChildCount; i++)
        {
            Children(i).Reset();
        }

        return true;
    }

    /// <summary>Starts timing (and the owner, if it does not run already).</summary>
    [Delphi(PortStatus.Ported)]
    public bool Start()
    {
        fCalls++;
        fOwner?.StartFromChild();
        fStart = Stopwatch.GetTimestamp();
        return true;
    }

    /// <summary>Stops timing and adds the time to the counter's (and the owner's, when no other child runs).</summary>
    [Delphi(PortStatus.Ported)]
    public bool Stop()
    {
        long CurrentTime = Stopwatch.GetTimestamp();
        fOwner?.StopFromChild();
        fAccumulatedTime += CurrentTime - fStart;
        return true;
    }

    [Delphi(PortStatus.Ported)]
    private double GetSeconds() => fAccumulatedTime / (double)Stopwatch.Frequency;

    /// <summary>The time counted, in seconds.</summary>
    [Delphi(PortStatus.Ported)]
    public double Seconds => GetSeconds();

    [Delphi(PortStatus.Ported)]
    private double GetPercentOfOwner() => fOwner is not null && fOwner.Seconds != 0 ? 100 * Seconds / fOwner.Seconds : 100;

    /// <summary>The share of the owner's time, in percent (100 without an owner).</summary>
    [Delphi(PortStatus.Ported)]
    public double PercentOfOwner => GetPercentOfOwner();

    /// <summary>A line for the counter and the lines of its children: name, seconds, calls and share of the owner.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetAsString()
    {
        string result = SysUtils.Format(BoldCommonConst.sCallCount, fName, Seconds, fCalls);
        if (fOwner is not null)
        {
            result = SysUtils.Format(BoldCommonConst.sPercentCount, result, PercentOfOwner, fOwner.fName);
        }

        result += BoldDefs.BOLDCRLF;
        for (int i = 0; i < ChildCount; i++)
        {
            result += Children(i).AsString;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public string AsString => GetAsString();

    [Delphi(PortStatus.Ported)]
    private void StartFromChild()
    {
        if (fNumberOfChildrenRunning == 0)
        {
            Start();
        }

        fNumberOfChildrenRunning++;
    }

    [Delphi(PortStatus.Ported)]
    private void StopFromChild()
    {
        fNumberOfChildrenRunning--;
        if (fNumberOfChildrenRunning == 0)
        {
            Stop();
        }
    }

    /// <summary>Writes AsString to the file.</summary>
    [Delphi(PortStatus.Ported)]
    public bool WriteToFile(string FileName)
    {
        var StringList = new TStringList { Text = AsString };
        StringList.SaveToFile(FileName);
        return true;
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization")]
    public static void Finalize_()
    {
        G_MainPerformanceCounter?.Dispose();
        G_MainPerformanceCounter = null;
    }
}
