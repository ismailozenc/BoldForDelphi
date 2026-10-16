// Delphi: Source/Persistence/DB/BoldPersistenceControllerDefault.pas
// Port map: CSharp/port/units/BoldPersistenceControllerDefault.md
// The persistence controller of a SQL database with the default mapping: it makes the default system mapper on first
// use and passes the persistence calls to it, once the database is open.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPersistenceControllerDefault", Symbol = "TBoldPersistenceControllerDefault.Destroy", Note = "Freed the mapper and configuration; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A persistence controller over the default mapping of a database the event provides.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceControllerDefault : TBoldPersistenceController
{
    private TBoldSystemDefaultMapper? fPersistenceMapper;
    private bool fActive;
    private readonly TMoldModel fMoldModel;
    private readonly TBoldTypeNameDictionary fTypeNameDictionary;
    private readonly TBoldSQLDataBaseConfig fSQLDataBaseConfig;
    private readonly TBoldGetDatabaseEvent? fOnGetDatabase;

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceControllerDefault.CreateFromMold")]
    public TBoldPersistenceControllerDefault(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary, TBoldSQLDataBaseConfig SQLDataBaseConfig,
        TBoldGetDatabaseEvent? GetDatabaseFunc)
    {
        fMoldModel = MoldModel;
        fTypeNameDictionary = TypeNameDictionary;
        fActive = false;
        fSQLDataBaseConfig = new TBoldSQLDataBaseConfig();
        fSQLDataBaseConfig.AssignConfig(SQLDataBaseConfig);
        fSQLDataBaseConfig.OnChange = SQLDatabaseConfigChanged;
        fOnGetDatabase = GetDatabaseFunc;
    }

    [Delphi(PortStatus.Ported)]
    private void EnsureActive(string Action)
    {
        if (!fActive)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sNotActive, GetType().Name, Action));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemDefaultMapper GetPersistenceMapper() =>
        fPersistenceMapper ??= new TBoldSystemDefaultMapper(fMoldModel, fTypeNameDictionary, SQLDataBaseConfig, fOnGetDatabase);

    [Delphi(PortStatus.Ported)]
    private void SQLDatabaseConfigChanged(object? Sender)
    {
        if (fPersistenceMapper is not null)
        {
            PersistenceMapper.SQLDataBaseConfig.AssignConfig(SQLDataBaseConfig);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        EnsureActive("PMExactifyIDs");
        PersistenceMapper.RootClassObjectPersistenceMapper.EnsureIDsExact(ObjectIdList, TranslationList);
    }

    /// <summary>Makes the ids exact, fetches, and commits a transaction the fetch started.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode, TBoldClientID BoldClientID)
    {
        EnsureActive("PMFetch");
        SendExtendedEvent(BoldSubscription.bpeStartFetch, [ObjectIdList, MemberIdList]);
        var TranslationList = new TBoldIDTranslationList();
        var ExactifyTranslationList = new TBoldIDTranslationList();
        TBoldObjectIdList FetchIdList = ObjectIdList.Clone();
        try
        {
            bool WasInTransaction = PersistenceMapper.Database.InTransaction;
            PMExactifyIds(FetchIdList, ExactifyTranslationList);
            ValueSpace.ExactifyIDs(ExactifyTranslationList);
            ObjectIdList.ExactifyIds(ExactifyTranslationList);
            FetchIdList.ExactifyIds(ExactifyTranslationList);
            PersistenceMapper.PMFetch(FetchIdList, ValueSpace, MemberIdList, FetchMode, TranslationList);
            ValueSpace.ApplytranslationList(TranslationList);
            if (!WasInTransaction && PersistenceMapper.Database.InTransaction)
            {
                PersistenceMapper.Database.Commit();
            }
        }
        finally
        {
            SendEvent(BoldSubscription.bpeEndFetch);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition, TBoldClientID BoldClientID)
    {
        EnsureActive("PMFetchIdListWithCondition");
        SendExtendedEvent(BoldSubscription.bpeStartFetchID, [Condition]);
        var TranslationList = new TBoldIDTranslationList();
        bool WasInTransaction = PersistenceMapper.Database.InTransaction;
        PersistenceMapper.PMFetchClassWithCondition(ObjectIdList, ValueSpace, Condition, FetchMode, TranslationList);
        ValueSpace.ApplytranslationList(TranslationList);
        if (!WasInTransaction && PersistenceMapper.Database.InTransaction)
        {
            PersistenceMapper.Database.Commit();
        }

        SendEvent(BoldSubscription.bpeEndFetchID);
    }

    /// <summary>Updates through the mapper; unless the precondition failed, the new time stamp and ids are taken.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldUpdatePrecondition? Precondition,
        TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        EnsureActive("PMUpdate");
        SendExtendedEvent(BoldSubscription.bpeStartUpdate, [ObjectIdList, ValueSpace]);
        TBoldIDTranslationList aTranslationList = TranslationList ?? new TBoldIDTranslationList();
        try
        {
            TBoldTimeStampType NewTimeStamp = 0;
            PersistenceMapper.PMUpdate(ObjectIdList, ValueSpace, Old_Values, Precondition, aTranslationList, ref NewTimeStamp);
            if (Precondition is null || !Precondition.Failed)
            {
                TimeStamp = NewTimeStamp;
                ValueSpace.ApplytranslationList(aTranslationList);
            }
        }
        finally
        {
            SendEvent(BoldSubscription.bpeEndUpdate);
        }
    }

    /// <summary>Opens the database (reading the db types, and the mapping if asked); a failed open discards the mapper.</summary>
    [Delphi(PortStatus.Ported)]
    public void OpenDatabase(bool ReadMappingFromDb)
    {
        try
        {
            PersistenceMapper.OpenDatabase(true, ReadMappingFromDb);
        }
        catch
        {
            fPersistenceMapper = null;
            throw;
        }

        fActive = true;
    }

    [Delphi(PortStatus.Ported)]
    public void CloseDataBase()
    {
        PersistenceMapper.CloseDataBase();
        fActive = false;
    }

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        EnsureActive("PMTranslateToGlobalIds");
        PersistenceMapper.PMTranslateToGlobalIds(ObjectIdList, TranslationList);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList)
    {
        EnsureActive("PMTranslateToLocalIds");
        PersistenceMapper.PMTranslateToLocalIds(GlobalIdList, TranslationList);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList)
    {
        EnsureActive("PMSetReadonlyness");
        PersistenceMapper.PMSetReadonlyness(ReadOnlyList, WriteableList);
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToPeristenceEvents(TBoldSubscriber Subscriber)
    {
        base.SubscribeToPeristenceEvents(Subscriber);
        PersistenceMapper.SubscribeToPersistenceEvents(Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        EnsureActive("ReserveNewIds");
        PersistenceMapper.ReserveNewIds(ValueSpace, ObjectIdList, TranslationList);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp)
    {
        EnsureActive("PMTimestampForTime");
        PersistenceMapper.PMTimestampForTime(ClockTime, ref Timestamp);
    }

    [Delphi(PortStatus.Ported)]
    public override void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime)
    {
        EnsureActive("PMTimeForTimeStamp");
        PersistenceMapper.PMTimeForTimestamp(Timestamp, ref ClockTime);
    }

    [Delphi(PortStatus.Ported)]
    public void StartTransaction() => PersistenceMapper.Database.StartTransaction();

    [Delphi(PortStatus.Ported)]
    public void CommitTransaction() => PersistenceMapper.Database.Commit();

    [Delphi(PortStatus.Ported)]
    public void RollbackTransaction() => PersistenceMapper.Database.RollBack();

    [Delphi(PortStatus.Ported)]
    public bool InTransaction() => PersistenceMapper.Database.InTransaction;

    [Delphi(PortStatus.Ported)] public TBoldSystemDefaultMapper PersistenceMapper => GetPersistenceMapper();
    [Delphi(PortStatus.Ported)] public bool Active => fActive;
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDataBaseConfig => fSQLDataBaseConfig;
}
