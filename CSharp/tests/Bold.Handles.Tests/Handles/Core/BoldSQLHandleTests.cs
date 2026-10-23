// Delphi: Source/Handles/Core/BoldSQLHandle.pas
// Port map: CSharp/port/units/BoldSQLHandle.md
namespace Bold.Tests;

public class BoldSQLHandleTests
{
    [Test]
    [Covers(typeof(TBoldSQLHandle), "*")]
    public void The_SQL_handle_has_a_typed_list_and_needs_an_active_system_to_run()
    {
        using var s = new HandlesSample(activate: false);
        var sql = new TBoldSQLHandle(s.Components) { Name = "Query" };
        Assert.That(sql.ClearBeforeExecute, Is.True);
        Assert.That(sql.ListMode, Is.EqualTo(TBoldListDupMode.bldmMerge));
        Assert.That(sql.MaxAnswers, Is.EqualTo(-1));
        Assert.That(sql.Offset, Is.EqualTo(-1));
        Assert.That(sql.JoinInheritedTables, Is.True);
        Assert.That(sql.Value, Is.Null, "no class yet");
        Assert.That(() => sql.ExecuteSQL(), Throws.InstanceOf<EBold>().With.Message.Contains("has no SystemHandle"));

        sql.StaticSystemHandle = s.SystemHandle;
        Assert.That(() => sql.ExecuteSQL(), Throws.InstanceOf<EBold>().With.Message.Contains("not active"));
        s.SystemHandle.Active = true;
        Assert.That(() => sql.ExecuteSQL(), Throws.InstanceOf<EBold>().With.Message.Contains("can't find valid type"));

        Func<int> changes = HandlesSample.CountIdentityChanges(sql);
        sql.ClassExpressionName = "Person";
        Assert.That(changes(), Is.EqualTo(1));
        Assert.That(sql.ClassExpressionName, Is.EqualTo("Person"));
        Assert.That(sql.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(sql.Value, Is.InstanceOf<BuildingClasses.TPersonList>(), "a list of the class's own list class");
        Assert.That(((TBoldObjectList)sql.Value!).DuplicateMode, Is.EqualTo(TBoldListDupMode.bldmMerge));
        sql.ListMode = TBoldListDupMode.bldmAllow;
        Assert.That(((TBoldObjectList)sql.Value!).DuplicateMode, Is.EqualTo(TBoldListDupMode.bldmAllow));

        sql.SQLWhereClause = "FIRSTNAME = :name";
        sql.SQLOrderByClause = "LASTNAME";
        sql.Params = new TParams();
        sql.ClearBeforeExecute = false;
        sql.MaxAnswers = 10;
        sql.Offset = 0;
        sql.JoinInheritedTables = false;
        Assert.That(sql.SQLWhereClause, Is.EqualTo("FIRSTNAME = :name"));
        Assert.That(sql.SQLOrderByClause, Is.EqualTo("LASTNAME"));
        Assert.That(sql.Params.Count, Is.EqualTo(0));
        ((TBoldObjectList)sql.Value!).Add(new BuildingClasses.TPerson(s.System));
        sql.ClearList();
        Assert.That(((TBoldObjectList)sql.Value!).Count, Is.EqualTo(0));
        s.System.Discard();
    }
}
