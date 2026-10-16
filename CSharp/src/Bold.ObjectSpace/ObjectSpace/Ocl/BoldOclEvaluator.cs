// Delphi: Source/ObjectSpace/Ocl/BoldOclEvaluator.pas
// Port map: CSharp/port/units/BoldOclEvaluator.md
// Evaluates a checked syntax tree in memory: each node gets its value (owned or referenced), and the subscriber is
// subscribed to everything the result depends on.

using Bold;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclEvaluator", Symbol = "BoldOCLInternalSortWrapper", Note = "SortList compares the keys with a lambda.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclEvaluator", Symbol = "BoldOCLInternalReverseSortWrapper", Note = "SortList compares the keys with a lambda.")]

namespace Bold;

/// <summary>The evaluator's helpers: the resubscribe mapping and the sort for orderBy.</summary>
public static partial class BoldOclEvaluator
{
    [Delphi(PortStatus.Ported)]
    internal static TBoldRequestedEvent MapResubscribe(bool Resubscribe) =>
        Resubscribe ? BoldSubscription.breReSubscribe : BoldSubscription.breReEvaluate;

    /// <summary>Adds the list's elements to the node's list, ordered by the keys (one per element) in SortKeyHolder.</summary>
    [Delphi(PortStatus.Ported, Note = "A stable sort: equal keys keep the list order, where Delphi's quicksort leaves them in any order.")]
    internal static void Sortlist(TBoldOclIteration Node, TBoldList BoldList, TBoldIndirectElement SortKeyHolder, TBoldOCLIteratorSpecifier Order)
    {
        var arglist = (TBoldList)SortKeyHolder.Value!;
        var Sortlist = new List<TBoldOCLSortClass>(BoldList.Count);
        for (int i = 0; i < BoldList.Count; i++)
        {
            Sortlist.Add(new TBoldOCLSortClass { SortArg = arglist[i], SortObj = BoldList[i] });
        }

        IEnumerable<TBoldOCLSortClass> sorted = Order switch
        {
            TBoldOCLIteratorSpecifier.OclOrderBy => Sortlist.Order(Comparer<TBoldOCLSortClass>.Create((a, b) => a.SortArg!.CompareTo(b.SortArg))),
            TBoldOCLIteratorSpecifier.OclOrderDescending => Sortlist.Order(Comparer<TBoldOCLSortClass>.Create((a, b) => -a.SortArg!.CompareTo(b.SortArg))),
            _ => Sortlist,
        };

        var NewList = (TBoldList)Node.Value!;
        foreach (TBoldOCLSortClass item in sorted)
        {
            NewList.Add(item.SortObj);
        }
    }
}

/// <summary>A sort key with the element it sorts.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldOCLSortClass : TBoldMemoryManagedObject
{
    public TBoldElement? SortArg;
    public TBoldElement? SortObj;
}

