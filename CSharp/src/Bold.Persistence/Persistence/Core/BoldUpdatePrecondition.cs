// Delphi: Source/Persistence/Core/BoldUpdatePrecondition.pas
// Port map: CSharp/port/units/BoldUpdatePrecondition.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldUpdatePrecondition", Symbol = "initialization", Note = "Registers the precondition streamer from the Bold.Persistence module initializer.")]

namespace Bold;

/// <summary>A condition an update must meet in the persistence layer; it reports why it failed.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldUpdatePrecondition : TBoldNonRefCountedObject, IBoldStreamable
{
    [Delphi(PortStatus.Ported)]
    protected TBoldUpdatePrecondition()
    {
    }

    [Delphi(PortStatus.Ported)] public abstract string GetStreamName();
    [Delphi(PortStatus.Ported)] protected virtual string GetFailureReason() => "";
    [Delphi(PortStatus.Ported)] protected virtual bool GetFailed() => false;

    /// <summary>Copies the results (what failed) from a precondition evaluated elsewhere.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void AssignOutValues(TBoldUpdatePrecondition Source);

    [Delphi(PortStatus.Ported)] public bool Failed => GetFailed();
    [Delphi(PortStatus.Ported)] public string FailureReason => GetFailureReason();
}

/// <summary>
/// Optimistic locking: the values the objects had when read (in a free-standing value space); the persistence layer
/// lists the objects whose stored values differ.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOptimisticLockingPrecondition : TBoldUpdatePrecondition, IDisposable
{
    private TBoldFreeStandingValueSpace? fFreeStandingValueSpace;
    private TBoldObjectIdList? fFailureList;

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fFreeStandingValueSpace);
        SysUtils.FreeAndNil(ref fFailureList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] private IBoldValueSpace GetValueSpace() => FreeStandingValueSpace;
    [Delphi(PortStatus.Ported)] private TBoldFreeStandingValueSpace GetFreeStandingValueSpace() => fFreeStandingValueSpace ??= new TBoldFreeStandingValueSpace();
    [Delphi(PortStatus.Ported)] private TBoldObjectIdList GetFailureList() => fFailureList ??= new TBoldObjectIdList();

    /// <summary>True when the value space holds any object to check.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetHasOptimisticLocks()
    {
        using var Ids = new TBoldObjectIdList();
        ValueSpace.AllObjectIds(Ids, true);
        return Ids.Count != 0;
    }

    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldUpdatePrecondition.OptimisticLockingPreConditionStreamName;

    [Delphi(PortStatus.Ported)]
    protected override string GetFailureReason() => SysUtils.Format(PersistenceConsts.sOptimisticLockingFailedForNObjects, FailureList.Count);

    [Delphi(PortStatus.Ported)] protected override bool GetFailed() => FailureList.Count != 0;

    [Delphi(PortStatus.Ported)] protected TBoldFreeStandingValueSpace FreeStandingValueSpace => GetFreeStandingValueSpace();

    [Delphi(PortStatus.Ported)]
    public override void AssignOutValues(TBoldUpdatePrecondition Source)
    {
        if (Source is TBoldOptimisticLockingPrecondition other)
        {
            FailureList.AddList(other.FailureList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddFailedObject(TBoldObjectId ObjectId)
    {
        if (!FailureList.IdInList(ObjectId))
        {
            FailureList.Add(ObjectId);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ClearValueSpace() => SysUtils.FreeAndNil(ref fFreeStandingValueSpace);

    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpace => GetValueSpace();
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList FailureList => GetFailureList();
    [Delphi(PortStatus.Ported)] public bool HasOptimisticLocks => GetHasOptimisticLocks();
}

/// <summary>The base streamer of preconditions; writes nothing of its own yet, as in Delphi.</summary>
[Delphi(PortStatus.Ported)]
internal abstract class TBoldXMLPreConditionStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node) => base.WriteObject(Obj, Node);

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node) => base.ReadObject(Obj, Node);
}

/// <summary>Streams the read values (only with a model-aware stream manager) and the failure list.</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLOptimisticLockingPreConditionStreamer : TBoldXMLPreConditionStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldUpdatePrecondition.OptimisticLockingPreConditionStreamName;

    /// <summary>Writes the value space's current values and the failure list. Like Delphi, does not call the inherited WriteObject.</summary>
    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        var Condition = (TBoldOptimisticLockingPrecondition)Obj;
        if (Node.Manager is TBoldDefaultXMLStreamManager Manager)
        {
            using var IdList = new TBoldObjectIdList();
            Condition.ValueSpace.AllObjectIds(IdList, true);
            TBoldValuePersistenceStateSet old = Manager.PersistenceStatesToBeStreamed;
            Manager.PersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsCurrent);
            Manager.WriteValueSpace(Condition.ValueSpace, IdList, null, Node.NewSubNode("ValueSpace"));
            Manager.PersistenceStatesToBeStreamed = old;
        }

        Node.WriteSubNodeObject("FailureList", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, Condition.FailureList);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        var Condition = (TBoldOptimisticLockingPrecondition)Obj;
        if (Node.Manager is TBoldDefaultXMLStreamManager Manager)
        {
            Manager.ReadValueSpace(Condition.ValueSpace, Node.GetSubNode("ValueSpace")!);
        }

        var IdList = (TBoldObjectIdList?)Node.ReadSubNodeObject("FailureList", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME);
        Condition.FailureList.Clear();
        if (IdList is not null)
        {
            Condition.FailureList.AddList(IdList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldOptimisticLockingPrecondition();
}

/// <summary>Stream name and streamer registration of BoldUpdatePrecondition.</summary>
public static partial class BoldUpdatePrecondition
{
    internal const string OptimisticLockingPreConditionStreamName = "OptimisticLockingPreCondition";

    internal static void RegisterStreamers() =>
        TBoldXMLStreamerRegistry.MainStreamerRegistry().RegisterStreamer(new TBoldXMLOptimisticLockingPreConditionStreamer());
}
