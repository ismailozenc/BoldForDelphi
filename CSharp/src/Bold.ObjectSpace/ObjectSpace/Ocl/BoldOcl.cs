// Delphi: Source/ObjectSpace/Ocl/BoldOcl.pas
// Port map: CSharp/port/units/BoldOcl.md
// The OCL evaluator: parses expressions (caching the trees by expression text), checks them in a context, and
// evaluates them in memory or, for object lists, in the persistence layer.

using System.Reflection;
using Bold;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldOcl", Symbol = "initialization", Note = "Empty in Delphi; nothing to do.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOcl", Symbol = "finalization", Note = "Freed the cached tables; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOcl", Symbol = "TBoldOcl.Destroy", Note = "Freed the dictionary, symbol table and global environment; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOcl", Symbol = "TBoldOclEntry.Destroy", Note = "Freed the self variable and the tree; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The OCL globals, the cached parser tables and the evaluator factories.</summary>
public static partial class BoldOcl
{
    /// <summary>Allow members that start with a capital letter (taken for type names otherwise).</summary>
    [Delphi(PortStatus.Ported)] public static bool BoldOCLAllowCapitalMembers = false;

    /// <summary>Compatibility with Bold's earlier expression language (a trailing dot is ignored, class names give class lists).</summary>
    [Delphi(PortStatus.Ported)] public static bool BoldNelCompatibility = false;

    /// <summary>Where every evaluated expression is logged, when set.</summary>
    [Delphi(PortStatus.Ported)] public static TBoldLogHandler? BoldOCLLogHandler = null;

    /// <summary>Supplies fixes for failing expressions at run time, when set.</summary>
    [Delphi(PortStatus.Ported)] public static TBoldOclRTDebugger? BoldOCLRTDebugger = null;

    /// <summary>Unused in Delphi as well (the tables are always read from the resources).</summary>
    [Delphi(PortStatus.Ported)] public static string BoldOclParserTablePath = "";

    private static SSLexTable? G_OclScannerTable;
    private static SSYaccTable? G_OclParserTable;
    private static int OclCounter;

    private static byte[]? ReadResource(string Name)
    {
        using Stream? stream = typeof(BoldOcl).Assembly.GetManifestResourceStream(Name);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>The scanner table, read from the embedded resource on first use.</summary>
    [Delphi(PortStatus.Ported, Note = "Reads the assembly's embedded resource where Delphi reads the module's RC resource.")]
    internal static SSLexTable OclScannerTable() =>
        G_OclScannerTable ??= SSLexTable.CreateResource(ReadResource("OCLSCANNERTABLE"), "OCLSCANNERTABLE", "SCANNERTABLE");

    /// <summary>The parser table, read from the embedded resource on first use.</summary>
    [Delphi(PortStatus.Ported, Note = "Reads the assembly's embedded resource where Delphi reads the module's RC resource.")]
    internal static SSYaccTable OclParserTable() =>
        G_OclParserTable ??= SSYaccTable.CreateResource(ReadResource("OCLPARSERTABLE"), "OCLPARSERTABLE", "PARSERTABLE");

    [Delphi(PortStatus.Ported)]
    internal static TBoldRequestedEvent MapResubscribe(bool Resubscribe) =>
        Resubscribe ? BoldSubscription.breReSubscribe : BoldSubscription.breReEvaluate;

    /// <summary>Logs an expression to BoldOCLLogHandler, with a running count.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldOCLLog(string s)
    {
        if (BoldOCLLogHandler is not null)
        {
            BoldOCLLogHandler.Log(SysUtils.FormatDateTime("c: ", SysUtils.Now()) + SysUtils.Format("OCL %4d - %s", OclCounter, SysUtils.Trim(s)));
        }
    }

    /// <summary>Turns on compatibility with the earlier expression language, allowing capital members.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldForceNelCompatibility()
    {
        BoldNelCompatibility = true;
        BoldOCLAllowCapitalMembers = true;
    }


    internal static void IncOclCounter() => OclCounter++;
}

/// <summary>A parsed expression with its context and self variable; cached in the dictionary unless variables are involved.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclEntry : TBoldMemoryManagedObject
{
    public string OclString;
    public TBoldOclVariableBinding? SelfVar;
    public TBoldSystemTypeInfo? Model;
    public TBoldElementTypeInfo? Context;
    public bool firstSemanticPass;
    public bool EvaluatedOnce;
    public bool OwnedByDictionary;
    public TBoldOclNode? Ocl;
    public bool UsedByOtherEvaluation;

    [Delphi(PortStatus.Ported)]
    public TBoldOclEntry(string Str, TBoldOclNode? OclNode)
    {
        firstSemanticPass = true;
        OclString = Str;
        Ocl = OclNode;
    }
}

/// <summary>Indexes entries by their expression text (case sensitive).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TOCLIndex : TBoldCaseSensitiveStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldOclEntry)Item!).OclString;
}

