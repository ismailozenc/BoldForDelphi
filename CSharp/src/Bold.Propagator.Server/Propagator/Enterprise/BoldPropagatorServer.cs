// Delphi: Source/Propagator/Enterprise/BoldPropagatorServer.pas
// Port map: CSharp/port/units/BoldPropagatorServer.md
using static Bold.BoldPropagatorConstants;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServerClass", Note = "The singleton is always a TBoldPropagatorServer.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "_DllCanUnloadNow", Note = "COM in-process server.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.CreateClassFactories", Note = "COM class factories; the host maps the propagator hub instead.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.GetAppID", Note = "COM registration.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.AppID", Note = "COM registration.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.RegServer", Note = "COM registration.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.SetConfiguration", Note = "Wrote new COM class ids for another propagator on the machine; a .NET propagator is given its own URL.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.ClientHandlerCLSID", Note = "COM class id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.EnqueuerCLSID", Note = "COM class id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.LockManagerCLSID", Note = "COM class id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.LockManagerAdminCLSID", Note = "COM class id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.PropagatorConnectionCLSID", Note = "COM class id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.AddComObject", Note = "Kept the live COM objects; the hub keeps its connections.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.RemoveComObject", Note = "Kept the live COM objects; the hub keeps its connections.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.GetComObjectCount", Note = "Kept the live COM objects.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.ComObjectCount", Note = "Kept the live COM objects.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.GetObjectByIndex", Note = "Kept the live COM objects.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorServer", Symbol = "TBoldPropagatorServer.ComObjects", Note = "Kept the live COM objects.")]

namespace Bold;

