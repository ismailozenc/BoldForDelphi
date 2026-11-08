// Delphi: Source/Extensions/OLLE/Core/DistributableInfo.pas
// Port map: CSharp/port/units/DistributableInfo.md
// The methods of the OLLE model's classes, which Delphi kept in DistributableInfo.inc. The classes themselves are
// generated, in DistributableInfo.g.cs.

namespace Bold.Olle;

public partial class TCheckingInObjectInfo
{
    /// <summary>The object was checked in at the timestamp: its foreign timestamp moves on, and the hold ends unless it is kept.</summary>
    public partial void AcknowledgeCheckIn(int NewTimeStamp)
    {
        THeldObjectInfo heldObjectInfo = HeldObjectInfo!;
        TForeignObjectInfo foreignObjectInfo = heldObjectInfo.ForeignObjectInfo!;
        if (foreignObjectInfo.ForeignTimeStamp <= NewTimeStamp)
        {
            foreignObjectInfo.ForeignTimeStamp = NewTimeStamp;
        }
        else
        {
            throw new EBold(SysUtils.Format("%s.AcknowledgeCheckIn: New timestamp is older than current", GetType().Name));
        }

        if (KeepHold)
        {
            heldObjectInfo.OriginalLocalTimeStamp = LocalTimeStampAtCheckIn;
        }
        else
        {
            heldObjectInfo.UnLinkAll();
            heldObjectInfo.Delete();
        }

        UnLinkAll();
        Delete();
    }

    /// <summary>The check-in failed: the object is no longer checking in.</summary>
    public partial void FailCheckIn()
    {
        UnLinkAll();
        Delete();
    }
}

public partial class TForeignObjectInfo
{
    /// <summary>Starts checking in the held object.</summary>
    public override partial void StartCheckIn(IBoldValueSpace ValueSpace, bool KeepHold)
    {
        if (HeldObjectInfo is null)
        {
            throw new EBold(SysUtils.Format("%s.StartCheckIn: Object is not held", GetType().Name));
        }

        HeldObjectInfo.StartCheckIn(ValueSpace, KeepHold);
    }

    /// <summary>The object is held from now on, from its owner, with no timestamps yet.</summary>
    public partial void InitializeHolding(TBoldObjectId ObjectId, TForeignPSInfo? Owner)
    {
        HeldObjectInfo = new THeldObjectInfo(BoldSystem);
        LocalId = ((TBoldDefaultID)ObjectId).AsInteger;
        ForeignTimeStamp = 0;
        this.Owner = Owner;
        HeldObjectInfo.OriginalLocalTimeStamp = 0;
    }

    /// <summary>The object came from its owner at the timestamp of its contents; it is held when asked.</summary>
    public partial void Put(IBoldValueSpace ValueSpace, bool Hold, int NewLocalTimeStamp)
    {
        var anObjectId = new TBoldDefaultID { AsInteger = LocalId };
        int NewTimeStamp = ValueSpace.ObjectContentsByObjectId(anObjectId)!.TimeStamp;
        if (NewTimeStamp < ForeignTimeStamp)
        {
            throw new EBold(SysUtils.Format("%s.EnsureForeignInfo: New timestamp is older than current", GetType().Name));
        }

        ForeignTimeStamp = NewTimeStamp;
        if (Hold)
        {
            this.Hold(NewLocalTimeStamp);
        }
    }

    /// <summary>The object is held, as it was at the local timestamp.</summary>
    public partial void Hold(int NewLocalTimeStamp)
    {
        HeldObjectInfo = new THeldObjectInfo(BoldSystem);
        HeldObjectInfo.OriginalLocalTimeStamp = NewLocalTimeStamp;
    }

    /// <summary>A foreign object cannot be lent.</summary>
    public override partial void Get(IBoldValueSpace ValueSpace, bool CheckOut, TForeignPSInfo? Holder) =>
        throw new EBold(SysUtils.Format("%s.Get: Object is a Foreign object", GetType().Name));
}

public partial class THeldObjectInfo
{
    /// <summary>
    /// Starts checking in the held object: its local timestamp is kept and its contents get the foreign timestamp,
    /// the version its owner has.
    /// </summary>
    public partial void StartCheckIn(IBoldValueSpace ValueSpace, bool KeepHold)
    {
        if (CheckingInObjectInfo is not null)
        {
            throw new EBold(SysUtils.Format("%s.StartCheckIn: Object already checking in", GetType().Name));
        }

        var anId = new TBoldDefaultID();
        CheckingInObjectInfo = new TCheckingInObjectInfo(BoldSystem);
        CheckingInObjectInfo.KeepHold = KeepHold;
        anId.AsInteger = ForeignObjectInfo!.LocalId;
        IBoldObjectContents contents = ValueSpace.ObjectContentsByObjectId(anId)!;
        CheckingInObjectInfo.LocalTimeStampAtCheckIn = contents.TimeStamp;
        contents.TimeStamp = ForeignObjectInfo.ForeignTimeStamp;
    }
}

