// Delphi: Source/ObjectSpace/Ocl/BoldOclLightWeightNodes.pas
// Port map: CSharp/port/units/BoldOclLightWeightNodes.md
// Light-weight OCL nodes: a copy of a syntax tree without type infos or values, small enough to stream to a persistence
// server, where it is turned into SQL. TBoldOclCondition carries such a tree with its context objects.

using System.Globalization;
using System.Xml;
using Bold;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOclCondition.Destroy", Note = "Freed the id list, environment and root node; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOLWNodeList.Destroy", Note = "Freed the owned nodes; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOLWListCoercion.Destroy", Note = "Freed the child; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOLWOperation.Destroy", Note = "Freed the arguments; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOLWIteration.Destroy", Note = "Freed the loop variable; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclLightWeightNodes", Symbol = "TBoldOLWMember.Destroy", Note = "Freed the context and qualifiers; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Stream names of the light-weight nodes, the binding helpers and the streamer registration.</summary>
public static partial class BoldOclLightWeightNodes
{
    internal const string OCLConditionStreamName = "OCLCondition";
    internal const string OLWNodeListStreamName = "OLWNodeList";
    internal const string OLWNodeStreamName = "OLWNode";
    internal const string OLWTypeNodeStreamName = "OLWTypeNode";
    internal const string OLWListCoercionStreamName = "OLWListCoercion";
    internal const string OLWOperationStreamName = "OLWOperation";
    internal const string OLWIterationStreamName = "OLWIteration";
    internal const string OLWMemberStreamName = "OLWMember";
    internal const string OLWVariableBindingStreamName = "OLWVariableBinding";
    internal const string OLWVariableReferenceStreamName = "OLWVariableReference";
    internal const string OLWStrLiteralStreamName = "OLWStrLiteral";
    internal const string OLWFloatLiteralStreamName = "OLWFloatLiteral";
    internal const string OLWIntLiteralStreamName = "OLWIntLiteral";
    internal const string OLWEnumLiteralStreamName = "OLWEnumLiteral";
    internal const string OLWDateLiteralStreamName = "OLWDateLiteral";
    internal const string OLWTimeLiteralStreamName = "OLWTimeLiteral";

    /// <summary>Writes the index of a binding already written in this stream, or the binding itself the first time.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void WriteBindingToStream(TBoldOLWVariableBinding? Binding, TBoldXMLNode Node)
    {
        var Bindings = (TBoldOLWNodeList)Node.GetStateObject("Bindings")!;
        int BindingIndex = Binding is null ? -1 : Bindings.IndexOf(Binding);
        Node.WriteSubNodeInteger("BindingIndex", BindingIndex);
        if (BindingIndex == -1)
        {
            Node.WriteSubNodeObject("Binding", OLWVariableBindingStreamName, Binding);
            if (Binding is not null)
            {
                Bindings.Add(Binding);
            }
        }
    }

    /// <summary>Reads a binding written by WriteBindingToStream, so references share the binding they had.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldOLWVariableBinding? ReadBindingFromStream(TBoldXMLNode Node)
    {
        var Bindings = (TBoldOLWNodeList)Node.GetStateObject("Bindings")!;
        int BindingIndex = Node.ReadSubNodeInteger("BindingIndex");
        if (BindingIndex != -1)
        {
            return (TBoldOLWVariableBinding)Bindings[BindingIndex];
        }

        var result = (TBoldOLWVariableBinding?)Node.ReadSubNodeObject("Binding", OLWVariableBindingStreamName);
        if (result is not null)
        {
            Bindings.Add(result);
        }

        return result;
    }

