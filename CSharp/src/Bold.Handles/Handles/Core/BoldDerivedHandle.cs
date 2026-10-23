// Delphi: Source/Handles/Core/BoldDerivedHandle.pas
// Port map: CSharp/port/units/BoldDerivedHandle.md

using System.ComponentModel;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>Derives the value of a derived handle from its root value, and places the subscriptions it depends on.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldHandleDeriveAndSubscribe(Component Sender, TBoldElement? RootValue, TBoldIndirectElement ResultElement, TBoldSubscriber? Subscriber);

/// <summary>Gives the static type of a handle from its root type.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldGetStaticBoldType(Component Sender, TBoldElementTypeInfo? RootType, TBoldElementTypeInfo? ResultType);

/// <summary>A rooted handle whose value an event derives from the root value; ValueTypeName gives its static type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDerivedHandle : TBoldRootedHandle
{
    private TBoldHandleDeriveAndSubscribe? fOnDeriveAndSubscribe;
    private string fValueTypeName = "";

    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    public TBoldDerivedHandle(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetOnDeriveAndSubscribe(TBoldHandleDeriveAndSubscribe? Value)
    {
        if (fOnDeriveAndSubscribe != Value)
        {
            fOnDeriveAndSubscribe = Value;
            MarkSubscriptionOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetValueTypeName(string Value)
    {
        if (fValueTypeName != Value)
        {
            fValueTypeName = Value;
            StaticBoldTypeChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void DeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
    {
        System.Diagnostics.Debug.Assert(DerivedObject is TBoldIndirectElement);
        fOnDeriveAndSubscribe?.Invoke(this, EffectiveRootValue(), (TBoldIndirectElement)DerivedObject!, Subscriber);
    }

    /// <summary>The type named by ValueTypeName in the static system handle's (or the root's) model; only attributes and lists.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType()
    {
        TBoldSystemTypeInfo? SystemTypeInfo;
        if (StaticSystemHandle is not null)
        {
            SystemTypeInfo = StaticSystemHandle.StaticSystemTypeInfo;
        }
        else
        {
            SystemTypeInfo = RootHandle?.StaticSystemTypeInfo;
        }

        if (SystemTypeInfo is null)
        {
            return null;
        }

        TBoldElementTypeInfo? result = SystemTypeInfo.ElementTypeInfoByExpressionName(ValueTypeName);
        if (result is not null && result.BoldValueType is not (TBoldValueTypeSet.bvtAttr or TBoldValueTypeSet.bvtList))
        {
            throw new EBold(SysUtils.Format(sIllegalTypeSelected, GetType().Name, ValueTypeName));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldHandleDeriveAndSubscribe? OnDeriveAndSubscribe
    {
        get => fOnDeriveAndSubscribe;
        set => SetOnDeriveAndSubscribe(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ValueTypeName
    {
        get => fValueTypeName;
        set => SetValueTypeName(value);
    }
}
