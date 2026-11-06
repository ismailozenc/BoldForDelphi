// Delphi: Source/UMLModel/Core/BoldUMLUtils.pas
// Port map: CSharp/port/units/BoldUMLUtils.md
using System.ComponentModel;

namespace Bold.Tests;

public class BoldUMLUtilsTests
{
    [Test]
    [Covers(typeof(BoldUMLUtils), "*")]
    public void The_database_configuration_of_a_model_is_its_persistence_handle_s()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        Assert.That(BoldUMLUtils.SQLDataBaseConfigforModel(model), Is.Null);
        var adapter = new TBoldDatabaseAdapterADO(components);
        var handle = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
        Assert.That(BoldUMLUtils.SQLDataBaseConfigforModel(model), Is.SameAs(handle.SQLDatabaseConfig));
        Assert.That(BoldUMLUtils.SQLDataBaseConfigforModel(new TBoldAbstractModel(components)), Is.Null);
    }
}
