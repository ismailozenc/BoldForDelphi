// Delphi: Source/PMapper/Default/BoldPMappersDefault.pas
// Port map: CSharp/port/units/BoldPMappersDefault.md
// Delphi's technical members set their fields before calling the inherited constructor; in .NET the base constructor
// runs first, so they set them in InitializeTechnicalMember, which TBoldMemberDefaultMapper calls before it generates
// the mapping information.

namespace Bold;

/// <summary>The default mapper of a member: its mapping information, columns, comparison and fetching by itself.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMemberDefaultMapper : TBoldMemberSQLMapper
{
    /// <summary>Generates the mapping information of a persistent member with columns (technical members too).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberDefaultMapper.CreateFromMold")]
    public TBoldMemberDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        if (moldMember is null)
        {
            InitializeTechnicalMember(moldClass);
        }

        if ((moldMember is null || moldMember.EffectivePersistent) && ColumnCount > 0)
        {
            GenerateMappingInfo(moldClass, moldMember);
        }

        AfterCreateFromMold(moldMember, moldClass);
    }

    /// <summary>The end of a subclass's Delphi constructor, for subclasses whose fields must be set before this constructor (primary constructors).</summary>
    protected virtual void AfterCreateFromMold(TMoldMember? moldMember, TMoldClass moldClass)
    {
    }

    /// <summary>Sets the fields of a technical member (one without a model member) before its mapping information is generated.</summary>
    protected virtual void InitializeTechnicalMember(TMoldClass moldClass)
    {
    }

    /// <summary>Adds the member's mapping (for the class's names, the member's former names, and the children of tmChildren classes).</summary>
    [Delphi(PortStatus.Ported)]
    private void GenerateMappingInfo(TMoldClass MoldClass, TMoldMember? MoldMember)
    {
        string ColumnNames = "";

        void GenerateLocal(string ClassExpressionName, string MemberExpressionName, TMoldClass LocalMoldClass)
        {
            if (LocalMoldClass.TableMapping is TTableMapping.tmOwn or TTableMapping.tmParent)
            {
                SystemPersistenceMapper.MappingInfo.AddMemberMapping(ClassExpressionName, MemberExpressionName, FindDefiningTable(LocalMoldClass, MoldMember), ColumnNames,
                    GetType().Name);
            }

            if (LocalMoldClass.TableMapping == TTableMapping.tmChildren)
            {
                for (int i = 0; i < LocalMoldClass.SubClasses.Count; i++)
                {
                    if (LocalMoldClass.SubClasses[i].EffectivePersistent)
                    {
                        GenerateLocal(ClassExpressionName, MemberExpressionName, LocalMoldClass.SubClasses[i]);
                    }
                }
            }
        }

        void GenerateForClassName(string ClassExpressionName)
        {
            GenerateLocal(ClassExpressionName, ExpressionName, MoldClass);
            if (MoldMember is not null)
            {
                var s = new TStringList { CommaText = MoldMember.FormerNames };
                for (int i = 0; i < s.Count; i++)
                {
                    GenerateLocal(ClassExpressionName, s[i], MoldClass);
                }
            }
        }

        if (RequiresMemberMapping())
        {
            for (int i = 0; i < ColumnCount; i++)
            {
                if (ColumnNames.Length != 0)
                {
                    ColumnNames += ", ";
                }

                // expansion below is to truncate the names to the right length again...
                ColumnNames += BoldNameExpander.BoldExpandName(InitialColumnName(i), "", TExpansionType.xtSQL, SystemPersistenceMapper.SQLDataBaseConfig.MaxDbIdentifierLength,
                    SystemPersistenceMapper.NationalCharConversion);
            }

            // this will loop over the name, all former names, and the names of the association
            for (int i = 0; i < MoldClass.AllPossibleNames.Count; i++)
            {
                GenerateForClassName(MoldClass.AllPossibleNames[i]);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemDefaultMapper GetSystemPersistenceMapper() => (TBoldSystemDefaultMapper)base.SystemPersistenceMapper;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectDefaultMapper GetObjectPersistenceMapper() => (TBoldObjectDefaultMapper)base.ObjectPersistenceMapper;

    /// <summary>When the field or the value is null, whether they are equal (and true); false when neither is null.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool CheckEitherNull(IBoldField field, IBoldValue Value, out bool Equal)
    {
        Equal = false;
        if (field.IsNull)
        {
            Equal = ((IBoldNullableValue)Value).IsNull;
            return true;
        }

        if (((IBoldNullableValue)Value).IsNull)
        {
            Equal = false;
            return true;
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAllowNullAsSQL() => AllowNull ? "" : SystemPersistenceMapper.SQLDataBaseConfig.EffectiveSQLForNotNull;

    [Delphi(PortStatus.Ported)]
    protected internal virtual void GetChangePoints(TBoldObjectIdList ObjectIDList, TBoldChangePointCondition Condition, TBoldSQLNameSpace NameSpace) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sNotSupportedOnMember, GetType().Name));

    /// <summary>Compares the member's columns with its value (when loaded), logging each column that differs.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool CompareFields(IBoldObjectContents ObjectContent, IBoldDataSet DataSet, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        bool result = true;
        if (SupportsComparingWithoutValue() || GetValue(ObjectContent) is not null)
        {
            for (int ColumnIndex = 0; ColumnIndex < ColumnCount; ColumnIndex++)
            {
                IBoldField? aField = DataSet.FieldByName(ColumnDescriptions[ColumnIndex].SQLName);
                if (aField is null)
                {
                    throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sSomeColumnsNotInTable, GetType().Name, "CompareFields", ColumnIndex, ColumnDescriptions[ColumnIndex].SQLName));
                }

                if (!CompareField(ObjectContent, aField, ColumnIndex, ValueSpace, TranslationList))
                {
                    BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sOptimisticLockingFailed,
                        [ObjectPersistenceMapper.ExpressionName, ExpressionName, ObjectContent.ObjectId.AsString, ColumnIndex, ColumnDescriptions[ColumnIndex].SQLName]);
                    result = false;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList);

    /// <summary>Describes the member's columns from its mapping (whose column count must match).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        TBoldMemberMappingInfo[] MemberMappings = SystemPersistenceMapper.MappingInfo.GetMemberMappings(ObjectPersistenceMapper.ExpressionName, ExpressionName);
        if (MemberMappings.Length == 1)
        {
            var Columns = new TStringList { CommaText = MemberMappings[0].Columns };
            if (Columns.Count != ColumnCount)
            {
                throw new EBold(SysUtils.Format(BoldPMConsts.sUnsupportedMappingChange, ObjectPersistenceMapper.ExpressionName, ExpressionName));
            }

            for (int i = 0; i < Columns.Count; i++)
            {
                // ??? is this really right? is it not supposed to be the mapper of the class that defines the attribute? - Yes, in theory. In practice it makes no difference.
                ColumnDescriptions.Add(SystemPersistenceMapper.EnsureColumn(MemberMappings[0].TableName, Columns[i], ColumnTypeAsSQL(i), AllowNullAsSQL,
                    ColumnBDEFieldType(i), ColumnSize(i), AllowNull, ObjectPersistenceMapper.Versioned, DefaultDbValue));
            }
        }
        else if (MemberMappings.Length == 0 && RequiresMemberMapping())
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindMappingForX, ObjectPersistenceMapper.ExpressionName, ExpressionName));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool RequiresMemberMapping() => ColumnCount > 0;

    /// <summary>The table of the class that stores the member, seen from LocalMoldClass.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual string FindDefiningTable(TMoldClass LocalMoldClass, TMoldMember? MoldMember)
    {
        TMoldClass? StoringClass = LocalMoldClass.FindStoringClass(MoldMember!.MoldClass!, false, MoldMember);
        if (StoringClass is null)
        {
            return "";
        }

        TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;
        return BoldNameExpander.BoldExpandPrefix(StoringClass.TableName, StoringClass.Name, cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength, StoringClass.Model.NationalCharConversion);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool SupportsComparingWithoutValue() => false;

    /// <summary>Fetches only this member of the objects, from its table (one object at a time when a live query is required).</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldIDTranslationList TranslationList, TBoldObjectIdList? FailureList)
    {
        // Note, when the list gets here it contains only object of one class, from one
        // Bold, that are not previously present in the Bold (or possibly a forced fetch)

        // all objectids are the same timestamp, this has been taken care of by PMFetchExactId
        int BlockSize = ObjectIdList.Count > 1 && RequiresLiveQuery ? 1 : SystemPersistenceMapper.SQLDataBaseConfig.FetchBlockSize;
        var sql = new TStringList();
        var TempList = new TStringList();
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        MemberPMList.Add(this);
        IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
        try
        {
            if (RequiresLiveQuery)
            {
                aQuery.RequestLiveQuery = true;
            }

            BoldPMappersDefault.ForEachBlock(ObjectIdList.Count, BlockSize, (start, stop) =>
            {
                sql.Clear();
                TBoldSQLTableDescription Table = ((TBoldSQLColumnDescription)ColumnDescriptions[0]).TableDescription;
                TempList.Clear();
                ObjectPersistenceMapper.SQLForMembers(Table, TempList, MemberPMList, TBoldSQLStyle.ssColumns, true, true, false);
                sql.Text = SysUtils.Format("SELECT %s FROM %s WHERE %s ", BoldUtils.BoldSeparateStringList(TempList, ", ", "", ""), Table.SQLName, BoldDefs.IDCOLUMN_NAME);
                if (start <= stop)
                {
                    BoldUtils.BoldAppendToStrings(sql, ObjectPersistenceMapper.IdListSegmentToWhereFragment(ObjectIdList, start, stop, aQuery), false);
                    aQuery.AssignSQL(sql);
                    aQuery.Open();
                    while (!aQuery.Eof)
                    {
                        TBoldObjectId TempId = SystemPersistenceMapper.NewIdFromQuery(aQuery, 1, 0, ObjectIdList[0].TimeStamp);
                        TBoldObjectId NewId = ObjectIdList.IDByID(TempId)
                            ?? throw new EBoldInternal(SysUtils.Format("%s.PMFetch: Database returned object we didn't ask for (ID: %d)", GetType().Name, aQuery.Fields(0).AsInteger));
                        if (!NewId.TopSortedIndexExact)
                        {
                            NewId = TranslationList.TranslateToNewId(NewId)!;
                        }

                        if (!NewId.TopSortedIndexExact)
                        {
                            throw new EBoldInternal(SysUtils.Format("%s.PMFetch: Got an Id with no or only approx class!", GetType().Name));
                        }

                        ObjectPersistenceMapper.ValuesFromFieldsByMemberList(NewId, ValueSpace, TranslationList, aQuery, MemberPMList);
                        aQuery.Next();
                    }

                    aQuery.Close();
                }
            });
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref aQuery);
        }
    }

    [Delphi(PortStatus.Ported)] public new TBoldObjectDefaultMapper ObjectPersistenceMapper => GetObjectPersistenceMapper();
    [Delphi(PortStatus.Ported)] public new TBoldSystemDefaultMapper SystemPersistenceMapper => GetSystemPersistenceMapper();
}

