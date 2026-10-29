// The data module of the Buildings and Owners example (Delphi: examples/Delphi/Compound/Building/Datamod.pas): the
// model, the system and its persistence on a database, and the renderers, filter and comparer the forms share.
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.Windows.Forms;
using Bold;
using BuildingClasses;

namespace BldOwn;

/// <summary>The data module of the application, as Delphi's global DataModule1.</summary>
public static class Datamod
{
    /// <summary>The application's data module (the program makes it before the main form).</summary>
    public static TDataModule1 DataModule1 { get; set; } = null!;
}

/// <summary>
/// The model and the system (inactive until opened) with its persistence on the connection's database, and the
/// renderers, filter and comparer of persons; the persons' own form is plugged into the auto forms.
/// </summary>
public class TDataModule1 : Component
{
    private readonly Container components = new();

    public readonly TBoldAbstractModel BoldModel1;
    public readonly TBoldAsCheckBoxStateRenderer IsRichRenderer;
    public readonly TBoldFilter IsRichFilter;
    public readonly TBoldComparer NameComparer;
    public readonly TBoldAsStringRenderer NegativeRedRenderer;
    public readonly TBoldSystemHandle BoldSystemHandle1;
    public readonly TBoldSystemTypeInfoHandle BoldSystemTypeInfoHandle1;
    public readonly TBoldPlaceableAFP BoldPlaceableAFP1;
    public readonly TBoldPersistenceHandleDB BoldPersistenceHandleDB1;

    /// <summary>The database connection (Delphi's IBDatabase1).</summary>
    public readonly TADOConnection ADOConnection1;

    /// <summary>The database adapter (Delphi's BoldDatabaseAdapterIB1).</summary>
    public readonly TBoldDatabaseAdapterADO BoldDatabaseAdapterADO1;

    /// <summary>The data module on a SQLite database (the connection need not be open).</summary>
    public TDataModule1(DbConnection connection)
    {
        BoldModel1 = new TBoldAbstractModel(components) { Name = "BoldModel1" };
        BoldModel1.ReadModel(BuildingModel.Lines());

        IsRichRenderer = new TBoldAsCheckBoxStateRenderer(components)
        {
            OnSubscribe = IsRichRendererSubscribe,
            OnGetAsCheckBoxState = IsRichRendererGetAsCheckBoxState,
        };
        IsRichFilter = new TBoldFilter(components)
        {
            OnSubscribe = IsRichFilterSubscribe,
            OnFilter = IsRichFilterFilter,
        };
        NameComparer = new TBoldComparer(components)
        {
            OnSubscribe = NameComparerSubscribe,
            OnCompare = NameComparerCompare,
        };
        NegativeRedRenderer = new TBoldAsStringRenderer(components);
        NegativeRedRenderer.OnHoldsChangedValue = NegativeRedRendererHoldsChangedValue;
        NegativeRedRenderer.OnSetFont = NegativeRedRendererSetFont;

        ADOConnection1 = new TADOConnection(connection);
        BoldDatabaseAdapterADO1 = new TBoldDatabaseAdapterADO(components) { Connection = ADOConnection1 };
        BoldDatabaseAdapterADO1.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        BoldPersistenceHandleDB1 = new TBoldPersistenceHandleDB(components)
        {
            BoldModel = BoldModel1,
            DatabaseAdapter = BoldDatabaseAdapterADO1,
        };

        BoldSystemTypeInfoHandle1 = new TBoldSystemTypeInfoHandle(components) { BoldModel = BoldModel1 };
        BoldSystemHandle1 = new TBoldSystemHandle(components)
        {
            IsDefault = true,
            SystemTypeInfoHandle = BoldSystemTypeInfoHandle1,
            PersistenceHandle = BoldPersistenceHandleDB1,
        };

        BoldPlaceableAFP1 = new TBoldPlaceableAFP(components)
        {
            OnGetFormClass = BoldPlaceableAFP1GetFormClass,
            OnRetrieveHandle = BoldPlaceableAFP1RetrieveHandle,
        };
    }

    /// <summary>Discards what was not saved, closes the system and the database.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BoldSystemHandle1.Active)
            {
                BoldSystemHandle1.System!.Discard();
                BoldSystemHandle1.Active = false;
            }

            components.Dispose();
            ADOConnection1.Close();
            ADOConnection1.Connection.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>A person with more than 10,000 is rich.</summary>
    private TCheckBoxState IsRichRendererGetAsCheckBoxState(TBoldElement? element, int representation, string Expression)
    {
        TCheckBoxState result = TCheckBoxState.cbGrayed;
        if (element is TPerson person)
        {
            result = person.Assets > 10000 ? TCheckBoxState.cbChecked : TCheckBoxState.cbUnchecked;
        }

        return result;
    }

    /// <summary>Persons by last name, then first name.</summary>
    private int NameComparerCompare(TBoldElement? item1, TBoldElement? item2)
    {
        int result = SysUtils.AnsiCompareText(((TPerson)item1!).LastName, ((TPerson)item2!).LastName);
        if (result == 0)
        {
            result = SysUtils.AnsiCompareText(((TPerson)item1).FirstName, ((TPerson)item2).FirstName);
        }

        return result;
    }

    private void NameComparerSubscribe(TBoldElement boldElement, TBoldSubscriber subscriber)
    {
        boldElement.SubscribeToExpression("firstName", subscriber, false);
        boldElement.SubscribeToExpression("lastName", subscriber, false);
    }

    private bool IsRichFilterFilter(TBoldElement? element) => element is TPerson person && person.Assets > 10000;

    private void NegativeRedRendererHoldsChangedValue(TBoldElement? element, int representation, string Expression, TBoldSubscriber? subscriber)
    {
        if (element is not null)
        {
            NegativeRedRenderer.DefaultHoldsChangedValue(element, representation, Expression, null, subscriber);
        }
    }

    /// <summary>Negative assets in red, others in blue.</summary>
    private void NegativeRedRendererSetFont(TBoldElement? element, TFont aFont, int representation, string Expression)
    {
        if (element is TPerson person)
        {
            aFont.Color = person.Assets < 0 ? Color.Red : Color.Blue;
        }
    }

    private void IsRichFilterSubscribe(TBoldElement element, TBoldSubscriber subscriber) =>
        element.SubscribeToExpression("assets", subscriber, false);

    private void IsRichRendererSubscribe(TBoldElement? element, int representation, string Expression, TBoldSubscriber? subscriber) =>
        element?.SubscribeToExpression("assets", subscriber, false);

    /// <summary>Persons show in their own form.</summary>
    private void BoldPlaceableAFP1GetFormClass(TBoldElement element, ref Type? result) =>
        result = element is TPerson ? typeof(TPersonAutoForm) : null;

    private void BoldPlaceableAFP1RetrieveHandle(Form form, ref TBoldReferenceHandle? result) =>
        result = form is TPersonAutoForm personForm ? personForm.brhPerson : null;
}
