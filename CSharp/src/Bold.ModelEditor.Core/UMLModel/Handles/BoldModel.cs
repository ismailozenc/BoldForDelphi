// Delphi: Source/UMLModel/Handles/BoldModel.pas
// Port map: CSharp/port/units/BoldModel.md
// The model component of the model editor: a model (TBoldAbstractModel) with its UML model, the UML graph that the
// editor edits. When the UML model has changed, the Mold model is made from it again: flattened and boldified,
// validated, converted, and the UML model left as it was.

using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldModel", Symbol = "initialization", Note = "The section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldModel", Symbol = "finalization", Note = "The list of models goes with the process.")]

namespace Bold;

/// <summary>Where the UML model comes from: the Mold model (none), or a string kept at design time or at run time.</summary>
[Delphi(PortStatus.Ported)]
public enum TUMLModelMode { ummNone, ummDesignTime, ummRunTime }

/// <summary>A model with a UML model to edit; the Mold model follows the UML model.</summary>
[Delphi(PortStatus.Ported, Note = "The UML model is the UML graph, where Delphi had a UML object space (D6, amended for the editor).")]
public class TBoldModel : TBoldAbstractModel
{
    private bool FUMLModelExposed;
    private readonly TBoldUMLBoldify fBoldify = new();
    private TBoldUMLObject? fUMLModel;
    private string fUMLModelAsString = "";
    private TBoldPassthroughSubscriber? fModelChangedSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldModel(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
        BoldModelUnit.TheModelList().AddModel(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModel.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BoldModelUnit.ModelListAssigned)
            {
                BoldModelUnit.TheModelList().RemoveModel(this);
            }

            UnSubscribeToUMLModel();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void AssertDesignTime()
    {
        if (!DesignMode)
        {
            throw new EBold("UMLModel not accessible in run time");
        }
    }

    /// <summary>Gives the UML model a data type for each type of the type name dictionary that it lacks.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureTypes()
    {
        TBoldUMLObject model = EnsuredUMLModel;
        void EnsureDataType(string modelName)
        {
            if (!TBoldUMLSupport.AllNamespaceContents(model).Any(e => e.IsKindOf("GeneralizableElement") && e.name == modelName))
            {
                TBoldUMLObject aType = model.Graph.Create("DataType");
                TBoldUMLSupport.EnsureBoldTaggedValues(aType);
                aType.namespace_ = model;
                aType.name = modelName;
            }
        }

        TBoldMemberTypeList memberTypes = BoldMemberTypeDictionary.BoldMemberTypes();
        TBoldTypeNameDictionary dictionary = TypeNameDictionary;
        for (int i = 0; i < dictionary.Count; i++)
        {
            TBoldTypeNameMapping mapping = dictionary.Mapping(i);
            if (mapping.ModelName == BoldDefs.DEFAULTNAME)
            {
                continue;
            }

            TBoldMemberTypeDescriptor? descriptor = memberTypes.DescriptorByDelphiName(mapping.ExpandedDelphiName);
            if (descriptor is null || descriptor.AbstractionLevel == TBoldAbstractionLevel.alConcrete)
            {
                EnsureDataType(mapping.ModelName);
            }
        }
    }

    /// <summary>The UML model, made when first asked for: from the Mold model, or from the string it was kept as.</summary>
    [Delphi(PortStatus.Ported, Note = "There is no UML metamodel object space to start, as Delphi started one; an empty string gives the UML model of the Mold model, where Delphi gave none.")]
    private void EnsureUMLModel()
    {
        if (fUMLModel is not null)
        {
            return;
        }

        switch (UMLModelMode)
        {
            case TUMLModelMode.ummDesignTime:
                AssertDesignTime();
                UMLModelFromString(fUMLModelAsString);
                break;
            case TUMLModelMode.ummRunTime:
                UMLModelFromString(fUMLModelAsString);
                break;
        }

        if (fUMLModel is null)
        {
            fUMLModel = new TBoldUMLModelGraph().Create("Model");
            TBoldModelConverter.MoldModelToUML(RawMoldModel, fUMLModel);
        }
        else
        {
            // Read from its string, the UML model is more current than the Mold model: exposed and not yet heard, the
            // Mold model is made from it when next asked for (Delphi read the Mold model from the form file too).
            FUMLModelExposed = true;
        }

        fUMLModelAsString = ""; // saving is straight from the UML model
    }

    [Delphi(PortStatus.Ported)]
    private void UMLModelFromString(string ModelAsString)
    {
        if (ModelAsString != "")
        {
            UnSubscribeToUMLModel();
            TBoldUMLObject model = new TBoldUMLModelGraph().Create("Model");
            TUMLModelStreamer.FillSystemFromString(model, ModelAsString);
            fUMLModel = model;
        }
    }

    [Delphi(PortStatus.Ported)]
    private string UMLModelToString()
    {
        EnsureUMLModel();
        return TUMLModelStreamer.SystemAsString(fUMLModel!);
    }

    [Delphi(PortStatus.Replaced, Note = "Form file streaming; UMLModelAsString gives and takes the string.")]
    private void ReadUMLModelAsString(string Value) => fUMLModelAsString = Value;

    [Delphi(PortStatus.Replaced, Note = "Form file streaming; UMLModelAsString gives and takes the string.")]
    private string WriteUMLModelAsString() => fUMLModelAsString = UMLModelToString();

    [Delphi(PortStatus.Replaced, Note = "Form file streaming; UMLModelAsString gives and takes the UML model as a string.")]
    protected new void DefineProperties() => throw new NotSupportedException("There are no form files in .NET; use UMLModelAsString.");

    /// <summary>The UML model as a string (XMI 2.1), as Delphi kept it in the form file when UMLModelMode is not ummNone.</summary>
    public string UMLModelAsString
    {
        get => WriteUMLModelAsString();
        set => ReadUMLModelAsString(value);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUMLObject GetEnsuredUMLModel()
    {
        EnsureUMLModel();
        MarkUMLModelExposed();
        return fUMLModel!;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUMLObject? GetUMLModel()
    {
        if (fUMLModel is not null)
        {
            MarkUMLModelExposed();
        }

        return fUMLModel;
    }

    /// <summary>The UML model changed: the Mold model is made again when next asked for.</summary>
    [Delphi(PortStatus.Ported)]
    private void UMLModelChangedRecieve(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        UnSubscribeToUMLModel();
        SendEvent(this, BoldAbstractModel.beModelChanged);
        UpdateDesigner();
    }

    [Delphi(PortStatus.Ported)]
    protected void SubscribeToUMLModel()
    {
        System.Diagnostics.Debug.Assert(fUMLModel is not null && fModelChangedSubscriber is null);
        fModelChangedSubscriber = new TBoldPassthroughSubscriber(UMLModelChangedRecieve);
        TBoldUMLSupport.SubscribeToEntireModel(fUMLModel!, fModelChangedSubscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected void UnSubscribeToUMLModel()
    {
        fModelChangedSubscriber?.CancelAllSubscriptions();
        fModelChangedSubscriber = null;
    }

    [Delphi(PortStatus.Ported)]
    protected void MarkUMLModelExposed()
    {
        if (!FUMLModelExposed)
        {
            FUMLModelExposed = true;
            SubscribeToUMLModel();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool GetUMLModelMoreCurrent() => UMLModelExposed && fModelChangedSubscriber is null;

    /// <summary>
    /// When the UML model changed: flattens and boldifies it, validates it (the errors go in the tagged value
    /// _BoldInternal.ModelErrors), takes its Mold model, and puts the UML model back as it was.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureMoldModelCurrent()
    {
        if (!UMLModelMoreCurrent)
        {
            return;
        }

        TBoldUMLObject umlModel = fUMLModel!;
        bool flattenState = TBoldUMLSupport.IsFlattened(umlModel);
        bool boldifyState = TBoldUMLBoldify.IsBoldified(umlModel);
        if (!flattenState)
        {
            TBoldUMLSupport.Flatten(umlModel);
        }

        if (!boldifyState)
        {
            Boldify.Boldify(umlModel);
        }

        var errors = new TStringList();
        using (var validator = new TBoldUMLModelValidator(umlModel))
        {
            validator.Validate(TypeNameDictionary);
        }

        foreach (TBoldUMLViolation violation in umlModel.Graph.Violations.Where(v => v.Severity == TSeverity.sError))
        {
            errors.Add(violation.Description);
        }

        TMoldModel moldModel = TBoldModelConverter.UMLModelToMold(umlModel);
        moldModel.SetTV(BOLDINTERALTVPREFIX + TV_MODELERRORS, errors.CommaText);
        SetFromModel(moldModel);
        if (!flattenState)
        {
            TBoldUMLSupport.UnFlatten(umlModel);
        }

        if (!boldifyState)
        {
            TBoldUMLBoldify.UnBoldify(umlModel);
        }

        SubscribeToUMLModel();
    }

    /// <summary>Takes a Mold model made elsewhere as the model; a UML model already made is made again from it.</summary>
    public override void LoadFromMoldModel(TMoldModel Model)
    {
        UnSubscribeToUMLModel();
        SetFromModel(Model);
        if (fUMLModel is not null)
        {
            TBoldModelConverter.MoldModelToUML(Model, fUMLModel);
            if (UMLModelExposed)
            {
                SubscribeToUMLModel();
            }
        }

        SendEvent(this, BoldAbstractModel.beModelChanged);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldify(TBoldUMLBoldify Value) => Boldify.Assign(Value);

    [Delphi(PortStatus.Ported)] protected bool UMLModelExposed => FUMLModelExposed;
    [Delphi(PortStatus.Ported)] protected bool UMLModelMoreCurrent => GetUMLModelMoreCurrent();

    /// <summary>The UML model if it has been made (none otherwise).</summary>
    [Delphi(PortStatus.Ported)] public TBoldUMLObject? UMLModel => GetUMLModel();

    /// <summary>The UML model, made if need be.</summary>
    [Delphi(PortStatus.Ported)] public TBoldUMLObject EnsuredUMLModel => GetEnsuredUMLModel();

    [Delphi(PortStatus.Ported)] public TUMLModelMode UMLModelMode { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldUMLBoldify Boldify
    {
        get => fBoldify;
        set => SetBoldify(value);
    }

    [Delphi(PortStatus.Ported)] public bool EditableModel { get; set; }

    /// <summary>The file the model was read from and is saved to (the code plug-ins write next to it).</summary>
    public string FileName { get; set; } = "";
}

/// <summary>The models there are.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldModelList
{
    private readonly List<TBoldModel> fEntries = [];

    [Delphi(PortStatus.Ported)]
    public TBoldModelList()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelList.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported)] protected internal void AddModel(TBoldModel BoldModel) => fEntries.Add(BoldModel);
    [Delphi(PortStatus.Ported)] protected internal void RemoveModel(TBoldModel BoldModel) => fEntries.Remove(BoldModel);
    [Delphi(PortStatus.Ported)] private TBoldModel GetItem(int Index) => fEntries[Index];
    [Delphi(PortStatus.Ported)] private int GetCount() => fEntries.Count;
    [Delphi(PortStatus.Ported)] public TBoldModel this[int Index] => GetItem(Index);
    [Delphi(PortStatus.Ported)] public int Count => GetCount();
}

/// <summary>Routines of BoldModel.</summary>
public static class BoldModelUnit
{
    private static TBoldModelList? G_ModelList;

    internal static bool ModelListAssigned => G_ModelList is not null;

    [Delphi(PortStatus.Ported, Symbol = "TheModelList")]
    public static TBoldModelList TheModelList() => G_ModelList ??= new TBoldModelList();
}
