// Delphi: Source/PMapper/Default/BoldPMappersLinkDefault.pas
// Port map: CSharp/port/units/BoldPMappersLinkDefault.md
// The default mappers of association ends: embedded single links (a column with the other end's id, and an order
// column for ordered other ends), and non-embedded ends (direct or through a link class) that fetch by selecting the
// rows that point back at the objects.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersLinkDefault", Symbol = "finalization", Note = "Removed the registrations; the registries live as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldPMappersLinkDefault.</summary>
public static partial class BoldPMappersLinkDefault
{
    /// <summary>A new id (time-stamped unless the maximum) of the class, translated (if a list is given) and ensured in the value space.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldDefaultID CreateAndEnsureId(int ObjectId, int ClassId, bool Exact, TBoldIDTranslationList? TranslationList, IBoldValueSpace ValueSpace,
        TBoldTimeStampType TimeStamp)
    {
        System.Diagnostics.Debug.Assert(ClassId != -1);
        TBoldDefaultID Result = TimeStamp == BoldDefs.BOLDMAXTIMESTAMP
            ? new TBoldDefaultID(ClassId, Exact)
            : new TBoldTimestampedDefaultId(TimeStamp, ClassId, Exact);
        Result.AsInteger = ObjectId;
        TranslationList?.AddTranslation(null, Result); // needed?
        ValueSpace.EnsureObjectId(Result);
        return Result;
    }

    /// <summary>Orders the fetched link rows by object, order, remote and closest id.</summary>
    [Delphi(PortStatus.Ported)]
    internal static int SortLinkValues(TTempLinkValues Item1, TTempLinkValues Item2)
    {
        int result = Item1.ObjectId - Item2.ObjectId;
        if (result == 0)
        {
            result = Item1.Ordervalue - Item2.Ordervalue;
        }

        if (result == 0)
        {
            result = Item1.RemoteId - Item2.RemoteId;
        }

        if (result == 0)
        {
            result = Item1.ClosestId - Item2.ClosestId;
        }

        return result;
    }

#pragma warning disable CA2255
    /// <summary>Registers the link mappers.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization()
    {
        TBoldMemberPersistenceMapperDescriptorList mappers = BoldPMapperLists.BoldMemberPersistenceMappers();
        mappers.AddDescriptor(typeof(TBoldNonEmbeddedLinkDefaultMapper), TBoldAbstractionLevel.alAbstract);
        mappers.AddDescriptor(typeof(TBoldMultiLinkDefaultMapper), TBoldAbstractionLevel.alAbstract);
        mappers.AddDescriptor(typeof(TBoldEmbeddedSingleLinkDefaultMapper), TBoldAbstractionLevel.alConcrete);
        mappers.AddDescriptor(typeof(TBoldDirectSingleLinkDefaultmapper), TBoldAbstractionLevel.alConcrete);
        mappers.AddDescriptor(typeof(TBoldIndirectSingleLinkDefaultmapper), TBoldAbstractionLevel.alConcrete);
        mappers.AddDescriptor(typeof(TBoldDirectMultiLinkDefaultmapper), TBoldAbstractionLevel.alConcrete);
        mappers.AddDescriptor(typeof(TBoldIndirectMultiLinkDefaultmapper), TBoldAbstractionLevel.alConcrete);
    }
#pragma warning restore CA2255
}

/// <summary>A fetched link row: the object, its order, the closest end (with class) and the remote end.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TTempLinkValues
{
    public int ObjectId;
    public int Ordervalue;
    public int ClosestId;
    public int ClosestClassid;
    public int RemoteId;
}

/// <summary>The base of the link mappers: the object mapper of the other end.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldLinkDefaultMapper : TBoldMemberDefaultMapper
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    protected TBoldLinkDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectDefaultMapper GetObjectPersistenceMappers(int Index) => (TBoldObjectDefaultMapper)SystemPersistenceMapper.ObjectPersistenceMappers[Index]!;

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldObjectDefaultMapper GetOtherEndObjectMapper();

    [Delphi(PortStatus.Ported)] public TBoldObjectDefaultMapper OtherEndObjectMapper => GetOtherEndObjectMapper();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldObjectDefaultMapper ObjectPersistenceMappers(int Index) => GetObjectPersistenceMappers(Index);
}

