// Delphi: Source/PMapper/SQL/BoldSqlQueryGenerator.pas
// Port map: CSharp/port/units/BoldSqlQueryGenerator.md
// Generates the queries and where clause fragments of a resolved SQL node tree.

namespace Bold;

/// <summary>Builds each node's query or fragment: literals become fragments, members join or select, symbols build the rest.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlQueryGenerator : TBoldSqlNodeVisitor
{
    private readonly TBoldSQLNameSpace fNameSpace;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlQueryGenerator(TBoldSQLNameSpace NameSpace)
    {
        fNameSpace = NameSpace;
    }

    /// <summary>
    /// A link: a new query (when the owner's query encloses this one) or the owner's, joining the other end's main table
    /// through the embedded column, the link class table, or the other end's column.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void HandleRelation(TBoldSqlMember N)
    {
        if (N.QueryOfMemberOfIsEnclosing())
        {
            N.NewQuery(fNameSpace);
        }
        else
        {
            N.Query = N.MemberOf.RelinquishQuery();
        }

        N.EnsureRetrievalOfMainColumns();
        if (N.MemberMapper is TBoldEmbeddedSingleLinkDefaultMapper)
        {
            // embedded Singlelinks
            var LastJoinLeftColumn = (TBoldSQLColumnDescription)N.MemberMapper.ColumnDescriptions[0];
            TBoldSQLTableReference TableForLastJoinLeft = N.MemberOf.TableReferenceForTable(LastJoinLeftColumn.TableDescription, N.Query, true);
            N.Query.AddJoin(N.MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME), TableForLastJoinLeft.GetColumnReference(LastJoinLeftColumn.SQLName));
        }
        else if (N.MemberMapper is TBoldNonEmbeddedLinkDefaultMapper LinkMapper)
        {
            if (LinkMapper.IsIndirect)
            {
                // Nonembedded Indirect links
                TBoldSQLTableReference TableRefForLink = N.Query.AddTableReference(LinkMapper.LinkClassTablename);
                N.Query.AddJoin(TableRefForLink.GetColumnReference(LinkMapper.ClosestColumnName), N.MemberOf.MainTableRef(N.Query).GetColumnReference(BoldDefs.IDCOLUMN_NAME));
                N.Query.AddJoin(TableRefForLink.GetColumnReference(LinkMapper.RemoteInnerLinkMapper!.MainColumnName), N.MainTableRef().GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            }
            else
            {
                // Nonembedded Direct links
                N.Query.AddJoin(N.MainTableRef().GetColumnReference(LinkMapper.ClosestColumnName), N.MemberOf.MainTableRef(N.Query).GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            }
        }
    }

    /// <summary>An attribute: its column (a boolean integer attribute is "column = 1", in an exists unless enclosed).</summary>
    [Delphi(PortStatus.Ported)]
    private void HandleAttribute(TBoldSqlMember N)
    {
        var MainColumn = (TBoldSQLColumnDescription)N.MemberMapper!.ColumnDescriptions[0];
        TBoldSQLTableReference TableRef = N.MemberOf.TableReferenceForTable(MainColumn.TableDescription, N.MemberOf.Query, true);
        TBoldSQLColumnReference ColumnRef = TableRef.GetColumnReference(MainColumn.SQLName);
        if (N.IsBoolean && N.MemberMapper is TBoldPMInteger)
        {
            // "boolattr = 1"
            TBoldSqlWCF WCF = new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(ColumnRef), new TBoldSQLWCFInteger(1), "=");
            if (!N.QueryOfMemberOfIsEnclosing())
            {
                TBoldSQLQuery Query = N.MemberOf.RelinquishQuery();
                Query.AddWCF(WCF);
                WCF = new TBoldSQLWCFExists(Query, Query.MainTable!);
            }

            N.WCF = WCF;
        }
        else
        {
            N.WCF = new TBoldSQLWCFColumnRef(ColumnRef);
            if (!N.QueryOfMemberOfIsEnclosing())
            {
                N.Query = N.MemberOf.RelinquishQuery();
            }
        }
    }

    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlNode(TBoldSqlNode N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlLiteral(TBoldSqlLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlTypeNode(TBoldSqlTypeNode N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlVariableBinding(TBoldSqlVariableBinding N) { }
    [Delphi(PortStatus.Ported)] protected internal override void VisitTBoldSqlVariableReference(TBoldSqlVariableReference N) { }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlListCoercion(TBoldSqlListCoercion N)
    {
        N.Child.AcceptVisitor(this);
        N.Query = N.Child.RelinquishQuery();
        N.WCF = N.Child.RelinquishWCF();
        N.ObjectMapper = N.Child.ObjectMapper;
        N.CopyTableReferences(N.Child);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlOperation(TBoldSqlOperation N)
    {
        if (N.GetType() == typeof(TBoldSqlOperation))
        {
            N.Args.TraverseList(this);
            N.Symbol!.BuildWCFOrQuery(N, fNameSpace);
        }
    }

    /// <summary>The collection's query and fragment go to the loop variable; then the other arguments, then the symbol builds.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlIteration(TBoldSqlIteration N)
    {
        N.Args[0].AcceptVisitor(this);
        N.LoopVar.AcceptVisitor(this);
        N.LoopVar.Query = N.Args[0].RelinquishQuery();
        N.LoopVar.WCF = N.Args[0].RelinquishWCF();
        N.LoopVar.CopyTableReferences(N.Args[0]);
        for (int i = 1; i < N.Args.Count; i++)
        {
            N.Args[i].AcceptVisitor(this);
        }

        N.CopyTableReferences(N.LoopVar);
        N.Symbol!.BuildWCFOrQuery(N, fNameSpace);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlMember(TBoldSqlMember N)
    {
        N.MemberOf.AcceptVisitor(this);
        System.Diagnostics.Debug.Assert(N.MemberOf.Query is not null);
        if (N.MemberMapper is TBoldSingleColumnMember)
        {
            HandleAttribute(N);
        }
        else if (N.MemberMapper is TBoldEmbeddedSingleLinkDefaultMapper or TBoldNonEmbeddedLinkDefaultMapper)
        {
            HandleRelation(N);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlStrLiteral(TBoldSqlStrLiteral N) => N.WCF = new TBoldSQLWCFString(N.StrValue);

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlFloatLiteral(TBoldSqlFloatLiteral N) => N.WCF = new TBoldSQLWCFFloat(N.FloatValue);

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlDateLiteral(TBoldSqlDateLiteral N) => N.WCF = new TBoldSQLWCFDate(N.DateValue);

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlTimeLiteral(TBoldSqlTimeLiteral N) => N.WCF = new TBoldSQLWCFTime(N.TimeValue);

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlEnumLiteral(TBoldSqlEnumLiteral N) => N.WCF = new TBoldSQLWCFInteger(N.Intvalue);

    [Delphi(PortStatus.Ported)]
    protected internal override void VisitTBoldSqlIntLiteral(TBoldSqlIntLiteral N) => N.WCF = new TBoldSQLWCFInteger(N.IntValue);
}
