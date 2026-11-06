// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
// The model editor's work on the model, without WinForms: the element on show and its page, the commands that add and
// delete elements, cut, copy and paste, dragging and dropping, overriding methods, boldifying and flattening, files,
// plug-ins and whether the model needs validating. The form, TBoldModelEditFrm in Bold.ModelEditor, shows the tree and
// the pages and calls this; the fields of the pages are in BoldUMLModelEditForm.Pages.cs.

using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm.QueryInterface", Note = "The controller implements IUMLModelPlugInContext as a C# interface; there is no COM QueryInterface.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm._AddRef", Note = "C# interfaces are not reference counted.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm._Release", Note = "C# interfaces are not reference counted.")]

namespace Bold;

/// <summary>Whether the element to paste was cut or copied.</summary>
[Delphi(PortStatus.Ported)]
public enum CutOrCopyKind { cckCut, cckCopy, cckNone }

/// <summary>The pages of the editor, one for each kind of element.</summary>
public enum TBoldModelEditPage { None, Model, Class, Attribute, Qualifier, Operation, Association, AssociationEnd, Parameter, Package, DataType }

/// <summary>What the tree's menu offers for the element on show, by the names of Delphi's menu items.</summary>
public sealed class TBoldModelEditCommands
{
    /// <summary>The caption of the delete command, or null when the element is not deleted from the menu.</summary>
    public string? DeleteXxx { get; set; }

    public bool N10 { get; set; }

    public bool NewPackage { get; set; }

    public bool NewClass { get; set; }

    public bool NewAssociation { get; set; }

    public bool NewDatatype { get; set; }

    public bool AddSubclass { get; set; }

    public bool InsertSuperclass { get; set; }

    public bool NewAttribute { get; set; }

    public bool NewOperation { get; set; }

    public bool NewParameter { get; set; }

    public bool NewQualifier { get; set; }

    public bool N9 { get; set; }

    public bool OverrideFrameworkMethods { get; set; }

    public bool OverrideModelMethods { get; set; }

    public bool OverrideInAllSubclasses { get; set; }
}

/// <summary>
/// The model editor without its form: the model component it edits, the element on show, and the commands of the
/// menus and the tree. It is the plug-ins' context, as Delphi's form was.
/// </summary>
public class TBoldModelEditController : IUMLModelPlugInContext, IDisposable
{
    /// <summary>The framework methods a class can override, as Delphi declares them.</summary>
    [Delphi(PortStatus.Ported, Symbol = "FrameworkMethods", Note = "The Delphi signatures; C++ Builder's edition had its own, which are not kept.")]
    public static readonly string[] FrameworkMethods =
    [
        "function GetStringRepresentation(Representation: TBoldRepresentation): string; virtual;",
        "procedure SetStringRepresentation(Representation: TBoldRepresentation; Value: string); virtual;",
        "procedure SubscribeToStringRepresentation(Representation: TBoldRepresentation; Subscriber: TBoldSubscriber; RequestedEvent: TBoldEvent); virtual;",
        "function ValidateCharacter(C: AnsiChar; Representation: TBoldRepresentation): Boolean; virtual;",
        "function ValidateString(Value: string; Representation: TBoldRepresentation): Boolean; virtual;",
        "function CompareToAs(CompareType: TBoldCompareType; BoldDirectElement: TBoldElement): Integer; virtual;",
        "procedure ReceiveEventFromOwned(Originator: TObject; OriginalEvent: TBoldEvent); virtual;",
        "function ReceiveQueryFromOwned(Originator: TObject; OriginalEvent: TBoldEvent; const Args: array of const; Subscriber: TBoldSubscriber): Boolean; virtual;",
        "procedure Assign(Source: TBoldElement); virtual;",
        "procedure CompleteCreate; virtual;",
        "function MayDelete: Boolean; virtual;",
        "function MayUpdate: Boolean; virtual;",
        "procedure PrepareDelete; virtual;",
    ];

    private readonly TBoldPassthroughSubscriber fModelChangedSubscriber;
    private readonly List<TBoldUMLObject> ModelMethods = [];
    private TBoldModel? fModelHandle;
    private TBoldUMLObject? fTreeRoot;
    private TBoldUMLObject? fCurrentElement;
    private TBoldUMLObject? fUMLObjectToCopyOrMove;
    private bool fModelSubscriptionsValid;
    private bool fIgnoreModelChanges;
    private bool fIsExecutingPlugin;
    private bool fModelNeedsValidation;
    private bool fIsAdvancedMode;

    public TBoldModelEditController()
    {
        fModelChangedSubscriber = new TBoldPassthroughSubscriber(ModelChangedRecieve);
    }

    /// <summary>Raised when another element is on show.</summary>
    public event Action? CurrentElementChanged;

    /// <summary>Raised when an element of the model changes, is made or is deleted, for the form to show it.</summary>
    public event Action<TBoldUMLObject>? ModelChanged;

    /// <summary>Raised when the status changes: whether the model needs validating, and what validating found.</summary>
    public event Action? StatusChanged;

    /// <summary>Raised when the advanced fields are shown or hidden.</summary>
    public event Action? AdvancedModeChanged;

    /// <summary>Whether the element to paste was cut or copied.</summary>
    public CutOrCopyKind CutOrCopy { get; set; } = CutOrCopyKind.cckNone;

    /// <summary>The page of the element on show.</summary>
    public TBoldModelEditPage CurrentPage { get; private set; }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ModelHandle")]
    public TBoldModel? ModelHandle
    {
        get => fModelHandle;
        set => SetModelHandle(value!);
    }

