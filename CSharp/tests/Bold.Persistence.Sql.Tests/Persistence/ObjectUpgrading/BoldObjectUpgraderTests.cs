// Delphi: Source/Persistence/ObjectUpgrading/BoldObjectUpgrader.pas, BoldObjectUpgraderHandle.pas, BoldBatchUpgrader.pas
// Port map: CSharp/port/units/BoldObjectUpgrader.md
namespace Bold.Tests;

public class BoldObjectUpgraderTests
{
    /// <summary>The sample at model version 2 on SQLite, with an upgrader handle that appends "+" to upgraded people's names.</summary>
    private sealed class Upgrading : IDisposable
    {
        public readonly ObjectSpaceSample Sample;
        public readonly SqliteHandle Db;
        public readonly TBoldObjectUpgraderHandle UpgraderHandle = new();
        public readonly string VersionColumn;

        public Upgrading()
        {
            SqliteHandle? db = null;
            Sample = new ObjectSpaceSample(
                m =>
                {
                    m.SetBoldTV(BoldDefaultTaggedValues.TAG_USEMODELVERSION, "True");
                    m.SetBoldTV(BoldDefaultTaggedValues.TAG_MODELVERSION, "2");
                },
                m =>
                {
                    db = new SqliteHandle(m);
                    db.Handle.UpgraderHandle = UpgraderHandle;
                    UpgraderHandle.PersistenceHandle = db.Handle;
                    return db.OpenController();
                });
            Db = db!;
            UpgraderHandle.SystemTypeInfo = Sample.SystemTypeInfo;
            var item = (TBoldObjectUpgraderConfigurationItemWithEvent)UpgraderHandle.Config.Add();
            item.ExpressionName = "Person";
            item.UpgradeOlderThanVersion = 2;
            item.OnUpgradeObject = o => o.BoldMemberByExpressionName("firstName").AsString += "+";
            VersionColumn = Mapper.RootClassObjectPersistenceMapper.ModelVersionMember!.ColumnDescriptions[0].SQLName;
        }

        public TBoldSystemDefaultMapper Mapper => Db.Handle.PersistenceControllerDefault.PersistenceMapper;

        /// <summary>Saves Ann, Bob and a building, then marks every stored object as written by version 1.</summary>
        public void StoreOldObjects()
        {
            Sample.NewPerson().BoldMemberByExpressionName("firstName").AsString = "Ann";
            Sample.NewPerson().BoldMemberByExpressionName("firstName").AsString = "Bob";
            Sample.NewBuilding().BoldMemberByExpressionName("address").AsString = "1 Main St";
            Sample.System.UpdateDatabase();
            Assert.That(Scalar($"SELECT COUNT(*) FROM TownRoot WHERE {VersionColumn} = 2"), Is.EqualTo(3));
            TestDatabase.Exec(Db.Connection, $"UPDATE TownRoot SET {VersionColumn} = 1");
        }

        public long Scalar(string sql) => Convert.ToInt64(TestDatabase.Scalar(Db.Connection, sql), null);

        public void Dispose()
        {
            UpgraderHandle.ObjectUpgrader.ReleaseBoldSystem();
            Sample.Dispose();
            Db.Dispose();
            UpgraderHandle.Dispose();
        }
    }

