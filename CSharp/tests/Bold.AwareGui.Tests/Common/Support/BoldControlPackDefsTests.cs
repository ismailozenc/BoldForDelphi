// Delphi: Source/Common/Support/BoldControlPackDefs.pas, Source/Common/Support/BoldNavigatorDefs.pas
// Port map: CSharp/port/units/BoldControlPackDefs.md
using static Bold.BoldControlPackDefs;

namespace Bold.Tests;

public class BoldControlPackDefsTests
{
    [Test]
    [Covers(typeof(BoldControlPackDefs), "*")]
    [Covers(typeof(TBoldApplyPolicy))]
    [Covers(typeof(TBoldDragMode))]
    [Covers(typeof(TBoldDropMode))]
    [Covers(typeof(TBoldPopupDeleteType))]
    [Covers(typeof(TBoldFollowerState))]
    [Covers(typeof(TBoldNilElementMode))]
    public void Constants_states_and_modes_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldControlPackDefs), "Source/Common/Support/BoldControlPackDefs.pas",
            nameof(bfsDisplayable), nameof(bfsOutOfDate), nameof(bfsActive), nameof(bfdNeedResubscribe));
        PortAssert.EnumsMatchDelphi("Source/Common/Support/BoldControlPackDefs.pas",
            typeof(TBoldApplyPolicy), typeof(TBoldDragMode), typeof(TBoldDropMode), typeof(TBoldPopupDeleteType), typeof(TBoldFollowerState), typeof(TBoldNilElementMode));

        Assert.That(bfsDisplayable.Contains(TBoldFollowerState.bfsCurrent) && bfsDisplayable.Contains(TBoldFollowerState.bfsDirty), Is.True);
        Assert.That(bfsDisplayable.Contains(TBoldFollowerState.bfsValueOutOfDate), Is.False);
        Assert.That(bfsOutOfDate.Contains(TBoldFollowerState.bfsValueOutOfDate) && bfsOutOfDate.Contains(TBoldFollowerState.bfsSubscriptionOutOfDate), Is.True);
        Assert.That(bfsActive.Contains(TBoldFollowerState.bfsInactiveValidElement), Is.False);
        Assert.That(bfsActive.Contains(TBoldFollowerState.bfsEmpty), Is.True);
        Assert.That(bfdNeedResubscribe.Contains(TBoldFollowerState.bfsActivating), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldNavigateBtn))]
    [Covers(typeof(TBoldDeleteMode))]
    [Covers(typeof(TBoldNavClick))]
    [Covers("BoldNavigatorDefs", "TBoldButtonSet", "TBoldNavButtonStyle")]
    public void Navigator_buttons_and_delete_modes_have_the_Delphi_values()
    {
        PortAssert.EnumsMatchDelphi("Source/Common/Support/BoldNavigatorDefs.pas", typeof(TBoldNavigateBtn), typeof(TBoldDeleteMode));
        PortAssert.DelegatesMatchDelphi("Source/Common/Support/BoldNavigatorDefs.pas", typeof(TBoldNavClick));
        TBoldButtonSet buttons = TBoldButtonSet.Of(TBoldNavigateBtn.nbFirst, TBoldNavigateBtn.nbLast);
        Assert.That(buttons.Contains(TBoldNavigateBtn.nbLast) && !buttons.Contains(TBoldNavigateBtn.nbNext), Is.True);
        TBoldNavButtonStyle style = TBoldNavButtonStyle.Of(TBoldNavButtonStyleElement.nsAllowTimer);
        Assert.That(style.Contains(TBoldNavButtonStyleElement.nsFocusRect), Is.False);
    }
}
