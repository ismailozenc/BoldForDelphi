// Delphi: Source/BoldAwareGUI/BoldControls/BoldDataSet.pas
// Port map: CSharp/port/units/BoldDataSet.md
// A data set over a Bold list handle: a record per element of the list, a field per field description (an OCL
// expression on the element). Delphi's TBoldDataSet is a VCL TDataSet with record buffers and bookmarks; this one is
// built on Bold.Rtl's TDataSet, which keeps the records itself, so the buffer and cursor methods have no counterpart.

using System.ComponentModel;
using System.Globalization;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBDSBookmarkInfo", Note = "The bookmark of a record buffer; a bookmark is the record's element.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "PBDSBookmarkInfo", Note = "See TBDSBookmarkInfo.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldDataSetFieldDescription.GetFieldOfs", Note = "The field's offset in a record buffer; records are arrays of values, FieldOfs is the field's index.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldDataSetFieldDescriptions.GetOwner", Note = "Gave the data set as the owner in the name path; the data set is no TPersistent. Dataset gives it.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetBookmarkInfo", Note = "The bookmark part of a record buffer; there are no record buffers.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.ClearCalcFields", Note = "Cleared the calculated fields of a record buffer; there are no calculated fields.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetActiveRecBuf", Note = "The record buffer of the state; ActiveBoldElement gives the element.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.AllocRecordBuffer", Note = "Bold.Rtl's TDataSet keeps the records.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.FreeRecordBuffer", Note = "Bold.Rtl's TDataSet keeps the records.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalInitRecord", Note = "Cleared a record buffer; a new record starts empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetRecord", Note = "Filled a buffer from an element while scrolling; the records are loaded from the elements (InternalOpen, Resync).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.SetRecNo", Note = "Empty in Delphi; Bold.Rtl's RecNo is read-only.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetRecordSize", Note = "The size of a record buffer; there are no record buffers.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetBookmarkData", Note = "A bookmark is the record's element (ActiveBoldElement).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetBookmarkFlag", Note = "The buffer's bookmark flag; Bold.Rtl's TDataSet keeps Bof and Eof.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.SetBookmarkFlag", Note = "See GetBookmarkFlag.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.SetBookmarkData", Note = "See GetBookmarkData.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalGotoBookmark", Note = "Locate with the key SELF goes to an element.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalSetToRecord", Note = "See InternalGotoBookmark.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalFirst", Note = "Bold.Rtl's First positions on the records.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalLast", Note = "Bold.Rtl's Last positions on the records.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.InternalAddRecord", Note = "Added an element for Delphi's AppendRecord; a record is appended with Append (DoOnNewRecord adds the element).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.IsCursorOpen", Note = "Bold.Rtl's TDataSet is open when Active; InternalOpen loads nothing without an enabled handle.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetRecordCount", Note = "RecordCount counts the records, one per element of the list.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDataSet", Symbol = "TBoldAbstractDataSet.GetRecNo", Note = "RecNo is the current record's number, the element's position in the list.")]

namespace Bold;

