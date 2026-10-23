// Delphi: Source/Persistence/ExternalPersistence/*.pas
// Port map: CSharp/port/units/BoldExternalPersistenceHandleEventDriven.md
namespace Bold.Tests;

public class BoldExternalPersistenceTests
{
    /// <summary>The sample with Customer, stored partly externally: code is its external key, name is external, note internal.</summary>
    private static void AddCustomer(TMoldModel m)
    {
        var customer = new TMoldClass(m, "Customer") { SuperClass = m.RootClass };
        customer.SetBoldTV(BoldDefaultTaggedValues.TAG_STORAGE, BoldDefaultTaggedValues.TV_STORAGE_PARTIALLYEXTERNAL);
        new TMoldAttribute(customer, "code") { BoldType = "Integer" }.SetBoldTV(BoldDefaultTaggedValues.TAG_STORAGE, BoldDefaultTaggedValues.TV_STORAGE_EXTERNALKEY);
        new TMoldAttribute(customer, "name").SetBoldTV(BoldDefaultTaggedValues.TAG_STORAGE, BoldDefaultTaggedValues.TV_STORAGE_EXTERNAL);
        _ = new TMoldAttribute(customer, "note");
    }

    private sealed class EventDriven : IDisposable
    {
        public readonly Dictionary<int, string> External = new() { [1] = "Acme", [2] = "Globex" };
        public readonly ObjectSpaceSample Sample;
        public readonly SqliteHandle Db;
        public readonly TBoldExternalPersistenceHandleEventDriven Handle = new();
        public readonly List<string> Events = [];

        public EventDriven()
        {
            SqliteHandle? db = null;
            Sample = new ObjectSpaceSample(AddCustomer, m =>
            {
                db = new SqliteHandle(m);
                Handle.BoldModel = db.Model;
                Handle.NextPersistenceHandle = db.Handle;
                TBoldExternalPersistenceConfigItem item = Handle.Config.Add();
                item.ExpressionName = "Customer";
                item.OnGetKeyList = keys =>
                {
                    foreach (int code in External.Keys)
                    {
                        keys.Add(new TBoldDefaultID { AsInteger = code });
                    }
                };
                item.OnReadObject = (obj, key, _) =>
                    ((IBoldStringContent)Handle.ValueForObject(obj.AsObjectContents, "name")!).asString = External[((TBoldDefaultID)key).AsInteger];
                item.OnUpdateObject = (obj, key, _) =>
                    External[((TBoldDefaultID)key).AsInteger] = Handle.StringValueForObject(obj.AsObjectContents, "name");
                Handle.OnStartUpdates = _ => Events.Add("start");
                Handle.OnEndUpdates = _ => Events.Add("end");
                Handle.Active = true;
                return Handle.PersistenceController;
            });
            Db = db!;
        }

        public TBoldObjectList Customers(TBoldSystem system) => system.Classes(Sample.SystemTypeInfo.ClassTypeInfoByExpressionName("Customer")!.TopSortedIndex)!;

        public void Dispose()
        {
            Sample.Dispose();
            Handle.Active = false;
            Handle.Dispose();
            Db.Dispose();
        }
    }

    private static string Name(TBoldObject o) => o.BoldMemberByExpressionName("name").AsString;

    [Test]
    [Covers(typeof(TBoldExternalPersistenceHandleEventDriven), "*")]
    [Covers(typeof(TBoldExternalPersistenceControllerEventDriven), "*")]
    [Covers(typeof(TBoldAbstractPartiallyExternalPC), "*")]
    [Covers(typeof(TBoldAbstractExternalPersistenceController), "*")]
    [Covers(typeof(TBoldAbstractExternalPersistenceHandle), "*")]
    [Covers(typeof(TBoldExternalPersistenceConfigItems), "*")]
    [Covers(typeof(TBoldObjectPersistenceAdapter), "*")]
    public void External_objects_get_internal_rows_and_are_read_and_written_through_the_events()
    {
        using var e = new EventDriven();
        TBoldObjectList customers = e.Customers(e.Sample.System);
        Assert.That(customers.Count, Is.EqualTo(2));
        Assert.That(Enumerable.Range(0, 2).Select(i => Name(customers[i]!)).Order(), Is.EqualTo(new[] { "Acme", "Globex" }));
        Assert.That(TestDatabase.Scalar(e.Db.Connection, "SELECT COUNT(*) FROM Customer"), Is.EqualTo(2));
        Assert.That(e.Handle.HasNewObjects, Is.True);

        TBoldObject acme = Enumerable.Range(0, 2).Select(i => customers[i]!).Single(c => Name(c) == "Acme");
        acme.BoldMemberByExpressionName("name").AsString = "Acme Corp";
        acme.BoldMemberByExpressionName("note").AsString = "big";
        e.Sample.System.UpdateDatabase();
        Assert.That(e.External[1], Is.EqualTo("Acme Corp"));
        Assert.That(TestDatabase.Scalar(e.Db.Connection, "SELECT note FROM Customer WHERE code = 1"), Is.EqualTo("big"));
        Assert.That(e.Events, Is.EqualTo(new[] { "start", "end" }));

        // A second system finds the same internal rows, and the external key gives the internal id.
        e.External.Remove(2);
        TBoldSystem second = e.Sample.NewSystem(e.Handle.PersistenceController);
        TBoldObjectList again = e.Customers(second);
        Assert.That(again.Count, Is.EqualTo(1));
        Assert.That(Name(again[0]!), Is.EqualTo("Acme Corp"));
        Assert.That(again[0]!.BoldMemberByExpressionName("note").AsString, Is.EqualTo("big"));
        Assert.That(e.Handle.HasDeletedObjects, Is.True);
        Assert.That(e.Handle.GetObjectIdByExternalKeyInteger("Customer", 1)?.AsString, Is.EqualTo(again[0]!.BoldObjectLocator!.BoldObjectID.AsString));
    }