/// <summary>Evaluates a checked tree; the result is the root node's value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclEvaluatorVisitor : TBoldOclVisitor
{
    private readonly TBoldSubscriber? CurrentSubscriber;
    private readonly TBoldSystemTypeInfo CurrentSystemTypeInfo;
    private readonly TBoldAttributeTypeInfo? fStringType;
    private readonly TBoldAttributeTypeInfo? fIntegerType;
    private readonly TBoldAttributeTypeInfo? fTimeType;
    private readonly TBoldAttributeTypeInfo? fDateType;
    private readonly TBoldAttributeTypeInfo? fFloatType;
    private readonly TBoldSystem? CurrentSystem;
    private bool fResubscribeAll;

    [Delphi(PortStatus.Ported)]
    public TBoldOclEvaluatorVisitor(TBoldSubscriber? Subscriber, bool ResubscribeAll, TBoldSystemTypeInfo SystemTypeInfo, TBoldSystem? BoldSystem,
        TBoldAttributeTypeInfo? StringType, TBoldAttributeTypeInfo? IntegerType, TBoldAttributeTypeInfo? FloatType, TBoldAttributeTypeInfo? DateType,
        TBoldAttributeTypeInfo? TimeType)
    {
        CurrentSubscriber = Subscriber;
        CurrentSystemTypeInfo = SystemTypeInfo;
        CurrentSystem = BoldSystem;
        fStringType = StringType;
        fIntegerType = IntegerType;
        fFloatType = FloatType;
        fDateType = DateType;
        fTimeType = TimeType;
        fResubscribeAll = ResubscribeAll;
    }

    [Delphi(PortStatus.Ported)] private TBAString MakeNewString() => (TBAString)CreateNewMember(fStringType);
    [Delphi(PortStatus.Ported)] private TBADate MakeNewDate() => (TBADate)CreateNewMember(fDateType);
    [Delphi(PortStatus.Ported)] private TBATime MakeNewTime() => (TBATime)CreateNewMember(fTimeType);
    [Delphi(PortStatus.Ported)] private TBAInteger MakeNewInteger() => (TBAInteger)CreateNewMember(fIntegerType);
    [Delphi(PortStatus.Ported)] private TBAFloat MakeNewFloat() => (TBAFloat)CreateNewMember(fFloatType);

    /// <summary>A new member of the type; object lists do not subscribe to their objects.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldMember CreateNewMember(TBoldElementTypeInfo? BoldType)
    {
        TBoldMember result = TBoldMemberFactory.CreateMemberFromBoldType(BoldType);
        if (result is TBoldObjectList list)
        {
            list.SubscribeToObjectsInList = false;
        }

        return result;
    }

    /// <summary>Subscribes to a referenced value (owned values are new and need no subscription).</summary>
    [Delphi(PortStatus.Ported)]
    public void SubScribeToElem(TBoldOclNode N)
    {
        if (N.Value is not null && CurrentSubscriber is not null && !N.OwnsValue)
        {
            N.Value.DefaultSubscribe(CurrentSubscriber, BoldOclEvaluator.MapResubscribe(N.Resubscribe || fResubscribeAll));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclListCoercion(TBoldOCLListCoercion N)
    {
        N.Child!.AcceptVisitor(this);
        if (N.Child.Value is not null)
        {
            N.Child.Value.GetAsList(N);
        }
        else
        {
            N.SetReferenceValue(null);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMethod(TBoldOclMethod N) => VisitTBoldOclOperation(N);

    /// <summary>
    /// Evaluates the arguments (if, or and and short-cut and resubscribe; except catches the first argument's failure)
    /// and the symbol; abstract-free null attribute arguments get temporary empty values.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclOperation(TBoldOclOperation N)
    {
        TBoldOclNodeList args = N.Args!;
        if (SysUtils.SameText(N.OperationName, "if"))
        {
            bool backupResubscribeAll = fResubscribeAll;
            fResubscribeAll = true;
            args[0]!.AcceptVisitor(this);
            fResubscribeAll = backupResubscribeAll;
            if (args[0]!.Value is not null && ((TBABoolean)args[0]!.Value!).AsBoolean)
            {
                args[1]!.AcceptVisitor(this);
            }
            else
            {
                args[2]!.AcceptVisitor(this);
            }
        }
        else if (SysUtils.SameText(N.OperationName, "except"))
        {
            bool backupResubscribeAll = fResubscribeAll;
            fResubscribeAll = true;
            try
            {
                args[0]!.AcceptVisitor(this);
                args[0]!.TransferValue(N);
            }
            catch (Exception)
            {
                args[1]!.AcceptVisitor(this);
                args[1]!.TransferValue(N);
            }

            fResubscribeAll = backupResubscribeAll;
            SubScribeToElem(N);
            return;
        }
        else if (SysUtils.SameText(N.OperationName, "or"))
        {
            fResubscribeAll = true;
            args[0]!.AcceptVisitor(this);
            if (args[0]!.Value is null || !((TBABoolean)args[0]!.Value!).AsBoolean)
            {
                args[1]!.AcceptVisitor(this);
            }
        }
        else if (SysUtils.SameText(N.OperationName, "and"))
        {
            fResubscribeAll = true;
            args[0]!.AcceptVisitor(this);
            if (args[0]!.Value is not null && ((TBABoolean)args[0]!.Value!).AsBoolean)
            {
                args[1]!.AcceptVisitor(this);
            }
        }
        else
        {
            for (int i = 0; i < args.Count(); i++)
            {
                args[i]!.AcceptVisitor(this);
            }
        }

        TBoldOclSymbol symbol = N.Symbol!;
        try
        {
            var OperationParams = new TBoldOclSymbolParameters();
            try
            {
                for (int i = 0; i < symbol.NumberOfArgs; i++)
                {
                    TBoldOclNode arg = args[i]!;
                    if (arg.Value is null && arg.BoldType is TBoldAttributeTypeInfo { IsAbstract: false })
                    {
                        arg.SetOwnedValue(TBoldMemberFactory.CreateMemberFromBoldType(arg.BoldType));
                        arg.HastemporaryDummyValue = true;
                    }

                    OperationParams.Values[i] = arg.Value is TBoldObjectReference reference ? reference.BoldObject : arg.Value;
                    OperationParams.Nodes[i] = arg;
                }

                OperationParams.Result = N;
                OperationParams.Subscriber = CurrentSubscriber;
                OperationParams.System = CurrentSystem;
                OperationParams.SystemTypeInfo = CurrentSystemTypeInfo;
                symbol.Evaluate(OperationParams);
                if (OperationParams.Result.OwnsValue && OperationParams.Result.Value is not null)
                {
                    OperationParams.Result.Value.MakeImmutable();
                }
            }
            finally
            {
                // Short-cut operations leave arguments unevaluated; drop the dummies so literals are evaluated again next time.
                for (int i = 0; i < symbol.NumberOfArgs; i++)
                {
                    if (args[i]!.HastemporaryDummyValue)
                    {
                        args[i]!.SetReferenceValue(null);
                        args[i]!.HastemporaryDummyValue = false;
                    }
                }
            }
        }
        catch (EBoldOclRunTimeError e)
        {
            e.Position = N.Position;
            e.ErrorFixed = true;
            throw;
        }

        if (N.Value is not null && N.Value.BoldType is null)
        {
            throw new EBoldInternal(SysUtils.Format("Result of evaluation of operation %s has no type", N.OperationName));
        }

        SubScribeToElem(N);
    }

    /// <summary>Evaluates the collection, then the body per element with the loop variable bound, letting the symbol accumulate the result.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIteration(TBoldOclIteration N)
    {
        var OperationParams = new TBoldOclSymbolParameters
        {
            Subscriber = CurrentSubscriber,
            System = CurrentSystem,
            SystemTypeInfo = CurrentSystemTypeInfo,
        };
        TBoldOclNodeList args = N.Args!;
        args[0]!.AcceptVisitor(this);
        N.SetOwnedValue(CreateNewMember(N.BoldType));
        switch (N.IteratorSpecifier)
        {
            case TBoldOCLIteratorSpecifier.OclUnique:
                ((TBABoolean)N.Value!).AsBoolean = true;
                break;
            case TBoldOCLIteratorSpecifier.OclForAll:
            case TBoldOCLIteratorSpecifier.OclExists:
                ((TBABoolean)N.Value!).AsBoolean = N.IteratorSpecifier == TBoldOCLIteratorSpecifier.OclForAll;
                break;
            case TBoldOCLIteratorSpecifier.OclOrderBy:
            case TBoldOCLIteratorSpecifier.OclOrderDescending:
            case TBoldOCLIteratorSpecifier.OclSelect:
            case TBoldOCLIteratorSpecifier.OclReject:
            case TBoldOCLIteratorSpecifier.OCLCollect:
                ((TBoldList)N.Value!).DuplicateMode = TBoldListDupMode.bldmAllow;
                if (N.Value is TBoldObjectList objectList)
                {
                    objectList.SubscribeToObjectsInList = false;
                }

                break;
            default:
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUnKnownIterator, N.Position));
        }

        if (args[0]!.Value is null)
        {
            return;
        }

        OperationParams.Values[0] = args[1]!.Value;
        OperationParams.Nodes[0] = args[1];
        OperationParams.Values[1] = N.LoopVar!.Value;
        OperationParams.Nodes[1] = N.LoopVar;
        var List = (TBoldList)args[0]!.Value!;
        if (N.IteratorSpecifier is TBoldOCLIteratorSpecifier.OclOrderBy or TBoldOCLIteratorSpecifier.OclOrderDescending)
        {
            var SortKeyHolder = new TBoldOclNode();
            var SortKeys = new TBoldMemberList { DuplicateMode = TBoldListDupMode.bldmAllow };
            // A sort argument that is not a real member (with run-time info) must be cloned.
            bool SortArgIsRTMember = args[1] is TBoldOclMember { RTInfo: not null };
            SortKeys.CloneMembers = !SortArgIsRTMember;
            SortKeyHolder.SetOwnedValue(SortKeys);
            OperationParams.Result = SortKeyHolder;
            List.EnsureRange(0, List.Count - 1);
            for (int i = 0; i < List.Count; i++)
            {
                N.LoopVar.SetReferenceValue(List[i]);
                args[1]!.AcceptVisitor(this);
                OperationParams.Values[0] = args[1]!.Value;
                OperationParams.Values[1] = N.LoopVar.Value;
                N.Symbol!.Evaluate(OperationParams);
            }

            BoldOclEvaluator.Sortlist(N, List, SortKeyHolder, N.IteratorSpecifier);
            if (!SortKeys.CloneMembers)
            {
                for (int i = SortKeys.Count - 1; i >= 0; i--)
                {
                    // Dummy keys not owned by an object are dropped here.
                    if (SortKeys[i].OwningObject is null)
                    {
                        SortKeys.RemoveByIndex(i);
                    }
                }
            }

            SortKeyHolder.Dispose();
        }
        else
        {
            OperationParams.Result = N;
            List.EnsureRange(0, List.Count - 1);
            for (int i = 0; i < List.Count; i++)
            {
                N.LoopVar.BoldType = null;
                N.LoopVar.SetReferenceValue(List[i]);
                args[1]!.AcceptVisitor(this);
                OperationParams.Values[0] = args[1]!.Value;
                OperationParams.Values[1] = N.LoopVar.Value;
                N.Symbol!.Evaluate(OperationParams);
            }
        }
    }

    /// <summary>A member of an object, of each object in a list (fetching the links together), or a class's extent.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclMember(TBoldOclMember N)
    {
        N.MemberOf!.AcceptVisitor(this);
        TBoldElement? memberOfValue = N.MemberOf.Value;
        if (memberOfValue is null)
        {
            N.SetReferenceValue(null);
            return;
        }

        switch (memberOfValue.BoldType!.BoldValueType)
        {
            case TBoldValueTypeSet.bvtSystem:
            {
                var ClassType = (TBoldClassTypeInfo)((TBoldListTypeInfo)N.BoldType!).ListElementTypeInfo!;
                N.SetReferenceValue(CurrentSystem!.Classes(ClassType.TopSortedIndex));
                break;
            }

            case TBoldValueTypeSet.bvtClass:
            {
                TBoldObject? Obj = memberOfValue switch
                {
                    TBoldObjectReference reference => reference.BoldObject,
                    TBoldObject o => o,
                    _ => throw new EBold(SysUtils.Format(BoldCoreConsts.sUnknownTypeOfMemberOf, memberOfValue.GetType().Name)),
                };
                if (Obj is not null)
                {
                    if (N.Qualifier is not null)
                    {
                        for (int i = 0; i < N.Qualifier.Count(); i++)
                        {
                            N.Qualifier[i]!.AcceptVisitor(this);
                        }

                        N.SetReferenceValue(RetrieveQualifiedSingle(N, Obj));
                    }
                    else
                    {
                        N.SetReferenceValue(Obj.BoldMembers(N.MemberIndex));
                    }
                }
                else if (N.BoldType!.BoldValueType == TBoldValueTypeSet.bvtList)
                {
                    N.SetOwnedValue(CreateNewMember(N.BoldType));
                }
                else
                {
                    N.SetReferenceValue(null);
                }

                break;
            }

            case TBoldValueTypeSet.bvtList:
            {
                var OldObjectList = (TBoldObjectList)memberOfValue;
                OldObjectList.EnsureObjects();
                switch (N.MemberType!.BoldValueType)
                {
                    case TBoldValueTypeSet.bvtAttr:
                        N.SetOwnedValue(RetrieveAttribute(N, OldObjectList));
                        break;
                    case TBoldValueTypeSet.bvtClass:
                        N.SetOwnedValue(RetrieveSingleLink(N, OldObjectList));
                        break;
                    case TBoldValueTypeSet.bvtList:
                        N.SetOwnedValue(RetrieveMultiLink(N, OldObjectList));
                        break;
                    default:
                        throw new EBold(SysUtils.Format(BoldCoreConsts.sUnknownTypeOfMember, N.MemberType.GetType().Name));
                }

                break;
            }

            default:
                throw new EBold(SysUtils.Format(BoldCoreConsts.sUnknownTypeOfMember, memberOfValue.BoldType.GetType().Name));
        }

        SubScribeToElem(N);
    }

    /// <summary>The object of a qualified role of Obj with the qualifier values.</summary>
    private TBoldObject? RetrieveQualifiedSingle(TBoldOclMember n, TBoldObject Obj)
    {
        var MemberList = new TBoldMemberList();
        for (int i = 0; i < n.Qualifier!.Count(); i++)
        {
            MemberList.Add((TBoldMember?)n.Qualifier[i]!.Value);
        }

        var Role = (TBoldObjectList)Obj.BoldMembers(n.MemberIndex);
        if (CurrentSubscriber is not null)
        {
            Role.DefaultSubscribe(CurrentSubscriber, BoldSubscription.breReSubscribe);
        }

        return Role.GetByIndexAndSubscribe(MemberList, CurrentSubscriber);
    }

    /// <summary>The attribute of each object, as a list of the members themselves.</summary>
    private TBoldMemberList RetrieveAttribute(TBoldOclMember N, TBoldObjectList OldObjectList)
    {
        var result = (TBoldMemberList)CreateNewMember(N.BoldType);
        result.DuplicateMode = TBoldListDupMode.bldmAllow;
        int index = N.MemberIndex;
        for (int i = 0; i < OldObjectList.Count; i++)
        {
            TBoldObject LoopObject = OldObjectList[i]!;
            result.Add(LoopObject.BoldMembers(index));
            if (CurrentSubscriber is not null)
            {
                LoopObject.BoldMembers(index).DefaultSubscribe(CurrentSubscriber, BoldOclEvaluator.MapResubscribe(N.Resubscribe || fResubscribeAll));
            }
        }

        return result;
    }

    private void FetchLinks(TBoldOclMember n, TBoldObjectList OldObjectList)
    {
        if (OldObjectList.Count > 0)
        {
            TBoldSystem tempSystem = CurrentSystem ?? OldObjectList[0]!.BoldSystem;
            tempSystem.FetchLinksWithObjects(OldObjectList, n.MemberName);
        }
    }

    /// <summary>The objects of a multi role of each object (or of a qualified role).</summary>
    private TBoldObjectList RetrieveMultiLink(TBoldOclMember n, TBoldObjectList OldObjectList)
    {
        FetchLinks(n, OldObjectList);
        var result = (TBoldObjectList)CreateNewMember(n.BoldType);
        result.DuplicateMode = TBoldListDupMode.bldmAllow;
        if (n.Qualifier is not null)
        {
            for (int i = 0; i < n.Qualifier.Count(); i++)
            {
                n.Qualifier[i]!.AcceptVisitor(this);
            }
        }

        for (int i = 0; i < OldObjectList.Count; i++)
        {
            TBoldObject LoopObject = OldObjectList[i]!;
            if (n.Qualifier is not null)
            {
                result.Add(RetrieveQualifiedSingle(n, LoopObject));
            }
            else
            {
                var roleList = (TBoldObjectList)LoopObject.BoldMembers(n.MemberIndex);
                if (CurrentSubscriber is not null)
                {
                    roleList.DefaultSubscribe(CurrentSubscriber, BoldOclEvaluator.MapResubscribe(n.Resubscribe));
                }

                for (int j = 0; j < roleList.Count; j++)
                {
                    result.Add(roleList[j]);
                }
            }
        }

        return result;
    }

    /// <summary>The object of a single role of each object, skipping empty links.</summary>
    private TBoldObjectList RetrieveSingleLink(TBoldOclMember n, TBoldObjectList OldObjectList)
    {
        FetchLinks(n, OldObjectList);
        var result = (TBoldObjectList)CreateNewMember(n.BoldType);
        result.DuplicateMode = TBoldListDupMode.bldmAllow;
        for (int i = 0; i < OldObjectList.Count; i++)
        {
            TBoldObject LoopObject = OldObjectList[i]!;
            var Link = (TBoldObjectReference)LoopObject.BoldMembers(n.MemberIndex);
            if (CurrentSubscriber is not null)
            {
                Link.DefaultSubscribe(CurrentSubscriber, BoldOclEvaluator.MapResubscribe(n.Resubscribe));
            }

            if (Link.BoldObject is not null)
            {
                result.Add(Link.BoldObject);
            }
        }

        return result;
    }

    /// <summary>The variable's value, subscribed to.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclVariableReference(TBoldOclVariableReference N)
    {
        TBoldElement? VariableValue = N.VariableBinding!.Value;
        N.SetReferenceValue(VariableValue);
        if (CurrentSubscriber is not null && VariableValue is not null)
        {
            VariableValue.DefaultSubscribe(CurrentSubscriber, BoldOclEvaluator.MapResubscribe(N.Resubscribe || fResubscribeAll));
        }
    }

    /// <summary>Nothing: an enum literal has no value of its own (operations compare its name).</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclEnumLiteral(TBoldOclEnumLiteral N)
    {
    }

    /// <summary>The list of the elements' values, or of the integers in the range.</summary>
    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclCollectionLiteral(TBoldOclCollectionLiteral N)
    {
        if (!N.OwnsValue)
        {
            N.SetOwnedValue(CreateNewMember(N.BoldType));
        }

        var BoldList = (TBoldList)N.Value!;
        BoldList.Clear();
        if (N.IsRange)
        {
            N.RangeStart!.AcceptVisitor(this);
            N.RangeStop!.AcceptVisitor(this);
            if (N.RangeStart.Value is null || N.RangeStop.Value is null)
            {
                throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boertRangeNotAssigned, N.RangeStart.Position));
            }

            TBAInteger tempInteger = MakeNewInteger();
            int stop = ((TBAInteger)N.RangeStop.Value).AsInteger;
            for (int i = ((TBAInteger)N.RangeStart.Value).AsInteger; i <= stop; i++)
            {
                tempInteger.AsInteger = i;
                BoldList.Add(tempInteger);
            }
        }
        else
        {
            for (int i = 0; i < N.Elements!.Count(); i++)
            {
                N.Elements[i]!.AcceptVisitor(this);
                BoldList.Add(N.Elements[i]!.Value);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclNumericLiteral(TBoldOclNumericLiteral N)
    {
        if (N.Value is null)
        {
            TBAFloat value = MakeNewFloat();
            N.SetOwnedValue(value);
            value.AsFloat = N.FloatValue;
            value.MakeImmutable();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclIntLiteral(TBoldOclIntLiteral N)
    {
        if (N.Value is null)
        {
            TBAInteger value = MakeNewInteger();
            N.SetOwnedValue(value);
            value.AsInteger = N.IntValue;
            value.MakeImmutable();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclStrLiteral(TBoldOclStrLiteral N)
    {
        if (N.Value is null)
        {
            TBAString value = MakeNewString();
            N.SetOwnedValue(value);
            value.AsString = N.StrValue;
            value.MakeImmutable();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclDateLiteral(TBoldOclDateLiteral N)
    {
        if (N.Value is null)
        {
            TBADate value = MakeNewDate();
            N.SetOwnedValue(value);
            value.AsDate = N.DateValue;
            value.MakeImmutable();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void VisitTBoldOclTimeLiteral(TBoldOclTimeLiteral N)
    {
        if (N.Value is null)
        {
            TBATime value = MakeNewTime();
            N.SetOwnedValue(value);
            value.AsTime = N.TimeValue;
            value.MakeImmutable();
        }
    }
}
