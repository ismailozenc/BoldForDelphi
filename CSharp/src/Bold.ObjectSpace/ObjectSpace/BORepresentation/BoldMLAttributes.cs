// Delphi: Source/ObjectSpace/BORepresentation/BoldMLAttributes.pas
// Port map: CSharp/port/units/BoldMLAttributes.md
// Multi-language attributes: strings with one value per language, value sets with multi-language names, and the
// language value set. The languages are the objects of the model class BoldMLLanguageClassName in the default system.

using System.Runtime.CompilerServices;
using static Bold.BoldCoreConsts;
using static Bold.BoldDefs;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldMLAttributes", Symbol = "TBAStringClass", Note = "System.Type: the string class of a multi-language string's parts.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldMLAttributes", Symbol = "charArray", Note = "An unused type in the implementation section.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldMLAttributes", Symbol = "finalization", Note = "Freed the language members and removed the descriptors; the process owns them until it ends.")]

namespace Bold;

/// <summary>The unit's languages, settings and registration.</summary>
public static partial class BoldMLAttributes
{
    /// <summary>The representation holding all languages: language, #255, value, #255, repeated.</summary>
    [Delphi(PortStatus.Ported)] public const TBoldRepresentation brMLString = 0;

    [Delphi(PortStatus.Ported)] public static string BoldMLStringSecondaryFallBack = "< %s: %s >";
    [Delphi(PortStatus.Ported)] public static string BoldMLStringUnknownFallback = "<< %s: %s >>";
    [Delphi(PortStatus.Ported)] public static string BoldMLLanguageClassName = "LanguageClass";
    [Delphi(PortStatus.Ported)] public static string BoldMLLanguageNameAttributeName = "LanguageName";
    [Delphi(PortStatus.Ported)] public static string BoldMLLanguageNumberAttributeName = "LanguageNumber";

    private static TBADerivedValueSetValueList? _BoldLanguageList;
    private static List<TBoldSystem>? _Systems;
    private static List<TBALanguage>? _SecondaryLanguages;
    private static List<TBALanguage>? _PrimaryLanguages;
    private static bool LoadingOfLanguages;

