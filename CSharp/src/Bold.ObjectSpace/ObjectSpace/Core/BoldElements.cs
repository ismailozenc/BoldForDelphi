// Delphi: Source/ObjectSpace/Core/BoldElements.pas
// Port map: CSharp/port/units/BoldElements.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldElements", Symbol = "TBoldValueTypes", Note = "Global using alias of DelphiSet<TBoldValueTypeSet>, in BoldElements.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldElements", Symbol = "TBoldDirectElement", Note = "Global using alias of TBoldElement, in BoldElements.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldElements", Symbol = "TBoldElementClass", Note = "Global using alias of System.Type, in BoldElements.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldElements", Symbol = "initialization", Note = "The modified value holder list is created by its static field initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldElements", Symbol = "finalization", Note = "Freed the modified value holder list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>What kind of value a type describes: a list, a class, an attribute, the system or a type.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldValueTypeSet { bvtList, bvtClass, bvtAttr, bvtSystem, bvtType }

/// <summary>Called to subscribe <paramref name="Subscriber"/> to <paramref name="Element"/>.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldElementSubscribe(TBoldElement Element, TBoldSubscriber Subscriber);

/// <summary>Looks up the OCL of a named definition.</summary>
[Delphi(PortStatus.Ported)]
public delegate string TBoldLookUpOclDefinition(string Name);

/// <summary>A component with an OCL expression, its context type and its external variables.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldOCLComponent
{
    [Delphi(PortStatus.Ported)] TBoldElementTypeInfo? GetContextType();
    [Delphi(PortStatus.Ported)] void SetExpression(string Expression);
    [Delphi(PortStatus.Ported)] TBoldExternalVariableList? GetVariableList();
    [Delphi(PortStatus.Ported)] string GetExpression();
    [Delphi(PortStatus.Ported)] TBoldElementTypeInfo? ContextType => GetContextType();

    [Delphi(PortStatus.Ported)]
    string Expression
    {
        get => GetExpression();
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)] TBoldExternalVariableList? VariableList => GetVariableList();
}

/// <summary>A named variable an OCL expression can use; subclasses give its value and type.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldExternalVariable : TBoldMemoryManagedObject
{
    private readonly string fName;

    [Delphi(PortStatus.Ported)]
    protected TBoldExternalVariable(string Name)
    {
        fName = Name;
    }

    [Delphi(PortStatus.Ported)] protected abstract TBoldElement? GetValue();
    [Delphi(PortStatus.Ported)] protected abstract TBoldElementTypeInfo? GetValueType();
    [Delphi(PortStatus.Ported)] public TBoldElement? Value => GetValue();
    [Delphi(PortStatus.Ported)] public string Name => fName;
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ValueType => GetValueType();
}

/// <summary>External variables for an OCL evaluation; the list owns them.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalVariableList : TBoldObjectArray
{
    [Delphi(PortStatus.Ported)]
    public TBoldExternalVariableList()
        : base(4, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner))
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariable GetVariables(int index) => (TBoldExternalVariable)base[index]!;

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldExternalVariable Variable) => base.Add(Variable);

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalVariableList.Variables")]
    public new TBoldExternalVariable this[int index] => GetVariables(index);
}

/// <summary>Evaluates OCL expressions on elements; the OCL unit provides the implementation.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldEvaluator : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    public abstract void DefineVariable(string VariableName, TBoldElement? VarValue, TBoldElementTypeInfo? VariableType, bool OwnValue);

    [Delphi(PortStatus.Ported)]
    public abstract void Evaluate(string Ocl, TBoldElement? Root, TBoldSubscriber? Subscriber, bool ResubscribeAll, TBoldIndirectElement? resultElement,
        bool EvaluateInPS = false, TBoldExternalVariableList? VariableList = null);

    [Delphi(PortStatus.Ported)]
    public abstract TBoldElementTypeInfo? ExpressionType(string Ocl, TBoldElementTypeInfo? Context, bool ReRaise, TBoldExternalVariableList? VariableList = null);

    [Delphi(PortStatus.Ported)]
    public abstract void SetLookupOclDefinition(TBoldLookUpOclDefinition? value);
}

