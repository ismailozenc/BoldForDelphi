// Delphi: Source/PMapper/SQL/BoldSQLMappingInfo.pas
// Port map: CSharp/port/units/BoldSQLMappingInfo.md
// The mapping information of a SQL database: where each member, class extent and object is stored, and the db type of each class.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldSQLMappingInfo.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldSQLMappingInfoClass", Note = "A class reference; System.Type in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldMemberMappingArray", Note = "TBoldMemberMappingInfo[] in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldObjectStorageMappingArray", Note = "TBoldObjectStorageMappingInfo[] in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldAllInstancesMappingArray", Note = "TBoldAllInstancesMappingInfo[] in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLMappingInfo", Symbol = "TBoldClassMappingArray", Note = "TBoldClassMappingInfo[] in .NET.")]

namespace Bold;

/// <summary>Unit-level state of BoldSQLMappingInfo: the positions of the shared indexes.</summary>
public static partial class BoldSQLMappingInfo
{
    internal static int IX_ClassExpressionNameIndex = -1;
    internal static int IX_ClassAndMemberExpressionNameIndex = -1;
}

/// <summary>A mapping for a class, by the class's expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassMappingInfo : TBoldMemoryManagedObject
{
    private readonly string fClassExpressionName;

    [Delphi(PortStatus.Ported)]
    public TBoldClassMappingInfo(string ClassExpressionName)
    {
        fClassExpressionName = ClassExpressionName;
    }

    [Delphi(PortStatus.Ported)] public string ClassExpressionName => fClassExpressionName;
}

/// <summary>Where a member is stored: its table, columns and mapper.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberMappingInfo : TBoldClassMappingInfo
{
    private readonly string fMemberName;
    private readonly string fTableName;
    private readonly string fColumns;
    private readonly string fMapperName;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberMappingInfo(string ClassExpressionName, string MemberName, string TableName, string Columns, string MapperName)
        : base(ClassExpressionName)
    {
        fMemberName = MemberName;
        fTableName = TableName;
        fColumns = Columns;
        fMapperName = MapperName;
    }

    [Delphi(PortStatus.Ported)]
    private string GetColumnByIndex(int Index)
    {
        var s = new TStringList { CommaText = Columns };
        return s[Index];
    }

    [Delphi(PortStatus.Ported)]
    public bool CompareMapping(TBoldMemberMappingInfo Mapping) =>
        SysUtils.SameText(TableName, Mapping.TableName) && SysUtils.SameText(Columns, Mapping.Columns);

    [Delphi(PortStatus.Ported)] public string MemberName => fMemberName;
    [Delphi(PortStatus.Ported)] public string TableName => fTableName;
    [Delphi(PortStatus.Ported)] public string MapperName => fMapperName;
    [Delphi(PortStatus.Ported)] public string Columns => fColumns;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public string ColumnByIndex(int Index) => GetColumnByIndex(Index);
}

/// <summary>The db type of a class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDbTypeMappingInfo : TBoldClassMappingInfo
{
    private readonly TBoldDbType fDbType;

    [Delphi(PortStatus.Ported)]
    public TBoldDbTypeMappingInfo(string ClassExpressionName, TBoldDbType DbType) : base(ClassExpressionName)
    {
        fDbType = DbType;
    }

    [Delphi(PortStatus.Ported)] public TBoldDbType DbType => fDbType;
}

/// <summary>A table to read for the instances of a class, and whether the class id must be checked there.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAllInstancesMappingInfo : TBoldClassMappingInfo
{
    internal bool fClassIdRequired;
    private readonly string fTableName;

    [Delphi(PortStatus.Ported)]
    public TBoldAllInstancesMappingInfo(string ClassExpressionName, string TableName, bool ClassIDRequired) : base(ClassExpressionName)
    {
        fTableName = TableName;
        fClassIdRequired = ClassIDRequired;
    }

    [Delphi(PortStatus.Ported)]
    public bool CompareMapping(TBoldAllInstancesMappingInfo Mapping) =>
        SysUtils.SameText(TableName, Mapping.TableName) && ClassIdRequired == Mapping.ClassIdRequired;

    [Delphi(PortStatus.Ported)] public string TableName => fTableName;
    [Delphi(PortStatus.Ported)] public bool ClassIdRequired => fClassIdRequired;
}

