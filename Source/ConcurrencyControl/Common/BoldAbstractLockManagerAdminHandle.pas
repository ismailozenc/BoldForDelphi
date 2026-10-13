unit BoldAbstractLockManagerAdminHandle; { C# port: CSharp/src/Bold.Propagation/ConcurrencyControl/Common/BoldAbstractLockManagerAdminHandle.cs; symbols and status: CSharp/port/units/BoldAbstractLockManagerAdminHandle.md }

interface

uses
  BoldLockingSupportInterfaces_TLB,
  BoldSubscription;

type

  {forward declarations}
  TBoldAbstractLockManagerAdminHandle = class;

  TBoldAbstractLockManagerAdminHandle = class(TBoldSubscribableComponent)
  protected
    function GetLockManagerAdmin: IBoldLockManagerAdmin; virtual; abstract;
  public
    property LockManagerAdmin: IBoldLockManagerAdmin read GetLockManagerAdmin;
  end;

implementation

end.

