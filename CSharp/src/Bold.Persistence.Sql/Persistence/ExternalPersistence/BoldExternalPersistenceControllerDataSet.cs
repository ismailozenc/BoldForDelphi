// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceControllerDataSet.pas
// Port map: CSharp/port/units/BoldExternalPersistenceControllerDataSet.md
// The partially external controller whose external storage is a data set per class: rows are found by the external
// key columns, members are read from and written to the columns, and links are followed through the other class's
// key columns. Delphi's variants are .NET values here (null for Null, an object array for several keys).

namespace Bold;

/// <summary>Unit-level routines of BoldExternalPersistenceControllerDataSet.</summary>
public static class BoldExternalPersistenceControllerDataSet
{
    [Delphi(PortStatus.Ported)]
    internal static int MemberIndexByName(TMoldClass MoldClass, string MemberName)
    {
        for (int Result = 0; Result < MoldClass.AllBoldMembers.Count; Result++)
        {
            if (SysUtils.SameText(MoldClass.AllBoldMembers[Result].ExpandedExpressionName, MemberName))
            {
                return Result;
            }
        }

        return -1;
    }

    [Delphi(PortStatus.Ported)]
    internal static int FindExternalKey(TMoldClass MoldClass)
    {
        for (int Result = 0; Result < MoldClass.AllBoldMembers.Count; Result++)
        {
            if (MoldClass.AllBoldMembers[Result].Storage == TBoldStorage.bsExternalKey)
            {
                return Result;
            }
        }

        return -1;
    }

    /// <summary>The member's column name, without a leading @.</summary>
    [Delphi(PortStatus.Ported)]
    internal static string RemovePreAt(TMoldMember Member)
    {
        string result = BoldNameExpander.BoldExpandName(Member.ColumnName, Member.Name, TExpansionType.xtSQL, -1, Member.MoldClass!.Model.NationalCharConversion);
        return result.StartsWith('@') ? result[1..] : result;
    }