/// <summary>
/// The base of everything OCL can reach: values, objects, lists and the meta elements that describe them. An element has
/// a type, string representations, comparison and assignment, and can evaluate OCL expressions with itself as context.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldElement : TBoldSubscribableObject
{
    /// <summary>The element that holds an uncommitted change of this one, per element (kept outside to save space).</summary>
    private static readonly TBoldExternalizedReferenceList G_ExternalModifiedValueHolders = new();

    [Delphi(PortStatus.Ported)]
    private object? GetModifiedValueHolder() =>
        GetElementFlag(BoldElements.befHasModifiedValueHolder) ? G_ExternalModifiedValueHolders.ReferencedObjects(this) : null;

    [Delphi(PortStatus.Ported)]
    private void SetModifiedValueHolder(object? Value)
    {
        if (GetElementFlag(BoldElements.befHasModifiedValueHolder))
        {
            G_ExternalModifiedValueHolders.SetReferencedObjects(this, null);
        }

        if (Value is not null)
        {
            G_ExternalModifiedValueHolders.SetReferencedObjects(this, Value);
        }

        SetElementFlag(BoldElements.befHasModifiedValueHolder, Value is not null);
    }

    [Delphi(PortStatus.Ported)]
    private bool GetMutable() => !GetElementFlag(BoldElements.befImmutable);

    /// <summary>The value as a string in the representation; raises EBold here, subclasses that support strings override it.</summary>
    [Delphi(PortStatus.Ported, Note = "Public here: the indexed property StringRepresentation has no C# form, so callers use this and SetStringRepresentation.")]
    public virtual string GetStringRepresentation(TBoldRepresentation Representation) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sRepresentationNotSupported, GetType().Name, Representation));

    [Delphi(PortStatus.Ported, Note = "Public here: the indexed property StringRepresentation has no C# form, so callers use this and GetStringRepresentation.")]
    public virtual void SetStringRepresentation(TBoldRepresentation Representation, string Value) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sRepresentationNotSupported, GetType().Name, Representation));

    [Delphi(PortStatus.Ported)]
    protected void CompareError(TBoldElement? BoldElement) =>
        throw new EBold(BoldElement is not null
            ? SysUtils.Format(BoldCoreConsts.sCompareNotSupported, GetType().Name, BoldElement.GetType().Name)
            : SysUtils.Format(BoldCoreConsts.sCompareNilNotSupported, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected void AssignError(TBoldElement? BoldElement) =>
        throw new EBold(BoldElement is not null
            ? SysUtils.Format(BoldCoreConsts.sAssignNotSupported, GetType().Name, BoldElement.GetType().Name)
            : SysUtils.Format(BoldCoreConsts.sAssignNilNotSupported, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected void MutableError(string NewValue) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sTriedToChangeImmutableElement, GetType().Name, AsString, NewValue));

    [Delphi(PortStatus.Ported)]
    protected void CompareTypeError(TBoldCompareType CompType, TBoldElement BoldElement) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sInvalidCompareType, GetType().Name, CompType.ToString(), BoldElement.GetType().Name));

    /// <summary>The evaluator of this element's type; raises EBold when the element has no type.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldEvaluator? GetEvaluator() =>
        BoldType is not null
            ? BoldType.Evaluator
            : throw new EBold(SysUtils.Format(BoldCoreConsts.sCannotGetEvaluatorWithoutType, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldElementTypeInfo? GetBoldType();

    /// <summary>A copy of this element, or null when it cannot be copied (the default).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual TBoldElement? CloneIfPossible() => null;

    /// <summary>Clears the subscriptions (sending beDestroying) and makes the element mutable, then disposes it.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        PrepareToDestroy();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public abstract void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = BoldSubscription.breReEvaluate);

    [Delphi(PortStatus.Ported)]
    public void PrepareToDestroy()
    {
        FreePublisher();
        SetElementFlag(BoldElements.befImmutable, false);
    }

    /// <summary>The value as a variant; here the default string representation.</summary>
    [Delphi(PortStatus.Ported, Note = "object? where Delphi has Variant.")]
    public virtual object? GetAsVariant() => AsString;

    [Delphi(PortStatus.Ported, Note = "object? where Delphi has Variant; converts with Convert.ToString.")]
    public virtual void SetAsVariant(object? Value) => AsString = Convert.ToString(Value, System.Globalization.CultureInfo.CurrentCulture) ?? "";

    [Delphi(PortStatus.Ported)]
    public bool IsEqual(TBoldElement? BoldElement) => IsEqualAs(TBoldCompareType.ctDefault, BoldElement);

    [Delphi(PortStatus.Ported)]
    public int CompareTo(TBoldElement? BoldElement) => CompareToAs(TBoldCompareType.ctDefault, BoldElement);

    /// <summary>Negative, zero or positive as this element sorts before, with or after the other; raises EBold here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual int CompareToAs(TBoldCompareType CompareType, TBoldElement? BoldElement) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sLocatedAbstractError, GetType().Name, "CompareToAs"));

    [Delphi(PortStatus.Ported)]
    public virtual bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement) => CompareToAs(CompareType, BoldElement) == 0;

    /// <summary>Copies the value of <paramref name="Source"/>; raises EBold here.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Assign(TBoldElement? Source) =>
        throw new EBold(Source is not null
            ? SysUtils.Format(BoldCoreConsts.sAssignNotSupported, GetType().Name, Source.GetType().Name)
            : SysUtils.Format(BoldCoreConsts.sAssignNilNotSupported, GetType().Name));

    [Delphi(PortStatus.Ported)]
    public virtual bool ValidateCharacter(char C, TBoldRepresentation Representation) => true;

    /// <summary>True when every character is valid.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        foreach (char c in Value)
        {
            if (!ValidateCharacter(c, Representation))
            {
                return false;
            }
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureValidString(string Value, TBoldRepresentation Representation)
    {
        if (!ValidateString(Value, Representation))
        {
            throw new EBoldAssertionFailed(BoldCoreConsts.sStringValidationFailed);
        }
    }

    /// <summary>Does nothing here; values subscribe to the events that change their string.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber,
        TBoldEvent RequestedEvent = BoldSubscription.breReEvaluate)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool ObserverMayModify(object? Observer) => false;

    [Delphi(PortStatus.Ported)]
    public virtual bool ObserverMayModifyAsString(TBoldRepresentation Representation, TBoldSubscriber? observer) => ObserverMayModify(observer);

    /// <summary>Records who is modifying the element; raises EBold when someone else already is.</summary>
    [Delphi(PortStatus.Ported)]
    public void RegisterModifiedValueHolder(object? observer)
    {
        object? holder = ModifiedValueHolder;
        if (holder is not null && holder != observer)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sMemberAlreadyModified, GetType().Name));
        }

        SetModifiedValueHolder(observer);
    }

    /// <summary>Clears the modifier, whoever it is (Delphi does not check the observer either).</summary>
    [Delphi(PortStatus.Ported)]
    public void UnRegisterModifiedValueHolder(object? observer) => SetModifiedValueHolder(null);

    [Delphi(PortStatus.Ported)]
    public void MakeImmutable() => SetElementFlag(BoldElements.befImmutable, true);

    [Delphi(PortStatus.Ported)]
    public abstract void GetAsList(TBoldIndirectElement ResultList);

    /// <summary>Puts this element in <paramref name="resultElement"/> as a reference.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void GetAsValue(TBoldIndirectElement resultElement) => resultElement.SetReferenceValue(this);

    [Delphi(PortStatus.Ported)]
    public void SubscribeToExpression(TBoldExpression Expression, TBoldSubscriber? Subscriber, bool Resubscribe = false, bool EvaluateInPS = false,
        TBoldExternalVariableList? VariableList = null) =>
        EvaluateAndSubscribeToExpression(Expression, Subscriber, null, Resubscribe, EvaluateInPS, VariableList);

    [Delphi(PortStatus.Ported)]
    public void EvaluateExpression(TBoldExpression Expression, TBoldIndirectElement? resultElement, bool EvaluateInPS = false,
        TBoldExternalVariableList? VariableList = null) =>
        EvaluateAndSubscribeToExpression(Expression, null, resultElement, false, EvaluateInPS, VariableList);

    /// <summary>The result when it is an existing element; null when the evaluation made a new one (it is freed).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElement? EvaluateExpressionAsDirectElement(TBoldExpression Expression, TBoldExternalVariableList? VariableList = null)
    {
        using var E = new TBoldIndirectElement();
        EvaluateExpression(Expression, E, false, VariableList);
        return E.OwnsValue ? null : E.Value;
    }

    /// <summary>
    /// Evaluates <paramref name="Expression"/> with this element as context and subscribes <paramref name="Subscriber"/> to
    /// what it depends on. A new result is made immutable; OCL errors are raised as EBold with the expression.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void EvaluateAndSubscribeToExpression(TBoldExpression Expression, TBoldSubscriber? Subscriber, TBoldIndirectElement? resultElement,
        bool Resubscribe = false, bool EvaluateInPS = false, TBoldExternalVariableList? VariableList = null)
    {
        try
        {
            Evaluator!.Evaluate(Expression, this, Subscriber, Resubscribe, resultElement, EvaluateInPS, VariableList);
            if (!EvaluateInPS && resultElement?.Value is not null && resultElement.OwnsValue)
            {
                resultElement.Value.MakeImmutable();
            }

            if (Subscriber is not null)
            {
                AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.breReSubscribe);
            }
        }
        catch (EBoldOCLAbort E)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sOCLExpressionError, E.Ocl, E.Message), E);
        }
        catch (EBoldOCLError E)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sOCLExpressionError, E.Ocl, E.Message), E);
        }
    }

    /// <summary>The result's string in the representation, or "" when the result is nil.</summary>
    [Delphi(PortStatus.Ported)]
    public string EvaluateExpressionAsString(TBoldExpression Expression, TBoldRepresentation Representation, bool EvaluateInPS = false,
        TBoldExternalVariableList? VariableList = null)
    {
        using var E = new TBoldIndirectElement();
        EvaluateExpression(Expression, E, EvaluateInPS, VariableList);
        return E.Value is not null ? E.Value.StringRepresentation(Representation) : "";
    }

    /// <summary>A result the caller owns: the new element the evaluation made, or a clone of an existing one (null if it cannot be cloned).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElement? EvaluateExpressionAsNewElement(TBoldExpression Expression, bool EvaluateInPS = false, TBoldExternalVariableList? VariableList = null)
    {
        using var E = new TBoldIndirectElement();
        EvaluateExpression(Expression, E, EvaluateInPS, VariableList);
        if (E.Value is null)
        {
            return null;
        }

        return E.OwnsValue ? E.RelinquishValue() : E.Value.CloneIfPossible();
    }

    [Delphi(PortStatus.Ported)] public TBoldEvaluator? Evaluator => GetEvaluator();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? BoldType => GetBoldType();

    /// <summary>The getter of the indexed property StringRepresentation; the setter is <see cref="SetStringRepresentation"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public string StringRepresentation(TBoldRepresentation Representation) => GetStringRepresentation(Representation);

    [Delphi(PortStatus.Ported)]
    public string AsString
    {
        get => GetStringRepresentation(BoldDefs.brDefault);
        set => SetStringRepresentation(BoldDefs.brDefault, value);
    }

    [Delphi(PortStatus.Ported)] public object? ModifiedValueHolder => GetModifiedValueHolder();
    [Delphi(PortStatus.Ported)] public bool Mutable => GetMutable();
}