    /// <summary>Registers the condition and node streamers (called from the Bold.ObjectSpace module initializer).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Run by a module initializer when the assembly loads.")]
#pragma warning disable CA2255 // The streamers must be registered before a condition is streamed, as Delphi's initialization section does.
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    internal static void RegisterStreamers()
    {
        TBoldXMLStreamerRegistry main = TBoldXMLStreamerRegistry.MainStreamerRegistry();
        main.RegisterStreamer(new TBoldXMLOCLConditionStreamer());
        main.RegisterStreamer(new TBoldXMLOLWNodeListStreamer());
        main.RegisterStreamer(new TBoldXMLOLWTypeNodeStreamer());
        main.RegisterStreamer(new TBoldXMLOLWListCoercionStreamer());
        main.RegisterStreamer(new TBoldXMLOLWOperationStreamer());
        main.RegisterStreamer(new TBoldXMLOLWIterationStreamer());
        main.RegisterStreamer(new TBoldXMLOLWMemberStreamer());
        main.RegisterStreamer(new TBoldXMLOLWVariableBindingStreamer());
        main.RegisterStreamer(new TBoldXMLOLWVariableReferenceStreamer());
        main.RegisterStreamer(new TBoldXMLOLWStrLiteralStreamer());
        main.RegisterStreamer(new TBoldXMLOLWDateLiteralStreamer());
        main.RegisterStreamer(new TBoldXMLOLWTimeLiteralStreamer());
        main.RegisterStreamer(new TBoldXMLOLWFloatLiteralStreamer());
        main.RegisterStreamer(new TBoldXMLOLWIntLiteralStreamer());
        main.RegisterStreamer(new TBoldXMLOLWEnumLiteralStreamer());
    }
}

