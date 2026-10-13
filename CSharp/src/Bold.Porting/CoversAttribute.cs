using System;

namespace Bold.Porting
{
    /// <summary>
    /// Marks a test (or every test in a test class) as checking the behaviour of ported elements.
    /// </summary>
    /// <remarks>
    /// The port tool reads these markers and the test results. A <see cref="PortStatus.Ported"/> element
    /// shows as verified only when a passing test covers it. Prefer the <see cref="Type"/> forms: they are
    /// checked by the compiler, for example
    /// <c>[Covers(typeof(BoldDefs), nameof(BoldDefs.NO_CLASS), nameof(BoldDefs.UNASSIGNEDID))]</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class CoversAttribute : Attribute
    {
        /// <summary>Covers a ported type itself.</summary>
        public CoversAttribute(Type type)
        {
            Type = type;
            Members = Array.Empty<string>();
        }

        /// <summary>Covers members of a ported type; pass the names with <c>nameof</c>.</summary>
        public CoversAttribute(Type type, params string[] members)
        {
            Type = type;
            Members = members;
        }

        /// <summary>Covers Delphi symbols by name, for elements with no C# declaration (such as type aliases).</summary>
        public CoversAttribute(string unit, params string[] symbols)
        {
            Unit = unit;
            Members = symbols;
        }

        public Type? Type { get; }

        public string? Unit { get; }

        public string[] Members { get; }
    }
}