/// <summary>A member stored in one column.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSingleColumnMember : TBoldMemberDefaultMapper
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSingleColumnMember(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnCount() => 1;

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnSize(int ColumnIndex) => 0;

    [Delphi(PortStatus.Ported, Note = "A class procedure in Delphi; it names the class, which is the declaring class here.")]
    public static void EnsureFirstColumn(int ColumnIndex)
    {
        if (ColumnIndex != 0)
        {
            throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, nameof(TBoldSingleColumnMember), "EnsureFirstColumn", ColumnIndex));
        }
    }
}

/// <summary>The model version of the object's row (in the root table), for object upgrading.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldModelVersionMember : TBoldSingleColumnMember
{
    private int fVersionNumber;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelVersionMember.CreateFromMold")]
    public TBoldModelVersionMember(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    protected override void InitializeTechnicalMember(TMoldClass moldClass)
    {
        fExpressionname = "_" + BoldDefs.MODELVERSIONCOLUMN_NAME;
        fDefaultDbValue = "";
        fAllowNull = false;
        fDelayedFetch = false;
        fContentName = "";
        fIsStoredInObject = true;
        fInitialColumnRootName = BoldDefs.MODELVERSIONCOLUMN_NAME;
        fVersionNumber = moldClass.Model.ModelVersion;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftInteger;

    // actually this method is irrelevant
    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) => true;

    [Delphi(PortStatus.Ported)]
    protected override string FindDefiningTable(TMoldClass LocalMoldClass, TMoldMember? MoldMember)
    {
        TMoldClass RootClass = LocalMoldClass.Model.RootClass;
        TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;
        return BoldNameExpander.BoldExpandPrefix(RootClass.TableName, RootClass.Name, cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength, RootClass.Model.NationalCharConversion);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsDirty(IBoldObjectContents ObjectContents) => true;

    // in an environment that uses ModelVersion,
    // it must always be fetched so that an automatic update can be performed
    [Delphi(PortStatus.Ported)]
    public override bool ShouldFetch(IBoldObjectContents ObjectContents) => true;

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList) =>
        Param.AsInteger = VersionNumber;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        // do nothing
    }

    [Delphi(PortStatus.Ported)]
    public int VersionFromQuery(IBoldQuery Query)
    {
        IBoldField? aField = Query.FieldByName(ColumnDescriptions[0].SQLName);
        return aField?.AsInteger ?? throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sColumnNotFoundInTable, GetType().Name, ColumnDescriptions[0].SQLName));
    }

    [Delphi(PortStatus.Ported)] public int VersionNumber { get => fVersionNumber; set => fVersionNumber = value; }
}

