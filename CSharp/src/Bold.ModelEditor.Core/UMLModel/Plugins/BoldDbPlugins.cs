// Delphi: Source/UMLModel/Plugins/BoldDbPlugins.pas
// Port map: CSharp/port/units/BoldDbPlugins.md

using System.Drawing;

namespace Bold;

/// <summary>A plug-in that works on the database of the model's persistence handle.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TUMLGenericDBPlugin : TUMLPlugInFunction
{
    protected TUMLGenericDBPlugin(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    /// <summary>The persistence handle of the model the user chooses to use.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldAbstractPersistenceHandleDB GetPersistenceHandle(TBoldModel BoldModel)
    {
        TBoldObjectArray? handles = BoldHandle.BoldHandleList;
        for (int i = 0; handles is not null && i < handles.Count; i++)
        {
            if (handles[i] is TBoldAbstractPersistenceHandleDB temp && temp.BoldModel == BoldModel &&
                Dialogs.MessageDlg($"Use Database settings from {temp.Name}?", TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == Controls.mrYes)
            {
                return temp;
            }
        }

        throw new Exception("No persistencehandle found. If your persistencehandle is on a datamodule/form that has not been opened, please open the datamodule/form and try again");
    }

    [Delphi(PortStatus.Ported)] protected override Color GetImageMaskColor() => Color.Teal;
    [Delphi(PortStatus.Ported)] protected override TPlugInType GetPlugInType() => TPlugInType.ptTool;
    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified);

    /// <summary>The persistence handle, once the model has been validated without errors.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractPersistenceHandleDB GetValidPersistenceHandle(IUMLModelPlugInContext Context)
    {
        TBoldModel boldModel = Context.GetCurrentModelHandle() ?? throw new EBold($"{MenuItemName}: Unable to find TBoldModel component for this model");
        TBoldAbstractPersistenceHandleDB result = GetPersistenceHandle(boldModel);
        using var validator = new TBoldUMLModelValidator(boldModel.EnsuredUMLModel, result.SQLDatabaseConfig);
        validator.Validate(boldModel.TypeNameDictionary);
        if (validator.HighestSeverity() == TSeverity.sError)
        {
            throw new EBold($"{MenuItemName}: Errors in model");
        }

        return result;
    }
}

/// <summary>Creates the database schema.</summary>
[Delphi(PortStatus.Ported)]
public class TUMLDBGenerator : TUMLGenericDBPlugin
{
    public TUMLDBGenerator(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Generate Database";
    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginGenDBImage";

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context) => GetValidPersistenceHandle(context).CreateDataBaseSchema();
}

/// <summary>Evolves the database to the model.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLDBEvolutorPlugin : TUMLGenericDBPlugin
{
    public TBoldUMLDBEvolutorPlugin(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    /// <summary>
    /// What evolves the database: the WinForms editor sets Bold.WinForms' evolutor form (TfrmBoldDbEvolutor.EvolveDB);
    /// without it the script is calculated and run.
    /// </summary>
    public static Action<TBoldAbstractPersistenceHandleDB, bool> EvolveDB { get; set; } = (handle, generic) =>
    {
        var evolutor = new TBoldDataBaseEvolutor(handle) { GenericScript = generic };
        evolutor.CalculateScript();
        evolutor.ExecuteScript();
    };

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        TBoldAbstractPersistenceHandleDB pHandle = GetValidPersistenceHandle(context);
        int res = Dialogs.MessageDlg(
            "Generic script? (generic scripts can be used on other databases with the same schema. Answering \"No\" gives you a script that will only apply to this database",
            TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo, TMsgDlgBtn.mbCancel), 0);
        if (res is Controls.mrYes or Controls.mrNo)
        {
            EvolveDB(pHandle, res == Controls.mrYes);
        }
    }

    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginDbEvolutor";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Evolve database";
}

/// <summary>A plug-in that runs a database validator.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldDbValidatorPlugIn : TUMLGenericDBPlugin
{
    protected TBoldDbValidatorPlugIn(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)] protected override TBoldUMLPluginOptions GetOptions() => TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified);

    [Delphi(PortStatus.Ported)]
    protected void ExecuteValidator(TBoldDbValidator Validator, IUMLModelPlugInContext context)
    {
        Validator.PersistenceHandle = GetValidPersistenceHandle(context);
        if (Validator.Execute())
        {
            Dialogs.ShowMessage("Database validated OK");
        }
    }
}

/// <summary>Checks the database's tables and columns against the model.</summary>
[Delphi(PortStatus.Ported)]
public class TBolddbStructureValidatorPlugin : TBoldDbValidatorPlugIn
{
    public TBolddbStructureValidatorPlugin(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        using var validator = new TBoldDbStructureValidator(null);
        ExecuteValidator(validator, context);
    }

    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginStructureValidator";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Validate database structure";
}

/// <summary>Checks the data in the database against the model.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDbDataValidatorPlugin : TBoldDbValidatorPlugIn
{
    public TBoldDbDataValidatorPlugin(bool InstallPlugin)
        : base(InstallPlugin)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Execute(IUMLModelPlugInContext context)
    {
        using var validator = new TBoldDbDataValidator(null);
        ExecuteValidator(validator, context);
    }

    [Delphi(PortStatus.Ported)] protected override string GetImageResourceName() => "UMLPluginDataValidator";
    [Delphi(PortStatus.Ported)] protected override string GetMenuItemName() => "Validate data integrity in database";
}

/// <summary>Variables and initialization of BoldDbPlugins.</summary>
public static class BoldDbPlugins
{
    private static TUMLDBGenerator? _DBGenerator;
    private static TBolddbStructureValidatorPlugin? _dbStructureValidator;
    private static TBoldDbDataValidatorPlugin? _dbDataValidator;
    private static TBoldUMLDBEvolutorPlugin? _DbEvolutor;

    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called when the plug-in list is first asked for.")]
    internal static void Initialize()
    {
        _DBGenerator ??= new TUMLDBGenerator(true);
        _dbStructureValidator ??= new TBolddbStructureValidatorPlugin(true);
        _dbDataValidator ??= new TBoldDbDataValidatorPlugin(true);
        _DbEvolutor ??= new TBoldUMLDBEvolutorPlugin(true);
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "Removes the plug-ins; the process ending does it otherwise.")]
    internal static void Finalize_()
    {
        _DBGenerator?.Dispose();
        _dbStructureValidator?.Dispose();
        _dbDataValidator?.Dispose();
        _DbEvolutor?.Dispose();
        _DBGenerator = null;
        _dbStructureValidator = null;
        _dbDataValidator = null;
        _DbEvolutor = null;
    }
}
