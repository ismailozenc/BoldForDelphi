// Delphi: Source/ObjectSpace/Ocl/BoldOclClasses.pas
// Port map: CSharp/port/units/BoldOclClasses.md
// The OCL syntax tree (nodes are indirect elements, so each holds its value after evaluation), the visitor, the
// variable environment, and the symbol dictionary with its helper.

using Bold;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclClasses", Symbol = "TBoldOClSymbolClass", Note = "Class references become System.Type or factory delegates; the symbol table holds instances.")]

namespace Bold;

/// <summary>Set, bag or sequence.</summary>
[Delphi(PortStatus.Ported)]
public enum TBOCollectionKind
{
    OCLSet,
    OCLBag,
    OCLSequence,
}

/// <summary>Which iteration an operation is.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldOCLIteratorSpecifier
{
    OclNoIterator,
    OclSelect,
    OclReject,
    OCLCollect,
    OclIterate,
    OclExists,
    OclForAll,
    OclOrderBy,
    OclOrderDescending,
    OclUnique,
}

/// <summary>How the type of an operation's result is deduced from its arguments.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldOclDeduceMethod
{
    tbodNo,
    tbodCopyLoopVar,
    tbodCopyArg1,
    tbodCopyArg1Elem,
    tbodCopyArg2,
    tbodCopyArg3,
    tbodLCC,
    tbodLCC23,
    tbodListofArg2,
    TbodObjectlist,
    tbodType,
    tbodTypecast,
    tbodArg1AsList,
    tbodListFromArg2,
}

/// <summary>An OCL operation as a procedure of its argument values and result.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldOCL_Operation(TBoldIndirectElement[] Args, TBoldIndirectElement Result);

/// <summary>What a symbol's Evaluate gets: the argument nodes and values, the result node, the subscriber and the system.</summary>
[Delphi(PortStatus.Ported, Note = "A class where Delphi has a record passed as const (by reference).")]
public class TBoldOclSymbolParameters
{
    public readonly TBoldOclNode?[] Nodes = new TBoldOclNode?[11];
    public readonly TBoldElement?[] Values = new TBoldElement?[11];
    public TBoldOclNode? Result;
    public TBoldSubscriber? Subscriber;
    public TBoldSystem? System;
    public TBoldSystemTypeInfo? SystemTypeInfo;
}

/// <summary>A list of nodes; it does not own them unless ClearAndFree is used.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclNodeList : TBoldMemoryManagedObject
{
    private readonly List<TBoldOclNode?> FList = [];

    [Delphi(PortStatus.Ported)]
    public TBoldOclNodeList()
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldOclNodeList.Destroy", Note = "Freed the list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    private TBoldOclNode? GetItem(int index) => FList[index];

    [Delphi(PortStatus.Ported)] private void PutItem(int index, TBoldOclNode? Value) => FList[index] = Value;

    [Delphi(PortStatus.Ported)]
    public TBoldOclNode? this[int index]
    {
        get => GetItem(index);
        set => PutItem(index, value);
    }

    [Delphi(PortStatus.Ported)]
    public int Add(TBoldOclNode? Item)
    {
        FList.Add(Item);
        return FList.Count - 1;
    }

    [Delphi(PortStatus.Ported)] public void Clear() => FList.Clear();

    /// <summary>Frees the nodes and clears the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void ClearAndFree()
    {
        for (int I = Count() - 1; I >= 0; I--)
        {
            FList[I]?.Dispose();
            FList[I] = null;
        }

        FList.Clear();
    }

    [Delphi(PortStatus.Ported)] public void Delete(int index) => FList.RemoveAt(index);
    [Delphi(PortStatus.Ported)] public void Insert(int index, TBoldOclNode? Item) => FList.Insert(index, Item);

    [Delphi(PortStatus.Ported)]
    public void Move(int CurIndex, int NewIndex)
    {
        TBoldOclNode? item = FList[CurIndex];
        FList.RemoveAt(CurIndex);
        FList.Insert(NewIndex, item);
    }

    [Delphi(PortStatus.Ported)]
    public int Remove(TBoldOclNode? Item)
    {
        int index = FList.IndexOf(Item);
        if (index >= 0)
        {
            FList.RemoveAt(index);
        }

        return index;
    }

    [Delphi(PortStatus.Ported)] public int Count() => FList.Count;
}

