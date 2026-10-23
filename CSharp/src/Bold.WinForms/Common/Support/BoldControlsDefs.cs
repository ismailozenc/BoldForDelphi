// Delphi: Source/Common/Support/BoldControlsDefs.pas
// Port map: CSharp/port/units/BoldControlsDefs.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldControlsDefs", Symbol = "TBoldColumnValues", Note = "Global using alias of DelphiSet<TBoldColumnValue>, in BoldControlsDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldControlsDefs", Symbol = "TBoldCWAdjustSet", Note = "Global using alias of DelphiSet<TBoldCWAdjust>, in BoldControlsDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldControlsDefs", Symbol = "BoldPropertiesController_SupportedPropertyTypes", Note = "The Delphi type kinds a properties controller sets; the port's controller sets properties of any type through reflection.")]

namespace Bold;

/// <summary>A value of a grid column that was set (and so is not taken from the grid).</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldColumnValue { cvColor, cvWidth, cvFont, cvAlignment, cvReadOnly, cvTitleColor, cvTitleCaption, cvTitleAlignment, cvTitleFont, cvImeMode, cvImeName }

/// <summary>How a column's width adjusts to its contents: it may grow, shrink, and count the title.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldCWAdjust { caAllowGrow, caAllowShrink, caIncludeTitle }

/// <summary>Draws a grid cell.</summary>
[Delphi(PortStatus.Ported, Note = "The state is a DataGridView cell's state, where Delphi's is a TGridDrawState.")]
public delegate void TBoldDrawCellEvent(object? Sender, TCanvas Canvas, int ACol, int ARow, Rectangle Rect, DataGridViewElementStates State);

/// <summary>How an image fits its control.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldStretchMode { bsmNoStretch, bsmStretchProportional, bsmStretchToFit, bsmStretchToScale }

/// <summary>When a tree's nodes get their children: all at once, when visible, or when expanded.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldNodeExpansionMethod { neAll, neVisible, neDemand }

/// <summary>Which way the mouse moved.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldMouseDirection { DirMouseUp, DirMouseDown }

/// <summary>What choosing in a combo box does: set the text, the value, nothing, the reference, or the list's current index.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldComboSelectChangeAction { bdscSetText, bdcsSetValue, bdcsNone, bdcsSetReference, bdcsSetListIndex }

/// <summary>The button of an edit box: none, a combo arrow, an ellipsis, or the application's own.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldEditButtonStyle { bbsNone, bbsCombo, bbsEllipsis, bbsCustom }
