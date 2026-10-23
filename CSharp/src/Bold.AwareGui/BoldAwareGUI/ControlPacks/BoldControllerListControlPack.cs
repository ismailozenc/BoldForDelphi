// Delphi: Source/BoldAwareGUI/ControlPacks/BoldControllerListControlPack.pas
// Port map: CSharp/port/units/BoldControllerListControlPack.md

using System.ComponentModel;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldControllerListControlPack", Symbol = "initialization", Note = "Made the default renderer; it is made on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldControllerListControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// A list of controllers (the columns of a grid row): its follower has a sub-follower per controller, all following
/// the same element. The list owns its controllers.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldControllerList : TBoldAsFollowerListController
{
    private List<TBoldFollowerController>? FList;

    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    public TBoldControllerList(object? aOwningComponent)
        : base(aOwningComponent)
    {
        FList = [];
        DragMode = TBoldDragMode.bdgSelection;
        DropMode = TBoldDropMode.bdpInsert;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldControllerList.Destroy")]
    public override void Dispose()
    {
        if (FList is not null)
        {
            for (int I = 0; I < Count; I++)
            {
                this[I].Dispose();
            }

            FList = null;
        }

        base.Dispose();
    }

    /// <summary>Gives the follower a sub-follower per controller, and subscribes to the element.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        base.DoMakeUptodateAndSubscribe(Follower, Subscribe);
        ((TBoldControllerListAsFollowerListRenderer)EffectiveRenderer).MakeUptodate(Follower, Follower.Element);
        if (Subscribe && Follower.Element is not null)
        {
            Follower.Element.SubscribeToExpression("", Follower.Subscriber, false);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFollowerController GetSubController(int index) => FList![index];

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldFollowerController BoldFollowerController)
    {
        FList!.Add(BoldFollowerController);
        Changed();
    }

    /// <summary>Takes the controller out of the list (without disposing it).</summary>
    [Delphi(PortStatus.Ported)]
    public void Remove(TBoldFollowerController BoldFollowerController)
    {
        FList!.Remove(BoldFollowerController);
        Changed();
    }

    /// <summary>Disposes the controller at the index and takes it out of the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void Delete(int index)
    {
        this[index].Dispose();
        FList!.RemoveAt(index);
        Changed();
    }

    [Delphi(PortStatus.Ported)]
    public void Move(int CurIndex, int ToIndex)
    {
        TBoldFollowerController item = FList![CurIndex];
        FList.RemoveAt(CurIndex);
        FList.Insert(ToIndex, item);
        Changed();
    }

    [Delphi(PortStatus.Ported)]
    private int GetCount() => FList!.Count;

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => GetEffectiveDisplayPropertyListRenderer();

    [Delphi(PortStatus.Ported)]
    protected TBoldControllerListAsFollowerListRenderer GetEffectiveDisplayPropertyListRenderer() =>
        TBoldControllerListAsFollowerListRenderer.DefaultRenderer(); // currently always uses default.

    [Delphi(PortStatus.Ported)] public int Count => GetCount();

    [Delphi(PortStatus.Ported, Symbol = "TBoldControllerList.Items")]
    public TBoldFollowerController this[int index] => GetSubController(index);
}

/// <summary>The renderer of a controller list: a sub-follower per controller, all following the element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldControllerListAsFollowerListRenderer : TBoldAsFollowerListRenderer
{
    private static TBoldControllerListAsFollowerListRenderer? DefaultDisplayPropertyListRenderer;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldControllerListAsFollowerListRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldControllerListAsFollowerListRenderer DefaultRenderer() =>
        DefaultDisplayPropertyListRenderer ??= new TBoldControllerListAsFollowerListRenderer();

    [Delphi(PortStatus.Ported)]
    public void MakeUptodate(TBoldFollower Follower, TBoldElement? Element)
    {
        var DestList = (TBoldFollowerList)Follower.RendererData;
        var SourceList = (TBoldControllerList)Follower.Controller!;
        for (int sourceIndex = 0; sourceIndex < SourceList.Count; sourceIndex++)
        {
            DestList.EnsureFollower(SourceList, sourceIndex, Element, SourceList[sourceIndex]);
        }

        DestList.PurgeEnd(SourceList, SourceList.Count);
    }
}
