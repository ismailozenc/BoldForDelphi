// Delphi: Source/MoldModel/Core/BoldNameExpander.pas
// Port map: CSharp/port/units/BoldNameExpander.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldNameExpander", Symbol = "initialization", Note = "The unit's initialization section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldNameExpander", Symbol = "finalization", Note = "Frees the cached expanders; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNameExpander", Symbol = "TBoldCharacterSet", Note = "Global using alias of DelphiSet<char>, in BoldNameExpander.Aliases.cs; characters above 255 are never in it.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNameExpander", Symbol = "TBoldNameExpanderClass", Note = "Global using alias of System.Type, in BoldNameExpander.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNameExpander", Symbol = "TBoldSQLNameExpanderClass", Note = "Global using alias of System.Type, in BoldNameExpander.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNameExpander", Symbol = "TBoldDelphiNameExpanderClass", Note = "Global using alias of System.Type, in BoldNameExpander.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNameExpander", Symbol = "TBoldExpressionNameExpanderClass", Note = "Global using alias of System.Type, in BoldNameExpander.Aliases.cs.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TExpansionType { xtDelphi, xtSQL, xtExpression }

/// <summary>
/// Turns a name template such as "T&lt;Name&gt;" into an identifier for a target language: replaces &lt;Name&gt;,
/// maps characters the language does not allow, and shortens names that are too long with a checksum suffix.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAbstractNameExpander
{
    private static readonly Dictionary<char, char> NationalCharacters = new()
    {
        ['â'] = 'a', ['Â'] = 'A', ['á'] = 'a', ['à'] = 'a', ['À'] = 'A', ['Á'] = 'A', ['Å'] = 'A', ['å'] = 'a', ['Ä'] = 'A', ['ä'] = 'a',
        ['Ö'] = 'O', ['ö'] = 'o', ['Ó'] = 'O', ['ó'] = 'o', ['Ò'] = 'O', ['ò'] = 'o', ['Ô'] = 'O', ['ô'] = 'o',
        ['É'] = 'E', ['È'] = 'E', ['Ê'] = 'E', ['Ë'] = 'E', ['ë'] = 'e', ['é'] = 'e', ['è'] = 'e', ['ê'] = 'e',
        ['Ü'] = 'U', ['ü'] = 'u', ['Ú'] = 'U', ['Û'] = 'U', ['û'] = 'u', ['ú'] = 'u', ['Ù'] = 'U', ['ù'] = 'u',
        ['Ñ'] = 'N', ['ñ'] = 'n',
        ['Í'] = 'I', ['Ì'] = 'I', ['Ï'] = 'I', ['Î'] = 'I', ['î'] = 'i', ['í'] = 'i', ['ì'] = 'i', ['ï'] = 'i',
        ['Ý'] = 'Y', ['ý'] = 'y', ['ÿ'] = 'y',
        ['Ç'] = 'C', ['ç'] = 'c',
    };

    private static readonly char[] Characters =
    [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'X', 'Y', 'Z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '_',
    ];

    internal static readonly TBoldCharacterSet IdentifierChars =
        TBoldCharacterSet.Range('a', 'z') + TBoldCharacterSet.Range('A', 'Z') + TBoldCharacterSet.Range('0', '9') + '_';

    internal static readonly TBoldCharacterSet FirstIdentifierChars =
        TBoldCharacterSet.Range('a', 'z') + TBoldCharacterSet.Range('A', 'Z') + '_';

    internal static bool InSet(char c, TBoldCharacterSet set) => c <= 255 && set.Contains(c);

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldCharacterSet GetValidCharacters() => TBoldCharacterSet.Empty;

    /// <summary>The replacement for a character the language does not allow: an unaccented letter when national characters are converted, otherwise '_'.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual char GetMapCharacter(char ch, TBoldNationalCharConversion NationalCharConversion)
    {
        bool convert = (NationalCharConversion == TBoldNationalCharConversion.nccDefault && BoldNameExpander.BoldNameExpanderMapAnsiCharacters)
            || NationalCharConversion == TBoldNationalCharConversion.nccTrue;
        return convert && NationalCharacters.TryGetValue(ch, out char mapped) ? mapped : '_';
    }

    [Delphi(PortStatus.Ported)]
    protected bool GetLanguageIsCaseSensitive() => false;

    /// <summary>Replaces each &lt;Name&gt; (any case) in <paramref name="Name"/> with <paramref name="ReplacementName"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual string ExpandName(string Name, string ReplacementName)
    {
        string DoExpand(string s)
        {
            if (s == BoldDefaultTaggedValues.TV_NAME)
            {
                return ReplacementName;
            }

            if (s.Length < BoldDefaultTaggedValues.TV_NAME_Length)
            {
                return s;
            }

            int i = BoldUtils.BoldCaseIndependentPos(BoldDefaultTaggedValues.TV_NAME, s);
            return i != 0
                ? SysUtils.Copy(s, 1, i - 1) + ReplacementName + DoExpand(SysUtils.Copy(s, i + BoldDefaultTaggedValues.TV_NAME_Length, SysUtils.MaxInt))
                : s;
        }

        return DoExpand(Name ?? "");
    }

    [Delphi(PortStatus.Ported)]
    public virtual string MapCharacters(string Name, TBoldNationalCharConversion NationalCharConversion)
    {
        TBoldCharacterSet validChars = ValidCharacters;
        if (validChars.IsEmpty || string.IsNullOrEmpty(Name))
        {
            return Name ?? "";
        }

        char[] result = Name.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            if (!InSet(result[i], validChars))
            {
                result[i] = MapCharacter(result[i], NationalCharConversion);
            }
        }

        return new string(result);
    }

    /// <summary>Shortens a name longer than <paramref name="MaxIdentifierLength"/> (-1: no limit), ending it with a checksum of the full name.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual string TruncateName(string Name, int MaxIdentifierLength)
    {
        if (MaxIdentifierLength != -1 && Name.Length > MaxIdentifierLength)
        {
            string result = SysUtils.Copy(Name, 1, MaxIdentifierLength - 3);
            int checkSum = 0;
            foreach (char c in Name)
            {
                checkSum = (checkSum ^ c) & 0xFFFF;
                checkSum = ((checkSum << 3) % 65536) + (checkSum >> 13);
            }

            while (checkSum > 0 && result.Length < MaxIdentifierLength)
            {
                result += Characters[checkSum % 37];
                checkSum /= 37;
            }

            return result;
        }

        return Name;
    }

    /// <summary>True when the name needs no mapping or truncation; otherwise <paramref name="Reason"/> says why not.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool ValidateName(string Name, ref string Reason, TBoldNationalCharConversion NationalCharConversion, int MaxIdentifierLength)
    {
        bool result = true;
        if (Name != MapCharacters(Name, NationalCharConversion))
        {
            result = false;
            Reason = BoldMoldConsts.sNameHasInvalidChars;
        }

        if (Name != TruncateName(Name, MaxIdentifierLength))
        {
            result = false;
            Reason = BoldMoldConsts.sNameTooLong;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldCharacterSet ValidCharacters => GetValidCharacters();

    [Delphi(PortStatus.Ported)]
    public char MapCharacter(char ch, TBoldNationalCharConversion NationalCharConversion) => GetMapCharacter(ch, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    public bool LanguageIsCaseSensitive => GetLanguageIsCaseSensitive();
}

/// <summary>SQL names: letters, digits, quotes and '_'; optionally upper-cased.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLNameExpander : TBoldAbstractNameExpander
{
    private static readonly TBoldCharacterSet SqlChars = IdentifierChars + '"' + '\'';

    [Delphi(PortStatus.Ported)]
    protected override TBoldCharacterSet GetValidCharacters() => SqlChars;

    [Delphi(PortStatus.Ported)]
    public override string ExpandName(string Name, string ReplacementName)
    {
        string result = base.ExpandName(Name, ReplacementName);
        return BoldNameExpander.BoldSQLNameExpanderUpperCaseNames ? SysUtils.UpperCase(result) : result;
    }
}

/// <summary>Delphi identifiers: letters, digits and '_', not starting with a digit.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDelphiNameExpander : TBoldAbstractNameExpander
{
    [Delphi(PortStatus.Ported)]
    protected override TBoldCharacterSet GetValidCharacters() => IdentifierChars;

    [Delphi(PortStatus.Ported)]
    public override string MapCharacters(string Name, TBoldNationalCharConversion NationalCharConversion) =>
        BoldNameExpander.MapFirstCharacter(this, base.MapCharacters(Name, NationalCharConversion), NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    public override bool ValidateName(string Name, ref string Reason, TBoldNationalCharConversion NationalCharConversion, int MaxIdentifierLength) =>
        BoldNameExpander.ValidateIdentifier(base.ValidateName(Name, ref Reason, NationalCharConversion, MaxIdentifierLength), Name, ref Reason);
}

/// <summary>OCL expression names: letters, digits and '_', not starting with a digit.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExpressionNameExpander : TBoldAbstractNameExpander
{
    [Delphi(PortStatus.Ported)]
    protected override TBoldCharacterSet GetValidCharacters() => IdentifierChars;

    [Delphi(PortStatus.Ported)]
    public override string MapCharacters(string Name, TBoldNationalCharConversion NationalCharConversion) =>
        BoldNameExpander.MapFirstCharacter(this, base.MapCharacters(Name, NationalCharConversion), NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    public override bool ValidateName(string Name, ref string Reason, TBoldNationalCharConversion NationalCharConversion, int MaxIdentifierLength) =>
        BoldNameExpander.ValidateIdentifier(base.ValidateName(Name, ref Reason, NationalCharConversion, MaxIdentifierLength), Name, ref Reason);
}

/// <summary>Unit-level settings and routines of BoldNameExpander.</summary>
public static partial class BoldNameExpander
{
    /// <summary>The SQL expander type; set it to a subclass to change how SQL names are made.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldSQLNameExpanderClass BoldSQLNameExpanderClass = typeof(TBoldSQLNameExpander);

    [Delphi(PortStatus.Ported)]
    public static TBoldDelphiNameExpanderClass BoldDelphiNameExpanderClass = typeof(TBoldDelphiNameExpander);

    [Delphi(PortStatus.Ported)]
    public static TBoldExpressionNameExpanderClass BoldExpressionNameExpanderClass = typeof(TBoldExpressionNameExpander);

    [Delphi(PortStatus.Ported)]
    public static bool BoldNameExpanderMapAnsiCharacters = false;

    [Delphi(PortStatus.Ported)]
    public static int BoldSQLNameExpanderNameLimit = 255;

    [Delphi(PortStatus.Ported)]
    public static bool BoldSQLNameExpanderUpperCaseNames = false;

    private static TBoldAbstractNameExpander? BoldSQLNameExpander;
    private static TBoldAbstractNameExpander? BoldDelphiNameExpander;
    private static TBoldAbstractNameExpander? BoldExpressionNameExpander;

    private static TBoldAbstractNameExpander Ensure(ref TBoldAbstractNameExpander? expander, Type type)
    {
        if (expander is null || expander.GetType() != type)
        {
            expander = (TBoldAbstractNameExpander)Activator.CreateInstance(type)!;
        }

        return expander;
    }

    internal static string MapFirstCharacter(TBoldAbstractNameExpander expander, string result, TBoldNationalCharConversion conversion)
    {
        if (result.Length > 0 && !TBoldAbstractNameExpander.InSet(result[0], TBoldAbstractNameExpander.FirstIdentifierChars))
        {
            result = expander.MapCharacter(result[0], conversion) + result.Substring(1);
        }

        return result;
    }

    internal static bool ValidateIdentifier(bool result, string name, ref string reason)
    {
        if (result && string.IsNullOrEmpty(name))
        {
            reason = BoldMoldConsts.sNameCannotBeEmpty;
            return false;
        }

        if (result && !TBoldAbstractNameExpander.InSet(name[0], TBoldAbstractNameExpander.FirstIdentifierChars))
        {
            reason = BoldMoldConsts.sInvalidFirstChar;
            return false;
        }

        return result;
    }

    /// <summary>
    /// Expands a name template for a target language: replaces &lt;Name&gt;, maps characters and truncates. For SQL a
    /// length of -1 means BoldSQLNameExpanderNameLimit. The result is a shared string.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldExpandName(string Name, string ReplacementName, TExpansionType ExpansionType, int MaxIdentifierLength, TBoldNationalCharConversion NationalCharConversion)
    {
        TBoldAbstractNameExpander? current;
        switch (ExpansionType)
        {
            case TExpansionType.xtSQL:
                current = Ensure(ref BoldSQLNameExpander, BoldSQLNameExpanderClass);
                if (MaxIdentifierLength == -1)
                {
                    MaxIdentifierLength = BoldSQLNameExpanderNameLimit;
                }

                break;
            case TExpansionType.xtDelphi:
                current = Ensure(ref BoldDelphiNameExpander, BoldDelphiNameExpanderClass);
                break;
            case TExpansionType.xtExpression:
                current = Ensure(ref BoldExpressionNameExpander, BoldExpressionNameExpanderClass);
                break;
            default:
                current = null;
                break;
        }

        string result = current is not null
            ? current.TruncateName(current.MapCharacters(current.ExpandName(Name, ReplacementName), NationalCharConversion), MaxIdentifierLength)
            : Name;
        return BoldSharedStrings.BoldSharedStringManager().GetSharedString(result);
    }

    /// <summary>Replaces each table prefix tag (any case) with <paramref name="Prefix"/>, then expands the result as an SQL name.</summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldExpandPrefix(string value, string ReplacementName, string Prefix, int MaxIdentifierLength, TBoldNationalCharConversion NationalCharConversion)
    {
        string result = value ?? "";
        int p = SysUtils.Pos(SysUtils.UpperCase(BoldDefs.TABLEPREFIXTAG), SysUtils.UpperCase(result));
        while (p != 0)
        {
            result = result.Remove(p - 1, BoldDefs.TABLEPREFIXTAG.Length).Insert(p - 1, Prefix);
            p = SysUtils.Pos(SysUtils.UpperCase(BoldDefs.TABLEPREFIXTAG), SysUtils.UpperCase(result));
        }

        return BoldExpandName(result, ReplacementName, TExpansionType.xtSQL, MaxIdentifierLength, NationalCharConversion);
    }
}
