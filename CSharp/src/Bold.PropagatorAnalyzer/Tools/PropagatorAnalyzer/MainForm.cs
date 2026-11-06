// Delphi: Tools/PropagatorAnalyzer/MainForm.pas
// Port map: CSharp/port/units/MainForm.md
// The propagator analyzer reads a thread log, as the propagator writes it with a thread log file name: a line per lock
// taken ("L=name"), released ("U=name") and per thread named ("ID=name"), each with its thread ("hh:nn:ss L=name:TID=n").
// It lists, for every lock, the locks a thread took while holding it; a lock taken both after and before another is
// marked "!", an order that can deadlock.

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "MainForm", Symbol = "Form1", Note = "The program's form is made in Program.Main; each thread info reports to the form that made it.")]

namespace Bold;

/// <summary>What a line of the thread log says a thread did.</summary>
[Delphi(PortStatus.Ported)]
public enum TThreadAction { taID, taLock, taUnlock }

/// <summary>A thread of the log: its name and the locks it holds, in the order it took them.</summary>
[Delphi(PortStatus.Ported)]
public class TThreadInfo : IDisposable
{
    private readonly TStringList FResources = new();
    private readonly TForm1? fOwner;

    /// <summary>A thread, reporting the lock orders it finds to the form.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the form to report to, where Delphi reported to the global Form1.")]
    public TThreadInfo(int Thread, TForm1? Owner = null)
    {
        this.Thread = Thread;
        fOwner = Owner;
    }