/// <summary>A condition given as a light-weight OCL tree, evaluated in the persistence layer; Context holds the ids of the root objects.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclCondition : TBoldConditionWithClass
{
    internal TBoldOLWNode? fRootNode;
    internal TBoldObjectIdList fContext;
    internal TBoldOLWNodeList fEnv;
    internal string fOclExpr = "";

    [Delphi(PortStatus.Ported)]
    public TBoldOclCondition()
    {
        fContext = new TBoldObjectIdList();
        fEnv = new TBoldOLWNodeList();
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OCLConditionStreamName;

    [Delphi(PortStatus.Ported)] public TBoldOLWNode? RootNode { get => fRootNode; set => fRootNode = value; }
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList Context => fContext;
    [Delphi(PortStatus.Ported)] public TBoldOLWNodeList Env => fEnv;
    [Delphi(PortStatus.Ported)] public string OclExpr { get => fOclExpr; set => fOclExpr = value; }
}

/// <summary>A visitor of light-weight nodes; every visit does nothing here.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWNodeVisitor
{
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWNode(TBoldOLWNode N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWListCoercion(TBoldOLWListCoercion N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWOperation(TBoldOLWOperation N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWIteration(TBoldOLWIteration N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWMember(TBoldOLWMember N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWLiteral(TBoldOLWLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWStrLiteral(TBoldOLWStrLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWDateLiteral(TBoldOLWDateLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWTimeLiteral(TBoldOLWTimeLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWFloatLiteral(TBoldOLWFloatLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWEnumLiteral(TBoldOLWEnumLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWIntLiteral(TBoldOLWIntLiteral N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWVariableBinding(TBoldOLWVariableBinding N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWVariableReference(TBoldOLWVariableReference N) { }
    [Delphi(PortStatus.Ported)] protected internal virtual void VisitTBoldOLWTypeNode(TBoldOLWTypeNode N) { }
}

/// <summary>A list of light-weight nodes, owning them by default.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWNodeList : TBoldNonRefCountedObject, IBoldStreamable
{
    private readonly List<TBoldOLWNode> fList = [];
    private bool fOwnsObjects;

    [Delphi(PortStatus.Ported)]
    public TBoldOLWNodeList()
    {
        fOwnsObjects = true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldOLWNode GetItem(int index) => fList[index];

    [Delphi(PortStatus.Ported)]
    private void PutItem(int index, TBoldOLWNode Value) => fList[index] = Value;

    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => BoldOclLightWeightNodes.OLWNodeListStreamName;

    [Delphi(PortStatus.Ported)]
    private int GetCount() => fList.Count;

    [Delphi(PortStatus.Ported)]
    private int GetIndexOf(TBoldOLWNode Node) => fList.IndexOf(Node);

    [Delphi(PortStatus.Ported)]
    public int Add(TBoldOLWNode Item)
    {
        fList.Add(Item);
        return fList.Count - 1;
    }

    /// <summary>Removes all nodes without freeing them (Delphi's ExternalVarBindings.Clear on a TList).</summary>
    public void Clear() => fList.Clear();

    [Delphi(PortStatus.Ported)]
    public virtual void TraverseList(TBoldOLWNodeVisitor V)
    {
        for (int i = 0; i < Count; i++)
        {
            this[i].AcceptVisitor(V);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOLWNodeList.Items")]
    public TBoldOLWNode this[int index] { get => GetItem(index); set => PutItem(index, value); }

    [Delphi(PortStatus.Ported)] public int Count => GetCount();

    [Delphi(PortStatus.Ported, Symbol = "TBoldOLWNodeList.IndexOf", Note = "An indexed property in Delphi.")]
    public int IndexOf(TBoldOLWNode Node) => GetIndexOf(Node);

    [Delphi(PortStatus.Ported)] public bool OwnsObjects { get => fOwnsObjects; set => fOwnsObjects = value; }
}

/// <summary>A light-weight node: its position in the expression and a visitor hook.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldOLWNode : TBoldNonRefCountedObject, IBoldStreamable
{
    internal int fPosition;

    [Delphi(PortStatus.Ported)]
    protected TBoldOLWNode(int Position)
    {
        fPosition = Position;
    }

    [Delphi(PortStatus.Ported)]
    public abstract string GetStreamName();

    [Delphi(PortStatus.Ported)]
    public virtual void AcceptVisitor(TBoldOLWNodeVisitor V) => V.VisitTBoldOLWNode(this);

    [Delphi(PortStatus.Ported)] public int Position => fPosition;
}

/// <summary>A class used as a value (as in Person.allInstances), by name and topological index.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWTypeNode.Create", Note = "The primary constructor.")]
public class TBoldOLWTypeNode(int Position, string TypeName, int TopSortedIndex) : TBoldOLWNode(Position)
{
    internal string fTypeName = TypeName;
    internal int fTopSortedIndex = TopSortedIndex;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWTypeNodeStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWTypeNode(this);
    }

    [Delphi(PortStatus.Ported)] public string TypeName => fTypeName;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
}

/// <summary>A single value used as a list.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWListCoercion.Create", Note = "The primary constructor.")]
public class TBoldOLWListCoercion(int Position, TBoldOLWNode? Child) : TBoldOLWNode(Position)
{
    internal TBoldOLWNode? fChild = Child;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWListCoercionStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWListCoercion(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLWNode? Child => fChild;
}

/// <summary>An operation by name with its arguments (the first is the context).</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWOperation.Create", Note = "The primary constructor.")]
public class TBoldOLWOperation(int Position, string OperationName) : TBoldOLWNode(Position)
{
    internal TBoldOLWNodeList fArgs = new();
    internal string fOperationName = OperationName;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWOperationStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWOperation(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLWNodeList Args => fArgs;
    [Delphi(PortStatus.Ported)] public string OperationName => fOperationName;
}

/// <summary>An iterating operation (select, collect, ...) with its loop variable.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWIteration.Create", Note = "The primary constructor.")]
public class TBoldOLWIteration(int Position, string OperationName, TBoldOLWVariableBinding? LoopVar) : TBoldOLWOperation(Position, OperationName)
{
    internal TBoldOLWVariableBinding? fLoopVar = LoopVar;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWIterationStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWIteration(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLWVariableBinding? LoopVar => fLoopVar;
}

/// <summary>A member of the context node, by name and index, with qualifiers.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWMember : TBoldOLWNode
{
    internal int fMemberIndex;
    internal string fMemberName;
    internal TBoldOLWNode? fmemberOf;
    internal TBoldOLWNodeList fQualifier;
    internal bool fIsBoolean;

    [Delphi(PortStatus.Ported)]
    public TBoldOLWMember(int Position, string memberName, int MemberIndex, TBoldOLWNode? MemberOf, bool IsBoolean) : base(Position)
    {
        fMemberName = memberName;
        fMemberIndex = MemberIndex;
        fmemberOf = MemberOf;
        fQualifier = new TBoldOLWNodeList();
        fIsBoolean = IsBoolean;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWMemberStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWMember(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLWNode? MemberOf => fmemberOf;
    [Delphi(PortStatus.Ported)] public string MemberName => fMemberName;
    [Delphi(PortStatus.Ported)] public int MemberIndex => fMemberIndex;
    [Delphi(PortStatus.Ported)] public TBoldOLWNodeList Qualifier => fQualifier;
    [Delphi(PortStatus.Ported)] public bool IsBoolean => fIsBoolean;
}

/// <summary>A variable: a loop variable, self, or an external variable with its value.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWVariableBinding.Create", Note = "The primary constructor.")]
public class TBoldOLWVariableBinding(int Position, string VariableName, int TopSortedIndex) : TBoldOLWNode(Position)
{
    internal string fVariableName = VariableName;
    internal int fTopSortedIndex = TopSortedIndex;
    internal object? fExternalVarValue;
    internal bool fIsLoopVar;
    internal int fRefCount;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWVariableBindingStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWVariableBinding(this);
    }

    /// <summary>Counts a reference; raises on the second reference to a variable that is not a loop variable.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddRef()
    {
        fRefCount++;
        if (fRefCount > 1 && !fIsLoopVar)
        {
            throw new EBold(BoldCoreConsts.sExternalVarsCanOnlyBeReferencedOnce);
        }
    }

    [Delphi(PortStatus.Ported)] public string VariableName => fVariableName;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
    [Delphi(PortStatus.Ported, Note = "A Variant in Delphi.")] public object? ExternalVarvalue { get => fExternalVarValue; set => fExternalVarValue = value; }
    [Delphi(PortStatus.Ported)] public bool IsLoopVar { get => fIsLoopVar; set => fIsLoopVar = value; }
}

/// <summary>A use of a variable.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWVariableReference.Create", Note = "The primary constructor.")]
public class TBoldOLWVariableReference(int Position, TBoldOLWVariableBinding? VariableBinding) : TBoldOLWNode(Position)
{
    internal TBoldOLWVariableBinding? fVariableBinding = VariableBinding;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWVariableReferenceStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWVariableReference(this);
    }

    [Delphi(PortStatus.Ported)] public TBoldOLWVariableBinding? VariableBinding => fVariableBinding;
}

/// <summary>The base of the literals.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldOLWLiteral(int Position) : TBoldOLWNode(Position)
{
    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWLiteral(this);
    }
}

/// <summary>A string literal.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWStrLiteral.Create", Note = "The primary constructor.")]
public class TBoldOLWStrLiteral(int Position, string StrValue) : TBoldOLWLiteral(Position)
{
    internal string fStrValue = StrValue;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWStrLiteralStreamName;

    /// <summary>Visits only as a string literal (Delphi does not call the inherited visits here).</summary>
    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V) => V.VisitTBoldOLWStrLiteral(this);

    [Delphi(PortStatus.Ported)] public string StrValue => fStrValue;
}

/// <summary>The base of date and time literals.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWMomentLiteral.Create", Note = "The primary constructor.")]
public abstract class TBoldOLWMomentLiteral(int Position, TDateTime MomentValue) : TBoldOLWLiteral(Position)
{
    internal TDateTime fMomentValue = MomentValue;
}

/// <summary>A date literal.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWDateLiteral(int Position, TDateTime MomentValue) : TBoldOLWMomentLiteral(Position, MomentValue)
{
    [Delphi(PortStatus.Ported)]
    private TDateTime GetDateValue() => fMomentValue;

    [Delphi(PortStatus.Ported)]
    private void SetDateValue(TDateTime Value) => fMomentValue = Value;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWDateLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWDateLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public TDateTime DateValue { get => GetDateValue(); set => SetDateValue(value); }
}

/// <summary>A time literal.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOLWTimeLiteral(int Position, TDateTime MomentValue) : TBoldOLWMomentLiteral(Position, MomentValue)
{
    [Delphi(PortStatus.Ported)]
    private TDateTime GetTimeValue() => fMomentValue;

    [Delphi(PortStatus.Ported)]
    private void SetTimeValue(TDateTime Value) => fMomentValue = Value;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWTimeLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWTimeLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public TDateTime TimeValue { get => GetTimeValue(); set => SetTimeValue(value); }
}

/// <summary>A float literal.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWFloatLiteral.Create", Note = "The primary constructor.")]
public class TBoldOLWFloatLiteral(int Position, double FloatValue) : TBoldOLWLiteral(Position)
{
    internal double fFloatValue = FloatValue;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWFloatLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWFloatLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public double FloatValue => fFloatValue;
}

/// <summary>An integer literal.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWIntLiteral.Create", Note = "The primary constructor.")]
public class TBoldOLWIntLiteral(int Position, int IntValue) : TBoldOLWLiteral(Position)
{
    internal int fIntValue = IntValue;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWIntLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWIntLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public int IntValue => fIntValue;
}

/// <summary>An enumeration literal (#name) with the integer value it maps to.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldOLWEnumLiteral.Create", Note = "The primary constructor.")]
public class TBoldOLWEnumLiteral(int Position, string Name) : TBoldOLWLiteral(Position)
{
    internal string fName = Name;
    internal int fIntValue;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldOclLightWeightNodes.OLWEnumLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void AcceptVisitor(TBoldOLWNodeVisitor V)
    {
        base.AcceptVisitor(V);
        V.VisitTBoldOLWEnumLiteral(this);
    }

    [Delphi(PortStatus.Ported)] public string Name => fName;
    [Delphi(PortStatus.Ported)] public int Intvalue { get => fIntValue; set => fIntValue = value; }
}

/// <summary>Streams an OCL condition: environment, root node, context ids and the OCL text; bindings are shared through the stream state.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOCLConditionStreamer : TBoldXMLConditionWithClassStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OCLConditionStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var Condition = (TBoldOclCondition)Obj;
        var Bindings = new TBoldOLWNodeList { OwnsObjects = false };
        Node.AddStateObject("Bindings", Bindings);
        Condition.fEnv = (TBoldOLWNodeList?)Node.ReadSubNodeObject("Env", BoldOclLightWeightNodes.OLWNodeListStreamName) ?? new TBoldOLWNodeList();
        Condition.fRootNode = (TBoldOLWNode?)Node.ReadSubNodeObject("RootNode", "");
        Condition.fContext = (TBoldObjectIdList?)Node.ReadSubNodeObject("Context", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) ?? new TBoldObjectIdList();
        Condition.fOclExpr = Node.ReadSubNodeString("OCL");
        Node.RemoveStateObject("Bindings");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var Condition = (TBoldOclCondition)Obj;
        var Bindings = new TBoldOLWNodeList { OwnsObjects = false };
        Node.AddStateObject("Bindings", Bindings);
        Node.WriteSubNodeObject("Env", BoldOclLightWeightNodes.OLWNodeListStreamName, Condition.fEnv);
        Node.WriteSubNodeObject("RootNode", "", Condition.fRootNode);
        Node.WriteSubNodeObject("Context", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, Condition.fContext);
        Node.WriteSubNodeString("OCL", Condition.fOclExpr);
        Node.RemoveStateObject("Bindings");
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOclCondition();
}

/// <summary>Streams a node list as Node subnodes.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWNodeListStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWNodeListStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var OLWList = (TBoldOLWNodeList)Obj;
        foreach (XmlNode aNode in Node.XMLDomElement.ChildNodes)
        {
            if (aNode is not XmlElement element)
            {
                continue;
            }

            TBoldXMLNode aSubNode = Node.MakeNodeForElement(element);
            if (aSubNode.Accessor == "Node")
            {
                OLWList.Add((TBoldOLWNode)aSubNode.ReadObject("")!);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var OLWList = (TBoldOLWNodeList)Obj;
        for (int i = 0; i < OLWList.Count; i++)
        {
            TBoldXMLNode aSubNode = Node.NewSubNode("Node");
            aSubNode.WriteObject("", OLWList[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWNodeList();
}

/// <summary>Streams the position shared by all nodes.</summary>
[Delphi(PortStatus.Ported)]
internal abstract class TBoldXMLOLWNodeStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWNode)Obj).fPosition = Node.ReadSubNodeInteger("Position");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeInteger("Position", ((TBoldOLWNode)Obj).fPosition);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWTypeNodeStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWTypeNodeStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var OLWTypeNode = (TBoldOLWTypeNode)Obj;
        OLWTypeNode.fTypeName = Node.ReadSubNodeString("TypeName");
        OLWTypeNode.fTopSortedIndex = Node.ReadSubNodeInteger("TopSortedIndex");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var OLWTypeNode = (TBoldOLWTypeNode)Obj;
        Node.WriteSubNodeString("TypeName", OLWTypeNode.TypeName);
        Node.WriteSubNodeInteger("TopSortedIndex", OLWTypeNode.TopSortedIndex);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWTypeNode(0, "", -1);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWListCoercionStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWListCoercionStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWListCoercion)Obj).fChild = (TBoldOLWNode?)Node.ReadSubNodeObject("Child", "");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeObject("Child", "", ((TBoldOLWListCoercion)Obj).fChild);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWListCoercion(0, null);
}

[Delphi(PortStatus.Ported)]
internal class TBoldXMLOLWOperationStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWOperationStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var OLWOperation = (TBoldOLWOperation)Obj;
        OLWOperation.fArgs = (TBoldOLWNodeList?)Node.ReadSubNodeObject("Args", "") ?? new TBoldOLWNodeList();
        OLWOperation.fOperationName = Node.ReadSubNodeString("OperationName");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var OLWOperation = (TBoldOLWOperation)Obj;
        Node.WriteSubNodeObject("Args", "", OLWOperation.fArgs);
        Node.WriteSubNodeString("OperationName", OLWOperation.fOperationName);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWOperation(0, "");
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWIterationStreamer : TBoldXMLOLWOperationStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWIterationStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWIteration)Obj).fLoopVar = BoldOclLightWeightNodes.ReadBindingFromStream(Node);
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        BoldOclLightWeightNodes.WriteBindingToStream(((TBoldOLWIteration)Obj).fLoopVar, Node);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWIteration(0, "", null);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWMemberStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWMemberStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var OLWMember = (TBoldOLWMember)Obj;
        OLWMember.fMemberIndex = Node.ReadSubNodeInteger("MemberIndex");
        OLWMember.fMemberName = Node.ReadSubNodeString("MemberName");
        OLWMember.fmemberOf = (TBoldOLWNode?)Node.ReadSubNodeObject("MemberOf", "");
        OLWMember.fQualifier = (TBoldOLWNodeList?)Node.ReadSubNodeObject("Qualifier", BoldOclLightWeightNodes.OLWNodeListStreamName) ?? new TBoldOLWNodeList();
        OLWMember.fIsBoolean = Node.ReadSubNodeBoolean("IsBoolean");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var OLWMember = (TBoldOLWMember)Obj;
        Node.WriteSubNodeInteger("MemberIndex", OLWMember.fMemberIndex);
        Node.WriteSubNodeString("MemberName", OLWMember.fMemberName);
        Node.WriteSubNodeObject("MemberOf", "", OLWMember.fmemberOf);
        Node.WriteSubNodeObject("Qualifier", BoldOclLightWeightNodes.OLWNodeListStreamName, OLWMember.fQualifier);
        Node.WriteSubNodeBoolean("IsBoolean", OLWMember.fIsBoolean);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWMember(0, "", -1, null, false);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWVariableBindingStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWVariableBindingStreamName;

    [Delphi(PortStatus.Ported, Note = "The external value comes back as a string, as in Delphi (marked FIXME there).")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var OLWBinding = (TBoldOLWVariableBinding)Obj;
        OLWBinding.fVariableName = Node.ReadSubNodeString("VariableName");
        OLWBinding.fTopSortedIndex = Node.ReadSubNodeInteger("TopSortedIndex");
        OLWBinding.fIsLoopVar = Node.ReadSubNodeBoolean("IsLoopVar");
        OLWBinding.fRefCount = Node.ReadSubNodeInteger("RefCount");
        OLWBinding.fExternalVarValue = Node.ReadSubNodeString("ExternalVarValue");
    }

    [Delphi(PortStatus.Ported, Note = "Writes the external value as invariant text; Delphi converts the Variant with the locale.")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var OLWBinding = (TBoldOLWVariableBinding)Obj;
        Node.WriteSubNodeString("VariableName", OLWBinding.fVariableName);
        Node.WriteSubNodeInteger("TopSortedIndex", OLWBinding.fTopSortedIndex);
        Node.WriteSubNodeBoolean("IsLoopVar", OLWBinding.fIsLoopVar);
        Node.WriteSubNodeInteger("RefCount", OLWBinding.fRefCount);
        Node.WriteSubNodeString("ExternalVarValue", Convert.ToString(OLWBinding.fExternalVarValue, CultureInfo.InvariantCulture) ?? "");
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWVariableBinding(0, "", -1);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWVariableReferenceStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWVariableReferenceStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWVariableReference)Obj).fVariableBinding = BoldOclLightWeightNodes.ReadBindingFromStream(Node);
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        BoldOclLightWeightNodes.WriteBindingToStream(((TBoldOLWVariableReference)Obj).fVariableBinding, Node);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWVariableReference(0, null);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWStrLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWStrLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWStrLiteral)Obj).fStrValue = Node.ReadSubNodeString("StrValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeString("StrValue", ((TBoldOLWStrLiteral)Obj).fStrValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWStrLiteral(0, "");
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWFloatLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWFloatLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWFloatLiteral)Obj).fFloatValue = Node.ReadSubNodeFloat("FloatValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeFloat("FloatValue", ((TBoldOLWFloatLiteral)Obj).fFloatValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWFloatLiteral(0, 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWIntLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWIntLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWIntLiteral)Obj).fIntValue = Node.ReadSubNodeInteger("IntValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeInteger("IntValue", ((TBoldOLWIntLiteral)Obj).fIntValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWIntLiteral(0, 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWDateLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWDateLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWDateLiteral)Obj).DateValue = Node.ReadSubNodeDate("DateValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeDate("DateValue", ((TBoldOLWDateLiteral)Obj).DateValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWDateLiteral(0, SysUtils.ZeroDateTime);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWTimeLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWTimeLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldOLWTimeLiteral)Obj).TimeValue = Node.ReadSubNodeTime("TimeValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeTime("TimeValue", ((TBoldOLWTimeLiteral)Obj).TimeValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWTimeLiteral(0, SysUtils.ZeroDateTime);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOLWEnumLiteralStreamer : TBoldXMLOLWNodeStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldOclLightWeightNodes.OLWEnumLiteralStreamName;

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var EnumLiteral = (TBoldOLWEnumLiteral)Obj;
        EnumLiteral.fName = Node.ReadSubNodeString("Name");
        EnumLiteral.fIntValue = Node.ReadSubNodeInteger("IntValue");
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var EnumLiteral = (TBoldOLWEnumLiteral)Obj;
        Node.WriteSubNodeString("Name", EnumLiteral.fName);
        Node.WriteSubNodeInteger("IntValue", EnumLiteral.fIntValue);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOLWEnumLiteral(0, "");
}
