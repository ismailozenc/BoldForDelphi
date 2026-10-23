// Delphi: Source/MoldModel/CodeGenerator/BoldGeneratorTemplatesDelphi.pas
// Port map: CSharp/port/units/BoldGeneratorTemplatesDelphi.md

using static Bold.BoldGeneratorTemplates;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGeneratorTemplatesDelphi", Symbol = "initialization", Note = "Installed the Delphi templates in Delphi builds; the port uses the C# templates unless InstallTemplates is called.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesDelphi", Symbol = "TBoldDelphiTemplateManager.EnsureSafeIDLType", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesDelphi", Symbol = "TBoldDelphiTemplateManager.InitializeCOMTemplateList", Note = "COM templates go with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesDelphi", Symbol = "TBoldDelphiTemplateManager.MethodToCOMHeader", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGeneratorTemplatesDelphi", Symbol = "TBoldDelphiTemplateManager.MethodToCOMCall", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]

namespace Bold;

/// <summary>The Delphi templates: the main unit, its interface include file, and the method templates.</summary>
[Delphi(PortStatus.Ported, Note = "A plain class; the template texts of the Delphi form file are in BoldGeneratorTemplatesDelphi.Templates.cs.")]
public partial class TBoldGeneratorTemplatesDelphiDM
{
    public readonly TBoldTemplateHolder UnitTemplate = TBoldTemplateManagerSupport.Holder(UnitTemplateLines, UnitTemplateFileName, UnitTemplateDescription);
    public readonly TBoldTemplateHolder InterfaceTemplate = TBoldTemplateManagerSupport.Holder(InterfaceTemplateLines, InterfaceTemplateFileName, InterfaceTemplateDescription);
    public readonly TBoldTemplateHolder MethodTemplate = TBoldTemplateManagerSupport.Holder(MethodTemplateLines, MethodTemplateFileName, MethodTemplateDescription);
    public readonly TBoldTemplateHolder DeriveAndSubscribeTemplate = TBoldTemplateManagerSupport.Holder(DeriveAndSubscribeTemplateLines, DeriveAndSubscribeTemplateFileName, DeriveAndSubscribeTemplateDescription);
    public readonly TBoldTemplateHolder IncFileHeaderTemplate = TBoldTemplateManagerSupport.Holder(IncFileHeaderTemplateLines, IncFileHeaderTemplateFileName, IncFileHeaderTemplateDescription);
    public readonly TBoldTemplateHolder ReverseDeriveTemplate = TBoldTemplateManagerSupport.Holder(ReverseDeriveTemplateLines, ReverseDeriveTemplateFileName, ReverseDeriveTemplateDescription);
    public readonly TBoldTemplateHolder PersistenceInterfaceTemplate = TBoldTemplateManagerSupport.Holder(PersistenceInterfaceTemplateLines, PersistenceInterfaceTemplateFileName, PersistenceInterfaceTemplateDescription);
}

