// Delphi: Source/MoldModel/CodeGenerator/BoldGeneratorTemplatesCPP.pas
// Port map: CSharp/port/units/BoldGeneratorTemplatesCPP.md

using static Bold.BoldGeneratorTemplates;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGeneratorTemplatesCPP", Symbol = "initialization", Note = "Installed the C++ templates in C++Builder builds; call InstallCPPTemplates to use them.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesCPP", Symbol = "TBoldCPPTemplateManager.EnsureSafeIDLType", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesCPP", Symbol = "TBoldCPPTemplateManager.InitializeCOMTemplateList", Note = "COM templates go with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesCPP", Symbol = "TBoldCPPTemplateManager.MethodToCOMHeader", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesCPP", Symbol = "TBoldCPPTemplateManager.MethodToCOMCall", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]

namespace Bold;

/// <summary>The C++Builder templates: the .cpp unit, its .hpp header, and the method templates.</summary>
[Delphi(PortStatus.Ported, Note = "A plain class; the template texts of the Delphi form file are in BoldGeneratorTemplatesCPP.Templates.cs.")]
public partial class TBoldGeneratorTemplateCPPDM
{
    public readonly TBoldTemplateHolder UnitTemplate = TBoldTemplateManagerSupport.Holder(UnitTemplateLines, UnitTemplateFileName, UnitTemplateDescription);
    public readonly TBoldTemplateHolder InterfaceTemplate = TBoldTemplateManagerSupport.Holder(InterfaceTemplateLines, InterfaceTemplateFileName, InterfaceTemplateDescription);
    public readonly TBoldTemplateHolder MethodTemplate = TBoldTemplateManagerSupport.Holder(MethodTemplateLines, MethodTemplateFileName, MethodTemplateDescription);
    public readonly TBoldTemplateHolder DeriveAndSubscribeTemplate = TBoldTemplateManagerSupport.Holder(DeriveAndSubscribeTemplateLines, DeriveAndSubscribeTemplateFileName, DeriveAndSubscribeTemplateDescription);
    public readonly TBoldTemplateHolder IncFileHeaderTemplate = TBoldTemplateManagerSupport.Holder(IncFileHeaderTemplateLines, IncFileHeaderTemplateFileName, IncFileHeaderTemplateDescription);
    public readonly TBoldTemplateHolder ReverseDeriveTemplate = TBoldTemplateManagerSupport.Holder(ReverseDeriveTemplateLines, ReverseDeriveTemplateFileName, ReverseDeriveTemplateDescription);
}

