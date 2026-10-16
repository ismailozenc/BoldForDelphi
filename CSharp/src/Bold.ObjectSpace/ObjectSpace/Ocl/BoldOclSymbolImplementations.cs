// Delphi: Source/ObjectSpace/Ocl/BoldOclSymbolImplementations.pas
// Port map: CSharp/port/units/BoldOclSymbolImplementations.md
// The built-in OCL operations. Each symbol declares its name, argument types and result type in Init and computes
// its result from the evaluated arguments in Evaluate. Iterations (select, collect, ...) are called once per element.

using System.Text;
using System.Text.RegularExpressions;
using Bold;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclSymbolImplementations", Symbol = "finalization", Note = "Freed the regular expressions and the operation list; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclSymbolImplementations", Symbol = "InitSQLRegExp", Note = "The BoldRegularExpression unit is not in the sources; FixSQLRegExp builds a .NET Regex instead.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclSymbolImplementations", Symbol = "InitRegExp", Note = "The BoldRegularExpression unit is not in the sources; regExpMatch uses a .NET Regex.")]

namespace Bold;

/// <summary>The registered operation classes, and the filling of a symbol table with them.</summary>
public static partial class BoldOclSymbolImplementations
{
    private static List<Type>? G_OCLOperations;

    [Delphi(PortStatus.Ported, Note = "A list of symbol types; a TList of class references in Delphi.")]
    internal static List<Type> OCLOperations() => G_OCLOperations ??= [];

    /// <summary>Registers an operation class (a TBoldOclSymbol with a constructor taking TBoldOclSymbolHelp), once.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the symbol's Type where Delphi takes a class reference.")]
    public static void RegisterOCLOperation(Type OperationClass)
    {
        if (!OCLOperations().Contains(OperationClass))
        {
            OCLOperations().Add(OperationClass);
        }
    }

    /// <summary>Adds an instance of every registered operation to the table.</summary>
    [Delphi(PortStatus.Ported)]
    public static void InitializeSymbolTable(TBoldSymbolDictionary SymTab)
    {
        foreach (Type operation in OCLOperations())
        {
            SymTab.Add((TBoldOclSymbol)Activator.CreateInstance(operation, SymTab.help)!);
        }
    }

    /// <summary>A SQL like pattern as a regular expression: % is any text, _ any character, the rest literal, anchored.</summary>
    [Delphi(PortStatus.Replaced, Note = "Builds a .NET Regex pattern; Delphi builds one for TRegularExpression with control characters as metacharacters.")]
    internal static string FixSQLRegExp(string S)
    {
        var result = new StringBuilder("^");
        foreach (char c in S)
        {
            result.Append(c switch
            {
                '%' => ".*",
                '_' => ".",
                _ => Regex.Escape(c.ToString()),
            });
        }

        return result.Append('$').ToString();
    }

    internal static TDateTime AddMoments(TDateTime a, TDateTime b) => DateTime.FromOADate(a.ToOADate() + b.ToOADate());

    /// <summary>Registers the built-in operations (Delphi's initialization section).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Run by a module initializer when the assembly loads.")]
#pragma warning disable CA2255 // The operations must be registered before any evaluator builds its symbol table, as Delphi's initialization section does.
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialization()
    {
        Type[] operations =
        [
            typeof(TBOS_Equal), typeof(TBOS_NotEqual), typeof(TBOS_Except), typeof(TBOS_Add), typeof(TBOS_Subtract), typeof(TBOS_UnaryMinus),
            typeof(TBOS_Multiply), typeof(TBOS_Divide), typeof(TBOS_Abs), typeof(TBOS_Floor), typeof(TBOS_Round), typeof(TBOS_strToInt),
            typeof(TBOS_Min), typeof(TBOS_Max), typeof(TBOS_Less), typeof(TBOS_Greater), typeof(TBOS_LessEQ), typeof(TBOS_GreaterEQ),
            typeof(TBOS_Div), typeof(TBOS_Mod), typeof(TBOS_Length), typeof(TBOS_concat), typeof(TBOS_ToUpper), typeof(TBOS_toLower),
            typeof(TBOS_SubString), typeof(TBOS_Pad), typeof(TBOS_PostPad), typeof(TBOS_or), typeof(TBOS_and), typeof(TBOS_not),
            typeof(TBOS_xor), typeof(TBOS_implies), typeof(TBOS_if), typeof(TBOS_Size), typeof(TBOS_includes), typeof(TBOS_Count),
            typeof(TBOS_IncludesAll), typeof(TBOS_isEmpty), typeof(TBOS_isNull), typeof(TBOS_NotEmpty), typeof(TBOS_Sum), typeof(TBOS_SumTime),
            typeof(TBOS_Maxvalue), typeof(TBOS_MinValue), typeof(TBOS_Average), typeof(TBOS_Exists), typeof(TBOS_ForAll), typeof(TBOS_union),
            typeof(TBOS_Intersection), typeof(TBOS_difference), typeof(TBOS_Including), typeof(TBOS_excluding), typeof(TBOS_SymmetricDifference),
            typeof(TBOS_Select), typeof(TBOS_reject), typeof(TBOS_collect), typeof(TBOS_AsSequence), typeof(TBOS_AsBag), typeof(TBOS_AsSet),
            typeof(TBOS_Append), typeof(TBOS_Prepend), typeof(TBOS_SubSequence), typeof(TBOS_at), typeof(TBOS_first), typeof(TBOS_last),
            typeof(TBOS_orderby), typeof(TBOS_orderDescending), typeof(TBOS_asString), typeof(TBOS_dateTimeAsFloat),
            typeof(TBOS_StringRepresentation), typeof(TBOS_TaggedValue), typeof(TBOS_TypeName), typeof(TBOS_Attributes),
            typeof(TBOS_AssociationEnds), typeof(TBOS_SuperTypes), typeof(TBOS_AllSuperTypes), typeof(TBOS_AllSubClasses),
            typeof(TBOS_AllInstances), typeof(TBOS_AllLoadedObjects), typeof(TBOS_EmptyLIst), typeof(TBOS_oclType), typeof(TBOS_oclIsKindOf),
            typeof(TBOS_OclIsTypeOf), typeof(TBOS_OclAsType), typeof(TBOS_safeCast), typeof(TBOS_SQLLike), typeof(TBOS_SQLLikeCaseInsensitive),
            typeof(TBOS_RegExpMatch), typeof(TBOS_InDateRange), typeof(TBOS_InTimeRange), typeof(TBOS_Constraints), typeof(TBOS_FormatNumeric),
            typeof(TBOS_FormatDateTime), typeof(TBOS_StrToDateTime), typeof(TBOS_StrToDate), typeof(TBOS_StrToTime), typeof(TBOS_AtTime),
            typeof(TBOS_ObjectTimeStamp), typeof(TBOS_AllInstancesAtTime), typeof(TBOS_Existing), typeof(TBOS_FilterOnType),
            typeof(TBOS_BoldTime), typeof(TBOS_TimeStampToTime), typeof(TBOS_TimeToTimeStamp),
        ];
        foreach (Type operation in operations)
        {
            RegisterOCLOperation(operation);
        }
    }
}

