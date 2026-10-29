// Delphi: Source/Common/Support/BoldRegistry.pas
// Port map: CSharp/port/units/BoldRegistry.md
using System.Runtime.Versioning;

namespace Bold.Tests;

/// <summary>Settings under Bold's key of the current user (Windows only: the CI runs them).</summary>
[Platform(Include = "Win")]
[SupportedOSPlatform("windows")]
public class BoldRegistryTests
{
    [Test]
    [Covers(typeof(TBoldRegistry), "*")]
    [Covers(typeof(TRegistryMode))]
    [Covers(typeof(BoldRegistry), "*")]
    public void Values_are_written_and_read_under_the_mode_s_part_of_Bold_s_key()
    {
        string test = "\\PortTest" + Guid.NewGuid().ToString("N");
        using var registry = new TBoldRegistry();
        Assert.That(registry.RegistryMode, Is.EqualTo(TRegistryMode.rmDesigntime));
        registry.RegistryMode = TRegistryMode.rmRuntime;
        try
        {
            Assert.That(registry.OpenKey(test), Is.True);
            Assert.That(registry.ValueExists("Name"), Is.False);
            Assert.That(registry.ReadString("Name", "none"), Is.EqualTo("none"), "a missing value gives the default");
            Assert.That(registry.ReadInteger("Count", 7), Is.EqualTo(7));
            Assert.That(registry.ReadBool("Flag", true), Is.True);

            registry.WriteString("Name", "Bold");
            registry.WriteInteger("Count", 42);
            registry.WriteBool("Flag", false);
            registry.CloseKey();
            Assert.That(registry.ValueExists("Name"), Is.False, "no key is open");

            Assert.That(registry.OpenNotDefaultKey(BoldUtils.BoldRootRegistryKey() + BoldRegistry.RegPathExtention[1] + test), Is.True);
            Assert.That(registry.ValueExists("name"), Is.True, "names are not case sensitive");
            Assert.That(registry.ReadString("Name", ""), Is.EqualTo("Bold"));
            Assert.That(registry.ReadInteger("Count", 0), Is.EqualTo(42));
            Assert.That(registry.ReadBool("Flag", true), Is.False);
            registry.CloseKey();
        }
        finally
        {
            Assert.That(registry.DeleteKey("\\" + BoldUtils.BoldRootRegistryKey() + BoldRegistry.RegPathExtention[1] + test), Is.True);
        }

        Assert.That(BoldRegistry.RegPathExtention, Is.EqualTo(new[] { "\\Designtime", "\\Runtime" }));
        string basePath = BoldRegistry.GetBoldBasePath();
        Assert.That(BoldRegistry.GetBoldLibPath(), Is.EqualTo(basePath == "" ? "" : basePath + "\\Lib"));
        Assert.That(BoldRegistry.GetBoldResPath(), Is.EqualTo(basePath == "" ? "" : basePath + "\\Res"));
    }
}
