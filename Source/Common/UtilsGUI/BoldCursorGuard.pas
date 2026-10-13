unit BoldCursorGuard; { C# port: CSharp/src/Bold.WinForms/Common/UtilsGUI/BoldCursorGuard.cs; symbols and status: CSharp/port/units/BoldCursorGuard.md }

interface

uses
  Controls,
  BoldBase;

type
  IBoldCursorGuard = interface(IUnknown)
  end;

  TBoldCursorGuard = class(TBoldRefCountedObject, IBoldCursorGuard)
  private
    fOldCursor: TCursor;
  public
    constructor Create(NewCursor: TCursor = crHourGlass);
    destructor Destroy; override;
  end;

implementation

uses
  Forms;

{ TBoldGuardCursorChange }

constructor TBoldCursorGuard.Create(NewCursor: TCursor);
begin
  fOldCursor := Screen.Cursor;
  Screen.Cursor := NewCursor;
end;

destructor TBoldCursorGuard.Destroy;
begin
  Screen.Cursor := fOldCursor;
  inherited;
end;

end.
