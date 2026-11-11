using System.Drawing;
using System.Windows.Forms;
using Bold.PortTool.Port;

namespace Bold.PortBrowser;

/// <summary>
/// The browser's window: the Delphi tree on the left, the C# tree on the right, the code of both in the middle, and
/// below them what the port did with the selection and why. Whatever is selected on one side, the other side follows.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Options _options;
    private readonly History _history = new();
    private readonly SideTree _delphiTree = new(Side.Delphi);
    private readonly SideTree _csharpTree = new(Side.CSharp);
    private readonly CodePane _delphiCode = new(Side.Delphi);
    private readonly CodePane _csharpCode = new(Side.CSharp);
    private readonly Label _summary;
    private readonly ListView _facts;
    private readonly ListView _tests;
    private readonly ListView _parts;
    private readonly ListView _found;
    private readonly TabControl _tabs;
    private readonly TabPage _testsPage;
    private readonly TabPage _partsPage;
    private readonly TabPage _foundPage;
    private readonly ToolStripButton _back;
    private readonly ToolStripButton _forward;
    private readonly ToolStripTextBox _search;
    private readonly ToolStripComboBox _filter;
    private readonly ToolStripStatusLabel _status;
    private readonly ToolStripStatusLabel _totals;
    private PortIndex? _index;
    private BrowserNode? _current;
    private string? _repository;
    private bool _navigating;

    public MainForm(Options options)
    {
        _options = options;
        Text = "Bold Port Browser";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1500, 900);
        KeyPreview = true;

        // The toolbar.
        _back = Button("Back", "Back (Alt+Left)", (_, _) => Go(_history.Back()));
        _forward = Button("Forward", "Forward (Alt+Right)", (_, _) => Go(_history.Forward()));
        _search = new ToolStripTextBox { AutoSize = false, Width = 220, ToolTipText = "A name or part of one; Enter finds it on both sides (Ctrl+F)" };
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Search();
            }
        };
        _filter = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Width = 150, ToolTipText = "Show only what has this status" };
        _filter.Items.Add("Everything");
        foreach (Indicator indicator in Enum.GetValues<Indicator>())
        {
            _filter.Items.Add(PortIndex.StatusText(indicator));
        }

        _filter.Items.Add("New in C#");
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();
        var tools = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        tools.Items.AddRange([
            _back, _forward,
            Button("Reload", "Read the sources again (F5)", (_, _) => LoadRepository(_repository)),
            new ToolStripSeparator(),
            new ToolStripLabel("Find:"), _search, Button("Find", "Find the name on both sides", (_, _) => Search()),
            new ToolStripSeparator(),
            new ToolStripLabel("Show:"), _filter,
            new ToolStripSeparator(),
            Button("Go to Name", "Go to the name under the cursor and its counterpart (F12, or a double click)", (_, _) => GoToNameAtCursor()),
            Button("Open in Visual Studio", "Open the selected file in Visual Studio, at its line", (_, _) => OpenOutside()),
            Button("Status Page", "Open the unit's status page (CSharp/port/units)", (_, _) => OpenStatusPage()),
            new ToolStripSeparator(),
            Button("Repository...", "Choose the repository folder (the one with Source and CSharp)", (_, _) => ChooseRepository()),
        ]);

        // The details.
        _summary = new Label { Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0), AutoEllipsis = true };
        _summary.Font = new Font(_summary.Font, FontStyle.Bold);
        _facts = List(("What", 130), ("", 1200));
        _tests = List(("Test", 520), ("File", 620), ("Line", 60));
        _tests.ItemActivate += (_, _) => Activate(_tests);
        _parts = List(("Counterpart", 420), ("Kind", 110), ("Status", 100), ("File", 620), ("Line", 60));
        _parts.ItemActivate += (_, _) => Activate(_parts);
        _found = List(("Name", 320), ("Side", 70), ("Kind", 110), ("Status", 100), ("Where", 760));
        _found.ItemActivate += (_, _) => Activate(_found);
        var detailsPage = new TabPage("Details");
        detailsPage.Controls.Add(_facts);
        detailsPage.Controls.Add(_summary);
        _testsPage = new TabPage("Tests");
        _testsPage.Controls.Add(_tests);
        _partsPage = new TabPage("Counterparts");
        _partsPage.Controls.Add(_parts);
        _foundPage = new TabPage("Found");
        _foundPage.Controls.Add(_found);
        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.AddRange([detailsPage, _testsPage, _partsPage, _foundPage]);

        // The layout: [Delphi tree | Delphi code | C# code | C# tree] above the details.
        var code = new SplitContainer { Dock = DockStyle.Fill };
        code.Panel1.Controls.Add(_delphiCode);
        code.Panel2.Controls.Add(_csharpCode);
        _delphiCode.Dock = DockStyle.Fill;
        _csharpCode.Dock = DockStyle.Fill;
        var right = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2 };
        right.Panel1.Controls.Add(code);
        right.Panel2.Controls.Add(Titled("C# port", _csharpTree));
        var left = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
        left.Panel1.Controls.Add(Titled("Delphi source", _delphiTree));
        left.Panel2.Controls.Add(right);
        var all = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, FixedPanel = FixedPanel.Panel2 };
        all.Panel1.Controls.Add(left);
        all.Panel2.Controls.Add(_tabs);

        _status = new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _totals = new ToolStripStatusLabel("");
        var statusBar = new StatusStrip();
        statusBar.Items.AddRange([_status, _totals]);

        Controls.Add(all);
        Controls.Add(tools);
        Controls.Add(statusBar);

        Shown += (_, _) =>
        {
            // Set once the window has its size: a splitter's distance must fit its container.
            all.SplitterDistance = Math.Max(200, all.Height - 230);
            left.SplitterDistance = 300;
            right.SplitterDistance = Math.Max(300, right.Width - 320);
            code.SplitterDistance = code.Width / 2;
            LoadRepository(FirstRepository());
        };

        _delphiTree.NodeChosen += (_, node) => Navigate(node);
        _csharpTree.NodeChosen += (_, node) => Navigate(node);
        foreach (CodePane pane in new[] { _delphiCode, _csharpCode })
        {
            pane.LineChosen += (_, _) => LineChosen(pane);
            pane.NameChosen += (_, _) => GoToName(pane);
        }

        _delphiTree.ContextMenuStrip = TreeMenu(_delphiTree);
        _csharpTree.ContextMenuStrip = TreeMenu(_csharpTree);
        UpdateButtons();
    }

    /// <summary>Whether both code panes show a file: what the --close switch reports.</summary>
    public bool ShowsBothSides => _delphiCode.File is not null && _csharpCode.File is not null;

    /// <summary>The loaded index; null until the repository has been read.</summary>
    public PortIndex? Index => _index;

    /// <summary>The repository has been read and the window shows where it was told to start.</summary>
    public bool Ready { get; private set; }

    public BrowserNode? Current => _current;

    private static ToolStripButton Button(string text, string tip, EventHandler click)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = tip };
        button.Click += click;
        return button;
    }

    private static ListView List(params (string Title, int Width)[] columns)
    {
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, BorderStyle = BorderStyle.None, ShowItemToolTips = true };
        foreach ((string title, int width) in columns)
        {
            list.Columns.Add(title, width);
        }

        return list;
    }

    private static Control Titled(string title, Control control)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var label = new Label { Text = title, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
        label.Font = new Font(label.Font, FontStyle.Bold);
        panel.Controls.Add(control);
        panel.Controls.Add(label);
        return panel;
    }

    private ContextMenuStrip TreeMenu(SideTree tree)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open in Visual Studio", null, (_, _) => OpenOutside(tree.Selected));
        menu.Items.Add("Open the Status Page", null, (_, _) => OpenStatusPage(tree.Selected));
        menu.Items.Add("Copy the Path", null, (_, _) =>
        {
            if (tree.Selected is { } node)
            {
                Clipboard.SetText(node.File is null ? node.Path : $"{node.File}:{node.Line}");
            }
        });
        // A right click selects the node it is on, as Explorer does.
        tree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                tree.SelectedNode = e.Node;
            }
        };
        return menu;
    }

    // ---- the repository ----

    private string? FirstRepository() =>
        PortIndex.FindRepository(_options.Repository) ??
        PortIndex.FindRepository(_options.Open is null ? null : Path.GetDirectoryName(Path.GetFullPath(_options.Open))) ??
        PortIndex.FindRepository(Directory.GetCurrentDirectory()) ??
        PortIndex.FindRepository(AppContext.BaseDirectory) ??
        PortIndex.FindRepository(Settings.Repository);

    private void ChooseRepository()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "The Bold repository: the folder with Source (Delphi) and CSharp",
            UseDescriptionForTitle = true,
            SelectedPath = _repository ?? "",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string? repository = PortIndex.FindRepository(dialog.SelectedPath);
        if (repository is null)
        {
            MessageBox.Show(this, $"{dialog.SelectedPath} is not in a Bold repository: no folder above it has both Source and CSharp.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadRepository(repository);
    }

    /// <summary>Reads both code bases in the background, then shows the trees and goes where the command line said.</summary>
    private void LoadRepository(string? repository)
    {
        if (repository is null)
        {
            _status.Text = "No repository: choose the folder with Source and CSharp (Repository...).";
            if (_options.Close)
            {
                Close();
            }
            else
            {
                ChooseRepository();
            }

            return;
        }

        _repository = repository;
        BrowserNode? keep = _current;
        _status.Text = $"Reading the Delphi source and the C# port in {repository} ...";
        UseWaitCursor = true;
        Enabled = false;
        string? results = _options.Results;
        // The reading runs on another thread; its result comes back to the window's own thread by BeginInvoke.
        Task.Run(() =>
        {
            PortIndex? index = null;
            Exception? problem = null;
            try
            {
                index = PortIndex.Load(repository, results);
            }
            catch (Exception e) when (e is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                problem = e;
            }

            try
            {
                BeginInvoke(() => Loaded(index, problem, keep));
            }
            catch (InvalidOperationException)
            {
                // the window was closed while the sources were read
            }
        });
    }

    private void Loaded(PortIndex? index, Exception? problem, BrowserNode? keep)
    {
        if (IsDisposed)
        {
            return;
        }

        Enabled = true;
        UseWaitCursor = false;
        if (index is null)
        {
            _status.Text = $"The repository could not be read: {problem?.Message}";
            if (!_options.Close)
            {
                MessageBox.Show(this, problem?.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            _history.Clear();
            _current = null;
            Settings.Repository = index.Root;
            Text = $"Bold Port Browser - {index.Root}";
            _index = index;
            ApplyFilter();
            _totals.Text = index.TotalsOf(index.DelphiRoot) + (index.HasTestResults ? "" : " (no test results: nothing shows as verified)");
            _status.Text = "Select a file or a symbol on either side.";
            BrowserNode? start = keep is null ? Start(index) : keep.File is null ? null : index.NodeAt(keep.File, keep.Line);
            if (start is not null)
            {
                Navigate(start);
            }

            Ready = true;
        }

        if (_options.Close)
        {
            // Let the window paint what it shows, then end with what it showed.
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        }
    }

    /// <summary>Where the command line says to start: a file and a line, or a name.</summary>
    private BrowserNode? Start(PortIndex index)
    {
        if (_options.Open is not null)
        {
            string file = Path.GetFullPath(_options.Open);
            BrowserNode? node = _options.Line > 0 ? index.NodeAt(file, _options.Line) : index.FindFile(file);
            if (node is not null)
            {
                return node;
            }

            _status.Text = $"{_options.Open} is not a source of the repository.";
        }

        return _options.Symbol is null ? null : index.FindByName(_options.Symbol, side: Side.Delphi) ?? index.FindByName(_options.Symbol, side: Side.CSharp);
    }

    // ---- showing a node ----

    private CodePane Pane(Side side) => side == Side.Delphi ? _delphiCode : _csharpCode;

    private SideTree Tree(Side side) => side == Side.Delphi ? _delphiTree : _csharpTree;

    private static Side Other(Side side) => side == Side.Delphi ? Side.CSharp : Side.Delphi;

    private void Go(BrowserNode? node)
    {
        if (node is not null)
        {
            Navigate(node, record: false);
        }

        UpdateButtons();
    }

    /// <summary>
    /// Shows a node: its code on its side, its counterpart's on the other (or why there is none), both trees on them,
    /// and the details. <paramref name="keep"/> is the pane the user is reading in, which is not scrolled.
    /// </summary>
    private void Navigate(BrowserNode node, bool record = true, CodePane? keep = null)
    {
        if (_index is null || _navigating)
        {
            return;
        }

        PortIndex index = _index;
        _navigating = true;
        try
        {
            _current = node;
            Tree(node.Side).Select(node);
            if (node.IsCode)
            {
                if (keep != Pane(node.Side))
                {
                    ShowCode(index, node);
                }

                IReadOnlyList<BrowserNode> counterparts = index.CounterpartsOf(node);
                if (counterparts.Count > 0)
                {
                    ShowCode(index, counterparts[0]);
                    Tree(Other(node.Side)).Select(counterparts[0]);
                }
                else
                {
                    Pane(Other(node.Side)).ShowMessage(node.Side == Side.Delphi ? "C#: no counterpart" : "Delphi: no counterpart", index.WhyNoCounterpart(node) ?? "");
                    Tree(Other(node.Side)).Select(null);
                }

                ShowDetails(index, node, counterparts);
            }
            else
            {
                ShowDetails(index, node, []);
            }

            if (record)
            {
                _history.Visit(node);
            }

            _status.Text = $"{(node.Side == Side.Delphi ? "Delphi" : "C#")}: {node.Path}" + (node.File is not null && node.Kind != NodeKind.File ? $" (line {node.Line})" : "");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _status.Text = e.Message;
        }
        finally
        {
            _navigating = false;
            UpdateButtons();
        }
    }

    private void ShowCode(PortIndex index, BrowserNode node)
    {
        if (node.File is null)
        {
            return;
        }

        // The status first: a long path is cut at its end.
        string status = node.Status is Indicator indicator ? $"[{PortIndex.StatusText(indicator)}]  " : node.IsNew ? "[new in C#]  " : "";
        Pane(node.Side).ShowFile(node.File, () => index.ReadText(node.File), node.Line, $"{status}{node.File}");
    }

    private void ShowDetails(PortIndex index, BrowserNode node, IReadOnlyList<BrowserNode> counterparts)
    {
        NodeDetails details = index.Describe(node);
        _summary.Text = $"{node.Name}: {details.Summary}";
        _summary.ForeColor = node.Status is Indicator indicator ? StatusImages.ColorOf(indicator == Indicator.Ported ? Indicator.Verified : indicator) : SystemColors.ControlText;
        Fill(_facts, details.Facts.Select(fact => (new[] { fact.Key, fact.Value }, (object?)null)));
        Fill(_tests, details.Tests.Select(test => (new[] { test.ShortName, test.File, test.Line.ToString() }, (object?)index.NodeAt(test.File, test.Line))));
        Fill(_parts, counterparts.Select(part => (
            new[] { part.Parent is { IsCode: true } parent ? $"{parent.Name}.{part.Name}" : part.Name, part.KindText, part.Status is Indicator s ? PortIndex.StatusText(s) : "", part.File ?? "", part.Line.ToString() },
            (object?)part)));
        _testsPage.Text = $"Tests ({details.Tests.Count})";
        _partsPage.Text = $"Counterparts ({counterparts.Count})";
    }

    private static void Fill(ListView list, IEnumerable<(string[] Cells, object? Tag)> rows)
    {
        list.BeginUpdate();
        try
        {
            list.Items.Clear();
            foreach ((string[] cells, object? tag) in rows)
            {
                list.Items.Add(new ListViewItem(cells) { Tag = tag, ToolTipText = cells[^1].Length > 80 ? cells[^1] : "" });
            }
        }
        finally
        {
            list.EndUpdate();
        }
    }

    /// <summary>A double click or Enter on a row of a list goes to its node.</summary>
    private void Activate(ListView list)
    {
        if (list.SelectedItems.Count > 0 && list.SelectedItems[0].Tag is BrowserNode node)
        {
            Navigate(node);
        }
    }

    // ---- reading in the code ----

    /// <summary>The cursor went to a line: the declaration it is in becomes the selection, and the other side follows.</summary>
    private void LineChosen(CodePane pane)
    {
        if (_index is null || pane.File is null)
        {
            return;
        }

        BrowserNode? node = _index.NodeAt(pane.File, pane.Line);
        if (node is not null && node != _current)
        {
            Navigate(node, keep: pane);
        }
    }

    private void GoToNameAtCursor() => GoToName(_csharpCode.CodeHasFocus ? _csharpCode : _delphiCode);

    /// <summary>Goes to the declaration of the name under the cursor, on the side it was read on, and to its counterpart.</summary>
    private void GoToName(CodePane pane)
    {
        if (_index is null || pane.File is null)
        {
            return;
        }

        BrowserNode? from = _index.NodeAt(pane.File, pane.Line);
        string? qualified = pane.NameAtCursor(withOwner: true);
        string? name = pane.NameAtCursor(withOwner: false);
        if (name is null)
        {
            return;
        }

        BrowserNode? target = (qualified is null ? null : _index.FindByName(qualified, from)) ?? _index.FindByName(name, from);
        if (target is null)
        {
            _status.Text = $"{name} is no file, type or member of the Delphi source or the C# port.";
        }
        else
        {
            Navigate(target);
        }
    }

    // ---- finding and filtering ----

    private void Search()
    {
        if (_index is null)
        {
            return;
        }

        PortIndex index = _index;
        IReadOnlyList<BrowserNode> found = index.Search(_search.Text);
        Fill(_found, found.Select(node => (
            new[] { node.Parent is { IsCode: true, Kind: not NodeKind.File } parent ? $"{parent.Name}.{node.Name}" : node.Name, node.Side == Side.Delphi ? "Delphi" : "C#", node.KindText,
                node.Status is Indicator s ? PortIndex.StatusText(s) : node.IsNew ? "new in C#" : "", node.File is null ? node.Path : $"{node.File} line {node.Line}" },
            (object?)node)));
        _foundPage.Text = $"Found ({found.Count})";
        _tabs.SelectedTab = _foundPage;
        _status.Text = found.Count == 0 ? $"Nothing has \"{_search.Text.Trim()}\" in its name." : $"{found.Count} names with \"{_search.Text.Trim()}\"; a double click goes there.";
        if (found.Count > 0)
        {
            Navigate(found[0]);
        }
    }

    private void ApplyFilter()
    {
        if (_index is null)
        {
            return;
        }

        Func<BrowserNode, bool>? show = null;
        int choice = _filter.SelectedIndex;
        Indicator[] indicators = Enum.GetValues<Indicator>();
        if (choice > 0 && choice <= indicators.Length)
        {
            Indicator wanted = indicators[choice - 1];
            show = node => node.Status == wanted;
        }
        else if (choice > indicators.Length)
        {
            show = node => node.IsNew;
        }

        _delphiTree.Show(_index.DelphiRoot, show);
        _csharpTree.Show(_index.CSharpRoot, show);
        if (_current is not null)
        {
            _delphiTree.Select(_current.Side == Side.Delphi ? _current : _index.CounterpartsOf(_current).FirstOrDefault());
            _csharpTree.Select(_current.Side == Side.CSharp ? _current : _index.CounterpartsOf(_current).FirstOrDefault());
        }
    }

    // ---- outside the browser ----

    private void OpenOutside(BrowserNode? node = null)
    {
        node ??= _current;
        if (_index is null || node?.File is null)
        {
            return;
        }

        string? problem = ExternalEditor.Open(_index.FullPath(node.File), node.Line);
        _status.Text = problem ?? $"Opened {node.File}.";
    }

    private void OpenStatusPage(BrowserNode? node = null)
    {
        node ??= _current;
        if (_index is null || node?.Unit is null)
        {
            _status.Text = "A status page belongs to a Delphi unit: select one, or something in one.";
            return;
        }

        string page = _index.FullPath(RepoLayout.UnitPage(node.Unit.Unit.Name));
        _status.Text = File.Exists(page) ? ExternalEditor.Shell(page) ?? $"Opened {page}." : $"{page} is not there.";
    }

    private void UpdateButtons()
    {
        _back.Enabled = _history.CanGoBack;
        _forward.Enabled = _history.CanGoForward;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Alt | Keys.Left:
                Go(_history.Back());
                return true;
            case Keys.Alt | Keys.Right:
                Go(_history.Forward());
                return true;
            case Keys.F5:
                LoadRepository(_repository);
                return true;
            case Keys.F12:
                GoToNameAtCursor();
                return true;
            case Keys.Control | Keys.F:
                _search.Focus();
                _search.SelectAll();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
