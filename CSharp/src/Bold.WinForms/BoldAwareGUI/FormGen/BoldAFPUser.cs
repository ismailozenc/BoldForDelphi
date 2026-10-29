// Delphi: Source/BoldAwareGUI/FormGen/BoldAFPUser.pas
// Port map: CSharp/port/units/BoldAFPUser.md

using System.Windows.Forms;

namespace Bold;

/// <summary>A form provider that keeps the form it makes and puts no controls of its own on it.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldUserFormProvider : TBoldAutoFormProvider
{
    private Form? fForm;

    protected TBoldUserFormProvider(TBoldElement BoldElement)
        : base(BoldElement)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Form? GetForm() => fForm;

    [Delphi(PortStatus.Ported)]
    protected override void SetForm(Form? Value) => fForm = Value;

    [Delphi(PortStatus.Ported)]
    protected override void EnsureComponents()
    {
    }
}