    [Delphi(PortStatus.Ported, Symbol = "TThreadInfo.destroy")]
    public void Dispose()
    {
        FResources.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>The locks held, the first taken first.</summary>
    [Delphi(PortStatus.Ported)]
    public TStringList Resources => FResources;

    [Delphi(PortStatus.Ported)]
    public string ThreadName { get; set; } = "";

    [Delphi(PortStatus.Ported)]
    public int Thread { get; }

    /// <summary>The thread takes a lock: the order of its locks is noted, and each lock depends on the one taken after it.</summary>
    [Delphi(PortStatus.Ported)]
    public void ProcessLock(string Name)
    {
        if (FResources.Count == 0 || FResources[FResources.Count - 1] != Name)
        {
            FResources.Add(Name);
            if (FResources.Count > 0)
            {
                string s = ThreadName;
                if (s == "")
                {
                    s = Thread.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                s += ":";
                for (int i = 0; i < FResources.Count; i++)
                {
                    if (i > 0)
                    {
                        s += "->";
                    }

                    s += FResources[i];
                }

                if (fOwner is not null && fOwner.fLockInfo.IndexOf(s) == -1)
                {
                    fOwner.fLockInfo.Add(s);
                }
            }

            for (int i = 0; i < FResources.Count - 1; i++)
            {
                fOwner?.AddDependentLock(FResources[i], FResources[i + 1]);
            }
        }
    }

    /// <summary>The thread releases its last lock.</summary>
    [Delphi(PortStatus.Ported)]
    public void ProcessUnLock(string Name)
    {
        if (FResources.Count > 0 && FResources[FResources.Count - 1] == Name)
        {
            FResources.Delete(FResources.Count - 1);
        }
    }
}

/// <summary>The analyzer's window: the log file, Analyze, and the locks taken after each lock.</summary>
[Delphi(PortStatus.Ported, Note = "Built in code where Delphi's form came from its .dfm.")]
public class TForm1 : Form
{
    public readonly TextBox Edit1;
    public readonly Button Button1;
    public readonly ProgressBar ProgressBar1;
    public readonly TextBox Memo1;
    public readonly Button Button2;
    public readonly OpenFileDialog OpenDialog1;
    internal TStringList fThreadInfos = null!;
    internal TStringList fLockInfo = null!;
    internal TStringList fDependentLocks = null!;

    public TForm1()
    {
        Text = "Propagator analyzer";
        ClientSize = new Size(1080, 723);
        Edit1 = new TextBox { Name = "Edit1", Bounds = new Rectangle(8, 8, 385, 21), Text = "BoldPropagator.thread" };
        Button2 = new Button { Name = "Button2", Bounds = new Rectangle(392, 8, 25, 21), Text = "..." };
        Button1 = new Button { Name = "Button1", Bounds = new Rectangle(424, 8, 75, 21), Text = "Analyze" };
        ProgressBar1 = new ProgressBar
        {
            Name = "ProgressBar1", Bounds = new Rectangle(504, 9, 569, 21), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Visible = false,
        };
        Memo1 = new TextBox
        {
            Name = "Memo1", Bounds = new Rectangle(0, 40, 1080, 683), Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom, Font = new Font(FontFamily.GenericMonospace, 9),
        };
        OpenDialog1 = new OpenFileDialog { DefaultExt = "thread", Filter = "ThreadLog|*.thread" };
        Controls.AddRange([Edit1, Button2, Button1, ProgressBar1, Memo1]);
        AcceptButton = Button1;
        Button1.Click += Button1Click;
        Button2.Click += Button2Click;
        FormCreate();
    }

    /// <summary>Reads the log in the file of the edit and lists, for each lock, the locks taken while it was held.</summary>
    [Delphi(PortStatus.Ported, Note = "Finds the entry of a line after its time in any time format; Delphi took the text from the tenth character, which fits a time of eight.")]
    private void Button1Click(object? Sender, EventArgs e)
    {
        ProgressBar1.Visible = true;
        try
        {
            Memo1.Lines = Analyze(File.ReadAllLines(Edit1.Text));
        }
        finally
        {
            ProgressBar1.Visible = false;
        }
    }

    /// <summary>The report of the lines of a thread log (the analysis of Button1Click).</summary>
    public string[] Analyze(IReadOnlyList<string> Lines)
    {
        ClearData(fThreadInfos);
        ClearData(fLockInfo);
        ClearData(fDependentLocks);
        ProgressBar1.Maximum = Math.Max(Lines.Count - 1, 0);
        for (int i = 0; i < Lines.Count; i++)
        {
            if (i % 100 == 0)
            {
                ProgressBar1.Value = Math.Min(i, ProgressBar1.Maximum);
            }

            string s = Entry(Lines[i]);
            TThreadAction Action;
            if (s.StartsWith('U'))
            {
                Action = TThreadAction.taUnlock;
            }
            else if (s.StartsWith('L'))
            {
                Action = TThreadAction.taLock;
            }
            else if (s.StartsWith("ID", StringComparison.Ordinal))
            {
                Action = TThreadAction.taID;
            }
            else
            {
                throw new InvalidOperationException("unknown action: " + s);
            }

            string Value = s[(s.IndexOf('=', StringComparison.Ordinal) + 1)..];
            int colon = Value.IndexOf(':', StringComparison.Ordinal);
            Value = colon < 0 ? "" : Value[..colon];
            s = s[(s.IndexOf(':', StringComparison.Ordinal) + 1)..];
            int bracket = Value.IndexOf('[', StringComparison.Ordinal);
            if (bracket >= 0)
            {
                Value = Value[..bracket];
            }

            int Thread = SysUtils.StrToInt(s[(s.IndexOf('=', StringComparison.Ordinal) + 1)..]);
            TThreadInfo ThreadInfo = GetThreadInfo(Thread);
            switch (Action)
            {
                case TThreadAction.taID:
                    ThreadInfo.ThreadName = Value;
                    break;
                case TThreadAction.taUnlock:
                    ThreadInfo.ProcessUnLock(Value);
                    break;
                case TThreadAction.taLock:
                    ThreadInfo.ProcessLock(Value);
                    break;
            }
        }

        var report = new List<string>();
        for (int i = 0; i < fDependentLocks.Count; i++)
        {
            report.Add(fDependentLocks[i]);
            var SList = (TStringList)fDependentLocks.Objects(i)!;
            for (int j = 0; j < SList.Count; j++)
            {
                report.Add((CheckDependency(SList[j], fDependentLocks[i]) ? "               ! " : "               * ") + SList[j]);
            }
        }

        report.Add("---");
        for (int i = 0; i < fLockInfo.Count; i++)
        {
            report.Add(fLockInfo[i]);
        }

        return [.. report];
    }

    /// <summary>
    /// The entry of a line of the thread log: the text after the time, which has no '=' ("L=name:TID=1" of
    /// "13:45:30 L=name:TID=1" or of "1:45:30 PM L=name:TID=1").
    /// </summary>
    private static string Entry(string Line)
    {
        int equals = Line.IndexOf('=', StringComparison.Ordinal);
        return equals < 0 ? Line : Line[(Line.LastIndexOf(' ', equals) + 1)..];
    }

    /// <summary>The thread of the number, made the first time.</summary>
    [Delphi(PortStatus.Ported)]
    private TThreadInfo GetThreadInfo(int Thread)
    {
        string key = Thread.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int i = fThreadInfos.IndexOf(key);
        if (i == -1)
        {
            var result = new TThreadInfo(Thread, this);
            fThreadInfos.AddObject(key, result);
            return result;
        }

        return (TThreadInfo)fThreadInfos.Objects(i)!;
    }

    [Delphi(PortStatus.Ported)]
    private void FormCreate()
    {
        fLockInfo = new TStringList { Sorted = true };
        fThreadInfos = new TStringList { Sorted = true };
        fDependentLocks = new TStringList();
        Memo1.Clear();
    }

    /// <summary>Notes that a lock was taken while another was held.</summary>
    [Delphi(PortStatus.Ported)]
    internal void AddDependentLock(string first, string second)
    {
        TStringList dependentLocks;
        int i = fDependentLocks.IndexOf(first);
        if (i == -1)
        {
            dependentLocks = new TStringList { Sorted = true };
            fDependentLocks.AddObject(first, dependentLocks);
        }
        else
        {
            dependentLocks = (TStringList)fDependentLocks.Objects(i)!;
        }

        if (dependentLocks.IndexOf(second) == -1)
        {
            dependentLocks.Add(second);
        }
    }

    /// <summary>True when the second lock was taken while the first was held.</summary>
    [Delphi(PortStatus.Ported)]
    private bool CheckDependency(string first, string second)
    {
        int i = fDependentLocks.IndexOf(first);
        return i != -1 && ((TStringList)fDependentLocks.Objects(i)!).IndexOf(second) != -1;
    }

    [Delphi(PortStatus.Ported)]
    private static void ClearData(TStringList sList)
    {
        for (int i = 0; i < sList.Count; i++)
        {
            (sList.Objects(i) as IDisposable)?.Dispose();
        }

        sList.Clear();
    }

    [Delphi(PortStatus.Ported, Note = "The form's Dispose.")]
    private void FormDestroy()
    {
        ClearData(fThreadInfos);
        ClearData(fLockInfo);
        ClearData(fDependentLocks);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FormDestroy();
            OpenDialog1.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Chooses the log file.</summary>
    [Delphi(PortStatus.Ported)]
    private void Button2Click(object? Sender, EventArgs e)
    {
        OpenDialog1.InitialDirectory = SysUtils.ExtractFilePath(Edit1.Text);
        OpenDialog1.FileName = SysUtils.ExtractFileName(Edit1.Text);
        if (OpenDialog1.ShowDialog(this) == DialogResult.OK)
        {
            Edit1.Text = OpenDialog1.FileName;
        }
    }
}
