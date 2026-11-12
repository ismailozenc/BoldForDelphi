namespace Bold.PortBrowser;

/// <summary>What the browser remembers between runs: the repository it last showed.</summary>
public static class Settings
{
    private static string File => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bold", "PortBrowser.txt");

    public static string? Repository
    {
        get
        {
            try
            {
                string? remembered = System.IO.File.Exists(File) ? System.IO.File.ReadAllText(File).Trim() : null;
                return string.IsNullOrEmpty(remembered) || !Directory.Exists(remembered) ? null : remembered;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        set
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(File)!);
                System.IO.File.WriteAllText(File, value ?? "");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // not remembered this time
            }
        }
    }
}
