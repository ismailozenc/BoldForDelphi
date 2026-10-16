using System.Globalization;

namespace Bold;

/// <summary>Delphi's EDatabaseError.</summary>
public class EDatabaseError(string? message = null, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Delphi's TDataSetState (the states this emulation uses).</summary>
public enum TDataSetState { dsInactive, dsBrowse, dsEdit, dsInsert }

/// <summary>Delphi's TLocateOption.</summary>
public enum TLocateOption { loCaseInsensitive, loPartialKey }

/// <summary>Delphi's TIndexOption.</summary>
public enum TIndexOption { ixPrimary, ixUnique, ixDescending, ixCaseInsensitive, ixExpression, ixNonMaintained }

/// <summary>Delphi's TFieldDef: the definition of a column.</summary>
public class TFieldDef(string Name, TFieldType DataType, int Size, bool Required)
{
    public string Name { get; set; } = Name;
    public TFieldType DataType { get; set; } = DataType;
    public int Size { get; set; } = Size;
    public bool Required { get; set; } = Required;
}

/// <summary>Delphi's TFieldDefs.</summary>
public class TFieldDefs : IEnumerable<TFieldDef>
{
    private readonly List<TFieldDef> fItems = [];

    public int Count => fItems.Count;
    public TFieldDef this[int Index] => fItems[Index];

    public TFieldDef Add(string Name, TFieldType DataType, int Size = 0, bool Required = false)
    {
        var def = new TFieldDef(Name, DataType, Size, Required);
        fItems.Add(def);
        return def;
    }

    public int IndexOf(string Name) => fItems.FindIndex(d => string.Equals(d.Name, Name, StringComparison.OrdinalIgnoreCase));
    public TFieldDef? Find(string Name) => fItems.Find(d => string.Equals(d.Name, Name, StringComparison.OrdinalIgnoreCase));
    public void Clear() => fItems.Clear();
    public IEnumerator<TFieldDef> GetEnumerator() => fItems.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Delphi's TIndexDef.</summary>
public class TIndexDef(string Name, string Fields, DelphiSet<TIndexOption> Options, string DescFields = "")
{
    public string Name { get; set; } = Name;
    public string Fields { get; set; } = Fields;
    public DelphiSet<TIndexOption> Options { get; set; } = Options;
    public string DescFields { get; set; } = DescFields;
}

/// <summary>Delphi's TIndexDefs.</summary>
public class TIndexDefs : IEnumerable<TIndexDef>
{
    private readonly List<TIndexDef> fItems = [];

    public int Count => fItems.Count;
    public TIndexDef this[int Index] => fItems[Index];

    public TIndexDef Add(string Name, string Fields, DelphiSet<TIndexOption> Options, string DescFields = "")
    {
        var def = new TIndexDef(Name, Fields, Options, DescFields);
        fItems.Add(def);
        return def;
    }

    public void Clear() => fItems.Clear();

    /// <summary>
    /// Delphi's GetIndexForFields: the first index whose leading fields (separated by semicolons) are Fields.
    /// </summary>
    public TIndexDef? GetIndexForFields(string Fields, bool CaseInsensitive)
    {
        StringComparison comparison = CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (TIndexDef def in fItems)
        {
            if (def.Fields.StartsWith(Fields, comparison) && (def.Fields.Length == Fields.Length || def.Fields[Fields.Length] == ';'))
            {
                return def;
            }
        }

        return null;
    }

    /// <summary>What reads the definitions from the database; the data set sets it (Delphi asks the data set too).</summary>
    public Action<TIndexDefs>? Updater { get; set; }

    /// <summary>Delphi's TIndexDefs.Update: reads the definitions again, when the data set can.</summary>
    public void Update() => Updater?.Invoke(this);

    public IEnumerator<TIndexDef> GetEnumerator() => fItems.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Delphi's TField: a column of a data set, reading and writing the value of the current record. Values are kept as
/// .NET values (string, int, long, double, decimal, bool, DateTime, byte[]), null for SQL NULL.
/// </summary>
public class TField
{
    private readonly TDataSet fDataSet;

    internal TField(TDataSet DataSet, TFieldDef Def, int FieldNo)
    {
        fDataSet = DataSet;
        FieldName = Def.Name;
        DataType = Def.DataType;
        Size = Def.Size;
        Required = Def.Required;
        this.FieldNo = FieldNo;
    }

    public TDataSet DataSet => fDataSet;
    public string FieldName { get; }
    public TFieldType DataType { get; }
    public int Size { get; }
    public bool Required { get; }

    /// <summary>The 1-based position in the data set's fields.</summary>
    public int FieldNo { get; }

    /// <summary>The value in the current record; null is SQL NULL.</summary>
    public object? Value
    {
        get => fDataSet.GetFieldData(FieldNo - 1);
        set => fDataSet.SetFieldData(FieldNo - 1, value is DBNull ? null : value);
    }

    public object? AsVariant
    {
        get => Value;
        set => Value = value;
    }

    public bool IsNull => Value is null;

    public string AsString
    {
        get => Value switch
        {
            null => "",
            byte[] bytes => new string(Array.ConvertAll(bytes, b => (char)b)),
            DateTime d => d.ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            object o => o.ToString() ?? "",
        };
        set => Value = value;
    }

    public int AsInteger
    {
        get => Value is null ? 0 : Convert.ToInt32(Value, CultureInfo.InvariantCulture);
        set => Value = value;
    }

    public long AsLargeInt
    {
        get => Value is null ? 0 : Convert.ToInt64(Value, CultureInfo.InvariantCulture);
        set => Value = value;
    }

    public double AsFloat
    {
        get => Value is null ? 0 : Convert.ToDouble(Value, CultureInfo.InvariantCulture);
        set => Value = value;
    }

    public decimal AsCurrency
    {
        get => Value is null ? 0 : Convert.ToDecimal(Value, CultureInfo.InvariantCulture);
        set => Value = value;
    }

    public bool AsBoolean
    {
        get => Value switch
        {
            null => false,
            bool b => b,
            string s => s.Length > 0 && (s[0] is 'T' or 't' or 'Y' or 'y' or '1'),
            object o => Convert.ToInt64(o, CultureInfo.InvariantCulture) != 0,
        };
        set => Value = value;
    }

    public DateTime AsDateTime
    {
        get => Value switch
        {
            null => DateTime.FromOADate(0),
            DateTime d => d,
            string s => DateTime.Parse(s, CultureInfo.InvariantCulture),
            double f => DateTime.FromOADate(f),
            object o => Convert.ToDateTime(o, CultureInfo.InvariantCulture),
        };
        set => Value = value;
    }
}

/// <summary>
/// Delphi's TDataSet, emulated over an in-memory record buffer: a subclass loads the records in InternalOpen and may
/// write changes back in InternalPost and InternalDelete. Navigation, field access, editing and Locate follow Delphi.
/// </summary>
public abstract class TDataSet
{
    private readonly List<object?[]> fRecords = [];
    private readonly List<TField> fFields = [];
    private int fRecNo = -1;
    private object?[]? fEditBuffer;
    private TDataSetState fState = TDataSetState.dsInactive;
    private bool fEof = true;
    private bool fBof = true;

    public TFieldDefs FieldDefs { get; } = new();
    public TDataSetState State => fState;
    public bool Active => fState != TDataSetState.dsInactive;
    public bool Eof => fEof;
    public bool Bof => fBof;
    public int RecordCount => fRecords.Count;
    public int FieldCount => fFields.Count;
    public IReadOnlyList<TField> Fields => fFields;

    /// <summary>The 1-based current record number, 0 when there is none.</summary>
    public int RecNo => fRecNo + 1;

    /// <summary>Loads the field definitions (when empty) and the records.</summary>
    protected abstract void InternalOpen(TFieldDefs FieldDefs, List<object?[]> Records);

    /// <summary>Writes a posted record; Inserted tells a new record from an edit. Nothing here (an in-memory data set).</summary>
    protected virtual void InternalPost(object?[] Record, object?[]? OldRecord, bool Inserted)
    {
    }

    /// <summary>Deletes a record from storage. Nothing here (an in-memory data set).</summary>
    protected virtual void InternalDelete(object?[] Record)
    {
    }

    protected virtual void InternalClose()
    {
    }

    public void Open()
    {
        if (Active)
        {
            return;
        }

        fRecords.Clear();
        InternalOpen(FieldDefs, fRecords);
        fFields.Clear();
        for (int i = 0; i < FieldDefs.Count; i++)
        {
            fFields.Add(new TField(this, FieldDefs[i], i + 1));
        }

        fState = TDataSetState.dsBrowse;
        First();
    }

    public void Close()
    {
        if (!Active)
        {
            return;
        }

        InternalClose();
        fRecords.Clear();
        fFields.Clear();
        fEditBuffer = null;
        fRecNo = -1;
        fState = TDataSetState.dsInactive;
        fEof = fBof = true;
    }

    private void CheckActive()
    {
        if (!Active)
        {
            throw new EDatabaseError($"Cannot perform this operation on a closed dataset ({GetType().Name})");
        }
    }

    private void CheckBrowse()
    {
        CheckActive();
        if (fState is TDataSetState.dsEdit or TDataSetState.dsInsert)
        {
            Post();
        }
    }

    public void First()
    {
        CheckBrowse();
        fRecNo = fRecords.Count > 0 ? 0 : -1;
        fBof = true;
        fEof = fRecords.Count == 0;
    }

    public void Last()
    {
        CheckBrowse();
        fRecNo = fRecords.Count - 1;
        fEof = true;
        fBof = fRecords.Count == 0;
    }

    public void Next()
    {
        CheckBrowse();
        if (fRecNo < fRecords.Count - 1)
        {
            fRecNo++;
            fBof = false;
        }
        else
        {
            fEof = true;
        }
    }

    public void Prior()
    {
        CheckBrowse();
        if (fRecNo > 0)
        {
            fRecNo--;
            fEof = false;
        }
        else
        {
            fBof = true;
        }
    }

    /// <summary>Moves by Distance records; returns how far it moved.</summary>
    public int MoveBy(int Distance)
    {
        int moved = 0;
        while (Distance > 0 && !Eof)
        {
            int before = fRecNo;
            Next();
            if (fRecNo != before)
            {
                moved++;
            }

            Distance--;
        }

        while (Distance < 0 && !Bof)
        {
            int before = fRecNo;
            Prior();
            if (fRecNo != before)
            {
                moved--;
            }

            Distance++;
        }

        return moved;
    }

    public TField? FindField(string FieldName) => fFields.Find(f => string.Equals(f.FieldName, FieldName, StringComparison.OrdinalIgnoreCase));

    public TField FieldByName(string FieldName) =>
        FindField(FieldName) ?? throw new EDatabaseError($"{GetType().Name}: Field '{FieldName}' not found");

    public object? this[string FieldName]
    {
        get => FieldByName(FieldName).Value;
        set => FieldByName(FieldName).Value = value;
    }

    /// <summary>Delphi's FieldValues[Name].</summary>
    public object? GetFieldValue(string FieldName) => this[FieldName];

    public void SetFieldValue(string FieldName, object? Value) => this[FieldName] = Value;

    internal object? GetFieldData(int Index)
    {
        CheckActive();
        if (fEditBuffer is not null)
        {
            return fEditBuffer[Index];
        }

        return fRecNo >= 0 ? fRecords[fRecNo][Index] : null;
    }

    internal void SetFieldData(int Index, object? Value)
    {
        CheckActive();
        if (fState == TDataSetState.dsBrowse)
        {
            Edit();
        }

        fEditBuffer![Index] = Value;
    }

    public void Edit()
    {
        CheckActive();
        if (fState != TDataSetState.dsBrowse)
        {
            return;
        }

        if (fRecNo < 0)
        {
            Append();
            return;
        }

        fEditBuffer = (object?[])fRecords[fRecNo].Clone();
        fState = TDataSetState.dsEdit;
    }

    public void Append()
    {
        CheckBrowse();
        fEditBuffer = new object?[fFields.Count];
        fState = TDataSetState.dsInsert;
    }

    public void Insert() => Append();

    public void Post()
    {
        CheckActive();
        if (fState == TDataSetState.dsInsert)
        {
            object?[] record = fEditBuffer!;
            InternalPost(record, null, true);
            fRecords.Add(record);
            fRecNo = fRecords.Count - 1;
        }
        else if (fState == TDataSetState.dsEdit)
        {
            object?[] old = fRecords[fRecNo];
            InternalPost(fEditBuffer!, old, false);
            fRecords[fRecNo] = fEditBuffer!;
        }

        fEditBuffer = null;
        fState = TDataSetState.dsBrowse;
        fEof = false;
        fBof = false;
    }

    public void Cancel()
    {
        fEditBuffer = null;
        if (Active)
        {
            fState = TDataSetState.dsBrowse;
        }
    }

    public void Delete()
    {
        CheckActive();
        if (fState == TDataSetState.dsInsert)
        {
            Cancel();
            return;
        }

        Cancel();
        if (fRecNo < 0)
        {
            throw new EDatabaseError("Cannot perform this operation on an empty dataset");
        }

        InternalDelete(fRecords[fRecNo]);
        fRecords.RemoveAt(fRecNo);
        if (fRecNo >= fRecords.Count)
        {
            fRecNo = fRecords.Count - 1;
        }

        fEof = fRecords.Count == 0;
    }

    /// <summary>Moves to the first record whose key fields (separated by ;) match KeyValues (one value or an array).</summary>
    public bool Locate(string KeyFields, object? KeyValues, DelphiSet<TLocateOption> Options)
    {
        CheckBrowse();
        string[] names = KeyFields.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object?[] values = names.Length == 1 ? [KeyValues] : (object?[])KeyValues!;
        int[] indexes = Array.ConvertAll(names, n => FieldByName(n).FieldNo - 1);
        for (int r = 0; r < fRecords.Count; r++)
        {
            bool match = true;
            for (int k = 0; k < indexes.Length && match; k++)
            {
                match = Matches(fRecords[r][indexes[k]], values[k], Options);
            }

            if (match)
            {
                fRecNo = r;
                fEof = fBof = false;
                return true;
            }
        }

        return false;
    }

    private static bool Matches(object? value, object? key, DelphiSet<TLocateOption> Options)
    {
        if (value is null || key is null)
        {
            return value is null && key is null;
        }

        if (value is string s || key is string)
        {
            string a = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            string b = Convert.ToString(key, CultureInfo.InvariantCulture) ?? "";
            StringComparison comparison = Options.Contains(TLocateOption.loCaseInsensitive) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return Options.Contains(TLocateOption.loPartialKey) ? a.StartsWith(b, comparison) : string.Equals(a, b, comparison);
        }

        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture) == Convert.ToDecimal(key, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException)
        {
            return Equals(value, key);
        }
    }
}

/// <summary>A data set over records given in code (as Delphi's TClientDataSet created in memory).</summary>
public class TMemoryDataSet : TDataSet
{
    private readonly List<object?[]> fInitial = [];

    /// <summary>Adds a record before opening.</summary>
    public void AddRecord(params object?[] Values) => fInitial.Add(Values);

    protected override void InternalOpen(TFieldDefs FieldDefs, List<object?[]> Records) => Records.AddRange(fInitial.Select(r => (object?[])r.Clone()));
}