/// <summary>An element of the runtime model, named in the model, in OCL and in Delphi code. Created immutable.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMetaElement : TBoldElement
{
    private readonly string fModelName;
    private readonly string fDelphiName;
    private readonly string fExpressionName;

    [Delphi(PortStatus.Ported)]
    protected TBoldMetaElement(string ModelName, string ExpressionName, string DelphiName)
    {
        fModelName = ModelName;
        fExpressionName = ExpressionName;
        fDelphiName = DelphiName;
        MakeImmutable();
    }

    /// <summary>The expression name, in every representation.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => ExpressionName;

    /// <summary>Equal only to itself.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement) => BoldElement == this;

    /// <summary>Meta elements do not change, so there is nothing to subscribe to; raises EBold for a mutable one.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = BoldSubscription.breReEvaluate)
    {
        if (Mutable)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sCannotSubscribeToMutableMetaElements, GetType().Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList) =>
        throw new EBoldFeatureNotImplementedYet(BoldCoreConsts.sMetaElementGetAsListNotImplemented);

    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public string DelphiName => fDelphiName;
    [Delphi(PortStatus.Ported)] public string ModelName => fModelName;
}

/// <summary>The type of an element: a class, attribute, list, type or the system. Knows the system type, whose evaluator it uses.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldElementTypeInfo : TBoldMetaElement
{
    private TBoldElementTypeInfo? fSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    protected TBoldElementTypeInfo(string ModelName, string ExpressionName, string DelphiName, TBoldElementTypeInfo? SystemTypeInfo)
        : base(ModelName, ExpressionName, DelphiName)
    {
        fSystemTypeInfo = SystemTypeInfo;
    }

    /// <summary>Lets the system type be its own system type; Delphi passes Self to the inherited constructor, which C# cannot.</summary>
    private protected void SetSystemTypeInfo(TBoldElementTypeInfo SystemTypeInfo) => fSystemTypeInfo = SystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    private TBoldValueTypeSet GetValueType() =>
        (TBoldValueTypeSet)GetInternalState(unchecked((uint)BoldElements.BoldValueTypeMask), BoldElements.BoldValueTypeShift);

    [Delphi(PortStatus.Ported)]
    protected override TBoldEvaluator? GetEvaluator() => SystemTypeInfo!.GetEvaluator();

    [Delphi(PortStatus.Ported)]
    protected void SetValueType(TBoldValueTypeSet NewValue) =>
        SetInternalState(unchecked((uint)BoldElements.BoldValueTypeMask), BoldElements.BoldValueTypeShift, (uint)NewValue);

    /// <summary>A new type list holding this type. Needs TBoldTypeList, ported with BoldSystem in M3.</summary>
    [Delphi(PortStatus.Partial, Note = "Raises EBoldFeatureNotImplementedYet until BoldTypeList (M3) is ported.")]
    public override void GetAsList(TBoldIndirectElement ResultList) =>
        throw new EBoldFeatureNotImplementedYet("TBoldElementTypeInfo.GetAsList needs TBoldTypeList, which is not ported yet.");

    [Delphi(PortStatus.Ported)]
    public abstract bool ConformsTo(TBoldElementTypeInfo? Element);

    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? SystemTypeInfo => fSystemTypeInfo;
    [Delphi(PortStatus.Ported)] public TBoldValueTypeSet BoldValueType => GetValueType();
}

