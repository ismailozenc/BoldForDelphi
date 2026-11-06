// Delphi: Source/UMLModel/ModelLinks/XMI/BoldDTDParser.pas
// Port map: CSharp/port/units/BoldDTDParser.md

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldDTDToken
{
    dtSpace, dtName, dtEntityDecl, dtElementDecl, dtAttlistDecl,
    dtEndDecl, dtOptional, dtZeroOrMore, dtOneOrMore,
    dtChoice, dtComma, dtOpenParen, dtCloseParen,
    dtAnyContent, dtEmptyContent, dtPCDataDecl,
    dtEOF, dtComment, dtStringLiteral, dtPercent, dtFixedDecl, dtImpliedDecl,
    dtRequiredDecl, dtSemicolon,
}

[Delphi(PortStatus.Ported)]
public delegate void TBoldDTDNameEvent(string Name);

[Delphi(PortStatus.Ported)]
public delegate void TBoldDTDEvent();

/// <summary>Splits a DTD into its tokens.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDTDScanner
{
    private static readonly char[] SpaceChars = [' ', '\t', '\r', '\n'];
    private readonly string fDTD;
    private int fPos;
    private string fLastTokenText = "";
    private TBoldDTDToken fLastToken = TBoldDTDToken.dtSpace;

    [Delphi(PortStatus.Ported)]
    public TBoldDTDScanner(string DTD)
    {
        fDTD = DTD;
        fPos = 0;
    }

    private char At(int Pos) => Pos < fDTD.Length ? fDTD[Pos] : '\0';

    private static bool IsNameChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or ':';

    [Delphi(PortStatus.Ported)]
    private bool TryToken(string TokenStr, TBoldDTDToken Token)
    {
        if (string.CompareOrdinal(fDTD, fPos, TokenStr, 0, TokenStr.Length) == 0)
        {
            fLastTokenText = TokenStr;
            fPos += TokenStr.Length;
            fLastToken = Token;
            return true;
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    private void SpaceToken()
    {
        int newpos = fPos;
        while (Array.IndexOf(SpaceChars, At(newpos)) >= 0)
        {
            newpos++;
        }

        fLastTokenText = fDTD[fPos..newpos];
        fPos = newpos;
        fLastToken = TBoldDTDToken.dtSpace;
    }

    [Delphi(PortStatus.Ported)]
    private void DeclarationToken()
    {
        if (!(TryToken("<!ELEMENT", TBoldDTDToken.dtElementDecl) || TryToken("<!ATTLIST", TBoldDTDToken.dtAttlistDecl) ||
              TryToken("<!ENTITY", TBoldDTDToken.dtEntityDecl)))
        {
            throw new EBold($"{GetType().Name}.NextToken: Not a valid token, pos {fPos + 1}");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void NameToken()
    {
        int newpos = fPos;
        while (IsNameChar(At(newpos)))
        {
            newpos++;
        }

        if (newpos == fPos)
        {
            throw new EBold($"{GetType().Name}: Unknown character: {At(newpos)}");
        }

        fLastTokenText = fDTD[fPos..newpos];
        fPos = newpos;
        fLastToken = TBoldDTDToken.dtName;
    }

    [Delphi(PortStatus.Ported)]
    private void OneCharToken(TBoldDTDToken Token)
    {
        fLastTokenText = fDTD.Substring(fPos, 1);
        fPos++;
        fLastToken = Token;
    }

    [Delphi(PortStatus.Ported)]
    private void HashDeclToken()
    {
        if (!(TryToken("#PCDATA", TBoldDTDToken.dtPCDataDecl) || TryToken("#FIXED", TBoldDTDToken.dtFixedDecl) ||
              TryToken("#IMPLIED", TBoldDTDToken.dtImpliedDecl) || TryToken("#REQUIRED", TBoldDTDToken.dtRequiredDecl)))
        {
            throw new EBold($"{GetType().Name}.HashDeclToken: Scanner error");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void EOFToken()
    {
        if (fLastToken == TBoldDTDToken.dtEOF)
        {
            throw new EBold($"{GetType().Name}: Tried to read past eof");
        }

        fLastToken = TBoldDTDToken.dtEOF;
        fLastTokenText = "";
    }

    [Delphi(PortStatus.Ported)]
    private bool TryComment()
    {
        if (string.CompareOrdinal(fDTD, fPos, "<!--", 0, 4) != 0)
        {
            return false;
        }

        int newpos = fPos + 4 + 2;
        while (!(At(newpos) == '>' && At(newpos - 1) == '-' && At(newpos - 2) == '-'))
        {
            if (newpos >= fDTD.Length)
            {
                throw new EBold($"{GetType().Name}: Unterminated comment");
            }

            newpos++;
        }

        newpos++;
        fLastToken = TBoldDTDToken.dtComment;
        fLastTokenText = fDTD[fPos..newpos];
        fPos = newpos;
        return true;
    }

    [Delphi(PortStatus.Ported)]
    private void StringLiteralToken()
    {
        char delimiter = fDTD[fPos];
        int newpos = fPos + 1;
        while (At(newpos) != delimiter)
        {
            if (newpos >= fDTD.Length)
            {
                throw new EBold($"{GetType().Name}: Unterminated string literal");
            }

            newpos++;
        }

        newpos++;
        fLastTokenText = fDTD[fPos..newpos];
        fLastToken = TBoldDTDToken.dtStringLiteral;
        fPos = newpos;
    }

    [Delphi(PortStatus.Ported)]
    public void NextToken()
    {
        switch (At(fPos))
        {
            case '\0':
                EOFToken();
                break;
            case ' ' or '\t' or '\r' or '\n':
                SpaceToken();
                break;
            case '<':
                if (!TryComment())
                {
                    DeclarationToken();
                }

                break;
            case '>':
                OneCharToken(TBoldDTDToken.dtEndDecl);
                break;
            case '?':
                OneCharToken(TBoldDTDToken.dtOptional);
                break;
            case '*':
                OneCharToken(TBoldDTDToken.dtZeroOrMore);
                break;
            case '+':
                OneCharToken(TBoldDTDToken.dtOneOrMore);
                break;
            case '|':
                OneCharToken(TBoldDTDToken.dtChoice);
                break;
            case ',':
                OneCharToken(TBoldDTDToken.dtComma);
                break;
            case '(':
                OneCharToken(TBoldDTDToken.dtOpenParen);
                break;
            case ')':
                OneCharToken(TBoldDTDToken.dtCloseParen);
                break;
            case '%':
                OneCharToken(TBoldDTDToken.dtPercent);
                break;
            case ';':
                OneCharToken(TBoldDTDToken.dtSemicolon);
                break;
            case '#':
                HashDeclToken();
                break;
            case '\'' or '"':
                StringLiteralToken();
                break;
            default:
                if (!(TryToken("ANY", TBoldDTDToken.dtAnyContent) || TryToken("EMPTY", TBoldDTDToken.dtEmptyContent)))
                {
                    NameToken();
                }

                break;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldDTDToken LastToken => fLastToken;
    [Delphi(PortStatus.Ported)] public string LastTokenText => fLastTokenText;
}

/// <summary>
/// Reads a DTD's element declarations, telling its events of each element and the names in its content: enough to
/// know the members of each class of the UML DTD. Attribute lists and entities are skipped; parameter entities are
/// expanded first.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDTDParser
{
    private TBoldDTDScanner fScanner = new("");

    [Delphi(PortStatus.Ported)]
    private void AssertToken(TBoldDTDToken Token)
    {
        if (fScanner.LastToken != Token)
        {
            throw new EBold($"Unexpected token: \"{fScanner.LastTokenText}\"");
        }

        fScanner.NextToken();
    }

    [Delphi(PortStatus.Ported)]
    private static void AssertNonterminal(bool Value)
    {
        if (!Value)
        {
            throw new EBold("Parse error...");
        }
    }

    [Delphi(PortStatus.Ported)]
    private void OptionalToken(TBoldDTDToken Token)
    {
        if (fScanner.LastToken == Token)
        {
            fScanner.NextToken();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool ParseToken(TBoldDTDToken Token)
    {
        if (fScanner.LastToken == Token)
        {
            fScanner.NextToken();
            return true;
        }

        return false;
    }

    /// <summary>Expands the parameter entities (%name;) with the text they were declared with.</summary>
    [Delphi(PortStatus.Ported)]
    private static string Preprocess(string DTD)
    {
        var peTable = new Dictionary<string, string>(StringComparer.Ordinal);
        var scanner = new TBoldDTDScanner(DTD);
        scanner.NextToken();
        while (scanner.LastToken != TBoldDTDToken.dtEOF)
        {
            if (scanner.LastToken == TBoldDTDToken.dtEntityDecl)
            {
                scanner.NextToken();
                scanner.NextToken();
                if (scanner.LastToken == TBoldDTDToken.dtPercent)
                {
                    scanner.NextToken();
                    scanner.NextToken();
                    string peName = scanner.LastTokenText;
                    scanner.NextToken();
                    scanner.NextToken();
                    string peText = scanner.LastTokenText;
                    peTable[peName] = peText[1..^1];
                }
            }

            scanner.NextToken();
        }

        var lines = DTD.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int j = 0;
            while (j < line.Length)
            {
                if (line[j] == '%' && j + 1 < line.Length && !char.IsWhiteSpace(line[j + 1]))
                {
                    int k = line.IndexOf(';', j + 1);
                    if (k < 0)
                    {
                        break;
                    }

                    line = line[..j] + " " + peTable.GetValueOrDefault(line[(j + 1)..k], "") + " " + line[(k + 1)..];
                }
                else
                {
                    j++;
                }
            }

            lines[i] = line;
        }

        return string.Join("\n", lines) + "\n";
    }

    // extSubsetDecl ::= ( markupdecl | conditionalSect | DeclSep)*
    [Delphi(PortStatus.Ported)]
    private void ParseExtSubsetDecl()
    {
        while (ParseMarkupDecl() || ParseToken(TBoldDTDToken.dtSpace))
        {
        }
    }

    // markupdecl ::= elementdecl | AttlistDecl | EntityDecl | NotationDecl | PI | Comment
    [Delphi(PortStatus.Ported)]
    private bool ParseMarkupDecl() => ParseElementDecl() || ParseAttListDecl() || ParseEntityDecl() || ParseToken(TBoldDTDToken.dtComment);

    // elementDecl ::= '<!ELEMENT' S Name S contentspec S? '>'
    [Delphi(PortStatus.Ported)]
    private bool ParseElementDecl()
    {
        if (!ParseToken(TBoldDTDToken.dtElementDecl))
        {
            return false;
        }

        AssertToken(TBoldDTDToken.dtSpace);
        if (fScanner.LastToken != TBoldDTDToken.dtName)
        {
            throw new EBold($"{GetType().Name}.ParseElementDecl: Unexpected token {fScanner.LastTokenText}, expected name");
        }

        StartElement(fScanner.LastTokenText);
        AssertToken(TBoldDTDToken.dtName);
        AssertToken(TBoldDTDToken.dtSpace);
        AssertNonterminal(ParseContentSpec());
        OptionalToken(TBoldDTDToken.dtSpace);
        AssertToken(TBoldDTDToken.dtEndDecl);
        EndElement();
        return true;
    }

    /// <summary>Skipped: the members of a class are its content, not its attributes.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ParseAttListDecl() => SkipDeclaration(TBoldDTDToken.dtAttlistDecl);

    /// <summary>Skipped: parameter entities are expanded before parsing.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ParseEntityDecl() => SkipDeclaration(TBoldDTDToken.dtEntityDecl);

    private bool SkipDeclaration(TBoldDTDToken Token)
    {
        if (!ParseToken(Token))
        {
            return false;
        }

        while (fScanner.LastToken != TBoldDTDToken.dtEndDecl)
        {
            fScanner.NextToken();
        }

        fScanner.NextToken();
        return true;
    }

    // contentspec ::= 'EMPTY' | 'ANY' | Mixed | children
    [Delphi(PortStatus.Ported)]
    private bool ParseContentSpec() => ParseToken(TBoldDTDToken.dtEmptyContent) || ParseToken(TBoldDTDToken.dtAnyContent) || ParseMixedOrChildren();

    [Delphi(PortStatus.Ported)]
    private bool ParseMixedOrChildren()
    {
        if (!ParseToken(TBoldDTDToken.dtOpenParen))
        {
            return false;
        }

        OptionalToken(TBoldDTDToken.dtSpace);
        AssertNonterminal(ParseMixed2() || ParseChildren2());
        return true;
    }

    [Delphi(PortStatus.Ported, Note = "Delphi raised \"not implemented\"; the parser uses ParseMixed2.")]
    private bool ParseMixed() => throw new EBold("Not implemented");

    [Delphi(PortStatus.Ported, Note = "Delphi left it unimplemented (it made an exception without raising it); the parser uses ParseChildren2.")]
    private bool ParseChildren() => throw new EBold("Not implemented");

    // Mixed ::= '#PCDATA' S? ( '|' S? Name S? )* ')*' | '#PCDATA' S? ')'   ('(' S? already parsed)
    [Delphi(PortStatus.Ported)]
    private bool ParseMixed2()
    {
        bool result = ParseToken(TBoldDTDToken.dtPCDataDecl);
        if (result)
        {
            OptionalToken(TBoldDTDToken.dtSpace);
            if (fScanner.LastToken == TBoldDTDToken.dtChoice)
            {
                while (fScanner.LastToken == TBoldDTDToken.dtChoice)
                {
                    fScanner.NextToken();
                    OptionalToken(TBoldDTDToken.dtSpace);
                    AssertToken(TBoldDTDToken.dtName);
                    OptionalToken(TBoldDTDToken.dtSpace);
                }

                AssertToken(TBoldDTDToken.dtCloseParen);
                AssertToken(TBoldDTDToken.dtZeroOrMore);
            }
            else
            {
                AssertToken(TBoldDTDToken.dtCloseParen);
            }
        }

        return result;
    }

    // children ::= (choice | seq) ('?' | '*' | '+')?   ('(' S? already parsed)
    [Delphi(PortStatus.Ported)]
    private bool ParseChildren2()
    {
        bool result = ParseChoiceOrSeq();
        if (result)
        {
            _ = ParseToken(TBoldDTDToken.dtOptional) || ParseToken(TBoldDTDToken.dtZeroOrMore) || ParseToken(TBoldDTDToken.dtOneOrMore);
        }

        return result;
    }

    // cp ::= (Name | choice | seq) ('?' | '*' | '+')?
    [Delphi(PortStatus.Ported)]
    private bool ParseCp()
    {
        bool result;
        if (fScanner.LastToken == TBoldDTDToken.dtName)
        {
            FoundCpName(fScanner.LastTokenText);
            fScanner.NextToken();
            result = true;
        }
        else if (ParseToken(TBoldDTDToken.dtOpenParen))
        {
            OptionalToken(TBoldDTDToken.dtSpace);
            AssertNonterminal(ParseChoiceOrSeq());
            result = true;
        }
        else
        {
            result = false;
        }

        if (result)
        {
            _ = ParseToken(TBoldDTDToken.dtOptional) || ParseToken(TBoldDTDToken.dtZeroOrMore) || ParseToken(TBoldDTDToken.dtOneOrMore);
        }

        return result;
    }

    // choice ::= '(' S? cp S? ( '|' S? cp S? )+ ')';  seq ::= '(' S? cp S? ( ',' S? cp S? )* ')'   ('(' S? already parsed)
    [Delphi(PortStatus.Ported)]
    private bool ParseChoiceOrSeq()
    {
        AssertNonterminal(ParseCp());
        OptionalToken(TBoldDTDToken.dtSpace);
        if (fScanner.LastToken is TBoldDTDToken.dtChoice or TBoldDTDToken.dtComma)
        {
            TBoldDTDToken separator = fScanner.LastToken;
            while (fScanner.LastToken == separator)
            {
                fScanner.NextToken();
                OptionalToken(TBoldDTDToken.dtSpace);
                AssertNonterminal(ParseCp());
                OptionalToken(TBoldDTDToken.dtSpace);
            }
        }

        AssertToken(TBoldDTDToken.dtCloseParen);
        return true;
    }

    [Delphi(PortStatus.Ported)] private void StartElement(string Name) => OnStartElement?.Invoke(Name);
    [Delphi(PortStatus.Ported)] private void EndElement() => OnEndElement?.Invoke();
    [Delphi(PortStatus.Ported)] private void FoundCpName(string Name) => OnFoundCpName?.Invoke(Name);

    [Delphi(PortStatus.Ported)]
    public void Parse(string DTD)
    {
        fScanner = new TBoldDTDScanner(Preprocess(DTD));
        fScanner.NextToken();
        ParseExtSubsetDecl();
        if (fScanner.LastToken != TBoldDTDToken.dtEOF)
        {
            throw new EBold($"{GetType().Name}: terminated abnormally at: {fScanner.LastTokenText}");
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldDTDNameEvent? OnStartElement { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldDTDEvent? OnEndElement { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldDTDNameEvent? OnFoundCpName { get; set; }
}