/// <summary>The template manager for Delphi code: Delphi method headers, inherited calls and include files (.inc).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDelphiTemplateManager : TBoldGeneratorTemplateManager
{
    private TBoldGeneratorTemplatesDelphiDM? fDataModule;

    [Delphi(PortStatus.Replaced, Symbol = "TBoldDelphiTemplateManager.Destroy", Note = "Freed the data module; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    private TBoldGeneratorTemplatesDelphiDM GetDataModule() => fDataModule ??= new TBoldGeneratorTemplatesDelphiDM();

    /// <summary>The parameters of the method in Delphi syntax (const, var and out included).</summary>
    [Delphi(PortStatus.Ported)]
    private static string GetDelphiSignature(TMoldMethod MoldMethod)
    {
        string result = "";
        for (int i = 0; i < MoldMethod.Parameters.Count; i++)
        {
            var Parameter = (TMoldParameter)MoldMethod.Parameters[i]!;
            if (result != "")
            {
                result += "; ";
            }

            if (Parameter.IsConst)
            {
                result += "const ";
            }
            else if (Parameter.ParameterKind == TBoldParameterDirectionKind.pdInout)
            {
                result += "var ";
            }
            else if (Parameter.ParameterKind == TBoldParameterDirectionKind.pdOut)
            {
                result += "out ";
            }

            result += SysUtils.Format("%s: %s", Parameter.ParameterName, Parameter.DelphiParameterType);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetDerivedMethodTemplate() => DataModule.DeriveAndSubscribeTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetReverseDeriveMethodTemplate() => DataModule.ReverseDeriveTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetMethodTemplate() => DataModule.MethodTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder GetIncFileHeaderTemplate() => DataModule.IncFileHeaderTemplate;
    [Delphi(PortStatus.Ported)] protected override TBoldTemplateHolder? GetPersistenceInterfaceTemplate() => DataModule.PersistenceInterfaceTemplate;

    /// <summary>The main unit and the interface include file.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializeTemplateList(TBoldTemplateList TemplateList)
    {
        if (TemplateList.Count == 0)
        {
            TemplateList.Add(DataModule.UnitTemplate);
            TemplateList.Add(DataModule.InterfaceTemplate);
        }
    }

    [Delphi(PortStatus.Ported)] protected override string GetDefaultIncFileExtension() => ".inc";

    [Delphi(PortStatus.Ported)] protected TBoldGeneratorTemplatesDelphiDM DataModule => GetDataModule();

    [Delphi(PortStatus.Ported)]
    public override string ReadMethodSignature(string ClassName, string AttributeName, string AttributeType) =>
        SysUtils.Format("function %s.Get%s: %s;", ClassName, AttributeName, AttributeType);

    [Delphi(PortStatus.Ported)]
    public override string WriteMethodSignature(string ClassName, string AttributeName, string AttributeType) =>
        SysUtils.Format("procedure %s.Set%s(NewValue: %s);", ClassName, AttributeName, AttributeType);

    /// <summary>Makes the Delphi templates the generator's.</summary>
    [Delphi(PortStatus.Ported)]
    public static void InstallTemplates() => BoldGeneratorTemplateManagerClass = typeof(TBoldDelphiTemplateManager);

    /// <summary>
    /// The Delphi header of a method: class, procedure, function or destructor, qualified with the class in the
    /// implementation, with the signature when asked, and with its directive (virtual, override ...) in a declaration.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override string MethodToCodeHeader(TMoldClass OwningClass, TMoldMethod Method, int TagValue, bool AddSignature, bool AutoOverride)
    {
        string[] directiveNames = ["", "virtual;", "override;", "dynamic;", "virtual; abstract;"];
        string s = "";
        if (Method.IsClassMethod)
        {
            s += "class ";
        }

        if (SysUtils.AnsiCompareText(Method.Name, "Destroy") == 0)
        {
            s += "destructor ";
        }
        else if (Method.HasReturnValue)
        {
            s += "function ";
        }
        else
        {
            s += "procedure ";
        }

        if (TagValue == ImplementationTag)
        {
            s += OwningClass.ExpandedDelphiName + ".";
        }

        s += Method.ExpandedDelphiName;

        if (AddSignature)
        {
            string Signature = GetDelphiSignature(Method);
            if (Signature != "")
            {
                s += "(" + Signature + ")";
            }

            if (Method.HasReturnValue)
            {
                s += ": " + Method.DelphiReturnType;
            }

            s += ";";
        }

        if (TagValue is >= publicTag and <= PrivateTag)
        {
            TDelphiFunctionType FuncType;
            if (AutoOverride)
            {
                FuncType = TDelphiFunctionType.dfOverride;
            }
            else
            {
                FuncType = Method.FuncType;
                if (Method.OverrideInAllSubclasses && FuncType == TDelphiFunctionType.dfNormal)
                {
                    FuncType = TDelphiFunctionType.dfVirtual;
                }
            }

            s += " " + directiveNames[(int)FuncType];
        }

        return s;
    }

    [Delphi(PortStatus.Ported)]
    public override bool StringContainsMethodHeader(string s)
    {
        string temp = SysUtils.UpperCase(s);
        return SysUtils.Pos("PROCEDURE", temp) != 0 || SysUtils.Pos("FUNCTION", temp) != 0 || SysUtils.Pos("DESTRUCTOR", temp) != 0;
    }

    /// <summary>The header up to its parameters or result type, unless the parameters are searched for too.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GetSearchStringfromMethodHeader(string header, bool SearchParameterList)
    {
        string result = header;
        if (!SearchParameterList && SysUtils.Pos("(", result) != 0)
        {
            result = SysUtils.Copy(result, 1, SysUtils.Pos("(", result) - 1);
        }

        if (!SearchParameterList && SysUtils.Pos(":", result) != 0)
        {
            result = SysUtils.Copy(result, 1, SysUtils.Pos(":", result) - 1);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void AddQualifierParam(ref string Params, string ParamName, string ParamType)
    {
        if (Params != "")
        {
            Params += "; ";
        }

        Params += ParamName + ": " + ParamType;
    }

    [Delphi(PortStatus.Ported)]
    public override void AddQualifierPropertyParam(ref string Params, string ParamName, string ParamType) => AddQualifierParam(ref Params, ParamName, ParamType);

    [Delphi(PortStatus.Ported)]
    public override void AddQualifierFunctionParam(ref string Params, string ParamName, string ParamType) => AddQualifierParam(ref Params, ParamName, ParamType);

    /// <summary>"inherited" for a procedure; "result := inherited Name(args)" for a function.</summary>
    [Delphi(PortStatus.Ported)]
    public override string GenerateInheritedCall(TMoldClass MoldClass, TMoldMethod MoldMethod) =>
        !MoldMethod.HasReturnValue ? "inherited" : SysUtils.Format("result := inherited %s(%s)", MoldMethod.Name, MoldMethod.CallSignature);
}

/// <summary>The Delphi templates bound to members by name (BoldMemberByExpressionName) instead of by index.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDelphiTemplateManagerNameBound : TBoldDelphiTemplateManager
{
    /// <summary>Rewrites the unit template to find members by expression name, then adds the templates.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializeTemplateList(TBoldTemplateList TemplateList)
    {
        if (TemplateList.Count == 0)
        {
            string template = DataModule.UnitTemplate.Template.Text;
            template = template.Replace("BoldMembers[$(MEMBERINDEX)]", "BoldMemberByExpressionName['$(MEMBERNAME)']", StringComparison.Ordinal);
            template = template.Replace("$(MEMBERINDEX)", "-1", StringComparison.Ordinal);
            DataModule.UnitTemplate.Template.Text = template;
        }

        base.InitializeTemplateList(TemplateList);
    }

    /// <summary>Makes the name bound Delphi templates the generator's.</summary>
    [Delphi(PortStatus.Ported)]
    public static new void InstallTemplates() => BoldGeneratorTemplateManagerClass = typeof(TBoldDelphiTemplateManagerNameBound);
}

/// <summary>Helpers of the template managers.</summary>
internal static class TBoldTemplateManagerSupport
{
    /// <summary>A template holder with the lines, file name and description (what a Delphi form file gives a holder).</summary>
    public static TBoldTemplateHolder Holder(string[] lines, string fileName, string description)
    {
        var holder = new TBoldTemplateHolder { FileName = fileName, Description = description };
        // A form file loads the lines in one update, so the holder's change handler runs once.
        holder.Template.AddStrings(lines);
        return holder;
    }
}
