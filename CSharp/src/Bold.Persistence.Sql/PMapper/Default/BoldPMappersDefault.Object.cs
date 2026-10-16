// Delphi: Source/PMapper/Default/BoldPMappersDefault.pas
// Port map: CSharp/port/units/BoldPMappersDefault.md

namespace Bold;

/// <summary>
/// The default mapper of a class: generates its mapping information, creates, updates (or versions) and deletes rows in
/// its tables, fetches objects by id lists and conditions, and compares them for optimistic locking.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectDefaultMapper : TBoldObjectSQLMapper
{
    private string fSubClassesID = "";
    private readonly TBoldModelVersionMember? fModelVersionMember;
    internal readonly TBoldOptimisticLockingMode fOptimisticLockingMode;

    /// <summary>Adds the technical members (model version, read-only, time stamp, global id) and generates the class's mapping information.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectDefaultMapper.CreateFromMold")]
    public TBoldObjectDefaultMapper(TMoldClass moldClass, TBoldSystemPersistenceMapper Owner, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldClass, Owner, TypeNameDictionary)
    {
        fOptimisticLockingMode = moldClass.EffectiveOptimisticLocking;
        if (SystemPersistenceMapper.UseModelVersion)
        {
            fModelVersionMember = new TBoldModelVersionMember(null, moldClass, this, -1, TypeNameDictionary);
            MemberPersistenceMappers.Add(fModelVersionMember);
        }

        if (SystemPersistenceMapper.UseReadOnly)
        {
            MemberPersistenceMappers.Add(new TBoldReadOnlynessMember(null, moldClass, this, -1, TypeNameDictionary));
        }

        if (SystemPersistenceMapper.UseXFiles)
        {
            if (SystemPersistenceMapper.UseTimestamp)
            {
                MemberPersistenceMappers.Add(new TBoldTimeStampMember(null, moldClass, this, -1, TypeNameDictionary));
            }

            if (SystemPersistenceMapper.UseGlobalId)
            {
                MemberPersistenceMappers.Add(new TBoldGlobalIdMember(null, moldClass, this, -1, TypeNameDictionary));
            }
        }

        fObjectIdClass = BoldDefaultStreamNames.BOLDDEFAULTIDNAME;
        for (int i = 0; i < moldClass.AllPossibleNames.Count; i++)
        {
            GenerateMappingInfo(moldClass.AllPossibleNames[i], moldClass);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemDefaultMapper GetSystemPersistenceMapper() => (TBoldSystemDefaultMapper)base.SystemPersistenceMapper;

    /// <summary>Sets the stop time of the current versions of the objects to just before the current time stamp.</summary>
    [Delphi(PortStatus.Ported)]
    private void PMUpdateStopTime(TBoldObjectIdList ObjectIDList)
    {
        var IdStringList = new TStringList();
        IBoldExecQuery? UpdateQuery = SystemPersistenceMapper.GetExecQuery();
        try
        {
            for (int i = 0; i < ObjectIDList.Count; i++)
            {
                IdStringList.Add(ObjectIDList[i].AsString);
            }

            UpdateQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = %d WHERE %s in (%s) AND (%s = %d)",
                SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName, BoldDefs.TIMESTAMPSTOPCOLUMNNAME, SystemPersistenceMapper.CurrentTimeStamp - 1,
                BoldDefs.IDCOLUMN_NAME, BoldUtils.BoldSeparateStringList(IdStringList, ",", "", ""), BoldDefs.TIMESTAMPSTOPCOLUMNNAME, BoldDefs.BOLDMAXTIMESTAMP));
            UpdateQuery.ExecSQL();
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref UpdateQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void GetChangePoints(TBoldObjectIdList ObjectIDList, TBoldChangePointCondition Condition, TBoldSQLNameSpace NameSpace)
    {
        if (Condition.MemberIdList is not null)
        {
            for (int i = 0; i < Condition.MemberIdList.Count; i++)
            {
                ((TBoldMemberDefaultMapper)MemberPersistenceMappers[MemberMapperIndexByMemberIndex(Condition.MemberIdList[i].MemberIndex)]!)
                    .GetChangePoints(ObjectIDList, Condition, NameSpace);
            }
        }
    }

    /// <summary>
    /// Fetches (or compares) the objects of the list at one time stamp, in blocks; missing objects are marked deleted, old
    /// versions are upgraded and fetched again, and custom members are fetched by their own mappers.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void PMMultiPurposeRetrieveExactIdList(TBoldObjectIdList ObjectsToFetch, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList, TBoldObjectIdList? MissingList, TBoldObjectIdList? FailureList, TBoldTimeStampType TimeStamp)
    {
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        var CustomMembers = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
        var sql = new TStringList();
        var RefetchIdList = new TBoldObjectIdList();
        int FetchBlockSize = SystemPersistenceMapper.SQLDataBaseConfig.FetchBlockSize;
        try
        {
            BuildMemberFetchLists(MemberIdList, MemberPMList, CustomMembers, FetchMode);
            try
            {
                if (SystemPersistenceMapper.SupportsObjectUpgrading)
                {
                    SystemPersistenceMapper.ObjectUpgrader!.StartTransaction();
                }

                // if the memberidlist is empty, then we must fetch the objects to make sure they still exist in the database
                // this ensures optimistic locking on deleted objects.
                // The below test will skip fetching the object if we are fetching only "custom members"
                if (MemberPMList.Count > 0 || MemberIdList is null || MemberIdList.Count == 0)
                {
                    MissingList?.AddList(ObjectsToFetch);

                    //  We do not want to retrieve more than FetchBlockSize objects at a time
                    BoldPMappersDefault.ForEachBlock(ObjectsToFetch.Count, FetchBlockSize, (Start, Stop) =>
                    {
                        sql.Clear();
                        RetrieveSelectStatement(sql, MemberPMList, FetchMode, Versioned);
                        if (Stop >= Start)
                        {
                            aQuery.ClearParams();
                            BoldUtils.BoldAppendToStrings(sql, IdListSegmentToWhereFragment(ObjectsToFetch, Start, Stop, aQuery), false);
                            if (Versioned)
                            {
                                RetrieveTimeStampCondition(sql, TimeStamp, true, "AND", false);
                            }

                            int SQLHits = 0;
                            aQuery.AssignSQL(sql);
                            aQuery.Open();
                            while (!aQuery.Eof)
                            {
                                SQLHits++;
                                TBoldObjectId TempId = SystemPersistenceMapper.NewIdFromQuery(aQuery, 1, 0, TimeStamp);
                                TBoldObjectId NewId = ObjectsToFetch.IDByID(TempId)
                                    ?? throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sUninvitedObjectReturnedFromDB, GetType().Name, aQuery.Fields(0).AsInteger));
                                if (MissingList is not null)
                                {
                                    int i = MissingList.IndexByID(NewId);
                                    if (i != -1)
                                    {
                                        MissingList.RemoveByIndex(i);
                                    }
                                }

                                if (!NewId.TopSortedIndexExact)
                                {
                                    NewId = TranslationList.TranslateToNewId(NewId)!;
                                }

                                if (!NewId.TopSortedIndexExact)
                                {
                                    throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sIDExactnessFailure, GetType().Name));
                                }

                                if (TimeStamp == BoldDefs.BOLDMAXTIMESTAMP && MemberIdList is null && SystemPersistenceMapper.SupportsObjectUpgrading && IsOldVersion(aQuery))
                                {
                                    RefetchIdList.Add(NewId);
                                    PortObject(NewId, aQuery);
                                }
                                else
                                {
                                    if (FetchMode == BoldDefs.fmCompare)
                                    {
                                        HandleCompareData(NewId, ValueSpace, TranslationList, aQuery, MemberPMList, FetchMode, FailureList!);
                                    }
                                    else
                                    {
                                        HandleFetchData(NewId, ValueSpace, TranslationList, aQuery, MemberPMList, FetchMode, FailureList);
                                    }

                                    SystemPersistenceMapper.SendExtendedEvent(BoldSubscription.bpeFetchObject, [NewId, ValueSpace]);
                                }

                                if (Versioned && TimeStamp != BoldDefs.BOLDMAXTIMESTAMP)
                                {
                                    ValueSpace.EnsuredObjectContentsByObjectId(NewId).IsReadOnly = true;
                                }

                                aQuery.Next();
                            }

                            aQuery.Close();
                            BoldPMappers.BoldPMLogFmt(BoldPMConsts.sLogFetchedXobjectsOfY, SQLHits, ExpressionName);
                        }
                    });

                    if (MissingList is not null && FetchMode != BoldDefs.fmDistributable && MissingList.Count > 0)
                    {
                        BoldPMappers.BoldPMLogFmt(BoldPMConsts.sLogXObjectsOfYMissing, MissingList.Count, ExpressionName);
                        for (int i = 0; i < MissingList.Count; i++)
                        {
                            IBoldObjectContents ObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(MissingList[i]);
                            ObjectContents.BoldExistenceState = TBoldExistenceState.besDeleted;
                            ObjectContents.IsReadOnly = true;
                        }
                    }
                }

                if (SystemPersistenceMapper.SupportsObjectUpgrading)
                {
                    SystemPersistenceMapper.ObjectUpgrader!.EndTransaction();
                }
            }
            catch
            {
                if (SystemPersistenceMapper.SupportsObjectUpgrading)
                {
                    SystemPersistenceMapper.ObjectUpgrader!.FailTransaction();
                }

                throw;
            }

            if (RefetchIdList.Count > 0)
            {
                PMMultiPurposeRetrieveExactIdList(RefetchIdList, ValueSpace, MemberIdList, FetchMode, TranslationList, MissingList, FailureList, TimeStamp);
            }

            for (int i = 0; i < CustomMembers.Count; i++)
            {
                CustomMembers[i]!.PMFetch(ObjectsToFetch, ValueSpace, FetchMode, TranslationList, FailureList);
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref aQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void HandleCompareData(TBoldObjectId FetchedId, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList, IBoldQuery Query,
        TBoldMemberPersistenceMapperList MemberPMList, int FetchMode, TBoldObjectIdList FailureList)
    {
        if (!CompareFieldsToMembers(FetchedId, ValueSpace, Query, MemberPMList, TranslationList) && !FailureList.IdInList(FetchedId))
        {
            FailureList.Add(FetchedId);
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool CompareFieldsToMembers(TBoldObjectId ObjectID, IBoldValueSpace ValueSpace, IBoldDataSet DataSet, TBoldMemberPersistenceMapperList memberList,
        TBoldIDTranslationList TranslationList)
    {
        bool result = true;
        IBoldObjectContents? ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectID);
        if (ObjectContents is not null)
        {
            for (int i = 0; i < memberList.Count; i++)
            {
                result = result && ((TBoldMemberDefaultMapper)memberList[i]!).CompareFields(ObjectContents, DataSet, ValueSpace, TranslationList);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds new link objects that link the same two objects as a link object in the database (two applications linking
    /// at the same time); those are translated to the existing link objects and listed as duplicates.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void DetectLinkClassDuplicates(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList, TBoldObjectIdList DuplicateList)
    {
        bool ObjectIsNew(TBoldObjectId? Id)
        {
            IBoldObjectContents? Obj = Id is null ? null : ValueSpace.ObjectContentsByObjectId(Id);
            return Obj is not null && Obj.BoldExistenceState == TBoldExistenceState.besExisting && Obj.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified;
        }

        var LinkMapper1 = (TBoldEmbeddedSingleLinkDefaultMapper)LinkClassRole1!;
        var LinkMapper2 = (TBoldEmbeddedSingleLinkDefaultMapper)LinkClassRole2!;

        // if we find a linkobject that links the same two objects as one of the new,
        // translate the new object to the old object.
        string QueryText = SysUtils.Format("SELECT %s, %s FROM %s WHERE (%s = %%s) AND (%s = %%s)",
            BoldDefs.IDCOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME, MainTable!.SQLName, LinkMapper1.MainColumnName, LinkMapper2.MainColumnName);
        IBoldQuery? Query = SystemPersistenceMapper.GetQuery();
        try
        {
            for (int i = 0; i < ObjectIdList.Count; i++)
            {
                IBoldObjectContents LinkObject = ValueSpace.EnsuredObjectContentsByObjectId(ObjectIdList[i]);
                TBoldObjectId? Id1 = ((IBoldObjectIdRef)LinkMapper1.GetValue(LinkObject)!).Id;
                TBoldObjectId? Id2 = ((IBoldObjectIdRef)LinkMapper2.GetValue(LinkObject)!).Id;

                // if either object is new, then this can not be a dupe
                if (!(ObjectIsNew(Id1) || ObjectIsNew(Id2)))
                {
                    if (Id1 is not null)
                    {
                        Id1 = TranslationList.TranslateToNewId(Id1);
                    }

                    if (Id2 is not null)
                    {
                        Id2 = TranslationList.TranslateToNewId(Id2);
                    }

                    // if the linkobject is broken (doesn't have 2 IDs) or is pointing to objects that are deleted,
                    // then we will get an inconsistent database later, but lets not get an AV here...
                    if (Id1 is not null && Id2 is not null)
                    {
                        Query.AssignSQLText(SysUtils.Format(QueryText, Id1.AsString, Id2.AsString));
                        Query.Open();
                        if (!Query.Eof)
                        {
                            TBoldObjectId OldLinkObjectId = SystemPersistenceMapper.NewIdFromQuery(Query, 1, 0, BoldDefs.BOLDMAXTIMESTAMP);
                            // the new linkobject has already received a translation to its persistent ID
                            // we need to add a translation from that ID to the existing link objectid
                            TranslationList.AddTranslation(TranslationList.TranslateToNewId(ObjectIdList[i]), OldLinkObjectId);
                            DuplicateList.Add(ObjectIdList[i]);
                        }

                        Query.Close();
                    }
                }
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref Query);
        }
    }

    /// <summary>
    /// The class's db type, the tables its instances are read from (own, parent's, or the children's) and the tables its
    /// objects are stored in (each superclass with its own table).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void GenerateMappingInfo(string ExpressionName, TMoldClass MoldClass)
    {
        TBoldSQLMappingInfo MappingInfo = SystemPersistenceMapper.MappingInfo;
        TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;

        void AddAllInstancesMappingRecursion(TMoldClass RecMoldClass)
        {
            if (RecMoldClass.TableMapping == TTableMapping.tmOwn)
            {
                string RecTableName = BoldNameExpander.BoldExpandPrefix(RecMoldClass.TableName, RecMoldClass.Name, cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength,
                    MoldClass.Model.NationalCharConversion);
                MappingInfo.AddAllInstancesMapping(ExpressionName, RecTableName, false);
            }
            else if (RecMoldClass.TableMapping == TTableMapping.tmChildren)
            {
                for (int i = 0; i < RecMoldClass.SubClasses.Count; i++)
                {
                    if (RecMoldClass.SubClasses[i].EffectivePersistent)
                    {
                        AddAllInstancesMappingRecursion(RecMoldClass.SubClasses[i]);
                    }
                }
            }
        }

        // Db types mapping
        MappingInfo.AddTypeIdMapping(ExpressionName, MoldClass.TopSortedIndex);

        // write info in AllInstancesmappingTable
        if (MoldClass.TableMapping == TTableMapping.tmOwn)
        {
            string TableName = BoldNameExpander.BoldExpandPrefix(MoldClass.TableName, MoldClass.Name, cfg.SystemTablePrefix, cfg.MaxDbIdentifierLength,
                MoldClass.Model.NationalCharConversion);
            MappingInfo.AddAllInstancesMapping(ExpressionName, TableName, false);
        }
        else if (MoldClass.TableMapping == TTableMapping.tmParent)
        {
            TBoldAllInstancesMappingInfo[] ParentMappings = MappingInfo.GetAllInstancesMapping(MoldClass.SuperClass!.ExpandedExpressionName);
            MappingInfo.AddAllInstancesMapping(ExpressionName, ParentMappings[0].TableName, true);
        }
        else if (MoldClass.TableMapping == TTableMapping.tmChildren)
        {
            AddAllInstancesMappingRecursion(MoldClass);
        }

        // write info in ObjectStorageMappingTable
        if (!MoldClass.IsAbstract)
        {
            TMoldClass? TempSuperClass = MoldClass.SuperClass;
            while (TempSuperClass is not null)
            {
                if (TempSuperClass.TableMapping == TTableMapping.tmOwn)
                {
                    TBoldAllInstancesMappingInfo[] ParentMappings = MappingInfo.GetAllInstancesMapping(TempSuperClass.ExpandedExpressionName);
                    MappingInfo.AddObjectStorageMapping(ExpressionName, ParentMappings[0].TableName);
                }

                TempSuperClass = TempSuperClass.SuperClass;
            }
        }
    }

    /// <summary>
    /// Updates versioned objects: ends the current versions, then copies each table's old rows into new versions with the
    /// changed members and the current time stamp.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void PMTemporalUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        int TickCounter = 0;
        PMUpdateStopTime(ObjectIdList);
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        IBoldExecQuery? aQuery = SystemPersistenceMapper.GetExecQuery();
        IBoldQuery? OldDataQuery = SystemPersistenceMapper.GetQuery();
        var TempList = new TStringList();
        var SQL = new TStringList();
        try
        {
            for (int T = 0; T < AllTables.Count; T++)
            {
                if (AllTables[T] == SystemPersistenceMapper.PSSystemDescription.XFilestable)
                {
                    continue;
                }

                // Clear the memberlist, as we reuse it, and fill it with members to use.
                // FIXME: This will probably not work for members with several tables... /JoHo
                MemberPMList.Clear();
                AddMembersOfTable(MemberPMList, AllTables[T], null);

                SQL.Clear();
                TempList.Clear();
                for (int i = 0; i < ObjectIdList.Count; i++)
                {
                    TempList.Add(ObjectIdList[i].AsString);
                }

                if (AllTables[T].ContainsStopTimeStamp)
                {
                    OldDataQuery.AssignSQLText(SysUtils.Format("SELECT * FROM %s WHERE (%s in %s) AND (%s = %d)",
                        AllTables[T].SQLName, BoldDefs.IDCOLUMN_NAME, BoldUtils.BoldSeparateStringList(TempList, ", ", "(", ")"),
                        BoldDefs.TIMESTAMPSTOPCOLUMNNAME, SystemPersistenceMapper.CurrentTimeStamp - 1));
                }
                else
                {
                    string root = SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName;
                    OldDataQuery.AssignSQLText(
                        "SELECT DataTable.* FROM " + AllTables[T].SQLName + " DataTable, " + root + " RootTable " +
                        "WHERE (DataTable." + BoldDefs.IDCOLUMN_NAME + " in " + BoldUtils.BoldSeparateStringList(TempList, ", ", "(", ")") + ") AND " +
                        "(DataTable." + BoldDefs.IDCOLUMN_NAME + " = RootTable." + BoldDefs.IDCOLUMN_NAME + ") AND " +
                        "(DataTable." + BoldDefs.TIMESTAMPSTARTCOLUMNNAME + " = RootTable." + BoldDefs.TIMESTAMPSTARTCOLUMNNAME + ") AND " +
                        "(RootTable." + BoldDefs.TIMESTAMPSTOPCOLUMNNAME + " = " + SysUtils.IntToStr(SystemPersistenceMapper.CurrentTimeStamp - 1) + ")");
                }

                OldDataQuery.Open();
                TempList.Clear();
                for (int i = 0; i < OldDataQuery.FieldCount; i++)
                {
                    TempList.Add(OldDataQuery.Fields(i).FieldName);
                }

                BoldUtils.BoldAppendToStrings(SQL, SysUtils.Format("INSERT INTO %s (%s) ", AllTables[T].SQLName, BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), true);
                TempList.Clear();
                int IdColumnIndex = -1;
                int TypeColumnIndex = -1;
                for (int i = 0; i < OldDataQuery.FieldCount; i++)
                {
                    TempList.Add(":" + OldDataQuery.Fields(i).FieldName);
                    if (SysUtils.SameText(OldDataQuery.Fields(i).FieldName, BoldDefs.IDCOLUMN_NAME))
                    {
                        IdColumnIndex = i;
                    }

                    if (SysUtils.SameText(OldDataQuery.Fields(i).FieldName, BoldDefs.TYPECOLUMN_NAME))
                    {
                        TypeColumnIndex = i;
                    }
                }

                BoldUtils.BoldAppendToStrings(SQL, SysUtils.Format("VALUES (%s) ", BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), true);
                if (IdColumnIndex == -1 || TypeColumnIndex == -1)
                {
                    throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sTypeAndIDColumnMissing, GetType().Name, aQuery.GetSQLText()));
                }

                aQuery.AssignSQL(SQL);
                while (!OldDataQuery.Eof)
                {
                    TBoldObjectId NewId = SystemPersistenceMapper.NewIdFromQuery(OldDataQuery, TypeColumnIndex, IdColumnIndex, BoldDefs.BOLDMAXTIMESTAMP);
                    // copy all old data
                    for (int i = 0; i < OldDataQuery.FieldCount; i++)
                    {
                        aQuery.ParamByName(OldDataQuery.Fields(i).FieldName).AssignFieldValue(OldDataQuery.Fields(i));
                    }

                    // fill with known new data.
                    ValuesToParamsByMemberList(NewId, ValueSpace, aQuery, MemberPMList, TranslationList, TBoldDBStorageMode.dsmUpdate);

                    // set the timestamps
                    aQuery.ParamByName(BoldDefs.TIMESTAMPSTARTCOLUMNNAME).AsInteger = SystemPersistenceMapper.CurrentTimeStamp;
                    if (AllTables[T].ContainsStopTimeStamp)
                    {
                        aQuery.ParamByName(BoldDefs.TIMESTAMPSTOPCOLUMNNAME).AsInteger = BoldDefs.BOLDMAXTIMESTAMP;
                    }

                    TickCounter++;
                    if (TickCounter % AllTables.Count == 0)
                    {
                        SystemPersistenceMapper.SendExtendedEvent(BoldSubscription.bpeCreateObject, [NewId, ValueSpace]);
                        TickCounter = 0;
                    }

                    aQuery.ExecSQL();
                    OldDataQuery.Next();
                }

                OldDataQuery.Close();
            }

            for (int A = 0; A < MemberPersistenceMappers.Count; A++)
            {
                if (MemberPersistenceMappers[A] is { IsStoredInObject: true, CustomCreateUpDate: true } member)
                {
                    member.PMCreate(ObjectIdList, ValueSpace, TranslationList);
                }
            }
        }
        finally
        {
            OldDataQuery.Close();
            SystemPersistenceMapper.ReleaseExecQuery(ref aQuery);
            SystemPersistenceMapper.ReleaseQuery(ref OldDataQuery);
        }
    }

    /// <summary>Fetches the old values of the embedded single links (for the other ends' bookkeeping).</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi passes no translation list; an empty one is passed here.")]
    private void FetchPreviousSingleLinkValues(TBoldObjectIdList ObjectIdList, IBoldValueSpace Old_Values)
    {
        var SingleLinkList = new TBoldMemberIdList();
        for (int i = 0; i < MemberPersistenceMappers.Count; i++)
        {
            if (MemberPersistenceMappers[i] is TBoldEmbeddedSingleLinkDefaultMapper)
            {
                SingleLinkList.Add(new TBoldMemberID(MemberPersistenceMappers[i]!.MemberIndex));
            }
        }

        if (SingleLinkList.Count > 0)
        {
            PMFetchExactIDList(ObjectIdList, Old_Values, SingleLinkList, BoldDefs.fmNormal, new TBoldIDTranslationList(), null);
        }
    }

    /// <summary>Reads the db types of the ids from a table and translates them to exact ids.</summary>
    [Delphi(PortStatus.Ported)]
    private void MakeIDsExactUsingTable(TBoldObjectIdList ObjectIDList, TBoldIDTranslationList TranslationList, TBoldSQLTableDescription Table)
    {
        if (ObjectIDList.Count == 0)
        {
            return;
        }

        var lst = new TStringList();
        IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
        try
        {
            //  We do not want to retrieve more than BLOCKSIZE objects at a time
            BoldPMappersDefault.ForEachBlock(ObjectIDList.Count, SystemPersistenceMapper.SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
            {
                lst.Clear();
                Table.RetrieveSelectIdAndTypeStatement(lst);
                string WhereFragment = IdListSegmentToWhereFragment(ObjectIDList, Start, Stop, aQuery);
                BoldUtils.BoldAppendToStrings(lst, SysUtils.Format(" WHERE %s %s", BoldDefs.IDCOLUMN_NAME, WhereFragment), true);
                aQuery.AssignSQL(lst);
                aQuery.Open();
                var Ids = new List<(int Id, TBoldDbType dbType)>();
                try
                {
                    while (!aQuery.Eof)
                    {
                        Ids.Add((aQuery.Fields(0).AsInteger, aQuery.Fields(1).AsInteger));
                        aQuery.Next();
                    }

                    for (int i = 0; i < ObjectIDList.Count; i++)
                    {
                        foreach ((int Id, TBoldDbType dbType) in Ids)
                        {
                            if (((TBoldDefaultID)ObjectIDList[i]).AsInteger == Id)
                            {
                                int TopSortedIndex = SystemPersistenceMapper.TopSortedIndexForBoldDbType(dbType);
                                TranslationList.AddTranslation(ObjectIDList[i], ObjectIDList[i].CloneWithClassId(TopSortedIndex, true));
                            }
                        }
                    }
                }
                finally
                {
                    aQuery.Close();
                }
            });
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>" = :ID1" or "in (:ID1, ...)", with parameters when there are not too many ids, else the ids themselves.</summary>
    [Delphi(PortStatus.Ported)]
    private string InternalIdListSegmentToWhereFragment(TBoldObjectIdList IdList, int Start, int Stop, IBoldParameterized Parameterized)
    {
        int ParamCount = Stop - Start + 1;
        bool UseParams = ParamCount <= SystemPersistenceMapper.SQLDataBaseConfig.MaxParamsInIdList;

        string GetParamStr(int IdIndex, int ParamIndex)
        {
            if (UseParams)
            {
                Parameterized.Createparam(TFieldType.ftInteger, "ID" + SysUtils.IntToStr(ParamIndex), TParamType.ptInput, sizeof(int)).AsInteger =
                    ((TBoldDefaultID)IdList[IdIndex]).AsInteger;
                return ":ID" + SysUtils.IntToStr(ParamIndex);
            }

            return IdList[IdIndex].AsString;
        }

        if (ParamCount == 1)
        {
            return " = " + GetParamStr(Start, 1);
        }

        var TempList = new TStringList();
        for (int i = Start; i <= Stop; i++)
        {
            TempList.Add(GetParamStr(i, i - Start + 1));
        }

        return BoldUtils.BoldSeparateStringList(TempList, ", ", "in (", ")");
    }

    [Delphi(PortStatus.Ported)]
    private void InternalMakeIDsExact(TBoldObjectIdList ObjectIDList, TBoldIDTranslationList TranslationList)
    {
        if (MainTable is not null)
        {
            MakeIDsExactUsingTable(ObjectIDList, TranslationList, MainTable);
        }
        else
        {
            for (int i = 0; i < SystemPersistenceMapper.ObjectPersistenceMappers.Count; i++)
            {
                if (SystemPersistenceMapper.ObjectPersistenceMappers[i] is TBoldObjectDefaultMapper ObjectMapper && ObjectMapper.SuperClass == this)
                {
                    ObjectMapper.InternalMakeIDsExact(ObjectIDList, TranslationList);
                }
            }
        }
    }

    /// <summary>Adds the member mappers stored in the object (not custom) whose first column is in the table (and dirty, when contents are given).</summary>
    private void AddMembersOfTable(TBoldMemberPersistenceMapperList MemberPMList, TBoldSQLTableDescription Table, IBoldObjectContents? DirtyIn)
    {
        for (int A = 0; A < MemberPersistenceMappers.Count; A++)
        {
            if (MemberPersistenceMappers[A] is TBoldMemberDefaultMapper MemberPMapper && MemberPMapper.IsStoredInObject && !MemberPMapper.CustomCreateUpDate &&
                (DirtyIn is null || MemberPMapper.IsDirty(DirtyIn)) &&
                ((TBoldSQLColumnDescription)MemberPMapper.ColumnDescriptions[0]).TableDescription == Table)
            {
                MemberPMList.Add(MemberPMapper);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void JoinSQLTableByKey(TStringList SQL, TBoldSQLTableDescription MainTable, TBoldSQLTableDescription JoinTable)
    {
        const string SQLEQUALITY = "%s.%s = %s.%s";
        SQL.Add(SysUtils.Format(SQLEQUALITY, TableAlias(JoinTable, true), BoldDefs.IDCOLUMN_NAME, TableAlias(MainTable, true), BoldDefs.IDCOLUMN_NAME));
        if (Versioned)
        {
            SQL.Add(SysUtils.Format(SQLEQUALITY, TableAlias(JoinTable, true), BoldDefs.TIMESTAMPSTARTCOLUMNNAME, TableAlias(MainTable, true), BoldDefs.TIMESTAMPSTARTCOLUMNNAME));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void SQLForID(TBoldSQLTableDescription Table, TStrings SQL, bool UseAlias) =>
        SQL.Add(SysUtils.Format("%s.%s", TableAlias(Table, UseAlias), BoldDefs.IDCOLUMN_NAME));

    [Delphi(PortStatus.Ported)]
    protected override void SQLForDistributed(TStrings SQL, TBoldSQLStyle SQLStyle)
    {
        string prefix = SQLStyle switch
        {
            TBoldSQLStyle.ssColumns => "",
            TBoldSQLStyle.ssParameters => ":",
            _ => "-",
        };
        if (prefix == "-")
        {
            return;
        }

        if (SystemPersistenceMapper.UseTimestamp)
        {
            SQL.Add(prefix + SystemPersistenceMapper.XFilesTimeStampColumn!.SQLName);
        }

        if (SystemPersistenceMapper.UseGlobalId)
        {
            SQL.Add(prefix + SystemPersistenceMapper.XFilesGlobalIdColumn!.SQLName);
        }
    }

    /// <summary>Fetches the ids of the class (with a SQL, time stamp or class condition) from each table its instances are in.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void PMFetchWithCondition(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldCondition BoldCondition, int FetchMode,
        TBoldIDTranslationList TranslationList)
    {
        if (BoldCondition is not (null or TBoldConditionWithClass))
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sUnknownConditionType, BoldCondition.GetType().Name));
        }

        var SQLCondition = BoldCondition as TBoldSQLCondition;
        TBoldAllInstancesMappingInfo[] MappingInfo = SystemPersistenceMapper.MappingInfo.GetAllInstancesMapping(ExpressionName);
        IBoldQuery? aQuery = SystemPersistenceMapper.GetQuery();
        var sql = new TStringList();
        try
        {
            for (int i = 0; i < MappingInfo.Length; i++)
            {
                sql.Clear();
                string WhereToken = "WHERE";
                TBoldTimeStampType timeStamp = BoldCondition is TBoldConditionWithClass withClass ? withClass.Time : BoldDefs.BOLDMAXTIMESTAMP;
                if (BoldCondition is TBoldTimestampCondition timestampCondition)
                {
                    sql.Add(SysUtils.Format("SELECT %s, %s", BoldDefs.IDCOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME));
                    sql.Add(SysUtils.Format("FROM %s", SystemPersistenceMapper.PSSystemDescription.XFilestable!.SQLName));
                    sql.Add(SysUtils.Format("WHERE %s > %d", SystemPersistenceMapper.XFilesTimeStampColumn!.SQLName, timestampCondition.Timestamp));
                    aQuery.AssignSQL(sql);
                }
                else
                {
                    sql.Add(SysUtils.Format("SELECT %s.%s, %s.%s", MappingInfo[i].TableName, BoldDefs.IDCOLUMN_NAME, MappingInfo[i].TableName, BoldDefs.TYPECOLUMN_NAME));
                    string fromLine = SysUtils.Format("FROM %s", MappingInfo[i].TableName);
                    string JoinLine = "";
                    string RootTableName = SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName;
                    bool sql92 = SystemPersistenceMapper.SQLDataBaseConfig.UseSQL92Joins;

                    // add the rest of the tables
                    if (SQLCondition is not null && SQLCondition.JoinInheritedTables && (SQLCondition.WhereFragment.Length != 0 || SQLCondition.OrderBy.Length != 0))
                    {
                        for (int j = 0; j < AllTables.Count; j++)
                        {
                            if (!SysUtils.SameText(AllTables[j].SQLName, MappingInfo[i].TableName) && AllTables[j] != SystemPersistenceMapper.PSSystemDescription.XFilestable)
                            {
                                string JoinCondition = SysUtils.Format("(%s.%s = %s.%s)", MappingInfo[i].TableName, BoldDefs.IDCOLUMN_NAME, AllTables[j].SQLName, BoldDefs.IDCOLUMN_NAME);
                                if (sql92)
                                {
                                    fromLine += SysUtils.Format(" left join %s on %s ", AllTables[j].SQLName, JoinCondition);
                                }
                                else
                                {
                                    fromLine += SysUtils.Format(", %s", AllTables[j].SQLName);
                                    JoinLine += SysUtils.Format("%s %s ", WhereToken, JoinCondition);
                                    WhereToken = "AND";
                                }
                            }
                        }
                    }
                    else if (Versioned && !SysUtils.SameText(MappingInfo[i].TableName, RootTableName))
                    {
                        string JoinCondition = SysUtils.Format("(%s.%s = %s.%s) ", MappingInfo[i].TableName, BoldDefs.IDCOLUMN_NAME, RootTableName, BoldDefs.IDCOLUMN_NAME);
                        if (sql92)
                        {
                            fromLine += SysUtils.Format(" left join %s on %s ", RootTableName, JoinCondition);
                        }
                        else
                        {
                            fromLine += ", " + RootTableName;
                            JoinLine += SysUtils.Format(" %s %s ", WhereToken, JoinCondition);
                            WhereToken = "AND";
                        }
                    }

                    sql.Add(fromLine);
                    if (JoinLine.Length != 0)
                    {
                        sql.Add(JoinLine);
                    }

                    if (Versioned)
                    {
                        RetrieveTimeStampCondition(sql, timeStamp, false, WhereToken, false);
                        WhereToken = "AND";
                    }

                    if (MappingInfo[i].ClassIdRequired)
                    {
                        sql.Add(SysUtils.Format("%s %s.%s in (%s)", WhereToken, MappingInfo[i].TableName, BoldDefs.TYPECOLUMN_NAME, SubClassesID));
                        WhereToken = "AND";
                    }

                    if (SQLCondition is not null)
                    {
                        if (SQLCondition.WhereFragment.Length != 0)
                        {
                            sql.Add(SysUtils.Format("%s (%s)", WhereToken, SQLCondition.WhereFragment));
                        }

                        if (SQLCondition.OrderBy.Length != 0)
                        {
                            sql.Add(SysUtils.Format("ORDER BY %s", SQLCondition.OrderBy));
                        }
                    }

                    aQuery.AssignSQL(sql);
                    if (SQLCondition is not null)
                    {
                        aQuery.AssignParams(SQLCondition.Params);
                    }
                }

                int answers = SystemPersistenceMapper.GetListUsingQuery(ObjectIDList, ValueSpace, aQuery, FetchMode, TranslationList, timeStamp,
                    BoldCondition?.MaxAnswers ?? -1, BoldCondition?.Offset ?? -1);
                if (BoldCondition is not null)
                {
                    BoldCondition.AvailableAnswers = answers;
                }

                BoldPMappers.BoldPMLogFmt(BoldPMConsts.sLogFetchedXObjectsOfYFromTableZ, ObjectIDList.Count, ExpressionName, MappingInfo[i].TableName);
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>The next id from the reserved range; when it is used up, the id table is advanced by the number of reserved ids.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override TBoldObjectId NextExternalObjectId(IBoldValueSpace ValueSpace, TBoldObjectId ObjectId)
    {
        TBoldSystemDefaultMapper SystemDefaultMapper = SystemPersistenceMapper;
        if (SystemDefaultMapper.NextDBID > SystemDefaultMapper.LastReservedDBID)
        {
            SystemDefaultMapper.DoStartTransaction(ValueSpace);
            IBoldQuery? aQuery = SystemDefaultMapper.GetQuery();
            IBoldExecQuery? aExecQuery = SystemDefaultMapper.GetExecQuery();
            try
            {
                try
                {
                    aExecQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = %s + %d",
                        SystemDefaultMapper.PSSystemDescription.IdTable!.SQLName, BoldDefs.IDCOLUMN_NAME, BoldDefs.IDCOLUMN_NAME, SystemDefaultMapper.ReservedCount));
                    aExecQuery.ExecSQL();
                    aQuery.AssignSQLText(SysUtils.Format("SELECT %s FROM %s", BoldDefs.IDCOLUMN_NAME, SystemDefaultMapper.PSSystemDescription.IdTable.SQLName));
                    aQuery.Open();
                    int NewID = aQuery.Fields(0).AsInteger;
                    aQuery.Close();
                    SystemDefaultMapper.DoCommit(ValueSpace);
                    SystemDefaultMapper.NextDBID = NewID - SystemDefaultMapper.ReservedCount;
                    SystemDefaultMapper.LastReservedDBID = SystemDefaultMapper.NextDBID + SystemDefaultMapper.ReservedCount - 1;
                }
                catch (Exception e) when (e is EDatabaseError or System.Data.Common.DbException)
                {
                    SystemDefaultMapper.DoRollBack(ValueSpace);
                    throw new EBoldCantGetID(SysUtils.Format(BoldPMConsts.sCannotGetID, e.GetType().Name, e.Message), e);
                }
            }
            finally
            {
                SystemDefaultMapper.ReleaseQuery(ref aQuery);
                SystemDefaultMapper.ReleaseExecQuery(ref aExecQuery);
            }
        }

        SystemDefaultMapper.ReservedCount = 0;
        var result = new TBoldDefaultID(ObjectId.TopSortedIndex, true) { AsInteger = SystemDefaultMapper.NextDBID };
        SystemDefaultMapper.NextDBID++;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldSQLTableDescription? DistributableTable() => SystemPersistenceMapper.PSSystemDescription.XFilestable;

    /// <summary>Fetches the objects in compare mode; objects missing in the database are failures.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void PMCompareExactIDList(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList MemberIdList, TBoldIDTranslationList TranslationList,
        TBoldObjectIdList FailureList)
    {
        var MissingList = new TBoldObjectIdList();
        PMMultiPurposeRetrieveExactIdList(ObjectIDList, ValueSpace, MemberIdList, BoldDefs.fmCompare, TranslationList, MissingList, FailureList, BoldDefs.BOLDMAXTIMESTAMP);
        if (MissingList.Count > 0)
        {
            TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
            BoldLog.LogFmt(BoldPMConsts.sOptimisticLockingFailedForNonExisting, [ExpressionName]);
            for (int i = 0; i < MissingList.Count; i++)
            {
                BoldLog.LogFmt(BoldPMConsts.sLogIdAsString, [MissingList[i].AsString]);
            }

            FailureList.AddList(MissingList);
        }
    }

    /// <summary>The X-files table (if used) comes first, then (inherited) the class's tables; the root class's versioned table gets a stop time column.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        // it is important that the x-files table come before the other tables
        // (and in general that the tables for a superclass comes before the tables of a subclass)
        if (SystemPersistenceMapper.UseXFiles)
        {
            AllTables.Add(SystemPersistenceMapper.PSSystemDescription.XFilestable);
        }

        base.InitializePSDescriptions();
        if (SuperClass is null)
        {
            SystemPersistenceMapper.PSSystemDescription.RootTable = MainTable;
        }

        // the starttimestamp column is added in EnsureTable
        // root class needs to have a stopcolumn as well...
        if (Versioned && this == SystemPersistenceMapper.RootClassObjectPersistenceMapper)
        {
            TBoldSQLDataBaseConfig cfg = SystemPersistenceMapper.SQLDataBaseConfig;
            SystemPersistenceMapper.EnsureColumn(MainTable!.SQLName, BoldDefs.TIMESTAMPSTOPCOLUMNNAME, cfg.ColumnTypeForInteger, cfg.EffectiveSQLForNotNull,
                BoldPMappersDefault.BOLDTIMESTAMPFIELDTYPE, 0, true, true, cfg.CorrectlyQuotedDefaultValue(SysUtils.IntToStr(BoldDefs.BOLDMAXTIMESTAMP)));
            // the following index improves performance alot in Interbase, and seems to have no negative impact in SQLServer.
            MainTable.EnsureIndex(BoldDefs.TIMESTAMPSTOPCOLUMNNAME, false, false);
            MainTable.ContainsStopTimeStamp = true;
        }
    }

    /// <summary>The id and type columns (or parameters) of a table, qualified by the alias for the main table.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SQLForKey(TBoldSQLTableDescription Table, TStrings SQL, TBoldSQLStyle SQLStyle, bool useAlias)
    {
        string tableQualifier = Table == MainTable && useAlias ? TableAlias(Table, useAlias) + "." : "";
        string ColumnString = SQLStyle == TBoldSQLStyle.ssParameters ? ":" + tableQualifier : tableQualifier;
        SQL.Add(ColumnString + Table.ColumnsList[0].SQLName);
        SQL.Add(ColumnString + Table.ColumnsList[1].SQLName);
    }

    /// <summary>Whether the table has columns besides the id, type and time stamp columns.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool UpdatesMembersInTable(TBoldSQLTableDescription aTable)
    {
        int limit = 2;
        if (aTable.Versioned)
        {
            limit++;
            if (aTable.ContainsStopTimeStamp)
            {
                limit++;
            }
        }

        return aTable.ColumnsList.Count > limit;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectDefaultMapper.IdListSegmentToWhereFragment(TBoldObjectIdList,integer,integer,IBoldExecQuery)")]
    public string IdListSegmentToWhereFragment(TBoldObjectIdList IdList, int Start, int Stop, IBoldExecQuery Query) =>
        InternalIdListSegmentToWhereFragment(IdList, Start, Stop, (IBoldParameterized)Query);

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectDefaultMapper.IdListSegmentToWhereFragment(TBoldObjectIdList,integer,integer,IBoldQuery)")]
    public string IdListSegmentToWhereFragment(TBoldObjectIdList IdList, int Start, int Stop, IBoldQuery Query) =>
        InternalIdListSegmentToWhereFragment(IdList, Start, Stop, (IBoldParameterized)Query);

    /// <summary>Makes the ids exact from the class's tables, then (with X-files) the rest from the X-files table.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeIDsExact(TBoldObjectIdList ObjectIDList, TBoldIDTranslationList TranslationList)
    {
        if (ObjectIDList.Count == 0)
        {
            return;
        }

        InternalMakeIDsExact(ObjectIDList, TranslationList);
        var MissingIds = new TBoldObjectIdList();
        if (SystemPersistenceMapper.UseXFiles)
        {
            for (int i = 0; i < ObjectIDList.Count; i++)
            {
                if (TranslationList.TranslateToNewId(ObjectIDList[i]) == ObjectIDList[i])
                {
                    MissingIds.Add(ObjectIDList[i]);
                }
            }

            if (MissingIds.Count > 0)
            {
                MakeIDsExactUsingTable(MissingIds, TranslationList, SystemPersistenceMapper.PSSystemDescription.XFilestable!);
            }
        }
    }

    /// <summary>Fetches objects of the class; versioned objects at other time stamps than the first are fetched separately.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetchExactIDList(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList, TBoldObjectIdList? MissingList)
    {
        // Note, when the list gets here it contains only object of one class, from one
        // Bold, that are not previously present in the Bold (or possibly a forced fetch)
        if (ObjectIDList.Count == 0)
        {
            return;
        }

        TBoldTimeStampType TimeStamp = ObjectIDList[0].TimeStamp;
        TBoldObjectIdList ObjectsToFetch;
        TBoldObjectIdList? ListOfOtherTimeStamps = null;
        if (Versioned && ObjectIDList.Count > 1)
        {
            ListOfOtherTimeStamps = new TBoldObjectIdList();
            ObjectsToFetch = new TBoldObjectIdList();
            for (int i = ObjectIDList.Count - 1; i >= 0; i--)
            {
                if (ObjectIDList[i].TimeStamp == TimeStamp)
                {
                    ObjectsToFetch.Add(ObjectIDList[i]);
                }
                else
                {
                    ListOfOtherTimeStamps.Add(ObjectIDList[i]);
                }
            }
        }
        else
        {
            ObjectsToFetch = ObjectIDList;
        }

        if (ListOfOtherTimeStamps is { Count: > 0 })
        {
            PMFetchExactIDList(ListOfOtherTimeStamps, ValueSpace, MemberIdList, FetchMode, TranslationList, MissingList);
        }

        PMMultiPurposeRetrieveExactIdList(ObjectsToFetch, ValueSpace, MemberIdList, FetchMode, TranslationList, MissingList, null, TimeStamp);
    }

    /// <summary>Deletes the objects' rows (versioned objects get a stop time); the X-files rows get the current time stamp.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMDelete(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationList)
    {
        if (Old_Values is not null)
        {
            FetchPreviousSingleLinkValues(ObjectIDList, Old_Values);
        }

        if (Versioned)
        {
            PMUpdateStopTime(ObjectIDList);
            return;
        }

        for (int I = 0; I < MemberPersistenceMappers.Count; I++)
        {
            if (MemberPersistenceMappers[I] is { CustomCreateUpDate: true } member)
            {
                member.PMDelete(ObjectIDList, ValueSpace, TranslationList);
            }
        }

        var lst = new TStringList();
        IBoldExecQuery? aQuery = SystemPersistenceMapper.GetExecQuery();
        try
        {
            BoldPMappersDefault.ForEachBlock(ObjectIDList.Count, SystemPersistenceMapper.SQLDataBaseConfig.FetchBlockSize, (start, stop) =>
            {
                lst.Clear();
                for (int i = start; i <= stop; i++)
                {
                    SystemPersistenceMapper.SendExtendedEvent(BoldSubscription.bpeDeleteObject, [ObjectIDList[i], ValueSpace]);
                }

                string IdListString = IdListSegmentToWhereFragment(ObjectIDList, start, stop, aQuery);

                //Execute SQLStatement on all tables except the xfiles table
                for (int T = 0; T < AllTables.Count; T++)
                {
                    if (AllTables[T] != SystemPersistenceMapper.PSSystemDescription.XFilestable)
                    {
                        aQuery.AssignSQLText("DELETE FROM " + AllTables[T].SQLName + BoldDefs.BOLDCRLF + "WHERE " + BoldDefs.IDCOLUMN_NAME + " " + IdListString + BoldDefs.BOLDCRLF);
                        aQuery.ExecSQL();
                    }
                    else if (AllTables[T].ColumnsList.IndexOf(SystemPersistenceMapper.XFilesTimeStampColumn) != -1)
                    {
                        aQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = %d WHERE %s %s", AllTables[T].SQLName, SystemPersistenceMapper.XFilesTimeStampColumn!.SQLName,
                            SystemPersistenceMapper.CurrentTimeStamp, BoldDefs.IDCOLUMN_NAME, IdListString));
                        aQuery.ExecSQL();
                    }
                }
            });

            for (int I = 0; I < ObjectIDList.Count; I++)
            {
                TranslationList.AddTranslation(ObjectIDList[I], null);
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref aQuery);
        }
    }

    /// <summary>
    /// Inserts a row per object in each table (members of that table, id, type and time stamps); new link objects that
    /// duplicate existing ones update those instead.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void PMCreate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        int TickCounter = 0;
        if (IsLinkClass && !Versioned)
        {
            // this will update link-objects rather than create new if they are already in the database
            // this happens only if two applications link the same two objects at the same time
            var DuplicateList = new TBoldObjectIdList();
            DetectLinkClassDuplicates(ObjectIDList, ValueSpace, TranslationList, DuplicateList);
            if (DuplicateList.Count != 0)
            {
                PMUpdate(DuplicateList, ValueSpace, null, TranslationList);
                for (int i = 0; i < DuplicateList.Count; i++)
                {
                    ObjectIDList.RemoveByIndex(ObjectIDList.IndexByID(DuplicateList[i]));
                }
            }

            if (ObjectIDList.Count == 0)
            {
                return;
            }
        }

        IBoldExecQuery? aQuery = SystemPersistenceMapper.GetExecQuery();
        var sql = new TStringList();
        try
        {
            for (int T = 0; T < AllTables.Count; T++)
            {
                // Clear the memberlist, as we reuse it, and fill it with members to use.
                MemberPMList.Clear();
                AddMembersOfTable(MemberPMList, AllTables[T], null);

                //	Create insert query.
                sql.Clear();
                aQuery.ClearParams();
                var TempList = new TStringList();
                SQLForMembers(AllTables[T], TempList, MemberPMList, TBoldSQLStyle.ssColumns, true, false, false);
                if (AllTables[T].Versioned)
                {
                    TempList.Add(BoldDefs.TIMESTAMPSTARTCOLUMNNAME);
                    if (AllTables[T].ContainsStopTimeStamp)
                    {
                        TempList.Add(BoldDefs.TIMESTAMPSTOPCOLUMNNAME);
                    }
                }

                BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("INSERT INTO %s (%s) ", AllTables[T].SQLName, BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), true);
                TempList.Clear();
                SQLForMembers(AllTables[T], TempList, MemberPMList, TBoldSQLStyle.ssParameters, true, false, false);
                if (AllTables[T].Versioned)
                {
                    TempList.Add(":" + BoldDefs.TIMESTAMPSTARTCOLUMNNAME);
                    if (AllTables[T].ContainsStopTimeStamp)
                    {
                        TempList.Add(":" + BoldDefs.TIMESTAMPSTOPCOLUMNNAME);
                    }
                }

                BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("VALUES (%s) ", BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), true);
                aQuery.AssignSQL(sql);
                aQuery.StartSQLBatch();
                try
                {
                    for (int I = 0; I < ObjectIDList.Count; I++)
                    {
                        // All tables have id and type as column 0 and 1 respectively
                        TBoldObjectId NewID = TranslationList.TranslateToNewId(ObjectIDList[I])!;
                        aQuery.ParamByName(BoldDefs.IDCOLUMN_NAME).AsInteger = ((TBoldDefaultID)NewID).AsInteger;
                        aQuery.ParamByName(BoldDefs.TYPECOLUMN_NAME).AsSmallInt = SystemPersistenceMapper.BoldDbTypeForTopSortedIndex(NewID.TopSortedIndex);
                        ValuesToParamsByMemberList(ObjectIDList[I], ValueSpace, aQuery, MemberPMList, TranslationList, TBoldDBStorageMode.dsmCreate);
                        if (AllTables[T].Versioned)
                        {
                            aQuery.ParamByName(BoldDefs.TIMESTAMPSTARTCOLUMNNAME).AsInteger = Versioned ? SystemPersistenceMapper.CurrentTimeStamp : 0;
                            if (AllTables[T].ContainsStopTimeStamp)
                            {
                                aQuery.ParamByName(BoldDefs.TIMESTAMPSTOPCOLUMNNAME).AsInteger = BoldDefs.BOLDMAXTIMESTAMP;
                            }
                        }

                        TickCounter++;
                        if (TickCounter % AllTables.Count == 0)
                        {
                            SystemPersistenceMapper.SendExtendedEvent(BoldSubscription.bpeCreateObject, [ObjectIDList[I], ValueSpace]);
                            TickCounter = 0;
                        }

                        aQuery.ExecSQL();
                    }

                    aQuery.EndSQLBatch();
                }
                catch
                {
                    aQuery.FailSQLBatch();
                    throw;
                }
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref aQuery);
        }

        for (int A = 0; A < MemberPersistenceMappers.Count; A++)
        {
            if (MemberPersistenceMappers[A] is { IsStoredInObject: true, CustomCreateUpDate: true } member)
            {
                member.PMCreate(ObjectIDList, ValueSpace, TranslationList);
            }
        }
    }

    /// <summary>Updates the dirty members of each object, table by table (versioned objects get new versions).</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationList)
    {
        if (Versioned)
        {
            PMTemporalUpdate(ObjectIDList, ValueSpace, TranslationList);
            return;
        }

        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        if (Old_Values is not null)
        {
            FetchPreviousSingleLinkValues(ObjectIDList, Old_Values);
        }

        IBoldExecQuery? aQuery = SystemPersistenceMapper.GetExecQuery();
        var sql = new TStringList();
        try
        {
            aQuery.StartSQLBatch();
            try
            {
                for (int T = 0; T < AllTables.Count; T++)
                {
                    if (!UpdatesMembersInTable(AllTables[T]))
                    {
                        continue;
                    }

                    for (int I = 0; I < ObjectIDList.Count; I++)
                    {
                        IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIDList[I])!;

                        // Clear the memberlist, as we reuse it, and fill it with members to use.
                        MemberPMList.Clear();
                        AddMembersOfTable(MemberPMList, AllTables[T], ObjectContents);
                        if (MemberPMList.Count > 0)
                        {
                            var TempList = new TStringList();
                            SQLForMembers(AllTables[T], TempList, MemberPMList, TBoldSQLStyle.ssValues, false, false, false);
                            sql.Clear();
                            BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("UPDATE %s", AllTables[T].SQLName), true);
                            BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("SET %s", BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), true);
                            BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("WHERE %s = :%s", BoldDefs.IDCOLUMN_NAME, BoldDefs.IDCOLUMN_NAME), true);
                            aQuery.ClearParams();
                            aQuery.AssignSQL(sql);
                            aQuery.ParamByName(BoldDefs.IDCOLUMN_NAME).AsInteger = ((TBoldDefaultID)TranslationList.TranslateToNewId(ObjectIDList[I])!).AsInteger;
                            ValuesToParamsByMemberList(ObjectIDList[I], ValueSpace, aQuery, MemberPMList, TranslationList, TBoldDBStorageMode.dsmUpdate);
                            SystemPersistenceMapper.SendExtendedEvent(BoldSubscription.bpeUpdateObject, [ObjectIDList[I], ValueSpace, aQuery]);
                            aQuery.ExecSQL();
                        }
                    }
                }

                aQuery.EndSQLBatch();
            }
            catch
            {
                aQuery.FailSQLBatch();
                throw;
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref aQuery);
        }

        for (int A = 0; A < MemberPersistenceMappers.Count; A++)
        {
            if (MemberPersistenceMappers[A] is { IsStoredInObject: true, CustomCreateUpDate: true } member)
            {
                member.PMUpdate(ObjectIDList, ValueSpace, TranslationList);
            }
        }
    }

    /// <summary>Reads the time stamp and global id of a distributable fetch.</summary>
    [Delphi(PortStatus.Ported)]
    public void DistributableInfoFromQuery(TBoldObjectId ObjectID, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList, IBoldDataSet DataSet)
    {
        if (SystemPersistenceMapper.UseTimestamp)
        {
            ValueSpace.EnsuredObjectContentsByObjectId(ObjectID).TimeStamp = DataSet.FieldByName(SystemPersistenceMapper.XFilesTimeStampColumn!.SQLName).AsInteger;
        }

        if (SystemPersistenceMapper.UseGlobalId)
        {
            ValueSpace.EnsuredObjectContentsByObjectId(ObjectID).GlobalId = DataSet.FieldByName(SystemPersistenceMapper.XFilesGlobalIdColumn!.SQLName).AsString;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void HandleFetchData(TBoldObjectId FetchedId, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList, IBoldQuery Query,
        TBoldMemberPersistenceMapperList MemberPMList, int FetchMode, TBoldObjectIdList? FailureList)
    {
        ValuesFromFieldsByMemberList(FetchedId, ValueSpace, TranslationList, Query, MemberPMList);
        if (FetchMode == BoldDefs.fmDistributable)
        {
            DistributableInfoFromQuery(FetchedId, ValueSpace, TranslationList, Query);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void PortObject(TBoldObjectId ObjectId, IBoldQuery Query) => SystemPersistenceMapper.ObjectUpgrader?.UpgradeObjectById(ObjectId, Query);

    /// <summary>Whether the fetched row is of an older model version that needs a manual upgrade.</summary>
    [Delphi(PortStatus.Ported)]
    public bool IsOldVersion(IBoldQuery Query)
    {
        int CurrentObjectVersion = fModelVersionMember!.VersionFromQuery(Query);
        return CurrentObjectVersion < SystemPersistenceMapper.ModelVersion && SystemPersistenceMapper.ObjectUpgrader is not null &&
            SystemPersistenceMapper.ObjectUpgrader.NeedsManualUpdate(ExpressionName, CurrentObjectVersion);
    }

    [Delphi(PortStatus.Ported)] public string SubClassesID { get => fSubClassesID; set => fSubClassesID = value; }
    [Delphi(PortStatus.Ported)] public new TBoldSystemDefaultMapper SystemPersistenceMapper => GetSystemPersistenceMapper();
    [Delphi(PortStatus.Ported)] public TBoldModelVersionMember? ModelVersionMember => fModelVersionMember;
}
