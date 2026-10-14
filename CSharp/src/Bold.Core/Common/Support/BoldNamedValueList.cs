// Delphi: Source/Common/Support/BoldNamedValueList.pas
// Port map: CSharp/port/units/BoldNamedValueList.md

namespace Bold;

/// <summary>A name with a string value and an object.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNamedValueListEntry : TBoldMemoryManagedObject
{
    private readonly string fName;
    private string fValue;
    private object? fObject;

    [Delphi(PortStatus.Ported)]
    public TBoldNamedValueListEntry(string Name, string Value, object? aObject)
    {
        fName = Name;
        fValue = Value;
        fObject = aObject;
    }

    [Delphi(PortStatus.Ported)]
    public string Name => fName;

    [Delphi(PortStatus.Ported)]
    public string Value
    {
        get => fValue;
        set => fValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public object? aObject
    {
        get => fObject;
        set => fObject = value;
    }
}

/// <summary>An ordered list of name/value entries with a case-sensitive index by name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNamedValueList : TBoldIndexableList
{
    private static int IX_Name = -1;

    /// <summary>The name index: entries keyed by name, case-sensitive.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TNameIndex")]
    private sealed class TNameIndex : TBoldCaseSensitiveStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldNamedValueListEntry)Item!).Name;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldNamedValueList()
    {
        SetIndexCapacity(2);
        BoldIndexableList.SetIndexVariable(ref IX_Name, AddIndex(new TNameIndex()));
        OwnsEntries = true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldNamedValueListEntry GetItem(int index) => (TBoldNamedValueListEntry)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldNamedValueListEntry? GetItemByName(string Name) =>
        (TBoldNamedValueListEntry?)((TNameIndex)Indexes(IX_Name)!).FindByString(Name);

    [Delphi(PortStatus.Ported)]
    private void SetCommaText(string CommaText)
    {
        var stringList = new TStringList { CommaText = CommaText };
        AddFromStrings(stringList);
    }

    [Delphi(PortStatus.Ported)]
    private string GetCommaText()
    {
        var stringList = new TStringList();
        AddToStrings(stringList);
        return stringList.CommaText;
    }

    [Delphi(PortStatus.Ported)]
    private string GetValueByname(string Name) => GetItemByName(Name)?.Value ?? "";

    /// <summary>The setter of ValueByName: sets the value, adding an entry when the name is new.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetValueByname(string Name, string NewValue)
    {
        TBoldNamedValueListEntry? entry = GetItemByName(Name);
        if (entry is not null)
        {
            entry.Value = NewValue;
        }
        else
        {
            AddEntry(Name, NewValue);
        }
    }

    [Delphi(PortStatus.Ported)]
    private object? GetObjectByName(string Name) => GetItemByName(Name)?.aObject;

    /// <summary>The setter of ObjectByName: sets the object, adding an entry when the name is new.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetObjectByName(string Name, object? NewObject)
    {
        TBoldNamedValueListEntry? entry = GetItemByName(Name);
        if (entry is not null)
        {
            entry.aObject = NewObject;
        }
        else
        {
            AddEntry(Name, "", NewObject);
        }
    }

    /// <summary>Adds an entry for each "name=value" line, with the line's object.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddFromStrings(TStrings strings)
    {
        for (int i = 0; i < strings.Count; i++)
        {
            string name = strings.Names(i);
            AddEntry(name, SysUtils.Copy(strings[i], name.Length + 2, SysUtils.MaxInt), strings.Objects(i));
        }
    }

    /// <summary>Adds a "name=value" line with the entry's object for each entry.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddToStrings(TStrings strings)
    {
        for (int i = 0; i < Count; i++)
        {
            TBoldNamedValueListEntry entry = this[i];
            strings.AddObject(entry.Name + "=" + entry.Value, entry.aObject);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddEntry(string Name, string Value, object? aObject = null) =>
        Add(new TBoldNamedValueListEntry(Name, Value, aObject));

    [Delphi(PortStatus.Ported)]
    public void RemoveName(string Name)
    {
        TBoldNamedValueListEntry? item = GetItemByName(Name);
        if (item is not null)
        {
            Remove(item);
        }
    }

    [Delphi(PortStatus.Ported)]
    public new TBoldNamedValueListEntry this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public TBoldNamedValueListEntry? ItemByName(string Name) => GetItemByName(Name);

    [Delphi(PortStatus.Ported)]
    public string CommaText
    {
        get => GetCommaText();
        set => SetCommaText(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ValueByName(string Name) => GetValueByname(Name);

    [Delphi(PortStatus.Ported)]
    public object? ObjectByName(string Name) => GetObjectByName(Name);
}
