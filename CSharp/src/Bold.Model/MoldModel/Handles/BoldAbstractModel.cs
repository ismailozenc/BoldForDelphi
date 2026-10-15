// Delphi: Source/MoldModel/Handles/BoldAbstractModel.pas
// Port map: CSharp/port/units/BoldAbstractModel.md

namespace Bold;

/// <summary>
/// The model component: holds the model as edited (raw) and a working copy with removed elements trimmed, and the type
/// name dictionary (from a type name handle, or an implicit one with the default mappings).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAbstractModel : TBoldSubscribableComponent
{
    private TMoldModel? FMoldModel;
    private TMoldModel? FRawMoldModel;
    private TBoldTypeNameHandle? FTypeNameHandle;
    private TBoldTypeNameDictionary? fImplicitTypeNameDictionary;
    private string FToBeRemovedInfoFileName = "";

    /// <summary>Starts with an empty model.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldAbstractModel(System.ComponentModel.IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        SetFromModel(null);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractModel.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref FRawMoldModel);
            SysUtils.FreeAndNil(ref FMoldModel);
            fImplicitTypeNameDictionary = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>Loads the model from its .bld lines (the form file's "Model" property in Delphi).</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the model's lines where Delphi takes a form file reader.")]
    public void ReadModel(TStrings ModelAsStrings) => SetFromModel(TMoldBLDRW.StringsToModel(ModelAsStrings));

    /// <summary>Writes the raw model's .bld lines.</summary>
    [Delphi(PortStatus.Ported, Note = "Writes to a string list where Delphi writes to a form file writer.")]
    public void WriteModel(TStrings ModelAsStrings) => TMoldBLDRW.ModelToStrings(RawMoldModel, ModelAsStrings);

    [Delphi(PortStatus.Replaced, Note = "Form file streaming; ReadModel and WriteModel take and give the model's lines.")]
    protected void DefineProperties() => throw new NotSupportedException("TBoldAbstractModel.DefineProperties: there are no form files in .NET; use ReadModel and WriteModel.");

    [Delphi(PortStatus.Replaced, Note = "TComponent free notification; the TypeNameHandle setter is called with null instead.")]
    protected void Notification(object? AComponent)
    {
        if (ReferenceEquals(AComponent, TypeNameHandle))
        {
            TypeNameHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TMoldModel GetMoldModel()
    {
        EnsureMoldModelCurrent();
        return FMoldModel!;
    }

    [Delphi(PortStatus.Ported)]
    private TMoldModel GetRawMoldModel()
    {
        EnsureMoldModelCurrent();
        return FRawMoldModel!;
    }

    /// <summary>Does nothing here; subclasses that load the model lazily override it.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void EnsureMoldModelCurrent()
    {
    }

    /// <summary>
    /// Takes <paramref name="model"/> (an empty model when null) as the raw model, and makes the working model a copy
    /// of it through its .bld text, with removed elements trimmed and the to-be-removed file applied.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void SetFromModel(TMoldModel? model)
    {
        var ModelAsStrings = new TStringList();
        model ??= TMoldBLDRW.StringsToModel(ModelAsStrings);
        SysUtils.FreeAndNil(ref FRawMoldModel);
        SysUtils.FreeAndNil(ref FMoldModel);
        FRawMoldModel = model;
        FRawMoldModel.TypeNameDictionary = TypeNameDictionary;
        TMoldBLDRW.ModelToStrings(model, ModelAsStrings);
        FMoldModel = TMoldBLDRW.StringsToModel(ModelAsStrings);
        FMoldModel.TrimRemoved();
        ReadToBeRemovedInfoFromFile(FMoldModel);
        FMoldModel.TypeNameDictionary = TypeNameDictionary;
    }

    [Delphi(PortStatus.Ported)]
    private void SetTypeNameHandle(TBoldTypeNameHandle? Value)
    {
        fImplicitTypeNameDictionary = null;
        FTypeNameHandle = Value;
        if (FMoldModel is not null)
        {
            FMoldModel.TypeNameDictionary = TypeNameDictionary;
            FRawMoldModel!.TypeNameDictionary = TypeNameDictionary;
        }
    }

    /// <summary>The handle's dictionary, or an implicit one with the default mappings.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldTypeNameDictionary GetTypeNameDictionary()
    {
        if (FTypeNameHandle is not null)
        {
            return FTypeNameHandle.Dictionary;
        }

        if (fImplicitTypeNameDictionary is null)
        {
            fImplicitTypeNameDictionary = (TBoldTypeNameDictionary)Activator.CreateInstance(BoldTypeNameDictionary.TBoldCurrentTypeNameDictionaryClass, [this])!;
            fImplicitTypeNameDictionary.AddDefaultMappings();
        }

        return fImplicitTypeNameDictionary;
    }

    /// <summary>Tells the design environment the model changed; only in design mode, which .NET does not have.</summary>
    [Delphi(PortStatus.Ported, Note = "Does nothing outside a designer; DesignMode stands in for csDesigning.")]
    public void UpdateDesigner()
    {
        if (DesignMode)
        {
            BoldEnvironment.BoldEffectiveEnvironment().UpdateDesigner(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetToBeRemovedInfoFileName(string Value) => FToBeRemovedInfoFileName = Value.Trim();

    /// <summary>
    /// Marks the classes, attributes ("Class.attribute") and associations named in the to-be-removed file, one per line, as
    /// to be removed.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void ReadToBeRemovedInfoFromFile(TMoldModel MoldModel)
    {
        if (ToBeRemovedInfoFileName == "" || !File.Exists(ToBeRemovedInfoFileName))
        {
            return;
        }

        var Info = new TStringList();
        Info.LoadFromFile(ToBeRemovedInfoFileName);
        for (int i = 0; i < Info.Count; i++)
        {
            string line = Info[i];
            int p = line.IndexOf('.');
            if (p >= 0)
            {
                TMoldClass? MoldClass = MoldModel.Classes.ItemsByName(line.Substring(0, p));
                if (MoldClass is not null)
                {
                    TMoldAttribute? MoldAttribute = MoldClass.Attributes.ItemsByName(line.Substring(p + 1));
                    if (MoldAttribute is not null && MoldAttribute.EvolutionState == TBoldEvolutionState.esNormal)
                    {
                        MoldAttribute.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, BoldDefaultTaggedValues.TV_EVOLUTIONSTATE_TOBEREMOVED);
                    }
                }
            }
            else
            {
                MoldModel.Classes.ItemsByName(line)?.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, BoldDefaultTaggedValues.TV_EVOLUTIONSTATE_TOBEREMOVED);
                MoldModel.Associations.ItemsByName(line)?.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, BoldDefaultTaggedValues.TV_EVOLUTIONSTATE_TOBEREMOVED);
            }
        }
    }

    /// <summary>Writes the names of the classes, attributes and associations to be removed, one per line.</summary>
    [Delphi(PortStatus.Ported)]
    public void WriteToBeRemovedInfoToFile(string FileName)
    {
        var Info = new TStringList();
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            TMoldClass MoldClass = MoldModel.Classes[i];
            if (MoldClass.EvolutionState == TBoldEvolutionState.esToBeRemoved)
            {
                Info.Add(MoldClass.Name);
            }

            for (int j = 0; j < MoldClass.Attributes.Count; j++)
            {
                if (MoldClass.Attributes[j].EvolutionState == TBoldEvolutionState.esToBeRemoved)
                {
                    Info.Add(SysUtils.Format("%s.%s", MoldClass.Name, MoldClass.Attributes[j].Name));
                }
            }
        }

        for (int i = 0; i < MoldModel.Associations.Count; i++)
        {
            if (MoldModel.Associations[i].EvolutionState == TBoldEvolutionState.esToBeRemoved)
            {
                Info.Add(MoldModel.Associations[i].Name);
            }
        }

        Info.SaveToFile(FileName);
    }

    [Delphi(PortStatus.Ported)] public TMoldModel MoldModel => GetMoldModel();
    [Delphi(PortStatus.Ported)] public TMoldModel RawMoldModel => GetRawMoldModel();
    [Delphi(PortStatus.Ported)] public TBoldTypeNameDictionary TypeNameDictionary => GetTypeNameDictionary();

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameHandle? TypeNameHandle
    {
        get => FTypeNameHandle;
        set => SetTypeNameHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ToBeRemovedInfoFileName
    {
        get => FToBeRemovedInfoFileName;
        set => SetToBeRemovedInfoFileName(value);
    }
}

/// <summary>Unit-level constants of BoldAbstractModel.</summary>
public static partial class BoldAbstractModel
{
    [Delphi(PortStatus.Ported)] public const int beModelChanged = 25;
}