/// <summary>A table that holds a row for each object of a class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectStorageMappingInfo : TBoldClassMappingInfo
{
    private readonly string fTableName;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectStorageMappingInfo(string ClassExpressionName, string TableName) : base(ClassExpressionName)
    {
        fTableName = TableName;
    }

    [Delphi(PortStatus.Ported)]
    public bool CompareMapping(TBoldObjectStorageMappingInfo Mapping) => SysUtils.SameText(TableName, Mapping.TableName);

    [Delphi(PortStatus.Ported)] public string TableName => fTableName;
}

/// <summary>Indexes class mappings by class expression name.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldClassMappingIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldClassMappingInfo)Item!).ClassExpressionName;
}

/// <summary>Indexes member mappings by "Class.Member".</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldMemberMappingIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item)
    {
        var MappingInfo = (TBoldMemberMappingInfo)Item!;
        return KeyStringForExpressionNames(MappingInfo.ClassExpressionName, MappingInfo.MemberName);
    }

    [Delphi(PortStatus.Ported)]
    internal string KeyStringForExpressionNames(string ClassExpressionName, string MemberName) => ClassExpressionName + "." + MemberName;

    [Delphi(PortStatus.Ported)]
    internal void FindAllByExpressionNames(string ClassExpressionName, string MemberName, TList aList) =>
        FindAllByString(KeyStringForExpressionNames(ClassExpressionName, MemberName), aList);
}

