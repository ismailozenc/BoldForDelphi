// Delphi: Source/ObjectSpace/Ocl/BoldOclError.pas
// Port map: CSharp/port/units/BoldOclError.md
namespace Bold.Tests;

public class BoldOclErrorTests
{
    [Test]
    [Covers(typeof(BoldOclError), "*")]
    public void Messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldOclError), "Source/ObjectSpace/Ocl/BoldOclError.pas");
    }

    [Test]
    [Covers(typeof(EBoldOCLError), "*")]
    [Covers(typeof(EBoldOCLAbort), "*")]
    [Covers(typeof(EBoldOCLInternalError))]
    [Covers(typeof(EBoldOclRunTimeError))]
    public void Errors_take_their_position_from_the_message()
    {
        var e = new EBoldOCLError(SysUtils.Format(BoldOclError.boeUndefinedOperation, 7, "foo")) { Ocl = "self.foo" };
        Assert.That(e.Position, Is.EqualTo(0));
        e.FixError();
        Assert.That(e.Position, Is.EqualTo(7));
        Assert.That(e.ErrorPointer(), Is.EqualTo("       ^"));
        e.Position = 2;
        e.FixError();
        Assert.That(e.Position, Is.EqualTo(2), "fixed once");

        var noNumber = new EBoldOCLError("no position here");
        noNumber.FixError();
        Assert.That(noNumber.Position, Is.EqualTo(0));
        Assert.That(noNumber.ErrorFixed, Is.True);

        var abort = new EBoldOCLAbort("12:stop");
        abort.FixError();
        Assert.That(abort.Position, Is.EqualTo(12));
        Assert.That(abort.ErrorPointer(), Has.Length.EqualTo(13));
        Assert.That(abort, Is.InstanceOf<EAbort>());
        var badNumber = new EBoldOCLAbort("x:stop");
        badNumber.FixError();
        Assert.That(badNumber.Position, Is.EqualTo(0));

        Assert.That(new EBoldOCLInternalError("1:x"), Is.InstanceOf<EBoldOCLError>());
        Assert.That(new EBoldOclRunTimeError("1:x"), Is.InstanceOf<EBoldOCLError>());
    }
}