    /// <summary>Reads the languages from the default system once, checking the model has the language class and attributes.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void EnsureLanguageList()
    {
        if (LoadingOfLanguages)
        {
            throw new EBold(sBootStrapProblem);
        }

        if (_BoldLanguageList is null)
        {
            LoadingOfLanguages = true;
            TBoldSystem system = TBoldSystem.DefaultSystem()!;
            TBoldClassTypeInfo LanguageTypeInfo = system.BoldSystemTypeInfo.ClassTypeInfoByExpressionName(BoldMLLanguageClassName)
                ?? throw new EBold(SysUtils.Format(sNeedClassCalledX, BoldMLLanguageClassName));
            TBoldMemberRTInfo MemberRTInfo = LanguageTypeInfo.MemberRTInfoByExpressionName(BoldMLLanguageNameAttributeName)
                ?? throw new EBold(SysUtils.Format(sNeedMemberCalledX, BoldMLLanguageClassName, BoldMLLanguageNameAttributeName));
            if (MemberRTInfo.MemberClass is null || !typeof(TBAString).IsAssignableFrom(MemberRTInfo.MemberClass))
            {
                throw new EBold(SysUtils.Format(sMustBeTBAString, BoldMLLanguageNameAttributeName, MemberRTInfo.MemberClass?.Name));
            }

            MemberRTInfo = LanguageTypeInfo.MemberRTInfoByExpressionName(BoldMLLanguageNumberAttributeName)
                ?? throw new EBold(SysUtils.Format(sNeedIntegerMemberX, BoldMLLanguageClassName, BoldMLLanguageNumberAttributeName));
            if (MemberRTInfo.MemberClass is null || !typeof(TBAInteger).IsAssignableFrom(MemberRTInfo.MemberClass))
            {
                throw new EBold(SysUtils.Format(sMemberXMustBeInteger, BoldMLLanguageNumberAttributeName, MemberRTInfo.MemberClass?.Name));
            }

            _BoldLanguageList = new TBADerivedValueSetValueList(system, BoldMLLanguageClassName, BoldMLLanguageNumberAttributeName, BoldMLLanguageNameAttributeName);
            LoadingOfLanguages = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public static TBAValueSetValueList BoldLanguageList()
    {
        EnsureLanguageList();
        return _BoldLanguageList!;
    }

    /// <summary>Raises unless the name is empty or a known language.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void ValidateLanguage(string LanguageName, string Location)
    {
        EnsureLanguageList();
        if (LanguageName.Length != 0 && !LoadingOfLanguages && _BoldLanguageList!.FindByText(brDefault, LanguageName) is null)
        {
            throw new EBold(SysUtils.Format(sInvalidLanguageName, Location, LanguageName));
        }
    }

    /// <summary>The system (the default one for null), with primary and secondary language members created for it.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldSystem EnsureValuesForElement(TBoldSystem? BoldSystem)
    {
        TBoldSystem result = BoldSystem ?? TBoldSystem.DefaultSystem()!;
        if (_Systems is null)
        {
            _Systems = [];
            _PrimaryLanguages = [];
            _SecondaryLanguages = [];
        }

        if (!_Systems.Contains(result))
        {
            _Systems.Add(result);
            _PrimaryLanguages!.Add((TBALanguage)TBoldMemberFactory.CreateMemberFromBoldType(result.BoldSystemTypeInfo.AttributeTypeInfoByDelphiName("TBALanguage")));
            _SecondaryLanguages!.Add((TBALanguage)TBoldMemberFactory.CreateMemberFromBoldType(result.BoldSystemTypeInfo.AttributeTypeInfoByDelphiName("TBALanguage")));
        }

        return result;
    }

    /// <summary>The language strings are shown in, for the system (null: the default system).</summary>
    [Delphi(PortStatus.Ported)]
    public static TBALanguage BoldPrimaryLanguage(TBoldSystem? BoldSystem)
    {
        BoldSystem = EnsureValuesForElement(BoldSystem);
        return _PrimaryLanguages![_Systems!.IndexOf(BoldSystem)];
    }

    /// <summary>The language used when a string has no value in the primary language.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBALanguage BoldSecondaryLanguage(TBoldSystem? BoldSystem)
    {
        BoldSystem = EnsureValuesForElement(BoldSystem);
        return _SecondaryLanguages![_Systems!.IndexOf(BoldSystem)];
    }

    [Delphi(PortStatus.Ported)]
    public static void BoldSetPrimaryLanguageByName(TBoldSystem? BoldSystem, string LanguageName)
    {
        ValidateLanguage(LanguageName, "BoldSetPrimaryLanguageByName");
        BoldPrimaryLanguage(BoldSystem).AsString = LanguageName;
    }

    [Delphi(PortStatus.Ported)]
    public static void BoldSetSecondaryLanguageByName(TBoldSystem? BoldSystem, string LanguageName)
    {
        ValidateLanguage(LanguageName, "BoldSetSecondaryLanguageByName");
        BoldSecondaryLanguage(BoldSystem).AsString = LanguageName;
    }

    /// <summary>Registers the classes as member types, when the assembly loads.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "A module initializer, so the classes are registered before any model is used.")]
#pragma warning disable CA2255 // The registration must run before any system type info is built, as Delphi's initialization section does.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialization()
    {
        TBoldMemberTypeList List = BoldMemberTypeDictionary.BoldMemberTypes();
        List.AddMemberTypeDescriptor(typeof(TBALanguage), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAMLValueSet), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAMLString), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAMLSubString), TBoldAbstractionLevel.alConcrete);
    }
}

/// <summary>A value of a multi-language value set: its representations are multi-language strings.</summary>
[Delphi(PortStatus.Ported)]
public class TBAMLValueSetValue : TBAValueSetValue
{
    private readonly TBoldMemberList FStringRepresentations = new();

