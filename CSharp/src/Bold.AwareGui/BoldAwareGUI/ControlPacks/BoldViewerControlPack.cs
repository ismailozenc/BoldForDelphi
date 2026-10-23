// Delphi: Source/BoldAwareGUI/ControlPacks/BoldViewerControlPack.pas
// Port map: CSharp/port/units/BoldViewerControlPack.md

using System.ComponentModel;
using System.Drawing;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldViewerControlPack", Symbol = "TBoldViewAdapterClass", Note = "A System.Type of a TBoldAbstractViewAdapter subclass.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldViewerControlPack", Symbol = "TBoldAbstractViewAdapter.GetPalette", Note = "Gave a Windows GDI palette (HPALETTE); GDI+ draws without palettes.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldViewerControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldViewerControlPack", Symbol = "finalization", Note = "Freed the default renderer and the view adapter list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the view adapter (an image, say) of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate TBoldAbstractViewAdapter? TBoldGetAsViewer(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a view adapter.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsViewer(TBoldElement? Element, TBoldAbstractViewAdapter? Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the view adapter in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldViewerIsChanged(TBoldViewerRendererData RendererData, TBoldAbstractViewAdapter? NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The view adapter a control shows (it owns it), and whether it changed.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldViewerRendererData : TBoldRendererData
{
    private TBoldAbstractViewAdapter? fViewAdapter;
    private bool fHasChanged;

    /// <summary>Renderer data for the follower.</summary>
    public TBoldViewerRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldViewerRendererData.Destroy")]
    public override void Dispose()
    {
        (fViewAdapter as IDisposable)?.Dispose();
        fViewAdapter = null;
        base.Dispose();
    }

    /// <summary>Takes a new view adapter (dropping the old one) and marks the data changed.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetViewAdapter(TBoldAbstractViewAdapter? Value)
    {
        if (Value != fViewAdapter)
        {
            (fViewAdapter as IDisposable)?.Dispose();
            fViewAdapter = Value;
            fHasChanged = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractViewAdapter? ViewAdapter
    {
        get => fViewAdapter;
        set => SetViewAdapter(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool HasChanged
    {
        get => fHasChanged;
        set => fHasChanged = value;
    }
}

/// <summary>Shows a blob (an image, say) through the view adapter registered for its content type, and saves it back.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsViewerRenderer : TBoldSingleRenderer
{
    private static TBoldAsViewerRenderer? DefaultAsViewerRenderer;

    private TBoldGetAsViewer? FOnGetAsViewer;
    private TBoldSetAsViewer? FOnSetAsViewer;
    private TBoldViewerIsChanged? fOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsViewerRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldViewerRendererData);

    /// <summary>The view adapter: OnSubscribe places the subscriptions when set, and OnGetAsViewer gives it when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldAbstractViewAdapter? GetAsViewerAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, Representation, Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsViewer is not null
            ? OnGetAsViewer(Element, Representation, Expression)
            : DefaultGetAsViewerAndSubscribe(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetAsViewer(TBoldElement? Element, TBoldAbstractViewAdapter? Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (FOnSetAsViewer is not null)
        {
            OnSetAsViewer!(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsViewer(Element, Value, Representation, Expression, VariableList);
        }
    }

    /// <summary>The renderer of viewer controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsViewerRenderer DefaultRenderer() => DefaultAsViewerRenderer ??= new TBoldAsViewerRenderer();

    /// <summary>
    /// A view adapter that reads the blob's content type, loaded from the blob (null for a null blob or no blob);
    /// raises when no registered adapter reads the content type.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested GetViewer is a private helper.")]
    public virtual TBoldAbstractViewAdapter? DefaultGetAsViewerAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (Element is null)
        {
            return null;
        }

        using var IndirectElement = new TBoldIndirectElement();
        Element.EvaluateAndSubscribeToExpression(Expression, Subscriber, IndirectElement, false, false, VariableList);
        if (IndirectElement.Value is not TBABlob blob || blob.IsNull)
        {
            return null;
        }

        TBoldAbstractViewAdapter Result = GetViewer(blob) ?? throw new EBold(SysUtils.Format(sViewerNotAvailable, blob.ContentType));
        using Stream Stream = blob.CreateBlobStream(TBoldBlobStreamMode.bmRead);
        Result.LoadFromStream(Stream);
        return Result;
    }

    private static TBoldAbstractViewAdapter? GetViewer(TBoldElement e)
    {
        for (int I = 0; I < TBoldAbstractViewAdapter.ViewAdapterClassCount(); I++)
        {
            var adapter = (TBoldAbstractViewAdapter)Activator.CreateInstance(TBoldAbstractViewAdapter.GetViewAdapterClass(I)!)!;
            if (adapter.CanReadContent(e.StringRepresentation(BoldDefs.brShort)))
            {
                return adapter;
            }

            (adapter as IDisposable)?.Dispose();
        }

        return null;
    }

    /// <summary>Saves the view adapter (and its content type) in the blob, or sets the blob to null.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DefaultSetAsViewer(TBoldElement? Element, TBoldAbstractViewAdapter? Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList)
            ?? throw new EBold(SysUtils.Format(sCannotSetValue, GetType().Name));
        var blob = (TBABlob)ValueElement;
        if (Value is not null)
        {
            using (Stream Stream = blob.CreateBlobStream(TBoldBlobStreamMode.bmWrite))
            {
                Value.SaveToStream(Stream);
            }

            blob.ContentType = Value.ContentType();
        }
        else
        {
            blob.SetToNull();
        }
    }

    /// <summary>True when a new adapter was set or the adapter's content changed.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldViewerRendererData RendererData, TBoldAbstractViewAdapter? NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        RendererData.HasChanged || (NewValue is not null && NewValue.HasChanged());

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldViewerRendererData RendererData, TBoldAbstractViewAdapter? NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        fOnIsChanged is not null
            ? fOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    /// <summary>Puts the element's view adapter in the renderer data, not changed.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var Controller = (TBoldViewerFollowerController)FollowerController;
        var data = (TBoldViewerRendererData)RendererData;
        data.ViewAdapter = GetAsViewerAndSubscribe(Element, Controller.Representation, Controller.Expression, Controller.GetVariableListAndSubscribe(Subscriber), Subscriber);
        data.HasChanged = false;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsViewer? OnGetAsViewer
    {
        get => FOnGetAsViewer;
        set => FOnGetAsViewer = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsViewer? OnSetAsViewer
    {
        get => FOnSetAsViewer;
        set => FOnSetAsViewer = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldViewerIsChanged? OnIsChanged
    {
        get => fOnIsChanged;
        set => fOnIsChanged = value;
    }
}

/// <summary>The controller of a control that shows a blob through a view adapter (an image control).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldViewerFollowerController : TBoldSingleFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldViewerFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldAsViewerRenderer? GetRenderer() => (TBoldAsViewerRenderer?)UntypedRenderer;
    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldAsViewerRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldAsViewerRenderer GetEffectiveAsViewerRenderer() => Renderer ?? TBoldAsViewerRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsViewerRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsViewerRenderer EffectiveAsViewerRenderer => GetEffectiveAsViewerRenderer();

    /// <summary>Writes the control's view adapter back.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        ReleaseChangedValue(Follower);
        SetAsViewer(GetCurrentViewer(Follower), Follower);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractViewAdapter? GetCurrentViewer(TBoldFollower Follower) => ((TBoldViewerRendererData)Follower.RendererData).ViewAdapter;

    [Delphi(PortStatus.Ported)]
    public void SetAsViewer(TBoldAbstractViewAdapter? Value, TBoldFollower Follower) =>
        EffectiveAsViewerRenderer.SetAsViewer(Follower.Element, Value, Representation, Expression, VariableList);

    /// <summary>The control has a new (or changed) view adapter: the follower becomes dirty when it changed.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(TBoldAbstractViewAdapter? NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldViewerRendererData)Follower.RendererData;
            data.ViewAdapter = NewValue;
            Follower.ControlledValueChanged(EffectiveAsViewerRenderer.IsChanged(data, NewValue, Representation, Expression, VariableList));
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAsViewerRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}

/// <summary>
/// Shows and edits one kind of content (an image format, say): it loads and saves the content, reads and writes the
/// clipboard and files, and paints itself. Subclasses register themselves; the viewer renderer picks the one that
/// reads a blob's content type. The class-level queries of Delphi (CanReadContent, Description...) are instance
/// methods here, asked of a new adapter.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractViewAdapter : TBoldMemoryManagedObject
{
    private static readonly List<Type> ViewAdapterList = [];

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractViewAdapter()
    {
        // Left for subclasses to implement
    }

    /// <summary>Registers an adapter class (which needs a constructor without parameters).</summary>
    [Delphi(PortStatus.Ported)]
    public static void RegisterViewAdapter(Type ViewAdapterClass)
    {
        lock (ViewAdapterList)
        {
            ViewAdapterList.Add(ViewAdapterClass);
        }
    }

    [Delphi(PortStatus.Ported)]
    public static int ViewAdapterClassCount()
    {
        lock (ViewAdapterList)
        {
            return ViewAdapterList.Count;
        }
    }

    [Delphi(PortStatus.Ported)]
    public static Type? GetViewAdapterClass(int index)
    {
        lock (ViewAdapterList)
        {
            return ViewAdapterList[index];
        }
    }

    [Delphi(PortStatus.Ported)] public abstract bool Empty();
    [Delphi(PortStatus.Ported)] public abstract void Clear();
    [Delphi(PortStatus.Ported)] public abstract bool HasChanged();

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual bool CanReadContent(string ContentType) => false;

    [Delphi(PortStatus.Ported)] public abstract string ContentType();

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual string Description() => "";

    [Delphi(PortStatus.Ported)] public abstract void CopyToClipboard();

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual bool CanPasteFromClipboard(string AcceptedContentType) => false;

    [Delphi(PortStatus.Ported)] public abstract void PasteFromClipboard();
    [Delphi(PortStatus.Ported)] public abstract void LoadFromStream(Stream Stream);
    [Delphi(PortStatus.Ported)] public abstract void SaveToStream(Stream Stream);

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual string DefaultExtension() => "";

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual string FileFilter() => "";

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual bool CanLoadFromFile(string Filename) => false;

    [Delphi(PortStatus.Ported)] public abstract void LoadFromFile(string Filename);
    [Delphi(PortStatus.Ported)] public abstract void SaveToFile(string Filename);
    [Delphi(PortStatus.Ported)] public abstract void Paint(TCanvas Canvas, Rectangle Rect);
    [Delphi(PortStatus.Ported)] public abstract int Width();
    [Delphi(PortStatus.Ported)] public abstract int Height();
}