/// <summary>The variables in scope (names uppercased), with an outer scope.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclEnvironment : TBoldMemoryManagedObject, IDisposable
{
    private int GenSymCounter;
    private readonly TBoldOclEnvironment? fOuterScope;
    private readonly List<TBoldOclVariableBinding> fList = [];

    [Delphi(PortStatus.Ported)]
    public TBoldOclEnvironment(TBoldOclEnvironment? OuterScope)
    {
        GenSymCounter = 0;
        fOuterScope = OuterScope;
    }

    /// <summary>Frees the bindings left.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldOclEnvironment.Destroy")]
    public void Dispose()
    {
        while (fList.Count != 0)
        {
            popBinding().Dispose();
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] private int GetCount() => fList.Count;
    [Delphi(PortStatus.Ported)] private TBoldOclVariableBinding GetBindings(int Index) => fList[Index];
    [Delphi(PortStatus.Ported)] public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public void pushBinding(TBoldOclVariableBinding B)
    {
        B.VariableName = SysUtils.UpperCase(B.VariableName);
        fList.Add(B);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOclVariableBinding popBinding()
    {
        TBoldOclVariableBinding result = this[Count - 1];
        fList.RemoveAt(Count - 1);
        return result;
    }

    /// <summary>The innermost binding of the name (any case), looking in the outer scopes too.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOclVariableBinding? Lookup(string S)
    {
        S = SysUtils.UpperCase(S);
        for (int I = fList.Count - 1; I >= 0; I--)
        {
            if (this[I].VariableName == S)
            {
                return this[I];
            }
        }

        return fOuterScope?.Lookup(S);
    }

    [Delphi(PortStatus.Ported)] public TBoldOclVariableBinding? lookupSelf() => Lookup("SELF");
    [Delphi(PortStatus.Ported)] public TBoldOclVariableBinding CurrentImplicitVariable() => this[Count - 1];

    [Delphi(PortStatus.Ported)]
    public string MakeGenSymName()
    {
        string result = SysUtils.IntToStr(GenSymCounter) + "#GenSym";
        GenSymCounter++;
        return result;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclEnvironment.Bindings")] public TBoldOclVariableBinding this[int Index] => GetBindings(Index);
}

/// <summary>Visits nodes; each visit does nothing here.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclVisitor : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclNode(TBoldOclNode N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclListCoercion(TBoldOCLListCoercion N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclCollectionLiteral(TBoldOclCollectionLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclOperation(TBoldOclOperation N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclIteration(TBoldOclIteration N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclMember(TBoldOclMember N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclMethod(TBoldOclMethod N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclLiteral(TBoldOclLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclStrLiteral(TBoldOclStrLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclNumericLiteral(TBoldOclNumericLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclEnumLiteral(TBoldOclEnumLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclIntLiteral(TBoldOclIntLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclMomentLiteral(TBoldOCLMomentLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclDateLiteral(TBoldOclDateLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclTimeLiteral(TBoldOclTimeLiteral N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclVariableBinding(TBoldOclVariableBinding N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclVariableReference(TBoldOclVariableReference N) { }
    [Delphi(PortStatus.Ported)] public virtual void VisitTBoldOclTypeNode(TBoldOclTypeNode N) { }
}

/// <summary>A node of the syntax tree: its type (set by the semantic check), position, and its value after evaluation.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclNode : TBoldIndirectElement
{
    protected TBoldElementTypeInfo? fBoldType;

    public int Position;

    [Delphi(PortStatus.Ported)]
    public TBoldOclNode()
    {
        IsConstant = false;
    }

    [Delphi(PortStatus.Ported)] protected void SetBoldType(TBoldElementTypeInfo? NewType) => fBoldType = NewType;
    [Delphi(PortStatus.Ported)] protected TBoldElementTypeInfo? GetBoldType() => fBoldType;

    [Delphi(PortStatus.Ported)]
    public virtual void AcceptVisitor(TBoldOclVisitor V)
    {
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElementTypeInfo? BoldType
    {
        get => GetBoldType();
        set => SetBoldType(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool NeedsListCoercion
    {
        get => GetElementFlag(BoldElements.befNeedsListCoercion);
        set => SetElementFlag(BoldElements.befNeedsListCoercion, value);
    }

    [Delphi(PortStatus.Ported)]
    public bool IsConstant
    {
        get => GetElementFlag(BoldElements.befIsConstant);
        set => SetElementFlag(BoldElements.befIsConstant, value);
    }

    [Delphi(PortStatus.Ported)]
    public bool Resubscribe
    {
        get => GetElementFlag(BoldElements.befResubscribe);
        set => SetElementFlag(BoldElements.befResubscribe, value);
    }

    [Delphi(PortStatus.Ported)]
    public bool HastemporaryDummyValue
    {
        get => GetElementFlag(BoldElements.befHastemporaryDummyValue);
        set => SetElementFlag(BoldElements.befHastemporaryDummyValue, value);
    }
}

/// <summary>A type name used as a value (Person in Person.allInstances).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclTypeNode : TBoldOclNode
{
    public string typeName = "";
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclTypeNode(this);
}

/// <summary>Makes a single value of its child a list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOCLListCoercion : TBoldOclNode
{
    public TBoldOclNode? Child;

    [Delphi(PortStatus.Ported, Symbol = "TBoldOCLListCoercion.Destroy")]
    public override void Dispose()
    {
        Child?.Dispose();
        Child = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclListCoercion(this);
}

/// <summary>An operation (or iteration) with its arguments (the first is the object it is applied to) and its symbol.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclOperation : TBoldOclNode
{
    public TBoldOCLIteratorSpecifier IteratorSpecifier;
    public bool IsMethod;
    public TBoldOclNodeList? Args;
    public TBoldOclSymbol? Symbol;
    public string OperationName = "";

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclOperation.Destroy")]
    public override void Dispose()
    {
        if (Args is not null)
        {
            for (int I = 0; I < Args.Count(); I++)
            {
                Args[I]?.Dispose();
                Args[I] = null;
            }

            Args = null;
        }

        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclOperation(this);
}

/// <summary>An iteration (select, collect, ...) with its loop variable.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclIteration : TBoldOclOperation
{
    public TBoldOclVariableBinding? LoopVar;

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclIteration.Destroy")]
    public override void Dispose()
    {
        LoopVar?.Dispose();
        LoopVar = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclIteration(this);
}

/// <summary>A member (attribute or role) of a node's value, with its qualifier.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclMember : TBoldOclNode
{
    public TBoldOclNode? MemberOf;
    public string MemberName = "";
    public TBoldElementTypeInfo? MemberType;
    public int MemberIndex;
    public TBoldOclNodeList? Qualifier;
    public TBoldMemberRTInfo? RTInfo;

    [Delphi(PortStatus.Ported)]
    public TBoldOclMember()
    {
        RTInfo = null;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclMember.Destroy")]
    public override void Dispose()
    {
        MemberOf?.Dispose();
        MemberOf = null;
        Qualifier?.ClearAndFree();
        Qualifier = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclMember(this);
}

/// <summary>A method call on a node's value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclMethod : TBoldOclOperation
{
    public TBoldOclNode? MethodOf;
    public bool MethodOf_AddedToArgs;

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclMethod.Destroy")]
    public override void Dispose()
    {
        if (!MethodOf_AddedToArgs)
        {
            MethodOf?.Dispose();
        }

        MethodOf = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclMethod(this);
}

/// <summary>A variable: a loop variable, self, or an external variable; its value is the variable's current value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclVariableBinding : TBoldOclNode
{
    public string VariableName = "";

    /// <summary>The type the parser read (select(i: Person | ...)), until the semantic check moves it to the node's type.</summary>
    public TStringList? TypeNameList;

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclVariableBinding.Destroy")]
    public override void Dispose()
    {
        TypeNameList = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclVariableBinding(this);
}

/// <summary>A use of a variable; the binding is not part of the tree.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclVariableReference : TBoldOclNode
{
    public string VariableName = "";
    public TBoldOclVariableBinding? VariableBinding;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclVariableReference(this);
}

/// <summary>A literal value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclLiteral : TBoldOclNode
{
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclLiteral(this);
}

/// <summary>A literal collection: a list of elements, or a range of integers.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclCollectionLiteral : TBoldOclLiteral
{
    public TBOCollectionKind CollectionKind;
    public bool IsRange;
    public TBoldOclNode? RangeStart;
    public TBoldOclNode? RangeStop;
    public TBoldOclNodeList? Elements;

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclCollectionLiteral.Destroy")]
    public override void Dispose()
    {
        RangeStart?.Dispose();
        RangeStart = null;
        RangeStop?.Dispose();
        RangeStop = null;
        Elements?.ClearAndFree();
        Elements = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclCollectionLiteral(this);
}

[Delphi(PortStatus.Ported)]
public class TBoldOclStrLiteral : TBoldOclLiteral
{
    public string StrValue = "";
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclStrLiteral(this);
}

[Delphi(PortStatus.Ported)]
public class TBoldOclNumericLiteral : TBoldOclLiteral
{
    public double FloatValue;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclNumericLiteral(this);
}

[Delphi(PortStatus.Ported)]
public class TBoldOclIntLiteral : TBoldOclNumericLiteral
{
    public int IntValue;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclIntLiteral(this);
}

[Delphi(PortStatus.Ported)]
public class TBoldOCLMomentLiteral : TBoldOclLiteral
{
    protected TDateTime fDateTimeValue;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclMomentLiteral(this);
}

[Delphi(PortStatus.Ported)]
public class TBoldOclDateLiteral : TBoldOCLMomentLiteral
{
    [Delphi(PortStatus.Ported)] private TDateTime GetDateTimeValue() => fDateTimeValue;
    [Delphi(PortStatus.Ported)] private void SetDateTimeValue(TDateTime Value) => fDateTimeValue = Value;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclDateLiteral(this);

    [Delphi(PortStatus.Ported)]
    public TDateTime DateValue
    {
        get => GetDateTimeValue();
        set => SetDateTimeValue(value);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldOclTimeLiteral : TBoldOCLMomentLiteral
{
    [Delphi(PortStatus.Ported)] private TDateTime GetDateTimeValue() => fDateTimeValue;
    [Delphi(PortStatus.Ported)] private void SetDateTimeValue(TDateTime Value) => fDateTimeValue = Value;
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclTimeLiteral(this);

    [Delphi(PortStatus.Ported)]
    public TDateTime TimeValue
    {
        get => GetDateTimeValue();
        set => SetDateTimeValue(value);
    }
}

/// <summary>An enumeration literal (#name).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclEnumLiteral : TBoldOclLiteral
{
    public string name = "";
    [Delphi(PortStatus.Ported)] public override void AcceptVisitor(TBoldOclVisitor V) => V.VisitTBoldOclEnumLiteral(this);
}

/// <summary>Indexes symbols by name.</summary>
[Delphi(PortStatus.Ported)]
internal class TSymbolNameIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)] protected override string ItemAsKeyString(object? Item) => ((TBoldOclSymbol)Item!).SymbolName;
}

/// <summary>The OCL operations, by name, and the helper with the basic types.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSymbolDictionary : TBoldIndexableList
{
    private static int IX_SymbolName = -1;
    private readonly TBoldOclSymbolHelp fhelp;

    /// <summary>A dictionary for the system type; ErrorsEncountered tells whether basic types are missing (raises instead when a system is given).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSymbolDictionary(TBoldSystemTypeInfo SystemTypeInfo, TBoldSystem? BoldSystem, ref bool ErrorsEncountered)
    {
        SetIndexCapacity(1);
        fhelp = new TBoldOclSymbolHelp(SystemTypeInfo, BoldSystem, ref ErrorsEncountered);
        BoldIndexableList.SetIndexVariable(ref IX_SymbolName, AddIndex(new TSymbolNameIndex()));
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldSymbolDictionary.Destroy", Note = "Freed the helper; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    private TBoldOclSymbol? GetSymbol(string Name) => (TBoldOclSymbol?)((TSymbolNameIndex)Indexes(IX_SymbolName)!).FindByString(Name);

    [Delphi(PortStatus.Ported)] private TBoldOclSymbol GetSymbolByIndex(int index) => (TBoldOclSymbol)this[index]!;

    [Delphi(PortStatus.Ported)] public TBoldOclSymbolHelp help => fhelp;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldOclSymbol? SymbolByName(string name) => GetSymbol(name);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldOclSymbol Symbols(int i) => GetSymbolByIndex(i);
}

/// <summary>The basic OCL types of a system, and the making of new values of them in result slots.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclSymbolHelp : TBoldMemoryManagedObject
{
    private readonly TBoldAttributeTypeInfo? fNumericType;
    private readonly TBoldAttributeTypeInfo? fIntegerType;
    private readonly TBoldAttributeTypeInfo? fBooleanType;
    private readonly TBoldAttributeTypeInfo? fRealType;
    private readonly TBoldAttributeTypeInfo? fDateType;
    private readonly TBoldAttributeTypeInfo? fDateTimeType;
    private readonly TBoldAttributeTypeInfo? fTimeType;
    private readonly TBoldAttributeTypeInfo? fCurrencyType;
    private readonly TBoldAttributeTypeInfo? fMomentType;
    private readonly TBoldAttributeTypeInfo? fConstraintType;
    private readonly TBoldAttributeTypeInfo? fStringType;
    private readonly TBoldTypeTypeInfo fTypeType;
    private readonly TBoldClassTypeInfo fObjectType;
    private readonly TBoldListTypeInfo? fListType;
    private readonly TBoldListTypeInfo? fObjectListType;
    private readonly TBoldListTypeInfo? fNumericListType;
    private readonly TBoldListTypeInfo? fMomentListType;
    private readonly TBoldListTypeInfo? fStringListType;
    private readonly TBoldListTypeInfo? fTypeListType;
    private readonly TBoldSystemTypeInfo fSystemTypeInfo;

    /// <summary>Finds the basic types; a missing or wrong one raises when a system is given, else sets ErrorsEncountered.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOclSymbolHelp(TBoldSystemTypeInfo SystemTypeInfo, TBoldSystem? BoldSystem, ref bool ErrorsEncountered)
    {
        bool errors = ErrorsEncountered;

        void SignalError(string Message, params object?[] args)
        {
            if (BoldSystem is not null)
            {
                throw new EBoldOCLError(SysUtils.Format(Message, args));
            }

            errors = true;
        }

        TBoldAttributeTypeInfo? InstallAttribute(string Name, Type AttrClass, bool Exact)
        {
            TBoldAttributeTypeInfo? AttrTypeInfo = SystemTypeInfo.AttributeTypeInfoByExpressionName(Name);
            if (AttrTypeInfo is null)
            {
                SignalError(BoldCoreConsts.sMissingOCLType, Name);
                return null;
            }

            if (AttrTypeInfo.AttributeClass is null)
            {
                SignalError(BoldCoreConsts.sMissingDelphiType, Name, AttrTypeInfo.DelphiName);
            }
            else if (Exact && AttrTypeInfo.AttributeClass != AttrClass)
            {
                SignalError(BoldCoreConsts.sTypeMustBeX, Name, AttrClass.Name, AttrTypeInfo.AttributeClass.Name);
            }
            else if (!Exact && !AttrClass.IsAssignableFrom(AttrTypeInfo.AttributeClass))
            {
                SignalError(BoldCoreConsts.sTypeMustInheritFromX, Name, AttrClass.Name, AttrTypeInfo.AttributeClass.Name);
            }

            return AttrTypeInfo;
        }

        fSystemTypeInfo = SystemTypeInfo;
        fTypeType = (TBoldTypeTypeInfo)SystemTypeInfo.BoldType!;
        fNumericType = InstallAttribute("Numeric", typeof(TBANumeric), true);
        fRealType = InstallAttribute("Float", typeof(TBAFloat), false);
        fStringType = InstallAttribute("String", typeof(TBAString), false);
        fIntegerType = InstallAttribute("Integer", typeof(TBAInteger), false);
        fBooleanType = InstallAttribute("Boolean", typeof(TBABoolean), false);
        fCurrencyType = InstallAttribute("Currency", typeof(TBACurrency), false);
        fMomentType = InstallAttribute("Moment", typeof(TBAMoment), false);
        fConstraintType = InstallAttribute("Constraint", typeof(TBAConstraint), false);
        fDateType = InstallAttribute("Date", typeof(TBADate), false);
        fDateTimeType = InstallAttribute("DateTime", typeof(TBADateTime), false);
        fTimeType = InstallAttribute("Time", typeof(TBATime), false);
        fObjectType = SystemTypeInfo.RootClassTypeInfo;
        fListType = SystemTypeInfo.ListTypeInfoByElement(null);
        fNumericListType = SystemTypeInfo.ListTypeInfoByElement(NumericType);
        fMomentListType = SystemTypeInfo.ListTypeInfoByElement(MomentType);
        fTypeListType = SystemTypeInfo.ListTypeInfoByElement(TypeType);
        fObjectListType = SystemTypeInfo.ListTypeInfoByElement(ObjectType);
        fStringListType = SystemTypeInfo.ListTypeInfoByElement(StringType);
        ErrorsEncountered = errors;
    }

    /// <summary>Gives the slot a new value of the type (of the node's own type when that conforms), or recycles an owned one.</summary>
    [Delphi(PortStatus.Ported)]
    private void fMakeNew(TBoldIndirectElement el, TBoldElementTypeInfo NewType)
    {
        if (!el.OwnsValue || el.Value is null)
        {
            if (el is TBoldOclNode { BoldType: not null } node && node.BoldType.ConformsTo(NewType))
            {
                el.SetOwnedValue(CreateNewMember(node.BoldType));
            }
            else
            {
                el.SetOwnedValue(CreateNewMember(NewType));
            }
        }
        else if (el.Value is TBoldAttribute attribute)
        {
            attribute.RecycleValue();
        }
    }

    /// <summary>A free-standing member of the type; object lists do not subscribe to their objects.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldMember CreateNewMember(TBoldElementTypeInfo? BoldType)
    {
        TBoldMember result = TBoldMemberFactory.CreateMemberFromBoldType(BoldType);
        if (result is TBoldObjectList list)
        {
            list.SubscribeToObjectsInList = false;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo SystemTypeInfo => fSystemTypeInfo;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? NumericType => fNumericType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? BooleanType => fBooleanType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? IntegerType => fIntegerType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? RealType => fRealType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? DateType => fDateType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? TimeType => fTimeType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? DateTimeType => fDateTimeType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? CurrencyType => fCurrencyType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? MomentType => fMomentType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? ConstraintType => fConstraintType;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? StringType => fStringType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? ListType => fListType;
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo ObjectType => fObjectType;
    [Delphi(PortStatus.Ported)] public TBoldTypeTypeInfo TypeType => fTypeType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? NumericListType => fNumericListType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? MomentListType => fMomentListType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? StringListType => fStringListType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? TypeListType => fTypeListType;
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? ObjectListType => fObjectListType;

    [Delphi(PortStatus.Ported)]
    public void MakeNewNumeric(TBoldIndirectElement El, double value)
    {
        fMakeNew(El, RealType!);
        ((TBAFloat)El.Value!).AsFloat = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewTime(TBoldIndirectElement El, TDateTime value)
    {
        fMakeNew(El, TimeType!);
        ((TBATime)El.Value!).AsTime = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewDateTime(TBoldIndirectElement El, TDateTime value)
    {
        fMakeNew(El, DateTimeType!);
        ((TBADateTime)El.Value!).AsDateTime = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewDate(TBoldIndirectElement El, TDateTime value)
    {
        fMakeNew(El, DateType!);
        ((TBADate)El.Value!).AsDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewBoolean(TBoldIndirectElement El, bool value)
    {
        fMakeNew(El, BooleanType!);
        ((TBABoolean)El.Value!).AsBoolean = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewInteger(TBoldIndirectElement El, int value)
    {
        fMakeNew(El, IntegerType!);
        ((TBAInteger)El.Value!).AsInteger = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewString(TBoldIndirectElement El, string value)
    {
        fMakeNew(El, StringType!);
        ((TBAString)El.Value!).AsString = value;
    }

    [Delphi(PortStatus.Ported)]
    public void MakeNewCurrency(TBoldIndirectElement El, decimal value)
    {
        fMakeNew(El, CurrencyType!);
        ((TBACurrency)El.Value!).AsCurrency = value;
    }

    /// <summary>Moves an owned mutable value to the destination, else gives it a clone (or nothing for no value).</summary>
    [Delphi(PortStatus.Ported)]
    public void TransferOrClone(TBoldIndirectElement source, TBoldIndirectElement dest)
    {
        if (source.Value is null)
        {
            dest.SetReferenceValue(null);
        }
        else if (source.OwnsValue && source.Value.Mutable)
        {
            source.TransferValue(dest);
        }
        else if (source.Value is TBoldMember member)
        {
            dest.SetOwnedValue(member.Clone());
        }
        else
        {
            throw new EBoldInternal(SysUtils.Format("TransferOrClone: Unable to clone Object of type %s", source.Value.GetType().Name));
        }
    }
}

/// <summary>An OCL operation: its name, formal argument types, result type (or how to deduce it) and how it evaluates.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldOclSymbol : TBoldMemoryManagedObject
{
    private readonly TBoldOclSymbolHelp fHelp;
    private string fSymbolName = "";
    private readonly List<TBoldElementTypeInfo?> fFormalArguments = [];
    private TBoldOclDeduceMethod fDeduceMethod;
    private TBoldElementTypeInfo? fResultType;
    private readonly bool fIsDotNotation;
    private bool fIsPostFix;
    private int fHelpContext;
    private bool fArgsNeedCommonType;

    /// <summary>Runs Init (the subclass's InternalInit call) and works out the dot notation.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldOclSymbol(TBoldOclSymbolHelp Help)
    {
        fHelp = Help;
#pragma warning disable CA2214 // Delphi calls the virtual Init from the constructor too; subclasses only call InternalInit there.
        Init();
#pragma warning restore CA2214
        fIsDotNotation = IsPostfix && (FormalArguments(0) is null || FormalArguments(0)!.BoldValueType != TBoldValueTypeSet.bvtList);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldOclSymbol.Destroy", Note = "Freed the argument list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected void InternalInit(string Name, TBoldElementTypeInfo?[] Args, TBoldOclDeduceMethod DeduceMethod, TBoldElementTypeInfo? resultType, bool IsPostfix,
        int HelpContext, bool ArgsNeedCommonType = false)
    {
        fSymbolName = Name;
        fHelpContext = HelpContext;
        fFormalArguments.AddRange(Args);
        if (resultType is not null)
        {
            fResultType = resultType;
        }
        else
        {
            fDeduceMethod = DeduceMethod;
        }

        fIsPostFix = IsPostfix;
        fArgsNeedCommonType = ArgsNeedCommonType;
    }

    [Delphi(PortStatus.Ported)] protected TBoldElementTypeInfo? GetFormalArguments(int index) => fFormalArguments[index];
    [Delphi(PortStatus.Ported)] protected int GetNumberOfArgs() => fFormalArguments.Count;
    [Delphi(PortStatus.Ported)] protected abstract void Init();

    [Delphi(PortStatus.Ported)] protected bool XBoolean(TBoldElement? Elem) => Elem is TBABoolean b && b.AsBoolean;
    [Delphi(PortStatus.Ported)] protected decimal XCurrency(TBoldElement? Elem) => Elem is TBACurrency c ? c.AsCurrency : 0;
    [Delphi(PortStatus.Ported)] protected int XInteger(TBoldElement? Elem) => Elem is TBAInteger { IsNull: false } i ? i.AsInteger : 0;
    [Delphi(PortStatus.Ported)] protected TBoldList? XList(TBoldElement? Elem) => Elem as TBoldList;

    [Delphi(PortStatus.Ported)]
    protected TDateTime XDateTime(TBoldElement? Elem) => Elem switch
    {
        TBADate d => d.AsDate,
        TBADateTime dt => dt.AsDateTime,
        TBATime t => t.AsTime,
        _ => SysUtils.ZeroDateTime,
    };

    [Delphi(PortStatus.Ported)] protected double XNumeric(TBoldElement? Elem) => Elem is TBANumeric n ? n.AsFloat : 0;
    [Delphi(PortStatus.Ported)] protected string XString(TBoldElement? Elem) => Elem?.AsString ?? "";
    [Delphi(PortStatus.Ported)] protected TBoldElementTypeInfo? XType(TBoldElement? Elem) => Elem as TBoldElementTypeInfo;
    [Delphi(PortStatus.Ported)] protected TBoldOclSymbolHelp Help => fHelp;

    [Delphi(PortStatus.Ported)]
    public abstract void Evaluate(TBoldOclSymbolParameters Params);

    /// <summary>The SQL for the operation, or empty when it has none.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void SQL(string[] Args, ref string result) => result = "";

    [Delphi(PortStatus.Ported)] public TBoldOclDeduceMethod DeduceMethod => fDeduceMethod;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldElementTypeInfo? FormalArguments(int Index) => GetFormalArguments(Index);

    [Delphi(PortStatus.Ported)] public int HelpContext => fHelpContext;
    [Delphi(PortStatus.Ported)] public bool isDotNotation => fIsDotNotation;
    [Delphi(PortStatus.Ported)] public bool IsPostfix => fIsPostFix;
    [Delphi(PortStatus.Ported)] public int NumberOfArgs => GetNumberOfArgs();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ResultType => fResultType;
    [Delphi(PortStatus.Ported)] public string SymbolName => fSymbolName;
    [Delphi(PortStatus.Ported)] public bool ArgsNeedCommonType => fArgsNeedCommonType;
}
