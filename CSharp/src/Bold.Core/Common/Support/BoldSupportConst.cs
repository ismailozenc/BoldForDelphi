// Delphi: Source/Common/Support/BoldSupportConst.pas
// Port map: CSharp/port/units/BoldSupportConst.md

namespace Bold;

/// <summary>The messages of BoldIsoDateTime.</summary>
public static class BoldSupportConst
{
    [Delphi(PortStatus.Ported)] public const string sInvalidDateFormatFormat = "ParseISODate: Invalid date format %s. Should be YYYY-MM-DD";
    [Delphi(PortStatus.Ported)] public const string sInnvalidDateFormatLargeMonth = "ParseISODate: Invalid date format %s. month > 12";
    [Delphi(PortStatus.Ported)] public const string sInvalidDateFormatSmallMonth = "ParseISODate: Invalid date format %s. month < 1";
    [Delphi(PortStatus.Ported)] public const string sInvalidDateFormatSmallDay = "ParseISODate: Invalid date format %s. date < 1";
    [Delphi(PortStatus.Ported)] public const string sInvalidDateFormatBadDay = "ParseISODate: Invalid date format %s. there is only %d days in month %d";
    [Delphi(PortStatus.Ported)] public const string sInvalidDateTimeFormat = "ParseISODateTime: Invalid datatime format %s. Should be YYYY-MM-DD HH:MM[:SS]";
    [Delphi(PortStatus.Ported)] public const string sInvalidTimeFormat = "ParseISOTime: Invalid time format %s. Should be HH:MM[:SS]";
    [Delphi(PortStatus.Ported)] public const string sInvalidTimeFormatLargeHour = "ParseISOTime: Invalid time format %s. h > 23";
    [Delphi(PortStatus.Ported)] public const string sInvalidTimeFormatLargeMinute = "ParseISOTime: Invalid time format %s. m > 59";
    [Delphi(PortStatus.Ported)] public const string sInvalidTimeFormatLargeSecond = "ParseISOTime: Invalid time format %s. s > 59";
}
