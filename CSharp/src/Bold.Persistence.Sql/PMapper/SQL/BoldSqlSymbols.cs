// Delphi: Source/PMapper/SQL/BoldSqlSymbols.pas
// Port map: CSharp/port/units/BoldSqlSymbols.md
// The OCL operations that can be evaluated in SQL (OCL2SQL): comparisons, arithmetic, logic, string and group
// functions, iterations, allInstances, list operations and type tests, each building its where clause fragment or query.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlSymbols", Symbol = "finalization", Note = "Freed the symbols; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldSqlSymbols: the registered symbols.</summary>
public static partial class BoldSqlSymbols
{
    private static readonly List<TBSS_Symbol> SqlSymbols = [];

    /// <summary>The symbol of an operation name (case insensitive).</summary>
    [Delphi(PortStatus.Ported)]
    public static TBSS_Symbol FindSymbolByName(string Name)
    {
        foreach (TBSS_Symbol symbol in SqlSymbols)
        {
            if (SysUtils.CompareText(symbol.Name, Name) == 0)
            {
                return symbol;
            }
        }

        throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindSymbolForX, Name));
    }

#pragma warning disable CA2255
    /// <summary>Registers the symbols (TBSS_Add twice, as in Delphi; SymmetricDifference is not supported since SQL does not support XOR).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization()
    {
        SqlSymbols.AddRange(
        [
            new TBSS_Add(), new TBSS_Equal(), new TBSS_NotEqual(), new TBSS_Add(), new TBSS_Subtract(), new TBSS_Multiply(), new TBSS_Divide(),
            new TBSS_Less(), new TBSS_Greater(), new TBSS_LessEQ(), new TBSS_GreaterEQ(), new TBSS_Div(), new TBSS_Mod(), new TBSS_or(), new TBSS_and(),
            new TBSS_xor(), new TBSS_implies(), new TBSS_not(), new TBSS_UnaryMinus(), new TBSS_isNull(), new TBSS_Size(), new TBSS_Select(),
            new TBSS_reject(), new TBSS_orderby(), new TBSS_orderDescending(), new TBSS_AllInstances(), new TBSS_SQLLike(),
            new TBSS_SQLLikeCaseInsensitive(), new TBSS_includes(), new TBSS_Sum(), new TBSS_Average(), new TBSS_MinValue(), new TBSS_Maxvalue(),
            new TBSS_Length(), new TBSS_Exists(), new TBSS_ForAll(), new TBSS_NotEmpty(), new TBSS_isEmpty(), new TBSS_ToUpper(), new TBSS_toLower(),
            new TBSS_union(), new TBSS_Intersection(), new TBSS_Difference(), new TBSS_OclIsTypeOf(), new TBSS_OclIsKindOf(), new TBSS_OclAsType(),
            new TBSS_FilterOnType(),
        ]);
    }
#pragma warning restore CA2255
}

/// <summary>A symbol named by its class name after "TBSS_".</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Symbol : TBoldSqlSymbol
{
    [Delphi(PortStatus.Ported)]
    protected override string GetName() => GetType().Name[5..];
}

/// <summary>(arg0 symbol arg1); argument queries become exists fragments.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_BinarySymbol : TBSS_Symbol
{
    /// <summary>Moves the destination's fragment into the source's query, and makes the destination's fragment an exists on it.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ConvertQueryToWCF(TBoldSqlNode SourceNode, TBoldSqlNode DestNode)
    {
        TBoldSQLQuery Query = SourceNode.RelinquishQuery();
        Query.AddWCF(DestNode.RelinquishWCF());
        DestNode.WCF = new TBoldSQLWCFExists(Query, Query.MainTable ?? SourceNode.MainTableRef(Query));
    }

    [Delphi(PortStatus.Ported)]
    protected void CollectArgWCFs(TBoldSqlOperation OpNode)
    {
        if (OpNode.Args[1].HasQuery)
        {
            ConvertQueryToWCF(OpNode.Args[1], OpNode);
        }

        if (OpNode.Args[0].HasQuery)
        {
            ConvertQueryToWCF(OpNode.Args[0], OpNode);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        OperationNode.WCF = new TBoldSQLWCFBinaryInfix(OperationNode.Args[0].RelinquishWCF()!, OperationNode.Args[1].RelinquishWCF()!, SQLName);
        CollectArgWCFs(OperationNode);
    }
}

[Delphi(PortStatus.Ported)] public class TBSS_BinaryBooleanSymbol : TBSS_BinarySymbol;

[Delphi(PortStatus.Ported)]
public class TBSS_Equal : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "=";
}

