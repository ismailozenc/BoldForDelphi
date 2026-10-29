// Delphi: Source/BoldAwareGUI/FormGen/BoldAFPPluggable.pas
// Port map: CSharp/port/units/BoldAFPPluggable.md

using System.ComponentModel;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAFPPluggable", Symbol = "finalization", Note = "Freed the list of placeable providers and the registry at exit; they end with the process.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAFPPluggableReg", Symbol = "Register", Note = "Put TBoldPlaceableAFP on the Delphi component palette; a WinForms component shows in the Toolbox by itself.")]

namespace Bold;

/// <summary>Gives the form class for an element (null for none).</summary>
[Delphi(PortStatus.Ported, Note = "The form class is a Type.")]
public delegate void TBoldGetFormClassEvent(TBoldElement Element, ref Type? Result);

/// <summary>Gives the reference handle of a form that shows the element (null for none).</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldRetrieveHandleEvent(Form Form, ref TBoldReferenceHandle? Result);

/// <summary>Says whether the member is shown on the default object form.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldMemberShouldBeDisplayedEvent(TBoldMemberRTInfo Member, ref bool ShowMember);

/// <summary>How an installable form's reference handle is found: by its name, or as the form's only reference handle.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldHandleLocatorStyle { hfByName, hfByType }

