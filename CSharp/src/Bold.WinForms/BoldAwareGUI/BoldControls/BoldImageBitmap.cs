// Delphi: Source/BoldAwareGUI/BoldControls/BoldImageBitmap.pas
// Port map: CSharp/port/units/BoldImageBitmap.md

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldImageBitmap", Symbol = "initialization", Note = "BoldWinForms.Initialize registers the adapter.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImageBitmap", Symbol = "TBoldViewBitmapAdapter.EnsureBitmap", Note = "Made an empty bitmap before loading or saving; a GDI+ bitmap is made from the content, and an empty adapter saves nothing.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldImageBitmap", Symbol = "TBoldViewBitmapAdapter.Destroy", Note = "Freed the bitmap; Dispose does.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldImageBitmap", Symbol = "TBoldViewBitmapAdapter.GetPalette", Note = "Gave a Windows GDI palette (HPALETTE); GDI+ draws without palettes.")]

namespace Bold;

/// <summary>Shows and edits a bitmap (image/bitmap, image/bmp, or no content type): a blob's picture in a Bold image.</summary>
[Delphi(PortStatus.Ported, Note = "A GDI+ Bitmap.")]
public class TBoldViewBitmapAdapter : TBoldAbstractViewAdapter, IDisposable
{
    private const string MIME_image_bmp = "image/bmp";
    private const string MIME_image_bitmap = "image/bitmap";

    private Bitmap? FBitmap;
    private bool fModified;

    [Delphi(PortStatus.Ported)]
    public TBoldViewBitmapAdapter()
    {
    }

    /// <summary>Frees the bitmap.</summary>
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref FBitmap);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public override bool Empty() => FBitmap is null || FBitmap.Width == 0 || FBitmap.Height == 0;

    [Delphi(PortStatus.Ported)]
    public override void Clear() => SysUtils.FreeAndNil(ref FBitmap);

    /// <summary>True once content was loaded or pasted (Delphi's TBitmap.Modified).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool HasChanged() => FBitmap is not null && fModified;

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanReadContent(string ContentType)
    {
        string S = ContentType.ToLowerInvariant();
        return S == "" || S == MIME_image_bitmap || S == MIME_image_bmp;
    }

    [Delphi(PortStatus.Ported)]
    public override string ContentType() => Empty() ? "" : MIME_image_bitmap;

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string Description() => sBitMapImage;

    [Delphi(PortStatus.Ported)]
    public override void CopyToClipboard()
    {
        if (!Empty())
        {
            Clipboard.SetImage(FBitmap!);
        }
    }

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanPasteFromClipboard(string AcceptedContentType)
    {
        string S = AcceptedContentType.ToLowerInvariant();
        return Clipboard.ContainsImage() && (S == "" || S == "image/*" || S == MIME_image_bitmap); // do not localize
    }

    [Delphi(PortStatus.Ported)]
    public override void PasteFromClipboard()
    {
        if (Clipboard.GetImage() is { } image)
        {
            SetBitmap(new Bitmap(image));
            image.Dispose();
        }
    }

    [Delphi(PortStatus.Ported, Note = "The bitmap is copied out of the stream (GDI+ would keep reading it).")]
    public override void LoadFromStream(Stream Stream)
    {
        using Image image = Image.FromStream(Stream);
        SetBitmap(new Bitmap(image));
    }

    [Delphi(PortStatus.Ported, Note = "Saves a BMP; an empty adapter saves nothing.")]
    public override void SaveToStream(Stream Stream) => FBitmap?.Save(Stream, ImageFormat.Bmp);

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string DefaultExtension() => "bmp"; // do not localize

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override string FileFilter() => SysUtils.Format("%s (*.bmp)|*.bmp", Description()); // do not localize

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a class method.")]
    public override bool CanLoadFromFile(string Filename) =>
        string.Equals(Path.GetExtension(Filename).TrimStart('.'), "bmp", StringComparison.OrdinalIgnoreCase); // do not localize

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

    /// <summary>Draws the bitmap stretched to the rectangle (when the canvas draws on a Graphics).</summary>
    [Delphi(PortStatus.Ported)]
    public override void Paint(TCanvas Canvas, Rectangle Rect)
    {
        if (FBitmap is not null && Canvas is TBoldGraphicsCanvas graphicsCanvas)
        {
            graphicsCanvas.Graphics.DrawImage(FBitmap, Rect);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int Width() => FBitmap?.Width ?? 0;

    [Delphi(PortStatus.Ported)]
    public override int Height() => FBitmap?.Height ?? 0;

    private void SetBitmap(Bitmap bitmap)
    {
        FBitmap?.Dispose();
        FBitmap = bitmap;
        fModified = true;
    }
}