/// <summary>Holds an element as a reference or owning it: the result slot of an OCL evaluation.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndirectElement : TBoldFlaggedObject, IDisposable
{
    private TBoldElement? fValue;

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetValue() => fValue;

    [Delphi(PortStatus.Ported)]
    private bool WriteableOwnsValue
    {
        get => GetElementFlag(BoldElements.befOwnsValue);
        set => SetElementFlag(BoldElements.befOwnsValue, value);
    }

    /// <summary>Frees the value when it is owned.</summary>
    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        if (OwnsValue)
        {
            SysUtils.FreeAndNil(ref fValue);
        }
        else
        {
            fValue = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Refers to <paramref name="NewValue"/> without owning it, freeing an owned old value.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetReferenceValue(TBoldElement? NewValue)
    {
        if (NewValue != fValue)
        {
            TBoldElement? toFree = OwnsValue ? fValue : null;
            fValue = NewValue;
            toFree?.Dispose();
        }
        else if (OwnsValue)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sNewValueAlreadyOwned, GetType().Name));
        }

        WriteableOwnsValue = false;
    }

    /// <summary>Takes ownership of <paramref name="NewValue"/>, freeing an owned old value.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetOwnedValue(TBoldElement? NewValue)
    {
        if (NewValue != fValue)
        {
            TBoldElement? toFree = OwnsValue ? fValue : null;
            fValue = NewValue;
            toFree?.Dispose();
        }

        WriteableOwnsValue = NewValue is not null;
    }

    /// <summary>Moves the value, with its ownership, to <paramref name="Target"/> (or drops it when there is none).</summary>
    [Delphi(PortStatus.Ported)]
    public void TransferValue(TBoldIndirectElement? Target)
    {
        if (Target is not null)
        {
            if (OwnsValue)
            {
                Target.SetOwnedValue(Value);
            }
            else
            {
                Target.SetReferenceValue(Value);
            }
        }

        fValue = null;
        WriteableOwnsValue = false;
    }

    /// <summary>Hands the value to the caller, who owns it if this did.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElement? RelinquishValue()
    {
        TBoldElement? result = Value;
        fValue = null;
        WriteableOwnsValue = false;
        return result;
    }

    [Delphi(PortStatus.Ported)] public bool OwnsValue => GetElementFlag(BoldElements.befOwnsValue);
    [Delphi(PortStatus.Ported)] public TBoldElement? Value => GetValue();
}

