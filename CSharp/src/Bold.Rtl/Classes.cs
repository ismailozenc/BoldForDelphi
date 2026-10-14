using System.Collections;
using System.Text;

namespace Bold;

public enum TDuplicates { dupIgnore, dupAccept, dupError }

/// <summary>Compares two list items, as Delphi's TListSortCompare.</summary>
public delegate int TListSortCompare(object? Item1, object? Item2);

/// <summary>Delphi's TNotifyEvent: a method taking the sender.</summary>
public delegate void TNotifyEvent(object? Sender);

/// <summary>Compares two entries of a string list by index, as Delphi's TStringListSortCompare.</summary>
public delegate int TStringListSortCompare(TStringList List, int Index1, int Index2);

/// <summary>
/// Delphi's TStrings: a list of strings with an object per string, name=value access, and the Text, CommaText and
/// DelimitedText conversions with Delphi's quoting rules.
/// </summary>
public abstract class TStrings : IEnumerable<string>
{
    private int _updateCount;

    public char Delimiter { get; set; } = ',';

    public char QuoteChar { get; set; } = '"';

    public char NameValueSeparator { get; set; } = '=';

    public bool StrictDelimiter { get; set; }

    public string LineBreak { get; set; } = SysUtils.sLineBreak;

    public abstract int Count { get; }

    public abstract string Get(int index);

    public abstract void Put(int index, string value);

    public abstract object? GetObject(int index);

    public abstract void PutObject(int index, object? value);

    public abstract void Insert(int index, string s);

    public abstract void Delete(int index);

    public abstract void Clear();

    public string this[int index]
    {
        get => Get(index);
        set => Put(index, value);
    }

    public string Strings(int index) => Get(index);

    public object? Objects(int index) => GetObject(index);

    public void SetObjects(int index, object? value) => PutObject(index, value);

    public bool IsUpdating => _updateCount > 0;

    public void BeginUpdate() => _updateCount++;

    public void EndUpdate() => _updateCount = Math.Max(0, _updateCount - 1);

    public virtual int Add(string s)
    {
        int index = Count;
        Insert(index, s);
        return index;
    }

    public int AddObject(string s, object? obj)
    {
        int index = Add(s);
        PutObject(index, obj);
        return index;
    }

