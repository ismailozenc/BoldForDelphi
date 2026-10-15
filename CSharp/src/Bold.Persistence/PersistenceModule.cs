using System.Runtime.CompilerServices;

namespace Bold;

/// <summary>Runs the Delphi units' initialization sections of Bold.Persistence when the assembly loads.</summary>
internal static class PersistenceModule
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        BoldUpdatePrecondition.RegisterStreamers();
    }
}
