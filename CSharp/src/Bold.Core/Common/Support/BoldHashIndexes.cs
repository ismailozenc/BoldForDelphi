// Delphi: Source/Common/Support/BoldHashIndexes.pas
// Port map: CSharp/port/units/BoldHashIndexes.md

using System.Runtime.CompilerServices;

namespace Bold;

/// <summary>How string keys compare: exactly, ASCII case-insensitively, or case-insensitively for every letter.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldStringCompareMode { bscCaseDependent, bscCaseIndependent, bscLocaleCaseIndependent }

/// <summary>Bold's string hash function.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldStringKey
{
    /// <summary>The hash of <paramref name="KeyString"/>; strings equal under <paramref name="CompareMode"/> hash equally.</summary>
    [Delphi(PortStatus.Ported)]
    public static uint HashString(string KeyString, TBoldStringCompareMode CompareMode) =>
        HashBuffer(KeyString ?? "", (KeyString ?? "").Length, CompareMode);

    /// <summary>The hash of the first <paramref name="length"/> characters of <paramref name="P"/>.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes a string where Delphi takes PChar.")]
    public static uint HashBuffer(string P, int length, TBoldStringCompareMode CompareMode)
    {
        uint result = 0;
        switch (CompareMode)
        {
            case TBoldStringCompareMode.bscCaseDependent:
                for (int i = 0; i < length; i++)
                {
                    result = ((result << 3) & 2147483647) | ((result >> (32 - 3)) ^ P[i]);
                }

                break;
            case TBoldStringCompareMode.bscCaseIndependent:
                for (int i = 0; i < length; i++)
                {
                    result = ((result << 3) & 2147483647) | ((result >> (32 - 3)) ^ (uint)(P[i] | 32));
                }

                break;
            default:
                result = HashBuffer(P.Substring(0, length).ToUpperInvariant(), length, TBoldStringCompareMode.bscCaseDependent);
                break;
        }

        return result;
    }
}

/// <summary>A hash index keyed by a string, ignoring case.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldStringHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected abstract string ItemAsKeyString(object? Item);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) =>
        TBoldStringKey.HashString(ItemAsKeyString(Item), TBoldStringCompareMode.bscLocaleCaseIndependent);

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => BoldUtils.BoldAnsiEqual(ItemAsKeyString(Item), (string)Key);

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) =>
        TBoldStringKey.HashString((string)Key, TBoldStringCompareMode.bscLocaleCaseIndependent);

    [Delphi(PortStatus.Ported)]
    public object? FindByString(string KeyString) => Find(KeyString);

    [Delphi(PortStatus.Ported)]
    public void FindAllByString(string KeyString, TList List) => FindAll(KeyString, List);
}

/// <summary>A hash index keyed by a string, case-sensitive.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldCaseSensitiveStringHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected abstract string ItemAsKeyString(object? Item);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) =>
        TBoldStringKey.HashString(ItemAsKeyString(Item), TBoldStringCompareMode.bscCaseDependent);

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ItemAsKeyString(Item) == (string)Key;

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => TBoldStringKey.HashString((string)Key, TBoldStringCompareMode.bscCaseDependent);

    [Delphi(PortStatus.Ported)]
    public object? FindByString(string KeyString) => Find(KeyString);
}

/// <summary>A hash index keyed by object identity.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldObjectHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected abstract object? ItemAsKeyObject(object? Item);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => HashObject(ItemAsKeyObject(Item));

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ReferenceEquals(Key, ItemAsKeyObject(Item));

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => HashObject(Key);

    /// <summary>The identity hash of the object; Delphi uses its address.</summary>
    [Delphi(PortStatus.Ported, Note = "RuntimeHelpers.GetHashCode where Delphi uses the object's address.")]
    protected static uint HashObject(object? KeyObject) => KeyObject is null ? 0 : unchecked((uint)RuntimeHelpers.GetHashCode(KeyObject));

    [Delphi(PortStatus.Ported)]
    protected internal object? FindByObject(object KeyObject) => Find(KeyObject);

    [Delphi(PortStatus.Ported)]
    protected internal void FindAllByObject(object KeyObject, TList List) => FindAll(KeyObject, List);
}

/// <summary>A hash index keyed by a class.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldClassHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected abstract TClass ItemAsKeyClass(object? O);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => HashClass(ItemAsKeyClass(Item));

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => (TClass)Key == ItemAsKeyClass(Item);

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => HashClass((TClass)Key);

    [Delphi(PortStatus.Ported, Note = "RuntimeHelpers.GetHashCode where Delphi uses the class's address.")]
    protected static uint HashClass(TClass KeyClass) => unchecked((uint)RuntimeHelpers.GetHashCode(KeyClass));

    [Delphi(PortStatus.Ported)]
    protected internal object? FindByClass(TClass KeyClass) => Find(KeyClass);
}

/// <summary>A hash index keyed by a GUID.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldGUIDHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    private static bool GuidEqual(TGUID Guid1, TGUID Guid2) => Guid1 == Guid2;

    [Delphi(PortStatus.Ported)]
    protected abstract TGUID ItemASKeyGUID(object? O);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => HashGUID(ItemASKeyGUID(Item));

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => GuidEqual((TGUID)Key, ItemASKeyGUID(Item));

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => HashGUID((TGUID)Key);

    /// <summary>D1 xor D2 xor (D3 shl 8) xor each byte of D4, as Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    protected uint HashGUID(TGUID KeyGUID)
    {
        Span<byte> b = stackalloc byte[16];
        KeyGUID.TryWriteBytes(b);
        uint d1 = BitConverter.ToUInt32(b[..4]);
        uint d2 = BitConverter.ToUInt16(b[4..6]);
        uint d3 = BitConverter.ToUInt16(b[6..8]);
        uint result = d1 ^ d2 ^ (d3 << 8);
        for (int i = 8; i < 16; i++)
        {
            result ^= b[i];
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal object? FindByGUID(TGUID KeyGUID) => Find(KeyGUID);
}

/// <summary>A hash index keyed by an unsigned integer, which is its own hash.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldCardinalHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected abstract uint ItemAsKeyCardinal(object? Item);

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => ItemAsKeyCardinal(Item);

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => Convert.ToUInt32(Key) == ItemAsKeyCardinal(Item);

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => Convert.ToUInt32(Key);

    [Delphi(PortStatus.Ported)]
    protected internal object? FindByCardinal(uint KeyCardinal) => Find(KeyCardinal);
}
