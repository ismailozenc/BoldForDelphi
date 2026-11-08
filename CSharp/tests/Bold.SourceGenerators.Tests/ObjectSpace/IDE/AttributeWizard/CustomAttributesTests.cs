// Delphi: Source/ObjectSpace/IDE/AttributeWizard (the attribute wizard, which the generator replaces)
// Port map: CSharp/port/units/BoldAttributeWizard.md
using System.ComponentModel;
using CustomAttributes;

namespace Bold.Tests;

/// <summary>
/// The attribute types the generator wrote for the CustomAttributes example, in a system on the Shapes model: a shape's
/// color (a value set), position (a new attribute with two fields) and label (a subclass of the string attribute).
/// </summary>
[NonParallelizable]
public class CustomAttributesTests
{
    /// <summary>A system on the Shapes model, in memory or in a SQLite database file.</summary>
    private sealed class Shapes : IDisposable
    {
        public readonly Container Components = new();
        public readonly TBoldTypeNameHandle TypeNames;
        public readonly TBoldAbstractModel Model;
        public readonly TBoldSystemTypeInfoHandle TypeInfoHandle;
        public readonly TBoldSystemHandle SystemHandle;
        public readonly string? DatabaseFile;
        public readonly TADOConnection? Connection;

        public Shapes(string? databaseFile = null, bool createSchema = true)
        {
            TypeNames = new TBoldTypeNameHandle(Components);
            ShapesModel.AddTypeNames(TypeNames.Dictionary);
            Model = new TBoldAbstractModel(Components) { Name = "Shapes", TypeNameHandle = TypeNames };
            Model.ReadModel(ShapesModel.Lines());
            TypeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { BoldModel = Model, UseGeneratedCode = false }; // no classes generated for Shapes
            SystemHandle = new TBoldSystemHandle(Components) { SystemTypeInfoHandle = TypeInfoHandle };
            if (databaseFile is not null)
            {
                DatabaseFile = databaseFile;
                Connection = TestDatabase.NewADOConnection(databaseFile);
                var adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
                adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
                var databaseHandle = new TBoldPersistenceHandleDB(Components) { BoldModel = Model, DatabaseAdapter = adapter };
                if (createSchema)
                {
                    databaseHandle.CreateDataBaseSchema();
                }

                SystemHandle.PersistenceHandle = databaseHandle;
            }

            SystemHandle.Active = true;
        }

        public TBoldSystem System => SystemHandle.System!;

        public TBoldObject NewShape() => System.CreateNewObjectByExpressionName("Shape");

        public TBoldObject[] AllShapes()
        {
            TBoldObjectList shapes = System.ClassByExpressionName("Shape");
            shapes.EnsureObjects();
            return [.. Enumerable.Range(0, shapes.Count).Select(i => shapes[i]!)];
        }

        public void Dispose()
        {
            if (SystemHandle.Active)
            {
                System.Discard();
                SystemHandle.Active = false;
            }

            Components.Dispose();
            if (Connection is not null)
            {
                Connection.Close();
                Connection.Connection.Dispose();
            }
        }
    }

    private static TBAColor ColorOf(TBoldObject shape) => (TBAColor)shape.BoldMemberByExpressionName("Color");

    private static TBAPoint PositionOf(TBoldObject shape) => (TBAPoint)shape.BoldMemberByExpressionName("Position");

    private static TBAShortString LabelOf(TBoldObject shape) => (TBAShortString)shape.BoldMemberByExpressionName("Label");

    [Test]
    [Covers("BoldWAdatamodule", "TdmAttributeWizard")]
    public void The_model_takes_the_types_through_its_type_name_handle()
    {
        using var shapes = new Shapes();
        Assert.That(shapes.TypeInfoHandle.StaticSystemTypeInfo!.SystemIsRunnable, Is.True, shapes.TypeInfoHandle.StaticSystemTypeInfo.InitializationLog.Text);
        TBoldObject shape = shapes.NewShape();
        Assert.That((ColorOf(shape), PositionOf(shape), LabelOf(shape)), Is.Not.Null);
        Assert.That(shape.BoldMemberByExpressionName("Name"), Is.InstanceOf<TBAString>().And.Not.InstanceOf<TBAShortString>());
        Assert.That(BoldMemberTypeDictionary.BoldMemberTypes().DescriptorByDelphiName("TBAPoint"), Is.Not.Null, "registered when the assembly was first used");
    }

