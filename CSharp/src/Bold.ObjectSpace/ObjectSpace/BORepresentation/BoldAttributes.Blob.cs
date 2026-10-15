// Delphi: Source/ObjectSpace/BORepresentation/BoldAttributes.pas
// Port map: CSharp/port/units/BoldAttributes.md
// Blobs: the blob stream, plain, typed and image blobs, and their proxies. A blob is a string whose characters are
// bytes (0..255), as in Delphi.

using static Bold.BoldAttributes;
using static Bold.BoldCoreConsts;
using static Bold.BoldDefs;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>How a blob stream is opened.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldBlobStreamMode
{
    bmRead,
    bmWrite,
    bmReadWrite,
}

/// <summary>A stream over a blob attribute's value; writing changes the attribute with the usual modify events.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldBlobStream : Stream
{
    private readonly TBABlob fBlobAttr;
    private readonly TBoldBlobStreamMode fMode;
    private int fPosition;

    /// <summary>Opens the stream; write modes need a mutable blob, read modes a non-null one, and write mode empties it.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldBlobStream(TBABlob BlobAttr, TBoldBlobStreamMode Mode)
    {
        fBlobAttr = BlobAttr;
        fMode = Mode;
        if (Mode is TBoldBlobStreamMode.bmReadWrite or TBoldBlobStreamMode.bmWrite && !BlobAttr.Mutable)
        {
            BlobAttr.MutableError("Binary Large Object");
        }

        if (Mode is TBoldBlobStreamMode.bmRead or TBoldBlobStreamMode.bmReadWrite)
        {
            fBlobAttr.EnsureNotNull();
        }

        if (Mode == TBoldBlobStreamMode.bmWrite)
        {
            Truncate();
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetBlobSize() => fBlobAttr.fValue.Length;

    [Delphi(PortStatus.Ported)]
    private void StartModifyOfBlob(string Operation)
    {
        if (!fBlobAttr.StartModify())
        {
            BoldRaiseLastFailure(fBlobAttr, "BlobStream: " + Operation, "");
        }

        fBlobAttr.PreChange();
    }

    [Delphi(PortStatus.Ported)]
    private void EndModifyOfBlob()
    {
        if (Size != 0)
        {
            fBlobAttr.SetToNonNull();
        }

        fBlobAttr.Changed(beValueChanged, [fBlobAttr.fValue]);
        fBlobAttr.EndModify();
    }

    [Delphi(PortStatus.Ported)]
    private void FailModifyOfBlob() => fBlobAttr.FailModify();

    /// <summary>Sets the size; zero sets the blob to null when null is allowed. Failures are swallowed as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    private void InternalSetSize(int NewSize, bool BlobEvents)
    {
        int OldPosition = fPosition;
        if (NewSize == 0 && (fBlobAttr.BoldAttributeRTInfo is null || fBlobAttr.BoldAttributeRTInfo.AllowNull))
        {
            fBlobAttr.SetToNull();
        }
        else
        {
            try
            {
                if (BlobEvents)
                {
                    StartModifyOfBlob("SetSize");
                }

                fBlobAttr.fValue = SetLength(fBlobAttr.fValue, NewSize);
                if (BlobEvents)
                {
                    EndModifyOfBlob();
                }
            }
            catch
            {
                if (BlobEvents)
                {
                    FailModifyOfBlob();
                }
            }
        }

        if (OldPosition > NewSize)
        {
            Seek(0, SeekOrigin.End);
        }
    }

    private static string SetLength(string s, int NewLength) =>
        NewLength <= s.Length ? s[..NewLength] : s + new string('\0', NewLength - s.Length);

    /// <summary>Reads up to Count bytes from the current position.</summary>
    [Delphi(PortStatus.Ported)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        int result = count > Size - fPosition ? Size - fPosition : count;
        if (result > 0)
        {
            string value = fBlobAttr.fValue;
            for (int i = 0; i < result; i++)
            {
                buffer[offset + i] = (byte)value[fPosition + i];
            }

            fPosition += result;
        }

        return Math.Max(result, 0);
    }

    /// <summary>Writes the bytes at the current position, growing the blob; a failure fails the modify and is swallowed.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's Write returns the count written; .NET's Write returns nothing, the count is in the position.")]
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (fPosition >= 0 && count >= 0)
        {
            int EndPos = fPosition + count;
            if (EndPos > 0)
            {
                try
                {
                    StartModifyOfBlob("Write");
                    string value = EndPos > Size ? SetLength(fBlobAttr.fValue, EndPos) : fBlobAttr.fValue;
                    char[] chars = value.ToCharArray();
                    for (int i = 0; i < count; i++)
                    {
                        chars[fPosition + i] = (char)buffer[offset + i];
                    }

                    fBlobAttr.fValue = new string(chars);
                    fPosition = EndPos;
                    EndModifyOfBlob();
                }
                catch
                {
                    FailModifyOfBlob();
                }
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "Delphi's Seek(Offset, Origin: Word) with origins 0, 1, 2; here SeekOrigin.")]
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                fPosition = (int)offset;
                break;
            case SeekOrigin.Current:
                fPosition += (int)offset;
                break;
            case SeekOrigin.End:
                fPosition = GetBlobSize() + (int)offset;
                break;
        }

        return fPosition;
    }

    [Delphi(PortStatus.Ported)]
    public void Clear() => SetSize(0);

    [Delphi(PortStatus.Ported)]
    public void Truncate() => SetSize(fPosition);

    /// <summary>Replaces the blob with the whole of the stream.</summary>
    [Delphi(PortStatus.Ported)]
    public void LoadFromStream(Stream Stream)
    {
        Stream.Position = 0;
        int Count = (int)Stream.Length;
        InternalSetSize(Count, Count == 0);
        if (Count != 0)
        {
            try
            {
                StartModifyOfBlob("LoadFromStream");
                byte[] data = new byte[Count];
                Stream.ReadExactly(data, 0, Count);
                char[] chars = new char[Count];
                for (int i = 0; i < Count; i++)
                {
                    chars[i] = (char)data[i];
                }

                fBlobAttr.fValue = new string(chars);
                EndModifyOfBlob();
            }
            catch
            {
                FailModifyOfBlob();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void LoadFromFile(string FileName)
    {
        using var Stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        LoadFromStream(Stream);
    }

    [Delphi(PortStatus.Ported)]
    public void SaveToStream(Stream Stream)
    {
        if (Size != 0)
        {
            string value = fBlobAttr.fValue;
            byte[] data = new byte[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                data[i] = (byte)value[i];
            }

            Stream.Write(data, 0, data.Length);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SaveToFile(string FileName)
    {
        using var Stream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
        SaveToStream(Stream);
    }

    [Delphi(PortStatus.Ported)]
    public void SetSize(int NewSize) => InternalSetSize(NewSize, true);

    /// <summary>The blob's size (Delphi's TStream.Size).</summary>
    public int Size => GetBlobSize();

    public override bool CanRead => fMode is TBoldBlobStreamMode.bmRead or TBoldBlobStreamMode.bmReadWrite or TBoldBlobStreamMode.bmWrite;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => GetBlobSize();

    public override long Position
    {
        get => fPosition;
        set => fPosition = (int)value;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => SetSize((int)value);
}

/// <summary>A binary large object: a string whose characters are bytes.</summary>
[Delphi(PortStatus.Ported)]
public class TBABlob : TBoldAttribute
{
    internal string fValue = "";

    public TBABlob()
    {
    }

    [Delphi(PortStatus.Ported)]
    internal void SetContent(string NewValue)
    {
        if (ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void SetDataValue(string NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (IsNull || fValue != NewValue)
        {
            if (!StartModify())
            {
                BoldRaiseLastFailure(this, "SetDataValue", "");
            }

            try
            {
                SetContent(NewValue);
                EndModify();
            }
            catch
            {
                FailModify();
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            SetContent("");
        }
        else if (Source is IBoldBlobContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asBlob);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void FreeContent()
    {
        base.FreeContent();
        fValue = "";
    }

    /// <summary>brDefault is the data, brShort the content type (empty for plain blobs).</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, Meth_GetStringRepresentation, "");
        }

        return Representation switch
        {
            brDefault => IsNull ? "" : fValue,
            brShort => "",
            _ => base.GetStringRepresentation(Representation),
        };
    }

    /// <summary>brDefault sets the data; brShort is ignored (a plain blob has no content type).</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
        }

        switch (Representation)
        {
            case brDefault:
                SetDataValue(Value);
                break;
            case brShort:
                break;
            default:
                base.SetStringRepresentation(Representation, Value);
                break;
        }
    }

    [Delphi(PortStatus.Ported)] protected string GetAsBlob() => GetStringRepresentation(brDefault);
    [Delphi(PortStatus.Ported)] protected void SetAsBlob(string NewValue) => SetStringRepresentation(brDefault, NewValue);

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(string NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBABlob_Proxy);

    /// <summary>Sets from a byte array, null (empty blob) or a string.</summary>
    [Delphi(PortStatus.Ported, Note = "A byte[] where Delphi has a variant byte array.")]
    public override void SetAsVariant(object? Value)
    {
        switch (Value)
        {
            case byte[] bytes:
                {
                    char[] chars = new char[bytes.Length];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        chars[i] = (char)bytes[i];
                    }

                    SetAsBlob(new string(chars));
                    break;
                }

            case null:
            case DBNull:
                SetAsBlob("");
                break;
            default:
                SetAsBlob(Convert.ToString(Value, System.Globalization.CultureInfo.InvariantCulture) ?? "");
                break;
        }
    }

    /// <summary>The data as a byte array, or null when empty.</summary>
    [Delphi(PortStatus.Ported, Note = "A byte[] where Delphi has a variant byte array (Delphi's array has one extra trailing byte).")]
    public override object? GetAsVariant()
    {
        string Data = GetAsBlob();
        if (Data.Length == 0)
        {
            return null;
        }

        byte[] result = new byte[Data.Length];
        for (int i = 0; i < Data.Length; i++)
        {
            result[i] = (byte)Data[i];
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldBlobStream CreateBlobStream(TBoldBlobStreamMode Mode) => new(this, Mode);

    /// <summary>Sets to null and drops the data to save memory.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetToNull()
    {
        base.SetToNull();
        fValue = "";
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBABlob blob)
        {
            if (blob.IsNull)
            {
                SetToNull();
            }
            else
            {
                AsString = blob.AsString;
                ContentType = blob.ContentType;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldBlobContent s)
        {
            SetDataValue(s.asBlob);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public string ContentType
    {
        get => GetStringRepresentation(brShort);
        set => SetStringRepresentation(brShort, value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(string NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBABlob CompareBlob)
        {
            if (EitherIsNull(this, CompareBlob))
            {
                return NullSmallest(CompareBlob);
            }

            return CompareType switch
            {
                TBoldCompareType.ctDefault or TBoldCompareType.ctAsString => SysUtils.CompareStr(fValue, CompareBlob.fValue),
                TBoldCompareType.ctAsText => SysUtils.CompareText(fValue, CompareBlob.fValue),
                TBoldCompareType.ctAsAnsiString => SysUtils.AnsiCompareStr(fValue, CompareBlob.fValue),
                TBoldCompareType.ctAsAnsiText => SysUtils.AnsiCompareText(fValue, CompareBlob.fValue),
                _ => base.CompareToAs(CompareType, BoldElement),
            };
        }

        return base.CompareToAs(CompareType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBABlob CompareBlob)
        {
            if (EitherIsNull(this, CompareBlob))
            {
                return CompareBlob.IsNull && IsNull;
            }

            return CompareBlob.fValue == fValue;
        }

        return base.IsEqualAs(CompareType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldBlobContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldBlobContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetEmptyValue() => AsString = "";
}

/// <summary>A blob with a content type (brShort).</summary>
[Delphi(PortStatus.Ported)]
public class TBATypedBlob : TBABlob
{
    private string fContentType = "";

    public TBATypedBlob()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetContentType2(string Value)
    {
        BoldClearLastFailure();
        if (!CanSetContentType(Value, null))
        {
            BoldRaiseLastFailure(this, "SetContentType", "");
        }

        if (ContentIsNull || fContentType != Value)
        {
            if (!StartModify())
            {
                BoldRaiseLastFailure(this, "SetContentType", "");
            }

            try
            {
                SetContentTypeContent(Value);
                EndModify();
            }
            catch
            {
                FailModify();
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void SetContentTypeContent(string NewValue)
    {
        PreChange();
        fContentType = NewValue;
        SetToNonNull();
        Changed(beValueChanged, [NewValue]);
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        if (Representation == brShort)
        {
            if (!CanRead(null))
            {
                BoldRaiseLastFailure(this, Meth_GetStringRepresentation, "");
            }

            return fContentType;
        }

        return base.GetStringRepresentation(Representation);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (Representation == brShort)
        {
            if (!ValidateString(Value, Representation))
            {
                BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
            }

            SetContentType2(Value);
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal string GetContentTypeContent()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetContentType", "");
        }

        return ContentType;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBATypedBlob_Proxy);

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldTypedBlob s && Source is IBoldBlobContent s2)
        {
            fContentType = s.ContentTypeContent;
            SetDataValue(s2.asBlob);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SetToNull()
    {
        base.SetToNull();
        fContentType = "";
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetContentType(string Value, TBoldSubscriber? Subscriber) => SendQuery(bqMaySetContentType, [Value], Subscriber);

    [Delphi(PortStatus.Ported, Note = "Compares the other blob's content type with this one's first (as Delphi does), then the data.")]
    public override int CompareToAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBATypedBlob other)
        {
            int result = SysUtils.CompareStr(other.ContentType, ContentType);
            return result == 0 ? base.CompareToAs(CompareType, BoldElement) : result;
        }

        return base.CompareToAs(CompareType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsEqualAs(TBoldCompareType CompareType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBATypedBlob other)
        {
            return other.ContentType == ContentType && base.IsEqualAs(CompareType, BoldElement);
        }

        return base.IsEqualAs(CompareType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldTypedBlob))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldTypedBlob");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }
}

/// <summary>A blob whose content type is always image/jpeg.</summary>
[Delphi(PortStatus.Ported)]
public class TBABlobImageJPEG : TBABlob
{
    public TBABlobImageJPEG()
    {
    }

    [Delphi(PortStatus.Ported, Note = "Delphi drops the inherited result for representations other than brShort (giving ''); here it is returned.")]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        Representation == brShort ? "image/jpeg" : base.GetStringRepresentation(Representation);

    /// <summary>brShort only accepts the fixed content type (or empty).</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (Representation == brShort)
        {
            if (Value.Length != 0 && Value != ContentType)
            {
                throw new EBold(SysUtils.Format(sCannotAssignXtoY, GetType().Name, Value, ContentType));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }
}

/// <summary>A blob whose content type is always image/bitmap.</summary>
[Delphi(PortStatus.Ported)]
public class TBABlobImageBMP : TBABlob
{
    public TBABlobImageBMP()
    {
    }

    [Delphi(PortStatus.Ported, Note = "Delphi drops the inherited result for representations other than brShort (giving ''); here it is returned.")]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        Representation == brShort ? "image/bitmap" : base.GetStringRepresentation(Representation);

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (Representation == brShort)
        {
            if (Value.Length != 0 && Value != ContentType)
            {
                throw new EBold(SysUtils.Format(sCannotAssignXtoY, GetType().Name, Value, ContentType));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }
}

/// <summary>Gives a blob as IBoldBlobContent.</summary>
[Delphi(PortStatus.Ported)]
internal class TBABlob_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldBlobContent
{
    [Delphi(PortStatus.Ported)] private TBABlob GetProxedBlob() => (TBABlob)ProxedElement;
    [Delphi(PortStatus.Ported)] public string GetContentAsBlob() => ProxedBlob.fValue;
    [Delphi(PortStatus.Ported)] public void SetContentAsBlob(string NewValue) => ProxedBlob.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] protected TBABlob ProxedBlob => GetProxedBlob();
}

/// <summary>Gives a typed blob as IBoldTypedBlob (and IBoldBlobContent).</summary>
[Delphi(PortStatus.Ported)]
internal class TBATypedBlob_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBABlob_Proxy(aProxedElement, aMode), IBoldTypedBlob
{
    [Delphi(PortStatus.Ported)] private TBATypedBlob GetProxedTypedBlob() => (TBATypedBlob)ProxedElement;
    [Delphi(PortStatus.Ported)] public string GetContentTypeContent() => ProxedTypedBlob.GetContentTypeContent();
    [Delphi(PortStatus.Ported)] public void SetContentTypeContent(string NewValue) => ProxedTypedBlob.SetContentTypeContent(NewValue);
    [Delphi(PortStatus.Ported)] protected TBATypedBlob ProxedTypedBlob => GetProxedTypedBlob();
}
