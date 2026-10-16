// Delphi: Source/PMapper/Default/BoldCustomBlobMapper.pas
// Port map: CSharp/port/units/BoldCustomBlobMapper.md
// A blob mapper that keeps all blobs in one separate table (id, member name, data) instead of a column of the class.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldCustomBlobMapper", Symbol = "finalization", Note = "Removed the registration; the registry lives as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldCustomBlobMapper.</summary>
public static partial class BoldCustomBlobMapper
{
    internal const string BLOBDATA_TABLENAME = "BLOBDATATABLE";
    internal const string BLOBDATA_DATACOLUMNNAME = "BLOBDATA";
    internal const string BLOBDATA_MEMBERCOLUMNNAME = "MEMBERNAME";

#pragma warning disable CA2255
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization() =>
        BoldPMapperLists.BoldMemberPersistenceMappers().AddDescriptor(typeof(TBoldPMBlobWithSeparateTable), TBoldAbstractionLevel.alConcrete);
#pragma warning restore CA2255
}

/// <summary>
/// A blob stored in the shared blob table (columns: the object id, the member name, the data), fetched and written by
/// itself. Versioned classes are not supported.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMBlobWithSeparateTable : TBoldMemberDefaultMapper
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldPMBlobWithSeparateTable.CreateFromMold")]
    public TBoldPMBlobWithSeparateTable(TMoldMember? moldMember, TMoldClass moldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        if (ExpressionName.Length > ColumnSize(1))
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sMemberNameTooLong, GetType().Name, ExpressionName, ColumnSize(1)));
        }

        fCustomCreateUpDate = true;
        fCustomFetch = true;
        if (moldClass.Versioned)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sVersionedClassesNotSupported, GetType().Name, moldClass.ExpressionName));
        }
    }

    private string MemberNameValue => ExpressionName.ToUpperInvariant();

    /// <summary>The member's blob in the value space (made when missing).</summary>
    [Delphi(PortStatus.Ported)]
    private IBoldBlobContent GetBlobValue(TBoldObjectId Id, IBoldValueSpace ValueSpace)
    {
        IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(Id)
            ?? throw new EBold(SysUtils.Format(BoldPMConsts.sObjectNotInValueSpace, GetType().Name, ObjectPersistenceMapper.ExpressionName, ExpressionName, Id.AsString));
        ObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), ContentName);
        IBoldValue value = ObjectContents.ValueByIndex(MemberIndex)
            ?? throw new EBold(SysUtils.Format(BoldPMConsts.sValueNotInValueSpace, GetType().Name, ObjectPersistenceMapper.ExpressionName, ExpressionName, Id.AsString));
        return value as IBoldBlobContent ?? throw new EBold(SysUtils.Format(BoldPMConsts.sValueNotBlob, GetType().Name, ObjectPersistenceMapper.ExpressionName, ExpressionName));
    }

    [Delphi(PortStatus.Ported)]
    private static string IdListToString(TBoldObjectIdList IdList) => string.Join(", ", Enumerable.Range(0, IdList.Count).Select(i => IdList[i].AsString));

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => ColumnIndex switch
    {
        0 => TFieldType.ftInteger,
        1 => TFieldType.ftString,
        2 => TFieldType.ftBlob,
        _ => base.GetColumnBDEFieldType(ColumnIndex),
    };

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex) => ColumnIndex switch
    {
        0 => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger,
        1 => SysUtils.Format(SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForString, ColumnSize(ColumnIndex)),
        2 => SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForBlob,
        _ => throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnTypeAsSQL", ColumnIndex)),
    };

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnCount() => 0;

    [Delphi(PortStatus.Ported)]
    protected override int GetColumnSize(int ColumnIndex) => ColumnIndex switch
    {
        0 => 0,
        1 => 40,
        2 => 0,
        _ => base.GetColumnSize(ColumnIndex),
    };

    [Delphi(PortStatus.Ported)]
    protected override string GetInitialColumnName(int ColumnIndex) => ColumnIndex switch
    {
        0 => BoldDefs.IDCOLUMN_NAME,
        1 => BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME,
        2 => BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME,
        _ => base.GetInitialColumnName(ColumnIndex),
    };

    [Delphi(PortStatus.Ported)]
    protected override bool CompareField(IBoldObjectContents ObjectContent, IBoldField Field, int ColumnIndex, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCustomCompareRequired, GetType().Name));

    /// <summary>Describes the shared blob table (once); its data column allows null since all blobs share it.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        base.InitializePSDescriptions();
        if (SystemPersistenceMapper.PSSystemDescription.SQLTablesList.ItemsBySQLName(BoldCustomBlobMapper.BLOBDATA_TABLENAME) is null)
        {
            string NotNull = SystemPersistenceMapper.SQLDataBaseConfig.EffectiveSQLForNotNull;
            var BlobTable = new TBoldSQLTableDescription(SystemPersistenceMapper.PSSystemDescription, ObjectPersistenceMapper.Versioned)
            {
                SQLName = BoldCustomBlobMapper.BLOBDATA_TABLENAME,
            };
            BlobTable.AddColumn(InitialColumnName(0), ColumnTypeAsSQL(0), NotNull, ColumnBDEFieldType(0), ColumnSize(0), false, "");
            BlobTable.AddColumn(InitialColumnName(1), ColumnTypeAsSQL(1), NotNull, ColumnBDEFieldType(1), ColumnSize(1), false, "");
            // the blob-column must allow null since it is reused for all blobs, and one of them might allow null
            BlobTable.AddColumn(InitialColumnName(2), ColumnTypeAsSQL(2), "", ColumnBDEFieldType(2), ColumnSize(2), true, "");
            BlobTable.EnsureIndex(InitialColumnName(0) + "," + InitialColumnName(1), true, true);
        }
    }

    /// <summary>Reads the blobs of the objects; objects without a row get null.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldIDTranslationList TranslationList, TBoldObjectIdList? FailureList)
    {
        IBoldQuery? q = SystemPersistenceMapper.GetQuery();
        var ActionList = new TBoldObjectIdList();
        try
        {
            ActionList.AddList(ObjectIdList);
            q.AssignSQLText(SysUtils.Format("SELECT %s, %s FROM %s WHERE %s IN (%s) AND UPPER(%s) = '%s'",
                BoldDefs.IDCOLUMN_NAME, BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME, BoldCustomBlobMapper.BLOBDATA_TABLENAME,
                BoldDefs.IDCOLUMN_NAME, IdListToString(ActionList), BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME, MemberNameValue));
            q.Open();
            while (!q.Eof)
            {
                TBoldObjectId Id = SystemPersistenceMapper.NewIdFromQuery(q, -1, 0, BoldDefs.BOLDMAXTIMESTAMP);
                GetBlobValue(Id, ValueSpace).asBlob = q.Fields(1).AsBlob;
                ActionList.RemoveByIndex(ActionList.IndexByID(Id));
                q.Next();
            }

            for (int i = 0; i < ActionList.Count; i++)
            {
                GetBlobValue(ActionList[i], ValueSpace).SetContentToNull();
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported, Note = "This fork's Delphi does not set the data (the assignment is commented out); it is set here.")]
    public override void PMCreate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        IBoldExecQuery? q = SystemPersistenceMapper.GetExecQuery();
        try
        {
            q.AssignSQLText(SysUtils.Format("INSERT INTO %s (%s, %s, %s) VALUES (:%s, :%s, :%s)",
                BoldCustomBlobMapper.BLOBDATA_TABLENAME, BoldDefs.IDCOLUMN_NAME, BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME, BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME,
                BoldDefs.IDCOLUMN_NAME, BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME, BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME));
            q.ParamByName(BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME).AsString = MemberNameValue;
            for (int i = 0; i < ObjectIdList.Count; i++)
            {
                q.ParamByName(BoldDefs.IDCOLUMN_NAME).AsInteger = SysUtils.StrToInt(TranslationList.TranslateToNewId(ObjectIdList[i])!.AsString);
                SetDataParam(q.ParamByName(BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME), GetBlobValue(ObjectIdList[i], ValueSpace));
                q.ExecSQL();
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported, Note = "This fork's Delphi does not set the data (the assignment is commented out); it is set here.")]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        IBoldExecQuery? q = SystemPersistenceMapper.GetExecQuery();
        try
        {
            q.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = :%s WHERE %s = :%s AND UPPER(%s) = '%s'",
                BoldCustomBlobMapper.BLOBDATA_TABLENAME, BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME, BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME,
                BoldDefs.IDCOLUMN_NAME, BoldDefs.IDCOLUMN_NAME, BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME, MemberNameValue));
            for (int i = 0; i < ObjectIdList.Count; i++)
            {
                SetDataParam(q.ParamByName(BoldCustomBlobMapper.BLOBDATA_DATACOLUMNNAME), GetBlobValue(ObjectIdList[i], ValueSpace));
                q.ParamByName(BoldDefs.IDCOLUMN_NAME).AsInteger = SysUtils.StrToInt(ObjectIdList[i].AsString);
                q.ExecSQL();
            }
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref q);
        }
    }

    /// <summary>The blob as the data parameter (cleared for null).</summary>
    private void SetDataParam(IBoldParameter Param, IBoldBlobContent Value)
    {
        if (Value.IsNull)
        {
            Param.Clear();
        }
        else
        {
            Param.DataType = SystemPersistenceMapper.SQLDataBaseConfig.FieldTypeForBlob;
            Param.SetAsBlob(Value.asBlob);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void PMDelete(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        IBoldExecQuery? q = SystemPersistenceMapper.GetExecQuery();
        try
        {
            q.AssignSQLText(SysUtils.Format("DELETE FROM %s WHERE %s IN (%s) AND UPPER(%s) = '%s'",
                BoldCustomBlobMapper.BLOBDATA_TABLENAME, BoldDefs.IDCOLUMN_NAME, IdListToString(ObjectIdList), BoldCustomBlobMapper.BLOBDATA_MEMBERCOLUMNNAME, MemberNameValue));
            q.ExecSQL();
        }
        finally
        {
            SystemPersistenceMapper.ReleaseExecQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported, Note = "A virtual class function in Delphi; a static method found by reflection (BoldPMappers.ClassCanStore).")]
    public static new bool CanStore(string ContentName) => ContentName == BoldDefaultStreamNames.BoldContentName_Blob;
}