public partial class TOwnObjectInfo
{
    /// <summary>An own object is not checked in: it is checked out.</summary>
    public override partial void StartCheckIn(IBoldValueSpace ValueSpace, bool KeepHold) =>
        throw new EBold(SysUtils.Format("%s.StartCheckIn: Object is own object", GetType().Name));

    /// <summary>Gives the object; it is checked out to the holder when asked.</summary>
    public override partial void Get(IBoldValueSpace ValueSpace, bool CheckOut, TForeignPSInfo? Holder)
    {
        if (CheckOut)
        {
            this.CheckOut(Holder);
        }
    }

    /// <summary>The object is checked out to the holder.</summary>
    public partial void CheckOut(TForeignPSInfo? Holder)
    {
        if (CheckedOutObjectInfo is not null)
        {
            throw new EBold(SysUtils.Format("%s.CheckOut: Object is already checked out", GetType().Name));
        }

        CheckedOutObjectInfo = new TCheckedOutObjectInfo(BoldSystem);
        CheckedOutObjectInfo.Holder = Holder;
    }

    /// <summary>The holder checks the object in; it is no longer checked out when released.</summary>
    public partial void CheckIn(IBoldValueSpace ValueSpace, bool Release, TForeignPSInfo? Holder)
    {
        if (CheckedOutObjectInfo is null)
        {
            throw new EBold(SysUtils.Format("%s.CheckIn: Object not checked out", GetType().Name));
        }

        if (CheckedOutObjectInfo.Holder != Holder)
        {
            throw new EBold(SysUtils.Format("%s.CheckIn: Object checked out to another holder", GetType().Name));
        }

        if (Release)
        {
            CheckedOutObjectInfo.Delete();
        }
    }

    /// <summary>Nothing, as in Delphi.</summary>
    public partial void UnCheckOut(TForeignPSInfo? Holder)
    {
    }
}

public partial class TForeignPSInfo
{
    private const string CheckingInObjectInfos = "ownedObjectInfos.heldObjectInfo.checkingInObjectInfo";

    /// <summary>The storage acknowledged the check-in at the timestamp: every object checking in from it is checked in.</summary>
    public partial void AcknowledgeCheckIn(int NewTimeStamp)
    {
        using var anElement = new TBoldIndirectElement();
        EvaluateExpression(CheckingInObjectInfos, anElement);
        var InfoObjects = (TBoldObjectList)anElement.Value!;
        for (int i = InfoObjects.Count - 1; i >= 0; i--)
        {
            ((TCheckingInObjectInfo)InfoObjects[i]!).AcknowledgeCheckIn(NewTimeStamp);
        }
    }

    /// <summary>The check-in failed: the objects that were to be released are listed, and none is checking in any more.</summary>
    public partial void FailCheckIn(TBoldObjectIdList FailedReleaseList)
    {
        var anId = new TBoldDefaultID(BoldValueSpaceInterfaces.BUSINESSCLASSESROOT_TOPSORTEDINDEX, false);
        using var anElement = new TBoldIndirectElement();
        EvaluateExpression(CheckingInObjectInfos, anElement);
        var InfoObjects = (TBoldObjectList)anElement.Value!;
        // a copy: the list loses each object as it fails (is deleted)
        TCheckingInObjectInfo[] CheckingIn = [.. Enumerable.Range(0, InfoObjects.Count).Select(i => (TCheckingInObjectInfo)InfoObjects[i]!)];
        foreach (TCheckingInObjectInfo aCIInfo in CheckingIn)
        {
            if (!aCIInfo.KeepHold)
            {
                anId.AsInteger = aCIInfo.HeldObjectInfo!.ForeignObjectInfo!.LocalId;
                FailedReleaseList.Add(anId);
            }

            aCIInfo.FailCheckIn();
        }
    }

    /// <summary>True while objects of the storage are checking in.</summary>
    public partial bool IsCheckingIn()
    {
        using var anElement = new TBoldIndirectElement();
        EvaluateExpression(CheckingInObjectInfos + "->size", anElement);
        return ((TBAInteger)anElement.Value!).AsInteger > 0;
    }

    /// <summary>True while a synch with the storage is not acknowledged or failed.</summary>
    public partial bool IsSynching() => OngoingSynchTimestamp != -1;
}
