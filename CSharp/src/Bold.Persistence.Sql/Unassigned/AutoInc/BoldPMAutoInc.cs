// Delphi: Source/Unassigned/AutoInc/BoldPMAutoInc.pas
// Port map: CSharp/port/units/BoldPMAutoInc.md
// The mapper of an identity column: the database assigns the value on insert, so the mapper writes nothing. On
// Interbase, which has no identity columns, a generator and a trigger are made with the table.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMAutoInc", Symbol = "finalization", Note = "Removed the registration; the registry lives as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldPMAutoInc.</summary>
public static class BoldPMAutoInc
{
#pragma warning disable CA2255
    /// <summary>Registers the mapper.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization() =>
        BoldPMapperLists.BoldMemberPersistenceMappers().AddDescriptor(typeof(TBoldPMAutoInc), TBoldAbstractionLevel.alConcrete);
#pragma warning restore CA2255
}

/// <summary>An integer identity column, assigned by the database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPMAutoInc : TBoldPMInteger
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldPMAutoInc.CreateFromMold")]
    public TBoldPMAutoInc(TMoldMember? Moldmember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(Moldmember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        fCustomCreateUpDate = true;
    }

    private bool IsInterbase => SystemPersistenceMapper.SQLDataBaseConfig.Engine is TBoldDataBaseEngine.dbeInterbaseSQLDialect1 or TBoldDataBaseEngine.dbeInterbaseSQLDialect3;

    [Delphi(PortStatus.Ported)]
    protected override TFieldType GetColumnBDEFieldType(int ColumnIndex) => TFieldType.ftAutoInc;

    [Delphi(PortStatus.Ported)]
    protected override string GetColumnTypeAsSQL(int ColumnIndex)
    {
        string result = SystemPersistenceMapper.SQLDataBaseConfig.ColumnTypeForInteger;
        if (!IsInterbase)
        {
            result += "IDENTITY(1, 1)";
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override string DefaultDefaultDbValue() => "";

    /// <summary>Nothing to write: the database assigns the value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMCreate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        // just sit back and relax
    }

    /// <summary>Nothing to write: an auto-increment value cannot be modified.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList)
    {
        // just sit back and relax
        // perhaps we should even raise an exception here since it is not allowed
        // to try to modify an autoinc-value
    }

    /// <summary>On Interbase, also makes the generator and the trigger that fills the column.</summary>
    [Delphi(PortStatus.Ported, Note = "Runs the statements on an exec query; Delphi called ExecSQL on an IBoldQuery, which has none.")]
    protected internal override void CreatePersistentStorage()
    {
        base.CreatePersistentStorage();
        if (IsInterbase)
        {
            // Interbase does not have AutoInc, we must create a trigger and a generator.
            IBoldExecQuery? Query = SystemPersistenceMapper.Database.GetExecQuery();
            try
            {
                Query.AssignSQLText(SysUtils.Format("CREATE GENERATOR %s", GetGeneratorName()));
                Query.ExecSQL();
                Query.AssignSQLText(SysUtils.Format("SET GENERATOR %s TO 0", GetGeneratorName()));
                Query.ExecSQL();
                Query.AssignSQLText(SysUtils.Format("CREATE TRIGGER %s FOR %s BEFORE INSERT AS BEGIN NEW.%s = GEN_ID(%s, 1); END",
                    BoldNameExpander.BoldExpandName(SysUtils.Copy(ExpressionName, 1, 8) + "_AutoInc", "", TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccDefault),
                    ((TBoldSQLTableDescription)ColumnDescriptions[0].Owner!).SQLName,
                    ColumnDescriptions[0].SQLName,
                    GetGeneratorName()));
                Query.ExecSQL();
            }
            finally
            {
                SystemPersistenceMapper.Database.ReleaseExecQuery(ref Query);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected string GetGeneratorName() =>
        ColumnDescriptions[0].SQLName + "_" + ((TBoldSQLTableDescription)ColumnDescriptions[0].Owner!).SQLName + "_generator";
}
