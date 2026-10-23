using System.Drawing;

namespace Bold;

/// <summary>Delphi's TAlignment: how text sits horizontally in its space.</summary>
public enum TAlignment { taLeftJustify, taRightJustify, taCenter }

/// <summary>Delphi's TFontStyle.</summary>
public enum TFontStyle { fsBold, fsItalic, fsUnderline, fsStrikeOut }

/// <summary>
/// Delphi's TFont as data: name, size, style and color, with a change event. It draws nothing; a GUI layer turns it
/// into its own font (Bold.WinForms into a System.Drawing.Font).
/// </summary>
public class TFont : TPersistent
{
    private string fName = "Segoe UI";
    private int fSize = 9;
    private DelphiSet<TFontStyle> fStyle = DelphiSet<TFontStyle>.Of();
    private Color fColor = SystemColors.WindowText;

    /// <summary>Raised when a property changes.</summary>
    public TNotifyEvent? OnChange { get; set; }

    public string Name
    {
        get => fName;
        set => Set(ref fName, value);
    }

    /// <summary>The size in points.</summary>
    public int Size
    {
        get => fSize;
        set => Set(ref fSize, value);
    }

    public DelphiSet<TFontStyle> Style
    {
        get => fStyle;
        set => Set(ref fStyle, value);
    }

    public Color Color
    {
        get => fColor;
        set => Set(ref fColor, value);
    }

    private void Set<T>(ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            Changed();
        }
    }

    /// <summary>Raises OnChange.</summary>
    protected virtual void Changed() => OnChange?.Invoke(this);

    public override void Assign(TPersistent? Source)
    {
        if (Source is TFont font)
        {
            fName = font.Name;
            fSize = font.Size;
            fStyle = font.Style;
            fColor = font.Color;
            Changed();
        }
        else
        {
            base.Assign(Source);
        }
    }
}

/// <summary>Delphi's TBrush: the fill color of a canvas.</summary>
public class TBrush : TPersistent
{
    public Color Color { get; set; } = SystemColors.Window;

    public override void Assign(TPersistent? Source)
    {
        if (Source is TBrush brush)
        {
            Color = brush.Color;
        }
        else
        {
            base.Assign(Source);
        }
    }
}

/// <summary>Delphi's TPen: the line color and width of a canvas.</summary>
public class TPen : TPersistent
{
    public Color Color { get; set; } = SystemColors.WindowText;
    public int Width { get; set; } = 1;
}

/// <summary>
/// Delphi's TCanvas, as far as Bold's renderers use it: text measured and drawn with the font, rectangles filled with
/// the brush, lines drawn with the pen. A GUI layer gives the drawing (Bold.WinForms draws with GDI+).
/// </summary>
public abstract class TCanvas
{
    public TFont Font { get; } = new();
    public TBrush Brush { get; } = new();
    public TPen Pen { get; } = new();

    /// <summary>The width of the text in pixels with the current font.</summary>
    public abstract int TextWidth(string Text);

    /// <summary>The height of the text in pixels with the current font.</summary>
    public abstract int TextHeight(string Text);

    /// <summary>Fills the rectangle with the brush and draws the text at (X, Y), clipped to the rectangle.</summary>
    public abstract void TextRect(Rectangle Rect, int X, int Y, string Text);

    /// <summary>Fills the rectangle with the brush.</summary>
    public abstract void FillRect(Rectangle Rect);

    /// <summary>Draws a line from (X1, Y1) to (X2, Y2) with the pen.</summary>
    public abstract void Line(int X1, int Y1, int X2, int Y2);
}
