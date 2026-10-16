// Delphi: Source/Persistence/DB/BoldPersistenceHandleDB.pas
// Port map: CSharp/port/units/BoldPersistenceHandleDB.md
namespace Bold.Tests;

public class BoldPersistenceHandleDBTests
{
    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);

    private static TBoldObject PersonNamed(TBoldSystem system, ObjectSpaceSample s, string name)
    {
        TBoldObjectList people = system.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
        return Enumerable.Range(0, people.Count).Select(i => people[i]!).Single(p => p.BoldMemberByExpressionName("firstName").AsString == name);
    }

    [Test]
    [Covers(typeof(TBoldPersistenceHandleDB), "*")]
    [Covers(typeof(TBoldAbstractPersistenceHandleDB), "CreateDataBaseSchema", "CreatePersistenceController", "SetActive")]
    [Covers(typeof(TBoldDefaultSystemDescription), "*")]
    [Covers(typeof(TBoldSQLSystemDescription), "CreatePersistentStorage", "CleanPersistentStorage")]
    [Covers(typeof(TBoldDefaultMappingInfo), "*")]
    public void Creating_the_schema_makes_the_system_and_class_tables()
    {
        using var s = new ObjectSpaceSample();
        using var db = new SqliteHandle(s.Model);
        var tables = new TStringList();
        db.Connection.GetTableNames(tables);
        foreach (string table in new[] { "BOLD_ID", "BOLD_TYPE", "BOLD_TABLES", "BOLD_MEMBERMAPPING", "BOLD_R_CLSMAP", "BOLD_W_CLSMAP", "BOLD_XFILES", "BOLD_TIMESTAMP", "TownRoot", "Person", "Building", "Friendship" })
        {
            Assert.That(tables.IndexOf(table), Is.Not.EqualTo(-1), table);
        }

        Assert.That(TestDatabase.Scalar(db.Connection, "SELECT BOLD_ID FROM BOLD_ID"), Is.EqualTo(1));
        Assert.That(Convert.ToInt32(TestDatabase.Scalar(db.Connection, "SELECT COUNT(*) FROM BOLD_TYPE"), null), Is.EqualTo(s.Model.Classes.Count));
        Assert.That(TestDatabase.Scalar(db.Connection, "SELECT COUNT(*) FROM BOLD_MEMBERMAPPING WHERE CLASSNAME = 'Person' AND MEMBERNAME = 'firstName'"), Is.EqualTo(1));
    }

    [Test]
    [Covers(typeof(TBoldPersistenceControllerDefault), "*")]
    [Covers(typeof(TBoldSystemDefaultMapper), "*")]
    [Covers(typeof(TBoldObjectDefaultMapper), "*")]
    [Covers(typeof(TBoldPMString), "*")]
    [Covers(typeof(TBoldPMInteger), "*")]
    [Covers(typeof(TBoldEmbeddedSingleLinkDefaultMapper), "*")]
    [Covers(typeof(TBoldDirectMultiLinkDefaultmapper), "*")]
    [Covers(typeof(TBoldIndirectMultiLinkDefaultmapper), "*")]
    public void Objects_saved_to_SQLite_load_into_a_new_system()
    {
        SqliteHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqliteHandle(m);
            return db.OpenController();
        });
        using SqliteHandle database = db!;
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
        s.System.UpdateDatabase();
        Assert.That(s.System.BoldDirty, Is.False);
        Assert.That(TestDatabase.Scalar(database.Connection, "SELECT COUNT(*) FROM Person"), Is.EqualTo(2));

        TBoldSystem second = s.NewSystem(database.Handle.PersistenceController);
        TBoldObject ann2 = PersonNamed(second, s, "Ann");
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

        // A change and a delete in the second system are written back.
        Member<TBAInteger>(ann2, "age").AsInteger = 43;
        PersonNamed(second, s, "Bob").Delete();
        second.UpdateDatabase();
        TBoldSystem third = s.NewSystem(database.Handle.PersistenceController);
        Assert.That(Member<TBAInteger>(PersonNamed(third, s, "Ann"), "age").AsInteger, Is.EqualTo(43));
        Assert.That(third.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!.Count, Is.EqualTo(1));
    }

    private static List<string> Names(TBoldObjectList list) =>
        Enumerable.Range(0, list.Count).Select(i => list[i]!.BoldMemberByExpressionName("firstName").AsString).Order().ToList();

    [Test]
    [Covers(typeof(TBoldSqlNodeMaker), "*")]
    [Covers(typeof(TBoldSqlNodeResolver), "*")]
    [Covers(typeof(TBoldSqlQueryGenerator), "*")]
    [Covers(typeof(TBoldSQLQuery), "*")]
    [Covers(typeof(TBSS_Select), "*")]
    [Covers(typeof(TBSS_BinarySymbol), "*")]
    [Covers(typeof(TBSS_Exists), "*")]
    [Covers(typeof(TBSS_NotEmpty), "*")]
    [Covers(typeof(TBSS_SQLLike), "*")]
    public void OCL_conditions_are_evaluated_in_SQL()
    {
        SqliteHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqliteHandle(m);
            return db.OpenController();
        });
        using SqliteHandle database = db!;
        string[] names = ["Ann", "Bob", "Cid", "Dot"];
        int[] ages = [42, 17, 30, 65];
        var people = new List<TBoldObject>();
        for (int i = 0; i < names.Length; i++)
        {
            TBoldObject p = s.NewPerson();
            p.BoldMemberByExpressionName("firstName").AsString = names[i];
            Member<TBAInteger>(p, "age").AsInteger = ages[i];
            people.Add(p);
        }

        TBoldObject house = s.NewBuilding();
        house.BoldMemberByExpressionName("address").AsString = "1 Main St";
        Member<TBoldObjectReference>(people[0], "home").BoldObject = house;
        Member<TBoldObjectReference>(people[2], "home").BoldObject = house;
        Member<TBoldObjectList>(people[0], "friends").Add(people[3]);
        s.System.UpdateDatabase();

        TBoldSystem second = s.NewSystem(database.Handle.PersistenceController);
        List<string> Query(string ocl)
        {
            var result = new TBoldIndirectElement();
            second.EvaluateExpression(ocl, result, true);
            return Names((TBoldObjectList)result.Value!);
        }

        Assert.That(Query("Person.allInstances->select(p | p.age > 26)"), Is.EqualTo(new[] { "Ann", "Cid", "Dot" }));
        Assert.That(Query("Person.allInstances->select(p | (p.age >= 30) and (p.age < 60))"), Is.EqualTo(new[] { "Ann", "Cid" }));
        Assert.That(Query("Person.allInstances->select(p | p.firstName = 'Bob')"), Is.EqualTo(new[] { "Bob" }));
        Assert.That(Query("Person.allInstances->select(p | p.firstName.sqlLike('%o%'))"), Is.EqualTo(new[] { "Bob", "Dot" }));
        Assert.That(Query("Person.allInstances->select(p | p.home->notEmpty)"), Is.EqualTo(new[] { "Ann", "Cid" }));
        Assert.That(Query("Person.allInstances->select(p | p.home.address = '1 Main St')"), Is.EqualTo(new[] { "Ann", "Cid" }));
        Assert.That(Query("Person.allInstances->select(p | p.friends->exists(f | f.age > 60))"), Is.EqualTo(new[] { "Ann" }));
        Assert.That(Query("Person.allInstances->reject(p | p.age > 26)"), Is.EqualTo(new[] { "Bob" }));
    }

    [Test]
    [Covers(typeof(TBoldObjectDefaultMapper), "PMFetchWithCondition")]
    public void SQL_conditions_fetch_the_matching_objects()
    {
        SqliteHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqliteHandle(m);
            return db.OpenController();
        });
        using SqliteHandle database = db!;
        foreach ((string name, int age) in new[] { ("Ann", 42), ("Bob", 17) })
        {
            TBoldObject p = s.NewPerson();
            p.BoldMemberByExpressionName("firstName").AsString = name;
            Member<TBAInteger>(p, "age").AsInteger = age;
        }

        s.System.UpdateDatabase();
        TBoldSystem second = s.NewSystem(database.Handle.PersistenceController);
        var list = new TBoldObjectList();
        var condition = new TBoldSQLCondition { TopSortedIndex = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex, WhereFragment = "age < 20" };
        second.GetAllWithCondition(list, condition);
        Assert.That(Names(list), Is.EqualTo(new[] { "Bob" }));
        Assert.That(condition.AvailableAnswers, Is.EqualTo(1));
    }
}
