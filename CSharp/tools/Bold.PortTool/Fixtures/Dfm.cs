using System.Globalization;
using System.Text;

namespace Bold.PortTool.Fixtures;

/// <summary>A component in a Delphi form file (.dfm), with its published properties and child components.</summary>
public sealed class DfmObject
{
    public required string ClassName { get; init; }

    public required string Name { get; init; }

    public Dictionary<string, object?> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<DfmObject> Children { get; } = [];

    public IEnumerable<DfmObject> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var descendant in child.DescendantsAndSelf())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>A string-list property (such as TBoldModel.Model) as lines, or null.</summary>
    public List<string>? StringList(string property) =>
        Properties.GetValueOrDefault(property) is List<object?> items && items.All(i => i is string)
            ? items.Cast<string>().ToList()
            : null;
}

/// <summary>
/// Reads Delphi form files in both formats: text ("object Form1: TForm1 ... end") and binary (TPF0, with or without the
/// Windows resource header that Delphi writes in front of it). Strings keep their single-byte code page values.
/// </summary>
public static class DfmReader
{
    public static DfmObject Read(byte[] bytes)
    {
        int start = FindBinaryStart(bytes);
        return start >= 0 ? new BinaryFormReader(bytes, start).ReadRoot() : new TextFormReader(Encoding.Latin1.GetString(bytes)).ReadRoot();
    }

    /// <summary>The offset of the "TPF0" signature in a binary form file, or -1 for a text form file.</summary>
    public static int FindBinaryStart(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == (byte)'T' && bytes[1] == (byte)'P' && bytes[2] == (byte)'F' && bytes[3] == (byte)'0')
        {
            return 0;
        }

        // Resource header: FF 0A 00, a zero-terminated name, two flag bytes, a 4-byte size, then TPF0.
        if (bytes.Length > 16 && bytes[0] == 0xFF && bytes[1] == 0x0A && bytes[2] == 0x00)
        {
            for (int k = 3; k + 4 <= bytes.Length && k < 300; k++)
            {
                if (bytes[k] == (byte)'T' && bytes[k + 1] == (byte)'P' && bytes[k + 2] == (byte)'F' && bytes[k + 3] == (byte)'0')
                {
                    return k;
                }
            }
        }

