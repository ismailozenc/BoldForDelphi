// Delphi: Source/Handles/Core/BoldRootedHandles.pas
// Port map: CSharp/port/units/BoldRootedHandles.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRootedHandles", Symbol = "TBoldRootedHandle.DefineProperties", Note = "Read old properties (TrackBold, DesignTimeContext, DesignTimeHandle) from Delphi form files; there are no form files in .NET. Set Subscribe and RootTypeName instead.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRootedHandles", Symbol = "TBoldRootedHandle.ReadTrackBold", Note = "Read the old TrackBold property from a Delphi form file; set Subscribe instead.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRootedHandles", Symbol = "TBoldRootedHandle.ReadDesignTimeContext", Note = "Read the old DesignTimeContext property from a Delphi form file; set RootTypeName instead.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRootedHandles", Symbol = "TBoldRootedHandle.ReadDesignTimeHandle", Note = "Skipped the old DesignTimeHandle property of a Delphi form file.")]

namespace Bold;

/// <summary>
/// A handle whose value is derived from the value of a root handle. A deriver keeps the value: it derives it on
/// demand, subscribes to what it used, and marks it out of date when any of that changes.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldRootedHandle : TBoldNonSystemHandle
{
    private const int breFreeHandle = 42;
    private const int breValueIdentityChanged = 43;

    private TBoldElementHandle? fInternalRootHandle;
    private bool fEnabled;
    private bool fStreamedEnabled;
    private TBoldPassthroughSubscriber? fInternalRootHandleSubscriber;
    private TBoldPassthroughSubscriber? fValueSubscriber;
    private TBoldDeriver? fDeriver;
    private TBoldIndirectElement? fResultElement;
    private string fRootTypeName = "";

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner. Delphi started disabled while its owner was loading; here BeginInit disables the handle until Loaded.")]
    protected TBoldRootedHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fInternalRootHandleSubscriber = new TBoldPassthroughSubscriber(_ReceiveFromRoot);
        fValueSubscriber = new TBoldPassthroughSubscriber(_ReceiveFromValue);
        fStreamedEnabled = true;
        fEnabled = true;
        fResultElement = new TBoldIndirectElement();
        fDeriver = new TBoldDeriver(fResultElement);
        fDeriver.OnNotifyOutOfdate = _NotifyOutOfDate;
        fDeriver.OnDeriveAndSubscribe = DeriveAndSubscribe;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRootedHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fValueSubscriber);
            SysUtils.FreeAndNil(ref fInternalRootHandleSubscriber);
            SysUtils.FreeAndNil(ref fDeriver);
            SysUtils.FreeAndNil(ref fResultElement);
        }

        base.Dispose(disposing);
    }

    /// <summary>Starts loading disabled, as Delphi's handle did while its owner was loading.</summary>
    public override void BeginInit()
    {
        base.BeginInit();
        fEnabled = false;
    }

    [Delphi(PortStatus.Ported)]
    private void SetRootTypeName(string Value)
    {
        if (Value != RootTypeName)
        {
            fRootTypeName = Value;
            StaticBoldTypeChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _ReceiveFromRoot(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == InternalRootHandle)
        {
            switch (RequestedEvent)
            {
                case breFreeHandle:
                    InternalRootHandle = null;
                    break;
                case breValueIdentityChanged:
                    EffectiveRootValueChanged();
                    break;
            }
        }
    }

    /// <summary>Drops a value that was deleted or destroyed, and tells subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveFromValue(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fResultElement!.Value && OriginalEvent is beObjectDeleted or beDestroying)
        {
            fResultElement.SetReferenceValue(null);
            if (!IsDeriving)
            {
                fValueSubscriber!.CancelAllSubscriptions();
                MarkSubscriptionOutOfDate();
                ValueIdentityChanged();
            }
        }
    }

    /// <summary>Sets the root handle; a handle cannot be its own root, directly or through other rooted handles.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetInternalRootHandle(TBoldElementHandle? Value)
    {
        if (fInternalRootHandle != Value)
        {
            if (Value == this || (Value is TBoldRootedHandle rooted && rooted.IsRootLinkedTo(this)))
            {
                throw new EBold(SysUtils.Format(sInternalRootHandle_CircRef, GetType().Name));
            }

            fInternalRootHandleSubscriber!.CancelAllSubscriptions();
            fInternalRootHandle = Value;
            if (InternalRootHandle is not null)
            {
                InternalRootHandle.AddSmallSubscription(fInternalRootHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), breFreeHandle);
                InternalRootHandle.AddSmallSubscription(fInternalRootHandleSubscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breValueIdentityChanged);
            }

            EffectiveRootValueChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _NotifyOutOfDate() => ValueIdentityChanged();

    [Delphi(PortStatus.Ported)] private bool GetSubscribe() => fDeriver!.Subscribe;
    [Delphi(PortStatus.Ported)] private bool GetIsDeriving() => fDeriver!.IsDeriving;

    /// <summary>Subscribes to the deletion of a value the handle does not own, to drop it.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SubscribeToValue()
    {
        fValueSubscriber!.CancelAllSubscriptions();
        if (!fResultElement!.OwnsValue && fResultElement.Value is not null)
        {
            fResultElement.Value.AddSmallSubscription(fValueSubscriber, TBoldSmallEventSet.Of(beObjectDeleted, beDestroying), beDestroying);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void EffectiveRootValueChanged()
    {
        System.Diagnostics.Debug.Assert(fDeriver is not null);
        MarkSubscriptionOutOfDate();
    }

    /// <summary>The root handle's value (the handle must be enabled).</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldElement? EffectiveRootValue()
    {
        System.Diagnostics.Debug.Assert(Enabled);
        return InternalRootHandle?.Value;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldSystemTypeInfo? GetStaticSystemTypeInfo() =>
        InternalRootHandle is not null ? InternalRootHandle.StaticSystemTypeInfo : base.GetStaticSystemTypeInfo();

    /// <summary>Enables the handle at the end of loading unless Enabled was set false while loading.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Loaded()
    {
        base.Loaded();
        try
        {
            if (fStreamedEnabled)
            {
                SetEnabled(true);
            }
        }
        catch (Exception) when (IsDesigning)
        {
            BoldEnvironment.BoldEffectiveEnvironment().HandleDesigntimeException(this);
        }
    }

    /// <summary>Enables or disables the handle; a disabled handle has no value. While loading this only notes the value.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void SetEnabled(bool Value)
    {
        if (IsLoading)
        {
            fStreamedEnabled = Value;
        }
        else if (Value != fEnabled)
        {
            fEnabled = Value;
            SendEvent(this, beValueIdentityChanged); // can't call ValueIdentityChanged: it should send even when disabling
            if (!Enabled)
            {
                MarkSubscriptionOutOfDate();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetSubscribe(bool Value) => fDeriver!.Subscribe = Value;

    [Delphi(PortStatus.Ported)]
    protected abstract void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber);

    [Delphi(PortStatus.Ported)]
    protected void MarkSubscriptionOutOfDate() => fDeriver!.MarkSubscriptionOutOfdate();

    [Delphi(PortStatus.Ported)]
    protected void EnsureCurrent() => fDeriver!.EnsureCurrent();

    [Delphi(PortStatus.Ported)] protected TBoldIndirectElement ResultElement => fResultElement!;

    /// <summary>The derived value (brought up to date), or null when disabled.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue()
    {
        if (Enabled)
        {
            fDeriver!.EnsureCurrent();
            return fResultElement!.Value;
        }

        return null;
    }

    /// <summary>The root handle's static type, else the type named by RootTypeName.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldElementTypeInfo? GetStaticRootType()
    {
        if (InternalRootHandle is not null)
        {
            return InternalRootHandle.StaticBoldType;
        }

        return StaticSystemTypeInfo?.ElementTypeInfoByExpressionName(RootTypeName);
    }

    [Delphi(PortStatus.Ported)]
    protected void ValueIdentityChanged()
    {
        if (Enabled)
        {
            SendEvent(this, beValueIdentityChanged);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldElementHandle? GetRootHandle() => InternalRootHandle;

    [Delphi(PortStatus.Ported)]
    protected virtual void SetRootHandle(TBoldElementHandle? Value) => InternalRootHandle = Value;

    [Delphi(PortStatus.Ported)]
    protected TBoldElementHandle? InternalRootHandle
    {
        get => fInternalRootHandle;
        set => SetInternalRootHandle(value);
    }

    [Delphi(PortStatus.Ported)] protected bool IsDeriving => GetIsDeriving();

    [Delphi(PortStatus.Ported)]
    public bool Subscribe
    {
        get => GetSubscribe();
        set => SetSubscribe(value);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void MarkOutOfDate() => MarkSubscriptionOutOfDate();

    /// <summary>True when the handle is the root, or the root of a root, of this handle.</summary>
    [Delphi(PortStatus.Ported)]
    public bool IsRootLinkedTo(TBoldElementHandle Handle)
    {
        System.Diagnostics.Debug.Assert(Handle is not null);
        if (Handle == InternalRootHandle)
        {
            return true;
        }

        return InternalRootHandle is TBoldRootedHandle rooted && rooted.IsRootLinkedTo(Handle);
    }

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is TBoldElementHandle handle && IsRootLinkedTo(handle));

    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? StaticRootType => GetStaticRootType();

    [Delphi(PortStatus.Ported)]
    public TBoldElementHandle? RootHandle
    {
        get => GetRootHandle();
        set => SetRootHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool Enabled
    {
        get => fEnabled;
        set => SetEnabled(value);
    }

    [Delphi(PortStatus.Ported)]
    public string RootTypeName
    {
        get => fRootTypeName;
        set => SetRootTypeName(value);
    }
}
