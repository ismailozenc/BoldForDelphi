// Delphi: Source/Unassigned/AutoInc/BoldAttributeAutoInc.pas
// Port map: CSharp/port/units/BoldAttributeAutoInc.md
// An integer attribute the database assigns (an identity column): it starts at -1, cannot be modified, and is
// refetched after each update.

using System.Runtime.CompilerServices;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAttributeAutoInc", Symbol = "finalization", Note = "Removed the registration; the registry lives as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldAttributeAutoInc.</summary>
public static class BoldAttributeAutoInc
{
    /// <summary>Registers TBAAutoInc as a member type, when the assembly loads.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "A module initializer, so the class is registered before any model is used.")]
#pragma warning disable CA2255 // The registration must run before any system type info is built, as Delphi's initialization section does.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialization() =>
        BoldMemberTypeDictionary.BoldMemberTypes().AddMemberTypeDescriptor(typeof(TBAAutoInc), TBoldAbstractionLevel.alAbstract);
}

/// <summary>An integer the database assigns on insert.</summary>
[Delphi(PortStatus.Ported)]
public class TBAAutoInc : TBAInteger
{
    /// <summary>The value has been assigned in the database as a part of the update; invalidate to enforce a refetch.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void CompleteUpdate()
    {
        base.CompleteUpdate();
        Invalidate();
    }

    /// <summary>Starts at -1 until the database has assigned the value.</summary>
    [Delphi(PortStatus.Ported)]
    internal override void DoSetInitialValue()
    {
        ((IBoldIntegerContent)AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)).asInteger = -1;
        base.DoSetInitialValue();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        DoSetInitialValue();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override bool MayModify()
    {
        Bold.BoldSystem.SetBoldLastFailureReason(new TBoldFailureReason(SysUtils.Format("AutoInc (%s) attributes can not be modified", DisplayName), this));
        return false;
    }
}
