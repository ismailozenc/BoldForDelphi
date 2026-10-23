// Delphi: Source/Handles/Core/BoldVariableHandle.pas
// Port map: CSharp/port/units/BoldVariableHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>
/// A handle that owns a value of its own: an attribute or a list of the type named by ValueTypeName, created on first
/// use with the initial values.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldVariableHandle : TBoldNonSystemHandle
{
    private string fValueTypeName = "";
    private TBoldElement? fValue;
    private TStringList? fInitialValues;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldVariableHandle(IContainer? owner = null)
        : base(owner)
    {
        var s = new TStringList();
        s.OnChange = InitialvaluesChanged;
        fInitialValues = s;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldVariableHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            fInitialValues = null;
            SysUtils.FreeAndNil(ref fValue);
        }

        base.Dispose(disposing);
    }

    /// <summary>Drops the value when the initial values change; the next use creates it again.</summary>
    [Delphi(PortStatus.Ported)]
    private void InitialvaluesChanged(object? Sender)
    {
        if (fValue is not null)
        {
            SysUtils.FreeAndNil(ref fValue);
            SendEvent(this, beValueIdentityChanged);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetValueTypeName(string Value)
    {
        if (Value != fValueTypeName)
        {
            fValueTypeName = Value;
            StaticBoldTypeChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetInitialValues(TStringList Value)
    {
        if (fInitialValues != Value)
        {
            fInitialValues!.Assign(Value);
        }
    }

    /// <summary>Creates the value: an attribute set to the first initial value, or a list with one element per initial value.</summary>
    [Delphi(PortStatus.Ported)]
    private void CreateVariableElement()
    {
        System.Diagnostics.Debug.Assert(fValue is null);
        if (StaticBoldType is null)
        {
            return;
        }

        fValue = TBoldMemberFactory.CreateMemberFromBoldType(StaticBoldType);
        if (InitialValues.Count > 0)
        {
            if (fValue is TBoldAttribute)
            {
                fValue.AsString = InitialValues[0];
            }
            else if (fValue is TBoldMemberList memberList)
            {
                for (int i = 0; i < InitialValues.Count; i++)
                {
                    memberList.AddNew().AsString = InitialValues[i];
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldObjectList? GetObjectList() => Value as TBoldObjectList;

    /// <summary>The value, created on first use; outside a designer the handle needs a system handle and a valid type.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue()
    {
        if (!IsDesigning && !IsLoading)
        {
            if (StaticSystemHandle is null)
            {
                throw new EBold(SysUtils.Format(sNoSystemHandle, GetType().Name, "GetValue", Name)); // do not localize
            }

            if (StaticBoldType is null)
            {
                throw new EBold(SysUtils.Format(sValueTypeNameInvalid, GetType().Name, Name, ValueTypeName));
            }
        }

        if (fValue is null && StaticBoldType is not null)
        {
            CreateVariableElement();
        }

        return fValue;
    }

    /// <summary>The type named by ValueTypeName; only attribute and list types are allowed.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType()
    {
        if (StaticSystemHandle is null)
        {
            return null;
        }

        TBoldElementTypeInfo? result = StaticSystemHandle.StaticSystemTypeInfo?.ElementTypeInfoByExpressionName(ValueTypeName);
        if (result is not null && result.BoldValueType is not (TBoldValueTypeSet.bvtAttr or TBoldValueTypeSet.bvtList))
        {
            throw new EBold(SysUtils.Format(sOnlyListsAndAttributeTypesAllowed, GetType().Name, ValueTypeName));
        }

        return result;
    }

    /// <summary>Drops the value before telling subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void StaticBoldTypeChanged()
    {
        SysUtils.FreeAndNil(ref fValue);
        base.StaticBoldTypeChanged(); // Send message after clearing value
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectList? ObjectList => GetObjectList();

    [Delphi(PortStatus.Ported)]
    public string ValueTypeName
    {
        get => fValueTypeName;
        set => SetValueTypeName(value);
    }

    [Delphi(PortStatus.Ported)]
    public TStringList InitialValues
    {
        get => fInitialValues!;
        set => SetInitialValues(value);
    }
}
