// Delphi: Source/PMapper/Default/BoldPMappersAttributeDefault.pas
// Port map: CSharp/port/units/BoldPMappersAttributeDefault.md
// The default attribute mappers: one column (two for typed blobs) of the type the configuration gives for the content.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersAttributeDefault", Symbol = "finalization", Note = "Removed the registrations; the registries live as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldPMappersAttributeDefault.</summary>
public static partial class BoldPMappersAttributeDefault
{
    /// <summary>The tolerance of date-time comparisons in optimistic locking.</summary>
    internal static readonly TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);

    /// <summary>Marks an empty blob in a database that does not keep empty blobs.</summary>
    internal const string InternalEmptyString = "<## Internal Empty ##>";

#pragma warning disable CA2255
    /// <summary>Registers the attribute mappers.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization()
    {
        TBoldMemberPersistenceMapperDescriptorList mappers = BoldPMapperLists.BoldMemberPersistenceMappers();
        mappers.AddDescriptor(typeof(TBoldSingleColumnMember), TBoldAbstractionLevel.alAbstract);
        foreach (Type t in new[]
        {
            typeof(TBoldPMString), typeof(TBoldPMStringBDECodePageBug), typeof(TBoldPMInteger), typeof(TBoldPMSmallInt), typeof(TBoldPMByte), typeof(TBoldPMShortInt),
            typeof(TBoldPMWord), typeof(TBoldPMFloat), typeof(TBoldPMCurrency), typeof(TBoldPMBlob), typeof(TBoldPMNonEmptyBlob), typeof(TBoldPMMemoBlob),
            typeof(TBoldPMTypedBlob), typeof(TBoldPMLiveBlob), typeof(TBoldPMLiveTypedBlob), typeof(TBoldPMDateTime), typeof(TBoldPMDate), typeof(TBoldPMTime),
            typeof(TBoldPMLogic),
        })
        {
            mappers.AddDescriptor(t, TBoldAbstractionLevel.alConcrete);
        }
    }
#pragma warning restore CA2255
}

/// <summary>A string, in a column of the attribute's length; empty strings may be stored as null.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMString : TBoldSingleColumnMember
{
    private readonly int fColumnSize;

    [Delphi(PortStatus.Ported, Symbol = "TBoldPMString.CreateFromMold")]
    public TBoldPMString(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        var attribute = (TMoldAttribute)Moldmember!;
        fColumnSize = attribute.Length;
        if (DefaultDbValue.Length != 0 && DefaultDbValue[0] != '\'' && DefaultDbValue[0] != '"')
        {
            DefaultDbValue = "'" + DefaultDbValue + "'";
        }

        if (SystemPersistenceMapper.SQLDataBaseConfig.StoreEmptyStringsAsNULL)
        {
            if (attribute.AllowNull)
            {
                throw new EBold(SysUtils.Format(BoldPMConsts.sAttributeMustNotAllowNullIfEmptyStringsStoreAsNull, MoldClass.Name, Moldmember!.Name));
            }

            fAllowNull = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SysUtils.Format(SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForString, GetColumnSize(ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftString;

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnSize(int ColumnIndex) => fColumnSize;

    /// <summary>Equal strings (a null field equals an empty string when empty strings are stored as null).</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aString = (IBoldStringContent)GetValue(ObjectContent)!;
        bool result = SystemPersistenceMapper.SQLDataBaseConfig.StoreEmptyStringsAsNULL && Field.IsNull && aString.asString.Length == 0;
        if (!result && !CheckEitherNull(Field, aString, out result))
        {
            result = Field.AsString == aString.asString;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => SystemPersistenceMapper.SQLDataBaseConfig.SupportsStringDefaultValues ? "''" : "";

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_String) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aString = (IBoldStringContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            if (SystemPersistenceMapper.SQLDataBaseConfig.StoreEmptyStringsAsNULL)
            {
                aString.asString = "";
            }
            else
            {
                aString.SetContentToNull();
            }
        }
        else
        {
            aString.asString = Field.AsString;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aString = (IBoldStringContent)GetEnsuredValue(ObjectContent)!;
        if (aString.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsString = aString.asString;
        }
    }
}

/// <summary>A string mapper that copied the string for the BDE; .NET strings are immutable, so it is the same as TBoldPMString.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMStringBDECodePageBug : TBoldPMString
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMStringBDECodePageBug(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    // make a copy of the string so that the param can mess with it alone
    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList) =>
        base.ValueToParam(ObjectContent, Param, ColumnIndex, TranslationList);
}

/// <summary>An integer column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMInteger : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMInteger(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftInteger;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var anInteger = (IBoldIntegerContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, anInteger, out bool result) ? result : Field.AsInteger == anInteger.asInteger;
    }

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => SystemPersistenceMapper.SQLDataBaseConfig.CorrectlyQuotedDefaultValue("0");

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Integer) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var anInteger = (IBoldIntegerContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            anInteger.SetContentToNull();
        }
        else
        {
            anInteger.asInteger = Field.AsInteger;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var anInteger = (IBoldIntegerContent)GetEnsuredValue(ObjectContent)!;
        if (anInteger.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsInteger = anInteger.asInteger;
        }
    }
}

