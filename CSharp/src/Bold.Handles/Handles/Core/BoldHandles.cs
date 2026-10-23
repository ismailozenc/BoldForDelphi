// Delphi: Source/Handles/Core/BoldHandles.pas
// Port map: CSharp/port/units/BoldHandles.md

using System.ComponentModel;
using static Bold.BoldAbstractModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>
/// A handle on an element: it has a value (the element), the static type it promises and the dynamic type of the
/// value it has.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldElementHandle : TBoldSubscribableComponent
{
    private bool fStrictType;

    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    protected TBoldElementHandle(IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldElementHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetDynamicBoldType() => Value?.BoldType;

    /// <summary>The type of the value, or the static type without a value.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetBoldType() => DynamicBoldType ?? StaticBoldType;

    [Delphi(PortStatus.Ported)] protected abstract TBoldElement? GetValue();
    [Delphi(PortStatus.Ported)] protected abstract TBoldSystemTypeInfo? GetStaticSystemTypeInfo();
    [Delphi(PortStatus.Ported)] protected abstract TBoldElementTypeInfo? GetStaticBoldType();

    /// <summary>Tells subscribers the value's identity changed (a type change counts as one).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void StaticBoldTypeChanged() => SendEvent(this, beValueIdentityChanged);

    /// <summary>True when the handle uses the component (its root, its system, its model...); false here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool RefersToComponent(TBoldSubscribableComponent? Component) => false;

    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo? StaticSystemTypeInfo => GetStaticSystemTypeInfo();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? BoldType => GetBoldType();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? DynamicBoldType => GetDynamicBoldType();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? StaticBoldType => GetStaticBoldType();
    [Delphi(PortStatus.Ported)] public TBoldElement? Value => GetValue();

    [Delphi(PortStatus.Ported)]
    public bool StrictType
    {
        get => fStrictType;
        set => fStrictType = value;
    }
}

/// <summary>
/// The handle on a model's runtime type info: it builds the system type info from its model on first use, and drops it
/// when the model changes.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemTypeInfoHandle : TBoldElementHandle
{
    private const int breModelDestroyed = 42;
    private const int breModelChanged = 43;

    private TBoldAbstractModel? fBoldModel;
    private TBoldPassthroughSubscriber? fModelSubscriber;
    private TBoldSystemTypeInfo? fSystemTypeInfo;
    private bool fUseGeneratedCode;
    private TBoldRegionDefinitions? fRegionDefinitions;
    private TBoldLookUpOclDefinition? fOnLookupOclDefinition;
    private bool fCheckCodeCheckSum;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldSystemTypeInfoHandle(IContainer? owner = null)
        : base(owner)
    {
        fUseGeneratedCode = true;
        CheckCodeCheckSum = true;
        fModelSubscriber = new TBoldPassthroughSubscriber(_Recieve);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSystemTypeInfoHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fModelSubscriber);
            SysUtils.FreeAndNil(ref fSystemTypeInfo);
            SysUtils.FreeAndNil(ref fRegionDefinitions);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void _Recieve(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case breModelChanged:
                ModelChanged();
                break;
            case breModelDestroyed:
                fBoldModel = null;
                fModelSubscriber!.CancelAllSubscriptions();
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldModel(TBoldAbstractModel? Value)
    {
        if (Value != fBoldModel)
        {
            fModelSubscriber!.CancelAllSubscriptions();
            if (Value is not null)
            {
                Value.AddSmallSubscription(fModelSubscriber, TBoldSmallEventSet.Of(beDestroying), breModelDestroyed);
                Value.AddSmallSubscription(fModelSubscriber, TBoldSmallEventSet.Of(beModelChanged), breModelChanged);
            }

            fBoldModel = Value;
            ModelChanged();
        }
    }

    /// <summary>Drops the type info and region definitions; the next use builds them from the model again.</summary>
    [Delphi(PortStatus.Ported, Note = "Frees the old type info after telling subscribers, as a running system needs it to shut down; Delphi freed it first.")]
    private void ModelChanged()
    {
        TBoldSystemTypeInfo? oldSystemTypeInfo = fSystemTypeInfo;
        fSystemTypeInfo = null;
        try
        {
            SysUtils.FreeAndNil(ref fRegionDefinitions);
            SendEvent(this, beValueIdentityChanged); // type change regarded as identity change
        }
        finally
        {
            SysUtils.FreeAndNil(ref oldSystemTypeInfo);
        }
    }

    /// <summary>The region definitions of the model, parsed on first use.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldRegionDefinitions? GetRegionDefinitions()
    {
        if (fRegionDefinitions is null && BoldModel is not null)
        {
            fRegionDefinitions = new TBoldRegionDefinitions();
            var Parser = new TBoldRegionParser(fRegionDefinitions, StaticSystemTypeInfo!);
            var Definitions = new TStringList();
            Definitions.Text = BoldModel.MoldModel.RegionDefinitions;
            if (!Parser.Parse(Definitions))
            {
                throw new EBoldBadLockExpression(Parser.Errors.Text);
            }
        }

        return fRegionDefinitions;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType() => GetStaticSystemTypeInfo();

    /// <summary>The system type info, built from the model on first use (generated code is not used in a designer).</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldSystemTypeInfo? GetStaticSystemTypeInfo()
    {
        if (fSystemTypeInfo is null && BoldModel is not null && BoldModel.MoldModel is not null)
        {
            fSystemTypeInfo = new TBoldSystemTypeInfo(
                BoldModel.MoldModel,
                UseGeneratedCode && !IsDesigning,
                CheckCodeCheckSum,
                BoldModel.TypeNameDictionary);
            fSystemTypeInfo.Evaluator!.SetLookupOclDefinition(fOnLookupOclDefinition);
            SendEvent(this, beValueIdentityChanged);
        }

        return fSystemTypeInfo;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue() => StaticSystemTypeInfo;

    /// <summary>Sets how the evaluator looks up named OCL definitions (now and in type infos built later).</summary>
    [Delphi(PortStatus.Ported)]
    public void InstallOclDefinitionLookUp(TBoldLookUpOclDefinition? Value)
    {
        fOnLookupOclDefinition = Value;
        fSystemTypeInfo?.Evaluator!.SetLookupOclDefinition(fOnLookupOclDefinition);
    }

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is not null && Component == BoldModel);

    [Delphi(PortStatus.Ported)] public TBoldRegionDefinitions? RegionDefinitions => GetRegionDefinitions();

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractModel? BoldModel
    {
        get => fBoldModel;
        set => SetBoldModel(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool UseGeneratedCode
    {
        get => fUseGeneratedCode;
        set => fUseGeneratedCode = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool CheckCodeCheckSum
    {
        get => fCheckCodeCheckSum;
        set => fCheckCodeCheckSum = value;
    }
}

/// <summary>A handle on a system: it can be active (have a running system) and one system handle can be the default.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractSystemHandle : TBoldElementHandle
{
    private const int breModelDestroyed = 42;
    private const int breValueIdentityChanged = 45;

    private static TBoldAbstractSystemHandle? G_DefaultBoldSystemHandle;

    private bool fIsDefault;
    private TBoldPassthroughSubscriber? fSystemTypeInfoHandleSubscriber;
    private TBoldSystemTypeInfoHandle? fSystemTypeInfoHandle;

    /// <summary>The first system handle made in a designer becomes the default.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner. A component is not in a designer while it is constructed, so in practice no handle becomes the default here.")]
    protected TBoldAbstractSystemHandle(IContainer? owner = null)
        : base(owner)
    {
        fSystemTypeInfoHandleSubscriber = new TBoldPassthroughSubscriber(_Recieve);
        if (G_DefaultBoldSystemHandle is null && IsDesigning) // only make first default at design time
        {
            IsDefault = true;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractSystemHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            if (G_DefaultBoldSystemHandle == this)
            {
                G_DefaultBoldSystemHandle = null;
            }

            IsDefault = false;
            SysUtils.FreeAndNil(ref fSystemTypeInfoHandleSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetSystemTypeInfoHandle(TBoldSystemTypeInfoHandle? Value)
    {
        if (Value != fSystemTypeInfoHandle)
        {
            if (Active)
            {
                throw new EBold(SysUtils.Format(sNotAllowedOnActiveHandle, Name));
            }

            fSystemTypeInfoHandleSubscriber!.CancelAllSubscriptions();
            if (Value is not null)
            {
                Value.AddSmallSubscription(fSystemTypeInfoHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), breModelDestroyed);
                Value.AddSmallSubscription(fSystemTypeInfoHandleSubscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breValueIdentityChanged);
            }

            fSystemTypeInfoHandle = Value;
            ModelChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _Recieve(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case breModelDestroyed:
                Active = false;
                SystemTypeInfoHandle = null;
                break;
            case breValueIdentityChanged:
                ModelChanged();
                break;
        }
    }

    /// <summary>Makes this handle the default (or stops it being the default), and its running system with it.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi cleared the default handle when switching IsDefault off only if the flag was still set after clearing it, which never held; this clears it when this handle is the default.")]
    private void setIsDefault(bool Value)
    {
        if (Value != IsDefault)
        {
            fIsDefault = Value;
            if (Value)
            {
                G_DefaultBoldSystemHandle = this;
            }
            else if (G_DefaultBoldSystemHandle == this)
            {
                G_DefaultBoldSystemHandle = null;
            }

            if (Active)
            {
                System!.IsDefault = Value;
            }
        }
    }

    [Delphi(PortStatus.Ported)] private bool GetIsDefault() => fIsDefault;

    [Delphi(PortStatus.Ported)] protected abstract bool GetActive();
    [Delphi(PortStatus.Ported)] protected abstract void SetActive(bool Value);
    [Delphi(PortStatus.Ported)] protected abstract TBoldSystem? GetSystem();

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType() => StaticSystemTypeInfo;

    [Delphi(PortStatus.Ported)] protected abstract void ModelChanged();

    [Delphi(PortStatus.Ported)]
    protected override TBoldSystemTypeInfo? GetStaticSystemTypeInfo() => SystemTypeInfoHandle?.StaticSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    public override bool RefersToComponent(TBoldSubscribableComponent? Component) =>
        base.RefersToComponent(Component) || (Component is not null && Component == SystemTypeInfoHandle);

    [Delphi(PortStatus.Ported)] public TBoldSystem? System => GetSystem();

    [Delphi(PortStatus.Ported)]
    public bool Active
    {
        get => GetActive();
        set => SetActive(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystemTypeInfoHandle? SystemTypeInfoHandle
    {
        get => fSystemTypeInfoHandle;
        set => SetSystemTypeInfoHandle(value);
    }

    /// <summary>The type info of the default system handle, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldSystemTypeInfo? DefaultBoldSystemTypeInfo() => DefaultBoldSystemHandle()?.StaticSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    public static TBoldAbstractSystemHandle? DefaultBoldSystemHandle() => G_DefaultBoldSystemHandle;

    [Delphi(PortStatus.Ported)]
    public bool IsDefault
    {
        get => GetIsDefault();
        set => setIsDefault(value);
    }
}

/// <summary>A handle that is not a system handle; its static system handle gives the types when it has no value.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldNonSystemHandle : TBoldElementHandle
{
    private const int breFreeHandle = 44;
    private const int breValueIdentityChanged = 45;

    private TBoldAbstractSystemHandle? fStaticSystemHandle;
    private TBoldPassthroughSubscriber? fStaticSystemHandleSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldNonSystemHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fStaticSystemHandleSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNonSystemHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fStaticSystemHandleSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        System.Diagnostics.Debug.Assert(Originator == StaticSystemHandle);
        switch (RequestedEvent)
        {
            case breFreeHandle:
                StaticSystemHandle = null;
                break;
            case breValueIdentityChanged:
                StaticBoldTypeChanged();
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetStaticSystemHandle(TBoldAbstractSystemHandle? Value)
    {
        if (fStaticSystemHandle != Value)
        {
            fStaticSystemHandleSubscriber!.CancelAllSubscriptions();
            fStaticSystemHandle = Value;
            if (StaticSystemHandle is not null)
            {
                StaticSystemHandle.AddSmallSubscription(fStaticSystemHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), breFreeHandle);
                StaticSystemHandle.AddSmallSubscription(fStaticSystemHandleSubscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breValueIdentityChanged);
            }

            StaticBoldTypeChanged();
        }
    }

    /// <summary>The static system handle's type info, else the default system handle's.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldSystemTypeInfo? GetStaticSystemTypeInfo()
    {
        if (StaticSystemHandle is not null)
        {
            return StaticSystemHandle.StaticSystemTypeInfo;
        }

        return TBoldAbstractSystemHandle.DefaultBoldSystemHandle() is not null ? TBoldAbstractSystemHandle.DefaultBoldSystemTypeInfo() : null;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractSystemHandle? StaticSystemHandle
    {
        get => fStaticSystemHandle;
        set => SetStaticSystemHandle(value);
    }
}
