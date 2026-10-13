unit BoldWAdatamodule; { C# port: CSharp/src/Bold.SourceGenerators/ObjectSpace/IDE/AttributeWizard/BoldWAdatamodule.cs; symbols and status: CSharp/port/units/BoldWAdatamodule.md }

interface

uses
  Windows,
  Messages,
  Classes,
  Graphics,
  Controls,
  Forms,
  Dialogs,
  Menus,
  ImgList;

type
  TdmAttributeWizard = class(TDataModule)
    AttributeWizardMenu: TPopupMenu;
    TestWizard1: TMenuItem;
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  dmAttributeWizard: TdmAttributeWizard;

implementation

{$R *.dfm}

end.
