// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceControllerSQL.pas
// Port map: CSharp/port/units/BoldExternalPersistenceControllerSQL.md
// The partially external controller whose external storage is another SQL database: a table per class, rows found
// by the external key columns, members read with one SELECT per block of objects and written with generated
// INSERT, UPDATE and DELETE statements. Delphi's variants are .NET values here (null for Null).

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceControllerSQL", Symbol = "TBoldExternalPersistenceControllerSQL.Destroy", Note = "Freed the class list; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceControllerSQL", Symbol = "TFetchContext.Destroy", Note = "Freed the fetch objects; PostFetch releases the query and the garbage collector the rest.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldExternalPersistenceControllerSQL", Symbol = "TFetchObjectClass", Note = "A System.Type of the fetch object class.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldQueryMode { bqmSelect, bqmUpdate, bqmInsert, bqmDelete }

/// <summary>Unit-level routines of BoldExternalPersistenceControllerSQL.</summary>
public static class BoldExternalPersistenceControllerSQL
{
    [Delphi(PortStatus.Ported)]
    internal static string _GetTableName(TMoldClass MoldClass) =>
        BoldNameExpander.BoldExpandName(MoldClass.TableName, MoldClass.Name, TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccFalse);

    [Delphi(PortStatus.Ported)]
    internal static string _GetColumnName(TMoldMember BoldMember) =>
        BoldNameExpander.BoldExpandName(BoldMember.ColumnName, BoldMember.Name, TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccFalse);

    [Delphi(PortStatus.Ported)]
    internal static int MemberIndexByName(TMoldClass MoldClass, string MemberName) => BoldExternalPersistenceControllerDataSet.MemberIndexByName(MoldClass, MemberName);

    [Delphi(PortStatus.Ported)]
    internal static int FindExternalKey(TMoldClass MoldClass) => BoldExternalPersistenceControllerDataSet.FindExternalKey(MoldClass);

    /// <summary>The member's column name, without a leading @ or _.</summary>
    [Delphi(PortStatus.Ported)]
    internal static string RemovePreAt(TMoldMember Member)
    {
        string result = BoldNameExpander.BoldExpandName(Member.ColumnName, Member.Name, TExpansionType.xtSQL, -1, Member.MoldClass!.Model.NationalCharConversion);
        return result.Length > 0 && result[0] is '@' or '_' ? result[1..] : result;
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
    internal static int GetCharCount(char C, string S) => BoldExternalPersistenceControllerDataSet.GetCharCount(C, S);

    [Delphi(PortStatus.Ported)]
    internal static string GetNextWord(ref string S, char Splitter) => BoldExternalPersistenceControllerDataSet.GetNextWord(ref S, Splitter);

    [Delphi(PortStatus.Ported)]
    internal static object? GetDataValue(TDataSet DataSet, string FieldNames) => BoldExternalPersistenceControllerDataSet.GetDataValue(DataSet, FieldNames);

    [Delphi(PortStatus.Ported)]
    internal static string VarArrayToStr(object?[] V) => BoldExternalPersistenceControllerDataSet.VarArrayToStr(V);

    [Delphi(PortStatus.Ported)]
    internal static object? NormalizeVariant(object? V) => BoldExternalPersistenceControllerDataSet.NormalizeVariant(V);

    [Delphi(PortStatus.Ported)]
    internal static object? BoldValueToVariant(IBoldValue? B) => BoldExternalPersistenceControllerDataSet.BoldValueToVariant(B);

    [Delphi(PortStatus.Ported)]
    internal static void VariantToBoldValue(IBoldValue B, object? Value) => BoldExternalPersistenceControllerDataSet.VariantToBoldValue(B, Value);

    [Delphi(PortStatus.Ported)]
    internal static void SetBoldValueToNull(IBoldValue B) => BoldExternalPersistenceControllerDataSet.SetBoldValueToNull(B);

    [Delphi(PortStatus.Ported)]
    internal static int GetKeyCount(TMoldClass MoldClass) => BoldExternalPersistenceControllerDataSet.GetKeyCount(MoldClass);

    [Delphi(PortStatus.Ported)]
    internal static object? GetObjectKeys(TMoldClass MoldClass, IBoldObjectContents ObjectContents) => BoldExternalPersistenceControllerDataSet.GetObjectKeys(MoldClass, ObjectContents);

    /// <summary>Adds the key members of the class.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void GetExternalKeyMembers(TMoldClass MoldClass, TBoldMemberIdList MemberIdList)
    {
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                MemberIdList.Add(new TBoldMemberID(i));
            }
        }
    }

    /// <summary>The text in quotes unless it is a number.</summary>
    [Delphi(PortStatus.Ported, Note = "Doubles embedded quotes.")]
    internal static string QuoteStringIfNeeded(string S) =>
        double.TryParse(S, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) ? S : "'" + S.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>Whether the member is written to the row: not derived, and not a multi or non-embedded link.</summary>
    internal static bool IsStoredInRow(TMoldMember Member) => !Member.Derived && !(Member is TMoldRole Role && (Role.Multi || !Role.Embed));
}

