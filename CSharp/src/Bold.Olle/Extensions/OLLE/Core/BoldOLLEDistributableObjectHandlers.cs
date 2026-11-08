// Delphi: Source/Extensions/OLLE/Core/BoldOLLEDistributableObjectHandlers.pas
// Port map: CSharp/port/units/BoldOLLEDistributableObjectHandlers.md
//
// The object handlers of OLLE, the object lending library. A persistent storage (PS, an application's database)
// lends its own objects to another storage, which holds them as foreign objects and checks them in again later.
// Objects travel as value spaces with global ids; each storage translates them to and from its local ids, and keeps
// the state of the lending in the OLLE system (the information objects of DistributableInfo).
using Bold.Olle;

// A type alias has no C# declaration to carry a marker; TBoldPSId is a global using alias in
// BoldOLLEDistributableObjectHandlers.Aliases.cs.
[assembly: Delphi(PortStatus.Ported, Unit = "BoldOLLEDistributableObjectHandlers", Symbol = "TBoldPSId", Note = "Global using alias of string, in BoldOLLEDistributableObjectHandlers.Aliases.cs.")]

namespace Bold;

/// <summary>The column names of the OLLE tables, and a routine of the object handlers.</summary>
public static class BoldOLLEDistributableObjectHandlers
{
    [Delphi(PortStatus.Ported)] public const string BOLD_OLL_IDATTRIBUTECOLUMN_NAME = "LOCALID";
    [Delphi(PortStatus.Ported)] public const string BOLD_OLL_PSIDATTRIBUTECOLUMN_NAME = "GLOBALID";
    [Delphi(PortStatus.Ported)] public const string BOLD_OLL_NAMEOFCLASSATTRIBUTECOLUMN_NAME = "NAMEOFCLASS";

    /// <summary>Adds the local id of the object the information object is about to the list.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void AddObjectToIdList(TDistributableObjectInfo aDistributableInfo, TBoldObjectIdList anIdList)
    {
        var anId = new TBoldDefaultID(BoldValueSpaceInterfaces.BUSINESSCLASSESROOT_TOPSORTEDINDEX, false);
        anId.AsInteger = aDistributableInfo.LocalId;
        anIdList.Add(anId);
    }
}

/// <summary>What is done with a link to an object that did not come along.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldLinkResolveAction { blraCut, blraAbort, blraFailObject, blraIgnore, blraMissing }

