// Delphi: Source/Common/TaggedValues/BoldTaggedValueList.pas
// Port map: CSharp/port/units/BoldTaggedValueList.md

namespace Bold;

/// <summary>The tagged value definitions of one UML element kind: type, tag and default value, in order and by tag.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTaggedValueList : TBoldMemoryManagedObject, IDisposable
{
    private readonly TBoldObjectArray fList;
    private readonly TBoldTaggedValueDefinitionIndex fDefinitionIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValueList()
    {
        fList = new TBoldObjectArray(1, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));
        fDefinitionIndex = new TBoldTaggedValueDefinitionIndex();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        fList.Dispose();
        fDefinitionIndex.Dispose();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private int GetCount() => fList.Count;

    [Delphi(PortStatus.Ported)]
    private TBoldTaggedValueDefinition GetDefinition(int i) => (TBoldTaggedValueDefinition)fList[i]!;

    [Delphi(PortStatus.Ported)]
    private string GetDefaultValueForTag(string Tag)
    {
        TBoldTaggedValueDefinition? def = GetDefinitionForTag(Tag);
        return def is not null
            ? def.DefaultValue
            : throw new EBoldInternal(SysUtils.Format("%s.GetDefaultValueForTag: Unknown tag \"%s\"", GetType().Name, Tag));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldTaggedValueDefinition? GetDefinitionForTag(string Tag) =>
        (TBoldTaggedValueDefinition?)fDefinitionIndex.FindByString(Tag);

    [Delphi(PortStatus.Ported)]
    public void Add(string TypeName, string Tag, string DefaultValue)
    {
        var def = new TBoldTaggedValueDefinition(TypeName, Tag, DefaultValue);
        fList.Add(def);
        fDefinitionIndex.Add(def);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValueDefinition Definition(int i) => GetDefinition(i);

    /// <summary>The default value of <paramref name="Tag"/>; raises EBoldInternal for an unknown tag.</summary>
    [Delphi(PortStatus.Ported)]
    public string DefaultValueForTag(string Tag) => GetDefaultValueForTag(Tag);

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValueDefinition? DefinitionForTag(string Tag) => GetDefinitionForTag(Tag);

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();
}

/// <summary>One tagged value definition. The strings are shared through the shared string manager.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTaggedValueDefinition : TBoldMemoryManagedObject
{
    private readonly string fTag;
    private readonly string fDefaultValue;
    private readonly string fTypeName;

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValueDefinition(string TypeName, string Tag, string DefaultValue)
    {
        TBoldSharedStringManager m = BoldSharedStrings.BoldSharedStringManager();
        fTag = m.GetSharedString(Tag);
        fTypeName = m.GetSharedString(TypeName);
        fDefaultValue = m.GetSharedString(DefaultValue);
    }

    [Delphi(PortStatus.Ported)]
    public string Tag => fTag;

    [Delphi(PortStatus.Ported)]
    public string DefaultValue => fDefaultValue;

    [Delphi(PortStatus.Ported)]
    public string TypeName => fTypeName;
}

/// <summary>Tagged value lists by UML element kind name ("Class", "Attribute", ...).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTaggedValuePerClassList : TBoldMemoryManagedObject, IDisposable
{
    private TBoldNamedValueList? fList;

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValuePerClassList()
    {
        fList = new TBoldNamedValueList();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        if (fList is not null)
        {
            for (int i = 0; i < fList.Count; i++)
            {
                (fList[i].aObject as IDisposable)?.Dispose();
            }

            fList.Dispose();
            fList = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>The list for <paramref name="ClassName"/>, created empty the first time it is asked for.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldTaggedValueList GetListForClassName(string ClassName)
    {
        var result = (TBoldTaggedValueList?)fList!.ObjectByName(ClassName);
        if (result is null)
        {
            result = new TBoldTaggedValueList();
            fList.AddEntry(ClassName, "", result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private string GetDefaultForClassAndTag(string ClassName, string Tag) => ListForClassName(ClassName).DefaultValueForTag(Tag);

    [Delphi(PortStatus.Ported)]
    public TBoldTaggedValueList ListForClassName(string ClassName) => GetListForClassName(ClassName);

    [Delphi(PortStatus.Ported)]
    public string DefaultForClassAndTag(string ClassName, string Tag) => GetDefaultForClassAndTag(ClassName, Tag);
}

/// <summary>Tagged value definitions keyed by tag, case-sensitive.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTaggedValueDefinitionIndex : TBoldCaseSensitiveStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldTaggedValueDefinition)Item!).Tag;
}
