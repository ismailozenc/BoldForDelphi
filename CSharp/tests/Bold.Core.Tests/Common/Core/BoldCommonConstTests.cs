// Delphi: Source/Common/Core/BoldCommonConst.pas
// Port map: CSharp/port/units/BoldCommonConst.md
namespace Bold.Tests;

public class BoldCommonConstTests
{
    [Test]
    [Covers(typeof(BoldCommonConst), "*")]
    public void Resource_strings_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldCommonConst), "Source/Common/Core/BoldCommonConst.pas");
    }
}
