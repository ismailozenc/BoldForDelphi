// Delphi: Source/Persistence/Core/PersistenceConsts.pas, Source/PMapper/Core/BoldPMConsts.pas
// Port map: CSharp/port/units/PersistenceConsts.md, CSharp/port/units/BoldPMConsts.md
namespace Bold.Tests;

public class PersistenceConstsTests
{
    [Test]
    [Covers(typeof(PersistenceConsts), "*")]
    [Covers(typeof(BoldPMConsts), "*")]
    public void Messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(PersistenceConsts), "Source/Persistence/Core/PersistenceConsts.pas");
        PortAssert.ConstantsMatchDelphi(typeof(BoldPMConsts), "Source/PMapper/Core/BoldPMConsts.pas");
    }
}
