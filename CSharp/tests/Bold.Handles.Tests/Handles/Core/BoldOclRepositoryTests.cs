// Delphi: Source/Handles/Core/BoldOclRepository.pas
// Port map: CSharp/port/units/BoldOclRepository.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldOclRepositoryTests
{
    [Test]
    [Covers(typeof(TBoldOclRepository), "*")]
    [Covers(typeof(TBoldOclDefinitions), "*")]
    [Covers(typeof(TBoldOclDefinition), "*")]
    public void Expressions_use_the_repositorys_definitions_by_name()
    {
        using var s = new HandlesSample();
        var repository = new TBoldOclRepository(s.Components) { Name = "Repository" };
        var rich = (TBoldOclDefinition)repository.OclDefinitions.Add();
        Assert.That(rich.Name, Is.EqualTo("Ocl1"));
        rich.Name = "rich";
        rich.Context = "Person";
        rich.Expression = "assets > 1000";
        var people = (TBoldOclDefinition)repository.OclDefinitions.Add();
        people.Name = "people";
        people.Expression = "Person.allInstances->select(assets > 500)";
        Assert.That(() => people.Name = "rich", Throws.InstanceOf<EBold>().With.Message.Contains("Not Unique"));
        Assert.That(repository.OclDefinitions.GetUniqueName(), Is.EqualTo("Ocl1"));
        Assert.That(repository.OclDefinitions[1], Is.SameAs(people));
        Assert.That(rich.Definitions, Is.SameAs(repository.OclDefinitions));
        Assert.That(rich.DisplayName, Is.EqualTo("%rich: assets > 1000"));
        Assert.That(rich.SystemTypeInfo, Is.Null, "no system handle yet");
        Assert.That(rich.GetContextType(), Is.Null);

        repository.SystemHandle = s.SystemHandle;
        Assert.That(repository.LookUpOclDefinition("rich"), Is.EqualTo("assets > 1000"));
        Assert.That(repository.LookUpOclDefinition("Rich"), Is.EqualTo(""), "names are case sensitive");
        Assert.That(rich.GetContextType()!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(((IBoldOCLComponent)rich).ContextType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(((IBoldOCLComponent)rich).GetVariableList(), Is.Null);
        ((IBoldOCLComponent)rich).Expression = "assets > 500";
        Assert.That(((IBoldOCLComponent)rich).Expression, Is.EqualTo("assets > 500"));

        s.NewPerson("Ann").Assets = 600;
        s.NewPerson("Bob").Assets = 0;
        var list = new TBoldListHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "%people" };
        Assert.That(list.Count, Is.EqualTo(1), "a definition is looked up when an expression is just its name");
        Assert.That(list.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));

        repository.SystemHandle = null;
        Assert.That(() => s.System.EvaluateExpressionAsDirectElement("%people"), Throws.InstanceOf<EBold>().With.Message.Contains("no repository installed"), "no repository, no definitions");
        repository.OclDefinitions = repository.OclDefinitions;
    }

    [Test]
    public void The_repository_validates_its_definitions()
    {
        using var s = new HandlesSample();
        using var environment = new TestEnvironment();
        using var log = new LogCapture();
        var repository = new TBoldOclRepository(s.Components) { Name = "Repository" };
        var validator = new TBoldComponentValidator();
        Assert.That(validator.ValidateComponent(repository), Is.True, "no system handle is only logged");
        Assert.That(log.Text, Does.Contain("has no SystemHandle"));

        repository.SystemHandle = new TBoldSystemHandle(s.Components);
        Assert.That(validator.ValidateComponent(repository), Is.True);
        Assert.That(log.Text, Does.Contain("has no TypeInfo"));

        repository.SystemHandle = s.SystemHandle;
        var good = (TBoldOclDefinition)repository.OclDefinitions.Add();
        good.Context = "Person";
        good.Expression = "firstName";
        Assert.That(validator.ValidateComponent(repository), Is.True);
        var bad = (TBoldOclDefinition)repository.OclDefinitions.Add();
        bad.Context = "Person";
        bad.Expression = "noSuchMember";
        Assert.That(validator.ValidateComponent(repository), Is.False);
        Assert.That(log.Text, Does.Contain("Repository.Ocl2"));
    }
}