/// <summary>Resolves links to objects that did not come along: by one action for held objects and another for the rest.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldBrokenLinkResolver
{
    /// <summary>
    /// Resolves the broken single link of the member of the object (cuts it, aborts, ignores it); false when the
    /// object is to fail instead.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public bool ResolveBrokenLink(IBoldObjectContents ObjectContents, int MemberIndex, bool Hold)
    {
        void Cut()
        {
            IBoldValue? aMember = ObjectContents.ValueByIndex(MemberIndex);
            if (aMember is IBoldObjectIdRef anIdRef)
            {
                anIdRef.SetFromId(null);
            }
            else if (aMember is IBoldObjectIdRefPair anIdRefPair)
            {
                anIdRefPair.SetFromIds(null, null);
            }
            else
            {
                throw new EBoldInternal(SysUtils.Format(OlleConsts.sMemberNotSingleLink, GetType().Name));
            }
        }

        bool result = true;
        TBoldLinkResolveAction ResolveAction = Hold ? HeldObjectAction : NonheldObjectAction;
        switch (ResolveAction)
        {
            case TBoldLinkResolveAction.blraCut:
                Cut();
                break;
            case TBoldLinkResolveAction.blraAbort:
                throw new EBold(OlleConsts.sUnresolvedLink);
            case TBoldLinkResolveAction.blraFailObject:
                result = false;
                break;
            case TBoldLinkResolveAction.blraIgnore:
                break;
            case TBoldLinkResolveAction.blraMissing:
                throw new EBoldFeatureNotImplementedYet(OlleConsts.sMissingObjectsNotImplemented);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldLinkResolveAction HeldObjectAction { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldLinkResolveAction NonheldObjectAction { get; set; }
}

/// <summary>
/// What the own and foreign object handlers share: the information objects of the OLLE system (found through the
/// mapping from local ids), fetching, translating ids, updating and locking objects of the application's storage, and
/// a transaction around each operation.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Delphi's private methods, which the handlers in the unit call, are protected.")]
public class TBoldDistributableObjectHandler
{
    private bool fMyTransaction;
    private TMapping? fTheMapping;

    /// <summary>The mapping from local ids to information objects: the one in the OLLE system, made when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    protected TMapping TheMapping()
    {
        if (fTheMapping is null)
        {
            TBoldObjectList Mappings = OllSystem.ClassByExpressionName("Mapping"); // do not localize
            if (Mappings.Count == 0)
            {
                fTheMapping = new TMapping(OllSystem);
            }
            else
            {
                System.Diagnostics.Debug.Assert(Mappings.Count == 1);
                fTheMapping = (TMapping)Mappings[0]!;
            }
        }

        return fTheMapping;
    }

    /// <summary>The information object of the local id among those mapped, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected TDistributableObjectInfo? LookupInfoByLocalId(TBoldDefaultID LocalId) => TheMapping().Q_ObjectInfo(LocalId.AsInteger);

    [Delphi(PortStatus.Ported)]
    protected void AddToMapping(TDistributableObjectInfo anObj) => TheMapping().M_ObjectInfo.Add(anObj);

    /// <summary>Adds the local ids the information objects are about to the list.</summary>
    [Delphi(PortStatus.Ported)]
    protected void GetLocalIdsFor(TDistributableObjectInfoList InfoObjects, TBoldObjectIdList IdList)
    {
        var TempId = new TBoldDefaultID(0, false);
        for (int i = 0; i < InfoObjects.Count; i++)
        {
            TempId.AsInteger = InfoObjects[i]!.LocalId;
            IdList.Add(TempId);
        }
    }

    /// <summary>The information object of the storage with the global id; a new one the first time.</summary>
    [Delphi(PortStatus.Ported)]
    protected TForeignPSInfo GetForeignPSInfo(TBoldPSId PSId)
    {
        TForeignPSInfo result;
        var PSInfoObjectIdList = new TBoldObjectIdList();
        SearchByOcl("ForeignPSInfo.allInstances->select(globalId = '" + PSId + "')", PSInfoObjectIdList); // do not localize
        if (PSInfoObjectIdList.Count == 0)
        {
            result = new TForeignPSInfo(OllSystem);
            result.GlobalID = PSId;
        }
        else
        {
            System.Diagnostics.Debug.Assert(PSInfoObjectIdList.Count == 1);
            TBoldObjectLocator aLocator = OllSystem.EnsuredLocatorByID(PSInfoObjectIdList[0]);
            aLocator.EnsureBoldObject();
            result = (TForeignPSInfo)aLocator.BoldObject!;
        }

        return result;
    }

    /// <summary>Adds the ids of the objects and of every object they link to, each once.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ExtractAllIds(TBoldObjectIdList IdList, IBoldValueSpace ValueSpace, TBoldObjectIdList OutIdList)
    {
        void EnsureIdInList(TBoldObjectId? Id, TBoldObjectIdList IdList)
        {
            if (Id is not null && !IdList.IdInList(Id))
            {
                IdList.Add(Id);
            }
        }

        for (int i = 0; i < IdList.Count; i++)
        {
            EnsureIdInList(IdList[i], OutIdList);
            IBoldObjectContents anObject = ValueSpace.ObjectContentsByObjectId(IdList[i])!;
            for (int j = 0; j < anObject.MemberCount; j++)
            {
                IBoldValue? aValue = anObject.ValueByIndex(j);
                if (aValue is IBoldObjectIdRef IdRef)
                {
                    EnsureIdInList(IdRef.Id, OutIdList);
                }
                else if (aValue is IBoldObjectIdRefPair IdRefPair)
                {
                    EnsureIdInList(IdRefPair.Id1, OutIdList);
                    EnsureIdInList(IdRefPair.Id2, OutIdList);
                }
            }
        }
    }

    /// <summary>Fetches the objects from the application's storage, as objects to distribute.</summary>
    [Delphi(PortStatus.Ported)]
    protected void Fetch(TBoldObjectIdList IdList, IBoldValueSpace ValueSpace) =>
        PController.PMFetch(IdList, ValueSpace, null, BoldDefs.fmDistributable, 0);

    /// <summary>
    /// Finds the information objects of the local ids, first among those mapped, then in the OLLE storage; the ids
    /// with none go to the remaining list.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void GetInfoObjectsFor(TBoldObjectIdList IdList, TDistributableObjectInfoList InfoObjectList, TBoldObjectIdList RemainingIdList)
    {
        static string IdListToSQL(TBoldObjectIdList IdList)
        {
            string result = IdList[0].AsString;
            for (int i = 1; i < IdList.Count; i++)
            {
                result = result + ", " + IdList[i].AsString;
            }

            return result;
        }

        TBoldObjectIdList RemainingIds = IdList.Clone();
        var FetchedInfoObjs = new TDistributableObjectInfoList();
        for (int i = RemainingIds.Count - 1; i >= 0; i--)
        {
            TDistributableObjectInfo? InfoObj = LookupInfoByLocalId((TBoldDefaultID)RemainingIds[i]);
            if (InfoObj is not null)
            {
                InfoObjectList.Add(InfoObj);
                RemainingIds.RemoveByIndex(i);
            }
        }

        if (RemainingIds.Count > 0 && OllSystem.PersistenceController is not null)
        {
            var aCondition = new TBoldSQLCondition();
            var FoundIds = new TBoldObjectIdList();
            aCondition.TopSortedIndex = OllSystem.BoldSystemTypeInfo.ClassTypeInfoByModelName("DistributableObjectInfo")!.TopSortedIndex; // do not localize
            aCondition.WhereFragment = BoldOLLEDistributableObjectHandlers.BOLD_OLL_IDATTRIBUTECOLUMN_NAME + " IN (" + IdListToSQL(RemainingIds) + ")"; // do not localize
            OllSystem.GetAllWithCondition(FetchedInfoObjs, aCondition);
            FetchedInfoObjs.EnsureObjects();
            for (int i = 0; i < FetchedInfoObjs.Count; i++)
            {
                AddToMapping(FetchedInfoObjs[i]!);
            }

            GetLocalIdsFor(FetchedInfoObjs, FoundIds);
            for (int i = 0; i < FoundIds.Count; i++)
            {
                RemainingIds.remove(RemainingIds.IDByID(FoundIds[i])!);
            }

            InfoObjectList.AddList(FetchedInfoObjs);
        }

        RemainingIdList.AddList(RemainingIds);
    }

    /// <summary>New information objects for own objects of the local ids, mapped.</summary>
    [Delphi(PortStatus.Ported)]
    protected void NewOwnInfoObjectsFor(TBoldObjectIdList IdList, TDistributableObjectInfoList InfoObjectList)
    {
        for (int i = IdList.Count - 1; i >= 0; i--)
        {
            var anOwnObjectInfo = new TOwnObjectInfo(OllSystem);
            anOwnObjectInfo.LocalId = ((TBoldDefaultID)IdList[i]).AsInteger;
            AddToMapping(anOwnObjectInfo);
            InfoObjectList.Add(anOwnObjectInfo);
        }
    }

    /// <summary>New information objects for foreign objects of the local ids from the owner, mapped; held when asked.</summary>
    [Delphi(PortStatus.Ported)]
    protected void NewForeignInfoObjectsFor(TBoldObjectIdList IdList, TDistributableObjectInfoList InfoObjectList, TForeignPSInfo Owner, bool Hold = false)
    {
        for (int i = 0; i < IdList.Count; i++)
        {
            var aForeignObjectInfo = new TForeignObjectInfo(OllSystem);
            if (Hold)
            {
                aForeignObjectInfo.InitializeHolding(IdList[i], Owner);
            }
            else
            {
                aForeignObjectInfo.LocalId = ((TBoldDefaultID)IdList[i]).AsInteger;
                aForeignObjectInfo.Owner = Owner;
            }

            AddToMapping(aForeignObjectInfo);
            InfoObjectList.Add(aForeignObjectInfo);
        }
    }

    /// <summary>The translation of the objects' local ids, and those of the objects they link to, to global ids.</summary>
    [Delphi(PortStatus.Ported)]
    protected void MakeGlobalTranslationListFor(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldIDTranslationList GlobalTranslationList)
    {
        var anIdList = new TBoldObjectIdList();
        ExtractAllIds(IdList, ValueSpace, anIdList);
        PController.PMTranslateToGlobalIds(anIdList, GlobalTranslationList);
    }

    /// <summary>The translation of the global ids of the objects, and of the objects they link to, to local ids.</summary>
    [Delphi(PortStatus.Ported)]
    protected void MakeLocalizingTranslationList(IBoldValueSpace ValueSpace, TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList)
    {
        var AllIdList = new TBoldObjectIdList();
        ExtractAllIds(GlobalIdList, ValueSpace, AllIdList);
        PController.PMTranslateToLocalIds(AllIdList, TranslationList);
    }

    /// <summary>Adds the ids of the OLLE objects the OCL expression gives.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SearchByOcl(string OclExpr, TBoldObjectIdList IdList)
    {
        using var anElement = new TBoldIndirectElement();
        OllSystem.EvaluateExpression(OclExpr, anElement);
        var anObjectList = (TBoldObjectList)anElement.Value!;
        for (int i = 0; i < anObjectList.Count; i++)
        {
            IdList.Add(anObjectList.Locators(i)!.BoldObjectID);
        }
    }

    /// <summary>
    /// Writes the objects to the application's storage, all of their values: objects new to the storage (global ids)
    /// are created unless deleted, deleted objects are deleted. The new timestamp comes back.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void Update(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldIDTranslationList TranslationList, ref int TimeStamp)
    {
        for (int i = IdList.Count - 1; i >= 0; i--)
        {
            IBoldObjectContents anObject = ValueSpace.ObjectContentsByObjectId(IdList[i])!;
            for (int j = 0; j < anObject.MemberCount; j++)
            {
                if (anObject.ValueByIndex(j) is { } aValue)
                {
                    aValue.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
                }
            }

            if (IdList[i] is TBoldGlobalId)
            {
                if (anObject.BoldExistenceState == TBoldExistenceState.besDeleted)
                {
                    IdList.RemoveByIndex(i);
                }
                else
                {
                    anObject.BoldExistenceState = TBoldExistenceState.besExisting;
                    anObject.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
                }
            }
            else if (anObject.BoldExistenceState == TBoldExistenceState.besDeleted)
            {
                anObject.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
            }
        }

        PController.PMUpdate(IdList, ValueSpace, null, null, TranslationList, ref TimeStamp, 0);
    }

    /// <summary>Makes the objects read-only, except those to free, which become writeable.</summary>
    [Delphi(PortStatus.Ported)]
    protected void LockAndFreeObjects(TBoldObjectIdList IdList, TBoldObjectIdList FreeList)
    {
        TBoldObjectIdList ReadOnlyList = IdList.Clone();
        for (int i = 0; i < FreeList.Count; i++)
        {
            ReadOnlyList.remove(ReadOnlyList.IDByID(FreeList[i])!);
        }

        PController.PMSetReadOnlyness(ReadOnlyList, FreeList);
    }

    /// <summary>
    /// Checks that the objects link only to objects that came along (or that the storage has): a link to an object
    /// with a global id that is not in the value space is resolved by the broken link resolver, and raises without one.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's nested CascadeToNeighbours, which nothing called, is left out.")]
    protected void VerifyAssociations(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList HoldList)
    {
        int i;

        void VerifyObjectRoles(IBoldObjectContents ObjectContents)
        {
            for (int j = 0; j < ObjectContents.MemberCount; j++)
            {
                IBoldValue? aMember = ObjectContents.ValueByIndex(j);
                if (aMember is not null)
                {
                    // FIXME: Doesn't work for non-embedded singlelinks, but that requires access to the model
                    if ((aMember is IBoldObjectIdRef anIdRef && anIdRef.Id is TBoldGlobalId id && !ValueSpace.HasContentsForId(id)) ||
                        (aMember is IBoldObjectIdRefPair anIdRefPair && anIdRefPair.Id1 is TBoldGlobalId id1 && !ValueSpace.HasContentsForId(id1)))
                    {
                        if (BrokenLinkResolver is not null)
                        {
                            if (!BrokenLinkResolver.ResolveBrokenLink(ObjectContents, j, HoldList.IdInList(IdList[i])))
                            {
                                throw new EBoldFeatureNotImplementedYet(SysUtils.Format(OlleConsts.sCannotFailIndividualObjects, GetType().Name));
                            }
                        }
                        else
                        {
                            throw new EBold(OlleConsts.sUnresolvedLink);
                        }
                    }
                }
            }
        }

        for (i = 0; i < IdList.Count; i++)
        {
            IBoldObjectContents? anObject = ValueSpace.ObjectContentsByObjectId(IdList[i]);
            if (anObject is not null)
            {
                VerifyObjectRoles(anObject);
            }
        }
    }

    /// <summary>Starts a transaction in the application's database, unless one is running.</summary>
    [Delphi(PortStatus.Ported)]
    protected void StartTransaction()
    {
        if (!PController.InTransaction())
        {
            PController.StartTransaction();
            fMyTransaction = true;
        }
    }

    /// <summary>Commits the transaction this handler started.</summary>
    [Delphi(PortStatus.Ported)]
    protected void CommitTransaction()
    {
        if (fMyTransaction)
        {
            PController.CommitTransaction();
        }

        fMyTransaction = false;
    }

    /// <summary>Rolls back the transaction this handler started.</summary>
    [Delphi(PortStatus.Ported)]
    protected void RollbackTransaction()
    {
        if (fMyTransaction)
        {
            PController.RollbackTransaction();
        }

        fMyTransaction = false;
    }

    /// <summary>The persistence controller of the application's storage.</summary>
    [Delphi(PortStatus.Ported)] public TBoldPersistenceControllerDefault PController { get; set; } = null!;

    /// <summary>The OLLE system.</summary>
    [Delphi(PortStatus.Ported)] public TBoldSystem OllSystem { get; set; } = null!;

    [Delphi(PortStatus.Ported)] public TBoldBrokenLinkResolver? BrokenLinkResolver { get; set; }
}

/// <summary>
/// Takes in objects another storage (their owner) lends, as foreign objects, and checks them in to the owner again.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldForeignObjectHandler : TBoldDistributableObjectHandler
{
    /// <summary>The objects came from the owner at the local timestamp: their information objects record it, and the hold.</summary>
    [Delphi(PortStatus.Ported)]
    private void PutObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList HoldList, int NewLocalTimeStamp, TForeignPSInfo Owner)
    {
        var InfoObjects = new TDistributableObjectInfoList();
        var anObjectId = new TBoldDefaultID();
        var MissingInfos = new TBoldObjectIdList();
        GetInfoObjectsFor(IdList, InfoObjects, MissingInfos);
        NewForeignInfoObjectsFor(MissingInfos, InfoObjects, Owner);
        for (int i = 0; i < InfoObjects.Count; i++)
        {
            TBoldObject? anObject = InfoObjects[i];
            if (anObject is not TForeignObjectInfo aForeignObjectInfo)
            {
                throw new EBold(SysUtils.Format(OlleConsts.sObjectNotForeign, GetType().Name));
            }

            if (aForeignObjectInfo.Owner != Owner)
            {
                throw new EBold(SysUtils.Format(OlleConsts.sWrongOwner, GetType().Name));
            }

            anObjectId.AsInteger = aForeignObjectInfo.LocalId;
            aForeignObjectInfo.Put(ValueSpace, HoldList.IdInList(anObjectId), NewLocalTimeStamp);
        }
    }

    /// <summary>
    /// Takes in the objects of the value space (with global ids) from the owner: they are written to this storage,
    /// read-only unless held, and recorded as foreign objects.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void Put(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList HoldList, TBoldPSId Owner)
    {
        var TranslationList = new TBoldIDTranslationList();
        var TranslationList2 = new TBoldIDTranslationList();
        TBoldObjectIdList IdList2 = IdList.Clone();
        TBoldObjectIdList HoldList2 = HoldList.Clone();
        StartTransaction();
        try
        {
            MakeLocalizingTranslationList(ValueSpace, IdList, TranslationList);
            ValueSpace.ApplytranslationList(TranslationList);
            IdList2.ApplyTranslationList(TranslationList);
            HoldList2.ApplyTranslationList(TranslationList);

            VerifyAssociations(ValueSpace, IdList2, HoldList2);
            int NewLocalTimeStamp = 0;
            Update(ValueSpace, IdList2, TranslationList2, ref NewLocalTimeStamp);

            // the value space got the new ids in Update
            IdList2.ApplyTranslationList(TranslationList2);
            HoldList2.ApplyTranslationList(TranslationList2);

            LockAndFreeObjects(IdList2, HoldList2);
            PutObjects(ValueSpace, IdList2, HoldList2, NewLocalTimeStamp, GetForeignPSInfo(Owner));
            OllSystem.UpdateDatabase();

            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Makes the objects read-only again: they are no longer held here.</summary>
    [Delphi(PortStatus.Ported)]
    private void ReleaseObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList) => PController.PMSetReadOnlyness(IdList, null!);

    /// <summary>
    /// Starts checking in the objects to their owner: they are fetched into the value space with global ids, those to
    /// release become read-only, and their information objects record the check-in until the owner acknowledges it.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void StartCheckIn(TBoldObjectIdList IdList, TBoldObjectIdList ReleaseList, IBoldValueSpace ValueSpace, TBoldPSId Owner)
    {
        var GlobalTranslationList = new TBoldIDTranslationList();
        StartTransaction();
        try
        {
            TForeignPSInfo OwnerForeignPSInfo = GetForeignPSInfo(Owner);
            if (OwnerForeignPSInfo.IsCheckingIn())
            {
                throw new EBold(SysUtils.Format(OlleConsts.sObjectsAlreadyBeingCheckedIn, GetType().Name));
            }

            Fetch(IdList, ValueSpace);
            StartCheckInObjects(ValueSpace, IdList, ReleaseList, OwnerForeignPSInfo);
            ReleaseObjects(ValueSpace, ReleaseList);

            MakeGlobalTranslationListFor(ValueSpace, IdList, GlobalTranslationList);
            ValueSpace.ApplytranslationList(GlobalTranslationList);
            IdList.ApplyTranslationList(GlobalTranslationList);
            ReleaseList.ApplyTranslationList(GlobalTranslationList);

            OllSystem.UpdateDatabase();

            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Each object starts checking in; the hold is kept for those not released.</summary>
    [Delphi(PortStatus.Ported)]
    private void StartCheckInObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList ReleaseList, TForeignPSInfo Owner)
    {
        var InfoObjectList = new TDistributableObjectInfoList();
        var NewInfoObjectList = new TBoldObjectIdList();
        var anId = new TBoldDefaultID();
        GetInfoObjectsFor(IdList, InfoObjectList, NewInfoObjectList);
        NewForeignInfoObjectsFor(NewInfoObjectList, InfoObjectList, Owner, true);
        for (int i = 0; i < InfoObjectList.Count; i++)
        {
            TDistributableObjectInfo aDistributableObjectInfo = InfoObjectList[i]!;
            anId.AsInteger = aDistributableObjectInfo.LocalId;
            aDistributableObjectInfo.StartCheckIn(ValueSpace, !ReleaseList.IdInList(anId));
        }
    }

    /// <summary>The owner took the check-in at its timestamp.</summary>
    [Delphi(PortStatus.Ported)]
    public void AcknowledgeCheckIn(TBoldPSId Owner, int NewTimeStamp)
    {
        GetForeignPSInfo(Owner).AcknowledgeCheckIn(NewTimeStamp);
        OllSystem.UpdateDatabase();
    }

    /// <summary>The owner did not take the check-in: the objects that were released are writeable again.</summary>
    [Delphi(PortStatus.Ported)]
    public void FailCheckIn(TBoldPSId Owner)
    {
        var FailedReleaseList = new TBoldObjectIdList();
        StartTransaction();
        try
        {
            GetForeignPSInfo(Owner).FailCheckIn(FailedReleaseList);
            UnReleaseObjects(FailedReleaseList);
            OllSystem.UpdateDatabase();

            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void UnReleaseObjects(TBoldObjectIdList IdList) => PController.PMSetReadOnlyness(null!, IdList);

    /// <summary>The local ids of the objects from the owner that are held here.</summary>
    [Delphi(PortStatus.Ported)]
    public void HeldObjectsFrom(TBoldPSId Owner, TBoldObjectIdList Objects)
    {
        Objects.Clear();
        TForeignObjectInfoList anObjectList = GetForeignPSInfo(Owner).OwnedObjectInfos;
        anObjectList.EnsureObjects();
        for (int i = 0; i < anObjectList.Count; i++)
        {
            if (anObjectList[i]!.HeldObjectInfo is not null)
            {
                BoldOLLEDistributableObjectHandlers.AddObjectToIdList(anObjectList[i]!, Objects);
            }
        }
    }

    /// <summary>The local ids of the objects held from the owner that changed here since they came.</summary>
    [Delphi(PortStatus.Ported)]
    public void ModifiedObjectsFrom(TBoldPSId Owner, TBoldObjectIdList Objects)
    {
        using var anElement = new TBoldIndirectElement();
        var anObjectIdList = new TBoldObjectIdList();
        using var aValueSpace = new TBoldFreeStandingValueSpace();
        GetForeignPSInfo(Owner).EvaluateExpression("ownedObjectInfos.heldObjectInfo", anElement); // do not localize
        var anObjectList = (TBoldObjectList)anElement.Value!;
        anObjectList.EnsureObjects();
        for (int i = 0; i < anObjectList.Count; i++)
        {
            BoldOLLEDistributableObjectHandlers.AddObjectToIdList(((THeldObjectInfo)anObjectList[i]!).ForeignObjectInfo!, anObjectIdList);
        }

        Fetch(anObjectIdList, aValueSpace);
        for (int i = 0; i < anObjectList.Count; i++)
        {
            IBoldObjectContents anObjectContents = ((IBoldValueSpace)aValueSpace).ObjectContentsByObjectId(anObjectIdList[i])!;
            if (anObjectContents.TimeStamp != ((THeldObjectInfo)anObjectList[i]!).OriginalLocalTimeStamp)
            {
                Objects.Add(anObjectIdList[i]);
            }
        }
    }

    /// <summary>The local ids of the objects from the owner.</summary>
    [Delphi(PortStatus.Ported)]
    public void ObjectsFrom(TBoldPSId Owner, TBoldObjectIdList Objects)
    {
        Objects.Clear();
        TForeignObjectInfoList anObjectList = GetForeignPSInfo(Owner).OwnedObjectInfos;
        anObjectList.EnsureObjects();
        for (int i = 0; i < anObjectList.Count; i++)
        {
            BoldOLLEDistributableObjectHandlers.AddObjectToIdList(anObjectList[i]!, Objects);
        }
    }
}

/// <summary>
/// Lends this storage's own objects to other storages (holders), takes them back when checked in, and gives the
/// objects that changed since the last synchronization with a storage.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOwnObjectHandler : TBoldDistributableObjectHandler
{
    /// <summary>The synchronization with the storage is done: it has the objects up to the timestamp given.</summary>
    [Delphi(PortStatus.Ported)]
    public void AcknowledgeSynch(TBoldPSId ForeignPS)
    {
        TForeignPSInfo aForeignPS = GetForeignPSInfo(ForeignPS);
        aForeignPS.LastSynchTimestamp = aForeignPS.OngoingSynchTimestamp;
        aForeignPS.OngoingSynchTimestamp = -1;
        OllSystem.UpdateDatabase();
    }

    /// <summary>
    /// Takes the objects the holder checks in (with global ids): they are written to this storage, which gives the new
    /// timestamp, and are no longer checked out when released (read-only otherwise).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void CheckIn(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList ReleaseList, TBoldPSId Holder, ref int NewTimeStamp)
    {
        var TranslationList = new TBoldIDTranslationList();
        var TranslationList2 = new TBoldIDTranslationList();
        TBoldObjectIdList IdList2 = IdList.Clone();
        TBoldObjectIdList ReleaseList2 = ReleaseList.Clone();
        StartTransaction();
        try
        {
            MakeLocalizingTranslationList(ValueSpace, IdList, TranslationList);
            ValueSpace.ApplytranslationList(TranslationList);
            IdList2.ApplyTranslationList(TranslationList);
            ReleaseList2.ApplyTranslationList(TranslationList);

            VerifyAssociations(ValueSpace, IdList2, IdList2);
            Update(ValueSpace, IdList2, TranslationList2, ref NewTimeStamp);

            // the value space got the new ids in Update
            IdList2.ApplyTranslationList(TranslationList2);
            ReleaseList2.ApplyTranslationList(TranslationList2);

            LockAndFreeObjects(IdList2, ReleaseList2);
            CheckInObjects(ValueSpace, IdList2, ReleaseList2, GetForeignPSInfo(Holder));
            OllSystem.UpdateDatabase();

            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Each object is checked in by the holder; an object new to this storage becomes an own object checked out to it first.</summary>
    [Delphi(PortStatus.Ported)]
    private void CheckInObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList ReleaseList, TForeignPSInfo Holder)
    {
        var InfoObjects = new TDistributableObjectInfoList();
        var NewInfoIds = new TBoldObjectIdList();
        var anObjectId = new TBoldDefaultID();
        GetInfoObjectsFor(IdList, InfoObjects, NewInfoIds);
        for (int i = 0; i < InfoObjects.Count; i++)
        {
            TBoldObject? anObject = InfoObjects[i];
            if (anObject is not TOwnObjectInfo anOwnObjectInfo)
            {
                throw new EBold(SysUtils.Format(OlleConsts.sObjectNotOwned, GetType().Name, "CheckInObjects")); // do not localize
            }

            anObjectId.AsInteger = anOwnObjectInfo.LocalId;
            anOwnObjectInfo.CheckIn(ValueSpace, ReleaseList.IdInList(anObjectId), Holder);
        }

        for (int i = 0; i < NewInfoIds.Count; i++)
        {
            var anOwnObjectInfo = new TOwnObjectInfo(OllSystem);
            anOwnObjectInfo.LocalId = ((TBoldDefaultID)NewInfoIds[i]).AsInteger;
            AddToMapping(anOwnObjectInfo);
            anOwnObjectInfo.CheckedOutObjectInfo = new TCheckedOutObjectInfo(OllSystem);
            anOwnObjectInfo.CheckedOutObjectInfo.Holder = Holder; // Needed for those objects that aren't released
            anOwnObjectInfo.CheckIn(ValueSpace, ReleaseList.IdInList(NewInfoIds[i]), Holder);
        }
    }

    /// <summary>The synchronization with the storage failed: there is none going on.</summary>
    [Delphi(PortStatus.Ported)]
    public void FailSynch(TBoldPSId ForeignPS)
    {
        GetForeignPSInfo(ForeignPS).OngoingSynchTimestamp = -1;
        OllSystem.UpdateDatabase();
    }

    /// <summary>Gives the objects (with global ids) to the holder; those to hold are checked out to it and read-only here.</summary>
    [Delphi(PortStatus.Ported)]
    public void Get(TBoldObjectIdList IdList, TBoldObjectIdList HoldList, TBoldPSId Holder, IBoldValueSpace ValueSpace)
    {
        StartTransaction();
        try
        {
            InternalGet(IdList, HoldList, Holder, ValueSpace);

            OllSystem.UpdateDatabase();
            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Each object is given, and checked out to the holder when it is to be held.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList IdList, TBoldObjectIdList HoldList, TForeignPSInfo Holder)
    {
        var InfoObjectList = new TDistributableObjectInfoList();
        var NewInfoObjectList = new TBoldObjectIdList();
        var anId = new TBoldDefaultID();
        GetInfoObjectsFor(IdList, InfoObjectList, NewInfoObjectList);
        NewOwnInfoObjectsFor(NewInfoObjectList, InfoObjectList);
        for (int i = 0; i < InfoObjectList.Count; i++)
        {
            TDistributableObjectInfo aDistributableObjectInfo = InfoObjectList[i]!;
            anId.AsInteger = aDistributableObjectInfo.LocalId;
            aDistributableObjectInfo.Get(ValueSpace, HoldList.IdInList(anId), Holder);
        }
    }

    /// <summary>
    /// Gives the storage the own objects that changed since its last synchronization (with global ids), and starts a
    /// synchronization up to the newest timestamp among them; it is acknowledged or failed later.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void GetSynch(TBoldPSId ForeignPS, TBoldObjectIdList IdList, IBoldValueSpace Valuespace)
    {
        StartTransaction();
        try
        {
            TForeignPSInfo aForeignPS = GetForeignPSInfo(ForeignPS);
            if (aForeignPS.IsSynching())
            {
                throw new EBold(SysUtils.Format(OlleConsts.sSynchInProgress, GetType().Name));
            }

            var ChangedObjects = new TBoldObjectIdList();
            var HoldList = new TBoldObjectIdList();
            var aCond = new TBoldTimestampCondition();
            var InfoObjects = new TDistributableObjectInfoList();
            var MissingIds = new TBoldObjectIdList();
            aCond.Timestamp = aForeignPS.LastSynchTimestamp;
            PController.PMFetchIDListWithCondition(ChangedObjects, Valuespace, BoldDefs.fmDistributable, aCond, -1);
            GetInfoObjectsFor(ChangedObjects, InfoObjects, MissingIds);
            NewOwnInfoObjectsFor(MissingIds, InfoObjects);
            for (int i = InfoObjects.Count - 1; i >= 0; i--)
            {
                if (InfoObjects[i] is not TOwnObjectInfo)
                {
                    InfoObjects.RemoveByIndex(i);
                }
            }

            GetLocalIdsFor(InfoObjects, IdList);
            InternalGet(IdList, HoldList, ForeignPS, Valuespace);
            int MaxTimestamp = 0;
            for (int i = 0; i < IdList.Count; i++)
            {
                int aTimestamp = Valuespace.ObjectContentsByObjectId(IdList[i])!.TimeStamp;
                if (aTimestamp > MaxTimestamp)
                {
                    MaxTimestamp = aTimestamp;
                }
            }

            aForeignPS.OngoingSynchTimestamp = MaxTimestamp;

            OllSystem.UpdateDatabase();
            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Fetches the objects, gives them to the holder, reserves those to hold, and turns their ids global.</summary>
    [Delphi(PortStatus.Ported)]
    private void InternalGet(TBoldObjectIdList IdList, TBoldObjectIdList HoldList, TBoldPSId Holder, IBoldValueSpace ValueSpace)
    {
        var GlobalTranslationList = new TBoldIDTranslationList();
        Fetch(IdList, ValueSpace);
        GetObjects(ValueSpace, IdList, HoldList, GetForeignPSInfo(Holder));
        ReserveObjects(ValueSpace, HoldList);

        MakeGlobalTranslationListFor(ValueSpace, IdList, GlobalTranslationList);
        ValueSpace.ApplytranslationList(GlobalTranslationList);
        IdList.ApplyTranslationList(GlobalTranslationList);
        HoldList.ApplyTranslationList(GlobalTranslationList);
    }

    /// <summary>The objects held elsewhere are read-only here.</summary>
    [Delphi(PortStatus.Ported)]
    private void ReserveObjects(IBoldValueSpace ValueSpace, TBoldObjectIdList HoldList) => PController.PMSetReadOnlyness(HoldList, null!);

    /// <summary>The holder gives the objects (global ids) back unchanged: they are writeable here again.</summary>
    [Delphi(PortStatus.Ported)]
    public void UnCheckOut(TBoldObjectIdList IdList, TBoldPSId Holder)
    {
        var TranslationList = new TBoldIDTranslationList();
        var EmptyIdList = new TBoldObjectIdList();
        StartTransaction();
        try
        {
            PController.PMTranslateToLocalIds(IdList, TranslationList);
            IdList.ApplyTranslationList(TranslationList);
            UnCheckOutObjects(IdList, GetForeignPSInfo(Holder));
            LockAndFreeObjects(EmptyIdList, IdList);
            OllSystem.UpdateDatabase();

            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }

    /// <summary>Each own object is no longer checked out to the holder.</summary>
    [Delphi(PortStatus.Ported)]
    private void UnCheckOutObjects(TBoldObjectIdList IdList, TForeignPSInfo Holder)
    {
        var InfoObjects = new TDistributableObjectInfoList();
        var MissingList = new TBoldObjectIdList();
        GetInfoObjectsFor(IdList, InfoObjects, MissingList);
        for (int i = 0; i < InfoObjects.Count; i++)
        {
            if (InfoObjects[i] is not TOwnObjectInfo anOwnObjectInfo)
            {
                throw new EBold(SysUtils.Format(OlleConsts.sObjectNotOwned, GetType().Name, "UnCheckOutObjects")); // do not localize
            }

            anOwnObjectInfo.UnCheckOut(Holder);
        }
    }
}
