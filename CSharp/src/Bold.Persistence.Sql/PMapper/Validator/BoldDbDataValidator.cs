// Delphi: Source/PMapper/Validator/BoldDbDataValidator.pas
// Port map: CSharp/port/units/BoldDbDataValidator.md
// Checks the data of a database with the default mapping: objects missing from a parent or child table, rows whose
// type differs between tables, links to objects that do not exist, single-single links that do not point back,
// objects of types that do not belong in a table, and broken link objects; each finding is logged with remedy SQL.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbDataValidator", Symbol = "TBoldDbDataValidator.Destroy", Note = "Freed the lists and released the query; DeActivate releases the query, the garbage collector the rest.")]

namespace Bold;

/// <summary>Unit-level constants of BoldDbDataValidator: the SQL of the tests.</summary>
public static class BoldDbDataValidator
{
    internal const string Field_BOLD_ID = "BOLD_ID";

    // [Own, Parent]
    internal const string ExistenceInParentTest =
        "SELECT OWN.BOLD_ID, OWN.BOLD_TYPE " + BoldDefs.BOLDCRLF +
        "FROM %s OWN " + BoldDefs.BOLDCRLF +
        "WHERE NOT EXISTS(" + BoldDefs.BOLDCRLF +
        "  SELECT PARENT.BOLD_ID " + BoldDefs.BOLDCRLF +
        "  FROM %s PARENT " + BoldDefs.BOLDCRLF +
        "  WHERE PARENT.BOLD_ID = OWN.BOLD_ID)";

    // [parent, types, Own]
    internal const string ExistenceInChildTest =
        "SELECT PARENT.BOLD_ID, PARENT.BOLD_TYPE " + BoldDefs.BOLDCRLF +
        "FROM %s PARENT" + BoldDefs.BOLDCRLF +
        "WHERE PARENT.BOLD_TYPE IN (%s) AND" + BoldDefs.BOLDCRLF +
        "      NOT EXISTS(" + BoldDefs.BOLDCRLF +
        "          SELECT BOLD_ID" + BoldDefs.BOLDCRLF +
        "          FROM %s OWN " + BoldDefs.BOLDCRLF +
        "          WHERE OWN.BOLD_ID = PARENT.BOLD_ID)";

    // [Parent, Own]
    internal const string TypeTest =
        "SELECT PARENT.BOLD_ID, PARENT.BOLD_TYPE, OWN.BOLD_TYPE" + BoldDefs.BOLDCRLF +
        "FROM %s PARENT, %s OWN" + BoldDefs.BOLDCRLF +
        "WHERE (PARENT.BOLD_ID = OWN.BOLD_ID) AND" + BoldDefs.BOLDCRLF +
        "      (PARENT.BOLD_TYPE <> OWN.BOLD_TYPE)";

    // find all objects that are related, but the other end does not exist.
    // [Link, OwnTable, RelatedTable]
    internal const string RelationTest =
        "SELECT OWN.%s, OWN.BOLD_ID" + BoldDefs.BOLDCRLF +
        "FROM %s OWN" + BoldDefs.BOLDCRLF +
        "WHERE OWN.%0:s <> -1 AND NOT EXISTS (" + BoldDefs.BOLDCRLF +
        "  SELECT RELATED.BOLD_ID" + BoldDefs.BOLDCRLF +
        "  FROM %2:s RELATED" + BoldDefs.BOLDCRLF +
        "  WHERE RELATED.BOLD_ID = OWN.%0:s)";

    // [Link1, link2, linktable]
    internal const string LinkObjectDupesTest =
        "SELECT LINKTABLE.BOLD_ID, LINKTABLE.%s, LINKTABLE.%s, COUNT(*)" + BoldDefs.BOLDCRLF +
        "FROM %s LINKTABLE" + BoldDefs.BOLDCRLF +
        "GROUP BY LINKTABLE.%0:s, LINKTABLE.%1:s" + BoldDefs.BOLDCRLF +
        "HAVING COUNT(*) >= 2" + BoldDefs.BOLDCRLF +
        "ORDER BY %0:s, %1:s";

