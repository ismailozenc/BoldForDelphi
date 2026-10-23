// Delphi: Source/BoldAwareGUI/ControlPacks/BoldNumericControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldFloatControlPack.pas,
// Source/BoldAwareGUI/ControlPacks/BoldDateTimeControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldCheckboxStateControlPack.pas,
// Source/BoldAwareGUI/ControlPacks/BoldViewerControlPack.pas, Source/BoldAwareGUI/ControlPacks/BoldMLRenderers.pas
// Port map: CSharp/port/units/BoldNumericControlPack.md
using System.Drawing;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The controllers of single values other than strings: integers, floats, dates, check boxes, viewers and multi-language strings.</summary>
[NonParallelizable]
public class BoldValueControlPacksTests
{
    /// <summary>A follower of the element with the controller, displayed.</summary>
    private static TBoldFollower Follow(TBoldElement? element, TBoldFollowerController controller)
    {
        var follower = new TBoldFollower(null, controller) { Element = element };
        GuiSample.Display();
        return follower;
    }

    [Test]
    [Covers(typeof(TBoldIntegerFollowerController), "*")]
    [Covers(typeof(TBoldAsIntegerRenderer), "*")]
    [Covers(typeof(TBoldIntegerRendererData), "*")]
    public void Integer_controllers_show_and_set_whole_numbers()
    {
        using var s = new GuiSample();
        TResidential_Building house = s.NewHouse("Main Street");
        house.ZipCode = 12345;
        var controller = new TBoldIntegerFollowerController(null) { Expression = "zipCode", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(house, controller);
        Assert.That(controller.GetCurrentAsInteger(follower), Is.EqualTo(12345));
        Assert.That(((TBoldIntegerRendererData)follower.RendererData).OldIntegerValue, Is.EqualTo(12345));

        controller.MayHaveChanged(54321, follower);
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsDirty));
        follower.Apply();
        Assert.That(house.ZipCode, Is.EqualTo(54321));
        controller.SetAsInteger(7, follower);
        Assert.That(house.ZipCode, Is.EqualTo(7));

        var assets = new TBoldIntegerFollowerController(null) { Expression = "assets" };
        TPerson ada = s.NewPerson("Ada");
        ada.Assets = 99.6m;
        using TBoldFollower rounded = Follow(ada, assets);
        Assert.That(assets.GetCurrentAsInteger(rounded), Is.EqualTo(100), "a number rounds");

        var names = new TBoldIntegerFollowerController(null) { Expression = "address" };
        Assert.That(() => Follow(house, names), Throws.InstanceOf<EBold>().With.Message.Contains("TBAString"), "a string is no integer");

