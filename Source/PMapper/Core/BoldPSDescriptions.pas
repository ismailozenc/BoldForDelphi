unit BoldPSDescriptions; { C# port: CSharp/src/Bold.Persistence.Sql/PMapper/Core/BoldPSDescriptions.cs; symbols and status: CSharp/port/units/BoldPSDescriptions.md }

interface

uses
  BoldPSParams;

type
  TBoldPSDescriptionElement = class;
  TBoldPSSystemDescription = class;

  {---TBoldPSDescriptionElement---}
  TBoldPSDescriptionElement = class
  private
    fOwner: TBoldPSDescriptionElement;
  public
    constructor Create(aOwner: TBoldPSDescriptionElement);
    property Owner: TBoldPSDescriptionElement read fOwner;
  end;

  {---TBoldPSSystemDescription---}
  TBoldPSSystemDescription = class(TBoldPSDescriptionElement)
  public
    procedure CreatePersistentStorage(PSParams: TBoldPSParams); virtual; abstract;
  end;

implementation

{---TBoldPSDescriptionElement---}
constructor TBoldPSDescriptionElement.Create(aOwner: TBoldPSDescriptionElement);
begin
  fOwner := aOwner;
end;

end.
