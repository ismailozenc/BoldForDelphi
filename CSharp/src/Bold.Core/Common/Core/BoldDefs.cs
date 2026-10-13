// Delphi: Source/Common/Core/BoldDefs.pas
// Port map: CSharp/port/units/BoldDefs.md

// Type aliases have no C# declaration to carry a marker; they are global using aliases in BoldDefs.Aliases.cs.
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldTimeStampType", Note = "Global using alias of int, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldClientID", Note = "Global using alias of int, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldByteSet", Note = "Global using alias of DelphiSet<byte>, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldDbType", Note = "Global using alias of int, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldRepresentation", Note = "Global using alias of int, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldExpression", Note = "Global using alias of string, in BoldDefs.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldValuePersistenceStateSet", Note = "Global using alias of DelphiSet<TBoldValuePersistenceState>, in BoldDefs.Aliases.cs.")]

namespace Bold;

/// <summary>Unit-level constants of the Delphi unit BoldDefs. Use <c>using static Bold.BoldDefs;</c> where Delphi has <c>uses BoldDefs</c>.</summary>
public static partial class BoldDefs
{
    [Delphi(PortStatus.Ported)] public const int NO_CLASS = -1;
    /// <summary>Used for 'databases' without NULL, such as Paradox and dBase.</summary>
    [Delphi(PortStatus.Ported)] public const int INTERNALNULLKEY = -1;
    /// <summary>Indicates a newly created default id without a database value.</summary>
    [Delphi(PortStatus.Ported)] public const int UNASSIGNEDID = -1;

    [Delphi(PortStatus.Ported)] public const int NOTVALIDCLIENTID = -1;

    [Delphi(PortStatus.Ported)] public const char BOLDESC = '\u001B';
    [Delphi(PortStatus.Ported)] public const char BOLDNULL = '\0';
    [Delphi(PortStatus.Ported)] public const string BOLDCRLF = "\r\n";
    [Delphi(PortStatus.Ported)] public const char BOLDCR = '\r';
    [Delphi(PortStatus.Ported)] public const char BOLDLF = '\n';

    /// <summary>Prepared for other directory separators.</summary>
    [Delphi(PortStatus.Ported)] public const char BOLDDIRSEPARATOR = '\\';

    [Delphi(PortStatus.Ported)] public const string ALLFILESFILTER = "All files (*.*)|*.*";

    [Delphi(PortStatus.Ported)] public const string IDCOLUMN_NAME = "BOLD_ID";
    [Delphi(PortStatus.Ported)] public const string TYPECOLUMN_NAME = "BOLD_TYPE";
    [Delphi(PortStatus.Ported)] public const string TABLEPREFIXTAG = "<Prefix>";
    [Delphi(PortStatus.Ported)] public const string DEFAULTTABLEPREFIX = "Bold";
    [Delphi(PortStatus.Ported)] public const string OBJECTTABLE_NAME = TABLEPREFIXTAG + "_OBJECT";
    [Delphi(PortStatus.Ported)] public const string TYPETABLE_NAME = TABLEPREFIXTAG + "_TYPE";
    [Delphi(PortStatus.Ported)] public const string IDTABLE_NAME = TABLEPREFIXTAG + "_ID";
    [Delphi(PortStatus.Ported)] public const string TABLETABLE_NAME = TABLEPREFIXTAG + "_TABLES";
    [Delphi(PortStatus.Ported)] public const string TIMESTAMPTABLE_NAME = TABLEPREFIXTAG + "_TIMESTAMP";
    [Delphi(PortStatus.Ported)] public const string CLOCKLOGTABLE_NAME = TABLEPREFIXTAG + "_CLOCKLOG";
    [Delphi(PortStatus.Ported)] public const string LASTCLOCKTABLE_NAME = TABLEPREFIXTAG + "_LASTCLOCK";

