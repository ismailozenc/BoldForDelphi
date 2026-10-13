using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bold;

/// <summary>
/// A Delphi set (<c>set of T</c>): a value type that holds ordinal values from 0 to 255.
/// </summary>
/// <remarks>
/// <para>
/// Delphi set operations map to operators: <c>+</c> is union, <c>-</c> is difference, <c>*</c> is
/// intersection, <c>=</c> and <c>&lt;&gt;</c> are <c>==</c> and <c>!=</c>, <c>&lt;=</c> is subset and
/// <c>&gt;=</c> is superset. <c>x in S</c> is <see cref="Contains"/>. <c>Include(S, x)</c> is
/// <c>S += x</c> and <c>Exclude(S, x)</c> is <c>S -= x</c>. A set literal <c>[a, b]</c> is
/// <see cref="Of(T[])"/> and <c>[a..b]</c> is <see cref="Range"/>.
/// </para>
/// <para>
/// <typeparamref name="T"/> must be an enum, <see cref="bool"/>, <see cref="char"/> or an integer type,
/// as in Delphi, and every element must have an ordinal value from 0 to 255.
/// A Delphi set type such as <c>TBoldValuePersistenceStateSet</c> is ported as a global using alias
/// of <c>DelphiSet&lt;TBoldValuePersistenceState&gt;</c>, so it keeps its Delphi name.
/// </para>
/// </remarks>
public readonly struct DelphiSet<T> : IEquatable<DelphiSet<T>>, IEnumerable<T>
    where T : unmanaged
{
    private static readonly TypeCode s_code = GetOrdinalTypeCode();

    private readonly ulong _bits0;
    private readonly ulong _bits1;
    private readonly ulong _bits2;
    private readonly ulong _bits3;

    private DelphiSet(ulong bits0, ulong bits1, ulong bits2, ulong bits3)
    {
        _bits0 = bits0;
        _bits1 = bits1;
        _bits2 = bits2;
        _bits3 = bits3;
    }

    /// <summary>The empty set, <c>[]</c>.</summary>
    public static DelphiSet<T> Empty => default;

    /// <summary>A set literal, <c>[a, b, c]</c>.</summary>
    public static DelphiSet<T> Of(params T[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var result = default(DelphiSet<T>);
        foreach (var element in elements)
        {
            result = result.With(Ord(element), true);
        }

        return result;
    }

    /// <summary>A range literal, <c>[low..high]</c>. It is empty when <paramref name="high"/> is below <paramref name="low"/>.</summary>
    public static DelphiSet<T> Range(T low, T high)
    {
        var result = default(DelphiSet<T>);
        int last = Ord(high);
        for (int ordinal = Ord(low); ordinal <= last; ordinal++)
        {
            result = result.With(ordinal, true);
        }

        return result;
    }

    /// <summary>True when the set has no elements.</summary>
    public bool IsEmpty => (_bits0 | _bits1 | _bits2 | _bits3) == 0;

    /// <summary>The number of elements.</summary>
    public int Count =>
        System.Numerics.BitOperations.PopCount(_bits0) + System.Numerics.BitOperations.PopCount(_bits1) +
        System.Numerics.BitOperations.PopCount(_bits2) + System.Numerics.BitOperations.PopCount(_bits3);

    /// <summary>Delphi <c>element in set</c>.</summary>
    public bool Contains(T element) => Has(Ord(element));

    /// <summary>The set with <paramref name="element"/> added; Delphi <c>Include</c>.</summary>
    public DelphiSet<T> Include(T element) => With(Ord(element), true);

    /// <summary>The set with <paramref name="element"/> removed; Delphi <c>Exclude</c>.</summary>
    public DelphiSet<T> Exclude(T element) => With(Ord(element), false);

    public static DelphiSet<T> operator +(DelphiSet<T> left, DelphiSet<T> right) =>
        new(left._bits0 | right._bits0, left._bits1 | right._bits1, left._bits2 | right._bits2, left._bits3 | right._bits3);

    public static DelphiSet<T> operator -(DelphiSet<T> left, DelphiSet<T> right) =>
        new(left._bits0 & ~right._bits0, left._bits1 & ~right._bits1, left._bits2 & ~right._bits2, left._bits3 & ~right._bits3);

    public static DelphiSet<T> operator *(DelphiSet<T> left, DelphiSet<T> right) =>
        new(left._bits0 & right._bits0, left._bits1 & right._bits1, left._bits2 & right._bits2, left._bits3 & right._bits3);

    public static DelphiSet<T> operator +(DelphiSet<T> set, T element) => set.Include(element);

    public static DelphiSet<T> operator -(DelphiSet<T> set, T element) => set.Exclude(element);

    public static bool operator ==(DelphiSet<T> left, DelphiSet<T> right) => left.Equals(right);

    public static bool operator !=(DelphiSet<T> left, DelphiSet<T> right) => !left.Equals(right);

    /// <summary>Delphi subset test: every element of <paramref name="left"/> is in <paramref name="right"/>.</summary>
    public static bool operator <=(DelphiSet<T> left, DelphiSet<T> right) => (left - right).IsEmpty;

    /// <summary>Delphi superset test: every element of <paramref name="right"/> is in <paramref name="left"/>.</summary>
    public static bool operator >=(DelphiSet<T> left, DelphiSet<T> right) => (right - left).IsEmpty;

    public bool Equals(DelphiSet<T> other) =>
        _bits0 == other._bits0 && _bits1 == other._bits1 && _bits2 == other._bits2 && _bits3 == other._bits3;

    public override bool Equals(object? obj) => obj is DelphiSet<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_bits0, _bits1, _bits2, _bits3);

    /// <summary>The elements in ordinal order, as Delphi enumerates a set.</summary>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The set in Delphi literal form, for example <c>[bvpsCurrent, bvpsModified]</c>.</summary>
    public override string ToString()
    {
        var text = new StringBuilder("[");
        foreach (var element in this)
        {
            if (text.Length > 1)
            {
                text.Append(", ");
            }

            text.Append(element.ToString());
        }

        return text.Append(']').ToString();
    }

    private bool Has(int ordinal)
    {
        ulong mask = 1UL << (ordinal & 63);
        return (ordinal >> 6) switch
        {
            0 => (_bits0 & mask) != 0,
            1 => (_bits1 & mask) != 0,
            2 => (_bits2 & mask) != 0,
            _ => (_bits3 & mask) != 0,
        };
    }

    private DelphiSet<T> With(int ordinal, bool present)
    {
        ulong mask = 1UL << (ordinal & 63);
        ulong Apply(ulong bits) => present ? bits | mask : bits & ~mask;
        return (ordinal >> 6) switch
        {
            0 => new DelphiSet<T>(Apply(_bits0), _bits1, _bits2, _bits3),
            1 => new DelphiSet<T>(_bits0, Apply(_bits1), _bits2, _bits3),
            2 => new DelphiSet<T>(_bits0, _bits1, Apply(_bits2), _bits3),
            _ => new DelphiSet<T>(_bits0, _bits1, _bits2, Apply(_bits3)),
        };
    }

    private static TypeCode GetOrdinalTypeCode()
    {
        var type = typeof(T);
        var code = Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type);
        return code switch
        {
            TypeCode.Boolean or TypeCode.Char or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
            TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => code,
            _ => throw new NotSupportedException(
                $"DelphiSet<{type.Name}> is not supported: a Delphi set needs an enum, Boolean, Char or integer element type."),
        };
    }

    private static int Ord(T element)
    {
        long ordinal = s_code switch
        {
            TypeCode.Boolean => Unsafe.As<T, bool>(ref element) ? 1 : 0,
            TypeCode.Char => Unsafe.As<T, char>(ref element),
            TypeCode.SByte => Unsafe.As<T, sbyte>(ref element),
            TypeCode.Byte => Unsafe.As<T, byte>(ref element),
            TypeCode.Int16 => Unsafe.As<T, short>(ref element),
            TypeCode.UInt16 => Unsafe.As<T, ushort>(ref element),
            TypeCode.Int32 => Unsafe.As<T, int>(ref element),
            TypeCode.UInt32 => Unsafe.As<T, uint>(ref element),
            TypeCode.Int64 => Unsafe.As<T, long>(ref element),
            _ => Unsafe.As<T, ulong>(ref element) > 255 ? 256 : (long)Unsafe.As<T, ulong>(ref element),
        };
        if (ordinal is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(element), element, "A Delphi set holds ordinal values from 0 to 255.");
        }

        return (int)ordinal;
    }

    private static T FromOrd(int ordinal)
    {
        T element = default;
        switch (s_code)
        {
            case TypeCode.Boolean: Unsafe.As<T, bool>(ref element) = ordinal != 0; break;
            case TypeCode.Char: Unsafe.As<T, char>(ref element) = (char)ordinal; break;
            case TypeCode.SByte: Unsafe.As<T, sbyte>(ref element) = (sbyte)ordinal; break;
            case TypeCode.Byte: Unsafe.As<T, byte>(ref element) = (byte)ordinal; break;
            case TypeCode.Int16: Unsafe.As<T, short>(ref element) = (short)ordinal; break;
            case TypeCode.UInt16: Unsafe.As<T, ushort>(ref element) = (ushort)ordinal; break;
            case TypeCode.Int32: Unsafe.As<T, int>(ref element) = ordinal; break;
            case TypeCode.UInt32: Unsafe.As<T, uint>(ref element) = (uint)ordinal; break;
            case TypeCode.Int64: Unsafe.As<T, long>(ref element) = ordinal; break;
            default: Unsafe.As<T, ulong>(ref element) = (ulong)ordinal; break;
        }

        return element;
    }

    /// <summary>Enumerates the elements of a <see cref="DelphiSet{T}"/> in ordinal order.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly DelphiSet<T> _set;
        private int _ordinal;

        internal Enumerator(DelphiSet<T> set)
        {
            _set = set;
            _ordinal = -1;
        }

        public readonly T Current => FromOrd(_ordinal);

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_ordinal <= 255)
            {
                if (_set.Has(_ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset() => _ordinal = -1;

        public readonly void Dispose()
        {
        }
    }
}