    // linkobjects with empty ends
    internal const string LinkObjectTest =
        "SELECT BOLD_ID" + BoldDefs.BOLDCRLF +
        "FROM %s" + BoldDefs.BOLDCRLF +
        "WHERE %s = -1 or %s = -1";

    // Linkobjects pointing to nonexisting objects
    // [LinkTable, OtherTable, LinkColumn]
    internal const string LinkObjectTest2 =
        "SELECT LT.BOLD_ID" + BoldDefs.BOLDCRLF +
        "FROM %s LT" + BoldDefs.BOLDCRLF +
        "WHERE NOT EXISTS (SELECT T.BOLD_ID FROM %s T WHERE LT.%s = T.BOLD_ID)";

    internal const string StrayObjectsTest =
        "SELECT BOLD_ID, BOLD_TYPE " + BoldDefs.BOLDCRLF +
        "FROM %s" + BoldDefs.BOLDCRLF +
        "WHERE NOT BOLD_TYPE IN (%s)";

    // [Table1, table2, link1, link2Own, Parent]
    internal const string SingleSingleEmbeddInconsistencyTest =
        "SELECT T1.BOLD_ID, T1.BOLD_TYPE " + BoldDefs.BOLDCRLF +
        "FROM %s T1, %s T2 " + BoldDefs.BOLDCRLF +
        "WHERE (T1.%s = T2.BOLD_ID) and (T2.%s <> T1.BOLD_ID)";
}

