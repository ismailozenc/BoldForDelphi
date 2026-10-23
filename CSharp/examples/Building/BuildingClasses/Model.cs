using Bold;

namespace BuildingClasses;

/// <summary>The model of the Building example, embedded in this assembly.</summary>
public static class BuildingModel
{
    /// <summary>A new copy of the model, read from the embedded Building.bld.</summary>
    public static TMoldModel Load()
    {
        using Stream stream = typeof(BuildingModel).Assembly.GetManifestResourceStream("BuildingClasses.Building.bld")!;
        using var reader = new StreamReader(stream);
        var lines = new TStringList { Text = reader.ReadToEnd() };
        return TMoldBLDRW.StringsToModel(lines);
    }
}
