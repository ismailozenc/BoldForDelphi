// Delphi: Source/Extensions/OLLE/Core/BoldOLLEdmmain.pas
// Port map: CSharp/port/units/BoldOLLEDmmain.md
using System.ComponentModel;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOLLEDmmain", Symbol = "dmOLL", Note = "Delphi's form variable, which nothing set or read: each OLLE controller makes a data module of its own.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldOLLEDmmain", Symbol = "initialization", Note = "Empty, as in Delphi.")]

namespace Bold;

/// <summary>
/// The OLLE data module: the OLLE model, its type information and the system of the information objects, which tell
/// which objects are lent, held and checked out. The OLLE controller gives the system its persistence.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A component that makes its components in code, where Delphi's data module read them from its form file. The model is a TBoldAbstractModel that reads the OLLE model embedded in Bold.Olle, where Delphi's form held it in a TBoldModel. The system handle is not the default: Delphi's form file had IsDefault true (the designer makes the first system handle placed the default), which made the OLLE system the default system of the application.")]
public class TdmOLL : Component
{
    private readonly Container components = new();

    /// <summary>The OLLE model (the classes of DistributableInfo).</summary>
    public readonly TBoldAbstractModel BoldModel1;

    /// <summary>The system of the OLLE information objects.</summary>
    public readonly TBoldSystemHandle BoldObjectInfoSystem;

    /// <summary>The type information of the OLLE model.</summary>
    public readonly TBoldSystemTypeInfoHandle BoldSystemTypeInfoHandle1;

    /// <summary>The data module, in the container when one is given (Delphi's owner).</summary>
    public TdmOLL(IContainer? Owner = null)
    {
        Owner?.Add(this);
        BoldModel1 = new TBoldAbstractModel(components) { Name = "BoldModel1" };
        BoldModel1.ReadModel(ModelLines());
        BoldSystemTypeInfoHandle1 = new TBoldSystemTypeInfoHandle(components) { Name = "BoldSystemTypeInfoHandle1", BoldModel = BoldModel1 };
        BoldObjectInfoSystem = new TBoldSystemHandle(components) { Name = "BoldObjectInfoSystem", SystemTypeInfoHandle = BoldSystemTypeInfoHandle1 };
    }

    /// <summary>The components of the data module; a component added here goes with it.</summary>
    public IContainer Components => components;

    /// <summary>The lines of the OLLE model, DistributableInfo.bld, embedded in Bold.Olle.</summary>
    public static TStringList ModelLines()
    {
        using Stream stream = typeof(TdmOLL).Assembly.GetManifestResourceStream("Bold.Olle.DistributableInfo.bld")
            ?? throw new EBoldInternal("Bold.Olle has no embedded DistributableInfo.bld");
        using var reader = new StreamReader(stream);
        return new TStringList { Text = reader.ReadToEnd() };
    }

    /// <summary>Disposes the components, the last made first (the system before its type information and model).</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }
}