/// <summary>
/// A single link stored in the object's row: the other end's id, and the order number when the other end is ordered.
/// The column count and the other end are set before the inherited constructor runs (as Delphi does), since it generates
/// the mapping information from the columns.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldEmbeddedSingleLinkDefaultMapper.CreateFromMold", Note = "The primary constructor.")]
public class TBoldEmbeddedSingleLinkDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
    TBoldTypeNameDictionary TypeNameDictionary) : TBoldLinkDefaultMapper(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
{
    private readonly bool fOtherEndExact = ((TMoldRole)moldMember!).OtherEnd!.MoldClass!.SubClasses.Count == 0;
    private readonly int fOtherEndObjectPMIndex = ((TMoldRole)moldMember!).OtherEnd!.MoldClass!.TopSortedIndex;
    private readonly int fOtherEndMemberIndex = ((TMoldRole)moldMember!).OtherEnd!.MoldClass!.AllBoldMembers.IndexOf(((TMoldRole)moldMember!).OtherEnd);
    private readonly int fColumnCount = ((TMoldRole)moldMember!).OtherEnd!.EffectiveOrdered ? 2 : 1;
    private readonly bool fIsInherited = moldMember!.MoldClass != moldClass;

    protected override void AfterCreateFromMold(TMoldMember? moldMember, TMoldClass moldClass) =>
        fIsStoredInObject = IsStoredInObject && ((TMoldRole)moldMember!).RoleType is TBoldRoleType.rtRole or TBoldRoleType.rtInnerLinkRole;

    /// <summary>
    /// Adds the ids of the objects whose link changed at a time: versions where the next version (one time stamp later)
    /// does not point at the same object, from both sides of the change.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal void GetNonEmbeddedChangePoints(TBoldObjectIdList ObjectIdList, TBoldChangePointCondition Condition, TBoldSQLNameSpace NameSpace)
    {
        void FetchIds(TBoldSQLQuery SQLQuery, int AddTime)
        {
            IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
            var SQL = new TStringList();
            try
            {
                SQLQuery.GenerateSQL(SQL);
                aQuery.AssignSQL(SQL);
                aQuery.Open();
                aQuery.First();
                while (!aQuery.Eof)
                {
                    TBoldTimeStampType Timestamp = aQuery.Fields(1).AsInteger;
                    if (Timestamp != BoldDefs.BOLDMAXTIMESTAMP)
                    {
                        TBoldObjectId ObjectId = NewDefaultId(aQuery.Fields(0).AsInteger, Timestamp + AddTime);
                        ObjectIdList.Add(ObjectId);
                        SendExtendedEvent(BoldSubscription.bpeFetchId, [ObjectId]);
                    }

                    aQuery.Next();
                }
            }
            finally
            {
                SystemPersistenceMapper.ReleaseQuery(ref aQuery);
            }
        }

        TBoldSQLQuery MakeMainQuery(out TBoldSQLTableReference NewTableRef, TBoldMemberMappingInfo MemberMapping)
        {
            var result = new TBoldSQLQuery(TBoldSQLQueryMode.qmSelect, SystemPersistenceMapper.PSSystemDescription, SystemPersistenceMapper.SQLDataBaseConfig, NameSpace)
            {
                IgnoreHistoricObjects = false,
            };
            NewTableRef = result.AddTableReference(MemberMapping.TableName);
            TBoldSQLColumnReference pointerColumnRef = NewTableRef.GetColumnReference(MemberMapping.ColumnByIndex(0));
            result.AddColumnToRetrieve(pointerColumnRef);
            result.AddWCF(TBoldSQLWCFBinaryInfix.CreateWCFForIdList(pointerColumnRef, Condition.IdList!));
            return result;
        }

        TBoldSQLQuery MakeSubQuery(TBoldSQLQuery Query, TBoldSQLTableReference JoinTableRef, out TBoldSQLTableReference NewTableRef, TBoldMemberMappingInfo MemberMapping)
        {
            var result = new TBoldSQLQuery(TBoldSQLQueryMode.qmSelect, SystemPersistenceMapper.PSSystemDescription, SystemPersistenceMapper.SQLDataBaseConfig, NameSpace)
            {
                IgnoreHistoricObjects = false,
            };
            NewTableRef = result.AddTableReference(MemberMapping.TableName);
            result.AddJoin(NewTableRef.GetColumnReference(MemberMapping.ColumnByIndex(0)), JoinTableRef.GetColumnReference(MemberMapping.ColumnByIndex(0)));
            result.AddJoin(NewTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME), JoinTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            Query.AddWCF(new TBoldSQLWCFUnaryPrefix(new TBoldSQLWCFExists(result, NewTableRef), "not"));
            return result;
        }

        TBoldSQLTableReference JoinRootTableInto(TBoldSQLQuery Query, TBoldSQLTableReference TableRef)
        {
            TBoldSQLTableReference result = Query.AddTableReference(SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName);
            Query.AddJoin(result.GetColumnReference(BoldDefs.IDCOLUMN_NAME), TableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            Query.AddJoin(result.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME), TableRef.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME));
            return result;
        }

        void AddConsecutiveTimeJoin(TBoldSQLQuery Query, TBoldSQLTableReference firstTableRef, TBoldSQLTableReference nextTableRef)
        {
            TBoldSQLColumnReference startColumnRef = nextTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME);
            TBoldSQLColumnReference stopColumnRef = firstTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTOPCOLUMNNAME);
            Query.AddWCF(new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(startColumnRef),
                new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(stopColumnRef), new TBoldSQLWCFInteger(1), "+"), "="));
        }

        TBoldMemberMappingInfo[] MappingInfos = SystemPersistenceMapper.MappingInfo.GetMemberMappings(ObjectPersistenceMapper.ExpressionName, ExpressionName);
        foreach (TBoldMemberMappingInfo mapping in MappingInfos)
        {
            TBoldSQLQuery anSQLQuery = MakeMainQuery(out TBoldSQLTableReference objTableRef, mapping);
            TBoldSQLQuery subQuery = MakeSubQuery(anSQLQuery, objTableRef, out TBoldSQLTableReference obj2TableRef, mapping);
            TBoldSQLTableReference rootTableRef = JoinRootTableInto(anSQLQuery, objTableRef);
            AddConsecutiveTimeJoin(subQuery, rootTableRef, obj2TableRef);
            anSQLQuery.AddColumnToRetrieve(rootTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTOPCOLUMNNAME));
            FetchIds(anSQLQuery, 1);

            anSQLQuery = MakeMainQuery(out objTableRef, mapping);
            subQuery = MakeSubQuery(anSQLQuery, objTableRef, out obj2TableRef, mapping);
            rootTableRef = JoinRootTableInto(subQuery, obj2TableRef);
            AddConsecutiveTimeJoin(subQuery, rootTableRef, objTableRef);
            anSQLQuery.AddColumnToRetrieve(objTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME));
            FetchIds(anSQLQuery, 0);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectId NewDefaultId(int IdValue, TBoldTimeStampType TimeStamp)
    {
        TBoldDefaultID ObjectId = TimeStamp != BoldDefs.BOLDMAXTIMESTAMP
            ? new TBoldTimestampedDefaultId(TimeStamp, fOtherEndObjectPMIndex, false)
            : new TBoldDefaultID(fOtherEndObjectPMIndex, false);
        ObjectId.AsInteger = IdValue;
        return ObjectId;
    }

    [Delphi(PortStatus.Ported)]
    private string GetMainColumnName() => ColumnDescriptions[0].SQLName;

    [Delphi(PortStatus.Ported)]
    private string GetOrderColumnName() => ColumnDescriptions[1].SQLName;

    [Delphi(PortStatus.Ported)]
    private int GetOtherEndMemberPMIndex() => OtherEndObjectMapper.MemberMapperIndexByMemberIndex(fOtherEndMemberIndex);

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => ColumnIndex is 0 or 1
        ? SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger
        : throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnTypeAsSQL", ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected override string GetAllowNullAsSQL() => "";

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnCount() => fColumnCount;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => ColumnIndex is 0 or 1
        ? TFieldType.ftInteger
        : throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnBDEFieldType", ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnSize(int ColumnIndex) => 0;

    [Delphi(PortStatus.Ported)]
    protected override string GetInitialColumnName(int ColumnIndex) => ColumnIndex switch
    {
        0 => InitialColumnRootName,
        1 => InitialColumnRootName + "_O",
        _ => throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetInitialColumnName", ColumnIndex)),
    };

    /// <summary>Column 0: the same id (or both none); column 1: the same order number (when there is a link).</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        var anIdRef = (IBoldObjectIdRef)GetValue(ObjectContent)!;
        bool fieldIsNull = Field.IsNull || Field.AsInteger == BoldDefs.INTERNALNULLKEY;
        return ColumnIndex switch
        {
            0 => fieldIsNull ? anIdRef.Id is null : anIdRef.Id is not null && SysUtils.IntToStr(Field.AsInteger) == anIdRef.Id.AsString,
            1 => fieldIsNull || anIdRef.Id is null || Field.AsInteger == anIdRef.OrderNo,
            _ => true,
        };
    }

    /// <summary>Adds an index on each of the member's columns (in the class that defines the member).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        base.InitializePSDescriptions();
        if (!IsInherited)
        {
            var Columns = new TStringList();
            foreach (TBoldMemberMappingInfo mapping in SystemPersistenceMapper.MappingInfo.GetMemberMappings(ObjectPersistenceMapper.ExpressionName, ExpressionName))
            {
                Columns.CommaText = mapping.Columns;
                for (int j = 0; j < Columns.Count; j++)
                {
                    SystemPersistenceMapper.EnsureIndex(mapping.TableName, Columns[j], false, false, ObjectPersistenceMapper.Versioned);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper GetOtherEndObjectMapper() => ObjectPersistenceMappers(OtherEndObjectPMIndex);

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => SystemPersistenceMapper.SQLDataBaseConfig.CorrectlyQuotedDefaultValue("-1");

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_ObjectIdRef) == 0;

    /// <summary>Column 0: the other end's (translated) id, or the null key; column 1: the order number.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList)
    {
        var SingleLink = (IBoldObjectIdRef)GetEnsuredValue(ObjectContent)!;
        switch (ColumnIndex)
        {
            case 0:
                Param.AsInteger = SingleLink.Id is not null ? ((TBoldDefaultID)TranslationList.TranslateToNewId(SingleLink.Id)!).AsInteger : BoldDefs.INTERNALNULLKEY;
                break;
            case 1:
                Param.AsInteger = SingleLink.OrderNo;
                break;
            default:
                throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "ValueToParam", ColumnIndex));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex)
    {
        var anIdRef = (IBoldObjectIdRef)GetEnsuredValue(ObjectContent)!;
        switch (ColumnIndex)
        {
            case 0:
                if (!Field.IsNull && Field.AsInteger != BoldDefs.INTERNALNULLKEY)
                {
                    anIdRef.SetFromId(BoldPMappersLinkDefault.CreateAndEnsureId(Field.AsInteger, OtherEndObjectPMIndex, OtherEndExact, TranslationList, ValueSpace,
                        OwningObjectId.TimeStamp));
                }
                else
                {
                    anIdRef.SetFromId(null);
                }

                break;
            case 1:
                anIdRef.OrderNo = Field.AsInteger;
                break;
            default:
                throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "ValueFromField", ColumnIndex));
        }
    }

    [Delphi(PortStatus.Ported)] public int OtherEndObjectPMIndex => fOtherEndObjectPMIndex;
    [Delphi(PortStatus.Ported)] public int OtherEndMemberPMIndex => GetOtherEndMemberPMIndex();
    [Delphi(PortStatus.Ported)] public bool OtherEndExact => fOtherEndExact;
    [Delphi(PortStatus.Ported)] public bool IsInherited => fIsInherited;
    [Delphi(PortStatus.Ported)] public string OrderColumnName => GetOrderColumnName();
    [Delphi(PortStatus.Ported)] public string MainColumnName => GetMainColumnName();
}

