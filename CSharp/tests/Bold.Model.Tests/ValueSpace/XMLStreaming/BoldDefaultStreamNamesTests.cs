// Delphi: Source/ValueSpace/XMLStreaming/BoldDefaultStreamNames.pas
// Port map: CSharp/port/units/BoldDefaultStreamNames.md
namespace Bold.Tests;

public class BoldDefaultStreamNamesTests
{
    [Test]
    [Covers(typeof(BoldDefaultStreamNames), "*")]
    public void Stream_names_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldDefaultStreamNames), "Source/ValueSpace/XMLStreaming/BoldDefaultStreamNames.pas");
    }
}
