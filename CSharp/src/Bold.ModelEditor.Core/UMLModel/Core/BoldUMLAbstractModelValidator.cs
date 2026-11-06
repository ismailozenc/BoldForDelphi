// Delphi: Source/UMLModel/Core/BoldUMLAbstractModelValidator.pas
// Port map: CSharp/port/units/BoldUMLAbstractModelValidator.md
// The model is the UML graph the editor edits; its violations are kept in the graph, as Delphi's UML model kept them.

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldModelValidatorSourceLanguage { mvslNone, mvslDelphi, mvslCpp }

/// <summary>Base of the model validators: the model, the database configuration and the language to check names for.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLAbstractModelValidator
{
    /// <summary>The language names are checked for by default: Delphi, as in Delphi Bold's Delphi edition.</summary>
    [Delphi(PortStatus.Ported, Symbol = "BoldDefaultValidatorSourceLanguage")]
    public const TBoldModelValidatorSourceLanguage BoldDefaultValidatorSourceLanguage = TBoldModelValidatorSourceLanguage.mvslDelphi;

    private readonly TBoldSQLDataBaseConfig? fSQLDataBaseConfig;
    private readonly TBoldModelValidatorSourceLanguage fLanguage;

    [Delphi(PortStatus.Ported, Note = "Takes the UML graph's model (D6, as amended for the editor).")]
    public TBoldUMLAbstractModelValidator(TBoldUMLObject UMLModel, TBoldSQLDataBaseConfig? SQLDataBaseConfig = null,
        TBoldModelValidatorSourceLanguage Language = TBoldModelValidatorSourceLanguage.mvslNone)
    {
        fSQLDataBaseConfig = SQLDataBaseConfig;
        this.UMLModel = UMLModel;
        fLanguage = Language;
    }

    [Delphi(PortStatus.Ported)]
    private List<TBoldUMLViolation> GetValidator() => UMLModel.Graph.Violations;

    [Delphi(PortStatus.Ported)]
    private void AddViolation(TSeverity severity, string description, TBoldUMLObject? element) =>
        Validator.Add(new TBoldUMLViolation(severity, description, element));

    [Delphi(PortStatus.Ported)]
    protected void AddError(string description, object?[] args, TBoldUMLObject? element) => AddViolation(TSeverity.sError, SysUtils.Format(description, args), element);

    [Delphi(PortStatus.Ported)]
    protected void AddHint(string description, object?[] args, TBoldUMLObject? element) => AddViolation(TSeverity.sHint, SysUtils.Format(description, args), element);

    [Delphi(PortStatus.Ported)]
    protected void AddWarning(string description, object?[] args, TBoldUMLObject? element) => AddViolation(TSeverity.sWarning, SysUtils.Format(description, args), element);

    [Delphi(PortStatus.Ported)]
    protected void ClearViolations() => Validator.Clear();

    [Delphi(PortStatus.Ported)] protected TBoldSQLDataBaseConfig? SQLDataBaseConfig => fSQLDataBaseConfig;
    [Delphi(PortStatus.Ported)] protected TBoldModelValidatorSourceLanguage Language => fLanguage;
    [Delphi(PortStatus.Ported)] public TBoldUMLObject UMLModel { get; }
    [Delphi(PortStatus.Ported)] public List<TBoldUMLViolation> Validator => GetValidator();

    /// <summary>The worst severity among the violations.</summary>
    [Delphi(PortStatus.Ported)]
    public TSeverity HighestSeverity() => Validator.Count == 0 ? TSeverity.sNone : Validator.Max(v => v.Severity);
}