/// <summary>The value of a moment attribute as a date-time (zero for others).</summary>
internal static class TBOS_MomentHelper
{
    internal static TDateTime MomentValue(TBAMoment Moment) => Moment switch
    {
        TBADateTime dt => dt.AsDateTime,
        TBADate d => d.AsDate,
        TBATime t => t.AsTime,
        _ => SysUtils.ZeroDateTime,
    };
}

/// <summary>Comparison of an enum literal (#name) with a value set.</summary>
[Delphi(PortStatus.Ported)]
internal abstract class TBOS_AbstractCompare(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Note = "Takes the value set from the second argument when it is the value set; Delphi takes the first (a bug that made #name = attr false).")]
    protected bool CompareEnumLiterals(TBoldOclSymbolParameters Params)
    {
        string str = "";
        TBAValueSet? valueSet = null;
        if (Params.Nodes[0] is TBoldOclEnumLiteral literal0)
        {
            str = literal0.name;
        }
        else if (Params.Values[0] is TBAValueSet set0)
        {
            valueSet = set0;
        }

        if (Params.Nodes[1] is TBoldOclEnumLiteral literal1)
        {
            if (str.Length == 0)
            {
                str = literal1.name;
            }
            else
            {
                throw new EBoldOclRunTimeError(SysUtils.Format(BoldCoreConsts.sCannotCompareEnumLiterals, 0, str, literal1.name));
            }
        }
        else if (Params.Values[1] is TBAValueSet set1)
        {
            valueSet = set1;
        }

        return valueSet is not null && valueSet.CompareToEnumLiteral(str);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Equal(TBoldOclSymbolHelp Help) : TBOS_AbstractCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("=", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 100);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (Params.Nodes[0] is TBoldOclEnumLiteral || Params.Nodes[1] is TBoldOclEnumLiteral)
        {
            Help.MakeNewBoolean(Params.Result!, CompareEnumLiterals(Params));
        }
        else if (Params.Values[0] is null)
        {
            Help.MakeNewBoolean(Params.Result!, Params.Values[1] is null);
        }
        else
        {
            Help.MakeNewBoolean(Params.Result!, Params.Values[0]!.IsEqual(Params.Values[1]));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_NotEqual(TBoldOclSymbolHelp Help) : TBOS_AbstractCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("<>", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 101);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (Params.Nodes[1] is TBoldOclEnumLiteral || Params.Nodes[0] is TBoldOclEnumLiteral)
        {
            Help.MakeNewBoolean(Params.Result!, !CompareEnumLiterals(Params));
        }
        else if (Params.Values[0] is null)
        {
            Help.MakeNewBoolean(Params.Result!, Params.Values[1] is not null);
        }
        else
        {
            Help.MakeNewBoolean(Params.Result!, !Params.Values[0]!.IsEqual(Params.Values[1]));
        }
    }
}