/// <summary>
/// A component that plugs forms of its own into the auto forms of objects: its events give the form class for an
/// element, the form's reference handle, and which members the default form shows. While one exists, its forms are
/// made for objects.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPlaceableAFP : Component
{
    private TBoldGetFormClassEvent? fOnGetFormClass;
    private TBoldRetrieveHandleEvent? fOnRetrieveHandle;
    private TBoldMemberShouldBeDisplayedEvent? fOnMemberShouldBeDisplayed;

    /// <summary>Registers the component (and the provider that asks the registered components) for all objects.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer.")]
    public TBoldPlaceableAFP(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        BoldAFPPluggable.EnsureInitialized();
        BoldAFPPluggable.G_AFPList.Add(this);
        // Handle all BoldObjects
        BoldAFP.AutoFormProviderRegistry().RegisterProvider(TBoldValueTypeSet.bvtClass, typeof(TBoldObject), typeof(TBoldRegisterAFP));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPlaceableAFP.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BoldAFP.AutoFormProviderRegistry().UnregisterProvider(typeof(TBoldRegisterAFP));
            BoldAFPPluggable.G_AFPList.Remove(this);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected internal Type? GetFormClass(TBoldElement Element)
    {
        Type? result = null;
        fOnGetFormClass?.Invoke(Element, ref result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldReferenceHandle? RetrieveHandle(Form? Form)
    {
        TBoldReferenceHandle? result = null;
        if (Form is not null && fOnRetrieveHandle is not null)
        {
            fOnRetrieveHandle(Form, ref result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal bool MemberShouldBeDisplayed(TBoldMemberRTInfo Member)
    {
        bool result = true;
        fOnMemberShouldBeDisplayed?.Invoke(Member, ref result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldGetFormClassEvent? OnGetFormClass
    {
        get => fOnGetFormClass;
        set => fOnGetFormClass = value;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldRetrieveHandleEvent? OnRetrieveHandle
    {
        get => fOnRetrieveHandle;
        set => fOnRetrieveHandle = value;
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldMemberShouldBeDisplayedEvent? OnMemberShouldBeDisplayed
    {
        get => fOnMemberShouldBeDisplayed;
        set => fOnMemberShouldBeDisplayed = value;
    }
}

/// <summary>
/// An object form provider with a form class of its own (whose reference handle shows the object), else, when
/// default forms are allowed, the default object form.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPluggableAFP : TBoldDefaultObjectAutoFormProvider
{
    private bool fDefaultBehaviour;

    protected TBoldPluggableAFP(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    [Delphi(PortStatus.Ported)] protected abstract TBoldReferenceHandle? RetrieveHandle();

    [Delphi(PortStatus.Ported, Note = "Gives a Type (a Form class).")]
    protected abstract Type? RetrieveFormClass();

    /// <summary>The default form's handle, else the reference handle of the form made.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureHandle()
    {
        if (DefaultBehaviour)
        {
            base.EnsureHandle();
        }
        else
        {
            BoldHandle = RetrieveHandle();
        }
    }

    /// <summary>The default form's controls; a form of its own has its controls.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureComponents()
    {
        if (DefaultBehaviour)
        {
            base.EnsureComponents();
        }
    }

    /// <summary>The provider's form class, else (when allowed) the default form's.</summary>
    [Delphi(PortStatus.Ported)]
    protected override Type? GetFormClass()
    {
        Type? result = RetrieveFormClass();
        DefaultBehaviour = result is null && BoldAFPPluggable.AllowDefaultForm;
        if (DefaultBehaviour)
        {
            result = base.GetFormClass();
        }

        return result;
    }

    /// <summary>The form keeps its own position and size.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void PostEnsureForm()
    {
    }

    /// <summary>True when the default object form is made.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool DefaultBehaviour
    {
        get => fDefaultBehaviour;
        set => fDefaultBehaviour = value;
    }
}

/// <summary>The provider that asks the placeable components, in the order they were made, for the form class and handle.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegisterAFP : TBoldPluggableAFP
{
    public TBoldRegisterAFP(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    /// <summary>The first handle a placeable component gives for the form.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldReferenceHandle? RetrieveHandle()
    {
        TBoldReferenceHandle? result = null;
        foreach (TBoldPlaceableAFP afp in BoldAFPPluggable.G_AFPList.ToList())
        {
            result = afp.RetrieveHandle(Form);
            if (result is not null)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>The first form class a placeable component gives for the element.</summary>
    [Delphi(PortStatus.Ported)]
    protected override Type? RetrieveFormClass()
    {
        Type? result = null;
        foreach (TBoldPlaceableAFP afp in BoldAFPPluggable.G_AFPList.ToList())
        {
            result = afp.GetFormClass(Element);
            if (result is not null)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>Shown when the default form shows it and every placeable component agrees.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool MemberShouldBeDisplayed(TBoldMemberRTInfo Member)
    {
        bool result = base.MemberShouldBeDisplayed(Member);
        if (result)
        {
            foreach (TBoldPlaceableAFP afp in BoldAFPPluggable.G_AFPList.ToList())
            {
                result = afp.MemberShouldBeDisplayed(Member);
                if (!result)
                {
                    break;
                }
            }
        }

        return result;
    }
}

/// <summary>The provider of the installable forms: a form class registered for the object's class (by model name).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldInstallableAFP : TBoldPluggableAFP
{
    public TBoldInstallableAFP(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    /// <summary>The form's reference handle of the registered name.</summary>
    [Delphi(PortStatus.Ported, Note = "Looks among the components the form owns (BoldOwnedComponents), Delphi's FindComponent.")]
    protected TBoldReferenceHandle? FindHandleByName() =>
        BoldOwnedComponents.Find(Form!, BoldAFPPluggable.InstallableFormsRegistry().DescriptorsByBOClassName(Element.BoldType!.ModelName)!.HandleName) as TBoldReferenceHandle;

    /// <summary>The form's only reference handle (none when it has several).</summary>
    [Delphi(PortStatus.Ported, Note = "Looks among the components the form owns (BoldOwnedComponents).")]
    protected TBoldReferenceHandle? FindHandleByType()
    {
        TBoldReferenceHandle? result = null;
        foreach (TBoldReferenceHandle handle in BoldOwnedComponents.All(Form!).OfType<TBoldReferenceHandle>())
        {
            // if we find one, lets use it, if we find more, drop the first and exit.
            if (result is null)
            {
                result = handle;
            }
            else
            {
                result = null;
                break;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override Type? RetrieveFormClass() =>
        BoldAFPPluggable.InstallableFormsRegistry().DescriptorsByBOClassName(Element.BoldType!.ModelName)?.FormClass;

    [Delphi(PortStatus.Ported)]
    protected override TBoldReferenceHandle? RetrieveHandle() =>
        BoldAFPPluggable.InstallableFormsRegistry().DescriptorsByBOClassName(Element.BoldType!.ModelName)!.HandleLocatorStyle switch
        {
            TBoldHandleLocatorStyle.hfByName => FindHandleByName(),
            TBoldHandleLocatorStyle.hfByType => FindHandleByType(),
            _ => null,
        };
}

/// <summary>An installable form: the form class for a class (by model name) and how its reference handle is found.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldInstallableFormsDescriptor : TBoldMemoryManagedObject
{
    private readonly Type fFormClass;
    private readonly string fBOClassName;
    private readonly string fHandleName;
    private readonly TBoldHandleLocatorStyle fHandleLocatorStyle;

    [Delphi(PortStatus.Ported, Note = "The form class is a Type.")]
    public TBoldInstallableFormsDescriptor(string BOClassName, Type FormClass, TBoldHandleLocatorStyle HandleLocatorStyle, string HandleName)
    {
        fBOClassName = BOClassName;
        fFormClass = FormClass;
        fHandleLocatorStyle = HandleLocatorStyle;
        fHandleName = HandleName;
    }

    [Delphi(PortStatus.Ported)] public Type FormClass => fFormClass;
    [Delphi(PortStatus.Ported)] public string BOClassName => fBOClassName;
    [Delphi(PortStatus.Ported)] public string HandleName => fHandleName;
    [Delphi(PortStatus.Ported)] public TBoldHandleLocatorStyle HandleLocatorStyle => fHandleLocatorStyle;
}

/// <summary>The installable forms, by the model name of their class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldInstallableFormsRegistry : TBoldIndexableList
{
    private static int IX_BOClassName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldInstallableFormsRegistry()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_BOClassName, AddIndex(new TBOClassNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldInstallableFormsDescriptor? GetDescriptorsByBOClassName(string BOClassName) =>
        (TBoldInstallableFormsDescriptor?)((TBOClassNameIndex)Indexes(IX_BOClassName)!).FindByString(BOClassName);

    [Delphi(PortStatus.Ported)]
    public void RegisterInstallableFormsDescriptor(string BOClassName, Type FormClass, TBoldHandleLocatorStyle HandleLocatorStyle, string HandleName) =>
        Add(new TBoldInstallableFormsDescriptor(BOClassName, FormClass, HandleLocatorStyle, HandleName));

    [Delphi(PortStatus.Ported)]
    public void UnRegisterInstallableFormsDescriptor(string BOClassName) => Remove(DescriptorsByBOClassName(BOClassName));

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldInstallableFormsDescriptor? DescriptorsByBOClassName(string BOClassName) => GetDescriptorsByBOClassName(BOClassName);

    [Delphi(PortStatus.Ported, Symbol = "TBOClassNameIndex")]
    private sealed class TBOClassNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBOClassNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldInstallableFormsDescriptor)Item!).BOClassName;
    }
}

/// <summary>Unit-level routines, variables and the initialization of BoldAFPPluggable.</summary>
public static class BoldAFPPluggable
{
    private static TBoldInstallableFormsRegistry? G_InstallableForms;

    /// <summary>The placeable providers, in the order they were made.</summary>
    internal static readonly List<TBoldPlaceableAFP> G_AFPList = [];

    /// <summary>A pluggable provider without a form class of its own makes the default object form.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool AllowDefaultForm = true;

    private static bool fInitialized;

    /// <summary>The registry of installable forms, made on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldInstallableFormsRegistry InstallableFormsRegistry()
    {
        EnsureInitialized();
        return G_InstallableForms ??= new TBoldInstallableFormsRegistry();
    }

    /// <summary>Registers the installable forms' provider for objects.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Runs when the unit is first used (its registry or a placeable provider), as a Delphi unit's initialization runs when the unit is linked into the application. The list is made with the class.")]
    internal static void Initialization() =>
        BoldAFP.AutoFormProviderRegistry().RegisterProvider(TBoldValueTypeSet.bvtClass, typeof(TBoldObject), typeof(TBoldInstallableAFP));

    /// <summary>Runs the initialization once.</summary>
    internal static void EnsureInitialized()
    {
        if (!fInitialized)
        {
            fInitialized = true;
            Initialization();
        }
    }
}