/// <summary>A field of a Bold data set: an OCL expression on the element, whose type gives the field's type and size.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDataSetFieldDescription : TCollectionItem, IDisposable
{
    private TBoldMemberRTInfo? fRTInfo;
    private TBoldStringFollowerController fBoldProperties;
    private int fBufferSize;
    private string fFieldName = "";
    private int fFieldSize;
    private TFieldType fFieldType;
    private bool fRequired;
    private bool fUseFieldSize;
    private bool fInvalid;

    /// <summary>A field of the descriptions; its follower joins the field followers.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldDataSetFieldDescription(TCollection AOwner)
        : base(AOwner is TBoldDataSetFieldDescriptions ? AOwner : throw new EBoldInternal("TBoldDataSetFieldDescription: the collection is no TBoldDataSetFieldDescriptions"))
    {
        fBoldProperties = new TBoldStringFollowerController(null)
        {
            AfterMakeUptoDate = AfterMakeUptoDate,
            OnGetContextType = GetListElementType,
        };
        FieldDescriptions.FieldFollowers.Add(fBoldProperties);
        Invalidate();
    }

    /// <summary>The field leaves its data set's descriptions.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldDataSetFieldDescription.Destroy", Note = "The descriptions drop the field's follower when it leaves them.")]
    public void Dispose()
    {
        Collection = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>The field's type, size and buffer size for the attribute class (a string of 255 when none matches).</summary>
    [Delphi(PortStatus.Ported, Note = "The attribute class is a Type.")]
    private void DefineFieldType(Type aClass)
    {
        //if we didn't find the bold type, we will set the field type to string(255)
        fFieldType = TFieldType.ftString;
        fFieldSize = 255;
        fBufferSize = 255;
        fRequired = false;
        for (int i = BoldDataSet.arConvInfo.Length - 1; i >= 0; i--)
        {
            TBoldConvInfo info = BoldDataSet.arConvInfo[i];
            if (info.FBoldType.IsAssignableFrom(aClass))
            {
                fFieldType = info.FFieldType;
                fBufferSize = info.FBufferSize;
                fUseFieldSize = info.FUseSize;
                fFieldSize = info.FUseSize ? info.FBufferSize : 0;
                fRequired = false;
                break;
            }
        }
    }

    /// <summary>
    /// A value of the field changed: an open data set loads its records again, or, while the field's element is
    /// edited, the field's value in the record being edited changes.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        TBoldAbstractDataSet dataset = Dataset;
        if (!dataset.Active)
        {
            return;
        }

        if (dataset.State is not (TDataSetState.dsEdit or TDataSetState.dsInsert))
        {
            dataset.ResyncRecords();
        }
        else if (dataset.ActiveBoldElement is { } element && element == Follower.OwningFollower?.Element)
        {
            dataset.BoldElementToBuffer(Follower.index, element);
        }
    }

    /// <summary>The field's name, else its expression with . ( - &gt; ) as _.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetEffectiveFieldName()
    {
        if (FieldName != "")
        {
            return FieldName;
        }

        char[] result = BoldProperties.Expression.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] is '.' or '(' or '-' or '>' or ')')
            {
                result[i] = '_';
            }
        }

        return new string(result);
    }

    /// <summary>The field's type, size and member from its expression's type (once after each change).</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureValid()
    {
        if (fInvalid)
        {
            TBoldElementTypeInfo ListElementType = this.ListelementType ?? throw new EBold(SysUtils.Format(sCannotInitializeFieldsWithoutContext, Dataset.Name));
            TBoldElementTypeInfo LFieldType = ListElementType.Evaluator!.ExpressionType(Expression, ListElementType, false)
                ?? throw new EBold(SysUtils.Format(sInvalidFieldExpression, Expression));
            fRTInfo = ListElementType.Evaluator is TBoldRTEvaluator evaluator ? evaluator.RTInfo(Expression, ListElementType, false) : null;

            if (LFieldType is TBoldAttributeTypeInfo attributeType)
            {
                DefineFieldType(attributeType.AttributeClass!);
                if (fRTInfo is TBoldAttributeRTInfo attribute && attribute.Length > 0 && fUseFieldSize)
                {
                    fBufferSize = attribute.Length + 1;
                    fFieldSize = fBufferSize - 1;
                }
            }
            else if (LFieldType is TBoldClassTypeInfo)
            {
                fFieldType = TFieldType.ftInteger;
                fFieldSize = 0;
                fBufferSize = sizeof(int);
                fRequired = false;
            }
            else
            {
                fFieldType = TFieldType.ftString;
                fFieldSize = 40;
                fBufferSize = 40;
                fRequired = false;
            }

            fInvalid = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldDataSetFieldDescriptions GetFieldDescriptions() => (TBoldDataSetFieldDescriptions)Collection!;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractDataSet GetDataSet() => FieldDescriptions.Dataset;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetListElementType() => Dataset.ListElementType;

    [Delphi(PortStatus.Ported)]
    private string GetExpression() => fBoldProperties.Expression;

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string AValue)
    {
        fBoldProperties.Expression = AValue;
        Invalidate();
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldProperties(TBoldStringFollowerController AValue)
    {
        if (AValue != fBoldProperties)
        {
            fBoldProperties.Assign(AValue);
        }

        Invalidate();
    }

    /// <summary>Copies the follower settings and the field name of another field.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldDataSetFieldDescription source)
        {
            BoldProperties.Assign(source.BoldProperties);
            FieldName = source.FieldName;
            Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void Invalidate() => fInvalid = true;

    [Delphi(PortStatus.Ported)]
    private int GetFieldSize()
    {
        EnsureValid();
        return fFieldSize;
    }

    [Delphi(PortStatus.Ported)]
    private TFieldType GetFieldType()
    {
        EnsureValid();
        return fFieldType;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetRequired()
    {
        EnsureValid();
        return fRequired;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfo? GetRTInfo()
    {
        EnsureValid();
        return fRTInfo;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetUseFieldSize()
    {
        EnsureValid();
        return fUseFieldSize;
    }

    [Delphi(PortStatus.Ported)]
    private void SetFieldName(string Value)
    {
        fFieldName = Value;
        Invalidate();
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => EffectiveFieldName + " = " + Expression;

    [Delphi(PortStatus.Ported)] public int BufferSize => fBufferSize;
    [Delphi(PortStatus.Ported)] public string EffectiveFieldName => GetEffectiveFieldName();
    [Delphi(PortStatus.Ported)] public int FieldSize => GetFieldSize();
    [Delphi(PortStatus.Ported)] public TFieldType FieldType => GetFieldType();

    [Delphi(PortStatus.Ported, Note = "The field's index in a record (Delphi's offset in a record buffer).")]
    public int FieldOfs => Index;

    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ListelementType => GetListElementType();
    [Delphi(PortStatus.Ported)] public bool Required => GetRequired();
    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfo? RTInfo => GetRTInfo();
    [Delphi(PortStatus.Ported)] public bool UseFieldSize => GetUseFieldSize();
    [Delphi(PortStatus.Ported)] public TBoldAbstractDataSet Dataset => GetDataSet();
    [Delphi(PortStatus.Ported)] public TBoldDataSetFieldDescriptions FieldDescriptions => GetFieldDescriptions();

    [Delphi(PortStatus.Ported)]
    public string Expression
    {
        get => GetExpression();
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringFollowerController BoldProperties
    {
        get => fBoldProperties;
        set => SetBoldProperties(value);
    }

    [Delphi(PortStatus.Ported)]
    public string FieldName
    {
        get => fFieldName;
        set => SetFieldName(value);
    }

    /// <summary>The field leaves the descriptions: its follower goes.</summary>
    internal void Removed()
    {
        FieldDescriptions.FieldFollowers.Remove(fBoldProperties);
        fBoldProperties.Dispose();
    }
}

/// <summary>The fields of a Bold data set, with the followers of their expressions.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDataSetFieldDescriptions : TCollection, IDisposable
{
    private readonly TBoldAbstractDataSet FDataset;
    private TBoldControllerList? FFieldFollowers;

    [Delphi(PortStatus.Ported)]
    public TBoldDataSetFieldDescriptions(TBoldAbstractDataSet AOwner)
        : base(typeof(TBoldDataSetFieldDescription))
    {
        FFieldFollowers = new TBoldControllerList(null);
        FDataset = AOwner;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDataSetFieldDescriptions.Destroy")]
    public void Dispose()
    {
        Clear();
        SysUtils.FreeAndNil(ref FFieldFollowers);
        GC.SuppressFinalize(this);
    }

    /// <summary>A field that leaves drops its follower (Delphi's field destructor).</summary>
    protected override void Notify(TCollectionItem Item, TCollectionNotification Action)
    {
        base.Notify(Item, Action);
        if (Action == TCollectionNotification.cnExtracting && Item is TBoldDataSetFieldDescription field && FFieldFollowers is not null)
        {
            field.Removed();
        }
    }

    /// <summary>A new field at the end (the data set's layout is not changed: a field without a name cannot open).</summary>
    [Delphi(PortStatus.Ported)]
    public new TBoldDataSetFieldDescription Add() => (TBoldDataSetFieldDescription)base.Add();

    [Delphi(PortStatus.Ported)]
    private TBoldDataSetFieldDescription GetItem(int AIndex) => (TBoldDataSetFieldDescription)base[AIndex];

    [Delphi(PortStatus.Ported)]
    public void InvalidateFields()
    {
        for (int i = 0; i < Count; i++)
        {
            Items(i).Invalidate();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldControllerList FieldFollowers => FFieldFollowers!;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi (the default one, also this[]).")]
    public TBoldDataSetFieldDescription Items(int index) => GetItem(index);

    public new TBoldDataSetFieldDescription this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractDataSet Dataset => FDataset;
}

/// <summary>How the values of an attribute class go into a field (Delphi's conversion table).</summary>
[Delphi(PortStatus.Ported, Note = "The Bold type is a Type.")]
internal readonly record struct TBoldConvInfo(Type FBoldType, TFieldType FFieldType, int FBufferSize, bool FUseSize, bool FUseDecimals);

/// <summary>
/// A data set of the elements of a list handle: a record per element and a field per field description. The current
/// record follows the handle's current element; posting writes nothing more (each field is written to its element as
/// it is set), cancelling discards the object's changes, and the records load again when the list or a value changes.
/// </summary>
[Delphi(PortStatus.Ported, Note = "On Bold.Rtl's TDataSet (an in-memory data set) rather than a VCL TDataSet.")]
public abstract class TBoldAbstractDataSet : TDataSet, IDisposable, ISupportInitialize
{
    private bool FAutoOpen;
    private TBoldPassthroughSubscriber? FContextSubscriber;
    private TBoldDataSetFieldDescriptions? FFieldDescriptions;
    private bool FLayoutChanged;
    private TBoldListAsFollowerListController? FListFollowerController;
    private TBoldListHandleFollower? FListFollower;
    private TBoldElementTypeInfo? fLastKnownType;
    private bool fInternalCursorReady;
    private bool fLoading;
    private bool fDestroying;
    private readonly List<TBoldElement?> fElements = [];
    private TBoldElement? fInsertedElement;
    private TBoldElement? fPostedElement;
    private bool fInternalChange;

    /// <summary>A data set; Owner is what its followers match on (its form).</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is the object the followers match on (a form), as Delphi's TComponent owner.")]
    protected TBoldAbstractDataSet(object? AOwner = null)
    {
        fLastKnownType = null;
        fInternalCursorReady = false;
        FContextSubscriber = new TBoldPassthroughSubscriber(_ReceiveContext);
        FFieldDescriptions = new TBoldDataSetFieldDescriptions(this);
        FLayoutChanged = true;
        FListFollowerController = new TBoldListAsFollowerListController(this, FFieldDescriptions.FieldFollowers)
        {
            OnAfterInsertItem = _InsertRow,
            OnAfterDeleteItem = _DeleteRow,
            OnGetContextType = GetListElementType,
        };
        FListFollower = new TBoldListHandleFollower(AOwner, FListFollowerController);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractDataSet.Destroy")]
    public void Dispose()
    {
        fDestroying = true;
        Close();
        SysUtils.FreeAndNil(ref FContextSubscriber);
        SysUtils.FreeAndNil(ref FListFollower);
        SysUtils.FreeAndNil(ref FListFollowerController);
        SysUtils.FreeAndNil(ref FFieldDescriptions);
        GC.SuppressFinalize(this);
    }

    /// <summary>The data set's name (in messages).</summary>
    public string Name { get; set; } = "";

    /// <summary>The handle's static element type.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldElementTypeInfo? GetListElementType() => BoldHandle?.StaticBoldType;

    [Delphi(PortStatus.Ported)]
    private void SetAutoOpen(bool AValue)
    {
        if (FAutoOpen != AValue)
        {
            FAutoOpen = AValue;
            if (FAutoOpen)
            {
                TryToOpen();
            }
        }
    }

    /// <summary>Opens, unless loading, going or open; a failure to open is silent.</summary>
    [Delphi(PortStatus.Ported)]
    public void TryToOpen()
    {
        if (!fLoading && !fDestroying && !Active)
        {
            try
            {
                Open();
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // Eat exceptions quietly
            }
        }
    }

    /// <summary>A new record is a new element of the list.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoOnNewRecord()
    {
        InsertElement();
        base.DoOnNewRecord();
    }

    /// <summary>A new element of the list, when the list can make one.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldElement? CreateNewElement() =>
        BoldHandle?.List is { CanCreateNew: true } list ? list.AddNew() : null;

    /// <summary>The record being inserted is a new element (none: the insert is aborted), whose values it shows.</summary>
    [Delphi(PortStatus.Ported)]
    private void InsertElement()
    {
        TBoldElement LElement = CreateNewElement() ?? throw new EAbort();
        fInsertedElement = LElement;
        BoldToBuffer(LElement);
    }

    /// <summary>The member of the field of the name.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfo? GetBoldRTInfo(string AFieldName)
    {
        InternalInitFieldDefs();
        for (int i = 0; i < FieldDescriptions.Count; i++)
        {
            if (FieldDescriptions[i].EffectiveFieldName == AFieldName)
            {
                return FieldDescriptions[i].RTInfo;
            }
        }

        throw new Exception(SysUtils.Format(sInvalidFieldName, AFieldName));
    }

    /// <summary>The records follow a list that lost elements.</summary>
    [Delphi(PortStatus.Ported, Note = "Bold.Rtl keeps the position within the records; the records load again.")]
    private void EnsureRecordPosInValidRange()
    {
        if (List is null || RecNo > List.Count)
        {
            ResyncRecords();
        }
    }

    /// <summary>The handle went (close), its value changed (the type may have), or the list lost an element.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveContext(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case BoldSubscription.beDestroying:
                Close();
                break;
            case BoldSubscription.beValueIdentityChanged:
                TypeMayHaveChanged();
                EnsureRecordPosInValidRange();
                break;
            case BoldSubscription.beItemDeleted:
                EnsureRecordPosInValidRange();
                break;
        }
    }

    /// <summary>A stream on the blob of the field in the current record (an empty one when there is none).</summary>
    [Delphi(PortStatus.Ported)]
    public Stream CreateBlobStream(TField Field, TBoldBlobStreamMode Mode)
    {
        if (IsEmpty || ActiveBoldElement is not { } element)
        {
            return new MemoryStream();
        }

        TBoldDataSetFieldDescription description = FieldDescriptions[Field.FieldNo - 1];
        using var IndirectElement = new TBoldIndirectElement();
        element.EvaluateExpression(description.Expression, IndirectElement);
        return IndirectElement.Value is TBABlob { IsNull: false } BlobValue ? BlobValue.CreateBlobStream(Mode) : new MemoryStream();
    }

    [Delphi(PortStatus.Ported)]
    protected override void InternalEdit() => base.InternalEdit();

    /// <summary>The data set can always be modified (Delphi's GetCanModify).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractDataSet.GetCanModify")]
    public override bool CanModify => true;

    /// <summary>The object's changes (its fields were written as they were set) are discarded.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InternalCancel()
    {
        ActiveBoldObject?.Discard();
        fInsertedElement = null;
        base.InternalCancel();
    }

    /// <summary>The values of the result fields of the first record whose key fields match (case-insensitive); null when none.</summary>
    [Delphi(PortStatus.Ported)]
    public override object? Lookup(string KeyFields, object? KeyValues, string ResultFields)
    {
        int found = LocateRecord(KeyFields, KeyValues, DelphiSet<TLocateOption>.Of(TLocateOption.loCaseInsensitive), false);
        if (found == -1)
        {
            return null;
        }

        string[] names = ResultFields.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object?[] values = Array.ConvertAll(names, n => BoldElementValue(FieldByName(n).FieldNo - 1, fElements[found]!));
        return values.Length == 1 ? values[0] : values;
    }

    /// <summary>The handle's current element follows the current record.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoAfterScroll()
    {
        if (FListFollower is not null && FListFollower.Follower.CurrentIndex != RecNo - 1)
        {
            FListFollower.SetFollowerIndex(RecNo - 1);
        }

        base.DoAfterScroll();
    }

    /// <summary>Whether the fields' values (as strings) match the key values, by the options.</summary>
    [Delphi(PortStatus.Ported, Note = "The fields are given by their indexes in a record.")]
    protected bool ValuesInFields(List<int> AFields, object? KeyValues, DelphiSet<TLocateOption> Options, object?[] Record)
    {
        bool CompareField(object? fieldValue, object? AValue)
        {
            string field = Convert.ToString(fieldValue is TBoldObjectLocator locator ? locator.BoldObject?.AsString : fieldValue, CultureInfo.CurrentCulture) ?? "";
            string key = Convert.ToString(AValue, CultureInfo.CurrentCulture) ?? "";
            StringComparison comparison = Options.Contains(TLocateOption.loCaseInsensitive) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            return Options.Contains(TLocateOption.loPartialKey)
                ? field.Length >= key.Length && string.Compare(field, 0, key, 0, key.Length, comparison) == 0
                : string.Equals(field, key, comparison);
        }

        if (AFields.Count == 1)
        {
            return CompareField(Record[AFields[0]], KeyValues is object?[] { Length: 1 } single ? single[0] : KeyValues);
        }

        object?[] keys = (object?[])KeyValues!;
        for (int i = 0; i < AFields.Count; i++)
        {
            if (!CompareField(Record[AFields[i]], keys[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The index of the record of the key: with the key field SELF the element of the locator (or object) given, else
    /// the first record whose key fields match; -1 when none.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Gives the record's index (Delphi set the cursor and gave whether one was found).")]
    protected int LocateRecord(string KeyFields, object? KeyValues, DelphiSet<TLocateOption> Options, bool SyncCursor)
    {
        if (BoldHandle is null || BoldHandle.Count == 0 || !Active)
        {
            return -1;
        }

        if (SysUtils.UpperCase(KeyFields) == "SELF") // Do not localize
        {
            TBoldElement? LElement = KeyValues switch
            {
                TBoldObjectLocator locator => locator.EnsuredBoldObject,
                TBoldElement element => element,
                _ => null,
            };
            return LElement is null ? -1 : fElements.IndexOf(LElement);
        }

        List<int> LFields = [.. KeyFields.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(n => FieldByName(n).FieldNo - 1)];
        for (int i = 0; i < fElements.Count; i++)
        {
            object?[] record = new object?[FieldDescriptions.Count];
            for (int f = 0; f < record.Length; f++)
            {
                record[f] = BoldElementValue(f, fElements[i]!);
            }

            if (ValuesInFields(LFields, KeyValues, Options, record))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Goes to the record of the key (see LocateRecord), with the scroll events.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool Locate(string KeyFields, object? KeyValues, DelphiSet<TLocateOption> Options)
    {
        if (State is TDataSetState.dsEdit or TDataSetState.dsInsert)
        {
            Post();
        }

        DoBeforeScroll();
        int found = LocateRecord(KeyFields, KeyValues, Options, true);
        if (found != -1)
        {
            GotoRecord(found);
            DoAfterScroll();
            return true;
        }

        return false;
    }

    /// <summary>Whether the bookmark (an element) is in the list.</summary>
    [Delphi(PortStatus.Ported)]
    public bool BookmarkValid(object? Bookmark) => GetIndexForBookmark(Bookmark) != -1;

    /// <summary>The index of the bookmark's element in the list, or -1.</summary>
    [Delphi(PortStatus.Ported)]
    private int GetIndexForBookmark(object? Bookmark) => Bookmark is TBoldElement element && List is { } list ? list.IndexOf(element) : -1;

    /// <summary>The values of every field of the element in the active record.</summary>
    [Delphi(PortStatus.Ported)]
    private void BoldToBuffer(TBoldElement Element)
    {
        for (int i = 0; i < FieldDescriptions.Count; i++)
        {
            BoldElementToBuffer(i, Element);
        }
    }

    /// <summary>An element left the list: the records load again (an edit of another element is cancelled).</summary>
    [Delphi(PortStatus.Ported)]
    private void _DeleteRow(int index, TBoldFollower owningFollower)
    {
        //_DeleteRow will also be called when closing the dataset, so do nothing
        if (!Active || State == TDataSetState.dsInsert)
        {
            return;
        }

        if (BoldHandle?.CurrentElement != ActiveBoldElement && State == TDataSetState.dsEdit)
        {
            Cancel();
        }
        else if (State != TDataSetState.dsEdit)
        {
            ResyncRecords();
        }
    }

    /// <summary>An element joined the list: the records load again (not while inserting).</summary>
    [Delphi(PortStatus.Ported)]
    private void _InsertRow(TBoldFollower Follower)
    {
        if (State is TDataSetState.dsInsert or TDataSetState.dsInactive)
        {
            return;
        }

        ResyncRecords();
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldList? GetList() => BoldHandle?.List;

    /// <summary>Writes the field's value to the element's member of the field's expression (when it can change).</summary>
    [Delphi(PortStatus.Ported, Note = "The value is the field's .NET value (string, int, double, decimal, bool, DateTime, a locator), not a record buffer.")]
    private void BufferToBoldElement(int AIndex, object? Value, TBoldElement LActiveElement)
    {
        TBoldDataSetFieldDescription LFieldDescr = FieldDescriptions[AIndex];
        using var IndirectElement = new TBoldIndirectElement();
        TBoldElement? LValue = null;
        try
        {
            LActiveElement.EvaluateExpression(LFieldDescr.Expression, IndirectElement);
            LValue = IndirectElement.Value;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Eat exceptions quietly
        }

        if (LValue is null || !LValue.Mutable)
        {
            return;
        }

        switch (LFieldDescr.FieldType)
        {
            case TFieldType.ftBoolean:
                LValue.AsString = Value is true ? "Y" : "N";
                break;
            case TFieldType.ftString:
            case TFieldType.ftMemo:
            case TFieldType.ftBlob:
                LValue.AsString = Convert.ToString(Value, CultureInfo.CurrentCulture) ?? "";
                break;
            case TFieldType.ftInteger:
            case TFieldType.ftSmallint:
            case TFieldType.ftWord:
                if (LValue is TBoldObjectReference reference)
                {
                    switch (Value)
                    {
                        case TBoldObjectLocator locator:
                            locator.EnsureBoldObject();
                            reference.BoldObject = locator.BoldObject;
                            break;
                        case TBoldObject boldObject:
                            reference.BoldObject = boldObject;
                            break;
                        default:
                            reference.BoldObject = null;
                            break;
                    }
                }
                else if (LValue is TBAValueSet valueSet)
                {
                    valueSet.AsInteger = Convert.ToInt32(Value ?? 0, CultureInfo.InvariantCulture);
                }
                else
                {
                    LValue.AsString = Value is null ? "" : Convert.ToInt32(Value, CultureInfo.InvariantCulture).ToString(CultureInfo.CurrentCulture);
                }

                break;
            case TFieldType.ftCurrency:
            case TFieldType.ftFloat:
                if (Value is null)
                {
                    LValue.AsString = "";
                }
                else
                {
                    LValue.SetAsVariant(Convert.ToDouble(Value, CultureInfo.InvariantCulture));
                }

                break;
            case TFieldType.ftTime:
            case TFieldType.ftDate:
            case TFieldType.ftDateTime:
                if (Value is DateTime moment)
                {
                    LValue.SetAsVariant(moment);
                }
                else
                {
                    LValue.AsString = "";
                }

                break;
        }
    }

    /// <summary>The field's value of the element (null for a null attribute or an expression that fails).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractDataSet.BoldElementToBuffer", Note = "Gives the field's .NET value; BoldElementToBuffer below puts it in the active record.")]
    internal object? BoldElementValue(int AIndex, TBoldElement LActiveElement)
    {
        TBoldDataSetFieldDescription LFieldDescr = FieldDescriptions[AIndex];
        using var IndirectElement = new TBoldIndirectElement();
        try
        {
            LActiveElement.EvaluateExpression(LFieldDescr.Expression, IndirectElement);
            TBoldElement? value = IndirectElement.Value;
            if (value is null || value is TBoldAttribute { IsNull: true })
            {
                return null;
            }

            switch (LFieldDescr.FieldType)
            {
                case TFieldType.ftBoolean:
                    return value.AsString == "Y";
                case TFieldType.ftString:
                case TFieldType.ftMemo:
                case TFieldType.ftBlob:
                    return value.AsString;
                case TFieldType.ftInteger:
                case TFieldType.ftSmallint:
                case TFieldType.ftWord:
                    return value switch
                    {
                        TBoldObjectReference reference => reference.Locator,
                        TBoldObject boldObject => boldObject.BoldObjectLocator,
                        TBAValueSet valueSet => valueSet.AsInteger,
                        _ => value.AsString == "" ? 0 : Convert.ToInt32(value.GetAsVariant() ?? value.AsString, CultureInfo.InvariantCulture),
                    };
                case TFieldType.ftFloat:
                    return Convert.ToDouble(value.GetAsVariant(), CultureInfo.InvariantCulture);
                case TFieldType.ftCurrency:
                    return Convert.ToDecimal(value.GetAsVariant(), CultureInfo.InvariantCulture);
                case TFieldType.ftDate:
                case TFieldType.ftTime:
                case TFieldType.ftDateTime:
                    return value is TBAMoment ? value.GetAsVariant() : value.AsString.Trim() == "" ? null : DateTime.Parse(value.AsString, CultureInfo.CurrentCulture);
                default:
                    return value.AsString;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Eat exceptions quietly
            return null;
        }
    }

    /// <summary>Puts the field's value of the element in the active record.</summary>
    internal void BoldElementToBuffer(int AIndex, TBoldElement LActiveElement) => SetBufferValue(AIndex, BoldElementValue(AIndex, LActiveElement));

    /// <summary>The element of the record being inserted, else of the current record.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetActiveBoldElement()
    {
        if (!Active)
        {
            return null;
        }

        if (State == TDataSetState.dsInsert)
        {
            return fInsertedElement;
        }

        return RecNo > 0 && RecNo <= fElements.Count ? fElements[RecNo - 1] : null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetActiveBoldObject() => ActiveBoldElement as TBoldObject;

    /// <summary>A field set in the record being edited or inserted is written to its element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractDataSet.SetFieldData", Note = "Bold.Rtl's FieldChanged: the field's value is written to the element (Delphi wrote the buffer, then the element).")]
    protected override void FieldChanged(int Index)
    {
        base.FieldChanged(Index);
        if (ActiveBoldElement is { } element && ActiveRecord is { } record)
        {
            BufferToBoldElement(Index, record[Index], element);
        }
    }

    /// <summary>Deletes the current object.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InternalDelete(object?[] Record)
    {
        TBoldObject? deleted = ActiveBoldObject;
        int index = RecNo - 1;
        fInternalChange = true;
        try
        {
            deleted?.Delete();
        }
        finally
        {
            fInternalChange = false;
        }

        if (index >= 0 && index < fElements.Count)
        {
            fElements.RemoveAt(index);
        }
    }

    /// <summary>The posted element becomes the handle's current element.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InternalPost(object?[] Record, object?[]? OldRecord, bool Inserted)
    {
        fPostedElement = Inserted ? fInsertedElement : ActiveBoldElement;
        if (Inserted)
        {
            fElements.Add(fInsertedElement);
            fInsertedElement = null;
        }

        int FRecordPos = List?.IndexOf(fPostedElement) ?? -1;
        if (FListFollower is not null && FRecordPos != FListFollower.Follower.CurrentIndex)
        {
            FListFollower.SetFollowerIndex(FRecordPos);
        }
    }

    /// <summary>The records follow the list's order after a post (an inserted element may be anywhere in it).</summary>
    protected override void DoAfterPost()
    {
        TBoldElement? posted = fPostedElement;
        fPostedElement = null;
        ResyncRecords();
        if (posted is not null && fElements.IndexOf(posted) is var index and >= 0)
        {
            GotoRecord(index);
        }

        base.DoAfterPost();
    }

    [Delphi(PortStatus.Ported)]
    protected override void InternalClose()
    {
        fInternalCursorReady = false;
        fElements.Clear();
        fInsertedElement = null;
    }

    /// <summary>Lets the environment handle an exception at design time.</summary>
    [Delphi(PortStatus.Ported)]
    protected void InternalHandleException() => BoldEnvironment.BoldEffectiveEnvironment().HandleDesigntimeException(this);

    /// <summary>A field definition per field description.</summary>
    [Delphi(PortStatus.Ported)]
    protected void InternalInitFieldDefs()
    {
        FieldDefs.Clear();
        for (int i = 0; i < FieldDescriptions.Count; i++)
        {
            TBoldDataSetFieldDescription description = FieldDescriptions[i];
            FieldDefs.Add(description.EffectiveFieldName, description.FieldType, description.FieldSize, description.Required);
        }
    }

    /// <summary>The field definitions (when the handle has an element type) and a record per element, with an enabled handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InternalOpen(TFieldDefs FieldDefs, List<object?[]> Records)
    {
        fInternalCursorReady = BoldHandle is { Enabled: true };
        if (!fInternalCursorReady)
        {
            FieldDefs.Clear();
            return;
        }

        if (BoldHandle!.ListElementType is not null)
        {
            InternalInitFieldDefs();
        }

        FLayoutChanged = false;
        InternalLoadRecords(Records);
    }

    /// <summary>A record per element of the list.</summary>
    protected override void InternalLoadRecords(List<object?[]> Records)
    {
        fElements.Clear();
        if (!fInternalCursorReady || List is not { } list)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            TBoldElement element = list[i]!;
            object?[] record = new object?[FieldDescriptions.Count];
            for (int f = 0; f < record.Length; f++)
            {
                record[f] = BoldElementValue(f, element);
            }

            fElements.Add(element);
            Records.Add(record);
        }
    }

    /// <summary>The records load again from the elements (Delphi's Resync), unless the data set is changing them itself.</summary>
    internal void ResyncRecords()
    {
        if (!fInternalChange)
        {
            Resync();
        }
    }

    /// <summary>The fields changed: an open data set closes and opens again (as does one that opens by itself).</summary>
    [Delphi(PortStatus.Ported)]
    protected void LayoutChanged()
    {
        bool WasActive = Active;
        FLayoutChanged = true;
        if (WasActive)
        {
            Close();
        }

        FieldDescriptions.InvalidateFields();
        if ((WasActive || AutoOpen) && !fDestroying)
        {
            TryToOpen();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldHandle() => FListFollower!.BoldHandle;

    /// <summary>The list handle; the data set hears when it goes, changes value or loses elements.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? NewValue)
    {
        if (FListFollower!.BoldHandle != NewValue)
        {
            FListFollower.BoldHandle = NewValue;
            FContextSubscriber!.CancelAllSubscriptions();
            if (NewValue is not null)
            {
                NewValue.AddSubscription(FContextSubscriber, BoldSubscription.beDestroying, BoldSubscription.beDestroying);
                NewValue.AddSubscription(FContextSubscriber, BoldSubscription.beValueIdentityChanged, BoldSubscription.beValueIdentityChanged);
                NewValue.List?.AddSubscription(FContextSubscriber, BoldSubscription.beItemDeleted, BoldSubscription.beItemDeleted);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetFieldDescriptions(TBoldDataSetFieldDescriptions Value) => FieldDescriptions.Assign(Value);

    /// <summary>A new element type changes the layout.</summary>
    [Delphi(PortStatus.Ported)]
    protected void TypeMayHaveChanged()
    {
        TBoldElementTypeInfo? NewType = ListElementType;
        if (NewType != fLastKnownType)
        {
            fLastKnownType = NewType;
            TypeHasChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void TypeHasChanged() =>
        //what to do?
        LayoutChanged();

    /// <summary>After loading: default fields when there are none, and opening when it opens by itself.</summary>
    [Delphi(PortStatus.Ported, Note = "Called by EndInit (ISupportInitialize), as Delphi called it after reading the form.")]
    protected virtual void Loaded()
    {
        if (FieldDescriptions.Count == 0)
        {
            CreateDefaultFields();
        }

        if (AutoOpen && !Active)
        {
            TryToOpen();
        }
    }

    /// <summary>A field per attribute of the element class (one field for attributes or lists of them).</summary>
    [Delphi(PortStatus.Ported)]
    public void CreateDefaultFields()
    {
        try
        {
            DeleteAllFields();
            switch (ListElementType)
            {
                case TBoldClassTypeInfo classType:
                    for (int i = 0; i < classType.AllMembers.Count; i++)
                    {
                        if (classType.AllMembers[i].IsAttribute)
                        {
                            FieldDescriptions.Add().BoldProperties.Expression = classType.AllMembers[i].ExpressionName;
                        }
                    }

                    break;
                case TBoldAttributeTypeInfo:
                case TBoldListTypeInfo:
                    FieldDescriptions.Add();
                    break;
            }
        }
        finally
        {
            LayoutChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void DeleteAllFields()
    {
        while (FieldDescriptions.Count > 0)
        {
            FieldDescriptions.Delete(0);
        }
    }

    /// <summary>Whether the field's value is not null in the current record (Delphi's GetFieldData without a buffer).</summary>
    [Delphi(PortStatus.Ported, Note = "Gives whether the value is not null; the field's value is TField.Value.")]
    public bool GetFieldData(TField Field)
    {
        if (ActiveBoldElement is not { } element || Field.FieldNo <= 0)
        {
            return false;
        }

        using var IndirectElement = new TBoldIndirectElement();
        element.EvaluateExpression(FieldDescriptions[Field.FieldNo - 1].Expression, IndirectElement);
        return IndirectElement.Value is not TBoldAttribute attribute || !attribute.IsNull;
    }

    void ISupportInitialize.BeginInit() => fLoading = true;

    void ISupportInitialize.EndInit()
    {
        fLoading = false;
        Loaded();
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldList? List => GetList();

    [Delphi(PortStatus.Ported)]
    protected internal TBoldElementTypeInfo? ListElementType => GetListElementType();

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    protected bool AutoOpen
    {
        get => FAutoOpen;
        set => SetAutoOpen(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElement? ActiveBoldElement => GetActiveBoldElement();

    [Delphi(PortStatus.Ported)]
    public TBoldObject? ActiveBoldObject => GetActiveBoldObject();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldMemberRTInfo? BoldRTInfo(string AFieldName) => GetBoldRTInfo(AFieldName);

    [Delphi(PortStatus.Ported)]
    public TBoldDataSetFieldDescriptions FieldDescriptions
    {
        get => FFieldDescriptions!;
        set => SetFieldDescriptions(value);
    }

    /// <summary>Whether the layout changed since the data set last opened.</summary>
    public bool IsLayoutChanged => FLayoutChanged;
}

/// <summary>A Bold data set with its handle and opening published.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDataSet : TBoldAbstractDataSet
{
    public TBoldDataSet(object? AOwner = null)
        : base(AOwner)
    {
    }

    /// <summary>Open or closed (Delphi's published Active).</summary>
    public new bool Active
    {
        get => base.Active;
        set
        {
            if (value)
            {
                Open();
            }
            else
            {
                Close();
            }
        }
    }

    /// <summary>The data set opens when it can.</summary>
    public new bool AutoOpen
    {
        get => base.AutoOpen;
        set => base.AutoOpen = value;
    }

    /// <summary>The list handle whose elements are the records.</summary>
    public new TBoldAbstractListHandle? BoldHandle
    {
        get => base.BoldHandle;
        set => base.BoldHandle = value;
    }
}

/// <summary>Unit-level constants of BoldDataSet.</summary>
public static class BoldDataSet
{
    /// <summary>The attribute classes and their fields; it is important that subclasses come after their superclass!</summary>
    internal static readonly TBoldConvInfo[] arConvInfo =
    [
        new(typeof(TBAString), TFieldType.ftString, 255, true, false),
        new(typeof(TBANumeric), TFieldType.ftInteger, sizeof(int), false, true),
        new(typeof(TBAInteger), TFieldType.ftInteger, sizeof(int), false, true),
        new(typeof(TBASmallInt), TFieldType.ftSmallint, sizeof(short), false, true),
        new(typeof(TBAShortInt), TFieldType.ftSmallint, sizeof(short), false, true),
        new(typeof(TBAWord), TFieldType.ftWord, sizeof(ushort), false, true),
        new(typeof(TBAByte), TFieldType.ftSmallint, sizeof(short), false, true),
        new(typeof(TBAFloat), TFieldType.ftFloat, sizeof(double), false, true),
        new(typeof(TBACurrency), TFieldType.ftCurrency, sizeof(double), false, true),
        new(typeof(TBABlob), TFieldType.ftBlob, 40, true, false),
        new(typeof(TBADateTime), TFieldType.ftDateTime, sizeof(double), false, false),
        new(typeof(TBADate), TFieldType.ftDate, sizeof(int), false, false),
        new(typeof(TBATime), TFieldType.ftTime, sizeof(int), false, false),
        new(typeof(TBAValueSet), TFieldType.ftInteger, sizeof(int), false, false),
        new(typeof(TBABoolean), TFieldType.ftBoolean, sizeof(short), false, false),
    ];
}
