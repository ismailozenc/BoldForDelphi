// Delphi: Source/BoldAwareGUI/BoldControls/BoldStringsPropertyController.pas
// Port map: CSharp/port/units/BoldStringsPropertyController.md

using System.ComponentModel;
using System.Reflection;
using static Bold.BoldGuiResourceStrings;

namespace Bold;

/// <summary>Something happened to a follower of the control.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldControlFollowerEvent(object? Sender, TBoldFollower Follower);

/// <summary>Something happened at the index of the control's list follower.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldControlSubFollowerEvent(object? Sender, int Index, TBoldFollower OwningFollower);

/// <summary>
/// Fills a string list property of another component (a list box's items, say) from a list handle: a string per
/// element, from the row controller, kept current as the list changes.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Derives from TBoldSubscribableComponent, the port's stand-in for TComponent (name, free notification).")]
public class TBoldStringsPropertyController : TBoldSubscribableComponent
{
    private TBoldListHandleFollower? fHandleFollower;
    private TBoldListAsFollowerListController? fBoldProperties;
    private TBoldStringFollowerController? fBoldRowProperties;
    private string fPropertyName = "";
    private IComponent? fVCLComponent;
    private TBoldControlFollowerEvent? fOnListAfterMakeUptoDate;
    private TBoldControlFollowerEvent? fOnListBeforeDeleteItem;
    private TBoldControlFollowerEvent? fOnItemBeforeMakeUpToDate;
    private TBoldControlFollowerEvent? fOnListBeforeMakeUptoDate;
    private TBoldControlFollowerEvent? fOnListAfterInsertItem;
    private TBoldControlSubFollowerEvent? fOnListBeforeInsertItem;
    private TBoldControlSubFollowerEvent? fOnListAfterDeleteItem;
    private TBoldControlFollowerEvent? fOnItemAfterMakeUpToDate;

