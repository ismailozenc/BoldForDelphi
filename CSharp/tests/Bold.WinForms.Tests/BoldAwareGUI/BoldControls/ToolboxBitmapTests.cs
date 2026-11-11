// The components' pictures in Visual Studio's toolbox: the bitmaps of Delphi's component palette (Images/Components),
// embedded under each class's name, where the WinForms designer looks for them.
using System.Drawing;
using System.Reflection;

namespace Bold.Tests;

public class ToolboxBitmapTests
{
    private static readonly Type[] Samples =
    [
        typeof(TBoldGrid), typeof(TBoldEdit), typeof(TBoldNavigator), typeof(TBoldTreeView),   // Bold.WinForms
        typeof(TBoldSystemHandle), typeof(TBoldListHandle), typeof(TBoldExpressionHandle),    // Bold.Handles
        typeof(TBoldPersistenceHandleDB), typeof(TBoldDatabaseAdapterADO),                    // Bold.Persistence.Sql
        typeof(TBoldAsStringRenderer),                                                        // Bold.AwareGui
    ];

    [TestCaseSource(nameof(Samples))]
    public void The_designer_finds_the_picture_of_a_component(Type component)
    {
        // As the designer asks when the class has no ToolboxBitmap attribute: a bitmap named as the class, in its assembly.
        using Image? image = ToolboxBitmapAttribute.GetImageFromResource(component, null, large: false);
        Assert.That(image, Is.Not.Null, $"Bold.{component.Name}.bmp in {component.Assembly.GetName().Name}");
        Assert.That(image!.Size, Is.EqualTo(new Size(24, 24)), "the palette bitmap, which the toolbox scales");
    }

    [Test]
    public void Every_embedded_picture_is_of_a_class_of_its_assembly()
    {
        int pictures = 0;
        foreach (Assembly assembly in Samples.Select(sample => sample.Assembly).Distinct())
        {
            foreach (string resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith("Bold.T", StringComparison.Ordinal) && name.EndsWith(".bmp", StringComparison.Ordinal)))
            {
                pictures++;
                string typeName = resource[..^".bmp".Length];
                Assert.That(assembly.GetType(typeName), Is.Not.Null, $"{resource} in {assembly.GetName().Name} names no class");
            }
        }

        Assert.That(pictures, Is.GreaterThanOrEqualTo(50));
    }
}
