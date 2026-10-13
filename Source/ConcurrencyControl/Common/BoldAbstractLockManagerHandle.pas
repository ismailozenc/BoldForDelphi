unit BoldAbstractLockManagerHandle; { C# port: CSharp/src/Bold.Propagation/ConcurrencyControl/Common/BoldAbstractLockManagerHandle.cs; symbols and status: CSharp/port/units/BoldAbstractLockManagerHandle.md }

interface

uses
  BoldLockingSupportInterfaces_TLB,
  BoldSubscription;

type
  {forward declarations}
  TBoldAbstractLockManagerHandle = class;

  TBoldAbstractLockManagerHandle = class(TBoldSubscribableComponent)
  protected
    function GetLockManager: IBoldLockManager; virtual; abstract;
  public
    property LockManager: IBoldLockManager read GetLockManager;
  end;

implementation

end.
