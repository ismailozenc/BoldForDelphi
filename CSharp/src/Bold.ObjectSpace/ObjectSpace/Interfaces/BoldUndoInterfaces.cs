// Delphi: Source/ObjectSpace/Interfaces/BoldUndoInterfaces.pas
// Port map: CSharp/port/units/BoldUndoInterfaces.md

namespace Bold;

/// <summary>Whether the undo handler is recording, undoing or redoing.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldUndoState { busNormal, busUndoing, busRedoing }

/// <summary>A named block of changes: the values before them.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldUndoBlock
{
    [Delphi(PortStatus.Ported)] string GetName();
    [Delphi(PortStatus.Ported)] IBoldValueSpace GetValueSpace();
    [Delphi(PortStatus.Ported)] bool GetContainsChanges();
    [Delphi(PortStatus.Ported)] string Name => GetName();
    [Delphi(PortStatus.Ported)] IBoldValueSpace ValueSpace => GetValueSpace();
    [Delphi(PortStatus.Ported)] bool ContainsChanges => GetContainsChanges();
}

/// <summary>An ordered list of undo (or redo) blocks; the top block is the latest.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldUndoList
{
    [Delphi(PortStatus.Ported)] int GetCount();
    [Delphi(PortStatus.Ported)] IBoldUndoBlock? GetItems(int Index);
    [Delphi(PortStatus.Ported)] IBoldUndoBlock? GetItemsByName(string Name);
    [Delphi(PortStatus.Ported)] IBoldUndoBlock? GetTopBlock();
    [Delphi(PortStatus.Ported)] int IndexOf(string BlockName);
    [Delphi(PortStatus.Ported)] bool CanMoveBlock(int CurIndex, int NewIndex);
    [Delphi(PortStatus.Ported)] void MergeBlocks(string DestinationBlockName, string SourceBlockName);
    [Delphi(PortStatus.Ported)] void MoveToTop(string BlockName);
    [Delphi(PortStatus.Ported)] void MoveBlock(int CurIndex, int NewIndex);
    [Delphi(PortStatus.Ported)] void RenameBlock(string OldName, string NewName);
    [Delphi(PortStatus.Ported)] bool RemoveBlock(string BlockName);
    [Delphi(PortStatus.Ported)] int Count => GetCount();
    [Delphi(PortStatus.Ported, Symbol = "IBoldUndoList.Items")] IBoldUndoBlock? this[int Index] => GetItems(Index);
    [Delphi(PortStatus.Ported)] IBoldUndoBlock? ItemsByName(string Name) => GetItemsByName(Name);
    [Delphi(PortStatus.Ported)] IBoldUndoBlock? TopBlock => GetTopBlock();
}

/// <summary>Undo and redo of the changes to an object space, in named blocks between check points.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldUndoHandler
{
    [Delphi(PortStatus.Ported)] IBoldUndoList GetUndoList();
    [Delphi(PortStatus.Ported)] IBoldUndoList GetRedoList();
    [Delphi(PortStatus.Ported)] string GetUniqueBlockName(string SuggestedName);
    [Delphi(PortStatus.Ported)] bool CanUndoBlock(string BlockName);
    [Delphi(PortStatus.Ported)] bool CanRedoBlock(string BlockName);
    [Delphi(PortStatus.Ported)] bool GetEnabled();
    [Delphi(PortStatus.Ported)] void SetEnabled(bool value);
    [Delphi(PortStatus.Ported)] void UndoLatest();
    [Delphi(PortStatus.Ported)] void RedoLatest();
    [Delphi(PortStatus.Ported)] void UndoBlock(string BlockName);
    [Delphi(PortStatus.Ported)] void RedoBlock(string BlockName);
    [Delphi(PortStatus.Ported)] void SetNamedCheckPoint(string CheckPointName);
    [Delphi(PortStatus.Ported)] void SetCheckPoint();
    [Delphi(PortStatus.Ported)] void ClearAllUndoBlocks();
    [Delphi(PortStatus.Ported)] IBoldUndoList UndoList => GetUndoList();
    [Delphi(PortStatus.Ported)] IBoldUndoList RedoList => GetRedoList();
    [Delphi(PortStatus.Ported)] bool Enabled { get => GetEnabled(); set => SetEnabled(value); }
}