/// <summary>The member mappings, indexed by class and member expression names.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberMappingList : TBoldIndexableList
{
    [Delphi(PortStatus.Ported)]
    public TBoldMemberMappingList()
    {
        BoldIndexableList.SetIndexVariable(ref BoldSQLMappingInfo.IX_ClassAndMemberExpressionNameIndex, AddIndex(new TBoldMemberMappingIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberMappingInfo[] GetMappingsByExpressionNames(string ClassExpressionName, string MemberName)
    {
        var aList = new TList();
        ((TBoldMemberMappingIndex)Indexes(BoldSQLMappingInfo.IX_ClassAndMemberExpressionNameIndex)!).FindAllByExpressionNames(ClassExpressionName, MemberName, aList);
        var result = new TBoldMemberMappingInfo[aList.Count];
        for (int i = 0; i < aList.Count; i++)
        {
            result[i] = (TBoldMemberMappingInfo)aList[i]!;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberMappingInfo GetItems(int index) => (TBoldMemberMappingInfo)base[index]!;

    [Delphi(PortStatus.Ported)]
    public void FillFromList(TBoldMemberMappingList SourceList)
    {
        for (int i = 0; i < SourceList.Count; i++)
        {
            Add(new TBoldMemberMappingInfo(SourceList[i].ClassExpressionName, SourceList[i].MemberName, SourceList[i].TableName, SourceList[i].Columns, SourceList[i].MapperName));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberMappingList.items")]
    public new TBoldMemberMappingInfo this[int index] => GetItems(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldMemberMappingInfo[] MappingsByExpressionNames(string ClassExpressionName, string MemberName) =>
        GetMappingsByExpressionNames(ClassExpressionName, MemberName);
}

/// <summary>Class mappings, indexed by class expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassMappingList : TBoldIndexableList
{
    [Delphi(PortStatus.Ported)]
    public TBoldClassMappingList()
    {
        BoldIndexableList.SetIndexVariable(ref BoldSQLMappingInfo.IX_ClassExpressionNameIndex, AddIndex(new TBoldClassMappingIndex()));
    }

    [Delphi(PortStatus.Ported)]
    public TBoldClassMappingInfo[] GetMappingsForClassName(string ClassExpressionName)
    {
        var aList = new TList();
        ((TBoldStringHashIndex)Indexes(BoldSQLMappingInfo.IX_ClassExpressionNameIndex)!).FindAllByString(ClassExpressionName, aList);
        var result = new TBoldClassMappingInfo[aList.Count];
        for (int i = 0; i < aList.Count; i++)
        {
            result[i] = (TBoldClassMappingInfo)aList[i]!;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void AddMapping(TBoldClassMappingInfo Mapping) => Add(Mapping);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldClassMappingInfo[] MappingsForClassName(string ClassExpressionName) => GetMappingsForClassName(ClassExpressionName);

    /// <summary>The item at the index, for the typed lists.</summary>
    protected object? ItemAt(int index) => base[index];
}

[Delphi(PortStatus.Ported)]
public class TBoldAllInstancesMappingList : TBoldClassMappingList
{
    [Delphi(PortStatus.Ported)]
    private TBoldAllInstancesMappingInfo GetItems(int index) => (TBoldAllInstancesMappingInfo)ItemAt(index)!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldAllInstancesMappingList.items")]
    public new TBoldAllInstancesMappingInfo this[int index] => GetItems(index);
}

[Delphi(PortStatus.Ported)]
public class TBoldObjectStorageMappingList : TBoldClassMappingList
{
    [Delphi(PortStatus.Ported)]
    private TBoldObjectStorageMappingInfo GetItems(int index) => (TBoldObjectStorageMappingInfo)ItemAt(index)!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectStorageMappingList.items")]
    public new TBoldObjectStorageMappingInfo this[int index] => GetItems(index);
}

[Delphi(PortStatus.Ported)]
public class TBoldDbTypeMappingList : TBoldClassMappingList
{
    [Delphi(PortStatus.Ported)]
    private TBoldDbTypeMappingInfo GetItems(int index) => (TBoldDbTypeMappingInfo)ItemAt(index)!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldDbTypeMappingList.items")]
    public new TBoldDbTypeMappingInfo this[int index] => GetItems(index);
}

/// <summary>The mapping information of a database; subclasses read it from and write it to their own tables.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSQLMappingInfo
{
    private TBoldDbType fHighestUsedDbtype;
    private readonly int fMaxDBIdentifierLength;
    private readonly TBoldNationalCharConversion fNationalCharConversion;
    protected string fSystemTablePrefix;
    protected TBoldMemberMappingList fMemberMapping;
    protected TBoldAllInstancesMappingList fAllInstancesMapping;
    protected TBoldObjectStorageMappingList fObjectStorageMapping;
    protected TBoldDbTypeMappingList fDbTypeMapping;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLMappingInfo(string SystemTablePrefix, int MaxDBIdentifierLength, TBoldNationalCharConversion NationalCharConversion)
    {
        fSystemTablePrefix = SystemTablePrefix;
        fMemberMapping = new TBoldMemberMappingList();
        fAllInstancesMapping = new TBoldAllInstancesMappingList();
        fObjectStorageMapping = new TBoldObjectStorageMappingList();
        fDbTypeMapping = new TBoldDbTypeMappingList();
        fHighestUsedDbtype = -1;
        fMaxDBIdentifierLength = MaxDBIdentifierLength;
        fNationalCharConversion = NationalCharConversion;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAllInstancesMappingInfo GetAllInstancesMappingInfo(int index) => fAllInstancesMapping[index];

    [Delphi(PortStatus.Ported)]
    private TBoldMemberMappingInfo GetMemberMappingInfo(int index) => fMemberMapping[index];

    [Delphi(PortStatus.Ported)]
    private TBoldObjectStorageMappingInfo GetObjectStorageMappingInfo(int index) => fObjectStorageMapping[index];

    [Delphi(PortStatus.Ported)]
    private TBoldDbTypeMappingInfo GetDbTypeMappingInfo(int Index) => fDbTypeMapping[Index];

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldMemberMappingInfo MemberMappingInfo(int index) => GetMemberMappingInfo(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldAllInstancesMappingInfo AllInstancesMappingInfo(int index) => GetAllInstancesMappingInfo(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldObjectStorageMappingInfo ObjectStorageMappingInfo(int index) => GetObjectStorageMappingInfo(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldDbTypeMappingInfo DbTypeMapping(int Index) => GetDbTypeMappingInfo(Index);

    [Delphi(PortStatus.Ported)]
    protected void ClearDbTypes() => fDbTypeMapping.Clear();

    [Delphi(PortStatus.Ported)]
    protected void ClearMappingInfo()
    {
        fMemberMapping.Clear();
        fAllInstancesMapping.Clear();
        fObjectStorageMapping.Clear();
    }

    [Delphi(PortStatus.Ported)]
    public abstract void ReadDataFromDB(IBoldDataBase DataBase, bool ReadDbTypeFromDB, bool ReadMappingFromDB);

    [Delphi(PortStatus.Ported)]
    public void WriteDataToDB(IBoldDataBase DataBase)
    {
        if (BoldLogHandler.BoldLog().ProcessInterruption())
        {
            return;
        }

        BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingMappingToDB);
        var Script = new TStringList();
        IBoldExecQuery? q = DataBase.GetExecQuery();
        try
        {
            ScriptForWriteData(Script);
            BoldLogHandler.BoldLog().ProgressMax = Script.Count;
            for (int i = 0; i < Script.Count; i++)
            {
                q.AssignSQLText(Script[i]);
                q.ExecSQL();
                BoldLogHandler.BoldLog().Progress = i;
            }
        }
        finally
        {
            DataBase.ReleaseExecQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract void ScriptForWriteData(TStrings Script, string Separator = "", bool ClearFirst = true, string terminator = "");

    [Delphi(PortStatus.Ported)]
    public TBoldSQLMappingInfo CloneWithoutDbType()
    {
        var result = (TBoldSQLMappingInfo)Activator.CreateInstance(GetType(), fSystemTablePrefix, fMaxDBIdentifierLength, fNationalCharConversion)!;
        for (int i = 0; i < fMemberMapping.Count; i++)
        {
            result.AddMemberMapping(MemberMappingInfo(i).ClassExpressionName, MemberMappingInfo(i).MemberName, MemberMappingInfo(i).TableName,
                MemberMappingInfo(i).Columns, MemberMappingInfo(i).MapperName);
        }

        for (int i = 0; i < fAllInstancesMapping.Count; i++)
        {
            result.AddAllInstancesMapping(AllInstancesMappingInfo(i).ClassExpressionName, AllInstancesMappingInfo(i).TableName, AllInstancesMappingInfo(i).ClassIdRequired);
        }

        for (int i = 0; i < fObjectStorageMapping.Count; i++)
        {
            result.AddObjectStorageMapping(ObjectStorageMappingInfo(i).ClassExpressionName, ObjectStorageMappingInfo(i).TableName);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void AddMemberMapping(string ClassExpressionName, string MemberName, string TableName, string ColumnNames, string MapperName)
    {
        foreach (TBoldMemberMappingInfo old in GetMemberMappings(ClassExpressionName, MemberName))
        {
            if (SysUtils.SameText(old.TableName, TableName) && SysUtils.SameText(old.Columns, ColumnNames))
            {
                return;
            }
        }

        fMemberMapping.Add(new TBoldMemberMappingInfo(ClassExpressionName, MemberName, TableName, ColumnNames, MapperName));
    }

    [Delphi(PortStatus.Ported)]
    public void AddAllInstancesMapping(string ClassExpressionName, string TableName, bool ClassIdRequired)
    {
        foreach (TBoldAllInstancesMappingInfo old in GetAllInstancesMapping(ClassExpressionName))
        {
            if (SysUtils.SameText(old.TableName, TableName))
            {
                old.fClassIdRequired = old.ClassIdRequired || ClassIdRequired;
                return;
            }
        }

        fAllInstancesMapping.Add(new TBoldAllInstancesMappingInfo(ClassExpressionName, TableName, ClassIdRequired));
    }

    [Delphi(PortStatus.Ported)]
    public void AddObjectStorageMapping(string ClassExpressionName, string TableName)
    {
        foreach (TBoldObjectStorageMappingInfo old in GetObjectStorageMapping(ClassExpressionName))
        {
            if (SysUtils.SameText(old.TableName, TableName))
            {
                return;
            }
        }

        fObjectStorageMapping.Add(new TBoldObjectStorageMappingInfo(ClassExpressionName, TableName));
    }

    [Delphi(PortStatus.Ported)]
    public void AddTypeIdMapping(string ClassExpressionName, TBoldDbType DbType)
    {
        if (ClassExpressionName.Length == 0)
        {
            throw new EBoldInternal(SysUtils.Format("%s.AddTypeIdMapping: ClassExpressionName is empty (dbtype: %d)", GetType().Name, DbType));
        }

        TBoldDbType OldMapping = GetDbTypeMapping(ClassExpressionName);
        if (OldMapping != -1 && OldMapping != DbType)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sMultipleDBTypes, GetType().Name, ClassExpressionName, DbType, OldMapping));
        }

        if (OldMapping == -1)
        {
            fDbTypeMapping.AddMapping(new TBoldDbTypeMappingInfo(ClassExpressionName, DbType));
            if (DbType > fHighestUsedDbtype)
            {
                fHighestUsedDbtype = DbType;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldMemberMappingInfo[] GetMemberMappings(string ClassExpressionName, string MemberName) =>
        fMemberMapping.MappingsByExpressionNames(ClassExpressionName, MemberName);

    [Delphi(PortStatus.Ported)]
    public TBoldAllInstancesMappingInfo[] GetAllInstancesMapping(string ClassExpressionName) =>
        Array.ConvertAll(fAllInstancesMapping.MappingsForClassName(ClassExpressionName), m => (TBoldAllInstancesMappingInfo)m);

    [Delphi(PortStatus.Ported)]
    public TBoldObjectStorageMappingInfo[] GetObjectStorageMapping(string ClassExpressionName) =>
        Array.ConvertAll(fObjectStorageMapping.MappingsForClassName(ClassExpressionName), m => (TBoldObjectStorageMappingInfo)m);

    [Delphi(PortStatus.Ported)]
    public TBoldDbType GetDbTypeMapping(string ClassExpressionName)
    {
        TBoldClassMappingInfo[] MappingArray = fDbTypeMapping.GetMappingsForClassName(ClassExpressionName);
        return MappingArray.Length > 0 ? ((TBoldDbTypeMappingInfo)MappingArray[0]).DbType : BoldDefs.NO_CLASS;
    }

    [Delphi(PortStatus.Ported)] public TBoldDbType HighestUsedDbType => fHighestUsedDbtype;
    [Delphi(PortStatus.Ported)] public int MaxDBIdentifierLength => fMaxDBIdentifierLength;
    [Delphi(PortStatus.Ported)] public TBoldNationalCharConversion NationalCharConversion => fNationalCharConversion;
    [Delphi(PortStatus.Ported)] public TBoldMemberMappingList MemberMappings => fMemberMapping;
    [Delphi(PortStatus.Ported)] public TBoldAllInstancesMappingList AllInstancesMappings => fAllInstancesMapping;
    [Delphi(PortStatus.Ported)] public TBoldObjectStorageMappingList ObjectStorageMappings => fObjectStorageMapping;
    [Delphi(PortStatus.Ported)] public TBoldDbTypeMappingList DbTypeMappings => fDbTypeMapping;
}
