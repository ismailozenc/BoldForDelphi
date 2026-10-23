namespace Bold;

/// <summary>
/// Delphi's TGridOption: what a grid draws (fixed and data grid lines) and lets the user do (select ranges, size and
/// move rows and columns, edit, tab between cells, select whole rows, always show the editor). TGridOptions is the set
/// (Grids.Aliases.cs).
/// </summary>
public enum TGridOption
{
    goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRangeSelect, goDrawFocusSelected, goRowSizing,
    goColSizing, goRowMoving, goColMoving, goEditing, goTabs, goRowSelect, goAlwaysShowEditor, goThumbTracking,
}

/// <summary>Delphi's TSelectCellEvent: asked before a grid's cell becomes the current one; clear CanSelect to refuse.</summary>
public delegate void TSelectCellEvent(object? Sender, int ACol, int ARow, ref bool CanSelect);
