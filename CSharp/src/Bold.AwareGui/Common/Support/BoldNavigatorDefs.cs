// Delphi: Source/Common/Support/BoldNavigatorDefs.pas
// Port map: CSharp/port/units/BoldNavigatorDefs.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldNavigatorDefs", Symbol = "TBoldButtonSet", Note = "Global using alias of DelphiSet<TBoldNavigateBtn>, in BoldNavigatorDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNavigatorDefs", Symbol = "TBoldNavButtonStyle", Note = "Global using alias of DelphiSet<TBoldNavButtonStyleElement>, in BoldNavigatorDefs.Aliases.cs; the set's anonymous enumeration is TBoldNavButtonStyleElement.")]

namespace Bold;

/// <summary>A button of a Bold navigator.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldNavigateBtn { nbFirst, nbPrior, nbNext, nbLast, nbInsert, nbDelete, nbMoveUp, nbMoveDown }

/// <summary>What deleting the current element of a list does: the list's default, remove it, delete the object, or unlink and delete it.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldDeleteMode { dmDefault, dmRemoveFromList, dmDelete, dmUnlinkAllAndDelete }

/// <summary>A style of a navigator button: repeat while held down, show a focus rectangle (the elements of TBoldNavButtonStyle).</summary>
public enum TBoldNavButtonStyleElement { nsAllowTimer, nsFocusRect }

/// <summary>A navigator button was clicked.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldNavClick(object? Sender, TBoldNavigateBtn Button);
