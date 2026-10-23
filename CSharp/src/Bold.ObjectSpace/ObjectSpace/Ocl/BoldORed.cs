// Delphi: Source/ObjectSpace/Ocl/BoldORed.pas
// Port map: CSharp/port/units/BoldORed.md
// The OCL lexer and parser classes for the generated tables, and the reductions that build the syntax tree.

using System.Collections;

namespace Bold;

/// <summary>The token and production numbers of the generated OCL tables, and the tree-building routines.</summary>
public static partial class BoldORed
{
    [Delphi(PortStatus.Ported)] public const int ALexExpressionListMain = 0;
    [Delphi(PortStatus.Ported)] public const int ALexExpressionListStringLiteral = 1;
    [Delphi(PortStatus.Ported)] public const int ALexINTEGER = 2;
    [Delphi(PortStatus.Ported)] public const int ALexFLOAT = 3;
    [Delphi(PortStatus.Ported)] public const int ALexNAME = 4;
    [Delphi(PortStatus.Ported)] public const int ALexTYPENAME = 5;
    [Delphi(PortStatus.Ported)] public const int ALexDATE = 6;
    [Delphi(PortStatus.Ported)] public const int ALexTIME = 7;
    [Delphi(PortStatus.Ported)] public const int ALexStringSt = 8;
    [Delphi(PortStatus.Ported)] public const int ALexLBrack = 9;
    [Delphi(PortStatus.Ported)] public const int ALexRBrack = 10;
    [Delphi(PortStatus.Ported)] public const int ALexLParen = 11;
    [Delphi(PortStatus.Ported)] public const int ALexRParen = 12;
    [Delphi(PortStatus.Ported)] public const int ALexLBrace = 13;
    [Delphi(PortStatus.Ported)] public const int ALexRBrace = 14;
    [Delphi(PortStatus.Ported)] public const int ALexGreater = 15;
    [Delphi(PortStatus.Ported)] public const int ALexLess = 16;
    [Delphi(PortStatus.Ported)] public const int ALexEqual = 17;
    [Delphi(PortStatus.Ported)] public const int ALexGTEqual = 18;
    [Delphi(PortStatus.Ported)] public const int ALexLTEqual = 19;
    [Delphi(PortStatus.Ported)] public const int ALexNEqual = 20;
    [Delphi(PortStatus.Ported)] public const int ALexPlus = 21;
    [Delphi(PortStatus.Ported)] public const int ALexMinus = 22;
    [Delphi(PortStatus.Ported)] public const int ALexMult = 23;
    [Delphi(PortStatus.Ported)] public const int ALexDiv = 24;
    [Delphi(PortStatus.Ported)] public const int ALexDot = 25;
    [Delphi(PortStatus.Ported)] public const int ALexArrow = 26;
    [Delphi(PortStatus.Ported)] public const int ALexComma = 27;
    [Delphi(PortStatus.Ported)] public const int ALexCommercialAt = 28;
    [Delphi(PortStatus.Ported)] public const int ALexIF = 29;
    [Delphi(PortStatus.Ported)] public const int ALexTHEN = 30;
    [Delphi(PortStatus.Ported)] public const int ALexELSE = 31;
    [Delphi(PortStatus.Ported)] public const int ALexENDIF = 32;
    [Delphi(PortStatus.Ported)] public const int ALexENUM = 33;
    [Delphi(PortStatus.Ported)] public const int ALexHASH = 34;
    [Delphi(PortStatus.Ported)] public const int ALexBAR = 35;
    [Delphi(PortStatus.Ported)] public const int ALexDOUBLECOLON = 36;
    [Delphi(PortStatus.Ported)] public const int ALexDOUBLEDOT = 37;
    [Delphi(PortStatus.Ported)] public const int ALexCOLON = 38;
    [Delphi(PortStatus.Ported)] public const int ALexBAG = 39;
    [Delphi(PortStatus.Ported)] public const int ALexSET = 40;
    [Delphi(PortStatus.Ported)] public const int ALexSEQUENCE = 41;
    [Delphi(PortStatus.Ported)] public const int ALexCOLLECTION = 42;
    [Delphi(PortStatus.Ported)] public const int ALexIntDIV = 43;
    [Delphi(PortStatus.Ported)] public const int ALexMOD = 44;
    [Delphi(PortStatus.Ported)] public const int ALexAND = 45;
    [Delphi(PortStatus.Ported)] public const int ALexOR = 46;
    [Delphi(PortStatus.Ported)] public const int ALexNOT = 47;
    [Delphi(PortStatus.Ported)] public const int ALexXOR = 48;
    [Delphi(PortStatus.Ported)] public const int ALexIMPLIES = 49;
    [Delphi(PortStatus.Ported)] public const int ALexString = 50;
    [Delphi(PortStatus.Ported)] public const int ALexStringEnd = 51;

