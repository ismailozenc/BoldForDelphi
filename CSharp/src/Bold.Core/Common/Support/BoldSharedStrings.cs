// Delphi: Source/Common/Support/BoldSharedStrings.pas
// Port map: CSharp/port/units/BoldSharedStrings.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "initialization", Note = "The unit's initialization section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "finalization", Note = "Frees the global manager; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSharedStrings", Symbol = "StringRefCount", Note = ".NET strings have no reference count; a holder keeps its string by a weak reference, which tells when no one uses it, and counts how often it was handed out.")]

namespace Bold;

/// <summary>
/// One shared string. Delphi read the string's reference count to see how many places still used it. .NET strings have
/// no reference count, so the holder keeps the instance it hands out by a weak reference, which tells when no one uses
/// it any more, and counts how many times it was handed out since; it keeps a copy of its own as the cache's key.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSharedStringHolder : TBoldMemoryManagedObject
{
    private readonly string fKey;
    private readonly WeakReference<string> fShared;
    private int fHandedOut;

    [Delphi(PortStatus.Ported, Note = "Keeps the string by a weak reference and a copy of it as the key.")]
    public TBoldSharedStringHolder(string s)
    {
        fKey = new string(s.AsSpan()); // a copy: the instance handed out must be free to go when no one uses it
        fShared = new WeakReference<string>(s);
    }

    /// <summary>The value, which the cache's index is keyed by.</summary>
    internal string Key => fKey;

    /// <summary>
    /// The instance to use for the value: the one handed out before while someone still uses it, else the candidate,
    /// which is shared from now on.
    /// </summary>
    internal string HandOut(string Candidate)
    {
        if (!fShared.TryGetTarget(out string? shared))
        {
            fShared.SetTarget(Candidate);
            shared = Candidate;
            fHandedOut = 0;
        }

        fHandedOut++;
        return shared;
    }

    [Delphi(PortStatus.Ported, Note = "0 once no one uses the string (the weak reference tells); otherwise how many times it was handed out since it was last unused, as .NET strings have no reference count to read.")]
    private int GetExternalRefCount() => fShared.TryGetTarget(out _) ? Math.Max(fHandedOut, 1) : 0;

    [Delphi(PortStatus.Ported, Note = "0 once no one uses the string; otherwise how many times it was handed out since it was last unused.")]
    public int ExternalRefCount => GetExternalRefCount();

    /// <summary>The shared instance while someone uses it, else the holder's copy.</summary>
    [Delphi(PortStatus.Ported)]
    public string Value => fShared.TryGetTarget(out string? shared) ? shared : fKey;
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
        protected override string ItemAsKeyString(object? Item) => ((TBoldSharedStringHolder)Item!).Key;
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
                result = holder.HandOut(s);
                fCachedHits++;
            }
            else
            {
                holder = new TBoldSharedStringHolder(s);
                fStringCache.Add(holder);
                result = holder.HandOut(s);
            }
        }

        fAddsRemainingToGarbageCollect--;
        if (fAddsRemainingToGarbageCollect <= 0)
        {
            GarbageCollect(true);
        }

        return result;
    }

    /// <summary>
    /// Drops the strings no one uses any more, and, unless <paramref name="KeepStringsWithOneReference"/>, those used
    /// in one place only (sharing them saves nothing).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Uses the holders' counts, which tell an unused string by a weak reference.")]
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

    /// <summary>What sharing saves, in characters: each string once for every further use, less the strings no one uses.</summary>
    [Delphi(PortStatus.Ported, Note = "In characters, from the holders' counts.")]
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

    [Delphi(PortStatus.Ported, Note = "In characters, from the holders' counts.")]
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
