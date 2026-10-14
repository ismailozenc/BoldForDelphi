using System.Globalization;

namespace Bold.PortTool.Delphi;

/// <summary>
/// Evaluates Delphi constant expressions, so tests can compare a ported constant with the value in the Delphi source
/// instead of with a value someone typed again. Integers come back as long, reals as double, strings and characters as
/// string, Booleans as bool. Anything it cannot evaluate (sets, arrays, records, other units' constants) gives null.
/// </summary>
public static class DelphiConstants
{
    private static readonly Dictionary<string, (long Low, long High)> OrdinalTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Integer"] = (int.MinValue, int.MaxValue),
        ["Longint"] = (int.MinValue, int.MaxValue),
        ["Cardinal"] = (0, uint.MaxValue),
        ["LongWord"] = (0, uint.MaxValue),
        ["Word"] = (0, ushort.MaxValue),
        ["SmallInt"] = (short.MinValue, short.MaxValue),
        ["Byte"] = (0, byte.MaxValue),
        ["ShortInt"] = (sbyte.MinValue, sbyte.MaxValue),
        ["Int64"] = (long.MinValue, long.MaxValue),
        ["Char"] = (0, 255),
        ["AnsiChar"] = (0, 255),
        ["Boolean"] = (0, 1),
    };

    /// <summary>
    /// The values of the unit's interface constants and resource strings, by name (case-insensitive).
    /// <paramref name="external"/> resolves identifiers the unit does not declare, such as constants of used units.
    /// </summary>
    public static Dictionary<string, object?> Evaluate(DelphiUnit unit, Func<string, object?>? external = null)
    {
        var symbols = unit.Symbols
            .Where(s => s.Kind is SymbolKind.Const or SymbolKind.ResourceString)
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var aliases = unit.Symbols
            .Where(s => s.Kind == SymbolKind.Alias && s.TypeText is not null)
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().TypeText!, StringComparer.OrdinalIgnoreCase);
        var enums = unit.Symbols
            .Where(s => s.EnumMembers is not null)
            .SelectMany(s => s.EnumMembers!)
            .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var busy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        object? Lookup(string name)
        {
            if (values.TryGetValue(name, out object? known))
            {
                return known;
            }

            if (symbols.TryGetValue(name, out var symbol) && symbol.ValueTokens is not null && busy.Add(name))
            {
                object? value = new Evaluator(symbol.ValueTokens, Lookup, aliases).Run();
                values[name] = value;
                busy.Remove(name);
                return value;
            }

            if (enums.TryGetValue(name, out long ordinal))
            {
                return ordinal;
            }

            return symbols.ContainsKey(name) ? null : external?.Invoke(name);
        }

        foreach (string name in symbols.Keys)
        {
            Lookup(name);
        }

        return symbols.Keys.ToDictionary(n => symbols[n].Name, n => values.GetValueOrDefault(n), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Evaluates one expression; <paramref name="lookup"/> resolves identifiers.</summary>
    public static object? EvaluateTokens(List<Token> tokens, Func<string, object?> lookup) =>
        new Evaluator(tokens, lookup, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).Run();

    private sealed class Evaluator(List<Token> tokens, Func<string, object?> lookup, Dictionary<string, string> aliases)
    {
        private int _position;

        private Token? Peek => _position < tokens.Count ? tokens[_position] : null;

        public object? Run()
        {
            try
            {
                object? value = Expression();
                return _position == tokens.Count ? value : null;
            }
            catch (Exception e) when (e is InvalidOperationException or OverflowException or FormatException or DivideByZeroException or InvalidCastException)
            {
                return null;
            }
        }

        private bool Accept(string text)
        {
            if (Peek?.Is(text) == true)
            {
                _position++;
                return true;
            }

            return false;
        }

        private object? Expression()
        {
            object? left = Simple();
            while (Peek is { } t && (t.Is("=") || t.Is("<>") || t.Is("<") || t.Is(">") || t.Is("<=") || t.Is(">=")))
            {
                _position++;
                object? right = Simple();
                int compare = Compare(left, right);
                left = t.Text switch
                {
                    "=" => compare == 0,
                    "<>" => compare != 0,
                    "<" => compare < 0,
                    ">" => compare > 0,
                    "<=" => compare <= 0,
                    _ => compare >= 0,
                };
            }

            return left;
        }

        private object? Simple()
        {
            object? value;
            if (Accept("-"))
            {
                value = Negate(Term());
            }
            else
            {
                Accept("+");
                value = Term();
            }

            while (Peek is { } t && (t.Is("+") || t.Is("-") || t.Is("or") || t.Is("xor")))
            {
                _position++;
                object? right = Term();
                value = t.Text.ToLowerInvariant() switch
                {
                    "+" => Add(value, right),
                    "-" => Arithmetic(value, right, (a, b) => a - b, (a, b) => a - b),
                    "or" => value is bool a && right is bool b ? a || b : Integer(value) | Integer(right),
                    _ => value is bool c && right is bool d ? c ^ d : Integer(value) ^ Integer(right),
                };
            }

            return value;
        }

        private object? Term()
        {
            object? value = Factor();
            while (Peek is { } t && (t.Is("*") || t.Is("/") || t.Is("div") || t.Is("mod") || t.Is("and") || t.Is("shl") || t.Is("shr")))
            {
                _position++;
                object? right = Factor();
                value = t.Text.ToLowerInvariant() switch
                {
                    "*" => Arithmetic(value, right, (a, b) => checked(a * b), (a, b) => a * b),
                    "/" => Real(value) / Real(right),
                    "div" => Integer(value) / Integer(right),
                    "mod" => Integer(value) % Integer(right),
                    "and" => value is bool a && right is bool b ? a && b : Integer(value) & Integer(right),
                    "shl" => Integer(value) << (int)Integer(right),
                    _ => Integer(value) >> (int)Integer(right),
                };
            }

            return value;
        }

        private object? Factor()
        {
            var token = Peek ?? throw new InvalidOperationException("expression ends early");
            _position++;
            switch (token.Kind)
            {
                case TokenKind.String:
                    return token.Text;
                case TokenKind.Number:
                    return Number(token.Text);
                case TokenKind.Symbol when token.Is("("):
                {
                    object? inner = Expression();
                    if (!Accept(")"))
                    {
                        throw new InvalidOperationException("missing )");
                    }

                    return inner;
                }

                case TokenKind.Identifier when token.Is("not"):
                {
                    object? operand = Factor();
                    return operand is bool b ? !b : ~Integer(operand);
                }

                case TokenKind.Identifier:
                    return Identifier(token.Text);
                default:
                    throw new InvalidOperationException($"cannot evaluate '{token.Text}'");
            }
        }

        private object? Identifier(string name)
        {
            if (Accept("("))
            {
                var arguments = new List<Token>();
                int depth = 0;
                while (Peek is { } t && !(depth == 0 && t.Is(")")))
                {
                    depth += t.Is("(") ? 1 : t.Is(")") ? -1 : 0;
                    arguments.Add(t);
                    _position++;
                }

                Accept(")");
                return Call(name, arguments);
            }

            switch (name.ToLowerInvariant())
            {
                case "true": return true;
                case "false": return false;
                case "maxint":
                case "maxlongint": return (long)int.MaxValue;
                case "nil": return null;
            }

            return lookup(name) ?? throw new InvalidOperationException($"unknown name {name}");
        }

        private object? Call(string function, List<Token> arguments)
        {
            string lower = function.ToLowerInvariant();
            if (lower is "high" or "low" && arguments.Count == 1 && arguments[0].IsIdentifier)
            {
                string type = arguments[0].Text;
                for (int k = 0; k < 8 && aliases.TryGetValue(type, out string? target); k++)
                {
                    type = target;
                }

                if (OrdinalTypes.TryGetValue(type, out var range))
                {
                    return lower == "high" ? range.High : range.Low;
                }

                throw new InvalidOperationException($"{function}({arguments[0].Text}) is not known");
            }

            object? value = new Evaluator(arguments, lookup, aliases).Run();
            return lower switch
            {
                "ord" => value is string { Length: 1 } c ? (long)c[0] : value is bool b ? (b ? 1L : 0L) : Integer(value),
                "chr" => ((char)Integer(value)).ToString(),
                "length" => (long)((value as string) ?? throw new InvalidOperationException("length needs a string")).Length,
                "succ" => Integer(value) + 1,
                "pred" => Integer(value) - 1,
                "abs" => value is double d ? (object)Math.Abs(d) : Math.Abs(Integer(value)),
                "integer" or "longint" or "cardinal" or "word" or "byte" or "smallint" or "shortint" or "int64" => Integer(value),
                "char" or "ansichar" => ((char)Integer(value)).ToString(),
                _ => throw new InvalidOperationException($"function {function} is not supported"),
            };
        }

        private static object Number(string text)
        {
            if (text.StartsWith('$'))
            {
                return long.Parse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
            {
                return integer;
            }

            return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static object? Add(object? left, object? right) =>
            left is string || right is string ? $"{AsText(left)}{AsText(right)}" : Arithmetic(left, right, (a, b) => checked(a + b), (a, b) => a + b);

        private static string AsText(object? value) => value as string ?? throw new InvalidOperationException("cannot add a number to a string");

        // Each branch is cast to object: "cond ? double : long" would otherwise turn every integer into a double.
        private static object Arithmetic(object? left, object? right, Func<long, long, long> integer, Func<double, double, double> real) =>
            left is double || right is double ? (object)real(Real(left), Real(right)) : integer(Integer(left), Integer(right));

        private static object Negate(object? value) => value is double d ? (object)(-d) : -Integer(value);

        private static long Integer(object? value) => value switch
        {
            long l => l,
            bool b => b ? 1 : 0,
            _ => throw new InvalidOperationException("an integer is needed"),
        };

        private static double Real(object? value) => value switch
        {
            long l => l,
            double d => d,
            _ => throw new InvalidOperationException("a number is needed"),
        };

        private static int Compare(object? left, object? right) => left switch
        {
            string a when right is string b => string.CompareOrdinal(a, b),
            double or long when right is double or long => Real(left).CompareTo(Real(right)),
            bool a when right is bool b => a.CompareTo(b),
            _ => throw new InvalidOperationException("cannot compare"),
        };
    }
}
