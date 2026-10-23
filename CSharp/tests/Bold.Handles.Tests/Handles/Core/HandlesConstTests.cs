// Delphi: Source/Handles/Core/HandlesConst.pas
// Port map: CSharp/port/units/HandlesConst.md
namespace Bold.Tests;

public class HandlesConstTests
{
    [Test]
    [Covers(typeof(HandlesConst), "*")]
    [Covers(typeof(BoldXMLProducers), "DEFAULT_XMLELEMENT_BOLDIDNAME")]
    public void The_messages_are_the_Delphi_messages()
    {
        PortAssert.ConstantsMatchDelphi(typeof(HandlesConst), "Source/Handles/Core/HandlesConst.pas");
        PortAssert.ConstantsMatchDelphi(typeof(BoldXMLProducers), "Source/Handles/XML/BoldXMLProducers.pas", "xpoDefault");
    }
}