[Delphi(PortStatus.Ported)]
public class TBSS_NotEqual : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "<>";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Less : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "<";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Greater : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => ">";
}

[Delphi(PortStatus.Ported)]
public class TBSS_LessEQ : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "<=";
}

[Delphi(PortStatus.Ported)]
public class TBSS_GreaterEQ : TBSS_BinaryBooleanSymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => ">=";
}

[Delphi(PortStatus.Ported)]
public class TBSS_SQLLike : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "LIKE";
}

/// <summary>UPPER(arg0) LIKE UPPER(arg1).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_SQLLikeCaseInsensitive : TBSS_SQLLike
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        OperationNode.WCF = new TBoldSQLWCFBinaryInfix(
            new TBoldSQLWCFUnaryPrefix(OperationNode.Args[0].RelinquishWCF()!, "UPPER"),
            new TBoldSQLWCFUnaryPrefix(OperationNode.Args[1].RelinquishWCF()!, "UPPER"), SQLName);
        CollectArgWCFs(OperationNode);
    }
}

[Delphi(PortStatus.Ported)]
public class TBSS_Add : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "+";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Subtract : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "-";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Multiply : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "*";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Divide : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "/";
}

[Delphi(PortStatus.Ported)] public class TBSS_or : TBSS_BinarySymbol;
[Delphi(PortStatus.Ported)] public class TBSS_and : TBSS_BinarySymbol;
[Delphi(PortStatus.Ported)] public class TBSS_xor : TBSS_BinarySymbol;

/// <summary>(NOT arg0) OR arg1.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_implies : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFUnaryPrefix(OperationNode.Args[0].RelinquishWCF()!, "NOT"), OperationNode.Args[1].RelinquishWCF()!, "OR");
}

/// <summary>(symbol(arg0)).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_UnarySymbol : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFUnaryPrefix(OperationNode.Args[0].RelinquishWCF()!, SQLName);
}

[Delphi(PortStatus.Ported)] public class TBSS_not : TBSS_UnarySymbol;

[Delphi(PortStatus.Ported)]
public class TBSS_UnaryMinus : TBSS_UnarySymbol
{
    [Delphi(PortStatus.Ported)] protected override string GetName() => "unary-";
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "-";
}

/// <summary>arg0 IS NULL (in an exists on the argument's query, if it has one).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_isNull : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        TBoldSqlWCF WCF = new TBoldSQLWCFUnaryPostfix(OperationNode.Args[0].RelinquishWCF()!, "IS NULL");
        if (OperationNode.Args[0].HasQuery)
        {
            OperationNode.Args[0].Query.AddWCF(WCF);
            TBoldSQLQuery Query = OperationNode.Args[0].RelinquishQuery();
            WCF = new TBoldSQLWCFExists(Query, Query.MainTable!);
        }

        OperationNode.WCF = WCF;
    }
}

[Delphi(PortStatus.Ported)] public class TBSS_Abs : TBSS_UnarySymbol;
[Delphi(PortStatus.Ported)] public class TBSS_Floor : TBSS_UnarySymbol;
[Delphi(PortStatus.Ported)] public class TBSS_Round : TBSS_UnarySymbol;

[Delphi(PortStatus.Ported)]
public class TBSS_ToUpper : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "UPPER";

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFUnaryPrefix(OperationNode.Args[0].RelinquishWCF()!, SQLName);
}

[Delphi(PortStatus.Ported)]
public class TBSS_toLower : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "LOWER";

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFUnaryPrefix(OperationNode.Args[0].RelinquishWCF()!, SQLName);
}

/// <summary>A count of the argument's query.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Size : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFSize(OperationNode.Args[0].RelinquishQuery());
}

/// <summary>x.isEmpty: not exists x.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_isEmpty : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        TBoldSQLQuery Query = OperationNode.Args[0].RelinquishQuery();
        OperationNode.WCF = new TBoldSQLWCFUnaryPrefix(new TBoldSQLWCFExists(Query, Query.MainTable!), "NOT");
    }
}

/// <summary>x.notEmpty: exists x.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_NotEmpty : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        TBoldSQLQuery Query = OperationNode.Args[0].RelinquishQuery();
        OperationNode.WCF = new TBoldSQLWCFExists(Query, Query.MainTable!);
    }
}

