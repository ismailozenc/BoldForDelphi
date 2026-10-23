// Delphi: Source/ObjectSpace/UtilsGUI/BoldComponentValidator.pas
// Port map: CSharp/port/units/BoldComponentValidator.md
using System.ComponentModel;

namespace Bold.Tests;

[NonParallelizable]
public class BoldComponentValidatorTests
{
    [Test]
    [Covers(typeof(TBoldComponentValidator), "*")]
    [Covers(typeof(IBoldValidateableComponent), "*")]
    public void The_validator_checks_the_OCL_of_components_and_logs_the_errors()
    {
        using var s = new HandlesSample();
        using var environment = new TestEnvironment { Root = s.Components };
        using var log = new LogCapture();
        var good = new TBoldListHandle(s.Components) { Name = "Good", RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        var bad = new TBoldExpressionHandle(s.Components) { Name = "Bad", RootHandle = s.SystemHandle, Expression = "Person.allInstances.noSuchMember" };
        var validator = new Validator();

        Assert.That(validator.ValidateComponent(good), Is.True);
        Assert.That(validator.ValidateComponent(bad, "Form."), Is.False);
        Assert.That(log.Text, Does.Contain("*** Error in Form.Bad"));
        Assert.That(log.Text, Does.Contain("*** Expression: Person.allInstances.noSuchMember"));

        log.Lines.Clear();
        Assert.That(validator.ValidateExpressionInContext("firstName", null, "NoContext"), Is.False);
        Assert.That(log.Text, Does.Contain("*** No context for NoContext"));

        log.Lines.Clear();
        Assert.That(validator.ValidateComponent(s.Components, "", true), Is.False, "a container is checked with what it holds");
        Assert.That(log.Calls, Does.Contain("header Validating Container"));
        Assert.That(log.Text, Does.Contain("Bad"));

        log.Lines.Clear();
        validator.ValidateApplication();
        Assert.That(log.Text, Does.Contain("Bad"), "the application's root component is checked");

        validator.Log();
        Assert.That(log.Calls, Does.Contain("start " + BoldCoreConsts.sComponentValidation));
        Assert.That(log.Calls, Does.Contain("end"));
    }

    private sealed class Validator : TBoldComponentValidator
    {
        public void Log()
        {
            InitializeLog();
            CompleteLog();
        }
    }
}
