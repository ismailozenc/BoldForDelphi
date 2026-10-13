using System.Xml.Linq;

namespace Bold.PortTool.Port;

public enum TestOutcome
{
    NotRun,
    Passed,
    Failed,
}

/// <summary>
/// Test results by test method ("Namespace.Class.Method"), read from Visual Studio TRX files (dotnet test --logger trx)
/// and NUnit 3 result files (NUnitLite --result). A method passes when every case of it passed.
/// </summary>
public sealed class TestResults
{
    private readonly Dictionary<string, TestOutcome> _outcomes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _messages = new(StringComparer.Ordinal);

    public List<string> Sources { get; } = [];

    public int Count => _outcomes.Count;

    public bool IsEmpty => _outcomes.Count == 0;

    public TestOutcome Outcome(string className, string method) =>
        _outcomes.GetValueOrDefault($"{className}.{method}", TestOutcome.NotRun);

    public IEnumerable<KeyValuePair<string, TestOutcome>> All => _outcomes;

    /// <summary>Failed test methods ("Namespace.Class.Method") with the first failure message of each.</summary>
    public IEnumerable<KeyValuePair<string, string>> Failures =>
        _outcomes.Where(o => o.Value == TestOutcome.Failed).Select(o => new KeyValuePair<string, string>(o.Key, _messages.GetValueOrDefault(o.Key, "")));

    public static TestResults Load(RepoLayout layout, string? directory)
    {
        var results = new TestResults();
        string dir = directory ?? layout.DefaultResultsDir;
        if (!Directory.Exists(dir))
        {
            return results;
        }

        foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (file.EndsWith(".trx", StringComparison.OrdinalIgnoreCase))
            {
                results.ReadTrx(file);
                results.Sources.Add(layout.Relative(file));
            }
            else if (file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && results.ReadNUnit(file))
            {
                results.Sources.Add(layout.Relative(file));
            }
        }

        return results;
    }

    private void Record(string className, string method, TestOutcome outcome, string? message = null)
    {
        int paren = method.IndexOf('(');
        if (paren >= 0)
        {
            method = method[..paren];
        }

        string key = $"{className}.{method}";
        if (outcome == TestOutcome.Failed && !string.IsNullOrWhiteSpace(message) && !_messages.ContainsKey(key))
        {
            _messages[key] = message.Trim();
        }

        var previous = _outcomes.GetValueOrDefault(key, TestOutcome.NotRun);
        _outcomes[key] = previous == TestOutcome.Failed || outcome == TestOutcome.Failed ? TestOutcome.Failed
            : previous == TestOutcome.Passed || outcome == TestOutcome.Passed ? TestOutcome.Passed
            : TestOutcome.NotRun;
    }

    private void ReadTrx(string file)
    {
        var document = XDocument.Load(file);
        XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var methods = document.Descendants(ns + "UnitTest")
            .Select(t => (Id: (string?)t.Attribute("id"), Method: t.Element(ns + "TestMethod")))
            .Where(t => t.Id is not null && t.Method is not null)
            .ToDictionary(t => t.Id!, t => ((string?)t.Method!.Attribute("className") ?? "", (string?)t.Method!.Attribute("name") ?? ""));
        foreach (var result in document.Descendants(ns + "UnitTestResult"))
        {
            string? id = (string?)result.Attribute("testId");
            if (id is null || !methods.TryGetValue(id, out var method))
            {
                continue;
            }

            string? message = (string?)result.Descendants(ns + "Message").FirstOrDefault();
            string? stack = (string?)result.Descendants(ns + "StackTrace").FirstOrDefault();
            Record(method.Item1, method.Item2, ParseOutcome((string?)result.Attribute("outcome")), Join(message, stack));
        }
    }

    private bool ReadNUnit(string file)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(file);
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }

        if (document.Root?.Name.LocalName != "test-run")
        {
            return false;
        }

        foreach (var testCase in document.Descendants("test-case"))
        {
            string? className = (string?)testCase.Attribute("classname");
            string? method = (string?)testCase.Attribute("methodname");
            if (className is null || method is null)
            {
                continue;
            }

            var failure = testCase.Element("failure");
            Record(className, method, ParseOutcome((string?)testCase.Attribute("result")),
                Join((string?)failure?.Element("message"), (string?)failure?.Element("stack-trace")));
        }

        return true;
    }

    private static string? Join(string? message, string? stack) =>
        message is null ? stack : stack is null ? message : $"{message.TrimEnd()}\n{stack.Trim()}";

    private static TestOutcome ParseOutcome(string? outcome) => outcome switch
    {
        "Passed" => TestOutcome.Passed,
        "Failed" or "Error" or "Timeout" or "Aborted" => TestOutcome.Failed,
        _ => TestOutcome.NotRun,
    };
}