    [Delphi(PortStatus.Ported)]
    public TBAMLValueSetValue(TBAValueSetValueList List)
    {
        List.AddValue(this);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBAMLValueSetValue.Destroy", Note = "Freed the member list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected override int GetStringRepresentationCount() => FStringRepresentations.Count;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        Representation >= 1 && Representation <= StringRepresentationCount
            ? FStringRepresentations[Representation - 1].AsString
            : throw new EBold(SysUtils.Format(sRepresentationNotSupported, GetType().Name, Representation));

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (Representation >= 1 && Representation <= StringRepresentationCount)
        {
            FStringRepresentations[Representation - 1].AsString = Value;
        }
        else
        {
            throw new EBold(SysUtils.Format(sRepresentationNotSupported, GetType().Name, Representation));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void AddMLString(TBAMLString Value) => FStringRepresentations.Add(Value);

    [Delphi(PortStatus.Ported)]
    protected internal TBAMLString GetMLString(int Representation) =>
        Representation >= 1 && Representation <= StringRepresentationCount
            ? (TBAMLString)FStringRepresentations[Representation - 1]
            : throw new EBold(SysUtils.Format(sRepresentationNotSupported, GetType().Name, Representation));

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Representation >= 1 && Representation <= StringRepresentationCount)
        {
            FStringRepresentations[Representation - 1].SubscribeToStringRepresentation(brDefault, Subscriber, RequestedEvent);
        }
        else
        {
            throw new EBold(SysUtils.Format(sRepresentationNotSupported, GetType().Name));
        }
    }
}

/// <summary>Multi-language value set values derived from objects whose string attributes are multi-language strings.</summary>
[Delphi(PortStatus.Ported)]
public class TBAMLValueSetValueList(TBoldSystem System, string ClassToFollow, string IntValueAttribute, params string[] StrValueAttributes)
    : TBADerivedValueSetValueList(System, ClassToFollow, IntValueAttribute, StrValueAttributes)
{
    /// <summary>The first value whose representation in the language equals the string.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBAValueSetValue? FindByStringAndLanguage(int Representation, string Value, string Language)
    {
        foreach (TBAValueSetValue v in FValueList)
        {
            if (((TBAMLValueSetValue)v).GetMLString(Representation).GetStringBylanguage(Language).AsString == Value)
            {
                return v;
            }
        }

        return null;
    }

    /// <summary>Adds a value whose representations are the members, which must be multi-language strings.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void AddMembers(int Int, TBoldMember[] Members)
    {
        var value = new TBAMLValueSetValue(this) { AsInteger = Int };
        foreach (TBoldMember member in Members)
        {
            if (member is not TBAMLString mlString)
            {
                throw new EBold(SysUtils.Format(sMLValueSetsRequireMLStrings, member.GetType().Name));
            }

            value.AddMLString(mlString);
        }
    }
}

