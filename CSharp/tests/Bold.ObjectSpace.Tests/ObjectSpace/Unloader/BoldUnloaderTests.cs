// Delphi: Source/ObjectSpace/Unloader/BoldUnloader.pas
// Port map: CSharp/port/units/BoldUnloader.md
namespace Bold.Tests;

public class BoldUnloaderTests
{
    [Test]
    [Covers(typeof(TBoldUnLoader), "*")]
    [Covers(typeof(TBoldInvalidateMemberEvent))]
    [Covers(typeof(TBoldUnloadObjectEvent))]
    public void Untouched_clean_objects_are_unloaded_and_fetched_again()
    {
        string file = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.xml");
        try
        {
            using var s = new ObjectSpaceSample(controller: m => new TBoldPersistenceControllerFileXML(file, true, m));
            TBoldObject ann = s.NewPerson();
            ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
            s.System.UpdateDatabase();
            TBoldObjectLocator locator = ann.BoldObjectLocator!;

            using var unloader = new TBoldUnLoader { ScanPerTick = 100, MinAgeForUnload = 0 };
            Assert.Catch<EBold>(() => unloader.Active = true, "a system is needed");
            unloader.BoldSystem = s.System;
            Assert.That(unloader.BoldSystem, Is.SameAs(s.System));
            var kept = new List<TBoldObject>();
            unloader.OnMayUnload = (TBoldObject o, ref bool unload) => kept.Add(o);
            unloader.OnMayInvalidate = (TBoldMember m, ref bool invalidate) => { };
            unloader.UnloadDelayedFetch = true;
            unloader.Active = true;

            for (int i = 0; i < 4 && locator.BoldObject is not null; i++)
            {
                unloader.Tick();
            }

            Assert.That(locator.BoldObject, Is.Null, "the object was unloaded");
            Assert.That(kept, Has.Count.EqualTo(1), "OnMayUnload was asked");
            Assert.That(locator.EnsuredBoldObject!.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Ann"), "and is fetched again on use");

            unloader.Active = false;
            Assert.That(unloader.Active, Is.False);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
