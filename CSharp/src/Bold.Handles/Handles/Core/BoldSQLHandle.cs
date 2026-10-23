// Delphi: Source/Handles/Core/BoldSQLHandle.pas
// Port map: CSharp/port/units/BoldSQLHandle.md

using System.ComponentModel;
using static Bold.BoldSubscription;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>A handle on a list of objects of one class, filled by an SQL where clause run by the persistence layer.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLHandle : TBoldNonSystemHandle
{
    private TBoldObjectClass? fBoldObjectClass;
    private string fWhereClause = "";
    private string fOrderByClause = "";
    private string fClassExpressionName = "";
    private TBoldObjectList? fObjectList;
    private TBoldListDupMode fListMode;
    private bool fClearBeforeExecute;
    private TParams? fParams;
    private bool fJoinInheritedTables;
    private int fMaxAnswers;
    private int fOffset;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldSQLHandle(IContainer? Owner = null)
        : base(Owner)
    {
        fClearBeforeExecute = true;
        fListMode = TBoldListDupMode.bldmMerge;
        fParams = new TParams();
        fJoinInheritedTables = true;
        fMaxAnswers = -1;
        fOffset = -1;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSQLHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fObjectList);
            fParams = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetClassExpressionName(string Value)
    {
        if (fClassExpressionName != Value)
        {
            fClassExpressionName = Value;
            SysUtils.FreeAndNil(ref fObjectList);
            SendEvent(this, beValueIdentityChanged);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetListMode(TBoldListDupMode Value)
    {
        if (fListMode != Value)
        {
            fListMode = Value;
            if (fObjectList is not null)
            {
                fObjectList.DuplicateMode = Value;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetParams(TParams Value) => Params.Assign(Value);

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetStaticBoldType() => StaticSystemTypeInfo?.ClassTypeInfoByExpressionName(fClassExpressionName);

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetValue()
    {
        EnsureList(false);
        return fObjectList;
    }

    /// <summary>Creates the (empty) list of the class; raises, when asked to, without a system handle or a valid class.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureList(bool RaiseException)
    {
        if (fObjectList is not null)
        {
            return;
        }

        if (StaticSystemHandle is null && RaiseException)
        {
            throw new EBold(SysUtils.Format(sNoSystemHandle, GetType().Name, "EnsureList", Name)); // do not localize
        }

        if (StaticBoldType is TBoldClassTypeInfo ClassTypeInfo)
        {
            fBoldObjectClass = ClassTypeInfo.ObjectClass;
            TBoldListTypeInfo? ListTypeInfo = StaticSystemTypeInfo!.ListTypeInfoByElement(ClassTypeInfo);
            fObjectList = (TBoldObjectList)TBoldMemberFactory.CreateMemberFromBoldType(ListTypeInfo);
            fObjectList.DuplicateMode = ListMode;
        }
        else if (RaiseException)
        {
            throw new EBold(SysUtils.Format(sCannotCreateListDueToInvalidType, GetType().Name, Name));
        }
    }

    /// <summary>Fetches the objects of the class that match the where clause into the list (emptied first by default).</summary>
    [Delphi(PortStatus.Ported)]
    public void ExecuteSQL()
    {
        if (StaticSystemHandle is null)
        {
            throw new EBold(SysUtils.Format(sNoSystemHandle, GetType().Name, "ExecuteSQL", Name)); // do not localize
        }

        if (!StaticSystemHandle.Active)
        {
            throw new EBold(SysUtils.Format(sSystemHandleNotActive, GetType().Name));
        }

        if (ClearBeforeExecute)
        {
            ClearList();
        }

        EnsureList(true);
        StaticSystemHandle.System!.GetAllInClassWithSQL(fObjectList!, fBoldObjectClass!, SQLWhereClause, SQLOrderByClause, Params, JoinInheritedTables, MaxAnswers, Offset);
    }

    [Delphi(PortStatus.Ported)]
    public void ClearList() => fObjectList?.Clear();

    [Delphi(PortStatus.Ported)]
    public string SQLWhereClause
    {
        get => fWhereClause;
        set => fWhereClause = value;
    }

    [Delphi(PortStatus.Ported)]
    public string SQLOrderByClause
    {
        get => fOrderByClause;
        set => fOrderByClause = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldListDupMode ListMode
    {
        get => fListMode;
        set => SetListMode(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ClassExpressionName
    {
        get => fClassExpressionName;
        set => SetClassExpressionName(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool ClearBeforeExecute
    {
        get => fClearBeforeExecute;
        set => fClearBeforeExecute = value;
    }

    [Delphi(PortStatus.Ported)]
    public int MaxAnswers
    {
        get => fMaxAnswers;
        set => fMaxAnswers = value;
    }

    [Delphi(PortStatus.Ported)]
    public int Offset
    {
        get => fOffset;
        set => fOffset = value;
    }

    [Delphi(PortStatus.Ported)]
    public TParams Params
    {
        get => fParams!;
        set => SetParams(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool JoinInheritedTables
    {
        get => fJoinInheritedTables;
        set => fJoinInheritedTables = value;
    }
}