/// <summary>
/// A link not stored in the object: it is fetched by selecting the rows of the closest other end (the other class, or the
/// link class for indirect links) that point back at the objects.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldNonEmbeddedLinkDefaultMapper : TBoldLinkDefaultMapper
{
    protected int fClosestOtherEndObjectMapperIndex;
    protected int fClosestOtherEndMemberIndex;
    protected int fRemoteInnerLinkMemberIndex;
    protected bool fIsIndirect;
    protected int fRemoteOtherEndObjectMapperIndex;

    [Delphi(PortStatus.Ported, Symbol = "TBoldNonEmbeddedLinkDefaultMapper.CreateFromMold")]
    protected TBoldNonEmbeddedLinkDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        fCustomFetch = true;
        fIsStoredInObject = IsStoredInObject && ((TMoldRole)moldMember!).RoleType is TBoldRoleType.rtRole or TBoldRoleType.rtInnerLinkRole;
    }

    [Delphi(PortStatus.Ported)]
    private int GetClosestOtherEndMemberMapperIndex() => ClosestOtherEndObjectMapper.MemberMapperIndexByMemberIndex(fClosestOtherEndMemberIndex);

    /// <summary>Selects the link rows (id, type, the embedding column, the order and the remote end) of the objects in the where fragment.</summary>
    [Delphi(PortStatus.Ported)]
    private void ProcessSQL(IBoldQuery Query, string WhereFragment, List<TTempLinkValues> resultList, TBoldTimeStampType TimeStamp)
    {
        const string LinkTableAlias = "LinkTable_Alias";
        const string RootTableAlias = "RootTable_Alias";
        var SelectList = new TStringList();
        var sql = new TStringList();
        TBoldMemberMappingInfo[] MappingInfo = SystemPersistenceMapper.MappingInfo.GetMemberMappings(ClosestOtherEndObjectMapper.ExpressionName, EmbeddingMapper()!.ExpressionName);
        foreach (TBoldMemberMappingInfo mapping in MappingInfo)
        {
            string EmbeddingColumnName = mapping.ColumnByIndex(0);
            string EmbeddingOrderColumnName = Ordered ? mapping.ColumnByIndex(1) : "";
            SelectList.Clear();
            // ID, TYPE, ClosstId, [OderColumn], [RemoteColumn]
            SelectList.Add(LinkTableAlias + "." + BoldDefs.IDCOLUMN_NAME);
            SelectList.Add(LinkTableAlias + "." + BoldDefs.TYPECOLUMN_NAME); // FIXME hardwired
            SelectList.Add(LinkTableAlias + "." + EmbeddingColumnName);
            // there is no need to add an OrderBy clause since the list will be ordered in memory prior to processing
            if (Ordered)
            {
                SelectList.Add(EmbeddingOrderColumnName);
            }

            if (IsIndirect)
            {
                SelectList.Add(RemoteInnerLinkMapper!.MainColumnName);
            }

            sql.Clear();
            sql.Add(SysUtils.Format("SELECT %s", BoldUtils.BoldSeparateStringList(SelectList, ", ", "", "")));
            sql.Add("FROM " + mapping.TableName + " " + LinkTableAlias);
            string rootTableName = SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName;
            bool OperatingOnRootTable = SysUtils.SameText(mapping.TableName, rootTableName);
            string RootTableJoin = SysUtils.Format("((%s.%s = %s.%s) and (%s.%s = %s.%s))",
                LinkTableAlias, BoldDefs.TIMESTAMPSTARTCOLUMNNAME, RootTableAlias, BoldDefs.TIMESTAMPSTARTCOLUMNNAME,
                LinkTableAlias, BoldDefs.IDCOLUMN_NAME, RootTableAlias, BoldDefs.IDCOLUMN_NAME);

            // Add the root table if it is needed and not already there
            bool sql92 = SystemPersistenceMapper.SQLDataBaseConfig.UseSQL92Joins;
            if (ClosestOtherEndObjectMapper.Versioned && !OperatingOnRootTable)
            {
                sql.Add(sql92
                    ? SysUtils.Format(" left join %s %s on %s", rootTableName, RootTableAlias, RootTableJoin)
                    : SysUtils.Format(", %s %s", rootTableName, RootTableAlias));
            }

            sql.Add(SysUtils.Format("WHERE (%s.%s) %s", LinkTableAlias, EmbeddingColumnName, WhereFragment));
            bool ClassIdRequired = true;
            foreach (TBoldAllInstancesMappingInfo ai in SystemPersistenceMapper.MappingInfo.GetAllInstancesMapping(ClosestOtherEndObjectMapper.ExpressionName))
            {
                if (SysUtils.SameText(ai.TableName, mapping.TableName) && !ai.ClassIdRequired)
                {
                    ClassIdRequired = false;
                }
            }

            if (ClassIdRequired)
            {
                sql.Add(SysUtils.Format("AND (%s in (%s))", BoldDefs.TYPECOLUMN_NAME, ClosestOtherEndObjectMapper.SubClassesID));
            }

            // add timestamp conditions and nessesary joins
            if (ClosestOtherEndObjectMapper.Versioned)
            {
                ClosestOtherEndObjectMapper.RetrieveTimeStampCondition(sql, TimeStamp, false, "AND", true, LinkTableAlias, OperatingOnRootTable ? LinkTableAlias : RootTableAlias);
                if (!OperatingOnRootTable && !sql92)
                {
                    sql.Add(SysUtils.Format("and %s", RootTableJoin));
                }
            }

            Query.AssignSQL(sql);
            Query.Open();
            while (!Query.Eof)
            {
                var LinkData = new TTempLinkValues
                {
                    ClosestId = Query.Fields(0).AsInteger,
                    ClosestClassid = Query.Fields(1).AsInteger,
                    ObjectId = Query.Fields(2).AsInteger,
                };
                int NextColumnIndex = 3;
                if (Ordered)
                {
                    LinkData.Ordervalue = Query.Fields(NextColumnIndex).AsInteger;
                    NextColumnIndex++;
                }

                if (IsIndirect)
                {
                    LinkData.RemoteId = Query.Fields(NextColumnIndex).AsInteger;
                }

                resultList.Add(LinkData);
                Query.Next();
            }
        }
    }

    /// <summary>Sets (or compares) the link of each object from the sorted rows; objects without rows get empty links.</summary>
    [Delphi(PortStatus.Ported)]
    private void ProcessResult(List<TTempLinkValues> ResultList, IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList,
        TBoldTimeStampType timeStamp, int FetchMode, TBoldObjectIdList? FailureList)
    {
        bool RemoteOtherEndExact = IsIndirect && !ObjectPersistenceMappers(RemoteOtherEndObjectMapperIndex).HasSubClasses;
        TBoldObjectIdList UnprocessedObjects = ObjectIdList.Clone();
        var ListOfClosestEnd = new TBoldObjectIdList();
        var ListOfRemoteEnd = new TBoldObjectIdList();
        var MemberID = new TBoldMemberID(MemberIndex);

        void ProcessResultForOneObject(TBoldObjectId CurrentObjectId)
        {
            IBoldObjectContents ObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(CurrentObjectId);
            ObjectContents.EnsureMember(MemberID, ContentName);
            IBoldValue MemberInterface = ObjectContents.ValueByMemberId(MemberID)!;
            if (FetchMode == BoldDefs.fmCompare)
            {
                int OldFailureCount = FailureList!.Count;
                CompareValuesToLists(CurrentObjectId, MemberInterface, ListOfClosestEnd, ListOfRemoteEnd, FailureList, TranslationList);
                if (FailureList.Count > OldFailureCount)
                {
                    TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
                    BoldLog.LogFmt(BoldPMConsts.sOptimisticLockingFailedForTheFollowing, [ObjectPersistenceMapper.ExpressionName, ExpressionName]);
                    for (int i = OldFailureCount; i < FailureList.Count; i++)
                    {
                        BoldLog.LogFmt(BoldPMConsts.sLogIdAsString, [FailureList[i].AsString]);
                    }
                }
            }
            else if (MemberInterface.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
            {
                StuffValuesFromLists(MemberInterface, ListOfClosestEnd, ListOfRemoteEnd);
            }

            ListOfClosestEnd.Clear();
            ListOfRemoteEnd.Clear();
            UnprocessedObjects.RemoveByIndex(UnprocessedObjects.IndexByID(CurrentObjectId));
        }

        void EnsureAndAddToList(TBoldObjectIdList List, int ClassId, bool Exact, int IdAsInteger) =>
            List.Add(BoldPMappersLinkDefault.CreateAndEnsureId(IdAsInteger, ClassId, Exact, TranslationList, ValueSpace, timeStamp));

        TBoldObjectId FindInListByIdAsInteger(TBoldObjectIdList List, int IdAsInteger)
        {
            var BoldId = new TBoldTimestampedDefaultId { AsInteger = IdAsInteger, TimeStamp = timeStamp };
            return List.IDByID(BoldId)!;
        }

        if (ResultList.Count > 0)
        {
            int CurrentId = ResultList[0].ObjectId;
            foreach (TTempLinkValues CurrentResult in ResultList)
            {
                if (CurrentId != CurrentResult.ObjectId)
                {
                    ProcessResultForOneObject(FindInListByIdAsInteger(UnprocessedObjects, CurrentId));
                    CurrentId = CurrentResult.ObjectId;
                }

                int TranslatedClassId = SystemPersistenceMapper.TopSortedIndexForBoldDbType(CurrentResult.ClosestClassid);
                EnsureAndAddToList(ListOfClosestEnd, TranslatedClassId, true, CurrentResult.ClosestId);
                if (IsIndirect)
                {
                    EnsureAndAddToList(ListOfRemoteEnd, RemoteOtherEndObjectMapperIndex, RemoteOtherEndExact, CurrentResult.RemoteId);
                }
            }

            ProcessResultForOneObject(FindInListByIdAsInteger(UnprocessedObjects, CurrentId));
        }

        // clear links for objects that had no related objects in the database
        while (UnprocessedObjects.Count > 0)
        {
            ProcessResultForOneObject(UnprocessedObjects[0]); // Lists empty at this point
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetLinkClassTableName() => LinkClassObjectMapper!.MainTable!.SQLName;

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldObjectDefaultMapper? GetLinkClassObjectMapper();

    [Delphi(PortStatus.Ported)]
    private string GetClosestColumnName() => EmbeddingMapper()!.MainColumnName;

    [Delphi(PortStatus.Ported)] protected int ClosestOtherEndObjectMapperIndex => fClosestOtherEndObjectMapperIndex;
    [Delphi(PortStatus.Ported)] protected int ClosestOtherEndMemberMapperIndex => GetClosestOtherEndMemberMapperIndex();
    [Delphi(PortStatus.Ported)] protected int RemoteOtherEndObjectMapperIndex => fRemoteOtherEndObjectMapperIndex;

    [Delphi(PortStatus.Ported)]
    private TBoldEmbeddedSingleLinkDefaultMapper? GetRemoteInnerLinkMapper()
    {
        if (!IsIndirect)
        {
            return null;
        }

        TBoldObjectDefaultMapper ObjectMapper = ObjectPersistenceMappers(ClosestOtherEndObjectMapperIndex);
        return (TBoldEmbeddedSingleLinkDefaultMapper?)ObjectMapper.MemberPersistenceMappers[ObjectMapper.MemberMapperIndexByMemberIndex(fRemoteInnerLinkMemberIndex)];
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectDefaultMapper GetClosestOtherEndObjectMapper() => ObjectPersistenceMappers(ClosestOtherEndObjectMapperIndex);

    [Delphi(PortStatus.Ported)]
    protected abstract bool GetIsOrdered();

    [Delphi(PortStatus.Ported)]
    protected internal override void GetChangePoints(TBoldObjectIdList ObjectIDList, TBoldChangePointCondition Condition, TBoldSQLNameSpace NameSpace) =>
        ((TBoldEmbeddedSingleLinkDefaultMapper)ObjectPersistenceMappers(fClosestOtherEndObjectMapperIndex).MemberPersistenceMappers[ClosestOtherEndMemberMapperIndex]!)
            .GetNonEmbeddedChangePoints(ObjectIDList, Condition, NameSpace);

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnCount() => 0;

    /// <summary>Abstract and never called in Delphi (the mapper has no columns); C# needs an implementation.</summary>
    protected override string GetColumnTypeAsSQL(int ColumnIndex) =>
        throw new EBoldBadColumnIndex(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnTypeAsSQL", ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected abstract void CompareValuesToLists(TBoldObjectId OwningId, IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd,
        TBoldObjectIdList FailureList, TBoldIDTranslationList TranslationList);

    [Delphi(PortStatus.Ported)]
    protected abstract void StuffValuesFromLists(IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd);

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCannotCallOnTransientClass, GetType().Name));

    /// <summary>The embedded single link at the closest other end, if it is one.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldEmbeddedSingleLinkDefaultMapper? EmbeddingMapper() =>
        ObjectPersistenceMappers(ClosestOtherEndObjectMapperIndex).MemberPersistenceMappers[ClosestOtherEndMemberMapperIndex] as TBoldEmbeddedSingleLinkDefaultMapper;

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper GetOtherEndObjectMapper() => ObjectPersistenceMappers(RemoteOtherEndObjectMapperIndex);

    [Delphi(PortStatus.Ported)]
    protected override bool GetSupportsPolymorphicFetch() => true;

    /// <summary>Fetches the links of objects of the class (or its subclasses) at one time stamp: rows in blocks, processed together.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, int FetchMode, TBoldIDTranslationList TranslationList, TBoldObjectIdList? FailureList)
    {
        if (ObjectIDList.Count == 0)
        {
            return;
        }

        int TopSortedIndex = ObjectPersistenceMapper.TopSortedIndex;

        // splitting on timestamps is performed by the objectpersistencemapper already
        TBoldTimeStampType TimeStamp = ObjectIDList[0].TimeStamp;
        for (int I = 0; I < ObjectIDList.Count; I++)
        {
            bool homogenous = SupportsPolymorphicFetch
                ? SystemPersistenceMapper.ObjectPersistenceMappers[ObjectIDList[I].TopSortedIndex]!.BoldIsA(ObjectPersistenceMapper)
                : ObjectIDList[I].TopSortedIndex == TopSortedIndex;
            if (!homogenous)
            {
                throw new EBoldInternal(SysUtils.Format("%s.PMFetch: Not homogenous Object-list", GetType().Name));
            }

            if (ObjectIDList[I].TimeStamp != TimeStamp)
            {
                throw new EBoldInternal(SysUtils.Format("%s.PMFetch: Objectlist contains multiple timestamps", GetType().Name));
            }
        }

        var ResultList = new List<TTempLinkValues>();
        IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
        try
        {
            // only do the fetching in blocks, the actual processing must be done in one go
            // since ProcessResult will clear all objects with empty links
            // besides, the point of blockfetching is mainly to reduce the size of the SQL-statement
            BoldPMappersDefault.ForEachBlock(ObjectIDList.Count, SystemPersistenceMapper.SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
            {
                aQuery.ClearParams();
                string WhereFragment = ObjectPersistenceMapper.IdListSegmentToWhereFragment(ObjectIDList, Start, Stop, aQuery);
                ProcessSQL(aQuery, WhereFragment, ResultList, TimeStamp);
            });

            // Delphi's TList.Sort (quicksort) is not stable; rows that compare equal are identical here, so a stable sort gives the same result.
            var sorted = ResultList.OrderBy(x => x, Comparer<TTempLinkValues>.Create(BoldPMappersLinkDefault.SortLinkValues)).ToList();
            BoldPMappers.BoldPMLogFmt(BoldPMConsts.sLogFetchIDs, sorted.Count, ObjectIDList.Count, ObjectPersistenceMapper.ExpressionName, ExpressionName);
            ProcessResult(sorted, ValueSpace, ObjectIDList, TranslationList, TimeStamp, FetchMode, FailureList);
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref aQuery);
        }
    }

    [Delphi(PortStatus.Ported)] public string ClosestColumnName => GetClosestColumnName();
    [Delphi(PortStatus.Ported)] public TBoldEmbeddedSingleLinkDefaultMapper? RemoteInnerLinkMapper => GetRemoteInnerLinkMapper();
    [Delphi(PortStatus.Ported)] public TBoldObjectDefaultMapper ClosestOtherEndObjectMapper => GetClosestOtherEndObjectMapper();
    [Delphi(PortStatus.Ported)] public bool IsIndirect => fIsIndirect;
    [Delphi(PortStatus.Ported)] public string LinkClassTablename => GetLinkClassTableName();
    [Delphi(PortStatus.Ported)] public TBoldObjectDefaultMapper? LinkClassObjectMapper => GetLinkClassObjectMapper();
    [Delphi(PortStatus.Ported)] public bool Ordered => GetIsOrdered();
}