/// <summary>A group function on the column of the argument's query.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_GroupFunctions : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        if (!OperationNode.Args[0].HasQuery)
        {
            throw new EBoldInternal(SysUtils.Format("Argument to %s has no Query", Name));
        }

        if (OperationNode.Args[0].RelinquishWCF() is not TBoldSQLWCFColumnRef columnRef)
        {
            throw new EBoldInternal(SysUtils.Format("Argument to %s has has no ColumnRef WCF", Name));
        }

        OperationNode.WCF = new TBoldSQLWCFGroupFunctionWithColumn(OperationNode.Args[0].RelinquishQuery(), columnRef.ColumnRef, SQLName);
    }
}

[Delphi(PortStatus.Ported)] public class TBSS_Sum : TBSS_GroupFunctions;

[Delphi(PortStatus.Ported)]
public class TBSS_Maxvalue : TBSS_GroupFunctions
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "MAX";
}

[Delphi(PortStatus.Ported)]
public class TBSS_MinValue : TBSS_GroupFunctions
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "MIN";
}

[Delphi(PortStatus.Ported)]
public class TBSS_Average : TBSS_GroupFunctions
{
    [Delphi(PortStatus.Ported)] protected override string GetSQLName() => "AVG";
}

/// <summary>An iteration: of the collection's class.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Iteration : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode) => OperationNode.Args[0].ObjectMapper;
}

/// <summary>The loop variable's query with the condition.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Select : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        var Sel = (TBoldSqlIteration)OperationNode;
        Sel.Query = Sel.LoopVar.RelinquishQuery();
        Sel.Query.AddWCF(OperationNode.Args[1].RelinquishWCF());
    }
}

/// <summary>The loop variable's query with the negated condition.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_reject : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        var Sel = (TBoldSqlIteration)OperationNode;
        Sel.Query = Sel.LoopVar.RelinquishQuery();
        Sel.Query.AddWCF(new TBoldSQLWCFUnaryPrefix(OperationNode.Args[1].RelinquishWCF()!, "NOT"));
    }
}

/// <summary>Exists on the loop variable's query with the condition.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Exists : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        TBoldSQLQuery Query = ((TBoldSqlIteration)OperationNode).LoopVar.RelinquishQuery();
        Query.AddWCF(OperationNode.Args[1].RelinquishWCF());
        OperationNode.WCF = new TBoldSQLWCFExists(Query, OperationNode.Args[0].MainTableRef(Query));
    }
}

/// <summary>ForAll(x): not exists (not x).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_ForAll : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        TBoldSQLQuery Query = ((TBoldSqlIteration)OperationNode).LoopVar.RelinquishQuery();
        Query.AddWCF(new TBoldSQLWCFUnaryPrefix(OperationNode.Args[1].RelinquishWCF()!, "NOT"));
        OperationNode.WCF = new TBoldSQLWCFUnaryPrefix(new TBoldSQLWCFExists(Query, OperationNode.Args[0].MainTableRef(Query)), "NOT");
    }
}

/// <summary>The loop variable's query ordered by the member's single column.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_orderby : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        if (OperationNode.Args[1] is not TBoldSqlMember OrderByMember)
        {
            throw new EBold(BoldPMConsts.sArgToOrderByMustBeMember);
        }

        if (OrderByMember.MemberMapper!.ColumnCount != 1)
        {
            throw new EBold(BoldPMConsts.sArgToOrderByMustHaveExactlyOneColumn);
        }

        TBoldSqlVariableBinding Loopvar = ((TBoldSqlIteration)OperationNode).LoopVar;
        TBoldSQLQuery Query = Loopvar.RelinquishQuery();
        var TableDescription = (TBoldSQLTableDescription)OrderByMember.MemberMapper.ColumnDescriptions[0].Owner!;
        TBoldSQLTableReference TableRef = OperationNode.TableReferenceForTable(TableDescription, Query, false);
        Query.AddColumnToOrderBy(TableRef.GetColumnReference(OrderByMember.MemberMapper.ColumnDescriptions[0].SQLName));
        OperationNode.WCF = Loopvar.RelinquishWCF();
        OperationNode.Query = Query;
    }
}

[Delphi(PortStatus.Ported)]
public class TBSS_orderDescending : TBSS_Iteration
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        throw new EBold(BoldPMConsts.sOrderByDescendingNotImplemented);
}