    [Delphi(PortStatus.Ported)] public const string MemberMappingTable_NAME = TABLEPREFIXTAG + "_MEMBERMAPPING";
    [Delphi(PortStatus.Ported)] public const string MMT_CLASSNAME_COLUMN = "CLASSNAME";
    [Delphi(PortStatus.Ported)] public const string MMT_MEMBERNAME_COLUMN = "MEMBERNAME";
    [Delphi(PortStatus.Ported)] public const string MMT_TABLENAME_COLUMN = "TABLENAME";
    [Delphi(PortStatus.Ported)] public const string MMT_COLUMNS_COLUMN = "COLUMNS";
    [Delphi(PortStatus.Ported)] public const string MMT_MAPPERNAME_COLUMN = "MAPPERNAME";

    [Delphi(PortStatus.Ported)] public const string AllInstancesMappingTable_NAME = TABLEPREFIXTAG + "_R_CLSMAP";
    [Delphi(PortStatus.Ported)] public const string AID_CLASSNAME_COLUMN = "CLASSNAME";
    [Delphi(PortStatus.Ported)] public const string AID_TABLENAME_COLUMN = "TABLENAME";
    [Delphi(PortStatus.Ported)] public const string AID_CLASSIDREQUIRED_COLUMN = "CLASSIDREQUIRED";

    [Delphi(PortStatus.Ported)] public const string ObjectStorageMappingTable_NAME = TABLEPREFIXTAG + "_W_CLSMAP";
    [Delphi(PortStatus.Ported)] public const string ST_CLASSNAME_COLUMN = "CLASSNAME";
    [Delphi(PortStatus.Ported)] public const string ST_TABLENAME_COLUMN = "TABLENAME";

    [Delphi(PortStatus.Ported)] public const string CLASSNAMECOLUMN_NAME = "CLASSNAME";
    [Delphi(PortStatus.Ported)] public const string TABLENAMECOLUMN_NAME = "TABLENAME";
    [Delphi(PortStatus.Ported)] public const string READONLYCOLUMN_NAME = "READ_ONLY";
    [Delphi(PortStatus.Ported)] public const string TIMESTAMPCOLUMN_NAME = "BOLD_TIME_STAMP";
    [Delphi(PortStatus.Ported)] public const string GLOBALIDCOLUMN_NAME = "EXTERNAL_ID";
    [Delphi(PortStatus.Ported)] public const string TIMESTAMPSTARTCOLUMNNAME = "TimeStampStart";
    [Delphi(PortStatus.Ported)] public const string TIMESTAMPSTOPCOLUMNNAME = "TimeStampStop";
    [Delphi(PortStatus.Ported)] public const string LASTTIMESTAMPCOLUMN_NAME = "LastTimestamp";
    [Delphi(PortStatus.Ported)] public const string THISTIMESTAMPCOLUMN_NAME = "ThisTimestamp";
    [Delphi(PortStatus.Ported)] public const string LASTCLOCKCOLUMN_NAME = "LastClockTime";
    [Delphi(PortStatus.Ported)] public const string THISCLOCKCOLUMN_NAME = "ThisClockTime";
    [Delphi(PortStatus.Ported)] public const string MODELVERSIONCOLUMN_NAME = "MODEL_VERSION";

    // Values for TBoldRepresentation.
    [Delphi(PortStatus.Ported)] public const TBoldRepresentation brDefault = 1;
    [Delphi(PortStatus.Ported)] public const TBoldRepresentation brShort = 2;
    [Delphi(PortStatus.Ported)] public const TBoldRepresentation brLong = 3;

    [Delphi(PortStatus.Ported)] public const int fmNormal = 0;
    [Delphi(PortStatus.Ported)] public const int fmDistributable = 1;
    [Delphi(PortStatus.Ported)] public const int fmCompare = 2;

    [Delphi(PortStatus.Ported)] public const string DEFAULTNAMELITERAL = "<Default>";

    /// <summary>Misspelled name kept for compatibility; deprecated in Delphi as well.</summary>
    [Delphi(PortStatus.Ported)]
    [Obsolete("Use DEFAULTNAMELITERAL.")]
    public const string DEFAULTNAMELITTERAL = DEFAULTNAMELITERAL;

    [Delphi(PortStatus.Ported)] public const string DEFAULTNAME = DEFAULTNAMELITERAL;

    [Delphi(PortStatus.Ported)] public const string BOLDTVPREFIX = "Bold.";