/// <summary>A single link not stored in the object.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSingleLinkDefaultMapper : TBoldNonEmbeddedLinkDefaultMapper
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    protected TBoldSingleLinkDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetIsOrdered() => false;

    [Delphi(PortStatus.Ported)]
    protected static TBoldObjectId? FirstIdInList(TBoldObjectIdList List)
    {
        System.Diagnostics.Debug.Assert(List.Count <= 1);
        return List.Count == 0 ? null : List[0];
    }
}

/// <summary>A single link whose other end embeds the link.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDirectSingleLinkDefaultmapper : TBoldSingleLinkDefaultMapper
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldDirectSingleLinkDefaultmapper.CreateFromMold")]
    public TBoldDirectSingleLinkDefaultmapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        var Role = (TMoldRole)moldMember!;
        TMoldClass ClassOfOtherEnd = Role.OtherEnd!.MoldClass!;
        fClosestOtherEndObjectMapperIndex = ClassOfOtherEnd.TopSortedIndex;
        fClosestOtherEndMemberIndex = ClassOfOtherEnd.AllBoldMembers.IndexOf(Role.OtherEnd);
        fRemoteOtherEndObjectMapperIndex = fClosestOtherEndObjectMapperIndex;
        fRemoteInnerLinkMemberIndex = -1;
    }

    [Delphi(PortStatus.Ported)]
    protected override void StuffValuesFromLists(IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd) =>
        ((IBoldObjectIdRef)MemberInterface).SetFromId(FirstIdInList(ListOfClosestEnd));

    [Delphi(PortStatus.Ported)]
    protected override void CompareValuesToLists(TBoldObjectId OwningId, IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd,
        TBoldObjectIdList FailureList, TBoldIDTranslationList TranslationList)
    {
        var IdRef = (IBoldObjectIdRef)MemberInterface;
        bool failed = IdRef.Id is not null
            ? ListOfClosestEnd.Count != 1 || !ListOfClosestEnd[0].IsEqual(TranslationList.TranslateToNewId(IdRef.Id))
            : ListOfClosestEnd.Count != 0;
        if (failed)
        {
            FailureList.AddIfNotInList(OwningId);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper? GetLinkClassObjectMapper() => null;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_ObjectIdRef) == 0;
}