    [Delphi(PortStatus.Ported)] public const int AYaccGoal = 1;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionPost = 2;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionAnd = 3;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionOr = 4;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionXor = 5;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionImp = 6;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionEQ = 7;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionLT = 8;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionGT = 9;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionLE = 10;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionGE = 11;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionNE = 12;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionAdd = 13;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionSub = 14;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionDiv = 15;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionMul = 16;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionDivI = 17;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionMod = 18;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionNeg = 19;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionNOT = 20;
    [Delphi(PortStatus.Ported)] public const int AYaccifExpression = 21;
    [Delphi(PortStatus.Ported)] public const int AYaccpostFixFirst = 22;
    [Delphi(PortStatus.Ported)] public const int AYaccpostFixMember = 23;
    [Delphi(PortStatus.Ported)] public const int AYaccpostFixMethod = 24;
    [Delphi(PortStatus.Ported)] public const int AYaccpostFixOperation = 25;
    [Delphi(PortStatus.Ported)] public const int AYaccpostFixOperationParams = 26;
    [Delphi(PortStatus.Ported)] public const int AYaccprimaryExpLittCollection = 27;
    [Delphi(PortStatus.Ported)] public const int AYaccprimaryExpLitt = 28;
    [Delphi(PortStatus.Ported)] public const int AYaccprimaryExpParen = 29;
    [Delphi(PortStatus.Ported)] public const int AYaccprimaryExpIf = 30;
    [Delphi(PortStatus.Ported)] public const int AYaccPrimaryNoParams = 31;
    [Delphi(PortStatus.Ported)] public const int AYaccPrimaryWithParams = 32;
    [Delphi(PortStatus.Ported)] public const int AYaccfeatureCallParameters1 = 33;
    [Delphi(PortStatus.Ported)] public const int AYaccfeatureCallParameters2 = 34;
    [Delphi(PortStatus.Ported)] public const int AYaccfeatureCallParameters3 = 35;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_String = 36;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_EmptyString = 37;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_Number = 38;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_float = 39;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_Date = 40;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_Time = 41;
    [Delphi(PortStatus.Ported)] public const int AYaccliteral_Name = 42;
    [Delphi(PortStatus.Ported)] public const int AYaccsimpleTypeSpecifier_Ety = 43;
    [Delphi(PortStatus.Ported)] public const int AYaccsimpleTypeSpecifier_Path = 44;
    [Delphi(PortStatus.Ported)] public const int AYaccsimpleTypeSpecifier_Enum = 45;
    [Delphi(PortStatus.Ported)] public const int AYaccliteralCollection = 46;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionListOrRange_List = 47;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionList = 48;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionList_Rec = 49;
    [Delphi(PortStatus.Ported)] public const int AYaccexpressionListOrRange_Range = 50;
    [Delphi(PortStatus.Ported)] public const int AYaccenumerationType = 51;
    [Delphi(PortStatus.Ported)] public const int AYaccenumTypeList = 52;
    [Delphi(PortStatus.Ported)] public const int AYaccenumTypeList_Rec = 53;
    [Delphi(PortStatus.Ported)] public const int AYaccqualifiers_Ety = 54;
    [Delphi(PortStatus.Ported)] public const int AYaccqualifiers = 55;
    [Delphi(PortStatus.Ported)] public const int AYaccpathTypeName = 56;
    [Delphi(PortStatus.Ported)] public const int AYacctypeNameList = 57;
    [Delphi(PortStatus.Ported)] public const int AYacctypeNameList_Rec = 58;
    [Delphi(PortStatus.Ported)] public const int AYaccpathName = 59;
    [Delphi(PortStatus.Ported)] public const int AYaccpathName_Rec = 60;
    [Delphi(PortStatus.Ported)] public const int AYaccanyName_Type = 61;
    [Delphi(PortStatus.Ported)] public const int AYaccanyName_Name = 62;
    [Delphi(PortStatus.Ported)] public const int AYacctimeExpression = 63;
    [Delphi(PortStatus.Ported)] public const int AYacctimeExpression_Ety = 64;
    [Delphi(PortStatus.Ported)] public const int AYaccactualParameterList_ety = 65;
    [Delphi(PortStatus.Ported)] public const int AYaccActualParameterlist_nonEty = 66;
    [Delphi(PortStatus.Ported)] public const int AYaccNonEmptyactualParameterList = 67;
    [Delphi(PortStatus.Ported)] public const int AYaccNonEmptyactualParameterList_Rec = 68;
    [Delphi(PortStatus.Ported)] public const int AYacccollectionKind_Set = 69;
    [Delphi(PortStatus.Ported)] public const int AYacccollectionKind_Bag = 70;
    [Delphi(PortStatus.Ported)] public const int AYacccollectionKind_Sequence = 71;
    [Delphi(PortStatus.Ported)] public const int AYacccollectionKind_Collection = 72;

