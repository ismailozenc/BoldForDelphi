// Delphi: Source/BoldAwareGUI/ControlPacks/BoldListListControlPack.pas
// Port map: CSharp/port/units/BoldListListControlPack.md

using System.ComponentModel;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldListListControlPack", Symbol = "initialization", Note = "Made the default renderer; it is made on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldListListControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// The controller of a list follower whose element is a Bold list: a row follower per element (with the row
/// controller), optionally with an empty row first or last, and only the active rows' elements fetched.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractListAsFollowerListController : TBoldAsFollowerListController
{
    private bool fInternalDrag;
    private bool fDefaultDblClick;
    private readonly TBoldFollowerController fFollowerController;
    private TBoldNilElementMode fNilElementMode;

    /// <summary>A controller whose rows follow their elements with <paramref name="FollowerController"/>.</summary>
    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    protected TBoldAbstractListAsFollowerListController(object? aOwningComponent, TBoldFollowerController FollowerController)
        : base(aOwningComponent)
    {
        fFollowerController = FollowerController;
        DragMode = TBoldDragMode.bdgSelection;
        DropMode = TBoldDropMode.bdpAppend;
        fDefaultDblClick = true;
        fNilElementMode = TBoldNilElementMode.neNone;
        fInternalDrag = true;
    }

    [Delphi(PortStatus.Ported)]
    protected override void DoAssign(TPersistent Source)
    {
        System.Diagnostics.Debug.Assert(Source is TBoldAbstractListAsFollowerListController);
        base.DoAssign(Source);
        var source = (TBoldAbstractListAsFollowerListController)Source;
        DefaultDblClick = source.DefaultDblClick;
        InternalDrag = source.InternalDrag;
        NilElementMode = source.NilElementMode;
    }

    /// <summary>Brings the rows up to date with the list, and subscribes to the list.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        base.DoMakeUptodateAndSubscribe(Follower, Subscribe);
        ((TBoldListAsFollowerListRenderer)EffectiveRenderer).MakeUptodate(Follower, fFollowerController);
        if (Subscribe && Follower.Element is not null)
        {
            Follower.Element.SubscribeToExpression("", Follower.Subscriber, false);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldListAsFollowerListRenderer? GetRenderer() => (TBoldListAsFollowerListRenderer?)UntypedRenderer;
    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldListAsFollowerListRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    private void SetNilElementMode(TBoldNilElementMode Value)
    {
        if (fNilElementMode != Value)
        {
            fNilElementMode = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveListAsFollowerListRenderer;

    [Delphi(PortStatus.Ported)]
    protected TBoldListAsFollowerListRenderer GetEffectiveListAsFollowerListRenderer() => Renderer ?? TBoldListAsFollowerListRenderer.DefaultRenderer();

    /// <summary>The index in the list of the current row (an empty first row is not in the list).</summary>
    [Delphi(PortStatus.Ported)]
    public int GetListIndex(TBoldFollower Follower)
    {
        int Result = Follower.CurrentIndex;
        if (NilElementMode == TBoldNilElementMode.neInsertFirst && Result >= 0)
        {
            Result--;
        }

        return Result;
    }

    /// <summary>The row of the list index: one less with an empty first row.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi names the result a row index too; with an empty first row it is the list index less one.")]
    public int ListIndexToIndex(TBoldFollower Follower, int ListIndex)
    {
        if (NilElementMode == TBoldNilElementMode.neInsertFirst)
        {
            return ListIndex - 1;
        }

        return ListIndex;
    }

    /// <summary>The list index of the row: one more with an empty first row (int.MaxValue stays as it is).</summary>
    [Delphi(PortStatus.Ported)]
    public int ListIndex(int index)
    {
        if (index == int.MaxValue)
        {
            return index; // otherwise the result will overflow to -maxint
        }

        if (NilElementMode == TBoldNilElementMode.neInsertFirst)
        {
            return index + 1;
        }

        return index;
    }

    internal TBoldNilElementMode GetNilElementMode() => fNilElementMode;

    [Delphi(PortStatus.Ported)] protected TBoldListAsFollowerListRenderer EffectiveListAsFollowerListRenderer => GetEffectiveListAsFollowerListRenderer();

    [Delphi(PortStatus.Ported)]
    protected bool DefaultDblClick
    {
        get => fDefaultDblClick;
        set => fDefaultDblClick = value;
    }

    [Delphi(PortStatus.Ported, Note = "Delphi redeclares the property with a field of its own, which hides the inherited InternalDrag; "
        + "drag and drop through the inherited controller methods sees the inherited one. Kept as in Delphi.")]
    protected new bool InternalDrag
    {
        get => fInternalDrag;
        set => fInternalDrag = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldListAsFollowerListRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldNilElementMode NilElementMode
    {
        get => fNilElementMode;
        set => SetNilElementMode(value);
    }
}

/// <summary>A list-as-follower-list controller with its settings published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListAsFollowerListController : TBoldAbstractListAsFollowerListController
{
    /// <summary>A controller whose rows follow their elements with <paramref name="FollowerController"/>.</summary>
    public TBoldListAsFollowerListController(object? aOwningComponent, TBoldFollowerController FollowerController)
        : base(aOwningComponent, FollowerController)
    {
    }

    public new bool DefaultDblClick
    {
        get => base.DefaultDblClick;
        set => base.DefaultDblClick = value;
    }

    public new bool InternalDrag
    {
        get => base.InternalDrag;
        set => base.InternalDrag = value;
    }

    public new TBoldListAsFollowerListRenderer? Renderer
    {
        get => base.Renderer;
        set => base.Renderer = value;
    }

    public new TBoldNilElementMode NilElementMode
    {
        get => base.NilElementMode;
        set => base.NilElementMode = value;
    }
}

/// <summary>
/// The renderer of a list follower whose element is a Bold list: keeps a row follower per element, fetches the active
/// rows' objects, and drags and drops the selected objects.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldListAsFollowerListRenderer : TBoldAsFollowerListRenderer
{
    private static TBoldListAsFollowerListRenderer? DefaultListAsFollowerListRenderer;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldListAsFollowerListRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>
    /// Fetches the objects of the rows from FirstActive to LastActive (in blocks of RangeBuffer), gives the rows whose
    /// element is not valid their element, and makes those rows active.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void SetActiveRange(TBoldFollower Follower, int FirstActive, int LastActive, int RangeBuffer = 1)
    {
        if (Follower.Element is null)
        {
            return;
        }

        System.Diagnostics.Debug.Assert(BoldControlPack.BoldTestType(Follower.Element, typeof(TBoldList)));
        var list = (TBoldList)Follower.Element;
        var FollowerController = (TBoldAbstractListAsFollowerListController)Follower.Controller!;
        int FirstToEnsure = FollowerController.ListIndex(FirstActive);
        int LastToEnsure = FollowerController.ListIndex(LastActive);

        if (RangeBuffer > 1)
        {
            FirstToEnsure = FirstToEnsure / RangeBuffer * RangeBuffer;
            LastToEnsure = (LastToEnsure / RangeBuffer + 1) * RangeBuffer;
        }

        if (FirstToEnsure < 0)
        {
            FirstToEnsure = 0;
        }

        if (FirstToEnsure >= list.Count)
        {
            FirstToEnsure = list.Count - 1;
        }

        if (LastToEnsure >= list.Count)
        {
            LastToEnsure = list.Count - 1;
        }

        list.EnsureRange(FirstToEnsure, LastToEnsure);

        int FirstFollowerActive = FirstActive;
        int LastFollowerActive = LastActive;

        if (FirstFollowerActive >= Follower.SubFollowerCount)
        {
            FirstFollowerActive = Follower.SubFollowerCount - 1;
        }

        if (FirstFollowerActive >= list.Count)
        {
            FirstFollowerActive = list.Count - 1;
        }

        if (FirstFollowerActive < 0)
        {
            FirstFollowerActive = 0;
        }

        if (LastFollowerActive >= Follower.SubFollowerCount)
        {
            LastFollowerActive = Follower.SubFollowerCount - 1;
        }

        if (LastFollowerActive >= list.Count)
        {
            LastFollowerActive = list.Count - 1;
        }

        for (int i = FirstFollowerActive; i <= LastFollowerActive; i++)
        {
            TBoldFollower subFollower = Follower.SubFollowers(i)!;
            if (!subFollower.ElementValid)
            {
                subFollower.Element = list[FollowerController.ListIndex(i)];
            }
        }

        base.SetActiveRange(Follower, FirstFollowerActive, LastFollowerActive);
    }

    /// <summary>The renderer of list controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldListAsFollowerListRenderer DefaultRenderer() => DefaultListAsFollowerListRenderer ??= new TBoldListAsFollowerListRenderer();

    /// <summary>
    /// Makes the rows follow the list: the empty first row, a row per element (rows outside the active range get no
    /// element yet), the empty last row; rows past them go.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested AddElement is a local function.")]
    public void MakeUptodate(TBoldFollower Follower, TBoldFollowerController FollowerController)
    {
        var DestList = (TBoldFollowerList)Follower.RendererData;
        var Controller = (TBoldAbstractListAsFollowerListController)Follower.Controller!;
        int DestIndex = 0;

        void AddElement(TBoldElement? aElement)
        {
            DestList.EnsureFollower(Controller, DestIndex, aElement, FollowerController);
            DestIndex++;
        }

        if (Controller.GetNilElementMode() == TBoldNilElementMode.neInsertFirst)
        {
            AddElement(null);
        }

        if (Follower.Element is not null)
        {
            var SourceList = (TBoldList)Follower.Element;
            SourceList.EnsureRange(Controller.ListIndex(DestList.FirstActive), Controller.ListIndex(DestList.LastActive));
            for (int SourceIndex = 0; SourceIndex < SourceList.Count; SourceIndex++)
            {
                if (DestIndex >= DestList.FirstActive && DestIndex <= DestList.LastActive)
                {
                    AddElement(SourceList[SourceIndex]);
                }
                else
                {
                    AddElement(null);
                    DestList.Followers(DestIndex - 1)!.ElementValid = false;
                }
            }
        }

        if (Controller.GetNilElementMode() == TBoldNilElementMode.neAddLast)
        {
            AddElement(null);
        }

        DestList.PurgeEnd(Controller, DestIndex);
    }

    /// <summary>Drags the objects of the selected rows.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DefaultStartDrag(TBoldElement? Element, TBoldDragMode DragMode, TBoldRendererData RendererData)
    {
        if (DragMode == TBoldDragMode.bdgSelection)
        {
            if (BoldGUI.BoldGUIHandler().DraggedObjects.Count != 0)
            {
                throw new EBold(SysUtils.Format(sDraggedObjectsNotCleared, GetType().Name));
            }

            var Followerlist = (TBoldFollowerList)RendererData;
            Followerlist.AddSelectedToList(BoldGUI.BoldGUIHandler().DraggedObjects);
        }
    }

    /// <summary>True when there are dragged objects the list accepts.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool DefaultDragOver(TBoldElement? Element, TBoldDropMode DropMode, bool InternalDrag, TBoldRendererData RendererData, int dropindex) =>
        BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0 &&
        BoldGUI.BoldGUIHandler().DraggedObjectsAssignable(Element, DropMode);

    /// <summary>
    /// Drops the dragged objects on the list: inserted at the index (objects already in the list move there), or
    /// appended; replacing is not implemented.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void DefaultDragDrop(TBoldElement? Element, TBoldDropMode DropMode, int dropindex)
    {
        var ObjectList = (TBoldObjectList)Element!;
        TBoldObjectList dragged = BoldGUI.BoldGUIHandler().DraggedObjects;
        const int Offset = 0;
        switch (DropMode)
        {
            case TBoldDropMode.bdpInsert:
                if (dropindex < 0)
                {
                    dropindex = 0;
                }

                if (dropindex > ObjectList.Count - 1)
                {
                    dropindex = ObjectList.Count - 1;
                }

                for (int I = 0; I < dragged.Count; I++)
                {
                    int prevIndex = ObjectList.IndexOf(dragged[I]);
                    if (prevIndex == -1)
                    {
                        if (dropindex + Offset >= 0 && dropindex + Offset < ObjectList.Count)
                        {
                            ObjectList.Insert(dropindex + Offset, dragged[I]);
                        }
                        else
                        {
                            ObjectList.Add(dragged[I]);
                        }

                        dropindex++;
                    }
                    else
                    {
                        ObjectList.Move(prevIndex, dropindex);
                        if (prevIndex > dropindex)
                        {
                            dropindex++;
                        }
                    }
                }

                break;

            case TBoldDropMode.bdpAppend:
                for (int I = 0; I < dragged.Count; I++)
                {
                    // Dupe checking by the ObjectList
                    ObjectList.Add(dragged[I]);
                }

                break;

            case TBoldDropMode.bdpReplace:
                throw new EBoldFeatureNotImplementedYet(SysUtils.Format(sReplaceNotImplemented, GetType().Name));
        }
    }
}
