// Delphi: Source/UMLModel/ModelLinks/Core/BoldUMLModelLink.pas
// Port map: CSharp/port/units/BoldUMLModelLink.md
// A link reads and writes a model in another tool's format. Delphi's links filled and read its UML object space; the
// port's fill and read the UML graph (D6), and ImportMoldModel/ExportMoldModel take the graph to and from the Mold
// model, flattening and boldifying as Delphi's TBoldModel did.

using static Bold.BoldSubscription;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "TSuffixIndex", Note = "TBoldUMLModelLinkList searches its descriptors by suffix.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "TSuffixIndex.ItemAsKeyString", Note = "The descriptor's suffix is compared.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "TLinkClassIndex", Note = "TBoldUMLModelLinkList searches its descriptors by link class.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "TLinkClassIndex.ItemAsKeyClass", Note = "The descriptor's link class is compared.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "initialization", Note = "The section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelLink", Symbol = "finalization", Note = "The list of links goes with the process.")]

namespace Bold;

/// <summary>The class of a model link, for the list of links.</summary>
[Delphi(PortStatus.Ported, Note = "A Type deriving from TBoldUMLModelLink.")]
public sealed record TBoldUMLModelLinkClass(Type LinkType)
{
    public TBoldUMLModelLink Create() => (TBoldUMLModelLink)Activator.CreateInstance(LinkType, [null])!;
}