/// <summary>The read-only flag of the object's row (in the root table).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldReadOnlynessMember : TBoldSingleColumnMember
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldReadOnlynessMember.CreateFromMold")]
    public TBoldReadOnlynessMember(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    protected override void InitializeTechnicalMember(TMoldClass moldClass)
    {
        fExpressionname = "_" + BoldDefs.READONLYCOLUMN_NAME;
        fDefaultDbValue = "";
        fAllowNull = false;
        fDelayedFetch = false;
        fContentName = "";
        fIsStoredInObject = true;
        fInitialColumnRootName = BoldDefs.READONLYCOLUMN_NAME;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftInteger;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) => true;

    [Delphi(PortStatus.Ported)]
    protected override string FindDefiningTable(TMoldClass LocalMoldClass, TMoldMember? MoldMember)
    {
        TMoldClass RootClass = LocalMoldClass.Model.RootClass;
        TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;
        return BoldNameExpander.BoldExpandPrefix(RootClass.TableName, RootClass.Name, cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength, RootClass.Model.NationalCharConversion);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsDirty(IBoldObjectContents ObjectContents) => false;

    [Delphi(PortStatus.Ported)]
    public override bool ShouldFetch(IBoldObjectContents ObjectContents) => true;

    // since IsDirty is false, this will only happen on Create or
    // when using UpdateWholeObjects (which is a bad thing...)
    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList) =>
        Param.AsInteger = 0;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex) =>
        ObjectContent.SetIsReadOnly(Field.AsInteger == 1);
}

