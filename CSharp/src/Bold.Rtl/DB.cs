using System.Globalization;

namespace Bold;

/// <summary>
/// Delphi's TFieldType, in Delphi's order (Delphi 7's values, then the ones later versions appended), because Bold
/// streams the ordinal.
/// </summary>
public enum TFieldType
{
    ftUnknown, ftString, ftSmallint, ftInteger, ftWord, ftBoolean, ftFloat, ftCurrency, ftBCD, ftDate, ftTime, ftDateTime,
    ftBytes, ftVarBytes, ftAutoInc, ftBlob, ftMemo, ftGraphic, ftFmtMemo, ftParadoxOle, ftDBaseOle, ftTypedBinary, ftCursor,
    ftFixedChar, ftWideString, ftLargeint, ftADT, ftArray, ftReference, ftDataSet, ftOraBlob, ftOraClob, ftVariant,
    ftInterface, ftIDispatch, ftGuid, ftTimeStamp, ftFMTBcd, ftFixedWideChar, ftWideMemo, ftOraTimeStamp, ftOraInterval,
    ftLongWord, ftShortint, ftByte, ftExtended, ftConnection, ftParams, ftStream, ftTimeStampOffset, ftObject, ftSingle,
}

/// <summary>Delphi's TParamType: the direction of a query parameter.</summary>
public enum TParamType { ptUnknown, ptInput, ptOutput, ptInputOutput, ptResult }

/// <summary>
/// Delphi's TParam: a named query parameter with a field type, a direction and a value. Setting a value through an
/// As... property also sets the field type, as in Delphi.
/// </summary>
public class TParam
{
    private object? fValue;

    public TParam()
    {
    }

    public TParam(string Name, TFieldType DataType = TFieldType.ftUnknown, TParamType ParamType = TParamType.ptUnknown)
    {
        this.Name = Name;
        this.DataType = DataType;
        this.ParamType = ParamType;
    }

    public string Name { get; set; } = "";

    public TFieldType DataType { get; set; }

    public TParamType ParamType { get; set; }

    /// <summary>The value; null (or DBNull) is SQL NULL.</summary>
    public object? Value
    {
        get => fValue;
        set => fValue = value is DBNull ? null : value;
    }

    public bool IsNull => fValue is null;

    public void Clear() => fValue = null;

    public string AsString
    {
        get => fValue is null ? "" : Convert.ToString(fValue, CultureInfo.InvariantCulture) ?? "";
        set
        {
            fValue = value;
            DataType = TFieldType.ftString;
        }
    }

    public string AsMemo
    {
        get => AsString;
        set
        {
            fValue = value;
            DataType = TFieldType.ftMemo;
        }
    }

    public string AsBlob
    {
        get => AsString;
        set
        {
            fValue = value;
            DataType = TFieldType.ftBlob;
        }
    }

    public int AsInteger
    {
        get => fValue is null ? 0 : Convert.ToInt32(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftInteger;
        }
    }

    public int AsSmallInt
    {
        get => AsInteger;
        set
        {
            fValue = value;
            DataType = TFieldType.ftSmallint;
        }
    }

    public long AsLargeInt
    {
        get => fValue is null ? 0 : Convert.ToInt64(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftLargeint;
        }
    }

    public double AsFloat
    {
        get => fValue is null ? 0 : Convert.ToDouble(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftFloat;
        }
    }

    public decimal AsCurrency
    {
        get => fValue is null ? 0 : Convert.ToDecimal(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftCurrency;
        }
    }

    public bool AsBoolean
    {
        get => fValue is not null && Convert.ToBoolean(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftBoolean;
        }
    }

    public DateTime AsDateTime
    {
        get => fValue is null ? default : Convert.ToDateTime(fValue, CultureInfo.InvariantCulture);
        set
        {
            fValue = value;
            DataType = TFieldType.ftDateTime;
        }
    }

    public DateTime AsDate
    {
        get => AsDateTime.Date;
        set
        {
            fValue = value.Date;
            DataType = TFieldType.ftDate;
        }
    }

    public DateTime AsTime
    {
        get => AsDateTime;
        set
        {
            fValue = value;
            DataType = TFieldType.ftTime;
        }
    }

    public decimal AsBCD
    {
        get => AsCurrency;
        set
        {
            fValue = value;
            DataType = TFieldType.ftBCD;
        }
    }

    public int AsWord
    {
        get => AsInteger;
        set
        {
            fValue = value;
            DataType = TFieldType.ftWord;
        }
    }

    /// <summary>Takes the field's type and the value, as Delphi's AssignFieldValue.</summary>
    public void AssignFieldValue(TField Field, object? Value)
    {
        DataType = Field.DataType;
        this.Value = Value;
    }

    /// <summary>Copies name, types and value from <paramref name="Source"/>.</summary>
    public void Assign(TParam Source)
    {
        Name = Source.Name;
        DataType = Source.DataType;
        ParamType = Source.ParamType;
        fValue = Source.fValue;
    }
}

/// <summary>Delphi's TParams: an ordered collection of parameters, looked up by name ignoring case.</summary>
public class TParams : IEnumerable<TParam>
{
    private readonly List<TParam> fItems = [];

    public int Count => fItems.Count;

    public TParam this[int index] => fItems[index];

    /// <summary>Adds a new, unnamed parameter.</summary>
    public TParam Add()
    {
        var p = new TParam();
        fItems.Add(p);
        return p;
    }

    public void AddParam(TParam Value) => fItems.Add(Value);

    public void RemoveParam(TParam Value) => fItems.Remove(Value);

    public TParam CreateParam(TFieldType FldType, string ParamName, TParamType ParamType)
    {
        var p = new TParam(ParamName, FldType, ParamType);
        fItems.Add(p);
        return p;
    }

    public TParam? FindParam(string Value) => fItems.Find(p => SysUtils.AnsiSameText(p.Name, Value));

    /// <summary>The parameter named <paramref name="Value"/>; raises when there is none, as Delphi does.</summary>
    public TParam ParamByName(string Value) =>
        FindParam(Value) ?? throw new InvalidOperationException(SysUtils.Format("Parameter '%s' not found", Value));

    public void Clear() => fItems.Clear();

    /// <summary>Replaces the parameters with copies of <paramref name="Source"/>'s.</summary>
    public void Assign(TParams Source)
    {
        fItems.Clear();
        foreach (TParam p in Source)
        {
            Add().Assign(p);
        }
    }

    public IEnumerator<TParam> GetEnumerator() => fItems.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