/// <summary>A small integer column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMSmallInt : TBoldPMInteger
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMSmallInt(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForSmallInt;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftSmallint;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Integer) == 0;
}

/// <summary>A byte, in a small integer column (there is no field type for bytes).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMByte : TBoldPMSmallInt
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMByte(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Integer) == 0;
}

/// <summary>A short integer, in a small integer column (there is no field type for short integers).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMShortInt : TBoldPMSmallInt
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMShortInt(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Integer) == 0;
}

/// <summary>A word, in an integer column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMWord : TBoldPMInteger
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMWord(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftWord;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Integer) == 0;
}

/// <summary>A boolean, in a one-character column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMLogic : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMLogic(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => "VARCHAR(1)";

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftBoolean;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBoolean = (IBoldBooleanContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aBoolean, out bool result) ? result : Field.AsBoolean == aBoolean.asBoolean;
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Boolean) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBoolean = (IBoldBooleanContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aBoolean.SetContentToNull();
        }
        else
        {
            aBoolean.asBoolean = Field.AsBoolean;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBoolean = (IBoldBooleanContent)GetEnsuredValue(ObjectContent)!;
        if (aBoolean.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsBoolean = aBoolean.asBoolean;
        }
    }
}

/// <summary>A float column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMFloat : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMFloat(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForFloat;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftFloat;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aFloat = (IBoldFloatContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aFloat, out bool result) ? result : Field.AsFloat == aFloat.asFloat;
    }

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => SystemPersistenceMapper.SQLDataBaseConfig.CorrectlyQuotedDefaultValue("0");

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Float) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aFloat = (IBoldFloatContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aFloat.SetContentToNull();
        }
        else
        {
            aFloat.asFloat = Field.AsFloat;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aFloat = (IBoldFloatContent)GetEnsuredValue(ObjectContent)!;
        if (aFloat.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsFloat = aFloat.asFloat;
        }
    }
}

/// <summary>A currency column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMCurrency : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMCurrency(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForCurrency;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftCurrency;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aCurrency = (IBoldCurrencyContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aCurrency, out bool result) ? result : Field.AsCurrency == aCurrency.asCurrency;
    }

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => SystemPersistenceMapper.SQLDataBaseConfig.CorrectlyQuotedDefaultValue("0");

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Currency) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aCurrency = (IBoldCurrencyContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aCurrency.SetContentToNull();
        }
        else
        {
            aCurrency.asCurrency = Field.AsCurrency;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aCurrency = (IBoldCurrencyContent)GetEnsuredValue(ObjectContent)!;
        if (aCurrency.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsCurrency = aCurrency.asCurrency;
        }
    }
}

/// <summary>A blob column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMBlob : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.FieldTypeForBlob;

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForBlob;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBlob = (IBoldBlobContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aBlob, out bool result) ? result : Field.AsBlob == aBlob.asBlob;
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Blob) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBlob = (IBoldBlobContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aBlob.SetContentToNull();
        }
        else
        {
            aBlob.asBlob = Field.AsBlob;
        }
    }

    /// <summary>The blob as the parameter's value.</summary>
    [Delphi(PortStatus.Ported, Note = "This fork's Delphi sets only the parameter's data type (the value assignment is commented out); the value is set here.")]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBlob = (IBoldBlobContent)GetEnsuredValue(ObjectContent)!;
        if (aBlob.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.DataType = ColumnBDEFieldType(ColumnIndex);
            Param.SetAsBlob(aBlob.asBlob);
        }
    }
}

/// <summary>A blob in a memo column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMMemoBlob : TBoldPMBlob
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMMemoBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftMemo;
}

/// <summary>A blob for databases that do not keep empty blobs: an empty blob is stored as a marker.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMNonEmptyBlob : TBoldPMBlob
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMNonEmptyBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aBlobContent = (IBoldBlobContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aBlobContent, out bool result) ? result : Equals(Field.Value, aBlobContent.asBlob);
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        base.ValueFromField(OwningObjectId, ObjectContent, ValueSpace, TranslationList, Field, ColumnIndex);
        var aBlobContent = (IBoldBlobContent)GetEnsuredValue(ObjectContent)!;
        if (!aBlobContent.IsNull && aBlobContent.asBlob == BoldPMappersAttributeDefault.InternalEmptyString)
        {
            aBlobContent.asBlob = "";
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        bool EmptyBlobHandled = false;
        var aBlobContent = (IBoldBlobContent)GetEnsuredValue(ObjectContent)!;
        if (!aBlobContent.IsNull && aBlobContent.asBlob.Length == 0)
        {
            aBlobContent.asBlob = BoldPMappersAttributeDefault.InternalEmptyString;
            EmptyBlobHandled = true;
        }

        base.ValueToParam(ObjectContent, Param, ColumnIndex, TranslationList);
        if (EmptyBlobHandled)
        {
            aBlobContent.asBlob = "";
        }
    }
}

