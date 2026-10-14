// Delphi: Source/Common/Subscription/BoldDeriver.pas
// Port map: CSharp/port/units/BoldDeriver.md

namespace Bold;

/// <summary>Derives the value of the derived object, subscribing through <paramref name="Subscriber"/> (null: no subscriptions).</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldDeriveAndResubscribe(object? DerivedObject, TBoldSubscriber? Subscriber);

/// <summary>Writes a value set on the derived object back to what it is derived from.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldReverseDerive(object? DerivedObject);

[Delphi(PortStatus.Ported)]
public enum TBoldDeriverState
{
    bdsCurrent, bdsOutOfDate, bdsSubscriptionOutOfDate, bdsDeriving, bdsDerivingAndSubscribing, bdsReverseDeriving,
    bdsReverseDerivingSubscriptionOutOfDate,
}

/// <summary>
/// Keeps a derived value current. Its subscriptions mark it out of date (breReEvaluate) or its subscriptions out of date
/// (breReSubscribe); EnsureCurrent derives again, resubscribing when needed.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractDeriver : TBoldSubscriber
{
    private readonly object? fDerivedObject;
    private bool fSubscribe;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractDeriver(object? DerivedObject)
    {
        fDerivedObject = DerivedObject;
        Subscribe = true;
    }

    /// <summary>Changes state and runs the entry actions: notify when out of date, cancel or derive as the state says.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetDeriverState(TBoldDeriverState Value)
    {
        if (Value == DeriverState)
        {
            return;
        }

        TBoldDeriverState oldState = InternalDeriverState;
        InternalDeriverState = Value;

        // The old state check avoids notifying more than once.
        if (oldState != TBoldDeriverState.bdsOutOfDate && Value is TBoldDeriverState.bdsOutOfDate or TBoldDeriverState.bdsSubscriptionOutOfDate)
        {
            DoNotifyOutOfDate();
        }

        switch (DeriverState)
        {
            case TBoldDeriverState.bdsSubscriptionOutOfDate:
            case TBoldDeriverState.bdsReverseDerivingSubscriptionOutOfDate:
                CancelAllSubscriptions();
                break;
            case TBoldDeriverState.bdsDerivingAndSubscribing:
                DoDeriveAndSubscribe(true);
                break;
            case TBoldDeriverState.bdsDeriving:
                DoDeriveAndSubscribe(false);
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetSubscribe(bool value)
    {
        if (value != Subscribe)
        {
            fSubscribe = value;
            if (value)
            {
                SetDeriverState(TBoldDeriverState.bdsSubscriptionOutOfDate);
            }
            else
            {
                CancelAllSubscriptions();
                if (DeriverState == TBoldDeriverState.bdsSubscriptionOutOfDate)
                {
                    SetDeriverState(TBoldDeriverState.bdsOutOfDate);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetIsDeriving() => BoldDeriver.bdsIsDeriving.Contains(DeriverState);

    [Delphi(PortStatus.Ported)] protected abstract void SetInternalDeriverState(TBoldDeriverState Value);
    [Delphi(PortStatus.Ported)] protected abstract TBoldDeriverState GetInternalDeriverState();

    /// <summary>breReEvaluate marks the value out of date; breReSubscribe marks the subscriptions out of date.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case BoldSubscription.breReEvaluate:
                if (DeriverState == TBoldDeriverState.bdsCurrent)
                {
                    DeriverState = TBoldDeriverState.bdsOutOfDate;
                }

                break;
            case BoldSubscription.breReSubscribe:
                if (DeriverState is TBoldDeriverState.bdsCurrent or TBoldDeriverState.bdsOutOfDate)
                {
                    DeriverState = TBoldDeriverState.bdsSubscriptionOutOfDate;
                }
                else if (DeriverState == TBoldDeriverState.bdsReverseDeriving)
                {
                    DeriverState = TBoldDeriverState.bdsReverseDerivingSubscriptionOutOfDate;
                }

                break;
            default:
                throw new EBold(SysUtils.Format(BoldCommonConst.sUnknownMessageReceived, GetType().Name, RequestedEvent));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldDeriverState DeriverState
    {
        get => GetInternalDeriverState();
        set => SetDeriverState(value);
    }

    [Delphi(PortStatus.Ported)] protected abstract void DoDeriveAndSubscribe(bool subscribe);

    [Delphi(PortStatus.Ported)]
    protected virtual void DoNotifyOutOfDate()
    {
        // no action
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoReverseDerive() => throw new EBold(SysUtils.Format(BoldCommonConst.sCannotReverseDerive, GetType().Name));

    [Delphi(PortStatus.Ported)] protected abstract bool GetCanReverseDerive();

    [Delphi(PortStatus.Ported)]
    protected TBoldDeriverState InternalDeriverState
    {
        get => GetInternalDeriverState();
        set => SetInternalDeriverState(value);
    }

    [Delphi(PortStatus.Ported)]
    public void MarkSubscriptionOutOfdate() => DeriverState = TBoldDeriverState.bdsSubscriptionOutOfDate;

    /// <summary>Marks the value out of date; a deriver whose subscriptions are out of date stays so.</summary>
    [Delphi(PortStatus.Ported)]
    public void MarkOutOfdate()
    {
        if (DeriverState == TBoldDeriverState.bdsCurrent)
        {
            DeriverState = TBoldDeriverState.bdsOutOfDate;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureCurrent()
    {
        if (DeriverState is TBoldDeriverState.bdsOutOfDate or TBoldDeriverState.bdsSubscriptionOutOfDate)
        {
            Derive();
        }
    }

    /// <summary>Derives (subscribing when the subscriptions are out of date) until a derivation completes, then is current.</summary>
    [Delphi(PortStatus.Ported)]
    public void Derive()
    {
        TBoldDeriverState newState = TBoldDeriverState.bdsSubscriptionOutOfDate;
        try
        {
            do
            {
                DeriverState = DeriverState == TBoldDeriverState.bdsSubscriptionOutOfDate
                    ? TBoldDeriverState.bdsDerivingAndSubscribing
                    : TBoldDeriverState.bdsDeriving;
                if (!BoldDeriver.bdsIsDeriving.Contains(DeriverState))
                {
                    CancelAllSubscriptions();
                }
            }
            while (!BoldDeriver.bdsIsDeriving.Contains(DeriverState));

            newState = TBoldDeriverState.bdsCurrent;
        }
        finally
        {
            DeriverState = newState;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReverseDerive() => DoReverseDerive();

    [Delphi(PortStatus.Ported)] public object? DerivedObject => fDerivedObject;

    [Delphi(PortStatus.Ported)]
    public bool Subscribe
    {
        get => fSubscribe;
        set => SetSubscribe(value);
    }

    [Delphi(PortStatus.Ported)] public bool IsDeriving => GetIsDeriving();
    [Delphi(PortStatus.Ported)] public bool CanReverseDerive => GetCanReverseDerive();
}

/// <summary>A deriver whose work is done by event handlers.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldEventPluggedDeriver : TBoldAbstractDeriver
{
    private TBoldDeriveAndResubscribe? fOnDeriveAndSubscribe;
    private TBoldReverseDerive? fOnReverseDerive;
    private TBoldJustNotifyEvent? fOnNotifyOutOfDate;

    protected TBoldEventPluggedDeriver(object? DerivedObject)
        : base(DerivedObject)
    {
    }

    /// <summary>Calls OnDeriveAndSubscribe with this deriver as subscriber, or null when not subscribing.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoDeriveAndSubscribe(bool subscribe) =>
        fOnDeriveAndSubscribe?.Invoke(DerivedObject, subscribe ? this : null);

    [Delphi(PortStatus.Ported)]
    protected override void DoNotifyOutOfDate() => fOnNotifyOutOfDate?.Invoke();

    [Delphi(PortStatus.Ported)]
    protected override void DoReverseDerive()
    {
        DeriverState = TBoldDeriverState.bdsReverseDeriving;
        fOnReverseDerive?.Invoke(DerivedObject);
        switch (DeriverState)
        {
            case TBoldDeriverState.bdsReverseDeriving:
                DeriverState = TBoldDeriverState.bdsOutOfDate;
                break;
            case TBoldDeriverState.bdsReverseDerivingSubscriptionOutOfDate:
                DeriverState = TBoldDeriverState.bdsSubscriptionOutOfDate;
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetCanReverseDerive() => OnReverseDerive is not null;

    [Delphi(PortStatus.Ported)]
    public TBoldDeriveAndResubscribe? OnDeriveAndSubscribe
    {
        get => fOnDeriveAndSubscribe;
        set => fOnDeriveAndSubscribe = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldReverseDerive? OnReverseDerive
    {
        get => fOnReverseDerive;
        set => fOnReverseDerive = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldJustNotifyEvent? OnNotifyOutOfdate
    {
        get => fOnNotifyOutOfDate;
        set => fOnNotifyOutOfDate = value;
    }
}

/// <summary>A deriver that keeps its state in a field.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDeriver : TBoldEventPluggedDeriver
{
    private TBoldDeriverState fInternalDeriverState;

    public TBoldDeriver(object? DerivedObject)
        : base(DerivedObject)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetInternalDeriverState(TBoldDeriverState Value) => fInternalDeriverState = Value;

    [Delphi(PortStatus.Ported)]
    protected override TBoldDeriverState GetInternalDeriverState() => fInternalDeriverState;
}

/// <summary>Unit-level constants of BoldDeriver.</summary>
public static partial class BoldDeriver
{
    [Delphi(PortStatus.Ported, Note = "A static readonly set; C# has no set constants.")]
    public static readonly DelphiSet<TBoldDeriverState> bdsIsDeriving =
        DelphiSet<TBoldDeriverState>.Of(TBoldDeriverState.bdsDeriving, TBoldDeriverState.bdsDerivingAndSubscribing);
}
