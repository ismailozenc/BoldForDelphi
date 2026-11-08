// Remote persistence (M10): Bold.Remote.Server and Bold.Remote.Client, built from master's SOAP persistence
// (Source/Persistence/SOAP on master).
using System.Data.Common;
using System.Text;
using BuildingClasses;
using Microsoft.Extensions.Configuration;

namespace Bold.Tests;

[NonParallelizable]
public class BoldRemotePersistenceTests
{
    public static IEnumerable<FixtureModelInfo> Fixtures => FixturePaths.Manifest;

    /// <summary>The M10 closing test: every fixture saves through the server and loads back through it, equal.</summary>
    [TestCaseSource(nameof(Fixtures))]
    public void Fixture_models_round_trip_through_the_server(FixtureModelInfo info)
    {
        TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(info.File));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        if (model.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS) != "")
        {
            Assert.Ignore("The model has errors, so it has no runtime model (see the runtime model fixture test).");
        }

        if (info.File == "BoldUMLModel.bld")
        {
            Assert.Ignore("The UML metamodel has a role named constraint, an SQL reserved word (see the SQLite round trip test).");
        }

        using var systemTypeInfo = new TBoldSystemTypeInfo(model, false, false, types);
        using var db = new SqliteHandle(model);
        db.OpenController();
        using var server = new RemoteServerSample(db.Handle, db.Model.MoldModel);
        using TBoldHttpPersistenceControllerProxy controller = server.NewController(model);
        var system = new TBoldSystem(null, systemTypeInfo, controller);
        TBoldSystem? second = null;
        try
        {
            system.MakeDefault();
            SystemFiller.Fill(system);
            system.UpdateDatabase();
            Assert.That(system.BoldDirty, Is.False);

            second = new TBoldSystem(null, systemTypeInfo, controller);
            second.MakeDefault();
            SystemFiller.AssertSame(system, second, m =>
                model.Classes.ItemsByName(m.ClassTypeInfo.ModelName)!.AllBoldMembers.ItemsByName(m.ModelName)?.Storage == TBoldStorage.bsExternal);
        }
        finally
        {
            foreach (TBoldSystem? s in new[] { system, second })
            {
                s?.Discard();
                s?.Dispose();
            }
        }
    }

    /// <summary>Two systems on the server change Ann; the second save follows the optimistic locking mode, as on the database itself.</summary>
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_OFF, "firstName", false, "Annie", TestName = "{m}(Off: the last save wins)")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS, "firstName", true, "Anna", TestName = "{m}(ModifiedMembers: the same member changed is refused)")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_TIMESTAMP, "age", true, "Anna", TestName = "{m}(TimeStamp: any later save of the object is refused)")]
    public void Optimistic_locking_works_through_the_server(string mode, string memberB, bool refused, string firstName)
    {
        using var s = new ObjectSpaceSample(m => m.SetBoldTV(BoldDefaultTaggedValues.TAG_OPTIMISTICLOCKING, mode));
        using var db = new SqliteHandle(s.Model);
        db.OpenController();
        using var server = new RemoteServerSample(db.Handle, db.Model.MoldModel);
        using TBoldHttpPersistenceControllerProxy controller = server.NewController(s.Model);
        TBoldSystem first = s.NewSystem(controller);
        TBoldObject ann = first.CreateNewObjectByExpressionName("Person");
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        ((TBAInteger)ann.BoldMemberByExpressionName("age")).AsInteger = 30;
        first.UpdateDatabase();

        TBoldObject Load(TBoldSystem system)
        {
            TBoldObject o = system.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)![0]!;
            _ = o.BoldMemberByExpressionName("firstName").AsString;
            _ = o.BoldMemberByExpressionName("age").AsString;
            return o;
        }

        TBoldSystem a = s.NewSystem(controller), b = s.NewSystem(controller);
        TBoldObject annA = Load(a), annB = Load(b);
        annA.BoldMemberByExpressionName("firstName").AsString = "Anna";
        a.UpdateDatabase();
        annB.BoldMemberByExpressionName(memberB).AsString = memberB == "age" ? "31" : "Annie";
        bool wasRefused = false;
        try
        {
            b.UpdateDatabase();
        }
        catch (EBoldOperationFailedForObjectList)
        {
            wasRefused = true;
            b.Discard();
        }

        Assert.That((wasRefused, Load(s.NewSystem(controller)).BoldMemberByExpressionName("firstName").AsString), Is.EqualTo((refused, firstName)));
    }

    [Test]
    public void Conditions_ids_and_timestamps_are_asked_of_the_server()
    {
        using var s = new ObjectSpaceSample();
        using var db = new SqliteHandle(s.Model);
        TBoldPersistenceController local = db.OpenController();
        using var server = new RemoteServerSample(db.Handle, db.Model.MoldModel);
        using TBoldHttpPersistenceControllerProxy controller = server.NewController(s.Model);
        TBoldSystem system = s.NewSystem(controller);
        for (int i = 0; i < 3; i++)
        {
            system.CreateNewObjectByExpressionName("Person").BoldMemberByExpressionName("firstName").AsString = $"P{i}";
        }

        system.CreateNewObjectByExpressionName("Building");
        system.UpdateDatabase();

        // The ids of a class's objects.
        int person = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex;
        var ids = new TBoldObjectIdList();
        controller.PMFetchIDListWithCondition(ids, new TBoldFreeStandingValueSpace(), 0, new TBoldConditionWithClass { TopSortedIndex = person }, BoldDefs.NOTVALIDCLIENTID);
        Assert.That(ids.Count, Is.EqualTo(3));

        // Ids known only by a superclass get their exact class.
        int root = s.SystemTypeInfo.ClassTypeInfoByExpressionName("BusinessClassesRoot")?.TopSortedIndex ?? 0;
        var inexact = new TBoldObjectIdList();
        inexact.Add(ids[0].CloneWithClassId(root, false));
        var translations = new TBoldIDTranslationList();
        controller.PMExactifyIds(inexact, translations);
        Assert.That(translations.Count, Is.EqualTo(1));
        Assert.That(translations.NewIds(0).TopSortedIndex, Is.EqualTo(person));

        // New ids reserved before saving.
        var newIds = new TBoldObjectIdList();
        newIds.Add(new TBoldInternalObjectId(person, true));
        var reserved = new TBoldIDTranslationList();
        controller.ReserveNewIds(new TBoldFreeStandingValueSpace(), newIds, reserved);
        Assert.That(reserved.Count, Is.EqualTo(1));
        Assert.That(reserved.NewIds(0).IsStorable, Is.True);

        // Timestamps and times: the server's answer, or its error, as the database gives it.
        TBoldTimeStampType localTimestamp = 0, remoteTimestamp = 0;
        TDateTime now = DateTime.Now;
        Exception? localError = Catch(() => local.PMTimestampForTime(now, ref localTimestamp));
        Exception? remoteError = Catch(() => controller.PMTimestampForTime(now, ref remoteTimestamp));
        Assert.That((remoteError is null, remoteTimestamp), Is.EqualTo((localError is null, localTimestamp)));
        if (remoteError is not null)
        {
            Assert.That(remoteError.Message, Does.Contain(localError!.Message));
        }

        TDateTime localTime = default, remoteTime = default;
        localError = Catch(() => local.PMTimeForTimestamp(1, ref localTime));
        remoteError = Catch(() => controller.PMTimeForTimestamp(1, ref remoteTime));
        Assert.That(remoteError is null, Is.EqualTo(localError is null));
        Assert.That(remoteTime, Is.EqualTo(localTime).Within(TimeSpan.FromSeconds(5)), "the same time, or both now where the database keeps no clock log");

        // Not offered by the server: nothing is done.
        Assert.That(() =>
        {
            controller.PMTranslateToGlobalIds(ids, new TBoldIDTranslationList());
            controller.PMTranslateToLocalIds(ids, new TBoldIDTranslationList());
            controller.PMSetReadOnlyness(ids, new TBoldObjectIdList());
        }, Throws.Nothing);
    }

    private static Exception? Catch(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    [Test]
    public void Failures_are_reported_clearly()
    {
        using var s = new ObjectSpaceSample();
        using var db = new SqliteHandle(s.Model);
        db.OpenController();
        using var server = new RemoteServerSample(db.Handle, db.Model.MoldModel);

        // The server answers what it cannot read, or an unknown operation, with a fault.
        using (var client = new HttpClient())
        {
            string Post(string body) =>
                client.PostAsync(server.PersistenceUrl, new StringContent(body, Encoding.UTF8, "application/xml")).GetAwaiter().GetResult()
                    .Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.That(Post("not xml"), Does.Contain("<Fault>"));
            Assert.That(Post("<Body><Unknown /></Body>"), Does.Contain("unknown operation Unknown"));
            Assert.That(Post("<Body />"), Does.Contain("no operation"));
        }

        // A failure on the server reaches the client with the server's message.
        using TBoldHttpPersistenceControllerProxy controller = server.NewController(s.Model);
        var ids = new TBoldObjectIdList();
        var unknownClass = new TBoldConditionWithClass { TopSortedIndex = 99 };
        Assert.That(() => controller.PMFetchIDListWithCondition(ids, new TBoldFreeStandingValueSpace(), 0, unknownClass, 0),
            Throws.TypeOf<EBold>().With.Message.Contains("failed on the server"));

        // A server that is not there.
        server.Stop();
        Assert.That(() => controller.PMFetchIDListWithCondition(ids, new TBoldFreeStandingValueSpace(), 0, new TBoldConditionWithClass(), 0),
            Throws.TypeOf<EBold>().With.Message.Contains("could not be reached"));

        // A handle needs its model and address.
        using var handle = new TBoldHttpClientPersistenceHandle();
        Assert.That(() => handle.PersistenceController, Throws.TypeOf<EBold>().With.Message.Contains("BoldModel"));
        handle.BoldModel = db.Model;
        Assert.That(() => handle.PersistenceController, Throws.TypeOf<EBold>().With.Message.Contains("Url"));
        handle.Url = "http://127.0.0.1:1/persistence";
        Assert.That(handle.PersistenceController, Is.TypeOf<TBoldHttpPersistenceControllerProxy>());
        Assert.That(((TBoldHttpPersistenceControllerProxy)handle.PersistenceController).Url.Port, Is.EqualTo(1));
    }

    [Test]
    public void The_server_reads_its_persistence_from_its_configuration()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"remote-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            string modelFile = Path.Combine(folder, "Building.bld");
            BuildingModel.Lines().SaveToFile(modelFile);
            string database = Path.Combine(folder, "Bold.db");
            var settings = new Dictionary<string, string?>
            {
                ["Bold:Model"] = modelFile,
                ["Bold:Provider"] = TestSqliteFactory.Name,
                ["Bold:ConnectionString"] = TestSqliteFactory.ConnectionString(database),
                ["Bold:Engine"] = "dbeSQLite",
                ["Bold:CreateSchema"] = "true",
                ["Bold:Propagator"] = "true",
            };
            DbProviderFactories.RegisterFactory(TestSqliteFactory.Name, TestSqliteFactory.Instance);
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            using TBoldRemotePersistenceSetup setup = TBoldRemotePersistenceSetup.FromConfiguration(configuration.GetSection("Bold"));
            Assert.That(setup.WithPropagator, Is.True);
            Assert.That(setup.PersistenceHandle, Is.TypeOf<TBoldSnooperHandle>());
            Assert.That(setup.Model.MoldModel.Classes.ItemsByName("Person"), Is.Not.Null);
            Assert.That(TestDatabase.Scalar(setup.Connection, "select count(*) from BOLD_TABLES"), Is.Not.Null, "the schema was created");
            Assert.That(() => TBoldRemotePersistenceSetup.FromConfiguration(new ConfigurationBuilder().Build()),
                Throws.TypeOf<EBold>().With.Message.Contains("Bold:Model"));
        }
        finally
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException)
            {
                // A file still held open is left in the temporary folder.
            }
        }
    }

    [Test]
    public void A_relative_model_is_found_in_the_content_root_and_an_existing_schema_is_kept()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"remote-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            BuildingModel.Lines().SaveToFile(Path.Combine(folder, "Building.bld"));
            var settings = new Dictionary<string, string?>
            {
                ["Bold:Model"] = "Building.bld",
                ["Bold:Provider"] = TestSqliteFactory.Name,
                ["Bold:ConnectionString"] = TestSqliteFactory.ConnectionString(Path.Combine(folder, "Bold.db")),
                ["Bold:Engine"] = "dbeSQLite",
                ["Bold:CreateSchema"] = "true",
            };
            DbProviderFactories.RegisterFactory(TestSqliteFactory.Name, TestSqliteFactory.Instance);
            IConfigurationSection section = new ConfigurationBuilder().AddInMemoryCollection(settings).Build().GetSection("Bold");
            Assert.That(() => TBoldRemotePersistenceSetup.FromConfiguration(section, Path.Combine(folder, "elsewhere")),
                Throws.TypeOf<EBold>().With.Message.Contains("does not exist"));
            using (TBoldRemotePersistenceSetup setup = TBoldRemotePersistenceSetup.FromConfiguration(section, folder))
            {
                Assert.That(setup.HasSchema(), Is.True, "a new database gets its tables");
                TestDatabase.Exec(setup.Connection, "UPDATE BOLD_ID SET BOLD_ID = 4711");
            }

            using (TBoldRemotePersistenceSetup setup = TBoldRemotePersistenceSetup.FromConfiguration(section, folder))
            {
                Assert.That(Convert.ToInt32(TestDatabase.Scalar(setup.Connection, "SELECT BOLD_ID FROM BOLD_ID")), Is.EqualTo(4711),
                    "a server started again with CreateSchema keeps the database it has");
            }
        }
        finally
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException)
            {
                // A file still held open is left in the temporary folder.
            }
        }
    }

    /// <summary>The tests' SQLite provider, registered for the configuration test.</summary>
    private sealed class TestSqliteFactory : DbProviderFactory
    {
        public const string Name = "Bold.Tests.Sqlite";
        public static readonly TestSqliteFactory Instance = new();

#if BOLD_OFFLINE
        public static string ConnectionString(string file) => "Data Source=" + file;

        public override DbConnection CreateConnection() => new Bold.Tests.Sqlite.SqliteConnection("");
#else
        public static string ConnectionString(string file) => "Data Source=" + file + ";Pooling=False";

        public override DbConnection CreateConnection() => new Microsoft.Data.Sqlite.SqliteConnection();
#endif
    }
}
