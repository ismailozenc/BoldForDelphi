// Delphi: Source/MoldModel/Handles/BoldTypeNameHandle.pas
// Port map: CSharp/port/units/BoldTypeNameHandle.md

namespace Bold;

/// <summary>A component holding a type name dictionary, filled with the default mappings.</summary>
[Delphi(PortStatus.Ported, Note = "A System.ComponentModel.Component where Delphi has a TComponent.")]
public class TBoldTypeNameHandle : System.ComponentModel.Component
{
    private TBoldTypeNameDictionary FDictionary;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldTypeNameHandle(System.ComponentModel.IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        FDictionary = (TBoldTypeNameDictionary)Activator.CreateInstance(BoldTypeNameDictionary.TBoldCurrentTypeNameDictionaryClass, [this])!;
        FDictionary.AddDefaultMappings();
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldTypeNameHandle.Destroy", Note = "Freed the dictionary; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported, Note = "Copies the mappings, as Delphi's TCollection.Assign: clears this dictionary and adds a copy of each.")]
    private void SetDictionary(TBoldTypeNameDictionary Value)
    {
        if (ReferenceEquals(Value, FDictionary))
        {
            return;
        }

        FDictionary.Clear();
        for (int i = 0; i < Value.Count; i++)
        {
            Value.Mapping(i).AssignTo(FDictionary.Add());
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameDictionary Dictionary
    {
        get => FDictionary;
        set => SetDictionary(value);
    }
}
