// Delphi: Source/Persistence/File/BoldPersistenceHandleFile.pas
// Port map: CSharp/port/units/BoldPersistenceHandleFile.md

using static Bold.BoldCoreConsts;
using static Bold.PersistenceConsts;

namespace Bold;

/// <summary>A persistence handle for a file: the file name, whether to cache the data, and the model.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPersistenceHandleFile : TBoldPersistenceHandle
{
    private string FFileName = "";
    private bool FCacheData;
    private TBoldAbstractModel? FBoldModel;

    protected TBoldAbstractPersistenceHandleFile(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)] private void SetFileName(string Value) => FFileName = Value;
    [Delphi(PortStatus.Ported)] private void SetCacheData(bool Value) => FCacheData = Value;
    [Delphi(PortStatus.Ported)] private void SetBoldModel(TBoldAbstractModel? Value) => FBoldModel = Value;

    [Delphi(PortStatus.Ported)]
    public string FileName
    {
        get => FFileName;
        set => SetFileName(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CacheData
    {
        get => FCacheData;
        set => SetCacheData(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractModel? BoldModel
    {
        get => FBoldModel;
        set => SetBoldModel(value);
    }
}

/// <summary>
/// A persistence controller that keeps the whole value space in a file: it reads the file on first use, fetches from the
/// local copy, and writes the whole file on every update. Subclasses read and write the file format.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPersistenceControllerFile : TBoldPersistenceController
{
    private TBoldFreeStandingValueSpace? fFreeStandingValueSpace;
    private readonly string fFileName;
    private TDateTime fFileTimeStamp;
    private readonly bool fCacheData;
    private readonly TMoldModel fMoldModel;

    [Delphi(PortStatus.Ported)]
    protected TBoldPersistenceControllerFile(string filename, bool CacheData, TMoldModel MoldModel)
    {
        fFileName = filename;
        fCacheData = CacheData;
        fMoldModel = MoldModel;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceControllerFile.Destroy")]
    public override void Dispose()
    {
        fFreeStandingValueSpace?.Dispose();
        fFreeStandingValueSpace = null;
        base.Dispose();
    }

    /// <summary>The file's last write time, or day zero when there is no file (Delphi: FileDateToDateTime(FileAge)).</summary>
    private TDateTime FileTimeStamp() => File.Exists(FileName) ? File.GetLastWriteTime(FileName) : SysUtils.ZeroDateTime;

    /// <summary>Reads the file into a new local value space on first use, marking everything current.</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureValueSpace()
    {
        if (fFreeStandingValueSpace is null)
        {
            fFreeStandingValueSpace = new TBoldFreeStandingValueSpace();
            if (File.Exists(FileName))
            {
                ReadValueSpace();
                fFreeStandingValueSpace.MarkAllObjectsAndMembersCurrent();
                fFileTimeStamp = FileTimeStamp();
            }
            else
            {
                fFileTimeStamp = SysUtils.ZeroDateTime;
            }
        }
    }

    /// <summary>Does nothing: unloading "does not work", as Delphi's comment says, so the local copy stays.</summary>
    [Delphi(PortStatus.Ported)]
    private void UnloadLocalValueSpace()
    {
    }

    [Delphi(PortStatus.Ported)]
    private IBoldValueSpace GetValueSpace()
    {
        EnsureValueSpace();
        return fFreeStandingValueSpace!;
    }

    [Delphi(PortStatus.Ported)] protected abstract void WriteValueSpace();
    [Delphi(PortStatus.Ported)] protected abstract void ReadValueSpace();

    [Delphi(PortStatus.Ported)]
    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) => throw new EBold(sNotImplemented);

    /// <summary>Copies the objects (all must be in the file) from the local value space.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID)
    {
        EnsureValueSpace();
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            if (!LocalValueSpace.GetHasContentsForId(ObjectIdList[i]))
            {
                throw new EBold(SysUtils.Format(sObjectNotInFile, GetType().Name, ObjectIdList[i].AsString));
            }
        }

        BoldValueSpaceInterfaces.BoldApplyPartialValueSpace(ValueSpace, LocalValueSpace, ObjectIdList, MemberIdList, false);
        if (!CacheData)
        {
            UnloadLocalValueSpace();
        }
    }

    /// <summary>Gives the ids of the objects of the condition's class and its subclasses; SQL conditions are taken as class conditions.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi shows a message box for SQL conditions; here the message is written to the trace output.")]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID)
    {
        if (Condition is TBoldSQLCondition sqlCondition)
        {
            System.Diagnostics.Trace.WriteLine(sSQLNotSpokenHere);
            var NewCondition = new TBoldConditionWithClass { TopSortedIndex = sqlCondition.TopSortedIndex };
            PMFetchIDListWithCondition(ObjectIdList, ValueSpace, FetchMode, NewCondition, BoldPersistenceController.NOTVALIDCLIENTID);
        }
        else if (Condition is TBoldConditionWithClass classCondition)
        {
            EnsureValueSpace();
            var AllObjectIdList = new TBoldObjectIdList();
            LocalValueSpace.AllObjectIds(AllObjectIdList, false);
            TMoldClass SearchClass = MoldModel.Classes[classCondition.TopSortedIndex];
            for (int i = 0; i < AllObjectIdList.Count; i++)
            {
                TMoldClass CurrentClass = MoldModel.Classes[AllObjectIdList[i].TopSortedIndex];
                if (CurrentClass.ChildTo(SearchClass))
                {
                    ObjectIdList.Add(AllObjectIdList[i]);
                }
            }

            if (!CacheData)
            {
                UnloadLocalValueSpace();
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownConditionType, Condition.GetType().Name));
        }
    }

    /// <summary>Gives new objects stored ids, copies the changes into the local value space and writes the whole file.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        if (Precondition is not null)
        {
            throw new EBold(SysUtils.Format(sPreconditionsNotSupported, GetType().Name, Precondition.GetType().Name));
        }

        EnsureValueSpace();
        if (FileTimeStamp() != fFileTimeStamp)
        {
            UnloadLocalValueSpace();
        }

        TBoldIDTranslationList LocalTranslationList = TranslationList ?? new TBoldIDTranslationList();
        ReserveNewIds(LocalValueSpace, ObjectIdList, LocalTranslationList);
        BoldValueSpaceInterfaces.BoldApplyPartialValueSpace(LocalValueSpace, ValueSpace, ObjectIdList, null, true,
            TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsTransient));
        LocalValueSpace.ApplytranslationList(LocalTranslationList);
        fFreeStandingValueSpace!.RemoveDeletedObjects();
        ValueSpace.ApplytranslationList(LocalTranslationList);
        WriteValueSpace();
        fFileTimeStamp = FileTimeStamp();
    }

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) => throw new EBold(sNotImplemented);

    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList) => throw new EBold(sNotImplemented);

    [Delphi(PortStatus.Ported)]
    public override void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList) => throw new EBold(sNotImplemented);

    [Delphi(PortStatus.Ported)]
    public override bool MultilinksAreStoredInObject() => true;

    /// <summary>Gives each internal id a random default id not used in the value space or the translation list.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            if (!ObjectIdList[i].IsStorable)
            {
                var NewId = new TBoldDefaultID(ObjectIdList[i].TopSortedIndex, ObjectIdList[i].TopSortedIndexExact);
                do
                {
                    NewId.AsInteger = Random.Shared.Next(int.MaxValue);
                }
                while (ValueSpace.GetObjectContentsByObjectId(NewId) is not null || TranslationList.TranslateToOldId(NewId) != NewId);

                TranslationList.AddTranslation(ObjectIdList[i], NewId);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public string FileName => fFileName;
    [Delphi(PortStatus.Ported)] public IBoldValueSpace LocalValueSpace => GetValueSpace();
    [Delphi(PortStatus.Ported)] public bool CacheData => fCacheData;
    [Delphi(PortStatus.Ported)] public TMoldModel MoldModel => fMoldModel;
}
