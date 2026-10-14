// Delphi: Source/MoldModel/TypeNameDictionary/BoldTypeNameDictionary.pas
// Port map: CSharp/port/units/BoldTypeNameDictionary.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldTypeNameDictionary", Symbol = "TBoldTypeNameDictionaryClass", Note = "Global using alias of System.Type, in BoldTypeNameDictionary.Aliases.cs.")]

namespace Bold;

/// <summary>
/// How one model type name (such as "String" or "Integer") maps to the Bold attribute class, persistence mapper,
/// accessor, native type, COM and IDL types and value interface. Names may contain &lt;Name&gt;, which expands to the
/// expression name.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A plain class; Delphi derives it from TCollectionItem for VCL streaming.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldTypeNameMapping.DefineProperties", Note = "VCL form streaming; the port reads the old StreamName key in SetAsString.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldTypeNameMapping.ReadStreamName", Note = "VCL form streaming; the port reads the old StreamName key in SetAsString.")]
public class TBoldTypeNameMapping
{
    private string FExpressionName = "";
    private string FAccessor = "";
    private string FDelphiName = "";
    private string FModelName = "";
    private string FNativeType = "";
    private string FMapperName = "";
    private string fContentsName = "";
    private string fUnitName = "";
    private string fComType = "";
    private string fIDLType = "";
    private string fValueInterface = "";
    private string fValueInterfaceAccessor = "";
    private string fValueInterfaceNativeType = "";

    internal TBoldTypeNameDictionary? Collection;

    private static string Expand(string name, string expressionName) =>
        BoldNameExpander.BoldExpandName(name, expressionName, TExpansionType.xtExpression, -1, TBoldNationalCharConversion.nccDefault);

    [Delphi(PortStatus.Ported)]
    private string GetAsString() => SysUtils.Format(
        "ModelName=%s,ExpressionName=%s,DelphiName=%s,ContentsName=%s,MapperName=%s,AccessorName=%s,NativeType=%s,UnitName=%s,ComType=%s,IDLType=%s,ValueInterface=%s,ValueInterfaceAccessor=%s,ValueInterfaceNativeType=%s",
        ModelName, ExpressionName, DelphiName, ContentsName, MapperName, Accessor, NativeType, UnitNameText, ComType, IDLType,
        ValueInterface, ValueInterfaceAccessor, ValueInterfaceNativeType);

    /// <summary>Reads "Key=Value" pairs separated by commas; the old key StreamName is read as ContentsName.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetAsString(string Value)
    {
        var list = new TStringList { CommaText = Value };
        ModelName = list.Values("ModelName");
        ExpressionName = list.Values("ExpressionName");
        DelphiName = list.Values("DelphiName");
        ContentsName = list.Values("StreamName") != "" ? list.Values("StreamName") : list.Values("ContentsName");
        MapperName = list.Values("MapperName");
        Accessor = list.Values("AccessorName");
        NativeType = list.Values("NativeType");
        UnitNameText = list.Values("UnitName");
        ComType = list.Values("ComType");
        IDLType = list.Values("IDLType");
        ValueInterface = list.Values("ValueInterface");
        ValueInterfaceAccessor = list.Values("ValueInterfaceAccessor");
        ValueInterfaceNativeType = list.Values("ValueInterfaceNativeType");
    }

    [Delphi(PortStatus.Ported)]
    private string GetExpandedAccessor() => Expand(Accessor, ExpressionName);

    [Delphi(PortStatus.Ported)]
    private string GetExpandedDelphiName() => Expand(DelphiName, ExpressionName);

    [Delphi(PortStatus.Ported)]
    private string GetExpandedMapperName() => Expand(MapperName, ExpressionName);

    [Delphi(PortStatus.Ported)]
    private string GetExpandedNativeType() => Expand(NativeType, ExpressionName);

    [Delphi(PortStatus.Ported)]
    private string GetExpandedContentsName() => Expand(ContentsName, ExpressionName);

