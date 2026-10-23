// Delphi: Source/MoldModel/CodeGenerator/BoldGeneratorTemplates.pas
// Port map: CSharp/port/units/BoldGeneratorTemplates.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManagerClass", Note = "A System.Type of a TBoldGeneratorTemplateManager subclass.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManager.InitializeCOMTemplateList", Note = "COM templates go with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManager.GetCOMTemplateList", Note = "COM templates go with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManager.ComFileTemplates", Note = "COM templates go with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManager.MethodToCOMHeader", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplates", Symbol = "TBoldGeneratorTemplateManager.MethodToCOMCall", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]

namespace Bold;

/// <summary>
/// The templates of one target language, and the language rules the generator needs: method headers, inherited calls,
/// qualifier parameters and how to find a method in a file.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldGeneratorTemplateManager
{
    private TBoldTemplateList? fTemplateList;

    [Delphi(PortStatus.Ported)] protected abstract TBoldTemplateHolder GetDerivedMethodTemplate();
    [Delphi(PortStatus.Ported)] protected abstract TBoldTemplateHolder GetReverseDeriveMethodTemplate();
    [Delphi(PortStatus.Ported)] protected abstract TBoldTemplateHolder GetMethodTemplate();
    [Delphi(PortStatus.Ported)] protected abstract TBoldTemplateHolder GetIncFileHeaderTemplate();

    [Delphi(PortStatus.Ported)]
    protected virtual void InitializeTemplateList(TBoldTemplateList TemplateList)
    {
    }

    [Delphi(PortStatus.Ported)] protected abstract string GetDefaultIncFileExtension();

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldTemplateHolder? GetPersistenceInterfaceTemplate() => null;

    [Delphi(PortStatus.Ported)]
    protected TBoldTemplateList GetTemplateList()
    {
        if (fTemplateList is null)
        {
            fTemplateList = new TBoldTemplateList();
            InitializeTemplateList(fTemplateList);
        }

        return fTemplateList;
    }

    [Delphi(PortStatus.Ported)] public abstract string ReadMethodSignature(string ClassName, string AttributeName, string AttributeType);
    [Delphi(PortStatus.Ported)] public abstract string WriteMethodSignature(string ClassName, string AttributeName, string AttributeType);
    [Delphi(PortStatus.Ported)] public abstract string MethodToCodeHeader(TMoldClass OwningClass, TMoldMethod Method, int TagValue, bool AddSignature, bool AutoOverride);
    [Delphi(PortStatus.Ported)] public abstract bool StringContainsMethodHeader(string s);
    [Delphi(PortStatus.Ported)] public abstract void AddQualifierPropertyParam(ref string Params, string ParamName, string ParamType);
    [Delphi(PortStatus.Ported)] public abstract void AddQualifierFunctionParam(ref string Params, string ParamName, string ParamType);
    [Delphi(PortStatus.Ported)] public abstract string GetSearchStringfromMethodHeader(string header, bool SearchParameterList);
    [Delphi(PortStatus.Ported)] public abstract string GenerateInheritedCall(TMoldClass MoldClass, TMoldMethod MoldMethod);

    [Delphi(PortStatus.Ported)] public TBoldTemplateList FileTemplates => GetTemplateList();
    [Delphi(PortStatus.Ported)] public TBoldTemplateHolder? PersistenceInterfaceTemplate => GetPersistenceInterfaceTemplate();
    [Delphi(PortStatus.Ported)] public TBoldTemplateHolder MethodTemplate => GetMethodTemplate();
    [Delphi(PortStatus.Ported)] public TBoldTemplateHolder DerivedMethodTemplate => GetDerivedMethodTemplate();
    [Delphi(PortStatus.Ported)] public TBoldTemplateHolder ReverseDeriveMethodTemplate => GetReverseDeriveMethodTemplate();
    [Delphi(PortStatus.Ported)] public TBoldTemplateHolder IncFileHeaderTemplate => GetIncFileHeaderTemplate();
    [Delphi(PortStatus.Ported)] public string DefaultIncFileExtension => GetDefaultIncFileExtension();
}

/// <summary>Unit-level constants, variables and routines of BoldGeneratorTemplates.</summary>
public static class BoldGeneratorTemplates
{
    [Delphi(PortStatus.Ported)] public const int publicTag = 0;
    [Delphi(PortStatus.Ported)] public const int protectedTag = 1;
    [Delphi(PortStatus.Ported)] public const int PrivateTag = 2;
    [Delphi(PortStatus.Ported)] public const int ImplementationTag = 3;

    private static TBoldGeneratorTemplateManager? G_BoldGeneratorTemplateManager;
    private static Type? fBoldGeneratorTemplateManagerClass;

    /// <summary>The template manager class to create (a language's InstallTemplates sets it).</summary>
    [Delphi(PortStatus.Ported, Note = "Setting another class replaces the manager on its next use; Delphi kept the first manager made.")]
    public static Type? BoldGeneratorTemplateManagerClass
    {
        get => fBoldGeneratorTemplateManagerClass;
        set
        {
            if (value != fBoldGeneratorTemplateManagerClass)
            {
                fBoldGeneratorTemplateManagerClass = value;
                G_BoldGeneratorTemplateManager = null;
            }
        }
    }

    /// <summary>The template manager, made from <see cref="BoldGeneratorTemplateManagerClass"/> on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldGeneratorTemplateManager BoldGeneratorTemplatesManager()
    {
        if (G_BoldGeneratorTemplateManager is null)
        {
            if (BoldGeneratorTemplateManagerClass is null)
            {
                throw new EBoldInternal("No GeneratorTemplateManagerClass assigned, unable to create template manager");
            }

            G_BoldGeneratorTemplateManager = (TBoldGeneratorTemplateManager)Activator.CreateInstance(BoldGeneratorTemplateManagerClass)!;
        }

        return G_BoldGeneratorTemplateManager;
    }
}
