// Delphi: Source/BoldAwareGUI/BoldControls/BoldPropertiesController.pas
// Port map: CSharp/port/units/BoldPropertiesController.md

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPropertiesController", Symbol = "TBoldDrivenPropertyCollection.GetOwner", Note = "The controller is a component, not a TPersistent, so it is not the collection's owner; PropertiesController gives it.")]

namespace Bold;

/// <summary>The driven properties of a properties controller.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDrivenPropertyCollection : TCollection
{
    private readonly TBoldPropertiesController FPropertiesController;

    [Delphi(PortStatus.Ported)]
    public TBoldDrivenPropertyCollection(TBoldPropertiesController PropertiesController)
        : base(typeof(TBoldDrivenProperty))
    {
        FPropertiesController = PropertiesController;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldDrivenProperty GetItem(int Index) => (TBoldDrivenProperty)base[Index];

    [Delphi(PortStatus.Ported)]
    private void SetItem(int Index, TBoldDrivenProperty Value) => base[Index] = Value;

    [Delphi(PortStatus.Ported)]
    public new TBoldDrivenProperty Add() => (TBoldDrivenProperty)base.Add();

    [Delphi(PortStatus.Ported)] protected internal TBoldPropertiesController PropertiesController => FPropertiesController;

    [Delphi(PortStatus.Ported, Symbol = "TBoldDrivenPropertyCollection.Items")]
    public new TBoldDrivenProperty this[int Index]
    {
        get => GetItem(Index);
        set => SetItem(Index, value);
    }
}

/// <summary>
/// A property of a component (VCLComponent, any object with properties) that the controller's expression drives. The
/// property name may be a path ("FocusControl.Font.Size", "Columns[2].Title"). Unless ReadOnly, the component's value
/// goes back to the element when the component is left (when it is the only driven property).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDrivenProperty : TCollectionItem
{
    /// <summary>The event that tells the component was left: Leave, the WinForms name of Delphi's OnExit.</summary>
    private const string EventNameOnExit = "Leave";

    private string FPropertyName = "";
    private IComponent? FVCLComponent;
    private bool FReadOnly;
    private readonly TBoldDrivenPropertyCollection fDrivenProperties;
    private Delegate? fOnExitHandler;

    [Delphi(PortStatus.Ported)]
    public TBoldDrivenProperty(TCollection? Collection)
        : base(Collection)
    {
        fDrivenProperties = (TBoldDrivenPropertyCollection)Collection!;
        FReadOnly = true;
    }

    /// <summary>Clears the property name when the component is cleared.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's check of the name against the component's properties came after an exit and never ran; it is not ported.")]
    protected virtual void EnsureValidPropertyName()
    {
        // At least clear the property when we clear the component
        if (VCLComponent is null)
        {
            PropertyName = "";
        }
    }

    /// <summary>
    /// Sets the component; the controller is told when it is disposed. With only this driven property, and the
    /// component not in a designer, leaving the component writes its value back.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void SetVCLComponent(IComponent? Value)
    {
        // We don't support the two way update for collections of more than one driven property
        bool AllowHookUnHook = Value is not null && !(Value.Site?.DesignMode == true || Collection!.Count > 1);

        if (AllowHookUnHook)
        {
            UnhookOnExit();
        }

        FVCLComponent = Value;
        if (AllowHookUnHook)
        {
            HookOnExit();
        }

        if (Value is not null)
        {
            ((TBoldDrivenPropertyCollection)Collection!).PropertiesController.FreeNotification(Value);
        }

        EnsureValidPropertyName();
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetPropertyName(string Value) => FPropertyName = Value;

    [Delphi(PortStatus.Ported)]
    protected virtual void SetReadOnly(bool Value) => FReadOnly = Value;

    /// <summary>The component was left: unless read-only, its value goes to the element (applied with the exit policy).</summary>
    [Delphi(PortStatus.Ported, Note = "The component's other Leave handlers run as well (.NET events are multicast), so there is no original handler to call.")]
    protected virtual void DoOnExit(object? Sender)
    {
        if (!ReadOnly && PropertiesController.BoldProperties.MayModify(PropertiesController.HandleFollower.Follower))
        {
            PropertiesController.BoldProperties.MayHaveChanged(GetPropValue(VCLComponent!, PropertyName), PropertiesController.HandleFollower.Follower);
            if (PropertiesController.BoldProperties.ApplyPolicy == TBoldApplyPolicy.bapExit)
            {
                PropertiesController.HandleFollower.Follower.Apply();
            }
        }
    }

    private void DoOnExitHandler(object? sender, EventArgs e) => DoOnExit(sender);

    /// <summary>Handles the component's Leave event, when it has one.</summary>
    [Delphi(PortStatus.Ported, Note = "Adds a handler to the Leave event through reflection, where Delphi replaced the OnExit method property.")]
    protected virtual void HookOnExit()
    {
        EventInfo? exitEvent = VCLComponent?.GetType().GetEvent(EventNameOnExit);
        if (exitEvent?.EventHandlerType is not null)
        {
            fOnExitHandler = Delegate.CreateDelegate(exitEvent.EventHandlerType, this, nameof(DoOnExitHandler), false, false);
            if (fOnExitHandler is not null)
            {
                exitEvent.AddEventHandler(VCLComponent, fOnExitHandler);
            }
        }
    }

    /// <summary>Stops handling the component's Leave event.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void UnhookOnExit()
    {
        EventInfo? exitEvent = VCLComponent?.GetType().GetEvent(EventNameOnExit);
        if (exitEvent is not null && fOnExitHandler is not null)
        {
            exitEvent.RemoveEventHandler(VCLComponent, fOnExitHandler);
        }

        fOnExitHandler = null;
    }

    /// <summary>Sets the property to the value of the controller's expression on the element (or to "nothing").</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetPropertyValue(TBoldFollower Follower)
    {
        using var ie = new TBoldIndirectElement();
        TBoldElement? SendElement = null;
        if (PropertiesController.BoldHandle is not null && Follower.Element is not null)
        {
            Follower.Element.EvaluateExpression(PropertiesController.BoldProperties.Expression, ie);
            SendElement = ie.Value;
        }

        SetRelativePropValue(VCLComponent, PropertyName, SendElement);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPropertiesController GetPropertiesController() => DrivenProperties.PropertiesController;

    /// <summary>
    /// Follows the path from StartInstance to the object that has the last property, and gives that object and the
    /// property's name: for 'FocusControl.Font.Size' from a label, its focus control's font and 'Size'. LastObject is
    /// null when the path goes through nothing.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Through reflection; a name that is not a property ends the path in nothing, where Delphi failed.")]
    private static void ConvertRelativeProp(object? StartInstance, string PropNamePath, out object? LastObject, out string PropName)
    {
        string[] Path = PropNamePath.Split('.');
        LastObject = StartInstance;
        PropName = Path[^1];
        for (int I = 0; I < Path.Length; I++)
        {
            // The path may very well follow unassigned links. This check prevents an AV
            if (LastObject is null)
            {
                return;
            }

            string PathItem = Path[I];
            int OpenBracketPos = PathItem.IndexOf('[', StringComparison.Ordinal);
            if (OpenBracketPos < 0)
            {
                // Special case for when the last property is of tkClass we don't want
                // to loose LastObject to be in fact the Previous before Last !
                if (I < Path.Length - 1)
                {
                    PropertyInfo? prop = LastObject.GetType().GetProperty(PathItem);
                    if (prop is null)
                    {
                        LastObject = null;
                    }
                    else if (KindOf(prop.PropertyType) == PropKind.Class)
                    {
                        LastObject = prop.GetValue(LastObject);
                    }
                }
            }
            else
            {
                PropertyInfo? prop = LastObject.GetType().GetProperty(PathItem[..OpenBracketPos]);
                object? collection = prop?.GetValue(LastObject);
                int ColIndex = SysUtils.StrToInt(PathItem[(OpenBracketPos + 1)..^1]);
                LastObject = collection switch
                {
                    TCollection c => c[ColIndex],
                    System.Collections.IList l => l[ColIndex],
                    _ => null,
                };
            }
        }
    }

    private enum PropKind { Enumeration, Integer, Class, Other }

    /// <summary>Delphi's type kinds as far as setting a property tells them apart.</summary>
    private static PropKind KindOf(Type t)
    {
        Type type = Nullable.GetUnderlyingType(t) ?? t;
        if (type.IsEnum || type == typeof(bool))
        {
            return PropKind.Enumeration;
        }

        if (type == typeof(int) || type == typeof(uint) || type == typeof(short) || type == typeof(ushort) ||
            type == typeof(byte) || type == typeof(sbyte) || type == typeof(long) || type == typeof(ulong))
        {
            return PropKind.Integer;
        }

        return type.IsValueType || type == typeof(string) || typeof(Delegate).IsAssignableFrom(type) ? PropKind.Other : PropKind.Class;
    }

    /// <summary>The value converted to the property's type, as Delphi's SetPropValue converts a variant.</summary>
    private static object? ConvertTo(Type t, object? value)
    {
        Type type = Nullable.GetUnderlyingType(t) ?? t;
        if (value is null || type.IsInstanceOfType(value))
        {
            return value;
        }

        if (type.IsEnum)
        {
            return value is string name ? Enum.Parse(type, name, true) : Enum.ToObject(type, Convert.ToInt64(value, CultureInfo.CurrentCulture));
        }

        if (value is string s)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromString(null, CultureInfo.CurrentCulture, s);
            }
        }

        return Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
    }

    /// <summary>The value of the property (a path) of the component, as a string.</summary>
    private static string GetPropValue(object Instance, string PropNamePath)
    {
        ConvertRelativeProp(Instance, PropNamePath, out object? LastObject, out string PropName);
        PropertyInfo prop = LastObject?.GetType().GetProperty(PropName)
            ?? throw new EBold(SysUtils.Format("Property %s does not exist", PropNamePath)); // do not localize
        return Convert.ToString(prop.GetValue(LastObject), CultureInfo.CurrentCulture) ?? "";
    }

    /// <summary>
    /// Sets the property at the path from StartInstance to the element's value; without an element, to 0 or the nil
    /// representation. A string list property gets the list's elements (or the value's lines); another object property
    /// gets the owner's component with the value as name. A path through nothing, or a misspelt name, does nothing.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Through reflection. An object property is found by its own name (Delphi looked up the whole path, which failed for paths); a .NET list property is filled like a string list.")]
    private void SetRelativePropValue(object? StartInstance, string PropNamePath, TBoldElement? Value)
    {
        // No property specified
        if (PropNamePath == "")
        {
            return;
        }

        ConvertRelativeProp(StartInstance, PropNamePath, out object? LastObject, out string PropName);
        // Property path followed unassigned links
        if (LastObject is null)
        {
            return;
        }

        PropertyInfo? PropInfo = LastObject.GetType().GetProperty(PropName);
        // Property name misspelled
        if (PropInfo is null)
        {
            return;
        }

        PropKind TypeKind = KindOf(PropInfo.PropertyType);
        object? VarValue;
        if (Value is not null)
        {
            VarValue = Value.GetAsVariant();
        }
        else
        {
            // Handle nil equivalents for various property types
            VarValue = TypeKind switch
            {
                PropKind.Enumeration or PropKind.Integer => 0,
                _ => PropertiesController.BoldProperties.NilStringRepresentation,
            };
        }

        // Special case for booleans that don't seem to be handled properly by SetPropValue
        if (VarValue is bool b)
        {
            VarValue = b ? "True" : "False"; // do not localize
        }

        if (TypeKind == PropKind.Class)
        {
            // Special case for objects
            object? PropertyObj = PropInfo.GetValue(LastObject);
            TStrings? Strings = PropertyObj switch
            {
                TStrings strings => strings,
                System.Collections.IList list when !PropInfo.PropertyType.IsArray => new TListStrings(list),
                _ => null,
            };
            if (Strings is not null)
            {
                Strings.Clear();
                if (Value is TBoldList List)
                {
                    for (int i = 0; i < List.Count; i++)
                    {
                        Strings.Add(List[i]!.GetStringRepresentation(PropertiesController.BoldProperties.Representation));
                    }
                }
                else
                {
                    Strings.Text = Convert.ToString(VarValue, CultureInfo.CurrentCulture) ?? "";
                }
            }
            else
            {
                PropInfo.SetValue(LastObject, PropertiesController.FindComponent(Convert.ToString(VarValue, CultureInfo.CurrentCulture) ?? ""));
            }
        }
        else if (TypeKind == PropKind.Integer)
        {
            // This is needed to handle an error in TypInfo when setting CARDINAL properties
            try
            {
                PropInfo.SetValue(LastObject, Convert.ChangeType(Convert.ToInt64(VarValue, CultureInfo.CurrentCulture), Nullable.GetUnderlyingType(PropInfo.PropertyType) ?? PropInfo.PropertyType, CultureInfo.CurrentCulture));
            }
            catch (Exception E)
            {
                throw new Exception(SysUtils.Format(sCannotSetIntegerProperty, PropNamePath, Convert.ToString(VarValue, CultureInfo.CurrentCulture), E.Message), E);
            }
        }
        else
        {
            try
            {
                PropInfo.SetValue(LastObject, ConvertTo(PropInfo.PropertyType, VarValue));
            }
            catch (Exception E)
            {
                throw new Exception(SysUtils.Format(sCannotSetProperty, PropNamePath, Convert.ToString(VarValue, CultureInfo.CurrentCulture), E.Message), E);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldDrivenProperty source)
        {
            VCLComponent = source.VCLComponent;
            PropertyName = source.PropertyName;
            ReadOnly = source.ReadOnly;
        }
        else
        {
            base.Assign(Source);
        }
    }

    /// <summary>"Component.Property (RO)", with placeholders for what is missing.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName()
    {
        string result = VCLComponent is not null ? TBoldPropertiesController.ComponentName(VCLComponent) : "<No Comp>"; // do not localize

        if (PropertyName.Trim() != "")
        {
            result = result + "." + PropertyName.Trim();
        }
        else
        {
            result += ".<No Prop>"; // do not localize
        }

        result += ReadOnly ? " (RO)" : " (RW)"; // do not localize
        return result;
    }

    [Delphi(PortStatus.Ported)] protected TBoldDrivenPropertyCollection DrivenProperties => fDrivenProperties;
    [Delphi(PortStatus.Ported)] public TBoldPropertiesController PropertiesController => GetPropertiesController();

    [Delphi(PortStatus.Ported, Note = "Any component; Delphi's is a TComponent.")]
    public IComponent? VCLComponent
    {
        get => FVCLComponent;
        set => SetVCLComponent(value);
    }

    [Delphi(PortStatus.Ported)]
    public string PropertyName
    {
        get => FPropertyName;
        set => SetPropertyName(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool ReadOnly
    {
        get => FReadOnly;
        set => SetReadOnly(value);
    }
}

/// <summary>
/// Drives properties of other components from an element handle: the string controller's expression is evaluated on
/// the handle's value and each driven property set to the result.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Derives from TBoldSubscribableComponent, the port's stand-in for TComponent (name, free notification).")]
public class TBoldPropertiesController : TBoldSubscribableComponent
{
    private readonly IContainer? fOwner;
    private TBoldElementHandleFollower? FHandleFollower;
    private TBoldStringFollowerController? FBoldProperties;
    private TBoldDrivenPropertyCollection? FDrivenProperties;

    /// <summary>Adds the controller to <paramref name="AOwner"/> when given; the owner's components are the ones object properties can name.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldPropertiesController(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fOwner = AOwner;
        FBoldProperties = new TBoldStringFollowerController(this);
        FHandleFollower = new TBoldElementHandleFollower(AOwner, FBoldProperties);

        FBoldProperties.AfterMakeUptoDate = SetPropertyValue;
        FBoldProperties.OnGetContextType = GetContextType;
        FDrivenProperties = new TBoldDrivenPropertyCollection(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPropertiesController.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destroying();
            SysUtils.FreeAndNil(ref FHandleFollower);
            FDrivenProperties?.Clear();
            FDrivenProperties = null;
            SysUtils.FreeAndNil(ref FBoldProperties);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected void SetBoldProperties(TBoldStringFollowerController Value) => FBoldProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    protected TBoldElementHandle? GetBoldHandle() => FHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    protected void SetBoldHandle(TBoldElementHandle? Value) => FHandleFollower!.BoldHandle = Value;

    /// <summary>Sets every driven property.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void SetPropertyValue(TBoldFollower Follower)
    {
        for (int I = 0; I < DrivenProperties.Count; I++)
        {
            DrivenProperties[I].SetPropertyValue(Follower);
        }
    }

    /// <summary>A driven component that is disposed is forgotten.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (FDrivenProperties is not null && !IsDestroying)
        {
            for (int I = 0; I < DrivenProperties.Count; I++)
            {
                TBoldDrivenProperty driven = DrivenProperties[I];
                if (AComponent == driven.VCLComponent && Operation == TOperation.opRemove)
                {
                    driven.VCLComponent = null;
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDrivenProperties(TBoldDrivenPropertyCollection Value) => FDrivenProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    /// <summary>The owner's component with the name (case-insensitive), or null; Delphi's Owner.FindComponent.</summary>
    internal IComponent? FindComponent(string Name)
    {
        if (fOwner is null || Name == "")
        {
            return null;
        }

        foreach (IComponent component in fOwner.Components)
        {
            if (SysUtils.SameText(ComponentName(component), Name))
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>The name of a component: a Bold component's or action's own name, else its site's.</summary>
    internal static string ComponentName(IComponent Component) => Component switch
    {
        TBoldSubscribableComponent bold => bold.Name,
        TBasicAction action => action.Name,
        _ => Component.Site?.Name ?? "",
    };

    [Delphi(PortStatus.Ported)] public TBoldElementHandleFollower HandleFollower => FHandleFollower!;

    [Delphi(PortStatus.Ported)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController BoldProperties
    {
        get => FBoldProperties!;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDrivenPropertyCollection DrivenProperties
    {
        get => FDrivenProperties!;
        set => SetDrivenProperties(value);
    }
}