/// <summary>A partially external controller over the tables of another SQL database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceControllerSQL : TBoldAbstractPartiallyExternalPC
{
    private readonly TBoldAbstractDatabaseAdapter FDatabaseAdapter;
    private readonly TStringList FClassesToHandle = new();

    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceControllerSQL(TMoldModel MoldModel, TBoldAbstractDatabaseAdapter ADatabaseAdapter, TBoldTypeNameDictionary TypeNameDictionary,
        TNotifyEvent? OnStartUpdates, TNotifyEvent? OnEndUpdates, TNotifyEvent? OnFailUpdates, TStrings AClassesToHandle)
        : base(MoldModel, TypeNameDictionary, OnStartUpdates, OnEndUpdates, OnFailUpdates)
    {
        FDatabaseAdapter = ADatabaseAdapter;
        FClassesToHandle.Assign(AClassesToHandle);
    }

    [Delphi(PortStatus.Ported)]
    protected override void CreateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            InternalCreateObject(MoldModel.Classes[ObjectIdList[i].TopSortedIndex], ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void DeleteObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            InternalDeleteObject(MoldModel.Classes[ObjectIdList[i].TopSortedIndex], ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!);
        }
    }

    /// <summary>
    /// Selects the rows of the keys, with the asked members or (by default) the external ones that are not derived,
    /// multi links or marked DelayedFetch.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void PrepareFetchExternal(TBoldObjectIdList ExternalKeys, IBoldValueSpace ValueSpace, TMoldClass MoldClass, TBoldMemberIdList? MemberIdList,
        ref object? FetchContext)
    {
        TBoldMemberIdList FetchMembers;
        if (MemberIdList is { Count: > 0 })
        {
            FetchMembers = MemberIdList;
        }
        else
        {
            FetchMembers = new TBoldMemberIdList();
            for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
            {
                TMoldMember Member = MoldClass.AllBoldMembers[i];
                if (Member.Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && !Member.Derived && !(Member is TMoldRole { Multi: true }) &&
                    !SysUtils.SameText(Member.TVByName("DelayedFetch"), "True"))
                {
                    FetchMembers.Add(new TBoldMemberID(i));
                }
            }
        }

        FetchContext = new TFetchContext(this, ExternalKeys, ValueSpace, MoldClass, FetchMembers);
    }

    [Delphi(PortStatus.Ported)]
    protected override void PostFetch(object? FetchContext, TMoldClass MoldClass)
    {
        (FetchContext as TFetchContext)?.PostFetch();
        base.PostFetch(FetchContext, MoldClass);
    }

    [Delphi(PortStatus.Ported)]
    protected override void FetchObject(IBoldObjectContents ObjectContents, TBoldMemberIdList? MemberIdList, object? FetchContext, IBoldValueSpace ValueSpace) =>
        ((TFetchContext)FetchContext!).FetchObject(ObjectContents);

    /// <summary>The keys of all rows of the class's table.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void GetExternalKeys(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        string DBFieldName = BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(MoldClass);
        IBoldQuery? BoldQuery = DatabaseAdapter.DatabaseInterface.GetQuery();
        try
        {
            BoldQuery.AssignSQLText(SysUtils.Format("SELECT %S FROM %S", DBFieldName.Replace(";", ", ", StringComparison.Ordinal), BoldExternalPersistenceControllerSQL._GetTableName(MoldClass)));
            BoldQuery.Open();
            while (!BoldQuery.Eof)
            {
                object? DBValue = BoldExternalPersistenceControllerSQL.NormalizeVariant(BoldExternalPersistenceControllerSQL.GetDataValue(BoldQuery.AsDataSet, DBFieldName));
                ExternalKeys.Add(BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue)
                    ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingObject, MoldClass.Name)));
                BoldQuery.Next();
            }

            BoldQuery.Close();
        }
        finally
        {
            DatabaseAdapter.DatabaseInterface.ReleaseQuery(ref BoldQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool HandlesClass(TMoldClass MoldClass) => FClassesToHandle.IndexOf(MoldClass.ExpandedExpressionName) != -1;

    [Delphi(PortStatus.Ported)]
    protected override bool ExternalKeyExistsInExternalStorage(TMoldClass MoldClass, TBoldObjectId ExternalKey)
    {
        IBoldQuery? BoldQuery = DatabaseAdapter.DatabaseInterface.GetQuery();
        try
        {
            string DBFieldName = BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(MoldClass);
            var ExternalKeys = new TBoldObjectIdList();
            ExternalKeys.Add(ExternalKey);
            BoldQuery.AssignSQLText(SysUtils.Format("SELECT %S FROM %S WHERE %S", DBFieldName.Replace(";", ", ", StringComparison.Ordinal),
                BoldExternalPersistenceControllerSQL._GetTableName(MoldClass), ExternalKeysToInternalSQL(MoldClass, ExternalKeys)));
            BoldQuery.Open();
            return !BoldQuery.AsDataSet.IsEmpty;
        }
        finally
        {
            DatabaseAdapter.DatabaseInterface.ReleaseQuery(ref BoldQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToPeristenceEvents(TBoldSubscriber Subscriber) => base.SubscribeToPeristenceEvents(Subscriber);

    [Delphi(PortStatus.Ported)]
    protected override void UpdateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            InternalUpdateObject(MoldModel.Classes[ObjectIdList[i].TopSortedIndex], ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!);
        }
    }

    /// <summary>The object's key as a default id (a number) or a string id (text, or several keys joined by semicolons).</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectId GetExternalKeyFromObject(IBoldObjectContents ObjectContents, IBoldValueSpace ValueSpace)
    {
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        object? DBValue = BoldExternalPersistenceControllerSQL.NormalizeVariant(BoldExternalPersistenceControllerSQL.GetObjectKeys(MoldClass, ObjectContents));
        return BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue) ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingObject, MoldClass.Name));
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetMaxFetchBlockSize() => DatabaseAdapter.SQLDatabaseConfig.FetchBlockSize;

    /// <summary>One key: the generated fragment; several: an OR of the key column comparisons of each key.</summary>
    [Delphi(PortStatus.Ported, Note = "Doubles embedded quotes in text keys.")]
    protected override string ExternalKeysToInternalSQL(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        string ExternalColumns = BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(MoldClass);
        if (BoldExternalPersistenceControllerSQL.GetCharCount(';', ExternalColumns) == 0)
        {
            return base.ExternalKeysToInternalSQL(MoldClass, ExternalKeys);
        }

        var Conditions = new List<string>();
        for (int i = 0; i < ExternalKeys.Count; i++)
        {
            string ExternalKey = ExternalKeys[i].AsString;
            string S = ExternalColumns;
            var Parts = new List<string>();
            for (int j = 0; j <= BoldExternalPersistenceControllerSQL.GetCharCount(';', ExternalKeys[i].AsString); j++)
            {
                string T = BoldExternalPersistenceControllerSQL.GetNextWord(ref ExternalKey, ';');
                if (!int.TryParse(T, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    T = "'" + T.Replace("'", "''", StringComparison.Ordinal) + "'";
                }

                Parts.Add("(" + BoldExternalPersistenceControllerSQL.GetNextWord(ref S, ';') + " = " + T + ")");
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
                        StrContent.asString = BoldExternalPersistenceControllerSQL.GetNextWord(ref S, ';');
                        break;
                    case IBoldIntegerContent IntContent:
                        IntContent.asInteger = SysUtils.StrToInt(BoldExternalPersistenceControllerSQL.GetNextWord(ref S, ';'));
                        break;
                    default:
                        throw new EBold(SysUtils.Format(ExPeConsts.sKeyTypeNotAutoHandled, MoldClass.Name, MoldClass.AllBoldMembers[i].Name));
                }
            }
        }
    }

    /// <summary>Moves the query to the object's row; false if it is not there.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool LocateInDB(TMoldClass MoldClass, IBoldObjectContents ObjectContents, IBoldQuery BoldQuery)
    {
        string ExternalKeys = BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(MoldClass);
        return ExternalKeys != "" &&
            BoldQuery.AsDataSet.Locate(ExternalKeys, BoldExternalPersistenceControllerSQL.GetObjectKeys(MoldClass, ObjectContents), new DelphiSet<TLocateOption>());
    }

    /// <summary>Sets the parameter named after each member's column to the member's value.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AssignParametersWithMemberIdList(TMoldClass MoldClass, IBoldObjectContents ObjectContents, IBoldParameterized Query, TBoldMemberIdList MemberIdList)
    {
        for (int i = 0; i < MemberIdList.Count; i++)
        {
            IBoldValue? BoldValue = ObjectContents.ValueByMemberId(MemberIdList[i]);
            string ParamName = BoldExternalPersistenceControllerSQL._GetColumnName(MoldClass.AllBoldMembers[MemberIdList[i].MemberIndex]);
            Query.ParamByName(ParamName).asVariant = BoldExternalPersistenceControllerSQL.BoldValueToVariant(BoldValue);
        }
    }

    /// <summary>Inserts the object's row with its modified external members.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void InternalCreateObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        var MemberIdList = new TBoldMemberIdList();
        IBoldExecQuery? BoldExecQuery = DatabaseAdapter.DatabaseInterface.GetExecQuery();
        try
        {
            GetExternalDirtyMembers(MoldClass, ObjectContents, MemberIdList);
            BoldExecQuery.AssignSQLText(GenerateInsertSQL(MoldClass, ObjectContents, MemberIdList));
            AssignParametersWithMemberIdList(MoldClass, ObjectContents, (IBoldParameterized)BoldExecQuery, MemberIdList);
            BoldExecQuery.ExecSQL();
        }
        finally
        {
            DatabaseAdapter.DatabaseInterface.ReleaseExecQuery(ref BoldExecQuery);
        }
    }

    /// <summary>INSERT INTO the class's table, with a parameter per member.</summary>
    [Delphi(PortStatus.Ported, Note = "Uses the columns of the listed members; Delphi used the first members of the class instead.")]
    protected virtual string GenerateInsertSQL(TMoldClass MoldClass, IBoldObjectContents ObjectContents, TBoldMemberIdList MemberIdList)
    {
        var Columns = new List<string>();
        for (int i = 0; i < MemberIdList.Count; i++)
        {
            Columns.Add(BoldExternalPersistenceControllerSQL._GetColumnName(MoldClass.AllBoldMembers[MemberIdList[i].MemberIndex]));
        }

        return Columns.Count > 0
            ? SysUtils.Format("INSERT INTO %s (%s) VALUES (%s)", BoldExternalPersistenceControllerSQL._GetTableName(MoldClass), string.Join(", ", Columns),
                string.Join(", ", Columns.Select(c => ":" + c)))
            : SysUtils.Format("INSERT INTO %s", BoldExternalPersistenceControllerSQL._GetTableName(MoldClass));
    }

    /// <summary>Updates the object's row with its modified external members.</summary>
    [Delphi(PortStatus.Ported, Note = "Does nothing when no member was modified; Delphi ran the empty statement.")]
    protected virtual void InternalUpdateObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        string SQL = GenerateUpdateSQL(MoldClass, ObjectContents);
        if (SQL == "")
        {
            return;
        }

        var MemberIdList = new TBoldMemberIdList();
        IBoldExecQuery? BoldExecQuery = DatabaseAdapter.DatabaseInterface.GetExecQuery();
        try
        {
            GetExternalDirtyMembers(MoldClass, ObjectContents, MemberIdList);
            BoldExecQuery.AssignSQLText(SQL);
            AssignParametersWithMemberIdList(MoldClass, ObjectContents, (IBoldParameterized)BoldExecQuery, MemberIdList);
            BoldExecQuery.ExecSQL();
        }
        finally
        {
            DatabaseAdapter.DatabaseInterface.ReleaseExecQuery(ref BoldExecQuery);
        }
    }

    /// <summary>UPDATE of the modified external members of the object's row; empty when none was modified.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual string GenerateUpdateSQL(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        var FieldNames = new List<string>();
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            TMoldMember Member = MoldClass.AllBoldMembers[i];
            if (Member.Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && BoldExternalPersistenceControllerSQL.IsStoredInRow(Member) &&
                ObjectContents.ValueByIndex(i)!.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
            {
                string Column = BoldExternalPersistenceControllerSQL._GetColumnName(Member);
                FieldNames.Add(Column + " = :" + Column);
            }
        }

        return FieldNames.Count > 0
            ? SysUtils.Format("UPDATE %S SET %S", BoldExternalPersistenceControllerSQL._GetTableName(MoldClass), string.Join(", ", FieldNames)) + " WHERE " +
                ObjectContentsToInternalSQL(MoldClass, ObjectContents)
            : "";
    }

    /// <summary>Deletes the object's row.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void InternalDeleteObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        IBoldExecQuery? BoldExecQuery = DatabaseAdapter.DatabaseInterface.GetExecQuery();
        try
        {
            BoldExecQuery.AssignSQLText(GenerateDeleteSQL(MoldClass, ObjectContents));
            BoldExecQuery.ExecSQL();
        }
        finally
        {
            DatabaseAdapter.DatabaseInterface.ReleaseExecQuery(ref BoldExecQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual string GenerateDeleteSQL(TMoldClass MoldClass, IBoldObjectContents ObjectContents) =>
        SysUtils.Format("DELETE FROM %S WHERE %S", BoldExternalPersistenceControllerSQL._GetTableName(MoldClass), ObjectContentsToInternalSQL(MoldClass, ObjectContents));

    /// <summary>Not implemented in Delphi; returns null.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual IBoldDataSet? InternalFetchObjects(TMoldClass MoldClass) => null;

    /// <summary>The external members that are stored in the row and modified.</summary>
    [Delphi(PortStatus.Ported)]
    protected void GetExternalDirtyMembers(TMoldClass MoldClass, IBoldObjectContents ObjectContents, TBoldMemberIdList MemberIdList)
    {
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            TMoldMember Member = MoldClass.AllBoldMembers[i];
            if (Member.Storage is TBoldStorage.bsExternal or TBoldStorage.bsExternalKey && BoldExternalPersistenceControllerSQL.IsStoredInRow(Member) &&
                ObjectContents.ValueByIndex(i)!.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
            {
                MemberIdList.Add(new TBoldMemberID(i));
            }
        }
    }

    /// <summary>Sets the parameters of the key members.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AssignParameters(TMoldClass MoldClass, IBoldObjectContents ObjectContents, IBoldParameterized Query)
    {
        var MemberIdList = new TBoldMemberIdList();
        BoldExternalPersistenceControllerSQL.GetExternalKeyMembers(MoldClass, MemberIdList);
        AssignParametersWithMemberIdList(MoldClass, ObjectContents, Query, MemberIdList);
    }

    /// <summary>The where fragment of the object's key.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual string ObjectContentsToInternalSQL(TMoldClass MoldClass, IBoldObjectContents ObjectContents)
    {
        var ObjectIdList = new TBoldObjectIdList();
        ObjectIdList.Add(GetExternalKeyFromObject(ObjectContents, null!));
        return ExternalKeysToInternalSQL(MoldClass, ObjectIdList);
    }

    /// <summary>The internal key translation, open to the fetch objects.</summary>
    internal string ExternalKeysToInternalSQLForFetch(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys) => ExternalKeysToInternalSQL(MoldClass, ExternalKeys);

    [Delphi(PortStatus.Ported)] protected internal TBoldAbstractDatabaseAdapter DatabaseAdapter => FDatabaseAdapter;
}

/// <summary>Reads one member of the objects of a fetch from the selected rows.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TAbstractFetchObject : TCollectionItem
{
    private readonly TMoldMember FMember;
    private readonly int FMemberIndex;

    [Delphi(PortStatus.Ported)]
    protected TAbstractFetchObject(TCollection ACollection, int AMemberIndex) : base(ACollection)
    {
        FMemberIndex = AMemberIndex;
        FMember = MoldClass.AllBoldMembers[MemberIndex];
    }

    [Delphi(PortStatus.Ported)] private TFetchObjectList GetFetchObjectList() => (TFetchObjectList)Collection!;
    [Delphi(PortStatus.Ported)] private TBoldExternalPersistenceControllerSQL GetPersistenceController() => FetchObjectList.PersistenceController;
    [Delphi(PortStatus.Ported)] private TMoldClass GetMoldClass() => FetchObjectList.MoldClass;
    [Delphi(PortStatus.Ported)] private IBoldValueSpace GetValueSpace() => FetchObjectList.ValueSpace;

    [Delphi(PortStatus.Ported)]
    protected internal abstract void Fetch(IBoldQuery Source, IBoldObjectContents ObjectContents);

    [Delphi(PortStatus.Ported)] protected TFetchObjectList FetchObjectList => GetFetchObjectList();
    [Delphi(PortStatus.Ported)] protected TBoldExternalPersistenceControllerSQL PersistenceController => GetPersistenceController();
    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpace => GetValueSpace();
    [Delphi(PortStatus.Ported)] public TMoldClass MoldClass => GetMoldClass();
    [Delphi(PortStatus.Ported)] public TMoldMember Member => FMember;
    [Delphi(PortStatus.Ported)] public int MemberIndex => FMemberIndex;
}

/// <summary>Reads an attribute from its column.</summary>
[Delphi(PortStatus.Ported)]
public class TMemberFetchObject(TCollection ACollection, int AMemberIndex) : TAbstractFetchObject(ACollection, AMemberIndex)
{
    [Delphi(PortStatus.Ported)]
    protected internal override void Fetch(IBoldQuery Source, IBoldObjectContents ObjectContents)
    {
        IBoldValue Value = ObjectContents.ValueByIndex(MemberIndex)!;
        string DBFieldName = BoldExternalPersistenceControllerSQL.RemovePreAt(MoldClass.AllBoldMembers[MemberIndex]);
        if (Source.FieldByName(DBFieldName).IsNull)
        {
            BoldExternalPersistenceControllerSQL.SetBoldValueToNull(Value);
        }
        else
        {
            BoldExternalPersistenceControllerSQL.VariantToBoldValue(Value, Source.FieldByName(DBFieldName).AsVariant);
        }
    }
}

/// <summary>Reads a link by selecting the keys of the other class's rows that refer to this row.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TAbstractRoleFetchObject(TCollection ACollection, int AMemberIndex) : TAbstractFetchObject(ACollection, AMemberIndex)
{
    private string SQL = "";

    [Delphi(PortStatus.Ported)]
    private TMoldRole GetMoldRole() => (TMoldRole)Member;

    /// <summary>SELECT the other class's keys WHERE its link columns equal this role's columns (as parameters).</summary>
    [Delphi(PortStatus.Ported, Note = "The sanity checks raise instead of being asserts.")]
    protected virtual string PrepareSQL()
    {
        if (SQL == "")
        {
            // Find match
            string W1 = BoldExternalPersistenceControllerSQL.RemovePreAt(Role.OtherEnd!); // WHERE %S
            string W2 = BoldExternalPersistenceControllerSQL.RemovePreAt(Role); //          = :%S
            // Sanity check
            if (W1 == "" || W2 == "")
            {
                throw new EBold(SysUtils.Format(ExPeConsts.sRoleHasNoColumnNames, Role.Association!.Name));
            }

            if (BoldExternalPersistenceControllerSQL.GetCharCount(';', W1) != BoldExternalPersistenceControllerSQL.GetCharCount(';', W2))
            {
                throw new EBold(SysUtils.Format(ExPeConsts.sRoleEndCountMismatch, Role.Association!.Name));
            }

            var Parts = new List<string>();
            for (int i = 0; i <= BoldExternalPersistenceControllerSQL.GetCharCount(';', BoldExternalPersistenceControllerSQL.RemovePreAt(Role.OtherEnd!)); i++)
            {
                Parts.Add("(" + BoldExternalPersistenceControllerSQL.GetNextWord(ref W1, ';') + " = :" + BoldExternalPersistenceControllerSQL.GetNextWord(ref W2, ';') + ")");
            }

            TMoldClass OtherClass = Role.OtherEnd!.MoldClass!;
            SQL = SysUtils.Format("SELECT %S FROM %S WHERE (%S)", BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(OtherClass).Replace(";", ", ", StringComparison.Ordinal),
                BoldExternalPersistenceControllerSQL._GetTableName(OtherClass), string.Join(" AND", Parts));
        }

        return SQL;
    }

    /// <summary>Opens the other end's query with this row's values for the role's columns.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual IBoldQuery ExecSQL(IBoldQuery Source, IBoldObjectContents ObjectContents)
    {
        IBoldQuery Result = PersistenceController.DatabaseAdapter.DatabaseInterface.GetQuery();
        Result.AssignSQLText(PrepareSQL());
        string KeyNames = BoldExternalPersistenceControllerSQL.RemovePreAt(Role);
        for (int i = 0, n = BoldExternalPersistenceControllerSQL.GetCharCount(';', KeyNames); i <= n; i++)
        {
            string KeyName = BoldExternalPersistenceControllerSQL.GetNextWord(ref KeyNames, ';');
            Result.ParamByName(KeyName).asVariant = Source[KeyName];
        }

        Result.Open();
        return Result;
    }

    /// <summary>The key of the current row of the other end's query.</summary>
    internal TBoldObjectId KeyFromRow(IBoldQuery OtherEndQuery)
    {
        object? DBValue = BoldExternalPersistenceControllerSQL.NormalizeVariant(
            BoldExternalPersistenceControllerSQL.GetDataValue(OtherEndQuery.AsDataSet, BoldExternalPersistenceControllerSQL.FindExternalKeyColumns(Role.OtherEnd!.MoldClass!)));
        return BoldExternalPersistenceControllerDataSet.IdFromValue(DBValue)
            ?? throw new EBold(SysUtils.Format(ExPeConsts.sUnknownVarTypeLoadingSingleID, MoldClass.Name, Role.Name));
    }

    [Delphi(PortStatus.Ported)] public TMoldRole Role => GetMoldRole();
}

/// <summary>Reads a single link.</summary>
[Delphi(PortStatus.Ported)]
public class TSingleRoleFetchObject(TCollection ACollection, int AMemberIndex) : TAbstractRoleFetchObject(ACollection, AMemberIndex)
{
    [Delphi(PortStatus.Ported, Note = "Releases the other end's query, and sets an empty link only through the member's link interface (Delphi passed an unassigned one).")]
    protected internal override void Fetch(IBoldQuery Source, IBoldObjectContents ObjectContents)
    {
        IBoldQuery? OtherEndQuery = ExecSQL(Source, ObjectContents);
        try
        {
            if (ObjectContents.ValueByIndex(MemberIndex) is IBoldObjectIdRef IdRef)
            {
                PersistenceController.SetSingleLink(IdRef, OtherEndQuery.AsDataSet.IsEmpty ? null : KeyFromRow(OtherEndQuery), Role.OtherEnd!.MoldClass!);
            }
        }
        finally
        {
            PersistenceController.DatabaseAdapter.DatabaseInterface.ReleaseQuery(ref OtherEndQuery);
        }
    }
}

/// <summary>Reads a multi link.</summary>
[Delphi(PortStatus.Ported)]
public class TMultiRoleFetchObject(TCollection ACollection, int AMemberIndex) : TAbstractRoleFetchObject(ACollection, AMemberIndex)
{
    [Delphi(PortStatus.Ported, Note = "Releases the other end's query.")]
    protected internal override void Fetch(IBoldQuery Source, IBoldObjectContents ObjectContents)
    {
        IBoldQuery? OtherEndQuery = ExecSQL(Source, ObjectContents);
        try
        {
            var ExternalIdList = new TBoldObjectIdList();
            while (!OtherEndQuery.Eof)
            {
                ExternalIdList.Add(KeyFromRow(OtherEndQuery));
                OtherEndQuery.Next();
            }

            if (ObjectContents.ValueByIndex(MemberIndex) is IBoldObjectIdListRef IdRefList)
            {
                PersistenceController.SetMultiLink(IdRefList, ExternalIdList, Role.OtherEnd!.MoldClass!);
            }
        }
        finally
        {
            PersistenceController.DatabaseAdapter.DatabaseInterface.ReleaseQuery(ref OtherEndQuery);
        }
    }
}

/// <summary>The fetch objects of the members being fetched for a block of objects of a class.</summary>
[Delphi(PortStatus.Ported)]
public class TFetchObjectList : TCollection
{
    private readonly TMoldClass FMoldClass;
    private readonly TBoldObjectIdList FExternalKeys;
    private readonly IBoldValueSpace FValueSpace;
    private readonly TBoldExternalPersistenceControllerSQL FPersistenceController;

    [Delphi(PortStatus.Ported)]
    public TFetchObjectList(TBoldExternalPersistenceControllerSQL APersistenceController, TMoldClass AMoldClass, TBoldMemberIdList AMemberIdList,
        TBoldObjectIdList AExternalKeys, IBoldValueSpace AValueSpace)
        : base(typeof(TAbstractFetchObject))
    {
        FPersistenceController = APersistenceController;
        FMoldClass = AMoldClass;
        FExternalKeys = AExternalKeys;
        FValueSpace = AValueSpace;
        for (int i = 0; i < AMemberIdList.Count; i++)
        {
            Add(MoldClass.AllBoldMembers[AMemberIdList[i].MemberIndex]);
        }
    }

    /// <summary>A fetch object for the member: of a multi link, a single link or an attribute.</summary>
    [Delphi(PortStatus.Ported)]
    protected TAbstractFetchObject Add(TMoldMember Member)
    {
        int Index = MoldClass.AllBoldMembers.IndexOf(Member);
        return Member switch
        {
            TMoldRole { Multi: true } => new TMultiRoleFetchObject(this, Index),
            TMoldRole => new TSingleRoleFetchObject(this, Index),
            _ => new TMemberFetchObject(this, Index),
        };
    }

    [Delphi(PortStatus.Ported)] private TAbstractFetchObject GetItem(int Index) => (TAbstractFetchObject)base[Index];
    [Delphi(PortStatus.Ported)] private void SetItem(int Index, TAbstractFetchObject Value) => base[Index].Assign(Value);

    [Delphi(PortStatus.Ported)]
    public int IndexOf(TBoldMemberID MemberId)
    {
        for (int Result = 0; Result < Count; Result++)
        {
            if (this[Result].MemberIndex == MemberId.MemberIndex)
            {
                return Result;
            }
        }

        return -1;
    }

    [Delphi(PortStatus.Ported)]
    public TAbstractFetchObject? Find(TBoldMemberID MemberId)
    {
        int Index = IndexOf(MemberId);
        return Index == -1 ? null : this[Index];
    }

    [Delphi(PortStatus.Ported, Symbol = "TFetchObjectList.Items")]
    public new TAbstractFetchObject this[int Index] { get => GetItem(Index); set => SetItem(Index, value); }

    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceControllerSQL PersistenceController => FPersistenceController;
    [Delphi(PortStatus.Ported)] public TMoldClass MoldClass => FMoldClass;
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList ExternalKeys => FExternalKeys;
    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpace => FValueSpace;
}

/// <summary>The rows of a block of objects, selected once, and the fetch objects that read their members.</summary>
[Delphi(PortStatus.Ported)]
public class TFetchContext
{
    private readonly TFetchObjectList FFetchObjectList;
    private IBoldQuery? Source;
    private readonly TBoldExternalPersistenceControllerSQL fPersistenceController;

    /// <summary>Selects the columns of the members (each once) of the rows of the keys.</summary>
    [Delphi(PortStatus.Ported, Note = "The sanity checks raise instead of being asserts.")]
    public TFetchContext(TBoldExternalPersistenceControllerSQL PersistenceController, TBoldObjectIdList ExternalKeys, IBoldValueSpace ValueSpace, TMoldClass MoldClass,
        TBoldMemberIdList MemberIdList)
    {
        // Sanity check, should never happen
        if (MemberIdList.Count == 0)
        {
            throw new EBoldInternal("MemberIdList.Count = 0!!");
        }

        if (ExternalKeys.Count == 0)
        {
            throw new EBoldInternal("Nothing to fetch!!");
        }

        fPersistenceController = PersistenceController;
        FFetchObjectList = new TFetchObjectList(PersistenceController, MoldClass, MemberIdList, ExternalKeys, ValueSpace);
        // Remove duplicates in memberlist, occours with roles
        var Columns = new List<string>();
        for (int i = 0; i < MemberIdList.Count; i++)
        {
            foreach (string Column in BoldExternalPersistenceControllerSQL.RemovePreAt(MoldClass.AllBoldMembers[MemberIdList[i].MemberIndex]).Split(';'))
            {
                if (!Columns.Contains(Column, StringComparer.OrdinalIgnoreCase))
                {
                    Columns.Add(Column);
                }
            }
        }

        string SQL = SysUtils.Format("SELECT %S FROM %S WHERE %S", string.Join(", ", Columns), BoldExternalPersistenceControllerSQL._GetTableName(MoldClass),
            PersistenceController.ExternalKeysToInternalSQLForFetch(MoldClass, ExternalKeys));
        Source = PersistenceController.DatabaseAdapter.DatabaseInterface.GetQuery();
        Source.AssignSQLText(SQL);
        Source.Open();
    }

    /// <summary>Reads the members of the object from its row.</summary>
    [Delphi(PortStatus.Ported)]
    public void FetchObject(IBoldObjectContents ObjectContents)
    {
        for (int i = 0; i < FetchObjectList.Count; i++)
        {
            if (!FetchObjectList.PersistenceController.LocateInDB(FetchObjectList.MoldClass, ObjectContents, Source!))
            {
                throw new EBold(ExPeConsts.sObjectNoLongerInDB);
            }

            FetchObjectList[i].Fetch(Source!, ObjectContents);
        }
    }

    /// <summary>Releases the query.</summary>
    [Delphi(PortStatus.Ported, Note = "Releases the query; Delphi did nothing.")]
    public void PostFetch()
    {
        if (Source is not null)
        {
            fPersistenceController.DatabaseAdapter.DatabaseInterface.ReleaseQuery(ref Source);
        }
    }

    [Delphi(PortStatus.Ported)] public TFetchObjectList FetchObjectList => FFetchObjectList;
}
