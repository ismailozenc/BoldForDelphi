// Delphi: Source/ValueSpace/XMLStreaming/BoldDefaultXMLStreaming.pas
// Port map: CSharp/port/units/BoldDefaultXMLStreaming.md
using System.Xml;

namespace Bold.Tests;

/// <summary>Writes a free-standing value space as XML for a model, reads it back and writes it again.</summary>
public class BoldDefaultXMLStreamingTests
{
    private static readonly string[] AttributeTypes =
        ["String", "Integer", "Float", "Currency", "Boolean", "Date", "Time", "DateTime", "Blob", "TypedBlob"];

    private sealed class Sample : IDisposable
    {
        public readonly TMoldModel Model = new(null, "Town");
        public readonly TMoldClass Person;
        public readonly TMoldClass Building;
        public readonly TMoldClass Company;
        public readonly TMoldClass Job;

        public Sample()
        {
            Model.TypeNameDictionary = new TBoldTypeNameDictionary(null);
            Model.TypeNameDictionary.AddDefaultMappings();
            Person = new TMoldClass(Model, "Person") { SuperClass = Model.RootClass };
            Building = new TMoldClass(Model, "Building") { SuperClass = Model.RootClass };
            Company = new TMoldClass(Model, "Company") { SuperClass = Model.RootClass };
            Job = new TMoldClass(Model, "Job") { SuperClass = Model.RootClass };
            foreach (string type in AttributeTypes)
            {
                new TMoldAttribute(Person, "a" + type) { BoldType = type }.SetStdTV("persistence", "persistent");
            }

            var residence = new TMoldAssociation(Model, "Residence");
            residence.SetStdTV("persistence", "persistent");
            _ = new TMoldRole(residence, "home") { MoldClass = Person, Multiplicity = "0..1" };
            _ = new TMoldRole(residence, "residents") { MoldClass = Building, Multiplicity = "0..*" };
            var employment = new TMoldAssociation(Model, "Employment") { LinkClass = Job };
            employment.SetStdTV("persistence", "persistent");
            _ = new TMoldRole(employment, "employer") { MoldClass = Person, Multiplicity = "0..1" };
            _ = new TMoldRole(employment, "employees") { MoldClass = Company, Multiplicity = "0..*" };
            Model.EnsureLinkRoles();
            Model.EnsureTopSorted();
        }

        public TBoldDefaultID Id(TMoldClass c, int value) => new(c.TopSortedIndex, true) { AsInteger = value };

        public void Dispose() => Model.Dispose();
    }

    /// <summary>Fills every member of the object with a value that depends on <paramref name="seed"/>, all modified.</summary>
    private static void Fill(Sample s, TBoldFreeStandingValueSpace vs, TMoldClass c, TBoldObjectId id, int seed)
    {
        TBoldFreeStandingObjectContents o = vs.GetEnsuredFSObjectContentsByObjectId(id);
        o.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        o.SetGlobalId(seed % 2 == 0 ? "" : "G" + seed);
        o.SetTimeStamp(seed);
        for (int i = 0; i < c.AllBoldMembers.Count; i++)
        {
            TMoldMember m = c.AllBoldMembers[i];
            o.EnsureMemberByIndex(i, m.TypeStreamName);
            TBoldFreeStandingValue v = o.FSValueByIndex(i)!;
            v.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
            var when = new DateTime(2000 + seed, 2, 3, 4, 5, 6);
            switch (v)
            {
                case TBFSString x: x.AsString = "text <&> " + seed; break;
                case TBFSInteger x: x.AsInteger = -seed; break;
                case TBFSFloat x: x.AsFloat = seed + 0.25; break;
                case TBFSCurrency x: x.AsCurrency = seed + 0.1234m; break;
                case TBFSBoolean x: x.AsBoolean = seed % 2 == 1; break;
                case TBFSDate x: x.AsDate = when.Date; break;
                case TBFSTime x: x.AsTime = new DateTime(1899, 12, 30, 4, 5, 6); break;
                case TBFSDateTime x: x.AsDateTime = when; break;
                case TBFSTypedBlob x: x.AsBlob = "\u0000\u0001ÿ" + seed; x.ContentTypeContent = "image/x"; break;
                case TBFSBlob x: x.SetContentToNull(); break;
                case TBFSObjectIDRef x: x.SetFromId(s.Id(s.Building, 50)); x.OrderNo = seed; break;
                case TBFSObjectIDRefPair x: x.SetFromIds(s.Id(s.Company, 60), s.Id(s.Job, 70)); break;
                case TBFSObjectIdListref x: x.AddId(s.Id(s.Person, 1)); x.AddId(s.Id(s.Person, 2)); break;
                case TBFSObjectIdListrefPair x:
                    using (TBoldObjectIdList persons = new(), jobs = new())
                    {
                        persons.Add(s.Id(s.Person, 1));
                        jobs.Add(s.Id(s.Job, 70));
                        x.SetFromIdLists(persons, jobs);
                    }

                    break;
            }
        }
    }