    [Test]
    [Covers(typeof(TBoldObjectUpgrader), "GenerateAutoUpgradeScript")]
    [Covers(typeof(TBoldObjectUpgraderConfigurationWithEvent), "*")]
    public void The_auto_upgrade_script_sets_the_version_of_classes_without_a_manual_upgrade()
    {
        using var u = new Upgrading();
        var script = new TStringList();
        u.UpgraderHandle.ObjectUpgrader.GenerateAutoUpgradeScript(script);
        Assert.That(script.Count, Is.EqualTo(1));
        int personType = u.Mapper.BoldDbTypeForTopSortedIndex(u.Sample.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex);
        Assert.That(script[0], Does.StartWith($"UPDATE TownRoot SET {u.VersionColumn} = 2 WHERE BOLD_TYPE IN ("));
        Assert.That(script[0].Split('(', ')')[1].Split(", "), Does.Not.Contain(personType.ToString()));

        // An item that upgrades below an older version lets newer objects of its class be upgraded by SQL.
        u.UpgraderHandle.Config.ItemByName("Person")!.UpgradeOlderThanVersion = 1;
        script.Clear();
        u.UpgraderHandle.ObjectUpgrader.GenerateAutoUpgradeScript(script);
        Assert.That(script[0], Is.EqualTo($"UPDATE TownRoot SET {u.VersionColumn} = 2 WHERE (BOLD_TYPE = {personType}) AND ({u.VersionColumn} >= 1) AND ({u.VersionColumn} <> 2)"));
    }

    [Test]
    [Covers(typeof(TBoldBatchUpgrader), "*")]
    [Covers(typeof(TBoldObjectUpgrader), "*")]
    [Covers(typeof(TBoldObjectUpgraderHandle), "*")]
    [Covers(typeof(TBoldObjectUpgraderConfigurationItemWithEvent), "*")]
    public void The_batch_upgrader_upgrades_old_objects_by_SQL_or_through_the_event()
    {
        using var u = new Upgrading();
        u.StoreOldObjects();
        var batch = new TBoldBatchUpgrader(u.Mapper, u.UpgraderHandle.ObjectUpgrader) { IntervalBetweenBatches = 0 };
        batch.UpgradeObjects();
        Assert.That(batch.AutoUpgradedObjects, Is.EqualTo(1));
        Assert.That(batch.UpgradedObjects, Is.EqualTo(2));
        Assert.That(batch.IsUpgrading, Is.False);
        Assert.That(u.Scalar($"SELECT COUNT(*) FROM TownRoot WHERE {u.VersionColumn} <> 2"), Is.EqualTo(0));
        Assert.That(u.Scalar("SELECT COUNT(*) FROM Person WHERE firstName IN ('Ann+', 'Bob+')"), Is.EqualTo(2));
    }

    [Test]
    [Covers(typeof(TBoldObjectUpgrader), "UpgradeObjectById", "UpgradeObject")]
    public void Fetching_an_old_object_upgrades_it_first()
    {
        using var u = new Upgrading();
        u.StoreOldObjects();
        TBoldSystem second = u.Sample.NewSystem(u.Db.Handle.PersistenceController);
        TBoldObjectList people = second.Classes(u.Sample.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
        string[] names = Enumerable.Range(0, people.Count).Select(i => people[i]!.BoldMemberByExpressionName("firstName").AsString).Order().ToArray();
        Assert.That(names, Is.EqualTo(new[] { "Ann+", "Bob+" }));
        Assert.That(u.Scalar($"SELECT COUNT(*) FROM TownRoot WHERE {u.VersionColumn} = 2"), Is.EqualTo(2));
    }

    [Test]
    public void The_handle_config_has_items_with_events_named_after_the_owner()
    {
        using var handle = new TBoldObjectUpgraderHandle();
        var item = (TBoldObjectUpgraderConfigurationItemWithEvent)handle.Config.Add();
        item.ExpressionName = "Person";
        Assert.That(item.GetNamePath(), Does.EndWith("._Person_"));
        var copy = new TBoldObjectUpgraderConfigurationWithEvent(null);
        TBoldUpgradeEvent upgrade = _ => { };
        item.OnUpgradeObject = upgrade;
        ((TBoldObjectUpgraderConfigurationItemWithEvent)copy.Add()).Assign(item);
        Assert.That(((TBoldObjectUpgraderConfigurationItemWithEvent)copy[0]).OnUpgradeObject, Is.SameAs(upgrade));
        Assert.That(copy[0].ExpressionName, Is.EqualTo("Person"));
    }
}
