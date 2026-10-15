// Delphi: Source/ObjectSpace/RTModel/BoldMemberTypeDictionary.pas
// Port map: CSharp/port/units/BoldMemberTypeDictionary.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldMemberTypeDictionary", Symbol = "initialization", Note = "Empty in Delphi; the list is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldMemberTypeDictionary", Symbol = "finalization", Note = "Freed the global list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The registered member classes (attribute and role types), by class and by class name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberTypeList : TBoldIndexableList
{
    private static int IX_MemberName = -1;
    private static int IX_MemberClass = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberTypeList()
    {
        SetIndexCapacity(2);
        BoldIndexableList.SetIndexVariable(ref IX_MemberName, AddIndex(new TMemberNameIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_MemberClass, AddIndex(new TMemberClassIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberTypeDescriptor? GetDescriptorByClass(TClass BoldMemberClass) =>
        (TBoldMemberTypeDescriptor?)((TMemberClassIndex)Indexes(IX_MemberClass)!).FindByMemberClass(BoldMemberClass);

    [Delphi(PortStatus.Ported)]
    private TBoldMemberTypeDescriptor GetMemberTypeDescriptors(int index) => (TBoldMemberTypeDescriptor)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldMemberTypeDescriptor? GetDescriptorByDelphiName(string DelphiName) =>
        (TBoldMemberTypeDescriptor?)((TMemberNameIndex)Indexes(IX_MemberName)!).FindByString(DelphiName);

    [Delphi(PortStatus.Ported)]
    public void AddMemberTypeDescriptor(TClass MemberClass, TBoldAbstractionLevel AbstractionLevel) =>
        Add(new TBoldMemberTypeDescriptor(MemberClass, AbstractionLevel));

    [Delphi(PortStatus.Ported)]
    public void RemoveDescriptorByClass(TClass BoldMemberClass) => Remove(DescriptorByClass(BoldMemberClass));

    /// <summary>The descriptor of the class named <paramref name="DelphiName"/> (ignoring case).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldMemberTypeDescriptor? DescriptorByDelphiName(string DelphiName) => GetDescriptorByDelphiName(DelphiName);

    [Delphi(PortStatus.Ported)]
    public TBoldMemberTypeDescriptor? DescriptorByClass(TClass BoldMemberClass) => GetDescriptorByClass(BoldMemberClass);

    [Delphi(PortStatus.Ported)]
    public TBoldMemberTypeDescriptor Descriptors(int Index) => GetMemberTypeDescriptors(Index);

    [Delphi(PortStatus.Ported, Symbol = "TMemberNameIndex")]
    private sealed class TMemberNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TMemberNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldMemberTypeDescriptor)Item!).MemberClass.Name;
    }

    [Delphi(PortStatus.Ported, Symbol = "TMemberClassIndex")]
    private sealed class TMemberClassIndex : TBoldClassHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TMemberClassIndex.ItemAsKeyClass")]
        protected override TClass ItemAsKeyClass(object? Item) => ((TBoldMemberTypeDescriptor)Item!).MemberClass;

        /// <summary>FindByClass, reachable from the list (the inherited method is visible to subclasses only).</summary>
        internal object? FindByMemberClass(TClass KeyClass) => FindByClass(KeyClass);
    }
}

/// <summary>A member class and whether it is abstract.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberTypeDescriptor
{
    private readonly TClass fMemberClass;
    private readonly TBoldAbstractionLevel fAbstractionLevel;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberTypeDescriptor(TClass MemberClass, TBoldAbstractionLevel AbstractionLevel)
    {
        fMemberClass = MemberClass;
        fAbstractionLevel = AbstractionLevel;
    }

    [Delphi(PortStatus.Ported)] public TClass MemberClass => fMemberClass;
    [Delphi(PortStatus.Ported)] public TBoldAbstractionLevel AbstractionLevel => fAbstractionLevel;
}

/// <summary>The global member type list, created on first use.</summary>
public static partial class BoldMemberTypeDictionary
{
    private static TBoldMemberTypeList? G_BoldMemberTypes;

    [Delphi(PortStatus.Ported)]
    public static TBoldMemberTypeList BoldMemberTypes() => G_BoldMemberTypes ??= new TBoldMemberTypeList();

    [Delphi(PortStatus.Ported)]
    public static bool BoldMemberTypesAssigned() => G_BoldMemberTypes is not null;
}