/// <summary>Members stored in the X-files table, which build their column descriptions themselves.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXFilesMembers : TBoldSingleColumnMember
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldXFilesMembers(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        base.InitializePSDescriptions();
        // since these members do not require membermappings... they have to build their PSDesc manually
        if (ColumnDescriptions.Count == 0)
        {
            for (int i = 0; i < ColumnCount; i++)
            {
                ColumnDescriptions.Add(SystemPersistenceMapper.PSSystemDescription.XFilestable!.ColumnsList.ItemsBySQLName(GetInitialColumnName(i)));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool RequiresMemberMapping() => false;

    /// <summary>The X-files table's name; too early to ask the system description (it would be made before all mappers exist).</summary>
    protected string XFilesTableName()
    {
        TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;
        return BoldNameExpander.BoldExpandPrefix(BoldDefs.TABLEPREFIXTAG + "_XFILES", "", cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength, SystemPersistenceMapper.NationalCharConversion);
    }
}

/// <summary>The object's time stamp in the X-files table, for time stamp optimistic locking.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTimeStampMember : TBoldXFilesMembers
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldTimeStampMember.CreateFromMold")]
    public TBoldTimeStampMember(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, BoldPMappersDefault.TIMESTAMPMEMBERINDEX, TypeNameDictionary)
    {
    }

    protected override void InitializeTechnicalMember(TMoldClass moldClass)
    {
        fExpressionname = "_" + BoldDefs.TIMESTAMPCOLUMN_NAME;
        fDefaultDbValue = "";
        fAllowNull = false;
        fDelayedFetch = moldClass.EffectiveOptimisticLocking != TBoldOptimisticLockingMode.bolmTimeStamp;
        fContentName = "";
        fIsStoredInObject = true;
        fInitialColumnRootName = BoldDefs.TIMESTAMPCOLUMN_NAME;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => BoldPMappersDefault.BOLDTIMESTAMPFIELDTYPE;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        ColumnIndex == 0
            ? Field.AsInteger == ObjectContent.TimeStamp
            : throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "CompareField", ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected override string FindDefiningTable(TMoldClass LocalMoldClass, TMoldMember? MoldMember) => XFilesTableName();

    [Delphi(PortStatus.Ported)]
    protected override bool SupportsComparingWithoutValue() => true;

    [Delphi(PortStatus.Ported)]
    public override bool IsDirty(IBoldObjectContents ObjectContents) => true;

    // the timestamp should only be loaded if the object uses timestamp-mode for optimistic locking
    [Delphi(PortStatus.Ported)]
    public override bool ShouldFetch(IBoldObjectContents ObjectContents) => ObjectPersistenceMapper.fOptimisticLockingMode == TBoldOptimisticLockingMode.bolmTimeStamp;

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList) =>
        Param.AsInteger = SystemPersistenceMapper.CurrentTimeStamp;

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex) =>
        ObjectContent.TimeStamp = Field.AsInteger;
}

