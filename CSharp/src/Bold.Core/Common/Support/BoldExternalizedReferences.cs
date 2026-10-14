// Delphi: Source/Common/Support/BoldExternalizedReferences.pas
// Port map: CSharp/port/units/BoldExternalizedReferences.md

namespace Bold;

/// <summary>
/// A reference from one object to another, kept outside the referring object. With ManageReferencedObject the list
/// disposes a referenced object when its reference is replaced or cleared.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalizedReferenceList : TBoldMemoryManagedObject, IDisposable
{
    private TBoldUnOrderedIndexableList? fList;
    private bool FManageReferencedObject;

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalLink")]
    private sealed class TBoldExternalLink : TBoldMemoryManagedObject
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldExternalLink.Referee")] public object? Referee;
        [Delphi(PortStatus.Ported, Symbol = "TBoldExternalLink.Referenced")] public object? Referenced;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalizedIndexList")]
    private sealed class TBoldExternalizedIndexList : TBoldUnOrderedIndexableList
    {
        private static int IX_ExternalRef = -1;

        [Delphi(PortStatus.Ported, Symbol = "TBoldExternalizedIndexList.create")]
        public TBoldExternalizedIndexList()
        {
            BoldIndexableList.SetIndexVariable(ref IX_ExternalRef, AddIndex(new TBoldExternalizedReferenceHashIndex()));
            OwnsEntries = true;
        }

        [Delphi(PortStatus.Ported, Symbol = "TBoldExternalizedIndexList.FindByReferee")]
        public TBoldExternalLink? FindByReferee(object Referee) =>
            (TBoldExternalLink?)((TBoldExternalizedReferenceHashIndex)Indexes(IX_ExternalRef)!).FindByObject(Referee);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalizedReferenceHashIndex")]
    private sealed class TBoldExternalizedReferenceHashIndex : TBoldObjectHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldExternalizedReferenceHashIndex.ItemAsKeyObject")]
        protected override object? ItemAsKeyObject(object? Item) => ((TBoldExternalLink)Item!).Referee;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldExternalizedReferenceList()
    {
        fList = new TBoldExternalizedIndexList();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private void SetManageReferencedObject(bool Value) => FManageReferencedObject = Value;

    [Delphi(PortStatus.Ported)]
    private int GetCount() => fList!.Count;

    [Delphi(PortStatus.Ported)]
    private object? GetReferencedObject(object Referee) => ((TBoldExternalizedIndexList)fList!).FindByReferee(Referee)?.Referenced;

    /// <summary>Replaces the reference of <paramref name="Referee"/>; null removes it.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetReferencedObject(object Referee, object? Referenced)
    {
        var list = (TBoldExternalizedIndexList)fList!;
        TBoldExternalLink? link = list.FindByReferee(Referee);
        if (link is not null)
        {
            if (ManageReferencedObject)
            {
                SysUtils.FreeAndNil(ref link.Referenced);
            }

            list.Remove(link);
        }

        if (Referenced is not null)
        {
            list.Add(new TBoldExternalLink { Referenced = Referenced, Referee = Referee });
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool ManageReferencedObject
    {
        get => FManageReferencedObject;
        set => SetManageReferencedObject(value);
    }

    [Delphi(PortStatus.Ported)]
    public object? ReferencedObjects(object Referee) => GetReferencedObject(Referee);

    /// <summary>The setter of ReferencedObjects.</summary>
    public void SetReferencedObjects(object Referee, object? Referenced) => SetReferencedObject(Referee, Referenced);

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();
}