/// <summary>A subscribable component (a handle) that reaches elements.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscribableComponentViaBoldElem : TBoldSubscribableComponent
{
}

/// <summary>Unit-level constants of BoldElements: state masks and the element flags of each kind of element.</summary>
public static partial class BoldElements
{
    [Delphi(PortStatus.Ported)] public const int BoldPSShift = 24;
    [Delphi(PortStatus.Ported)] public const int BoldPersistenceStateMask = 3 << BoldPSShift;
    [Delphi(PortStatus.Ported)] public const int BoldESShift = 26;
    [Delphi(PortStatus.Ported)] public const int BoldExistenceStateMask = 3 << BoldESShift;
    [Delphi(PortStatus.Ported)] public const int BoldDMShift = 26;
    [Delphi(PortStatus.Ported)] public const int BoldDuplicateModeMask = 3 << BoldDMShift;
    [Delphi(PortStatus.Ported)] public const int BoldDSShift = 28;
    [Delphi(PortStatus.Ported)] public const int BoldDerivationStateMask = 7 << BoldDSShift;
    [Delphi(PortStatus.Ported)] public const int BoldDefaultRegionModeShift = 24;
    [Delphi(PortStatus.Ported)] public const int BoldDefaultRegionModeMask = 7 << BoldDefaultRegionModeShift;

