// Delphi: Source/UMLModel/ModelLinks/XMI/BoldMOFInterfaces.pas
// Port map: CSharp/port/units/BoldMOFInterfaces.md

namespace Bold;

/// <summary>An object of the metamodel, seen as MOF sees it: attributes, references, its class and id.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldMOFObject
{
    [Delphi(PortStatus.Ported)] IBoldMOFAttributeList Attributes();
    [Delphi(PortStatus.Ported)] IBoldMOFReferenceList References();
    [Delphi(PortStatus.Ported)] string QualifiedClassName();
    [Delphi(PortStatus.Ported)] string LocalId();
}

[Delphi(PortStatus.Ported)]
public interface IBoldMOFAttribute
{
    [Delphi(PortStatus.Ported)] bool IsBoolean();
    [Delphi(PortStatus.Ported)] bool IsEnum();
    [Delphi(PortStatus.Ported)] bool IsDerived();
    [Delphi(PortStatus.Ported)] bool IsObject();
    [Delphi(PortStatus.Ported)] IBoldMOFObject AsObject();
    [Delphi(PortStatus.Ported)] string AsString();
    [Delphi(PortStatus.Ported)] bool AsBoolean();
    [Delphi(PortStatus.Ported)] string QualifiedName();
}

[Delphi(PortStatus.Ported)]
public interface IBoldMOFReference
{
    [Delphi(PortStatus.Ported)] bool IsMulti();
    [Delphi(PortStatus.Ported)] bool IsComposite();
    [Delphi(PortStatus.Ported)] bool IsDerived();
    [Delphi(PortStatus.Ported)] IBoldMOFObject? SingleObject();
    [Delphi(PortStatus.Ported)] IBoldMOFObjectList Objects();
    [Delphi(PortStatus.Ported)] string QualifiedName();
}

[Delphi(PortStatus.Ported)]
public interface IBoldMOFObjectList
{
    [Delphi(PortStatus.Ported)] int Count();
    [Delphi(PortStatus.Ported)] IBoldMOFObject GetObject(int index);
}

[Delphi(PortStatus.Ported)]
public interface IBoldMOFAttributeList
{
    [Delphi(PortStatus.Ported)] int Count();
    [Delphi(PortStatus.Ported)] IBoldMOFAttribute Attribute(int index);
}

[Delphi(PortStatus.Ported)]
public interface IBoldMOFReferenceList
{
    [Delphi(PortStatus.Ported)] int Count();
    [Delphi(PortStatus.Ported)] IBoldMOFReference Reference(int index);
}