    private static string Write(Sample s, TBoldFreeStandingValueSpace vs)
    {
        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), s.Model);
        var doc = new XmlDocument();
        TBoldXMLNode root = manager.NewRootNode(doc, "ValueSpace");
        using var ids = new TBoldObjectIdList();
        vs.AllObjectIds(ids, true);
        manager.WriteValueSpace(vs, ids, null, root);
        manager.WriteValueSpace(null, ids, null, root);
        return doc.OuterXml;
    }

    private static TBoldFreeStandingValueSpace Read(Sample s, string xml, bool ignorePersistenceState)
    {
        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), s.Model)
        {
            IgnorePersistenceState = ignorePersistenceState,
        };
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var vs = new TBoldFreeStandingValueSpace();
        manager.ReadValueSpace(vs, manager.GetRootNode(doc, "ValueSpace"));
        manager.ReadValueSpace(null, manager.GetRootNode(doc, "ValueSpace"));
        return vs;
    }

    private static TBoldFreeStandingValueSpace SampleSpace(Sample s)
    {
        var vs = new TBoldFreeStandingValueSpace();
        Fill(s, vs, s.Person, s.Id(s.Person, 1), 1);
        Fill(s, vs, s.Person, s.Id(s.Person, 2), 2);
        Fill(s, vs, s.Building, s.Id(s.Building, 50), 3);
        Fill(s, vs, s.Company, s.Id(s.Company, 60), 4);
        Fill(s, vs, s.Job, s.Id(s.Job, 70), 5);
        Fill(s, vs, s.Person, new TBoldInternalObjectId(77, s.Person.TopSortedIndex, false), 6);
        vs.GetFSObjectContentsByObjectId(s.Id(s.Person, 2))!.BoldExistenceState = TBoldExistenceState.besDeleted;
        return vs;
    }

    [Test]
    [Covers(typeof(TBoldDefaultXMLStreamManager), "*")]
    [Covers(typeof(TBoldXMLClassStreamer), "*")]
    [Covers(typeof(TBoldXMLMemberStreamer), "*")]
    [Covers(typeof(TBoldXMLModelElementStreamer), "*")]
    [Covers(typeof(TBoldXMLModelElementStreamerList), "*")]
    [Covers(typeof(TBoldDefaultXMLStreamerRegistry), "*")]
    [Covers(typeof(TBoldXMLValueStreamer), "*")]
    [Covers(typeof(TBoldXMLNullableValueStreamer), "*")]
    [Covers(typeof(TBoldXMLStringContentStreamer), "*")]
    [Covers(typeof(TBoldXMLIntegerContentStreamer), "*")]
    [Covers(typeof(TBoldXMLFloatContentStreamer), "*")]
    [Covers(typeof(TBoldXMLCurrencyContentStreamer), "*")]
    [Covers(typeof(TBoldXMLBooleanContentStreamer), "*")]
    [Covers(typeof(TBoldXMLDateContentStreamer), "*")]
    [Covers(typeof(TBoldXMLTimeContentStreamer), "*")]
    [Covers(typeof(TBoldXMLDateTimeContentStreamer), "*")]
    [Covers(typeof(TBoldXMLBlobContentStreamer), "*")]
    [Covers(typeof(TBoldXMLTypedBlobStreamer), "*")]
    [Covers(typeof(TBoldXMLRoleStreamer), "*")]
    [Covers(typeof(TBoldXMLIdRefStreamer), "*")]
    [Covers(typeof(TBoldXMLIdRefPairStreamer), "*")]
    [Covers(typeof(TBoldXMLIdListRefStreamer), "*")]
    [Covers(typeof(TBoldXMLIdListRefPairStreamer), "*")]
    [Covers(typeof(TBoldXMLObjectIdStreamer), "*")]
    [Covers("BoldDefaultXMLStreaming", "initialization")]
    [Covers("BoldId", "initialization", "TBoldXMLInternalObjectIdStreamer", "TBoldXMLObjectIdListStreamer")]
    [Covers("BoldDefaultId", "initialization", "TBoldXMLDefaultIdStreamer")]
    public void Value_space_round_trips_through_XML()
    {
        using var s = new Sample();
        using TBoldFreeStandingValueSpace vs = SampleSpace(s);
        string xml = Write(s, vs);
        Assert.That(xml, Does.Contain("<Person>"));
        Assert.That(xml, Does.Contain("<ClassName>Person</ClassName><Exact>0</Exact>"), "inexact ids say so");

        using TBoldFreeStandingValueSpace read = Read(s, xml, false);
        Assert.That(Write(s, read), Is.EqualTo(xml), "reading and writing again gives the same document");

        TBoldFreeStandingObjectContents p1 = read.GetFSObjectContentsByObjectId(s.Id(s.Person, 1))!;
        Assert.That(p1.GetGlobalId(), Is.EqualTo("G1"));
        Assert.That(p1.GetTimeStamp(), Is.EqualTo(1));
        Assert.That(read.GetFSObjectContentsByObjectId(s.Id(s.Person, 2))!.BoldExistenceState, Is.EqualTo(TBoldExistenceState.besDeleted));
        TBoldFreeStandingObjectContents internalOne = read.GetFSObjectContentsByObjectId(new TBoldInternalObjectId(77, 0, true))!;
        Assert.That(internalOne.GetObjectId().TopSortedIndexExact, Is.False);
        Assert.That(internalOne.GetObjectId().TopSortedIndex, Is.EqualTo(s.Person.TopSortedIndex));

        int index(string name) => s.Person.AllBoldMembers.IndexOf(s.Person.AllBoldMembers.ItemsByName(name)!);
        Assert.That(((TBFSString)p1.FSValueByIndex(index("aString"))!).AsString, Is.EqualTo("text <&> 1"));
        Assert.That(((TBFSCurrency)p1.FSValueByIndex(index("aCurrency"))!).AsCurrency, Is.EqualTo(1.1234m));
        Assert.That(((TBFSFloat)p1.FSValueByIndex(index("aFloat"))!).AsFloat, Is.EqualTo(1.25));
        Assert.That(((TBFSDateTime)p1.FSValueByIndex(index("aDateTime"))!).AsDateTime, Is.EqualTo(new DateTime(2001, 2, 3, 4, 5, 6)));
        Assert.That(((TBFSBlob)p1.FSValueByIndex(index("aBlob"))!).GetContentIsNull(), Is.True);
        var typed = (TBFSTypedBlob)p1.FSValueByIndex(index("aTypedBlob"))!;
        Assert.That(typed.AsBlob, Is.EqualTo("\u0000\u0001ÿ1"));
        Assert.That(typed.ContentTypeContent, Is.EqualTo("image/x"));
        Assert.That(((TBFSObjectIDRef)p1.FSValueByIndex(index("home"))!).Id!.AsString, Is.EqualTo("50"));
        Assert.That(p1.FSValueByIndex(index("home"))!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified));
    }

    [Test]
    [Covers(typeof(TBoldDefaultXMLStreamManager), nameof(TBoldDefaultXMLStreamManager.IgnorePersistenceState),
        nameof(TBoldDefaultXMLStreamManager.PersistenceStatesToBeStreamed), nameof(TBoldDefaultXMLStreamManager.PersistenceStatesToOverwrite))]
    public void Streaming_follows_the_persistence_state_settings()
    {
        using var s = new Sample();
        using TBoldFreeStandingValueSpace vs = SampleSpace(s);
        TBoldFreeStandingObjectContents p1 = vs.GetFSObjectContentsByObjectId(s.Id(s.Person, 1))!;
        p1.FSValueByIndex(0)!.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
        string xml = Write(s, vs);
        Assert.That(xml, Does.Not.Contain("<aString>text <&> 1"), "only modified values are written by default");

        using TBoldFreeStandingValueSpace ignoring = Read(s, xml, true);
        TBoldFreeStandingObjectContents read = ignoring.GetFSObjectContentsByObjectId(s.Id(s.Person, 1))!;
        Assert.That(read.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent), "states are not read by default; new contents start at the first state");
        Assert.That(read.FSValueByIndex(1)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid));
        Assert.That(read.FSValueByIndex(0), Is.Null);

        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), s.Model)
        {
            PersistenceStatesToOverwrite = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsCurrent),
            PersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsCurrent),
        };
        Assert.That(manager.Model, Is.SameAs(s.Model));
        Assert.That(manager.ClassStreamerByName("Person").MemberStreamerByName("aInteger").Index, Is.EqualTo(1));
        Assert.That(manager.ClassStreamers(s.Person.TopSortedIndex).ExpressionName, Is.EqualTo("Person"));
        Assert.Throws<EBold>(() => manager.ClassStreamerByName("Nobody"));
        Assert.Throws<EBold>(() => manager.ClassStreamers(99));
        Assert.Throws<EBold>(() => manager.ClassStreamerByName("Person").MemberStreamerByName("nothing"));
        Assert.That(manager.ClassStreamerByName("Person").MemberStreamers(99), Is.Null);

        var doc = new XmlDocument();
        TBoldXMLNode root = manager.NewRootNode(doc, "ValueSpace");
        using var ids = new TBoldObjectIdList();
        ids.Add(s.Id(s.Person, 1));
        using var members = new TBoldMemberIdList();
        members.Add(new TBoldMemberID(0));
        members.Add(new TBoldMemberID(1));
        manager.WriteValueSpace(vs, ids, members, root);
        Assert.That(doc.OuterXml, Does.Contain("<aString>"), "the current value is written now");
        Assert.That(doc.OuterXml, Does.Not.Contain("<aInteger>"), "the modified one is not");
    }
}
