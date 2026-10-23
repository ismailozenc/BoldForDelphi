// Delphi: Source/Common/Support/BoldControlPackDefs.pas
// Port map: CSharp/port/units/BoldControlPackDefs.md

namespace Bold;

/// <summary>When a changed value goes back to the object: at each change, when the control is left, or on demand.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldApplyPolicy { bapChange, bapExit, bapDemand }

/// <summary>What dragging from a control drags: nothing, or the selected objects.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldDragMode { bdgNone, bdgSelection }

/// <summary>What dropping objects on a control does with them.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldDropMode { bdpNone, bdpInsert, bdpAppend, bdpReplace }

/// <summary>What the delete item of a Bold popup menu does.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldPopupDeleteType { bpdNone, bdpModel, bpdDelete, bdpUnlink }

/// <summary>The states of a follower: whether its display is current, changed by the user, out of date, or inactive.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldFollowerState
{
    bfsEmpty, bfsCurrent, bfsDirty,
    bfsValueOutOfDate, bfsSubscriptionOutOfDate,
    bfsInactiveValidElement, bfsInactiveInvalidElement, bfsActivating,
}

/// <summary>Where a list shows the empty (nil) element, if at all.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldNilElementMode { neNone, neInsertFirst, neAddLast }

/// <summary>Unit-level constants of BoldControlPackDefs: follower state groups, node part indexes and requested events.</summary>
public static partial class BoldControlPackDefs
{
    [Delphi(PortStatus.Ported)]
    public static readonly DelphiSet<TBoldFollowerState> bfsDisplayable =
        DelphiSet<TBoldFollowerState>.Of(TBoldFollowerState.bfsCurrent, TBoldFollowerState.bfsDirty);

    [Delphi(PortStatus.Ported)]
    public static readonly DelphiSet<TBoldFollowerState> bfsOutOfDate =
        DelphiSet<TBoldFollowerState>.Of(TBoldFollowerState.bfsValueOutOfDate, TBoldFollowerState.bfsSubscriptionOutOfDate);

    [Delphi(PortStatus.Ported)]
    public static readonly DelphiSet<TBoldFollowerState> bfsActive = DelphiSet<TBoldFollowerState>.Of(
        TBoldFollowerState.bfsEmpty, TBoldFollowerState.bfsCurrent, TBoldFollowerState.bfsDirty,
        TBoldFollowerState.bfsValueOutOfDate, TBoldFollowerState.bfsSubscriptionOutOfDate);

    [Delphi(PortStatus.Ported)]
    public static readonly DelphiSet<TBoldFollowerState> bfdNeedResubscribe =
        DelphiSet<TBoldFollowerState>.Of(TBoldFollowerState.bfsSubscriptionOutOfDate, TBoldFollowerState.bfsActivating);

    [Delphi(PortStatus.Ported)] public const int BoldNodeListIndex = 0;
    [Delphi(PortStatus.Ported)] public const int BoldNodeIconIndex = 1;
    [Delphi(PortStatus.Ported)] public const int BoldNodeTextIndex = 2;
    [Delphi(PortStatus.Ported)] public const int BoldNodeFirstColumnIndex = 3;

    [Delphi(PortStatus.Ported)] public const int breHandleNil = 42;
    [Delphi(PortStatus.Ported)] public const int breValueIdentityChanged = 43;
    [Delphi(PortStatus.Ported)] public const int breListIdentityChanged = 44;
    [Delphi(PortStatus.Ported)] public const int breHandleIndexChanged = 45;

    [Delphi(PortStatus.Ported)] public const int beListPartEnabledChanged = 100;
}
