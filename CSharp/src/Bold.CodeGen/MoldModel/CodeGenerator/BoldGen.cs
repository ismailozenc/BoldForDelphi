// Delphi: Source/MoldModel/CodeGenerator/BoldGen.pas
// Port map: CSharp/port/units/BoldGen.md

using static Bold.BoldGeneratorTemplates;
using static Bold.BoldTemplateExpander;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGen", Symbol = "TBoldGeneratorClass", Note = "A System.Type of a TBoldGenerator subclass.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldGen", Symbol = "TBoldCodeGenInitializerClass", Note = "A System.Type of a TBoldCodeGenInitializer subclass.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.MethodToCOMHeader", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.MethodToIDLHeader", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.MethodToCOMCall", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.EnsureSafeIDLType", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.GenerateComInterfaces", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.GenerateMIDLCode", Note = "COM code generation goes with the COM object space, which is not ported (drop list).")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldGen", Symbol = "TBoldGenerator.GenerateIDLVariables", Note = "Only COM code generation set the IDL variables; it is not ported (drop list).")]

namespace Bold;

/// <summary>Whether to generate a function's header or body.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldFunctionContent { fcHeader, fcBody }

/// <summary>Whether to ensure (add only if missing) or always add.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldEnsureFlag { efEnsure, efAdd }

/// <summary>Picks classes of a model.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TMoldClassFilterFunction(TMoldClass MoldClass);

