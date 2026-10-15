using System.Runtime.CompilerServices;

namespace Bold;

/// <summary>Runs the Delphi units' initialization sections of Bold.ValueSpace when the assembly loads.</summary>
internal static class ValueSpaceModule
{
    // Delphi units register their streamers in initialization sections; a module initializer is the .NET equivalent.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        BoldId.RegisterStreamers();
        BoldDefaultId.RegisterStreamers();
        BoldDefaultXMLStreaming.RegisterStreamers();
        BoldFreeStandingValueFactories.Initialize();
    }
}
