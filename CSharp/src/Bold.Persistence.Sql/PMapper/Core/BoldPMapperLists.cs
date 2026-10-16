// Delphi: Source/PMapper/Core/BoldPMapperLists.pas
// Port map: CSharp/port/units/BoldPMapperLists.md
// The registries of mapper classes: member mappers by class name (and class), system and object mappers by name.

[assembly: Delphi(PortStatus.Ported, Unit = "BoldPMapperLists", Symbol = "initialization", Note = "Empty in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMapperLists", Symbol = "finalization", Note = "Freed the registries; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A registered mapper class with its name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceMapperDescriptor
{
    private readonly string fName;
    private readonly Type fPersistenceMapperClass;

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceMapperDescriptor(string name, Type PersistenceMapperClass)
    {
        fName = name;
        fPersistenceMapperClass = PersistenceMapperClass;
    }

    [Delphi(PortStatus.Ported)] public string Name => fName;
    [Delphi(PortStatus.Ported)] public Type PersistenceMapperClass => fPersistenceMapperClass;
}

/// <summary>The registered member mapper classes, by class name and class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberPersistenceMapperDescriptorList : TBoldIndexableList
{
    private static int IX_MemberPMapperName = -1;
    private static int IX_MemberPMapperClass = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberPersistenceMapperDescriptorList()
    {
        SetIndexCapacity(2);
        BoldIndexableList.SetIndexVariable(ref IX_MemberPMapperName, AddIndex(new TBoldMemberPMapperNameIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_MemberPMapperClass, AddIndex(new TBoldMemberPMapperClassIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberPersistenceMapperDescriptor? GetDescriptorBydelphiName(string name) =>
        (TBoldMemberPersistenceMapperDescriptor?)((TBoldMemberPMapperNameIndex)Indexes(IX_MemberPMapperName)!).FindByString(name);

    [Delphi(PortStatus.Ported)]
    private TBoldMemberPersistenceMapperDescriptor GetDescriptors(int index) => (TBoldMemberPersistenceMapperDescriptor)this[index]!;

    [Delphi(PortStatus.Ported)]
    public void AddDescriptor(Type MemberPersistenceMapperClass, TBoldAbstractionLevel AbstractionLevel) =>
        Add(new TBoldMemberPersistenceMapperDescriptor(MemberPersistenceMapperClass, AbstractionLevel));

    [Delphi(PortStatus.Ported)]
    public void RemoveDescriptorByClass(Type aClass)
    {
        System.Diagnostics.Debug.Assert(DescriptorByDelphiName(aClass.Name) is not null, "Trying to remove TBoldMemberPersistenceMapperDescriptor " + aClass.Name + " not in list");
        Remove(DescriptorByDelphiName(aClass.Name));
    }

    /// <summary>The descriptor of the named mapper; for the default name, the mapper of the type's mapping (or the default mapping).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldMemberPersistenceMapperDescriptor? DescriptorForModelNameWithDefaultSupport(string ModelName, string DefaultMapperName, TBoldTypeNameDictionary TypeNameDictionary)
    {
        string ActualMapperName;
        if (SysUtils.SameText(DefaultMapperName, BoldDefs.DEFAULTNAME))
        {
            TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(ModelName) ?? TypeNameDictionary.MappingForModelName(BoldDefs.DEFAULTNAME);
            ActualMapperName = Mapping?.ExpandedMapperName ?? "";
        }
        else
        {
            ActualMapperName = DefaultMapperName;
        }

        return DescriptorByDelphiName(ActualMapperName);
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldMemberPersistenceMapperDescriptor? DescriptorByDelphiName(string name) => GetDescriptorBydelphiName(name);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldMemberPersistenceMapperDescriptor Descriptors(int index) => GetDescriptors(index);
}

/// <summary>A registered member mapper class with its abstraction level.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberPersistenceMapperDescriptor : TBoldPersistenceMapperDescriptor
{
    private readonly TBoldAbstractionLevel fAbstractionLevel;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberPersistenceMapperDescriptor(Type MemberPersistenceMapperClass, TBoldAbstractionLevel AbstractionLevel)
        : base(MemberPersistenceMapperClass.Name, MemberPersistenceMapperClass)
    {
        fAbstractionLevel = AbstractionLevel;
    }

    [Delphi(PortStatus.Ported)] private Type GetMemberPersistenceMapperClass() => PersistenceMapperClass;
    [Delphi(PortStatus.Ported)] public Type MemberPersistenceMapperClass => GetMemberPersistenceMapperClass();
    [Delphi(PortStatus.Ported)] public TBoldAbstractionLevel AbstractionLevel => fAbstractionLevel;

    [Delphi(PortStatus.Ported)]
    public bool CanStore(string ContentName) => BoldPMappers.ClassCanStore(MemberPersistenceMapperClass, ContentName);
}

/// <summary>The registered system mapper classes, by name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemPersistenceMapperDescriptorList : TBoldIndexableList
{
    private static int IX_SystemPMapperName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldSystemPersistenceMapperDescriptorList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_SystemPMapperName, AddIndex(new TBoldSystemPMapperNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemPersistenceMapperDescriptor? GetDescriptorByName(string name) =>
        (TBoldSystemPersistenceMapperDescriptor?)((TBoldSystemPMapperNameIndex)Indexes(IX_SystemPMapperName)!).FindByString(name);

    [Delphi(PortStatus.Ported)]
    private TBoldSystemPersistenceMapperDescriptor GetDescriptors(int index) => (TBoldSystemPersistenceMapperDescriptor)this[index]!;

    [Delphi(PortStatus.Ported)]
    public void RemoveDescriptorByName(string Name)
    {
        System.Diagnostics.Debug.Assert(DescriptorByName(Name) is not null, "Trying to remove TBoldSystemPersistenceMapperDescriptor descriptor " + Name + " not in list");
        Remove(DescriptorByName(Name));
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldSystemPersistenceMapperDescriptor? DescriptorByName(string name) => GetDescriptorByName(name);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldSystemPersistenceMapperDescriptor Descriptors(int index) => GetDescriptors(index);
}

/// <summary>A registered system mapper class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemPersistenceMapperDescriptor : TBoldPersistenceMapperDescriptor
{
    [Delphi(PortStatus.Ported)]
    public TBoldSystemPersistenceMapperDescriptor(string name, Type BoldSystemPersistenceMapperClass)
        : base(name, BoldSystemPersistenceMapperClass)
    {
    }

    [Delphi(PortStatus.Ported)] private Type GetSystemPersistenceMapperClass() => PersistenceMapperClass;
    [Delphi(PortStatus.Ported)] public Type SystemPersistenceMapperClass => GetSystemPersistenceMapperClass();
}

/// <summary>The registered object mapper classes, by name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectPersistenceMapperDescriptorList : TBoldIndexableList
{
    private static int IX_ObjectPMapperName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectPersistenceMapperDescriptorList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_ObjectPMapperName, AddIndex(new TBoldObjectPMapperNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectPersistenceMapperDescriptor? GetDescriptorByName(string name) =>
        (TBoldObjectPersistenceMapperDescriptor?)((TBoldObjectPMapperNameIndex)Indexes(IX_ObjectPMapperName)!).FindByString(name);

    [Delphi(PortStatus.Ported)]
    private TBoldObjectPersistenceMapperDescriptor GetDescriptors(int index) => (TBoldObjectPersistenceMapperDescriptor)this[index]!;

    [Delphi(PortStatus.Ported)]
    public void RemoveDescriptorByName(string Name)
    {
        System.Diagnostics.Debug.Assert(DescriptorByName(Name) is not null, "Trying to remove TBoldObjectPersistenceMapperDescriptor descriptor " + Name + " not in list");
        Remove(DescriptorByName(Name));
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldObjectPersistenceMapperDescriptor? DescriptorByName(string name) => GetDescriptorByName(name);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldObjectPersistenceMapperDescriptor Descriptors(int index) => GetDescriptors(index);
}

/// <summary>A registered object mapper class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectPersistenceMapperDescriptor : TBoldPersistenceMapperDescriptor
{
    [Delphi(PortStatus.Ported)]
    public TBoldObjectPersistenceMapperDescriptor(string name, Type BoldObjectPersistenceMapperClass)
        : base(name, BoldObjectPersistenceMapperClass)
    {
    }

    [Delphi(PortStatus.Ported)] private Type GetObjectPersistenceMapperClass() => PersistenceMapperClass;
    [Delphi(PortStatus.Ported)] public Type ObjectPersistenceMapper => GetObjectPersistenceMapperClass();
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldMemberPMapperNameIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldMemberPersistenceMapperDescriptor)Item!).MemberPersistenceMapperClass.Name;
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldMemberPMapperClassIndex : TBoldClassHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override TClass ItemAsKeyClass(object? Item) => ((TBoldMemberPersistenceMapperDescriptor)Item!).MemberPersistenceMapperClass;
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldSystemPMapperNameIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldSystemPersistenceMapperDescriptor)Item!).Name;
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldObjectPMapperNameIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldObjectPersistenceMapperDescriptor)Item!).Name;
}

/// <summary>The global registries of mapper classes, made on first use.</summary>
public static partial class BoldPMapperLists
{
    private static TBoldMemberPersistenceMapperDescriptorList? G_BoldMemberPersistenceMappers;
    private static TBoldSystemPersistenceMapperDescriptorList? G_BoldSystemPersistenceMappers;
    private static TBoldObjectPersistenceMapperDescriptorList? G_BoldObjectPersistenceMappers;

    [Delphi(PortStatus.Ported)]
    public static TBoldMemberPersistenceMapperDescriptorList BoldMemberPersistenceMappers() => G_BoldMemberPersistenceMappers ??= new();

    [Delphi(PortStatus.Ported)] public static bool BoldMemberPersistenceMappersAssigned() => G_BoldMemberPersistenceMappers is not null;

    [Delphi(PortStatus.Ported)]
    public static TBoldSystemPersistenceMapperDescriptorList BoldSystemPersistenceMappers() => G_BoldSystemPersistenceMappers ??= new();

    [Delphi(PortStatus.Ported)] public static bool BoldSystemPersistenceMappersAssigned() => G_BoldSystemPersistenceMappers is not null;

    [Delphi(PortStatus.Ported)]
    public static TBoldObjectPersistenceMapperDescriptorList BoldObjectPersistenceMappers() => G_BoldObjectPersistenceMappers ??= new();

    [Delphi(PortStatus.Ported)] public static bool BoldObjectPersistenceMappersAssigned() => G_BoldObjectPersistenceMappers is not null;
}