    /// <summary>The sample on SQLite with an external persistence handle (made by the function) chained in front.</summary>
    private sealed class Chained<THandle> : IDisposable where THandle : TBoldAbstractpartiallyExternalPH
    {
        public readonly ObjectSpaceSample Sample;
        public readonly SqliteHandle Db;
        public readonly THandle Handle;

        public Chained(THandle handle)
        {
            Handle = handle;
            SqliteHandle? db = null;
            Sample = new ObjectSpaceSample(AddCustomer, m =>
            {
                db = new SqliteHandle(m);
                Handle.BoldModel = db.Model;
                Handle.NextPersistenceHandle = db.Handle;
                Handle.Active = true;
                return Handle.PersistenceController;
            });
            Db = db!;
        }

        public TBoldObjectList Customers(TBoldSystem system) => system.Classes(Sample.SystemTypeInfo.ClassTypeInfoByExpressionName("Customer")!.TopSortedIndex)!;

        public void Dispose()
        {
            Sample.Dispose();
            Handle.Active = false;
            Handle.Dispose();
            Db.Dispose();
        }
    }

    [Test]
    [Covers(typeof(TBoldExternalPersistenceHandleDataSet), "*")]
    [Covers(typeof(TBoldExternalPersistenceControllerDataSet), "*")]
    [Covers(typeof(TBoldExternalPersistenceConfigDataSetItems), "*")]
    [Covers(typeof(TBoldExternalPersistenceConfigDataSetItem), "*")]
    public void A_data_set_holds_the_external_members()
    {
        var external = new TMemoryDataSet();
        external.FieldDefs.Add("code", TFieldType.ftInteger);
        external.FieldDefs.Add("name", TFieldType.ftString, 255);
        external.AddRecord(1, "Acme");
        external.AddRecord(2, "Globex");
        external.Open();
        var handle = new TBoldExternalPersistenceHandleDataSet();
        TBoldExternalPersistenceConfigDataSetItem item = handle.Config.Add();
        item.ClassExpressionName = "Customer";
        item.DataSet = external;
        using var c = new Chained<TBoldExternalPersistenceHandleDataSet>(handle);
        TBoldObjectList customers = c.Customers(c.Sample.System);
        Assert.That(Enumerable.Range(0, customers.Count).Select(i => Name(customers[i]!)).Order(), Is.EqualTo(new[] { "Acme", "Globex" }));

        TBoldObject globex = Enumerable.Range(0, 2).Select(i => customers[i]!).Single(o => Name(o) == "Globex");
        globex.BoldMemberByExpressionName("name").AsString = "Globex Inc";
        c.Sample.System.UpdateDatabase();
        Assert.That(external.Locate("code", 2, new DelphiSet<TLocateOption>()), Is.True);
        Assert.That(external.FieldByName("name").AsString, Is.EqualTo("Globex Inc"));
    }

    [Test]
    [Covers(typeof(TBoldExternalPersistenceHandleSQL), "*")]
    [Covers(typeof(TBoldExternalPersistenceControllerSQL), "*")]
    [Covers(typeof(TFetchContext), "*")]
    [Covers(typeof(TFetchObjectList), "*")]
    [Covers(typeof(TMemberFetchObject), "*")]
    public void Another_SQL_database_holds_the_external_members()
    {
        string file = Path.Combine(Path.GetTempPath(), $"bold-ext-{Guid.NewGuid():N}.db");
        TADOConnection connection = TestDatabase.NewADOConnection(file);
        try
        {
            TestDatabase.Exec(connection, "CREATE TABLE Customer (code INTEGER, name VARCHAR(255))");
            TestDatabase.Exec(connection, "INSERT INTO Customer VALUES (1, 'Acme')");
            TestDatabase.Exec(connection, "INSERT INTO Customer VALUES (2, 'Globex')");
            using var adapter = new TBoldDatabaseAdapterADO { Connection = connection };
            adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
            var handle = new TBoldExternalPersistenceHandleSQL { DatabaseAdapter = adapter };
            handle.ClassesToHandle.Add("Customer");
            using (var c = new Chained<TBoldExternalPersistenceHandleSQL>(handle))
            {
                TBoldObjectList customers = c.Customers(c.Sample.System);
                Assert.That(Enumerable.Range(0, customers.Count).Select(i => Name(customers[i]!)).Order(), Is.EqualTo(new[] { "Acme", "Globex" }));

                TBoldObject acme = Enumerable.Range(0, 2).Select(i => customers[i]!).Single(o => Name(o) == "Acme");
                acme.BoldMemberByExpressionName("name").AsString = "Acme's";
                c.Sample.System.UpdateDatabase();
                Assert.That(TestDatabase.Scalar(connection, "SELECT name FROM Customer WHERE code = 1"), Is.EqualTo("Acme's"));
            }
        }
        finally
        {
            connection.Close();
            connection.Connection.Dispose();
            File.Delete(file);
        }
    }
}
