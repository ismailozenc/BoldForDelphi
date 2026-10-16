// Delphi: Source/PMapper/SQL/BoldSqlNodes.pas
// Port map: CSharp/port/units/BoldSqlNodes.md
// The SQL node tree made from an OCL condition: each node knows its object mapper, the table references it uses, and
// the where clause fragment or query it builds; visitors resolve and generate them.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlNodeList.Destroy", Note = "Freed the nodes; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlNode.Destroy", Note = "Freed the table references, fragment and query; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlListCoercion.Destroy", Note = "Freed the child; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlOperation.Destroy", Note = "Freed the arguments; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlIteration.Destroy", Note = "Freed the loop variable; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSqlMember.Destroy", Note = "Freed the owner node and qualifier; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodes", Symbol = "TBoldSQLWCFVariable.Destroy", Note = "Empty in Delphi.")]

namespace Bold;

/// <summary>Visits the SQL nodes; each Visit method does nothing here.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlNodeVisitor
{
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlNode(TBoldSqlNode N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlListCoercion(TBoldSqlListCoercion N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlOperation(TBoldSqlOperation N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlIteration(TBoldSqlIteration N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlMember(TBoldSqlMember N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlLiteral(TBoldSqlLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlStrLiteral(TBoldSqlStrLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlFloatLiteral(TBoldSqlFloatLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlEnumLiteral(TBoldSqlEnumLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlIntLiteral(TBoldSqlIntLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlDateLiteral(TBoldSqlDateLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlTimeLiteral(TBoldSqlTimeLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlVariableBinding(TBoldSqlVariableBinding N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlVariableReference(TBoldSqlVariableReference N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldSqlTypeNode(TBoldSqlTypeNode N) { }
}

/// <summary>A list of SQL nodes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlNodeList : List<TBoldSqlNode>
{
    [Delphi(PortStatus.Ported)]
    private TBoldSqlNode GetItem(int index) => base[index];

    [Delphi(PortStatus.Ported)]
    private void PutItem(int index, TBoldSqlNode Value) => base[index] = Value;

    [Delphi(PortStatus.Ported)]
    public new int Add(TBoldSqlNode Item)
    {
        base.Add(Item);
        return Count - 1;
    }

    [Delphi(PortStatus.Ported)]
    public virtual void TraverseList(TBoldSqlNodeVisitor V)
    {
        for (int i = 0; i < Count; i++)
        {
            this[i].AcceptVisitor(V);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSqlNodeList.Items")]
    public new TBoldSqlNode this[int index] { get => GetItem(index); set => PutItem(index, value); }
}

/// <summary>A node of the SQL tree: position, object mapper, table references, and the fragment or query it builds.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlNode
{
    private readonly int fPosition;
    private TBoldObjectSQLMapper? fObjectMapper;
    private readonly TBoldSqlTableReferenceList fTableReferences;
    private TBoldSqlWCF? fWCF;
    private TBoldSQLQuery? fQuery;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlNode(int Position)
    {
        fPosition = Position;
        fTableReferences = new TBoldSqlTableReferenceList(0, TBoldContainerOptions.Empty);
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasObjectMapper() => fObjectMapper is not null;

    [Delphi(PortStatus.Ported)]
    private bool GetHasQuery() => fQuery is not null;

    [Delphi(PortStatus.Ported)]
    protected internal virtual TBoldSqlTableReferenceList GetTableReferences() => fTableReferences;

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldSQLQuery GetQuery()
    {
        System.Diagnostics.Debug.Assert(fQuery is not null);
        return fQuery!;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldObjectSQLMapper GetObjectMapper()
    {
        System.Diagnostics.Debug.Assert(fObjectMapper is not null);
        return fObjectMapper!;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetObjectMapper(TBoldObjectSQLMapper? Value)
    {
        fObjectMapper = Value;
        if (HasObjectMapper && ObjectMapper.SystemPersistenceMapper.MappingInfo.GetAllInstancesMapping(ObjectMapper.ExpressionName).Length > 1)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sChildMappedClassesNotSupported, ObjectMapper.ExpressionName));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetQuery(TBoldSQLQuery? Value) => fQuery = Value;

    /// <summary>
    /// The node's reference to a table of its class in the query (reused when the query has it); a further table is
    /// joined to the first on id (and start time when versioned), and the main table gets the class condition when needed.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual TBoldSQLTableReference TableReferenceForTable(TBoldSQLTableDescription Table, TBoldSQLQuery Query, bool ForceOwntable)
    {
        System.Diagnostics.Debug.Assert(fObjectMapper is not null, SysUtils.Format("Node %s has not ObjectMapper", GetType().Name));
        if (ObjectMapper.AllTables.IndexOf(Table) == -1)
        {
            throw new EBoldInternal(SysUtils.Format("Table %s does not belong to class %s", Table.SQLName, ObjectMapper.ExpressionName));
        }

        // if the table reference has been used before, by the same query, then reuse it.
        for (int i = 0; i < fTableReferences.Count; i++)
        {
            if (fTableReferences[i].TableDescription == Table && Query.HastableReferenceInList(fTableReferences[i]))
            {
                return fTableReferences[i];
            }
        }

        TBoldSQLTableReference result = Query.AddTableReference(Table.SQLName);
        fTableReferences.Add(result);
        if (fTableReferences.Count > 1)
        {
            Query.AddJoin(fTableReferences[0].GetColumnReference(BoldDefs.IDCOLUMN_NAME), result.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            if (fTableReferences[0].TableDescription.Versioned && result.TableDescription.Versioned)
            {
                Query.AddJoin(fTableReferences[0].GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME), result.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME));
            }
        }

        if (Table == ObjectMapper.MainTable)
        {
            TBoldAllInstancesMappingInfo[] MappingInfos = ObjectMapper.SystemPersistenceMapper.MappingInfo.GetAllInstancesMapping(ObjectMapper.ExpressionName);
            if (MappingInfos[0].ClassIdRequired)
            {
                var TypeIDWCF = new TBoldSQLWCFGenericExpression("(" + ((TBoldObjectDefaultMapper)ObjectMapper).SubClassesID + ")");
                var TypeColRefWCF = new TBoldSQLWCFColumnRef(result.GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
                Query.AddWCF(new TBoldSQLWCFBinaryInfix(TypeColRefWCF, TypeIDWCF, "IN")); // ensures the maintable as first table;
            }
        }

        return result;
    }

    /// <summary>Takes the node's query away from it.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual TBoldSQLQuery RelinquishQuery()
    {
        System.Diagnostics.Debug.Assert(fQuery is not null);
        TBoldSQLQuery result = fQuery!;
        fQuery = null;
        return result;
    }

    /// <summary>Takes the node's fragment away from it, or its query as a subquery selecting the main table's ids.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual TBoldSqlWCF? RelinquishWCF()
    {
        if (fWCF is not null)
        {
            TBoldSqlWCF result = fWCF;
            fWCF = null;
            return result;
        }

        if (fQuery is not null)
        {
            TBoldSQLTableReference QueryMainTable = fQuery.MainTable!;
            fQuery.ClearColumnsToRetrieve();
            fQuery.AddColumnToRetrieve(QueryMainTable.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
            var result = new TBoldSQLWCFWithQuery(fQuery);
            fQuery = null;
            return result;
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public virtual void AcceptVisitor(TBoldSqlNodeVisitor V) => V.VisitTBoldSqlNode(this);

    /// <summary>A new select of the node's class, retrieving the main table's id and type.</summary>
    [Delphi(PortStatus.Ported)]
    public void NewQuery(TBoldSQLNameSpace NameSpace)
    {
        System.Diagnostics.Debug.Assert(fQuery is null);
        fQuery = new TBoldSQLQuery(TBoldSQLQueryMode.qmSelect, ObjectMapper.SystemPersistenceMapper.PSSystemDescription, ObjectMapper.SystemPersistenceMapper.SQLDataBaseConfig, NameSpace);
        EnsureRetrievalOfMainColumns();
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureRetrievalOfMainColumns()
    {
        Query.ClearColumnsToRetrieve();
        TBoldSQLTableReference MaintableRef = TableReferenceForTable(ObjectMapper.MainTable!, Query, true);
        Query.AddColumnToRetrieve(MaintableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
        Query.AddColumnToRetrieve(MaintableRef.GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
    }

    [Delphi(PortStatus.Ported)]
    public void CopyTableReferences(TBoldSqlNode node)
    {
        TBoldSqlTableReferenceList references = node.GetTableReferences();
        for (int i = 0; i < references.Count; i++)
        {
            fTableReferences.Add(references[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSQLTableReference MainTableRef(TBoldSQLQuery? Query = null) => TableReferenceForTable(ObjectMapper.MainTable!, Query ?? this.Query, true);

    [Delphi(PortStatus.Ported)] public int Position => fPosition;
    [Delphi(PortStatus.Ported)] public TBoldObjectSQLMapper ObjectMapper { get => GetObjectMapper(); set => SetObjectMapper(value); }
    [Delphi(PortStatus.Ported)] public TBoldSqlWCF? WCF { get => fWCF; set => fWCF = value; }
    [Delphi(PortStatus.Ported)] public TBoldSQLQuery Query { get => GetQuery(); set => SetQuery(value); }
    [Delphi(PortStatus.Ported)] public bool HasQuery => GetHasQuery();
    [Delphi(PortStatus.Ported)] public bool HasObjectMapper => GetHasObjectMapper();
}

/// <summary>A class used as a value (as in oclAsType).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlTypeNode : TBoldSqlNode
{
    private readonly string fTypeName;
    private readonly int fTopSortedIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlTypeNode(int Position, string TypeName, int TopSortedIndex) : base(Position)
    {
        fTypeName = TypeName;
        fTopSortedIndex = TopSortedIndex;
    }

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlTypeNode(this);
    }

    [Delphi(PortStatus.Ported)] public string TypeName => fTypeName;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
}

/// <summary>A single value seen as a list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlListCoercion : TBoldSqlNode
{
    private readonly TBoldSqlNode fChild;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlListCoercion(int Position, TBoldSqlNode Child) : base(Position)
    {
        fChild = Child;
    }

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlListCoercion(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldSqlNode Child => fChild;
}

/// <summary>An operation with arguments, resolved to a SQL symbol.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlOperation : TBoldSqlNode
{
    private readonly TBoldSqlNodeList fArgs = new();
    private readonly string fOperationName;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlOperation(int Position, string OperationName) : base(Position)
    {
        fOperationName = OperationName;
    }

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlOperation(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldSqlNodeList Args => fArgs;
    [Delphi(PortStatus.Ported)] public string OperationName => fOperationName;
    [Delphi(PortStatus.Ported)] public TBoldSqlSymbol? Symbol { get; set; }
}

/// <summary>An iteration (select, reject, exists, ...) with its loop variable.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlIteration : TBoldSqlOperation
{
    private readonly TBoldSqlVariableBinding fLoopVar;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlIteration(int Position, string OperationName, TBoldSqlVariableBinding LoopVar) : base(Position, OperationName)
    {
        fLoopVar = LoopVar;
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldSQLTableReference TableReferenceForTable(TBoldSQLTableDescription Table, TBoldSQLQuery Query, bool ForceOwntable) =>
        LoopVar.TableReferenceForTable(Table, Query, ForceOwntable);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlIteration(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldSqlVariableBinding LoopVar => fLoopVar;
}

/// <summary>A member of another node, with its member mapper.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlMember : TBoldSqlNode
{
    private readonly int fMemberIndex;
    private readonly string fMemberName;
    private readonly TBoldSqlNode fmemberOf;
    private readonly TBoldSqlNodeList fQualifier = new();
    private readonly bool fIsBoolean;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlMember(int Position, string memberName, int MemberIndex, TBoldSqlNode MemberOf, bool IsBoolean) : base(Position)
    {
        fMemberName = memberName;
        fMemberIndex = MemberIndex;
        fmemberOf = MemberOf;
        fIsBoolean = IsBoolean;
    }

    /// <summary>The table reference of the owner variable (unless forced to own), else this node's own.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldSQLTableReference TableReferenceForTable(TBoldSQLTableDescription Table, TBoldSQLQuery Query, bool ForceOwnTable) =>
        MemberOf is TBoldSqlVariableReference && !ForceOwnTable
            ? MemberOf.TableReferenceForTable(Table, Query, ForceOwnTable)
            : base.TableReferenceForTable(Table, Query, ForceOwnTable);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlMember(this);
    }

    /// <summary>Whether the query of the owner node will enclose this node (a loop variable), or the query can be taken.</summary>
    [Delphi(PortStatus.Ported)]
    public bool QueryOfMemberOfIsEnclosing() => MemberOf is TBoldSqlVariableReference reference && reference.VariableBinding.IsLoopVar;

    [Delphi(PortStatus.Ported)] public TBoldSqlNode MemberOf => fmemberOf;
    [Delphi(PortStatus.Ported)] public string MemberName => fMemberName;
    [Delphi(PortStatus.Ported)] public int MemberIndex => fMemberIndex;
    [Delphi(PortStatus.Ported)] public TBoldSqlNodeList Qualifier => fQualifier;
    [Delphi(PortStatus.Ported)] public TBoldMemberSQLMapper? MemberMapper { get; set; }
    [Delphi(PortStatus.Ported)] public bool IsBoolean => fIsBoolean;
}

/// <summary>A variable (self, an external variable or a loop variable) and its value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlVariableBinding : TBoldSqlNode
{
    private readonly string fVariableName;
    private readonly int fTopSortedIndex;
    private int fRefCount;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlVariableBinding(int Position, string VariableName, int TopSortedIndex) : base(Position)
    {
        fVariableName = VariableName;
        fTopSortedIndex = TopSortedIndex;
    }

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlVariableBinding(this);
    }

    /// <summary>Counts a reference; external variables can be referenced only once.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddRef()
    {
        fRefCount++;
        if (fRefCount > 1 && !IsLoopVar)
        {
            throw new EBold(BoldPMConsts.sExternalVarsCanOnlyBereferencedOnce);
        }
    }

    [Delphi(PortStatus.Ported)] public string VariableName => fVariableName;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
    [Delphi(PortStatus.Ported)] public object? ExternalVarvalue { get; set; }
    [Delphi(PortStatus.Ported)] public bool IsLoopVar { get; set; }
    [Delphi(PortStatus.Ported)] public bool IsExternal { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList? Context { get; set; }
}

/// <summary>A use of a variable; mapper, query and table references are the binding's.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlVariableReference : TBoldSqlNode
{
    private readonly TBoldSqlVariableBinding fVariableBinding;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlVariableReference(int Position, TBoldSqlVariableBinding VariableBinding) : base(Position)
    {
        fVariableBinding = VariableBinding;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldSqlTableReferenceList GetTableReferences() => fVariableBinding.GetTableReferences();

    [Delphi(PortStatus.Ported)]
    protected override TBoldSQLQuery GetQuery() => VariableBinding.Query;

    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectSQLMapper GetObjectMapper() => VariableBinding.ObjectMapper;

    [Delphi(PortStatus.Ported)]
    protected override void SetObjectMapper(TBoldObjectSQLMapper? Value) => throw new EBoldInternal("Can not set Objectmapper in a variablereference");

    [Delphi(PortStatus.Ported)]
    protected override void SetQuery(TBoldSQLQuery? Value) => throw new EBoldInternal("Can not set Query in a variablereference, try relinquish");

    [Delphi(PortStatus.Ported)]
    public override TBoldSQLTableReference TableReferenceForTable(TBoldSQLTableDescription Table, TBoldSQLQuery Query, bool ForceOwntable) =>
        VariableBinding.TableReferenceForTable(Table, Query, ForceOwntable);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlVariableReference(this);
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldSQLQuery RelinquishQuery() => VariableBinding.RelinquishQuery();

    /// <summary>The variable as a parameter.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldSqlWCF? RelinquishWCF() => new TBoldSQLWCFVariable(VariableBinding);

    [Delphi(PortStatus.Ported)]
    public bool IsExternalVariable() => VariableBinding.Context is not null;

    [Delphi(PortStatus.Ported)] public TBoldSqlVariableBinding VariableBinding => fVariableBinding;
}

/// <summary>A literal value.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSqlLiteral(int Position) : TBoldSqlNode(Position)
{
    [Delphi(PortStatus.Ported)]
    protected abstract string GetAsString();

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
}

[Delphi(PortStatus.Ported)]
public class TBoldSqlStrLiteral : TBoldSqlLiteral
{
    private readonly string fStrValue;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlStrLiteral(int Position, string StrValue) : base(Position)
    {
        fStrValue = StrValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => StrValue;

    /// <summary>Visits only as a string literal (Delphi does not call the inherited visit here).</summary>
    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V) => V.VisitTBoldSqlStrLiteral(this);

    [Delphi(PortStatus.Ported)] public string StrValue => fStrValue;
}

[Delphi(PortStatus.Ported)]
public class TBoldSqlDateLiteral : TBoldSqlLiteral
{
    private readonly TDateTime fDateValue;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlDateLiteral(int Position, TDateTime DateValue) : base(Position)
    {
        fDateValue = DateValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => SysUtils.DateToStr(fDateValue);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlDateLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public TDateTime DateValue => fDateValue;
}

[Delphi(PortStatus.Ported)]
public class TBoldSqlTimeLiteral : TBoldSqlLiteral
{
    private readonly TDateTime fTimeValue;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlTimeLiteral(int Position, TDateTime TimeValue) : base(Position)
    {
        fTimeValue = TimeValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => SysUtils.TimeToStr(TimeValue);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlTimeLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public TDateTime TimeValue => fTimeValue;
}

[Delphi(PortStatus.Ported)]
public class TBoldSqlFloatLiteral : TBoldSqlLiteral
{
    private readonly double fFloatValue;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlFloatLiteral(int Position, double FloatValue) : base(Position)
    {
        fFloatValue = FloatValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => SysUtils.Format("%e", FloatValue);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlFloatLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public double FloatValue => fFloatValue;
}

[Delphi(PortStatus.Ported)]
public class TBoldSqlIntLiteral : TBoldSqlLiteral
{
    private readonly int fIntValue;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlIntLiteral(int Position, int IntValue) : base(Position)
    {
        fIntValue = IntValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => SysUtils.IntToStr(IntValue);

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlIntLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public int IntValue => fIntValue;
}

/// <summary>An enum literal by name; its integer value is set when resolved.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlEnumLiteral : TBoldSqlLiteral
{
    private readonly string fName;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlEnumLiteral(int Position, string Name) : base(Position)
    {
        fName = Name;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetAsString() => Name;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldSqlNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldSqlEnumLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public string Name => fName;
    [Delphi(PortStatus.Ported)] public int Intvalue { get; set; }
}

/// <summary>An OCL operation that can be done in SQL: it resolves the mapper of its result and builds its fragment or query.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSqlSymbol
{
    [Delphi(PortStatus.Ported)]
    protected abstract string GetName();

    [Delphi(PortStatus.Ported)]
    protected virtual string GetSQLName() => Name;

    [Delphi(PortStatus.Ported)]
    public virtual TBoldObjectSQLMapper? ResolveObjectMapper(TBoldSqlOperation OperationNode) => null;

    [Delphi(PortStatus.Ported)]
    public virtual void BuildWCFOrQuery(TBoldSqlOperation OperationNode, TBoldSQLNameSpace NameSpace) =>
        throw new EBoldInternal(SysUtils.Format("%s can not build WCFs or Queries...", Name));

    [Delphi(PortStatus.Ported)] public string Name => GetName();
    [Delphi(PortStatus.Ported)] public string SQLName => GetSQLName();
}

/// <summary>A variable as a query parameter.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLWCFVariable : TBoldSqlWCF
{
    private TParam? fParam;
    private readonly TBoldSqlVariableBinding fVariableBinding;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLWCFVariable(TBoldSqlVariableBinding VariableBinding)
    {
        fVariableBinding = VariableBinding;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetAsString(TBoldSQLQuery Query)
    {
        fParam ??= Query.AddParam();
        fParam.Name = fVariableBinding.VariableName;
        fParam.ParamType = TParamType.ptInput;
        fParam.Value = fVariableBinding.ExternalVarvalue;
        return ":" + fParam.Name;
    }
}
