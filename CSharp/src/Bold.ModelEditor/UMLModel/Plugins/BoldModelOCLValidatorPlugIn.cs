// Delphi: Source/UMLModel/Plugins/BoldModelOCLValidatorPlugIn.pas
// Port map: CSharp/port/units/BoldModelOCLValidatorPlugIn.md

using System.Drawing;

namespace Bold;

/// <summary>The tool that validates the OCL of the model, in the validation form.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLOCLValidator(bool InstallInEditor) : TUMLPlugInFunction(InstallInEditor)
{
    private static TUMLOCLValidator? _UMLOCLValidator;

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        TfrmValidation frmValidation = TfrmValidation.EnsureValidationForm(context.GetCurrentModelHandle(), TfrmValidation.G_ValidationFormDefaultOwner, null);
        frmValidation.ValidationProc = TUMLOCLValidatorCallBack.Validate;
        frmValidation.Validate();
        frmValidation.Show();
    }

    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;

    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => GetType().Name;

    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Validate OCL in model";

    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified);

    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptTool;

    /// <summary>Installs the plug-in (once), as the unit's initialization did.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    public static void Initialize() => _UMLOCLValidator ??= new TUMLOCLValidator(true);

    [Delphi(PortStatus.Ported, Symbol = "finalization")]
    public static void Finalize_()
    {
        _UMLOCLValidator?.Dispose();
        _UMLOCLValidator = null;
    }
}