    /// <summary>The key columns of the class, separated by semicolons.</summary>
    [Delphi(PortStatus.Ported)]
    internal static string FindExternalKeyColumns(TMoldClass MoldClass)
    {
        var Columns = new List<string>();
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                Columns.Add(RemovePreAt(MoldClass.AllBoldMembers[i]));
            }
        }

        return string.Join(";", Columns);
    }

    [Delphi(PortStatus.Ported)]
    internal static int GetCharCount(char C, string S) => S.Count(ch => ch == C);

    /// <summary>The text up to the splitter, which is removed from S with it.</summary>
    [Delphi(PortStatus.Ported)]
    internal static string GetNextWord(ref string S, char Splitter)
    {
        int p = S.IndexOf(Splitter);
        string Result;
        if (p >= 0)
        {
            Result = S[..p];
            S = S[(p + 1)..];
        }
        else
        {
            Result = S;
            S = "";
        }

        return Result;
    }

    /// <summary>The value of the field, or an array of the values of the fields separated by semicolons.</summary>
    [Delphi(PortStatus.Ported)]
    internal static object? GetDataValue(TDataSet DataSet, string FieldNames)
    {
        int FieldCount = GetCharCount(';', FieldNames) + 1;
        if (FieldCount == 1)
        {
            return DataSet.FieldByName(FieldNames).Value;
        }

        string S = FieldNames;
        var Result = new object?[FieldCount];
        for (int i = 0; i < FieldCount; i++)
        {
            Result[i] = DataSet.FieldByName(GetNextWord(ref S, ';')).Value;
        }

        return Result;
    }

    [Delphi(PortStatus.Ported)]
    internal static string VarArrayToStr(object?[] V) => string.Join(";", V.Select(VarToStr));

    private static string VarToStr(object? v) => v is null ? "" : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "";

    /// <summary>An array as its values joined by semicolons; anything else as it is.</summary>
    [Delphi(PortStatus.Ported)]
    internal static object? NormalizeVariant(object? V) => V is object?[] Array ? VarArrayToStr(Array) : V;

    /// <summary>The value as a .NET value: null when null, else the content's value.</summary>
    [Delphi(PortStatus.Ported)]
    internal static object? BoldValueToVariant(IBoldValue? B) => B switch
    {
        IBoldNullableValue { IsNull: true } => null,
        IBoldStringContent S => S.asString,
        IBoldIntegerContent I => I.asInteger,
        IBoldFloatContent F => F.asFloat,
        IBoldCurrencyContent C => C.asCurrency,
        IBoldBooleanContent Bool => Bool.asBoolean,
        IBoldDateContent D => D.asDate,
        IBoldTimeContent T => T.asTime,
        IBoldDateTimeContent DT => DT.asDateTime,
        IBoldBlobContent BL => BL.asBlob,
        _ => throw new EBold(ExPeConsts.sUnknownDataType),
    };

    /// <summary>Sets the value from a .NET value (null sets a nullable value to null).</summary>
    [Delphi(PortStatus.Ported)]
    internal static void VariantToBoldValue(IBoldValue B, object? Value)
    {
        var Invariant = System.Globalization.CultureInfo.InvariantCulture;
        switch (B)
        {
            case IBoldNullableValue Nullable when Value is null or DBNull:
                Nullable.SetContentToNull();
                break;
            case IBoldStringContent S:
                S.asString = Convert.ToString(Value, Invariant) ?? "";
                break;
            case IBoldIntegerContent I:
                I.asInteger = Convert.ToInt32(Value, Invariant);
                break;
            case IBoldFloatContent F:
                F.asFloat = Convert.ToDouble(Value, Invariant);
                break;
            case IBoldCurrencyContent C:
                C.asCurrency = Convert.ToDecimal(Value, Invariant);
                break;
            case IBoldBooleanContent Bool:
                Bool.asBoolean = Convert.ToBoolean(Value, Invariant);
                break;
            case IBoldDateContent D:
                D.asDate = Convert.ToDateTime(Value, Invariant);
                break;
            case IBoldTimeContent T:
                T.asTime = Convert.ToDateTime(Value, Invariant);
                break;
            case IBoldDateTimeContent DT:
                DT.asDateTime = Convert.ToDateTime(Value, Invariant);
                break;
            case IBoldBlobContent BL:
                BL.asBlob = Value is byte[] bytes ? System.Text.Encoding.Latin1.GetString(bytes) : Convert.ToString(Value, Invariant) ?? "";
                break;
            default:
                throw new EBold(ExPeConsts.sUnknownDataType);
        }
    }

    /// <summary>Sets the value to null, or to its type's empty value when it cannot be null.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void SetBoldValueToNull(IBoldValue B)
    {
        switch (B)
        {
            case IBoldNullableValue Nullable:
                Nullable.SetContentToNull();
                break;
            default:
                throw new EBold(ExPeConsts.sUnknownDataType);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal static int GetKeyCount(TMoldClass MoldClass)
    {
        int Result = 0;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                Result++;
            }
        }

        return Result;
    }

    /// <summary>The value of the key member, an array of the values of several, or null when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    internal static object? GetObjectKeys(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        int KeyCount = GetKeyCount(MoldClass);
        if (KeyCount == 0)
        {
            return null;
        }

        var Keys = new List<object?>();
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                object? Value = BoldValueToVariant(ObjectContents.ValueByIndex(i));
                if (KeyCount == 1)
                {
                    return Value;
                }

                Keys.Add(Value);
            }
        }

        return Keys.ToArray();
    }

    /// <summary>A default id for a number, a string id for text; other values cannot be keys.</summary>
    internal static TBoldObjectId? IdFromValue(object? DBValue) => DBValue switch
    {
        int or short or long or byte or float or double or decimal => new TBoldDefaultID { AsInteger = Convert.ToInt32(DBValue, System.Globalization.CultureInfo.InvariantCulture) },
        string s => new TBoldStringID { AsString = s },
        _ => null,
    };
}