/// <summary>The parsed expressions of an evaluator, by expression text.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOClDictionary : TBoldIndexableList
{
    private static int IX_OCLEntry = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldOClDictionary()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_OCLEntry, AddIndex(new TOCLIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldOclEntry? GetOcl(string Expr) => (TBoldOclEntry?)((TOCLIndex)Indexes(IX_OCLEntry)!).FindByString(Expr);

    [Delphi(PortStatus.Ported)]
    public void AddOcl(TBoldOclEntry ENTRY) => Add(ENTRY);

    [Delphi(PortStatus.Ported, Symbol = "TBoldOClDictionary.OclEntryByExpressionString", Note = "An indexed property in Delphi.")]
    public TBoldOclEntry? OclEntryByExpressionString(string Expr) => GetOcl(Expr);
}

/// <summary>
/// The OCL evaluator of a system type (and optionally a system): the symbol table, the predefined variables true,
/// false, timeStampNow and nil, and the cache of parsed expressions.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOcl : TBoldRTEvaluator
{
    private static readonly HashSet<char> ValidCharacters =
    [
        ' ', '\t', BoldDefs.BOLDCR, '_', '[', ']', '{', '}', '(', ')', '+', '-', '*', '/', '=', '>', '<', ',', '.', '@', '|', '\'', ':', '#', '«', '»',
        .. "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
    ];

    private readonly TBoldOClDictionary fOclDictionary;
    private readonly TBoldSymbolDictionary fSymbolTable;
    private readonly TBoldSystemTypeInfo fSystemTypeInfo;
    private TBoldOclEnvironment? fGlobalEnv;
    private readonly TBoldSystem? fBoldSystem;
    private readonly TBoldAttributeTypeInfo? fStringType;
    private readonly TBoldAttributeTypeInfo? fIntegerType;
    private readonly TBoldAttributeTypeInfo? fFloatType;
    private readonly TBoldAttributeTypeInfo? fBooleanType;
    private readonly TBoldAttributeTypeInfo? fDateType;
    private readonly TBoldAttributeTypeInfo? fTimeType;
    private readonly bool fCanEvaluate;
    private TBoldLookUpOclDefinition? fOnLookUpOclDefinition;

    /// <summary>An evaluator for the system type; with a system, it can evaluate (and reach class extents).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOcl(TBoldSystemTypeInfo SystemTypeInfo, TBoldSystem? BoldSystem)
    {
        fSystemTypeInfo = SystemTypeInfo;
        fBoldSystem = BoldSystem;
        fOclDictionary = new TBoldOClDictionary();
        fCanEvaluate = true;
        fSymbolTable = new TBoldSymbolDictionary(SystemTypeInfo, BoldSystem, ref fCanEvaluate);
        BoldOclSymbolImplementations.InitializeSymbolTable(fSymbolTable);
        fStringType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("String");
        fIntegerType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("Integer");
        fFloatType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("Float");
        fBooleanType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("Boolean");
        fTimeType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("Time");
        fDateType = fSystemTypeInfo.AttributeTypeInfoByExpressionName("Date");

        var TrueConst = (TBABoolean)TBoldMemberFactory.CreateMemberFromBoldType(fBooleanType);
        TrueConst.AsBoolean = true;
        DefineVariable("true", TrueConst, fBooleanType, true);

        var FalseConst = (TBABoolean)TBoldMemberFactory.CreateMemberFromBoldType(fBooleanType);
        FalseConst.AsBoolean = false;
        DefineVariable("false", FalseConst, fBooleanType, true);

        var MaxTimeStamp = (TBAInteger)TBoldMemberFactory.CreateMemberFromBoldType(fIntegerType);
        MaxTimeStamp.AsInteger = BoldDefs.BOLDMAXTIMESTAMP;
        DefineVariable("timeStampNow", MaxTimeStamp, fIntegerType, true);

        DefineVariable("nil", null, SystemTypeInfo.NilTypeInfo, false);
    }

    [Delphi(PortStatus.Ported)] public TBoldSymbolDictionary SymbolTable => fSymbolTable;
    [Delphi(PortStatus.Ported)] public TBoldSystem? BoldSystem => fBoldSystem;

    /// <summary>Defines (or redefines) a global variable, owning or referencing its value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DefineVariable(string VariableName, TBoldElement? VarValue, TBoldElementTypeInfo? VariableType, bool OwnValue)
    {
        fGlobalEnv ??= new TBoldOclEnvironment(null);
        TBoldOclVariableBinding? NewVar = fGlobalEnv.Lookup(SysUtils.UpperCase(VariableName));
        if (NewVar is null)
        {
            NewVar = new TBoldOclVariableBinding { VariableName = VariableName };
            fGlobalEnv.pushBinding(NewVar);
        }

        if (VariableType is not null)
        {
            NewVar.BoldType = VariableType;
        }
        else if (VarValue is not null)
        {
            NewVar.BoldType = VarValue.BoldType;
        }

        if (OwnValue)
        {
            NewVar.SetOwnedValue(VarValue);
        }
        else
        {
            NewVar.SetReferenceValue(VarValue);
        }
    }

    /// <summary>
    /// Parses the expression, or takes the cached tree when it is constant or free and parsed in the same context;
    /// a tree in use elsewhere gives a fresh, uncached parse. Parse errors are turned into positioned EBoldOCLAbort.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private TBoldIndirectElement? SyntacticParse(string Ocl, bool StoreInDictionary, ref TBoldOclEntry? ResultEntry, TBoldElementTypeInfo? Context)
    {
        char[] chars = Ocl.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] is '\t' or '\n' or '\f' or '\r')
            {
                chars[i] = ' ';
            }
        }

        string FixedExpr = new(chars);
        bool InQuote;
        if (FixedExpr.Contains('«') || FixedExpr.Contains('»'))
        {
            InQuote = false;
            for (int i = 0; i < FixedExpr.Length; i++)
            {
                switch (Ocl[i])
                {
                    case '\'':
                        InQuote = !InQuote;
                        break;
                    case '«' or '»' when !InQuote:
                        throw new EBoldOCLAbort(SysUtils.Format(BoldCoreConsts.sExpressionNotComplete, i + 1));
                }
            }
        }

        ResultEntry = fOclDictionary.OclEntryByExpressionString(Ocl);
        if (ResultEntry is not null)
        {
            if (ResultEntry.Ocl!.IsConstant || (!ResultEntry.UsedByOtherEvaluation && ResultEntry.Context == Context))
            {
                ResultEntry.UsedByOtherEvaluation = true;
                return ResultEntry.Ocl;
            }

            ResultEntry = null;
            StoreInDictionary = false;
        }

        TBoldIndirectElement? result = null;
        var Consumer = new SSLexStringConsumer(FixedExpr);
        var Lexer = new ALexClass(Consumer, BoldOcl.OclScannerTable());
        var Parser = new AYaccClass(Lexer, BoldOcl.OclParserTable());
        try
        {
            try
            {
                Parser.Parse();
            }
            catch (SSException e)
            {
                for (int i = 0; i < Parser.Stack.TopOfStack; i++)
                {
                    if (Parser.Stack.PArray[i] is AYaccStackElement element)
                    {
                        element.FreeAllOwnedElements();
                    }
                }

                InQuote = false;
                int QuotePos = 0;
                int ParenPos = 0;
                if (e.Message.Contains("SSLex0105e: Invalid token,", StringComparison.Ordinal))
                {
                    for (int i = 0; i < Ocl.Length; i++)
                    {
                        if (Ocl[i] == '\'')
                        {
                            InQuote = !InQuote;
                        }

                        if (!InQuote && !ValidCharacters.Contains(Ocl[i]))
                        {
                            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidCharacter, i));
                        }
                    }
                }

                if (e.Message.Contains("SSYacc0105e: SyncErr failed, no valid token", StringComparison.Ordinal))
                {
                    int Paren = 0;
                    for (int i = 0; i < Ocl.Length; i++)
                    {
                        switch (Ocl[i])
                        {
                            case '(' when !InQuote:
                                Paren++;
                                ParenPos = i + 1;
                                break;
                            case ')' when !InQuote:
                                Paren--;
                                break;
                            case '\'':
                                QuotePos = i + 1;
                                InQuote = !InQuote;
                                break;
                        }
                    }

                    if (Paren != 0)
                    {
                        throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUnMatchedParentesis, ParenPos - 1));
                    }

                    if (InQuote)
                    {
                        throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeunterminatedQoute, QuotePos - 1));
                    }
                }

                throw new EBoldOCLAbort(SysUtils.Format("%d:" + e.Message, e.Position));
            }
            catch (EBoldOCLAbort e)
            {
                e.Ocl = Ocl;
                e.FixError();
                throw;
            }
            catch (EBoldOCLError e)
            {
                e.Ocl = Ocl;
                e.FixError();
                throw;
            }
        }
        finally
        {
            if (Parser.finalValue is not null)
            {
                ResultEntry = new TBoldOclEntry(Ocl, Parser.finalValue.Node) { UsedByOtherEvaluation = true };
                result = ResultEntry.Ocl;
            }
        }

        if (StoreInDictionary)
        {
            fOclDictionary.AddOcl(ResultEntry!);
            ResultEntry!.OwnedByDictionary = true;
        }

        return result;
    }

    /// <summary>
    /// Parses (expanding a %definition) and checks the expression in the context with Self bound; a tree already checked
    /// in a conforming context, or constant, is reused.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private TBoldIndirectElement? SemanticCheck(string Ocl, TBoldElementTypeInfo? Context, bool StoreInDictionary, ref TBoldOclEntry? ResultEntry, TBoldOclEnvironment Env)
    {
        try
        {
            if (Ocl[0] == '%')
            {
                Ocl = LookupOclDefinition(SysUtils.Copy(Ocl, 2, int.MaxValue));
            }

            SyntacticParse(Ocl, StoreInDictionary, ref ResultEntry, Context);

            // A nil context must still get the semantic check's exception, which evaluating a nil root relies on.
            if ((!ResultEntry!.firstSemanticPass && Context is not null && Context.ConformsTo(ResultEntry.Context)) ||
                (ResultEntry.Ocl is not null && ResultEntry.Ocl.IsConstant))
            {
                return ResultEntry.Ocl;
            }

            ResultEntry.Context = Context;
            ResultEntry.Model = fSystemTypeInfo;
            int EnvSize = Env.Count;
            ResultEntry.SelfVar?.Dispose();
            ResultEntry.SelfVar = new TBoldOclVariableBinding { VariableName = "Self", BoldType = ResultEntry.Context };
            Env.pushBinding(ResultEntry.SelfVar);
            var Visitor = new TBoldOclSemanticsVisitor(ResultEntry.Model, this, SymbolTable, Env);
            if (BoldOcl.BoldNelCompatibility && Ocl.Contains('('))
            {
                Visitor.IgnoreNelCompatibility = true;
            }

            try
            {
                try
                {
                    Visitor.Traverse(ref ResultEntry.Ocl);
                }
                catch (EBoldOCLAbort e)
                {
                    e.Ocl = ResultEntry.OclString;
                    ResultEntry.Context = null;
                    throw;
                }
                catch (EBoldOCLError e)
                {
                    e.Ocl = ResultEntry.OclString;
                    ResultEntry.Context = null;
                    throw;
                }
                catch (Exception e)
                {
                    // The tree is syntactically correct, so it is kept.
                    ResultEntry.Context = null;
                    throw new EBoldOCLAbort(SysUtils.Format("%d: %s", -1, e.Message), e);
                }
            }
            finally
            {
                Env.popBinding();
                if (EnvSize != Env.Count)
                {
                    throw new EBoldOCLInternalError(SysUtils.Format(BoldOclError.boeEnvSizeError, 0, EnvSize, Env.Count));
                }
            }

            ResultEntry.firstSemanticPass = false;
            return ResultEntry.Ocl;
        }
        catch (EBoldOCLAbort e)
        {
            e.Ocl = Ocl;
            e.FixError();
            throw;
        }
        catch (EBoldOCLError e)
        {
            e.Ocl = Ocl;
            e.FixError();
            throw;
        }
    }

    /// <summary>Releases an entry after use: back to the dictionary, or freed when not cached.</summary>
    private static void ReleaseEntry(TBoldOclEntry? ResultEntry)
    {
        if (ResultEntry is null)
        {
            return;
        }

        if (ResultEntry.OwnedByDictionary)
        {
            ResultEntry.UsedByOtherEvaluation = false;
        }
        else
        {
            ResultEntry.Ocl?.Dispose();
            ResultEntry.Ocl = null;
        }
    }

    /// <summary>The run-time info of the member the expression ends in, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldMemberRTInfo? RTInfo(string Ocl, TBoldElementTypeInfo? Context, bool ReRaise)
    {
        TBoldMemberRTInfo? result = null;
        TBoldOclEntry? ResultEntry = null;
        using var Env = new TBoldOclEnvironment(fGlobalEnv);
        try
        {
            try
            {
                if (Ocl.Length != 0)
                {
                    SemanticCheck(Ocl, Context, false, ref ResultEntry, Env);
                }

                if (ResultEntry?.Ocl is TBoldOclMember member)
                {
                    result = member.RTInfo;
                }
            }
            catch (Exception e) when (e is EBoldOCLAbort or EBoldOCLError)
            {
                result = null;
                if (ReRaise)
                {
                    throw;
                }
            }
        }
        finally
        {
            ReleaseEntry(ResultEntry);
        }

        return result;
    }

    /// <summary>The type of the expression in the context (the context itself for an empty expression), or null on errors unless ReRaise.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldElementTypeInfo? ExpressionType(string Ocl, TBoldElementTypeInfo? Context, bool ReRaise, TBoldExternalVariableList? VariableList = null)
    {
        if (SysUtils.Trim(Ocl).Length == 0)
        {
            return Context;
        }

        TBoldElementTypeInfo? result = null;
        TBoldOclEntry? ResultEntry = null;
        using var Env = new TBoldOclEnvironment(fGlobalEnv);
        AddVarsToEnv(Env, VariableList, false);
        try
        {
            try
            {
                SemanticCheck(Ocl, Context, false, ref ResultEntry, Env);
                if (ResultEntry is not null)
                {
                    result = ResultEntry.Ocl!.BoldType;
                }
            }
            catch (Exception e) when (e is EBoldOCLAbort or EBoldOCLError)
            {
                result = null;
                if (ReRaise)
                {
                    throw;
                }
            }
        }
        finally
        {
            ReleaseEntry(ResultEntry);
        }

        return result;
    }

    /// <summary>
    /// Evaluates the expression on the root into resultElement, subscribing the subscriber to what it depends on.
    /// A semantic error with a nil root gives nil; EvaluateInPS evaluates an object-list expression in the persistence layer.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void Evaluate(string Ocl, TBoldElement? Root, TBoldSubscriber? Subscriber, bool ResubscribeAll, TBoldIndirectElement? resultElement,
        bool EvaluateInPS = false, TBoldExternalVariableList? VariableList = null)
    {
        if (!fCanEvaluate)
        {
            throw new EBoldOCLError(BoldCoreConsts.sTypesMissingFromEvaluator);
        }

        TBoldOclEntry? ResultEntry = null;
        TBoldOclEnvironment? Env = null;
        if (BoldOcl.BoldNelCompatibility && Ocl.Length > 0 && Ocl[^1] == '.')
        {
            Ocl = Ocl[..^1];
        }

        string CurrentComponentPath = BoldOcl.BoldOCLRTDebugger is not null && Subscriber is not null ? Subscriber.ContextString : "";
        BoldOcl.BoldOCLLog(Ocl);
        TBoldElementTypeInfo? LocalContext = null;
        try
        {
            BoldOcl.IncOclCounter();
            LocalContext = Root?.BoldType;
            if (Root is TBoldObjectReference reference)
            {
                if (Subscriber is not null)
                {
                    Root.DefaultSubscribe(Subscriber, BoldOcl.MapResubscribe(ResubscribeAll || Ocl.Length != 0));
                }

                Root = reference.BoldObject;
            }

            if (Ocl.Length == 0)
            {
                resultElement?.SetReferenceValue(Root);
                if (Subscriber is not null && Root is not null)
                {
                    Root.DefaultSubscribe(Subscriber, BoldOcl.MapResubscribe(ResubscribeAll));
                }

                return;
            }

            if (BoldOcl.BoldOCLRTDebugger is not null && BoldOcl.BoldOCLRTDebugger.HasFixFor(Ocl, LocalContext))
            {
                Ocl = BoldOcl.BoldOCLRTDebugger.GetFixFor(Ocl, LocalContext);
            }

            try
            {
                try
                {
                    Env = new TBoldOclEnvironment(fGlobalEnv);
                    AddVarsToEnv(Env, VariableList, true);
                    SemanticCheck(Ocl, LocalContext, VariableList is null, ref ResultEntry, Env);
                }
                catch (Exception e) when (e is EBoldOCLAbort or EBoldOCLError)
                {
                    // No root, but syntactically correct and semantically erroneous: the result is nil.
                    if (Root is null && ResultEntry is not null)
                    {
                        resultElement?.SetReferenceValue(null);
                        return;
                    }

                    throw;
                }

                TBoldOclNode resultNode = ResultEntry!.Ocl!;
                if (resultNode.IsConstant && ResultEntry.EvaluatedOnce)
                {
                    resultElement?.SetReferenceValue(resultNode.Value);
                    return;
                }

                ResultEntry.SelfVar!.SetReferenceValue(Root);
                if (EvaluateInPS)
                {
                    PSEvaluation(Ocl, Root, ResultEntry, Env);
                }
                else
                {
                    var EvaluatorVisitor = new TBoldOclEvaluatorVisitor(Subscriber, ResubscribeAll, fSystemTypeInfo, BoldSystem, fStringType, fIntegerType,
                        fFloatType, fDateType, fTimeType);
                    resultNode.AcceptVisitor(EvaluatorVisitor);
                }

                ResultEntry.EvaluatedOnce = true;
                if (resultElement is not null)
                {
                    if (resultNode.IsConstant)
                    {
                        resultElement.SetReferenceValue(resultNode.Value);
                    }
                    else
                    {
                        resultNode.TransferValue(resultElement);
                    }

                    if (BoldOcl.BoldNelCompatibility && resultElement.Value is TBoldClassTypeInfo classTypeInfo && fBoldSystem is not null)
                    {
                        resultElement.SetReferenceValue(fBoldSystem.ClassByExpressionName(classTypeInfo.ExpressionName));
                        // The value was transferred out, so the next use must check again to get a new one.
                        ResultEntry.firstSemanticPass = true;
                    }

                    if (resultElement.OwnsValue && resultElement.Value is not null)
                    {
                        if (!EvaluateInPS)
                        {
                            resultElement.Value.MakeImmutable();
                        }

                        if (resultElement.Value is TBoldObjectList objectList)
                        {
                            if (Subscriber is null)
                            {
                                objectList.SubscribeToObjectsInList = true;
                            }
                            else
                            {
                                objectList.SubscribeToLocatorsInList = true;
                            }
                        }
                    }
                }
            }
            catch (EBoldOCLAbort e)
            {
                if (BoldOcl.BoldOCLRTDebugger is not null && BoldOcl.BoldOCLRTDebugger.AddFixFor(Ocl, LocalContext, CurrentComponentPath, e.Message))
                {
                    Evaluate(Ocl, Root, Subscriber, ResubscribeAll, resultElement);
                }
                else
                {
                    e.Ocl = Ocl;
                    e.FixError();
                    throw;
                }
            }
            catch (EBoldOCLError e)
            {
                if (BoldOcl.BoldOCLRTDebugger is not null && BoldOcl.BoldOCLRTDebugger.AddFixFor(Ocl, LocalContext, CurrentComponentPath, e.Message))
                {
                    Evaluate(Ocl, Root, Subscriber, ResubscribeAll, resultElement);
                }
                else
                {
                    e.Ocl = Ocl;
                    e.FixError();
                    throw;
                }
            }
            catch (Exception e)
            {
                if (BoldOcl.BoldOCLRTDebugger is not null && BoldOcl.BoldOCLRTDebugger.AddFixFor(Ocl, LocalContext, CurrentComponentPath, e.Message))
                {
                    Evaluate(Ocl, Root, Subscriber, ResubscribeAll, resultElement);
                }
                else
                {
                    throw;
                }
            }
        }
        finally
        {
            Env?.Dispose();
            ReleaseEntry(ResultEntry);
        }
    }

    /// <summary>The OCL of a %definition through the lookup, which must be set.</summary>
    [Delphi(PortStatus.Ported)]
    private string LookupOclDefinition(string name)
    {
        if (fOnLookUpOclDefinition is not null)
        {
            return fOnLookUpOclDefinition(name);
        }

        throw new EBoldOCLError(SysUtils.Format(BoldCoreConsts.sCannotFindOCLDefinitionWithoutRepository, "%" + name));
    }

    [Delphi(PortStatus.Ported)]
    public override void SetLookupOclDefinition(TBoldLookUpOclDefinition? value) => fOnLookUpOclDefinition = value;

    /// <summary>Evaluates an object-list expression in the persistence layer through an OCL condition built from light-weight nodes.</summary>
    [Delphi(PortStatus.Ported)]
    private void PSEvaluation(string Expr, TBoldElement? Root, TBoldOclEntry ResultEntry, TBoldOclEnvironment Env)
    {
        TBoldSystem? LocalBoldSystem = BoldSystem;
        if (LocalBoldSystem is null && Root is TBoldObjectList rootList && rootList.Count > 0)
        {
            LocalBoldSystem = rootList[0]!.BoldSystem;
        }

        var OLWNodeMaker = new TBoldOLWNodeMaker(ResultEntry.Ocl!, fSystemTypeInfo, LocalBoldSystem, Env);
        ResultEntry.Ocl!.AcceptVisitor(OLWNodeMaker);
        if (OLWNodeMaker.Failed)
        {
            throw new EBoldOCLError(SysUtils.Format("%d:%s", OLWNodeMaker.FailurePosition, OLWNodeMaker.FailureReason));
        }

        var OclCondition = new TBoldOclCondition { OclExpr = Expr };
        for (int i = 0; i < OLWNodeMaker.ExternalVarBindings.Count; i++)
        {
            OclCondition.Env.Add(OLWNodeMaker.ExternalVarBindings[i]);
        }

        OLWNodeMaker.ExternalVarBindings.Clear();
        if (Root is TBoldObject rootObject)
        {
            OclCondition.Context.Add(rootObject.BoldObjectLocator!.BoldObjectID);
        }
        else if (Root is TBoldObjectList RootAsList)
        {
            RootAsList.EnsureObjects();
            for (int i = 0; i < RootAsList.Count; i++)
            {
                OclCondition.Context.Add(RootAsList[i]!.BoldObjectLocator!.BoldObjectID);
            }
        }
        else if (Root is not null && Root is not TBoldSystem)
        {
            throw new EBoldOCLError(SysUtils.Format(BoldCoreConsts.sInvalidForSQLEvaluation, Root.AsString, Root.GetType().Name));
        }

        OclCondition.RootNode = OLWNodeMaker.RootNode;
        var ResList = (TBoldObjectList)TBoldMemberFactory.CreateMemberFromBoldType(ResultEntry.Ocl.BoldType);
        var ClassTypeInfo = (TBoldClassTypeInfo)((TBoldListTypeInfo)ResultEntry.Ocl.BoldType!).ListElementTypeInfo!;
        OclCondition.TopSortedIndex = ClassTypeInfo.TopSortedIndex;
        LocalBoldSystem!.GetAllWithCondition(ResList, OclCondition);
        ResultEntry.Ocl.SetOwnedValue(ResList);
    }

    /// <summary>Binds the external variables in the environment (with their values when evaluating).</summary>
    [Delphi(PortStatus.Ported)]
    private void AddVarsToEnv(TBoldOclEnvironment Env, TBoldExternalVariableList? VariableList, bool Initializevalues)
    {
        if (VariableList is null)
        {
            return;
        }

        for (int i = 0; i < VariableList.Count; i++)
        {
            var VarBinding = new TBoldOclVariableBinding { VariableName = VariableList[i].Name };
            if (Initializevalues)
            {
                VarBinding.SetReferenceValue(VariableList[i].Value);
            }

            VarBinding.BoldType = VariableList[i].ValueType;
            Env.pushBinding(VarBinding);
        }
    }
}