    // Flags of TBoldElement.
    [Delphi(PortStatus.Ported)] public const int befImmutable = BoldBase.BoldElementFlag0;
    [Delphi(PortStatus.Ported)] public const int befHasModifiedValueHolder = BoldBase.BoldElementFlag1;

    // Flags of business elements.
    [Delphi(PortStatus.Ported)] public const int befPersistent = BoldBase.BoldElementFlag2;
    [Delphi(PortStatus.Ported)] public const int befTouched = BoldBase.BoldElementFlag3;

    // Flags of TBoldObject.
    [Delphi(PortStatus.Ported)] public const int befInDirtyList = BoldBase.BoldElementFlag4;
    [Delphi(PortStatus.Ported)] public const int befMemberModified = BoldBase.BoldElementFlag5;
    [Delphi(PortStatus.Ported)] public const int befMemberModifiedKnown = BoldBase.BoldElementFlag6;
    [Delphi(PortStatus.Ported)] public const int befObjectReadOnly = BoldBase.BoldElementFlag7;
    [Delphi(PortStatus.Ported)] public const int befObjectWasCreatedNew = BoldBase.BoldElementFlag8;
    [Delphi(PortStatus.Ported)] public const int befStoresTimeStamp = BoldBase.BoldElementFlag9;

    // Flags of TBoldMember.
    [Delphi(PortStatus.Ported)] public const int befIsNull = BoldBase.BoldElementFlag4;
    [Delphi(PortStatus.Ported)] public const int befDerived = BoldBase.BoldElementFlag5;
    [Delphi(PortStatus.Ported)] public const int befHasDeriver = BoldBase.BoldElementFlag6;
    [Delphi(PortStatus.Ported)] public const int befMemberReadOnly = BoldBase.BoldElementFlag7;
    [Delphi(PortStatus.Ported)] public const int befHasRtInfo = BoldBase.BoldElementFlag8;
    [Delphi(PortStatus.Ported)] public const int befEnsuringCurrent = BoldBase.BoldElementFlag9;
    [Delphi(PortStatus.Ported)] public const int befOwnedByObject = BoldBase.BoldElementFlag10;

    // Flags of TBoldObjectList.
    [Delphi(PortStatus.Ported)] public const int befAdjusted = BoldBase.BoldElementFlag11;
    [Delphi(PortStatus.Ported)] public const int befSubscribeToObjectsInList = BoldBase.BoldElementFlag12;
    [Delphi(PortStatus.Ported)] public const int befSubscribeToLocatorsInList = BoldBase.BoldElementFlag13;

    // Flags of TBoldObjectReference.
    [Delphi(PortStatus.Ported)] public const int befHasOldValues = BoldBase.BoldElementFlag11;

    // State of TBoldElementTypeInfo. Delphi builds the mask with BoldPSShift, which has the same value.
    [Delphi(PortStatus.Ported)] public const int BoldValueTypeShift = 24;
    [Delphi(PortStatus.Ported)] public const int BoldValueTypeMask = 7 << BoldPSShift;

    // Flags of TBoldIndirectElement.
    [Delphi(PortStatus.Ported)] public const int befOwnsValue = BoldBase.BoldElementFlag0;

