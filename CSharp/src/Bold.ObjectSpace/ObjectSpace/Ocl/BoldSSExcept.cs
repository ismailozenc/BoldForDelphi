// Delphi: Source/ObjectSpace/Ocl/BoldSSExcept.pas
// Port map: CSharp/port/units/BoldSSExcept.md
// The exceptions of the Sandstone lexer and parser engine the OCL parser is generated for.

namespace Bold;

/// <summary>What went wrong in the lexer or parser engine.</summary>
[Delphi(PortStatus.Ported)]
public enum SSExceptionError
{
    SSExceptionLexFileOpen,
    SSExceptionLexLexemeLength,
    SSExceptionOutOfMemory,
    SSExceptionLexFileRead,
    SSExceptionLexBadTable,
    SSExceptionMissingPart,
    SSExceptionBadList,
    SSExceptionStackTop,
    SSExceptionStackPop,
    SSExceptionLexError,
    SSExceptionYaccMissingTable,
    SSExceptionYaccMissingLexer,
    SSExceptionYaccFileOpen,
    SSExceptionYaccRead,
    SSExceptionYaccTableSize,
    SSExceptionYaccParse,
    SSExceptionYaccEof,
    SSExceptionYaccElement,
    SSExceptionYaccSyncErrToken,
    SSExceptionYaccSyncErrEof,
    SSExceptionLexFindResource,
    SSExceptionYaccFindResource,
    SSExceptionLexBadResource,
    SSExceptionYaccBadResource,
}

/// <summary>An error of the lexer or parser engine, with its kind and (for lexer errors) the offset.</summary>
[Delphi(PortStatus.Ported)]
public class SSException : EAbort
{
    public int Position;
    public SSExceptionError Id;

    [Delphi(PortStatus.Ported)]
    public SSException(SSExceptionError TheId, string FmtStr)
        : base(FmtStr)
    {
        Id = TheId;
    }

    private SSException(SSExceptionError TheId, string Message, int ThePosition)
        : base(Message)
    {
        Id = TheId;
        Position = ThePosition;
    }

    [Delphi(PortStatus.Ported)]
    public static SSException CreateName(SSExceptionError TheId, string FmtStr, string Name) => new(TheId, SysUtils.Format(FmtStr, Name), 0);

    [Delphi(PortStatus.Ported)]
    public static SSException CreateLong(SSExceptionError TheId, string TheFmtStr, int TheLong) =>
        new(TheId, SysUtils.Format(ReplaceLd(TheFmtStr, false), TheLong), 0);

    /// <summary>A message with two numbers and a name; the position is the second number (the offset).</summary>
    [Delphi(PortStatus.Ported)]
    public static SSException CreateLongLongNameLen(SSExceptionError TheId, string FmtStr, int Long0, int Long1, string TheName, int TheLen) =>
        new(TheId, SysUtils.Format(ReplaceLd(FmtStr, true), Long0, Long1, TheName.Length > TheLen ? TheName[..TheLen] : TheName), Long1);

    /// <summary>C's %ld as Delphi's %d (the first only, or all, as Delphi's StringReplace calls do).</summary>
    private static string ReplaceLd(string s, bool all)
    {
        if (all)
        {
            return s.Replace("%ld", "%d", StringComparison.Ordinal);
        }

        int i = s.IndexOf("%ld", StringComparison.Ordinal);
        return i < 0 ? s : s[..i] + "%d" + s[(i + 3)..];
    }
}
