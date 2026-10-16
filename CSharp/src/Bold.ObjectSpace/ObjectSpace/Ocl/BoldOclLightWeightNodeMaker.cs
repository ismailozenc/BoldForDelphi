// Delphi: Source/ObjectSpace/Ocl/BoldOclLightWeightNodeMaker.pas
// Port map: CSharp/port/units/BoldOclLightWeightNodeMaker.md

using Bold;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodeMaker", Symbol = "TBoldOLWNodeMaker.Destroy", Note = "Freed the binding lists and the external bindings left after a failure; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// Copies a checked OCL tree into light-weight nodes for evaluation in the persistence layer, or records why it cannot
/// (transient members, methods, collection literals, a result that is not an object list).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWNodeMaker : TBoldOclVisitor
{
    private bool fFailed;
    private int fFailurePosition;
    private string fFailureReason = "";
    private TBoldOLWNode? fRootNode;
    private readonly List<TBoldOLWVariableBinding> fOLWVarBindings = [];
    private readonly List<TBoldOclVariableBinding> fVarBindings = [];
    private readonly TBoldOLWNodeList fExternalVarBindings = new() { OwnsObjects = false };
    private TBoldOclNode? fOclRootNode;
    private readonly TBoldOclEnvironment? fEnv;

    [Delphi(PortStatus.Ported)]
    public TBoldOLWNodeMaker(TBoldOclNode OclRootNode, TBoldSystemTypeInfo SystemTypeInfo, TBoldSystem? System, TBoldOclEnvironment? Env)
    {
        fOclRootNode = OclRootNode;
        fEnv = Env;
        if (OclRootNode.BoldType is not TBoldListTypeInfo || !OclRootNode.BoldType.ConformsTo(SystemTypeInfo.RootClassTypeInfo.ListTypeInfo))
        {
            SetFailure(0, SysUtils.Format(BoldCoreConsts.sPSResultMustBeObjectList, OclRootNode.BoldType?.AsString ?? ""));
        }

        if (System is null)
        {
            SetFailure(0, BoldCoreConsts.sPSEvalrequiresSystem);
        }
    }

    [Delphi(PortStatus.Ported, Note = "A List of the light-weight bindings; a TList in Delphi.")] protected List<TBoldOLWVariableBinding> OLWVarBindings => fOLWVarBindings;
    [Delphi(PortStatus.Ported, Note = "A List of the OCL bindings; a TList in Delphi.")] protected List<TBoldOclVariableBinding> VarBindings => fVarBindings;

    [Delphi(PortStatus.Ported)]
    protected void SetFailure(int Position, string Message)
    {
        fFailed = true;
        fFailureReason = Message;
        fFailurePosition = Position;
        fOclRootNode = null;
    }

    /// <summary>The light-weight binding made for the OCL binding, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOLWVariableBinding? OLWBindingForVarBinding(TBoldOclVariableBinding? VarBinding)
    {
        int p = VarBinding is null ? -1 : fVarBindings.IndexOf(VarBinding);
        return p != -1 ? fOLWVarBindings[p] : null;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclCollectionLiteral(TBoldOclCollectionLiteral N) => SetFailure(N.Position, BoldCoreConsts.sCollectionLiteralsNotSupported);

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclEnumLiteral(TBoldOclEnumLiteral N) => fRootNode = new TBoldOLWEnumLiteral(N.Position, N.name);

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIntLiteral(TBoldOclIntLiteral N) => fRootNode = new TBoldOLWIntLiteral(N.Position, N.IntValue);

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIteration(TBoldOclIteration N)
    {
        N.LoopVar!.AcceptVisitor(this);
        var OLWIteration = new TBoldOLWIteration(N.Position, N.OperationName, (TBoldOLWVariableBinding?)RootNode);
        for (int i = 0; i < N.Args!.Count(); i++)
        {
            N.Args[i]!.AcceptVisitor(this);
            OLWIteration.Args.Add(RootNode!);
        }

        fRootNode = OLWIteration;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclListCoercion(TBoldOCLListCoercion N)
    {
        N.Child!.AcceptVisitor(this);
        fRootNode = new TBoldOLWListCoercion(N.Position, RootNode);
    }

    /// <summary>Nothing: an abstract class.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclLiteral(TBoldOclLiteral N)
    {
    }

    /// <summary>A member; transient members fail, except link roles whose main role is persistent.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMember(TBoldOclMember N)
    {
        base.VisitTBoldOclMember(N);
        N.MemberOf!.AcceptVisitor(this);
        bool IsBoolean = N.BoldType!.ConformsTo(((TBoldSystemTypeInfo)N.BoldType.SystemTypeInfo!).AttributeTypeInfoByExpressionName("Boolean"));
        var OLWMember = new TBoldOLWMember(N.Position, N.MemberName, N.MemberIndex, RootNode, IsBoolean);

        if (!N.RTInfo!.Persistent)
        {
            bool EffectivePersistent = false;
            if (N.RTInfo is TBoldRoleRTInfo RoleRTInfo && RoleRTInfo.RoleType == TBoldRoleType.rtLinkRole)
            {
                var MainRole = (TBoldRoleRTInfo)RoleRTInfo.ClassTypeInfo.AllMembers[RoleRTInfo.IndexOfMainRole];
                EffectivePersistent = MainRole.Persistent;
            }

            if (!EffectivePersistent)
            {
                SetFailure(N.Position, SysUtils.Format(BoldCoreConsts.sTransientMembersCannoteUsed, N.RTInfo.ExpressionName));
            }
        }

        if (N.Qualifier is not null)
        {
            for (int i = 0; i < N.Qualifier.Count(); i++)
            {
                N.Qualifier[i]!.AcceptVisitor(this);
                OLWMember.Qualifier.Add(RootNode!);
            }
        }

        fRootNode = OLWMember;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMethod(TBoldOclMethod N) => SetFailure(N.Position, BoldCoreConsts.sMethodsNotSupported);

    /// <summary>Nothing: an abstract class.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclNode(TBoldOclNode N)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclNumericLiteral(TBoldOclNumericLiteral N)
    {
        if (N.GetType() == typeof(TBoldOclNumericLiteral))
        {
            fRootNode = new TBoldOLWFloatLiteral(N.Position, N.FloatValue);
        }
    }

    /// <summary>An operation; in = and &lt;&gt;, an enum literal compared to a value set gets the integer value of the name.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclOperation(TBoldOclOperation N)
    {
        if (N.GetType() != typeof(TBoldOclOperation))
        {
            return;
        }

        var OLWOperation = new TBoldOLWOperation(N.Position, N.OperationName);
        for (int i = 0; i < N.Args!.Count(); i++)
        {
            N.Args[i]!.AcceptVisitor(this);
            OLWOperation.Args.Add(RootNode!);
        }

        fRootNode = OLWOperation;
        if (N.OperationName is not ("=" or "<>"))
        {
            return;
        }

        TBoldOLWEnumLiteral? Literal = null;
        string LiteralName = "";
        TBoldElementTypeInfo? LiteralType = null;
        if (N.Args[0] is TBoldOclEnumLiteral enum0 && N.Args[1] is not TBoldOclEnumLiteral)
        {
            LiteralName = enum0.name;
            LiteralType = N.Args[1]!.BoldType;
            Literal = (TBoldOLWEnumLiteral)OLWOperation.Args[0];
        }

        if (N.Args[1] is TBoldOclEnumLiteral enum1 && N.Args[0] is not TBoldOclEnumLiteral)
        {
            LiteralName = enum1.name;
            LiteralType = N.Args[0]!.BoldType;
            Literal = (TBoldOLWEnumLiteral)OLWOperation.Args[1];
        }

        TBoldAttributeTypeInfo? BooleanType = ((TBoldSystemTypeInfo)N.BoldType!.SystemTypeInfo!).AttributeTypeInfoByExpressionName("Boolean");
        if (N.Args[0]!.BoldType!.ConformsTo(BooleanType) && N.Args[1]!.BoldType!.ConformsTo(BooleanType))
        {
            SetFailure(N.Position, BoldCoreConsts.sUseBooleanOperations);
        }

        if (Literal is null)
        {
            return;
        }

        if (LiteralType!.ConformsTo(BooleanType))
        {
            SetFailure(N.Position, BoldCoreConsts.sUseBooleanOperationsWithLiterals);
        }

        if (LiteralType.ConformsTo(((TBoldSystemTypeInfo)LiteralType.SystemTypeInfo!).AttributeTypeInfoByExpressionName("ValueSet")))
        {
            var ValueSet = (TBAValueSet)TBoldMemberFactory.CreateMemberFromBoldType(LiteralType);
            if (ValueSet.Values.FindByString(BoldDefs.brDefault, LiteralName) is not null)
            {
                ValueSet.AsString = LiteralName;
                Literal.Intvalue = ValueSet.AsInteger;
            }
            else
            {
                SetFailure(N.Position, SysUtils.Format(BoldCoreConsts.sEnumNameNotValid, LiteralName, LiteralType.ExpressionName));
            }
        }
        else
        {
            SetFailure(N.Position, SysUtils.Format(BoldCoreConsts.sEnumComparedToNonEnum, LiteralName, LiteralType.ExpressionName));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclStrLiteral(TBoldOclStrLiteral N)
    {
        base.VisitTBoldOclStrLiteral(N);
        fRootNode = new TBoldOLWStrLiteral(N.Position, N.StrValue);
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclTypeNode(TBoldOclTypeNode N)
    {
        if (N.Value is TBoldClassTypeInfo ClassTypeInfo)
        {
            fRootNode = new TBoldOLWTypeNode(N.Position, N.typeName, ClassTypeInfo.TopSortedIndex);
        }
        else
        {
            SetFailure(N.Position, SysUtils.Format(BoldCoreConsts.sTypeNotSupported, N.typeName));
        }
    }

    /// <summary>A variable with a value or of a class type; an object's value is its id as an integer (-1 when not stored).</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclVariableBinding(TBoldOclVariableBinding N)
    {
        if (N.Value is not null || N.BoldType is TBoldClassTypeInfo)
        {
            int TopSortedIndex = -1;
            if (N.BoldType is TBoldClassTypeInfo classType)
            {
                TopSortedIndex = classType.TopSortedIndex;
            }

            if (N.BoldType is TBoldListTypeInfo { ListElementTypeInfo: TBoldClassTypeInfo elementType })
            {
                TopSortedIndex = elementType.TopSortedIndex;
            }

            var VarBind = new TBoldOLWVariableBinding(N.Position, N.VariableName, TopSortedIndex);
            if (N.BoldType is TBoldClassTypeInfo)
            {
                TBoldObject? Obj = N.Value switch
                {
                    TBoldObjectReference reference => reference.BoldObject,
                    TBoldObject o => o,
                    _ => null,
                };
                VarBind.ExternalVarvalue = Obj is not null && Obj.BoldObjectLocator!.BoldObjectID.IsStorable
                    ? SysUtils.StrToIntDef(Obj.BoldObjectLocator!.BoldObjectID.AsString, -1)
                    : -1;
            }
            else if (N.Value is TBoldAttribute attribute)
            {
                VarBind.ExternalVarvalue = attribute.GetAsVariant();
            }

            VarBindings.Add(N);
            OLWVarBindings.Add(VarBind);
            fRootNode = VarBind;
        }
        else
        {
            fRootNode = new TBoldOLWVariableBinding(N.Position, N.VariableName, -1);
            SetFailure(N.Position, SysUtils.Format(BoldCoreConsts.sLoopVariablesMustBeClassType, N.BoldType?.AsString ?? ""));
        }
    }

    /// <summary>A reference; a variable not bound in the expression is made an external binding.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclVariableReference(TBoldOclVariableReference N)
    {
        TBoldOLWVariableBinding? VarBind = OLWBindingForVarBinding(N.VariableBinding);
        if (VarBind is null)
        {
            N.VariableBinding!.AcceptVisitor(this);
            VarBind = (TBoldOLWVariableBinding?)RootNode;
            ExternalVarBindings.Add(VarBind!);
        }

        fRootNode = new TBoldOLWVariableReference(N.Position, VarBind);
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclDateLiteral(TBoldOclDateLiteral N) => fRootNode = new TBoldOLWDateLiteral(N.Position, N.DateValue);

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclTimeLiteral(TBoldOclTimeLiteral N) => fRootNode = new TBoldOLWTimeLiteral(N.Position, N.TimeValue);

    [Delphi(PortStatus.Ported)] public TBoldOLWNode? RootNode => fRootNode;
    [Delphi(PortStatus.Ported)] public bool Failed => fFailed;
    [Delphi(PortStatus.Ported)] public string FailureReason => fFailureReason;
    [Delphi(PortStatus.Ported)] public int FailurePosition => fFailurePosition;

    [Delphi(PortStatus.Ported, Note = "A non-owning TBoldOLWNodeList; a TList in Delphi.")]
    public TBoldOLWNodeList ExternalVarBindings => fExternalVarBindings;
}
