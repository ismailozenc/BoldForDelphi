// Delphi: Source/BoldAwareGUI/BoldControls/BoldCaptionController.pas, BoldPageControl.pas, BoldProgressBar.pas,
// BoldTrackBar.pas, BoldRichEdit.pas, BoldImage.pas, BoldImageBitmap.pas, BoldImageJPEG.pas
// Port map: CSharp/port/units/BoldImage.md
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The controls of one value: caption controller, page control, progress bar, track bar, rich edit and image.</summary>
[NonParallelizable]
public class BoldValueControlsTests
{
    private sealed class TestTrackBar : TBoldTrackBar
    {
        public void ScrollTo(int value)
        {
            Value = value;
            OnScroll(EventArgs.Empty);
        }
    }

    [Test]
    [Covers(typeof(TBoldCustomCaptionController), "*")]
    [Covers(typeof(TBoldCaptionController), "*")]
    public void A_caption_controller_puts_the_element_s_string_in_the_tracked_control()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        using var label = new Label();
        using var group = new GroupBox();
        using var controller = new TBoldCaptionController { BoldHandle = s.Reference(ann), TrackControl = label };
        controller.BoldProperties.Expression = "firstName";
        GuiSample.Display();
        Assert.That(label.Text, Is.EqualTo("Ann"));
        Assert.That(controller.Caption, Is.EqualTo("Ann"));

        ann.FirstName = "Anna";
        GuiSample.Display();
        Assert.That(label.Text, Is.EqualTo("Anna"));