    // Flags of TBoldOCLNode.
    [Delphi(PortStatus.Ported)] public const int befNeedsListCoercion = BoldBase.BoldElementFlag1;
    [Delphi(PortStatus.Ported)] public const int befIsConstant = BoldBase.BoldElementFlag2;
    [Delphi(PortStatus.Ported)] public const int befResubscribe = BoldBase.BoldElementFlag3;
    [Delphi(PortStatus.Ported)] public const int befHastemporaryDummyValue = BoldBase.BoldElementFlag4;

    // Flags of TBoldSystemTypeInfo.
    [Delphi(PortStatus.Ported)] public const int befSystemPersistent = BoldBase.BoldElementFlag2;
    [Delphi(PortStatus.Ported)] public const int befSystemIsRunnable = BoldBase.BoldElementFlag3;
    [Delphi(PortStatus.Ported)] public const int befEnforceOptimisticLocking = BoldBase.BoldElementFlag4;
    [Delphi(PortStatus.Ported)] public const int befUpdateWholeObjects = BoldBase.BoldElementFlag5;
    [Delphi(PortStatus.Ported)] public const int befGenerateDefaultRegions = BoldBase.BoldElementFlag6;

    // Flags of TBoldClassTypeInfo.
    [Delphi(PortStatus.Ported)] public const int befClassPersistent = BoldBase.BoldElementFlag2;
    [Delphi(PortStatus.Ported)] public const int befHasSubclasses = BoldBase.BoldElementFlag3;
    [Delphi(PortStatus.Ported)] public const int befIsAbstract = BoldBase.BoldElementFlag4;
    [Delphi(PortStatus.Ported)] public const int beFIsImported = BoldBase.BoldElementFlag5;
    [Delphi(PortStatus.Ported)] public const int befIsLinkClass = BoldBase.BoldElementFlag6;
    [Delphi(PortStatus.Ported)] public const int befClassToBeRemoved = BoldBase.BoldElementFlag7;
    [Delphi(PortStatus.Ported)] public const int befVersioned = BoldBase.BoldElementFlag8;
    [Delphi(PortStatus.Ported)] public const int befGenerateDefaultRegion = BoldBase.BoldElementFlag9;

    // Flags of TBoldMemberRTInfo.
    [Delphi(PortStatus.Ported)] public const int befMemberPersistent = BoldBase.BoldElementFlag2;
    [Delphi(PortStatus.Ported)] public const int befDelayedFetch = BoldBase.BoldElementFlag3;
    [Delphi(PortStatus.Ported)] public const int befIsStoredInObject = BoldBase.BoldElementFlag4;
    [Delphi(PortStatus.Ported)] public const int befIsMultiRole = BoldBase.BoldElementFlag5;
    [Delphi(PortStatus.Ported)] public const int befIsSingleRole = BoldBase.BoldElementFlag6;
    [Delphi(PortStatus.Ported)] public const int befIsDerived = BoldBase.BoldElementFlag7;
    [Delphi(PortStatus.Ported)] public const int befIsReverseDerived = BoldBase.BoldElementFlag8;
    [Delphi(PortStatus.Ported)] public const int befIsNonVersionedInVersionedClass = BoldBase.BoldElementFlag9;
    [Delphi(PortStatus.Ported)] public const int befMemberToBeRemoved = BoldBase.BoldElementFlag10;

    // Flags of TBoldRoleRTInfo.
    [Delphi(PortStatus.Ported)] public const int befIsIndirect = BoldBase.BoldElementFlag11;
    [Delphi(PortStatus.Ported)] public const int befIsNavigable = BoldBase.BoldElementFlag12;
    [Delphi(PortStatus.Ported)] public const int befIsOrdered = BoldBase.BoldElementFlag13;
    [Delphi(PortStatus.Ported)] public const int befOtherEndOrdered = BoldBase.BoldElementFlag14;
    [Delphi(PortStatus.Ported)] public const int befMandatory = BoldBase.BoldElementFlag15;
    [Delphi(PortStatus.Ported)] public const int befForceOtherEnd = BoldBase.BoldElementFlag16;
    [Delphi(PortStatus.Ported)] public const int befQualifiedMulti = BoldBase.BoldElementFlag17;

    // Flags of TBoldAttributeRTInfo.
    [Delphi(PortStatus.Ported)] public const int befHasInitalvalue = BoldBase.BoldElementFlag11;
    [Delphi(PortStatus.Ported)] public const int befAllowNull = BoldBase.BoldElementFlag12;
}