    [Delphi(PortStatus.Ported)] public const string LanguageNameDelphi = "Delphi";
    [Delphi(PortStatus.Ported)] public const string SQLDialectAnsi = "AnsiSQL";

    [Delphi(PortStatus.Ported)] public const char PROPAGATOR_PARAMETER_DELIMITER_CHAR = '|';

    [Delphi(PortStatus.Ported)] public const TBoldTimeStampType BOLDMAXTIMESTAMP = TBoldTimeStampType.MaxValue;
    [Delphi(PortStatus.Ported)] public const int BOLDINVALIDTIMESTAMP = -1;

    /// <summary>The host the code was built for. The C++Builder branch of the Delphi source does not apply.</summary>
    [Delphi(PortStatus.Ported)] public const string BOLD_HOST_IDE = "Delphi";

    [Delphi(PortStatus.Ported)] public const int ONE_SECOND = 1000;
    [Delphi(PortStatus.Ported)] public const int SIXTY_SECONDS = 60 * ONE_SECOND;
    [Delphi(PortStatus.Ported)] public const int THIRTY_MINUTES = SIXTY_SECONDS * 30;

    [Delphi(PortStatus.Ported)] public const int beConnected = 60;
    [Delphi(PortStatus.Ported)] public const int beDisconnected = 61;
    [Delphi(PortStatus.Ported)] public const int beDisconnecting = 62;
    [Delphi(PortStatus.Ported)] public const int beModified = 63;
}

[Delphi(PortStatus.Ported)]
public enum TBoldRoleType { rtRole, rtLinkRole, rtInnerLinkRole }

[Delphi(PortStatus.Ported)]
public enum TBoldDataBaseGenerationMode { dbgTable, dbgQuery }

[Delphi(PortStatus.Ported)]
public enum TBoldStorage { bsInternal, bsPartiallyExternal, bsExternal, bsExternalKey }

[Delphi(PortStatus.Ported)]
public delegate bool TBoldBooleanFunction();

[Delphi(PortStatus.Ported)]
public delegate DateTime TBoldGetTimeEvent();

[Delphi(PortStatus.Ported)]
public delegate void TBoldJustNotifyEvent();

[Delphi(PortStatus.Ported)]
public delegate void TBoldNotifyEventWithErrorMessage(object Sender, string ErrorMessage);

[Delphi(PortStatus.Ported)]
public delegate void TBoldLockManagerProgressEvent(int completed, int queued);

[Delphi(PortStatus.Ported)]
public enum TBoldCompareType { ctDefault, ctAsString, ctAsText, ctAsAnsiString, ctAsAnsiText, ctAsDate, ctAsTime }

[Delphi(PortStatus.Ported)]
public enum TBoldOrientation { orHorizontal, orVertical, orGrid }

[Delphi(PortStatus.Ported)]
public enum TBoldSQLStyle { ssColumns, ssParameters, ssValues }

[Delphi(PortStatus.Ported)]
public enum TBoldAbstractionLevel { alAbstract, alConcrete }

/// <summary>General Bold exception class.</summary>
[Delphi(PortStatus.Ported)]
public class EBold(string? message = null, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Raised for design-time errors.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldDesignTime(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>Raised for errors during import.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldImport(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldBadRepresentation(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>Raised for internal errors.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldInternal(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldFeatureNotImplementedYet(string? message = null, Exception? innerException = null) : EBoldInternal(message, innerException);

/// <summary>Raised for a column index out of bounds.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldBadColumnIndex(string? message = null, Exception? innerException = null) : EBoldInternal(message, innerException);

/// <summary>Raised when a user-defined constraint (such as a May operation) is violated.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldAssertionFailed(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldXMLLoadError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldXMLIncorrectXPath(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldEnsureDatabaseLockError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldLockManagerError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public class EBoldLicenseError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public enum TBoldValuePersistenceState { bvpsCurrent, bvpsModified, bvpsInvalid, bvpsTransient }

[Delphi(PortStatus.Ported)]
public enum TBoldLogType { ltInfo, ltDetail, ltWarning, ltError, ltSeparator }

[Delphi(PortStatus.Ported)]
public enum TBoldModuleType { mtUnit, mtText, mtIncFile }
