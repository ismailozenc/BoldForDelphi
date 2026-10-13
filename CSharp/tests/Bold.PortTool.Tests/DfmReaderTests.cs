using System.Text;
using Bold.PortTool.Fixtures;

namespace Bold.Tests;

public class DfmReaderTests
{
    private static readonly string[] ModelLines = ["VERSION 19", "(Model", "\t\"Tiny\"", ")"];

    [Test]
    public void Reads_a_model_from_a_text_form()
    {
        const string form = """
            object Form1: TForm1
              Left = 10
              Caption = 'It''s a form'
              Font.Style = [fsBold]
              object BoldModel1: TBoldModel
                UMLModelMode = umIdle
                Model = (
                  'VERSION 19'
                  '(Model'
                  #9'"Ti' +
                    'ny"'
                  ')')
                Left = 8
              end
              object Grid: TStringGrid
                ColWidths = (
                  64
                  64)
                Items = <
                  item
                    Caption = 'One'
                  end>
              end
            end
            """;
        var root = DfmReader.Read(Encoding.Latin1.GetBytes(form));
        var model = root.DescendantsAndSelf().Single(c => c.ClassName == "TBoldModel");
        Assert.Multiple(() =>
        {
            Assert.That(root.Name, Is.EqualTo("Form1"));
            Assert.That(root.Properties["Caption"], Is.EqualTo("It's a form"));
            Assert.That(model.Name, Is.EqualTo("BoldModel1"));
            Assert.That(model.StringList("Model"), Is.EqualTo(ModelLines));
            Assert.That(root.Children.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void Reads_a_model_from_a_binary_form_with_and_without_the_resource_header()
    {
        byte[] stream = BinaryForm();
        byte[] withHeader = [0xFF, 0x0A, 0x00, .. "TFORM1"u8, 0x00, 0x30, 0x10, .. BitConverter.GetBytes(stream.Length), .. stream];
        foreach (byte[] bytes in new[] { stream, withHeader })
        {
            var root = DfmReader.Read(bytes);
            var model = root.DescendantsAndSelf().Single(c => c.ClassName == "TBoldModel");
            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("TForm1"));
                Assert.That(root.Properties["Left"], Is.EqualTo(300));
                Assert.That(model.StringList("Model"), Is.EqualTo(ModelLines));
                Assert.That(model.Properties["Tag"], Is.EqualTo(-2));
            });
        }
    }

    /// <summary>Builds a TPF0 stream: a form with a Left property, and a TBoldModel child with a string list and a set.</summary>
    private static byte[] BinaryForm()
    {
        var data = new List<byte>();
        void ShortString(string text)
        {
            data.Add((byte)text.Length);
            data.AddRange(Encoding.Latin1.GetBytes(text));
        }

        data.AddRange("TPF0"u8.ToArray());
        ShortString("TForm1");
        ShortString("Form1");
        ShortString("Left");
        data.Add(3); // vaInt16
        data.AddRange(BitConverter.GetBytes((short)300));
        data.Add(0); // end of properties
        ShortString("TBoldModel");
        ShortString("BoldModel1");
        ShortString("Model");
        data.Add(1); // vaList
        ShortString2(data, ModelLines[0]);
        ShortString2(data, ModelLines[1]);
        data.Add(12); // vaLString
        data.AddRange(BitConverter.GetBytes(ModelLines[2].Length));
        data.AddRange(Encoding.Latin1.GetBytes(ModelLines[2]));
        ShortString2(data, ModelLines[3]);
        data.Add(0); // end of list
        ShortString("Options");
        data.Add(11); // vaSet
        ShortString("opOne");
        ShortString("");
        ShortString("Tag");
        data.Add(2); // vaInt8
        data.Add(unchecked((byte)(sbyte)-2));
        data.Add(0); // end of properties
        data.Add(0); // no children
        data.Add(0); // end of the form's children
        return data.ToArray();

        static void ShortString2(List<byte> data, string text)
        {
            data.Add(6); // vaString
            data.Add((byte)text.Length);
            data.AddRange(Encoding.Latin1.GetBytes(text));
        }
    }
}