/// <summary>The object's global id in the X-files table, made by ExternalIDGenerator for new objects.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGlobalIdMember : TBoldXFilesMembers
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldGlobalIdMember.CreateFromMold")]
    public TBoldGlobalIdMember(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    protected override void InitializeTechnicalMember(TMoldClass moldClass)
    {
        fExpressionname = "_" + BoldDefs.GLOBALIDCOLUMN_NAME;
        fDefaultDbValue = "";
        fAllowNull = false;
        fDelayedFetch = true;
        fContentName = "String";
        fIsStoredInObject = true;
        fInitialColumnRootName = BoldDefs.GLOBALIDCOLUMN_NAME;
    }

    // {F4252AB4-8FFA-460F-BDBD-1BB57D588D14}
    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => SysUtils.Format(SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForString, 39);

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftString;

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) => true;

    [Delphi(PortStatus.Ported)]
    protected override string FindDefiningTable(TMoldClass LocalMoldClass, TMoldMember? MoldMember) => XFilesTableName();

    [Delphi(PortStatus.Ported)]
    public override bool IsDirty(IBoldObjectContents ObjectContents) => false;

    [Delphi(PortStatus.Ported)]
    public override bool ShouldFetch(IBoldObjectContents ObjectContents) => false;

    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        string GlobalId = ObjectContent.GlobalId;
        if (GlobalId.Length == 0)
        {
            GlobalId = BoldPMappersDefault.ExternalIDGenerator();
        }

        Param.AsString = GlobalId;
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex) =>
        ObjectContent.GlobalId = Field.AsString;
}
