// Delphi: Source/PMapper/SQL/BoldSqlNodeMaker.pas
// Port map: CSharp/port/units/BoldSqlNodeMaker.md
// Makes the SQL node tree from an OCL condition's light-weight node tree, sharing one binding per variable.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSqlNodeMaker", Symbol = "TBoldSqlNodeMaker.Destroy", Note = "Released the bindings and freed the tree; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Visits the light-weight OCL nodes and makes the matching SQL nodes; the last one made is the root.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSqlNodeMaker : TBoldOLWNodeVisitor
{
    private readonly TBoldOclCondition fCondition;
    private readonly TBoldSqlNodeList fSqlVarBindings = new();
    private readonly List<TBoldOLWVariableBinding> fOLWVarBindings = [];
    private TBoldSqlNode? fRootNode;

    [Delphi(PortStatus.Ported)]
    public TBoldSqlNodeMaker(TBoldOclCondition Condition)
    {
        fCondition = Condition;
    }

    [Delphi(PortStatus.Ported, Note = "A list of the light-weight bindings (not owned).")]
    protected List<TBoldOLWVariableBinding> OLWVarBindings => fOLWVarBindings;

    /// <summary>Makes the bindings of the condition's environment (external variables), then the tree of its root node.</summary>
    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        for (int i = 0; i < fCondition.Env.Count; i++)
        {
            fCondition.Env[i].AcceptVisitor(this);
            ((TBoldSqlVariableBinding)SQLVarBindings[SQLVarBindings.Count - 1]).IsExternal = true;
        }

        fCondition.RootNode!.AcceptVisitor(this);
    }

    // abstract class
    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWNode(TBoldOLWNode N)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWListCoercion(TBoldOLWListCoercion N)
    {
        N.Child!.AcceptVisitor(this);
        fRootNode = new TBoldSqlListCoercion(N.Position, RootNode!);
    }

    /// <summary>A plain operation (iterations are made in VisitTBoldOLWIteration).</summary>
    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWOperation(TBoldOLWOperation N)
    {
        if (N.GetType() == typeof(TBoldOLWOperation))
        {
            var SqlOperation = new TBoldSqlOperation(N.Position, N.OperationName);
            for (int i = 0; i < N.Args.Count; i++)
            {
                N.Args[i].AcceptVisitor(this);
                SqlOperation.Args.Add(RootNode!);
            }

            fRootNode = SqlOperation;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWIteration(TBoldOLWIteration N)
    {
        N.LoopVar!.AcceptVisitor(this);
        var SqlIteration = new TBoldSqlIteration(N.Position, N.OperationName, (TBoldSqlVariableBinding)RootNode!);
        for (int i = 0; i < N.Args.Count; i++)
        {
            N.Args[i].AcceptVisitor(this);
            SqlIteration.Args.Add(RootNode!);
        }

        fRootNode = SqlIteration;
    }

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWMember(TBoldOLWMember N)
    {
        N.MemberOf!.AcceptVisitor(this);
        var SqlMember = new TBoldSqlMember(N.Position, N.MemberName, N.MemberIndex, RootNode!, N.IsBoolean);
        for (int i = 0; i < N.Qualifier.Count; i++)
        {
            N.Qualifier[i].AcceptVisitor(this);
            SqlMember.Qualifier.Add(RootNode!);
        }

        fRootNode = SqlMember;
    }

    // Abstract class
    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWLiteral(TBoldOLWLiteral N)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWStrLiteral(TBoldOLWStrLiteral N) => fRootNode = new TBoldSqlStrLiteral(N.Position, N.StrValue);

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWFloatLiteral(TBoldOLWFloatLiteral N) => fRootNode = new TBoldSqlFloatLiteral(N.Position, N.FloatValue);

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWEnumLiteral(TBoldOLWEnumLiteral N) => fRootNode = new TBoldSqlEnumLiteral(N.Position, N.Name);

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWDateLiteral(TBoldOLWDateLiteral N) => fRootNode = new TBoldSqlDateLiteral(N.Position, N.DateValue);

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWTimeLiteral(TBoldOLWTimeLiteral N) => fRootNode = new TBoldSqlTimeLiteral(N.Position, N.TimeValue);

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWIntLiteral(TBoldOLWIntLiteral N) => fRootNode = new TBoldSqlIntLiteral(N.Position, N.IntValue);

    /// <summary>The SQL binding of the variable's name, made (with the variable's value) the first time.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWVariableBinding(TBoldOLWVariableBinding N)
    {
        TBoldSqlVariableBinding? VarBind = null;
        for (int i = 0; i < SQLVarBindings.Count; i++)
        {
            if (((TBoldSqlVariableBinding)SQLVarBindings[i]).VariableName == N.VariableName)
            {
                VarBind = (TBoldSqlVariableBinding)SQLVarBindings[i];
            }
        }

        if (VarBind is null)
        {
            VarBind = new TBoldSqlVariableBinding(N.Position, N.VariableName, N.TopSortedIndex) { ExternalVarvalue = N.ExternalVarvalue };
            OLWVarBindings.Add(N);
            SQLVarBindings.Add(VarBind);
        }

        fRootNode = VarBind;
    }

    /// <summary>A reference to the variable's SQL binding; a binding first met here is external.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWVariableReference(TBoldOLWVariableReference N)
    {
        TBoldSqlVariableBinding? VarBind = SQLBindingForVarBinding(N.VariableBinding!);
        if (VarBind is null)
        {
            N.VariableBinding!.AcceptVisitor(this);
            VarBind = (TBoldSqlVariableBinding)RootNode!;
            VarBind.IsExternal = true;
            if (SQLVarBindings.IndexOf(VarBind) == -1)
            {
                OLWVarBindings.Add(N.VariableBinding);
                SQLVarBindings.Add(VarBind);
            }
        }

        fRootNode = new TBoldSqlVariableReference(N.Position, VarBind);
    }

    [Delphi(PortStatus.Ported)]
    protected override void VisitTBoldOLWTypeNode(TBoldOLWTypeNode N) => fRootNode = new TBoldSqlTypeNode(N.Position, N.TypeName, N.TopSortedIndex);

    [Delphi(PortStatus.Ported)]
    public TBoldSqlVariableBinding? SQLBindingForVarBinding(TBoldOLWVariableBinding VarBinding)
    {
        for (int i = 0; i < fOLWVarBindings.Count; i++)
        {
            if (fOLWVarBindings[i].VariableName == VarBinding.VariableName)
            {
                return (TBoldSqlVariableBinding)fSqlVarBindings[i];
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)] public TBoldSqlNode? RootNode => fRootNode;
    [Delphi(PortStatus.Ported)] public TBoldSqlNodeList SQLVarBindings => fSqlVarBindings;
}
