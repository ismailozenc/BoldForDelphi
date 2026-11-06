// Delphi: Source/UMLModel/Plugins/BoldDbPlugins.pas
// Port map: CSharp/port/units/BoldDbPlugins.md
using System.ComponentModel;
using System.Drawing;

namespace Bold.Tests;

public class BoldDbPluginsTests
{
    /// <summary>A model component with a database handle on a SQLite file.</summary>
    private sealed class Sample : IDisposable
    {
        public readonly Container Components = new();
        public readonly TBoldModel Model;
        public readonly TADOConnection Connection;
        public readonly TBoldPersistenceHandleDB Handle;
        public readonly string File = Path.Combine(Path.GetTempPath(), $"bold-editor-{Guid.NewGuid():N}.db");

        public Sample()
        {
            Model = EditorSamples.ModelComponent(Components);
            Connection = TestDatabase.NewADOConnection(File);
            var adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
            adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
            Handle = new TBoldPersistenceHandleDB(Components) { BoldModel = Model, DatabaseAdapter = adapter, Name = "EditorTestDatabase" };
        }

        public TestPlugInContext Context => new(Model);

        public bool HasTable(string name) =>
            Convert.ToInt64(TestDatabase.Scalar(Connection, $"select count(*) from sqlite_master where type = 'table' and upper(name) = upper('{name}')")) == 1;

        /// <summary>Answers yes for this sample's handle and to the other questions, no for other handles.</summary>
        public DialogAnswers Answers(int otherwise = Controls.mrYes) =>
            new(msg => msg.StartsWith("Use Database settings from") ? (msg.Contains(Handle.Name) ? Controls.mrYes : Controls.mrNo) : otherwise);

        public void Dispose()
        {
            Handle.Active = false;
            Components.Dispose();
            Connection.Close();
            Connection.Connection.Dispose();
            System.IO.File.Delete(File);
        }
    }

    [Test]
    [Covers(typeof(TUMLDBGenerator), "*")]
    [Covers(typeof(TUMLGenericDBPlugin), "*")]
    [Covers(typeof(BoldDbPlugins), "*")]
    public void The_database_is_generated_for_a_model_without_errors()
    {
        using var sample = new Sample();
        var generator = new TUMLDBGenerator(false);
        Assert.That((generator.MenuItemName, generator.ImageResourceName, generator.PlugInType), Is.EqualTo(("Generate Database", "UMLPluginGenDBImage", TPlugInType.ptTool)));
        Assert.That(generator.ImageMaskColor, Is.EqualTo(Color.Teal));
        Assert.That(generator.Options, Is.EqualTo(TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified)));
        using (sample.Answers())
        {
            generator.Execute(sample.Context);
        }

        Assert.That(sample.HasTable("Building"), Is.True);
        Assert.That(sample.HasTable("Person"), Is.True);

        EditorSamples.NewElement(sample.Model.EnsuredUMLModel, "Class", "Person");
        using (sample.Answers())
        {
            Assert.That(() => generator.Execute(sample.Context), Throws.InstanceOf<EBold>().With.Message.EqualTo("Generate Database: Errors in model"));
        }

        using (new DialogAnswers(Controls.mrNo))
        {
            Assert.That(() => generator.Execute(sample.Context), Throws.Exception.With.Message.StartsWith("No persistencehandle found"));
        }

        Assert.That(BoldUMLPlugInRegistry.PlugIns.Count(p => p.PluginClassName == nameof(TUMLDBGenerator)), Is.EqualTo(1));
    }

    [Test]
    [Covers(typeof(TBolddbStructureValidatorPlugin), "*")]
    [Covers(typeof(TBoldDbDataValidatorPlugin), "*")]
    [Covers(typeof(TBoldDbValidatorPlugIn), "*")]
    public void The_database_is_validated_against_the_model()
    {
        using var sample = new Sample();
        var structure = new TBolddbStructureValidatorPlugin(false);
        var data = new TBoldDbDataValidatorPlugin(false);
        Assert.That((structure.MenuItemName, structure.ImageResourceName), Is.EqualTo(("Validate database structure", "UMLPluginStructureValidator")));
        Assert.That((data.MenuItemName, data.ImageResourceName), Is.EqualTo(("Validate data integrity in database", "UMLPluginDataValidator")));
        Assert.That(data.Options, Is.EqualTo(TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified)));
        using DialogAnswers answers = sample.Answers();
        new TUMLDBGenerator(false).Execute(sample.Context);
        structure.Execute(sample.Context);
        data.Execute(sample.Context);
        Assert.That(answers.Messages.Count(m => m == "Database validated OK"), Is.EqualTo(2));
    }

    [Test]
    [Covers(typeof(TBoldUMLDBEvolutorPlugin), "*")]
    public void The_database_evolves_with_the_model()
    {
        using var sample = new Sample();
        var evolutor = new TBoldUMLDBEvolutorPlugin(false);
        Assert.That((evolutor.MenuItemName, evolutor.ImageResourceName), Is.EqualTo(("Evolve database", "UMLPluginDbEvolutor")));
        using (sample.Answers())
        {
            new TUMLDBGenerator(false).Execute(sample.Context);
        }

        TBoldUMLObject garage = EditorSamples.NewElement(sample.Model.EnsuredUMLModel, "Class", "Garage");
        garage.persistent = true;
        garage.SetFirstParent(sample.Model.EnsuredUMLModel.Classes.Single(c => c.name == "BusinessClassesRoot"));
        EditorSamples.NewAttribute(garage, "spaces", "Integer");
        Action<TBoldAbstractPersistenceHandleDB, bool> evolve = TBoldUMLDBEvolutorPlugin.EvolveDB;
        bool? generic = null;
        try
        {
            TBoldUMLDBEvolutorPlugin.EvolveDB = (handle, isGeneric) => generic = isGeneric;
            using (sample.Answers(Controls.mrCancel))
            {
                evolutor.Execute(sample.Context);
            }

            Assert.That(generic, Is.Null, "cancelled");
            using (sample.Answers(Controls.mrNo))
            {
                evolutor.Execute(sample.Context);
            }

            Assert.That(generic, Is.False);
        }
        finally
        {
            TBoldUMLDBEvolutorPlugin.EvolveDB = evolve;
        }

        Assert.That(sample.HasTable("Garage"), Is.False);
        using (sample.Answers())
        {
            evolutor.Execute(sample.Context);
        }

        Assert.That(sample.HasTable("Garage"), Is.True);
    }
}