/// <summary>
/// Generates the code of a model's classes: for each component (unit) of the model it fills the variables of the
/// template manager's file templates and writes the expanded templates; it also ensures that the method
/// implementations the model needs exist in the include files, adding the missing ones.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenerator : IDisposable
{
    private TMoldModel? fMoldModel;
    private TBoldFileHandler? fBoldFilehandler;
    private bool fUseTypedLists;
    private readonly TStringList fTypesWithoutNative;
    private readonly TStringList fUnmappedTypes;
    private readonly TStringList FMethodIndex;
    private readonly TBoldTypeNameDictionary fTypeNameDictionary;
    private TMoldClass? fCurrentClass;
    private TMoldComponent? fCurrentComponent;
    private bool FGenerateBold1CompatibleCode;
    private string fBaseFilePath = "";

    private static TBoldLogHandler BoldLog => BoldLogHandler.BoldLog();

    private static TBoldGeneratorTemplateManager Manager => BoldGeneratorTemplatesManager();

    [Delphi(PortStatus.Ported)]
    public TBoldGenerator(TBoldTypeNameDictionary TypeNameDictionary)
    {
        FMethodIndex = new TStringList();
        fTypeNameDictionary = TypeNameDictionary;
        fUnmappedTypes = new TStringList();
        fTypesWithoutNative = new TStringList();
        FGenerateBold1CompatibleCode = true;
    }

    /// <summary>Writes and closes the open files.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldGenerator.Destroy")]
    public virtual void Dispose()
    {
        BoldFileHandler.BoldCloseAllFilehandlers();
        GC.SuppressFinalize(this);
    }

    private static string VisibilityToString(TVisibilityKind Visibility) => Visibility switch
    {
        TVisibilityKind.vkPrivate => "private",
        TVisibilityKind.vkProtected => "protected",
        _ => "public",
    };

    /// <summary>
    /// Runs the code generator initializer, then for each component expands every template of the list into its file
    /// (the file name comes from the template, with COMPONENTNAME set).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual void ExpandTemplateList(TBoldTemplateList TemplateList)
    {
        MoldModel.EnsureTopSorted();
        RunCodeGenInitializer();

        BoldLog.ProgressMax = MoldModel.Classes.Count * 3;

        BoldLog.LogFmt(BoldMoldConsts.sLogGeneratingInPath, [BaseFilePath]);
        for (int ComponentIx = 0; ComponentIx < MoldModel.Components.Count; ComponentIx++)
        {
            for (int TemplateIx = 0; TemplateIx < TemplateList.Count; TemplateIx++)
            {
                if (BoldLog.ProcessInterruption())
                {
                    return;
                }

                TBoldTemplateHolder template = TemplateList[TemplateIx];
                template.Variables.SetVariable("COMPONENTNAME", MoldModel.Components[ComponentIx].Name);
                BoldLog.LogFmt(BoldMoldConsts.sLogGeneratingFile, [template.ExpandedFileName]);

                SetCurrentFileHandler(BaseFilePath, template.ExpandedFileName, ModuleTypeForFile(template.ExpandedFileName), true, false);
                BoldFilehandler!.Clear();
                BoldLog.LogHeader = BoldMoldConsts.sLogInitializingVars;
                InitializeTemplateForComponent(template, MoldModel, MoldModel.Components[ComponentIx], true);
                if (BoldLog.ProcessInterruption())
                {
                    return;
                }

                BoldLog.LogHeader = BoldMoldConsts.sLogExpandingTemplate;
                BoldFilehandler.AddStrings(template.ExpandedTemplate);
            }
        }

        if (MoldModel.MainComponent.Name == BoldDefaultTaggedValues.BoldDefaultTaggedValueList().DefaultForClassAndTag("Model", BoldDefaultTaggedValues.TAG_UNITNAME))
        {
            BoldLog.Separator();
            BoldLog.Log(BoldMoldConsts.sLogConsiderNameChange1, TBoldLogType.ltWarning);
            BoldLog.Log(BoldMoldConsts.sLogConsiderNameChange2, TBoldLogType.ltWarning);
            BoldLog.Separator();
        }
    }

    /// <summary>Runs the code generator initializer (it moves link classes between components) and checks the file names.</summary>
    protected void RunCodeGenInitializer()
    {
        if (BoldGen.BoldCodeGenInitializerClass is not null)
        {
            var initializer = (TBoldCodeGenInitializer)Activator.CreateInstance(BoldGen.BoldCodeGenInitializerClass, MoldModel)!;
            initializer.Execute();
            initializer.ValidateFileNames();
        }
    }

    /// <summary>
    /// Whether the variables of a component's template take the class; C# addition, so a generator can expand a
    /// template for one class at a time.
    /// </summary>
    protected virtual bool ClassInTemplate(TMoldClass MoldClass, TMoldComponent Component) => MoldClass.Component == Component;

    /// <summary>
    /// Fills the template's variables for the component: the unit, its dependencies, the model, and (when asked) each
    /// class with its members, methods and manually derived members. Variables of a class have the suffix .n (the
    /// class's number), and of its members .n.m.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested procedures are private helpers (AddVariable, InitializeAttribute, InitializeNativeAttribute, InitializeQualifier, InitializeRole, ParameterInfo, InitializeMethod, InitializeClass, BuildListOfAllUsedUnits); the IDL variables and InitializeDispIdMethod went with COM code generation.")]
    protected virtual void InitializeTemplateForComponent(TBoldTemplateHolder Template, TMoldModel Model, TMoldComponent Component, bool InitializeClasses)
    {
        Template.Variables.Clear();
        AddVariable(Template, "USETYPEDLISTS", BoldBooleanToTemplateVar(UseTypedLists));
        AddVariable(Template, "UNITNAME", Component.Name);

        AddVariable(Template, "UNITGUID", MoldModel.GUID != "" ? MoldModel.GUID : BoldGUIDUtils.BoldCreateGUIDAsString(true));
        AddVariable(Template, "CRC", MoldModel.CRC());
        AddVariable(Template, "COPYRIGHTNOTICE", Model.BoldTVByName("CopyrightNotice"));

        var Dependencies = new TStringList();
        Component.GetInterfaceDependencies(Dependencies);
        for (int i = 0; i < Dependencies.Count; i++)
        {
            AddVariable(Template, "INTERFACEADAPTERDEPENDENCY." + SysUtils.IntToStr(i), Dependencies[i] + "Adapters");
            AddVariable(Template, "INTERFACEDEPENDENCY." + SysUtils.IntToStr(i), Dependencies[i]);
        }

        AddVariable(Template, "INTERFACEDEPENDENCIESCOUNT", SysUtils.IntToStr(Dependencies.Count));

        Component.GetImplementationDependencies(Dependencies);
        for (int i = 0; i < Dependencies.Count; i++)
        {
            AddVariable(Template, "IMPLEMENTATIONADAPTERDEPENDENCY." + SysUtils.IntToStr(i), Dependencies[i] + "Adapters");
            AddVariable(Template, "IMPLEMENTATIONDEPENDENCY." + SysUtils.IntToStr(i), Dependencies[i]);
        }

        AddVariable(Template, "IMPLEMENTATIONDEPENDENCIESCOUNT", SysUtils.IntToStr(Dependencies.Count));

        BuildListOfAllUsedUnits(Model, Dependencies);
        for (int i = 0; i < Dependencies.Count; i++)
        {
            AddVariable(Template, "ATTRIBUTECLASSDEPENDENCY." + SysUtils.IntToStr(i), Dependencies[i]);
        }

        AddVariable(Template, "ATTRIBUTECLASSDEPENDENCIESCOUNT", SysUtils.IntToStr(Dependencies.Count));

        string temp = "";
        for (int i = 0; i < Model.Components.Count; i++)
        {
            if (temp != "")
            {
                temp += ", ";
            }

            temp += Model.Components[i].Name;
        }

        AddVariable(Template, "ALLCOMPONENTS", temp);

        int ClassCount = 0;
        AddVariable(Template, "MODELNAME", MoldModel.ExpandedExpressionName);

        if (MoldModel.InterfaceUses != "")
        {
            AddVarList(Template, "INTERFACEUSES", MoldModel.InterfaceUses + ",");
        }

        if (MoldModel.ImplementationUses != "")
        {
            AddVarList(Template, "IMPLEMENTATIONUSES", MoldModel.ImplementationUses + ",");
        }

        var IncludeFiles = new TStringList();

        if (InitializeClasses)
        {
            for (int i = 0; i < MoldModel.Classes.Count; i++)
            {
                if (ClassInTemplate(MoldModel.Classes[i], Component))
                {
                    if (MoldModel.Classes[i].HasCodeStubs)
                    {
                        string FileName = MoldModel.Classes[i].EffectiveIncFileName(Manager.DefaultIncFileExtension);
                        if (IncludeFiles.IndexOf(FileName) == -1)
                        {
                            IncludeFiles.Add(FileName);
                        }
                    }

                    if ((ClassCount + 1) % BoldGen.DEBUGGERWORKAROUNDINTERVAL == 0)
                    {
                        AddVariable(Template, "DEBUGGERWORKAROUND." + SysUtils.IntToStr(ClassCount), "1");
                    }

                    InitializeClass(Template, MoldModel.Classes[i], "." + SysUtils.IntToStr(ClassCount));
                    ClassCount++;
                    BoldLog.ProgressStep();
                    BoldLog.Sync();
                    if (BoldLog.ProcessInterruption())
                    {
                        return;
                    }
                }
            }
        }

        AddVariable(Template, "CLASSCOUNT", SysUtils.IntToStr(ClassCount));
        for (int i = 0; i < IncludeFiles.Count; i++)
        {
            AddVariable(Template, "INCLUDEFILE." + SysUtils.IntToStr(i), IncludeFiles[i]);
        }

        AddVariable(Template, "INCLUDEFILECOUNT", SysUtils.IntToStr(IncludeFiles.Count));

        if (GenerateBold1CompatibleCode)
        {
            AddVariable(Template, "DELPHIATTRIBUTEPOSTFIX", "_");
        }
        else
        {
            AddVariable(Template, "BOLDATTRIBUTEPOSTFIX", "Attribute");
        }
    }

    private static void AddVariable(TBoldTemplateHolder Template, string VariableName, string VariableValue) =>
        Template.Variables.ForceAdd(VariableName, VariableValue, DelphiSet<int>.Empty);

    /// <summary>The variables of an attribute: its attribute class, native type and accessor, value interface, kind and visibility.</summary>
    private void InitializeAttribute(TBoldTemplateHolder Template, TMoldAttribute MoldAttribute, string PostFix)
    {
        void internalInitialize()
        {
            AddVariable(Template, "MEMBERHASNATIVE" + PostFix, "1");
            AddVariable(Template, "MEMBERISSETABLE" + PostFix, !MoldAttribute.Derived || MoldAttribute.ReverseDerived ? "1" : "0");
        }

        string TypeName;
        string ValueInterfacename;
        TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(MoldAttribute.BoldType);
        if (Mapping is null)
        {
            if (UnMappedTypes.IndexOf(MoldAttribute.BoldType) == -1)
            {
                BoldLog.LogFmt(BoldMoldConsts.sLogNoDelphiMappingForType, [MoldAttribute.BoldType], TBoldLogType.ltWarning);
                UnMappedTypes.Add(MoldAttribute.BoldType);
            }

            TypeName = "TBoldAttribute";
            ValueInterfacename = "IBoldNullableValue";
        }
        else
        {
            TypeName = Mapping.ExpandedDelphiName;
            ValueInterfacename = Mapping.ValueInterface;
            if (Mapping.ExpandedAccessor != "" && Mapping.ExpandedNativeType != "")
            {
                internalInitialize();
                AddVariable(Template, "MEMBERNATIVEACCESSOR" + PostFix, Mapping.ExpandedAccessor);
                AddVariable(Template, "MEMBERNATIVETYPE" + PostFix, Mapping.ExpandedNativeType);
            }
            else if (TypesWithoutNative.IndexOf(MoldAttribute.BoldType) == -1)
            {
                BoldLog.LogFmt(BoldMoldConsts.sLogNoNativeMappingForType, [MoldAttribute.BoldType, MoldAttribute.Name, MoldAttribute.MoldClass!.Name], TBoldLogType.ltWarning);
                TypesWithoutNative.Add(MoldAttribute.BoldType);
            }

            if (MoldAttribute.EffectivePersistent)
            {
                if (Mapping.ValueInterfaceAccessor != "" && Mapping.ValueInterfaceNativeType != "")
                {
                    AddVariable(Template, "MEMBERHASVALUEINTERFACENATIVE" + PostFix, "1");
                    AddVariable(Template, "MEMBERVALUEINTERFACENATIVEACCESSOR" + PostFix, Mapping.ValueInterfaceAccessor);
                    AddVariable(Template, "MEMBERVALUEINTERFACENATIVETYPE" + PostFix, Mapping.ValueInterfaceNativeType);
                }
                else if (TypesWithoutNative.IndexOf(MoldAttribute.BoldType) == -1)
                {
                    BoldLog.LogFmt(BoldMoldConsts.sNoValueTypeMappingForType, [MoldAttribute.BoldType, MoldAttribute.Name, MoldAttribute.MoldClass!.Name], TBoldLogType.ltWarning);
                    TypesWithoutNative.Add(MoldAttribute.BoldType);
                }
            }
        }

        AddVariable(Template, "MEMBERTYPE" + PostFix, TypeName);
        AddVariable(Template, "MEMBERVALUEINTERFACE" + PostFix, ValueInterfacename);
        AddVariable(Template, "MEMBERKIND" + PostFix, "Attribute");
        AddVariable(Template, "MEMBERVISIBILITY" + PostFix, VisibilityToString(MoldAttribute.Visibility));
    }

    /// <summary>The variables of a Delphi attribute (a native field or property of the class, not a Bold member).</summary>
    private void InitializeNativeAttribute(TBoldTemplateHolder Template, TMoldAttribute MoldAttribute, string PostFix)
    {
        static string PropertyAccessKindToStr(TDelphiPropertyAccessKind Kind) => Kind switch
        {
            TDelphiPropertyAccessKind.pkNone => "None",
            TDelphiPropertyAccessKind.pkField => "Field",
            TDelphiPropertyAccessKind.pkPrivateMethod => "PrivateMethod",
            _ => "ProtectedVirtualMethod",
        };

        string TypeName = GetNativeDelphiTypeForModelNameNoDefaults(MoldAttribute);
        AddVariable(Template, "DelphiAttributeType" + PostFix, TypeName);
        AddVariable(Template, "DelphiAttributeKind" + PostFix, "Attribute");
        AddVariable(Template, "DelphiAttributeHasField" + PostFix, BoldBooleanToTemplateVar(MoldAttribute.HasDelphiField));
        AddVariable(Template, "DelphiAttributeVisibility" + PostFix, VisibilityToString(MoldAttribute.Visibility));
        AddVariable(Template, "DelphiAttributePropertyRead" + PostFix, PropertyAccessKindToStr(MoldAttribute.DelphiPropertyRead));
        AddVariable(Template, "DelphiAttributePropertyWrite" + PostFix, PropertyAccessKindToStr(MoldAttribute.DelphiPropertyWrite));
    }

    /// <summary>The qualifier variables of a role: each qualifier's name, attribute class and accessor, and the parameter lists.</summary>
    private void InitializeQualifier(TBoldTemplateHolder Template, TMoldRole MoldRole, string PostFix)
    {
        string QualifierPropertyParams = "";
        string QualifierFunctionParams = "";
        bool AllOK = true;
        for (int i = 0; i < MoldRole.Qualifiers.Count; i++)
        {
            TMoldMember? Member = MoldRole.OtherEnd!.MoldClass!.AllBoldMembers.ItemsByName(MoldRole.Qualifiers[i].Name);
            if (Member is TMoldAttribute AttrOfOtherEnd)
            {
                TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(AttrOfOtherEnd.BoldType);
                if (Mapping is not null)
                {
                    AddVariable(Template, "QUALIFIERNAME" + PostFix + "." + SysUtils.IntToStr(i), AttrOfOtherEnd.ExpandedDelphiName);
                    AddVariable(Template, "QUALIFIERBOLDTYPE" + PostFix + "." + SysUtils.IntToStr(i), Mapping.ExpandedDelphiName);
                    AddVariable(Template, "QUALIFIERACCESSOR" + PostFix + "." + SysUtils.IntToStr(i), Mapping.ExpandedAccessor);
                    Manager.AddQualifierPropertyParam(ref QualifierPropertyParams, AttrOfOtherEnd.ExpandedDelphiName, Mapping.ExpandedNativeType);
                    Manager.AddQualifierFunctionParam(ref QualifierFunctionParams, AttrOfOtherEnd.ExpandedDelphiName, Mapping.ExpandedNativeType);
                }
                else
                {
                    AllOK = false;
                }
            }
            else
            {
                AllOK = false;
            }
        }

        AddVariable(Template, "QUALIFIERPROPERTYPARAMS" + PostFix, QualifierPropertyParams);
        AddVariable(Template, "QUALIFIERFUNCTIONPARAMS" + PostFix, QualifierFunctionParams);
        AddVariable(Template, "ROLEQUALIFIED" + PostFix, BoldBooleanToTemplateVar(AllOK && MoldRole.Qualifiers.Count > 0));
        AddVariable(Template, "QUALIFIERCOUNT" + PostFix, SysUtils.IntToStr(MoldRole.Qualifiers.Count));
    }

    /// <summary>The variables of a role: the class at the other end (as seen from this class), kind, navigability and qualifiers.</summary>
    private void InitializeRole(TBoldTemplateHolder Template, TMoldRole MoldRole, string PostFix)
    {
        TMoldClass? OtherEnd = MoldRole.MoldClass!.LowestVisibleAncestor(MoldRole.OtherEnd!.MoldClass!);
        if ((!MoldRole.Multi || UseTypedLists) && OtherEnd is not null)
        {
            AddVariable(Template, "MEMBERTYPE" + PostFix, OtherEnd.ExpandedDelphiName);
            AddVariable(Template, "MEMBERPERSISTENCEINTERFACE" + PostFix, MakePersistenceInterfaceName(OtherEnd));
        }
        else
        {
            AddVariable(Template, "MEMBERTYPE" + PostFix, "TBoldObject");
        }

        AddVariable(Template, "DISPID" + PostFix, SysUtils.IntToHex(MoldRole.DispId, 8));
        AddVariable(Template, "MEMBERKIND" + PostFix, MoldRole.Multi ? "MultiRole" : "SingleRole");
        AddVariable(Template, "ISTRUEROLE" + PostFix, BoldBooleanToTemplateVar(MoldRole.RoleType == TBoldRoleType.rtRole));
        AddVariable(Template, "MEMBERVISIBILITY" + PostFix, VisibilityToString(MoldRole.Visibility));
        AddVariable(Template, "ROLENAVIGABLE" + PostFix, BoldBooleanToTemplateVar(MoldRole.Navigable));

        InitializeQualifier(Template, MoldRole, PostFix);
    }

    /// <summary>
    /// What a method parameter (or result) is for OCL: Kind 0 gives "object" or "attribute", 1 the Bold type and 2 the
    /// accessor. A type other than a class, String, Integer or Double makes the method not OCL compatible.
    /// </summary>
    private static string ParameterInfo(TBoldTemplateHolder Template, TMoldMethod MoldMethod, string ParameterType, string ParameterDelphiType, int Kind)
    {
        TMoldClass? MoldClass = MoldMethod.MoldClass!.Model.Classes.ItemsByExpressionName(ParameterType) ??
                                MoldMethod.MoldClass.Model.Classes.ItemsByDelphiName(ParameterDelphiType);
        if (MoldClass is not null)
        {
            return Kind switch { 0 => "object", 1 => ParameterDelphiType, _ => "" };
        }

        if (SysUtils.SameText(ParameterType, "STRING"))
        {
            return Kind switch { 0 => "attribute", 1 => "TBAString", _ => ".AsString" };
        }

        if (SysUtils.SameText(ParameterType, "INTEGER"))
        {
            return Kind switch { 0 => "attribute", 1 => "TBAInteger", _ => ".AsInteger" };
        }

        if (SysUtils.SameText(ParameterType, "DOUBLE"))
        {
            return Kind switch { 0 => "attribute", 1 => "TBAFloat", _ => ".AsFloat" };
        }

        Template.Variables.SetVariable("CASEMETHODISOCLCOMPATIBLE", "0");
        return "";
    }

    /// <summary>The variables of a method: its header, name, visibility, kind and OCL information of the result and parameters.</summary>
    private void InitializeMethod(TBoldTemplateHolder Template, TMoldMethod MoldMethod, string postfix, bool AutoOverride)
    {
        AddVariable(Template, "INTERFACEMETHODHEADER" + postfix, MethodToDelphiHeader(MoldMethod.MoldClass!, MoldMethod, publicTag, true, AutoOverride));
        AddVariable(Template, "METHODNAME" + postfix, MoldMethod.ExpandedDelphiName);
        AddVariable(Template, "METHODVISIBILITY" + postfix, VisibilityToString(MoldMethod.Visibility));
        string MethodType = MoldMethod.IsClassMethod ? "class " : "";
        MethodType += MoldMethod.HasReturnValue ? "function" : "procedure";
        AddVariable(Template, "METHODKIND" + postfix, MethodType);
        AddVariable(Template, "METHODISOCLCOMPATIBLE" + postfix, "1");

        if (MoldMethod.HasReturnValue)
        {
            AddVariable(Template, "METHODRESULTBOLDTYPE" + postfix, ParameterInfo(Template, MoldMethod, MoldMethod.ReturnType, MoldMethod.DelphiReturnType, 1));
            AddVariable(Template, "METHODRESULTACCESSOR" + postfix, ParameterInfo(Template, MoldMethod, MoldMethod.ReturnType, MoldMethod.DelphiReturnType, 2));
        }
        else
        {
            AddVariable(Template, "METHODRESULTBOLDTYPE" + postfix, "TBAInteger");
            AddVariable(Template, "METHODRESULTACCESSOR" + postfix, ".AsInteger");
        }

        for (int i = 0; i < MoldMethod.Parameters.Count; i++)
        {
            var parameter = (TMoldParameter)MoldMethod.Parameters[i]!;
            AddVariable(Template, "METHODPARAMETERKIND" + postfix + "." + SysUtils.IntToStr(i), ParameterInfo(Template, MoldMethod, parameter.ParameterType, parameter.DelphiParameterType, 0));
            AddVariable(Template, "METHODPARAMETERBOLDTYPE" + postfix + "." + SysUtils.IntToStr(i), ParameterInfo(Template, MoldMethod, parameter.ParameterType, parameter.DelphiParameterType, 1));
            AddVariable(Template, "METHODPARAMETERACCESSOR" + postfix + "." + SysUtils.IntToStr(i), ParameterInfo(Template, MoldMethod, parameter.ParameterType, parameter.DelphiParameterType, 2));
        }

        AddVariable(Template, "METHODPARAMETERCOUNT" + postfix, SysUtils.IntToStr(MoldMethod.Parameters.Count));
    }

    /// <summary>The variables of a class: names, superclass, its own members, native attributes, methods and manually derived members.</summary>
    private void InitializeClass(TBoldTemplateHolder Template, TMoldClass MoldClass, string PostFix)
    {
        AddVariable(Template, "CLASSNAME" + PostFix, MoldClass.ExpandedDelphiName);
        AddVariable(Template, "CLASSGUID" + PostFix, MoldClass.GUID != "" ? MoldClass.GUID : BoldGUIDUtils.BoldCreateGUIDAsString(true));
        AddVariable(Template, "LISTGUID" + PostFix, MoldClass.ListGUID != "" ? MoldClass.ListGUID : BoldGUIDUtils.BoldCreateGUIDAsString(true));
        AddVariable(Template, "INTERFACENAME" + PostFix, MoldClass.ExpandedInterfaceName);
        AddVariable(Template, "PERSISTENCEINTERFACENAME" + PostFix, MakePersistenceInterfaceName(MoldClass));
        AddVariable(Template, "PERSISTENCEINTERFACEGUID" + PostFix, BoldGUIDUtils.BoldCreateGUIDWithBracketsAsString());
        AddVariable(Template, "ISVERSIONED" + PostFix, BoldBooleanToTemplateVar(MoldClass.Versioned));
        AddVariable(Template, "ISLINKCLASS" + PostFix, BoldBooleanToTemplateVar(MoldClass.Association is not null));
        AddVariable(Template, "CLASSEXPRESSIONNAME" + PostFix, MoldClass.ExpandedExpressionName);
        if (MoldClass.SuperClass is not null)
        {
            AddVariable(Template, "SUPERCLASSNAME" + PostFix, MoldClass.SuperClass.ExpandedDelphiName);
            AddVariable(Template, "SUPERPERSISTENCEINTERFACENAME" + PostFix, MakePersistenceInterfaceName(MoldClass.SuperClass));
        }
        else
        {
            AddVariable(Template, "SUPERCLASSNAME" + PostFix, "TBoldObject");
            AddVariable(Template, "SUPERCLASSNAMESPACEPREFIX" + PostFix, "Boldsystem::");
            AddVariable(Template, "CONSTRUCTORPARAMETER" + PostFix, ", true");
            AddVariable(Template, "SUPERPERSISTENCEINTERFACENAME" + PostFix, "IPersistentBoldObject");
        }

        if (MoldClass.IntroducesManuallyDerivedMembers)
        {
            AddVariable(Template, "CLASSINTRODUCESMANUALLYDERIVEDMEMBERS" + PostFix, "true");
        }

        int MemberCounter = 0;
        for (int i = MoldClass.FirstOwnBoldMemberIndex; i < MoldClass.AllBoldMembers.Count; i++)
        {
            TMoldMember Member = MoldClass.AllBoldMembers[i];
            string MemberPostFix = PostFix + "." + SysUtils.IntToStr(i - MoldClass.FirstOwnBoldMemberIndex);
            AddVariable(Template, "MEMBERNAME" + MemberPostFix, Member.ExpandedDelphiName);
            AddVariable(Template, "MEMBERINDEX" + MemberPostFix, SysUtils.IntToStr(i));
            AddVariable(Template, "MEMBERPERSISTENT" + MemberPostFix, BoldBooleanToTemplateVar(Member.EffectivePersistent));
            if (Member is TMoldAttribute attribute)
            {
                InitializeAttribute(Template, attribute, MemberPostFix);
            }
            else if (Member is TMoldRole role)
            {
                InitializeRole(Template, role, MemberPostFix);
            }

            MemberCounter++;
        }

        AddVariable(Template, "MEMBERCOUNT" + PostFix, SysUtils.IntToStr(MemberCounter));

        MemberCounter = 0;
        for (int i = MoldClass.FirstOwnNativeAttributeIndex; i < MoldClass.AllNativeAttributes.Count; i++)
        {
            TMoldAttribute Attribute = MoldClass.AllNativeAttributes[i];
            if (Attribute.DelphiPropertyRead != TDelphiPropertyAccessKind.pkNone || Attribute.DelphiPropertyWrite != TDelphiPropertyAccessKind.pkNone)
            {
                string MemberPostFix = PostFix + "." + SysUtils.IntToStr(i - MoldClass.FirstOwnNativeAttributeIndex);
                AddVariable(Template, "DelphiAttributeName" + MemberPostFix, Attribute.ExpandedDelphiName);
                InitializeNativeAttribute(Template, Attribute, MemberPostFix);
                MemberCounter++;
            }
        }

        AddVariable(Template, "DelphiAttributeCount" + PostFix, SysUtils.IntToStr(MemberCounter));

        for (int i = 0; i < MoldClass.Methods.Count; i++)
        {
            InitializeMethod(Template, MoldClass.Methods[i], PostFix + "." + SysUtils.IntToStr(i), false);
        }

        for (int i = 0; i < MoldClass.AllAutoOverrideMethods.Count; i++)
        {
            InitializeMethod(Template, MoldClass.AllAutoOverrideMethods[i], PostFix + "." + SysUtils.IntToStr(MoldClass.Methods.Count + i), true);
        }

        AddVariable(Template, "METHODCOUNT" + PostFix, SysUtils.IntToStr(MoldClass.Methods.Count + MoldClass.AllAutoOverrideMethods.Count));

        int Derivedcounter = 0;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            TMoldMember member = MoldClass.AllBoldMembers[i];
            if (member.ManuallyDerived && (member.MoldClass == MoldClass || BoldDefaultTaggedValues.TVIsTrue(member.BoldTVByName(BoldDefaultTaggedValues.TAG_VIRTUALDERIVE))))
            {
                string DerivedPostFix = PostFix + "." + SysUtils.IntToStr(Derivedcounter);
                AddVariable(Template, "DERIVEDMEMBERINTRODUCEDHERE" + DerivedPostFix, BoldBooleanToTemplateVar(member.MoldClass == MoldClass));
                AddVariable(Template, "DERIVEDMEMBERREVERSEDERIVED" + DerivedPostFix, BoldBooleanToTemplateVar(member.ReverseDerived));
                AddVariable(Template, "DERIVEDMEMBERNAME" + DerivedPostFix, member.ExpandedDelphiName);
                Derivedcounter++;
            }
        }

        AddVariable(Template, "DERIVEDMEMBERCOUNT" + PostFix, SysUtils.IntToStr(Derivedcounter));
    }

    /// <summary>The units of the attribute classes the model uses, sorted.</summary>
    private void BuildListOfAllUsedUnits(TMoldModel Model, TStringList StringList)
    {
        StringList.Clear();
        StringList.Sorted = true;
        StringList.Duplicates = TDuplicates.dupIgnore;
        for (int c = 0; c < Model.Classes.Count; c++)
        {
            for (int a = 0; a < Model.Classes[c].Attributes.Count; a++)
            {
                TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(Model.Classes[c].Attributes[a].BoldType);
                if (Mapping is not null)
                {
                    StringList.Add(Mapping.UnitNameText == "" ? "BoldAttributes" : Mapping.UnitNameText);
                }
            }
        }
    }

    /// <summary>Makes the handler of the file current (an include file gets the include file header when empty).</summary>
    [Delphi(PortStatus.Ported, Note = "Protected, so a generator can write files of its own; private in Delphi.")]
    protected void SetCurrentFileHandler(string path, string Filename, TBoldModuleType ModuleType, bool Show, bool IsIncFile) =>
        fBoldFilehandler = BoldFileHandler.BoldFileHandlerForFile(path, Filename, ModuleType, Show, IsIncFile ? AddIncFileHeader : null);

    /// <summary>Adds the method (the expanded template) to the current file unless a method with its name is there.</summary>
    [Delphi(PortStatus.Ported, Note = "Protected virtual, so a generator can place the method in the file its own way; private in Delphi.")]
    protected virtual void EnsureMethod(TStrings Strings)
    {
        string SearchString = Manager.GetSearchStringfromMethodHeader(Strings[0], BoldGen.SEARCHPARAMETERLIST);
        if (!FindInCurrentFile(SearchString))
        {
            BoldFilehandler!.NewLine();
            BoldFilehandler.AddStrings(Strings);
        }
    }

    /// <summary>The include file of a class's method implementations; C# addition, so a generator can place them elsewhere.</summary>
    protected virtual string IncFileNameForClass(TMoldClass MoldClass) => MoldClass.EffectiveIncFileName(Manager.DefaultIncFileExtension);

    /// <summary>
    /// For each class with code stubs, opens its include file and adds the implementations it lacks: the model's methods
    /// (not abstract ones), overrides of methods marked OverrideInAllSubclasses, the get and set methods of native
    /// attributes, and the derive and reverse derive methods of manually derived members.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested InitializeMethod is the private helper InitializeEnsuredMethod.")]
    public virtual void EnsureMethodImplementations()
    {
        for (int F = 0; F < MoldModel.Components.Count; F++)
        {
            fCurrentComponent = MoldModel.Components[F];
            string CurrentIncFileName = "";
            for (int C = 0; C < MoldModel.Classes.Count; C++)
            {
                if (BoldLog.ProcessInterruption())
                {
                    return;
                }

                CurrentClass = MoldModel.Classes[C];
                if (CurrentClass.Component == MoldModel.Components[F])
                {
                    if (CurrentClass.HasCodeStubs && CurrentClass.Component == MoldModel.Components[F])
                    {
                        string LastIncFileName = CurrentIncFileName;
                        CurrentIncFileName = IncFileNameForClass(CurrentClass);
                        if (SysUtils.AnsiCompareText(LastIncFileName, CurrentIncFileName) != 0)
                        {
                            SetCurrentFileHandler(BaseFilePath, CurrentIncFileName, TBoldModuleType.mtIncFile, true, true);
                            InitializeMethodIndex();
                        }

                        BoldLog.LogFmt(BoldMoldConsts.sProcessingClassXFileY, [CurrentClass.Name, CurrentIncFileName]);

                        // user defined methods
                        for (int i = 0; i < CurrentClass.Methods.Count; i++)
                        {
                            if (CurrentClass.Methods[i].FuncType != TDelphiFunctionType.dfAbstractVirtual)
                            {
                                TBoldTemplateHolder template = Manager.MethodTemplate;
                                InitializeEnsuredMethod(CurrentClass, CurrentClass.Methods[i], template.Variables);
                                template.Variables.SetVariable("METHODHEADER", MethodToDelphiHeader(CurrentClass, CurrentClass.Methods[i], ImplementationTag, true, false));
                                template.Variables.SetVariable("CALLINHERITED", BoldBooleanToTemplateVar(CurrentClass.Methods[i].CanCallInherited));
                                EnsureMethod(template.ExpandedTemplate);
                            }
                        }

                        // auto override methods
                        for (int i = 0; i < CurrentClass.AllAutoOverrideMethods.Count; i++)
                        {
                            TBoldTemplateHolder template = Manager.MethodTemplate;
                            TMoldMethod MoldMethod = CurrentClass.AllAutoOverrideMethods[i];
                            InitializeEnsuredMethod(CurrentClass, MoldMethod, template.Variables);
                            template.Variables.SetVariable("METHODHEADER", MethodToDelphiHeader(CurrentClass, MoldMethod, ImplementationTag, true, true));
                            bool SuperMethodIsAbstract = MoldMethod.FuncType == TDelphiFunctionType.dfAbstractVirtual && CurrentClass.SuperClass == MoldMethod.MoldClass;
                            template.Variables.SetVariable("CALLINHERITED", BoldBooleanToTemplateVar(!SuperMethodIsAbstract));
                            EnsureMethod(template.ExpandedTemplate);
                        }

                        // native attributes get method
                        for (int i = CurrentClass.FirstOwnNativeAttributeIndex; i < CurrentClass.AllNativeAttributes.Count; i++)
                        {
                            TMoldAttribute Attr = CurrentClass.AllNativeAttributes[i];
                            if (Attr.DelphiPropertyRead is TDelphiPropertyAccessKind.pkPrivateMethod or TDelphiPropertyAccessKind.pkProtectedVirtualMethod)
                            {
                                string TypeName = GetNativeDelphiTypeForModelNameNoDefaults(Attr);
                                TBoldTemplateHolder template = Manager.MethodTemplate;
                                template.Variables.SetVariable("CLASSNAME", CurrentClass.ExpandedDelphiName);
                                template.Variables.SetVariable("METHODHEADER", Manager.ReadMethodSignature(CurrentClass.ExpandedDelphiName, Attr.ExpandedDelphiName, TypeName));
                                template.Variables.SetVariable("CALLINHERITED", BoldBooleanToTemplateVar(false));
                                EnsureMethod(template.ExpandedTemplate);
                            }
                        }

                        // native attributes set method
                        for (int i = CurrentClass.FirstOwnNativeAttributeIndex; i < CurrentClass.AllNativeAttributes.Count; i++)
                        {
                            TMoldAttribute Attr = CurrentClass.AllNativeAttributes[i];
                            if (Attr.DelphiPropertyWrite is TDelphiPropertyAccessKind.pkPrivateMethod or TDelphiPropertyAccessKind.pkProtectedVirtualMethod)
                            {
                                string TypeName = GetNativeDelphiTypeForModelNameNoDefaults(Attr);
                                TBoldTemplateHolder template = Manager.MethodTemplate;
                                template.Variables.SetVariable("CLASSNAME", CurrentClass.ExpandedDelphiName);
                                template.Variables.SetVariable("METHODHEADER", Manager.WriteMethodSignature(CurrentClass.ExpandedDelphiName, Attr.ExpandedDelphiName, TypeName));
                                template.Variables.SetVariable("CALLINHERITED", BoldBooleanToTemplateVar(false));
                                EnsureMethod(template.ExpandedTemplate);
                            }
                        }

                        // derived members (always procedures)
                        for (int i = 0; i < CurrentClass.AllBoldMembers.Count; i++)
                        {
                            TMoldMember member = CurrentClass.AllBoldMembers[i];
                            if (member.ManuallyDerived &&
                                (i >= CurrentClass.FirstOwnBoldMemberIndex || BoldDefaultTaggedValues.TVIsTrue(member.BoldTVByName(BoldDefaultTaggedValues.TAG_VIRTUALDERIVE))))
                            {
                                TBoldTemplateHolder template = Manager.DerivedMethodTemplate;
                                TBoldTypeNameMapping? Mapping = member is TMoldAttribute attribute ? TypeNameDictionary.MappingForModelName(attribute.BoldType) : null;
                                if (Mapping is not null && Mapping.ExpandedAccessor != "" && Mapping.ExpandedNativeType != "")
                                {
                                    template.Variables.SetVariable("MEMBERHASNATIVE", "1");
                                    template.Variables.SetVariable("MEMBERNATIVEACCESSOR", Mapping.ExpandedAccessor);
                                    template.Variables.SetVariable("MEMBERNATIVETYPE", Mapping.ExpandedNativeType);
                                }
                                else
                                {
                                    template.Variables.SetVariable("MEMBERHASNATIVE", "0");
                                }

                                template.Variables.SetVariable("CLASSNAME", CurrentClass.ExpandedDelphiName);
                                template.Variables.SetVariable("INHERITEDCALL", "inherited");
                                template.Variables.SetVariable("INTRODUCEDHERE", BoldBooleanToTemplateVar(i >= CurrentClass.FirstOwnBoldMemberIndex));
                                template.Variables.SetVariable("MEMBERNAME", member.ExpandedDelphiName);
                                AddSuperClassName(template.Variables, CurrentClass);
                                EnsureMethod(template.ExpandedTemplate);

                                if (member.ReverseDerived)
                                {
                                    TBoldTemplateHolder reverse = Manager.ReverseDeriveMethodTemplate;
                                    reverse.Variables.SetVariable("CLASSNAME", CurrentClass.ExpandedDelphiName);
                                    reverse.Variables.SetVariable("INHERITEDCALL", "inherited");
                                    reverse.Variables.SetVariable("INTRODUCEDHERE", BoldBooleanToTemplateVar(i >= CurrentClass.FirstOwnBoldMemberIndex));
                                    reverse.Variables.SetVariable("MEMBERNAME", member.ExpandedDelphiName);
                                    AddSuperClassName(reverse.Variables, CurrentClass);
                                    EnsureMethod(reverse.ExpandedTemplate);
                                }
                            }
                        }
                    }

                    BoldLog.ProgressStep();
                    BoldLog.Sync();
                }
            }
        }
    }

    private void InitializeEnsuredMethod(TMoldClass MoldClass, TMoldMethod MoldMethod, TBoldTemplateVariables Variables)
    {
        Variables.SetVariable("INHERITEDCALL", Manager.GenerateInheritedCall(MoldClass, MoldMethod));
        Variables.SetVariable("CLASSNAME", MoldClass.ExpandedDelphiName);
        AddSuperClassName(Variables, MoldClass);
    }

    [Delphi(PortStatus.Ported)]
    private string MethodToDelphiHeader(TMoldClass OwningClass, TMoldMethod Method, int TagValue, bool AddSignature, bool AutoOverride) =>
        Manager.MethodToCodeHeader(OwningClass, Method, TagValue, AddSignature, AutoOverride);

    /// <summary>Fills a new include file with the include file header of the current component.</summary>
    [Delphi(PortStatus.Ported)]
    private void AddIncFileHeader(TStringList StringList)
    {
        InitializeTemplateForComponent(Manager.IncFileHeaderTemplate, MoldModel, fCurrentComponent!, false);
        StringList.AddStrings(Manager.IncFileHeaderTemplate.ExpandedTemplate);
    }

    /// <summary>Whether a method header in the current file contains the text (ignoring case); only the name is searched for.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool FindInCurrentFile(string s)
    {
        s = SysUtils.UpperCase(s);
        for (int i = 0; i < MethodIndex.Count; i++)
        {
            if (SysUtils.Pos(s, MethodIndex[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Collects the lines of the current file that hold a method header.</summary>
    [Delphi(PortStatus.Ported)]
    protected void InitializeMethodIndex()
    {
        MethodIndex.Clear();
        for (int i = 0; i < BoldFilehandler!.Count; i++)
        {
            if (Manager.StringContainsMethodHeader(BoldFilehandler.Content(i)))
            {
                MethodIndex.Add(SysUtils.UpperCase(BoldFilehandler.Content(i)));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMoldModel(TMoldModel Value)
    {
        if (fMoldModel != Value)
        {
            fMoldModel = Value;
            Value.EnsureTopSorted();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetGenerateBold1CompatibleCode(bool Value) => FGenerateBold1CompatibleCode = Value;

    /// <summary>The native type of the exact mapping of the attribute's type, else the type name itself.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetNativeDelphiTypeForModelNameNoDefaults(TMoldAttribute Attr) =>
        TypeNameDictionary.ExactMappingForModelName(Attr.BoldType)?.ExpandedNativeType ?? Attr.BoldType;

    /// <summary>Adds the comma separated values as BASENAME.0, BASENAME.1, ... and their count as BASENAMECOUNT.</summary>
    [Delphi(PortStatus.Ported)]
    private static void AddVarList(TBoldTemplateHolder Template, string VariableBaseName, string CommaValues)
    {
        var sList = new TStringList { CommaText = CommaValues };
        while (sList[sList.Count - 1] == "")
        {
            sList.Delete(sList.Count - 1);
        }

        for (int i = 0; i < sList.Count; i++)
        {
            Template.Variables.ForceAdd(VariableBaseName + "." + SysUtils.IntToStr(i), sList[i], DelphiSet<int>.Empty);
        }

        Template.Variables.ForceAdd(VariableBaseName + "COUNT", SysUtils.IntToStr(sList.Count), DelphiSet<int>.Empty);
    }

    [Delphi(PortStatus.Ported)]
    private static void AddSuperClassName(TBoldTemplateVariables variables, TMoldClass MoldClass) =>
        variables.SetVariable("SUPERCLASSNAME", MoldClass.SuperClass is not null ? MoldClass.SuperClass.ExpandedDelphiName : "TBoldObject");

    [Delphi(PortStatus.Ported)]
    private static string MakePersistenceInterfaceName(TMoldClass MoldClass) =>
        BoldNameExpander.BoldExpandName("IPersistent<Name>", MoldClass.Name, TExpansionType.xtDelphi, -1, TBoldNationalCharConversion.nccDefault);

    /// <summary>Generates the business classes: expands the template manager's file templates.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateBusinessObjectCode() => ExpandTemplateList(Manager.FileTemplates);

    [Delphi(PortStatus.Ported)]
    public void GeneratePersistenceInterfaces()
    {
        if (Manager.PersistenceInterfaceTemplate is { } template)
        {
            var TemplateList = new TBoldTemplateList();
            TemplateList.Add(template);
            ExpandTemplateList(TemplateList);
        }
        else
        {
            throw new EBold(BoldMoldConsts.sNoTemplateForPersistenceInterfaces);
        }
    }

    /// <summary>A unit for .pas, an include file for .inc, else text.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldModuleType ModuleTypeForFile(string FileName)
    {
        string Extension = SysUtils.UpperCase(SysUtils.ExtractFileExt(FileName));
        return Extension switch
        {
            ".PAS" => TBoldModuleType.mtUnit,
            ".INC" => TBoldModuleType.mtIncFile,
            _ => TBoldModuleType.mtText,
        };
    }

    [Delphi(PortStatus.Ported)] protected TStringList MethodIndex => FMethodIndex;

    [Delphi(PortStatus.Ported)]
    protected TMoldClass CurrentClass
    {
        get => fCurrentClass!;
        set => fCurrentClass = value;
    }

    [Delphi(PortStatus.Ported)]
    public TMoldModel MoldModel
    {
        get => fMoldModel!;
        set => SetMoldModel(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool UseTypedLists
    {
        get => fUseTypedLists;
        set => fUseTypedLists = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldTypeNameDictionary TypeNameDictionary => fTypeNameDictionary;
    [Delphi(PortStatus.Ported)] public TStringList UnMappedTypes => fUnmappedTypes;
    [Delphi(PortStatus.Ported)] public TStringList TypesWithoutNative => fTypesWithoutNative;

    [Delphi(PortStatus.Ported)]
    public bool GenerateBold1CompatibleCode
    {
        get => FGenerateBold1CompatibleCode;
        set => SetGenerateBold1CompatibleCode(value);
    }

    [Delphi(PortStatus.Ported)]
    public string BaseFilePath
    {
        get => fBaseFilePath;
        set => fBaseFilePath = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldFileHandler? BoldFilehandler => fBoldFilehandler;
}

/// <summary>
/// Prepares a model for code generation by moving classes between components (units): implicit link classes go to
/// their superclass's component or to the component of both association ends, and components depend on the
/// components of their superclasses.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldCodeGenInitializer
{
    private readonly TMoldModel fMoldModel;

    [Delphi(PortStatus.Ported, Symbol = "TBoldCodeGenInitializer.create")]
    public TBoldCodeGenInitializer(TMoldModel MoldModel)
    {
        fMoldModel = MoldModel;
    }

    [Delphi(PortStatus.Ported)]
    protected bool FilterLinkClass(TMoldClass MoldClass) => MoldClass.Association is not null;

    [Delphi(PortStatus.Ported)]
    protected TMoldComponent? FindComponent(string ComponentName) => MoldModel.FindComponent(ComponentName);

    [Delphi(PortStatus.Ported)]
    protected TMoldComponent? RenameComponent(string OldComponentName, string NewComponentName) => MoldModel.RenameComponent(OldComponentName, NewComponentName);

    [Delphi(PortStatus.Ported)]
    protected TMoldComponent EnsureComponent(string ComponentName) => MoldModel.EnsureComponent(ComponentName);

    /// <summary>Makes the sub component depend on the super component (both must exist).</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureDependency(string SubComponentName, string SuperComponentname)
    {
        if (SysUtils.AnsiCompareText(SubComponentName, SuperComponentname) == 0)
        {
            return;
        }

        TMoldComponent? SubComponent = FindComponent(SubComponentName);
        TMoldComponent? SuperComponent = FindComponent(SuperComponentname);
        if (SubComponent is not null && SuperComponent is not null && SubComponent.Dependencies.IndexOf(SuperComponent) == -1)
        {
            SubComponent.Dependencies.Add(SuperComponent);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void EnsureInheritanceDependencies()
    {
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            if (MoldModel.Classes[i].SuperClass is { } super)
            {
                EnsureDependency(MoldModel.Classes[i].Component.Name, super.Component.Name);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void MoveClassesToComponent(TMoldClassFilterFunction MoldClassFilterFunction, string ComponentName)
    {
        TMoldComponent? TargetComponent = null;
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            if (MoldClassFilterFunction(MoldModel.Classes[i]))
            {
                TargetComponent ??= EnsureComponent(ComponentName);
                MoldModel.Classes[i].Component = TargetComponent;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void MoveClassTreeToComponent(TMoldClass? SuperClass, string ComponentName, bool MoveSuperClass)
    {
        TMoldComponent? TargetComponent = null;
        if (SuperClass is null)
        {
            throw new Exception(BoldMoldConsts.sMoveToComponent_NoSuperClass);
        }

        if (MoldModel is null)
        {
            throw new Exception(BoldMoldConsts.sMoveToComponent_NoMoldModel);
        }

        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            if (MoldModel.Classes[i].ChildTo(SuperClass) && (MoveSuperClass || MoldModel.Classes[i] != SuperClass))
            {
                TargetComponent ??= EnsureComponent(ComponentName);
                MoldModel.Classes[i].Component = TargetComponent;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void MoveClassToComponent(string ClassName, string ComponentName) => MoveClassToComponentByClass(MoldModel.GetClassByName(ClassName), ComponentName);

    [Delphi(PortStatus.Ported)]
    protected void MoveClassToComponentByClass(TMoldClass MoldClass, string ComponentName) => MoldClass.Component = EnsureComponent(ComponentName);

    /// <summary>
    /// Moves an implicit link class to the component of its association ends, when both ends are in one component other
    /// than the link class's and that component depends on the link class's.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested GetAssociationEndComponent is a private helper.")]
    protected void MoveImplicitLinkClassesToAssociationEndComponent()
    {
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            if (MoldModel.Classes[i].Association is { } association)
            {
                TMoldClass LinkClass = MoldModel.Classes[i];
                // Only for implicit link classes.
                if (LinkClass.TVByName(BoldDefaultTaggedValues.BOLDBOLDIFYPREFIX + BoldDefaultTaggedValues.TAG_AUTOCREATED) == BoldDefaultTaggedValues.TV_TRUE)
                {
                    TMoldComponent? Component1 = GetAssociationEndComponent(association, 0);
                    TMoldComponent? Component2 = GetAssociationEndComponent(association, 1);
                    // If the two ends belong to one component other than the link class's, and it depends on the link
                    // class's component, the link class moves there.
                    if (Component1 is not null && Component1 != LinkClass.Component && Component1 == Component2 && Component1.DependentOf(LinkClass.Component))
                    {
                        LinkClass.Component = Component1;
                    }
                }
            }
        }
    }

    private static TMoldComponent? GetAssociationEndComponent(TMoldAssociation Association, int Index) =>
        Association.Roles[Index] is { MoldClass: { } moldClass } ? moldClass.Component : null;

    /// <summary>Moves an implicit link class of the main component to its superclass's component, when that is another one.</summary>
    [Delphi(PortStatus.Ported)]
    protected void MoveImplicitLinkClassesToSuperClassComponent()
    {
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            if (MoldModel.Classes[i].Association is not null)
            {
                TMoldClass LinkClass = MoldModel.Classes[i];
                // An implicit link class in the default component ...
                if (LinkClass.Component == MoldModel.MainComponent &&
                    LinkClass.TVByName(BoldDefaultTaggedValues.BOLDBOLDIFYPREFIX + BoldDefaultTaggedValues.TAG_AUTOCREATED) == BoldDefaultTaggedValues.TV_TRUE &&
                    LinkClass.SuperClass!.Component != MoldModel.MainComponent)
                {
                    // ... that inherits from a class in another component moves there.
                    LinkClass.Component = LinkClass.SuperClass.Component;
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TMoldModel MoldModel => fMoldModel;

    /// <summary>Moves implicit link classes to their superclass's component, adds the inheritance dependencies, then moves implicit link classes to their ends' component.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Execute()
    {
        MoveImplicitLinkClassesToSuperClassComponent();
        EnsureInheritanceDependencies();
        MoveImplicitLinkClassesToAssociationEndComponent();
    }

    /// <summary>Logs each class whose include file has the name of another component's include file; false when one does.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ValidateFileNames()
    {
        bool result = true;
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            for (int j = 0; j < MoldModel.Components.Count; j++)
            {
                if (MoldModel.Classes[i].Component != MoldModel.Components[j] &&
                    SysUtils.AnsiCompareText(MoldModel.Classes[i].EffectiveIncFileName(BoldGeneratorTemplatesManager().DefaultIncFileExtension),
                        MoldModel.Components[j].Name + "." + BoldGeneratorTemplatesManager().DefaultIncFileExtension) == 0)
                {
                    BoldLogHandler.BoldLog().LogFmt(BoldMoldConsts.sCollidingFileName, [MoldModel.Classes[i].Name, MoldModel.Components[j].Name]);
                    result = false;
                }
            }
        }

        return result;
    }
}

/// <summary>Unit-level constants and variables of BoldGen.</summary>
public static class BoldGen
{
    [Delphi(PortStatus.Ported)] public const int INDENTSIZE = 2;
    [Delphi(PortStatus.Ported)] public const int DEBUGGERWORKAROUNDINTERVAL = 50;
    [Delphi(PortStatus.Ported)] public const bool AddVisitorSupport = true;

    internal const bool SEARCHPARAMETERLIST = false;

    /// <summary>The generator class to use.</summary>
    [Delphi(PortStatus.Ported)] public static Type BoldGeneratorClass = typeof(TBoldGenerator);

    /// <summary>The code generator initializer to run before generating; null runs none.</summary>
    [Delphi(PortStatus.Ported)] public static Type? BoldCodeGenInitializerClass = typeof(TBoldCodeGenInitializer);
}
