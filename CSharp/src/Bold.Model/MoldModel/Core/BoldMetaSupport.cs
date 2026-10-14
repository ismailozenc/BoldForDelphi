// Delphi: Source/MoldModel/Core/BoldMetaSupport.pas
// Port map: CSharp/port/units/BoldMetaSupport.md

namespace Bold;

/// <summary>A Delphi object space class name and its COM interface name.</summary>
[Delphi(PortStatus.Ported)]
public readonly record struct TBoldNamePairRecord(string InterfaceName, string DelphiName);

/// <summary>Maps parameter type names of model methods to COM, IDL and interface type names.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMetaSupport
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldCOMIDLTypeNameMapping")]
    private readonly record struct TBoldCOMIDLTypeNameMapping(string DelphiName, string IDLName, string ComName);

    private static readonly TBoldCOMIDLTypeNameMapping[] BoldCOMIDLTypeNameMapping =
    [
        new("String", "BSTR", BoldMetaSupport.BoldWideStringTypeName),
        new("Integer", "long", "Integer"),
        new("Boolean", "VARIANT_BOOL", BoldMetaSupport.BoldWordBoolTypeName),
        new("Currency", "CURRENCY", "Currency"),
        new("Double", "double", "Double"),
        new("TDateTime", "DATE", "TDateTime"),
        new("Longint", "long", "Longint"),
        new("Smallint", "short", "Smallint"),
        new("Single", "float", "Single"),
        new("WideString", "BSTR", "WideString"),
        new("WordBool", "VARIANT_BOOL", "WordBool"),
        new("OleVariant", "VARIANT", "OleVariant"),
        new("Variant", "VARIANT", "OleVariant"),
        new("Shortint", "byte", "Shortint"),
        new("Int64", "__int64", "Int64"),
        new("Byte", "unsigned char", "Byte"),
        new("Word", "unsigned short", "Word"),
        new("Cardinal", "unsigned long", "Cardinal"),
        new("LongWord", "unsigned long", "LongWord"),
        new("Largeuint", "unsigned __int64", "Largeuint"),
        new("HResult", "HRESULT", "HResult"),
        new("SCODE", "SCODE", "SCODE"),
    ];

    /// <summary>The interface name for a model class (by name or Delphi name), an object space class, IDispatch or IUnknown; else "".</summary>
    [Delphi(PortStatus.Ported)]
    public static string ParameterTypeToInterfaceType(string ParameterType, TMoldModel MoldModel)
    {
        string result = "";
        TMoldClass? moldClass = MoldModel.Classes.ItemsByName(ParameterType) ?? MoldModel.Classes.ItemsByDelphiName(ParameterType);
        if (moldClass is not null)
        {
            result = moldClass.ExpandedInterfaceName;
        }
        else
        {
            foreach (TBoldNamePairRecord pair in BoldMetaSupport.BoldObjectSpaceInterfacenames)
            {
                if (SysUtils.CompareText(ParameterType, pair.DelphiName) == 0)
                {
                    return pair.InterfaceName;
                }
            }
        }

        if (SysUtils.SameText(ParameterType, "IDispatch"))
        {
            return "IDispatch";
        }

        if (SysUtils.SameText(ParameterType, "IUnknown"))
        {
            return "IUnknown";
        }

        return result;
    }

    /// <summary>The IDL type of a parameter type; <paramref name="IsPtr"/> is set when it is an interface.</summary>
    [Delphi(PortStatus.Ported)]
    public static string ParameterTypeToIDLType(string ParameterType, TMoldModel MoldModel, ref bool IsPtr)
    {
        IsPtr = false;
        foreach (TBoldCOMIDLTypeNameMapping m in BoldCOMIDLTypeNameMapping)
        {
            if (SysUtils.CompareText(ParameterType, m.DelphiName) == 0)
            {
                return m.IDLName;
            }
        }

        string result = ParameterTypeToInterfaceType(ParameterType, MoldModel);
        if (result != "")
        {
            IsPtr = true;
        }

        return result;
    }

    /// <summary>The COM type of a parameter type; <paramref name="IsConst"/> is set for strings and interfaces.</summary>
    [Delphi(PortStatus.Ported)]
    public static string ParameterTypeToCOMType(string ParameterType, TMoldModel MoldModel, ref bool IsConst)
    {
        IsConst = false;
        foreach (TBoldCOMIDLTypeNameMapping m in BoldCOMIDLTypeNameMapping)
        {
            if (SysUtils.CompareText(ParameterType, m.DelphiName) == 0)
            {
                IsConst = m.ComName == BoldMetaSupport.BoldWideStringTypeName;
                return m.ComName;
            }
        }

        string result = ParameterTypeToInterfaceType(ParameterType, MoldModel);
        if (result != "")
        {
            IsConst = true;
        }

        return result;
    }
}

/// <summary>Unit-level constants of BoldMetaSupport.</summary>
public static partial class BoldMetaSupport
{
    [Delphi(PortStatus.Ported)] public const string BoldWideStringTypeName = "WideString";
    [Delphi(PortStatus.Ported)] public const string BoldWordBoolTypeName = "WordBool";

    [Delphi(PortStatus.Ported)]
    public static readonly TBoldNamePairRecord[] BoldObjectSpaceInterfacenames =
    [
        new("IBoldElement", "TBoldElement"),
        new("IBoldMetaElement", "TBoldMetaElement"),
        new("IBoldElementTypeInfo", "TBoldElementTypeInfo"),
        new("IBoldTypeTypeInfo", "TBoldTypeTypeInfo"),
        new("IBoldClassTypeInfo", "TBoldClassTypeInfo"),
        new("IBoldNilTypeInfo", "TBoldNilTypeInfo"),
        new("IBoldListTypeInfo", "TBoldListTypeInfo"),
        new("IBoldAttributeTypeInfo", "TBoldAttributeTypeInfo"),
        new("IBoldSystemTypeInfo", "TBoldSystemTypeInfo"),
        new("IBoldDomainElement", "TBoldDomainElement"),
        new("IBoldObject", "TBoldObject"),
        new("IBoldMember", "TBoldMember"),
        new("IBoldAttribute", "TBoldAttribute"),
        new("IBoldObjectReference", "TBoldObjectReference"),
        new("IBoldList", "TBoldList"),
        new("IBoldObjectList", "TBoldObjectList"),
        new("IBoldMemberList", "TBoldMemberList"),
        new("IBoldSystem", "TBoldSystem"),
        new("IBoldBlob", "TBoldBlob"),
        new("IBoldElementHandle", "TBoldElementHandle"),
    ];
}