/// <summary>A blob fetched by itself with a live query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMLiveBlob : TBoldPMBlob
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldPMLiveBlob.CreateFromMold")]
    public TBoldPMLiveBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        fCustomFetch = true;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetRequiresLiveQuery() => true;
}

/// <summary>A blob with its content type in a second column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMTypedBlob : TBoldPMBlob
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMTypedBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => ColumnIndex == 1 ? TFieldType.ftString : base.GetColumnBDEFieldType(ColumnIndex);

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) =>
        ColumnIndex == 1 ? SysUtils.Format(SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForString, 63) : base.GetColumnTypeAsSQL(ColumnIndex);

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnCount() => 2;

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnSize(int ColumnIndex) => ColumnIndex == 1 ? 63 : base.GetColumnSize(ColumnIndex);

    [Delphi(PortStatus.Ported)]
    protected override string GetInitialColumnName(int ColumnIndex) => ColumnIndex == 1 ? InitialColumnRootName + "_Content" : base.GetInitialColumnName(ColumnIndex);

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        if (ColumnIndex == 1)
        {
            var aTypedBlob = (IBoldTypedBlob)GetValue(ObjectContent)!;
            return CheckEitherNull(Field, aTypedBlob, out bool result) ? result : Equals(Field.Value, aTypedBlob.ContentTypeContent);
        }

        return base.CompareField(ObjectContent, Field, ColumnIndex, ValueSpace, TranslationList);
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_TypedBlob) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        if (ColumnIndex == 1)
        {
            var aTypedBlob = (IBoldTypedBlob)GetEnsuredValue(ObjectContent)!;
            if (Field.IsNull)
            {
                aTypedBlob.SetContentToNull();
            }
            else
            {
                aTypedBlob.ContentTypeContent = Field.AsString;
            }
        }
        else
        {
            base.ValueFromField(OwningObjectId, ObjectContent, ValueSpace, TranslationList, Field, ColumnIndex);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        if (ColumnIndex == 1)
        {
            var aTypedBlob = (IBoldTypedBlob)GetEnsuredValue(ObjectContent)!;
            if (aTypedBlob.IsNull)
            {
                SetParamToNullWithDataType(Param, GetColumnBDEFieldType(ColumnIndex));
            }
            else
            {
                Param.AsString = aTypedBlob.ContentTypeContent;
            }
        }
        else
        {
            base.ValueToParam(ObjectContent, Param, ColumnIndex, TranslationList);
        }
    }
}

/// <summary>A typed blob fetched by itself with a live query.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMLiveTypedBlob : TBoldPMTypedBlob
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldPMLiveTypedBlob.CreateFromMold")]
    public TBoldPMLiveTypedBlob(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        fCustomFetch = true;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetRequiresLiveQuery() => true;
}

/// <summary>A date-time column; compared within two seconds.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMDateTime : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMDateTime(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftDateTime;

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForDateTime;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDateTime = (IBoldDateTimeContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aDateTime, out bool result) ? result : (Field.AsDateTime - aDateTime.asDateTime).Duration() < BoldPMappersAttributeDefault.TwoSeconds;
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_DateTime) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDateTime = (IBoldDateTimeContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aDateTime.SetContentToNull();
        }
        else
        {
            aDateTime.asDateTime = Field.AsDateTime;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDateTime = (IBoldDateTimeContent)GetEnsuredValue(ObjectContent)!;
        if (aDateTime.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsDateTime = aDateTime.asDateTime;
        }
    }
}

/// <summary>A date column; compared by day.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMDate : TBoldPMDateTime
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMDate(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftDate;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDate = (IBoldDateContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aDate, out bool result) ? result : Field.AsDate.Date == aDate.asDate.Date;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForDate;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Date) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDate = (IBoldDateContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aDate.SetContentToNull();
        }
        else
        {
            aDate.asDate = Field.AsDateTime;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aDate = (IBoldDateContent)GetEnsuredValue(ObjectContent)!;
        if (aDate.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsDate = aDate.asDate;
        }
    }
}

/// <summary>A time column; compared within two seconds.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMTime : TBoldPMDateTime
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldPMTime(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftTime;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aTime = (IBoldTimeContent)GetValue(ObjectContent)!;
        return CheckEitherNull(Field, aTime, out bool result) ? result : (Field.AsTime - aTime.asTime).Duration() < BoldPMappersAttributeDefault.TwoSeconds;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForTime;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_Time) == 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        EnsureFirstColumn(ColumnIndex);
        var aTime = (IBoldTimeContent)GetEnsuredValue(ObjectContent)!;
        if (Field.IsNull)
        {
            aTime.SetContentToNull();
        }
        else
        {
            aTime.asTime = Field.AsDateTime;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        EnsureFirstColumn(ColumnIndex);
        var aTime = (IBoldTimeContent)GetEnsuredValue(ObjectContent)!;
        if (aTime.IsNull)
        {
            SetParamToNullWithDataType(Param, GetColumnBDEFieldType(0));
        }
        else
        {
            Param.AsTime = aTime.asTime;
        }
    }
}