/// <summary>A new query of the class.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_AllInstances : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode) => OperationNode.Args[0].ObjectMapper;

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) => OperationNode.NewQuery(NameSpace);
}

/// <summary>(symbol(arg0, arg1)).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_PrefixBinarySymbol : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        OperationNode.WCF = new TBoldSQLWCFBinaryPrefix(OperationNode.Args[0].RelinquishWCF()!, OperationNode.Args[1].RelinquishWCF()!, SQLName);
}

[Delphi(PortStatus.Ported)] public class TBSS_Div : TBSS_PrefixBinarySymbol;
[Delphi(PortStatus.Ported)] public class TBSS_Mod : TBSS_PrefixBinarySymbol;

/// <summary>Exists on the collection's query with its id equal to (or in) the argument.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_includes : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        if (!OperationNode.Args[0].HasQuery)
        {
            throw new EBoldInternal("Arg 0 for Includes has no Query");
        }

        TBoldSQLColumnReference Q1IdColumnRef = OperationNode.Args[0].MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME);
        TBoldSqlWCF? WCF;
        if (OperationNode.Args[1] is TBoldSqlVariableReference VarReference)
        {
            WCF = VarReference.IsExternalVariable()
                ? TBoldSQLWCFBinaryInfix.CreateWCFForIdList(Q1IdColumnRef, VarReference.VariableBinding.Context!)
                // BoldId = VariabelReference.BoldId
                : new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(Q1IdColumnRef),
                    new TBoldSQLWCFColumnRef(OperationNode.Args[1].MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME)), "=");
        }
        else
        {
            // BoldID in (Arg1.Query)
            TBoldSQLQuery ArgQuery = OperationNode.Args[1].RelinquishQuery();
            WCF = new TBoldSQLWCFInQuery(new TBoldSQLWCFColumnRef(Q1IdColumnRef), ArgQuery, OperationNode.Args[1].MainTableRef(ArgQuery));
        }

        OperationNode.Args[0].Query.AddWCF(WCF);
        TBoldSQLQuery Query = OperationNode.Args[0].RelinquishQuery();
        OperationNode.WCF = new TBoldSQLWCFExists(Query, OperationNode.Args[0].MainTableRef(Query));
    }
}

[Delphi(PortStatus.Ported)] public class TBSS_Length : TBSS_UnarySymbol;

/// <summary>The list operations: of the common superclass, matching ids to each argument.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_ListOperations : TBSS_Symbol
{
    /// <summary>The main query's id equal to, or in, the argument.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldSqlWCF CreateBoldIdMatchWCF(TBoldSqlOperation MainNode, TBoldSqlNode ArgNode)
    {
        TBoldSQLColumnReference MainColumn = MainNode.Query.MainTable!.GetColumnReference(BoldDefs.IDCOLUMN_NAME);
        if (ArgNode is TBoldSqlVariableReference VarRef)
        {
            return VarRef.IsExternalVariable()
                ? TBoldSQLWCFBinaryInfix.CreateWCFForIdList(MainColumn, VarRef.VariableBinding.Context!)!
                // BoldId = VariabelReference.BoldId
                : new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(MainColumn), new TBoldSQLWCFColumnRef(ArgNode.MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME)), "=");
        }

        // BoldID in (Arg1.Query)
        TBoldSQLQuery Query = ArgNode.RelinquishQuery();
        return new TBoldSQLWCFInQuery(new TBoldSQLWCFColumnRef(MainColumn), Query, ArgNode.MainTableRef(Query));
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode)
    {
        TBoldObjectSQLMapper ObjectMapper1 = OperationNode.Args[0].ObjectMapper;
        TBoldObjectSQLMapper ObjectMapper2 = OperationNode.Args[1].ObjectMapper;
        while (ObjectMapper1 != ObjectMapper2)
        {
            if (ObjectMapper1.TopSortedIndex > ObjectMapper2.TopSortedIndex)
            {
                ObjectMapper1 = (TBoldObjectSQLMapper)ObjectMapper1.SuperClass!;
            }
            else
            {
                ObjectMapper2 = (TBoldObjectSQLMapper)ObjectMapper2.SuperClass!;
            }
        }

        return ObjectMapper1;
    }

    /// <summary>A new query of the class with (id matches arg0) Symbol (id matches arg1).</summary>
    private protected void BuildCombination(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace, string Operation, bool NegateSecond)
    {
        OperationNode.NewQuery(NameSpace);
        TBoldSqlWCF first = CreateBoldIdMatchWCF(OperationNode, OperationNode.Args[0]);
        TBoldSqlWCF second = CreateBoldIdMatchWCF(OperationNode, OperationNode.Args[1]);
        OperationNode.Query.AddWCF(new TBoldSQLWCFBinaryInfix(first, NegateSecond ? new TBoldSQLWCFUnaryPrefix(second, "NOT") : second, Operation));
    }
}

