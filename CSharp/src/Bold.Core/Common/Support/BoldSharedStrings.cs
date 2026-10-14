// Delphi: Source/Common/Support/BoldSharedStrings.pas
// Port map: CSharp/port/units/BoldSharedStrings.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "initialization", Note = "The unit's initialization section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "finalization", Note = "Frees the global manager; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "StringRefCount", Note = ".NET strings have no reference count; holders count how often they were handed out instead.")]

namespace Bold;

/// <summary>
/// One shared string. Delphi reads the string's reference count to see how many places still use it; .NET has no
/// such count, so the holder counts how many times the manager handed the string out.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSharedStringHolder : TBoldMemoryManagedObject
{
    private readonly string fValue;
    internal int fHandedOut;

    [Delphi(PortStatus.Ported)]
    public TBoldSharedStringHolder(string s)
    {
        fValue = s;
    }

    [Delphi(PortStatus.Partial, Note = "How many times the string was handed out, not how many references are alive.")]
    private int GetExternalRefCount() => fHandedOut;

    [Delphi(PortStatus.Partial, Note = "How many times the string was handed out, not how many references are alive.")]
    public int ExternalRefCount => GetExternalRefCount();

    [Delphi(PortStatus.Ported)]
    public string Value => fValue;
}

/// <summary>The cache of shared strings, indexed by value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSharedStringCache : TBoldUnOrderedIndexableList
{
    private static int IX_StringHolderValue = -1;

    [Delphi(PortStatus.Ported, Symbol = "TBoldStringHolderValueIndex")]
    private sealed class TBoldStringHolderValueIndex : TBoldCaseSensitiveStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldStringHolderValueIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldSharedStringHolder)Item!).Value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSharedStringCache()
    {
        BoldIndexableList.SetIndexVariable(ref IX_StringHolderValue, AddIndex(new TBoldStringHolderValueIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSharedStringHolder? GetHolderByValue(string s) =>
        (TBoldSharedStringHolder?)((TBoldStringHolderValueIndex)Indexes(IX_StringHolderValue)!).FindByString(s);

    [Delphi(PortStatus.Ported)]
    public TBoldSharedStringHolder? HolderByValue(string s) => GetHolderByValue(s);
}

/// <summary>Hands out one shared instance per distinct string value, so equal strings read from a model share memory.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSharedStringManager : IDisposable
{
    private TBoldSharedStringCache? fStringCache;
    private int fCachedHits;
    private int fAddsRemainingToGarbageCollect;

    [Delphi(PortStatus.Ported)]
    public TBoldSharedStringManager()
    {
        fStringCache = new TBoldSharedStringCache();
        fAddsRemainingToGarbageCollect = 100;
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        fStringCache?.Dispose();
        fStringCache = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>The shared instance equal to <paramref name="s"/>; the first time a value is seen it becomes the shared one.</summary>
    [Delphi(PortStatus.Ported)]
    public string GetSharedString(string s)
    {
        string result;
        if (string.IsNullOrEmpty(s))
        {
            result = s;
        }
        else
        {
            TBoldSharedStringHolder? holder = fStringCache!.HolderByValue(s);
            if (holder is not null)
            {
                result = holder.Value;
                fCachedHits++;
            }
            else
            {
                holder = new TBoldSharedStringHolder(s);
                fStringCache.Add(holder);
                result = s;
            }

            holder.fHandedOut++;
        }

        fAddsRemainingToGarbageCollect--;
        if (fAddsRemainingToGarbageCollect <= 0)
        {
            GarbageCollect(true);
        }

        return result;
    }

    /// <summary>Drops holders handed out fewer than one time (or two, when <paramref name="KeepStringsWithOneReference"/> is false).</summary>
    [Delphi(PortStatus.Partial, Note = "Decides by hand-out count, as reference counts are not available in .NET.")]
    public void GarbageCollect(bool KeepStringsWithOneReference)
    {
        int minimumReferencesToKeep = KeepStringsWithOneReference ? 1 : 2;
        var toRemove = new List<TBoldSharedStringHolder>();
        using (TBoldIndexableListTraverser traverser = fStringCache!.CreateTraverser()!)
        {
            while (!traverser.EndOfList)
            {
                var holder = (TBoldSharedStringHolder)traverser.Item!;
                if (holder.ExternalRefCount < minimumReferencesToKeep)
                {
                    toRemove.Add(holder);
                }

                traverser.Next();
            }
        }

        foreach (TBoldSharedStringHolder holder in toRemove)
        {
            fStringCache.Remove(holder);
        }

        fAddsRemainingToGarbageCollect = fStringCache.Count;
    }

    [Delphi(PortStatus.Partial, Note = "Estimated from hand-out counts.")]
    private int GetSavedMemory()
    {
        int result = 0;
        using TBoldIndexableListTraverser traverser = fStringCache!.CreateTraverser()!;
        while (!traverser.EndOfList)
        {
            var holder = (TBoldSharedStringHolder)traverser.Item!;
            result += (holder.ExternalRefCount - 1) * holder.Value.Length;
            traverser.Next();
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private string GetInfoString() =>
        SysUtils.Format("Number of shared strings: %d", fStringCache!.Count) + BoldDefs.BOLDCRLF +
        SysUtils.Format("Saved memory %d", SavedMemory);

    [Delphi(PortStatus.Partial, Note = "Estimated from hand-out counts.")]
    public int SavedMemory => GetSavedMemory();

    [Delphi(PortStatus.Ported)]
    public string InfoString => GetInfoString();

    /// <summary>How many requests the cache answered with an existing instance.</summary>
    public int CachedHits => fCachedHits;
}

/// <summary>Unit-level routines of BoldSharedStrings.</summary>
public static partial class BoldSharedStrings
{
    private static TBoldSharedStringManager? G_BoldSharedStringManager;

    /// <summary>The process-wide shared string manager, created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldSharedStringManager BoldSharedStringManager() =>
        G_BoldSharedStringManager ??= new TBoldSharedStringManager();
}