        return -1;
    }

    private sealed class BinaryFormReader(byte[] bytes, int start)
    {
        private int _position = start + 4;

        private byte Byte() => _position < bytes.Length ? bytes[_position++] : throw new InvalidDataException("unexpected end of binary form");

        private int Int32()
        {
            int value = BitConverter.ToInt32(bytes, _position);
            _position += 4;
            return value;
        }

        private string ShortString()
        {
            int length = Byte();
            string value = Encoding.Latin1.GetString(bytes, _position, length);
            _position += length;
            return value;
        }

        private string LongString(Encoding encoding, int unit)
        {
            int length = Int32() * unit;
            string value = encoding.GetString(bytes, _position, length);
            _position += length;
            return value;
        }

        public DfmObject ReadRoot() => ReadComponent();

        private DfmObject ReadComponent()
        {
            byte prefix = bytes[_position];
            if ((prefix & 0xF0) == 0xF0)
            {
                _position++;
                if ((prefix & 0x02) != 0)
                {
                    ReadValue(); // child position
                }
            }

            var component = new DfmObject { ClassName = ShortString(), Name = ShortString() };
            while (bytes[_position] != 0)
            {
                string name = ShortString();
                component.Properties[name] = ReadValue();
            }

            _position++;
            while (bytes[_position] != 0)
            {
                component.Children.Add(ReadComponent());
            }

            _position++;
            return component;
        }

        private object? ReadValue()
        {
            byte type = Byte();
            switch (type)
            {
                case 0: return null; // vaNull
                case 1: // vaList
                {
                    var list = new List<object?>();
                    while (bytes[_position] != 0)
                    {
                        list.Add(ReadValue());
                    }

                    _position++;
                    return list;
                }

                case 2: return (int)(sbyte)Byte();
                case 3:
                {
                    short value = BitConverter.ToInt16(bytes, _position);
                    _position += 2;
                    return (int)value;
                }

                case 4: return Int32();
                case 5: _position += 10; return 0.0; // vaExtended
                case 6: return ShortString(); // vaString
                case 7: return new DfmIdentifier(ShortString()); // vaIdent
                case 8: return false;
                case 9: return true;
                case 10: // vaBinary
                {
                    int length = Int32();
                    var data = bytes[_position..(_position + length)];
                    _position += length;
                    return data;
                }

                case 11: // vaSet
                {
                    var set = new List<string>();
                    string item;
                    while ((item = ShortString()).Length > 0)
                    {
                        set.Add(item);
                    }

                    return set;
                }

                case 12: return LongString(Encoding.Latin1, 1); // vaLString
                case 13: return null; // vaNil
                case 14: // vaCollection
                {
                    var items = new List<object?>();
                    while (bytes[_position] != 0)
                    {
                        if (bytes[_position] is 2 or 3 or 4)
                        {
                            ReadValue(); // item index
                        }

                        if (Byte() != 1)
                        {
                            throw new InvalidDataException("collection item without a property list");
                        }

                        var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        while (bytes[_position] != 0)
                        {
                            string name = ShortString();
                            item[name] = ReadValue();
                        }

                        _position++;
                        items.Add(item);
                    }

                    _position++;
                    return items;
                }

                case 15: _position += 4; return 0.0; // vaSingle
                case 16: _position += 8; return 0.0; // vaCurrency
                case 17: _position += 8; return 0.0; // vaDate
                case 18: return LongString(Encoding.Unicode, 2); // vaWString
                case 19: _position += 8; return 0L; // vaInt64
                case 20: return LongString(Encoding.UTF8, 1); // vaUTF8String
                case 21: _position += 8; return 0.0; // vaDouble
                default:
                    throw new InvalidDataException($"unknown value type {type} at offset {_position - 1}");
            }
        }
    }

    private sealed class TextFormReader(string text)
    {
        private int _position;
        private int _line = 1;

        public DfmObject ReadRoot()
        {
            SkipSpace();
            return ReadObject();
        }

        private DfmObject ReadObject()
        {
            string keyword = Word(); // object, inherited or inline
            if (keyword is not ("object" or "inherited" or "inline"))
            {
                throw Error($"expected 'object' but found '{keyword}'");
            }

            string name = "";
            string className = Word();
            SkipSpace();
            if (Peek() == ':')
            {
                _position++;
                name = className;
                className = Word();
            }

            SkipSpace();
            if (Peek() == '[')
            {
                SkipPast(']');
            }

            var component = new DfmObject { ClassName = className, Name = name };
            while (true)
            {
                SkipSpace();
                string word = PeekWord();
                if (word.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    Word();
                    return component;
                }

                if (word is "object" or "inherited" or "inline")
                {
                    component.Children.Add(ReadObject());
                    continue;
                }

                string property = Word();
                SkipSpace();
                Expect('=');
                component.Properties[property] = ReadValue();
            }
        }

        private object? ReadValue()
        {
            SkipSpace();
            char c = Peek();
            if (c is '\'' or '#')
            {
                return ReadString();
            }

            if (c == '(')
            {
                _position++;
                var list = new List<object?>();
                while (true)
                {
                    SkipSpace();
                    if (Peek() == ')')
                    {
                        _position++;
                        return list;
                    }

                    list.Add(ReadValue());
                }
            }

            if (c == '[')
            {
                _position++;
                var set = new List<string>();
                while (true)
                {
                    SkipSpace();
                    if (Peek() == ']')
                    {
                        _position++;
                        return set;
                    }

                    if (Peek() == ',')
                    {
                        _position++;
                        continue;
                    }

                    set.Add(Word());
                }
            }

            if (c == '{')
            {
                int end = text.IndexOf('}', _position);
                string hex = new(text[(_position + 1)..end].Where(Uri.IsHexDigit).ToArray());
                CountLines(_position, end);
                _position = end + 1;
                return Convert.FromHexString(hex);
            }

            if (c == '<')
            {
                _position++;
                var items = new List<object?>();
                while (true)
                {
                    SkipSpace();
                    if (Peek() == '>')
                    {
                        _position++;
                        return items;
                    }

                    string word = Word(); // item
                    if (!word.Equals("item", StringComparison.OrdinalIgnoreCase))
                    {
                        throw Error($"expected 'item' but found '{word}'");
                    }

                    var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    while (true)
                    {
                        SkipSpace();
                        if (PeekWord().Equals("end", StringComparison.OrdinalIgnoreCase))
                        {
                            Word();
                            break;
                        }

                        string property = Word();
                        SkipSpace();
                        Expect('=');
                        item[property] = ReadValue();
                    }

                    items.Add(item);
                }
            }

            if (char.IsDigit(c) || c is '-' or '+' or '$')
            {
                int start = _position;
                _position++;
                while (_position < text.Length && (char.IsLetterOrDigit(Peek()) || Peek() is '.' or '-' or '+'))
                {
                    _position++;
                }

                string number = text[start.._position];
                return long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : number;
            }

            string identifier = Word();
            return identifier.ToLowerInvariant() switch
            {
                "true" => true,
                "false" => false,
                "nil" => null,
                _ => new DfmIdentifier(identifier),
            };
        }

        /// <summary>'text', #13 codes, and pieces joined with "+" across lines.</summary>
        private string ReadString()
        {
            var value = new StringBuilder();
            while (true)
            {
                char c = Peek();
                if (c == '\'')
                {
                    _position++;
                    while (true)
                    {
                        if (_position >= text.Length)
                        {
                            throw Error("unterminated string");
                        }

                        char d = text[_position];
                        if (d == '\'')
                        {
                            if (_position + 1 < text.Length && text[_position + 1] == '\'')
                            {
                                value.Append('\'');
                                _position += 2;
                                continue;
                            }

                            _position++;
                            break;
                        }

                        value.Append(d);
                        _position++;
                    }
                }
                else if (c == '#')
                {
                    _position++;
                    int start = _position;
                    while (_position < text.Length && char.IsDigit(text[_position]))
                    {
                        _position++;
                    }

                    value.Append((char)int.Parse(text.AsSpan(start, _position - start), CultureInfo.InvariantCulture));
                }
                else
                {
                    int save = _position;
                    int saveLine = _line;
                    SkipSpace();
                    if (Peek() == '+')
                    {
                        _position++;
                        SkipSpace();
                        continue;
                    }

                    _position = save;
                    _line = saveLine;
                    return value.ToString();
                }
            }
        }

        private char Peek() => _position < text.Length ? text[_position] : '\0';

        private void Expect(char c)
        {
            if (Peek() != c)
            {
                throw Error($"expected '{c}'");
            }

            _position++;
        }

        private string Word()
        {
            SkipSpace();
            int start = _position;
            while (_position < text.Length && (char.IsLetterOrDigit(text[_position]) || text[_position] is '_' or '.'))
            {
                _position++;
            }

            if (start == _position)
            {
                throw Error($"expected a name but found '{Peek()}'");
            }

            return text[start.._position];
        }

        private string PeekWord()
        {
            int save = _position;
            int saveLine = _line;
            try
            {
                return _position < text.Length && (char.IsLetter(Peek()) || Peek() == '_') ? Word() : "";
            }
            finally
            {
                _position = save;
                _line = saveLine;
            }
        }

        private void SkipSpace()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
            {
                if (text[_position] == '\n')
                {
                    _line++;
                }

                _position++;
            }
        }

        private void SkipPast(char c)
        {
            int end = text.IndexOf(c, _position);
            _position = end < 0 ? text.Length : end + 1;
        }

        private void CountLines(int from, int to)
        {
            for (int k = from; k < to; k++)
            {
                if (text[k] == '\n')
                {
                    _line++;
                }
            }
        }

        private InvalidDataException Error(string message) => new($"line {_line}: {message}");
    }
}

/// <summary>An identifier value in a form file, such as an enum value or a component reference.</summary>
public sealed record DfmIdentifier(string Name);
