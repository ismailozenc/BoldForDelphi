// Delphi: Source/ObjectSpace/Ocl/BoldOclError.pas
// Port map: CSharp/port/units/BoldOclError.md

namespace Bold;

/// <summary>Messages of the OCL parser and evaluator. Each starts with the position in the expression and a colon.</summary>
public static partial class BoldOclError
{
    [Delphi(PortStatus.Ported)] public const string boeUserSpecifiedCollections = "%d:User specified collections are not implemented";
    [Delphi(PortStatus.Ported)] public const string boeTimeExpressionsUnIMpl = "%d:Timeexpressions aren't implemented";
    [Delphi(PortStatus.Ported)] public const string boeUserSpecifiedEnumsUnImpl = "%d:User specified enumerations aren't implemented";
    [Delphi(PortStatus.Ported)] public const string boePackageSpecUnImpl = "%d:Package specification in types aren't implemented";
    [Delphi(PortStatus.Ported)] public const string boeConversionError = "%d:Strange... Conversion failed in OCL parser (tried to convert %s to %s)";
    [Delphi(PortStatus.Ported)] public const string boeInvalidMemberName = "%d:Member names may not be type path-names, only single names";
    [Delphi(PortStatus.Ported)] public const string boeInvalidMethodName = "%d:Method names may not be type path-names, only single names";
    [Delphi(PortStatus.Ported)] public const string boeInvalidLoopVariable = "%d:This function (%s) may not define a loop variable";
    [Delphi(PortStatus.Ported)] public const string boeOperationWithOutContext = "%d:Strange indeed! an operation without a context...";
    [Delphi(PortStatus.Ported)] public const string boeInvalidQualifier = "%d:Qualifiers aren't legal here";
    [Delphi(PortStatus.Ported)] public const string boeInvalidTimeSpecifier = "%d:Timespecifiers aren't legal here";
    [Delphi(PortStatus.Ported)] public const string boeOperationNotDefinedOnArgTypes = "%d:%s %s %s is not defined";
    [Delphi(PortStatus.Ported)] public const string boeNoConform = "%d:%s does not conform to %s";
    [Delphi(PortStatus.Ported)] public const string boeSystemAsOperationArgument = "%d:The entire system can not be argument to an operation";
    [Delphi(PortStatus.Ported)] public const string boeUndefinedOperation = "%d:Undefined operation: %s";
    [Delphi(PortStatus.Ported)] public const string boeWrongNumberOfArgs = "%d:%d parameters expected, %d found...";
    [Delphi(PortStatus.Ported)] public const string boeInvalidOperationOnAttr = "%d:%s is not an operation on the type %s";
    [Delphi(PortStatus.Ported)] public const string boeMethodsNotImplemented = "%d:Method calls on classes are not yet implemented";
    [Delphi(PortStatus.Ported)] public const string boeUnknownClass = "%d:%s is not a classname in the model";
    [Delphi(PortStatus.Ported)] public const string boeUnknownMember = "%d:%s is not a member of %s";
    [Delphi(PortStatus.Ported)] public const string boeUnknownMemberType = "%d:This member is neither an attribute or a role (%s)";
    [Delphi(PortStatus.Ported)] public const string boeNotAnOperation = "%d:%s is not an operation on %s";
    [Delphi(PortStatus.Ported)] public const string boeUnknownVariable = "%d:Can't find variable %s in environment";
    [Delphi(PortStatus.Ported)] public const string boeOperationWithLoopvar = "%d:Only iterations may use CopyLoopVar-semantics! (tried with %s)";
    [Delphi(PortStatus.Ported)] public const string boeNonListArgumentToCopyArg1Elem = "%d:Can't take ListElement-type of a non-list";
    [Delphi(PortStatus.Ported)] public const string boeNoCommonSuperClass = "%d:The parameters of this operation have no common superclass (%s and %s)";
    [Delphi(PortStatus.Ported)] public const string boeNoRootElement = "%d:OCL expression has no root element";
    [Delphi(PortStatus.Ported)] public const string boeUnKnownRoot = "%d:Unknown type of root: %s";
    [Delphi(PortStatus.Ported)] public const string boeExpressionNeedsContext = "%d:This expression needs a context/model";
    [Delphi(PortStatus.Ported)] public const string boeExpressionNeedsRoot = "%d:This expression needs a root element";
    [Delphi(PortStatus.Ported)] public const string boeVariableNotAssigned = "%d:This variable (%s) has no value or type";
    [Delphi(PortStatus.Ported)] public const string boeSubscribeBeforeEval = "%d:You can not subscribe to an expression that has not been evaluated";
    [Delphi(PortStatus.Ported)] public const string boeCollectionNotValidLiteral = "%d:You can not create a literal of type \"Collection\", try Set, Sequence or Bag instead";
    [Delphi(PortStatus.Ported)] public const string boeElementnotConformToCollection = "%d:All elements in a collection literal must conform to a common type, this one doesn't";
    [Delphi(PortStatus.Ported)] public const string boeRangeMustBeInt = "%d:Ranges can only consist of integers";
    [Delphi(PortStatus.Ported)] public const string boeInvalidCharacter = "%d:Expression contains an invalid character";
    [Delphi(PortStatus.Ported)] public const string boeUnMatchedParentesis = "%d:Parenthesis are not in balance";
    [Delphi(PortStatus.Ported)] public const string boeunterminatedQoute = "%d:Unterminated string in expression";
    [Delphi(PortStatus.Ported)] public const string boeUnKnownIterator = "%d:Semantics for this iterator has not been implemented";
    [Delphi(PortStatus.Ported)] public const string boeUnknownContext = "%d:Unknown type (%s) of Context in SemanticCheck";
    [Delphi(PortStatus.Ported)] public const string boeUnKnownTypeOfLoopVar = "%d:Unknown type (%s) of loop variable";
    [Delphi(PortStatus.Ported)] public const string boeModelIsNoModel = "%d:For some Reason, the model of the expression is no model...(%s)";
    [Delphi(PortStatus.Ported)] public const string boeInvalidTypeNameName = "%d:This Multiple parts in typenames are not implemented";
    [Delphi(PortStatus.Ported)] public const string boeunknownType = "%d:This is not a typename";
    [Delphi(PortStatus.Ported)] public const string boeunknownTypeButMember = "%d:%s is not a typename (but it is a member of the current class context, use lowercase!)";
    [Delphi(PortStatus.Ported)] public const string boeMemberofType = "%d:Types can not have members (tried %s.%s)";
    [Delphi(PortStatus.Ported)] public const string boeMemberofAttr = "%d:Attributes can not have members (tried %s.%s)";
    [Delphi(PortStatus.Ported)] public const string boeEnvSizeError = "%d:Started with %d vars, ended up with %d...";
    [Delphi(PortStatus.Ported)] public const string boeunknownExprtypeinDeduce = "%d:Unable to deduce type of expression " + BoldDefs.BOLDCRLF +
        "Operation: %s " + BoldDefs.BOLDCRLF + "%s" + BoldDefs.BOLDCRLF + "Using deduce method: %s";
    [Delphi(PortStatus.Ported)] public const string boeOperationNotOclable = "%d:Operation %s is not OCLCompatible";
    [Delphi(PortStatus.Ported)] public const string borteAtIndexOutOfBounds = "%d:Tried to take element #%d of a list with %d elements";
    [Delphi(PortStatus.Ported)] public const string borteDivisionByZero = "%d:Division By Zero";
    [Delphi(PortStatus.Ported)] public const string borteInvalidCast = "%d:Invalid OCL-cast, tried to cast a %s to a %s";
    [Delphi(PortStatus.Ported)] public const string borteOperationNotDefinedOnArgTypes = "%d:%s %s %s is not defined";
    [Delphi(PortStatus.Ported)] public const string borteNonClassAttribute = "%d:Can not take attributes of a non-class (%s)";
    [Delphi(PortStatus.Ported)] public const string bortePadStringEmpty = "%d:Trying to pad with an empty string doesn't really get me anywhere...";
    [Delphi(PortStatus.Ported)] public const string boeArgrtIsNotList = "%d:Argument of '%s' is not a list";
    [Delphi(PortStatus.Ported)] public const string boeArgrtIsNottype = "%d:Argument of '%s' is not a type";
    [Delphi(PortStatus.Ported)] public const string boertRangeNotAssigned = "%d:Invalid value of range";

