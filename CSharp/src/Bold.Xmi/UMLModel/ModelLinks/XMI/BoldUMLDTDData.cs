// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLDTDData.pas
// Port map: CSharp/port/units/BoldUMLDTDData.md

namespace Bold;

/// <summary>
/// The members of each class of the UML 1.3 DTD, as qualified names in the DTD's order: "Foundation.Core.Class" gives
/// "Foundation.Core.ModelElement.name, Foundation.Core.ModelElement.visibility, ...". Delphi read the table from a
/// data module that held it; here it is a resource of the assembly, the same text.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLDTDReader
{
    private static TBoldUMLDTDReader? G_Shared;
    private readonly Dictionary<string, string> fLookup = new(StringComparer.Ordinal);
    private string fCurrentElementName = "";
    private string fCurrentCps = "";

    /// <summary>The reader of the UML 1.3 DTD, made once.</summary>
    public static TBoldUMLDTDReader Shared => G_Shared ??= new TBoldUMLDTDReader();

    /// <summary>The UML 1.3 DTD's table.</summary>
    [Delphi(PortStatus.Ported, Note = "Reads the table from the assembly's resource BoldUMLDTDData.uml13.txt, where Delphi read the data module's template.")]
    public TBoldUMLDTDReader()
    {
        using Stream stream = typeof(TBoldUMLDTDReader).Assembly.GetManifestResourceStream("BoldUMLDTDData.uml13.txt")
            ?? throw new EBold("The UML DTD resource is missing.");
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            int equals = line.IndexOf('=');
            if (equals > 0)
            {
                fLookup[line[..equals]] = line[(equals + 1)..];
            }
        }
    }

    /// <summary>The table of a DTD file.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldUMLDTDReader CreateFromFile(string Name)
    {
        var result = new TBoldUMLDTDReader(true);
        var parser = new TBoldDTDParser
        {
            OnStartElement = result.StartElement,
            OnEndElement = result.EndElement,
            OnFoundCpName = result.FoundCpName,
        };
        parser.Parse(File.ReadAllText(Name));
        return result;
    }

    private TBoldUMLDTDReader(bool Empty)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLDTDReader.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void StartElement(string Name) => fCurrentElementName = Name;

    [Delphi(PortStatus.Ported)]
    private void EndElement()
    {
        if (IsClass(fCurrentElementName))
        {
            fLookup[fCurrentElementName] = fCurrentCps;
        }

        fCurrentElementName = "";
        fCurrentCps = "";
    }

    [Delphi(PortStatus.Ported)]
    private void FoundCpName(string Name) => fCurrentCps += (fCurrentCps != "" ? ", " : "") + Name;

    /// <summary>A class's name starts with a capital after its package path; a member's does not.</summary>
    [Delphi(PortStatus.Ported)]
    private static bool IsClass(string Name)
    {
        int last = Name.LastIndexOf('.');
        return last + 1 < Name.Length && char.IsAsciiLetterUpper(Name[last + 1]);
    }

    /// <summary>The table as name=members lines, sorted.</summary>
    [Delphi(PortStatus.Ported)]
    public void SaveToFile(string Name) =>
        File.WriteAllLines(Name, fLookup.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value));

    /// <summary>The qualified names of a class's members, comma separated; empty for a class the DTD does not have.</summary>
    [Delphi(PortStatus.Ported)]
    public string MembersOfClass(string ClassName) => fLookup.GetValueOrDefault(ClassName, "");

    /// <summary>The DTD's classes.</summary>
    public IEnumerable<string> ClassNames => fLookup.Keys;
}

/// <summary>Delphi's data module that held the table, as a template.</summary>
[Delphi(PortStatus.Replaced, Note = "The table is a resource of the assembly (BoldUMLDTDData.uml13.txt), read by TBoldUMLDTDReader.")]
public static class TdmUML13DTD
{
    /// <summary>The table's text.</summary>
    public static string UML13DTD
    {
        get
        {
            using Stream stream = typeof(TBoldUMLDTDReader).Assembly.GetManifestResourceStream("BoldUMLDTDData.uml13.txt")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

/// <summary>Variables of BoldUMLDTDData.</summary>
public static class BoldUMLDTDData
{
    [Delphi(PortStatus.Dropped, Note = "The data module's global instance; the table is read from the resource when wanted.")]
    public static object? dmUML13DTD => null;
}
