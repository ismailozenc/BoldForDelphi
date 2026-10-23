// New in the C# port: finds generated code in assemblies. Delphi registers generated code from the initialization
// section of each generated unit (see BoldGeneratedCodeDictionary); .NET has no unit initialization that runs at
// program start, so generated code says where its registration is with BoldGeneratedCodeAttribute.
using System.Reflection;

namespace Bold;

/// <summary>
/// Marks an assembly with generated code: <see cref="Registrar"/> is a static class with a public static Register()
/// that adds the generated classes to <see cref="BoldGeneratedCodeDictionary.GeneratedCodes"/> (once).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BoldGeneratedCodeAttribute(Type registrar) : Attribute
{
    public Type Registrar { get; } = registrar;
}

/// <summary>
/// The generated-code registry: runs the registration of the generated code in the loaded assemblies (and those the
/// application references), so a system can use generated classes without the program touching them first.
/// </summary>
public static class BoldGeneratedCodeRegistry
{
    private static readonly HashSet<Assembly> sRegistered = [];
    private static readonly object sLock = new();

    /// <summary>Registers the generated code of the assembly (once).</summary>
    public static void Register(Assembly assembly)
    {
        lock (sLock)
        {
            if (!sRegistered.Add(assembly))
            {
                return;
            }
        }

        foreach (BoldGeneratedCodeAttribute attribute in assembly.GetCustomAttributes<BoldGeneratedCodeAttribute>())
        {
            MethodInfo register = attribute.Registrar.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes) ??
                throw new EBold(SysUtils.Format("%s has no public static Register()", attribute.Registrar.FullName));
            register.Invoke(null, null);
        }
    }

    /// <summary>
    /// Registers the generated code of every loaded assembly, after loading the assemblies the entry assembly references
    /// (a generated classes assembly the program refers to may not be loaded yet).
    /// </summary>
    public static void EnsureRegistered()
    {
        if (Assembly.GetEntryAssembly() is { } entry)
        {
            foreach (AssemblyName name in entry.GetReferencedAssemblies())
            {
                try
                {
                    Assembly.Load(name);
                }
                catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
                {
                    // An assembly that cannot be loaded has no generated code to use.
                }
            }
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic)
            {
                Register(assembly);
            }
        }
    }
}
