// Delphi: Source/Common/Support/BoldCommonBitmaps.pas
// Port map: CSharp/port/units/BoldCommonBitmaps.md

using System.Drawing;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldCommonBitmaps", Symbol = "initialization", Note = "The bitmaps are loaded on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldCommonBitmaps", Symbol = "finalization", Note = "Freed the bitmaps; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The bitmaps of the grid, the navigator and the edit box button, from the resources of Bold.WinForms.</summary>
public static class BoldCommonBitmaps
{
    private static readonly Dictionary<string, Bitmap?> fBitmaps = [];

    /// <summary>The bitmap of the resource; a transparent one takes its lower left pixel's color as transparent.</summary>
    [Delphi(PortStatus.Ported, Note = "Reads the bitmap embedded in Bold.WinForms (from Images in the repository).")]
    private static Bitmap? NewBitMap(string Name, bool Transparent)
    {
        lock (fBitmaps)
        {
            if (!fBitmaps.TryGetValue(Name, out Bitmap? result))
            {
                using Stream? stream = typeof(BoldCommonBitmaps).Assembly.GetManifestResourceStream("Bold.Images." + Name + ".bmp");
                if (stream is not null)
                {
                    result = new Bitmap(stream);
                    if (Transparent)
                    {
                        result.MakeTransparent(result.GetPixel(0, result.Height - 1));
                    }
                }

                fBitmaps[Name] = result;
            }

            return result;
        }
    }

    /// <summary>Loads every bitmap (each also loads on first use).</summary>
    [Delphi(PortStatus.Ported)]
    private static void LoadBitmapsFromResources()
    {
        _ = bmpBoldGridSelected;
        _ = bmpBoldGridCurrent;
        _ = bmpBoldGridConstraint_true;
        _ = bmpBoldGridConstraint_false;
        _ = bmpBoldNavigatorDelete;
        _ = bmpBoldNavigatorFirst;
        _ = bmpBoldNavigatorLast;
        _ = bmpBoldNavigatorInsert;
        _ = bmpBoldNavigatorNext;
        _ = bmpBoldNavigatorPrior;
        _ = bmpBoldNavigatorMoveUp;
        _ = bmpBoldNavigatorMoveDown;
        _ = bmpBOLDEDIT_ELLIPSIS;
    }

    /// <summary>Drops every bitmap; they load again when used.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void FreeAllBitMaps()
    {
        lock (fBitmaps)
        {
            foreach (Bitmap? bitmap in fBitmaps.Values)
            {
                bitmap?.Dispose();
            }

            fBitmaps.Clear();
        }
    }

    /// <summary>Loads every bitmap now.</summary>
    public static void Load() => LoadBitmapsFromResources();

    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldGridSelected => NewBitMap("BoldGrid Selected", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldGridCurrent => NewBitMap("BoldGrid Current", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldGridConstraint_true => NewBitMap("BoldGrid Constraint True", false);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldGridConstraint_false => NewBitMap("BoldGrid Constraint False", false);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorDelete => NewBitMap("BoldNavigator Delete", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorFirst => NewBitMap("BoldNavigator First", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorLast => NewBitMap("BoldNavigator Last", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorInsert => NewBitMap("BoldNavigator Insert", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorNext => NewBitMap("BoldNavigator Next", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorPrior => NewBitMap("BoldNavigator Prior", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorMoveUp => NewBitMap("BoldNavigator MoveUp", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBoldNavigatorMoveDown => NewBitMap("BoldNavigator MoveDown", true);
    [Delphi(PortStatus.Ported)] public static Bitmap? bmpBOLDEDIT_ELLIPSIS => NewBitMap("BoldEdit Ellipsis", true);
}
