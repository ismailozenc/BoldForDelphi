// Delphi: Source/BoldAwareGUI/ControlPacks/BoldNodeControlPack.pas
// Port map: CSharp/port/units/BoldNodeControlPack.md

using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldNodeControlPack", Symbol = "TBoldNodeDescriptionClass", Note = "A System.Type of a TBoldNodeDescription class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNodeControlPack", Symbol = "TBoldNodeDescriptionsClass", Note = "A System.Type of a TBoldNodeDescriptions class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNodeControlPack", Symbol = "TBoldTreeFollowerControllerClass", Note = "A System.Type of a TBoldTreeFollowerController class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldNodeControlPack", Symbol = "TBoldNodeFollowerControllerClass", Note = "A System.Type of a TBoldNodeFollowerController class.")]

namespace Bold;

/// <summary>
/// The controller of a tree: a generic list whose rows are nodes, each node described by the node description its
/// part names (a list of child nodes, an icon and a text).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldTreeFollowerController : TBoldGenericListController
{
    private TBoldNodeDescriptions? FNodeDescriptions;
    private TBoldNodeDescription? FDefaultNodeDescription;
    private TBoldFollowerEvent? FOnIconChanged;
    private TBoldFollowerEvent? FOnTextChanged;
    private TGetFollowerControllerByNameEvent? FOnGetFollowerController;

    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    public TBoldTreeFollowerController(object? aOwningComponent)
        : base(aOwningComponent, null!)
    {
        GetFollowerControllerByNameFunc = DoGetFollowerController;
        FNodeDescriptions = (TBoldNodeDescriptions)Activator.CreateInstance(GetBoldNodeDescriptionsClass(), this)!;
    }

    /// <summary>The class of the node descriptions (a TBoldNodeDescriptions class).</summary>
    [Delphi(PortStatus.Ported, Note = "Returns a Type where Delphi returns a class reference.")]
    protected virtual Type GetBoldNodeDescriptionsClass() => typeof(TBoldNodeDescriptions);

    [Delphi(PortStatus.Ported, Symbol = "TBoldTreeFollowerController.Destroy")]
    public override void Dispose()
    {
        FreePublisher();
        FNodeDescriptions?.Clear();
        FNodeDescriptions = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void SetNodeDescriptions(TBoldNodeDescriptions Value) => FNodeDescriptions!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private string GetDefaultNodeDescriptionName() => FDefaultNodeDescription?.Name ?? "";

    /// <summary>Makes the description with the name the default (none for an empty name); other names raise.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetDefaultNodeDescriptionName(string Value)
    {
        if (Value != DefaultNodeDescriptionName)
        {
            TBoldNodeDescription? NodeDescription = FNodeDescriptions!.FindByName(Value);
            if (NodeDescription is not null || Value == "")
            {
                FDefaultNodeDescription = NodeDescription;
                Changed();
            }
            else
            {
                throw new EBold(SysUtils.Format(sInvalidNodeName, GetType().Name, Value));
            }
        }
    }

    /// <summary>The node controller of the description with the name, or of the default description for '&lt;Default&gt;'.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldFollowerController? DefaultGetNodeFollowerControllerByName(string Name)
    {
        TBoldNodeDescription? NodeDescription = FNodeDescriptions!.FindByName(Name);
        if (NodeDescription is not null)
        {
            return NodeDescription.NodeFollowerController;
        }

        if (FDefaultNodeDescription is not null && string.CompareOrdinal(Name, BoldDefs.DEFAULTNAME) == 0)
        {
            return FDefaultNodeDescription.NodeFollowerController;
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void DoIconChanged(TBoldFollower Follower) => FOnIconChanged?.Invoke(Follower);

    [Delphi(PortStatus.Ported)]
    protected internal void DoTextChanged(TBoldFollower Follower) => FOnTextChanged?.Invoke(Follower);

    /// <summary>The node controller for the name: OnGetFollowerController's, else the default lookup.</summary>
    [Delphi(PortStatus.Ported, Note = "The Delphi function; the C++Builder variant with a var parameter is not ported.")]
    protected internal TBoldFollowerController? DoGetFollowerController(string Name) =>
        FOnGetFollowerController is not null ? FOnGetFollowerController(Name) : DefaultGetNodeFollowerControllerByName(Name);

    [Delphi(PortStatus.Ported)]
    protected override void DoAssign(TPersistent Source)
    {
        System.Diagnostics.Debug.Assert(Source is TBoldTreeFollowerController);
        base.DoAssign(Source);
        var source = (TBoldTreeFollowerController)Source;
        NodeDescriptions.Assign(source.NodeDescriptions);
        DefaultNodeDescriptionName = source.DefaultNodeDescriptionName;
        OnGetFollowerController = source.OnGetFollowerController;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? OnIconChanged
    {
        get => FOnIconChanged;
        set => FOnIconChanged = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? OnTextChanged
    {
        get => FOnTextChanged;
        set => FOnTextChanged = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldNodeDescriptions NodeDescriptions
    {
        get => FNodeDescriptions!;
        set => SetNodeDescriptions(value);
    }

    [Delphi(PortStatus.Ported)]
    public string DefaultNodeDescriptionName
    {
        get => GetDefaultNodeDescriptionName();
        set => SetDefaultNodeDescriptionName(value);
    }

    [Delphi(PortStatus.Ported)]
    public TGetFollowerControllerByNameEvent? OnGetFollowerController
    {
        get => FOnGetFollowerController;
        set => FOnGetFollowerController = value;
    }
}

/// <summary>The node descriptions of a tree controller, found by name; a change to one changes the controller.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNodeDescriptions : TCollection
{
    internal readonly TBoldTreeFollowerController FTreeFollowerController;

    [Delphi(PortStatus.Ported)]
    public TBoldNodeDescriptions(TBoldTreeFollowerController aTreeFollowerController)
        : base(typeof(TBoldNodeDescription))
    {
        ItemClass = GetNodeDescriptionClass();
        FTreeFollowerController = aTreeFollowerController;
    }

    /// <summary>The class of the descriptions (a TBoldNodeDescription class).</summary>
    [Delphi(PortStatus.Ported, Note = "Returns a Type where Delphi returns a class reference.")]
    protected virtual Type GetNodeDescriptionClass() => typeof(TBoldNodeDescription);

    [Delphi(PortStatus.Ported)]
    private TBoldNodeDescription GetNodeControllerItem(int Index) => (TBoldNodeDescription)base[Index];

    [Delphi(PortStatus.Ported)]
    private void SetNodeControllerItem(int Index, TBoldNodeDescription Value) => base[Index] = Value;

    [Delphi(PortStatus.Ported)]
    protected override TPersistent? GetOwner() => FTreeFollowerController;

    [Delphi(PortStatus.Ported)]
    protected override void Update(TCollectionItem? Item) => FTreeFollowerController.Changed();

    /// <summary>Frees a description that leaves the collection, as Delphi freed a deleted collection item.</summary>
    protected override void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
        base.Notify(Item, Action);
        if (Action == TCollectionNotification.cnExtracting && Item is TBoldNodeDescription description)
        {
            description.Dispose();
        }
    }

    /// <summary>The description with the name (case-insensitive), or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldNodeDescription? FindByName(string Name)
    {
        for (int I = 0; I < Count; I++)
        {
            var Result = (TBoldNodeDescription)base[I];
            if (SysUtils.CompareText(Result.Name, Name) == 0)
            {
                return Result;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public new TBoldNodeDescription Add()
    {
        BeginUpdate();
        try
        {
            return (TBoldNodeDescription)Activator.CreateInstance(GetNodeDescriptionClass(), this)!;
        }
        finally
        {
            EndUpdate();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNodeDescriptions.Items")]
    public new TBoldNodeDescription this[int Index]
    {
        get => GetNodeControllerItem(Index);
        set => SetNodeControllerItem(Index, value);
    }
}

/// <summary>
/// A kind of tree node, by name: the node controller (its child list, icon and text controllers) and its context
/// type (the type named by ContextTypeName, or else by Name).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldNodeDescription : TCollectionItem
{
    private string FName = "";
    private TBoldNodeFollowerController? FNodeFollowerController;

    [Delphi(PortStatus.Ported)]
    public TBoldNodeDescription(TCollection? Collection)
        : base(Collection)
    {
        FNodeFollowerController = (TBoldNodeFollowerController)Activator.CreateInstance(
            GetBoldNodeFollowerControllerClass(), ((TBoldNodeDescriptions)Collection!).FTreeFollowerController)!;
        FNodeFollowerController.OnGetContextType = GetContextType;
    }

    /// <summary>The class of the node controller (a TBoldNodeFollowerController class).</summary>
    [Delphi(PortStatus.Ported, Note = "Returns a Type where Delphi returns a class reference.")]
    protected virtual Type GetBoldNodeFollowerControllerClass() => typeof(TBoldNodeFollowerController);

    /// <summary>Frees the node controller; called when the description leaves its collection.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldNodeDescription.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref FNodeFollowerController);
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldNodeDescription source)
        {
            Name = source.Name;
            HideNodeWithNoChildren = source.HideNodeWithNoChildren;
            ContextTypeName = source.ContextTypeName;
            ListController.Assign(source.ListController);
            IconController.Assign(source.IconController);
            TextController.Assign(source.TextController);
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName()
    {
        string Result = FName;
        if (Result == "")
        {
            Result = base.GetDisplayName();
        }

        return Result;
    }

    /// <summary>Sets the name; a name another description has raises.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetName(string Value)
    {
        if (FName != Value)
        {
            if (Value != "" && SysUtils.CompareText(FName, Value) != 0 && ((TBoldNodeDescriptions)Collection!).FindByName(Value) is not null)
            {
                throw new EBold(SysUtils.Format(SBoldInvalidName, Value));
            }

            FName = Value;
            Changed(false);
        }
    }

    [Delphi(PortStatus.Ported)] private bool GetHideNodeWithNoChildren() => FNodeFollowerController!.HideNodeWithNoChildren;
    [Delphi(PortStatus.Ported)] private void SetHideNodeWithNoChildren(bool Value) => FNodeFollowerController!.HideNodeWithNoChildren = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldGenericListController GetListController() => (TBoldGenericListController)FNodeFollowerController![BoldNodeListIndex];

    [Delphi(PortStatus.Ported)]
    private TBoldIntegerFollowerController GetIconController() => (TBoldIntegerFollowerController)FNodeFollowerController![BoldNodeIconIndex];

    [Delphi(PortStatus.Ported)]
    private TBoldStringFollowerController GetTextController() => (TBoldStringFollowerController)FNodeFollowerController![BoldNodeTextIndex];

    [Delphi(PortStatus.Ported)] private void SetListController(TBoldGenericListController Value) => GetListController().Assign(Value);
    [Delphi(PortStatus.Ported)] private void SetIconController(TBoldIntegerFollowerController Value) => GetIconController().Assign(Value);
    [Delphi(PortStatus.Ported)] private void SetTextController(TBoldStringFollowerController Value) => GetTextController().Assign(Value);

    [Delphi(PortStatus.Ported)] private string GetContextTypeName() => FNodeFollowerController!.ContextTypeName;
    [Delphi(PortStatus.Ported)] private void SetContextTypeName(string Value) => FNodeFollowerController!.ContextTypeName = Value;

    /// <summary>The type named by ContextTypeName (or else by Name) in the tree's system, or null without a tree context.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType()
    {
        TBoldElementTypeInfo? ContextType = NodeFollowerController.FTreeFollowerController.ContextType;
        if (ContextType is not null)
        {
            var systemTypeInfo = (TBoldSystemTypeInfo)ContextType.SystemTypeInfo!;
            return systemTypeInfo.ElementTypeInfoByExpressionName(ContextTypeName != "" ? ContextTypeName : Name);
        }

        return null;
    }

    [Delphi(PortStatus.Ported)] public TBoldNodeFollowerController NodeFollowerController => FNodeFollowerController!;

    [Delphi(PortStatus.Ported)]
    public string Name
    {
        get => FName;
        set => SetName(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ContextTypeName
    {
        get => GetContextTypeName();
        set => SetContextTypeName(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool HideNodeWithNoChildren
    {
        get => GetHideNodeWithNoChildren();
        set => SetHideNodeWithNoChildren(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGenericListController ListController
    {
        get => GetListController();
        set => SetListController(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldIntegerFollowerController IconController
    {
        get => GetIconController();
        set => SetIconController(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController TextController
    {
        get => GetTextController();
        set => SetTextController(value);
    }
}

/// <summary>
/// The controller of a tree node: a controller list of the child-list controller (the node's parts), the icon
/// controller and the text controller, whose events go to the tree controller.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldNodeFollowerController : TBoldControllerList
{
    internal readonly TBoldTreeFollowerController FTreeFollowerController;
    private bool FHideNodeWithNoChildren;
    private string FContextTypeName = "";
    private readonly TBoldGenericListController fGenericListFollowerController;
    private readonly TBoldIntegerFollowerController fIconFollowerController;
    private readonly TBoldStringFollowerController fTextFollowerController;

    [Delphi(PortStatus.Ported)]
    public TBoldNodeFollowerController(TBoldTreeFollowerController aTreeFollowerController)
        : base(aTreeFollowerController.OwningComponent)
    {
        object? OwningComponent = aTreeFollowerController.OwningComponent;
        FTreeFollowerController = aTreeFollowerController;

        fGenericListFollowerController = new TBoldGenericListController(OwningComponent, FTreeFollowerController.DoGetFollowerController);
        fGenericListFollowerController.OnBeforeInsertItem = FTreeFollowerController.DoBeforeInsertItem;
        fGenericListFollowerController.OnAfterInsertItem = FTreeFollowerController.DoAfterInsertItem;
        fGenericListFollowerController.OnBeforeDeleteItem = FTreeFollowerController.DoBeforeDeleteItem;
        fGenericListFollowerController.OnAfterDeleteItem = FTreeFollowerController.DoAfterDeleteItem;
        fGenericListFollowerController.BeforeMakeUptoDate = FTreeFollowerController.BeforeMakeUptoDate;
        fGenericListFollowerController.OnGetContextType = GetContextType;
        Add(fGenericListFollowerController);

        fIconFollowerController = new TBoldIntegerFollowerController(OwningComponent);
        fIconFollowerController.AfterMakeUptoDate = FTreeFollowerController.DoIconChanged;
        fIconFollowerController.Expression = "-1";
        fIconFollowerController.OnGetContextType = GetContextType;
        Add(IconFollowerController);

        fTextFollowerController = new TBoldStringFollowerController(OwningComponent);
        fTextFollowerController.AfterMakeUptoDate = FTreeFollowerController.DoTextChanged;
        fTextFollowerController.OnGetContextType = GetContextType;
        Add(TextFollowerController);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNodeFollowerController.Destroy")]
    public override void Dispose()
    {
        FreePublisher(); //CHECKME Is this really needed?
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void SetHideNodeWithNoChildren(bool Value)
    {
        if (FHideNodeWithNoChildren != Value)
        {
            FHideNodeWithNoChildren = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetContextTypeName(string Value)
    {
        if (FContextTypeName != Value)
        {
            FContextTypeName = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemTypeInfo? GetSystemTypeInfo() => (TBoldSystemTypeInfo?)FTreeFollowerController.ContextType?.SystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    public bool HideNodeWithNoChildren
    {
        get => FHideNodeWithNoChildren;
        set => SetHideNodeWithNoChildren(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ContextTypeName
    {
        get => FContextTypeName;
        set => SetContextTypeName(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo? SystemTypeInfo => GetSystemTypeInfo();
    [Delphi(PortStatus.Ported)] public TBoldGenericListController GenericListFollowerController => fGenericListFollowerController;
    [Delphi(PortStatus.Ported)] public TBoldIntegerFollowerController IconFollowerController => fIconFollowerController;
    [Delphi(PortStatus.Ported)] public TBoldStringFollowerController TextFollowerController => fTextFollowerController;
}
