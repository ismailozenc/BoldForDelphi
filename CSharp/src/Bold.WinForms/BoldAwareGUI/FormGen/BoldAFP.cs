// Delphi: Source/BoldAwareGUI/FormGen/BoldAFP.pas
// Port map: CSharp/port/units/BoldAFP.md

using System.Windows.Forms;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldAFP", Symbol = "TBoldAutoFormProviderClass", Note = "A System.Type of a TBoldAutoFormProvider class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldAFP", Symbol = "initialization", Note = "Empty in Delphi; the registry is made on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAFP", Symbol = "finalization", Note = "Freed the registry; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// Makes a form for an element (an object, a list, an attribute): the form, a reference handle on the element, and
/// the controls that show it. A provider class is registered for a kind of element.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAutoFormProvider : TBoldMemoryManagedObject
{
    private TBoldReferenceHandle? fBoldHandle;
    private readonly TBoldElement fElement;

    /// <summary>A provider of a form for the element.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldAutoFormProvider(TBoldElement BoldElement)
    {
        fElement = BoldElement;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetElementIsList() => ElementTypeInfo.BoldValueType == TBoldValueTypeSet.bvtList;

    [Delphi(PortStatus.Ported)]
    private bool GetHandleIsList() => BoldHandle!.StaticBoldType is not null && BoldHandle.StaticBoldType.BoldValueType == TBoldValueTypeSet.bvtList;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo GetElementTypeInfo() => Element.BoldType!;

    /// <summary>Makes the controls: before, the provider's own, after (the handle gets the element).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DoEnsureComponents()
    {
        PreEnsureComponents(); //Empty!
        EnsureComponents();
        PostEnsureComponents();
    }

    /// <summary>Makes a new form of the form class and puts the handle and controls on it.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DoGenerateForm()
    {
        EnsureForm();
        if (Form is not null)
        {
            EnsureHandle();
            DoEnsureComponents();
        }
    }

    [Delphi(PortStatus.Ported)] protected abstract void EnsureComponents();

    /// <summary>Makes the form, of the form class (no form without one).</summary>
    [Delphi(PortStatus.Ported, Note = "Makes the form with Activator; Delphi made it owned by the application.")]
    protected virtual void EnsureForm() =>
        Form = FormClass is not null ? (Form)Activator.CreateInstance(FormClass)! : null;

    [Delphi(PortStatus.Ported)] protected abstract void EnsureHandle();
    [Delphi(PortStatus.Ported)] protected abstract Form? GetForm();

    [Delphi(PortStatus.Ported, Note = "Gives a Type (a Form class) where Delphi gives a class reference.")]
    protected abstract Type? GetFormClass();

    [Delphi(PortStatus.Ported, Note = "The control the controls go on; Delphi's is a TWinControl.")]
    protected abstract Control? GetTarget();

    /// <summary>The model name of the element (a member, an object or a system), else the default.</summary>
    [Delphi(PortStatus.Ported)]
    protected string ModelNameOrDefault(string DefaultName) => Element switch
    {
        TBoldMember member when member.BoldMemberRTInfo is not null => member.BoldMemberRTInfo.ModelName,
        TBoldObject obj => obj.BoldClassTypeInfo.ModelName,
        TBoldSystem system => system.BoldSystemTypeInfo.ModelName,
        _ => DefaultName,
    };

    /// <summary>Gives the handle its element; without a handle the form goes and this raises.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void PostEnsureComponents()
    {
        if (BoldHandle is not null)
        {
            BoldHandle.Value = Element;
        }
        else
        {
            Form?.Dispose();
            Form = null;
            throw new EBoldInternal(SysUtils.Format("%s.PostEnsureComponents: BoldHandle not assigned", GetType().Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PostGenerateAutoForm()
    {
        // Intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PreEnsureComponents()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PreGenerateAutoForm()
    {
        // Intentionally left blank
    }

    [Delphi(PortStatus.Ported)] protected abstract void SetForm(Form? Value);

    /// <summary>Makes the form for the element; null when the provider makes none.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual Form? GenerateAutoForm()
    {
        PreGenerateAutoForm();
        DoGenerateForm();
        if (Form is not null)
        {
            PostGenerateAutoForm();
        }

        return Form;
    }

    [Delphi(PortStatus.Ported)] protected TBoldElement Element => fElement;
    [Delphi(PortStatus.Ported)] protected bool ElementIsList => GetElementIsList();

    [Delphi(PortStatus.Ported)]
    protected Form? Form
    {
        get => GetForm();
        set => SetForm(value);
    }

    [Delphi(PortStatus.Ported)] protected bool HandleIsList => GetHandleIsList();
    [Delphi(PortStatus.Ported)] protected Control? Target => GetTarget();
    [Delphi(PortStatus.Ported)] protected TBoldElementTypeInfo ElementTypeInfo => GetElementTypeInfo();

    [Delphi(PortStatus.Ported)]
    public TBoldReferenceHandle? BoldHandle
    {
        get => fBoldHandle;
        set => fBoldHandle = value;
    }

    [Delphi(PortStatus.Ported)] public Type? FormClass => GetFormClass();
}

/// <summary>A registered provider class: the kind of element (value type and element class) it makes forms for.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAutoFormProviderHolder : TBoldMemoryManagedObject
{
    private readonly Type fClass;
    private readonly TBoldValueTypeSet fValueType;
    private readonly Type fElementClass;
    private readonly bool fIsListProvider;

    [Delphi(PortStatus.Ported, Note = "The classes are Types.")]
    public TBoldAutoFormProviderHolder(TBoldValueTypeSet ValueType, Type ElementClass, Type ProviderClass, bool IsList)
    {
        fClass = ProviderClass;
        fValueType = ValueType;
        fElementClass = ElementClass;
        fIsListProvider = IsList;
    }

    [Delphi(PortStatus.Ported)] public Type ProviderClass => fClass;
    [Delphi(PortStatus.Ported)] public Type ElementClass => fElementClass;
    [Delphi(PortStatus.Ported)] public TBoldValueTypeSet ValueType => fValueType;
    [Delphi(PortStatus.Ported)] public bool IsListProvider => fIsListProvider;
}

/// <summary>The registered form providers; the last registered that matches an element makes its form.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAutoFormProviderRegistry : TBoldIndexableList
{
    private static int IX_ProviderClass = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldAutoFormProviderRegistry()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_ProviderClass, AddIndex(new TProviderClassIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAutoFormProviderHolder GetHolders(int index) => (TBoldAutoFormProviderHolder)this[index]!;

    /// <summary>
    /// The holder for the element: the last one whose element class the element is (and that is a list provider just
    /// when the element is a list), else the last whose value type is the element's (or its list's elements').
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's closest-class search came after a break and never ran; it is not ported.")]
    protected TBoldAutoFormProviderHolder? GetMatchingProvider(TBoldElement BoldElement)
    {
        TBoldAutoFormProviderHolder? Result = null;
        TBoldValueTypeSet MatchType = TBoldValueTypeSet.bvtSystem; // Set to avoid compiler warning
        bool LookForListProvider = BoldElement.BoldType!.BoldValueType == TBoldValueTypeSet.bvtList;
        if (LookForListProvider)
        {
            MatchType = ((TBoldListTypeInfo)BoldElement.BoldType).ListElementTypeInfo!.BoldValueType;
        }

        // Match on classtype
        for (int i = Count - 1; i >= 0; i--)
        {
            if (Holders(i).ElementClass.IsInstanceOfType(BoldElement) && Holders(i).IsListProvider == LookForListProvider)
            {
                Result = Holders(i);
                break;
            }
        }

        // Match on BoldValueType
        if (Result is null)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (BoldElement.BoldType.BoldValueType == Holders(i).ValueType || (LookForListProvider && MatchType == Holders(i).ValueType))
                {
                    Result = Holders(i);
                    break;
                }
            }
        }

        return Result;
    }

    /// <summary>A new form for the element, from the matching provider; null when none matches.</summary>
    [Delphi(PortStatus.Ported)]
    public Form? FormForElement(TBoldElement BoldElement)
    {
        TBoldAutoFormProviderHolder? Holder = GetMatchingProvider(BoldElement);
        if (Holder is null)
        {
            return null;
        }

        var provider = (TBoldAutoFormProvider)Activator.CreateInstance(Holder.ProviderClass, BoldElement)!;
        return provider.GenerateAutoForm();
    }

    [Delphi(PortStatus.Ported)]
    public void RegisterListProvider(TBoldValueTypeSet ValueType, Type ElementClass, Type ProviderClass) =>
        Add(new TBoldAutoFormProviderHolder(ValueType, ElementClass, ProviderClass, true));

    [Delphi(PortStatus.Ported)]
    public void RegisterProvider(TBoldValueTypeSet ValueType, Type ElementClass, Type ProviderClass) =>
        Add(new TBoldAutoFormProviderHolder(ValueType, ElementClass, ProviderClass, false));

    [Delphi(PortStatus.Ported)]
    public void UnregisterProvider(Type ProviderClass) =>
        Remove(((TProviderClassIndex)Indexes(IX_ProviderClass)!).FindByProviderClass(ProviderClass));

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldAutoFormProviderHolder Holders(int index) => GetHolders(index);

    [Delphi(PortStatus.Ported, Symbol = "TProviderClassIndex")]
    private sealed class TProviderClassIndex : TBoldClassHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TProviderClassIndex.ItemAsKeyClass")]
        protected override TClass ItemAsKeyClass(object? Item) => ((TBoldAutoFormProviderHolder)Item!).ProviderClass;

        /// <summary>FindByClass, reachable from the registry.</summary>
        internal object? FindByProviderClass(TClass KeyClass) => FindByClass(KeyClass);
    }
}

/// <summary>Unit-level routines of BoldAFP.</summary>
public static class BoldAFP
{
    private static TBoldAutoFormProviderRegistry? G_AutoFormProviderRegistry;

    /// <summary>The registry of form providers, made on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAutoFormProviderRegistry AutoFormProviderRegistry() => G_AutoFormProviderRegistry ??= new TBoldAutoFormProviderRegistry();
}
