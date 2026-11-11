using System.Drawing;
using System.Windows.Forms;

namespace Bold.PortBrowser;

/// <summary>
/// One of the two code panes: a title and the code of a file, read-only, coloured and with line numbers. It shows a
/// line by selecting it, tells where the user clicked, and gives the name under the cursor.
/// </summary>
public sealed class CodePane : UserControl
{
    private readonly Label _title;
    private readonly RichTextBox _box;
    private bool _showing;

    public CodePane(Side side)
    {
        Side = side;
        _title = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0),
            BackColor = side == Side.Delphi ? Color.FromArgb(255, 244, 214) : Color.FromArgb(220, 235, 255),
            Font = new Font(Font, FontStyle.Bold),
            AutoEllipsis = true,
        };
        _box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            WordWrap = false,          // a line of the file is a line of the box
            DetectUrls = false,
            HideSelection = false,     // the selected line shows while a tree has the focus
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Both,
            Font = new Font("Consolas", 10F),
            ShortcutsEnabled = true,
        };
        _box.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                TellLine();
            }
        };
        _box.KeyUp += (_, e) =>
        {
            if (e.KeyCode is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End)
            {
                TellLine();
            }
        };
        _box.MouseDoubleClick += (_, _) => NameChosen?.Invoke(this, EventArgs.Empty);
        Controls.Add(_box);
        Controls.Add(_title);
        ShowMessage(side == Side.Delphi ? "Delphi" : "C#", "");
    }

    public Side Side { get; }

    /// <summary>The file the pane shows, from the repository's root; null while it shows a message.</summary>
    public string? File { get; private set; }

    /// <summary>The line of the cursor, from 1.</summary>
    public int Line => _box.GetLineFromCharIndex(_box.SelectionStart) + 1;

    /// <summary>The user put the cursor on a line of the file.</summary>
    public event EventHandler? LineChosen;

    /// <summary>The user double-clicked a name.</summary>
    public event EventHandler? NameChosen;

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    /// <summary>Shows the file's text (once for a file) and selects the line.</summary>
    public void ShowFile(string file, Func<string> readText, int line, string title)
    {
        _showing = true;
        try
        {
            if (!string.Equals(File, file, StringComparison.OrdinalIgnoreCase))
            {
                string text = readText();
                _box.Rtf = CodeHighlighter.ToRtf(text, CodeHighlighter.LanguageOf(file), _box.Font.Name, (int)Math.Round(_box.Font.SizeInPoints));
                File = file;
            }

            Title = title;
            SelectLine(line);
        }
        finally
        {
            _showing = false;
        }
    }

    /// <summary>Shows a text in place of code: why there is no counterpart, say.</summary>
    public void ShowMessage(string title, string message)
    {
        _showing = true;
        try
        {
            File = null;
            Title = title;
            _box.Rtf = CodeHighlighter.ToRtf(message, CodeLanguage.Plain, "Segoe UI", 10, lineNumbers: false);
            _box.Select(0, 0);
        }
        finally
        {
            _showing = false;
        }
    }

    /// <summary>Selects the line and scrolls it into view, with a few lines above it and the lines after it.</summary>
    public void SelectLine(int line)
    {
        int last = Math.Max(0, _box.GetLineFromCharIndex(_box.TextLength));
        int index = Math.Clamp(line - 1, 0, last);
        // Scrolling to a line below it first leaves the line in the upper part of the pane, with what follows it in view.
        _box.Select(_box.GetFirstCharIndexFromLine(Math.Min(last, index + 24)), 0);
        _box.ScrollToCaret();
        _box.Select(_box.GetFirstCharIndexFromLine(Math.Max(0, index - 4)), 0);
        _box.ScrollToCaret();
        int start = _box.GetFirstCharIndexFromLine(index);
        int end = index < last ? _box.GetFirstCharIndexFromLine(index + 1) - 1 : _box.TextLength;
        if (start >= 0)
        {
            _box.Select(start, Math.Max(0, end - start));
        }
    }

    /// <summary>The name under the cursor, with the name before its dot when it has one: "System.UpdateDatabase".</summary>
    public string? NameAtCursor(bool withOwner)
    {
        if (File is null)
        {
            return null;
        }

        int position = _box.SelectionStart;
        int lineIndex = _box.GetLineFromCharIndex(position);
        int start = _box.GetFirstCharIndexFromLine(lineIndex);
        string[] lines = _box.Lines;
        return lineIndex < lines.Length ? CodeHighlighter.NameAt(lines[lineIndex], position - start, withOwner) : null;
    }

    public void FocusCode() => _box.Focus();

    public bool CodeHasFocus => _box.Focused;

    private void TellLine()
    {
        if (!_showing && File is not null && _box.SelectionLength == 0)
        {
            LineChosen?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _title.Font.Dispose();
            _box.Font.Dispose();
        }

        base.Dispose(disposing);
    }
}
