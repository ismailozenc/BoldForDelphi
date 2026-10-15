// Delphi: Source/ValueSpace/Interfaces/BoldValueInterfaces.pas
// Port map: CSharp/port/units/BoldValueInterfaces.md

namespace Bold;

/// <summary>A value in a value space: its contents type name, persistence state and copying from another value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldValue
{
    [Delphi(PortStatus.Ported)] string GetContentName();
    [Delphi(PortStatus.Ported)] void AssignContent(IBoldValue Source);
    [Delphi(PortStatus.Ported)] TBoldValuePersistenceState GetBoldPersistenceState();
    [Delphi(PortStatus.Ported)] void SetBoldPersistenceState(TBoldValuePersistenceState Value);

    [Delphi(PortStatus.Ported)]
    TBoldValuePersistenceState BoldPersistenceState
    {
        get => GetBoldPersistenceState();
        set => SetBoldPersistenceState(value);
    }

    [Delphi(PortStatus.Ported)] string ContentName => GetContentName();
}

/// <summary>A value that can be null.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldNullableValue : IBoldValue
{
    [Delphi(PortStatus.Ported)] void SetContentToNull();
    [Delphi(PortStatus.Ported)] bool GetContentIsNull();
    [Delphi(PortStatus.Ported)] bool IsNull => GetContentIsNull();
}

/// <summary>A string value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldStringContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsString(string NewValue);
    [Delphi(PortStatus.Ported)] string GetContentAsString();

    [Delphi(PortStatus.Ported)]
    string asString
    {
        get => GetContentAsString();
        set => SetContentAsString(value);
    }
}

/// <summary>A value with several string representations, selected by number.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldStringRepresentable : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] string GetStringRepresentation(int representation);
    [Delphi(PortStatus.Ported)] void SetStringRepresentation(int Representation, string NewValue);
    [Delphi(PortStatus.Ported)] string StringRepresentation(int representation) => GetStringRepresentation(representation);
}

/// <summary>An integer value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldIntegerContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsInteger(int NewValue);
    [Delphi(PortStatus.Ported)] int GetContentAsInteger();

    [Delphi(PortStatus.Ported)]
    int asInteger
    {
        get => GetContentAsInteger();
        set => SetContentAsInteger(value);
    }
}

/// <summary>A floating point value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldFloatContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsFloat(double NewValue);
    [Delphi(PortStatus.Ported)] double GetContentAsFloat();

    [Delphi(PortStatus.Ported)]
    double asFloat
    {
        get => GetContentAsFloat();
        set => SetContentAsFloat(value);
    }
}

/// <summary>A currency value (decimal where Delphi has Currency).</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldCurrencyContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsCurrency(decimal NewValue);
    [Delphi(PortStatus.Ported)] decimal GetContentAsCurrency();

    [Delphi(PortStatus.Ported)]
    decimal asCurrency
    {
        get => GetContentAsCurrency();
        set => SetContentAsCurrency(value);
    }
}

/// <summary>A boolean value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldBooleanContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsBoolean(bool NewValue);
    [Delphi(PortStatus.Ported)] bool GetContentAsBoolean();

    [Delphi(PortStatus.Ported)]
    bool asBoolean
    {
        get => GetContentAsBoolean();
        set => SetContentAsBoolean(value);
    }
}

/// <summary>A date value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldDateContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsDate(TDateTime NewValue);
    [Delphi(PortStatus.Ported)] TDateTime GetContentAsDate();

    [Delphi(PortStatus.Ported)]
    TDateTime asDate
    {
        get => GetContentAsDate();
        set => SetContentAsDate(value);
    }
}

/// <summary>A time value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldTimeContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsTime(TDateTime NewValue);
    [Delphi(PortStatus.Ported)] TDateTime GetContentAsTime();

    [Delphi(PortStatus.Ported)]
    TDateTime asTime
    {
        get => GetContentAsTime();
        set => SetContentAsTime(value);
    }
}

