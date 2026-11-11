// The hand-written pages of the port (the readmes, the user guide, the tutorials, the plan): the files they link to are
// there, and the tutorials cover every Library example.
using System.Text.RegularExpressions;

namespace Bold.Tests;

public partial class DocumentationTests
{
    [GeneratedRegex(@"\]\(([^)\s]+)\)")]
    private static partial Regex Link();

    /// <summary>The pages people write: every .md under CSharp, but the status pages the port tool writes and build output.</summary>
    private static IEnumerable<string> Pages()
    {
        string root = RepoPaths.Full("CSharp");
        string generated = Path.Combine(root, "port") + Path.DirectorySeparatorChar;
        char separator = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(file => !file.StartsWith(generated, StringComparison.Ordinal))
            .Where(file => !new[] { "bin", "obj", "artifacts", "TestResults", "node_modules" }
                .Any(folder => file.Contains($"{separator}{folder}{separator}", StringComparison.Ordinal)));
    }

    [Test]
    public void The_pages_link_to_files_that_are_there()
    {
        var broken = new List<string>();
        int links = 0;
        foreach (string page in Pages())
        {
            foreach (Match match in Link().Matches(File.ReadAllText(page)))
            {
                string target = match.Groups[1].Value;
                if (target.StartsWith('#') || target.Contains("://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    continue;   // a heading of the same page, or the web
                }

                links++;
                string path = Uri.UnescapeDataString(target.Split('#')[0]).Replace('/', Path.DirectorySeparatorChar);
                string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, path));
                if (!File.Exists(full) && !Directory.Exists(full))
                {
                    broken.Add($"{Path.GetRelativePath(RepoPaths.Root, page)}: {target}");
                }
            }
        }

        Assert.That(links, Is.GreaterThan(20), "the pages were found");
        Assert.That(broken, Is.Empty, "links to files that are not there");
    }

    [Test]
    public void The_tutorials_cover_every_Library_example()
    {
        string tutorials = File.ReadAllText(RepoPaths.Full("CSharp/docs/TUTORIALS.md"));
        string[] examples = Directory.EnumerateFiles(RepoPaths.Full("CSharp/examples/Library"), "Library.*.csproj", SearchOption.AllDirectories)
            .Select(project => Path.GetFileName(Path.GetDirectoryName(project)!))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.That(examples, Has.Length.EqualTo(9));
        foreach (string example in examples)
        {
            Assert.That(tutorials, Does.Contain($"dotnet run --project examples/Library/{example}"), $"the tutorial of {example} says how to run it");
        }
    }

    /// <summary>What a tutorial says its program prints is what the program's test checks: the lines that do not change from run to run.</summary>
    [TestCase("The model Library: 5 classes, 3 associations.")]
    [TestCase("Before 1970: The Left Hand of Darkness, Solaris")]
    [TestCase("Cy gets none: no copy is left.")]
    [TestCase("  Not saved: Member 'Ann': At most three loans.")]
    [TestCase("Back office cannot change the title: Front desk holds the lock.")]
    [TestCase("  Move data from Book.Year to Book.Published (dbtypes: 2)")]
    [TestCase("Library.xmi: 4 UML classes and 3 associations, for a UML tool.")]
    [TestCase("Read again: Solaris, ISBN 978-0-306-40615-7, Worn, shelf M-1.")]
    [TestCase("Checked in, Central: Solaris (3 copies), The Cyberiad (1 copy).")]
    public void The_tutorials_quote_what_the_examples_tests_check(string line)
    {
        Assert.That(File.ReadAllText(RepoPaths.Full("CSharp/docs/TUTORIALS.md")), Does.Contain(line));
        Assert.That(File.ReadAllText(RepoPaths.Full("CSharp/tests/Bold.Examples.Tests/LibraryExamplesTests.cs")), Does.Contain(line.Trim()),
            "the examples' tests check this line of output");
    }
}
