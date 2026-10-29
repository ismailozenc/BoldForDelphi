// Delphi: Source/BoldAwareGUI/BoldControls/BoldImageJPEG.pas
// Port map: CSharp/port/units/BoldImageJPEG.md

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldImageJPEG", Symbol = "initialization", Note = "BoldWinForms.Initialize registers the adapter.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImageJPEG", Symbol = "TBoldViewJPEGAdapter.EnsureBitmap", Note = "Made an empty JPEG image before loading or saving; a GDI+ image is made from the content, and an empty adapter saves nothing.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImageJPEG", Symbol = "TBoldViewJPEGAdapter.Destroy", Note = "Freed the image; Dispose does.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImageJPEG", Symbol = "THack", Note = "Reached TJPEGImage's protected NewBitmap to paste a bitmap; a GDI+ image is made from the clipboard's image.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldImageJPEG", Symbol = "TBoldViewJPEGAdapter.GetPalette", Note = "Gave a Windows GDI palette (HPALETTE); GDI+ draws without palettes.")]

namespace Bold;

/// <summary>Shows and edits a JPEG image (image/jpeg): a blob's picture in a Bold image; a pasted picture is kept as JPEG.</summary>
[Delphi(PortStatus.Ported, Note = "A GDI+ image, saved as JPEG.")]
public class TBoldViewJPEGAdapter : TBoldAbstractViewAdapter, IDisposable
{
    private const string MIME_image_jpeg = "image/jpeg";

    private Bitmap? FJPEGImage;
    private bool fModified;

    [Delphi(PortStatus.Ported)]
    public TBoldViewJPEGAdapter()
    {
    }

    /// <summary>Frees the image.</summary>
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref FJPEGImage);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public override bool Empty() => FJPEGImage is null || FJPEGImage.Width == 0 || FJPEGImage.Height == 0;

    [Delphi(PortStatus.Ported)]
    public override void Clear() => SysUtils.FreeAndNil(ref FJPEGImage);

    /// <summary>True once content was loaded or pasted (Delphi's TJPEGImage.Modified).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool HasChanged() => FJPEGImage is not null && fModified;

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanReadContent(string ContentType) => ContentType.ToLowerInvariant() == MIME_image_jpeg;

    [Delphi(PortStatus.Ported)]
    public override string ContentType() => Empty() ? "" : MIME_image_jpeg;

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string Description() => sJpegImage;

    [Delphi(PortStatus.Ported)]
    public override void CopyToClipboard()
    {
        if (!Empty())
        {
            Clipboard.SetImage(FJPEGImage!);
        }
    }

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanPasteFromClipboard(string AcceptedContentType)
    {
        string S = AcceptedContentType.ToLowerInvariant();
        return Clipboard.ContainsImage() && (S == "" || S == "image/*" || S == MIME_image_jpeg); // do not localize
    }

    [Delphi(PortStatus.Ported)]
    public override void PasteFromClipboard()
    {
        if (Clipboard.GetImage() is { } image)
        {
            SetImage(new Bitmap(image));
            image.Dispose();
        }
    }

    [Delphi(PortStatus.Ported, Note = "The image is copied out of the stream (GDI+ would keep reading it).")]
    public override void LoadFromStream(Stream Stream)
    {
        using Image image = Image.FromStream(Stream);
        SetImage(new Bitmap(image));
    }

    [Delphi(PortStatus.Ported, Note = "Saves a JPEG; an empty adapter saves nothing.")]
    public override void SaveToStream(Stream Stream) => FJPEGImage?.Save(Stream, ImageFormat.Jpeg);

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string DefaultExtension() => "jpg"; // do not localize

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string FileFilter() => SysUtils.Format("%s (*.jpg, *.jpeg)|*.jpg;*.jpeg", Description()); // do not localize

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanLoadFromFile(string Filename)
    {
        string Extension = Path.GetExtension(Filename).TrimStart('.');
        return string.Equals(Extension, "jpg", StringComparison.OrdinalIgnoreCase) || // do not localize
               string.Equals(Extension, "jpeg", StringComparison.OrdinalIgnoreCase); // do not localize
    }

    [Delphi(PortStatus.Ported)]
    public override void LoadFromFile(string Filename)
    {
        using Stream Stream = File.OpenRead(Filename);
        LoadFromStream(Stream);
    }

    [Delphi(PortStatus.Ported)]
    public override void SaveToFile(string Filename)
    {
        using Stream Stream = File.Create(Filename);
        SaveToStream(Stream);
    }

    /// <summary>Draws the image stretched to the rectangle (when the canvas draws on a Graphics).</summary>
    [Delphi(PortStatus.Ported)]
    public override void Paint(TCanvas Canvas, Rectangle Rect)
    {
        if (FJPEGImage is not null && Canvas is TBoldGraphicsCanvas graphicsCanvas)
        {
            graphicsCanvas.Graphics.DrawImage(FJPEGImage, Rect);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int Width() => FJPEGImage?.Width ?? 0;

    [Delphi(PortStatus.Ported)]
    public override int Height() => FJPEGImage?.Height ?? 0;

    private void SetImage(Bitmap image)
    {
        FJPEGImage?.Dispose();
        FJPEGImage = image;
        fModified = true;
    }
}