    /// <summary>Adds the controller to <paramref name="AOwner"/> when given.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldStringsPropertyController(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fBoldRowProperties = new TBoldStringFollowerController(this);
        fBoldRowProperties.AfterMakeUptoDate = _ItemAfterMakeUptoDate;
        fBoldRowProperties.BeforeMakeUptoDate = _ItemBeforeMakeUptoDate;
        fBoldRowProperties.OnGetContextType = GetContextType;
        fBoldProperties = new TBoldListAsFollowerListController(this, fBoldRowProperties);
        fBoldProperties.OnAfterInsertItem = _ListAfterInsertItem;
        fBoldProperties.OnAfterDeleteItem = _ListAfterDeleteItem;
        fBoldProperties.OnBeforeInsertItem = _ListBeforeInsertItem;
        fBoldProperties.OnBeforeDeleteItem = _ListBeforeDeleteItem;
        fBoldProperties.BeforeMakeUptoDate = _ListBeforeMakeUptoDate;
        fBoldProperties.AfterMakeUptoDate = _ListAfterMakeUptoDate;
        fHandleFollower = new TBoldListHandleFollower(AOwner, fBoldProperties);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldStringsPropertyController.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destroying();
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref fBoldProperties);
            SysUtils.FreeAndNil(ref fBoldRowProperties);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldListAsFollowerListController Value) => fBoldProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? Value) => fHandleFollower!.BoldHandle = Value;

    /// <summary>The component that is disposed is forgotten.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (VCLComponent is not null && !IsDestroying && AComponent == VCLComponent && Operation == TOperation.opRemove)
        {
            VCLComponent = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _ListAfterMakeUptoDate(TBoldFollower Follower)
    {
        StringsProperty?.EndUpdate();
        fOnListAfterMakeUptoDate?.Invoke(this, Follower);
    }

    /// <summary>Starts updating the strings; strings that do not match the rows (changed by other means) raise, unless there are no rows.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ListBeforeMakeUptoDate(TBoldFollower Follower)
    {
        TStrings? Strings = StringsProperty;
        if (Strings is not null)
        {
            Strings.BeginUpdate();
            if (Follower.SubFollowerCount != Strings.Count)
            {
                if (Follower.SubFollowerCount == 0)
                {
                    Strings.Clear();
                }
                else
                {
                    throw new EBold(SysUtils.Format(sStringsControlledByOtherMeans, Name));
                }
            }
        }

        fOnListBeforeMakeUptoDate?.Invoke(this, Follower);
    }

    [Delphi(PortStatus.Ported)]
    private void _ListAfterDeleteItem(int index, TBoldFollower OwningFollower)
    {
        StringsProperty?.Delete(index);
        fOnListAfterDeleteItem?.Invoke(this, index, OwningFollower);
    }

    [Delphi(PortStatus.Ported)]
    private void _ListAfterInsertItem(TBoldFollower Follower)
    {
        StringsProperty?.InsertObject(Follower.index, "", Follower);
        fOnListAfterInsertItem?.Invoke(this, Follower);
    }

    /// <summary>Puts the row's string (and the follower) in the strings.</summary>
    [Delphi(PortStatus.Ported, Note = "Sets the object only for an index in the list, as the indentation says Delphi meant; Delphi set it for any index.")]
    private void _ItemAfterMakeUptoDate(TBoldFollower Follower)
    {
        TStrings? Strings = StringsProperty;
        if (Strings is not null)
        {
            int index = Follower.index;
            if (index > -1 && index < Strings.Count)
            {
                Strings[index] = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
                Strings.SetObjects(index, Follower);
            }
        }

        fOnItemAfterMakeUpToDate?.Invoke(this, Follower);
    }

    [Delphi(PortStatus.Ported)]
    private void SetRowProperties(TBoldStringFollowerController Value) => fBoldRowProperties!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    /// <summary>The string list property of the component, or null.</summary>
    [Delphi(PortStatus.Ported, Note = "Through reflection; a .NET list property (a list box's Items) is used through TListStrings.")]
    private TStrings? GetStringsProperty()
    {
        // fixme code for properties not at top level
        if (VCLComponent is not null && PropertyName != "")
        {
            PropertyInfo? prop = VCLComponent.GetType().GetProperty(PropertyName);
            return prop?.GetValue(VCLComponent) switch
            {
                TStrings strings => strings,
                System.Collections.IList list => new TListStrings(list),
                _ => null,
            };
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    private void _ListBeforeDeleteItem(TBoldFollower Follower) => fOnListBeforeDeleteItem?.Invoke(this, Follower);

    [Delphi(PortStatus.Ported)]
    private void _ListBeforeInsertItem(int index, TBoldFollower OwningFollower) => fOnListBeforeInsertItem?.Invoke(this, index, OwningFollower);

    [Delphi(PortStatus.Ported)]
    private void _ItemBeforeMakeUptoDate(TBoldFollower Follower) => fOnItemBeforeMakeUpToDate?.Invoke(this, Follower);

    /// <summary>Sets the component (told when it is disposed); clearing it clears the property name.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetVCLComponent(IComponent? Value)
    {
        //FIXME: Add code to validate PropertyName in new component
        fVCLComponent = Value;
        if (fVCLComponent is not null)
        {
            FreeNotification(fVCLComponent);
        }

        if (fVCLComponent is null)
        {
            PropertyName = "";
        }
    }

    [Delphi(PortStatus.Ported)] protected TStrings? StringsProperty => GetStringsProperty();
    [Delphi(PortStatus.Ported)] public TBoldListHandleFollower HandleFollower => fHandleFollower!;

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldListAsFollowerListController BoldProperties
    {
        get => fBoldProperties!;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController BoldRowProperties
    {
        get => fBoldRowProperties!;
        set => SetRowProperties(value);
    }

    [Delphi(PortStatus.Ported, Note = "Any component; Delphi's is a TComponent.")]
    public IComponent? VCLComponent
    {
        get => fVCLComponent;
        set => SetVCLComponent(value);
    }

    [Delphi(PortStatus.Ported)]
    public string PropertyName
    {
        get => fPropertyName;
        set => fPropertyName = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnListBeforeMakeUptoDate
    {
        get => fOnListBeforeMakeUptoDate;
        set => fOnListBeforeMakeUptoDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnListAfterMakeUptoDate
    {
        get => fOnListAfterMakeUptoDate;
        set => fOnListAfterMakeUptoDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnListAfterInsertItem
    {
        get => fOnListAfterInsertItem;
        set => fOnListAfterInsertItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlSubFollowerEvent? OnListAfterDeleteItem
    {
        get => fOnListAfterDeleteItem;
        set => fOnListAfterDeleteItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlSubFollowerEvent? OnListBeforeInsertItem
    {
        get => fOnListBeforeInsertItem;
        set => fOnListBeforeInsertItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnListBeforeDeleteItem
    {
        get => fOnListBeforeDeleteItem;
        set => fOnListBeforeDeleteItem = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnItemBeforeMakeUpToDate
    {
        get => fOnItemBeforeMakeUpToDate;
        set => fOnItemBeforeMakeUpToDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldControlFollowerEvent? OnItemAfterMakeUpToDate
    {
        get => fOnItemAfterMakeUpToDate;
        set => fOnItemAfterMakeUpToDate = value;
    }
}
