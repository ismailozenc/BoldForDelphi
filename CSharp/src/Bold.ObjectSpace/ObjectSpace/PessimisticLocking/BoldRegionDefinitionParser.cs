// Delphi: Source/ObjectSpace/PessimisticLocking/BoldRegionDefinitionParser.pas
// Port map: CSharp/port/units/BoldRegionDefinitionParser.md

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>A lock region expression refers to something the model does not have.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldBadLockExpression(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>Raised inside the parser to stop at a syntax error; Parse catches it (the error is in Errors).</summary>
[Delphi(PortStatus.Ported)]
public class EBoldLockExpressionSyntaxError(string? message = null, Exception? innerException = null) : EAbort(message, innerException);

[Delphi(PortStatus.Ported)]
public enum TBoldRegionDefinitionSymbolType { stClassName, stMemberName, stRegionName }

[Delphi(PortStatus.Ported)]
public enum TBoldRegionParserErrorType { petSyntax, petSemantics }

/// <summary>
/// Parses lock region definitions, one per line, into region definitions, then expands them to subclasses and adds the
/// default regions of the model. The format:
/// <c>RegionName[ClassName]: member, member | SubRegion[role], SubRegion[role]-</c> (a trailing - makes the subregion
/// independent), for example <c>Region1[Person]: firstName, lastName | Region2[home]</c>.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionParser : TBoldMemoryManagedObject
{
    private const char SPACE = ' ';
    private const char TAB = '\t';
    private const string BOLDDEFAULTREGIONNAME = "Default";
    private const string BOLDDEFAULTEMPTYREGIONNAME = "Exist";
    private const string BOLDINDEPENDENTCASCADECOREREGIONPREFIX = "IC_";

    private string fExpression = "";

    /// <summary>The position in the expression, 1-based as in Delphi.</summary>
    private int fPosition;

    private readonly TBoldRegionDefinitions fRegionDefinitions;
    private readonly TBoldSystemTypeInfo fSystemTypeInfo;
    private readonly TStringList fErrors = new();
    private int fLastSymbolPosition;

    [Delphi(PortStatus.Ported)]
    public TBoldRegionParser(TBoldRegionDefinitions RegionDefinitions, TBoldSystemTypeInfo SystemTypeInfo)
    {
        fRegionDefinitions = RegionDefinitions;
        fSystemTypeInfo = SystemTypeInfo;
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldRegionParser.Destroy", Note = "Freed the error list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public TStringList Errors => fErrors;

    /// <summary>Consumes the text or records a syntax error (which stops the line).</summary>
    [Delphi(PortStatus.Ported)]
    protected void Eat(string s)
    {
        int OrgPosition = fPosition;
        foreach (char c in s)
        {
            if (EOS || NextToken != c)
            {
                AddError(OrgPosition, sExpectedToken, [s], TBoldRegionParserErrorType.petSyntax);
            }
            else
            {
                fPosition++;
            }
        }

        Skip();
    }

    /// <summary>
    /// When the model asks for them: the "Default" region of each class (its attributes and roles, with subregions as
    /// the roles' default region modes say) and the empty "Exist" region.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void GenerateDefaultRegions()
    {
        if (!SystemTypeInfo.GenerateDefaultRegions)
        {
            return;
        }

        TBoldRegionCoreDefinition DefaultCore = RegionDefinitions.EnsuredCoreDefinition(BOLDDEFAULTREGIONNAME);
        TBoldRegionCoreDefinition DefaultEmptyCore = RegionDefinitions.EnsuredCoreDefinition(BOLDDEFAULTEMPTYREGIONNAME);
        for (int ClassIx = 0; ClassIx < SystemTypeInfo.TopSortedClasses.Count; ClassIx++)
        {
            TBoldClassTypeInfo ClassTypeInfo = SystemTypeInfo.TopSortedClasses[ClassIx];
            DefaultEmptyCore.EnsuredConcreteDefinition(ClassTypeInfo, out _);

            if (!ClassTypeInfo.GenerateDefaultRegion)
            {
                continue;
            }

            TBoldConcreteRegionDefinition DefaultRegion = DefaultCore.EnsuredConcreteDefinition(ClassTypeInfo, out bool existed);
            if (existed)
            {
                continue;
            }

            for (int MemberIx = 0; MemberIx < ClassTypeInfo.AllMembers.Count; MemberIx++)
            {
                TBoldMemberRTInfo MemberRTInfo = ClassTypeInfo.AllMembers[MemberIx];
                if (MemberRTInfo.IsAttribute)
                {
                    _ = new TBoldRegionElementInclusion(DefaultRegion, MemberRTInfo);
                }
                else if (MemberRTInfo is TBoldRoleRTInfo { IsDerived: false } RoleRTInfo)
                {
                    switch (RoleRTInfo.DefaultRegionMode)
                    {
                        case TBoldAssociationEndDefaultRegionMode.aedrmExistence:
                            _ = new TBoldRegionElementInclusion(DefaultRegion, MemberRTInfo);
                            _ = new TBoldSubregionReference(DefaultRegion, DefaultEmptyCore, RoleRTInfo, true);
                            break;
                        case TBoldAssociationEndDefaultRegionMode.aedrmCascade:
                            _ = new TBoldRegionElementInclusion(DefaultRegion, MemberRTInfo);
                            _ = new TBoldSubregionReference(DefaultRegion, DefaultCore, RoleRTInfo, true);
                            break;
                        case TBoldAssociationEndDefaultRegionMode.aedrmIndependentCascade:
                            _ = new TBoldRegionElementInclusion(DefaultRegion, MemberRTInfo);
                            TBoldRegionCoreDefinition IndependentCascadeCore =
                                RegionDefinitions.EnsuredCoreDefinition(BOLDINDEPENDENTCASCADECOREREGIONPREFIX + RoleRTInfo.ExpressionName);
                            TBoldConcreteRegionDefinition IndependentCascadeRegion = IndependentCascadeCore.EnsuredConcreteDefinition(ClassTypeInfo, out bool icExisted);
                            if (!icExisted)
                            {
                                _ = new TBoldRegionElementInclusion(IndependentCascadeRegion, MemberRTInfo);
                                _ = new TBoldSubregionReference(IndependentCascadeRegion, DefaultCore, RoleRTInfo, true);
                            }

                            break;
                    }
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEOS() => fPosition > fExpression.Length;

    private static bool IsIdentStart(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';

    /// <summary>Reads an identifier (records a syntax error when there is none).</summary>
    [Delphi(PortStatus.Ported)]
    protected string GetSymbol(TBoldRegionDefinitionSymbolType Symboltype)
    {
        fLastSymbolPosition = fPosition;
        if (EOS || !IsIdentStart(NextToken))
        {
            string msg = Symboltype switch
            {
                TBoldRegionDefinitionSymbolType.stClassName => sClassNameExpected,
                TBoldRegionDefinitionSymbolType.stMemberName => sMemberNameExpected,
                _ => sRegionNameExpected,
            };
            AddError(fPosition, msg, [], TBoldRegionParserErrorType.petSyntax);
        }

        string result = NextToken.ToString();
        fPosition++;
        while (!EOS && (IsIdentStart(NextToken) || NextToken is >= '0' and <= '9'))
        {
            result += NextToken;
            fPosition++;
        }

        Skip();
        return result;
    }

    /// <summary>Parses all lines into the region definitions (cleared first); true when there were no errors.</summary>
    [Delphi(PortStatus.Ported)]
    public bool Parse(TStrings RegionDefinitionList)
    {
        RegionDefinitions.Clear();
        for (int i = 0; i < RegionDefinitionList.Count; i++)
        {
            fExpression = RegionDefinitionList[i];
            fPosition = 1;
            Skip();
            try
            {
                ParseCurrentExpression();
            }
            catch (EBoldLockExpressionSyntaxError)
            {
                // the error is in Errors; go on with the next line
            }
        }

        RegionDefinitions.ExpandDefinitions();
        GenerateDefaultRegions();
        CheckRegionReferences();
        return fErrors.Count == 0;
    }

    [Delphi(PortStatus.Ported)]
    private void ParseMembers(TBoldConcreteRegionDefinition ConcreteRegionDefinition)
    {
        while (!EOS && NextToken != '|')
        {
            string MemberName = GetSymbol(TBoldRegionDefinitionSymbolType.stMemberName);
            TBoldMemberRTInfo? MemberRTInfo = ConcreteRegionDefinition.RootClass.MemberRTInfoByExpressionName(MemberName);
            if (MemberRTInfo is not null)
            {
                _ = new TBoldRegionElementInclusion(ConcreteRegionDefinition, MemberRTInfo);
            }
            else
            {
                AddError(fLastSymbolPosition, sXIsNotAMember, [MemberName, ConcreteRegionDefinition.RootClass.ExpressionName], TBoldRegionParserErrorType.petSemantics);
            }

            TryToEat(",");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ParseSubregions(TBoldConcreteRegionDefinition ConcreteRegionDefinition)
    {
        while (!EOS)
        {
            string RegionName = GetSymbol(TBoldRegionDefinitionSymbolType.stRegionName);
            Eat("[");
            string MemberName = GetSymbol(TBoldRegionDefinitionSymbolType.stMemberName);
            Eat("]");
            bool IsDependent = !TryToEat("-");
            TBoldMemberRTInfo? MemberRTInfo = ConcreteRegionDefinition.RootClass.MemberRTInfoByExpressionName(MemberName);
            if (MemberRTInfo is null)
            {
                AddError(fLastSymbolPosition, sXIsNotAMember, [MemberName, ConcreteRegionDefinition.RootClass.ExpressionName], TBoldRegionParserErrorType.petSemantics);
            }
            else if (MemberRTInfo is not TBoldRoleRTInfo role)
            {
                AddError(fLastSymbolPosition, sMemberIsNotARole, [ConcreteRegionDefinition.RootClass.ExpressionName, MemberName], TBoldRegionParserErrorType.petSemantics);
            }
            else
            {
                TBoldRegionCoreDefinition Subregion = RegionDefinitions.EnsuredCoreDefinition(RegionName);
                _ = new TBoldSubregionReference(ConcreteRegionDefinition, Subregion, role, IsDependent);
                _ = new TBoldRegionElementInclusion(ConcreteRegionDefinition, MemberRTInfo);
            }

            TryToEat(",");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ParseMembersAndSubregions(TBoldRegionCoreDefinition CoreDefinition, TBoldClassTypeInfo ClassTypeInfo)
    {
        TBoldConcreteRegionDefinition ConcreteRegionDefinition = CoreDefinition.EnsuredConcreteDefinition(ClassTypeInfo, out bool AlreadyDefined);
        if (AlreadyDefined)
        {
            AddError(1, sMultipleDefinitions, [CoreDefinition.Name, ClassTypeInfo.ExpressionName], TBoldRegionParserErrorType.petSemantics);
        }
        else
        {
            ParseMembers(ConcreteRegionDefinition);
            if (TryToEat("|"))
            {
                ParseSubregions(ConcreteRegionDefinition);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void Skip()
    {
        while (!EOS && (NextToken == SPACE || NextToken == TAB))
        {
            fPosition++;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool TryToEat(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            int p = fPosition + i;
            if (EOS || p > fExpression.Length || fExpression[p - 1] != s[i])
            {
                return false;
            }
        }

        fPosition += s.Length;
        Skip();
        return true;
    }

    [Delphi(PortStatus.Ported)]
    private char GetNextToken() => fExpression[fPosition - 1];

    [Delphi(PortStatus.Ported)]
    private void ParseCurrentExpression()
    {
        string RegionName = GetSymbol(TBoldRegionDefinitionSymbolType.stRegionName);
        TBoldRegionCoreDefinition CoreDefinition = RegionDefinitions.EnsuredCoreDefinition(RegionName);
        Eat("[");
        string ClassName = GetSymbol(TBoldRegionDefinitionSymbolType.stClassName);
        Eat("]");
        Eat(":");
        TBoldClassTypeInfo? ClassTypeInfo = fSystemTypeInfo.ClassTypeInfoByExpressionName(ClassName);
        if (ClassTypeInfo is not null)
        {
            ParseMembersAndSubregions(CoreDefinition, ClassTypeInfo);
        }
        else
        {
            AddError(fLastSymbolPosition, sUnknownClassName, [ClassName], TBoldRegionParserErrorType.petSemantics);
        }
    }

    /// <summary>Records the error (with the expression and position unless -1); a syntax error stops the line.</summary>
    [Delphi(PortStatus.Ported)]
    private void AddError(int pos, string msg, object?[] args, TBoldRegionParserErrorType ErrorType)
    {
        string ErrorMsg = pos != -1 ? SysUtils.Format("\"%s\" (%d): ", fExpression, pos) : "";
        ErrorMsg += SysUtils.Format(msg, args);
        fErrors.Add(ErrorMsg);
        if (ErrorType == TBoldRegionParserErrorType.petSyntax)
        {
            throw new EBoldLockExpressionSyntaxError(ErrorMsg);
        }
    }

    /// <summary>Records an error for each region used as a subregion but not defined.</summary>
    [Delphi(PortStatus.Ported)]
    private void CheckRegionReferences()
    {
        for (int i = 0; i < fRegionDefinitions.CoreDefinitions.Count; i++)
        {
            var CoreDef = (TBoldRegionCoreDefinition)fRegionDefinitions.CoreDefinitions[i]!;
            if (CoreDef.ConcreteDefinitions.Count == 0)
            {
                for (int j = 0; j < CoreDef.UsedBy.Count; j++)
                {
                    AddError(-1, sReferencedRegionNotDefined,
                        [CoreDef.Name, CoreDef.UsedBy[j].ParentRegion.CoreDefinition.Name, CoreDef.UsedBy[j].ParentRegion.RootClass.ExpressionName,
                         CoreDef.UsedBy[j].SubregionRootNavigation.ExpressionName], TBoldRegionParserErrorType.petSemantics);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)] protected bool EOS => GetEOS();
    [Delphi(PortStatus.Ported)] protected char NextToken => GetNextToken();
    [Delphi(PortStatus.Ported)] public TBoldRegionDefinitions RegionDefinitions => fRegionDefinitions;
    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo SystemTypeInfo => fSystemTypeInfo;
}