    private static AYaccStackElement El(object? o) => (AYaccStackElement)o!;

    /// <summary>A list element holding a copy of the element.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeList(AYaccStackElement First, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.List = [First.Clone()];
        return result;
    }

    /// <summary>The list with a copy of the element put first.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement PushList(AYaccStackElement First, AYaccStackElement List, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.List = List.List;
        result.List!.Insert(0, First.Clone());
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeEmptyList()
    {
        var result = AYaccStackElement.CreateOffs(0);
        result.List = [];
        return result;
    }

    /// <summary>Drops the list's (cloned) elements once their contents have been taken.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void ReleaseList(AYaccStackElement List) => List.List = null;

    /// <summary>Adds the nodes of the parse list to the node list (creating it), and releases the parse list.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void RetrieveNodeList(AYaccStackElement? ParseList, ref TBoldOclNodeList? NodeList)
    {
        if (ParseList?.List is null)
        {
            return;
        }

        NodeList ??= new TBoldOclNodeList();
        foreach (object? item in ParseList.List)
        {
            NodeList.Add(El(item).Node);
        }

        ReleaseList(ParseList);
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement Make1Operation(string OpName, AYaccStackElement arg1, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(arg1.Offset);
        var op = new TBoldOclOperation { Position = Offs, Args = new TBoldOclNodeList(), OperationName = OpName };
        op.Args.Add(arg1.Node);
        result.Node = op;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement Make2Operation(string OpName, AYaccStackElement arg1, AYaccStackElement arg2, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var op = new TBoldOclOperation { Position = Offs, Args = new TBoldOclNodeList(), OperationName = OpName };
        op.Args.Add(arg1.Node);
        op.Args.Add(arg2.Node);
        result.Node = op;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement Make3Operation(string OpName, AYaccStackElement arg1, AYaccStackElement arg2, AYaccStackElement arg3, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var op = new TBoldOclOperation { Position = Offs, Args = new TBoldOclNodeList(), OperationName = OpName };
        op.Args.Add(arg1.Node);
        op.Args.Add(arg2.Node);
        op.Args.Add(arg3.Node);
        result.Node = op;
        return result;
    }

    /// <summary>Unary minus as the operation "unary-".</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeUnaryMinus(string OpName, AYaccStackElement arg1, int Offs) => Make1Operation("unary-", arg1, Offs);

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeCollectionKind(TBOCollectionKind Kind, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.CollectionKind = Kind;
        result.Node = new TBoldOclCollectionLiteral { Position = Offs };
        return result;
    }

    /// <summary>Raises: time expressions (@name) are not implemented.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeTimeExpression(AYaccStackElement Kind, int Offs) =>
        throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeTimeExpressionsUnIMpl, Offs));

    /// <summary>A name; one starting with an upper-case letter is taken as a type name.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeName(AYaccStackElement Name, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.Name = BoldSharedStrings.BoldSharedStringManager().GetSharedString(Name.Lexeme!.Buffer);
        if (result.Name[0] is >= 'A' and <= 'Z')
        {
            result.nameIstype = true;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeDeclarator(AYaccStackElement? VarName, AYaccStackElement? VarType, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        if (VarName is not null)
        {
            result.Name = BoldSharedStrings.BoldSharedStringManager().GetSharedString(VarName.Lexeme!.Buffer);
        }

        if (VarType is not null)
        {
            result.DeclaratorTypeList = VarType.List;
            result.SimpleTypeSpecifierIsEnum = VarType.SimpleTypeSpecifierIsEnum;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeRange(AYaccStackElement start, AYaccStackElement Stop, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.start = start.Node;
        result.stop = Stop.Node;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeLiteralCollection(AYaccStackElement CollectionType, AYaccStackElement ExprListOrRange, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var LiteralCollection = new TBoldOclCollectionLiteral { CollectionKind = CollectionType.CollectionKind, Position = Offs };
        if (ExprListOrRange.start is not null)
        {
            LiteralCollection.IsRange = true;
            LiteralCollection.RangeStart = ExprListOrRange.start;
            LiteralCollection.RangeStop = ExprListOrRange.stop;
        }
        else
        {
            LiteralCollection.IsRange = false;
            LiteralCollection.Elements = new TBoldOclNodeList();
            RetrieveNodeList(ExprListOrRange, ref LiteralCollection.Elements);
        }

        result.Node = LiteralCollection;
        return result;
    }

    /// <summary>Raises: user-specified enumerations are not implemented.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeSimpleEnumType(AYaccStackElement Enumeration, int Offs) =>
        throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeUserSpecifiedEnumsUnImpl, Offs));

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeSimplePath(AYaccStackElement Path, int Offs)
    {
        if (Path.List!.Count != 1)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boePackageSpecUnImpl, Offs));
        }

        var result = AYaccStackElement.CreateOffs(Offs);
        result.SimpleTypeSpecifierIsEnum = false;
        result.List = Path.List;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeLiteralName(AYaccStackElement Name, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        result.Node = new TBoldOclEnumLiteral { Position = Offs, name = Name.Name };
        return result;
    }

    /// <summary>A string literal; \' and \\ are unescaped.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeLiteralString(AYaccStackElement? Str, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var node = new TBoldOclStrLiteral();
        if (Str is not null)
        {
            string s = Str.Lexeme!.Buffer;
            s = s.Replace("\\'", "'", StringComparison.Ordinal);
            s = s.Replace("\\\\", "\\", StringComparison.Ordinal);
            node.StrValue = BoldSharedStrings.BoldSharedStringManager().GetSharedString(s);
        }
        else
        {
            node.StrValue = "";
        }

        result.Node = node;
        node.Position = Offs;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeLiteralInt(AYaccStackElement Int, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var node = new TBoldOclIntLiteral();
        try
        {
            node.IntValue = SysUtils.StrToInt(Int.Lexeme!.Buffer);
        }
        catch (EConvertError)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeConversionError, Offs, Int.Lexeme!.Buffer, "integer"));
        }

        result.Node = node;
        node.Position = Offs;
        return result;
    }

    /// <summary>A date literal #yyyy-mm-dd.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeLiteralDate(AYaccStackElement Date, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var node = new TBoldOclDateLiteral();
        string buffer = Date.Lexeme!.Buffer;
        int y = SysUtils.StrToInt(SysUtils.Copy(buffer, 2, 4));
        int m = SysUtils.StrToInt(SysUtils.Copy(buffer, 7, 2));
        int d = SysUtils.StrToInt(SysUtils.Copy(buffer, 10, 2));
        try
        {
            node.DateValue = SysUtils.EncodeDate(y, m, d);
        }
        catch (EConvertError)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeConversionError, Offs, buffer, "Date"));
        }

        result.Node = node;
        node.Position = Offs;
        return result;
    }

    /// <summary>A time literal #hh:mm[:ss].</summary>
    [Delphi(PortStatus.Ported, Note = "Makes a time literal; Delphi made a date literal, whose value (the date part) was always 0, so a time literal never held its time.")]
    internal static AYaccStackElement MakeLiteralTime(AYaccStackElement Time, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var node = new TBoldOclTimeLiteral();
        string buffer = Time.Lexeme!.Buffer;
        int h = SysUtils.StrToInt(SysUtils.Copy(buffer, 2, 2));
        int m = SysUtils.StrToInt(SysUtils.Copy(buffer, 5, 2));
        int s = SysUtils.StrToIntDef(SysUtils.Copy(buffer, 8, 2), 0);
        try
        {
            node.TimeValue = SysUtils.EncodeTime(h, m, s, 0);
        }
        catch (EConvertError)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeConversionError, Offs, buffer, "Time"));
        }

        result.Node = node;
        node.Position = Offs;
        return result;
    }

    [Delphi(PortStatus.Ported, Note = "Parses with '.' as the decimal separator; Delphi swaps in the locale's separator first.")]
    internal static AYaccStackElement MakeLiteralfloat(AYaccStackElement Float, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var node = new TBoldOclNumericLiteral();
        try
        {
            node.FloatValue = SysUtils.StrToFloat(Float.Lexeme!.Buffer);
        }
        catch (EConvertError)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeConversionError, Offs, Float.Lexeme!.Buffer, "real"));
        }

        result.Node = node;
        node.Position = Offs;
        return result;
    }

    /// <summary>The parameters of a call, with the loop variable's name and type when declared.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeFeatureCall(AYaccStackElement? Declaratorname, AYaccStackElement? DeclaratorType, AYaccStackElement? paramList, int Offs)
    {
        AYaccStackElement? Declarator = Declaratorname is not null ? MakeDeclarator(Declaratorname, DeclaratorType, Declaratorname.Offset) : null;
        var result = AYaccStackElement.CreateOffs(Offs);
        if (Declarator is not null)
        {
            result.Name = Declarator.Name;
            result.DeclaratorTypeList = Declarator.DeclaratorTypeList;
            result.SimpleTypeSpecifierIsEnum = Declarator.SimpleTypeSpecifierIsEnum;
        }

        if (paramList is not null)
        {
            result.List = paramList.List;
        }

        return result;
    }

    /// <summary>A member of the context, or (without a context) a type name when the name starts with upper case.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeMember(AYaccStackElement? Context, AYaccStackElement Name, AYaccStackElement Time, AYaccStackElement Qual, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        if (Context is null && El(Name.List![0]).nameIstype)
        {
            if (Name.List.Count != 1)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidTypeNameName, Offs));
            }

            result.Node = new TBoldOclTypeNode { Position = Offs, typeName = El(Name.List[0]).Name };
            ReleaseList(Name);
        }
        else
        {
            var member = new TBoldOclMember { Position = Offs };
            result.Node = member;
            RetrieveNodeList(Qual, ref member.Qualifier);
            if (Name.List!.Count != 1)
            {
                ReleaseList(Name);
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidMemberName, Offs));
            }

            member.MemberName = El(Name.List[0]).Name;
            ReleaseList(Name);
            if (Context is not null)
            {
                member.MemberOf = Context.Node;
            }
        }

        return result;
    }

    /// <summary>A method call; qualifiers and time specifiers are not allowed.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeMethod(AYaccStackElement? Context, AYaccStackElement Name, AYaccStackElement Time, AYaccStackElement Qual,
        AYaccStackElement Params, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        var method = new TBoldOclMethod { Position = Offs };
        result.Node = method;
        if (Qual.List is not null)
        {
            Qual.List = null;
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidQualifier, Offs));
        }

        if (Time.Name.Length != 0)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidTimeSpecifier, Offs));
        }

        if (Name.List!.Count != 1)
        {
            ReleaseList(Name);
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidMethodName, Offs));
        }

        method.OperationName = El(Name.List[0]).Name;
        method.IsMethod = true;
        ReleaseList(Name);
        if (Context is not null)
        {
            method.MethodOf = Context.Node;
        }

        RetrieveNodeList(Params, ref method.Args);
        return result;
    }

    /// <summary>The loop variable of an iteration, with the type names given in its declaration.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldOclVariableBinding MakeLoopVar(AYaccStackElement? Params)
    {
        var result = new TBoldOclVariableBinding { TypeNameList = new TStringList() };
        if (Params is not null)
        {
            result.Position = Params.Offset;
            result.VariableName = Params.Name;
            if (Params.DeclaratorTypeList is not null)
            {
                foreach (object? item in Params.DeclaratorTypeList)
                {
                    result.TypeNameList.Add(El(item).Name);
                }

                Params.DeclaratorTypeList = null;
            }
        }

        return result;
    }

    /// <summary>An operation (->) on the context; select, collect and the other iterator names make an iteration with a loop variable.</summary>
    [Delphi(PortStatus.Ported)]
    internal static AYaccStackElement MakeOperation(AYaccStackElement? Context, AYaccStackElement Name, AYaccStackElement Time, AYaccStackElement Qual,
        AYaccStackElement? Params, int Offs)
    {
        var result = AYaccStackElement.CreateOffs(Offs);
        string upcaseName = SysUtils.UpperCase(El(Name.List![0]).Name);
        TBoldOCLIteratorSpecifier IteratorSpecifier = upcaseName switch
        {
            "SELECT" => TBoldOCLIteratorSpecifier.OclSelect,
            "REJECT" => TBoldOCLIteratorSpecifier.OclReject,
            "COLLECT" => TBoldOCLIteratorSpecifier.OCLCollect,
            "FORALL" => TBoldOCLIteratorSpecifier.OclForAll,
            "EXISTS" => TBoldOCLIteratorSpecifier.OclExists,
            "ITERATE" => TBoldOCLIteratorSpecifier.OclIterate,
            "ORDERBY" => TBoldOCLIteratorSpecifier.OclOrderBy,
            "ORDERDESCENDING" => TBoldOCLIteratorSpecifier.OclOrderDescending,
            "UNIQUE" => TBoldOCLIteratorSpecifier.OclUnique,
            _ => TBoldOCLIteratorSpecifier.OclNoIterator,
        };

        TBoldOclOperation op;
        if (IteratorSpecifier != TBoldOCLIteratorSpecifier.OclNoIterator)
        {
            op = new TBoldOclIteration { LoopVar = MakeLoopVar(Params), IteratorSpecifier = IteratorSpecifier };
        }
        else
        {
            op = new TBoldOclOperation { IteratorSpecifier = IteratorSpecifier };
            if (Params is not null && Params.Name.Length != 0)
            {
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidLoopVariable, Offs, El(Name.List[0]).Name));
            }
        }

        result.Node = op;
        op.Position = Offs;
        if (Qual.List is not null)
        {
            Qual.List = null;
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidQualifier, Offs));
        }

        if (Time.Name.Length != 0)
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidTimeSpecifier, Offs));
        }

        if (Name.List.Count != 1)
        {
            ReleaseList(Name);
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeInvalidMemberName, Offs));
        }

        op.OperationName = El(Name.List[0]).Name;
        ReleaseList(Name);
        op.Args = new TBoldOclNodeList();
        if (Context is not null)
        {
            op.Args.Add(Context.Node);
        }
        else
        {
            throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeOperationWithOutContext, Offs));
        }

        RetrieveNodeList(Params, ref op.Args);
        return result;
    }
}

