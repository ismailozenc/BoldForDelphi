// Delphi: Source/Persistence/File/BoldPersistenceHandleFileXML.pas
// Port map: CSharp/port/units/BoldPersistenceHandleFileXML.md
namespace Bold.Tests;

public class BoldPersistenceHandleFileXMLTests
{
    private string fFile = "";

    [SetUp]
    public void SetUp() => fFile = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.xml");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(fFile))
        {
            File.Delete(fFile);
        }
    }

    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);

    [Test]
    [Covers(typeof(TBoldPersistenceControllerFileXML), "*")]
    [Covers(typeof(TBoldPersistenceControllerFile), "*")]
    [Covers(typeof(TBoldSystem), "UpdateDatabase")]
    public void Objects_saved_to_an_XML_file_load_into_a_new_system()
    {
        using var s = new ObjectSpaceSample(controller: m => new TBoldPersistenceControllerFileXML(fFile, true, m));
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson();
        TBoldObject house = s.NewBuilding();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        Member<TBAInteger>(ann, "age").AsInteger = 42;
        Member<TBACurrency>(ann, "salary").AsCurrency = 1234.5m;
        Member<TBADate>(ann, "born").AsDate = new DateTime(1990, 12, 24);
        Member<TBABoolean>(ann, "married").AsBoolean = true;
        bob.BoldMemberByExpressionName("firstName").AsString = "Bob";
        house.BoldMemberByExpressionName("address").AsString = "1 Main St";
        Member<TBoldObjectReference>(ann, "home").BoldObject = house;
        Member<TBoldObjectList>(ann, "friends").Add(bob);
        Assert.That(s.System.BoldDirty, Is.True);
        s.System.UpdateDatabase();
        Assert.That(s.System.BoldDirty, Is.False);
        Assert.That(File.Exists(fFile), Is.True);
        Assert.That(File.ReadAllText(fFile), Does.Contain("ValueSpace").And.Contain("Ann"));

        TBoldSystem second = s.NewSystem(new TBoldPersistenceControllerFileXML(fFile, true, s.Model));
        TBoldObjectList people = second.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
        Assert.That(people.Count, Is.EqualTo(2));
        TBoldObject ann2 = Enumerable.Range(0, 2).Select(i => people[i]!).Single(p => p.BoldMemberByExpressionName("firstName").AsString == "Ann");
        Assert.That(Member<TBAInteger>(ann2, "age").AsInteger, Is.EqualTo(42));
        Assert.That(Member<TBACurrency>(ann2, "salary").AsCurrency, Is.EqualTo(1234.5m));
        Assert.That(Member<TBADate>(ann2, "born").AsDate, Is.EqualTo(new DateTime(1990, 12, 24)));
        Assert.That(Member<TBABoolean>(ann2, "married").AsBoolean, Is.True);
        TBoldObject? home = Member<TBoldObjectReference>(ann2, "home").BoldObject;
        Assert.That(home?.BoldMemberByExpressionName("address").AsString, Is.EqualTo("1 Main St"));
        TBoldObjectList friends = Member<TBoldObjectList>(ann2, "friends");
        Assert.That(friends.Count, Is.EqualTo(1));
        Assert.That(friends[0]!.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Bob"));
        Assert.That(Member<TBoldObjectList>(home!, "residents").Includes(ann2), Is.True);

        // A change in the second system is written back.
        Member<TBAInteger>(ann2, "age").AsInteger = 43;
        second.UpdateDatabase();
        TBoldSystem third = s.NewSystem(new TBoldPersistenceControllerFileXML(fFile, true, s.Model));
        TBoldObjectList people3 = third.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
        TBoldObject ann3 = Enumerable.Range(0, 2).Select(i => people3[i]!).Single(p => p.BoldMemberByExpressionName("firstName").AsString == "Ann");
        Assert.That(Member<TBAInteger>(ann3, "age").AsInteger, Is.EqualTo(43));
    }

    [Test]
    [Covers(typeof(TBoldPersistenceHandleFileXML), "*")]
    [Covers(typeof(TBoldAbstractPersistenceHandleFile), "*")]
    [Covers(typeof(TBoldPersistenceHandle), "*")]
    public void The_handle_creates_an_XML_controller_for_its_model()
    {
        using var s = new ObjectSpaceSample();
        using var handle = new TBoldPersistenceHandleFileXML { FileName = fFile, CacheData = true };
        Assert.Catch<EBold>(() => _ = handle.PersistenceController, "a model is required");
        using var model = new TBoldAbstractModelForTests(s.Model);
        handle.BoldModel = model;
        var controller = (TBoldPersistenceControllerFileXML)handle.PersistenceController;
        Assert.That(controller.FileName, Is.EqualTo(fFile));
        Assert.That(controller.CacheData, Is.True);
        Assert.That(controller.MoldModel, Is.SameAs(model.MoldModel));
        Assert.That(controller.MultilinksAreStoredInObject(), Is.True);
        Assert.That(handle.HandledObject, Is.SameAs(controller));
        Assert.That(handle.PersistenceController, Is.SameAs(controller), "created once");
        handle.Active = true;
        Assert.That(handle.Active, Is.True);
        handle.ReleasePersistenceController();
        Assert.That(handle.Active, Is.False);
        Assert.That(handle.PersistenceController, Is.Not.SameAs(controller), "a new controller after release");
        Assert.Catch<EBold>(() => controller.PMSetReadOnlyness(new TBoldObjectIdList(), new TBoldObjectIdList()));
        Assert.That(BoldPersistenceHandleFileXML.XML_LINKEXTENSION, Is.EqualTo(".xml"));
    }
}

/// <summary>A model component holding a copy of a mold model (through its .bld text).</summary>
internal sealed class TBoldAbstractModelForTests : TBoldAbstractModel
{
    public TBoldAbstractModelForTests(TMoldModel model)
    {
        var strings = new TStringList();
        TMoldBLDRW.ModelToStrings(model, strings);
        SetFromModel(TMoldBLDRW.StringsToModel(strings));
    }

    public void ChangeModel(TMoldModel model)
    {
        var strings = new TStringList();
        TMoldBLDRW.ModelToStrings(model, strings);
        SetFromModel(TMoldBLDRW.StringsToModel(strings));
        SendEvent(this, BoldAbstractModel.beModelChanged);
    }
}