/// <summary>The template manager for C++Builder code: __fastcall headers, Class::Method implementations and _impl.cpp include files.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCPPTemplateManager : TBoldGeneratorTemplateManager
{
    private TBoldGeneratorTemplateCPPDM? fDataModule;

    [Delphi(PortStatus.Replaced, Symbol = "TBoldCPPTemplateManager.Destroy", Note = "Freed the data module; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    private TBoldGeneratorTemplateCPPDM GetDataModule() => fDataModule ??= new TBoldGeneratorTemplateCPPDM();

    /// <summary>The parameters in C++ syntax; a parameter of a model class is a pointer.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested AdaptCPPParameterType is a private helper.")]
    private static string GetCPPSignature(TMoldMethod MoldMethod)
    {
        string result = "";
        for (int i = 0; i < MoldMethod.Parameters.Count; i++)
        {
            var Parameter = (TMoldParameter)MoldMethod.Parameters[i]!;
            if (result != "")
            {
                result += ", ";
            }

            result += SysUtils.Format("%s %s", AdaptCPPParameterType(MoldMethod, Parameter.DelphiParameterType), Parameter.ParameterName);
        }

        return result;
    }

    private static string AdaptCPPParameterType(TMoldMethod MoldMethod, string ParamType) =>
        MoldMethod.MoldClass!.Model.Classes.ItemsByDelphiName(ParamType) is not null ? ParamType + "*" : ParamType;

    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetDerivedMethodTemplate() => DataModule.DeriveAndSubscribeTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetReverseDeriveMethodTemplate() => DataModule.ReverseDeriveTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetMethodTemplate() => DataModule.MethodTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetIncFileHeaderTemplate() => DataModule.IncFileHeaderTemplate;

    /// <summary>The .cpp unit and the .hpp header.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializeTemplateList(TBoldTemplateList TemplateList)
    {
        if (TemplateList.Count == 0)
        {
            TemplateList.Add(DataModule.UnitTemplate);
            TemplateList.Add(DataModule.InterfaceTemplate);
        }
    }

    [Delphi(PortStatus.Ported)] protected override string GetDefaultIncFileExtension() => "_impl.cpp";

    [Delphi(PortStatus.Ported)] protected TBoldGeneratorTemplateCPPDM DataModule => GetDataModule();

    [Delphi(PortStatus.Ported)]
    public override string ReadMethodSignature(string ClassName, string AttributeName, string AttributeType) =>
        SysUtils.Format("%s __fastcall %s::Get%s()", AttributeType, ClassName, AttributeName);

    [Delphi(PortStatus.Ported)]
    public override string WriteMethodSignature(string ClassName, string AttributeName, string AttributeType) =>
        SysUtils.Format("void __fastcall %s::Set%s(%s NewValue)", ClassName, AttributeName, AttributeType);

    /// <summary>
    /// The C++ header of a method: static and virtual in a declaration, the return type, __fastcall, Class:: in the
    /// implementation, the parameters, and "= 0" for an abstract method.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override string MethodToCodeHeader(TMoldClass OwningClass, TMoldMethod Method, int TagValue, bool AddSignature, bool AutoOverride)
    {
        string s = "";
        bool declaration = TagValue is >= publicTag and <= PrivateTag;
        if (declaration && Method.IsClassMethod)
        {
            s += "static ";
        }

        if (declaration && (AutoOverride || Method.FuncType == TDelphiFunctionType.dfVirtual || Method.OverrideInAllSubclasses))
        {
            s += "virtual ";
        }

        s += Method.HasReturnValue ? Method.DelphiReturnType + " " : "void ";
        s += "__fastcall ";
        if (TagValue == ImplementationTag)
        {
            s += OwningClass.ExpandedDelphiName + "::";
        }

        s += Method.ExpandedDelphiName;
        if (AddSignature)
        {
            string Signature = GetCPPSignature(Method);
            s += Signature != "" ? "(" + Signature + ")" : "(void)";
        }

        if (Method.FuncType == TDelphiFunctionType.dfAbstractVirtual)
        {
            s += " = 0";
        }

        return s;
    }

    [Delphi(PortStatus.Ported)]
    public override bool StringContainsMethodHeader(string s) => SysUtils.Pos("::", s) != 0;

    [Delphi(PortStatus.Ported)]
    public override void AddQualifierPropertyParam(ref string Params, string ParamName, string ParamType)
    {
        if (Params != "")
        {
            Params += "][";
        }

        Params += ParamType + " " + ParamName;
    }

    [Delphi(PortStatus.Ported)]
    public override void AddQualifierFunctionParam(ref string Params, string ParamName, string ParamType)
    {
        if (Params != "")
        {
            Params += ", ";
        }

        Params += ParamType + " " + ParamName;
    }

    /// <summary>The header up to its parameters, unless the parameters are searched for too.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetSearchStringfromMethodHeader(string header, bool SearchParameterList)
    {
        string result = header;
        if (!SearchParameterList && SysUtils.Pos("(", result) != 0)
        {
            result = SysUtils.Copy(result, 1, SysUtils.Pos("(", result) - 1);
        }

        return result;
    }

    /// <summary>"Super::Name(args)", with return for a function.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GenerateInheritedCall(TMoldClass MoldClass, TMoldMethod MoldMethod)
    {
        string result = MoldMethod.HasReturnValue ? "return " : "";
        result += MoldClass.SuperClass is not null ? MoldClass.SuperClass.ExpandedDelphiName : "TBoldObject";
        return result + SysUtils.Format("::%s(%s)", MoldMethod.Name, MoldMethod.CallSignature);
    }

    /// <summary>Makes the C++ templates the generator's.</summary>
    [Delphi(PortStatus.Ported)]
    public static void InstallCPPTemplates() => BoldGeneratorTemplateManagerClass = typeof(TBoldCPPTemplateManager);
}