/// <summary>Collection kinds of the parser (only used in commented-out code in Delphi).</summary>
[Delphi(PortStatus.Ported)]
internal enum TBoldOclCollectionType
{
    tboNoCollection,
    tboBag,
    tboSet,
    tboSequence,
    tboCollection,
    tboCopyArg1,
    tboMinCollection,
}

/// <summary>The OCL lexer: the table engine with the token names.</summary>
[Delphi(PortStatus.Ported)]
public class ALexClass(SSLexConsumer TheConsumer, SSLexTable TheTable) : SSLex(TheConsumer, TheTable)
{
    [Delphi(PortStatus.Ported)]
    public override string TokenToString(int TheToken) => TheToken switch
    {
        BoldORed.ALexINTEGER => "INTEGER",
        BoldORed.ALexFLOAT => "FLOAT",
        BoldORed.ALexNAME => "NAME",
        BoldORed.ALexTYPENAME => "TYPENAME",
        BoldORed.ALexDATE => "DATE",
        BoldORed.ALexTIME => "TIME",
        BoldORed.ALexStringSt => "STRINGstart",
        BoldORed.ALexLBrack => "[",
        BoldORed.ALexRBrack => "]",
        BoldORed.ALexLParen => "(",
        BoldORed.ALexRParen => ")",
        BoldORed.ALexLBrace => "{",
        BoldORed.ALexRBrace => "}",
        BoldORed.ALexGreater => ">",
        BoldORed.ALexLess => "<",
        BoldORed.ALexEqual => "=",
        BoldORed.ALexGTEqual => ">=",
        BoldORed.ALexLTEqual => "<=",
        BoldORed.ALexNEqual => "<>",
        BoldORed.ALexPlus => "+",
        BoldORed.ALexMinus => "-",
        BoldORed.ALexMult => "*",
        BoldORed.ALexDiv => "/",
        BoldORed.ALexDot => ".",
        BoldORed.ALexArrow => "->",
        BoldORed.ALexComma => ",",
        BoldORed.ALexCommercialAt => "@",
        BoldORed.ALexIF => "if",
        BoldORed.ALexTHEN => "then",
        BoldORed.ALexELSE => "else",
        BoldORed.ALexENDIF => "endif",
        BoldORed.ALexENUM => "enum",
        BoldORed.ALexHASH => "#",
        BoldORed.ALexBAR => "|",
        BoldORed.ALexDOUBLECOLON => "::",
        BoldORed.ALexDOUBLEDOT => "..",
        BoldORed.ALexCOLON => ":",
        BoldORed.ALexBAG => "Bag",
        BoldORed.ALexSET => "Set",
        BoldORed.ALexSEQUENCE => "Sequence",
        BoldORed.ALexCOLLECTION => "Collection",
        BoldORed.ALexIntDIV => "IntDIV",
        BoldORed.ALexMOD => "MOD",
        BoldORed.ALexAND => "AND",
        BoldORed.ALexOR => "OR",
        BoldORed.ALexNOT => "NOT",
        BoldORed.ALexXOR => "XOR",
        BoldORed.ALexIMPLIES => "IMPLIES",
        BoldORed.ALexString => "STRINGliteral",
        BoldORed.ALexStringEnd => "STRINGend",
        -2 => "%error",
        -1 => "eof",
        _ => "Token Not Found",
    };
}

