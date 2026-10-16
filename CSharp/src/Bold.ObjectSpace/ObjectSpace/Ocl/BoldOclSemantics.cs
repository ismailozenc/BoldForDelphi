// Delphi: Source/ObjectSpace/Ocl/BoldOclSemantics.pas
// Port map: CSharp/port/units/BoldOclSemantics.md
// The semantic check: resolves names to members, variables, types and symbols, types every node, inserts list
// coercions and implicit collects, and marks constant subtrees.

namespace Bold;

/// <summary>Descriptions of the deduce methods, used in error messages.</summary>
public static partial class BoldOclSemantics
{
    internal static readonly string[] BoldOclDeduceMethodName =
    [
        "No DeduceMethod", "Same type as loopvariable", "Same type as arg1",
        "Same type as listelement of arg1", "Same type as arg2", "Same type as arg3",
        "Least common supertype of arg1 and arg2", "Least common supertype of arg2 and arg3",
        "Same type as listelement of arg2", "Type is ObjectList", "Type is Metatype", "Type is arg2 (typecast)", "Arg1 as a list", "a list with the type of Arg2",
    ];
}

/// <summary>Checks and types a syntax tree in an environment; nodes may be replaced (see Traverse).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclSemanticsVisitor : TBoldOclVisitor
{
    private readonly TBoldSystemTypeInfo? CurrentSystemTypeInfo;
    private TBoldOclNode? replacementNode;
    private bool ReSubscribe;
    private readonly TBoldSymbolDictionary SymbolTable;
    private readonly TBoldOclEnvironment fEnv;
    private readonly TBoldEvaluator? fEvaluator;
    private bool fIgnoreNelCompatibility;

    [Delphi(PortStatus.Ported)]
    public TBoldOclSemanticsVisitor(TBoldSystemTypeInfo? Model, TBoldEvaluator? Evaluator, TBoldSymbolDictionary SymTab, TBoldOclEnvironment Env)
    {
        CurrentSystemTypeInfo = Model;
        fEnv = Env;
        fEvaluator = Evaluator;
        SymbolTable = SymTab;
    }

    private TBoldSystemTypeInfo System => CurrentSystemTypeInfo!;

    [Delphi(PortStatus.Ported)]
    public void PushResubscribe(ref bool ReSubscribe, ref bool OldReSubscribe)
    {
        OldReSubscribe = ReSubscribe;
        ReSubscribe = true;
    }

    /// <summary>Raises unless the argument conforms to the formal type; marks it for list coercion when a list is expected.</summary>
    [Delphi(PortStatus.Ported)]
    public void CheckArgumentType(TBoldOclNode actArg, TBoldElementTypeInfo? FormArg)
    {
        if (FormArg is not null && !actArg.BoldType!.ConformsTo(FormArg))
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeNoConform, actArg.Position, actArg.BoldType.ExpressionName, FormArg.ExpressionName));
        }

        if (actArg.BoldType is not TBoldListTypeInfo && FormArg is TBoldListTypeInfo)
        {
            actArg.NeedsListCoercion = true;
        }
    }

    /// <summary>Checks the node (removing an earlier list coercion first); the node is replaced when the visit sets a replacement.</summary>
    [Delphi(PortStatus.Ported)]
    public void Traverse(ref TBoldOclNode? Node)
    {
        if (Node is TBoldOCLListCoercion TempNode)
        {
            Node = TempNode.Child;
            Node!.NeedsListCoercion = false;
            TempNode.Child = null;
            TempNode.Dispose();
        }

        Node!.Resubscribe = ReSubscribe;
        Node.SetReferenceValue(null);
        bool LocalReSubscribe = ReSubscribe;
        try
        {
            Node.AcceptVisitor(this);
            ReSubscribe = LocalReSubscribe;
        }
        finally
        {
            if (replacementNode is not null)
            {
                Node.Dispose();
                Node = replacementNode;
                replacementNode = null;
            }
        }
    }

    /// <summary>Traverse for the elements Start..Stop of a list.</summary>
    [Delphi(PortStatus.Ported)]
    public void TraverseList(TBoldOclNodeList List, int Start, int Stop)
    {
        for (int I = Start; I <= Stop; I++)
        {
            if (List[I] is TBoldOCLListCoercion TempNode)
            {
                List[I] = TempNode.Child;
                List[I]!.NeedsListCoercion = false;
                TempNode.Child = null;
                TempNode.Dispose();
            }

            List[I]!.Resubscribe = ReSubscribe;
            List[I]!.SetReferenceValue(null);
            bool LocalReSubscribe = ReSubscribe;
            try
            {
                List[I]!.AcceptVisitor(this);
                ReSubscribe = LocalReSubscribe;
            }
            finally
            {
                if (replacementNode is not null)
                {
                    List[I]!.Dispose();
                    List[I] = replacementNode;
                    replacementNode = null;
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOclSymbol? FindSymbol(string name) => SymbolTable.SymbolByName(name);

    /// <summary>Wraps the arguments marked for list coercion in coercion nodes.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddListCoercionOnArgs(TBoldOclOperation N)
    {
        for (int I = 0; I < N.Args!.Count(); I++)
        {
            if (N.Args[I]!.NeedsListCoercion)
            {
                var CoercionNode = new TBoldOCLListCoercion
                {
                    Child = N.Args[I],
                    BoldType = System.ListTypeInfoByElement(N.Args[I]!.BoldType),
                };
                N.Args[I] = CoercionNode;
            }
        }
    }

    /// <summary>
    /// Replaces an operation applied to a list whose symbol takes a single value by a collect of the operation over the
    /// list (the new node becomes the replacement).
    /// </summary>
    private void FixImplicitCollect(TBoldOclOperation n)
    {
        TBoldOclOperation NewOperation;
        if (n is TBoldOclMethod method)
        {
            NewOperation = new TBoldOclMethod { MethodOf = method.MethodOf, MethodOf_AddedToArgs = method.MethodOf_AddedToArgs };
        }
        else
        {
            NewOperation = new TBoldOclOperation();
        }

        TBoldOclNode? OldNode0 = n.Args![0];
        n.Args.Delete(0);
        string VarName = fEnv.MakeGenSymName();
        var VarNode = new TBoldOclVariableReference { VariableName = VarName, Position = n.Position };
        NewOperation.OperationName = n.OperationName;
        NewOperation.Position = n.Position;
        NewOperation.Args = new TBoldOclNodeList();
        NewOperation.Args.Add(VarNode);
        int count = n.Args.Count();
        for (int i = 0; i < count; i++)
        {
            NewOperation.Args.Add(n.Args[0]);
            n.Args.Delete(0);
        }

        var CollectNode = new TBoldOclIteration
        {
            IteratorSpecifier = TBoldOCLIteratorSpecifier.OCLCollect,
            OperationName = "Collect",
            Position = n.Position,
            Args = new TBoldOclNodeList(),
        };
        CollectNode.Args.Add(OldNode0);
        CollectNode.Args.Add(NewOperation);
        CollectNode.LoopVar = new TBoldOclVariableBinding { VariableName = VarName };

        var tempList = new TBoldOclNodeList();
        tempList.Add(CollectNode);
        try
        {
            TraverseList(tempList, 0, 0);
        }
        finally
        {
            replacementNode = tempList[0];
            tempList[0] = null;
        }
    }

    /// <summary>Finds the symbol, checks the arguments (with boolean shortcuts resubscribing), coerces and types the result.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclOperation(TBoldOclOperation N)
    {
        if (!N.IsMethod)
        {
            N.Symbol = FindSymbol(N.OperationName);
            if (N.Symbol is null)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUndefinedOperation, N.Position, N.OperationName));
            }
        }

        N.IsConstant = true;
        try
        {
            if (SysUtils.SameText(N.OperationName, "if") || SysUtils.SameText(N.OperationName, "or") || SysUtils.SameText(N.OperationName, "and"))
            {
                bool OldResubscribe = ReSubscribe;
                ReSubscribe = true;
                TraverseList(N.Args!, 0, 0);
                ReSubscribe = OldResubscribe;
            }
            else
            {
                TraverseList(N.Args!, 0, 0);
            }

            if (N.Args![0]!.BoldType is TBoldListTypeInfo && N.Symbol!.FormalArguments(0) is not TBoldListTypeInfo)
            {
                FixImplicitCollect(N);
                return;
            }

            if (N.Args.Count() != N.Symbol!.NumberOfArgs)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeWrongNumberOfArgs, N.Position, N.Symbol.NumberOfArgs, N.Args.Count()));
            }

            TraverseList(N.Args, 1, N.Args.Count() - 1);
            for (int I = 0; I < N.Args.Count(); I++)
            {
                N.IsConstant = N.IsConstant && N.Args[I]!.IsConstant;
                CheckArgumentType(N.Args[I]!, N.Symbol.FormalArguments(I));
            }

            AddListCoercionOnArgs(N);
            DeduceBoldType(N);
            if (N.Symbol.ArgsNeedCommonType)
            {
                TBoldElementTypeInfo? Type1 = N.Args[0]!.BoldType;
                if (Type1 is TBoldListTypeInfo list1)
                {
                    Type1 = list1.ListElementTypeInfo;
                }

                TBoldElementTypeInfo? Type2 = N.Args[1]!.BoldType;
                if (Type2 is TBoldListTypeInfo list2)
                {
                    Type2 = list2.ListElementTypeInfo;
                }

                if (!Type2!.ConformsTo(Type1) && !Type1!.ConformsTo(Type2))
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldCoreConsts.sArgumentsDoNotConform, N.Position, N.Symbol.SymbolName, Type1.AsString, Type2.AsString));
                }
            }
        }
        catch (Exception)
        {
            N.IsConstant = false;
            throw;
        }
    }

    /// <summary>Checks the collection, binds the loop variable (named or generated) and checks the body in its scope.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIteration(TBoldOclIteration N)
    {
        N.Symbol = FindSymbol(N.OperationName);
        if (N.Symbol is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUndefinedOperation, N.Position, N.OperationName));
        }

        if (N.Args!.Count() != N.Symbol.NumberOfArgs)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeWrongNumberOfArgs, N.Position, N.Symbol.NumberOfArgs, N.Args.Count()));
        }

        bool OldReSubscribe = false;
        PushResubscribe(ref ReSubscribe, ref OldReSubscribe);
        TraverseList(N.Args, 0, 0);
        CheckArgumentType(N.Args[0]!, N.Symbol.FormalArguments(0));
        N.IsConstant = N.Args[0]!.IsConstant;
        try
        {
            ReSubscribe = OldReSubscribe;
            N.LoopVar ??= new TBoldOclVariableBinding();
            if (N.LoopVar.VariableName.Length == 0)
            {
                N.LoopVar.VariableName = fEnv.MakeGenSymName();
            }

            AddListCoercionOnArgs(N);
            N.LoopVar.SetReferenceValue(null);
            if (N.LoopVar.BoldType is null)
            {
                if (N.Args[0]!.BoldType is TBoldListTypeInfo listType)
                {
                    N.LoopVar.BoldType = listType.ListElementTypeInfo;
                }
                else
                {
                    throw new EBoldOCLInternalError(SysUtils.Format(BoldOclError.boeUnKnownTypeOfLoopVar, N.Position, N.Args[0]!.BoldType?.GetType().Name ?? ""));
                }
            }

            fEnv.pushBinding(N.LoopVar);
            try
            {
                TraverseList(N.Args, 1, N.Args.Count() - 1);
                for (int I = 1; I < N.Args.Count(); I++)
                {
                    N.IsConstant = N.IsConstant && N.Args[I]!.IsConstant;
                    CheckArgumentType(N.Args[I]!, N.Symbol.FormalArguments(I));
                }
            }
            finally
            {
                fEnv.popBinding();
            }

            AddListCoercionOnArgs(N);
            DeduceBoldType(N);
        }
        catch (Exception)
        {
            N.IsConstant = false;
            throw;
        }
    }

    /// <summary>When the member or method name is a dot-notation symbol (as in x.oclType), replaces the node by that operation.</summary>
    [Delphi(PortStatus.Ported)]
    public bool TestForOperation(TBoldOclNode n)
    {
        string OpName = n switch
        {
            TBoldOclMember member => member.MemberName,
            TBoldOclMethod method => method.OperationName,
            _ => throw new EBoldOCLInternalError(SysUtils.Format("%d:Illegal call for TestForOperation", n.Position)),
        };

        TBoldOclSymbol? PossibleOperation = FindSymbol(OpName);
        if (PossibleOperation is null || !PossibleOperation.isDotNotation)
        {
            return false;
        }

        var TempOperation = new TBoldOclOperation
        {
            Symbol = PossibleOperation,
            Resubscribe = ReSubscribe,
            OperationName = OpName,
            Args = new TBoldOclNodeList(),
        };
        if (n is TBoldOclMember m)
        {
            TempOperation.Args.Add(m.MemberOf);
            m.MemberOf = null;
        }
        else if (n is TBoldOclMethod meth)
        {
            TempOperation.Args.Add(meth.MethodOf);
            for (int i = 0; i < meth.Args!.Count(); i++)
            {
                TempOperation.Args.Add(meth.Args[i]);
                meth.Args[i] = null;
            }

            meth.MethodOf = null;
        }

        TempOperation.Position = n.Position;
        try
        {
            TempOperation.AcceptVisitor(this);
        }
        finally
        {
            if (replacementNode is null)
            {
                replacementNode = TempOperation;
            }
            else
            {
                // An implicit collect replaced the operation with a copy that took over the arguments.
                TempOperation.Dispose();
            }
        }

        return true;
    }

    /// <summary>A method call; model methods are not OCL symbols, so this raises unless the name is an operation.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMethod(TBoldOclMethod N)
    {
        if (N.MethodOf is null)
        {
            TBoldOclVariableBinding binding = fEnv.CurrentImplicitVariable();
            N.MethodOf = new TBoldOclVariableReference
            {
                VariableBinding = binding,
                VariableName = binding.VariableName,
                BoldType = binding.BoldType,
                Position = N.Position,
            };
        }

        if (TestForOperation(N))
        {
            return;
        }

        if (CurrentSystemTypeInfo is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeExpressionNeedsContext, N.Position));
        }

        Traverse(ref N.MethodOf);
        if (!N.MethodOf_AddedToArgs)
        {
            N.Args!.Insert(0, N.MethodOf);
            N.MethodOf_AddedToArgs = true;
        }

        TBoldClassTypeInfo? ClassTypeInfo = N.MethodOf!.BoldType switch
        {
            TBoldClassTypeInfo c => c,
            TBoldListTypeInfo { ListElementTypeInfo: TBoldClassTypeInfo e } => e,
            _ => null,
        };
        if (ClassTypeInfo is null)
        {
            throw new EBoldOCLInternalError(SysUtils.Format(BoldCoreConsts.sMethodNotoperatingOnClass, N.Position, N.OperationName));
        }

        TBoldMethodRTInfo? MethodRTInfo = ClassTypeInfo.Methods.ItemsByExpressionName(N.OperationName);
        if (MethodRTInfo is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUndefinedOperation, N.Position, N.OperationName));
        }

        N.Symbol = null;
        if (N.Symbol is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeOperationNotOclable, N.Position, N.OperationName));
        }

        N.IsMethod = true;
        VisitTBoldOclOperation(N);
    }

    /// <summary>
    /// A member: a variable when unqualified and bound, otherwise a member of the implicit variable or the context;
    /// classes of the system, attributes and roles (with qualifiers), or a dot-notation operation.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMember(TBoldOclMember N)
    {
        ReSubscribe = true;
        if (CurrentSystemTypeInfo is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeExpressionNeedsContext, N.Position));
        }

        if (N.MemberOf is null)
        {
            TBoldOclVariableBinding? Binding = fEnv.Lookup(N.MemberName);
            if (Binding is null)
            {
                TBoldOclVariableBinding implicitVar = fEnv.CurrentImplicitVariable();
                N.MemberOf = new TBoldOclVariableReference
                {
                    VariableBinding = implicitVar,
                    VariableName = implicitVar.VariableName,
                    BoldType = implicitVar.BoldType,
                    Position = N.Position,
                };
            }
            else
            {
                var VarRef = new TBoldOclVariableReference
                {
                    VariableBinding = Binding,
                    VariableName = N.MemberName,
                    Position = N.Position,
                    BoldType = Binding.BoldType,
                };
                if (VarRef.BoldType is null)
                {
                    throw new EBoldOCLInternalError(SysUtils.Format(BoldOclError.boeVariableNotAssigned, N.Position, VarRef.VariableName));
                }

                replacementNode = VarRef;
                return;
            }
        }
        else
        {
            Traverse(ref N.MemberOf);
        }

        TBoldElementTypeInfo? ParentType = N.MemberOf!.BoldType;
        TBoldElementTypeInfo? ElementType = ParentType is TBoldListTypeInfo parentList ? parentList.ListElementTypeInfo : ParentType;
        if (ElementType is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeVariableNotAssigned, N.Position, ""));
        }

        switch (ElementType.BoldValueType)
        {
            case TBoldValueTypeSet.bvtSystem:
                if (!TestForOperation(N))
                {
                    TBoldClassTypeInfo? ClassTypeInfo = CurrentSystemTypeInfo.ClassTypeInfoByExpressionName(N.MemberName);
                    if (ClassTypeInfo is null)
                    {
                        throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUnknownClass, N.Position, N.MemberName));
                    }

                    N.BoldType = CurrentSystemTypeInfo.ListTypeInfoByElement(ClassTypeInfo);
                    N.MemberType = null;
                }

                break;
            case TBoldValueTypeSet.bvtClass:
                VisitClassMember(N, (TBoldClassTypeInfo)ElementType, ParentType);
                break;
            case TBoldValueTypeSet.bvtAttr:
                if (!TestForOperation(N))
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeMemberofAttr, N.Position, ElementType.AsString, N.MemberName));
                }

                break;
            case TBoldValueTypeSet.bvtType:
                if (!TestForOperation(N))
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeMemberofType, N.Position, ElementType.AsString, N.MemberName));
                }

                break;
        }

        N.IsConstant = false;
    }

    /// <summary>The bvtClass branch of VisitTBoldOclMember.</summary>
    private void VisitClassMember(TBoldOclMember N, TBoldClassTypeInfo ClassTypeInfo, TBoldElementTypeInfo? ParentType)
    {
        N.MemberIndex = ClassTypeInfo.MemberIndexByExpressionName(N.MemberName);
        if (N.MemberIndex != -1)
        {
            N.RTInfo = ClassTypeInfo.AllMembers[N.MemberIndex];
            if (N.RTInfo is TBoldRoleRTInfo { IsNavigable: false })
            {
                N.RTInfo = null;
            }
        }
        else
        {
            N.RTInfo = null;
        }

        N.MemberType = N.RTInfo?.BoldType;
        if (N.MemberType is null && !TestForOperation(N))
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUnknownMember, N.Position, N.MemberName, ClassTypeInfo.ExpressionName));
        }

        if (N.Qualifier is not null)
        {
            var RoleRTInfo = ClassTypeInfo.MemberRTInfoByExpressionName(N.MemberName) as TBoldRoleRTInfo;
            if (RoleRTInfo is null)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldCoreConsts.sOnlyRolesCanBeQualified, N.Position));
            }

            if (!RoleRTInfo.IsQualified)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldCoreConsts.sXIsNotAQualifiedRole, N.Position, ClassTypeInfo.ExpressionName, RoleRTInfo.ExpressionName));
            }

            if (N.Qualifier.Count() != RoleRTInfo.Qualifiers!.Count)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeWrongNumberOfArgs, N.Position, RoleRTInfo.Qualifiers.Count, N.Qualifier.Count()));
            }

            N.BoldType = RoleRTInfo.IsQualifiedMulti || N.MemberOf?.BoldType is TBoldListTypeInfo ? N.MemberType : RoleRTInfo.ClassTypeInfoOfOtherEnd;
            TraverseList(N.Qualifier, 0, N.Qualifier.Count() - 1);
            for (int i = 0; i < RoleRTInfo.Qualifiers.Count; i++)
            {
                CheckArgumentType(N.Qualifier[i]!, RoleRTInfo.Qualifiers[i].BoldType);
            }
        }
        else if (N.MemberType is not TBoldListTypeInfo && ParentType is TBoldListTypeInfo)
        {
            N.BoldType = System.ListTypeInfoByElement(N.MemberType);
        }
        else
        {
            N.BoldType = N.MemberType;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclVariableReference(TBoldOclVariableReference N)
    {
        N.VariableBinding = fEnv.Lookup(N.VariableName);
        if (N.VariableBinding is null)
        {
            throw new EBoldOCLInternalError(SysUtils.Format(BoldOclError.boeUnknownVariable, N.Position, N.VariableName));
        }

        N.BoldType = N.VariableBinding.BoldType;
        if (N.BoldType is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeVariableNotAssigned, N.Position, N.VariableName));
        }
    }

    /// <summary>A type name as a value; with capital members allowed, a member of the context with that name instead.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclTypeNode(TBoldOclTypeNode n)
    {
        n.BoldType = System.BoldType;
        TBoldElementTypeInfo? CurrentType = System.ClassTypeInfoByExpressionName(n.typeName);
        TBoldElementTypeInfo? ExpressionContext = fEnv.CurrentImplicitVariable().BoldType;
        CurrentType ??= System.AttributeTypeInfoByExpressionName(n.typeName);
        if (CurrentType is null || (!IgnoreNelCompatibility && BoldOcl.BoldNelCompatibility &&
                                    ExpressionContext?.BoldValueType is TBoldValueTypeSet.bvtClass or TBoldValueTypeSet.bvtList))
        {
            if (ExpressionContext is not null)
            {
                TBoldClassTypeInfo? ClassTypeInfo = null;
                if (ExpressionContext.BoldValueType == TBoldValueTypeSet.bvtClass)
                {
                    ClassTypeInfo = (TBoldClassTypeInfo)ExpressionContext;
                }

                if (ExpressionContext is TBoldListTypeInfo { ListElementTypeInfo: { BoldValueType: TBoldValueTypeSet.bvtClass } element })
                {
                    ClassTypeInfo = (TBoldClassTypeInfo)element;
                }

                if (ClassTypeInfo?.MemberRTInfoByExpressionName(n.typeName) is not null)
                {
                    if (BoldOcl.BoldOCLAllowCapitalMembers)
                    {
                        TBoldOclNode? NewMember = new TBoldOclMember { MemberName = n.typeName, Position = n.Position };
                        Traverse(ref NewMember);
                        replacementNode = NewMember;
                        return;
                    }

                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeunknownTypeButMember, n.Position, n.typeName));
                }
            }

            if (CurrentType is null)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeunknownType, n.Position));
            }
        }

        n.SetReferenceValue(CurrentType);
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclStrLiteral(TBoldOclStrLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("String");
        N.IsConstant = true;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldClassTypeInfo? LeastCommonSuperClass(TBoldClassTypeInfo? C1, TBoldClassTypeInfo? C2)
    {
        if (C1 is null || C2 is null)
        {
            return null;
        }

        if (C1.BoldIsA(C2))
        {
            return C2;
        }

        return C2.BoldIsA(C1) ? C1 : LeastCommonSuperClass(C1.SuperClassTypeInfo, C2);
    }

    /// <summary>The least common supertype; attributes of the same class conform, so either is the answer.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldAttributeTypeInfo? LeastCommonSuperType(TBoldAttributeTypeInfo? AttributeTypeInfo1, TBoldAttributeTypeInfo? AttributeTypeInfo2)
    {
        if (AttributeTypeInfo1 is null || AttributeTypeInfo2 is null)
        {
            return null;
        }

        if (AttributeTypeInfo1.BoldIsA(AttributeTypeInfo2))
        {
            return AttributeTypeInfo2;
        }

        if (AttributeTypeInfo2.BoldIsA(AttributeTypeInfo1))
        {
            return AttributeTypeInfo1;
        }

        return AttributeTypeInfo2.AttributeClass == AttributeTypeInfo1.AttributeClass
            ? AttributeTypeInfo1
            : LeastCommonSuperType(AttributeTypeInfo1.SuperAttributeTypeInfo, AttributeTypeInfo2);
    }

    /// <summary>A range of integers, or a list typed by the least common supertype of its elements.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclCollectionLiteral(TBoldOclCollectionLiteral N)
    {
        if (N.IsRange)
        {
            Traverse(ref N.RangeStart);
            Traverse(ref N.RangeStop);
            TBoldAttributeTypeInfo? IntType = System.AttributeTypeInfoByExpressionName("Integer");
            if (!N.RangeStart!.BoldType!.ConformsTo(IntType))
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeRangeMustBeInt, N.RangeStart.Position));
            }

            if (!N.RangeStop!.BoldType!.ConformsTo(IntType))
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeRangeMustBeInt, N.RangeStop.Position));
            }

            N.BoldType = System.ListTypeInfoByElement(IntType);
            N.IsConstant = N.RangeStop.IsConstant && N.RangeStart.IsConstant;
            return;
        }

        if (N.Elements!.Count() == 0)
        {
            N.BoldType = System.ListTypeInfoByElement(null);
            return;
        }

        TraverseList(N.Elements, 0, N.Elements.Count() - 1);
        N.IsConstant = N.Elements[0]!.IsConstant;
        if (N.Elements[0]!.BoldType is TBoldClassTypeInfo firstClass)
        {
            TBoldClassTypeInfo? TopClass = firstClass;
            for (int I = 1; I < N.Elements.Count(); I++)
            {
                N.IsConstant = N.IsConstant && N.Elements[I]!.IsConstant;
                if (N.Elements[I]!.BoldType is not TBoldClassTypeInfo elementClass)
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeElementnotConformToCollection, N.Elements[I]!.Position));
                }

                TopClass = LeastCommonSuperClass(TopClass, elementClass);
            }

            N.BoldType = System.ListTypeInfoByElement(TopClass);
        }
        else
        {
            TBoldAttributeTypeInfo? TopType = (TBoldAttributeTypeInfo?)N.Elements[0]!.BoldType;
            for (int I = 1; I < N.Elements.Count(); I++)
            {
                if (N.Elements[I]!.BoldType is not TBoldAttributeTypeInfo elementType)
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeElementnotConformToCollection, N.Elements[I]!.Position));
                }

                N.IsConstant = N.IsConstant && N.Elements[I]!.IsConstant;
                TopType = LeastCommonSuperType(TopType, elementType);
                if (TopType is null)
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeElementnotConformToCollection, N.Elements[I]!.Position));
                }
            }

            N.BoldType = System.ListTypeInfoByElement(TopType);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclNumericLiteral(TBoldOclNumericLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("Float");
        N.IsConstant = true;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclEnumLiteral(TBoldOclEnumLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("ValueSet");
        N.IsConstant = true;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIntLiteral(TBoldOclIntLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("Integer");
        N.IsConstant = true;
    }

    /// <summary>Sets the operation's type by its symbol's deduce method; raises when no type results.</summary>
    [Delphi(PortStatus.Ported)]
    public void DeduceBoldType(TBoldOclOperation N)
    {
        TBoldOclSymbol symbol = N.Symbol!;
        TBoldOclNodeList args = N.Args!;
        switch (symbol.DeduceMethod)
        {
            case TBoldOclDeduceMethod.tbodNo:
                N.BoldType = symbol.ResultType;
                break;
            case TBoldOclDeduceMethod.tbodCopyArg1:
                N.BoldType = args[0]!.BoldType;
                break;
            case TBoldOclDeduceMethod.tbodCopyArg2:
                N.BoldType = args[1]!.BoldType;
                break;
            case TBoldOclDeduceMethod.tbodCopyArg3:
                N.BoldType = args[2]!.BoldType;
                break;
            case TBoldOclDeduceMethod.tbodArg1AsList:
                N.BoldType = args[0]!.BoldType!.BoldValueType == TBoldValueTypeSet.bvtList ? args[0]!.BoldType : System.ListTypeInfoByElement(args[0]!.BoldType);
                break;
            case TBoldOclDeduceMethod.tbodListofArg2:
                N.BoldType = args[1]!.BoldType!.BoldValueType == TBoldValueTypeSet.bvtList ? args[1]!.BoldType : System.ListTypeInfoByElement(args[1]!.BoldType);
                break;
            case TBoldOclDeduceMethod.tbodCopyArg1Elem:
                if (args[0]!.BoldType is not TBoldListTypeInfo arg0List)
                {
                    throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeNonListArgumentToCopyArg1Elem, N.Position));
                }

                N.BoldType = arg0List.ListElementTypeInfo;
                break;
            case TBoldOclDeduceMethod.tbodCopyLoopVar:
                if (N is not TBoldOclIteration iteration)
                {
                    throw new EBoldOCLInternalError(SysUtils.Format(BoldOclError.boeOperationWithLoopvar, N.Position, symbol.SymbolName));
                }

                N.BoldType = iteration.LoopVar!.BoldType;
                break;
            case TBoldOclDeduceMethod.tbodLCC:
            case TBoldOclDeduceMethod.tbodLCC23:
                DeduceLeastCommonType(N, symbol.DeduceMethod == TBoldOclDeduceMethod.tbodLCC ? 0 : 1);
                break;
            case TBoldOclDeduceMethod.tbodType:
                N.BoldType = args[0]!.BoldType!.BoldType;
                break;
            case TBoldOclDeduceMethod.tbodTypecast:
                N.BoldType = (TBoldElementTypeInfo?)args[1]!.Value;
                break;
            case TBoldOclDeduceMethod.TbodObjectlist:
                if (args[0]!.Value is not null)
                {
                    N.BoldType = System.ListTypeInfoByElement((TBoldElementTypeInfo)args[0]!.Value!);
                }

                break;
            case TBoldOclDeduceMethod.tbodListFromArg2:
                N.BoldType = System.ListTypeInfoByElement((TBoldElementTypeInfo?)args[1]!.Value);
                break;
        }

        if (N.BoldType is null)
        {
            var ArgTypes = new TStringList();
            for (int i = 0; i < args.Count(); i++)
            {
                ArgTypes.Add(SysUtils.Format("Arg %s: %s", SysUtils.IntToStr(i + 1), args[i]!.BoldType?.AsString ?? ""));
            }

            string ContextStr = BoldUtils.BoldSeparateStringList(ArgTypes, ", ", "", "");
            throw new EBoldOCLError(SysUtils.Format(BoldOclError.boeunknownExprtypeinDeduce, N.Position, N.OperationName, ContextStr,
                BoldOclSemantics.BoldOclDeduceMethodName[(int)symbol.DeduceMethod]));
        }
    }

    /// <summary>The tbodLCC/tbodLCC23 branch of DeduceBoldType: the least common type of two arguments, a list when either is.</summary>
    private void DeduceLeastCommonType(TBoldOclOperation N, int argno1)
    {
        int argno2 = argno1 + 1;
        TBoldElementTypeInfo? arg1 = N.Args![argno1]!.BoldType;
        TBoldElementTypeInfo? arg2 = N.Args[argno2]!.BoldType;
        bool IsList = false;
        if (arg1 is TBoldListTypeInfo list1)
        {
            arg1 = list1.ListElementTypeInfo;
            if (arg2 is TBoldListTypeInfo list2)
            {
                arg2 = list2.ListElementTypeInfo;
            }
            else
            {
                N.Args[argno2]!.NeedsListCoercion = true;
            }

            IsList = true;
        }
        else if (arg2 is TBoldListTypeInfo list2)
        {
            N.Args[argno1]!.NeedsListCoercion = true;
            arg2 = list2.ListElementTypeInfo;
            IsList = true;
        }

        N.BoldType = (arg1, arg2) switch
        {
            (TBoldClassTypeInfo c1, TBoldClassTypeInfo c2) => LeastCommonSuperClass(c1, c2),
            (TBoldAttributeTypeInfo a1, TBoldAttributeTypeInfo a2) => LeastCommonSuperType(a1, a2),
            (TBoldTypeTypeInfo, TBoldTypeTypeInfo) => arg1,
            _ => null,
        };
        if (N.BoldType is null)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeNoCommonSuperClass, N.Position, arg1?.ExpressionName ?? "", arg2?.ExpressionName ?? ""));
        }

        if (IsList)
        {
            N.BoldType = System.ListTypeInfoByElement(N.BoldType);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclDateLiteral(TBoldOclDateLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("Date");
        N.IsConstant = true;
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclTimeLiteral(TBoldOclTimeLiteral N)
    {
        N.BoldType = System.AttributeTypeInfoByExpressionName("Time");
        N.IsConstant = true;
    }

    [Delphi(PortStatus.Ported)] public bool IgnoreNelCompatibility { get => fIgnoreNelCompatibility; set => fIgnoreNelCompatibility = value; }
}
