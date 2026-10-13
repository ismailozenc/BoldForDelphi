unit BoldExpertMenus; { C# port: not ported (IDE / design-time code); see CSharp/port/units/BoldExpertMenus.md }

interface

uses
  Menus,
  Classes;

type
  TdmExpertMenus = class(TDataModule)
    GettingStartedMenu: TPopupMenu;
    GettingStarted1: TMenuItem;
  private
    { Private declarations }
  public
    { Public declarations }
  end;

implementation

{$R *.DFM}

end.
