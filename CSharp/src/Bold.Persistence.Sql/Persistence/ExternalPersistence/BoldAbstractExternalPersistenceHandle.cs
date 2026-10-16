// Delphi: Source/Persistence/ExternalPersistence/BoldAbstractExternalPersistenceHandle.pas
// Port map: CSharp/port/units/BoldAbstractExternalPersistenceHandle.md
// The handle of an external persistence controller, with activation and update events and typed access to the
// values of objects being read or written.

namespace Bold;

/// <summary>The base of the handles of external storage.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractExternalPersistenceHandle(System.ComponentModel.IContainer? Owner = null) : TBoldPersistenceHandlePassthroughWithModel(Owner)
{
    private TNotifyEvent? FOnStartUpdates;
    private TNotifyEvent? FOnEndUpdates;
    private TNotifyEvent? fOnFailUpdates;
    private TNotifyEvent? FOnActivate;
    private TNotifyEvent? FOnDeActivate;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractExternalPersistenceController GetPersistenceController() => (TBoldAbstractExternalPersistenceController)base.PersistenceController;

    /// <summary>OnActivate comes before activation, OnDeActivate after deactivation.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        if (Value)
        {
            OnActivate?.Invoke(this);
            base.SetActive(Value);
        }
        else
        {
            base.SetActive(Value);
            OnDeActivate?.Invoke(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    public IBoldValue? KeyForObject(IBoldObjectContents ObjectContents) => PersistenceController.KeyForObject(ObjectContents);

    [Delphi(PortStatus.Ported)]
    public string KeyStringForObject(IBoldObjectContents ObjectContents) =>
        KeyForObject(ObjectContents) is IBoldStringContent StrValue ? StrValue.asString : throw new EBold(SysUtils.Format(ExPeConsts.sKeyNotString, GetType().Name));

    [Delphi(PortStatus.Ported)]
    public int KeyIntForObject(IBoldObjectContents ObjectContents) =>
        KeyForObject(ObjectContents) is IBoldIntegerContent IntValue ? IntValue.asInteger : throw new EBold(SysUtils.Format(ExPeConsts.sKeyNotInteger, GetType().Name));

    [Delphi(PortStatus.Ported)]
    public IBoldValue? ValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName) => PersistenceController.ValueForObject(ObjectContents, MemberExpressionName);

    [Delphi(PortStatus.Ported)]
    public string StringValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName) =>
        ValueForObject(ObjectContents, MemberExpressionName) is IBoldStringContent StrValue
            ? StrValue.asString
            : throw new EBold(SysUtils.Format(ExPeConsts.sValueNotString, GetType().Name, MemberExpressionName));

    [Delphi(PortStatus.Ported)]
    public TDateTime DateValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName) =>
        ValueForObject(ObjectContents, MemberExpressionName) is IBoldDateContent DateValue
            ? DateValue.asDate
            : throw new EBold(SysUtils.Format(ExPeConsts.sValueNotDate, GetType().Name, MemberExpressionName));

    [Delphi(PortStatus.Ported)]
    public decimal CurrencyValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName) =>
        ValueForObject(ObjectContents, MemberExpressionName) is IBoldCurrencyContent CurrValue
            ? CurrValue.asCurrency
            : throw new EBold(SysUtils.Format(ExPeConsts.sValueNotCurrency, GetType().Name, MemberExpressionName));

    [Delphi(PortStatus.Ported)]
    public int IntValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName) =>
        ValueForObject(ObjectContents, MemberExpressionName) is IBoldIntegerContent IntValue
            ? IntValue.asInteger
            : throw new EBold(SysUtils.Format(ExPeConsts.sValueNotInteger, GetType().Name, MemberExpressionName));

    /// <summary>The contents of the object a single link refers to, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents? GetReferredObject(IBoldObjectContents ObjectContents, string MemberExpressionName, IBoldValueSpace ValueSpace) =>
        ValueForObject(ObjectContents, MemberExpressionName) is IBoldObjectIdRef { Id: { } Id } ? ValueSpace.ObjectContentsByObjectId(Id) : null;

    [Delphi(PortStatus.Ported)] protected TNotifyEvent? OnStartUpdates { get => FOnStartUpdates; set => FOnStartUpdates = value; }
    [Delphi(PortStatus.Ported)] protected TNotifyEvent? OnEndUpdates { get => FOnEndUpdates; set => FOnEndUpdates = value; }
    [Delphi(PortStatus.Ported)] protected TNotifyEvent? OnFailUpdates { get => fOnFailUpdates; set => fOnFailUpdates = value; }
    [Delphi(PortStatus.Ported)] protected TNotifyEvent? OnActivate { get => FOnActivate; set => FOnActivate = value; }
    [Delphi(PortStatus.Ported)] protected TNotifyEvent? OnDeActivate { get => FOnDeActivate; set => FOnDeActivate = value; }
    [Delphi(PortStatus.Ported)] public new TBoldAbstractExternalPersistenceController PersistenceController => GetPersistenceController();
}