/// <summary>A parse stack element of the OCL parser: the node built so far, or a name, list or range being collected.</summary>
[Delphi(PortStatus.Ported)]
public class AYaccStackElement : SSYaccStackElement
{
    public TBoldOclNode? start;
    public TBoldOclNode? stop;
    public TBoldOclNode? Node;
    public string Name = "";
    public bool SimpleTypeSpecifierIsEnum;
    public ArrayList? DeclaratorTypeList;
    public ArrayList? List;
    public int Offset;
    public TBOCollectionKind CollectionKind;
    public bool nameIstype;

    [Delphi(PortStatus.Ported)]
    public AYaccStackElement()
    {
        Offset = -1;
    }

    [Delphi(PortStatus.Ported)]
    public static AYaccStackElement CreateOffs(int offs) => new() { Offset = offs };

    /// <summary>A copy sharing the node and lists.</summary>
    [Delphi(PortStatus.Ported)]
    public AYaccStackElement Clone() => new()
    {
        Node = Node,
        List = List,
        start = start,
        stop = stop,
        Name = Name,
        nameIstype = nameIstype,
        SimpleTypeSpecifierIsEnum = SimpleTypeSpecifierIsEnum,
        DeclaratorTypeList = DeclaratorTypeList,
    };

    /// <summary>Frees the node and the nodes of the listed elements (after a failed parse).</summary>
    [Delphi(PortStatus.Ported)]
    public void FreeAllOwnedElements()
    {
        Node?.Dispose();
        Node = null;
        if (List is not null)
        {
            foreach (object? item in List)
            {
                ((AYaccStackElement)item!).FreeAllOwnedElements();
            }

            List = null;
        }
    }
}

