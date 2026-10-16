// Delphi: Source/PMapper/Validator/BoldDbValidator.pas
// Port map: CSharp/port/units/BoldDbValidator.md
// The base of the database validators: it opens the persistence handle (checking the type table against the model
// first), runs the validation, and logs the remedy SQL for what it found. Delphi asks with a message dialog whether
// to add the missing class types; here the question goes to the ConfirmHandler callback.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbValidator", Symbol = "TBoldDbValidator.Destroy", Note = "Freed the remedy list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldDbValidator.</summary>
public static class BoldDbValidator
{
    /// <summary>
    /// Asks the question (yes or no). Delphi shows a message dialog; without a handler the answer is no, so classes
    /// without a db type are only logged.
    /// </summary>
    public static Func<string, bool>? ConfirmHandler { get; set; }
}

/// <summary>Validates a database against the model of a persistence handle.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldDbValidator : System.ComponentModel.Component
{
    private TBoldSystemSQLMapper? fSystemMapper;
    private readonly TStringList fRemedy = new();
    private TBoldAbstractPersistenceHandleDB? fPersistenceHandle;
    private bool FEnabled;

    [Delphi(PortStatus.Ported, Symbol = "TBoldDbValidator.create", Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldDbValidator(System.ComponentModel.IContainer? owner = null)
    {
        owner?.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    private void SetEnabled(bool Value) => FEnabled = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldSystemSQLMapper GetSystemSQLMapper() => fSystemMapper ??= PersistenceHandle!.PersistenceControllerDefault.PersistenceMapper;

    [Delphi(PortStatus.Ported)]
    protected virtual void SetPersistenceHandle(TBoldAbstractPersistenceHandleDB? Value) => fPersistenceHandle = Value;

    /// <summary>
    /// Before the db types are read: gives each object mapper its db type from the type table, logs classes in the
    /// database but not in the model and classes without a db type, and adds the missing types if confirmed.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's message dialog is the BoldDbValidator.ConfirmHandler callback.")]
    private void CheckTypeTableConsistency(TBoldSystemSQLMapper SystemSQLMapper)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        IBoldQuery? query = SystemSQLMapper.GetQuery();
        IBoldExecQuery? ExecQuery = SystemSQLMapper.GetExecQuery();
        try
        {
            query.AssignSQLText(SysUtils.Format("SELECT * FROM %s", TypeTableName()));
            int HighestBoldDbType = -1;
            query.Open();
            while (!query.Eof)
            {
                int BoldDbType = query.FieldByName("BOLD_TYPE").AsInteger;
                if (BoldDbType > HighestBoldDbType)
                {
                    HighestBoldDbType = BoldDbType;
                }

                string Name = query.FieldByName("CLASSNAME").AsString;
                bool Found = false;
                for (int i = 0; i < SystemSQLMapper.ObjectPersistenceMappers.Count; i++)
                {
                    if (SystemSQLMapper.ObjectPersistenceMappers[i] is TBoldObjectSQLMapper ObjectPMapper &&
                        SysUtils.CompareText(ObjectPMapper.ExpressionName, Name) == 0)
                    {
                        ObjectPMapper.BoldDbType = BoldDbType;
                        Found = true;
                        break;
                    }
                }

                if (!Found)
                {
                    BoldLog.LogFmt(BoldPMConsts.sClassInDBNotInModel, [Name, BoldDbType]);
                }

                query.Next();
            }

            query.Close();
            bool MissingClasses = false;
            for (int i = 0; i < SystemSQLMapper.ObjectPersistenceMappers.Count; i++)
            {
                if (SystemSQLMapper.ObjectPersistenceMappers[i] is TBoldObjectSQLMapper { BoldDbType: -1 } ObjectPMapper)
                {
                    if (!MissingClasses)
                    {
                        BoldLog.Separator();
                    }

                    BoldLog.LogFmt(BoldPMConsts.sClassWithoutDBID, [ObjectPMapper.ExpressionName]);
                    MissingClasses = true;
                }
            }

            if (MissingClasses && (BoldDbValidator.ConfirmHandler?.Invoke(BoldPMConsts.sCorrectClassesWithNoID) ?? false))
            {
                BoldLog.Separator();
                ExecQuery.AssignSQLText(SysUtils.Format("INSERT INTO %s (%s, %s) VALUES (:%s, :%s)",
                    TypeTableName(), BoldDefs.TYPECOLUMN_NAME, BoldDefs.CLASSNAMECOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME, BoldDefs.CLASSNAMECOLUMN_NAME));
                for (int i = 0; i < SystemSQLMapper.ObjectPersistenceMappers.Count; i++)
                {
                    if (SystemSQLMapper.ObjectPersistenceMappers[i] is TBoldObjectSQLMapper { BoldDbType: -1 } ObjectPMapper)
                    {
                        HighestBoldDbType++;
                        ObjectPMapper.BoldDbType = HighestBoldDbType;
                        BoldLog.LogFmt(BoldPMConsts.sAddingBoldDBType, [ObjectPMapper.BoldDbType, ObjectPMapper.ExpressionName]);
                        ExecQuery.ParamByName(BoldDefs.TYPECOLUMN_NAME).AsInteger = HighestBoldDbType;
                        ExecQuery.ParamByName(BoldDefs.CLASSNAMECOLUMN_NAME).AsString = ObjectPMapper.ExpressionName;
                        ExecQuery.ExecSQL();
                    }
                }
            }
        }
        finally
        {
            SystemSQLMapper.ReleaseQuery(ref query);
            SystemSQLMapper.ReleaseExecQuery(ref ExecQuery);
        }
    }

    /// <summary>Opens the persistence handle, checking the type table first.</summary>
    [Delphi(PortStatus.Ported)]
    public void Activate()
    {
        SystemSQLMapper.OnPreInitializeBoldDbType = CheckTypeTableConsistency;
        PersistenceHandle!.Active = true;
    }

    /// <summary>Closes the persistence handle and releases its controller.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DeActivate()
    {
        fSystemMapper = null;
        if (PersistenceHandle is not null)
        {
            PersistenceHandle.Active = false;
            PersistenceHandle.ReleasePersistenceController();
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract void Validate();

    /// <summary>Validates, logging the remedy; true when nothing was found. Failures are logged, not raised.</summary>
    [Delphi(PortStatus.Ported)]
    public bool Execute()
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        BoldLog.StartLog(BoldPMConsts.sDatabaseValidation);
        bool result = false;
        if (PersistenceHandle is not null)
        {
            try
            {
                try
                {
                    Activate();
                    Validate();
                    if (remedy.Count != 0)
                    {
                        BoldLog.Separator();
                        BoldLog.Log(BoldPMConsts.sInconsistenciesFound, TBoldLogType.ltWarning);
                        for (int i = 0; i < remedy.Count; i++)
                        {
                            BoldLog.Log(remedy[i]);
                        }

                        BoldLog.Separator();
                    }

                    result = remedy.Count == 0;
                }
                finally
                {
                    DeActivate();
                }
            }
            catch (Exception e)
            {
                BoldLog.LogFmt(BoldPMConsts.sDBValidationFailed, [e.Message], TBoldLogType.ltError);
            }
        }
        else
        {
            BoldLog.Log(BoldPMConsts.sCannotValidateWithoutPHandle, TBoldLogType.ltError);
        }

        BoldLog.EndLog();
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private IBoldDataBase GetDataBase() => PersistenceHandle!.DatabaseInterface;

    /// <summary>The name of the table of class db types.</summary>
    [Delphi(PortStatus.Ported)]
    protected string TypeTableName() =>
        BoldNameExpander.BoldExpandPrefix(BoldDefs.TYPETABLE_NAME, "", PersistenceHandle!.SQLDatabaseConfig.SystemTablePrefix,
            SystemSQLMapper.SQLDataBaseConfig.MaxDbIdentifierLength, SystemSQLMapper.NationalCharConversion);

    [Delphi(PortStatus.Ported)] protected TBoldSystemSQLMapper SystemSQLMapper => GetSystemSQLMapper();
    [Delphi(PortStatus.Ported)] protected IBoldDataBase DataBase => GetDataBase();
    [Delphi(PortStatus.Ported)] public TStringList remedy => fRemedy;
    [Delphi(PortStatus.Ported)] public TBoldAbstractPersistenceHandleDB? PersistenceHandle { get => fPersistenceHandle; set => SetPersistenceHandle(value); }
    [Delphi(PortStatus.Ported)] public bool Enabled { get => FEnabled; set => SetEnabled(value); }
}