/// <summary>A single link through a link class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndirectSingleLinkDefaultmapper : TBoldSingleLinkDefaultMapper
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldIndirectSingleLinkDefaultmapper.CreateFromMold")]
    public TBoldIndirectSingleLinkDefaultmapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        var Role = (TMoldRole)moldMember!;
        fIsIndirect = true;
        TMoldClass LinkClass = Role.Association!.LinkClass!;
        fClosestOtherEndObjectMapperIndex = LinkClass.TopSortedIndex;
        fClosestOtherEndMemberIndex = LinkClass.AllBoldMembers.IndexOf(Role.LinkRole!.OtherEnd);
        fRemoteOtherEndObjectMapperIndex = Role.OtherEnd!.MoldClass!.TopSortedIndex;
        fRemoteInnerLinkMemberIndex = LinkClass.AllBoldMembers.IndexOf(Role.OtherEnd.LinkRole!.OtherEnd);
        if (LinkClass.TableMapping == TTableMapping.tmChildren)
        {
            throw new EBoldFeatureNotImplementedYet(SysUtils.Format(BoldPMConsts.sChildMappedLinkClassesNotSupported, moldClass.Name, Role.Name, LinkClass.Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void StuffValuesFromLists(IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd) =>
        ((IBoldObjectIdRefPair)MemberInterface).SetFromIds(FirstIdInList(ListOfClosestEnd), FirstIdInList(ListOfRemoteEnd));

    [Delphi(PortStatus.Ported)]
    protected override void CompareValuesToLists(TBoldObjectId OwningId, IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd,
        TBoldObjectIdList FailureList, TBoldIDTranslationList TranslationList)
    {
        var IdRefPair = (IBoldObjectIdRefPair)MemberInterface;
        bool failed = IdRefPair.Id1 is not null
            ? ListOfClosestEnd.Count != 1 || !ListOfClosestEnd[0].IsEqual(TranslationList.TranslateToNewId(IdRefPair.Id1)) ||
              ListOfRemoteEnd.Count != 1 || !ListOfRemoteEnd[0].IsEqual(TranslationList.TranslateToNewId(IdRefPair.Id2))
            : ListOfClosestEnd.Count != 0;
        if (failed)
        {
            FailureList.AddIfNotInList(OwningId);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper? GetLinkClassObjectMapper() => ObjectPersistenceMappers(ClosestOtherEndObjectMapperIndex);

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair) == 0;
}

/// <summary>A multi link, ordered or not.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMultiLinkDefaultMapper : TBoldNonEmbeddedLinkDefaultMapper
{
    private readonly bool FOrdered;

    [Delphi(PortStatus.Ported, Symbol = "TBoldMultiLinkDefaultMapper.CreateFromMold")]
    protected TBoldMultiLinkDefaultMapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        FOrdered = ((TMoldRole)moldMember!).EffectiveOrdered;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetIsOrdered() => FOrdered;
}

/// <summary>A multi link whose other ends embed the link.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDirectMultiLinkDefaultmapper : TBoldMultiLinkDefaultMapper
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldDirectMultiLinkDefaultmapper.CreateFromMold")]
    public TBoldDirectMultiLinkDefaultmapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        var Role = (TMoldRole)moldMember!;
        TMoldClass ClassOfOtherEnd = Role.OtherEnd!.MoldClass!;
        fClosestOtherEndObjectMapperIndex = ClassOfOtherEnd.TopSortedIndex;
        fClosestOtherEndMemberIndex = ClassOfOtherEnd.AllBoldMembers.IndexOf(Role.OtherEnd);
        fRemoteOtherEndObjectMapperIndex = fClosestOtherEndObjectMapperIndex;
        fRemoteInnerLinkMemberIndex = -1;
    }

    [Delphi(PortStatus.Ported)]
    protected override void StuffValuesFromLists(IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd) =>
        ((IBoldObjectIdListRef)MemberInterface).SetFromIdList(ListOfClosestEnd);

    /// <summary>
    /// Compares the lists unordered even for ordered links, since the main purpose is consistency and the order of the
    /// optimistic locking value can not be guaranteed.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void CompareValuesToLists(TBoldObjectId OwningId, IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd,
        TBoldObjectIdList FailureList, TBoldIDTranslationList TranslationList)
    {
        var IdList = (IBoldObjectIdListRef)MemberInterface;
        bool Equal = IdList.Count == ListOfClosestEnd.Count;
        if (Equal)
        {
            for (int i = 0; i < IdList.Count; i++)
            {
                Equal = Equal && ListOfClosestEnd.IdInList(TranslationList.TranslateToNewId(IdList.IdList(i)));
            }
        }

        if (!Equal)
        {
            FailureList.AddIfNotInList(OwningId);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper? GetLinkClassObjectMapper() => null;

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_ObjectIdListRef) == 0;
}

/// <summary>A multi link through a link class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndirectMultiLinkDefaultmapper : TBoldMultiLinkDefaultMapper
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldIndirectMultiLinkDefaultmapper.CreateFromMold")]
    public TBoldIndirectMultiLinkDefaultmapper(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        var Role = (TMoldRole)moldMember!;
        fIsIndirect = true;
        TMoldClass LinkClass = Role.Association!.LinkClass!;
        fClosestOtherEndObjectMapperIndex = LinkClass.TopSortedIndex;
        fClosestOtherEndMemberIndex = LinkClass.AllBoldMembers.IndexOf(Role.LinkRole!.OtherEnd);
        fRemoteOtherEndObjectMapperIndex = Role.OtherEnd!.MoldClass!.TopSortedIndex;
        fRemoteInnerLinkMemberIndex = LinkClass.AllBoldMembers.IndexOf(Role.OtherEnd.LinkRole!.OtherEnd);
        if (LinkClass.TableMapping == TTableMapping.tmChildren)
        {
            throw new EBoldFeatureNotImplementedYet(SysUtils.Format(BoldPMConsts.sChildMappedLinkClassesNotSupported, moldClass.Name, Role.Name, LinkClass.Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void StuffValuesFromLists(IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd) =>
        ((IBoldObjectIdListRefPair)MemberInterface).SetFromIdLists(ListOfClosestEnd, ListOfRemoteEnd);

    [Delphi(PortStatus.Ported)]
    protected override void CompareValuesToLists(TBoldObjectId OwningId, IBoldValue MemberInterface, TBoldObjectIdList ListOfClosestEnd, TBoldObjectIdList ListOfRemoteEnd,
        TBoldObjectIdList FailureList, TBoldIDTranslationList TranslationList)
    {
        var IdLists = (IBoldObjectIdListRefPair)MemberInterface;
        bool Equal = IdLists.Count == ListOfClosestEnd.Count;
        if (Equal)
        {
            for (int i = 0; i < IdLists.Count; i++)
            {
                Equal = Ordered
                    ? Equal && ListOfClosestEnd[i].IsEqual(TranslationList.TranslateToNewId(IdLists.IdList1(i))) &&
                      ListOfRemoteEnd[i].IsEqual(TranslationList.TranslateToNewId(IdLists.IdList2(i)))
                    : Equal && ListOfClosestEnd.IdInList(TranslationList.TranslateToNewId(IdLists.IdList1(i))) &&
                      ListOfRemoteEnd.IdInList(TranslationList.TranslateToNewId(IdLists.IdList2(i)));
            }
        }

        if (!Equal)
        {
            FailureList.AddIfNotInList(OwningId);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectDefaultMapper? GetLinkClassObjectMapper() => ObjectPersistenceMappers(ClosestOtherEndObjectMapperIndex);

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => SysUtils.AnsiCompareText(ContentName, BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair) == 0;
}