    public void AddStrings(TStrings strings)
    {
        BeginUpdate();
        try
        {
            for (int i = 0; i < strings.Count; i++)
            {
                AddObject(strings.Get(i), strings.GetObject(i));
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    public void AddStrings(IEnumerable<string> strings)
    {
        foreach (string s in strings)
        {
            Add(s);
        }
    }

    public virtual void Assign(TStrings source)
    {
        BeginUpdate();
        try
        {
            Clear();
            AddStrings(source);
        }
        finally
        {
            EndUpdate();
        }
    }

    /// <summary>Compares two strings as the list does (case-insensitive unless the list says otherwise).</summary>
    protected virtual int CompareStrings(string s1, string s2) => SysUtils.AnsiCompareText(s1, s2);

    public virtual int IndexOf(string s)
    {
        for (int i = 0; i < Count; i++)
        {
            if (CompareStrings(Get(i), s) == 0)
            {
                return i;
            }
        }

        return -1;
    }

    public int IndexOfObject(object? obj)
    {
        for (int i = 0; i < Count; i++)
        {
            if (ReferenceEquals(GetObject(i), obj) || Equals(GetObject(i), obj))
            {
                return i;
            }
        }

        return -1;
    }

    public void Exchange(int index1, int index2)
    {
        string s = Get(index1);
        object? o = GetObject(index1);
        Put(index1, Get(index2));
        PutObject(index1, GetObject(index2));
        Put(index2, s);
        PutObject(index2, o);
    }

    public void Move(int curIndex, int newIndex)
    {
        if (curIndex == newIndex)
        {
            return;
        }

        string s = Get(curIndex);
        object? o = GetObject(curIndex);
        Delete(curIndex);
        Insert(newIndex, s);
        PutObject(newIndex, o);
    }

    // ---- name=value ----

    public string Names(int index)
    {
        string s = Get(index);
        int p = s.IndexOf(NameValueSeparator);
        return p > 0 ? s[..p] : "";
    }

    public string ValueFromIndex(int index)
    {
        string s = Get(index);
        int p = s.IndexOf(NameValueSeparator);
        return p >= 0 ? s[(p + 1)..] : "";
    }

    public int IndexOfName(string name)
    {
        for (int i = 0; i < Count; i++)
        {
            string s = Get(i);
            int p = s.IndexOf(NameValueSeparator);
            if (p >= 0 && CompareStrings(s[..p], name) == 0)
            {
                return i;
            }
        }

        return -1;
    }

    public string Values(string name)
    {
        int i = IndexOfName(name);
        return i >= 0 ? ValueFromIndex(i) : "";
    }

    /// <summary>Sets Name=Value; an empty value removes the entry, as in Delphi.</summary>
    public void SetValues(string name, string value)
    {
        int i = IndexOfName(name);
        if (value.Length > 0)
        {
            if (i < 0)
            {
                Add(name + NameValueSeparator + value);
            }
            else
            {
                Put(i, name + NameValueSeparator + value);
            }
        }
        else if (i >= 0)
        {
            Delete(i);
        }
    }

    // ---- text forms ----

    public virtual string Text
    {
        get
        {
            var text = new StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                text.Append(Get(i)).Append(LineBreak);
            }

            return text.ToString();
        }
        set
        {
            BeginUpdate();
            try
            {
                Clear();
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                int start = 0, i = 0;
                while (i < value.Length)
                {
                    char c = value[i];
                    if (c is '\r' or '\n')
                    {
                        Add(value[start..i]);
                        if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
                        {
                            i++;
                        }

                        start = i + 1;
                    }

                    i++;
                }

                if (start < value.Length)
                {
                    Add(value[start..]);
                }
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    public string CommaText
    {
        get
        {
            char oldDelimiter = Delimiter, oldQuote = QuoteChar;
            bool oldStrict = StrictDelimiter;
            Delimiter = ',';
            QuoteChar = '"';
            StrictDelimiter = false;
            try
            {
                return DelimitedText;
            }
            finally
            {
                Delimiter = oldDelimiter;
                QuoteChar = oldQuote;
                StrictDelimiter = oldStrict;
            }
        }
        set
        {
            char oldDelimiter = Delimiter, oldQuote = QuoteChar;
            bool oldStrict = StrictDelimiter;
            Delimiter = ',';
            QuoteChar = '"';
            StrictDelimiter = false;
            try
            {
                DelimitedText = value;
            }
            finally
            {
                Delimiter = oldDelimiter;
                QuoteChar = oldQuote;
                StrictDelimiter = oldStrict;
            }
        }
    }

    /// <summary>
    /// Items joined by Delimiter. An item is quoted when it is empty-with-single-item, or holds the quote character, the
    /// delimiter or (unless StrictDelimiter) a character up to space, as Delphi does.
    /// </summary>
    public string DelimitedText
    {
        get
        {
            if (Count == 1 && Get(0).Length == 0)
            {
                return new string(QuoteChar, 2);
            }

            var text = new StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                string s = Get(i);
                bool quote = s.Any(c => c == QuoteChar || c == Delimiter || (!StrictDelimiter && c <= ' '));
                if (i > 0)
                {
                    text.Append(Delimiter);
                }

                text.Append(quote ? SysUtils.AnsiQuotedStr(s, QuoteChar) : s);
            }

            return text.ToString();
        }
        set
        {
            BeginUpdate();
            try
            {
                Clear();
                string v = value ?? "";
                int i = 0;
                void SkipBlanks()
                {
                    while (!StrictDelimiter && i < v.Length && v[i] <= ' ' && v[i] != Delimiter)
                    {
                        i++;
                    }
                }

                SkipBlanks();
                while (i < v.Length)
                {
                    string item;
                    if (v[i] == QuoteChar)
                    {
                        var sb = new StringBuilder();
                        i++;
                        while (i < v.Length)
                        {
                            if (v[i] == QuoteChar)
                            {
                                if (i + 1 < v.Length && v[i + 1] == QuoteChar)
                                {
                                    sb.Append(QuoteChar);
                                    i += 2;
                                    continue;
                                }

                                i++;
                                break;
                            }

                            sb.Append(v[i]);
                            i++;
                        }

                        item = sb.ToString();
                    }
                    else
                    {
                        int start = i;
                        while (i < v.Length && v[i] != Delimiter && (StrictDelimiter || v[i] > ' '))
                        {
                            i++;
                        }

                        item = v[start..i];
                    }

                    Add(item);
                    SkipBlanks();
                    if (i < v.Length && v[i] == Delimiter)
                    {
                        i++;
                        SkipBlanks();
                        if (i >= v.Length)
                        {
                            Add("");
                        }
                    }
                }
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    /// <summary>
    /// The encoding of LoadFromFile and SaveToFile when none is given: Windows-1252, the ANSI code page Delphi used
    /// for the files Bold wrote. A file with a byte order mark is read in the encoding the mark names.
    /// </summary>
    public static Encoding DefaultEncoding { get; set; } = CreateAnsiEncoding();

    private static Encoding CreateAnsiEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }

    public void LoadFromFile(string fileName, Encoding? encoding = null) =>
        Text = File.ReadAllText(fileName, encoding ?? DefaultEncoding);

    public void SaveToFile(string fileName, Encoding? encoding = null) =>
        File.WriteAllText(fileName, Text, encoding ?? DefaultEncoding);

    public string[] ToStringArray()
    {
        var items = new string[Count];
        for (int i = 0; i < Count; i++)
        {
            items[i] = Get(i);
        }

        return items;
    }

    public IEnumerator<string> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return Get(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => Text;
}

/// <summary>Delphi's TStringList: strings with objects, optionally sorted, case-insensitive unless CaseSensitive.</summary>
public class TStringList : TStrings
{
    private readonly List<(string Text, object? Object)> _items = [];
    private bool _sorted;

    public TStringList()
    {
    }

    public TStringList(IEnumerable<string> strings) => AddStrings(strings);

    public bool CaseSensitive { get; set; }

    public TDuplicates Duplicates { get; set; } = TDuplicates.dupIgnore;

    /// <summary>When true, the list owns its objects and disposes those that are IDisposable on removal.</summary>
    public bool OwnsObjects { get; set; }

    public bool Sorted
    {
        get => _sorted;
        set
        {
            if (value && !_sorted)
            {
                Sort();
            }

            _sorted = value;
        }
    }

    public override int Count => _items.Count;

    public int Capacity
    {
        get => _items.Capacity;
        set => _items.Capacity = Math.Max(value, _items.Count);
    }

    protected override int CompareStrings(string s1, string s2) =>
        CaseSensitive ? SysUtils.AnsiCompareStr(s1, s2) : SysUtils.AnsiCompareText(s1, s2);

    private void Check(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            throw new EListError($"List index out of bounds ({index})");
        }
    }

    public override string Get(int index)
    {
        Check(index);
        return _items[index].Text;
    }

    public override void Put(int index, string value)
    {
        if (_sorted)
        {
            throw new EListError("Operation not allowed on sorted list");
        }

        Check(index);
        _items[index] = (value, _items[index].Object);
    }

    public override object? GetObject(int index)
    {
        Check(index);
        return _items[index].Object;
    }

    public override void PutObject(int index, object? value)
    {
        Check(index);
        _items[index] = (_items[index].Text, value);
    }

    public override int Add(string s)
    {
        if (!_sorted)
        {
            _items.Add((s, null));
            return _items.Count - 1;
        }

        if (Find(s, out int index))
        {
            switch (Duplicates)
            {
                case TDuplicates.dupIgnore:
                    return index;
                case TDuplicates.dupError:
                    throw new EListError("String list does not allow duplicates");
            }
        }

        _items.Insert(index, (s, null));
        return index;
    }

    public override void Insert(int index, string s)
    {
        if (_sorted)
        {
            throw new EListError("Operation not allowed on sorted list");
        }

        if (index < 0 || index > _items.Count)
        {
            throw new EListError($"List index out of bounds ({index})");
        }

        _items.Insert(index, (s, null));
    }

    public override void Delete(int index)
    {
        Check(index);
        var obj = _items[index].Object;
        _items.RemoveAt(index);
        if (OwnsObjects && obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public override void Clear()
    {
        if (OwnsObjects)
        {
            foreach (var (_, obj) in _items)
            {
                (obj as IDisposable)?.Dispose();
            }
        }

        _items.Clear();
    }

    /// <summary>Binary search in a sorted list; returns true when found, and the index where it is or would go.</summary>
    public bool Find(string s, out int index)
    {
        int low = 0, high = _items.Count - 1;
        bool found = false;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            int c = CompareStrings(_items[middle].Text, s);
            if (c < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
                if (c == 0)
                {
                    found = true;
                    if (Duplicates != TDuplicates.dupAccept)
                    {
                        low = middle;
                    }
                }
            }
        }

        index = low;
        return found;
    }

    public override int IndexOf(string s)
    {
        if (!_sorted)
        {
            return base.IndexOf(s);
        }

        return Find(s, out int index) ? index : -1;
    }

    public virtual void Sort() => CustomSort((list, i1, i2) => list.CompareStrings(list._items[i1].Text, list._items[i2].Text));

    public void CustomSort(TStringListSortCompare compare)
    {
        if (_items.Count < 2)
        {
            return;
        }

        // The comparison reads the list by index, so sort the indexes and rebuild the list afterwards. Ties keep their order.
        var order = Enumerable.Range(0, _items.Count).ToList();
        order.Sort((a, b) =>
        {
            int r = compare(this, a, b);
            return r != 0 ? r : a.CompareTo(b);
        });
        var snapshot = _items.ToList();
        _items.Clear();
        _items.AddRange(order.Select(i => snapshot[i]));
    }
}

/// <summary>Delphi's TList: an untyped list of references with Delphi's method names.</summary>
public class TList : IEnumerable<object?>
{
    protected readonly List<object?> FList = [];

    public int Count
    {
        get => FList.Count;
        set
        {
            if (value < 0)
            {
                throw new EListError($"List count out of bounds ({value})");
            }

            if (value < FList.Count)
            {
                FList.RemoveRange(value, FList.Count - value);
            }
            else
            {
                while (FList.Count < value)
                {
                    FList.Add(null);
                }
            }
        }
    }

    public int Capacity
    {
        get => FList.Capacity;
        set => FList.Capacity = Math.Max(value, FList.Count);
    }

    public object? this[int index]
    {
        get => Get(index);
        set => Put(index, value);
    }

    public object? Items(int index) => Get(index);

    protected virtual object? Get(int index)
    {
        Check(index);
        return FList[index];
    }

    protected virtual void Put(int index, object? item)
    {
        Check(index);
        FList[index] = item;
    }

    protected void Check(int index)
    {
        if (index < 0 || index >= FList.Count)
        {
            throw new EListError($"List index out of bounds ({index})");
        }
    }

    public int Add(object? item)
    {
        FList.Add(item);
        return FList.Count - 1;
    }

    public void Insert(int index, object? item)
    {
        if (index < 0 || index > FList.Count)
        {
            throw new EListError($"List index out of bounds ({index})");
        }

        FList.Insert(index, item);
    }

    public virtual void Delete(int index)
    {
        Check(index);
        FList.RemoveAt(index);
    }

    public int Remove(object? item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            Delete(index);
        }

        return index;
    }

    public object? Extract(object? item)
    {
        int index = IndexOf(item);
        if (index < 0)
        {
            return null;
        }

        FList.RemoveAt(index);
        return item;
    }

    public int IndexOf(object? item)
    {
        for (int i = 0; i < FList.Count; i++)
        {
            if (ReferenceEquals(FList[i], item) || (item is ValueType && Equals(FList[i], item)))
            {
                return i;
            }
        }

        return -1;
    }

    public virtual void Clear() => FList.Clear();

    public void Exchange(int index1, int index2)
    {
        Check(index1);
        Check(index2);
        (FList[index1], FList[index2]) = (FList[index2], FList[index1]);
    }

    public void Move(int curIndex, int newIndex)
    {
        if (curIndex == newIndex)
        {
            return;
        }

        Check(curIndex);
        var item = FList[curIndex];
        FList.RemoveAt(curIndex);
        Insert(newIndex, item);
    }

    /// <summary>Removes nil entries.</summary>
    public void Pack() => FList.RemoveAll(i => i is null);

    public object? First() => Get(0);

    public object? Last() => Get(FList.Count - 1);

    public void Sort(TListSortCompare compare)
    {
        var copy = FList.Select((item, index) => (item, index)).ToList();
        copy.Sort((a, b) =>
        {
            int r = compare(a.item, b.item);
            return r != 0 ? r : a.index.CompareTo(b.index);
        });
        FList.Clear();
        FList.AddRange(copy.Select(c => c.item));
    }

    public IEnumerator<object?> GetEnumerator() => FList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
