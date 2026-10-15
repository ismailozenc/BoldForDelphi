// Delphi: Source/ObjectSpace/RTModel/BoldGeneratedCodeDictionary.pas
// Port map: CSharp/port/units/BoldGeneratedCodeDictionary.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGeneratedCodeDictionary", Symbol = "initialization", Note = "The global list is created by its static field initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGeneratedCodeDictionary", Symbol = "finalization", Note = "Freed the global list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Installs the generated business classes of a model into the list.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TInstallBusinessClasses(TBoldGeneratedClassList BoldObjectClasses);

/// <summary>Installs the generated list classes of a model into the list.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TInstallObjectListClasses(TBoldGeneratedClassList BoldObjectListClasses);

/// <summary>The models with generated code, by expression name: what code generation registers when its unit loads.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGeneratedCodeList : TBoldIndexableList
{
    private static int IX_GeneratedCodeExpressionName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedCodeList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_GeneratedCodeExpressionName, AddIndex(new TGeneratedCodeExpressionNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldGeneratedCodeDescriptor? GetDescriptorByExpressionName(string ExpressionName) =>
        (TBoldGeneratedCodeDescriptor?)((TGeneratedCodeExpressionNameIndex)Indexes(IX_GeneratedCodeExpressionName)!).FindByString(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private TBoldGeneratedCodeDescriptor GetModelDescriptors(int index) => (TBoldGeneratedCodeDescriptor)base[index]!;

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedCodeDescriptor AddGeneratedCodeDescriptorWithFunc(string ExpressionName, TInstallBusinessClasses? InstBusiClasses,
        TInstallObjectListClasses? InstObjListClasses, string CRC = "")
    {
        var result = new TBoldGeneratedCodeDescriptor(ExpressionName, InstBusiClasses, InstObjListClasses, CRC);
        Add(result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedCodeDescriptor? DescriptorByExpressionName(string ExpressionName) => GetDescriptorByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedCodeDescriptor ModelEntries(int index) => GetModelDescriptors(index);

    [Delphi(PortStatus.Ported, Symbol = "TGeneratedCodeExpressionNameIndex")]
    private sealed class TGeneratedCodeExpressionNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TGeneratedCodeExpressionNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldGeneratedCodeDescriptor)Item!).ExpressionName;
    }
}

/// <summary>The generated code of one model: its name, a CRC of the model, and the routines that install its classes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGeneratedCodeDescriptor
{
    private readonly string fExpressionName;
    private readonly string fCRC;
    private readonly TInstallBusinessClasses? fInstallBusinessClasses;
    private readonly TInstallObjectListClasses? fInstallObjectListClasses;

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedCodeDescriptor(string ExpressionName, TInstallBusinessClasses? InstallBusinessClasses,
        TInstallObjectListClasses? InstallObjectListClasses, string CRC)
    {
        fExpressionName = ExpressionName;
        fInstallBusinessClasses = InstallBusinessClasses;
        fInstallObjectListClasses = InstallObjectListClasses;
        fCRC = CRC;
    }

    [Delphi(PortStatus.Ported)] public TInstallBusinessClasses? InstallBusinessClasses => fInstallBusinessClasses;
    [Delphi(PortStatus.Ported)] public TInstallObjectListClasses? InstallObjectListClasses => fInstallObjectListClasses;
    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public string CRC => fCRC;
}

/// <summary>Generated classes by expression name (ignoring case).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGeneratedClassList : TBoldIndexableList
{
    private static int IX_GeneratedClassExpressionName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedClassList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_GeneratedClassExpressionName, AddIndex(new TGeneratedClassExpressionNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldGeneratedClassDescriptor? GetDescriptorByExpressionName(string ExpressionName) =>
        (TBoldGeneratedClassDescriptor?)((TGeneratedClassExpressionNameIndex)Indexes(IX_GeneratedClassExpressionName)!).FindByString(ExpressionName);

    [Delphi(PortStatus.Ported)]
    public void AddEntry(TBoldGeneratedClassDescriptor BoldObjectClassEntry) => Add(BoldObjectClassEntry);

    [Delphi(PortStatus.Ported)]
    public void AddObjectEntry(string ExpressionName, TClass AClass) => AddEntry(new TBoldGeneratedClassDescriptor(ExpressionName, AClass));

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedClassDescriptor? EntryByExpressionName(string ExpressionName) => GetDescriptorByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported, Symbol = "TGeneratedClassExpressionNameIndex")]
    private sealed class TGeneratedClassExpressionNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TGeneratedClassExpressionNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldGeneratedClassDescriptor)Item!).ExpressionName;
    }
}

/// <summary>A generated class and the expression name of its model element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGeneratedClassDescriptor
{
    private readonly string fExpressionName;
    private readonly TClass fClass;

    [Delphi(PortStatus.Ported)]
    public TBoldGeneratedClassDescriptor(string ExpressionName, TClass AClass)
    {
        fClass = AClass;
        fExpressionName = ExpressionName;
    }

    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public TClass TheClass => fClass;
}

/// <summary>The global list of models with generated code.</summary>
public static partial class BoldGeneratedCodeDictionary
{
    private static readonly TBoldGeneratedCodeList? G_BoldGeneratedCodes = new();

    /// <summary>The global list; raises EBoldInternal if it does not exist (Delphi frees it at shutdown).</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldGeneratedCodeList GeneratedCodes() =>
        G_BoldGeneratedCodes ?? throw new EBoldInternal("GeneratedCodes: BoldGeneratedCodeDictionary not initialized");

    [Delphi(PortStatus.Ported)]
    public static bool BoldGeneratedCodesAssigned() => G_BoldGeneratedCodes is not null;
}
