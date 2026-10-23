// Delphi: Source/Handles/Core/BoldReferenceHandle.pas
// Port map: CSharp/port/units/BoldReferenceHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A handle whose value is set from code; it lets go of the value when it is destroyed or (by default) deleted.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldReferenceHandle : TBoldNonSystemHandle
{
    private const int breValueDestroyed = 42;
    private const int breObjectDeleted = 43;

    private string fStaticValueTypeName = "";
    private TBoldElement? fValue;
    private TBoldPassthroughSubscriber? fValueSubscriber;
    private TNotifyEvent? FOnValueDestroyed;
    private TNotifyEvent? FOnObjectDeleted;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldReferenceHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fValueSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldReferenceHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fValueSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetValue(TBoldElement? NewValue)
    {
        if (fValue != NewValue)
        {
            fValue = NewValue;
            fValueSubscriber!.CancelAllSubscriptions();
            if (fValue is not null)
            {
                fValue.AddSmallSubscription(fValueSubscriber, TBoldSmallEventSet.Of(beDestroying), breValueDestroyed);
                fValue.AddSmallSubscription(fValueSubscriber, TBoldSmallEventSet.Of(beObjectDeleted), breObjectDeleted);
            }

            SendEvent(this, beValueIdentityChanged);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetStaticValueTypeName(string Value)
    {
        if (Value != StaticValueTypeName)
        {
            fStaticValueTypeName = Value;
            StaticBoldTypeChanged();
        }
    }

    /// <summary>A deleted object is dropped unless OnObjectDeleted handles it; a destroyed value is always dropped.</summary>
    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breObjectDeleted)
        {
            if (OnObjectDeleted is not null)
            {
                OnObjectDeleted(this);
            }
            else
            {
                Value = null;
            }
        }

        if (RequestedEvent == breValueDestroyed)
        {
            Value = null;
            OnValueDestroyed?.Invoke(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue() => fValue;

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType() => StaticSystemTypeInfo?.ElementTypeInfoByExpressionName(StaticValueTypeName);

    [Delphi(PortStatus.Ported)]
    public new TBoldElement? Value
    {
        get => GetValue();
        set => SetValue(value);
    }

    [Delphi(PortStatus.Ported)]
    public string StaticValueTypeName
    {
        get => fStaticValueTypeName;
        set => SetStaticValueTypeName(value);
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnObjectDeleted
    {
        get => FOnObjectDeleted;
        set => FOnObjectDeleted = value;
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnValueDestroyed
    {
        get => FOnValueDestroyed;
        set => FOnValueDestroyed = value;
    }
}