/// <summary>
/// The propagator server, a singleton (<see cref="Instance"/>): its configuration, the propagator and the lock
/// manager. The configuration is read from the INI file next to the program (the module name with .ini), as in
/// Delphi: [FILE LOGGING] ENABLED, MAXFILESIZE, THREAD; [CONFIG PARAMS] THREADPOOLSIZE,
/// DISCONNECTCLIENTSONSENDFAILURE; [COM SERVER] SERVERNAME.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPropagatorServer : IDisposable
{
    private static readonly object G_Lock = new();
    private static TBoldPropagatorServer? G_PropagatorServer;

    private readonly object fLock = new();
    private string FModuleName = "";
    private int FClientNotifierPoolSize = DEFAULT_THREADPOOLSIZE;
    private bool FEnableLogging = DEFAULT_ENABLELOGGING;
    private string FLogFileName = "";
    private string FErrorLogFileName = "";
    private string fThreadLogFileName = "";
    private int FMaxLogFileSize = DEFAULT_LOGFILESIZE;
    private string FServerName = "EnterprisePropagator";
    private TBoldAdvancedPropagator? FAdvancedPropagator;
    private TBoldLockManager? fLockManager;
    private TBoldLockManagerAdmin? fLockManagerAdmin;
    private bool fDisconnectClientsOnSendFailure = DEFAULT_DISCONNECT_CLIENTS_ON_SENDFAILURE;
    private bool FInitialized;

    [Delphi(PortStatus.Ported)]
    public TBoldPropagatorServer()
    {
        FInitialized = false;
    }

    /// <summary>The server; created the first time.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldPropagatorServer Instance()
    {
        lock (G_Lock)
        {
            return G_PropagatorServer ??= new TBoldPropagatorServer();
        }
    }

    /// <summary>Frees the server (and stops the propagator).</summary>
    [Delphi(PortStatus.Ported)]
    public static void FreeSingleton()
    {
        TBoldPropagatorServer? server;
        lock (G_Lock)
        {
            server = G_PropagatorServer;
        }

        // Disposed while still the instance: threads that ask for the propagator meanwhile find none.
        server?.Dispose();
        lock (G_Lock)
        {
            if (G_PropagatorServer == server)
            {
                G_PropagatorServer = null;
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPropagatorServer.Destroy")]
    public void Dispose()
    {
        TBoldAdvancedPropagator? propagator;
        TBoldLockManager? lockManager;
        lock (fLock)
        {
            propagator = FAdvancedPropagator;
            lockManager = fLockManager;
            FAdvancedPropagator = null;
            fLockManagerAdmin = null;
            fLockManager = null;
            FInitialized = false;
        }

        propagator?.Dispose();
        lockManager?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>The propagator, initialized with the configuration the first time it is asked for.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldAdvancedPropagator? getAdvancedPropagator()
    {
        lock (fLock)
        {
            if (FAdvancedPropagator is not null && !FAdvancedPropagator.Initialized)
            {
                FAdvancedPropagator.SetConfigurationParams(FClientNotifierPoolSize, FEnableLogging, FLogFileName, FErrorLogFileName, fThreadLogFileName,
                    FMaxLogFileSize, fDisconnectClientsOnSendFailure);
                FAdvancedPropagator.Initialize();
            }

            return FAdvancedPropagator;
        }
    }

    [Delphi(PortStatus.Ported)]
    private string getModuleName()
    {
        if (FModuleName == "")
        {
            FModuleName = BoldUtils.GetModuleFileNameAsString(true);
        }

        return FModuleName;
    }

    /// <summary>Reads the configuration and starts the propagator.</summary>
    [Delphi(PortStatus.Ported, Note = "Without COM class factories and the server name switch; the host maps the propagator's hub.")]
    public void Initialize()
    {
        LoadConfiguration();
        lock (fLock)
        {
            FAdvancedPropagator ??= new TBoldAdvancedPropagator();
        }

        _ = AdvancedPropagator;
        FInitialized = true;
    }

    /// <summary>Reads the configuration from <see cref="ConfigFileName"/>; missing values keep their defaults.</summary>
    [Delphi(PortStatus.Ported, Note = "Reads the COM SERVER section's SERVERNAME only; the class ids were COM's.")]
    public void LoadConfiguration()
    {
        Dictionary<string, Dictionary<string, string>> ConfigFile = ReadIniFile(ConfigFileName);
        string Section = "FILE LOGGING";
        FLogFileName = SysUtils.ChangeFileExt(ModuleName, ".log");
        FErrorLogFileName = SysUtils.ChangeFileExt(ModuleName, ".error");
        fThreadLogFileName = ReadBool(ConfigFile, Section, "THREAD", false) ? SysUtils.ChangeFileExt(ModuleName, ".thread") : "";
        FEnableLogging = ReadBool(ConfigFile, Section, "ENABLED", DEFAULT_ENABLELOGGING);
        FMaxLogFileSize = ReadInteger(ConfigFile, Section, "MAXFILESIZE", DEFAULT_LOGFILESIZE);
        Section = "CONFIG PARAMS";
        FClientNotifierPoolSize = ReadInteger(ConfigFile, Section, "THREADPOOLSIZE", DEFAULT_THREADPOOLSIZE);
        fDisconnectClientsOnSendFailure = ReadBool(ConfigFile, Section, "DISCONNECTCLIENTSONSENDFAILURE", DEFAULT_DISCONNECT_CLIENTS_ON_SENDFAILURE);
        Section = "COM SERVER";
        FServerName = ReadString(ConfigFile, Section, "SERVERNAME", "EnterprisePropagator");
    }

    /// <summary>The lock manager, created the first time.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldLockManager getLockManager()
    {
        TBoldAdvancedPropagator propagator = AdvancedPropagator ?? throw new EBold("The propagator server is not initialized.");
        lock (fLock)
        {
            return fLockManager ??= new TBoldLockManager(propagator);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLockManagerAdmin getLockManagerAdmin()
    {
        TBoldLockManager lockManager = LockManager;
        lock (fLock)
        {
            return fLockManagerAdmin ??= new TBoldLockManagerAdmin(lockManager);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldAdvancedPropagator? AdvancedPropagator => getAdvancedPropagator();
    [Delphi(PortStatus.Ported)] public TBoldLockManager LockManager => getLockManager();
    [Delphi(PortStatus.Ported)] public TBoldLockManagerAdmin LockManagerAdmin => getLockManagerAdmin();
    [Delphi(PortStatus.Ported)] public string ModuleName => getModuleName();

    [Delphi(PortStatus.Ported)]
    public bool Initialized
    {
        get => FInitialized;
        set => FInitialized = value;
    }

    [Delphi(PortStatus.Ported)] public string ServerName => FServerName;

    /// <summary>The configuration file: the module name with .ini unless set.</summary>
    public string ConfigFileName
    {
        get => field ?? SysUtils.ChangeFileExt(ModuleName, ".ini");
        set;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadIniFile(string FileName)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(FileName))
        {
            return result;
        }

        Dictionary<string, string>? section = null;
        foreach (string rawLine in File.ReadAllLines(FileName))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                string name = line[1..^1].Trim();
                if (!result.TryGetValue(name, out section))
                {
                    section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[name] = section;
                }
            }
            else if (section is not null && line.IndexOf('=') is var eq and > 0)
            {
                section[line[..eq].Trim()] = line[(eq + 1)..].Trim();
            }
        }

        return result;
    }

    private static string ReadString(Dictionary<string, Dictionary<string, string>> Ini, string Section, string Ident, string Default) =>
        Ini.TryGetValue(Section, out var section) && section.TryGetValue(Ident, out string? value) ? value : Default;

    private static int ReadInteger(Dictionary<string, Dictionary<string, string>> Ini, string Section, string Ident, int Default) =>
        int.TryParse(ReadString(Ini, Section, Ident, ""), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value)
            ? value : Default;

    private static bool ReadBool(Dictionary<string, Dictionary<string, string>> Ini, string Section, string Ident, bool Default) =>
        ReadInteger(Ini, Section, Ident, Default ? 1 : 0) != 0;
}
