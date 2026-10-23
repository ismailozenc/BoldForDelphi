// Delphi: Source/Common/Support/BoldStringList.pas
// Port map: CSharp/port/units/BoldStringList.md

namespace Bold;

/// <summary>
/// A string list that shares its strings (through the shared string manager) and, when sorted, finds name=value
/// entries by binary search.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldStringList : TStringList
{
    private bool fChangingValue;

    /// <summary>The value of the name; a binary search when the list is sorted.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetFastValue(string Name)
    {
        if (!Sorted)
        {
            return Values(Name);
        }

        return GetIndexOfName(Name, out int i) ? SysUtils.Copy(Get(i), Name.Length + 2, SysUtils.MaxInt) : "";
    }

    /// <summary>Sets the value of the name, keeping a sorted list sorted.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetFastValue(string Name, string Value)
    {
        if (!Sorted)
        {
            SetValues(Name, Value);
        }
        else if (GetIndexOfName(Name, out int i))
        {
            fChangingValue = true;
            Sorted = false;
            Put(i, Name + "=" + Value);
            Sorted = true;
            fChangingValue = false;
        }
        else
        {
            Add(Name + "=" + Value);
        }
    }

    /// <summary>The part after the first "=" of the entry, or empty.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetValueByIndex(int index)
    {
        string s = Get(index);
        int p = SysUtils.Pos("=", s);
        return p != 0 ? SysUtils.Copy(s, p + 1, SysUtils.MaxInt) : "";
    }

    /// <summary>Compares the shorter string with the start of the other, ignoring case.</summary>
    [Delphi(PortStatus.Ported, Symbol = "BoldAnsiLCompareShortest", Note = "A unit-level routine in Delphi.")]
    private static int BoldAnsiLCompareShortest(string s1, string s2)
    {
        int Len = Math.Min(s1.Length, s2.Length);
        return SysUtils.AnsiCompareText(s1[..Len], s2[..Len]);
    }

    [Delphi(PortStatus.Ported, Note = "Public, as the RTL emulation's Put is.")]
    public override void Put(int Index, string S) => base.Put(Index, BoldSharedStrings.BoldSharedStringManager().GetSharedString(S));

    /// <summary>Sorts, except while SetFastValue re-sorts a list it keeps sorted.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Sort()
    {
        if (!fChangingValue)
        {
            base.Sort();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddMultipleStrings(string[] StringArr)
    {
        BeginUpdate();
        try
        {
            foreach (string s in StringArr)
            {
                Add(s);
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AssignMultipleStrings(string[] StringArr)
    {
        BeginUpdate();
        try
        {
            Clear();
            AddMultipleStrings(StringArr);
        }
        finally
        {
            EndUpdate();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int Add(string S) => base.Add(BoldSharedStrings.BoldSharedStringManager().GetSharedString(S));

    /// <summary>Binary-searches a sorted list for the entry "Name=..."; Index is where it is or would go.</summary>
    [Delphi(PortStatus.Ported)]
    public bool GetIndexOfName(string Name, out int Index) => GetIndexOfprefix(Name + "=", out Index);

    /// <summary>Binary-searches a sorted list for an entry starting with the prefix (the first one unless duplicates are accepted).</summary>
    [Delphi(PortStatus.Ported)]
    public bool GetIndexOfprefix(string Name, out int Index)
    {
        bool result = false;
        int L = 0;
        int H = Count - 1;
        while (L <= H)
        {
            int I = (L + H) >> 1;
            int C = BoldAnsiLCompareShortest(Get(I), Name);
            if (C < 0)
            {
                L = I + 1;
            }
            else
            {
                H = I - 1;
                if (C == 0)
                {
                    result = true;
                    if (Duplicates != TDuplicates.dupAccept)
                    {
                        L = I;
                    }
                }
            }
        }

        Index = L;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveValue(string Name)
    {
        if (GetIndexOfName(Name, out int i))
        {
            Delete(i);
        }
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a getter method, with SetFastValues to set.")]
    public string FastValues(string Name) => GetFastValue(Name);

    /// <summary>Sets the value of the name (the setter of FastValues).</summary>
    public void SetFastValues(string Name, string Value) => SetFastValue(Name, Value);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public string ValueByIndex(int index) => GetValueByIndex(index);
}