/// <summary>A date and time value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldDateTimeContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsDateTime(TDateTime NewValue);
    [Delphi(PortStatus.Ported)] TDateTime GetContentAsDateTime();

    [Delphi(PortStatus.Ported)]
    TDateTime asDateTime
    {
        get => GetContentAsDateTime();
        set => SetContentAsDateTime(value);
    }
}

/// <summary>A blob: a string whose characters are bytes.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldBlobContent : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentAsBlob(string NewValue);
    [Delphi(PortStatus.Ported)] string GetContentAsBlob();

    [Delphi(PortStatus.Ported)]
    string asBlob
    {
        get => GetContentAsBlob();
        set => SetContentAsBlob(value);
    }
}

/// <summary>A blob with a content type; the type is this content, the data is IBoldBlobContent.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldTypedBlob : IBoldNullableValue
{
    [Delphi(PortStatus.Ported)] void SetContentTypeContent(string NewValue);
    [Delphi(PortStatus.Ported)] string GetContentTypeContent();

    [Delphi(PortStatus.Ported)]
    string ContentTypeContent
    {
        get => GetContentTypeContent();
        set => SetContentTypeContent(value);
    }
}

/// <summary>A single object reference by id, with the position of this end in an ordered link.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldObjectIdRef : IBoldValue
{
    [Delphi(PortStatus.Ported)] void SetFromId(TBoldObjectId? Id);
    [Delphi(PortStatus.Ported)] TBoldObjectId? GetId();
    [Delphi(PortStatus.Ported)] int GetOrderNo();
    [Delphi(PortStatus.Ported)] void SetOrderNo(int NewOrder);
    [Delphi(PortStatus.Ported)] TBoldObjectId? Id => GetId();

    [Delphi(PortStatus.Ported)]
    int OrderNo
    {
        get => GetOrderNo();
        set => SetOrderNo(value);
    }
}

/// <summary>A list of object references by id.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldObjectIdListRef : IBoldValue
{
    [Delphi(PortStatus.Ported)] void SetFromIdList(TBoldObjectIdList? IdLIst);
    [Delphi(PortStatus.Ported)] TBoldObjectId GetIdList(int Index);
    [Delphi(PortStatus.Ported)] TBoldObjectId IdList(int Index) => GetIdList(Index);
    [Delphi(PortStatus.Ported)] int GetCount();
    [Delphi(PortStatus.Ported)] int Count => GetCount();
}

/// <summary>Two parallel id lists: the objects and the link objects of a multi end with a link class.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldObjectIdListRefPair : IBoldValue
{
    [Delphi(PortStatus.Ported)] TBoldObjectId GetIdList1(int Index);
    [Delphi(PortStatus.Ported)] TBoldObjectId GetIdList2(int Index);
    [Delphi(PortStatus.Ported)] void SetFromIdLists(TBoldObjectIdList? IdList1, TBoldObjectIdList? IdList2);
    [Delphi(PortStatus.Ported)] int GetCount();
    [Delphi(PortStatus.Ported)] int Count => GetCount();
    [Delphi(PortStatus.Ported)] TBoldObjectId IdList1(int Index) => GetIdList1(Index);
    [Delphi(PortStatus.Ported)] TBoldObjectId IdList2(int Index) => GetIdList2(Index);
}

/// <summary>A single reference with its link object: two ids.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldObjectIdRefPair : IBoldValue
{
    [Delphi(PortStatus.Ported)] void SetFromIds(TBoldObjectId? Id1, TBoldObjectId? Id2);
    [Delphi(PortStatus.Ported)] TBoldObjectId? GetId1();
    [Delphi(PortStatus.Ported)] TBoldObjectId? Id1 => GetId1();
    [Delphi(PortStatus.Ported)] TBoldObjectId? GetId2();
    [Delphi(PortStatus.Ported)] TBoldObjectId? Id2 => GetId2();
    [Delphi(PortStatus.Ported)] int GetOrderNo();
    [Delphi(PortStatus.Ported)] void SetOrderNo(int NewOrder);

    [Delphi(PortStatus.Ported)]
    int OrderNo
    {
        get => GetOrderNo();
        set => SetOrderNo(value);
    }
}
