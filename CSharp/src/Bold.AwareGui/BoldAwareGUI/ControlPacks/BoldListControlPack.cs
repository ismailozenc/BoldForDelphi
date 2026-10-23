// Delphi: Source/BoldAwareGUI/ControlPacks/BoldListControlPack.pas
// Port map: CSharp/port/units/BoldListControlPack.md

using System.ComponentModel;

namespace Bold;

/// <summary>
/// The controller of a list of followers (the rows of a list box or grid): the rows' insertion and deletion events,
/// the active range (the rows that are shown) and selection.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsFollowerListController : TBoldFollowerController
{
    private TBoldSubFollowerEvent? fOnBeforeInsertItem;
    private TBoldFollowerEvent? fOnAfterInsertItem;
    private TBoldFollowerEvent? fOnBeforeDeleteItem;
    private TBoldSubFollowerEvent? fOnAfterDeleteItem;

    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    public TBoldAsFollowerListController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal void DoBeforeInsertItem(int index, TBoldFollower OwningFollower) => fOnBeforeInsertItem?.Invoke(index, OwningFollower);

    [Delphi(PortStatus.Ported)]
    protected internal void DoAfterInsertItem(TBoldFollower Follower) => fOnAfterInsertItem?.Invoke(Follower);

    [Delphi(PortStatus.Ported)]
    protected internal void DoBeforeDeleteItem(TBoldFollower Follower) => fOnBeforeDeleteItem?.Invoke(Follower);

    [Delphi(PortStatus.Ported)]
    protected internal void DoAfterDeleteItem(int index, TBoldFollower OwningFollower) => fOnAfterDeleteItem?.Invoke(index, OwningFollower);

    /// <summary>Drops the rows when the renderer data goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void CleanRendererData(TBoldRendererData RendererData)
    {
        base.CleanRendererData(RendererData);
        ((TBoldFollowerList)RendererData).PurgeEnd(this, 0);
    }

    /// <summary>Makes the rows from FirstActive to LastActive active (shown), through the renderer.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetActiveRange(TBoldFollower Follower, int FirstActive, int LastActive, int RangeBuffer = 1) =>
        ((TBoldAsFollowerListRenderer)EffectiveRenderer).SetActiveRange(Follower, FirstActive, LastActive, RangeBuffer);

    [Delphi(PortStatus.Ported)]
    public void SelectAll(TBoldFollower Follower, bool SetSelect) => ((TBoldFollowerList)Follower.RendererData).SelectAll(SetSelect);

    [Delphi(PortStatus.Ported)]
    public void SelectRange(TBoldFollower Follower, int index) => ((TBoldFollowerList)Follower.RendererData).SelectRange(index);

    [Delphi(PortStatus.Ported)]
    public void SetSelected(TBoldFollower Follower, int index, bool Value) => ((TBoldFollowerList)Follower.RendererData).SetSelected(index, Value);

    [Delphi(PortStatus.Ported)]
    public void ToggleSelected(TBoldFollower Follower, int index)
    {
        var list = (TBoldFollowerList)Follower.RendererData;
        list.SetSelected(index, !list.Selected(index));
    }

    [Delphi(PortStatus.Ported)]
    public bool GetSelected(TBoldFollower Follower, int index) => ((TBoldFollowerList)Follower.RendererData).Selected(index);

    [Delphi(PortStatus.Ported)]
    public TBoldSubFollowerEvent? OnBeforeInsertItem
    {
        get => fOnBeforeInsertItem;
        set => fOnBeforeInsertItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? OnAfterInsertItem
    {
        get => fOnAfterInsertItem;
        set => fOnAfterInsertItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? OnBeforeDeleteItem
    {
        get => fOnBeforeDeleteItem;
        set => fOnBeforeDeleteItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSubFollowerEvent? OnAfterDeleteItem
    {
        get => fOnAfterDeleteItem;
        set => fOnAfterDeleteItem = value;
    }
}

/// <summary>The renderer of a list of followers: its renderer data is the list of row followers.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsFollowerListRenderer : TBoldRenderer
{
    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsFollowerListRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldFollowerList);

    [Delphi(PortStatus.Ported)]
    public virtual void SetActiveRange(TBoldFollower Follower, int FirstActive, int LastActive, int RangeBuffer = 1) =>
        ((TBoldFollowerList)Follower.RendererData).SetActiveRange(FirstActive, LastActive);
}

/// <summary>
/// The rows of a list follower: a follower per row (it owns them), which rows are active, which are selected and which
/// is current.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldFollowerList : TBoldRendererData
{
    private readonly List<TBoldFollower> FArray = [];
    private int FFirstActive;
    private int FLastActive;
    private int FCurrentItem;
    private int FPrevSelected;

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerList(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
        FLastActive = int.MaxValue;
        FCurrentItem = -1;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFollowerList.Destroy")]
    public override void Dispose()
    {
        foreach (TBoldFollower follower in FArray)
        {
            follower.Dispose();
        }

        FArray.Clear();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private int GetSelectedCount()
    {
        int Result = 0;
        for (int Row = 0; Row < Count; Row++)
        {
            if (Selected(Row))
            {
                Result++;
            }
        }

        return Result;
    }

    /// <summary>Selects or unselects the row; a selected row is where SelectRange starts.</summary>
    [Delphi(PortStatus.Ported, Note = "The setter of the indexed property Selected.")]
    public void SetSelected(int index, bool V)
    {
        if (index > -1 && index < Count && Followers(index)!.Selected != V)
        {
            if (V)
            {
                FPrevSelected = index;
            }

            Followers(index)!.Selected = V;
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetSelected(int index) => Followers(index)!.Selected;

    /// <summary>Inserts the row follower at the index, between the controller's insertion events.</summary>
    [Delphi(PortStatus.Ported)]
    private void Insert(TBoldAsFollowerListController ListFollowerController, int Index, TBoldFollower Follower)
    {
        ListFollowerController.DoBeforeInsertItem(Index, OwningFollower);
        FArray.Insert(Index, Follower);
        for (int I = Index; I < Count; I++)
        {
            Followers(I)!.index = I;
        }

        ListFollowerController.DoAfterInsertItem(Followers(Index)!);
    }

    /// <summary>Frees the row follower at the index, between the controller's deletion events.</summary>
    [Delphi(PortStatus.Ported)]
    private void Delete(TBoldAsFollowerListController ListFollowerController, int index)
    {
        ListFollowerController.DoBeforeDeleteItem(Followers(index)!);
        Followers(index)!.Dispose();
        FArray.RemoveAt(index);
        for (int I = index; I < Count; I++)
        {
            Followers(I)!.index = I;
        }

        ListFollowerController.DoAfterDeleteItem(index, OwningFollower);
    }

    /// <summary>Makes the active rows' followers displayable.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void EnsureSubfollowersDisplayable()
    {
        for (int I = 0; I < Count; I++)
        {
            TBoldFollower follower = Followers(I)!;
            if (follower.Active)
            {
                follower.EnsureDisplayable();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override int GetSubFollowerCount() => FArray.Count;

    /// <summary>The row follower at the index, or null outside the list.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override TBoldFollower? GetSubFollower(int Index) => Index >= 0 && Index < FArray.Count ? FArray[Index] : null;

    [Delphi(PortStatus.Ported)]
    protected internal override int GetCurrentSubFollowerIndex() => FCurrentItem;

    [Delphi(PortStatus.Ported)]
    protected internal override void SetCurrentSubFollowerIndex(int index)
    {
        base.SetCurrentSubFollowerIndex(index);
        FCurrentItem = index;
    }

    /// <summary>Makes the rows from FirstActive to LastActive active, and the others inactive.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetActiveRange(int FirstActive, int LastActive)
    {
        FFirstActive = FirstActive;
        FLastActive = LastActive;
        for (int I = 0; I < Count; I++)
        {
            Followers(I)!.Active = I >= FirstActive && I <= LastActive;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SelectAll(bool SetSelect)
    {
        for (int Row = 0; Row < Count; Row++)
        {
            Followers(Row)!.Selected = SetSelect;
        }
    }

    /// <summary>Selects the rows from the last selected row to the index (only those).</summary>
    [Delphi(PortStatus.Ported)]
    public void SelectRange(int index)
    {
        SelectAll(false);
        int pSelect = FPrevSelected;
        for (int Row = FPrevSelected; Row <= index; Row++)
        {
            SetSelected(Row, true);
        }

        for (int Row = FPrevSelected; Row >= index; Row--)
        {
            SetSelected(Row, true);
        }

        FPrevSelected = pSelect;
    }

    /// <summary>Adds the objects of the selected rows to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddSelectedToList(TBoldList BoldList)
    {
        for (int Row = 0; Row < Count; Row++)
        {
            TBoldFollower follower = Followers(Row)!;
            if (follower.Selected && follower.Element is not null && BoldControlPack.BoldTestType(follower.Element, typeof(TBoldObject)))
            {
                BoldList.Add(follower.Element);
            }
        }
    }

    /// <summary>Deletes the rows from PurgeCount on, in one notification.</summary>
    [Delphi(PortStatus.Ported)]
    public void PurgeEnd(TBoldAsFollowerListController ListFollowerController, int PurgeCount)
    {
        TBoldPublisher.StartNotify();
        try
        {
            for (int I = Count - 1; I >= PurgeCount; I--)
            {
                Delete(ListFollowerController, I);
            }
        }
        finally
        {
            TBoldPublisher.EndNotify();
        }
    }

    /// <summary>
    /// Makes the row at the index follow the element with the controller: keeps a matching row, deletes the row when
    /// the next one matches (an element went), else inserts a new row (an element came). May disturb the rows after it.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureFollower(TBoldAsFollowerListController ListFollowerController, int Index, TBoldElement? Element, TBoldFollowerController Controller)
    {
        if (Element is null && Index < Count && Followers(Index)!.Controller == Controller)
        {
            Followers(Index)!.Element = Element;
        }

        if (Index < Count && Followers(Index)!.Element == Element && Followers(Index)!.Controller == Controller)
        {
            // all ok
        }
        else if (Index + 1 < Count && Followers(Index + 1)!.Element == Element && Followers(Index + 1)!.Controller == Controller)
        {
            Delete(ListFollowerController, Index);
        }
        else
        {
            Insert(ListFollowerController, Index, new TBoldFollower(OwningFollower, Controller, Element));
        }

        Followers(Index)!.Active = Index >= FirstActive && Index <= LastActive;
    }

    [Delphi(PortStatus.Ported, Note = "The default indexed property in Delphi; here a method.")]
    public TBoldFollower? Followers(int index) => GetSubFollower(index);

    [Delphi(PortStatus.Ported)] public int Count => GetSubFollowerCount();
    [Delphi(PortStatus.Ported)] public int FirstActive => FFirstActive;
    [Delphi(PortStatus.Ported)] public int LastActive => FLastActive;
    [Delphi(PortStatus.Ported)] public int SelectedCount => GetSelectedCount();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a getter method, with SetSelected to set.")]
    public bool Selected(int index) => GetSelected(index);

    [Delphi(PortStatus.Ported)]
    public int CurrentItem
    {
        get => FCurrentItem;
        set => FCurrentItem = value;
    }
}
