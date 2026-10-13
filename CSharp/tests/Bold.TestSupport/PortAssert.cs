using System.Globalization;
using System.Reflection;
using Bold.PortTool.Delphi;

namespace Bold.Tests;

/// <summary>
/// Assertions that compare ported C# declarations with the Delphi declarations they came from. The expected values are
/// read from the Delphi source at test time, so a test cannot pass with a value that was typed in wrong twice.
/// </summary>
public static class PortAssert
{
    /// <summary>
    /// Every public constant of <paramref name="unitClass"/> that carries a [Delphi] marker has the value of the Delphi
    /// constant (or resource string) of the same name in <paramref name="delphiFile"/>.
    /// </summary>
    public static void ConstantsMatchDelphi(Type unitClass, string delphiFile)
    {
        var expected = DelphiSource.Constants(delphiFile);
        var failures = new List<string>();
        int compared = 0;
        foreach (var field in unitClass.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral || f.IsInitOnly))
        {
            var marker = field.GetCustomAttributes<DelphiAttribute>().FirstOrDefault();
            if (marker is null || marker.Status is not (PortStatus.Ported or PortStatus.Replaced))
            {
                continue;
            }

            string name = marker.Symbol ?? field.Name;
            if (!expected.TryGetValue(name, out object? value))
            {
                failures.Add($"{name}: {delphiFile} has no such constant");
                continue;
            }

            if (value is null)
            {
                failures.Add($"{name}: the Delphi value cannot be evaluated; check it in a test of its own and leave it out here");
                continue;
            }

            object? actual = field.GetValue(null);
            if (!Same(value, actual))
            {
                failures.Add($"{name}: Delphi {Show(value)}, C# {Show(actual)}");
            }

            compared++;
        }

        Assert.That(failures, Is.Empty, $"Constants of {unitClass.Name} that differ from {delphiFile}");
        Assert.That(compared, Is.GreaterThan(0), $"{unitClass.Name} has no ported constants to compare");
    }

    /// <summary>Each enum has the Delphi members in the Delphi order, with the same names and ordinal values.</summary>
    public static void EnumsMatchDelphi(string delphiFile, params Type[] enums)
    {
        var failures = new List<string>();
        foreach (var type in enums)
        {
            var symbol = DelphiSource.Symbol(delphiFile, DelphiName(type));
            if (symbol.EnumMembers is null)
            {
                failures.Add($"{type.Name}: not an enum in {delphiFile}");
                continue;
            }

            var expected = symbol.EnumMembers.Select(m => $"{m.Key}={m.Value}");
            var actual = Enum.GetValues(type).Cast<object>()
                .Select(v => $"{Enum.GetName(type, v)}={Convert.ToInt64(v, CultureInfo.InvariantCulture)}");
            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"{type.Name}: Delphi ({string.Join(", ", expected)}), C# ({string.Join(", ", actual)})");
            }
        }

        Assert.That(failures, Is.Empty, $"Enums that differ from {delphiFile}");
    }

    /// <summary>Each class derives from the C# port of its Delphi parent (Exception for Exception, object for TObject).</summary>
    public static void ClassParentsMatchDelphi(string delphiFile, params Type[] classes)
    {
        var failures = new List<string>();
        foreach (var type in classes)
        {
            var symbol = DelphiSource.Symbol(delphiFile, DelphiName(type));
            string parent = symbol.Parent ?? "TObject";
            var actual = type.BaseType;
            bool ok = parent.ToLowerInvariant() switch
            {
                "tobject" => actual == typeof(object),
                "exception" => actual == typeof(Exception),
                _ => string.Equals(actual?.Name, parent, StringComparison.Ordinal),
            };
            if (!ok)
            {
                failures.Add($"{type.Name}: Delphi parent {parent}, C# base {actual?.Name ?? "none"}");
            }
        }

        Assert.That(failures, Is.Empty, $"Classes whose parent differs from {delphiFile}");
    }

    /// <summary>Each delegate has the parameters of its Delphi event type, with the same names, and returns a value only if the Delphi type is a function.</summary>
    public static void DelegatesMatchDelphi(string delphiFile, params Type[] delegates)
    {
        var failures = new List<string>();
        foreach (var type in delegates)
        {
            var symbol = DelphiSource.Symbol(delphiFile, DelphiName(type));
            var invoke = type.GetMethod("Invoke") ?? throw new ArgumentException($"{type.Name} is not a delegate");
            var actualNames = invoke.GetParameters().Select(p => p.Name ?? "").ToList();
            if (!symbol.ParameterNames.SequenceEqual(actualNames, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add($"{type.Name}: Delphi parameters ({string.Join(", ", symbol.ParameterNames)}), C# ({string.Join(", ", actualNames)})");
            }

            bool delphiReturns = symbol.ReturnType is not null;
            bool csharpReturns = invoke.ReturnType != typeof(void);
            if (delphiReturns != csharpReturns)
            {
                failures.Add($"{type.Name}: Delphi {(delphiReturns ? $"returns {symbol.ReturnType}" : "returns nothing")}, C# returns {invoke.ReturnType.Name}");
            }
        }

        Assert.That(failures, Is.Empty, $"Delegates whose parameters differ from {delphiFile}");
    }

    private static string DelphiName(Type type)
    {
        var marker = type.GetCustomAttributes<DelphiAttribute>().FirstOrDefault();
        return marker?.Symbol ?? type.Name;
    }

    private static bool Same(object expected, object? actual) => (expected, actual) switch
    {
        (long e, sbyte or byte or short or ushort or int or uint or long) => e == Convert.ToInt64(actual, CultureInfo.InvariantCulture),
        (long e, ulong a) => e >= 0 && (ulong)e == a,
        (string e, char a) => e.Length == 1 && e[0] == a,
        (string e, string a) => e == a,
        (double e, double a) => e.Equals(a),
        (double e, float a) => e.Equals((double)a),
        (bool e, bool a) => e == a,
        _ => false,
    };

    private static string Show(object? value) => value switch
    {
        null => "null",
        string s => $"\"{string.Concat(s.Select(c => c < ' ' ? $"#{(int)c}" : c.ToString()))}\"",
        char c => c < ' ' ? $"#{(int)c}" : $"'{c}'",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };
}
