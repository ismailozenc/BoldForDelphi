// Delphi: Source/BoldAwareGUI/ControlPacks/BoldGenericListControlPack.pas
// Port map: CSharp/port/units/BoldGenericListControlPack.md

using System.ComponentModel;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;
using static Bold.BoldSubscription;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGenericListControlPack", Symbol = "initialization", Note = "Made the default renderer; it is made on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGenericListControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The controller registered under the name, or null.</summary>
[Delphi(PortStatus.Ported, Note = "The Delphi function type; the C++Builder variant with a var parameter is not ported.")]
public delegate TBoldFollowerController? TGetFollowerControllerByNameEvent(string Name);

/// <summary>The controller for the element (looked up by name through the given function), or null for the default way.</summary>
[Delphi(PortStatus.Ported, Note = "The Delphi function type; the C++Builder variant with a var parameter is not ported.")]
public delegate TBoldFollowerController? TGetFollowerControllerEvent(TBoldElement? Element, TBoldSubscriber? Subscriber, TGetFollowerControllerByNameEvent GetFollowerControllerByName);

/// <summary>Puts the part's element for the element in ResultElement.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TGetElementEvent(TBoldGenericListPart Sender, TBoldElement? Element, TBoldSubscriber? Subscriber, TBoldIndirectElement ResultElement, bool Resubscribe);

