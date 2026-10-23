// Delphi: Source/Handles/Core/BoldOclRepository.pas
// Port map: CSharp/port/units/BoldOclRepository.md

using System.ComponentModel;
using static Bold.HandlesConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclRepository", Symbol = "TBoldOclDefinitions.GetOwner", Note = "Gave the owning component for the object inspector's name path; OwningRepository gives it.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclRepository", Symbol = "TBoldOclDefinition.QueryInterface", Note = "Use a C# type test (obj is IBoldOCLComponent).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclRepository", Symbol = "TBoldOclDefinition._AddRef", Note = "No reference counting in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclRepository", Symbol = "TBoldOclDefinition.Destroy", Note = "Did nothing but call the inherited destructor.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldOclRepository", Symbol = "TBoldOclDefinition._Release", Note = "No reference counting in .NET.")]

namespace Bold;

/// <summary>
/// Named OCL expressions for a system: once connected to a system handle, an expression can use a definition by its
/// name, as %name.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclRepository : TBoldSubscribableComponent, IBoldValidateableComponent
{
    private TBoldOclDefinitions FOclDefinitions;
    private TBoldSystemHandle? FSystemHandle;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldOclRepository(IContainer? owner = null)
    {
        owner?.Add(this);
        FOclDefinitions = new TBoldOclDefinitions(this);
    }

    [Delphi(PortStatus.Ported)]
    private void SetOclDefinitions(TBoldOclDefinitions Value) => FOclDefinitions = Value;

    /// <summary>Moves the definition lookup from the old system handle to the new one.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetSystemHandle(TBoldSystemHandle? Value)
    {
        SystemHandle?.InstallOclDefinitionLookUp(null);
        FSystemHandle = Value;
        SystemHandle?.InstallOclDefinitionLookUp(LookUpOclDefinition);
    }

    /// <summary>Checks every definition's expression in its context; logs a missing system handle or type info.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix)
    {
        bool result = true;
        if (SystemHandle is null)
        {
            BoldLogHandler.BoldLog().LogFmt(sRepositoryHasNoSystemHandle, [NamePrefix, Name]);
        }
        else if (SystemHandle.StaticSystemTypeInfo is null)
        {
            BoldLogHandler.BoldLog().LogFmt(sSystemHandleHasNoTypeInfo, [NamePrefix, Name]);
        }
        else
        {
            for (int i = 0; i < OclDefinitions.Count; i++)
            {
                TBoldElementTypeInfo? Context = SystemHandle.StaticSystemTypeInfo.ElementTypeInfoByExpressionName(OclDefinitions[i].Context);
                result = ComponentValidator.ValidateExpressionInContext(
                    OclDefinitions[i].Expression,
                    Context,
                    NamePrefix + Name + "." + OclDefinitions[i].Name) && result;
            }
        }

        return result;
    }

    bool IBoldValidateableComponent.ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix) =>
        ValidateComponent(ComponentValidator, NamePrefix);

    /// <summary>The expression of the named definition, or empty.</summary>
    [Delphi(PortStatus.Ported)]
    public string LookUpOclDefinition(string Name) => OclDefinitions.LookUpOclDefinition(Name);

    [Delphi(PortStatus.Ported)]
    public TBoldOclDefinitions OclDefinitions
    {
        get => FOclDefinitions;
        set => SetOclDefinitions(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystemHandle? SystemHandle
    {
        get => FSystemHandle;
        set => SetSystemHandle(value);
    }
}

/// <summary>The definitions of an OCL repository, with unique names.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclDefinitions : TCollection
{
    private readonly TBoldOclRepository fOwningRepository;

    [Delphi(PortStatus.Ported)]
    public TBoldOclDefinitions(TBoldOclRepository OwningRepository)
        : base(typeof(TBoldOclDefinition))
    {
        fOwningRepository = OwningRepository;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldOclDefinition GetItems(int Index) => (TBoldOclDefinition)base[Index];

    [Delphi(PortStatus.Ported)] protected internal TBoldOclRepository OwningRepository => fOwningRepository;

    [Delphi(PortStatus.Ported)]
    protected internal bool NameIsUnique(string Name)
    {
        for (int i = 0; i < Count; i++)
        {
            if (SysUtils.AnsiCompareStr(Name, this[i].Name) == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The first free name of Ocl1, Ocl2...</summary>
    [Delphi(PortStatus.Ported)]
    public string GetUniqueName()
    {
        int i = 1;
        string result;
        do
        {
            result = "Ocl" + SysUtils.IntToStr(i); // do not translate
            i++;
        }
        while (!NameIsUnique(result));
        return result;
    }

    /// <summary>The expression of the definition with the name (case matters), or empty.</summary>
    [Delphi(PortStatus.Ported)]
    public string LookUpOclDefinition(string Name)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i].Name == Name)
            {
                return this[i].Expression;
            }
        }

        return "";
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclDefinitions.Items", Note = "The default indexed property in Delphi.")]
    public new TBoldOclDefinition this[int Index] => GetItems(Index);
}

/// <summary>A named OCL expression in the context of a class (or other type) of the repository's system.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclDefinition : TCollectionItem, IBoldOCLComponent
{
    private string fName = "";
    private string fExpression = "";
    private string fContext = "";

    /// <summary>Adds the definition to the collection with a new unique name.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldOclDefinition(TCollection? Collection)
        : base(Collection)
    {
        Name = ((TBoldOclDefinitions)Collection!).GetUniqueName();
    }

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Expression) => fExpression = Expression;

    [Delphi(PortStatus.Ported)] private string GetExpression() => fExpression;
    [Delphi(PortStatus.Ported)] private TBoldExternalVariableList? GetVariableList() => null;
    [Delphi(PortStatus.Ported)] private TBoldOclDefinitions GetDefinitions() => (TBoldOclDefinitions)Collection!;

    [Delphi(PortStatus.Ported)]
    private TBoldSystemTypeInfo? GetSystemTypeInfo() => Definitions.OwningRepository.SystemHandle?.StaticSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    private void SetName(string Value)
    {
        if (Value != Name)
        {
            if (!((TBoldOclDefinitions)Collection!).NameIsUnique(Value))
            {
                throw new EBold(SysUtils.Format(sNameNotUnique, Value));
            }

            fName = Value;
        }
    }

    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);
    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();
    string IBoldOCLComponent.GetExpression() => GetExpression();

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => "%" + Name + ": " + Expression;

    /// <summary>The type named by Context in the repository's system.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElementTypeInfo? GetContextType() => SystemTypeInfo?.ElementTypeInfoByExpressionName(Context);

    [Delphi(PortStatus.Ported)] public TBoldOclDefinitions Definitions => GetDefinitions();
    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo? SystemTypeInfo => GetSystemTypeInfo();

    [Delphi(PortStatus.Ported)]
    public string Name
    {
        get => fName;
        set => SetName(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Expression
    {
        get => GetExpression();
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Context
    {
        get => fContext;
        set => fContext = value;
    }
}
