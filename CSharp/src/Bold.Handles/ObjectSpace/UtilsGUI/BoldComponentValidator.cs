// Delphi: Source/ObjectSpace/UtilsGUI/BoldComponentValidator.pas
// Port map: CSharp/port/units/BoldComponentValidator.md

using System.ComponentModel;
using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>A component that checks itself (its OCL expressions, say) when the component validator asks.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldValidateableComponent
{
    [Delphi(PortStatus.Ported)]
    bool ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix);
}

/// <summary>
/// Checks the OCL of components: an OCL component's expression must have a type in its context, and a validateable
/// component checks itself. Errors go to the Bold log.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldComponentValidator
{
    /// <summary>Checks the component (and, when asked, the components it holds); true when all is well.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes any object where Delphi takes a TPersistent; the components a component holds are those of an IContainer.")]
    public bool ValidateComponent(object Component, string NamePrefix = "", bool TraverseSubComponents = false)
    {
        if (BoldEnvironment.BoldEffectiveEnvironment().IsFormOrDataModule(Component))
        {
            BoldLogHandler.BoldLog().LogHeader = SysUtils.Format(sValidatingHeader, NamePathOf(Component));
        }

        bool result = true;
        if (Component is IBoldOCLComponent OCLComponent)
        {
            result = result && ValidateOCLComponent(OCLComponent, NamePrefix + NamePathOf(Component));
        }

        if (Component is IBoldValidateableComponent ValidateableComponent)
        {
            result = result && ValidateValidateableComponent(ValidateableComponent, NamePrefix);
        }

        if (TraverseSubComponents)
        {
            foreach (object subComponent in SubComponents(Component))
            {
                result = result && ValidateComponent(subComponent, NamePrefix + NamePathOf(Component) + ".", TraverseSubComponents);
            }
        }

        return result;
    }

    /// <summary>True when the expression has a type in the context; logs the error and the expression otherwise.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ValidateExpressionInContext(string Expression, TBoldElementTypeInfo? Context, string Name)
    {
        bool result = true;
        if (Context is null)
        {
            result = false;
            BoldLogHandler.BoldLog().LogFmt(sNoContext, [Name]);
        }
        else
        {
            try
            {
                Context.Evaluator!.ExpressionType(Expression, Context, true);
            }
            catch (Exception e)
            {
                result = false;
                BoldLogHandler.BoldLog().LogFmt(sValidationError, [Name, e.Message]);
            }
        }

        if (!result)
        {
            BoldLogHandler.BoldLog().LogFmt(sValidationExpression, [Expression]);
        }

        return result;
    }

    /// <summary>Checks every component of the application's root component.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValidateApplication()
    {
        BoldLogHandler.BoldLog().Show();
        object? RootComponent = BoldEnvironment.BoldEffectiveEnvironment().RootComponent;
        if (RootComponent is not null)
        {
            foreach (object component in SubComponents(RootComponent))
            {
                ValidateComponent(component, "", true);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool ValidateOCLComponent(IBoldOCLComponent Component, string Name) =>
        ValidateExpressionInContext(Component.Expression, Component.ContextType, Name);

    [Delphi(PortStatus.Ported)]
    protected bool ValidateValidateableComponent(IBoldValidateableComponent Component, string NamePrefix) =>
        Component.ValidateComponent(this, NamePrefix);

    [Delphi(PortStatus.Ported)]
    protected void InitializeLog()
    {
        BoldLogHandler.BoldLog().Show();
        BoldLogHandler.BoldLog().StartLog(sComponentValidation);
    }

    [Delphi(PortStatus.Ported)]
    protected void CompleteLog()
    {
        BoldLogHandler.BoldLog().EndLog();
        BoldLogHandler.BoldLog().Hide();
        BoldLogHandler.BoldLog().Show();
    }

    /// <summary>The name of a component in messages (Delphi's GetNamePath): its name, or its designer name.</summary>
    private static string NamePathOf(object Component) => Component switch
    {
        TBoldSubscribableComponent boldComponent => boldComponent.Name,
        TPersistent persistent => persistent.GetNamePath(),
        IComponent component => component.Site?.Name ?? component.GetType().Name,
        _ => Component.GetType().Name,
    };

    /// <summary>The components a component holds (Delphi's Components): those of a container.</summary>
    private static IEnumerable<object> SubComponents(object Component) =>
        Component is IContainer container ? container.Components.Cast<object>() : [];
}
