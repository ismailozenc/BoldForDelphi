// Delphi: Source/Persistence/System/BoldPersistenceControllerSystem.pas
// Port map: CSharp/port/units/BoldPersistenceControllerSystem.md
namespace Bold.Tests;

public class BoldPersistenceControllerSystemTests
{
    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);

    private static TBoldObject ByName(TBoldObjectList list, string name) =>
        Enumerable.Range(0, list.Count).Select(i => list[i]!).Single(p => p.BoldMemberByExpressionName("firstName").AsString == name);

    [Test]
    [Covers(typeof(TBoldPersistenceControllerSystem), "*")]
    [Covers("BoldPersistenceControllerSystem", "TBoldIdLocatorPair", "TBoldIdLocatorPair.Create", "TBoldIdLocatorPair.Destroy", "TBoldMappingIdIndex",
        "TBoldMappingIdIndex.HashItem", "TBoldMappingIdIndex.Match", "TBoldMappingIdIndex.Hash", "TBoldMappingLocatorIndex", "TBoldMappingLocatorIndex.HashItem",
        "TBoldMappingLocatorIndex.Match", "TBoldMappingLocatorIndex.Hash", "TBoldIdLocatorMapping", "TBoldIdLocatorMapping.Create",
        "TBoldIdLocatorMapping.GetLocatorById", "TBoldIdLocatorMapping.GetIdByLocator", "TBoldIdLocatorMapping.GetPairByLocator",
        "TBoldIdLocatorMapping.AddPair", "TBoldIdLocatorMapping.EnsuredIdByLocator", "TBoldIdLocatorMapping.RemoveByLocator",
        "TBoldIdLocatorMapping.LocatorById", "TBoldIdLocatorMapping.IdByLocator")]
    public void A_system_stores_its_objects_in_another_system()
    {
        using var s = new ObjectSpaceSample();
        TBoldSystem storage = s.System;
        var controller = new TBoldPersistenceControllerSystem { BoldSystem = storage };
        TBoldSystem client = s.NewSystem(controller);
        int personIndex = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex;

        // The update creates the stored objects in list order and resolves links as it goes, as Delphi does, so the
        // building is created before the person that links to it.
        TBoldObject house = client.CreateNewObjectByExpressionName("Building");
        TBoldObject ann = client.CreateNewObjectByExpressionName("Person");
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        Member<TBAInteger>(ann, "age").AsInteger = 30;
        house.BoldMemberByExpressionName("address").AsString = "Elm St";
        Member<TBoldObjectReference>(ann, "home").BoldObject = house;
        client.UpdateDatabase();

        TBoldObjectList stored = storage.Classes(personIndex)!;
        Assert.That(stored.Count, Is.EqualTo(1));
        TBoldObject storedAnn = stored[0]!;
        Assert.That(storedAnn.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Ann"));
        Assert.That(Member<TBoldObjectReference>(storedAnn, "home").BoldObject!.BoldMemberByExpressionName("address").AsString, Is.EqualTo("Elm St"));
        TBoldObjectId storedId = controller.IdByLocator(storedAnn.BoldObjectLocator!)!;
        Assert.That(controller.LocatorById(storedId), Is.SameAs(storedAnn.BoldObjectLocator));
        Assert.That(ann.BoldObjectLocator!.BoldObjectID.IsEqual(storedId), Is.True, "the client's object got the stored id");

        var controller2 = new TBoldPersistenceControllerSystem { BoldSystem = storage };
        TBoldSystem client2 = s.NewSystem(controller2);
        TBoldObject ann2 = ByName(client2.Classes(personIndex)!, "Ann");
        Assert.That(Member<TBAInteger>(ann2, "age").AsInteger, Is.EqualTo(30));
        Assert.That(Member<TBoldObjectReference>(ann2, "home").BoldObject!.BoldMemberByExpressionName("address").AsString, Is.EqualTo("Elm St"));
        Member<TBAInteger>(ann2, "age").AsInteger = 31;
        client2.UpdateDatabase();
        Assert.That(Member<TBAInteger>(storedAnn, "age").AsInteger, Is.EqualTo(31));

        ann2.Delete();
        client2.UpdateDatabase();
        Assert.That(storage.Classes(personIndex)!.Count, Is.EqualTo(0), "a delete is passed on");

        Assert.Catch<EBold>(() => controller.PMTranslateToGlobalIds(new TBoldObjectIdList(), new TBoldIDTranslationList()));
        controller.ReserveNewIds(storage.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepContents), new TBoldObjectIdList(), new TBoldIDTranslationList());
    }
}
