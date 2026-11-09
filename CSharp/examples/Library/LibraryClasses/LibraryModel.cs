using Bold;

namespace LibraryClasses;

/// <summary>The Library model the classes were generated from, embedded in this assembly.</summary>
public static class LibraryModel
{
    /// <summary>The lines of the embedded Library.bld, as a model component reads them (TBoldAbstractModel.ReadModel).</summary>
    public static TStringList Lines()
    {
        using Stream stream = typeof(LibraryModel).Assembly.GetManifestResourceStream("LibraryClasses.Library.bld")!;
        using var reader = new StreamReader(stream);
        return new TStringList { Text = reader.ReadToEnd() };
    }
}