    [Test]
    [Covers("BoldWAValueSetDlg", "TValueSetDlg")]
    public void A_color_is_one_of_its_values()
    {
        using var shapes = new Shapes();
        TBAColor color = ColorOf(shapes.NewShape());
        Assert.That(color.AsColor, Is.EqualTo(TColorEnum.cRed), "the first value");
        color.AsColor = TColorEnum.cGreen;
        Assert.That((color.AsInteger, color.AsString), Is.EqualTo((1, "Green")));
        Assert.That(color.Values.Count, Is.EqualTo(3));
        Assert.That(color.Values.FindByString(2, "Grün")?.AsInteger, Is.EqualTo((int)TColorEnum.cGreen), "the second representation");
        color.AsString = "Blue";
        Assert.That(color.AsColor, Is.EqualTo(TColorEnum.cBlue));
        Assert.That(() => color.AsString = "Pink", Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers("BoldWAMethodInfo", "TMethodInfo")]
    public void A_point_has_two_coordinates_and_tells_when_they_change()
    {
        using var shapes = new Shapes();
        TBAPoint position = PositionOf(shapes.NewShape());
        int changes = 0;
        using var subscriber = new TBoldPassthroughSubscriber((_, _, _) => changes++);
        position.AddSmallSubscription(subscriber, TBoldSmallEventSet.Of(BoldSubscription.beValueChanged), BoldSubscription.breReEvaluate);
        position.MoveTo(3, 4);
        Assert.That((position.AsX, position.AsY, position.AsString), Is.EqualTo((3, 4, "(3, 4)")));
        Assert.That(changes, Is.EqualTo(2), "one change a coordinate");
        position.AsX = 3;
        Assert.That(changes, Is.EqualTo(2), "setting the same value changes nothing");
        position.AsString = "(5, -6)";
        Assert.That((position.AsX, position.AsY), Is.EqualTo((5, -6)));
        Assert.That(position.CanSetX(7, null), Is.True);

        TBAPoint other = PositionOf(shapes.NewShape());
        other.Assign(position);
        Assert.That((other.AsX, other.AsY), Is.EqualTo((5, -6)), "assigned from another point");
        position.AsString = "";
        Assert.That(position.IsNull, Is.True);
        other.Assign(position);
        Assert.That(other.IsNull, Is.True);
    }

    [Test]
    [Covers("BoldWAInterfaces", "IUnitGenerator")]
    public void A_point_is_given_as_its_content_and_copied_through_a_free_standing_value()
    {
        using var shapes = new Shapes();
        TBAPoint position = PositionOf(shapes.NewShape());
        position.MoveTo(1, 2);
        var content = (IBoldPointContent)position.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!;
        Assert.That((content.X, content.Y, content.IsNull), Is.EqualTo((1, 2, false)));
        content.X = 10;
        Assert.That(position.AsX, Is.EqualTo(10), "the content is the attribute's");

        var value = (TBFSPoint)BoldFreeStandingValueFactories.FreeStandingValueFactory!.CreateInstance(TBAPoint.ContentName_Point)!;
        value.AssignContent(content);
        Assert.That((value.X, value.Y, value.GetStreamName()), Is.EqualTo((10, 2, "Point")));

        TBAPoint other = PositionOf(shapes.NewShape());
        other.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!.AssignContent(value);
        Assert.That((other.AsX, other.AsY), Is.EqualTo((10, 2)), "a free-standing value assigned to an attribute");
    }

    [Test]
    [Covers("BoldWASubClassForm1", "TSubClassForm1")]
    public void A_short_string_takes_at_most_ten_characters()
    {
        using var shapes = new Shapes();
        TBAShortString label = LabelOf(shapes.NewShape());
        label.AsString = "Ten chars!";
        Assert.That(label.AsString, Is.EqualTo("Ten chars!"));
        Assert.That(label.ValidateString("Eleven char", 1), Is.False);
        Assert.That(() => label.AsString = "Eleven char", Throws.InstanceOf<EBold>());
        TBoldTypeNameMapping mapping = shapes.TypeNames.Dictionary.MappingForModelName("ShortString")!;
        Assert.That((mapping.DelphiName, mapping.ExpandedContentsName, mapping.ExpandedMapperName), Is.EqualTo(("TBAShortString", "String", "TBoldPMString")),
            "the string attribute's content and persistence");
    }

    [Test]
    [Covers("BoldOTACodeGen", "TUnitCreator")]
    [Covers("BoldWProjectWizard", "TProjectWizard")]
    public void The_types_are_stored_and_read_back()
    {
        string file = Path.Combine(Path.GetTempPath(), $"shapes-{Guid.NewGuid():N}.db");
        try
        {
            using (var shapes = new Shapes(file))
            {
                TBoldObject shape = shapes.NewShape();
                shape.BoldMemberByExpressionName("Name").AsString = "Square";
                ColorOf(shape).AsColor = TColorEnum.cBlue;
                PositionOf(shape).MoveTo(7, -8);
                LabelOf(shape).AsString = "corner";
                TBoldObject empty = shapes.NewShape();
                empty.BoldMemberByExpressionName("Name").AsString = "Nothing";
                PositionOf(empty).SetToNull();
                shapes.System.UpdateDatabase();
                Assert.That(Convert.ToInt32(TestDatabase.Scalar(shapes.Connection!, "SELECT Position FROM Shape WHERE Name = 'Square'")), Is.EqualTo(7));
                Assert.That(Convert.ToInt32(TestDatabase.Scalar(shapes.Connection!, "SELECT Position_1 FROM Shape WHERE Name = 'Square'")), Is.EqualTo(-8),
                    "a column a field, as the generated mapper says");
                Assert.That(Convert.ToInt32(TestDatabase.Scalar(shapes.Connection!, "SELECT Color FROM Shape WHERE Name = 'Square'")), Is.EqualTo((int)TColorEnum.cBlue),
                    "a value set is stored as its integer");
            }

            using (var shapes = new Shapes(file, createSchema: false))
            {
                TBoldObject[] all = shapes.AllShapes();
                TBoldObject square = all.Single(s => s.BoldMemberByExpressionName("Name").AsString == "Square");
                Assert.That(ColorOf(square).AsColor, Is.EqualTo(TColorEnum.cBlue));
                Assert.That((PositionOf(square).AsX, PositionOf(square).AsY), Is.EqualTo((7, -8)));
                Assert.That(LabelOf(square).AsString, Is.EqualTo("corner"));
                Assert.That(PositionOf(all.Single(s => s != square)).IsNull, Is.True, "a null point stays null");

                PositionOf(square).AsY = 9;
                shapes.System.UpdateDatabase();
            }

            using (var shapes = new Shapes(file, createSchema: false))
            {
                TBoldObject square = shapes.AllShapes().Single(s => s.BoldMemberByExpressionName("Name").AsString == "Square");
                Assert.That((PositionOf(square).AsX, PositionOf(square).AsY), Is.EqualTo((7, 9)), "a change is stored");
            }
        }
        finally
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }
}
