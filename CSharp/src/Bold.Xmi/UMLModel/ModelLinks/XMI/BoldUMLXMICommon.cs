// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMICommon.pas
// Port map: CSharp/port/units/BoldUMLXMICommon.md

namespace Bold;

/// <summary>Names in XMI files: unqualified names, members qualified by the class that defines them, expression classes.</summary>
public static class BoldUMLXMICommon
{
    /// <summary>The name after the last ':' or '.'.</summary>
    [Delphi(PortStatus.Ported)]
    public static string UnqualifiedName(string aText) => aText[(aText.LastIndexOfAny([':', '.']) + 1)..];

    /// <summary>A qualified name without the packages Delphi's UML model put its classes in.</summary>
    [Delphi(PortStatus.Ported)]
    public static string RemoveBoldPackageNames(string Name)
    {
        const string BoldPackagePrefix = "BoldUMLModel.UMLMeta.";
        return Name.StartsWith(BoldPackagePrefix, StringComparison.Ordinal) ? Name[BoldPackagePrefix.Length..] : Name;
    }

    /// <summary>A member name without the trailing underscore Delphi added to names that are reserved words.</summary>
    [Delphi(PortStatus.Ported)]
    private static string RemoveTrailingUnderscore(string Name) => Name.EndsWith('_') ? Name[..^1] : Name;

    /// <summary>
    /// The qualified name of the class that defines a member of the class <paramref name="QualifiedClassName"/>, from
    /// the UML DTD; the class itself when the DTD does not know the member.
    /// </summary>
    [Delphi(PortStatus.Replaced, Note = "Finds the defining class in the UML DTD by the member's name; Delphi walked the runtime type info of its UML model, which is not ported (D6).")]
    public static string FindDefiningClass(string QualifiedClassName, string MemberName)
    {
        string member = RemoveTrailingUnderscore(MemberName);
        foreach (string qualifiedMember in TBoldUMLDTDReader.Shared.MembersOfClass(QualifiedClassName).Split(',', StringSplitOptions.TrimEntries))
        {
            if (UnqualifiedName(qualifiedMember) == member)
            {
                return qualifiedMember[..qualifiedMember.LastIndexOf('.')];
            }
        }

        return QualifiedClassName;
    }

    /// <summary>A member's name qualified by the class that defines it, as Foundation.Core.ModelElement.name.</summary>
    [Delphi(PortStatus.Replaced, Note = "Takes the class's qualified name and the member's name; Delphi took the member's runtime type info (D6).")]
    public static string QualifiedMemberName(string QualifiedClassName, string MemberName) =>
        FindDefiningClass(QualifiedClassName, MemberName) + "." + RemoveTrailingUnderscore(MemberName);

    [Delphi(PortStatus.Ported)]
    public static bool IsNameOfExpressionClass(string Name) => Name is "Expression" or "ActionExpression" or "ArgListsExpression" or "BooleanExpression"
        or "IterationExpression" or "MappingExpression" or "ObjectSetExpression" or "ProcedureExpression" or "TimeExpression" or "TypeExpression";
}
