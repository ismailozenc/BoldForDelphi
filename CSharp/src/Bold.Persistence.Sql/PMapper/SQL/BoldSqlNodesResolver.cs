// Delphi: Source/PMapper/SQL/BoldSqlNodesResolver.pas
// Port map: CSharp/port/units/BoldSqlNodesResolver.md
// Resolves the SQL node tree: the symbol of each operation and the object (and member) mapper of each node.

namespace Bold;

/// <summary>Resolves symbols and mappers in the SQL node tree, external variables first.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlNodeResolver : TBoldSqlNodeVisitor
{
    private readonly TBoldSystemSQLMapper fSystemMapper;
    private readonly TBoldSqlNode? fRootNode;
    private readonly TBoldSqlNodeList? fExternalVariables;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlNodeResolver(TBoldSystemSQLMapper SystemMapper, TBoldSqlNode? RootNode, TBoldSqlNodeList? ExternalVariables)
    {
        fRootNode = RootNode;
        fExternalVariables = ExternalVariables;
        fSystemMapper = SystemMapper;
    }

    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        if (fExternalVariables is not null)
        {
            for (int i = 0; i < fExternalVariables.Count; i++)
            {
                fExternalVariables[i].AcceptVisitor(this);
            }
        }

        fRootNode?.AcceptVisitor(this);
    }

    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlNode(TBoldSqlNode N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlLiteral(TBoldSqlLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlStrLiteral(TBoldSqlStrLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlFloatLiteral(TBoldSqlFloatLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlEnumLiteral(TBoldSqlEnumLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlDateLiteral(TBoldSqlDateLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlTimeLiteral(TBoldSqlTimeLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlIntLiteral(TBoldSqlIntLiteral N) { }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlListCoercion(TBoldSqlListCoercion N)
    {
        N.Child.AcceptVisitor(this);
        N.ObjectMapper = N.Child.ObjectMapper;
    }

    /// <summary>A plain operation: its symbol, its arguments, then its mapper (iterations are resolved in VisitTBoldSqlIteration).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlOperation(TBoldSqlOperation N)
    {
        if (N.GetType() == typeof(TBoldSqlOperation))
        {
            N.Symbol = BoldSqlSymbols.FindSymbolByName(N.OperationName);
            N.Args.TraverseList(this);
            N.ObjectMapper = N.Symbol!.ResolveObjectMapper(N)!;
        }
    }

    /// <summary>The collection, the symbol and mapper, the loop variable (of the same class), then the other arguments.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlIteration(TBoldSqlIteration N)
    {
        N.Args[0].AcceptVisitor(this);
        N.Symbol = BoldSqlSymbols.FindSymbolByName(N.OperationName);
        N.ObjectMapper = N.Symbol!.ResolveObjectMapper(N)!;
        N.LoopVar.ObjectMapper = N.ObjectMapper;
        N.LoopVar.AcceptVisitor(this);
        N.LoopVar.IsLoopVar = true;
        for (int i = 1; i < N.Args.Count; i++)
        {
            N.Args[i].AcceptVisitor(this);
        }
    }

    /// <summary>The member's mapper; a link's node gets the other end's object mapper.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlMember(TBoldSqlMember N)
    {
        N.MemberOf.AcceptVisitor(this);
        TBoldObjectSQLMapper owner = N.MemberOf.ObjectMapper;
        N.MemberMapper = (TBoldMemberSQLMapper?)owner.MemberPersistenceMappers[owner.MemberMapperIndexByMemberIndex(N.MemberIndex)];
        if (N.MemberMapper is TBoldLinkDefaultMapper link)
        {
            N.ObjectMapper = link.OtherEndObjectMapper;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlVariableBinding(TBoldSqlVariableBinding N)
    {
        if (!N.HasObjectMapper && N.TopSortedIndex != -1)
        {
            N.ObjectMapper = (TBoldObjectSQLMapper)fSystemMapper.ObjectPersistenceMappers[N.TopSortedIndex]!;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlVariableReference(TBoldSqlVariableReference N) => N.VariableBinding.AddRef();

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlTypeNode(TBoldSqlTypeNode N) =>
        N.ObjectMapper = (TBoldObjectSQLMapper)fSystemMapper.ObjectPersistenceMappers[N.TopSortedIndex]!;
}
