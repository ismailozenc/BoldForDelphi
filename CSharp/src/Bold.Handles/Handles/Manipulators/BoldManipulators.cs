// Delphi: Source/Handles/Manipulators/BoldManipulators.pas
// Port map: CSharp/port/units/BoldManipulators.md

using System.ComponentModel;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldManipulators", Symbol = "TBoldManipulator.Destroy", Note = "Freed the mappers; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldManipulators", Symbol = "TBoldManipulatorMapperCollection.GetOwner", Note = "Gave the owning component for the object inspector's name path; Owner gives it.")]

namespace Bold;

/// <summary>How an id string names an element: with class and member names (verbose) or with numbers only (terse).</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldIdStringRepresentation { isrVerbose, isrTerse }

/// <summary>Turns a string into another (to encrypt or decrypt id strings).</summary>
[Delphi(PortStatus.Ported)]
public delegate string TBoldStringStringFunction(string s);

/// <summary>Gives an element as a string for a named mapping.</summary>
[Delphi(PortStatus.Ported)]
public delegate string TBoldManipulatorGetter(TBoldElement? Element);

/// <summary>Sets an element from a string for a named mapping.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldManipulatorSetter(TBoldElement? Element, string NewValue);

/// <summary>
/// Reads and writes the elements of a system through id strings, as a web page does: an id string names an object or
/// one of its members, optionally with a mapping that converts the value, and can be encrypted.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldManipulator : TBoldSubscribableComponentViaBoldElem
{
    private TBoldIdStringRepresentation fIdStringRepresentation;
    private TBoldAbstractSystemHandle? fBoldSystemHandle;
    private TBoldStringStringFunction? fOnEncrypt;
    private TBoldStringStringFunction? fOnDecrypt;
    private TBoldManipulatorMapperCollection FMappers;
    private TBoldSystem? fBoldSystem;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldManipulator(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        FMappers = new TBoldManipulatorMapperCollection(this);
    }

    [Delphi(PortStatus.Ported)]
    private string RawIdStringForElement(TBoldElement? Element)
    {
        string Result = "";
        if (Element is TBoldObject boldObject)
        {
            if (IdStringRepresentation == TBoldIdStringRepresentation.isrVerbose)
            {
                Result = boldObject.BoldClassTypeInfo.ExpressionName + ".";
            }

            if (!boldObject.BoldObjectLocator!.BoldObjectID.IsStorable)
            {
                Result += "i";
            }

            Result += boldObject.BoldObjectLocator.BoldObjectID.AsString;
        }
        else if (Element is TBoldMember member && member.OwningObject is not null)
        {
            Result = RawIdStringForElement(member.OwningObject) + ":";
            Result += IdStringRepresentation == TBoldIdStringRepresentation.isrVerbose
                ? member.BoldMemberRTInfo!.ExpressionName
                : SysUtils.IntToStr(member.BoldMemberRTInfo!.index);
        }

        return Result;
    }

    /// <summary>The object (or member of an object) that an unencrypted id string without mapping names.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElement? ElementForRawIdString(string IdString)
    {
        if (IdStringRepresentation == TBoldIdStringRepresentation.isrVerbose)
        {
            IdString = SysUtils.Copy(IdString, SysUtils.Pos(".", IdString) + 1, SysUtils.MaxInt);
        }

        int ColonPos = SysUtils.Pos(":", IdString);
        string ObjectIdStr = ColonPos == 0 ? IdString : SysUtils.Copy(IdString, 1, ColonPos - 1);
        TBoldObjectId ObjectId;
        if (ObjectIdStr[0] == 'i')
        {
            ObjectId = new TBoldInternalObjectId(SysUtils.StrToInt(SysUtils.Copy(ObjectIdStr, 2, SysUtils.MaxInt)), 0, false);
        }
        else
        {
            var defaultId = new TBoldDefaultID(0, false);
            defaultId.AsInteger = SysUtils.StrToInt(ObjectIdStr);
            ObjectId = defaultId;
        }

        TBoldObject BoldObject = AttachedSystem!.EnsuredLocatorByID(ObjectId).EnsuredBoldObject!; // FIXME error handling if object has been deleted
        if (ColonPos == 0)
        {
            return BoldObject;
        }

        return IdStringRepresentation == TBoldIdStringRepresentation.isrVerbose
            ? BoldObject.BoldMemberByExpressionName(SysUtils.Copy(IdString, ColonPos + 1, SysUtils.MaxInt))
            : BoldObject.BoldMembers(SysUtils.StrToInt(SysUtils.Copy(IdString, ColonPos + 1, SysUtils.MaxInt)));
    }

    [Delphi(PortStatus.Ported)]
    private string AddMapping(string RawIdString, string Mapping) =>
        Mapping != "" ? SysUtils.Format("%s[%s]", RawIdString, Mapping) : RawIdString; // do not localize

    /// <summary>The mapping name in brackets at the end of the id string, or empty.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives empty for an id string without a mapping, where Delphi left the result undefined.")]
    private string GetMapping(string IdString)
    {
        if (IdString.Length > 0 && IdString[^1] == ']')
        {
            string Result = SysUtils.Copy(IdString, SysUtils.Pos("[", IdString) + 1, SysUtils.MaxInt);
            return SysUtils.Copy(Result, 1, Result.Length - 1);
        }

        return "";
    }

    [Delphi(PortStatus.Ported)]
    private string StripMapping(string IdString) =>
        IdString != "" && IdString[^1] == ']' ? SysUtils.Copy(IdString, 1, SysUtils.Pos("[", IdString) - 1) : IdString;

    [Delphi(PortStatus.Ported)]
    private void SetMappers(TBoldManipulatorMapperCollection Value) => FMappers.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystemHandle(TBoldAbstractSystemHandle? Value)
    {
        fBoldSystemHandle = Value;
        fBoldSystemHandle?.FreeNotification(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystem? GetAttachedSystem() => BoldSystemHandle is not null ? BoldSystemHandle.System : fBoldSystem;

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystem(TBoldSystem? Value) => fBoldSystem = Value; // TODO: Get OnDestroy notification for system

    [Delphi(PortStatus.Ported)] protected TBoldSystem? AttachedSystem => GetAttachedSystem();

    /// <summary>Forgets the system handle when it goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (AComponent == BoldSystemHandle && Operation == TOperation.opRemove)
        {
            fBoldSystemHandle = null;
        }
    }

    /// <summary>The element's value as a string (through the mapping when given) and its id string.</summary>
    [Delphi(PortStatus.Ported)]
    public string GetValueAndId(TBoldElement? Element, ref string IdString, string Mapping = "")
    {
        string Result;
        IdString = RawIdStringForElement(Element);
        if (Mapping == "")
        {
            Result = Element is not null ? Element.AsString : "";
        }
        else
        {
            IdString = AddMapping(IdString, Mapping);
            Result = Mappers.ItemByname(Mapping).GetAsString(Element); // FIXME error handling
        }

        if (fOnEncrypt is not null && IdString != "")
        {
            IdString = OnEncrypt!(IdString);
        }

        return Result;
    }

    /// <summary>The member's expression name, else the element's type's expression name.</summary>
    [Delphi(PortStatus.Ported)]
    public string DefaultTagForElement(TBoldElement Element)
    {
        if (Element is TBoldMember { BoldMemberRTInfo: { } memberRTInfo })
        {
            return memberRTInfo.ExpressionName;
        }

        if (Element.BoldType is { } boldType)
        {
            return boldType.ExpressionName;
        }

        throw new EBold(SysUtils.Format(sElementLacksTypeInfo, GetType().Name));
    }

    /// <summary>Sets the element of the id string from the string: an attribute's value, a reference's object (by its id string), or through the mapping.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested SetObjectReference is a private helper.")]
    public void SetValue(string IdString, string NewValue)
    {
        if (fOnDecrypt is not null && IdString != "")
        {
            IdString = OnDecrypt!(IdString);
        }

        TBoldElement? Element = ElementForRawIdString(StripMapping(IdString));
        string Mapping = GetMapping(IdString);
        if (Mapping == "")
        {
            switch (Element)
            {
                case TBoldAttribute:
                    Element.AsString = NewValue;
                    break;
                case TBoldObjectReference reference:
                    SetObjectReference(reference, NewValue);
                    break;
                case TBoldObjectList:
                    throw new EBold(sCannotSetMultiLinkFromValue);
                case TBoldObject:
                    throw new EBold(sCannotSetObjectFromValue);
            }
        }
        else
        {
            Mappers.ItemByname(Mapping).SetFromString(Element, NewValue); // FIXME error handling
        }
    }

    private void SetObjectReference(TBoldObjectReference r, string NewId)
    {
        if (fOnDecrypt is not null && NewId != "")
        {
            NewId = OnDecrypt!(NewId);
        }

        if (ElementForRawIdString(StripMapping(NewId)) is TBoldObject NewElement)
        {
            r.BoldObject = NewElement; // FIXME error handling
        }
    }

    /// <summary>The element of an id string (decrypted, without mapping).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElement? ElementForIdString(string IdString)
    {
        if (fOnDecrypt is not null && IdString != "")
        {
            IdString = OnDecrypt!(IdString);
        }

        IdString = StripMapping(IdString);
        return ElementForRawIdString(IdString);
    }

    /// <summary>The id string of the element, with the mapping when given, encrypted when OnEncrypt is set.</summary>
    [Delphi(PortStatus.Ported)]
    public string IdStringForElement(TBoldElement? Element, string Mapping = "")
    {
        string Result = RawIdStringForElement(Element);
        if (Mapping != "")
        {
            Result = AddMapping(Result, Mapping);
        }

        if (fOnEncrypt is not null && Result != "")
        {
            Result = OnEncrypt!(Result);
        }

        return Result;
    }

    [Delphi(PortStatus.Ported)]
    public void DeleteObject(string IdString)
    {
        if (ElementForIdString(IdString) is TBoldObject boldObject)
        {
            boldObject.Delete();
        }
    }

    /// <summary>Creates an object of the class and gives its id string.</summary>
    [Delphi(PortStatus.Ported, Note = "The message names the manipulator's class, where Delphi's named the class to create (its parameter hid the class name).")]
    public string CreateObject(string Classname)
    {
        if (AttachedSystem is null)
        {
            throw new EBold(SysUtils.Format(sNoSystemHandle, GetType().Name, "CreateObject", Name)); // do not localize
        }

        return IdStringForElement(AttachedSystem.CreateNewObjectByExpressionName(Classname));
    }

    /// <summary>Sets the elements of "id=value" lines.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFromList(TStrings IdValuePairs)
    {
        for (int i = 0; i < IdValuePairs.Count; i++)
        {
            string CurrentRow = IdValuePairs[i];
            int EqualPos = SysUtils.Pos("=", CurrentRow);
            string Id = SysUtils.Copy(CurrentRow, 0, EqualPos - 1);
            string NewValue = SysUtils.Copy(CurrentRow, EqualPos + 1, SysUtils.MaxInt);
            SetValue(Id, NewValue);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldIdStringRepresentation IdStringRepresentation
    {
        get => fIdStringRepresentation;
        set => fIdStringRepresentation = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystem? BoldSystem
    {
        get => fBoldSystem;
        set => SetBoldSystem(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractSystemHandle? BoldSystemHandle
    {
        get => fBoldSystemHandle;
        set => SetBoldSystemHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringStringFunction? OnEncrypt
    {
        get => fOnEncrypt;
        set => fOnEncrypt = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringStringFunction? OnDecrypt
    {
        get => fOnDecrypt;
        set => fOnDecrypt = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldManipulatorMapperCollection Mappers
    {
        get => FMappers;
        set => SetMappers(value);
    }
}

/// <summary>A named mapping: OnGet gives an element as a string and OnSet sets it from one.</summary>
[Delphi(PortStatus.Ported, Note = "Can be assigned (name and events), so setting a manipulator's Mappers copies them; Delphi's mapper could not be assigned and that failed.")]
public class TBoldManipulatorMapper : TCollectionItem
{
    private TBoldManipulatorGetter? fOnGet;
    private TBoldManipulatorSetter? fOnset;
    private string fMappingName = "";

    /// <summary>Adds the mapper to the collection.</summary>
    public TBoldManipulatorMapper(TCollection? Collection)
        : base(Collection)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => MappingName;

    /// <summary>Copies the name and events of another mapper.</summary>
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldManipulatorMapper mapper)
        {
            MappingName = mapper.MappingName;
            OnGet = mapper.OnGet;
            OnSet = mapper.OnSet;
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Gives empty without OnGet, where Delphi left the result undefined.")]
    public string GetAsString(TBoldElement? Element) => fOnGet is not null ? OnGet!(Element) : "";

    [Delphi(PortStatus.Ported)]
    public void SetFromString(TBoldElement? Element, string NewValue) => fOnset?.Invoke(Element, NewValue);

    [Delphi(PortStatus.Ported)]
    public string MappingName
    {
        get => fMappingName;
        set => fMappingName = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldManipulatorGetter? OnGet
    {
        get => fOnGet;
        set => fOnGet = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldManipulatorSetter? OnSet
    {
        get => fOnset;
        set => fOnset = value;
    }
}

/// <summary>The mappers of a manipulator.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldManipulatorMapperCollection : TCollection
{
    private readonly Component fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldManipulatorMapperCollection(Component Owner)
        : base(typeof(TBoldManipulatorMapper))
    {
        fOwner = Owner;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldManipulatorMapper GetItems(int Index) => (TBoldManipulatorMapper)base[Index];

    /// <summary>The mapper with the name; raises when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldManipulatorMapper GetItemByname(string Name)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i].MappingName == Name)
            {
                return this[i];
            }
        }

        throw new EBold(SysUtils.Format(sUnknownMapping, Name));
    }

    /// <summary>The manipulator the mappers belong to.</summary>
    public Component Owner => fOwner;

    [Delphi(PortStatus.Ported, Symbol = "TBoldManipulatorMapperCollection.Items", Note = "The default indexed property in Delphi.")]
    public new TBoldManipulatorMapper this[int Index] => GetItems(Index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldManipulatorMapper ItemByname(string Name) => GetItemByname(Name);
}