    [Delphi(PortStatus.Ported)]
    private string GetExpandedComType() => Expand(ComType, ExpressionName);

    [Delphi(PortStatus.Ported)]
    protected internal string AsString
    {
        get => GetAsString();
        set => SetAsString(value);
    }

    /// <summary>Copies every name to <paramref name="Dest"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssignTo(TBoldTypeNameMapping Dest)
    {
        Dest.ModelName = ModelName;
        Dest.ExpressionName = ExpressionName;
        Dest.DelphiName = DelphiName;
        Dest.ContentsName = ContentsName;
        Dest.MapperName = MapperName;
        Dest.Accessor = Accessor;
        Dest.NativeType = NativeType;
        Dest.UnitNameText = UnitNameText;
        Dest.ComType = ComType;
        Dest.IDLType = IDLType;
        Dest.ValueInterface = ValueInterface;
        Dest.ValueInterfaceAccessor = ValueInterfaceAccessor;
        Dest.ValueInterfaceNativeType = ValueInterfaceNativeType;
    }

    [Delphi(PortStatus.Ported)] public string ExpandedDelphiName => GetExpandedDelphiName();
    [Delphi(PortStatus.Ported)] public string ExpandedContentsName => GetExpandedContentsName();
    [Delphi(PortStatus.Ported)] public string ExpandedMapperName => GetExpandedMapperName();
    [Delphi(PortStatus.Ported)] public string ExpandedAccessor => GetExpandedAccessor();
    [Delphi(PortStatus.Ported)] public string ExpandedNativeType => GetExpandedNativeType();
    [Delphi(PortStatus.Ported)] public string ExpandedComType => GetExpandedComType();