/// <summary>A partially external controller over a data set per class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceControllerDataSet : TBoldAbstractPartiallyExternalPC
{
    private readonly TBoldExternalPersistenceConfigDataSetItems FConfig;
    private readonly int fMaxFetchBlockSize;

    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceControllerDataSet(TMoldModel MoldModel, TBoldExternalPersistenceConfigDataSetItems Config, TBoldTypeNameDictionary TypeNameDictionary,
        TNotifyEvent? OnStartUpdates, TNotifyEvent? OnEndUpdates, TNotifyEvent? OnFailUpdates, int MaxFetchBlockSize)
        : base(MoldModel, TypeNameDictionary, OnStartUpdates, OnEndUpdates, OnFailUpdates)
    {
        FConfig = Config;
        fMaxFetchBlockSize = MaxFetchBlockSize;
    }

    /// <summary>Whether the member is written to and read from the row: not derived, and not a multi or non-embedded link.</summary>
    private static bool IsStoredInRow(TMoldMember Member) => !Member.Derived && !(Member is TMoldRole Role && (Role.Multi || !Role.Embed));

    private static string ColumnNameOf(TMoldMember Member) =>
        BoldNameExpander.BoldExpandName(Member.ColumnName, Member.Name, TExpansionType.xtSQL, -1, Member.MoldClass!.Model.NationalCharConversion);

    /// <summary>Inserts a row with the external and key members of each object.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void CreateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (ConfigItemByObjectContents(ObjectContents) is { DataSet: { } DataSet })
            {
                DataSet.Insert();
                for (int j = 0; j < MoldClass.AllBoldMembers.Count; j++)
                {
                    TMoldMember Member = MoldClass.AllBoldMembers[j];
                    if (Member.Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && IsStoredInRow(Member))
                    {
                        DataSet[ColumnNameOf(Member)] = BoldExternalPersistenceControllerDataSet.BoldValueToVariant(ObjectContents.ValueByIndex(j));
                    }
                }

                DataSet.Post();
            }
        }
    }

    /// <summary>Deletes the row of each object.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DeleteObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            LocateInDB(MoldModel.Classes[ObjectIdList[i].TopSortedIndex], ObjectContents)?.Delete();
        }
    }

    /// <summary>Reads the external members (all, or the asked ones) from the object's row; a read-only data set makes the object read-only.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested SyncMember is a private method. A multi link scans the other data set for matching rows instead of setting a filter expression (whose text keys Delphi left unquoted).")]
    protected override void FetchObject(IBoldObjectContents ObjectContents, TBoldMemberIdList? MemberIdList, object? FetchContext, IBoldValueSpace ValueSpace)
    {
        TBoldExternalPersistenceConfigDataSetItem? ConfigItem = ConfigItemByObjectContents(ObjectContents);
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        TDataSet? DataSet = LocateInDB(MoldClass, ObjectContents);
        if (DataSet is null || ConfigItem is null)
        {
            return;
        }

        if (MemberIdList is not null)
        {
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                if (MoldClass.AllBoldMembers[MemberIdList[i].MemberIndex].Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey)
                {
                    SyncMember(ObjectContents, ConfigItem, MoldClass, MemberIdList[i].MemberIndex);
                }
            }
        }
        else
        {
            for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
            {
                if (MoldClass.AllBoldMembers[i].Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && IsStoredInRow(MoldClass.AllBoldMembers[i]))
                {
                    SyncMember(ObjectContents, ConfigItem, MoldClass, i);
                }
            }
        }

        if (!DataSet.CanModify)
        {
            ObjectContents.IsReadOnly = true;
        }
    }

    /// <summary>
    /// Reads one member from the current row: an attribute from its column, a single link from its key columns, a
    /// multi link from the other class's rows whose link columns hold this row's keys. (SyncMember, nested in Delphi's
    /// FetchObject.)
    /// </summary>
    private void SyncMember(IBoldObjectContents ObjectContents, TBoldExternalPersistenceConfigDataSetItem ConfigItem, TMoldClass MoldClass, int MemberIndex)
    {
        IBoldValue Value = ObjectContents.ValueByIndex(MemberIndex)!;
        string DBFieldName = BoldExternalPersistenceControllerDataSet.RemovePreAt(MoldClass.AllBoldMembers[MemberIndex]);
        TDataSet DataSet = ConfigItem.DataSet!;
        if (MoldClass.AllBoldMembers[MemberIndex] is TMoldRole MoldRole)
        {
            TMoldClass OtherClass = MoldRole.OtherEnd!.MoldClass!;
            if (MoldRole.Multi)
            {
                TBoldExternalPersistenceConfigDataSetItem MultiLinkConfigItem = Config.FindExpressionName(OtherClass.ExpandedExpressionName)
                    ?? throw new EBold(SysUtils.Format(ExPeConsts.sLinkToUnconfiguredTable, OtherClass.ExpandedExpressionName));
                TDataSet MultiLinkDataSet = MultiLinkConfigItem.DataSet!;
                string[] LinkColumns = BoldExternalPersistenceControllerDataSet.RemovePreAt(MoldRole.OtherEnd).Split(';');
                string[] KeyColumns = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(MoldClass).Split(';');
                string[] Keys = Array.ConvertAll(KeyColumns, c => DataSet.FieldByName(c).AsString);
                string OtherKeyColumns = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(OtherClass);
                var MultiLinkList = new TBoldObjectIdList();
                MultiLinkDataSet.First();
                while (!MultiLinkDataSet.Eof)
                {
                    bool Matches = true;
                    for (int i = 0; i < LinkColumns.Length && Matches; i++)
                    {
                        Matches = MultiLinkDataSet.FieldByName(LinkColumns[i]).AsString == Keys[i];
                    }

                    if (Matches)
                    {
                        object? DBValue = BoldExternalPersistenceControllerDataSet.NormalizeVariant(BoldExternalPersistenceControllerDataSet.GetDataValue(MultiLinkDataSet, OtherKeyColumns));
                        if (DBValue is not null)
                        {
                            // the type of the field MUST match the type of the internal ID
                            MultiLinkList.Add(BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue)
                                ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingID, MoldClass.Name, MoldRole.Name)));
                        }
                    }

                    MultiLinkDataSet.Next();
                }

                if (Value is IBoldObjectIdListRef IDRefList)
                {
                    SetMultiLink(IDRefList, MultiLinkList, OtherClass);
                }
            }
            else
            {
                object? DBValue = BoldExternalPersistenceControllerDataSet.NormalizeVariant(BoldExternalPersistenceControllerDataSet.GetDataValue(DataSet, DBFieldName));
                if (DBValue is not null)
                {
                    TBoldObjectId ExternalKey = BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue)
                        ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingSingleID, MoldClass.Name, MoldRole.Name));
                    if (Value is IBoldObjectIdRef IDRef)
                    {
                        SetSingleLink(IDRef, ExternalKey, OtherClass);
                    }
                }
            }
        }
        else if (DataSet.FieldByName(DBFieldName).IsNull)
        {
            BoldExternalPersistenceControllerDataSet.SetBoldValueToNull(Value);
        }
        else
        {
            BoldExternalPersistenceControllerDataSet.VariantToBoldValue(Value, DataSet.FieldByName(DBFieldName).Value);
        }
    }

    /// <summary>The keys of all rows of the class's data set.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void GetExternalKeys(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        TDataSet DataSet = Config.FindExpressionName(MoldClass.ExpandedExpressionName)!.DataSet!;
        DataSet.First();
        string DBFieldName = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(MoldClass);
        while (!DataSet.Eof)
        {
            object? DBValue = BoldExternalPersistenceControllerDataSet.NormalizeVariant(BoldExternalPersistenceControllerDataSet.GetDataValue(DataSet, DBFieldName));
            ExternalKeys.Add(BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue)
                ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingObject, MoldClass.Name)));
            DataSet.Next();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool HandlesClass(TMoldClass MoldClass) => true;

    [Delphi(PortStatus.Ported, Note = "Locates by the key's parts; Delphi passed the joined key string, which only matches a single key.")]
    protected override bool ExternalKeyExistsInExternalStorage(TMoldClass MoldClass, TBoldObjectId ExternalKey)
    {
        string Columns = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(MoldClass);
        string[] Parts = ExternalKey.AsString.Split(';');
        return Config.FindExpressionName(MoldClass.ExpandedExpressionName)!.DataSet!.Locate(Columns, Parts.Length == 1 ? Parts[0] : Parts.Cast<object?>().ToArray(), new DelphiSet<TLocateOption>());
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldExternalPersistenceConfigDataSetItem? ConfigItemByObjectContents(IBoldObjectContents ObjectContents) =>
        Config.FindExpressionName(MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex].ExpandedExpressionName);

    /// <summary>Writes the external members stored in the row of each object.</summary>
    [Delphi(PortStatus.Ported, Note = "Writes only the external and key members, and posts the edited row; Delphi wrote internal members too, without Edit and Post.")]
    protected override void UpdateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (ConfigItemByObjectContents(ObjectContents) is not null && LocateInDB(MoldClass, ObjectContents) is { } DataSet)
            {
                DataSet.Edit();
                for (int j = 0; j < MoldClass.AllBoldMembers.Count; j++)
                {
                    TMoldMember Member = MoldClass.AllBoldMembers[j];
                    if (Member.Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && IsStoredInRow(Member))
                    {
                        DataSet[ColumnNameOf(Member)] = BoldExternalPersistenceControllerDataSet.BoldValueToVariant(ObjectContents.ValueByIndex(j));
                    }
                }

                DataSet.Post();
            }
        }
    }

    /// <summary>The object's key as a default id (a number) or a string id (text, or several keys joined by semicolons).</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectId GetExternalKeyFromObject(IBoldObjectContents ObjectContents, IBoldValueSpace ValueSpace)
    {
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        object? DBValue = BoldExternalPersistenceControllerDataSet.NormalizeVariant(BoldExternalPersistenceControllerDataSet.GetObjectKeys(MoldClass, ObjectContents));
        return BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue) ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingObject, MoldClass.Name));
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetMaxFetchBlockSize() => fMaxFetchBlockSize;

    /// <summary>One key: the generated fragment; several: an OR of the key column comparisons of each key.</summary>
    [Delphi(PortStatus.Ported, Note = "Includes the first key; Delphi's loop started at the second. Quotes embedded quotes in text keys.")]
    protected override string ExternalKeysToInternalSQL(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        string ExternalColumns = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(MoldClass);
        if (BoldExternalPersistenceControllerDataSet.GetCharCount(';', ExternalColumns) == 0)
        {
            return base.ExternalKeysToInternalSQL(MoldClass, ExternalKeys);
        }

        var Conditions = new List<string>();
        for (int i = 0; i < ExternalKeys.Count; i++)
        {
            string ExternalKey = ExternalKeys[i].AsString;
            string S = ExternalColumns;
            var Parts = new List<string>();
            for (int j = 0; j <= BoldExternalPersistenceControllerDataSet.GetCharCount(';', ExternalKeys[i].AsString); j++)
            {
                string T = BoldExternalPersistenceControllerDataSet.GetNextWord(ref ExternalKey, ';');
                if (!int.TryParse(T, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    T = "'" + T.Replace("'", "''", StringComparison.Ordinal) + "'";
                }

                Parts.Add("(" + BoldExternalPersistenceControllerDataSet.GetNextWord(ref S, ';') + " = " + T + ")");
            }

            Conditions.Add("(" + string.Join(" AND ", Parts) + ")");
        }

        return string.Join(" OR ", Conditions);
    }

    /// <summary>Writes the key's parts (separated by semicolons) to the string or integer key members.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void AssignKeyToObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents, TBoldObjectId ExternalKey, IBoldValueSpace ValueSpace)
    {
        string S = ExternalKey.AsString;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                switch (ObjectContents.ValueByIndex(i))
                {
                    case IBoldStringContent StrContent:
                        StrContent.asString = BoldExternalPersistenceControllerDataSet.GetNextWord(ref S, ';');
                        break;
                    case IBoldIntegerContent IntContent:
                        IntContent.asInteger = SysUtils.StrToInt(BoldExternalPersistenceControllerDataSet.GetNextWord(ref S, ';'));
                        break;
                    default:
                        throw new EBold(SysUtils.Format(ExPeConsts.sKeyTypeNotAutoHandled, MoldClass.Name, MoldClass.AllBoldMembers[i].Name));
                }
            }
        }
    }

    /// <summary>The class's data set positioned on the object's row, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected TDataSet? LocateInDB(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        string ExternalKeys = BoldExternalPersistenceControllerDataSet.FindExternalKeyColumns(MoldClass);
        if (ConfigItemByObjectContents(ObjectContents) is { DataSet: { } DataSet } && ExternalKeys != "")
        {
            object? ExternalKeyValues = BoldExternalPersistenceControllerDataSet.GetObjectKeys(MoldClass, ObjectContents);
            if (DataSet.Locate(ExternalKeys, ExternalKeyValues, new DelphiSet<TLocateOption>()))
            {
                return DataSet;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceConfigDataSetItems Config => FConfig;
}
