// Delphi: Source/UMLModel/Core/UMLConsts.pas
// Port map: CSharp/port/units/UMLConsts.md

namespace Bold;

/// <summary>Messages of the UML model units.</summary>
public static class UMLConsts
{
    [Delphi(PortStatus.Ported)] public const string sWrongValue = "%s.%s: Wrong value.";
    [Delphi(PortStatus.Ported)] public const string sConvertingModelToMold = "Converting UMLModel to Mold";
    [Delphi(PortStatus.Ported)] public const string sConvertingModelToUML = "Converting MoldModel to UML";
    [Delphi(PortStatus.Ported)] public const string sFlatteningModel = "Flattening model";
    [Delphi(PortStatus.Ported)] public const string sBoldifyingModel = "Boldifying Model";
    [Delphi(PortStatus.Ported)] public const string sCannotBoldifyIfRootClassHasSuperClass = "Can't boldify model where root class has superclass";
    [Delphi(PortStatus.Ported)] public const string sCanOnlyBeCalledIfBoldified = "TBoldUMLBoldify.GetRootClass: can only be called on boldified model";
    [Delphi(PortStatus.Ported)] public const string sUnknownTypeOfMember = "%s.BoldCopy: Unknown type of member %s: %s";
}
