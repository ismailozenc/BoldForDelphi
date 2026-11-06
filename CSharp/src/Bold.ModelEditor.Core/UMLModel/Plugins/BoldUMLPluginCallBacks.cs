// Delphi: Source/UMLModel/Plugins/BoldUMLPluginCallBacks.pas
// Port map: CSharp/port/units/BoldUMLPluginCallBacks.md

namespace Bold;

/// <summary>Validates the OCL of a model, for the validation form.</summary>
[Delphi(PortStatus.Ported)]
public static class TUMLOCLValidatorCallBack
{
    [Delphi(PortStatus.Ported)]
    public static void Validate(TBoldModel Modelhandle) => new TOCLValidityChecker(Modelhandle.TypeNameDictionary).ValidateModel(Modelhandle.EnsuredUMLModel);
}

/// <summary>Validates a model, for the validation form.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldUMLModelValidatorCallBack
{
    [Delphi(PortStatus.Ported)]
    public static void Validate(TBoldModel Modelhandle)
    {
        using var modelValidator = new TBoldUMLModelValidator(Modelhandle.EnsuredUMLModel, BoldUMLUtils.SQLDataBaseConfigforModel(Modelhandle));
        modelValidator.Validate(Modelhandle.TypeNameDictionary);
    }
}
