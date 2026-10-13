unit BoldPMapper; { C# port: CSharp/src/Bold.Persistence.Sql/PMapper/Core/BoldPMapper.cs; symbols and status: CSharp/port/units/BoldPMapper.md }

interface

uses
  BoldSubscription;

type
  TBoldPersistenceMapper = class;
  TBoldPersistenceMapperClass = class of TBoldPersistenceMapper;

  {---TBoldPersistenceMapper---}
  TBoldPersistenceMapper = class(TBoldSubscribableObject)
  protected
    procedure CreatePersistentStorage; virtual;
  end;

implementation

{---TBoldPersistenceMapper---}
procedure TBoldPersistenceMapper.CreatePersistentStorage;
begin
end;

end.

