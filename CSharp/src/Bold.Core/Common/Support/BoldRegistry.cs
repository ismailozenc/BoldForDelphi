// Delphi: Source/Common/Support/BoldRegistry.pas
// Port map: CSharp/port/units/BoldRegistry.md

using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Bold;

/// <summary>Which part of Bold's registry key settings go under: design time or run time.</summary>
[Delphi(PortStatus.Ported)]
public enum TRegistryMode { rmDesigntime, rmRuntime }

/// <summary>
/// Settings in the Windows registry, under Bold's key of the current user (Software\BoldSoft\...) and its design time
/// or run time part: open a key, read and write values, close the key.
/// </summary>
[Delphi(PortStatus.Ported, Note = "On Microsoft.Win32.Registry (Windows only); Delphi's TRegistry semantics: a key path is relative to the open key, or to the root with a leading backslash.")]
[SupportedOSPlatform("windows")]
public class TBoldRegistry : IDisposable
{
    private readonly RegistryKey fRoot = Registry.CurrentUser;
    private RegistryKey? fCurrentKey;
    private TRegistryMode fRegistryMode;

    [Delphi(PortStatus.Ported)]
    public TBoldRegistry()
    {
        fRegistryMode = TRegistryMode.rmDesigntime;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegistry.Destroy")]
    public void Dispose()
    {
        CloseKey();
        GC.SuppressFinalize(this);
    }

    /// <summary>Opens (making it when missing) the key under Bold's key and the mode's part.</summary>
    [Delphi(PortStatus.Ported)]
    public bool OpenKey(string Key) => Open(BoldUtils.BoldRootRegistryKey() + BoldRegistry.RegPathExtention[(int)RegistryMode] + Key);

    /// <summary>Opens (making it when missing) the key as given.</summary>
    [Delphi(PortStatus.Ported)]
    public bool OpenNotDefaultKey(string Key) => Open(Key);

    [Delphi(PortStatus.Ported)]
    public void CloseKey()
    {
        fCurrentKey?.Dispose();
        fCurrentKey = null;
    }

    [Delphi(PortStatus.Ported)]
    public string ReadString(string Name, string DefaultValue) =>
        ValueExists(Name) ? Convert.ToString(fCurrentKey!.GetValue(Name), System.Globalization.CultureInfo.InvariantCulture) ?? "" : DefaultValue;

    /// <summary>A boolean is an integer that is not zero.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ReadBool(string Name, bool DefaultValue) => ValueExists(Name) ? ReadInteger(Name, 0) != 0 : DefaultValue;

    [Delphi(PortStatus.Ported)]
    public int ReadInteger(string Name, int DefaultValue) =>
        ValueExists(Name) ? Convert.ToInt32(fCurrentKey!.GetValue(Name), System.Globalization.CultureInfo.InvariantCulture) : DefaultValue;

    [Delphi(PortStatus.Ported)]
    public void WriteString(string Name, string Value) => CurrentKey().SetValue(Name, Value, RegistryValueKind.String);

    /// <summary>Written as the integer 1 or 0.</summary>
    [Delphi(PortStatus.Ported)]
    public void WriteBool(string Name, bool Value) => WriteInteger(Name, Value ? 1 : 0);

    [Delphi(PortStatus.Ported)]
    public void WriteInteger(string Name, int Value) => CurrentKey().SetValue(Name, Value, RegistryValueKind.DWord);

    [Delphi(PortStatus.Ported)]
    public bool ValueExists(string Name) => fCurrentKey is not null && fCurrentKey.GetValueNames().Contains(Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Deletes the key (relative to the open key, or to the root with a leading backslash) and its subkeys.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DeleteKey(string Key)
    {
        (RegistryKey baseKey, string path) = Resolve(Key);
        try
        {
            baseKey.DeleteSubKeyTree(path, false);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException or ArgumentException)
        {
            return false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TRegistryMode RegistryMode
    {
        get => fRegistryMode;
        set => fRegistryMode = value;
    }

    private RegistryKey CurrentKey() => fCurrentKey ?? throw new InvalidOperationException("No registry key is open (call OpenKey first)");

    /// <summary>The key a path starts from and the path below it.</summary>
    private (RegistryKey BaseKey, string Path) Resolve(string key) =>
        key.StartsWith('\\') || fCurrentKey is null ? (fRoot, key.TrimStart('\\')) : (fCurrentKey, key);

    private bool Open(string key)
    {
        (RegistryKey baseKey, string path) = Resolve(key);
        try
        {
            RegistryKey? opened = baseKey.CreateSubKey(path, true);
            if (opened is null)
            {
                return false;
            }

            CloseKey();
            fCurrentKey = opened;
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return false;
        }
    }
}

/// <summary>Unit-level routines and constants of BoldRegistry.</summary>
[SupportedOSPlatform("windows")]
public static class BoldRegistry
{
    /// <summary>The parts of Bold's key for the registry modes.</summary>
    [Delphi(PortStatus.Ported)]
    public static readonly string[] RegPathExtention = ["\\Designtime", "\\Runtime"];

    /// <summary>Where Bold is installed: RootDir under Bold's key of the local machine (empty when missing).</summary>
    [Delphi(PortStatus.Ported)]
    public static string GetBoldBasePath()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(BoldUtils.BoldRootRegistryKey(), false);
        return key?.GetValue("RootDir") as string ?? "";
    }

    [Delphi(PortStatus.Ported)]
    public static string GetBoldLibPath()
    {
        string result = GetBoldBasePath();
        return result != "" ? result + "\\Lib" : result;
    }

    [Delphi(PortStatus.Ported)]
    public static string GetBoldResPath()
    {
        string result = GetBoldBasePath();
        return result != "" ? result + "\\Res" : result;
    }
}