        var renderer = new TBoldAsIntegerRenderer { OnGetAsInteger = (_, _, _) => 42, OnSetAsInteger = (_, v, _, _) => house.ZipCode = v * 2, OnIsChanged = (_, v, _, _) => v != 0 };
        var custom = new TBoldIntegerFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower f = Follow(house, custom);
        Assert.That(custom.GetCurrentAsInteger(f), Is.EqualTo(42));
        custom.MayHaveChanged(0, f);
        Assert.That(f.State, Is.EqualTo(TBoldFollowerState.bfsCurrent), "OnIsChanged decides");
        custom.MayHaveChanged(5, f);
        f.Apply();
        Assert.That(house.ZipCode, Is.EqualTo(10));
        Assert.That(TBoldAsIntegerRenderer.DefaultRenderer(), Is.SameAs(TBoldAsIntegerRenderer.DefaultRenderer()));
    }

    [Test]
    [Covers(typeof(TBoldFloatFollowerController), "*")]
    [Covers(typeof(TBoldAsFloatRenderer), "*")]
    [Covers(typeof(TBoldFloatRendererData), "*")]
    public void Float_controllers_show_and_set_numbers()
    {
        using var s = new ValuesSample();
        TBoldObject thing = s.NewThing();
        var weight = ValuesSample.Member<TBAFloat>(thing, "weight");
        weight.AsFloat = 12.5;
        var controller = new TBoldFloatFollowerController(null) { Expression = "weight", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(thing, controller);
        Assert.That(controller.GetCurrentAsFloat(follower), Is.EqualTo(12.5));
        Assert.That(((TBoldFloatRendererData)follower.RendererData).OldFloatValue, Is.EqualTo(12.5));
        controller.MayHaveChanged(20.25, follower);
        follower.Apply();
        Assert.That(weight.AsFloat, Is.EqualTo(20.25));
        controller.SetAsFloat(1.5, follower);
        Assert.That(weight.AsFloat, Is.EqualTo(1.5));

        var count = new TBoldFloatFollowerController(null) { Expression = "count" };
        ValuesSample.Member<TBAInteger>(thing, "count").AsInteger = 3;
        using TBoldFollower counted = Follow(thing, count);
        Assert.That(count.GetCurrentAsFloat(counted), Is.EqualTo(3.0), "any number shows as a float");
        Assert.That(() => count.SetAsFloat(2, counted), Throws.InstanceOf<EBold>(), "only a float or a moment takes a float");

        var renderer = new TBoldAsFloatRenderer { OnGetAsFloat = (_, _, _) => 3.25, OnSetAsFloat = (_, v, _, _) => weight.AsFloat = v + 1, OnIsChanged = (_, v, _, _) => v > 0 };
        var custom = new TBoldFloatFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower f = Follow(thing, custom);
        Assert.That(custom.GetCurrentAsFloat(f), Is.EqualTo(3.25));
        custom.MayHaveChanged(2, f);
        f.Apply();
        Assert.That(weight.AsFloat, Is.EqualTo(3.0));
        Assert.That(TBoldAsFloatRenderer.DefaultRenderer(), Is.Not.Null);
    }

    [Test]
    [Covers(typeof(TBoldDateTimeFollowerController), "*")]
    [Covers(typeof(TBoldAsDateTimeRenderer), "*")]
    [Covers(typeof(TBoldDateTimeRendererData), "*")]
    public void Date_time_controllers_show_and_set_moments()
    {
        using var s = new ValuesSample();
        TBoldObject thing = s.NewThing();
        var moment = ValuesSample.Member<TBADateTime>(thing, "seen");
        moment.AsDateTime = new DateTime(2024, 2, 29, 13, 45, 0);
        var controller = new TBoldDateTimeFollowerController(null) { Expression = "seen", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(thing, controller);
        Assert.That((DateTime)controller.GetCurrentAsDateTime(follower), Is.EqualTo(new DateTime(2024, 2, 29, 13, 45, 0)));
        Assert.That((DateTime)((TBoldDateTimeRendererData)follower.RendererData).OldDateTimeValue, Is.EqualTo(new DateTime(2024, 2, 29, 13, 45, 0)));
        controller.MayHaveChanged(new DateTime(2025, 1, 1), follower);
        follower.Apply();
        Assert.That(moment.AsDateTime, Is.EqualTo(new DateTime(2025, 1, 1)));
        controller.SetAsDateTime(new DateTime(2026, 6, 1), follower);
        Assert.That(moment.AsDateTime, Is.EqualTo(new DateTime(2026, 6, 1)));

        var born = new TBoldDateTimeFollowerController(null) { Expression = "born" };
        ValuesSample.Member<TBADate>(thing, "born").AsDate = new DateTime(1990, 12, 24);
        using TBoldFollower date = Follow(thing, born);
        Assert.That((DateTime)born.GetCurrentAsDateTime(date), Is.EqualTo(new DateTime(1990, 12, 24)));
        born.SetAsDateTime(new DateTime(1991, 1, 2), date);
        Assert.That(ValuesSample.Member<TBADate>(thing, "born").AsDate, Is.EqualTo(new DateTime(1991, 1, 2)));
        var names = new TBoldDateTimeFollowerController(null) { Expression = "name" };
        Assert.That(() => Follow(thing, names), Throws.InstanceOf<EBold>(), "a string is no moment");

        DateTime set = default;
        var renderer = new TBoldAsDateTimeRenderer { OnGetAsDateTime = (_, _, _) => new DateTime(2000, 1, 1), OnSetAsDateTime = (_, v, _, _) => set = v, OnIsChanged = (_, _, _, _) => true };
        var custom = new TBoldDateTimeFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower f = Follow(thing, custom);
        Assert.That((DateTime)custom.GetCurrentAsDateTime(f), Is.EqualTo(new DateTime(2000, 1, 1)));
        custom.MayHaveChanged(new DateTime(2000, 1, 1), f);
        f.Apply();
        Assert.That(set, Is.EqualTo(new DateTime(2000, 1, 1)));
        Assert.That(TBoldAsDateTimeRenderer.DefaultRenderer(), Is.Not.Null);
    }

    [Test]
    [Covers(typeof(TBoldCheckBoxStateFollowerController), "*")]
    [Covers(typeof(TBoldAsCheckBoxStateRenderer), "*")]
    [Covers(typeof(TBoldCheckBoxRendererData), "*")]
    public void Check_box_controllers_show_booleans_with_null_as_grayed()
    {
        using var s = new ValuesSample();
        TBoldObject ada = s.NewThing();
        var married = ValuesSample.Member<TBABoolean>(ada, "married");
        married.SetToNull();
        var controller = new TBoldCheckBoxStateFollowerController(null) { Expression = "married", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(ada, controller);
        Assert.That(controller.GetCurrentAsCheckBoxState(follower), Is.EqualTo(TCheckBoxState.cbGrayed));

        controller.MayHaveChanged(TCheckBoxState.cbChecked, follower);
        follower.Apply();
        Assert.That(married.AsBoolean, Is.True);
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsCheckBoxState(follower), Is.EqualTo(TCheckBoxState.cbChecked));
        controller.SetAsCheckBoxState(TCheckBoxState.cbUnchecked, follower);
        Assert.That(married.AsBoolean, Is.False);
        controller.SetAsCheckBoxState(TCheckBoxState.cbGrayed, follower);
        Assert.That(married.IsNull, Is.True);
        Assert.That(controller.ValidateCheckBoxState(TCheckBoxState.cbChecked, follower), Is.True);

        var renderer = new TBoldAsCheckBoxStateRenderer
        {
            OnGetAsCheckBoxState = (_, _, _) => TCheckBoxState.cbChecked,
            OnSetAsCheckBoxState = (_, v, _, _) => married.AsBoolean = v == TCheckBoxState.cbUnchecked,
            OnValidateCheckBoxState = (_, v, _, _) => v != TCheckBoxState.cbGrayed,
            OnIsChanged = (_, _, _, _) => true,
        };
        var custom = new TBoldCheckBoxStateFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower f = Follow(ada, custom);
        Assert.That(custom.GetCurrentAsCheckBoxState(f), Is.EqualTo(TCheckBoxState.cbChecked));
        Assert.That(custom.ValidateCheckBoxState(TCheckBoxState.cbGrayed, f), Is.False);
        custom.MayHaveChanged(TCheckBoxState.cbUnchecked, f);
        f.Apply();
        Assert.That(married.AsBoolean, Is.True);

        var names = new TBoldCheckBoxStateFollowerController(null) { Expression = "name" };
        using TBoldFollower other = Follow(ada, names);
        Assert.That(names.GetCurrentAsCheckBoxState(other), Is.EqualTo(TCheckBoxState.cbGrayed), "a value that is not a boolean is grayed");
        Assert.That(() => names.SetAsCheckBoxState(TCheckBoxState.cbChecked, other), Throws.InstanceOf<EBold>());
    }

    /// <summary>A view adapter for text blobs.</summary>
    private sealed class TextViewAdapter : TBoldAbstractViewAdapter
    {
        public string Text = "";
        public bool Changed;

        public override bool CanReadContent(string ContentType) => ContentType == "text/plain";
        public override bool Empty() => Text == "";
        public override void Clear() => Text = "";
        public override bool HasChanged() => Changed;
        public override string ContentType() => "text/plain";
        public override void CopyToClipboard() => throw new NotSupportedException();
        public override void PasteFromClipboard() => throw new NotSupportedException();

        public override void LoadFromStream(Stream Stream)
        {
            using var reader = new StreamReader(Stream, leaveOpen: true);
            Text = reader.ReadToEnd();
        }

        public override void SaveToStream(Stream Stream)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Text);
            Stream.Write(bytes, 0, bytes.Length);
        }

        public override void LoadFromFile(string Filename) => Text = File.ReadAllText(Filename);
        public override void SaveToFile(string Filename) => File.WriteAllText(Filename, Text);
        public override void Paint(TCanvas Canvas, Rectangle Rect) => Canvas.TextRect(Rect, Rect.Left, Rect.Top, Text);
        public override int Width() => Text.Length;
        public override int Height() => 1;
    }

    [Test]
    [Covers(typeof(TBoldViewerFollowerController), "*")]
    [Covers(typeof(TBoldAsViewerRenderer), "*")]
    [Covers(typeof(TBoldViewerRendererData), "*")]
    [Covers(typeof(TBoldAbstractViewAdapter), "*")]
    public void Viewer_controllers_show_blobs_through_the_adapter_for_their_content_type()
    {
        using var s = new ValuesSample();
        TBoldAbstractViewAdapter.RegisterViewAdapter(typeof(TextViewAdapter));
        Assert.That(Enumerable.Range(0, TBoldAbstractViewAdapter.ViewAdapterClassCount()).Select(TBoldAbstractViewAdapter.GetViewAdapterClass), Does.Contain(typeof(TextViewAdapter)));

        TBoldObject thing = s.NewThing();
        var blob = ValuesSample.Member<TBATypedBlob>(thing, "photo");
        blob.AsString = "hello";
        blob.ContentType = "text/plain";
        var controller = new TBoldViewerFollowerController(null) { Expression = "photo", ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(thing, controller);
        var adapter = (TextViewAdapter)controller.GetCurrentViewer(follower)!;
        Assert.That(adapter.Text, Is.EqualTo("hello"));

        adapter.Text = "world";
        adapter.Changed = true;
        controller.MayHaveChanged(adapter, follower);
        Assert.That(follower.State, Is.EqualTo(TBoldFollowerState.bfsDirty));
        follower.Apply();
        Assert.That(blob.AsString, Is.EqualTo("world"));

        controller.SetAsViewer(null, follower);
        Assert.That(blob.IsNull, Is.True, "no viewer empties the blob");
        GuiSample.Display();
        Assert.That(controller.GetCurrentViewer(follower), Is.Null);

        TBoldObject other = s.NewThing();
        var unknown = ValuesSample.Member<TBATypedBlob>(other, "photo");
        unknown.AsString = "?";
        unknown.ContentType = "application/unknown";
        Assert.That(() => Follow(other, new TBoldViewerFollowerController(null) { Expression = "photo" }), Throws.InstanceOf<EBold>(), "no adapter reads the content type");

        var given = new TextViewAdapter { Text = "given" };
        var renderer = new TBoldAsViewerRenderer { OnGetAsViewer = (_, _, _) => given, OnSetAsViewer = (_, v, _, _) => blob.AsString = ((TextViewAdapter)v!).Text.ToUpperInvariant(), OnIsChanged = (_, _, _, _) => true };
        var custom = new TBoldViewerFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower f = Follow(thing, custom);
        Assert.That(custom.GetCurrentViewer(f), Is.SameAs(given));
        custom.MayHaveChanged(given, f);
        f.Apply();
        Assert.That(blob.AsString, Is.EqualTo("GIVEN"));
        Assert.That(TBoldAsViewerRenderer.DefaultRenderer(), Is.Not.Null);
    }

    [Test]
    [Covers(typeof(TBoldAsMLStringRenderer), "*")]
    public void Multi_language_renderers_pass_their_language_to_the_events()
    {
        using var s = new GuiSample();
        TPerson ada = s.NewPerson("Ada");
        var languages = new List<string>();
        var renderer = new TBoldAsMLStringRenderer
        {
            Language = "sv",
            OnLanguageGetAsString = (e, _, _, lang) => ((TPerson)e!).FirstName + "@" + lang,
            OnLanguageSetAsString = (_, v, _, _, lang) => languages.Add(v + "@" + lang),
            OnLanguageSubscribe = (e, _, _, sub, _) => e!.SubscribeToExpression("firstName", sub),
            OnMayModify = (_, _, _, _) => true, // the string controller uses the renderer's steps only when one of its own events is set
        };
        var controller = new TBoldStringFollowerController(null) { Renderer = renderer, ApplyPolicy = TBoldApplyPolicy.bapDemand };
        using TBoldFollower follower = Follow(ada, controller);
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Ada@sv"));
        ada.FirstName = "Augusta";
        GuiSample.Display();
        Assert.That(controller.GetCurrentAsString(follower), Is.EqualTo("Augusta@sv"));
        controller.MayHaveChanged("Gusta", follower);
        follower.Apply();
        Assert.That(languages, Is.EqualTo(new[] { "Gusta@sv" }));

        var plain = new TBoldAsMLStringRenderer();
        var names = new TBoldStringFollowerController(null) { Renderer = plain, Expression = "firstName" };
        using TBoldFollower other = Follow(ada, names);
        Assert.That(names.GetCurrentAsString(other), Is.EqualTo("Augusta"), "a value that is not multi-language shows as usual");
    }
}
