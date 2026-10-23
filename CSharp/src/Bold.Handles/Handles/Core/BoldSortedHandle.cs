// Delphi: Source/Handles/Core/BoldSortedHandle.pas
// Port map: CSharp/port/units/BoldSortedHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A comparer for list handles: OnCompare orders elements and OnSubscribe subscribes to what it reads.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldComparer : TBoldSubscribableComponentViaBoldElem
{
    private TBoldElementCompare? FOnCompare;
    private TBoldElementSubscribe? FOnSubscribe;
    private TBoldPrepareListOperation? fOnPrepareSort;

    /// <summary>Adds the comparer to <paramref name="Owner"/> when given.</summary>
    public TBoldComparer(IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Subscribe(TBoldElement boldElement, TBoldSubscriber Subscriber) => FOnSubscribe?.Invoke(boldElement, Subscriber);

    /// <summary>OnCompare's order; without it all elements are equal.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual int Compare(TBoldElement? Item1, TBoldElement? Item2) => FOnCompare is not null ? OnCompare!(Item1, Item2) : 0;

    /// <summary>Sorts an object list.</summary>
    [Delphi(PortStatus.Ported)]
    public void SortList(TBoldList List)
    {
        if (List is TBoldObjectList objectList)
        {
            objectList.Sort(Compare);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void PrepareSort(TBoldList List) => OnPrepareSort?.Invoke(List);

    [Delphi(PortStatus.Ported)]
    public TBoldElementSubscribe? OnSubscribe
    {
        get => FOnSubscribe;
        set => FOnSubscribe = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElementCompare? OnCompare
    {
        get => FOnCompare;
        set => FOnCompare = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPrepareListOperation? OnPrepareSort
    {
        get => fOnPrepareSort;
        set => fOnPrepareSort = value;
    }
}

/// <summary>A rooted handle whose value is its root's list sorted by the comparer.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSortedHandle : TBoldRootedHandle
{
    private TBoldComparer? FBoldComparer;

    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    public TBoldSortedHandle(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldComparer(TBoldComparer? NewValue)
    {
        if (NewValue != FBoldComparer)
        {
            FBoldComparer = NewValue;
            MarkSubscriptionOutOfDate();
        }
    }

    /// <summary>Builds an immutable sorted copy of the root list; subscribes to the list and, through the comparer, to each element.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        TBoldElement? rootValue = EffectiveRootValue();
        if (rootValue is null)
        {
            ResultElement.SetOwnedValue(null);
        }
        else if (BoldComparer is null)
        {
            rootValue.GetAsList(ResultElement);
        }
        else
        {
            using var ValueAsListHolder = new TBoldIndirectElement();
            rootValue.GetAsList(ValueAsListHolder);
            var SourceList = (TBoldList)ValueAsListHolder.Value!;
            var NewList = (TBoldList)TBoldMemberFactory.CreateMemberFromBoldType(SourceList.BoldType);
            NewList.DuplicateMode = TBoldListDupMode.bldmAllow;
            if (Subscriber is not null)
            {
                SourceList.DefaultSubscribe(Subscriber, breReSubscribe);
            }

            SourceList.EnsureRange(0, SourceList.Count - 1);
            BoldComparer.PrepareSort(SourceList);
            for (int i = 0; i < SourceList.Count; i++)
            {
                NewList.Add(SourceList[i]);
                if (Subscriber is not null)
                {
                    BoldComparer.Subscribe(SourceList[i]!, Subscriber);
                }
            }

            NewList.Sort(BoldComparer.Compare);
            NewList.MakeImmutable();
            ResultElement.SetOwnedValue(NewList);
        }

        SubscribeToValue();
    }

    /// <summary>The root's list type (the list type of the root type when that is not a list).</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType()
    {
        TBoldElementTypeInfo? SRType = StaticRootType;
        if (SRType is null)
        {
            return null;
        }

        return SRType is TBoldListTypeInfo ? SRType : ((TBoldSystemTypeInfo)SRType.SystemTypeInfo!).ListTypeInfoByElement(SRType);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldComparer? BoldComparer
    {
        get => FBoldComparer;
        set => SetBoldComparer(value);
    }
}