/// <summary>A value set whose names have one value per language.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBAMLValueSet : TBAValueSet
{
    protected TBAMLValueSet()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected string GetStringRepresentationByLanguage(int Representation, string Languagename) =>
        IsNull ? "" : GetStringBylanguage(Representation, Languagename)!.AsString;

    /// <summary>Sets the value whose representation in the language is the string; raises when there is none.</summary>
    [Delphi(PortStatus.Ported, Note = "Public: the setter of the indexed property StringRepresentationByLanguage.")]
    public void SetStringRepresentationByLanguage(int Representation, string Languagename, string NewValue)
    {
        var MLValueSetValueList = (TBAMLValueSetValueList)Values;
        TBAValueSetValue MLValue = MLValueSetValueList.FindByStringAndLanguage(Representation, NewValue, Languagename)
            ?? throw new EBold(sInvalidValue);
        AsInteger = MLValue.AsInteger;
    }

    /// <summary>The current value's string for the representation and language, or null for a plain value.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBAString? GetStringBylanguage(int representation, string LanguageName)
    {
        BoldMLAttributes.ValidateLanguage(LanguageName, GetType().Name + ".GetStringByLanguage");
        TBAValueSetValue? temp = Values.FindByInteger(AsInteger);
        return temp is TBAMLValueSetValue ml ? ml.GetMLString(representation).GetStringBylanguage(LanguageName) : null;
    }

    /// <summary>Starts at the first value, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        TBAValueSetValue? x = Values?.GetFirstValue();
        if (x is not null)
        {
            ContentAsInteger = x.AsInteger;
        }
        else
        {
            SetContentToNull();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        base.DefaultSubscribe(Subscriber, RequestedEvent);
        BoldMLAttributes.BoldPrimaryLanguage(BoldSystem).DefaultSubscribe(Subscriber, RequestedEvent);
    }

    [Delphi(PortStatus.Ported, Note = "Indexes Values by the value's integer, as Delphi does.")]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate) =>
        Values[AsInteger].SubscribeToStringRepresentation(Representation, Subscriber, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void SubscribeToLanguage(int representation, string Language, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent)
    {
        BoldMLAttributes.ValidateLanguage(Language, GetType().Name + ".SubscribeToLanguage");
        GetStringBylanguage(representation, Language)!.DefaultSubscribe(Subscriber, RequestedEvent);
    }

    /// <summary>True when the string is the value's name, in any language, expanded as an OCL enum literal.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool CompareToEnumLiteral(string str)
    {
        TBAValueSetValueList languages = BoldMLAttributes.BoldLanguageList();
        for (int i = 0; i < languages.Count; i++)
        {
            string LangRepr = StringRepresentationByLanguage(brDefault, languages[i].AsString);
            LangRepr = BoldNameExpander.BoldExpandName(LangRepr, "", TExpansionType.xtExpression, -1, TBoldNationalCharConversion.nccTrue);
            if (SysUtils.SameText(LangRepr, str))
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a getter method, with SetStringRepresentationByLanguage to set.")]
    public string StringRepresentationByLanguage(int representation, string Language) => GetStringRepresentationByLanguage(representation, Language);
}

/// <summary>A language: a value set whose values are the language objects of the default system.</summary>
[Delphi(PortStatus.Ported)]
public class TBALanguage : TBAValueSet
{
    public TBALanguage()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TBAValueSetValueList GetValues()
    {
        BoldMLAttributes.EnsureLanguageList();
        return BoldMLAttributes.BoldLanguageList();
    }

    /// <summary>Starts at language number 0, else the first language, else null.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        TBAValueSetValueList? y = Values;
        TBAValueSetValue? x = y is null ? null : y.FindByInteger(0) ?? y.GetFirstValue();
        if (x is not null)
        {
            ContentAsInteger = x.AsInteger;
        }
        else
        {
            SetContentToNull();
        }
    }
}

/// <summary>
/// A string with one value per language. brDefault reads the primary language, falling back to the secondary one and
/// then any language (marked with BoldMLStringSecondaryFallBack / BoldMLStringUnknownFallback); brMLString is all of
/// them, and is what is stored (as a blob).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBAMLString : TBAString
{
    private TStringList? fMLStrings;

    public TBAMLString()
    {
    }

    /// <summary>brMLString replaces all languages without sending events per language; brDefault sets the primary language.</summary>
    [Delphi(PortStatus.Ported)]
    private void InternalSetDataValue(TBoldRepresentation Representation, string Value)
    {
        SetToNonNull();
        switch (Representation)
        {
            case BoldMLAttributes.brMLString:
                fMLStrings ??= new TStringList();
                while (fMLStrings.Count > 0)
                {
                    (fMLStrings.Objects(0) as IDisposable)?.Dispose();
                    fMLStrings.Delete(0);
                }

                while (Value.Contains('ÿ'))
                {
                    int p = Value.IndexOf('ÿ');
                    string Lang = Value[..p];
                    Value = Value[(p + 1)..];
                    p = Value.IndexOf('ÿ');
                    string LangValue = p >= 0 ? Value[..p] : Value;
                    Value = p >= 0 ? Value[(p + 1)..] : "";
                    GetStringBylanguage(Lang).ProxyInterface(typeof(IBoldStringContent), TBoldDomainElementProxyMode.bdepContents, out object? StringContent);
                    ((IBoldStringContent)StringContent!).asString = LangValue;
                }

                Changed(beValueChanged, []);
                break;
            case brDefault:
                GetStringBylanguage(BoldMLAttributes.BoldPrimaryLanguage(BoldSystem).AsString).AsString = Value;
                break;
        }
    }

    [Delphi(PortStatus.Ported)] internal string GetContentAsBlob() => GetStringRepresentation(BoldMLAttributes.brMLString);
    [Delphi(PortStatus.Ported)] internal void SetContentAsBlob(string NewValue) => SetStringRepresentation(BoldMLAttributes.brMLString, NewValue);

    /// <summary>The part for the language (empty: the primary language), created when missing.</summary>
    [Delphi(PortStatus.Ported)]
    internal TBAString GetStringBylanguage(string LanguageName)
    {
        BoldMLAttributes.ValidateLanguage(LanguageName, GetType().Name + ".GetStringByLanguage");
        if (LanguageName.Length == 0)
        {
            LanguageName = BoldMLAttributes.BoldPrimaryLanguage(BoldSystem).AsString;
        }

        int StrPos = fMLStrings!.IndexOf(LanguageName);
        if (StrPos == -1)
        {
            var BoldString = (TBAString)CreateInstance(StringClass(), m => m.SetOwningElement(this));
            StrPos = fMLStrings.AddObject(LanguageName, BoldString);
        }

        return (TBAString)fMLStrings.Objects(StrPos)!;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is IBoldBlobContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetStringRepresentation(BoldMLAttributes.brMLString, s.asBlob);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        switch (Representation)
        {
            case BoldMLAttributes.brMLString:
                {
                    string result = "";
                    for (int i = 0; i < fMLStrings!.Count; i++)
                    {
                        result += fMLStrings[i] + 'ÿ' + ((TBAString)fMLStrings.Objects(i)!).AsString + 'ÿ';
                    }

                    return result;
                }

            case brDefault:
                {
                    if (IsNull)
                    {
                        return "";
                    }

                    TBALanguage DisplayLanguage = BoldMLAttributes.BoldPrimaryLanguage(BoldSystem);
                    string result = GetStringBylanguage(DisplayLanguage.AsString).AsString;
                    if (result.Length == 0)
                    {
                        DisplayLanguage = BoldMLAttributes.BoldSecondaryLanguage(BoldSystem);
                        result = GetStringBylanguage(DisplayLanguage.AsString).AsString;
                        if (result.Length != 0)
                        {
                            result = SysUtils.Format(BoldMLAttributes.BoldMLStringSecondaryFallBack, DisplayLanguage.AsString, result);
                        }
                        else
                        {
                            TBAValueSetValueList languages = BoldMLAttributes.BoldLanguageList();
                            for (int i = 0; i < languages.Count; i++)
                            {
                                result = GetStringBylanguage(languages[i].AsString).AsString;
                                if (result.Length != 0)
                                {
                                    result = SysUtils.Format(BoldMLAttributes.BoldMLStringUnknownFallback, languages[i].AsString, result);
                                    break;
                                }
                            }
                        }
                    }

                    return result;
                }

            default:
                return base.GetStringRepresentation(Representation);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (Representation is BoldMLAttributes.brMLString or brDefault)
        {
            InternalSetDataValue(Representation, Value);
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    /// <summary>Passes value events of the parts on, and brackets their modifications with this string's.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void ReceiveEventFromOwned(object? originator, TBoldEvent originalEvent)
    {
        if (beValueEvents.Contains(originalEvent))
        {
            SendEvent(originalEvent);
        }

        if (originalEvent == bePrepareModify)
        {
            StartModify();
            PreChange();
        }

        if (originalEvent == beCompleteModify)
        {
            EndModify();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override bool ReceiveQueryFromOwned(object? Originator, TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        OriginalEvent != bqMayModify || CanModify();

    [Delphi(PortStatus.Ported)]
    protected virtual Type StringClass() => typeof(TBAMLSubString);

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        fMLStrings = new TStringList();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBAMLString_Proxy);

    [Delphi(PortStatus.Ported, Symbol = "TBAMLString.Destroy")]
    public override void Dispose()
    {
        base.Dispose();
        if (fMLStrings is not null)
        {
            while (fMLStrings.Count > 0)
            {
                (fMLStrings.Objects(0) as IDisposable)?.Dispose();
                fMLStrings.Delete(0);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Blob;

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation) =>
        fMLStrings!.Count > 0 ? ((TBAString)fMLStrings.Objects(0)!).ValidateString(Value, Representation) : base.ValidateString(Value, Representation);

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        fMLStrings!.Count > 0 ? ((TBAString)fMLStrings.Objects(0)!).ValidateCharacter(C, Representation) : base.ValidateCharacter(C, Representation);

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBAMLString)
        {
            SetStringRepresentation(BoldMLAttributes.brMLString, Source.GetStringRepresentation(BoldMLAttributes.brMLString));
        }
        else
        {
            AsString = Source!.AsString;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        base.DefaultSubscribe(Subscriber, RequestedEvent);
        BoldMLAttributes.BoldPrimaryLanguage(BoldSystem).DefaultSubscribe(Subscriber, RequestedEvent);
        BoldMLAttributes.BoldSecondaryLanguage(BoldSystem).DefaultSubscribe(Subscriber, RequestedEvent);
    }

    /// <summary>brDefault subscribes to the primary language part, and to the secondary one when the primary is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Representation == brDefault)
        {
            TBALanguage Language;

            bool SubscribeAndTestEmpty()
            {
                TBAString temp = GetStringBylanguage(Language.AsString);
                temp.DefaultSubscribe(Subscriber, RequestedEvent);
                Language.DefaultSubscribe(Subscriber, RequestedEvent);
                return temp.AsString.Length == 0;
            }

            DefaultSubscribe(Subscriber, RequestedEvent);
            Language = BoldMLAttributes.BoldPrimaryLanguage(BoldSystem);
            if (SubscribeAndTestEmpty())
            {
                Language = BoldMLAttributes.BoldSecondaryLanguage(BoldSystem);
                SubscribeAndTestEmpty();
            }
        }
        else
        {
            base.SubscribeToStringRepresentation(Representation, Subscriber, RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SubscribeToLanguage(string Language, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent)
    {
        BoldMLAttributes.ValidateLanguage(Language, GetType().Name + ".SubscribeToLanguage");
        GetStringBylanguage(Language).DefaultSubscribe(Subscriber, RequestedEvent);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldBlobContent))
        {
            object proxy = Activator.CreateInstance(ProxyClass(), this, Mode)!;
            if (proxy is not IBoldBlobContent)
            {
                throw new EBoldInternal(SysUtils.Format(sProxyClassDidntImplementInterface, GetType().Name));
            }

            Obj = proxy;
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBAString AsStringByLanguage(string Language) => GetStringBylanguage(Language);
}

/// <summary>Gives a multi-language string as blob contents (all languages) as well as string contents.</summary>
[Delphi(PortStatus.Ported)]
public class TBAMLString_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBAString_Proxy(aProxedElement, aMode), IBoldBlobContent
{
    [Delphi(PortStatus.Ported)] private TBAMLString GetProxedMLString() => (TBAMLString)ProxedElement;

    [Delphi(PortStatus.Ported, Note = "Delphi implements IBoldBlobContent by delegation to this property; here the proxy implements it and forwards.")]
    protected TBAMLString ProxedMLString => GetProxedMLString();

    public string GetContentAsBlob() => ProxedMLString.GetContentAsBlob();
    public void SetContentAsBlob(string NewValue) => ProxedMLString.SetContentAsBlob(NewValue);
}

/// <summary>One language's part of a multi-language string: modifications go through the owning string's undo and locking.</summary>
[Delphi(PortStatus.Ported)]
public class TBAMLSubString : TBAString
{
    public TBAMLSubString()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override bool StartModify()
    {
        if (OwningElement is TBoldMember OwningMember)
        {
            if (BoldPersistenceState is not (TBoldValuePersistenceState.bvpsCurrent or TBoldValuePersistenceState.bvpsModified
                or TBoldValuePersistenceState.bvpsTransient or TBoldValuePersistenceState.bvpsInvalid))
            {
                StateError("StartModify");
            }

            bool result = CanModify();
            if (result && OwningMember.BoldSystem is not null && OwningMember.OwningObject is not null &&
                OwningMember.BoldSystem.PessimisticLockHandler is not null && !OwningMember.BoldMemberRTInfo!.IsDerived)
            {
                result = OwningMember.BoldSystem.PessimisticLockHandler.LockElement(OwningMember);
            }

            if (result)
            {
                if (OwningMember.OwningObject is not null && !OwningMember.BoldSystem!.InTransaction() && StoreInUndo())
                {
                    OwningMember.BoldSystem.UndoHandler.HandleMember(OwningMember.OwningObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents),
                        OwningMember.BoldMemberRTInfo!.index, OwningMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
                }

                DoStartModify();
            }

            return result;
        }

        return base.StartModify();
    }
}
