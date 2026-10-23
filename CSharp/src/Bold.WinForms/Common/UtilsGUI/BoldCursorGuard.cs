// Delphi: Source/Common/UtilsGUI/BoldCursorGuard.pas
// Port map: CSharp/port/units/BoldCursorGuard.md

using System.Windows.Forms;

namespace Bold;

/// <summary>A guard of the screen cursor (a using block): the cursor shows the new one until the guard is disposed.</summary>
[Delphi(PortStatus.Ported, Note = "An IDisposable for a using block; Delphi's was reference counted.")]
public interface IBoldCursorGuard : IDisposable
{
}

/// <summary>Shows a cursor (the wait cursor by default) until disposed, then the one before.</summary>
[Delphi(PortStatus.Ported, Note = "Cursor.Current stands in for Screen.Cursor.")]
public sealed class TBoldCursorGuard : IBoldCursorGuard
{
    private readonly Cursor fOldCursor;

    [Delphi(PortStatus.Ported)]
    public TBoldCursorGuard(Cursor? NewCursor = null)
    {
        fOldCursor = Cursor.Current;
        Cursor.Current = NewCursor ?? Cursors.WaitCursor;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCursorGuard.Destroy")]
    public void Dispose() => Cursor.Current = fOldCursor;
}
