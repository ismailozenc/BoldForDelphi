// Delphi: Source/ObjectSpace/Undo/BoldUndoHandler.pas
// Port map: CSharp/port/units/BoldUndoHandler.md

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>A named undo block: the values objects and members had before the changes recorded in it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUndoBlock : TBoldNonRefCountedObject, IBoldUndoBlock, IDisposable
{
    internal string FName;
    private TBoldFreeStandingValueSpace? FValueSpace;
    private bool FContainsChanges;

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoBlock.CreateNamedBlock")]
    public TBoldUndoBlock(string BlockName, TBoldFreeStandingValueSpace? FSVAlueSpace = null)
    {
        FContainsChanges = false;
        FName = BlockName;
        FValueSpace = FSVAlueSpace;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoBlock.Destroy")]
    public void Dispose()
    {
        FValueSpace?.Dispose();
        FValueSpace = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>True when this block has a value for a member that the other block also has (so they cannot be reordered).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool IsDependantOn(TBoldUndoBlock Block)
    {
        var ObjectIds = new TBoldObjectIdList();
        FSValueSpace.AllObjectIds(ObjectIds, true);
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldFreeStandingObjectContents ObjectContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectIds[i])!;
            for (int j = 0; j < ObjectContents.MemberCount; j++)
            {
                if (ObjectContents.ValueByIndex(j) is not null && Block.ValueExists(ObjectIds[i], j, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Adds the other block's values (replacing this block's own only when overwriting).</summary>
    [Delphi(PortStatus.Ported)]
    public void Merge(TBoldUndoBlock Block, bool Overwrite)
    {
        var ObjectIds = new TBoldObjectIdList();
        Block.FSValueSpace.AllObjectIds(ObjectIds, true);
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldObjectId ObjectId = ObjectIds[i];
            TBoldFreeStandingObjectContents ObjectContents = Block.FSValueSpace.GetFSObjectContentsByObjectId(ObjectId)!;
            if (!FSValueSpace.GetHasContentsForId(ObjectId))
            {
                FSValueSpace.EnsureObjectContents(ObjectId);
            }

            TBoldFreeStandingObjectContents OwnContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectId)!;
            for (int j = 0; j < ObjectContents.MemberCount; j++)
            {
                IBoldValue? value = ObjectContents.ValueByIndex(j);
                if (value is null)
                {
                    continue;
                }

                if (OwnContents.MemberCount > j && OwnContents.ValueByIndex(j) is { } own)
                {
                    if (Overwrite)
                    {
                        own.AssignContent(value);
                    }
                }
                else
                {
                    OwnContents.EnsureMember(new TBoldMemberID(j), value.ContentName);
                    OwnContents.ValueByIndex(j)!.AssignContent(value);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool ValueExists(TBoldObjectId ObjectID, int MemberIndex, out IBoldValue? Value)
    {
        Value = null;
        TBoldFreeStandingObjectContents? ObjectContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectID);
        if (ObjectContents is not null && MemberIndex < ObjectContents.MemberCount)
        {
            Value = ObjectContents.ValueByIndex(MemberIndex);
        }

        return Value is not null;
    }

    [Delphi(PortStatus.Ported)]
    public bool ValueExists(TBoldObjectId ObjectID, int MemberIndex)
    {
        TBoldFreeStandingObjectContents? OC = FSValueSpace.GetFSObjectContentsByObjectId(ObjectID);
        return OC is not null && MemberIndex < OC.MemberCount && OC.ValueByIndex(MemberIndex) is not null;
    }

    /// <summary>Records the member's value, unless the block already has one for it; true when recorded.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool HandleMember(IBoldObjectContents ObjectContents, int MemberIndex, IBoldValue MemberValue)
    {
        TBoldObjectId ObjectId = ObjectContents.ObjectId;
        bool result = !ValueExists(ObjectId, MemberIndex);
        if (result)
        {
            FContainsChanges = true;
            TBoldFreeStandingObjectContents? FSObjectContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectId);
            if (FSObjectContents is null)
            {
                FSValueSpace.EnsureObjectContents(ObjectId);
                FSObjectContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectId)!;
                FSObjectContents.ApplyObjectContents(ObjectContents, false, false);
            }

            FSObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), MemberValue.ContentName);
            FSObjectContents.ValueByIndex(MemberIndex)!.AssignContent(MemberValue);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal bool HasObjectContentsForAnyObjectInList(TBoldObjectList ObjectList)
    {
        for (int i = 0; i < ObjectList.Count; i++)
        {
            if (FSValueSpace.GetFSObjectContentsByObjectId(ObjectList[i]!.BoldObjectLocator!.BoldObjectID) is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Records the object's existence and persistence state, unless already recorded (as existing and current when asked).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void HandleObject(IBoldObjectContents Obj, bool RegardAsExisting)
    {
        TBoldObjectId ObjectId = Obj.ObjectId;
        if (FSValueSpace.GetFSObjectContentsByObjectId(ObjectId) is null)
        {
            FContainsChanges = true;
            FSValueSpace.EnsureObjectContents(ObjectId);
            TBoldFreeStandingObjectContents ObjectContents = FSValueSpace.GetFSObjectContentsByObjectId(ObjectId)!;
            ObjectContents.ApplyObjectContents(Obj, false, false);
            if (RegardAsExisting)
            {
                ObjectContents.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
                ObjectContents.BoldExistenceState = TBoldExistenceState.besExisting;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFreeStandingValueSpace GetFSValueSpace() => FValueSpace ??= new TBoldFreeStandingValueSpace();

    [Delphi(PortStatus.Ported)]
    private void SetFSValueSpace(TBoldFreeStandingValueSpace? Value)
    {
        if (FValueSpace != Value)
        {
            FValueSpace?.Dispose();
            FValueSpace = Value;
            FContainsChanges = true;
        }
    }

    [Delphi(PortStatus.Ported)] public string GetName() => FName;
    [Delphi(PortStatus.Ported)] public IBoldValueSpace GetValueSpace() => FSValueSpace;
    [Delphi(PortStatus.Ported)] public bool GetContainsChanges() => FContainsChanges;

    [Delphi(PortStatus.Ported)]
    public void ApplytranslationList(TBoldIDTranslationList IdTranslationList) => ValueSpace.ApplytranslationList(IdTranslationList);

    [Delphi(PortStatus.Ported)] public string BlockName => FName;

    [Delphi(PortStatus.Ported)]
    public TBoldFreeStandingValueSpace FSValueSpace
    {
        get => GetFSValueSpace();
        set => SetFSValueSpace(value);
    }

    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpace => GetValueSpace();
    [Delphi(PortStatus.Ported)] public bool ContainsChanges => GetContainsChanges();
}

/// <summary>The undo (or redo) blocks, oldest first; the last is the current block.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUndoBlockList : TBoldNonRefCountedObject, IBoldUndoList, IDisposable
{
    private readonly TStringList FList = new();

    [Delphi(PortStatus.Ported)]
    public TBoldUndoBlockList()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoBlockList.Destroy")]
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUndoBlock? GetBlocksByIndex(int Index) => Index >= 0 && Index < FList.Count ? (TBoldUndoBlock?)FList.Objects(Index) : null;

    [Delphi(PortStatus.Ported)]
    private TBoldUndoBlock? GetBlocksByName(string BlockName)
    {
        int Idx = FList.IndexOf(BlockName);
        return Idx != -1 ? (TBoldUndoBlock?)FList.Objects(Idx) : null;
    }

    [Delphi(PortStatus.Ported)] public int GetCount() => FList.Count;

    /// <summary>The newest block, created ("UnNamed") when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldUndoBlock GetCurrentBlock() => Count == 0 ? AddBlock("UnNamed") : (TBoldUndoBlock)FList.Objects(Count - 1)!;

    [Delphi(PortStatus.Ported)]
    private TBoldUndoBlock GetAssertedBlocksByName(string BlockName) =>
        BlocksByName(BlockName) ?? throw new EBold(SysUtils.Format(sNoSuchBlock, BlockName));

    [Delphi(PortStatus.Ported)]
    private TBoldUndoBlock GetAssertedBlocksByIndex(int Index) =>
        BlocksbyIndex(Index) ?? throw new EBold(SysUtils.Format(sNoSuchBlockIndex, Index));

    [Delphi(PortStatus.Ported)] public IBoldUndoBlock? GetItems(int Index) => BlocksbyIndex(Index);
    [Delphi(PortStatus.Ported)] public IBoldUndoBlock? GetItemsByName(string Name) => BlocksByName(Name);
    [Delphi(PortStatus.Ported)] public IBoldUndoBlock? GetTopBlock() => CurrentBlock;

    [Delphi(PortStatus.Ported)]
    protected internal void Clear()
    {
        for (int i = 0; i < FList.Count; i++)
        {
            ((TBoldUndoBlock?)FList.Objects(i))?.Dispose();
        }

        FList.Clear();
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldUndoBlock AddBlock(string BlockName, TBoldFreeStandingValueSpace? FSVAlueSpace = null)
    {
        if (FList.IndexOfName(BlockName) != -1)
        {
            throw new EBold(SysUtils.Format(sBlockNameInUse, GetType().Name, BlockName));
        }

        int Idx = FList.Add(BlockName);
        var result = new TBoldUndoBlock(BlockName, FSVAlueSpace);
        FList.SetObjects(Idx, result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal int AssertedIndexOf(string BlockName)
    {
        int result = IndexOf(BlockName);
        if (result == -1)
        {
            throw new EBold(SysUtils.Format(sNoSuchBlock, BlockName));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void InternalRemoveBlock(string BlockName)
    {
        int idx = AssertedIndexOf(BlockName);
        ((TBoldUndoBlock?)FList.Objects(idx))?.Dispose();
        FList.Delete(idx);
    }

    [Delphi(PortStatus.Ported)]
    public void ApplytranslationList(TBoldIDTranslationList IdTranslationList)
    {
        for (int i = 0; i < Count; i++)
        {
            BlocksbyIndex(i)!.ApplytranslationList(IdTranslationList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void MoveBlock(int CurIndex, int NewIndex)
    {
        if (!CanMoveBlock(CurIndex, NewIndex))
        {
            throw new EBold(sCannotMoveBlock);
        }

        FList.Move(CurIndex, NewIndex);
    }

    [Delphi(PortStatus.Ported)]
    public int IndexOf(string BlockName) => FList.IndexOf(BlockName);

    /// <summary>Removes the block when it can be moved to the top; false when it cannot.</summary>
    [Delphi(PortStatus.Ported)]
    public bool RemoveBlock(string BlockName)
    {
        bool result = CanMoveToTop(AssertedIndexOf(BlockName));
        if (result)
        {
            InternalRemoveBlock(BlockName);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void RenameBlock(string OldName, string NewName)
    {
        if (BlocksByName(NewName) is not null)
        {
            throw new EBold(sCannotRenameBlock);
        }

        TBoldUndoBlock aBlock = AssertedBlocksByName(OldName);
        FList[IndexOf(OldName)] = NewName;
        aBlock.FName = NewName;
    }

    [Delphi(PortStatus.Ported)]
    public void MoveToTop(string BlockName) => MoveBlock(IndexOf(BlockName), FList.Count - 1);

    /// <summary>Merges the source block into the destination block (the later one's values win) and removes the source.</summary>
    [Delphi(PortStatus.Ported)]
    public void MergeBlocks(string DestinationBlockName, string SourceBlockName)
    {
        TBoldUndoBlock DestinationBlock = AssertedBlocksByName(DestinationBlockName);
        TBoldUndoBlock SourceBlock = AssertedBlocksByName(SourceBlockName);
        if (!CanMergeBlock(IndexOf(SourceBlockName), IndexOf(DestinationBlockName)))
        {
            throw new EBold(sCannotMergeBlocks);
        }

        DestinationBlock.Merge(SourceBlock, IndexOf(DestinationBlockName) > IndexOf(SourceBlockName));
        InternalRemoveBlock(SourceBlockName);
    }

    /// <summary>True when the block depends on none of the blocks it would pass.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CanMoveBlock(int CurIndex, int NewIndex)
    {
        TBoldUndoBlock CurBlock = AssertedBlocksByIndex(CurIndex);
        TBoldUndoBlock NewBlock = AssertedBlocksByIndex(NewIndex);
        bool result = CurIndex == NewIndex || !CurBlock.IsDependantOn(NewBlock);
        if (result)
        {
            if (CurIndex < NewIndex)
            {
                for (int i = CurIndex + 1; result && i <= NewIndex; i++)
                {
                    result = !CurBlock.IsDependantOn(BlocksbyIndex(i)!);
                }
            }
            else
            {
                for (int i = CurIndex - 1; result && i >= NewIndex; i--)
                {
                    result = !CurBlock.IsDependantOn(BlocksbyIndex(i)!);
                }
            }
        }

        return result;
    }

    /// <summary>True when no block between the two depends on them (Delphi checks the current block both ways).</summary>
    [Delphi(PortStatus.Ported)]
    public bool CanMergeBlock(int CurIndex, int NewIndex)
    {
        bool result = true;
        TBoldUndoBlock CurBlock = AssertedBlocksByIndex(CurIndex);
        TBoldUndoBlock NewBlock = AssertedBlocksByIndex(CurIndex);
        if (CurIndex < NewIndex)
        {
            for (int i = CurIndex + 1; result && i <= NewIndex - 1; i++)
            {
                result = !CurBlock.IsDependantOn(BlocksbyIndex(i)!);
            }
        }
        else if (CurIndex > NewIndex)
        {
            for (int i = CurIndex - 1; result && i >= NewIndex + 1; i--)
            {
                result = !NewBlock.IsDependantOn(BlocksbyIndex(i)!);
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool CanMoveToTop(int CurIndex) => CanMoveBlock(CurIndex, FList.Count - 1);

    /// <summary>The earlier blocks that have values for members this block has (Delphi reads the first object's contents for every object).</summary>
    [Delphi(PortStatus.Ported)]
    public void GetDependantBlocks(string BlockName, TList DependantBlocks)
    {
        TBoldUndoBlock aBlock = GetAssertedBlocksByName(BlockName);
        if (DependantBlocks is null)
        {
            throw new EBold(SysUtils.Format(sParameterNotDefined, GetType().Name));
        }

        var ObjectIds = new TBoldObjectIdList();
        aBlock.FSValueSpace.AllObjectIds(ObjectIds, true);
        DependantBlocks.Clear();
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldFreeStandingObjectContents ObjectContents = aBlock.FSValueSpace.GetFSObjectContentsByObjectId(ObjectIds[0])!;
            for (int j = 0; j < ObjectContents.MemberCount; j++)
            {
                for (int b = AssertedIndexOf(aBlock.BlockName) - 1; b >= 0; b--)
                {
                    TBoldUndoBlock CurBlock = BlocksbyIndex(b)!;
                    if (ObjectContents.ValueByIndex(j) is not null && CurBlock.ValueExists(ObjectIds[i], j, out _) && DependantBlocks.IndexOf(CurBlock) == -1)
                    {
                        DependantBlocks.Add(CurBlock);
                    }
                }
            }
        }
    }

    /// <summary>Merges all blocks into the oldest.</summary>
    [Delphi(PortStatus.Ported)]
    public void MergeAll()
    {
        for (int i = Count - 1; i > 0; i--)
        {
            TBoldUndoBlock aBlock = BlocksbyIndex(i)!;
            BlocksbyIndex(i - 1)!.Merge(aBlock, true);
            InternalRemoveBlock(aBlock.BlockName);
        }
    }

    [Delphi(PortStatus.Ported)] protected internal TBoldUndoBlock AssertedBlocksByName(string BlockName) => GetAssertedBlocksByName(BlockName);
    [Delphi(PortStatus.Ported)] protected internal TBoldUndoBlock AssertedBlocksByIndex(int Index) => GetAssertedBlocksByIndex(Index);
    [Delphi(PortStatus.Ported)] public TBoldUndoBlock? BlocksbyIndex(int Index) => GetBlocksByIndex(Index);
    [Delphi(PortStatus.Ported)] public TBoldUndoBlock? BlocksByName(string BlockName) => GetBlocksByName(BlockName);
    [Delphi(PortStatus.Ported)] public int Count => GetCount();
    [Delphi(PortStatus.Ported)] public TBoldUndoBlock CurrentBlock => GetCurrentBlock();
}

/// <summary>
/// Undo and redo for a system: records, per block, the values members and objects had before they changed (through
/// HandleMember and HandleObject); undoing a block restores them and records a redo block.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUndoHandler : TBoldAbstractUndoHandler, IBoldUndoHandler
{
    private enum TObjectAction { oaDelete, oaExisingPersistent, oaNewPersistent, oaTransient, oaUse }

    private readonly TBoldUndoBlockList FUndoBlocks = new();
    private readonly TBoldUndoBlockList FRedoBlocks = new();
    private TBoldUndoState fUndoState;
    private bool fEnabled;

    [Delphi(PortStatus.Ported)]
    public TBoldUndoHandler(TBoldSystem System)
        : base(System)
    {
        fUndoState = TBoldUndoState.busNormal;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoHandler.Destroy")]
    public override void Dispose()
    {
        FUndoBlocks.Dispose();
        FRedoBlocks.Dispose();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void HandleMember(IBoldObjectContents ObjectContents, int MemberIndex, IBoldValue MemberValue)
    {
        if (fEnabled && UndoState == TBoldUndoState.busNormal)
        {
            RedoBlocks.Clear();
            UndoBlocks.CurrentBlock.HandleMember(ObjectContents, MemberIndex, MemberValue);
        }
    }

    /// <summary>Before a save: drops redo, the undo blocks touching the saved objects, and merges the rest.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PrepareUpdate(TBoldObjectList ObjectList)
    {
        RedoBlocks.Clear();
        int i = 0;
        while (i < UndoBlocks.Count)
        {
            TBoldUndoBlock aBlock = UndoBlocks.BlocksbyIndex(i)!;
            if (aBlock.HasObjectContentsForAnyObjectInList(ObjectList))
            {
                UndoBlocks.InternalRemoveBlock(aBlock.BlockName);
            }
            else
            {
                i++;
            }
        }

        UndoBlocks.MergeAll();
    }

    /// <summary>Starts a new block with the name (or renames the current block when it is empty).</summary>
    [Delphi(PortStatus.Ported)]
    public void SetNamedCheckPoint(string CheckPointName)
    {
        if (UndoBlocks.BlocksByName(CheckPointName) is not null || RedoBlocks.BlocksByName(CheckPointName) is not null)
        {
            throw new EBold(SysUtils.Format(sBlockNameInUse, GetType().Name, CheckPointName));
        }

        if (!UndoBlocks.CurrentBlock.ContainsChanges)
        {
            UndoBlocks.RenameBlock(UndoBlocks.CurrentBlock.BlockName, CheckPointName);
        }
        else
        {
            UndoBlocks.AddBlock(CheckPointName);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void UndoBlock(string BlockName)
    {
        if (System.InTransaction())
        {
            throw new EBold(SysUtils.Format(sCannotUndoInTransaction, GetType().Name));
        }

        UndoState = TBoldUndoState.busUndoing;
        try
        {
            DoUndoInTransaction(BlockName, UndoBlocks, RedoBlocks);
        }
        finally
        {
            UndoState = TBoldUndoState.busNormal;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RedoBlock(string BlockName)
    {
        if (System.InTransaction())
        {
            throw new EBold(SysUtils.Format(sCannotUndoInTransaction, GetType().Name));
        }

        UndoState = TBoldUndoState.busRedoing;
        try
        {
            DoUndoInTransaction(BlockName, RedoBlocks, UndoBlocks);
        }
        finally
        {
            UndoState = TBoldUndoState.busNormal;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoHandler.Redolatest")]
    public void RedoLatest()
    {
        if (RedoBlocks.Count > 0)
        {
            RedoBlock(RedoBlocks.CurrentBlock.BlockName);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUndoHandler.UnDoLatest")]
    public void UndoLatest()
    {
        if (UndoBlocks.Count > 0)
        {
            UndoBlock(UndoBlocks.CurrentBlock.BlockName);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void HandleObject(IBoldObjectContents Obj, bool RegardAsExisting)
    {
        if (fEnabled && UndoState == TBoldUndoState.busNormal)
        {
            RedoBlocks.Clear();
            UndoBlocks.CurrentBlock.HandleObject(Obj, RegardAsExisting);
        }
    }

    /// <summary>
    /// Restores the undo values (objects and attributes, then links, then deletions), saving the current values in the
    /// redo value space first.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void DoUndo(TBoldFreeStandingValueSpace UnDoValueSpace, TBoldFreeStandingValueSpace RedoValueSpace)
    {
        var ObjectIds = new TBoldObjectIdList();

        static void GetInnerLinkIndices(TBoldObject BoldObject, out int MemberIndex1, out int MemberIndex2)
        {
            MemberIndex1 = -1;
            MemberIndex2 = -1;
            for (int i = 0; i < BoldObject.BoldMemberCount; i++)
            {
                if (BoldObject.BoldClassTypeInfo.AllMembers[i] is TBoldRoleRTInfo { RoleType: TBoldRoleType.rtInnerLinkRole })
                {
                    if (MemberIndex1 == -1)
                    {
                        MemberIndex1 = i;
                    }
                    else if (MemberIndex2 == -1)
                    {
                        MemberIndex2 = i;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        static void UnDoInnerLinks(TBoldObject BoldObject, IBoldObjectContents ObjectContents)
        {
            GetInnerLinkIndices(BoldObject, out int MemberIndex0, out int MemberIndex1);
            var Member0 = (TBoldObjectReference)BoldObject.BoldMembers(MemberIndex0);
            var Member1 = (TBoldObjectReference)BoldObject.BoldMembers(MemberIndex1);

            if (ObjectContents.BoldExistenceState == TBoldExistenceState.besExisting)
            {
                var Value0 = (IBoldObjectIdRef?)ObjectContents.ValueByIndex(MemberIndex0);
                var Value1 = (IBoldObjectIdRef?)ObjectContents.ValueByIndex(MemberIndex1);
                if (Value0 is not null && Value1 is not null)
                {
                    // The first assignment cannot set up the other end, as the other inner link is not set yet.
                    var SavIdValue = new TBFSObjectIDRef();
                    SavIdValue.AssignContent(Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
                    Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(Value1);
                    Member0.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(Value0);
                    Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(SavIdValue);
                    Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(Value1);
                }
                else if (Value0 is not null)
                {
                    Member0.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(Value0);
                }
                else if (Value1 is not null)
                {
                    Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(Value1);
                }
            }
            else
            {
                var NilIdValue = new TBFSObjectIDRef { BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent };
                Member0.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(NilIdValue);
                Member1.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(NilIdValue);
            }
        }

        static void UnDoObjectAndAttributes(TBoldObject BoldObject, IBoldObjectContents ObjectContents)
        {
            IBoldObjectContents contents = BoldObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents);
            contents.BoldPersistenceState = ObjectContents.BoldPersistenceState;
            contents.BoldExistenceState = ObjectContents.BoldExistenceState;
            for (int i = 0; i < BoldObject.BoldMemberCount; i++)
            {
                if (ObjectContents.ValueByIndex(i) is { } aValue && BoldObject.BoldMembers(i) is TBoldAttribute attribute)
                {
                    attribute.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(aValue);
                }
            }
        }

        static void UnDoLinks(TBoldObject BoldObject, IBoldObjectContents ObjectContents)
        {
            for (int i = 0; i < BoldObject.BoldMemberCount; i++)
            {
                if (ObjectContents.ValueByIndex(i) is { } aValue && BoldObject.BoldMembers(i) is TBoldObjectReference reference &&
                    reference.BoldRoleRTInfo!.RoleType == TBoldRoleType.rtRole)
                {
                    reference.AsIBoldValue(TBoldDomainElementProxyMode.bdepUnDo).AssignContent(aValue);
                }
            }

            if (BoldObject.BoldClassTypeInfo.IsLinkClass)
            {
                UnDoInnerLinks(BoldObject, ObjectContents);
            }
        }

        void SaveOldValues()
        {
            for (int i = 0; i < ObjectIds.Count; i++)
            {
                TBoldObjectId oid = ObjectIds[i];
                RedoValueSpace.EnsureObjectContents(oid);
                TBoldObject? BoldObject = System.Locators.ObjectByID(oid);
                TBoldFreeStandingObjectContents FSObjectContents = UnDoValueSpace.GetFSObjectContentsByObjectId(oid)!;
                TBoldFreeStandingObjectContents redo = RedoValueSpace.GetFSObjectContentsByObjectId(oid)!;
                if (BoldObject is not null)
                {
                    IBoldObjectContents current = BoldObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents);
                    if (FSObjectContents.BoldExistenceState == TBoldExistenceState.besExisting)
                    {
                        redo.ApplyObjectContents(FSObjectContents, true, false);
                        redo.UpdateObjectContentsFrom(current);
                    }
                    else
                    {
                        redo.ApplyObjectContents(current, false, false);
                        for (int M = 0; M < BoldObject.BoldMemberCount; M++)
                        {
                            if (BoldObject.BoldMemberAssigned(M) && BoldObject.BoldMembers(M).StoreInUndo())
                            {
                                IBoldValue memberValue = BoldObject.BoldMembers(M).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
                                redo.EnsureMember(new TBoldMemberID(M), memberValue.ContentName);
                                redo.ValueByIndex(M)!.AssignContent(memberValue);
                            }
                        }
                    }
                }
                else
                {
                    redo.BoldExistenceState = TBoldExistenceState.besNotCreated;
                    redo.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
                }
            }
        }

        static TObjectAction GetObjectAction(TBoldFreeStandingObjectContents FSObjectContents, TBoldObject? BoldObject)
        {
            if (BoldObject is not null)
            {
                return FSObjectContents.BoldExistenceState == TBoldExistenceState.besExisting ? TObjectAction.oaUse : TObjectAction.oaDelete;
            }

            return FSObjectContents.BoldExistenceState switch
            {
                TBoldExistenceState.besExisting => FSObjectContents.BoldPersistenceState switch
                {
                    TBoldValuePersistenceState.bvpsCurrent => TObjectAction.oaExisingPersistent,
                    TBoldValuePersistenceState.bvpsModified => TObjectAction.oaNewPersistent,
                    TBoldValuePersistenceState.bvpsTransient => TObjectAction.oaTransient,
                    _ => throw new EBold(sInternalError),
                },
                _ => FSObjectContents.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid ? throw new EBold(sInternalError) : TObjectAction.oaDelete,
            };
        }

        UnDoValueSpace.AllObjectIds(ObjectIds, false);
        SaveOldValues();

        // pass one: create the objects and set the attributes
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldObjectId oid = ObjectIds[i];
            RedoValueSpace.EnsureObjectContents(oid);
            TBoldObject? BoldObject = System.Locators.ObjectByID(oid);
            TBoldFreeStandingObjectContents FSObjectContents = UnDoValueSpace.GetFSObjectContentsByObjectId(oid)!;
            TObjectAction ObjectAction = GetObjectAction(FSObjectContents, BoldObject);
            if (BoldObject is null)
            {
                string ClassName = System.BoldSystemTypeInfo.TopSortedClasses[oid.TopSortedIndex].ExpressionName;
                if (ObjectAction == TObjectAction.oaExisingPersistent)
                {
                    BoldObject = System.CreateExistingObjectByID(oid);
                }
                else
                {
                    if (ObjectAction == TObjectAction.oaNewPersistent)
                    {
                        BoldObject = System.CreateNewObjectByExpressionName(ClassName, true);
                    }
                    else if (ObjectAction == TObjectAction.oaTransient)
                    {
                        BoldObject = System.CreateNewObjectByExpressionName(ClassName, false);
                    }

                    if (ObjectAction != TObjectAction.oaDelete)
                    {
                        System.Locators.UpdateID(BoldObject!.BoldObjectLocator!, oid, true);
                    }
                }
            }

            if (ObjectAction != TObjectAction.oaDelete)
            {
                UnDoObjectAndAttributes(BoldObject!, FSObjectContents);
            }
        }

        // pass two: the links, now that all objects are in place
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldObjectId oid = ObjectIds[i];
            RedoValueSpace.EnsureObjectContents(oid);
            TBoldObject? BoldObject = System.Locators.ObjectByID(oid);
            if (BoldObject is not null)
            {
                UnDoLinks(BoldObject, UnDoValueSpace.GetFSObjectContentsByObjectId(oid)!);
            }
        }

        // pass three: delete objects
        for (int i = 0; i < ObjectIds.Count; i++)
        {
            TBoldObjectId oid = ObjectIds[i];
            TBoldObject? BoldObject = System.Locators.ObjectByID(oid);
            TBoldFreeStandingObjectContents FSObjectContents = UnDoValueSpace.GetFSObjectContentsByObjectId(oid)!;
            if (BoldObject is { BoldExistenceState: TBoldExistenceState.besExisting } && GetObjectAction(FSObjectContents, BoldObject) == TObjectAction.oaDelete)
            {
                IBoldObjectContents contents = BoldObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents);
                contents.BoldExistenceState = TBoldExistenceState.besDeleted;
                contents.BoldPersistenceState = FSObjectContents.BoldPersistenceState;
                DeleteObject(BoldObject);
            }
        }
    }

    /// <summary>Undoes (or redoes) the block in a transaction and moves it to the other list; a failure rolls back (and is not raised, as in Delphi).</summary>
    [Delphi(PortStatus.Ported)]
    private void DoUndoInTransaction(string BlockName, TBoldUndoBlockList FromList, TBoldUndoBlockList ToList)
    {
        TBoldUndoBlock aBlock = FromList.BlocksByName(BlockName) ?? throw new EBold(SysUtils.Format(sInvalidBlockName, BlockName));
        if (!FromList.CanMoveToTop(FromList.IndexOf(BlockName)))
        {
            throw new EBold(SysUtils.Format(sCannotMoveToTop, BlockName));
        }

        TBoldFreeStandingValueSpace? RedoValueSpace = new TBoldFreeStandingValueSpace();
        System.StartTransaction();
        try
        {
            DoUndo(aBlock.FSValueSpace, RedoValueSpace);
            FromList.InternalRemoveBlock(BlockName);
            ToList.AddBlock(BlockName).FSValueSpace = RedoValueSpace;
            RedoValueSpace = null;
            System.CommitTransaction();
        }
        catch (Exception)
        {
            System.RollbackTransaction();
        }
        finally
        {
            RedoValueSpace?.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)] public bool CanRedoBlock(string BlockName) => RedoBlocks.CanMoveToTop(RedoBlocks.AssertedIndexOf(BlockName));
    [Delphi(PortStatus.Ported)] public bool CanUndoBlock(string BlockName) => UndoBlocks.CanMoveToTop(UndoBlocks.AssertedIndexOf(BlockName));
    [Delphi(PortStatus.Ported)] public IBoldUndoList GetRedoList() => FRedoBlocks;
    [Delphi(PortStatus.Ported)] public IBoldUndoList GetUndoList() => FUndoBlocks;

    /// <summary>The suggested name, or "name 0", "name 1"... when it is in use.</summary>
    [Delphi(PortStatus.Ported)]
    public string GetUniqueBlockName(string SuggestedName)
    {
        int i = 0;
        string result = SuggestedName;
        while (UndoBlocks.IndexOf(result) != -1 || RedoBlocks.IndexOf(result) != -1)
        {
            result = SysUtils.Format("%s %d", SuggestedName, i);
            i++;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void ApplytranslationList(TBoldIDTranslationList IdTranslationList)
    {
        UndoBlocks.ApplytranslationList(IdTranslationList);
        RedoBlocks.ApplytranslationList(IdTranslationList);
    }

    [Delphi(PortStatus.Ported)]
    public void SetCheckPoint() => SetNamedCheckPoint(GetUniqueBlockName(""));

    [Delphi(PortStatus.Ported)]
    public void ClearAllUndoBlocks()
    {
        FUndoBlocks.Clear();
        FRedoBlocks.Clear();
    }

    [Delphi(PortStatus.Ported)] public bool GetEnabled() => fEnabled;
    [Delphi(PortStatus.Ported)] public void SetEnabled(bool value) => fEnabled = value;

    [Delphi(PortStatus.Ported)] public TBoldUndoBlockList UndoBlocks => FUndoBlocks;
    [Delphi(PortStatus.Ported)] public TBoldUndoBlockList RedoBlocks => FRedoBlocks;

    [Delphi(PortStatus.Ported)]
    public TBoldUndoState UndoState
    {
        get => fUndoState;
        set => fUndoState = value;
    }
}