/// <summary>Validates the data of the database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDbDataValidator : TBoldDbValidator
{
    private readonly TStringList fTypeTestedTables = new();
    private IBoldQuery? fQuery;
    private readonly TStringList fExistenceInParentTestedTables = new();

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldDbDataValidator(System.ComponentModel.IContainer? owner = null) : base(owner)
    {
    }

    /// <summary>Whether the member is declared in a superclass.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool MemberIsInherited(TBoldMemberPersistenceMapper MemberMapper) =>
        MemberMapper.ObjectPersistenceMapper.SuperClass is { } SuperClass && MemberMapper.MemberIndex < SuperClass.MemberPersistenceMappers.Count;

    /// <summary>Runs a test on two tables (once per pair if a check list is given), collecting the ids (and types); true if any.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool Prepare2TableTest(string SQLTemplate, TStringList? CheckList, object?[] args, string table1, string table2, TStrings IdList, TStrings? TypeList = null)
    {
        if (CheckList is null || (CheckList.IndexOf(table1 + ":" + table2) == -1 && CheckList.IndexOf(table2 + ":" + table1) == -1))
        {
            IdList.Clear();
            TypeList?.Clear();
            Query.AssignSQLText(SysUtils.Format(SQLTemplate, args));
            Query.Open();
            while (!Query.Eof)
            {
                IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
                TypeList?.Add(Query.FieldByName("BOLD_TYPE").AsString);
                Query.Next();
            }

            Query.Close();
            CheckList?.Add(table1 + ":" + table2);
            return IdList.Count > 0;
        }

        return false;
    }

    /// <summary>Runs every test on every class; the database must be open.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Validate()
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        if (!PersistenceHandle!.DatabaseInterface.Connected)
        {
            remedy.Add(BoldPMConsts.sDBNotOpened);
            BoldLog.Log(BoldPMConsts.sDBMustBeOpened);
            return;
        }

        BoldLog.ProgressMax = SystemSQLMapper.ObjectPersistenceMappers.Count - 1;
        for (int i = 0; i < SystemSQLMapper.ObjectPersistenceMappers.Count; i++)
        {
            if (SystemSQLMapper.ObjectPersistenceMappers[i] is TBoldObjectSQLMapper ObjectPMapper)
            {
                BoldLog.LogHeader = SysUtils.Format(BoldPMConsts.sProcessingClass, ObjectPMapper.ExpressionName);
                ValidateExistence(ObjectPMapper);
                ValidateRelations(ObjectPMapper);
                if (ObjectPMapper is TBoldObjectDefaultMapper DefaultMapper)
                {
                    ValidateStrayObjects(DefaultMapper);
                }

                if (ObjectPMapper.IsLinkClass)
                {
                    //  The SQL for this is broken... why?
                    //
                    //      ValidateLinkObjectDupes(ObjectPMapper);
                    ValidateLinkObjects(ObjectPMapper);
                }
            }

            BoldLog.Progress = i;
        }
    }

    /// <summary>Remedy SQL that inserts the missing rows, with empty values for the mandatory columns.</summary>
    [Delphi(PortStatus.Ported)]
    private void SuggesttableInsert(TBoldSQLTableDescription table, TStrings IdList, TStrings TypeList)
    {
        BoldLogHandler.BoldLog().Log(table.SQLName);
        string NotNullColumns = "";
        string NotNullValues = "";
        string UnsupportedColumns = "";
        bool Skip = false;
        // check all columns except BodlID/BoldType
        for (int j = 2; j < table.ColumnsList.Count; j++)
        {
            var Column = (TBoldSQLColumnDescription)table.ColumnsList[j]!;
            if (Column.Mandatory)
            {
                NotNullColumns += ", " + Column.SQLName;
                switch (Column.FieldType)
                {
                    case TFieldType.ftString or TFieldType.ftMemo or TFieldType.ftFixedChar or TFieldType.ftWideString or TFieldType.ftBlob:
                        NotNullValues += ", ''";
                        break;
                    case TFieldType.ftLargeint or TFieldType.ftBCD or TFieldType.ftCurrency or TFieldType.ftFloat or TFieldType.ftSmallint or TFieldType.ftInteger or TFieldType.ftWord:
                        NotNullValues += ", 0";
                        break;
                    case TFieldType.ftBoolean:
                        NotNullValues += ", false";
                        break;
                    case TFieldType.ftDate:
                        NotNullValues += ", " + SysUtils.DateToStr(SysUtils.ZeroDateTime);
                        break;
                    case TFieldType.ftTime:
                        NotNullValues += ", " + SysUtils.TimeToStr(SysUtils.ZeroDateTime);
                        break;
                    case TFieldType.ftDateTime:
                        NotNullValues += ", " + SysUtils.DateTimeToStr(SysUtils.ZeroDateTime);
                        break;
                    default:
                        Skip = true;
                        NotNullValues += ", <XXX>";
                        if (UnsupportedColumns != "")
                        {
                            UnsupportedColumns += ", ";
                        }

                        UnsupportedColumns += Column.SQLName;
                        break;
                }
            }
        }

        string remedySQL = "INSERT INTO %s (BOLD_ID, BOLD_TYPE%s) VALUES (%s, %s%s)";
        if (Skip)
        {
            remedy.Add(SysUtils.Format(BoldPMConsts.sColumnsHaveUnsupportedType, UnsupportedColumns));
            remedySQL = "// " + remedySQL;
        }
        else
        {
            remedy.Add(SysUtils.Format(BoldPMConsts.sAddMissingEntries, table.SQLName));
        }

        for (int j = 0; j < IdList.Count; j++)
        {
            remedy.Add(SysUtils.Format(remedySQL, table.SQLName, NotNullColumns, IdList[j], TypeList[j], NotNullValues));
        }
    }

    /// <summary>Checks each table of the class against its parent's: rows missing in either, and differing types.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValidateExistence(TBoldObjectSQLMapper ObjectSQLMapper)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        var IdList = new TStringList();
        var TypeList = new TStringList();
        var Tables = new List<TBoldSQLTableDescription>();
        TBoldObjectSQLMapper? tempMapper = ObjectSQLMapper;
        while (tempMapper is not null)
        {
            if (tempMapper.MainTable is { } MainTable && !Tables.Contains(MainTable))
            {
                Tables.Add(MainTable);
            }

            tempMapper = (TBoldObjectSQLMapper?)tempMapper.SuperClass;
        }

        for (int i = 0; i < Tables.Count - 1; i++)
        {
            string SubTable = Tables[i].SQLName;
            string SuperTable = Tables[i + 1].SQLName;
            if (Prepare2TableTest(BoldDbDataValidator.ExistenceInParentTest, ExistenceInParentTestedTables, [SubTable, SuperTable], SubTable, SuperTable, IdList, TypeList))
            {
                BoldLog.LogFmt(BoldPMConsts.sLogObjectsMissingInParentTable, [SubTable, SuperTable]);
                BoldLog.Log(IdList.CommaText);
                SuggesttableInsert(Tables[i + 1], IdList, TypeList);
            }

            if (Prepare2TableTest(BoldDbDataValidator.TypeTest, TypeTestedTables, [SuperTable, SubTable], SuperTable, SubTable, IdList))
            {
                BoldLog.LogFmt(BoldPMConsts.sLogObjectsHaveDifferentType, [SuperTable, SubTable]);
                BoldLog.Log(IdList.CommaText);
            }
        }

        if (ObjectSQLMapper is TBoldObjectDefaultMapper DefaultMapper && ObjectSQLMapper.SuperClass is { } SuperClass)
        {
            TBoldAllInstancesMappingInfo[] OwnMapping = SystemSQLMapper.MappingInfo.GetAllInstancesMapping(ObjectSQLMapper.ExpressionName);
            TBoldAllInstancesMappingInfo[] ParentMapping = SystemSQLMapper.MappingInfo.GetAllInstancesMapping(SuperClass.ExpressionName);
            if (OwnMapping.Length == 1 && ParentMapping.Length == 1 && OwnMapping[0].TableName != ParentMapping[0].TableName)
            {
                string SuperTable = ParentMapping[0].TableName;
                string SubTable = OwnMapping[0].TableName;
                if (Prepare2TableTest(BoldDbDataValidator.ExistenceInChildTest, null, [SuperTable, DefaultMapper.SubClassesID, SubTable], SubTable, SuperTable, IdList, TypeList))
                {
                    BoldLog.LogFmt(BoldPMConsts.sLogObjectsMissingInChildtable, [SuperTable, SubTable]);
                    BoldLog.Log(IdList.CommaText);
                    SuggesttableInsert(ObjectSQLMapper.MainTable!, IdList, TypeList);
                }
            }
        }
    }

    /// <summary>Logs link objects that link the same two objects.</summary>
    [Delphi(PortStatus.Ported, Note = "Moves to the next row and closes the query; Delphi never did, so the loop did not end (the call is commented out in Validate).")]
    public void ValidateLinkObjectDupes(TBoldObjectSQLMapper ObjectSQLMapper)
    {
        var IdList = new TStringList();
        string LinkColumn1 = ((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole1!).MainColumnName;
        string LinkColumn2 = ((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole2!).MainColumnName;
        string LinkTable = ObjectSQLMapper.MainTable!.SQLName;
        Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.LinkObjectDupesTest, LinkColumn1, LinkColumn2, LinkTable));
        Query.Open();
        int LastLink1 = -int.MaxValue;
        int LastLink2 = -int.MaxValue;
        while (!Query.Eof)
        {
            int link1 = Query.FieldByName(LinkColumn1).AsInteger;
            int link2 = Query.FieldByName(LinkColumn2).AsInteger;
            if (link1 == LastLink1 && link2 == LastLink2)
            {
                IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
            }
            else
            {
                LastLink1 = link1;
                LastLink2 = link2;
            }

            Query.Next();
        }

        Query.Close();
        if (IdList.Count > 0)
        {
            BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogLinkObjectsAreDupes);
            BoldLogHandler.BoldLog().Log(IdList.CommaText);
        }
    }

    /// <summary>Link objects with an empty end, or an end that does not exist, get remedy SQL that deletes them.</summary>
    [Delphi(PortStatus.Ported, Note = "Each test starts with an empty id list and closes its query; Delphi reported the earlier ids again.")]
    public void ValidateLinkObjects(TBoldObjectSQLMapper ObjectSQLMapper)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        var IdList = new TStringList();
        string LinkColumn1 = ((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole1!).MainColumnName;
        string LinkColumn2 = ((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole2!).MainColumnName;
        string LinkTable = ObjectSQLMapper.MainTable!.SQLName;

        void CheckSpacePointers(TBoldEmbeddedSingleLinkDefaultMapper LinkMapper)
        {
            IdList.Clear();
            Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.LinkObjectTest2, LinkTable, LinkMapper.OtherEndObjectMapper.MainTable!.SQLName, LinkMapper.MainColumnName));
            Query.Open();
            while (!Query.Eof)
            {
                IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
                Query.Next();
            }

            Query.Close();
            if (IdList.Count > 0)
            {
                BoldLog.LogFmt(BoldPMConsts.sLogLinkObjectsLinkUnexistingObjects, [ObjectSQLMapper.ExpressionName]);
                BoldLog.Log(IdList.CommaText);
                remedy.Add(SysUtils.Format(BoldPMConsts.sCommentRemoveSpaceLinkObjects, ObjectSQLMapper.ExpressionName));
                AddRemedyForDeleteObjects(ObjectSQLMapper, IdList);
            }
        }

        Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.LinkObjectTest, LinkTable, LinkColumn1, LinkColumn2));
        Query.Open();
        while (!Query.Eof)
        {
            IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
            Query.Next();
        }

        Query.Close();
        if (IdList.Count > 0)
        {
            BoldLog.LogFmt(BoldPMConsts.sLogBrokenLinkObjects, [ObjectSQLMapper.ExpressionName]);
            BoldLog.Log(IdList.CommaText);
            remedy.Add(SysUtils.Format(BoldPMConsts.sCommentRemoveBrokenLinkObjects, ObjectSQLMapper.ExpressionName));
            AddRemedyForDeleteObjects(ObjectSQLMapper, IdList);
        }

        CheckSpacePointers((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole1!);
        CheckSpacePointers((TBoldEmbeddedSingleLinkDefaultMapper)ObjectSQLMapper.LinkClassRole2!);
    }

    /// <summary>Single links stored in the class's table that point to missing objects, or (single-single) do not point back.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValidateRelations(TBoldObjectSQLMapper ObjectSQLMapper)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        var IdList = new TStringList();
        if (ObjectSQLMapper.MainTable is not { } LinkTable)
        {
            return;
        }

        for (int i = 0; i < ObjectSQLMapper.MemberPersistenceMappers.Count; i++)
        {
            if (ObjectSQLMapper.MemberPersistenceMappers[i] is TBoldEmbeddedSingleLinkDefaultMapper SingleLink)
            {
                TBoldMemberMappingInfo[] MemberMappings = ObjectSQLMapper.SystemPersistenceMapper.MappingInfo.GetMemberMappings(ObjectSQLMapper.ExpressionName, SingleLink.ExpressionName);
                // check if the member is stored in our table.
                if (MemberMappings.Length == 1 && SysUtils.SameText(MemberMappings[0].TableName, LinkTable.SQLName))
                {
                    TBoldSQLTableDescription RelatedTable = ((TBoldObjectSQLMapper)SystemSQLMapper.ObjectPersistenceMappers[SingleLink.OtherEndObjectPMIndex]!).MainTable!;
                    Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.RelationTest, SingleLink.MainColumnName, LinkTable.SQLName, RelatedTable.SQLName));
                    Query.Open();
                    IdList.Clear();
                    while (!Query.Eof)
                    {
                        IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
                        Query.Next();
                    }

                    Query.Close();
                    if (IdList.Count > 0)
                    {
                        BoldLog.LogFmt(BoldPMConsts.sLogObjectsWithBrokenLinks, [ObjectSQLMapper.ExpressionName, SingleLink.ExpressionName]);
                        BoldLog.Log(IdList.CommaText);
                        remedy.Add(SysUtils.Format(BoldPMConsts.sCommentCleanRelation, ObjectSQLMapper.ExpressionName, SingleLink.ExpressionName));
                        remedy.Add(SysUtils.Format("UPDATE %s SET %s = -1 WHERE BOLD_ID IN (%s);", LinkTable.SQLName, SingleLink.MainColumnName, IdList.CommaText));
                    }

                    // find inconsistent single-single embedded links
                    if (ObjectSQLMapper.SystemPersistenceMapper.ObjectPersistenceMappers[SingleLink.OtherEndObjectPMIndex] is TBoldObjectSQLMapper ClassOfOtherEnd &&
                        ClassOfOtherEnd.MemberPersistenceMappers[SingleLink.OtherEndMemberPMIndex] is TBoldEmbeddedSingleLinkDefaultMapper SingleRoleOfOtherEnd)
                    {
                        Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.SingleSingleEmbeddInconsistencyTest,
                            ObjectSQLMapper.MainTable.SQLName, ClassOfOtherEnd.MainTable!.SQLName, SingleLink.MainColumnName, SingleRoleOfOtherEnd.MainColumnName));
                        Query.Open();
                        IdList.Clear();
                        while (!Query.Eof)
                        {
                            IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
                            Query.Next();
                        }

                        Query.Close();
                        if (IdList.Count > 0)
                        {
                            BoldLog.LogFmt(BoldPMConsts.sLogObjectsWithWrongLinks, [ObjectSQLMapper.ExpressionName, SingleLink.ExpressionName]);
                            BoldLog.Log(IdList.CommaText);
                            remedy.Add(SysUtils.Format(BoldPMConsts.sCommentCleanRelation, ObjectSQLMapper.ExpressionName, SingleLink.ExpressionName));
                            remedy.Add(SysUtils.Format("UPDATE %s SET %s = -1 WHERE BOLD_ID IN (%s);", ObjectSQLMapper.MainTable.SQLName, SingleLink.MainColumnName, IdList.CommaText));
                        }
                    }
                }
            }
        }
    }

    /// <summary>Rows in the class's own table of types that are not the class or a subclass.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValidateStrayObjects(TBoldObjectDefaultMapper ObjectDefaultMapper)
    {
        TBoldAllInstancesMappingInfo[] OwnMapping = SystemSQLMapper.MappingInfo.GetAllInstancesMapping(ObjectDefaultMapper.ExpressionName);
        if (OwnMapping.Length == 1 && !OwnMapping[0].ClassIdRequired)
        {
            var IdList = new TStringList();
            string TableName = ObjectDefaultMapper.MainTable!.SQLName;
            Query.AssignSQLText(SysUtils.Format(BoldDbDataValidator.StrayObjectsTest, TableName, ObjectDefaultMapper.SubClassesID));
            Query.Open();
            while (!Query.Eof)
            {
                IdList.Add(Query.FieldByName(BoldDbDataValidator.Field_BOLD_ID).AsString);
                Query.Next();
            }

            Query.Close();
            if (IdList.Count > 0)
            {
                BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sLogObjectsWithIllegalType, [TableName]);
                BoldLogHandler.BoldLog().Log(IdList.CommaText);
                remedy.Add(SysUtils.Format(BoldPMConsts.sCommentRemoveObjectsWithIllegaltype, TableName));
                remedy.Add(SysUtils.Format("DELETE FROM %s WHERE BOLD_ID IN (%s);", TableName, IdList.CommaText));
                Query.AssignSQLText(SysUtils.Format("SELECT * FROM %s WHERE BOLD_ID IN (%s)", TableName, IdList.CommaText));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private IBoldQuery GetQuery() => fQuery ??= SystemSQLMapper.GetQuery();

    [Delphi(PortStatus.Ported)]
    protected override void DeActivate()
    {
        if (fQuery is not null)
        {
            SystemSQLMapper.ReleaseQuery(ref fQuery);
        }

        base.DeActivate();
    }

    /// <summary>Remedy SQL that deletes the objects from all the class's tables except the XFiles table.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AddRemedyForDeleteObjects(TBoldObjectSQLMapper Mapper, TStringList IdList)
    {
        for (int i = 0; i < Mapper.AllTables.Count; i++)
        {
            if (Mapper.AllTables[i] != ((TBoldDefaultSystemDescription)Mapper.SystemPersistenceMapper.PSSystemDescription).XFilestable)
            {
                remedy.Add(SysUtils.Format("DELETE FROM %s WHERE BOLD_ID IN (%s);", Mapper.AllTables[i].SQLName, IdList.CommaText));
            }
        }
    }

    [Delphi(PortStatus.Ported)] protected IBoldQuery Query => GetQuery();
    [Delphi(PortStatus.Ported)] protected TStringList TypeTestedTables => fTypeTestedTables;
    [Delphi(PortStatus.Ported)] protected TStringList ExistenceInParentTestedTables => fExistenceInParentTestedTables;
}