/// <summary>A follower list that also owns elements (the computed values the rows follow), freed with it or on demand.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFollowerListWithOwnedList : TBoldFollowerList
{
    private TBoldObjectArray? FOwnedElements;

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerListWithOwnedList(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
        FOwnedElements = new TBoldObjectArray(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner, TBoldContainerOption.bcoThreadSafe));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFollowerListWithOwnedList.Destroy")]
    public override void Dispose()
    {
        FOwnedElements?.Clear();
        FOwnedElements = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public void AddOwnedElement(TBoldElement Element) => FOwnedElements!.Add(Element);

    [Delphi(PortStatus.Ported)]
    public void FreeAllOwnedElements() => FOwnedElements!.Clear();
}

/// <summary>
/// A part of a generic list: an expression gives its element (or, interpreted as a list, its elements), and an
/// expression gives the names of the controllers to try for each, ending with the default.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenericListPart : TCollectionItem
{
    private TBoldExpression FElementExpression = "";
    private TBoldExpression FControllerExpression;
    private bool FInterpretAsList;
    private TGetElementEvent? FOnGetElement;
    private TGetFollowerControllerEvent? FOnGetFollowerController;
    private bool fEnabled;
    private string fName = "";
    private TBoldPublisher? fPublisher;

    [Delphi(PortStatus.Ported)]
    public TBoldGenericListPart(TCollection? Collection)
        : base(Collection)
    {
        FControllerExpression = "oclType.asstring->union(oclType.allsupertypes.asString)->union('<Default>')"; // do not localize
        fEnabled = true;
    }

    /// <summary>Tells the subscribers the part goes; called when it leaves its collection.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldGenericListPart.Destroy")]
    public void Dispose()
    {
        fPublisher?.NotifySubscribersAndClearSubscriptions(this);
        SysUtils.FreeAndNil(ref fPublisher);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPublisher GetPublisher() => fPublisher ??= new TBoldPublisher();

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => ((TBoldGenericListParts)Collection!).FOwner.ContextType;

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldGenericListPart source)
        {
            ElementExpression = source.ElementExpression;
            ControllerExpression = source.ControllerExpression;
            InterpretAsList = source.InterpretAsList;
            OnGetElement = source.OnGetElement;
            OnGetFollowerController = source.OnGetFollowerController;
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetElementExpression(TBoldExpression Value)
    {
        if (Value != ElementExpression)
        {
            FElementExpression = Value;
            Changed(false);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetControllerExpression(TBoldExpression Value)
    {
        if (Value != ControllerExpression)
        {
            FControllerExpression = Value;
            Changed(false);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetInterpretAsList(bool Value)
    {
        if (Value != InterpretAsList)
        {
            FInterpretAsList = Value;
            Changed(false);
        }
    }

    /// <summary>Enabling or disabling the part makes the followers that use it resubscribe.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetEnabled(bool Value)
    {
        if (Value != fEnabled)
        {
            fEnabled = Value;
            Publisher.SendExtendedEvent(this, beListPartEnabledChanged, []);
        }
    }

    /// <summary>Evaluates the element expression on the element (nothing for no element).</summary>
    [Delphi(PortStatus.Ported)]
    public void DefaultGetElement(TBoldElement? Element, TBoldSubscriber? Subscriber, TBoldIndirectElement ResultElement, bool Resubscribe)
    {
        if (Element is not null)
        {
            Element.EvaluateAndSubscribeToExpression(ElementExpression, Subscriber, ResultElement, Resubscribe);
        }
        else
        {
            ResultElement.SetOwnedValue(null);
        }
    }

    /// <summary>The part's element for the element (nothing when disabled), through OnGetElement when assigned.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetElement(TBoldElement? Element, TBoldSubscriber? Subscriber, TBoldIndirectElement ResultElement, bool Resubscribe)
    {
        if (Enabled)
        {
            if (FOnGetElement is not null)
            {
                FOnGetElement(this, Element, Subscriber, ResultElement, Resubscribe);
            }
            else
            {
                DefaultGetElement(Element, Subscriber, ResultElement, Resubscribe);
            }
        }
        else
        {
            ResultElement.SetReferenceValue(null);
        }

        Publisher.AddSubscription(Subscriber, beListPartEnabledChanged, breReSubscribe);
    }

    /// <summary>
    /// The first controller found by name among the names the controller expression gives for the element, else the
    /// default controller.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested LoopList is a local function.")]
    public TBoldFollowerController? DefaultGetFollowerController(TBoldElement Element, TBoldSubscriber? Subscriber, TGetFollowerControllerByNameEvent GetFollowerControllerByName)
    {
        TBoldFollowerController? LoopList(TBoldList List)
        {
            TBoldFollowerController? result = null;
            for (int i = 0; i < List.Count; i++)
            {
                result = GetFollowerControllerByName(List[i]!.AsString);
                if (result is not null)
                {
                    break;
                }
            }

            return result;
        }

        TBoldFollowerController? Result = null;
        using (var E = new TBoldIndirectElement())
        using (var ListHolder = new TBoldIndirectElement())
        {
            Element.EvaluateAndSubscribeToExpression(ControllerExpression, Subscriber, E, false);
            if (E.Value is not null)
            {
                E.Value.GetAsList(ListHolder);
                var List = (TBoldList)ListHolder.Value!;
                Result = LoopList(List);
            }
        }

        return Result ?? GetFollowerControllerByName(BoldDefs.DEFAULTNAME);
    }

    /// <summary>The controller for the element: OnGetFollowerController's, else the default one.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi rewrote the message of the exception; here an EBold with that message wraps it.")]
    public TBoldFollowerController? GetFollowerController(TBoldElement Element, TBoldSubscriber? Subscriber, TGetFollowerControllerByNameEvent GetFollowerControllerByName)
    {
        try
        {
            TBoldFollowerController? Result = null;
            if (FOnGetFollowerController is not null)
            {
                Result = FOnGetFollowerController(Element, Subscriber, GetFollowerControllerByName);
            }

            return Result ?? DefaultGetFollowerController(Element, Subscriber, GetFollowerControllerByName);
        }
        catch (Exception E)
        {
            throw new EBold(SysUtils.Format(sCouldNotGetController, E.Message, BoldDefs.BOLDCRLF, GetNamePath()), E);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldPublisher Publisher => GetPublisher();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ContextType => GetContextType();

    [Delphi(PortStatus.Ported)]
    public TBoldExpression ElementExpression
    {
        get => FElementExpression;
        set => SetElementExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldExpression ControllerExpression
    {
        get => FControllerExpression;
        set => SetControllerExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool InterpretAsList
    {
        get => FInterpretAsList;
        set => SetInterpretAsList(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Name
    {
        get => fName;
        set => fName = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool Enabled
    {
        get => fEnabled;
        set => SetEnabled(value);
    }

    [Delphi(PortStatus.Ported)]
    public TGetElementEvent? OnGetElement
    {
        get => FOnGetElement;
        set => FOnGetElement = value;
    }

    [Delphi(PortStatus.Ported)]
    public TGetFollowerControllerEvent? OnGetFollowerController
    {
        get => FOnGetFollowerController;
        set => FOnGetFollowerController = value;
    }
}

/// <summary>The parts of a generic list controller; a change to a part changes the controller.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenericListParts : TCollection
{
    internal readonly TBoldGenericListController FOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldGenericListParts(TBoldGenericListController aOwner)
        : base(typeof(TBoldGenericListPart))
    {
        FOwner = aOwner;
    }

    [Delphi(PortStatus.Ported)]
    protected override TPersistent? GetOwner() => FOwner;

    [Delphi(PortStatus.Ported)]
    protected override void Update(TCollectionItem? Item) => FOwner.Changed();

    /// <summary>Frees a part that leaves the collection, as Delphi freed a deleted collection item.</summary>
    protected override void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
        base.Notify(Item, Action);
        if (Action == TCollectionNotification.cnExtracting && Item is TBoldGenericListPart part)
        {
            part.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldGenericListPart GetPart(int Index) => (TBoldGenericListPart)base[Index];

    [Delphi(PortStatus.Ported)]
    private void SetPart(int Index, TBoldGenericListPart Value) => base[Index] = Value;

    [Delphi(PortStatus.Ported)]
    public new TBoldGenericListPart Add() => new(this);

    [Delphi(PortStatus.Ported, Symbol = "TBoldGenericListParts.Items")]
    public new TBoldGenericListPart this[int Index]
    {
        get => GetPart(Index);
        set => SetPart(Index, value);
    }
}

/// <summary>
/// The renderer of a generic list: for each part, a row per element (or per element of its list) with the controller
/// the part picks for it; the rows' computed elements are owned by the follower list.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenericAsListRenderer : TBoldAsFollowerListRenderer
{
    private static TBoldGenericAsListRenderer? DefaultGenericAsListRenderer;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldGenericAsListRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldFollowerListWithOwnedList);

    /// <summary>Makes the rows follow the parts' elements; rows past them go.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested AddNode and RetrieveElementAndCheckIfOwned are local functions.")]
    public void MakeUptodate(TBoldFollower Follower, TBoldGenericListParts Parts, TGetFollowerControllerByNameEvent GetFollowerControllerByName)
    {
        var DestList = (TBoldFollowerListWithOwnedList)Follower.RendererData;
        var Controller = (TBoldGenericListController)Follower.Controller!;
        int DestListIndex = 0;
        TBoldGenericListPart Part = null!;

        void AddNode(TBoldElement Element)
        {
            TBoldFollowerController? FollowerController = Part.GetFollowerController(Element, Follower.Subscriber, GetFollowerControllerByName);
            if (FollowerController is not null)
            {
                DestList.EnsureFollower(Controller, DestListIndex, Element, FollowerController);
                DestListIndex++;
            }
        }

        bool RetrieveElementAndCheckIfOwned(TBoldGenericListPart part, out TBoldElement? element)
        {
            using var ie = new TBoldIndirectElement();
            part.GetElement(Follower.Element, Follower.Subscriber, ie, false);
            bool result = ie.OwnsValue;
            element = ie.OwnsValue ? ie.RelinquishValue() : ie.Value;
            return result;
        }

        DestList.FreeAllOwnedElements();
        for (int PartIndex = 0; PartIndex < Parts.Count; PartIndex++)
        {
            Part = Parts[PartIndex];
            if (RetrieveElementAndCheckIfOwned(Part, out TBoldElement? Element))
            {
                DestList.AddOwnedElement(Element!);
            }

            if (Element is not null)
            {
                TBoldList? Sourcelist = Element as TBoldList;
                if (Part.InterpretAsList && Sourcelist is not null)
                {
                    if (BoldControlPack.BoldTestType(Sourcelist, typeof(TBoldObjectList)))
                    {
                        ((TBoldObjectList)Sourcelist).EnsureRange(0, Sourcelist.Count - 1);
                    }

                    for (int SourceIndex = 0; SourceIndex < Sourcelist.Count; SourceIndex++)
                    {
                        AddNode(Sourcelist[SourceIndex]!);
                    }
                }
                else
                {
                    AddNode(Element);
                }
            }
        }

        DestList.PurgeEnd(Controller, DestListIndex);
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldGenericAsListRenderer DefaultRenderer() => DefaultGenericAsListRenderer ??= new TBoldGenericAsListRenderer();
}

/// <summary>
/// The controller of a generic list: its rows come from its parts, each with a controller looked up by name (a tree's
/// node descriptions, for instance).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenericListController : TBoldAsFollowerListController
{
    private TBoldGenericListParts? FParts;
    private TGetFollowerControllerByNameEvent FGetFollowerControllerByName;

    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    public TBoldGenericListController(object? aOwningComponent, TGetFollowerControllerByNameEvent GetFollowerControllerByNameFunc)
        : base(aOwningComponent)
    {
        FGetFollowerControllerByName = GetFollowerControllerByNameFunc;
        FParts = new TBoldGenericListParts(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldGenericListController.Destroy")]
    public override void Dispose()
    {
        FreePublisher();
        FParts?.Clear();
        FParts = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)] private TBoldGenericAsListRenderer? GetRenderer() => (TBoldGenericAsListRenderer?)UntypedRenderer;
    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldGenericAsListRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveGenericAsListRenderer;

    [Delphi(PortStatus.Ported)]
    protected TBoldGenericAsListRenderer GetEffectiveGenericAsListRenderer() => Renderer ?? TBoldGenericAsListRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    public bool CanHaveSubFollowers() => Parts.Count > 0;

    [Delphi(PortStatus.Ported)]
    private void SetParts(TBoldGenericListParts Value) => FParts!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    protected internal override void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        base.DoMakeUptodateAndSubscribe(Follower, Subscribe);
        EffectiveGenericAsListRenderer.MakeUptodate(Follower, Parts, FGetFollowerControllerByName);
    }

    [Delphi(PortStatus.Ported)]
    protected override void DoAssign(TPersistent Source)
    {
        System.Diagnostics.Debug.Assert(Source is TBoldGenericListController);
        base.DoAssign(Source);
        Parts.Assign(((TBoldGenericListController)Source).Parts);
    }

    /// <summary>The part with the name (case-insensitive), or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldGenericListPart? FindPartByName(string Name)
    {
        for (int i = 0; i < Parts.Count; i++)
        {
            if (SysUtils.SameText(Parts[i].Name, Name))
            {
                return Parts[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Sets the controller lookup after construction, for a descendant that passes one of its own methods (C# cannot
    /// pass an instance method to the base constructor).
    /// </summary>
    protected TGetFollowerControllerByNameEvent GetFollowerControllerByNameFunc
    {
        set => FGetFollowerControllerByName = value;
    }

    [Delphi(PortStatus.Ported)] protected TBoldGenericAsListRenderer EffectiveGenericAsListRenderer => GetEffectiveGenericAsListRenderer();

    [Delphi(PortStatus.Ported)]
    public TBoldGenericListParts Parts
    {
        get => FParts!;
        set => SetParts(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGenericAsListRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}