        controller.TrackControl = group;
        Assert.That(group.Text, Is.EqualTo("Anna"), "a new tracked control gets the caption");
    }

    [Test]
    [Covers(typeof(TBoldPageControl), "*")]
    public void A_page_control_shows_the_page_named_as_the_element_s_string()
    {
        using var s = new GuiSample();
        TPerson bob = s.NewPerson("Bob");
        using var pages = new TBoldPageControl();
        pages.TabPages.Add(new TabPage { Name = "Ann" });
        pages.TabPages.Add(new TabPage { Name = "Bob" });
        using var form = new TestForm(pages);
        pages.BoldProperties.Expression = "firstName";
        pages.BoldHandle = s.Reference(bob);
        GuiSample.Display();
        Assert.That(pages.SelectedTab!.Name, Is.EqualTo("Bob"));

        bob.FirstName = "Ann";
        GuiSample.Display();
        Assert.That(pages.SelectedTab!.Name, Is.EqualTo("Ann"));

        bob.FirstName = "Nobody";
        GuiSample.Display();
        Assert.That(pages.SelectedTab!.Name, Is.EqualTo("Ann"), "no page has the name: the page stays");
    }

    [Test]
    [Covers(typeof(TBoldProgressBar), "*")]
    public void A_progress_bar_shows_and_sets_an_integer()
    {
        using var s = new GuiSample();
        TResidential_Building house = s.NewHouse("Elm Street");
        house.ZipCode = 42;
        using var bar = new TBoldProgressBar();
        bar.BoldProperties.Expression = "zipCode";
        bar.BoldHandle = s.Reference(house);
        GuiSample.Display();
        Assert.That(bar.Position, Is.EqualTo(42));
        Assert.That(bar.EffectiveReadOnly, Is.False);

        bar.Position = 50;
        GuiSample.ApplyAll();
        Assert.That(house.ZipCode, Is.EqualTo(50));

        house.ZipCode = 500;
        GuiSample.Display();
        Assert.That(bar.Position, Is.EqualTo(bar.Maximum), "beyond the maximum shows the maximum");

        bar.ReadOnly = true;
        GuiSample.Display();
        Assert.That(bar.EffectiveReadOnly, Is.True);
        Assert.Throws<EBold>(() => bar.Position = 10);
    }

    [Test]
    [Covers(typeof(TBoldTrackBar), "*")]
    public void A_track_bar_shows_an_integer_and_moving_it_sets_the_integer()
    {
        using var s = new GuiSample();
        TResidential_Building house = s.NewHouse("Elm Street");
        house.ZipCode = 3;
        using var bar = new TestTrackBar { Maximum = 10 };
        using var form = new TestForm(bar);
        bar.BoldProperties.Expression = "zipCode";
        bar.BoldHandle = s.Reference(house);
        GuiSample.Display();
        Assert.That(bar.Position, Is.EqualTo(3));

        bar.ScrollTo(7);
        GuiSample.ApplyAll();
        Assert.That(house.ZipCode, Is.EqualTo(7));
        GuiSample.Display(); // the applied value comes back before the next change

        bar.Position = 2;
        GuiSample.ApplyAll();
        Assert.That(house.ZipCode, Is.EqualTo(2));

        bar.ReadOnly = true;
        GuiSample.Display();
        Assert.Throws<EBold>(() => bar.Position = 5);
    }

    [Test]
    [Covers(typeof(TBoldCustomRichEdit), "*")]
    [Covers(typeof(TBoldRichEdit), "*")]
    public void A_rich_edit_shows_plain_and_rich_text_and_writes_typed_text_back()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        using var edit = new TBoldRichEdit();
        using var form = new TestForm(edit);
        edit.BoldProperties.Expression = "firstName";
        edit.BoldHandle = s.Reference(ann);
        GuiSample.Display();
        Assert.That(edit.Text, Is.EqualTo("Ann"));
        Assert.That(edit.EffectiveReadOnly, Is.False);

        edit.Text = "Bea";
        GuiSample.ApplyAll();
        Assert.That(ann.FirstName, Is.EqualTo("Bea"));

        ann.FirstName = @"{\rtf1\ansi {\b Bold} text}";
        GuiSample.Display();
        Assert.That(edit.Text.Trim(), Is.EqualTo("Bold text"));
        Assert.That(edit.FormattedText, Does.StartWith(@"{\rtf"));
        GuiSample.ApplyAll();
        Assert.That(ann.FirstName, Does.StartWith(@"{\rtf"), "the rich text the element put in is not written back as plain text");

        edit.ReadOnly = true;
        GuiSample.Display();
        Assert.Throws<EBold>(() => edit.Text = "Cid");
    }

    [Test]
    [Covers(typeof(TBoldImage), "*")]
    [Covers(typeof(TBoldViewBitmapAdapter), "*")]
    [Covers(typeof(TBoldViewJPEGAdapter), "*")]
    public void An_image_shows_a_blob_s_picture_and_loads_a_file_into_it()
    {
        using var s = new ValuesSample();
        TBoldObject thing = s.NewThing();
        var photo = ValuesSample.Member<TBATypedBlob>(thing, "photo");
        using (var picture = new Bitmap(3, 2))
        using (var stream = new MemoryStream())
        {
            picture.SetPixel(0, 0, Color.Red);
            picture.Save(stream, ImageFormat.Bmp);
            photo.AsString = "";
            using (Stream blob = photo.CreateBlobStream(TBoldBlobStreamMode.bmWrite))
            {
                blob.Write(stream.ToArray());
            }

            photo.ContentType = "image/bitmap";
        }

        using var image = new TBoldImage { StretchMode = TBoldStretchMode.bsmStretchProportional, Center = true };
        using var form = new TestForm(image);
        image.BoldProperties.Expression = "photo";
        image.BoldHandle = new TBoldReferenceHandle(null) { Value = thing };
        GuiSample.Display();
        Assert.That(image.Viewer, Is.InstanceOf<TBoldViewBitmapAdapter>());
        Assert.That(image.Viewer!.Width(), Is.EqualTo(3));
        Assert.That(image.Viewer.ContentType(), Is.EqualTo("image/bitmap"));
        using (var drawn = new Bitmap(image.Width, image.Height))
        {
            image.DrawToBitmap(drawn, new Rectangle(0, 0, image.Width, image.Height));
        }

        image.Scale = 200;
        Assert.That(image.StretchMode, Is.EqualTo(TBoldStretchMode.bsmStretchToScale));
        image.CalcDisplayRect();

        string file = Path.Combine(Path.GetTempPath(), $"BoldImageTest{Guid.NewGuid():N}.jpg");
        try
        {
            using (var jpeg = new Bitmap(5, 4))
            {
                jpeg.Save(file, ImageFormat.Jpeg);
            }

            image.LoadFromFile(file);
            Assert.That(image.Viewer, Is.InstanceOf<TBoldViewJPEGAdapter>());
            GuiSample.ApplyAll();
            Assert.That(photo.ContentType, Is.EqualTo("image/jpeg"));
            GuiSample.Display();
            Assert.That(image.Viewer!.Width(), Is.EqualTo(5));
            Assert.Throws<EBold>(() => image.LoadFromFile(Path.ChangeExtension(file, ".xyz")), "no adapter loads the file");
        }
        finally
        {
            File.Delete(file);
        }

        image.ReadOnly = true;
        image.LoadFromFile("ignored.bmp");
        Assert.That(image.Viewer, Is.InstanceOf<TBoldViewJPEGAdapter>(), "a read-only image loads nothing");
    }
}
