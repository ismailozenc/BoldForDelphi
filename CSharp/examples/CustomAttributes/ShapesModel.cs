using Bold;

namespace CustomAttributes;

/// <summary>The Shapes model, embedded in this assembly: a shape with a name, a color, a position and a label.</summary>
public static class ShapesModel
{
    /// <summary>The lines of the embedded Shapes.bld, as a model component reads them (TBoldAbstractModel.ReadModel).</summary>
    public static TStringList Lines()
    {
        using Stream stream = typeof(ShapesModel).Assembly.GetManifestResourceStream("CustomAttributes.Shapes.bld")!;
        using var reader = new StreamReader(stream);
        return new TStringList { Text = reader.ReadToEnd() };
    }

    /// <summary>Adds the custom attribute types to a type name dictionary, as an application adds them to its TypeNameHandle.</summary>
    public static void AddTypeNames(TBoldTypeNameDictionary dictionary)
    {
        TBAColor.AddTypeNameMapping(dictionary);
        TBAPoint.AddTypeNameMapping(dictionary);
        TBAShortString.AddTypeNameMapping(dictionary);
    }
}