/// <summary>x->except(alternative); the evaluator handles it, so Evaluate is never called.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Except(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("except", [null, null], TBoldOclDeduceMethod.tbodLCC, null, true, 101);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Add(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("+", [null, null], TBoldOclDeduceMethod.tbodLCC, null, false, 102);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo resultType = Params.Result!.BoldType!;
        if (resultType.ConformsTo(Help.StringType))
        {
            Help.MakeNewString(Params.Result, XString(Params.Values[0]) + XString(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, XInteger(Params.Values[0]) + XInteger(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Params.Result, XCurrency(Params.Values[0]) + XCurrency(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.NumericType))
        {
            Help.MakeNewNumeric(Params.Result, XNumeric(Params.Values[0]) + XNumeric(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.MomentType))
        {
            Help.MakeNewDateTime(Params.Result, BoldOclSymbolImplementations.AddMoments(XDateTime(Params.Values[0]), XDateTime(Params.Values[1])));
        }
        else
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteOperationNotDefinedOnArgTypes, 0,
                Params.Values[0]?.BoldType?.AsString ?? "", "+", Params.Values[1]?.BoldType?.AsString ?? ""));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Subtract(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("-", [Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodLCC, null, false, 103);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo resultType = Params.Result!.BoldType!;
        if (resultType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, XInteger(Params.Values[0]) - XInteger(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Params.Result, XCurrency(Params.Values[0]) - XCurrency(Params.Values[1]));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, XNumeric(Params.Values[0]) - XNumeric(Params.Values[1]));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_UnaryMinus(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("unary-", [Help.NumericType], TBoldOclDeduceMethod.tbodCopyArg1, null, false, 104);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo resultType = Params.Result!.BoldType!;
        if (resultType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, -XInteger(Params.Values[0]));
        }
        else if (resultType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Params.Result, -XCurrency(Params.Values[0]));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, -XNumeric(Params.Values[0]));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Multiply(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("*", [Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodLCC, null, false, 105);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo resultType = Params.Result!.BoldType!;
        if (resultType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, XInteger(Params.Values[0]) * XInteger(Params.Values[1]));
        }
        else if (resultType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Params.Result, XCurrency(Params.Values[0]) * XCurrency(Params.Values[1]));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, XNumeric(Params.Values[0]) * XNumeric(Params.Values[1]));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Divide(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("/", [Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodNo, Help.RealType, false, 106);

    [Delphi(PortStatus.Ported, Note = "Checks for a zero divisor, since .NET floating division gives infinity where Delphi raises EZeroDivide.")]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        double divisor = XNumeric(Params.Values[1]);
        if (divisor == 0)
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteDivisionByZero, 0));
        }

        Help.MakeNewNumeric(Params.Result!, XNumeric(Params.Values[0]) / divisor);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Abs(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("abs", [Help.NumericType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 107);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo resultType = Params.Result!.BoldType!;
        if (resultType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, Math.Abs(XInteger(Params.Values[0])));
        }
        else if (resultType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Params.Result, Math.Abs(XCurrency(Params.Values[0])));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, Math.Abs(XNumeric(Params.Values[0])));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Floor(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("floor", [Help.NumericType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 108);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, (int)Math.Floor(XNumeric(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Round(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("round", [Help.NumericType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 109);

    /// <summary>Rounds half to even, as Delphi's Round.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, (int)Math.Round(XNumeric(Params.Values[0]), MidpointRounding.ToEven));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_strToInt(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("strToInt", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 109);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, SysUtils.StrToIntDef(XString(Params.Values[0]), 0));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Min(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("min", [Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodLCC, null, true, 111);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (Params.Result!.BoldType!.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, BoldMath.MinIntValue(XInteger(Params.Values[0]), XInteger(Params.Values[1])));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, BoldMath.MinValue(XNumeric(Params.Values[0]), XNumeric(Params.Values[1])));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Max(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("max", [Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodLCC, null, true, 110);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (Params.Result!.BoldType!.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Params.Result, BoldMath.MaxIntValue(XInteger(Params.Values[0]), XInteger(Params.Values[1])));
        }
        else
        {
            Help.MakeNewNumeric(Params.Result, BoldMath.MaxValue(XNumeric(Params.Values[0]), XNumeric(Params.Values[1])));
        }
    }
}

/// <summary>The comparisons: false when either side is null.</summary>
internal abstract class TBOS_OrderCompare(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    protected void Compare(TBoldOclSymbolParameters Params, Func<int, bool> Test)
    {
        if (Params.Values[0] is not null && Params.Values[1] is not null)
        {
            Help.MakeNewBoolean(Params.Result!, Test(Params.Values[0]!.CompareTo(Params.Values[1])));
        }
        else
        {
            Help.MakeNewBoolean(Params.Result!, false);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Less(TBoldOclSymbolHelp Help) : TBOS_OrderCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("<", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 112);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Compare(Params, c => c < 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Greater(TBoldOclSymbolHelp Help) : TBOS_OrderCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit(">", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 113);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Compare(Params, c => c > 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_LessEQ(TBoldOclSymbolHelp Help) : TBOS_OrderCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("<=", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 114);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Compare(Params, c => c <= 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_GreaterEQ(TBoldOclSymbolHelp Help) : TBOS_OrderCompare(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit(">=", [null, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, false, 115);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Compare(Params, c => c >= 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Div(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("div", [Help.IntegerType, Help.IntegerType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, false, 116);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        try
        {
            Help.MakeNewInteger(Params.Result!, XInteger(Params.Values[0]) / XInteger(Params.Values[1]));
        }
        catch (DivideByZeroException)
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteDivisionByZero, 0));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Mod(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("mod", [Help.IntegerType, Help.IntegerType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, false, 117);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, XInteger(Params.Values[0]) % XInteger(Params.Values[1]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Length(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("length", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 118);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, XString(Params.Values[0]).Length);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_concat(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("concat", [Help.StringType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 119);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewString(Params.Result!, XString(Params.Values[0]) + XString(Params.Values[1]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_ToUpper(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("toUpper", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 120);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewString(Params.Result!, SysUtils.AnsiUpperCase(XString(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_toLower(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("toLower", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 121);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewString(Params.Result!, SysUtils.AnsiLowerCase(XString(Params.Values[0])));
}

/// <summary>The characters lower..upper (1-based, inclusive).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SubString(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() =>
        InternalInit("subString", [Help.StringType, Help.IntegerType, Help.IntegerType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 122);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        int lower = XInteger(Params.Values[1]);
        int upper = XInteger(Params.Values[2]);
        Help.MakeNewString(Params.Result!, SysUtils.Copy(XString(Params.Values[0]), lower, upper - lower + 1));
    }
}

/// <summary>Pads in front with the padder to the length (cutting a partial padder from the left).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Pad(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("pad", [Help.StringType, Help.IntegerType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 123);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        string PaddedStr = XString(Params.Values[0]);
        int FinalLength = XInteger(Params.Values[1]);
        if (PaddedStr.Length < FinalLength)
        {
            string Padder = XString(Params.Values[2]);
            if (Padder.Length == 0)
            {
                throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.bortePadStringEmpty, 0));
            }

            while (PaddedStr.Length < FinalLength)
            {
                PaddedStr = Padder + PaddedStr;
            }

            if (PaddedStr.Length != FinalLength)
            {
                PaddedStr = SysUtils.Copy(PaddedStr, PaddedStr.Length - FinalLength + 1, FinalLength);
            }
        }

        Help.MakeNewString(Params.Result!, PaddedStr);
    }
}

/// <summary>Pads at the end with the padder to the length (cutting the excess).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_PostPad(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("postPad", [Help.StringType, Help.IntegerType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 124);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        string PaddedStr = XString(Params.Values[0]);
        int FinalLength = XInteger(Params.Values[1]);
        if (PaddedStr.Length < FinalLength)
        {
            string Padder = XString(Params.Values[2]);
            if (Padder.Length == 0)
            {
                throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.bortePadStringEmpty, 0));
            }

            while (PaddedStr.Length < FinalLength)
            {
                PaddedStr += Padder;
            }

            if (PaddedStr.Length != FinalLength)
            {
                PaddedStr = SysUtils.Copy(PaddedStr, 1, FinalLength);
            }
        }

        Help.MakeNewString(Params.Result!, PaddedStr);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_FormatNumeric(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("formatNumeric", [Help.NumericType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 124);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewString(Params.Result!, SysUtils.Format(XString(Params.Values[1]), XNumeric(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_FormatDateTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("formatDateTime", [Help.MomentType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 124);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        var Moment = (TBAMoment)Params.Values[0]!;
        Help.MakeNewString(Params.Result!, Moment.IsNull ? "" : SysUtils.FormatDateTime(XString(Params.Values[1]), TBOS_MomentHelper.MomentValue(Moment)));
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_StrToDate(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("strToDate", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.DateType, true, 124);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewDate(Params.Result!, SysUtils.StrToDate(XString(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_StrToTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("strToTime", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.TimeType, true, 124);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewTime(Params.Result!, SysUtils.StrToTime(XString(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_StrToDateTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("strToDateTime", [Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.DateTimeType, true, 124);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewDateTime(Params.Result!, SysUtils.StrToDateTime(XString(Params.Values[0])));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_or(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("or", [Help.BooleanType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 125);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, XBoolean(Params.Values[0]) || XBoolean(Params.Values[1]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_and(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("and", [Help.BooleanType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 126);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, XBoolean(Params.Values[0]) && XBoolean(Params.Values[1]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_not(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("not", [Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 129);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, !XBoolean(Params.Values[0]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_xor(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("xor", [Help.BooleanType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 127);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, XBoolean(Params.Values[0]) ^ XBoolean(Params.Values[1]));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_implies(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("implies", [Help.BooleanType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 128);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, !XBoolean(Params.Values[0]) || XBoolean(Params.Values[1]));
}

/// <summary>if c then a else b endif: moves the value of the branch taken to the result.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_if(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("if", [Help.BooleanType, null, null], TBoldOclDeduceMethod.tbodLCC23, null, true, 130);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XBoolean(Params.Values[0]))
        {
            Params.Nodes[1]!.TransferValue(Params.Result!);
        }
        else
        {
            Params.Nodes[2]!.TransferValue(Params.Result!);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Size(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("size", [Help.ListType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 131);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, XList(Params.Values[0])?.Count ?? 0);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_includes(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("includes", [Help.ListType, null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 132, true);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        bool temp = false;
        TBoldList? List = XList(Params.Values[0]);
        if (List is not null)
        {
            for (int i = 0; i < List.Count; i++)
            {
                temp = temp || List[i]!.IsEqual(Params.Values[1]);
            }
        }

        Help.MakeNewBoolean(Params.Result!, temp);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Count(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("count", [Help.ListType, null], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 133, true);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        if (List is null)
        {
            Help.MakeNewInteger(Params.Result!, 0);
            return;
        }

        int Count = 0;
        for (int i = 0; i < List.Count; i++)
        {
            if (List[i]!.IsEqual(Params.Values[1]))
            {
                Count++;
            }
        }

        Help.MakeNewInteger(Params.Result!, Count);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_IncludesAll(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("includesAll", [Help.ListType, Help.ListType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 134, true);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? list1 = XList(Params.Values[0]);
        TBoldList? list2 = XList(Params.Values[1]);
        if (list2 is null || list2.Count == 0)
        {
            Help.MakeNewBoolean(Params.Result!, true);
        }
        else if (list1 is null)
        {
            Help.MakeNewBoolean(Params.Result!, false);
        }
        else
        {
            bool IncludesAll = true;
            for (int i = 0; i < list2.Count && IncludesAll; i++)
            {
                IncludesAll = list1.Includes(list2[i]);
            }

            Help.MakeNewBoolean(Params.Result!, IncludesAll);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_isEmpty(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("isEmpty", [Help.ListType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 135);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, XList(Params.Values[0]) is not { Count: > 0 });
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_NotEmpty(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("notEmpty", [Help.ListType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 136);

    /// <summary>True for a non-empty list; also true when there is no list at all, as in Delphi.</summary>
    [Delphi(PortStatus.Ported, Note = "Keeps Delphi's result of true when there is no list.")]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        Help.MakeNewBoolean(Params.Result!, List is null || List.Count != 0);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_isNull(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("isNull", [null], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 180);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, Params.Values[0] is TBoldAttribute { IsNull: true });
}

/// <summary>The sum, as an integer, currency or float by the element type.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Sum(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("sum", [Help.NumericListType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 137);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        double Sum = 0;
        TBoldList? List = XList(Params.Values[0]);
        if (List is not null)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Sum += XNumeric(List[i]);
            }
        }

        TBoldSymbolResult.MakeNumber(Help, Params.Result!, Sum);
    }
}

/// <summary>Puts a number in the result as an integer (rounded), currency or float, by the result's type.</summary>
internal static class TBoldSymbolResult
{
    internal static void MakeNumber(TBoldOclSymbolHelp Help, TBoldOclNode Result, double value)
    {
        TBoldElementTypeInfo ListElementType = Result.BoldType!;
        if (ListElementType.ConformsTo(Help.IntegerType))
        {
            Help.MakeNewInteger(Result, (int)Math.Round(value, MidpointRounding.ToEven));
        }
        else if (ListElementType.ConformsTo(Help.CurrencyType))
        {
            Help.MakeNewCurrency(Result, (decimal)value);
        }
        else
        {
            Help.MakeNewNumeric(Result, value);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SumTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("sumTime", [Help.MomentListType], TBoldOclDeduceMethod.tbodNo, Help.DateTimeType, true, 189);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TDateTime Sum = SysUtils.ZeroDateTime;
        TBoldList? List = XList(Params.Values[0]);
        if (List is not null)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Sum = BoldOclSymbolImplementations.AddMoments(Sum, XDateTime(List[i]));
            }
        }

        Help.MakeNewDateTime(Params.Result!, Sum);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Maxvalue(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("maxValue", [Help.NumericListType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 139);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        double MAX = 0;
        if (List is { Count: > 0 })
        {
            MAX = XNumeric(List[0]);
            for (int i = 1; i < List.Count; i++)
            {
                MAX = Math.Max(MAX, XNumeric(List[i]));
            }
        }

        TBoldSymbolResult.MakeNumber(Help, Params.Result!, MAX);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_MinValue(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("minValue", [Help.NumericListType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 138);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        double Min = 0;
        if (List is { Count: > 0 })
        {
            Min = XNumeric(List[0]);
            for (int i = 1; i < List.Count; i++)
            {
                Min = Math.Min(Min, XNumeric(List[i]));
            }
        }

        TBoldSymbolResult.MakeNumber(Help, Params.Result!, Min);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Average(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("average", [Help.NumericListType], TBoldOclDeduceMethod.tbodNo, Help.RealType, true, 140);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        if (List is not { Count: > 0 })
        {
            Help.MakeNewNumeric(Params.Result!, 0);
            return;
        }

        double Sum = 0;
        for (int i = 0; i < List.Count; i++)
        {
            Sum += XNumeric(List[i]);
        }

        Help.MakeNewNumeric(Params.Result!, Sum / List.Count);
    }
}

/// <summary>Iteration step: the result stays true while the body is true for all.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_ForAll(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("forAll", [Help.ListType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 142);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        ((TBABoolean)Params.Result!.Value!).AsBoolean = XBoolean(Params.Result.Value) && XBoolean(Params.Values[0]);
}

/// <summary>Iteration step: the result becomes true when the body is true for one.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Exists(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("exists", [Help.ListType, Help.BooleanType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 141);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        ((TBABoolean)Params.Result!.Value!).AsBoolean = XBoolean(Params.Result.Value) || XBoolean(Params.Values[0]);
}

/// <summary>The union; a fresh list when the second list's type is wider than the first's.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_union(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("union", [Help.ListType, Help.ListType], TBoldOclDeduceMethod.tbodLCC, null, true, 143);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Values[0]) is null)
        {
            Help.TransferOrClone(Params.Nodes[1]!, Params.Result!);
            return;
        }

        // When the result is a supertype of the arguments, neither list can be reused.
        if (Params.Nodes[0]!.BoldType is not null && Params.Nodes[1]!.BoldType is not null && !Params.Nodes[1]!.BoldType!.ConformsTo(Params.Nodes[0]!.BoldType))
        {
            Params.Result!.SetOwnedValue(Help.CreateNewMember(Params.Result.BoldType));
            XList(Params.Result.Value)!.AddList(XList(Params.Values[0])!);
        }
        else
        {
            Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        }

        if (XList(Params.Values[1]) is { } second)
        {
            XList(Params.Result!.Value)!.AddList(second);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Intersection(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("intersection", [Help.ListType, Help.ListType], TBoldOclDeduceMethod.tbodLCC, null, true, 144, true);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? list1 = XList(Params.Values[0]);
        TBoldList? list2 = XList(Params.Values[1]);
        if (list1 is null)
        {
            Help.TransferOrClone(Params.Nodes[1]!, Params.Result!);
        }
        else if (list2 is null)
        {
            Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        }
        else
        {
            if (list2.Count < list1.Count)
            {
                // Loop over the shorter list.
                (list1, list2) = (list2, list1);
            }

            var resList = (TBoldList)Help.CreateNewMember(Params.Result!.BoldType);
            for (int i = 0; i < list1.Count; i++)
            {
                if (list2.Includes(list1[i]))
                {
                    resList.Add(list1[i]);
                }
            }

            Params.Result.SetOwnedValue(resList);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_difference(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("difference", [Help.ListType, Help.ListType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 145, true);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Values[0]) is null)
        {
            Params.Result!.SetReferenceValue(null);
        }
        else if (XList(Params.Values[1]) is null)
        {
            Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        }
        else
        {
            TBoldList list2 = XList(Params.Values[1])!;
            Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
            TBoldList list1 = XList(Params.Result!.Value)!;
            for (int i = 0; i < list2.Count; i++)
            {
                int p = list1.IndexOf(list2[i]);
                if (p != -1)
                {
                    list1.RemoveByIndex(p);
                }
            }
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Including(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("including", [Help.ListType, Help.ObjectType], TBoldOclDeduceMethod.tbodLCC, null, true, 146);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        var resList = (TBoldList)Help.CreateNewMember(Params.Result!.BoldType);
        Params.Result.SetOwnedValue(resList);
        if (XList(Params.Values[0]) is { } source)
        {
            resList.AddList(source);
        }

        if (Params.Values[1] is not null)
        {
            resList.Add(Params.Values[1]);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_excluding(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("excluding", [Help.ListType, Help.ObjectType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 147);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        TBoldList? OrgList = XList(Params.Result!.Value);
        if (OrgList is not null && OrgList.Includes(Params.Values[1]))
        {
            OrgList.Remove(Params.Values[1]);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SymmetricDifference(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("symmetricDifference", [Help.ListType, Help.ListType], TBoldOclDeduceMethod.tbodLCC, null, true, 148);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? list1 = XList(Params.Values[0]);
        TBoldList? list2 = XList(Params.Values[1]);
        if (list1 is null)
        {
            Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        }
        else if (list2 is null)
        {
            Help.TransferOrClone(Params.Nodes[1]!, Params.Result!);
        }
        else
        {
            var resList = (TBoldList)Help.CreateNewMember(Params.Result!.BoldType);
            for (int i = 0; i < list1.Count; i++)
            {
                if (!list2.Includes(list1[i]))
                {
                    resList.Add(list1[i]);
                }
            }

            for (int i = 0; i < list2.Count; i++)
            {
                if (!list1.Includes(list2[i]))
                {
                    resList.Add(list2[i]);
                }
            }

            Params.Result.SetOwnedValue(resList);
        }
    }
}

/// <summary>Iteration step: adds the element when the body is true.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Select(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("select", [Help.ListType, Help.BooleanType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 149);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Result!.Value) is { } result && XBoolean(Params.Values[0]))
        {
            result.Add(Params.Values[1]);
        }
    }
}

/// <summary>Iteration step: adds the element when the body is false.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_reject(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("reject", [Help.ListType, Help.BooleanType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 150);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Result!.Value) is { } result && !XBoolean(Params.Values[0]))
        {
            result.Add(Params.Values[1]);
        }
    }
}

/// <summary>Iteration step: adds the body's value (flattening a list).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_collect(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("collect", [Help.ListType, null], TBoldOclDeduceMethod.tbodListofArg2, null, true, 151);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Result!.Value) is not { } ResultList)
        {
            return;
        }

        if (XList(Params.Values[0]) is { } SourceList)
        {
            for (int i = 0; i < SourceList.Count; i++)
            {
                ResultList.Add(SourceList[i]);
            }
        }
        else
        {
            ResultList.Add(Params.Values[0]);
        }
    }
}

/// <summary>Copies the argument as a list into the result (an empty list for none).</summary>
[Delphi(PortStatus.Ported)]
internal abstract class TBOS__ListCopier(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected void CopyListToResult(TBoldOclSymbolParameters Params)
    {
        if (Params.Values[0] is not null)
        {
            var temp = new TBoldIndirectElement();
            Params.Values[0]!.GetAsList(temp);
            Help.TransferOrClone(temp, Params.Result!);
            temp.Dispose();
        }
        else
        {
            Params.Result!.SetOwnedValue(Help.CreateNewMember(Params.Result.BoldType));
        }
    }

    protected void CopyWithMode(TBoldOclSymbolParameters Params, TBoldListDupMode Mode)
    {
        CopyListToResult(Params);
        if (XList(Params.Result!.Value) is { } list)
        {
            list.DuplicateMode = Mode;
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AsSequence(TBoldOclSymbolHelp Help) : TBOS__ListCopier(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("asSequence", [Help.ListType], TBoldOclDeduceMethod.tbodArg1AsList, null, true, 152);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => CopyWithMode(Params, TBoldListDupMode.bldmAllow);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AsBag(TBoldOclSymbolHelp Help) : TBOS__ListCopier(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("asBag", [Help.ListType], TBoldOclDeduceMethod.tbodArg1AsList, null, true, 153);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => CopyWithMode(Params, TBoldListDupMode.bldmAllow);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AsSet(TBoldOclSymbolHelp Help) : TBOS__ListCopier(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("asSet", [Help.ListType], TBoldOclDeduceMethod.tbodArg1AsList, null, true, 154);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => CopyWithMode(Params, TBoldListDupMode.bldmMerge);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Append(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("append", [Help.ListType, Help.ObjectType], TBoldOclDeduceMethod.tbodLCC, null, true, 155);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        XList(Params.Result!.Value)?.Add(Params.Values[1]);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Prepend(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("prepend", [Help.ListType, Help.ObjectType], TBoldOclDeduceMethod.tbodLCC, null, true, 156);

    [Delphi(PortStatus.Ported, Note = "Inserts the second argument; Delphi inserts the first (the list itself), a bug.")]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        Help.TransferOrClone(Params.Nodes[0]!, Params.Result!);
        XList(Params.Result!.Value)?.Insert(0, Params.Values[1]);
    }
}

/// <summary>The elements start..stop (1-based, clipped to the list).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SubSequence(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("subSequence", [Help.ListType, Help.IntegerType, Help.IntegerType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 157);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList List = XList(Params.Values[0]) ?? throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boeArgrtIsNotList, 0, "->subSequence"));
        int Start = Math.Max(XInteger(Params.Values[1]) - 1, 0);
        int Stop = Math.Min(XInteger(Params.Values[2]) - 1, List.Count - 1);
        var resList = (TBoldList)Help.CreateNewMember(Params.Result!.BoldType);
        for (int i = Start; i <= Stop; i++)
        {
            resList.Add(List[i]);
        }

        Params.Result.SetOwnedValue(resList);
    }
}

/// <summary>The element at a 1-based index; raises when out of bounds.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_at(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("at", [Help.ListType, Help.IntegerType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 158);

    [Delphi(PortStatus.Ported, Note = "Checks the bounds where Delphi catches the list's EListError.")]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        if (List is null)
        {
            Params.Result!.SetReferenceValue(null);
            return;
        }

        int P = XInteger(Params.Values[1]) - 1;
        if (P < 0 || P >= List.Count)
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteAtIndexOutOfBounds, 0, P, List.Count));
        }

        List[P]!.GetAsValue(Params.Result!);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_first(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("first", [Help.ListType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 159);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        TBoldElement? EL = List is { Count: > 0 } ? List[0] : null;
        if (EL is not null)
        {
            EL.GetAsValue(Params.Result!);
        }
        else
        {
            Params.Result!.SetReferenceValue(null);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_last(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("last", [Help.ListType], TBoldOclDeduceMethod.tbodCopyArg1Elem, null, true, 160);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList? List = XList(Params.Values[0]);
        TBoldElement? EL = List is { Count: > 0 } ? List[List.Count - 1] : null;
        if (EL is not null)
        {
            EL.GetAsValue(Params.Result!);
        }
        else
        {
            Params.Result!.SetReferenceValue(null);
        }
    }
}

/// <summary>Iteration step of orderby: collects the sort key (a null key of the body's type when none).</summary>
[Delphi(PortStatus.Ported)]
internal abstract class TBOS_GenericOrder(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (XList(Params.Result!.Value) is not TBoldMemberList SortList)
        {
            return;
        }

        if (Params.Values[0] is not null)
        {
            SortList.Add((TBoldMember)Params.Values[0]!);
        }
        else
        {
            var DummyValue = (TBoldAttribute)TBoldMemberFactory.CreateMemberFromBoldType(Params.Nodes[0]!.BoldType);
            DummyValue.SetToNull();
            SortList.Add(DummyValue);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_orderby(TBoldOclSymbolHelp Help) : TBOS_GenericOrder(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("orderby", [Help.ListType, null], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 161);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_orderDescending(TBoldOclSymbolHelp Help) : TBOS_GenericOrder(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("orderdescending", [Help.ListType, null], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 162);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_asString(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("asString", [null], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 163);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        Help.MakeNewString(Params.Result!, XString(Params.Values[0]));
        Params.Values[0]?.SubscribeToStringRepresentation(BoldDefs.brDefault, Params.Subscriber, BoldSubscription.breReEvaluate);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_dateTimeAsFloat(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("dateTimeAsFloat", [Help.MomentType], TBoldOclDeduceMethod.tbodNo, Help.RealType, true, 163);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewNumeric(Params.Result!, XDateTime(Params.Values[0]).ToOADate());
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_StringRepresentation(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("stringRepresentation", [null, Help.IntegerType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 190);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        int Rep = Params.Values[1] is not null ? XInteger(Params.Values[1]) : BoldDefs.brDefault;
        if (Params.Values[0] is not null)
        {
            Help.MakeNewString(Params.Result!, Params.Values[0]!.StringRepresentation(Rep));
            Params.Values[0]!.SubscribeToStringRepresentation(Rep, Params.Subscriber, BoldSubscription.breReEvaluate);
        }
        else
        {
            Help.MakeNewString(Params.Result!, "");
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_TaggedValue(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("taggedValue", [Help.ObjectType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 190);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        string value = Params.Values[0] is TBoldObject obj ? obj.BoldClassTypeInfo.TaggedValues(XString(Params.Values[1])) : "";
        Help.MakeNewString(Params.Result!, value);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_TypeName(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("typename", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.StringType, true, 164);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewString(Params.Result!, XString(Params.Values[0]));
}

/// <summary>The names of a class's members that pass the filter.</summary>
internal abstract class TBOS_MemberNames(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    protected void AddNames(TBoldOclSymbolParameters Params, Func<TBoldMemberRTInfo, bool> Filter)
    {
        Params.Result!.SetOwnedValue(Help.CreateNewMember(Params.Result.BoldType));
        if (Params.Values[0] is not TBoldClassTypeInfo ClassInfo)
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteNonClassAttribute, 0, Params.Values[0]?.AsString ?? ""));
        }

        var Name = (TBAString)Help.CreateNewMember(Help.StringType);
        for (int i = 0; i < ClassInfo.AllMembers.Count; i++)
        {
            TBoldMemberRTInfo MemberRTInfo = ClassInfo.AllMembers[i];
            if (Filter(MemberRTInfo))
            {
                Name.AsString = MemberRTInfo.ExpressionName;
                ((TBoldList)Params.Result.Value!).Add(Name);
            }
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Attributes(TBoldOclSymbolHelp Help) : TBOS_MemberNames(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("attributes", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.StringListType, true, 165);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => AddNames(Params, m => m.IsAttribute);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AssociationEnds(TBoldOclSymbolHelp Help) : TBOS_MemberNames(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("associationEnds", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.StringListType, true, 166);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => AddNames(Params, m => m.IsRole);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SuperTypes(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("superTypes", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.TypeListType, true, 168);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldTypeList List = TBoldTypeListFactory.CreateList(Params.Result!.BoldType!);
        Params.Result.SetOwnedValue(List);
        switch (Params.Values[0])
        {
            case TBoldClassTypeInfo { SuperClassTypeInfo: { } super }:
                List.Add(super);
                break;
            case TBoldAttributeTypeInfo { SuperAttributeTypeInfo: { } superAttr }:
                List.Add(superAttr);
                break;
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AllSuperTypes(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("allSuperTypes", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.TypeListType, true, 169);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldTypeList List = TBoldTypeListFactory.CreateList(Params.Result!.BoldType!);
        Params.Result.SetOwnedValue(List);
        if (Params.Values[0] is TBoldClassTypeInfo ClassTypeInfo)
        {
            for (TBoldClassTypeInfo? c = ClassTypeInfo.SuperClassTypeInfo; c is not null; c = c.SuperClassTypeInfo)
            {
                List.Add(c);
            }
        }
        else if (Params.Values[0] is TBoldAttributeTypeInfo AttrTypeInfo)
        {
            for (TBoldAttributeTypeInfo? a = AttrTypeInfo.SuperAttributeTypeInfo; a is not null; a = a.SuperAttributeTypeInfo)
            {
                List.Add(a);
            }
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AllSubClasses(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("allSubClasses", [Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.TypeListType, true, 169);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldTypeList List = TBoldTypeListFactory.CreateList(Params.Result!.BoldType!);
        Params.Result.SetOwnedValue(List);
        if (Params.Values[0] is TBoldClassTypeInfo ClassTypeInfo)
        {
            TBoldClassTypeInfoList classes = Params.SystemTypeInfo!.TopSortedClasses;
            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i].ConformsTo(ClassTypeInfo))
                {
                    List.Add(classes[i]);
                }
            }
        }
    }
}

/// <summary>The extent of a class, or the values of a value-set type.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AllInstances(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("allInstances", [Help.TypeType], TBoldOclDeduceMethod.TbodObjectlist, null, true, 170);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        if (Params.Values[0] is TBoldClassTypeInfo ClassTypeInfo)
        {
            if (Params.System is null)
            {
                throw new EBoldOclRunTimeError(BoldCoreConsts.sUnableToGetAllInstances);
            }

            Params.Result!.SetReferenceValue(Params.System.Classes(ClassTypeInfo.TopSortedIndex));
        }
        else if (Params.Values[0] is TBoldAttributeTypeInfo AttributeTypeInfo &&
                 AttributeTypeInfo.ConformsTo(Params.SystemTypeInfo!.AttributeTypeInfoByExpressionName("ValueSet")))
        {
            var ValueSetlist = (TBoldMemberList)Help.CreateNewMember(Params.SystemTypeInfo.ListTypeInfoByElement(AttributeTypeInfo));
            var StrList = new TStringList();
            var valueSet = (TBAValueSet)Help.CreateNewMember(AttributeTypeInfo);
            valueSet.Values.ToStrings(BoldDefs.brDefault, StrList);
            for (int i = 0; i < StrList.Count; i++)
            {
                ValueSetlist.AddNew().SetStringRepresentation(BoldDefs.brDefault, StrList[i]);
            }

            Params.Result!.SetOwnedValue(ValueSetlist);
        }
    }
}

/// <summary>The loaded objects of a class (the extent itself when it is current), subscribed to additions and fetches.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AllLoadedObjects(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("allLoadedObjects", [Help.TypeType], TBoldOclDeduceMethod.TbodObjectlist, null, true, 170);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        var ClassTypeInfo = (TBoldClassTypeInfo)Params.Values[0]!;
        if (Params.System is null)
        {
            throw new EBoldOclRunTimeError(BoldCoreConsts.sUnableToGetAllLoadedObjects);
        }

        TBoldObjectList ClassList = Params.System.Classes(ClassTypeInfo.TopSortedIndex)!;
        if (Params.Subscriber is not null)
        {
            ClassList.AddSmallSubscription(Params.Subscriber,
                TBoldSmallEventSet.Of(BoldSubscription.beItemAdded, BoldSubscription.beItemDeleted, BoldSubscription.beObjectFetched), BoldSubscription.breReEvaluate);
        }

        if (ClassList.BoldPersistenceState != TBoldValuePersistenceState.bvpsCurrent)
        {
            var ObjectList = (TBoldObjectList)Help.CreateNewMember(Params.Result!.BoldType);
            Params.Result.SetOwnedValue(ObjectList);
            using TBoldLocatorListTraverser Traverser = Params.System.Locators.CreateTraverser();
            while (!Traverser.EndOfList)
            {
                TBoldObject? obj = Traverser.locator.BoldObject;
                if (obj is not null && obj.BoldType!.ConformsTo(ClassTypeInfo))
                {
                    ObjectList.Add(obj);
                }

                Traverser.Next();
            }
        }
        else
        {
            Params.Result!.SetReferenceValue(ClassList);
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_EmptyLIst(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_emptyList.Init")] protected override void Init() => InternalInit("emptyList", [Help.TypeType], TBoldOclDeduceMethod.TbodObjectlist, null, true, 170);
    [Delphi(PortStatus.Ported, Symbol = "TBOS_EmptyList.Evaluate")] public override void Evaluate(TBoldOclSymbolParameters Params) => Params.Result!.SetOwnedValue(Help.CreateNewMember(Params.Result.BoldType));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_oclType(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("oclType", [null], TBoldOclDeduceMethod.tbodNo, Help.TypeType, true, 171);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Params.Result!.SetReferenceValue(Params.Values[0]?.BoldType);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_oclIsKindOf(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("oclIsKindOf", [null, Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 172);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo type = XType(Params.Values[1]) ?? throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boeArgrtIsNottype, 0, "->oclIsKindOf"));
        Help.MakeNewBoolean(Params.Result!, Params.Values[0] is not null && Params.Values[0]!.BoldType!.ConformsTo(type));
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_OclIsTypeOf(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("oclIsTypeOf", [null, Help.TypeType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 173);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo type = XType(Params.Values[1]) ?? throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boeArgrtIsNottype, 0, "->oclIsTypeOf"));
        Help.MakeNewBoolean(Params.Result!, Params.Values[0] is not null && Params.Values[0]!.BoldType == type);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_OclAsType(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("oclAsType", [null, Help.TypeType], TBoldOclDeduceMethod.tbodTypecast, null, true, 174);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo type = XType(Params.Values[1]) ?? throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boeArgrtIsNottype, 0, "->oclAsType"));
        if (Params.Values[0] is null || Params.Values[0]!.BoldType!.ConformsTo(type))
        {
            Params.Result!.SetReferenceValue(Params.Values[0]);
        }
        else
        {
            throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.borteInvalidCast, 0, Params.Values[0]!.BoldType!.AsString, Params.Values[1]!.AsString));
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_safeCast(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_SafeCast.Init")] protected override void Init() => InternalInit("safeCast", [null, Help.TypeType], TBoldOclDeduceMethod.tbodTypecast, null, true, 174);

    [Delphi(PortStatus.Ported, Symbol = "TBOS_SafeCast.Evaluate")]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldElementTypeInfo type = XType(Params.Values[1]) ?? throw new EBoldOclRunTimeError(SysUtils.Format(BoldOclError.boeArgrtIsNottype, 0, ".safeCast"));
        Params.Result!.SetReferenceValue(Params.Values[0] is null || Params.Values[0]!.BoldType!.ConformsTo(type) ? Params.Values[0] : null);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SQLLike(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_sqlLike.Init")] protected override void Init() => InternalInit("sqlLike", [Help.StringType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 175);

    [Delphi(PortStatus.Ported, Note = "Matches with a .NET Regex built by FixSQLRegExp.")]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewBoolean(Params.Result!, Regex.IsMatch(XString(Params.Values[0]), BoldOclSymbolImplementations.FixSQLRegExp(XString(Params.Values[1])), RegexOptions.Singleline));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_SQLLikeCaseInsensitive(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_SqlLikeCaseInsensitive.Init")]
    protected override void Init() => InternalInit("sqlLikeCaseInsensitive", [Help.StringType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 176);

    [Delphi(PortStatus.Ported, Symbol = "TBOS_SQLLikeCaseInSensitive.Evaluate", Note = "Matches with a .NET Regex built by FixSQLRegExp.")]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewBoolean(Params.Result!, Regex.IsMatch(XString(Params.Values[0]), BoldOclSymbolImplementations.FixSQLRegExp(XString(Params.Values[1])),
            RegexOptions.Singleline | RegexOptions.IgnoreCase));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_RegExpMatch(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("regExpMatch", [Help.StringType, Help.StringType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 177);

    /// <summary>True when the pattern is found in the string.</summary>
    [Delphi(PortStatus.Replaced, Note = "Uses .NET regular expression syntax; BoldRegularExpression (not in the sources) had its own.")]
    public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewBoolean(Params.Result!, Regex.IsMatch(XString(Params.Values[0]), XString(Params.Values[1])));
}

/// <summary>Whether the moment's day number is in start..stop (day numbers count from 1899-12-30).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_InDateRange(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("inDateRange", [Help.MomentType, Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 178);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        var Moment = (TBAMoment)Params.Values[0]!;
        if (Moment.IsNull)
        {
            Help.MakeNewBoolean(Params.Result!, false);
            return;
        }

        double val = Math.Truncate(TBOS_MomentHelper.MomentValue(Moment).ToOADate());
        double Start = Math.Truncate(((TBANumeric)Params.Values[1]!).AsFloat);
        double Stop = Math.Truncate(((TBANumeric)Params.Values[2]!).AsFloat);
        Help.MakeNewBoolean(Params.Result!, val >= Start && val <= Stop);
    }
}

/// <summary>Whether the moment's time of day (as a fraction of a day) is in start..stop.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_InTimeRange(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() => InternalInit("inTimeRange", [Help.MomentType, Help.NumericType, Help.NumericType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 179);

    private static double Frac(double x) => x - Math.Truncate(x);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        double val = Frac(TBOS_MomentHelper.MomentValue((TBAMoment)Params.Values[0]!).ToOADate());
        double Start = Frac(((TBANumeric)Params.Values[1]!).AsFloat);
        double Stop = Frac(((TBANumeric)Params.Values[2]!).AsFloat);
        Help.MakeNewBoolean(Params.Result!, val >= Start && val <= Stop);
    }
}

/// <summary>The constraints of a member, of an object's class and superclasses, or of the system.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Constraints(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)]
    protected override void Init() =>
        InternalInit("constraints", [null], TBoldOclDeduceMethod.tbodNo, Help.SystemTypeInfo.ListTypeInfoByElement(Help.ConstraintType), true, 181);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        var ResList = (TBoldMemberList)Help.CreateNewMember(Params.Result!.BoldType);
        Params.Result.SetOwnedValue(ResList);

        void AddConstraint(TBoldConstraintRTInfo? IncomingConstraint, TBoldElement element) =>
            ((TBAConstraint)ResList.AddNew()).Initialize(IncomingConstraint, element);

        switch (Params.Values[0])
        {
            case TBoldMember Member:
                for (int i = 0; i < Member.BoldMemberRTInfo!.ConstraintCount; i++)
                {
                    AddConstraint(Member.BoldMemberRTInfo.ConstraintByIndex(i), Member);
                }

                break;
            case TBoldObject obj:
                for (TBoldClassTypeInfo? ClassTypeInfo = obj.BoldClassTypeInfo; ClassTypeInfo is not null; ClassTypeInfo = ClassTypeInfo.SuperClassTypeInfo)
                {
                    for (int i = 0; i < ClassTypeInfo.ConstraintCount; i++)
                    {
                        AddConstraint(ClassTypeInfo.ConstraintByIndex(i), obj);
                    }
                }

                break;
            case TBoldSystem system:
                for (int i = 0; i < system.BoldSystemTypeInfo.ConstraintCount; i++)
                {
                    AddConstraint(system.BoldSystemTypeInfo.ConstraintByIndex(i), system);
                }

                break;
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AtTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("atTime", [Help.ObjectType, Help.IntegerType], TBoldOclDeduceMethod.tbodCopyArg1, null, true, 182);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Params.Result!.SetReferenceValue(((TBoldObject)Params.Values[0]!).AtTime(((TBAInteger)Params.Values[1]!).AsInteger));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_ObjectTimeStamp(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("objectTimeStamp", [Help.ObjectType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 182);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewInteger(Params.Result!, ((TBoldObject)Params.Values[0]!).BoldObjectLocator!.BoldObjectID.TimeStamp);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_AllInstancesAtTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_allInstancesAtTime.Init")]
    protected override void Init() => InternalInit("allInstancesAtTime", [Help.TypeType, Help.IntegerType], TBoldOclDeduceMethod.TbodObjectlist, null, true, 183);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        int NewTime = ((TBAInteger)Params.Values[1]!).AsInteger;
        if (Params.Values[0] is not TBoldClassTypeInfo ClassTypeInfo)
        {
            throw new EBoldOclRunTimeError(BoldCoreConsts.sAllInstancesAtTimeOnlyAllowedOnClasses);
        }

        if (Params.System is null)
        {
            throw new EBoldOclRunTimeError(BoldCoreConsts.sUnableToGetAllInstances);
        }

        Params.Result!.SetReferenceValue(Params.System.Classes(ClassTypeInfo.TopSortedIndex)!.AtTime(NewTime));
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_Existing(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported, Symbol = "TBOS_existing.Init")] protected override void Init() => InternalInit("existing", [Help.ObjectType], TBoldOclDeduceMethod.tbodNo, Help.BooleanType, true, 184);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewBoolean(Params.Result!, ((TBoldObject)Params.Values[0]!).BoldExistenceState == TBoldExistenceState.besExisting);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_FilterOnType(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("filterOnType", [Help.ListType, Help.TypeType], TBoldOclDeduceMethod.tbodListFromArg2, null, true, 185);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params)
    {
        TBoldList IncomingList = XList(Params.Values[0])!;
        var OutList = (TBoldList)TBoldMemberFactory.CreateMemberFromBoldType(Params.Result!.BoldType);
        var type = (TBoldElementTypeInfo)Params.Values[1]!;
        for (int i = 0; i < IncomingList.Count; i++)
        {
            if (IncomingList[i]!.BoldType!.ConformsTo(type))
            {
                OutList.Add(IncomingList[i]);
            }
        }

        Params.Result.SetOwnedValue(OutList);
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_BoldTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("boldTime", [Help.ObjectType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 186);
    [Delphi(PortStatus.Ported)] public override void Evaluate(TBoldOclSymbolParameters Params) => Help.MakeNewInteger(Params.Result!, ((TBoldObject)Params.Values[0]!).BoldTime);
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_TimeStampToTime(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("timeStampToTime", [Help.IntegerType], TBoldOclDeduceMethod.tbodNo, Help.DateTimeType, true, 187);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewDateTime(Params.Result!, Params.System!.TimeForTimestamp(((TBAInteger)Params.Values[0]!).AsInteger));
}

[Delphi(PortStatus.Ported)]
internal sealed class TBOS_TimeToTimeStamp(TBoldOclSymbolHelp Help) : TBoldOclSymbol(Help)
{
    [Delphi(PortStatus.Ported)] protected override void Init() => InternalInit("timeToTimeStamp", [Help.DateTimeType], TBoldOclDeduceMethod.tbodNo, Help.IntegerType, true, 188);

    [Delphi(PortStatus.Ported)]
    public override void Evaluate(TBoldOclSymbolParameters Params) =>
        Help.MakeNewInteger(Params.Result!, Params.System!.TimestampForTime(((TBADateTime)Params.Values[0]!).AsDateTime));
}