/// <summary>The OCL parser: the table engine with the reductions that build the syntax tree.</summary>
[Delphi(PortStatus.Ported)]
public class AYaccClass(SSLex TheLexer, SSYaccTable TheTable) : SSYacc(TheLexer, TheTable)
{
    public AYaccStackElement? finalValue;

    [Delphi(PortStatus.Ported)]
    public override SSYaccStackElement StackElement() => new AYaccStackElement();

    /// <summary>Builds the tree element for the production from the stack elements of its right side.</summary>
    [Delphi(PortStatus.Ported)]
    public override SSYaccStackElement? Reduce(int TheProduction, int TheSize)
    {
        AYaccStackElement Stack(int i) => (AYaccStackElement)ElementFromProduction(i);
        int LexOffset(int i) => Stack(i).Lexeme!.Offset;

        switch (TheProduction)
        {
            case BoldORed.AYaccGoal:
                finalValue = Stack(0);
                return null;
            case BoldORed.AYaccexpressionPost: return Stack(0);
            case BoldORed.AYaccexpressionAnd: return BoldORed.Make2Operation("and", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionOr: return BoldORed.Make2Operation("or", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionXor: return BoldORed.Make2Operation("xor", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionImp: return BoldORed.Make2Operation("implies", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionEQ: return BoldORed.Make2Operation("=", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionLT: return BoldORed.Make2Operation("<", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionGT: return BoldORed.Make2Operation(">", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionLE: return BoldORed.Make2Operation("<=", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionGE: return BoldORed.Make2Operation(">=", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionNE: return BoldORed.Make2Operation("<>", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionAdd: return BoldORed.Make2Operation("+", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionSub: return BoldORed.Make2Operation("-", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionDiv: return BoldORed.Make2Operation("/", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionMul: return BoldORed.Make2Operation("*", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionNeg: return BoldORed.MakeUnaryMinus("-", Stack(1), LexOffset(0));
            case BoldORed.AYaccexpressionNOT: return BoldORed.Make1Operation("not", Stack(1), LexOffset(0));
            case BoldORed.AYaccifExpression: return BoldORed.Make3Operation("if", Stack(1), Stack(3), Stack(5), LexOffset(0));
            case BoldORed.AYaccpostFixFirst: return Stack(0);
            case BoldORed.AYaccpostFixMember: return BoldORed.MakeMember(Stack(0), Stack(2), Stack(3), Stack(4), Stack(2).Offset);
            case BoldORed.AYaccpostFixMethod: return BoldORed.MakeMethod(Stack(0), Stack(2), Stack(3), Stack(4), Stack(5), Stack(2).Offset);
            case BoldORed.AYaccpostFixOperation: return BoldORed.MakeOperation(Stack(0), Stack(2), Stack(3), Stack(4), null, Stack(2).Offset);
            case BoldORed.AYaccpostFixOperationParams: return BoldORed.MakeOperation(Stack(0), Stack(2), Stack(3), Stack(4), Stack(5), Stack(2).Offset);
            case BoldORed.AYaccprimaryExpLittCollection: return Stack(0);
            case BoldORed.AYaccprimaryExpLitt: return Stack(0);
            case BoldORed.AYaccprimaryExpParen: return Stack(1);
            case BoldORed.AYaccprimaryExpIf: return Stack(0);
            case BoldORed.AYaccPrimaryNoParams: return BoldORed.MakeMember(null, Stack(0), Stack(1), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccPrimaryWithParams: return BoldORed.MakeMethod(null, Stack(0), Stack(1), Stack(2), Stack(3), Stack(0).Offset);
            case BoldORed.AYaccliteral_String: return BoldORed.MakeLiteralString(Stack(1), LexOffset(0));
            case BoldORed.AYaccliteral_Number: return BoldORed.MakeLiteralInt(Stack(0), LexOffset(0));
            case BoldORed.AYaccliteral_Name: return BoldORed.MakeLiteralName(Stack(1), Stack(1).Offset);
            case BoldORed.AYaccsimpleTypeSpecifier_Ety: return StackElement();
            case BoldORed.AYaccsimpleTypeSpecifier_Path: return BoldORed.MakeSimplePath(Stack(0), Stack(0).Offset);
            case BoldORed.AYaccsimpleTypeSpecifier_Enum: return BoldORed.MakeSimpleEnumType(Stack(0), Stack(0).Offset);
            case BoldORed.AYaccliteralCollection: return BoldORed.MakeLiteralCollection(Stack(0), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccexpressionListOrRange_List: return Stack(0);
            case BoldORed.AYaccexpressionList: return BoldORed.MakeList(Stack(0), Stack(0).Offset);
            case BoldORed.AYaccexpressionList_Rec: return BoldORed.PushList(Stack(0), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccexpressionListOrRange_Range: return BoldORed.MakeRange(Stack(0), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccenumerationType: return Stack(2);
            case BoldORed.AYaccenumTypeList: return BoldORed.MakeList(Stack(1), LexOffset(1));
            case BoldORed.AYaccenumTypeList_Rec: return BoldORed.PushList(Stack(1), Stack(3), LexOffset(1));
            case BoldORed.AYaccqualifiers_Ety: return StackElement();
            case BoldORed.AYaccqualifiers: return Stack(1);
            case BoldORed.AYaccpathTypeName: return Stack(0);
            case BoldORed.AYacctypeNameList: return BoldORed.MakeList(Stack(0), LexOffset(0));
            case BoldORed.AYacctypeNameList_Rec: return BoldORed.PushList(Stack(0), Stack(2), LexOffset(0));
            case BoldORed.AYaccpathName: return BoldORed.MakeList(Stack(0), Stack(0).Offset);
            case BoldORed.AYaccpathName_Rec: return BoldORed.PushList(Stack(0), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccanyName_Type: return BoldORed.MakeName(Stack(0), LexOffset(0));
            case BoldORed.AYaccanyName_Name: return BoldORed.MakeName(Stack(0), LexOffset(0));
            case BoldORed.AYacctimeExpression: return BoldORed.MakeTimeExpression(Stack(1), Stack(1).Offset);
            case BoldORed.AYacccollectionKind_Set: return BoldORed.MakeCollectionKind(TBOCollectionKind.OCLSet, LexOffset(0));
            case BoldORed.AYacccollectionKind_Bag: return BoldORed.MakeCollectionKind(TBOCollectionKind.OCLBag, LexOffset(0));
            case BoldORed.AYacccollectionKind_Sequence: return BoldORed.MakeCollectionKind(TBOCollectionKind.OCLSequence, LexOffset(0));
            case BoldORed.AYacccollectionKind_Collection:
                throw new EBoldOCLAbort(SysUtils.Format(BoldOclError.boeCollectionNotValidLiteral, LexOffset(0)));
            case BoldORed.AYacctimeExpression_Ety: return StackElement();
            case BoldORed.AYaccliteral_float: return BoldORed.MakeLiteralfloat(Stack(0), LexOffset(0));
            case BoldORed.AYaccexpressionDivI: return BoldORed.Make2Operation("div", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccexpressionMod: return BoldORed.Make2Operation("mod", Stack(0), Stack(2), LexOffset(1));
            case BoldORed.AYaccfeatureCallParameters1: return BoldORed.MakeFeatureCall(null, null, Stack(1), Stack(1).Offset);
            case BoldORed.AYaccfeatureCallParameters2: return BoldORed.MakeFeatureCall(Stack(1), null, Stack(3), Stack(3).Offset);
            case BoldORed.AYaccfeatureCallParameters3: return BoldORed.MakeFeatureCall(Stack(1), Stack(3), Stack(5), Stack(5).Offset);
            case BoldORed.AYaccactualParameterList_ety: return BoldORed.MakeEmptyList();
            case BoldORed.AYaccActualParameterlist_nonEty: return Stack(0);
            case BoldORed.AYaccNonEmptyactualParameterList: return BoldORed.MakeList(Stack(0), Stack(0).Offset);
            case BoldORed.AYaccNonEmptyactualParameterList_Rec: return BoldORed.PushList(Stack(0), Stack(2), Stack(0).Offset);
            case BoldORed.AYaccliteral_EmptyString: return BoldORed.MakeLiteralString(null, LexOffset(0));
            case BoldORed.AYaccliteral_Date: return BoldORed.MakeLiteralDate(Stack(0), LexOffset(0));
            case BoldORed.AYaccliteral_Time: return BoldORed.MakeLiteralTime(Stack(0), LexOffset(0));
            default:
                return null;
        }
    }
}
