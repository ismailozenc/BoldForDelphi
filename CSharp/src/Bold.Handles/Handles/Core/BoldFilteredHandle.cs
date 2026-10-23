// Delphi: Source/Handles/Core/BoldFilteredHandle.pas
// Port map: CSharp/port/units/BoldFilteredHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>Says whether an element passes a filter.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldElementFilter(TBoldElement? Element);

/// <summary>A filter for list handles: OnFilter decides which elements pass and OnSubscribe subscribes to what it reads.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFilter : TBoldSubscribableComponentViaBoldElem
{
    private TBoldElementFilter? FOnFilter;
    private TBoldElementSubscribe? FOnSubscribe;
    private TStringList? fPreFetchRoles;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldFilter(IContainer? owner = null)
    {
        owner?.Add(this);
        fPreFetchRoles = new TStringList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFilter.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fPreFetchRoles = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)] private TStrings GetPreFetchRoles() => fPreFetchRoles!;
    [Delphi(PortStatus.Ported)] private void SetPreFetchRoles(TStrings Value) => fPreFetchRoles!.Assign(Value);

    /// <summary>True when there are roles to keep (the designer writes PreFetchRoles only then).</summary>
    [Delphi(PortStatus.Ported)]
    private bool StorePreFetchRoles() => PreFetchRoles.Count != 0;

    private bool ShouldSerializePreFetchRoles() => StorePreFetchRoles();

    [Delphi(PortStatus.Ported)]
    public virtual void Subscribe(TBoldElement boldElement, TBoldSubscriber Subscriber) => FOnSubscribe?.Invoke(boldElement, Subscriber);

    /// <summary>OnFilter's verdict; without it every element passes.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool Filter(TBoldElement? Element) => FOnFilter is null || OnFilter!(Element);

    /// <summary>Removes the elements that do not pass from the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void FilterList(TBoldList List)
    {
        for (int i = List.Count - 1; i >= 0; i--)
        {
            if (!Filter(List[i]))
            {
                List.RemoveByIndex(i);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TStrings PreFetchRoles
    {
        get => GetPreFetchRoles();
        set => SetPreFetchRoles(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElementSubscribe? OnSubscribe
    {
        get => FOnSubscribe;
        set => FOnSubscribe = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElementFilter? OnFilter
    {
        get => FOnFilter;
        set => FOnFilter = value;
    }
}

/// <summary>A rooted handle whose value is its root's list with only the elements that pass the filter.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFilteredHandle : TBoldRootedHandle
{
    private TBoldFilter? FBoldFilter;

    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    public TBoldFilteredHandle(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldFilter(TBoldFilter? NewValue)
    {
        if (NewValue != FBoldFilter)
        {
            FBoldFilter = NewValue;
            MarkSubscriptionOutOfDate();
        }
    }

    /// <summary>
    /// Builds an immutable list of the root list's elements that pass the filter, after fetching the filter's prefetch
    /// roles for all objects at once; subscribes to the list and, through the filter, to each element.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        TBoldElement? rootValue = EffectiveRootValue();
        if (rootValue is null)
        {
            ResultElement.SetOwnedValue(null);
        }
        else if (BoldFilter is null)
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

            if (SourceList.Count > 0 && SourceList is TBoldObjectList objectList)
            {
                objectList.EnsureObjects();
                var ListTypeInfo = (TBoldListTypeInfo)SourceList.BoldType!;
                var ClassTypeInfo = (TBoldClassTypeInfo)ListTypeInfo.ListElementTypeInfo!;
                for (int i = 0; i < BoldFilter.PreFetchRoles.Count; i++)
                {
                    TBoldMemberRTInfo? MemberRTInfo = ClassTypeInfo.MemberRTInfoByExpressionName(BoldFilter.PreFetchRoles[i]);
                    if (MemberRTInfo is TBoldRoleRTInfo)
                    {
                        objectList[0]!.BoldSystem.FetchLinksWithObjects(objectList, BoldFilter.PreFetchRoles[i]);
                    }
                }
            }

            for (int i = 0; i < SourceList.Count; i++)
            {
                if (BoldFilter.Filter(SourceList[i]))
                {
                    NewList.Add(SourceList[i]);
                }

                if (Subscriber is not null)
                {
                    BoldFilter.Subscribe(SourceList[i]!, Subscriber);
                }
            }

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
    public TBoldFilter? BoldFilter
    {
        get => FBoldFilter;
        set => SetBoldFilter(value);
    }
}
