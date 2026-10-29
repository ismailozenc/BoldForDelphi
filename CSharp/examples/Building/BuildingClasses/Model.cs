using Bold;

namespace BuildingClasses;

/// <summary>The model of the Building example, embedded in this assembly.</summary>
public static class BuildingModel
{
    /// <summary>A new copy of the model, read from the embedded Building.bld.</summary>
    public static TMoldModel Load() => TMoldBLDRW.StringsToModel(Lines());

    /// <summary>The lines of the embedded Building.bld, as a model component reads them (TBoldAbstractModel.ReadModel).</summary>
    public static TStringList Lines()
    {
        using Stream stream = typeof(BuildingModel).Assembly.GetManifestResourceStream("BuildingClasses.Building.bld")!;
        using var reader = new StreamReader(stream);
        return new TStringList { Text = reader.ReadToEnd() };
    }
}
