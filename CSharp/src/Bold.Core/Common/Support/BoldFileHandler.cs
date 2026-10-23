// Delphi: Source/Common/Support/BoldFileHandler.pas
// Port map: CSharp/port/units/BoldFileHandler.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldFileHandler", Symbol = "TBoldFileHandlerClass", Note = "A System.Type of a TBoldFileHandler subclass.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldFileHandler", Symbol = "initialization", Note = "Empty in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldFileHandler", Symbol = "finalization", Note = "Freed the handler list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Fills a new, empty file (a header, for example).</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldInitializeFileContents(TStringList StringList);

/// <summary>
/// The contents of one file being written, kept in memory as lines with an indent level, and written when flushed (when
/// changed). Each handler is kept in <see cref="BoldFileHandler.BoldFileHandlerList"/> until it is disposed.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldFileHandler : IDisposable
{
    private string fFileFilter;
    private string FFileName;
    private int fIndentLevel;
    private bool fLastWasNewLine;
    private readonly TBoldModuleType fModuleType;
    private readonly TBoldInitializeFileContents? FOnInitializeFileContents;
    private readonly string fSetFileName;
    private TStringList? fStringList;
    private bool fStringListModified;
    private bool fDisposed;

    [Delphi(PortStatus.Ported, Note = "ShowFileInGuiIfPossible is not used, as in Delphi.")]
    protected TBoldFileHandler(string FileName, TBoldModuleType ModuleType, bool ShowFileInGuiIfPossible, TBoldInitializeFileContents? OnInitializeFileContents)
    {
        FFileName = FileName;
        fSetFileName = FileName;
        fModuleType = ModuleType;
        fLastWasNewLine = true;
        fIndentLevel = 0;
        FOnInitializeFileContents = OnInitializeFileContents;
        fFileFilter = BoldCommonConst.sFileHandlerMask;
        Encoding = BoldFileHandler.FileEncoding;
        BoldFileHandler.BoldFileHandlerList().Add(this);
    }

    /// <summary>Writes the file when changed and leaves the handler list.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldFileHandler.Destroy", Note = "A failed write is shown through BoldFileHandler.ShowMessage, where Delphi showed a message box.")]
    public void Dispose()
    {
        if (fDisposed)
        {
            return;
        }

        fDisposed = true;
        try
        {
            FlushFile();
        }
        catch (Exception e)
        {
            BoldFileHandler.ShowMessage(SysUtils.Format(BoldCommonConst.sFileSaveProblem, e.Message));
        }

        fStringList = null;
        BoldFileHandler.BoldFileHandlerList().Remove(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>The encoding of the file; null for <see cref="TStrings.DefaultEncoding"/> (the ANSI code page, as Delphi wrote).</summary>
    public System.Text.Encoding? Encoding { get; set; }

    [Delphi(PortStatus.Ported)]
    private void ContentsChanged(object? sender) => fStringListModified = true;

    [Delphi(PortStatus.Ported)]
    private string GetContent(int i) => StringList[i];

    [Delphi(PortStatus.Ported)]
    private int GetCount() => StringList.Count;

    [Delphi(PortStatus.Ported)]
    private bool GetIsEmpty() => StringList.Count == 0;

    [Delphi(PortStatus.Ported)]
    private TStringList GetStringList()
    {
        InitializeStringList();
        return fStringList!;
    }

    [Delphi(PortStatus.Ported)]
    private string IndentSpace() => SysUtils.StringOfChar(' ', fIndentLevel * BoldFileHandler.BOLD_INDENTSIZE);

    /// <summary>Whether the file can be written: it does not exist or is not read only.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool CheckWriteable(string FName = "")
    {
        if (FName == "")
        {
            FName = Filename;
        }

        return !(File.Exists(FName) && new FileInfo(FName).IsReadOnly);
    }

    /// <summary>The first line (zero based) that contains the text, ignoring case; -1 when none does.</summary>
    [Delphi(PortStatus.Ported)]
    protected int GetLineFromStringList(string S)
    {
        string Uc = SysUtils.AnsiUpperCase(S);
        for (int i = 0; i < StringList.Count; i++)
        {
            if (SysUtils.Pos(Uc, SysUtils.AnsiUpperCase(StringList[i])) > 0)
            {
                return i;
            }
        }

        return -1;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void DoFlushFile();

    [Delphi(PortStatus.Ported)]
    protected virtual void CloseFile()
    {
        // do nothing
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void LoadStringList();

    [Delphi(PortStatus.Ported)]
    protected TStringList StringList => GetStringList();

    /// <summary>Adds the text to the current line, or as a new indented line after a line break.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddString(string s)
    {
        if (LastWasNewLine)
        {
            StringList.Add(IndentSpace() + s);
        }
        else
        {
            StringList[StringList.Count - 1] = StringList[StringList.Count - 1] + s;
        }

        fLastWasNewLine = false;
    }

    /// <summary>Writes each line.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddStrings(TStrings s)
    {
        for (int i = 0; i < s.Count; i++)
        {
            WriteLn(s[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Clear() => StringList.Clear();

    [Delphi(PortStatus.Ported)]
    public void Dedent()
    {
        fIndentLevel--;
        if (fIndentLevel < 0)
        {
            fIndentLevel = 0;
        }
    }

    /// <summary>Writes "end;" one level out, and an empty line when asked.</summary>
    [Delphi(PortStatus.Ported)]
    public void EndBlock(bool AddNewLine)
    {
        Dedent();
        WriteLn("end;");
        if (AddNewLine)
        {
            NewLine();
        }
    }

    /// <summary>Writes the file when it changed, and drops the contents.</summary>
    [Delphi(PortStatus.Ported)]
    public void FlushFile()
    {
        if (fStringListModified)
        {
            DoFlushFile();
        }

        fStringListModified = false;
        fStringList = null;
    }

    [Delphi(PortStatus.Ported)]
    public void Format(string Fmt, params object?[] Args) => AddString(SysUtils.Format(Fmt, Args));

    [Delphi(PortStatus.Ported)]
    public void FormatLn(string Fmt, params object?[] Args) => WriteLn(SysUtils.Format(Fmt, Args));

    [Delphi(PortStatus.Ported)]
    public void Indent() => fIndentLevel++;

    /// <summary>Replaces the contents, or adds to them on a new line.</summary>
    [Delphi(PortStatus.Ported)]
    public void ModifyContents(string NewContents, bool Replace)
    {
        if (Replace)
        {
            StringList.Text = NewContents;
        }
        else
        {
            StringList.Text = StringList.Text + BoldDefs.BOLDCRLF + NewContents;
        }
    }

    /// <summary>Ends the current line; a second line break gives an empty line.</summary>
    [Delphi(PortStatus.Ported)]
    public void NewLine()
    {
        if (LastWasNewLine)
        {
            StringList.Add("");
        }

        fLastWasNewLine = true;
    }

    /// <summary>Writes "begin" and indents.</summary>
    [Delphi(PortStatus.Ported)]
    public void StartBlock()
    {
        WriteLn("begin");
        Indent();
    }

    /// <summary>Loads the file once; an empty file gets its initial contents. The contents start unmodified.</summary>
    [Delphi(PortStatus.Ported)]
    public void InitializeStringList()
    {
        if (fStringList is null)
        {
            fStringList = new TStringList();
            fStringList.OnChange = ContentsChanged;
            LoadStringList();
            if (fStringList.Count == 0 && OnInitializeFileContents is not null)
            {
                OnInitializeFileContents(fStringList);
            }

            fStringListModified = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool TextFoundInFile(string S) => GetLineFromStringList(S) != -1;

    [Delphi(PortStatus.Ported)]
    public void WriteLn(string Str)
    {
        AddString(Str);
        NewLine();
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public string Content(int i) => GetContent(i);

    [Delphi(PortStatus.Ported)] public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public string FileFilter
    {
        get => fFileFilter;
        set => fFileFilter = value;
    }

    [Delphi(PortStatus.Ported)]
    public string Filename
    {
        get => FFileName;
        set => FFileName = value;
    }

    [Delphi(PortStatus.Ported)] public bool IsEmpty => GetIsEmpty();
    [Delphi(PortStatus.Ported)] public bool LastWasNewLine => fLastWasNewLine;
    [Delphi(PortStatus.Ported)] public TBoldModuleType ModuleType => fModuleType;
    [Delphi(PortStatus.Ported)] public TBoldInitializeFileContents? OnInitializeFileContents => FOnInitializeFileContents;
    [Delphi(PortStatus.Ported)] public string SetFileName => fSetFileName;
    [Delphi(PortStatus.Ported)] public bool StringListModified => fStringListModified;

    /// <summary>Called by BoldRemoveUnchangedFilesFromEditor.</summary>
    internal void CloseFileForList() => CloseFile();
}

/// <summary>A file handler for a file on disk.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDiskFileHandler(string FileName, TBoldModuleType ModuleType, bool ShowFileInGuiIfPossible, TBoldInitializeFileContents? OnInitializeFileContents)
    : TBoldFileHandler(FileName, ModuleType, ShowFileInGuiIfPossible, OnInitializeFileContents)
{
    /// <summary>Writes the file (in the current directory when the name has no path), or logs and shows that it is read only.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoFlushFile()
    {
        if (SysUtils.ExtractFilePath(Filename) == "")
        {
            Filename = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + Filename;
        }

        if (CheckWriteable())
        {
            StringList.SaveToFile(Filename, Encoding);
            BoldLogHandler.BoldLog().LogFmt(BoldCommonConst.sSaved, [Filename]);
        }
        else
        {
            BoldLogHandler.BoldLog().LogFmt(BoldCommonConst.sModuleReadOnly, [Filename], TBoldLogType.ltError);
            BoldFileHandler.ShowMessage(SysUtils.Format(BoldCommonConst.sModuleReadOnly, Filename));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void LoadStringList()
    {
        if (File.Exists(Filename))
        {
            StringList.LoadFromFile(Filename, Encoding);
        }
    }
}

/// <summary>Unit-level routines and variables of BoldFileHandler.</summary>
public static class BoldFileHandler
{
    private static TBoldObjectArray? G_FileHandlerList;

    [Delphi(PortStatus.Ported)] public static int BOLD_INDENTSIZE = 2;
    [Delphi(PortStatus.Ported)] public static bool BoldFilehandlerLog = true;

    /// <summary>The handler class BoldFileHandlerForFile creates; it needs the constructor of TBoldFileHandler.</summary>
    [Delphi(PortStatus.Ported)] public static Type BoldPrefferedFileHandlerClass = typeof(TBoldDiskFileHandler);

    /// <summary>The encoding new handlers read and write files in; null for <see cref="TStrings.DefaultEncoding"/>.</summary>
    public static System.Text.Encoding? FileEncoding;

    /// <summary>Shows a message to the user; Delphi showed a message box. The default logs it as an error.</summary>
    public static Action<string> ShowMessage = message => BoldLogHandler.BoldLog().Log(message, TBoldLogType.ltError);

    [Delphi(PortStatus.Ported)]
    public static TBoldObjectArray BoldFileHandlerList() => G_FileHandlerList ??= new TBoldObjectArray(0, TBoldContainerOptions.Of());

    /// <summary>The open handler for the file (ignoring case), or a new one of the preferred class with its contents loaded.</summary>
    [Delphi(PortStatus.Ported, Note = "Finds an open handler by path and name; Delphi compared the name without the path to the handler's path and name, so with a path it never found one and made a second handler for the file, whose writes overwrote the first's.")]
    public static TBoldFileHandler BoldFileHandlerForFile(string path, string FileName, TBoldModuleType ModuleType, bool ShowInEditor, TBoldInitializeFileContents? OnInitializeFileContents)
    {
        try
        {
            for (int i = 0; i < BoldFileHandlerList().Count; i++)
            {
                var handler = (TBoldFileHandler)BoldFileHandlerList()[i]!;
                if (SysUtils.AnsiUpperCase(handler.SetFileName) == SysUtils.AnsiUpperCase(path + FileName))
                {
                    return handler;
                }
            }

            var result = (TBoldFileHandler)Activator.CreateInstance(BoldPrefferedFileHandlerClass, path + FileName, ModuleType, ShowInEditor, OnInitializeFileContents)!;
            result.InitializeStringList();
            return result;
        }
        catch (Exception e)
        {
            Exception inner = e is System.Reflection.TargetInvocationException { InnerException: { } i2 } ? i2 : e;
            throw new EBold(SysUtils.Format(BoldCommonConst.sUnableToCreateFileHandle, FileName, inner.Message));
        }
    }

    /// <summary>Writes and closes every open handler.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldCloseAllFilehandlers()
    {
        if (G_FileHandlerList is not null)
        {
            for (int i = G_FileHandlerList.Count - 1; i >= 0; i--)
            {
                ((TBoldFileHandler)G_FileHandlerList[i]!).Dispose();
            }

            G_FileHandlerList.Clear();
        }
    }

    /// <summary>Closes the handlers whose contents did not change.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldRemoveUnchangedFilesFromEditor()
    {
        if (G_FileHandlerList is not null)
        {
            for (int i = G_FileHandlerList.Count - 1; i >= 0; i--)
            {
                var FileHandler = (TBoldFileHandler)G_FileHandlerList[i]!;
                if (!FileHandler.StringListModified)
                {
                    FileHandler.CloseFileForList();
                    FileHandler.Dispose();
                }
            }
        }
    }
}
