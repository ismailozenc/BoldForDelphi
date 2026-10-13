using Bold.PortTool.CSharp;

namespace Bold.Tests;

public class CSharpScannerTests
{
    private const string Source = """"
        // Delphi: Source/Sample.pas
        using Bold.Porting;

        [assembly: Delphi(PortStatus.Ported, Unit = "Sample", Symbol = "TCount", Note = "alias " + "of int")]

        namespace Bold;

        public static partial class Sample
        {
            [Delphi(PortStatus.Ported)] public const int LIMIT = 10, OTHER = 11;
            [Delphi(PortStatus.Partial, Note = $"not {"yet"}")] public static readonly string Text = "a;b{c}";
            static Sample() { }
            [Delphi(PortStatus.Ported)]
            public static int Twice(int Value) => Value * 2;
        }

        [Delphi(PortStatus.Ported)]
        public class EFoo(string? message = null) : Exception(message);

        [Delphi(PortStatus.Stub)]
        public delegate void TNotify(object Sender, string Text);

        [Bold.Porting.DelphiAttribute(PortStatus.Ported)]
        public class TFoo : IDisposable
        {
            private readonly Dictionary<string, List<int>> _map = new() { ["a"] = [1, 2] };

            [Delphi(PortStatus.Ported)]
            public TFoo(object Owner) { var s = @"verbatim ""quoted"" }"; }

            [Delphi(PortStatus.Ported, Symbol = "TFoo.Destroy")]
            public void Dispose() { }

            [Delphi(PortStatus.Ported)]
            public int this[int Index] => Index;

            [Delphi(PortStatus.Ported)]
            public string Name { get; set; } = """
                raw { string }
                """;

            public event EventHandler? Changed;

            public IEnumerable<T> Items<T>(Func<T, bool> filter) where T : class => [];

            private class Nested
            {
                public void Inner() { }
            }
        }
        """";

    private static CsFile Scan() => CSharpScanner.Scan(Source, "CSharp/src/Bold.Core/Sample.cs");

    private static CsDeclaration Find(CsFile file, string fullName) => file.Declarations.Single(d => d.FullName == fullName);

    [Test]
    public void Finds_types_and_members_with_their_owners()
    {
        var file = Scan();
        var expected = new[]
        {
            "Sample class", "Sample.LIMIT const", "Sample.OTHER const", "Sample.Text field", "Sample.Sample static constructor",
            "Sample.Twice method", "EFoo class", "TNotify delegate", "TFoo class", "TFoo._map field", "TFoo.TFoo constructor",
            "TFoo.Dispose method", "TFoo.this indexer", "TFoo.Name property", "TFoo.Changed event", "TFoo.Items method",
            "TFoo.Nested class", "TFoo.Nested.Inner method",
        };
        Assert.That(file.Declarations.Select(d => $"{d.FullName} {d.KindText}"), Is.EqualTo(expected));
        Assert.That(file.Warnings, Is.Empty);
        Assert.That(file.UnitName, Is.EqualTo("Sample"));
    }

    [Test]
    public void Reads_marker_arguments()
    {
        var file = Scan();
        var assembly = file.AssemblyAttributes.Single();
        var text = Find(file, "Sample.Text").Attributes.Single();
        var dispose = Find(file, "TFoo.Dispose").Attributes.Single();
        Assert.Multiple(() =>
        {
            Assert.That(assembly.Named("Symbol")!.AsString(), Is.EqualTo("TCount"));
            Assert.That(assembly.Named("Note")!.AsString(), Is.EqualTo("alias of int"), "literals joined with + are read");
            Assert.That(text.Positional.First().AsEnumMember("PortStatus"), Is.EqualTo("Partial"));
            Assert.That(text.Named("Note")!.AsString(), Is.Null, "an interpolated string is not a constant");
            Assert.That(dispose.Named("Symbol")!.AsString(), Is.EqualTo("TFoo.Destroy"));
            Assert.That(Find(file, "TFoo").Attributes.Single().Name, Is.EqualTo("Delphi"), "qualified names and the Attribute suffix are dropped");
        });
    }

    [Test]
    public void Counts_parameters_and_records_lines()
    {
        var file = Scan();
        Assert.Multiple(() =>
        {
            Assert.That(Find(file, "Sample.Twice").ParameterCount, Is.EqualTo(1));
            Assert.That(Find(file, "TNotify").ParameterCount, Is.EqualTo(2));
            Assert.That(Find(file, "TFoo.this").ParameterCount, Is.EqualTo(1));
            Assert.That(Find(file, "TFoo.Items").ParameterCount, Is.EqualTo(1), "generic arguments do not add parameters");
            Assert.That(Find(file, "Sample.LIMIT").Line, Is.EqualTo(10));
            Assert.That(Find(file, "Sample.Twice").Line, Is.EqualTo(14), "a member's line is its declaration, after its attributes");
            Assert.That(Find(file, "TFoo.Nested").IsPrivate, Is.True);
        });
    }

    [Test]
    public void Using_aliases_are_declarations()
    {
        var file = CSharpScanner.Scan("""
            global using TCount = int;
            global using TSet = global::Bold.DelphiSet<byte>;
            using static System.Math;
            using System.Text;
            """, "CSharp/src/Bold.Core/Sample.Aliases.cs");
        Assert.That(file.Declarations.Select(d => $"{d.Name} {d.KindText}"), Is.EqualTo(new[] { "TCount alias", "TSet alias" }));
        Assert.That(file.Declarations.Select(d => d.Line), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Reads_typeof_and_nameof_arguments()
    {
        var file = CSharpScanner.Scan("""
            namespace Bold.Tests;
            public class T
            {
                [Test, Covers(typeof(global::Bold.TFoo<int>), nameof(TFoo.Name), "Items")]
                public void Check() { }
            }
            """, "x.cs");
        var covers = file.Declarations.Single(d => d.Name == "Check").Attributes.Single(a => a.Name == "Covers");
        var arguments = covers.Positional.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(arguments[0].AsTypeOf(), Is.EqualTo("Bold.TFoo"));
            Assert.That(arguments[1].AsString(), Is.EqualTo("Name"));
            Assert.That(arguments[2].AsString(), Is.EqualTo("Items"));
            Assert.That(file.Declarations.Single(d => d.Name == "Check").IsTestMethod, Is.True);
        });
    }
}