    /// <summary>The element whose page is on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CurrentElement")]
    public TBoldUMLObject? CurrentElement
    {
        get => GetCurrentElement();
        set => SetCurrentElement(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CurrentModel")]
    public TBoldUMLObject? CurrentModel => GetCurrentModel();

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CurrentModelIsBoldified")]
    public bool CurrentModelIsBoldified => GetCurrentModelIsBoldified();

    /// <summary>The element cut or copied, to paste.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.UMLObjectToCopyOrMove")]
    public TBoldUMLObject? UMLObjectToCopyOrMove
    {
        get => GetUMLObjectToCopyOrMove();
        set => SetUMLObjectToCopyOrMove(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.IsAdvancedMode")]
    public bool IsAdvancedMode
    {
        get => fIsAdvancedMode;
        set => SetIsAdvancedMode(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ModelNeedsValidation")]
    public bool ModelNeedsValidation
    {
        get => fModelNeedsValidation;
        set => SetModelNeedsValidation(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.IgnoreModelChanges")]
    public bool IgnoreModelChanges
    {
        get => fIgnoreModelChanges;
        set => SetIgnoreModelChanges(value);
    }

    public bool IsExecutingPlugin
    {
        get => GetIsExecutingPlugin();
        set => SetIsExecutingPlugin(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CurrentTypeNameDictionary")]
    public TBoldTypeNameDictionary? CurrentTypeNameDictionary => GetCurrentTypeNameDictionary();

    /// <summary>The model's root: the model component's UML model.</summary>
    public TBoldUMLObject? TreeRoot => fTreeRoot;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetModelHandle", Note = "The form makes the validation form for the model after setting it.")]
    private void SetModelHandle(TBoldModel Value)
    {
        UnhookModel();
        fModelHandle = Value;
        fTreeRoot = Value.EnsuredUMLModel;
        TBoldUMLObject model = CurrentModel ?? throw new EBoldInternal($"{GetType().Name}.SetRoot: Root must be part of Model");
        EnsureTypesFromTypeNameHandle();
        TBoldUMLSupport.EnsureBoldTaggedValues(model);
        fModelNeedsValidation = false;
        ModelNeedsValidation = true;
        model.Graph.ModelValidated += ModelValidatedReceive;
        model.Graph.Changed += GraphChanged;
        CurrentElement = model;
    }

    private void UnhookModel()
    {
        if (fTreeRoot is not null)
        {
            fTreeRoot.Graph.ModelValidated -= ModelValidatedReceive;
            fTreeRoot.Graph.Changed -= GraphChanged;
        }

        fModelChangedSubscriber.CancelAllSubscriptions();
        fModelSubscriptionsValid = false;
    }

    private void GraphChanged(TBoldUMLObject Element) => ModelChanged?.Invoke(Element);

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentModelHandle")]
    public TBoldModel GetCurrentModelHandle() => fModelHandle!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentElement")]
    public TBoldUMLObject? GetCurrentElement() => fCurrentElement;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentModel")]
    protected TBoldUMLObject? GetCurrentModel() => fTreeRoot?.model;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetIsExecutingPlugin")]
    public bool GetIsExecutingPlugin() => fIsExecutingPlugin;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetIsExecutingPlugin")]
    public void SetIsExecutingPlugin(bool Value) => fIsExecutingPlugin = Value;

    /// <summary>Shows the element: its page is the page of its kind.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetCurrentElement", Note = "Chooses the page; the form shows it, after applying what is being edited.")]
    private void SetCurrentElement(TBoldUMLObject? Element)
    {
        fCurrentElement = Element;
        CurrentPage = PageFor(Element);
        CurrentElementChanged?.Invoke();
    }

    /// <summary>The page of an element; an attribute without an owner is a qualifier.</summary>
    public static TBoldModelEditPage PageFor(TBoldUMLObject? Element) => Element switch
    {
        null => TBoldModelEditPage.None,
        _ when Element.IsKindOf("Model") => TBoldModelEditPage.Model,
        _ when Element.IsKindOf("Class") => TBoldModelEditPage.Class,
        _ when Element.IsKindOf("Attribute") => Element.owner is null ? TBoldModelEditPage.Qualifier : TBoldModelEditPage.Attribute,
        _ when Element.IsKindOf("Operation") => TBoldModelEditPage.Operation,
        _ when Element.IsKindOf("Association") => TBoldModelEditPage.Association,
        _ when Element.IsKindOf("AssociationEnd") => TBoldModelEditPage.AssociationEnd,
        _ when Element.IsKindOf("Parameter") => TBoldModelEditPage.Parameter,
        _ when Element.IsKindOf("Package") => TBoldModelEditPage.Package,
        _ when Element.IsKindOf("DataType") => TBoldModelEditPage.DataType,
        _ => TBoldModelEditPage.None,
    };

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetIsAdvancedMode")]
    private void SetIsAdvancedMode(bool advanced)
    {
        if (fIsAdvancedMode != advanced)
        {
            fIsAdvancedMode = advanced;
            AdvancedModeChanged?.Invoke();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentModelIsBoldified")]
    private bool GetCurrentModelIsBoldified() => CurrentModel is { } model && TBoldUMLBoldify.IsBoldified(model);

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetUMLObjectToCopyOrMove")]
    private TBoldUMLObject? GetUMLObjectToCopyOrMove() => fUMLObjectToCopyOrMove;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetUMLObjectToCopyOrMove")]
    private void SetUMLObjectToCopyOrMove(TBoldUMLObject? Value) => fUMLObjectToCopyOrMove = Value;

    /// <summary>The class on show, or the class of the feature on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentClass")]
    public TBoldUMLObject? GetCurrentClass() => CurrentElement switch
    {
        { } e when e.IsKindOf("Class") => e,
        { } e when e.IsKindOf("Feature") && e.owner is { } owner && owner.IsKindOf("Class") => owner,
        _ => null,
    };

    /// <summary>The namespace new classes go to: the package on show, or that of the element on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentPackage", Note = "Also gives the namespace of an operation's class, where Delphi gave none and the menu's New Class failed.")]
    public TBoldUMLObject? GetCurrentPackage() => CurrentElement switch
    {
        { } e when e.IsKindOf("Class") => e.namespace_,
        { } e when e.IsKindOf("Association") => e.namespace_,
        { } e when e.IsKindOf("Feature") => e.owner?.namespace_,
        { } e when e.IsKindOf("Namespace") => e,
        _ => null,
    };

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCurrentTypeNameDictionary")]
    private TBoldTypeNameDictionary? GetCurrentTypeNameDictionary() => ModelHandle?.TypeNameDictionary;

    /// <summary>What the menus offer for the element on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.popTreePopup", Note = "Says what the menus offer; the form shows the items and builds the override menus.")]
    public TBoldModelEditCommands Commands()
    {
        var c = new TBoldModelEditCommands();
        TBoldUMLObject? e = CurrentElement;
        if (e is null)
        {
            return c;
        }

        if (e.IsKindOf("Package"))
        {
            if (!e.IsKindOf("Model"))
            {
                c.DeleteXxx = "Delete Package " + e.name;
            }

            c.NewClass = c.NewAssociation = c.NewDatatype = c.NewPackage = c.N9 = true;
        }
        else if (e.IsKindOf("Class"))
        {
            c.DeleteXxx = "Delete Class " + e.name;
            c.N10 = c.NewClass = c.InsertSuperclass = c.AddSubclass = c.NewAttribute = c.NewOperation = c.N9 = true;
            c.OverrideFrameworkMethods = c.OverrideModelMethods = true;
        }
        else if (e.IsKindOf("Attribute"))
        {
            c.DeleteXxx = "Delete Attribute " + e.name;
            if (e.owner is not null) // a qualifier has none
            {
                c.NewAttribute = c.AddSubclass = c.NewOperation = c.NewClass = true;
            }
        }
        else if (e.IsKindOf("Operation"))
        {
            c.DeleteXxx = "Delete Operation " + e.name;
            c.N10 = c.NewAttribute = c.AddSubclass = c.NewOperation = c.NewParameter = c.NewClass = true;
            string kind = e.GetBoldTV(TAG_DELPHIOPERATIONKIND);
            c.OverrideInAllSubclasses = e.owner is { } owner && owner.SubClasses.Count > 0 &&
                (kind == TV_DELPHIOPERATIONKIND_VIRTUAL || kind == TV_DELPHIOPERATIONKIND_DYNAMIC || kind == TV_DELPHIOPERATIONKIND_ABSTRACTVIRTUAL);
        }
        else if (e.IsKindOf("Association"))
        {
            c.DeleteXxx = "Delete Association " + e.name;
        }
        else if (e.IsKindOf("AssociationEnd"))
        {
            c.NewQualifier = true;
        }
        else if (e.IsKindOf("Parameter"))
        {
            c.DeleteXxx = "Delete Parameter " + e.name;
        }

        return c;
    }

    /// <summary>Deletes the element on show, but not the root class of a boldified model, an association end or the model.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DeleteXxx1Click", Note = "The form does not delete while a node is being edited. The model is not deleted, which Delphi's Delete key did.")]
    public void DeleteXxx()
    {
        TBoldUMLObject? e = CurrentElement;
        if (e is null || e.IsKindOf("Model"))
        {
            return;
        }

        TBoldUMLObject? next = e.qualifyingOwner;
        if (e.IsKindOf("Class"))
        {
            if (CurrentModelIsBoldified && TBoldUMLBoldify.GetRootClass(CurrentModel!) == e)
            {
                return;
            }

            TBoldUMLSupport.RelinkSpecializations(e);
            e.Delete();
        }
        else if (!e.IsKindOf("AssociationEnd"))
        {
            e.Delete();
        }

        if (e.Deleted)
        {
            CurrentElement = next;
        }
    }

    private static TBoldUMLObject NewElement(TBoldUMLModelGraph Graph, string MetaClass)
    {
        TBoldUMLObject element = Graph.Create(MetaClass);
        TBoldUMLSupport.EnsureBoldTaggedValues(element);
        return element;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewClass1Click", Note = "Returns the class, for the form to show it.")]
    public TBoldUMLObject NewClass()
    {
        TBoldUMLObject NameSpace = GetCurrentPackage() ?? CurrentModel!;
        TBoldUMLObject NewClass = NewElement(NameSpace.Graph, "Class");
        NewClass.namespace_ = NameSpace;
        NewClass.name = TBoldUMLSupport.UniqueName(NewClass, "NewClass");
        if (CurrentModelIsBoldified)
        {
            NewClass.SetFirstParent(TBoldUMLBoldify.GetRootClass(CurrentModel!));
        }

        NewClass.persistent = true;
        return NewClass;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewAssociation1Click", Note = "Returns the association, for the form to show it.")]
    public TBoldUMLObject NewAssociation()
    {
        TBoldUMLObject NameSpace = CurrentElement!;
        TBoldUMLObject NewAssociation = NewElement(NameSpace.Graph, "Association");
        NewAssociation.namespace_ = NameSpace;
        NewAssociation.name = TBoldUMLSupport.UniqueName(NewAssociation, "NewAssociation");
        NewAssociation.persistent = true;
        for (int i = 0; i <= 1; i++)
        {
            TBoldUMLObject NewAssociationEnd = NewElement(NameSpace.Graph, "AssociationEnd");
            NewAssociation.AddRef("connection", NewAssociationEnd);
            NewAssociationEnd.isNavigable = true;
            NewAssociationEnd.name = $"AssociationEnd{i}";
        }

        return NewAssociation;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewAttribute1Click", Note = "Returns the attribute, for the form to show it.")]
    public TBoldUMLObject NewAttribute()
    {
        TBoldUMLObject Class_ = GetCurrentClass()!;
        TBoldUMLObject NewAttribute = NewElement(Class_.Graph, "Attribute");
        Class_.AddRef("feature", NewAttribute);
        NewAttribute.name = TBoldUMLSupport.UniqueName(NewAttribute, "NewAttribute");
        NewAttribute.typeName = "String";
        NewAttribute.persistent = Class_.persistent;
        return NewAttribute;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewOperation1Click", Note = "Returns the operation, for the form to show it.")]
    public TBoldUMLObject NewOperation()
    {
        TBoldUMLObject Class_ = GetCurrentClass()!;
        TBoldUMLObject NewOperation = NewElement(Class_.Graph, "Operation");
        Class_.AddRef("feature", NewOperation);
        NewOperation.name = TBoldUMLSupport.UniqueName(NewOperation, "NewOperation");
        return NewOperation;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewParameter1Click", Note = "Returns the parameter, for the form to show it.")]
    public TBoldUMLObject NewParameter()
    {
        TBoldUMLObject BehavioralFeature = CurrentElement!;
        TBoldUMLObject NewParameter = NewElement(BehavioralFeature.Graph, "Parameter");
        BehavioralFeature.AddRef("parameter", NewParameter);
        NewParameter.name = TBoldUMLSupport.UniqueName(NewParameter, "NewParameter");
        return NewParameter;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewQualifier1Click", Note = "Returns the qualifier, for the form to show it.")]
    public TBoldUMLObject NewQualifier()
    {
        TBoldUMLObject AssociationEnd = CurrentElement!;
        TBoldUMLObject NewQualifier = NewElement(AssociationEnd.Graph, "Attribute");
        AssociationEnd.AddRef("qualifier", NewQualifier);
        NewQualifier.name = TBoldUMLSupport.UniqueName(NewQualifier, "NewQualifier");
        return NewQualifier;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewDatatype1Click", Note = "Returns the data type, for the form to show it.")]
    public TBoldUMLObject NewDatatype()
    {
        TBoldUMLObject NameSpace = CurrentElement is { } e && e.IsKindOf("Package") ? e : CurrentModel!;
        TBoldUMLObject NewDataType = NewElement(NameSpace.Graph, "DataType");
        NewDataType.namespace_ = NameSpace;
        NewDataType.name = TBoldUMLSupport.UniqueName(NewDataType, "NewDataType");
        NewDataType.persistent = true;
        return NewDataType;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.NewPackage1Click", Note = "Returns the package, for the form to show it.")]
    public TBoldUMLObject NewPackage()
    {
        TBoldUMLObject NameSpace = CurrentElement!;
        TBoldUMLObject NewPackage = NewElement(NameSpace.Graph, "Package");
        NewPackage.namespace_ = NameSpace;
        NewPackage.name = TBoldUMLSupport.UniqueName(NewPackage, "NewPackage");
        return NewPackage;
    }

    /// <summary>Puts a new class between the class on show and its superclass.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.InsertSuperclass1Click", Note = "Returns the class, for the form to show it.")]
    public TBoldUMLObject InsertSuperclass()
    {
        TBoldUMLObject current = GetCurrentClass()!;
        TBoldUMLObject NameSpace = CurrentElement!.namespace_!;
        TBoldUMLObject NewClass = NewElement(NameSpace.Graph, "Class");
        NewClass.namespace_ = NameSpace;
        NewClass.name = TBoldUMLSupport.UniqueName(NewClass, CurrentElement.name);
        NewClass.SetFirstParent(current.Superclass);
        current.SetFirstParent(NewClass);
        NewClass.persistent = true;
        return NewClass;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.AddSubclass1Click", Note = "Returns the class, for the form to show it.")]
    public TBoldUMLObject AddSubclass()
    {
        TBoldUMLObject SuperClass = GetCurrentClass() ?? throw new Exception("Unable to understand which class to use as superclass");
        TBoldUMLObject NewClass = NewElement(SuperClass.Graph, "Class");
        NewClass.namespace_ = SuperClass.namespace_;
        NewClass.name = TBoldUMLSupport.UniqueName(NewClass, SuperClass.name);
        NewClass.SetFirstParent(SuperClass);
        NewClass.persistent = true;
        return NewClass;
    }

    /// <summary>Removes everything from the model, when the user says so.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.Clear1Click", Note = "Clears as Delphi's TUMLModel.Clear did.")]
    public void Clear()
    {
        if (CurrentModel is { } model &&
            Dialogs.MessageDlg("This will remove everything from your model, are you sure?", TMsgDlgType.mtConfirmation,
                DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == Controls.mrYes)
        {
            ClearModel(model);
            CurrentElement = model;
        }
    }

    private static void ClearModel(TBoldUMLObject Model)
    {
        while (Model.ownedElement.Count > 0)
        {
            Model.ownedElement[^1].Delete();
        }

        while (Model.TaggedValues.Count > 0)
        {
            Model.TaggedValues[^1].Delete();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.EnsureTypesFromTypeNameHandle", Note = "The form also clears the attribute's type when it is no longer a type of the model.")]
    public void EnsureTypesFromTypeNameHandle()
    {
        IgnoreModelChanges = true;
        try
        {
            ModelHandle?.EnsureTypes();
        }
        finally
        {
            IgnoreModelChanges = false;
        }
    }

    /// <summary>The values of the lists the pages choose from: table mappings, attribute kinds, property access, function types and delete actions.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PopulateComboBoxes", Note = "Gives the values; the pages show them.")]
    public static Dictionary<string, string[]> PopulateComboBoxes()
    {
        static string[] Values(Action<TStrings> add)
        {
            var strings = new TStringList();
            add(strings);
            return [.. Enumerable.Range(0, strings.Count).Select(i => strings[i])];
        }

        return new Dictionary<string, string[]>
        {
            ["bvhTableMapping"] = Values(TBoldTaggedValueSupport.AddTableMappings),
            ["bvhAttributeKind"] = Values(TBoldTaggedValueSupport.AddAttributeKinds),
            ["bvhDelphiProperty"] = Values(TBoldTaggedValueSupport.AddPropertyAccessKinds),
            ["bvhDelphiFunctionType"] = Values(TBoldTaggedValueSupport.AddDelphiFunctionTypes),
            ["bvhDeleteActions"] = Values(TBoldTaggedValueSupport.AddDeleteActions),
            ["bvhMultiplicityValues"] = ["0..1", "1..1", "0..*", "1..*"],
            ["bvhOptimisticLocking"] = [BoldDefs.DEFAULTNAME, "Off", "ModifiedMembers", "AllMembers", "TimeStamp"],
        };
    }

    /// <summary>The model changed: it needs validating again.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ModelChangedRecieve")]
    private void ModelChangedRecieve(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        fModelSubscriptionsValid = false;
        if (!fIgnoreModelChanges)
        {
            ModelNeedsValidation = true;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ModelValidatedReceive", Note = "Hears the UML graph's ModelValidated event, where Delphi subscribed to beModelValidated.")]
    private void ModelValidatedReceive()
    {
        ModelNeedsValidation = false;
        StatusChanged?.Invoke();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SubscribeToModelChanges")]
    private void SubscribeToModelChanges()
    {
        if (!fModelSubscriptionsValid)
        {
            fModelChangedSubscriber.CancelAllSubscriptions();
            if (!ModelNeedsValidation && CurrentModel is { } model)
            {
                TBoldUMLSupport.SubscribeToEntireModel(model, fModelChangedSubscriber);
                fModelSubscriptionsValid = true;
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetModelNeedsValidation", Note = "Raises StatusChanged for the form to update its status bar.")]
    private void SetModelNeedsValidation(bool Value)
    {
        if (Value != fModelNeedsValidation)
        {
            fModelNeedsValidation = Value;
            StatusChanged?.Invoke();
            SubscribeToModelChanges();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetIgnoreModelChanges")]
    private void SetIgnoreModelChanges(bool Value)
    {
        if (Value != fIgnoreModelChanges)
        {
            fIgnoreModelChanges = Value;
            if (Value)
            {
                fModelChangedSubscriber.CancelAllSubscriptions();
                fModelSubscriptionsValid = false;
            }
            else
            {
                SubscribeToModelChanges();
            }
        }
    }

    /// <summary>What the status bar says about the model.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.UpdateStatusbar", Note = "Gives the text and the severity; the form shows them.")]
    public (string Text, TSeverity Severity) StatusText()
    {
        TSeverity HighestSeverity = CurrentModel?.Graph.HighestSeverity() ?? TSeverity.sNone;
        string InfoText = HighestSeverity switch
        {
            TSeverity.sHint => "There are Hints in this model, Select \"Tools\"->\"Check Consistency\" for further details.",
            TSeverity.sWarning => "There are Warnings in this model, Select \"Tools\"->\"Check Consistency\" for further details.",
            TSeverity.sError => "There are Errors in this model, Select \"Tools\"->\"Check Consistency\" for further details.",
            _ => ModelNeedsValidation ? "Model not validated" : "No errors in model",
        };
        return (InfoText, HighestSeverity);
    }

    /// <summary>
    /// Runs a change of the whole model. If it fails, the model is read back as it was, as Delphi rolled back the UML
    /// object space's transaction.
    /// </summary>
    internal void Transaction(Action Change, bool Reraise)
    {
        TBoldUMLObject model = CurrentModel!;
        string before = TUMLModelStreamer.SystemAsString(model);
        try
        {
            Change();
        }
        catch (Exception)
        {
            ClearModel(model);
            TUMLModelStreamer.FillSystemFromString(model, before);
            if (Reraise)
            {
                throw;
            }
        }
    }

    /// <summary>Boldifies the model, or unboldifies a boldified one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.Boldifymodel1Click", Note = "A failure puts the model back as it was (see Transaction).")]
    public void Boldifymodel()
    {
        IgnoreModelChanges = true;
        try
        {
            Transaction(
                () =>
                {
                    if (TBoldUMLBoldify.IsBoldified(CurrentModel!))
                    {
                        TBoldUMLBoldify.UnBoldify(CurrentModel!);
                    }
                    else
                    {
                        ModelHandle!.Boldify.Boldify(CurrentModel!);
                    }
                },
                false);
        }
        finally
        {
            IgnoreModelChanges = false;
        }
    }

    /// <summary>Flattens the package structure, or unflattens a flattened one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.FlattenClick", Note = "A failure puts the model back as it was (see Transaction).")]
    public void Flatten()
    {
        IgnoreModelChanges = true;
        try
        {
            Transaction(
                () =>
                {
                    if (TBoldUMLSupport.IsFlattened(CurrentModel!))
                    {
                        TBoldUMLSupport.UnFlatten(CurrentModel!);
                    }
                    else
                    {
                        TBoldUMLSupport.Flatten(CurrentModel!);
                    }
                },
                false);
        }
        finally
        {
            IgnoreModelChanges = false;
        }
    }

    /// <summary>The captions of the boldify and flatten commands, which undo what was done.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SetBoldifyFlattenCaptions", Note = "Gives the captions; the form sets them.")]
    public (string Boldify, string Flatten)? BoldifyFlattenCaptions()
    {
        if (CurrentModel is not { } model)
        {
            return null;
        }

        return (TBoldUMLBoldify.IsBoldified(model) ? "UnBoldify model" : "Boldify model",
            TBoldUMLSupport.IsFlattened(model) ? "UnFlatten package structure" : "Flatten package structure");
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.EnsureFlattenedAndBoldified", Note = "A failure puts the model back as it was and is raised again.")]
    public void EnsureFlattenedAndBoldified()
    {
        if (CurrentModel is not { } model)
        {
            return;
        }

        TBoldUMLBoldify Boldifier = ModelHandle!.Boldify;
        if (!TBoldUMLSupport.IsFlattened(model) || !TBoldUMLBoldify.IsBoldified(model))
        {
            IgnoreModelChanges = true;
            try
            {
                Transaction(
                    () =>
                    {
                        if (!TBoldUMLSupport.IsFlattened(model))
                        {
                            TBoldUMLSupport.Flatten(model);
                        }

                        if (!TBoldUMLBoldify.IsBoldified(model))
                        {
                            Boldifier.Boldify(model);
                        }
                    },
                    true);
            }
            finally
            {
                IgnoreModelChanges = false;
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.EnsureUnFlattenedAndUnBoldified", Note = "A failure puts the model back as it was and is raised again.")]
    public void EnsureUnFlattenedAndUnBoldified()
    {
        if (CurrentModel is not { } model)
        {
            return;
        }

        IgnoreModelChanges = true;
        try
        {
            Transaction(
                () =>
                {
                    if (TBoldUMLSupport.IsFlattened(model))
                    {
                        TBoldUMLSupport.UnFlatten(model);
                    }

                    if (TBoldUMLBoldify.IsBoldified(model))
                    {
                        TBoldUMLBoldify.UnBoldify(model);
                    }
                },
                true);
        }
        finally
        {
            IgnoreModelChanges = false;
        }
    }

    /// <summary>Runs a plug-in, with the model boldified or unboldified as it needs.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ExecPlugIn", Note = "The form applies what is being edited and shows the wait cursor.")]
    public void ExecPlugIn(TUMLPlugIn PlugIn)
    {
        if (PlugIn.Options.Contains(TBoldUMLPluginOption.poRequireBoldified))
        {
            EnsureFlattenedAndBoldified();
        }

        if (PlugIn.Options.Contains(TBoldUMLPluginOption.poRequireUnBoldified))
        {
            EnsureUnFlattenedAndUnBoldified();
        }

        PlugIn.GuardedExecute(this);
    }

    /// <summary>The link for a file, by its extension.</summary>
    public static TBoldUMLModelLinkClass? LinkForFile(string FileName) =>
        BoldUMLModelLinkUnit.BoldUMLModelLinkList().LinkClasses(-1, Path.GetExtension(FileName));

    /// <summary>The file filter of the open dialog: the links that read, without "All files".</summary>
    public static string OpenFileFilter()
    {
        string filter = BoldUMLModelLinkUnit.BoldUMLModelLinkList().FileFilter;
        return filter[..^BoldDefs.ALLFILESFILTER.Length].TrimEnd('|');
    }

    /// <summary>The file filter of the save dialog: the links that write, without "All files".</summary>
    public static string SaveFileFilter()
    {
        string filter = BoldUMLModelLinkUnit.BoldUMLModelLinkList().ExportableFileFilter;
        return filter[..^BoldDefs.ALLFILESFILTER.Length].TrimEnd('|');
    }

    /// <summary>Reads the model from a file through the link of its extension.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.OpenFile1Click", Note = "Takes the file the form's dialog chose.")]
    public bool OpenFile(string FileName)
    {
        if (fIsExecutingPlugin)
        {
            Dialogs.ShowMessage("Already executing a plugin");
            return false;
        }

        fIsExecutingPlugin = true;
        try
        {
            if (LinkForFile(FileName) is not { } UMLLinkClass)
            {
                return false;
            }

            TBoldUMLModelLink link = UMLLinkClass.Create();
            try
            {
                link.FileName = FileName;
                bool result = link.ImportModel(CurrentModel!);
                if (ModelHandle is { } handle)
                {
                    handle.FileName = FileName;
                }

                CurrentElement = CurrentModel;
                return result;
            }
            finally
            {
                link.Dispose();
            }
        }
        finally
        {
            fIsExecutingPlugin = false;
        }
    }

    /// <summary>Writes the model to a file through the link of its extension.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.SaveFileAs1Click", Note = "Takes the file the form's dialog chose.")]
    public bool SaveFileAs(string FileName)
    {
        if (fIsExecutingPlugin)
        {
            Dialogs.ShowMessage("Already executing a plugin");
            return false;
        }

        fIsExecutingPlugin = true;
        try
        {
            if (LinkForFile(FileName) is not { } UMLLinkClass)
            {
                return false;
            }

            TBoldUMLModelLink link = UMLLinkClass.Create();
            try
            {
                link.FileName = FileName;
                bool result = link.ExportModel(CurrentModel!);
                if (result && ModelHandle is { } handle)
                {
                    handle.FileName = FileName;
                }

                return result;
            }
            finally
            {
                link.Dispose();
            }
        }
        finally
        {
            fIsExecutingPlugin = false;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetCopyPasteRef")]
    private void GetCopyPasteRef()
    {
        if (UMLObjectToCopyOrMove != CurrentElement)
        {
            UMLObjectToCopyOrMove = CurrentElement;
        }
    }

    /// <summary>Cuts the element on show, for pasting; not an association, the model or the root class of a boldified model.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.BoldTreeView1Cut", Note = "The form greys the node.")]
    public bool Cut()
    {
        TBoldUMLObject? e = CurrentElement;
        if (e is null || e.IsKindOf("Association") || e.IsKindOf("Model"))
        {
            return false;
        }

        if (e.IsKindOf("Class") && CurrentModelIsBoldified && e == TBoldUMLBoldify.GetRootClass(CurrentModel!))
        {
            return false;
        }

        CutOrCopy = CutOrCopyKind.cckCut;
        GetCopyPasteRef();
        return true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.BoldTreeView1Copy")]
    public void Copy()
    {
        CutOrCopy = CutOrCopyKind.cckCopy;
        GetCopyPasteRef();
    }

    /// <summary>Pastes the element cut or copied onto the element on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.BoldTreeView1Paste", Note = "The form ungreys the node.")]
    public void Paste()
    {
        if (UMLObjectToCopyOrMove is not { } source || source.Deleted)
        {
            return;
        }

        if (source.IsKindOf("Class"))
        {
            if (source != CurrentElement)
            {
                PasteClass();
            }
        }
        else if (source.IsKindOf("Feature"))
        {
            PasteFeature();
        }
        else if (source.IsKindOf("Parameter"))
        {
            PasteParameter();
        }
        else if (source.IsKindOf("Association"))
        {
            PasteAssociation();
        }
        else if (source.IsKindOf("AssociationEnd"))
        {
            PasteAssociationEnd();
        }
    }

    /// <summary>Forgets what was cut (the escape key).</summary>
    public void CancelCut()
    {
        UMLObjectToCopyOrMove = null;
        CutOrCopy = CutOrCopyKind.cckNone;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetElementToPaste")]
    private TBoldUMLObject? GetElementToPaste() => CutOrCopy switch
    {
        CutOrCopyKind.cckCopy => TBoldCopyAndClone.BoldClone(UMLObjectToCopyOrMove, TBoldCopyMode.bcmDeep, true),
        CutOrCopyKind.cckCut => UMLObjectToCopyOrMove,
        _ => null,
    };

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PasteClass")]
    private void PasteClass()
    {
        TBoldUMLObject UMLClass = GetElementToPaste()!;
        TBoldUMLObject? current = CurrentElement;
        if (current is not null && current.IsKindOf("Class"))
        {
            UMLClass.SetFirstParent(current);
            UMLClass.namespace_ = current.namespace_;
        }
        else
        {
            // with no class on show the model's superclass is the default
            if (CurrentModelIsBoldified)
            {
                UMLClass.SetFirstParent(TBoldUMLBoldify.GetRootClass(CurrentModel!));
            }

            UMLClass.namespace_ = current is not null && current.IsKindOf("Package") ? current : CurrentModel;
        }

        UMLClass.name = TBoldUMLSupport.UniqueName(UMLClass, UMLObjectToCopyOrMove!.name);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PasteFeature")]
    private void PasteFeature()
    {
        if (CurrentElement is { } target && target.IsKindOf("Class"))
        {
            TBoldUMLObject newFeature = GetElementToPaste()!;
            newFeature.owner = target;
            newFeature.name = TBoldUMLSupport.UniqueName(newFeature, UMLObjectToCopyOrMove!.name);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PasteAssociation")]
    private void PasteAssociation()
    {
        if (CurrentElement is { } target && target.IsKindOf("Package"))
        {
            TBoldUMLObject newAssociation = GetElementToPaste()!;
            TBoldUMLObject oldAssociation = UMLObjectToCopyOrMove!;
            newAssociation.namespace_ = target;
            newAssociation.name = TBoldUMLSupport.UniqueName(newAssociation, oldAssociation.name);
            List<TBoldUMLObject> oldEnds = oldAssociation.connection, newEnds = newAssociation.connection;
            for (int i = 0; i < oldEnds.Count && i < newEnds.Count; i++)
            {
                newEnds[i].name = TBoldUMLSupport.UniqueName(newEnds[i], oldEnds[i].name);
            }
        }
    }

    /// <summary>Pastes an end on a class: the end's association, copied or moved, has the class at that end.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PasteAssociationEnd", Note = "Takes the end's association, where Delphi took the end itself as the association, which failed.")]
    private void PasteAssociationEnd()
    {
        if (CurrentElement is { } target && target.IsKindOf("Class") && UMLObjectToCopyOrMove!.association is { } oldAssociation)
        {
            TBoldUMLObject? newAssociation = CutOrCopy switch
            {
                CutOrCopyKind.cckCopy => TBoldCopyAndClone.BoldClone(oldAssociation, TBoldCopyMode.bcmDeep, true),
                CutOrCopyKind.cckCut => oldAssociation,
                _ => null,
            };
            if (newAssociation is null)
            {
                return;
            }

            newAssociation.namespace_ = target.namespace_;
            newAssociation.name = TBoldUMLSupport.UniqueName(newAssociation, oldAssociation.name);
            List<TBoldUMLObject> oldEnds = oldAssociation.connection, newEnds = newAssociation.connection;
            for (int i = 0; i < oldEnds.Count && i < newEnds.Count; i++)
            {
                if (oldEnds[i] == UMLObjectToCopyOrMove)
                {
                    newEnds[i].type_ = target;
                }

                newEnds[i].name = TBoldUMLSupport.UniqueName(newEnds[i], oldEnds[i].name);
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.PasteParameter")]
    private void PasteParameter()
    {
        if (CurrentElement is { } target && target.IsKindOf("Operation"))
        {
            TBoldUMLObject newParameter = GetElementToPaste()!;
            newParameter.SetRef("behavioralFeature", target);
            newParameter.name = TBoldUMLSupport.UniqueName(newParameter, UMLObjectToCopyOrMove!.name);
        }
    }

    /// <summary>Whether an element dragged over another may be dropped on it.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.BoldTreeView1DragOver", Note = "Takes the dragged element and the element under the mouse.")]
    public bool CanDrop(TBoldUMLObject? DraggedElement, TBoldUMLObject? DropTarget)
    {
        if (DraggedElement is null || DropTarget is null)
        {
            return false;
        }

        if (DropTarget.IsKindOf("Class"))
        {
            return DropOnUMLClass(DraggedElement);
        }

        if (DropTarget.IsKindOf("Attribute"))
        {
            return DropOnUMLAttribute(DraggedElement);
        }

        if (DropTarget.IsKindOf("Operation"))
        {
            return DropOnUMLOperation(DraggedElement);
        }

        if (DropTarget.IsKindOf("Parameter"))
        {
            return DropOnUMLParameter(DraggedElement);
        }

        if (DropTarget.IsKindOf("Association"))
        {
            return DropOnUMLAssociation(DraggedElement);
        }

        if (DropTarget.IsKindOf("AssociationEnd"))
        {
            return DropOnUMLAssociationEnd(DraggedElement);
        }

        return DropTarget.IsKindOf("Package") && DropOnUMLPackage(DraggedElement);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLClass")]
    private bool DropOnUMLClass(TBoldUMLObject ToBeDropped)
    {
        bool Result = ToBeDropped.IsKindOf("Operation") || ToBeDropped.IsKindOf("Attribute") || ToBeDropped.IsKindOf("AssociationEnd") || ToBeDropped.IsKindOf("Class");
        if (ToBeDropped.IsKindOf("Class") && CurrentModelIsBoldified && ToBeDropped == TBoldUMLBoldify.GetRootClass(CurrentModel!))
        {
            Result = false;
        }

        return Result;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLAssociation")]
    private bool DropOnUMLAssociation(TBoldUMLObject ToBeDropped) => false;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLAssociationEnd")]
    private bool DropOnUMLAssociationEnd(TBoldUMLObject ToBeDropped) => false;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLAttribute")]
    private bool DropOnUMLAttribute(TBoldUMLObject ToBeDropped) => false;

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLPackage")]
    private bool DropOnUMLPackage(TBoldUMLObject ToBeDropped) =>
        ToBeDropped.IsKindOf("Class") || (ToBeDropped.IsKindOf("Package") && !ToBeDropped.IsKindOf("Model"));

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLOperation")]
    private bool DropOnUMLOperation(TBoldUMLObject ToBeDropped) => ToBeDropped.IsKindOf("Parameter");

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DropOnUMLParameter")]
    private bool DropOnUMLParameter(TBoldUMLObject ToBeDropped) => ToBeDropped.IsKindOf("Parameter");

    /// <summary>Drops a dragged element on another.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.BoldTreeView1DragDrop", Note = "Takes the dropped element and the element under the mouse.")]
    public void Drop(TBoldUMLObject DroppedElement, TBoldUMLObject? DropTarget)
    {
        if (DropTarget is null)
        {
            return;
        }

        if (DropTarget.IsKindOf("Class"))
        {
            DroppedOnUMLClass(DroppedElement, DropTarget);
        }
        else if (DropTarget.IsKindOf("Operation"))
        {
            DroppedOnUMLOperation(DroppedElement, DropTarget);
        }
        else if (DropTarget.IsKindOf("Parameter"))
        {
            DroppedOnUMLParameter(DroppedElement, DropTarget);
        }
        else if (DropTarget.IsKindOf("Package"))
        {
            DroppedOnUMLPackage(DroppedElement, DropTarget);
        }
    }

    /// <summary>Moves a parameter before or after another of the same operation's class.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DroppedOnUMLParameter")]
    private void DroppedOnUMLParameter(TBoldUMLObject Dropped, TBoldUMLObject Target)
    {
        if (Dropped.IsKindOf("Parameter") && Dropped.Ref("behavioralFeature") is { } operation &&
            Target.Ref("behavioralFeature") is { } targetOperation && operation.owner == targetOperation.owner &&
            operation.parameter.IndexOf(Target) is var index and >= 0)
        {
            operation.MoveRef("parameter", Dropped, index);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DroppedOnUMLClass")]
    private void DroppedOnUMLClass(TBoldUMLObject Dropped, TBoldUMLObject Target)
    {
        if (Dropped.IsKindOf("Attribute") || Dropped.IsKindOf("Operation"))
        {
            Dropped.owner = Target;
        }
        else if (Dropped.IsKindOf("Class"))
        {
            if (Target != Dropped)
            {
                Dropped.SetFirstParent(Target);
            }
        }
        else if (Dropped.IsKindOf("AssociationEnd") && Dropped.otherEnd is { } otherEnd)
        {
            otherEnd.type_ = Target;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DroppedOnUMLOperation")]
    private void DroppedOnUMLOperation(TBoldUMLObject Dropped, TBoldUMLObject Target) => Dropped.SetRef("behavioralFeature", Target);

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.DroppedOnUMLPackage")]
    private void DroppedOnUMLPackage(TBoldUMLObject Dropped, TBoldUMLObject Target)
    {
        if (Dropped.IsKindOf("Classifier") || Dropped.IsKindOf("Package"))
        {
            Dropped.namespace_ = Target;
        }
    }

    /// <summary>The name of the method a Delphi signature declares.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetMethodName")]
    public static string GetMethodName(string Method)
    {
        int StartPos = 0;
        if (Method.Contains("function", StringComparison.Ordinal))
        {
            StartPos = Method.IndexOf("function", StringComparison.Ordinal) + 9;
        }
        else if (Method.Contains("procedure", StringComparison.Ordinal))
        {
            StartPos = Method.IndexOf("procedure", StringComparison.Ordinal) + 10;
        }

        int EndPos;
        if (!Method.Contains('('))
        {
            EndPos = StartPos == 10 ? Method.IndexOf(';') : Method.IndexOf(':');
        }
        else
        {
            EndPos = Method.IndexOf('(');
        }

        return EndPos > StartPos ? Method[StartPos..EndPos].Trim() : "";
    }

    /// <summary>The names of the parameters a Delphi signature declares, with "const" or "var" if there.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetParamNames")]
    public static void GetParamNames(string Method, TStringList ResultList)
    {
        char[] chars = Method.ToCharArray();
        int FinalPos = Array.IndexOf(chars, ')');
        int EndPos = 0;
        while (Array.IndexOf(chars, ':') >= 0 && Array.IndexOf(chars, ':') < FinalPos)
        {
            int StartPos;
            int open = Array.IndexOf(chars, '(');
            if (open >= 0)
            {
                StartPos = open + 1;
                chars[open] = 'X';
            }
            else
            {
                int semicolon = Array.IndexOf(chars, ';');
                StartPos = semicolon + 2;
                chars[semicolon] = 'X';
            }

            int colon = Array.IndexOf(chars, ':');
            if (colon >= 0)
            {
                EndPos = colon - 1;
                chars[colon] = 'X'; // so that it is not found again
            }

            ResultList.Add(new string(chars, StartPos, EndPos - StartPos + 1).Trim());
        }
    }

    /// <summary>The types of the parameters a Delphi signature declares.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetParamTypes")]
    public static void GetParamTypes(string Method, TStringList ResultList)
    {
        char[] chars = Method.ToCharArray();
        int FinalPos = Array.IndexOf(chars, ')');
        while (Array.IndexOf(chars, ':') >= 0 && Array.IndexOf(chars, ':') < FinalPos)
        {
            int colon = Array.IndexOf(chars, ':');
            int StartPos = colon + 2;
            chars[colon] = 'X';
            int EndPos;
            int semicolon = Array.IndexOf(chars, ';');
            if (semicolon >= 0 && semicolon < FinalPos)
            {
                EndPos = semicolon - 1;
                chars[semicolon] = 'X'; // so that it is not found again
            }
            else
            {
                EndPos = FinalPos - 1;
            }

            ResultList.Add(new string(chars, StartPos, EndPos - StartPos + 1).Trim());
        }
    }

    /// <summary>The return type a Delphi signature declares, or "" for a procedure.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.GetReturnType")]
    public static string GetReturnType(string Method)
    {
        int StartPos = Method.LastIndexOf(':');
        string TempString = StartPos < 0 ? "" : Method[(StartPos + 1)..].Trim();
        if (TempString.Contains(')'))
        {
            return "";
        }

        int EndPos = TempString.IndexOf(';');
        return EndPos < 0 ? "" : TempString[..EndPos].Trim();
    }

    /// <summary>The framework methods the class does not override yet, by name.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CreateFrameworkMethodItems", Note = "Gives the names; the form makes the menu items.")]
    public static List<string> CreateFrameworkMethodItems(TBoldUMLObject UMLClass)
    {
        var result = new List<string>();
        foreach (string method in FrameworkMethods)
        {
            var TypeList = new TStringList();
            GetParamTypes(method, TypeList);
            if (!TBoldUMLOperationSupport.ClassHasOperation(UMLClass, GetMethodName(method), TypeList))
            {
                result.Add(GetMethodName(method));
            }
        }

        return result;
    }

    /// <summary>The virtual methods of the class's superclasses it does not override yet, by name.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CreateModelMethodItems", Note = "Gives the names; the form makes the menu items.")]
    public List<string> CreateModelMethodItems(TBoldUMLObject UMLClass)
    {
        ModelMethods.Clear();
        GetAllOverrideableMethods(UMLClass, ModelMethods);
        var result = new List<string>();
        var TypeList = new TStringList();
        foreach (TBoldUMLObject method in ModelMethods)
        {
            TypeList.Clear();
            TBoldUMLOperationSupport.ParameterTypesToList(method, TypeList);
            if (!TBoldUMLOperationSupport.ClassHasOperation(UMLClass, method.name, TypeList))
            {
                result.Add(method.name);
            }
        }

        return result;
    }

    /// <summary>The virtual, dynamic and abstract operations of the class and its superclasses, the topmost first.</summary>
    // TUMLClassifier.GetAllOverrideableMethods of Delphi's UML model (BoldUMLModel, not ported), on the UML graph
    public static void GetAllOverrideableMethods(TBoldUMLObject UMLClass, List<TBoldUMLObject> Methods)
    {
        if (UMLClass.Superclass is { } superclass)
        {
            GetAllOverrideableMethods(superclass, Methods);
        }

        foreach (TBoldUMLObject feature in UMLClass.feature.Where(f => f.IsKindOf("Operation")))
        {
            string kind = feature.GetBoldTV(TAG_DELPHIOPERATIONKIND);
            if (kind == TV_DELPHIOPERATIONKIND_VIRTUAL || kind == TV_DELPHIOPERATIONKIND_DYNAMIC || kind == TV_DELPHIOPERATIONKIND_ABSTRACTVIRTUAL)
            {
                Methods.Add(feature);
            }
        }
    }

    /// <summary>Overrides a framework method in the class on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.OverrideFrameworkMethod", Note = "Returns the operation made, or null.")]
    public TBoldUMLObject? OverrideFrameworkMethod(string MethodName)
    {
        TBoldUMLObject Class_ = GetCurrentClass()!;
        string Method = FindFrameworkMethod(MethodName);
        if (Method == "")
        {
            Dialogs.MessageDlg("Error: Could not find operation!", TMsgDlgType.mtError, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbOK), 0);
            return null;
        }

        TBoldUMLObject NewMethod = NewElement(Class_.Graph, "Operation");
        NewMethod.name = GetMethodName(Method);
        Class_.AddRef("feature", NewMethod);
        NewMethod.SetBoldTV(TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_OVERRIDE);
        var ParamNames = new TStringList();
        var ParamTypes = new TStringList();
        GetParamNames(Method, ParamNames);
        GetParamTypes(Method, ParamTypes);
        CreateParamsFromStringList(ParamNames, ParamTypes, NewMethod, GetReturnType(Method));
        return NewMethod;
    }

    /// <summary>Makes the parameters of an operation from the names and types of a Delphi signature.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.CreateParamsFromStringList")]
    public static void CreateParamsFromStringList(TStringList ParamNames, TStringList ParamTypes, TBoldUMLObject NewOperation, string ReturnType)
    {
        for (int Index = 0; Index < ParamNames.Count; Index++)
        {
            string CurrentParamName = ParamNames[Index];
            TBoldUMLObject UMLParameter = NewElement(NewOperation.Graph, "Parameter");
            NewOperation.AddRef("parameter", UMLParameter);
            int at = CurrentParamName.IndexOf("const ", StringComparison.Ordinal);
            if (at >= 0)
            {
                UMLParameter.SetBoldTV(TAG_ISCONST, TV_TRUE);
                CurrentParamName = CurrentParamName.Remove(at, "const ".Length);
            }
            else
            {
                UMLParameter.SetBoldTV(TAG_ISCONST, TV_FALSE);
            }

            at = CurrentParamName.IndexOf("var ", StringComparison.Ordinal);
            if (at >= 0)
            {
                UMLParameter.kind = TBoldParameterDirectionKind.pdInout;
                CurrentParamName = CurrentParamName.Remove(at, "var ".Length);
            }
            else
            {
                UMLParameter.kind = TBoldParameterDirectionKind.pdIn;
            }

            UMLParameter.name = CurrentParamName;
            UMLParameter.typeName = ParamTypes[Index];
        }

        if (ReturnType != "")
        {
            TBoldUMLObject UMLParameter = NewElement(NewOperation.Graph, "Parameter");
            NewOperation.AddRef("parameter", UMLParameter);
            UMLParameter.name = "return";
            UMLParameter.typeName = ReturnType;
            UMLParameter.kind = TBoldParameterDirectionKind.pdReturn;
        }
    }

    /// <summary>Overrides a superclass's virtual operation in the class on show.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.OverrideModelMethod", Note = "Returns the operation made, or null.")]
    public TBoldUMLObject? OverrideModelMethod(string MethodName)
    {
        if (ModelMethods.Count == 0 && GetCurrentClass() is { } cls)
        {
            CreateModelMethodItems(cls);
        }

        if (FindModelMethod(MethodName) is not { } OldOperation)
        {
            Dialogs.MessageDlg("Error: Could not find operation!", TMsgDlgType.mtError, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbOK), 0);
            return null;
        }

        TBoldUMLObject Class_ = GetCurrentClass()!;
        TBoldUMLObject NewOperation = NewElement(Class_.Graph, "Operation");
        Class_.AddRef("feature", NewOperation);
        NewOperation.name = OldOperation.name;
        NewOperation.SetBoldTV(TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_OVERRIDE);
        foreach (TBoldUMLObject OldParameter in OldOperation.parameter)
        {
            TBoldUMLObject NewParameter = NewElement(Class_.Graph, "Parameter");
            NewOperation.AddRef("parameter", NewParameter);
            NewParameter.name = OldParameter.name;
            NewParameter.typeName = OldParameter.typeName;
            NewParameter.SetBoldTV(TAG_ISCONST, OldParameter.GetBoldTV(TAG_ISCONST));
            NewParameter.kind = OldParameter.kind;
        }

        return NewOperation;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.FindFrameworkMethod")]
    public static string FindFrameworkMethod(string MethodName) =>
        FrameworkMethods.FirstOrDefault(m => GetMethodName(m) == MethodName) ?? "";

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.FindModelMethod")]
    private TBoldUMLObject? FindModelMethod(string MethodName) => ModelMethods.FirstOrDefault(m => m.name == MethodName);

    /// <summary>Overrides the operation on show in every subclass of its class.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.mnuOverrideInAllSubclassesClick")]
    public void OverrideInAllSubclasses()
    {
        TBoldUMLObject UMLOperation = CurrentElement!;
        TBoldUMLOperationSupport.OverrideInAllSubclasses(UMLOperation.owner!, UMLOperation);
    }

    public void Dispose()
    {
        UnhookModel();
        GC.SuppressFinalize(this);
    }
}