    /// <summary>The position before the first colon of <paramref name="Message"/>, or 0 when there is none or it is not a number.</summary>
    internal static int PositionOf(string Message)
    {
        int colon = Message.IndexOf(':');
        return SysUtils.StrToIntDef(colon >= 0 ? Message.Substring(0, colon) : "", 0);
    }
}

/// <summary>Stops an OCL operation quietly; carries the position in the expression and the expression.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldOCLAbort(string? message = null, Exception? innerException = null) : EAbort(message, innerException)
{
    public int Position;
    public string Ocl = "";
    public bool ErrorFixed;

    /// <summary>Takes the position from the start of the message ("12:..."), once.</summary>
    [Delphi(PortStatus.Ported)]
    public void FixError()
    {
        if (!ErrorFixed)
        {
            Position = BoldOclError.PositionOf(Message);
        }

        ErrorFixed = true;
    }

    /// <summary>A caret under the error position.</summary>
    [Delphi(PortStatus.Ported)]
    public string ErrorPointer() => SysUtils.StringOfChar(' ', Position) + "^";
}

/// <summary>An error in an OCL expression; carries the position in the expression and the expression.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldOCLError(string? message = null, Exception? innerException = null) : EBold(message, innerException)
{
    public int Position;
    public string Ocl = "";
    public bool ErrorFixed;

    /// <summary>Takes the position from the start of the message ("12:..."), once.</summary>
    [Delphi(PortStatus.Ported)]
    public void FixError()
    {
        if (!ErrorFixed)
        {
            Position = BoldOclError.PositionOf(Message);
        }

        ErrorFixed = true;
    }

    /// <summary>A caret under the error position.</summary>
    [Delphi(PortStatus.Ported)]
    public string ErrorPointer() => SysUtils.StringOfChar(' ', Position) + "^";
}

[Delphi(PortStatus.Ported)]
public class EBoldOCLInternalError(string? message = null, Exception? innerException = null) : EBoldOCLError(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldOclRunTimeError(string? message = null, Exception? innerException = null) : EBoldOCLError(message, innerException);