/// <summary>a->union(b): (id in a) or (id in b).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_union : TBSS_ListOperations
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) => BuildCombination(OperationNode, NameSpace, "OR", false);
}

/// <summary>a->Intersection(b): (id in a) and (id in b).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Intersection : TBSS_ListOperations
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) => BuildCombination(OperationNode, NameSpace, "AND", false);
}

/// <summary>a->symmetricDifference(b): ((id in a) xor (id in b)); not registered since SQL does not support XOR.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_SymmetricDifference : TBSS_ListOperations
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) => BuildCombination(OperationNode, NameSpace, "XOR", false);
}

/// <summary>a->difference(b): (id in a) and (not (id in b)).</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_Difference : TBSS_ListOperations
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) => BuildCombination(OperationNode, NameSpace, "AND", true);
}

/// <summary>The type column equals the class's db type.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_OclIsTypeOf : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        var TypeColRef = new TBoldSQLWCFColumnRef(OperationNode.Args[0].MainTableRef().GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
        var TypeValue = new TBoldSQLWCFInteger(OperationNode.Args[1].ObjectMapper.BoldDbType);
        OperationNode.WCF = new TBoldSQLWCFBinaryInfix(TypeColRef, TypeValue, "=");
        CollectArgWCFs(OperationNode);
    }
}

/// <summary>The type column is in the class's and subclasses' db types.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_OclIsKindOf : TBSS_BinarySymbol
{
    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        var ObjectMapper = (TBoldObjectDefaultMapper)OperationNode.Args[1].ObjectMapper;
        var TypeColRef = new TBoldSQLWCFColumnRef(OperationNode.Args[0].MainTableRef().GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
        var TypeValue = new TBoldSQLWCFGenericExpression("(" + ObjectMapper.SubClassesID + ")");
        OperationNode.WCF = new TBoldSQLWCFBinaryInfix(TypeColRef, TypeValue, "in");
        CollectArgWCFs(OperationNode);
    }
}

/// <summary>The argument as the class: a new query joined on id for a loop variable, else the argument's query.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_OclAsType : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode) => OperationNode.Args[1].ObjectMapper;

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        if (OperationNode.Args[0] is TBoldSqlVariableReference { VariableBinding.IsLoopVar: true })
        {
            OperationNode.NewQuery(NameSpace);
            OperationNode.EnsureRetrievalOfMainColumns();
            OperationNode.Query.AddJoin(OperationNode.MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME),
                OperationNode.Args[0].MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME));
        }
        else
        {
            OperationNode.Query = OperationNode.Args[0].RelinquishQuery();
        }
    }
}

/// <summary>The argument filtered to the class: the type column in its db types, in the query or with the fragment.</summary>
[Delphi(PortStatus.Ported)]
public class TBSS_FilterOnType : TBSS_Symbol
{
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode) => OperationNode.Args[1].ObjectMapper;

    [Delphi(PortStatus.Ported)]
    public override void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace)
    {
        var ObjectMapper = (TBoldObjectDefaultMapper)OperationNode.Args[1].ObjectMapper;
        var TypeColRef = new TBoldSQLWCFColumnRef(OperationNode.Args[0].MainTableRef().GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
        var TypeValue = new TBoldSQLWCFGenericExpression("(" + ObjectMapper.SubClassesID + ")");
        TBoldSqlWCF WCF = new TBoldSQLWCFBinaryInfix(TypeColRef, TypeValue, "IN");
        if (OperationNode.Args[0].HasQuery)
        {
            OperationNode.Query = OperationNode.Args[0].RelinquishQuery();
        }

        if (OperationNode.HasQuery)
        {
            OperationNode.Query.AddWCF(WCF);
        }
        else
        {
            if (OperationNode.Args[0].WCF is not null)
            {
                WCF = new TBoldSQLWCFBinaryInfix(WCF, OperationNode.Args[0].RelinquishWCF()!, "AND");
            }

            OperationNode.WCF = WCF;
        }

        OperationNode.CopyTableReferences(OperationNode.Args[0]);
    }
}
