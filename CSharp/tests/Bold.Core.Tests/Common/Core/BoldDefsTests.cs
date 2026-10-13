// Delphi: Source/Common/Core/BoldDefs.pas
// Port map: CSharp/port/units/BoldDefs.md
namespace Bold.Tests;

/// <summary>
/// BoldDefs holds Bold's shared constants, enums, event types and exceptions. Expected values come from
/// Source/Common/Core/BoldDefs.pas itself, read by the port tool's parser, not from copies typed into the tests.
/// </summary>
public class BoldDefsTests
{
    private const string Delphi = "Source/Common/Core/BoldDefs.pas";

    [Test]
    [Covers(typeof(BoldDefs), "*")]
    public void Constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldDefs), Delphi);
    }

    [Test]
    [Covers(typeof(TBoldRoleType))]
    [Covers(typeof(TBoldDataBaseGenerationMode))]
    [Covers(typeof(TBoldStorage))]
    [Covers(typeof(TBoldCompareType))]
    [Covers(typeof(TBoldOrientation))]
    [Covers(typeof(TBoldSQLStyle))]
    [Covers(typeof(TBoldAbstractionLevel))]
    [Covers(typeof(TBoldValuePersistenceState))]
    [Covers(typeof(TBoldLogType))]
    [Covers(typeof(TBoldModuleType))]
    public void Enums_have_the_Delphi_members_in_order()
    {
        PortAssert.EnumsMatchDelphi(Delphi,
            typeof(TBoldRoleType), typeof(TBoldDataBaseGenerationMode), typeof(TBoldStorage), typeof(TBoldCompareType),
            typeof(TBoldOrientation), typeof(TBoldSQLStyle), typeof(TBoldAbstractionLevel), typeof(TBoldValuePersistenceState),
            typeof(TBoldLogType), typeof(TBoldModuleType));
    }

    [Test]
    [Covers(typeof(TBoldBooleanFunction))]
    [Covers(typeof(TBoldGetTimeEvent))]
    [Covers(typeof(TBoldJustNotifyEvent))]
    [Covers(typeof(TBoldNotifyEventWithErrorMessage))]
    [Covers(typeof(TBoldLockManagerProgressEvent))]
    public void Event_types_have_the_Delphi_parameters()
    {
        PortAssert.DelegatesMatchDelphi(Delphi,
            typeof(TBoldBooleanFunction), typeof(TBoldGetTimeEvent), typeof(TBoldJustNotifyEvent),
            typeof(TBoldNotifyEventWithErrorMessage), typeof(TBoldLockManagerProgressEvent));
        Assert.That(typeof(TBoldGetTimeEvent).GetMethod("Invoke")!.ReturnType, Is.EqualTo(typeof(DateTime)), "TDateTime becomes DateTime");
    }

    private static readonly Type[] Exceptions =
    [
        typeof(EBold), typeof(EBoldDesignTime), typeof(EBoldImport), typeof(EBoldBadRepresentation), typeof(EBoldInternal),
        typeof(EBoldFeatureNotImplementedYet), typeof(EBoldBadColumnIndex), typeof(EBoldAssertionFailed), typeof(EBoldXMLLoadError),
        typeof(EBoldXMLIncorrectXPath), typeof(EBoldEnsureDatabaseLockError), typeof(EBoldLockManagerError), typeof(EBoldLicenseError),
    ];

    [Test]
    [Covers(typeof(EBold))]
    [Covers(typeof(EBoldDesignTime))]
    [Covers(typeof(EBoldImport))]
    [Covers(typeof(EBoldBadRepresentation))]
    [Covers(typeof(EBoldInternal))]
    [Covers(typeof(EBoldFeatureNotImplementedYet))]
    [Covers(typeof(EBoldBadColumnIndex))]
    [Covers(typeof(EBoldAssertionFailed))]
    [Covers(typeof(EBoldXMLLoadError))]
    [Covers(typeof(EBoldXMLIncorrectXPath))]
    [Covers(typeof(EBoldEnsureDatabaseLockError))]
    [Covers(typeof(EBoldLockManagerError))]
    [Covers(typeof(EBoldLicenseError))]
    public void Exceptions_derive_as_in_Delphi_and_carry_message_and_cause()
    {
        PortAssert.ClassParentsMatchDelphi(Delphi, Exceptions);
        var cause = new InvalidOperationException("cause");
        foreach (var type in Exceptions)
        {
            var exception = (Exception)Activator.CreateInstance(type, "message", cause)!;
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Is.EqualTo("message"), type.Name);
                Assert.That(exception.InnerException, Is.SameAs(cause), type.Name);
                Assert.That(exception, Is.InstanceOf<EBold>(), $"{type.Name} is caught by 'on E: EBold'");
            });
        }
    }

    [Test]
    [Covers("BoldDefs", "TBoldTimeStampType", "TBoldClientID", "TBoldDbType", "TBoldRepresentation", "TBoldExpression",
        "TBoldByteSet", "TBoldValuePersistenceStateSet")]
    public void Type_aliases_keep_the_Delphi_names_for_the_Delphi_types()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DelphiSource.Symbol(Delphi, "TBoldTimeStampType").TypeText, Is.EqualTo("integer"));
            Assert.That(typeof(TBoldTimeStampType), Is.EqualTo(typeof(int)));
            Assert.That(typeof(TBoldClientID), Is.EqualTo(typeof(int)));
            Assert.That(typeof(TBoldDbType), Is.EqualTo(typeof(int)));
            Assert.That(typeof(TBoldRepresentation), Is.EqualTo(typeof(int)));
            Assert.That(typeof(TBoldExpression), Is.EqualTo(typeof(string)));
            Assert.That(typeof(TBoldByteSet), Is.EqualTo(typeof(DelphiSet<byte>)));
            Assert.That(typeof(TBoldValuePersistenceStateSet), Is.EqualTo(typeof(DelphiSet<TBoldValuePersistenceState>)));
        });

        TBoldValuePersistenceStateSet states = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsModified);
        states += TBoldValuePersistenceState.bvpsInvalid;
        Assert.That(states.ToString(), Is.EqualTo("[bvpsModified, bvpsInvalid]"));
    }
}
