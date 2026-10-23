// Delphi: Source/Handles/Core/BoldCursorHandle.pas
// Port map: CSharp/port/units/BoldCursorHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>
/// A list handle on its root's value as a list: its value is the current element, which stays the same element when
/// the list changes around it (or moves to the last one when it went).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldCursorHandle : TBoldAbstractListHandle
{
    private bool fAutoFirst;
    private TBoldIndirectElement? fListElement;
    private int fCurrentIndex;
    private TBoldPassthroughSubscriber? fListSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldCursorHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fListElement = new TBoldIndirectElement();
        fListSubscriber = new TBoldPassthroughSubscriber(_ReceiveFromList);
        fCurrentIndex = -1;
        AutoFirst = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCursorHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fListSubscriber);
            SysUtils.FreeAndNil(ref fListElement);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetAutoFirst(bool Value)
    {
        fAutoFirst = Value;
        if (Value && CurrentIndex < 0 && Count > 0)
        {
            CurrentIndex = 0;
        }
    }

    /// <summary>Forgets a list that is destroyed.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveFromList(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        System.Diagnostics.Debug.Assert(OriginalEvent == beDestroying);
        if (!IsDeriving)
        {
            fListElement!.SetReferenceValue(null);
            fListSubscriber!.CancelAllSubscriptions();
            ValueIdentityChanged();
        }
    }

    /// <summary>
    /// Takes the root's value as a list and finds the current element again: the old current element where it is now,
    /// else the same index (the last element when the list got shorter, the first with AutoFirst).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        fListSubscriber!.CancelAllSubscriptions();

        TBoldElement? rootValue = EffectiveRootValue();
        if (rootValue is null)
        {
            fListElement!.SetOwnedValue(null);
        }
        else
        {
            rootValue.GetAsList(fListElement!);
        }

        if (fListElement!.Value is not null && !fListElement.OwnsValue)
        {
            fListElement.Value.AddSmallSubscription(fListSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
        }

        var TheList = (TBoldList?)fListElement.Value;
        int ListCount;
        int IndexOfOldCurrent;
        if (TheList is not null)
        {
            ListCount = TheList.Count;
            IndexOfOldCurrent = TheList.IndexOf(ResultElement.Value); // Don't ensure current!
        }
        else
        {
            ListCount = 0;
            IndexOfOldCurrent = -1;
        }

        int NewCurrentIndex;
        if (IndexOfOldCurrent != -1)
        {
            NewCurrentIndex = IndexOfOldCurrent;
        }
        else if (fCurrentIndex >= ListCount)
        {
            NewCurrentIndex = ListCount - 1;
        }
        else if (fCurrentIndex < 0 && AutoFirst && ListCount > 0)
        {
            NewCurrentIndex = 0;
        }
        else
        {
            NewCurrentIndex = fCurrentIndex;
        }

        fCurrentIndex = NewCurrentIndex;

        TBoldElement? NewValue = fCurrentIndex == -1 ? null : TheList![fCurrentIndex];
        if (NewValue != ResultElement.Value)
        {
            ResultElement.SetReferenceValue(NewValue);
            SubscribeToValue();
        }

        if (Subscriber is not null)
        {
            EffectiveRootValue()?.DefaultSubscribe(Subscriber, breReEvaluate);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetCurrentElement()
    {
        if (!Enabled)
        {
            return null;
        }

        EnsureCurrent();
        return ResultElement.Value;
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCurrentIndex()
    {
        if (!Enabled)
        {
            return -1;
        }

        EnsureCurrent();
        return fCurrentIndex;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldList? GetList()
    {
        if (!Enabled)
        {
            return null;
        }

        EnsureCurrent();
        System.Diagnostics.Debug.Assert(fListElement!.Value is TBoldList or null);
        return (TBoldList?)fListElement!.Value;
    }

    /// <summary>Makes the element at the index current (-1 for none); changing the index is an identity change.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetCurrentIndex(int NewIndex)
    {
        if (NewIndex < -1 || NewIndex >= Count) // -1 accepted as "no current element"
        {
            throw new EBold(SysUtils.Format(sIndexOutOfBounds, GetType().Name, Count - 1, NewIndex));
        }

        TBoldElement? NewValue = NewIndex == -1 ? null : List![NewIndex];
        if (NewIndex != CurrentIndex || NewValue != ResultElement.Value)
        {
            fCurrentIndex = NewIndex;
            ResultElement.SetReferenceValue(NewValue);
            SubscribeToValue();
            ValueIdentityChanged(); // changing index is an identity change
        }
    }

    /// <summary>The element type of the root's list type (or the root type when it is not a list).</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType()
    {
        TBoldElementTypeInfo? SRType = StaticRootType;
        return SRType is TBoldListTypeInfo listType ? listType.ListElementTypeInfo : SRType;
    }

    [Delphi(PortStatus.Ported)]
    public bool AutoFirst
    {
        get => fAutoFirst;
        set => SetAutoFirst(value);
    }
}
