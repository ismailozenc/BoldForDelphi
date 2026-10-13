unit BoldAbstractPropagatorHandle; { C# port: CSharp/src/Bold.Propagation/Propagator/Common/BoldAbstractPropagatorHandle.cs; symbols and status: CSharp/port/units/BoldAbstractPropagatorHandle.md }

interface

uses
  BoldPropagatorInterfaces_TLB,
  BoldSubscription;

type
  {forward declarations}
  TBoldAbstractPropagatorHandle = class;

  TBoldAbstractPropagatorHandle = class(TBoldSubscribableComponent)
  protected
    function GetClientHandler: IBoldClientHandler; virtual; abstract;
    function GetEventPropagator: IBoldEventPropagator; virtual; abstract;
    function GetConnected: Boolean; virtual; abstract;
    procedure SetConnected(const Value: Boolean); virtual; abstract;
  public
    procedure DoPropagatorCallFailed(Sender: TObject; const ErrorMessage: string); virtual; abstract;
    property ClientHandler: IBoldClientHandler read GetClientHandler;
    property EventPropagator: IBoldEventPropagator read GetEventPropagator;
    property Connected: Boolean read GetConnected write SetConnected;
  end;

implementation

end.
