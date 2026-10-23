// Delphi: Source/Common/Support/BoldFileHandler.pas
// Port map: CSharp/port/units/BoldFileHandler.md
namespace Bold.Tests;

public class BoldFileHandlerTests
{
    private string fFolder = "";

    [SetUp]
    public void SetUp()
    {
        fFolder = Path.Combine(Path.GetTempPath(), $"bold-files-{Guid.NewGuid():N}") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(fFolder);
    }

    [TearDown]
    public void TearDown()
    {
        BoldFileHandler.BoldCloseAllFilehandlers();
        foreach (string file in Directory.GetFiles(fFolder))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(fFolder, true);
    }

    [Test]
    [Covers(typeof(TBoldFileHandler), "*")]
    [Covers(typeof(TBoldDiskFileHandler), "*")]
    [Covers(typeof(BoldFileHandler), "*")]
    [Covers("BoldFileHandler", "TBoldFileHandlerClass", "TBoldInitializeFileContents", "initialization", "finalization")]
    public void Handlers_write_indented_lines_and_flush_when_changed()
    {
        TBoldFileHandler file = BoldFileHandler.BoldFileHandlerForFile(fFolder, "Unit1.pas", TBoldModuleType.mtUnit, false, null);
        Assert.That(BoldFileHandler.BoldFileHandlerForFile(fFolder, "UNIT1.PAS", TBoldModuleType.mtUnit, false, null), Is.SameAs(file), "one handler per file name, ignoring case");
        Assert.That((file.IsEmpty, file.ModuleType, file.SetFileName, file.FileFilter), Is.EqualTo((true, TBoldModuleType.mtUnit, fFolder + "Unit1.pas", BoldCommonConst.sFileHandlerMask)));
        file.WriteLn("procedure X;");
        file.StartBlock();
        file.Format("a := %d", 1);
        file.AddString(";");
        file.NewLine();
        file.FormatLn("b := %s;", "'x'");
        file.Indent();
        file.Dedent();
        file.Dedent();
        file.EndBlock(true);
        file.NewLine();
        Assert.That(Enumerable.Range(0, file.Count).Select(file.Content), Is.EqualTo(new[] { "procedure X;", "begin", "  a := 1;", "  b := 'x';", "end;", "", "" }));
        Assert.That(file.TextFoundInFile("A := 1"), Is.True, "searching ignores case");
        Assert.That(file.StringListModified, Is.True);
        file.FlushFile();
        Assert.That(file.StringListModified, Is.False);
        Assert.That(File.ReadAllText(fFolder + "Unit1.pas"), Does.StartWith("procedure X;\r\nbegin\r\n  a := 1;"));

        // The contents load again from the file; an unchanged handler does not write.
        File.WriteAllText(fFolder + "Unit1.pas", "changed outside\r\n");
        Assert.That(file.Content(0), Is.EqualTo("changed outside"));
        file.FlushFile();
        Assert.That(File.ReadAllText(fFolder + "Unit1.pas"), Is.EqualTo("changed outside\r\n"));

        file.ModifyContents("added", false);
        Assert.That((file.Content(1), file.Content(2)), Is.EqualTo(("", "added")), "the text ends with a line break, and another comes before the addition");
        file.ModifyContents("replaced", true);
        Assert.That((file.Count, file.Content(0)), Is.EqualTo((1, "replaced")));
        file.Clear();
        Assert.That(file.IsEmpty, Is.True);
        file.Dispose();
        Assert.That(BoldFileHandler.BoldFileHandlerList().IndexOf(file), Is.EqualTo(-1), "a disposed handler leaves the list");
    }

    [Test]
    [Covers(typeof(TBoldFileHandler), "InitializeStringList", "OnInitializeFileContents")]
    [Covers(typeof(BoldFileHandler), "BoldRemoveUnchangedFilesFromEditor", "BoldCloseAllFilehandlers")]
    public void New_files_get_their_initial_contents_and_unchanged_ones_close()
    {
        TBoldFileHandler file = BoldFileHandler.BoldFileHandlerForFile(fFolder, "Methods.inc", TBoldModuleType.mtIncFile, false, lines => lines.Add("// header"));
        Assert.That(file.Content(0), Is.EqualTo("// header"));
        Assert.That(file.StringListModified, Is.False, "the initial contents are not a change");
        TBoldFileHandler other = BoldFileHandler.BoldFileHandlerForFile(fFolder, "Other.inc", TBoldModuleType.mtIncFile, false, null);
        other.WriteLn("x");
        BoldFileHandler.BoldRemoveUnchangedFilesFromEditor();
        Assert.That(BoldFileHandler.BoldFileHandlerList().IndexOf(file), Is.EqualTo(-1), "the unchanged file is closed");
        Assert.That(File.Exists(fFolder + "Methods.inc"), Is.False, "and not written");
        Assert.That(BoldFileHandler.BoldFileHandlerList().IndexOf(other), Is.Not.EqualTo(-1));
        BoldFileHandler.BoldCloseAllFilehandlers();
        Assert.That(File.ReadAllText(fFolder + "Other.inc"), Is.EqualTo("x\r\n"), "closing writes the changed file");
    }

    [Test]
    [Covers(typeof(TBoldDiskFileHandler), "DoFlushFile")]
    public void A_read_only_file_is_reported_not_written()
    {
        File.WriteAllText(fFolder + "Locked.pas", "old\r\n");
        File.SetAttributes(fFolder + "Locked.pas", FileAttributes.ReadOnly);
        var messages = new List<string>();
        Action<string> show = BoldFileHandler.ShowMessage;
        BoldFileHandler.ShowMessage = messages.Add;
        try
        {
            TBoldFileHandler file = BoldFileHandler.BoldFileHandlerForFile(fFolder, "Locked.pas", TBoldModuleType.mtUnit, false, null);
            file.WriteLn("new");
            file.FlushFile();
        }
        finally
        {
            BoldFileHandler.ShowMessage = show;
        }

        Assert.That(messages, Is.EqualTo(new[] { SysUtils.Format(BoldCommonConst.sModuleReadOnly, fFolder + "Locked.pas") }));
        Assert.That(File.ReadAllText(fFolder + "Locked.pas"), Is.EqualTo("old\r\n"));
    }
}
