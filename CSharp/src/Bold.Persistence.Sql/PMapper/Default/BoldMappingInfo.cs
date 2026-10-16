// Delphi: Source/PMapper/Default/BoldMappingInfo.pas
// Port map: CSharp/port/units/BoldMappingInfo.md
// The default mapping information, kept in the BOLD_R_CLSMAP, BOLD_MEMBERMAPPING, BOLD_R_OBJSTORAGE and BOLD_TYPE tables.

namespace Bold;

/// <summary>Reads and writes the mapping information in Bold's own system tables.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultMappingInfo : TBoldSQLMappingInfo
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldDefaultMappingInfo(string SystemTablePrefix, int MaxDBIdentifierLength, TBoldNationalCharConversion NationalCharConversion)
        : base(SystemTablePrefix, MaxDBIdentifierLength, NationalCharConversion)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void ScriptForClearData(TStrings Script, string separator, string terminator)
    {
        void ClearTable(string TableName)
        {
            Script.Add("DELETE FROM " + TableName + terminator);
            if (separator.Length != 0)
            {
                Script.Add(separator);
            }
        }

        ClearTable(AITableName());
        ClearTable(MMTableName());
        ClearTable(OSTableName());
        ClearTable(DbTypeTableName());
    }

    [Delphi(PortStatus.Ported)]
    private string AITableName() =>
        BoldNameExpander.BoldExpandPrefix(BoldDefs.AllInstancesMappingTable_NAME, "", fSystemTablePrefix, MaxDBIdentifierLength, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    private string MMTableName() =>
        BoldNameExpander.BoldExpandPrefix(BoldDefs.MemberMappingTable_NAME, "", fSystemTablePrefix, MaxDBIdentifierLength, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    private string OSTableName() =>
        BoldNameExpander.BoldExpandPrefix(BoldDefs.ObjectStorageMappingTable_NAME, "", fSystemTablePrefix, MaxDBIdentifierLength, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    private string DbTypeTableName() =>
        BoldNameExpander.BoldExpandPrefix(BoldDefs.TYPETABLE_NAME, "", fSystemTablePrefix, MaxDBIdentifierLength, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    private string ExpandColumn(string ColumnName) =>
        BoldNameExpander.BoldExpandName(ColumnName, "", TExpansionType.xtSQL, MaxDBIdentifierLength, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    public override void ReadDataFromDB(IBoldDataBase DataBase, bool ReadDbTypeFromDB, bool ReadMappingFromDB)
    {
        const string SELECTFROM = "SELECT * FROM ";
        IBoldQuery? q = DataBase.GetQuery();

        string GetMappernameFromQuery() => q!.FindField(ExpandColumn(BoldDefs.MMT_MAPPERNAME_COLUMN))?.AsString ?? "";

        try
        {
            if (ReadMappingFromDB)
            {
                ClearMappingInfo();
                q.AssignSQLText(SELECTFROM + AITableName());
                q.Open();
                while (!q.Eof)
                {
                    AddAllInstancesMapping(q.FieldByName(ExpandColumn(BoldDefs.AID_CLASSNAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.AID_TABLENAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.AID_CLASSIDREQUIRED_COLUMN)).AsInteger == 1);
                    q.Next();
                }

                q.Close();

                q.AssignSQLText(SELECTFROM + MMTableName());
                q.Open();
                while (!q.Eof)
                {
                    AddMemberMapping(q.FieldByName(ExpandColumn(BoldDefs.MMT_CLASSNAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.MMT_MEMBERNAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.MMT_TABLENAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.MMT_COLUMNS_COLUMN)).AsString,
                        GetMappernameFromQuery());
                    q.Next();
                }

                q.Close();

                q.AssignSQLText(SELECTFROM + OSTableName());
                q.Open();
                while (!q.Eof)
                {
                    AddObjectStorageMapping(q.FieldByName(ExpandColumn(BoldDefs.ST_CLASSNAME_COLUMN)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.ST_TABLENAME_COLUMN)).AsString);
                    q.Next();
                }

                q.Close();
            }

            if (ReadDbTypeFromDB)
            {
                ClearDbTypes();
                q.AssignSQLText(SELECTFROM + DbTypeTableName());
                q.Open();
                while (!q.Eof)
                {
                    AddTypeIdMapping(q.FieldByName(ExpandColumn(BoldDefs.CLASSNAMECOLUMN_NAME)).AsString,
                        q.FieldByName(ExpandColumn(BoldDefs.TYPECOLUMN_NAME)).AsInteger);
                    q.Next();
                }

                q.Close();
            }
        }
        finally
        {
            DataBase.ReleaseQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ScriptForWriteData(TStrings Script, string Separator = "", bool ClearFirst = true, string terminator = "")
    {
        if (ClearFirst)
        {
            ScriptForClearData(Script, Separator, terminator);
        }

        void AddSeparator()
        {
            if (Separator.Length != 0)
            {
                Script.Add(Separator);
            }
        }

        for (int i = 0; i < fAllInstancesMapping.Count; i++)
        {
            Script.Add(SysUtils.Format("INSERT INTO %s (%s, %s, %s) VALUES ('%s', '%s', %d)%s",
                AITableName(), ExpandColumn(BoldDefs.AID_CLASSNAME_COLUMN), ExpandColumn(BoldDefs.AID_TABLENAME_COLUMN), ExpandColumn(BoldDefs.AID_CLASSIDREQUIRED_COLUMN),
                AllInstancesMappingInfo(i).ClassExpressionName, AllInstancesMappingInfo(i).TableName, AllInstancesMappingInfo(i).ClassIdRequired ? 1 : 0, terminator));
            AddSeparator();
        }

        for (int i = 0; i < fMemberMapping.Count; i++)
        {
            Script.Add(SysUtils.Format("INSERT INTO %s (%s, %s, %s, %s, %s) VALUES ('%s', '%s', '%s', '%s', '%s')%s",
                MMTableName(), ExpandColumn(BoldDefs.MMT_CLASSNAME_COLUMN), ExpandColumn(BoldDefs.MMT_MEMBERNAME_COLUMN), ExpandColumn(BoldDefs.MMT_TABLENAME_COLUMN),
                ExpandColumn(BoldDefs.MMT_COLUMNS_COLUMN), ExpandColumn(BoldDefs.MMT_MAPPERNAME_COLUMN),
                MemberMappingInfo(i).ClassExpressionName, MemberMappingInfo(i).MemberName, MemberMappingInfo(i).TableName, MemberMappingInfo(i).Columns,
                MemberMappingInfo(i).MapperName, terminator));
            AddSeparator();
        }

        for (int i = 0; i < fObjectStorageMapping.Count; i++)
        {
            Script.Add(SysUtils.Format("INSERT INTO %s (%s, %s) VALUES ('%s', '%s')%s",
                OSTableName(), ExpandColumn(BoldDefs.ST_CLASSNAME_COLUMN), ExpandColumn(BoldDefs.ST_TABLENAME_COLUMN),
                ObjectStorageMappingInfo(i).ClassExpressionName, ObjectStorageMappingInfo(i).TableName, terminator));
            AddSeparator();
        }

        for (int i = 0; i < fDbTypeMapping.Count; i++)
        {
            Script.Add(SysUtils.Format("INSERT INTO %s (%s, %s) VALUES (%d, '%s')%s",
                DbTypeTableName(), ExpandColumn(BoldDefs.TYPECOLUMN_NAME), ExpandColumn(BoldDefs.CLASSNAMECOLUMN_NAME),
                DbTypeMapping(i).DbType, DbTypeMapping(i).ClassExpressionName, terminator));
            AddSeparator();
        }
    }
}
