// Delphi: Source/ObjectSpace/BORepresentation/BoldAttributes.pas
// Port map: CSharp/port/units/BoldAttributes.md
namespace Bold.Tests;

public class BoldAttributesTests
{
    private static TValue Fs<TValue, TContent>(Action<TContent> set) where TValue : IBoldValue, new()
    {
        var value = new TValue();
        set((TContent)(object)value);
        value.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
        return value;
    }

    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);

    [Test]
    [Covers(typeof(BoldAttributes), nameof(BoldAttributes.bqBaseAttributes), nameof(BoldAttributes.bqMaySetContentType), nameof(BoldAttributes.bqMaxAttributes), "Initialization")]
    public void Constants_and_registration()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldAttributes), "Source/ObjectSpace/BORepresentation/BoldAttributes.pas");
        TBoldMemberTypeList types = BoldMemberTypeDictionary.BoldMemberTypes();
        foreach (Type t in new[] { typeof(TBAString), typeof(TBAInteger), typeof(TBAFloat), typeof(TBACurrency), typeof(TBABlob), typeof(TBADateTime), typeof(TBABoolean), typeof(TBAConstraint) })
        {
            Assert.That(types.DescriptorByDelphiName(t.Name)?.AbstractionLevel, Is.EqualTo(TBoldAbstractionLevel.alConcrete), t.Name);
        }

        Assert.That(types.DescriptorByDelphiName("TBAMoment")?.AbstractionLevel, Is.EqualTo(TBoldAbstractionLevel.alAbstract));
    }

    [Test]
    [Covers(typeof(TBAString), "*")]
    [Covers(typeof(TBAString_Proxy), "*")]
    [Covers(typeof(TBATrimmedString), "*")]
    public void Strings_set_compare_validate_and_give_contents()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var name = Member<TBAString>(p, "firstName");
        Assert.That(name.IsNull, Is.False, "a new string attribute starts empty, not null");
        name.AsString = "Ann";
        Assert.That(name.AsString, Is.EqualTo("Ann"));
        Assert.That(name.ValidateString(new string('x', 20), BoldDefs.brDefault), Is.True);
        Assert.That(name.ValidateString(new string('x', 21), BoldDefs.brDefault), Is.False, "the model's length is 20");
        Assert.That(BoldSystem.GetBoldLastFailureReason()?.Reason, Is.EqualTo(BoldCoreConsts.sStringTooLong));
        Assert.Catch<EBoldFailure>(() => name.AsString = new string('x', 21));
        Assert.That(name.AsString, Is.EqualTo("Ann"));

        var other = new TBAString { AsString = "ann" };
        Assert.That(name.CompareToAs(TBoldCompareType.ctDefault, other), Is.EqualTo(0), "default compares ignoring case");
        Assert.That(name.CompareToAs(TBoldCompareType.ctAsString, other), Is.LessThan(0), "'A' < 'a' as string");
        Assert.That(name.CompareToAs(TBoldCompareType.ctAsText, other), Is.EqualTo(0));

        var proxy = (IBoldStringContent)name.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
        Assert.That(proxy, Is.InstanceOf<TBAString_Proxy>());
        Assert.That(proxy.asString, Is.EqualTo("Ann"));
        proxy.asString = "Bo";
        Assert.That(name.AsString, Is.EqualTo("Bo"));

        var copy = new TBAString();
        copy.Assign(name);
        Assert.That(copy.AsString, Is.EqualTo("Bo"));
        copy.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(Fs<TBFSString, IBoldStringContent>(c => c.asString = "Cy"));
        Assert.That(copy.AsString, Is.EqualTo("Cy"));
        copy.SetEmptyValue();
        Assert.That(copy.AsString, Is.EqualTo(""));

        var trimmed = new TBATrimmedString { AsString = "  padded  " };
        Assert.That(trimmed.AsString, Is.EqualTo("padded"));
    }

    [Test]
    [Covers(typeof(TBANumeric), "*")]
    [Covers(typeof(TBAInteger), "*")]
    [Covers("BoldAttributes", "TBAInteger_Proxy", "TBAInteger_Proxy.GetProxedInteger", "TBAInteger_Proxy.GetContentAsInteger", "TBAInteger_Proxy.SetContentAsInteger", "TBAInteger_Proxy.ProxedInteger")]
    [Covers(typeof(TBASmallInt), "*")]
    [Covers(typeof(TBAShortInt), "*")]
    [Covers(typeof(TBAWord), "*")]
    [Covers(typeof(TBAByte), "*")]
    public void Integers_check_their_range()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var age = Member<TBAInteger>(p, "age");
        age.AsInteger = 42;
        Assert.That(age.AsString, Is.EqualTo("42"));
        Assert.That(age.AsFloat, Is.EqualTo(42.0));
        Assert.That(age.GetAsVariant(), Is.EqualTo(42));
        age.AsString = "-7";
        Assert.That(age.AsInteger, Is.EqualTo(-7));
        age.AsString = "";
        Assert.That(age.IsNull, Is.True, "an empty string sets null where null is allowed");
        Assert.That(age.ValidateCharacter('5', BoldDefs.brDefault) && !age.ValidateCharacter('x', BoldDefs.brDefault), Is.True);
        age.SetAsVariant(3);
        Assert.That(((IBoldIntegerContent)age.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)).asInteger, Is.EqualTo(3));

        var b = new TBAByte { AsByte = 200 };
        Assert.That(b.CheckRange(256), Is.False);
        Assert.That(BoldSystem.GetBoldLastFailureReason()!.Reason, Does.Contain("Range is 0--255. Attempted to set 256"));
        Assert.Catch<EBoldFailure>(() => b.AsInteger = 300);
        Assert.That(b.AsByte, Is.EqualTo(200));
        Assert.That(new TBAShortInt().CheckRange(-129), Is.False);
        Assert.That(new TBASmallInt().CheckRange(-32768), Is.True);
        Assert.That(new TBAWord { AsWord = 65535 }.AsInteger, Is.EqualTo(65535));
        Assert.That(new TBAShortInt { AsShortInt = -5 }.AsShortInt, Is.EqualTo(-5));
        Assert.That(new TBASmallInt { AsSmallInt = 1000 }.AsSmallInt, Is.EqualTo(1000));

        var f = new TBAFloat { AsFloat = 41.5 };
        Assert.That(age.CompareTo(f), Is.LessThan(0), "integers compare with other numbers by float value");
        var empty = new TBAInteger();
        empty.SetEmptyValue();
        Assert.That(empty.AsInteger, Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(TBAFloat), "*")]
    [Covers("BoldAttributes", "TBAFloat_Proxy", "TBAFloat_Proxy.GetProxedFloat", "TBAFloat_Proxy.GetContentAsFloat", "TBAFloat_Proxy.SetContentAsFloat", "TBAFloat_Proxy.ProxedFloat")]
    [Covers(typeof(TBACurrency), "*")]
    [Covers("BoldAttributes", "TBACurrency_Proxy", "TBACurrency_Proxy.GetProxedCurrency", "TBACurrency_Proxy.GetContentAsCurrency", "TBACurrency_Proxy.SetContentAsCurrency", "TBACurrency_Proxy.ProxedCurrency")]
    public void Floats_and_currencies_convert_and_compare()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var weight = Member<TBAFloat>(p, "weight");
        weight.AsString = "72.25";
        Assert.That(weight.AsFloat, Is.EqualTo(72.25));
        Assert.That(weight.AsString, Is.EqualTo("72.25"));
        Assert.That(weight.ValidateString("7x", BoldDefs.brDefault), Is.False);
        weight.AsInteger = 3;
        Assert.That(weight.AsFloat, Is.EqualTo(3.0));

        var salary = Member<TBACurrency>(p, "salary");
        salary.AsString = "1234.5";
        Assert.That(salary.AsCurrency, Is.EqualTo(1234.5m));
        Assert.That(salary.AsString, Is.EqualTo("1234.5"));
        salary.AsString = "10.";
        Assert.That(salary.AsCurrency, Is.EqualTo(10m), "a trailing decimal separator gets a zero");
        salary.AsFloat = 0.123456;
        Assert.That(salary.AsCurrency, Is.EqualTo(0.1235m), "currency keeps four decimals");
        salary.Assign(weight);
        Assert.That(salary.AsCurrency, Is.EqualTo(3m));
        Assert.That(salary.CompareTo(weight), Is.EqualTo(0));
        Assert.That(weight.CompareTo(new TBACurrency { AsCurrency = 4m }), Is.EqualTo(-1));

        weight.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(salary.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
        Assert.That(weight.AsFloat, Is.EqualTo(3.0), "a float takes currency contents");
        ((IBoldCurrencyContent)salary.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)).asCurrency = 9.5m;
        Assert.That(salary.AsCurrency, Is.EqualTo(9.5m));
        ((IBoldFloatContent)weight.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)).asFloat = 1.5;
        Assert.That(weight.GetAsVariant(), Is.EqualTo(1.5));
        salary.SetAsVariant(2.25m);
        Assert.That(salary.GetAsVariant(), Is.EqualTo(2.25m));
    }

    [Test]
    [Covers(typeof(TBAMoment), "*")]
    [Covers(typeof(TBADateTime), "*")]
    [Covers(typeof(TBADate), "*")]
    [Covers(typeof(TBATime), "*")]
    [Covers("BoldAttributes", "TBAMoment_Proxy", "TBAMoment_Proxy.GetProxedMoment", "TBAMoment_Proxy.GetContentAsDateTime", "TBAMoment_Proxy.SetContentAsDateTime", "TBAMoment_Proxy.GetContentAsDate", "TBAMoment_Proxy.SetContentAsDate", "TBAMoment_Proxy.GetContentAsTime", "TBAMoment_Proxy.SetContentAsTime", "TBAMoment_Proxy.ProxedMoment")]
    [Covers("BoldAttributes", "TBADateTime_Proxy")]
    [Covers("BoldAttributes", "TBADate_Proxy")]
    [Covers("BoldAttributes", "TBATime_Proxy")]
    public void Moments_split_into_date_and_time()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var seen = Member<TBADateTime>(p, "seen");
        seen.AsDateTime = new DateTime(2024, 2, 29, 13, 45, 30);
        Assert.That((seen.Years, seen.Months, seen.Days, seen.Hours, seen.Minutes, seen.Seconds), Is.EqualTo(((ushort)2024, (ushort)2, (ushort)29, (ushort)13, (ushort)45, (ushort)30)));
        Assert.That(seen.AsDate, Is.EqualTo(new DateTime(2024, 2, 29)));
        Assert.That(seen.AsTime, Is.EqualTo(new DateTime(1899, 12, 30, 13, 45, 30)), "the time is on Delphi's day zero");
        seen.AsDate = new DateTime(2020, 1, 2, 5, 0, 0);
        Assert.That(seen.AsDateTime, Is.EqualTo(new DateTime(2020, 1, 2, 13, 45, 30)), "setting the date keeps the time");
        seen.AsTime = new DateTime(2000, 5, 5, 8, 0, 0);
        Assert.That(seen.AsDateTime, Is.EqualTo(new DateTime(2020, 1, 2, 8, 0, 0)), "setting the time keeps the date");
        Assert.That(seen.AsString, Is.EqualTo("1/2/2020 8:00:00 AM"));
        seen.AsString = "3/4/2021 10:11:12 PM";
        Assert.That(seen.AsDateTime, Is.EqualTo(new DateTime(2021, 3, 4, 22, 11, 12)));
        Assert.That(seen.ValidateString("not a date", BoldDefs.brDefault), Is.False);
        seen.AsString = "<now>";
        Assert.That((DateTime.Now - seen.AsDateTime).TotalMinutes, Is.LessThan(1));

        var born = Member<TBADate>(p, "born");
        born.AsString = "12/24/1990";
        Assert.That(born.AsDate, Is.EqualTo(new DateTime(1990, 12, 24)));
        Assert.That((born.Years, born.Months, born.Days), Is.EqualTo(((ushort)1990, (ushort)12, (ushort)24)));
        Assert.That(born.AsString, Is.EqualTo("12/24/1990"));
        Assert.That(born.ValidateCharacter('/', BoldDefs.brDefault) && !born.ValidateCharacter(':', BoldDefs.brDefault), Is.True);

        var wakes = Member<TBATime>(p, "wakes");
        wakes.AsString = "6:30:15";
        Assert.That((wakes.Hours, wakes.Minutes, wakes.Seconds), Is.EqualTo(((ushort)6, (ushort)30, (ushort)15)));
        Assert.That(wakes.AsSeconds, Is.EqualTo(6 * 3600 + 30 * 60 + 15));

        Assert.That(born.CompareToAs(TBoldCompareType.ctAsDate, seen), Is.LessThan(0));
        seen.AsTime = new DateTime(1899, 12, 30, 6, 30, 16);
        Assert.That(wakes.CompareToAs(TBoldCompareType.ctAsTime, seen), Is.LessThan(0));

        var contents = seen.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
        Assert.That(contents, Is.InstanceOf<IBoldDateContent>().And.InstanceOf<IBoldTimeContent>());
        ((IBoldDateContent)contents).asDate = new DateTime(2001, 1, 1);
        Assert.That(seen.AsDate, Is.EqualTo(new DateTime(2001, 1, 1)));
        born.Assign(seen);
        Assert.That(born.AsDate, Is.EqualTo(new DateTime(2001, 1, 1)));
        wakes.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(Fs<TBFSTime, IBoldTimeContent>(c => c.asTime = new DateTime(1899, 12, 30, 7, 0, 0)));
        Assert.That(wakes.Hours, Is.EqualTo(7));
        wakes.SetEmptyValue();
        Assert.That(wakes.AsTime, Is.EqualTo(SysUtils.ZeroDateTime));
    }

    [Test]
    [Covers(typeof(TBABlob), "*")]
    [Covers("BoldAttributes", "TBABlob_Proxy", "TBABlob_Proxy.GetProxedBlob", "TBABlob_Proxy.GetContentAsBlob", "TBABlob_Proxy.SetContentAsBlob", "TBABlob_Proxy.ProxedBlob")]
    [Covers(typeof(TBoldBlobStream), "*")]
    [Covers(typeof(TBATypedBlob), "*")]
    [Covers("BoldAttributes", "TBATypedBlob_Proxy", "TBATypedBlob_Proxy.GetProxedTypedBlob", "TBATypedBlob_Proxy.GetContentTypeContent", "TBATypedBlob_Proxy.SetContentTypeContent", "TBATypedBlob_Proxy.ProxedTypedBlob")]
    [Covers(typeof(TBABlobImageJPEG), "*")]
    [Covers(typeof(TBABlobImageBMP), "*")]
    public void Blobs_stream_bytes_and_keep_content_types()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var photo = Member<TBABlob>(p, "photo");
        using (TBoldBlobStream w = photo.CreateBlobStream(TBoldBlobStreamMode.bmWrite))
        {
            w.Write([1, 2, 0, 255], 0, 4);
        }

        Assert.That(photo.AsString, Is.EqualTo("\u0001\u0002\u0000ÿ"));
        Assert.That(photo.GetAsVariant(), Is.EqualTo(new byte[] { 1, 2, 0, 255 }));
        using (TBoldBlobStream r = photo.CreateBlobStream(TBoldBlobStreamMode.bmRead))
        {
            var buffer = new byte[10];
            Assert.That(r.Read(buffer, 0, 10), Is.EqualTo(4));
            Assert.That(buffer[3], Is.EqualTo(255));
            var copy = new MemoryStream();
            r.SaveToStream(copy);
            Assert.That(copy.ToArray(), Is.EqualTo(new byte[] { 1, 2, 0, 255 }));
        }

        using (TBoldBlobStream rw = photo.CreateBlobStream(TBoldBlobStreamMode.bmReadWrite))
        {
            rw.LoadFromStream(new MemoryStream([9, 8, 7]));
            Assert.That(rw.Size, Is.EqualTo(3));
            rw.Seek(1, SeekOrigin.Begin);
            rw.Truncate();
        }

        Assert.That(photo.AsString, Is.EqualTo("\u0009"));
        photo.CreateBlobStream(TBoldBlobStreamMode.bmReadWrite).Clear();
        Assert.That(photo.IsNull, Is.True, "an emptied blob is null when null is allowed");
        photo.SetAsVariant(new byte[] { 65, 66 });
        Assert.That(photo.AsString, Is.EqualTo("AB"));
        Assert.That(photo.ContentType, Is.EqualTo(""));
        photo.ContentType = "ignored";
        Assert.That(photo.ContentType, Is.EqualTo(""), "a plain blob has no content type");

        var typed = new TBATypedBlob { AsString = "data" };
        typed.ContentType = "text/plain";
        Assert.That(typed.ContentType, Is.EqualTo("text/plain"));
        var typedContents = typed.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
        Assert.That(((IBoldTypedBlob)typedContents).ContentTypeContent, Is.EqualTo("text/plain"));
        Assert.That(((IBoldBlobContent)typedContents).asBlob, Is.EqualTo("data"));
        var typed2 = new TBATypedBlob();
        typed2.Assign(typed);
        Assert.That(typed2.IsEqual(typed), Is.True);
        typed2.ContentType = "text/html";
        Assert.That(typed2.IsEqual(typed), Is.False, "typed blobs compare content types too");
        typed2.SetToNull();
        Assert.That(typed2.ContentType, Is.EqualTo(""));

        var jpeg = new TBABlobImageJPEG { AsString = "jpg" };
        Assert.That(jpeg.ContentType, Is.EqualTo("image/jpeg"));
        Assert.That(jpeg.AsString, Is.EqualTo("jpg"));
        Assert.Catch<EBold>(() => jpeg.ContentType = "image/png");
        Assert.That(new TBABlobImageBMP().ContentType, Is.EqualTo("image/bitmap"));
        Assert.That(photo.CompareTo(new TBABlob { AsString = "AC" }), Is.LessThan(0));
    }

    [Test]
    [Covers(typeof(TBAValueSetValue), "*")]
    [Covers(typeof(TBAValueSetValueList), "*")]
    [Covers(typeof(TBAValueSet), "*")]
    [Covers(typeof(TBABoolean), "*")]
    [Covers("BoldAttributes", "TBAValueSet_Proxy", "TBAValueSet_Proxy.GetProxedvalueSet", "TBAValueSet_Proxy.GetContentAsInteger", "TBAValueSet_Proxy.SetContentAsInteger", "TBAValueSet_Proxy.SetContentAsString", "TBAValueSet_Proxy.GetContentAsString", "TBAValueSet_Proxy.ProxedValueSet")]
    [Covers("BoldAttributes", "TBABoolean_Proxy", "TBABoolean_Proxy.GetContentAsBoolean", "TBABoolean_Proxy.SetContentAsBoolean")]
    public void Booleans_are_value_sets()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        var married = Member<TBABoolean>(p, "married");
        Assert.That(married.AsBoolean, Is.False, "a value set starts at its first value");
        married.SetStringRepresentation(3, "true");
        Assert.That(married.AsBoolean, Is.True);
        Assert.That(married.AsString, Is.EqualTo("Y"), "brDefault is the first representation");
        Assert.That(married.GetStringRepresentation(BoldDefs.brShort), Is.EqualTo("T"));
        Assert.That(married.GetStringRepresentation(3), Is.EqualTo("True"));
        Assert.That(married.ValidateString("true", BoldDefs.brDefault), Is.False, "brDefault only takes Y and N");
        Assert.That(married.ValidateString("maybe", BoldDefs.brDefault), Is.False);
        Assert.That(BoldSystem.GetBoldLastFailureReason()!.Reason, Is.EqualTo("Unknown stringvalue 'maybe', Allowed values: (N, Y)"));
        Assert.That(married.ValidateCharacter('Y', BoldDefs.brDefault) && !married.ValidateCharacter('Q', BoldDefs.brDefault), Is.True);
        Assert.That(married.CompareToEnumLiteral("Y"), Is.True);

        TBAValueSetValueList values = married.Values;
        Assert.That(values.Count, Is.EqualTo(2));
        Assert.That(values[1].StringRepresentationCount, Is.EqualTo(4), "one empty representation 0 and three strings");
        Assert.That(values.FindByInteger(0), Is.SameAs(values.GetFirstValue()));
        Assert.That(values.FindByText(2, "t"), Is.SameAs(values[1]));
        var strings = new TStringList();
        values.ToStringsWithNil(BoldDefs.brDefault, strings, "<none>");
        Assert.That(strings.ToArray(), Is.EqualTo(new[] { "<none>", "N", "Y" }));

        var contents = married.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
        Assert.That(((IBoldBooleanContent)contents).asBoolean, Is.True);
        ((IBoldStringContent)contents).asString = "N";
        Assert.That(married.AsBoolean, Is.False);
        ((IBoldIntegerContent)contents).asInteger = 1;
        Assert.That(married.AsInteger, Is.EqualTo(1));
        married.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(Fs<TBFSBoolean, IBoldBooleanContent>(c => c.asBoolean = false));
        Assert.That(married.GetAsVariant(), Is.EqualTo(false));
        married.SetAsVariant(true);
        Assert.That(married.CompareTo(new TBABoolean { AsBoolean = false }), Is.EqualTo(1));
        Assert.Catch<EBold>(() => married.AsInteger = 5, "there is no value 5");

        var copy = new TBABoolean();
        copy.Assign(married);
        Assert.That(copy.AsBoolean, Is.True);
        copy.SetEmptyValue();
        Assert.That(copy.AsBoolean, Is.False);
        Assert.That(values[0].BoldType, Is.Null);
        using var ie = new TBoldIndirectElement();
        values[0].GetAsList(ie);
        Assert.That(ie.Value, Is.SameAs(values[0]));
    }
}
