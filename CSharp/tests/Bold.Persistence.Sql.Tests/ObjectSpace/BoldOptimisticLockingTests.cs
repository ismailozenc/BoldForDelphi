// Delphi: Source/ObjectSpace/BORepresentation/BoldOptimisticLockingSupport.pas
// Port map: CSharp/port/units/BoldOptimisticLockingSupport.md
// Two systems on one SQLite database change the same object; the optimistic locking mode decides whether the second
// update is refused.
namespace Bold.Tests;

public class BoldOptimisticLockingTests
{
    /// <summary>
    /// Ann is saved; systems A and B load her; A changes her first name and saves; then B changes
    /// <paramref name="memberB"/> and saves. Returns whether B's save was refused, and Ann's first name and age in
    /// the database afterwards.
    /// </summary>
    private static (bool refused, string firstName, string age) Race(string mode, string memberB)
    {
        using var s = new ObjectSpaceSample(m => m.SetBoldTV(BoldDefaultTaggedValues.TAG_OPTIMISTICLOCKING, mode));
        using var db = new SqliteHandle(s.Model);
        TBoldPersistenceController controller = db.OpenController();
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
        bool refused = false;
        try
        {
            b.UpdateDatabase();
        }
        catch (EBoldOperationFailedForObjectList)
        {
            refused = true;
            b.Discard();
        }

        TBoldObject check = Load(s.NewSystem(controller));
        return (refused, check.BoldMemberByExpressionName("firstName").AsString, check.BoldMemberByExpressionName("age").AsString);
    }

    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_OFF, "firstName", false, "Annie", "30", TestName = "Off: the last save wins")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS, "firstName", true, "Anna", "30", TestName = "ModifiedMembers: the same member changed is refused")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS, "age", false, "Anna", "31", TestName = "ModifiedMembers: another member changed is saved")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_ALLMEMBERS, "age", true, "Anna", "30", TestName = "AllMembers: any changed member of the object is refused")]
    [TestCase(BoldDefaultTaggedValues.TV_OPTIMISTICLOCKING_TIMESTAMP, "age", true, "Anna", "30", TestName = "TimeStamp: any later save of the object is refused")]
    [Covers(typeof(TBoldOptimisticLockHandler), "*")]
    [Covers(typeof(TBoldUpdatePrecondition), "*")]
    [Covers(typeof(TBoldOptimisticLockingPrecondition), "*")]
    [Covers(typeof(TBoldOldValueHandler), "*")]
    public void The_second_save_follows_the_locking_mode(string mode, string memberB, bool refused, string firstName, string age)
    {
        Assert.That(Race(mode, memberB), Is.EqualTo((refused, firstName, age)));
    }
}