/// <summary>Reads a model from, and writes one to, another tool's format.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldUMLModelLink : TBoldHandle
{
    private TBoldAbstractModel? fBoldModel;
    private TBoldPassthroughSubscriber? fSubscriber;

    [Delphi(PortStatus.Ported)]
    protected TBoldUMLModelLink(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
        fSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLModelLink.Destroy")]
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fSubscriber);
        }
    }

    /// <summary>Links a model; logs a warning when another link already has it.</summary>
    [Delphi(PortStatus.Ported, Note = "Logs the warning where Delphi showed a message box.")]
    private void SetBoldModel(TBoldAbstractModel? Value)
    {
        fSubscriber!.CancelAllSubscriptions();
        if (Value is not null)
        {
            TBoldUMLModelLink? oldLink = BoldUMLModelLinkUnit.BoldGetUMLModelLinkForComponent(Value);
            if (oldLink is not null && oldLink != this)
            {
                BoldLogHandler.BoldLog().Log($"Warning: {oldLink} is already connected to {Value}");
            }

            Value.AddSmallSubscription(fSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
        }

        fBoldModel = Value;
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fBoldModel && OriginalEvent == beDestroying)
        {
            fBoldModel = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetCanExport() => false;

    [Delphi(PortStatus.Ported)]
    protected abstract string GetDisplayName();

    [Delphi(PortStatus.Ported)]
    protected virtual string GetFileName() => throw new EBoldInternal($"{GetType().Name}.GetFileName: Link can't import from file");

    [Delphi(PortStatus.Ported)]
    protected virtual void SetFileName(string Value) => throw new EBoldInternal($"{GetType().Name}.SetFileName: Link can't import from file");

    [Delphi(PortStatus.Ported)]
    protected override object? GetHandledObject() => null;

    /// <summary>Writes the UML model.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the UML graph where Delphi took its UML object space's model (D6).")]
    public abstract bool ExportModel(TBoldUMLObject UMLModel);

    /// <summary>Reads the model into the UML model, which is cleared first.</summary>
    [Delphi(PortStatus.Ported, Note = "Fills the UML graph where Delphi filled its UML object space's model (D6).")]
    public abstract bool ImportModel(TBoldUMLObject UMLModel);

    [Delphi(PortStatus.Ported)] public bool CanExport => GetCanExport();
    [Delphi(PortStatus.Ported)] public string DisplayName => GetDisplayName();

    [Delphi(PortStatus.Ported)]
    public string FileName
    {
        get => GetFileName();
        set => SetFileName(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractModel? BoldModel
    {
        get => fBoldModel;
        set => SetBoldModel(value);
    }

    /// <summary>How a model is boldified on import (Delphi's TBoldModel.Boldify).</summary>
    public TBoldUMLBoldify Boldify { get; } = new();

    /// <summary>Reads the model and makes its Mold model: flattened and boldified, as Delphi's TBoldModel made it.</summary>
    public TMoldModel ImportMoldModel()
    {
        TBoldUMLObject umlModel = new TBoldUMLModelGraph().Create("Model");
        if (!ImportModel(umlModel))
        {
            throw new EBoldImport($"{GetType().Name}: the model could not be read from {DisplayName}");
        }

        return UMLModelToMoldModel(umlModel, Boldify);
    }

    /// <summary>Reads the model into the model component (or the linked one).</summary>
    public void ImportModel(TBoldAbstractModel? Model = null)
    {
        Model ??= BoldModel ?? throw new EBold($"{GetType().Name}.ImportModel: no model to import to");
        Model.LoadFromMoldModel(ImportMoldModel());
    }

    /// <summary>Writes a Mold model: as a UML model, unflattened and unboldified as the tool expects it.</summary>
    public bool ExportModel(TMoldModel MoldModel) => ExportModel(MoldModelToUMLModel(MoldModel));

    /// <summary>The Mold model of a UML model: flattened and boldified first, as Delphi's TBoldModel did.</summary>
    public static TMoldModel UMLModelToMoldModel(TBoldUMLObject UMLModel, TBoldUMLBoldify? Boldify = null)
    {
        if (!TBoldUMLSupport.IsFlattened(UMLModel))
        {
            TBoldUMLSupport.Flatten(UMLModel);
        }

        if (!TBoldUMLBoldify.IsBoldified(UMLModel))
        {
            (Boldify ?? new TBoldUMLBoldify()).Boldify(UMLModel);
        }

        return TBoldModelConverter.UMLModelToMold(UMLModel);
    }

    /// <summary>The UML model of a Mold model, unboldified and unflattened.</summary>
    public static TBoldUMLObject MoldModelToUMLModel(TMoldModel MoldModel)
    {
        TBoldUMLObject umlModel = TBoldModelConverter.MoldModelToUML(MoldModel);
        if (TBoldUMLBoldify.IsBoldified(umlModel))
        {
            TBoldUMLBoldify.UnBoldify(umlModel);
        }

        if (TBoldUMLSupport.IsFlattened(umlModel))
        {
            TBoldUMLSupport.UnFlatten(umlModel);
        }

        return umlModel;
    }
}

/// <summary>A kind of link: the file suffix it reads and writes, and its description.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLModelLinkDescriptor
{
    [Delphi(PortStatus.Ported)]
    public TBoldUMLModelLinkDescriptor(string Suffix, string Description, TBoldUMLModelLinkClass ModelLinkClass)
    {
        this.Suffix = Suffix;
        this.Description = Description;
        this.ModelLinkClass = ModelLinkClass;
    }

    [Delphi(PortStatus.Ported)]
    private string GetFilterString() => $"{Description} (*{Suffix})|*{Suffix}";

    [Delphi(PortStatus.Ported)] public string Description { get; }
    [Delphi(PortStatus.Ported)] public string FilterString => GetFilterString();
    [Delphi(PortStatus.Ported)] public TBoldUMLModelLinkClass ModelLinkClass { get; }
    [Delphi(PortStatus.Ported)] public string Suffix { get; }

    [Delphi(PortStatus.Ported)]
    public bool CanExport()
    {
        using TBoldUMLModelLink link = ModelLinkClass.Create();
        return link.CanExport;
    }
}

/// <summary>The kinds of links, found by suffix or by class.</summary>
[Delphi(PortStatus.Ported, Note = "A list searched by suffix and by class, where Delphi used an indexable list with two hash indexes.")]
public class TBoldUMLModelLinkList
{
    private readonly List<TBoldUMLModelLinkDescriptor> fItems = [];

    [Delphi(PortStatus.Ported)]
    public TBoldUMLModelLinkList()
    {
    }

    public int Count => fItems.Count;

    [Delphi(PortStatus.Ported)]
    private TBoldUMLModelLinkDescriptor GetDescriptorByIndex(int Index) => fItems[Index];

    [Delphi(PortStatus.Ported)]
    private TBoldUMLModelLinkDescriptor? GetDescriptorByLinkClass(TBoldUMLModelLinkClass ModelLinkClass) =>
        fItems.FirstOrDefault(d => d.ModelLinkClass == ModelLinkClass);

    /// <summary>The link of a suffix, whatever its case (Delphi's index of suffixes ignored case).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldUMLModelLinkDescriptor? GetDescriptorBySuffix(string Suffix) =>
        fItems.FirstOrDefault(d => string.Equals(d.Suffix, Suffix, StringComparison.OrdinalIgnoreCase));

    [Delphi(PortStatus.Ported)]
    private string GetFileFilter() => string.Concat(fItems.Select(d => d.FilterString + "|")) + BoldDefs.ALLFILESFILTER;

    [Delphi(PortStatus.Ported)]
    private TBoldUMLModelLinkClass? GetLinkClasses(int Index, string Suffix) =>
        Index >= 0 && Index < Count ? fItems[Index].ModelLinkClass : GetDescriptorBySuffix(Suffix)?.ModelLinkClass;

    [Delphi(PortStatus.Ported)]
    private string GetExportableFileFilter() => string.Concat(fItems.Where(d => d.CanExport()).Select(d => d.FilterString + "|")) + BoldDefs.ALLFILESFILTER;

    [Delphi(PortStatus.Ported)] protected TBoldUMLModelLinkDescriptor Descriptors(int index) => GetDescriptorByIndex(index);
    [Delphi(PortStatus.Ported)] protected TBoldUMLModelLinkDescriptor? LinkDescriptorBySuffix(string Suffix) => GetDescriptorBySuffix(Suffix);

    [Delphi(PortStatus.Ported)]
    public void AddLink(string Suffix, string Description, TBoldUMLModelLinkClass ModelLinkClass) =>
        fItems.Add(new TBoldUMLModelLinkDescriptor(Suffix, Description, ModelLinkClass));

    [Delphi(PortStatus.Ported)]
    public void RemoveLink(TBoldUMLModelLinkClass ModelLinkClass)
    {
        if (GetDescriptorByLinkClass(ModelLinkClass) is { } descriptor)
        {
            fItems.Remove(descriptor);
        }
    }

    [Delphi(PortStatus.Ported)] public string FileFilter => GetFileFilter();
    [Delphi(PortStatus.Ported)] public string ExportableFileFilter => GetExportableFileFilter();
    [Delphi(PortStatus.Ported)] public TBoldUMLModelLinkClass? LinkClasses(int index, string Suffix) => GetLinkClasses(index, Suffix);
    [Delphi(PortStatus.Ported)] public TBoldUMLModelLinkDescriptor? LinkDescriptorByLinkClass(TBoldUMLModelLinkClass ModelLinkClass) => GetDescriptorByLinkClass(ModelLinkClass);
}

/// <summary>Routines of BoldUMLModelLink.</summary>
public static class BoldUMLModelLinkUnit
{
    private static TBoldUMLModelLinkList? G_BoldUMLModelLinkList;

    [Delphi(PortStatus.Ported, Symbol = "EnsureList")]
    private static void EnsureList()
    {
        if (G_BoldUMLModelLinkList is null)
        {
            G_BoldUMLModelLinkList = new TBoldUMLModelLinkList();
            // Delphi's XMI link registered itself in its unit's initialization.
            TBoldUMLXMILink.Register(G_BoldUMLModelLinkList);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "BoldUMLModelLinkList")]
    public static TBoldUMLModelLinkList BoldUMLModelLinkList()
    {
        EnsureList();
        return G_BoldUMLModelLinkList!;
    }

    [Delphi(PortStatus.Ported, Symbol = "BoldUMLModelLinkListAssigned")]
    public static bool BoldUMLModelLinkListAssigned() => G_BoldUMLModelLinkList is not null;

    /// <summary>The link whose model is the given one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "BoldGetUMLModelLinkForComponent")]
    public static TBoldUMLModelLink? BoldGetUMLModelLinkForComponent(TBoldAbstractModel BoldModel)
    {
        TBoldObjectArray? handles = BoldHandle.BoldHandleList;
        for (int i = 0; handles is not null && i < handles.Count; i++)
        {
            if (handles[i] is TBoldUMLModelLink link && link.BoldModel == BoldModel)
            {
                return link;
            }
        }

        return null;
    }
}