    [Delphi(PortStatus.Ported)] public string ModelName { get => FModelName; set => FModelName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ExpressionName { get => FExpressionName; set => FExpressionName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string DelphiName { get => FDelphiName; set => FDelphiName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ContentsName { get => fContentsName; set => fContentsName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string MapperName { get => FMapperName; set => FMapperName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string Accessor { get => FAccessor; set => FAccessor = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string NativeType { get => FNativeType; set => FNativeType = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string UnitNameText { get => fUnitName; set => fUnitName = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ComType { get => fComType; set => fComType = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string IDLType { get => fIDLType; set => fIDLType = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ValueInterface { get => fValueInterface; set => fValueInterface = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ValueInterfaceAccessor { get => fValueInterfaceAccessor; set => fValueInterfaceAccessor = value ?? ""; }
    [Delphi(PortStatus.Ported)] public string ValueInterfaceNativeType { get => fValueInterfaceNativeType; set => fValueInterfaceNativeType = value ?? ""; }
}

/// <summary>
/// The type name mappings of a model. Lookup by model name ignores case and falls back to the &lt;Default&gt; mapping.
/// Files hold one mapping per line in the AsString format.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A list of mappings; Delphi derives it from TCollection for VCL streaming.")]
public class TBoldTypeNameDictionary
{
    private static readonly string[][] DefaultMappings =
    [
        [BoldDefs.DEFAULTNAMELITERAL, "String", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "WideString", "BSTR", "IBoldStringContent", "AsString", "String"],
        ["String", "String", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "WideString", "BSTR", "IBoldStringContent", "AsString", "String"],
        ["AnsiString", "String", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "WideString", "BSTR", "IBoldStringContent", "AsString", "String"],
        ["<Numeric>", "Numeric", "TBA<Name>", "", "", "", "", "BoldAttributes", "", "", "", "", ""],
        ["Integer", "Integer", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Int", "Integer", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Word", "Word", "TBA<Name>", "Integer", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["SmallInt", "SmallInt", "TBA<Name>", "Integer", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Byte", "Byte", "TBA<Name>", "Integer", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["ShortInt", "ShortInt", "TBA<Name>", "Integer", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Float", "Float", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "double", "BoldAttributes", "Double", "DOUBLE", "IBoldFloatContent", "AsFloat", "double"],
        ["Double", "Float", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "double", "BoldAttributes", "Double", "DOUBLE", "IBoldFloatContent", "AsFloat", "double"],
        ["Currency", "Currency", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "<Name>", "BoldAttributes", "Currency", "CURRENCY", "IBoldCurrencyContent", "AsCurrency", "Currency"],
        ["<Moment>", "Moment", "TBA<Name>", "", "", "", "", "BoldAttributes", "", "", "", "", ""],
        ["DateTime", "DateTime", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "T<Name>", "BoldAttributes", "TDateTime", "DATE", "IBoldDateTimeContent", "AsDateTime", "TDateTime"],
        ["TimeStamp", "DateTime", "TBADateTime", "<Name>", "TBoldPM<Name>", "As<Name>", "T<Name>", "BoldAttributes", "TDateTime", "DATE", "IBoldDateTimeContent", "AsDateTime", "TDateTime"],
        ["Date", "Date", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "T<Name>", "BoldAttributes", "TDateTime", "DATE", "IBoldDateContent", "AsDate", "TDateTime"],
        ["Time", "Time", "TBA<Name>", "<Name>", "TBoldPM<Name>", "As<Name>", "T<Name>", "BoldAttributes", "TDateTime", "DATE", "IBoldTimeContent", "AsTime", "TDateTime"],
        ["<ValueSet>", "ValueSet", "TBA<Name>", "Integer", "TBoldPMInteger", "AsInteger", "integer", "BoldAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Boolean", "Boolean", "TBA<Name>", "Integer", "TBoldPMInteger", "As<Name>", "<Name>", "BoldAttributes", "WordBool", "VARIANT_BOOL", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Bool", "Boolean", "TBA<Name>", "Integer", "TBoldPMInteger", "As<Name>", "<Name>", "BoldAttributes", "WordBool", "VARIANT_BOOL", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Constraint", "Constraint", "TBA<Name>", "Integer", "TBoldPMInteger", "AsBoolean", "Boolean", "BoldAttributes", "WordBool", "VARIANT_BOOL", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Blob", "Blob", "TBA<Name>", "<Name>", "TBoldPM<Name>", "AsString", "String", "BoldAttributes", "WideString", "BSTR", "IBoldBlobContent", "AsBlob", "String"],
        ["TypedBlob", "TypedBlob", "TBA<Name>", "<Name>", "TBoldPM<Name>", "AsString", "String", "BoldAttributes", "WideString", "BSTR", "IBoldTypedBlob", "AsBlob", "String"],
        ["BlobImageBMP", "BlobImageBMP", "TBA<Name>", "Blob", "TBoldPMBlob", "AsString", "String", "BoldAttributes", "WideString", "BSTR", "IBoldBlobContent", "AsBlob", "String"],
        ["BlobImageJPEG", "BlobImageJPEG", "TBA<Name>", "Blob", "TBoldPMBlob", "AsString", "String", "BoldAttributes", "WideString", "BSTR", "IBoldBlobContent", "AsBlob", "String"],
        ["MLString", "MLString", "TBA<Name>", "Blob", "TBoldPMBlob", "AsString", "String", "BoldMLAttributes", "WideString", "BSTR", "IBoldBlobContent", "AsBlob", "String"],
        ["MLSubString", "MLSubString", "TBA<Name>", "String", "TBoldPMString", "AsString", "String", "BoldMLAttributes", "WideString", "BSTR", "IBoldStringContent", "AsString", "String"],
        ["MLValueSet", "MLValueSet", "TBA<Name>", "Integer", "TBoldPMInteger", "AsInteger", "integer", "BoldMLAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
        ["Language", "Language", "TBA<Name>", "Integer", "TBoldPMInteger", "AsInteger", "integer", "BoldMLAttributes", "Integer", "LONG", "IBoldIntegerContent", "AsInteger", "integer"],
    ];

    private readonly object? fOwner;
    private readonly List<TBoldTypeNameMapping> fItems = [];

    [Delphi(PortStatus.Ported, Note = "The owner is any object (a component in Delphi).")]
    public TBoldTypeNameDictionary(object? Owner)
    {
        fOwner = Owner;
    }

    /// <summary>The number of mappings.</summary>
    public int Count => fItems.Count;

    /// <summary>Adds an empty mapping, as TCollection.Add.</summary>
    public TBoldTypeNameMapping Add()
    {
        var mapping = new TBoldTypeNameMapping { Collection = this };
        fItems.Add(mapping);
        return mapping;
    }

    /// <summary>Removes every mapping.</summary>
    public void Clear() => fItems.Clear();

    [Delphi(PortStatus.Ported)]
    private TBoldTypeNameMapping GetMapping(int Index) => fItems[Index];

    /// <summary>The mapping for <paramref name="ModelName"/>, or the &lt;Default&gt; mapping.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldTypeNameMapping? GetMappingForModelName(string ModelName) =>
        GetExactMappingForModelName(ModelName) ?? GetExactMappingForModelName(BoldDefs.DEFAULTNAME);

    [Delphi(PortStatus.Ported)]
    private TBoldTypeNameMapping? GetExactMappingForModelName(string ModelName) =>
        fItems.FirstOrDefault(m => SysUtils.SameText(m.ModelName, ModelName));

    [Delphi(PortStatus.Ported)]
    private void SaveToStringList(TStringList StrList)
    {
        foreach (TBoldTypeNameMapping m in fItems)
        {
            StrList.Add(m.AsString);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void LoadFromStringList(TStringList StrList)
    {
        for (int i = 0; i < StrList.Count; i++)
        {
            if (SysUtils.Trim(StrList[i]) != "")
            {
                Add().AsString = SysUtils.Trim(StrList[i]);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected object? GetOwner() => fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameMapping Mapping(int Index) => GetMapping(Index);

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameMapping? MappingForModelName(string ModelName) => GetMappingForModelName(ModelName);

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameMapping? ExactMappingForModelName(string ModelName) => GetExactMappingForModelName(ModelName);

    [Delphi(PortStatus.Ported)]
    public void SaveToFile(string FileName)
    {
        var list = new TStringList();
        SaveToStringList(list);
        list.SaveToFile(FileName);
    }

    [Delphi(PortStatus.Ported)]
    public void LoadFromFile(string FileName)
    {
        var list = new TStringList();
        list.LoadFromFile(FileName);
        LoadFromStringList(list);
    }

    /// <summary>Adds Bold's 30 built-in mappings (String, Integer, Float, DateTime, Blob, ...).</summary>
    [Delphi(PortStatus.Ported)]
    public void AddDefaultMappings()
    {
        foreach (string[] row in DefaultMappings)
        {
            TBoldTypeNameMapping m = Add();
            m.ModelName = row[0];
            m.ExpressionName = row[1];
            m.DelphiName = row[2];
            m.ContentsName = row[3];
            m.MapperName = row[4];
            m.Accessor = row[5];
            m.NativeType = row[6];
            m.UnitNameText = row[7];
            m.ComType = row[8];
            m.IDLType = row[9];
            m.ValueInterface = row[10];
            m.ValueInterfaceAccessor = row[11];
            m.ValueInterfaceNativeType = row[12];
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameMapping AddMapping() => Add();
}

/// <summary>Unit-level variables of BoldTypeNameDictionary.</summary>
public static partial class BoldTypeNameDictionary
{
    /// <summary>The dictionary type to create; a subclass can replace it.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldTypeNameDictionaryClass TBoldCurrentTypeNameDictionaryClass = typeof(TBoldTypeNameDictionary);
}
