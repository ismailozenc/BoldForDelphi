// Delphi: Source/Common/TaggedValues/BoldTaggedValueSupport.pas
// Port map: CSharp/port/units/BoldTaggedValueSupport.md

using static Bold.BoldDefaultTaggedValues;

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldOptimisticLockingMode { bolmDefault, bolmOff, bolmModifiedMembers, bolmAllMembers, bolmTimeStamp }

[Delphi(PortStatus.Ported)]
public enum TBoldAttributeKind { bastBold, bastDelphi }

[Delphi(PortStatus.Ported)]
public enum TDeleteAction { daDefault, daAllow, daProhibit, daCascade }

[Delphi(PortStatus.Ported)]
public enum TDelphiFunctionType { dfNormal, dfVirtual, dfOverride, dfDynamic, dfAbstractVirtual }

[Delphi(PortStatus.Ported)]
public enum TTableMapping { tmOwn, tmParent, tmChildren, tmImported }

[Delphi(PortStatus.Ported)]
public enum TDelphiPropertyAccessKind { pkNone, pkField, pkPrivateMethod, pkProtectedVirtualMethod }

[Delphi(PortStatus.Ported)]
public enum TBoldEvolutionState { esNormal, esToBeRemoved, esRemoved }

[Delphi(PortStatus.Ported)]
public enum TBoldNationalCharConversion { nccDefault, nccFalse, nccTrue }

[Delphi(PortStatus.Ported)]
public enum TBoldAssociationEndDefaultRegionMode { aedrmDefault, aedrmNone, aedrmCascade, aedrmIndependentCascade, aedrmExistence }

/// <summary>
/// Converts between tagged value strings and their enums, and lists the allowed strings of each enum tagged value.
/// Unknown strings convert to the default member; unknown enum values raise EBold.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldTaggedValueSupport
{
    [Delphi(PortStatus.Ported)]
    private static string UnknownValue(string Invoker) =>
        throw new EBold(SysUtils.Format(BoldCommonConst.sUnknownValue, nameof(TBoldTaggedValueSupport), Invoker));

    private static void AddAll<T>(TStrings Strings, Func<T, string> toString) where T : struct, Enum
    {
        foreach (T value in Enum.GetValues<T>())
        {
            Strings.Add(toString(value));
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void AddEvolutionStates(TStrings Strings) => AddAll<TBoldEvolutionState>(Strings, EvolutionStateToString);

    [Delphi(PortStatus.Ported)]
    public static void AddNationalCharConversions(TStrings Strings) => AddAll<TBoldNationalCharConversion>(Strings, NationalCharConversionToString);

    [Delphi(PortStatus.Ported)]
    public static void AddDefaultRegionModes(TStrings Strings) => AddAll<TBoldAssociationEndDefaultRegionMode>(Strings, DefaultRegionModeToString);

    [Delphi(PortStatus.Ported)]
    public static void AddOptimisticLockingModes(TStrings Strings) => AddAll<TBoldOptimisticLockingMode>(Strings, OptimisticLockingModeToString);

    [Delphi(PortStatus.Ported)]
    public static void AddDeleteActions(TStrings Strings) => AddAll<TDeleteAction>(Strings, DeleteActionToString);

    [Delphi(PortStatus.Ported)]
    public static void AddDelphiFunctionTypes(TStrings Strings) => AddAll<TDelphiFunctionType>(Strings, DelphiFunctionTypeToString);

    [Delphi(PortStatus.Ported)]
    public static void AddPropertyAccessKinds(TStrings Strings) => AddAll<TDelphiPropertyAccessKind>(Strings, DelphiPropertyAccessKindToString);

    [Delphi(PortStatus.Ported)]
    public static void AddAttributeKinds(TStrings Strings) => AddAll<TBoldAttributeKind>(Strings, AttributeKindToString);

    [Delphi(PortStatus.Ported)]
    public static void AddTableMappings(TStrings Strings) => AddAll<TTableMapping>(Strings, TableMappingToString);

    [Delphi(PortStatus.Ported)]
    public static TDelphiFunctionType StringToDelphiFunctionType(string Value) => Value switch
    {
        _ when Value == TV_DELPHIOPERATIONKIND_NORMAL => TDelphiFunctionType.dfNormal,
        _ when Value == TV_DELPHIOPERATIONKIND_VIRTUAL => TDelphiFunctionType.dfVirtual,
        _ when Value == TV_DELPHIOPERATIONKIND_OVERRIDE => TDelphiFunctionType.dfOverride,
        _ when Value == TV_DELPHIOPERATIONKIND_DYNAMIC => TDelphiFunctionType.dfDynamic,
        _ when Value == TV_DELPHIOPERATIONKIND_ABSTRACTVIRTUAL => TDelphiFunctionType.dfAbstractVirtual,
        "AbstractVirtual" => TDelphiFunctionType.dfAbstractVirtual, // legacy
        _ => TDelphiFunctionType.dfNormal,
    };

    [Delphi(PortStatus.Ported)]
    public static TBoldNationalCharConversion StringToNationalCharConversion(string value)
    {
        if (value == BoldDefs.DEFAULTNAME)
        {
            return TBoldNationalCharConversion.nccDefault;
        }

        if (TVIsTrue(value))
        {
            return TBoldNationalCharConversion.nccTrue;
        }

        return TVIsFalse(value) ? TBoldNationalCharConversion.nccFalse : TBoldNationalCharConversion.nccDefault;
    }

    [Delphi(PortStatus.Ported)]
    public static string NationalCharConversionToString(TBoldNationalCharConversion Value) => Value switch
    {
        TBoldNationalCharConversion.nccDefault => BoldDefs.DEFAULTNAME,
        TBoldNationalCharConversion.nccTrue => TV_TRUE,
        TBoldNationalCharConversion.nccFalse => TV_FALSE,
        _ => UnknownValue(nameof(NationalCharConversionToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TBoldStorage StringToStorage(string value) => value switch
    {
        _ when value == TV_STORAGE_INTERNAL => TBoldStorage.bsInternal,
        _ when value == TV_STORAGE_PARTIALLYEXTERNAL => TBoldStorage.bsPartiallyExternal,
        _ when value == TV_STORAGE_EXTERNAL => TBoldStorage.bsExternal,
        _ when value == TV_STORAGE_EXTERNALKEY => TBoldStorage.bsExternalKey,
        _ => TBoldStorage.bsInternal,
    };

    [Delphi(PortStatus.Ported)]
    public static string DelphiFunctionTypeToString(TDelphiFunctionType Value) => Value switch
    {
        TDelphiFunctionType.dfNormal => TV_DELPHIOPERATIONKIND_NORMAL,
        TDelphiFunctionType.dfVirtual => TV_DELPHIOPERATIONKIND_VIRTUAL,
        TDelphiFunctionType.dfOverride => TV_DELPHIOPERATIONKIND_OVERRIDE,
        TDelphiFunctionType.dfDynamic => TV_DELPHIOPERATIONKIND_DYNAMIC,
        TDelphiFunctionType.dfAbstractVirtual => TV_DELPHIOPERATIONKIND_ABSTRACTVIRTUAL,
        _ => UnknownValue(nameof(DelphiFunctionTypeToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TTableMapping StringToTableMapping(string Value) => Value switch
    {
        _ when Value == TV_TABLEMAPPING_OWN => TTableMapping.tmOwn,
        _ when Value == TV_TABLEMAPPING_PARENT => TTableMapping.tmParent,
        _ when Value == TV_TABLEMAPPING_CHILDREN => TTableMapping.tmChildren,
        _ when Value == TV_TABLEMAPPING_IMPORTED => TTableMapping.tmImported,
        _ => TTableMapping.tmOwn,
    };

    [Delphi(PortStatus.Ported)]
    public static string TableMappingToString(TTableMapping Value) => Value switch
    {
        TTableMapping.tmOwn => TV_TABLEMAPPING_OWN,
        TTableMapping.tmParent => TV_TABLEMAPPING_PARENT,
        TTableMapping.tmChildren => TV_TABLEMAPPING_CHILDREN,
        TTableMapping.tmImported => TV_TABLEMAPPING_IMPORTED,
        _ => UnknownValue(nameof(TableMappingToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TBoldAttributeKind StringToAttributeKind(string Value) =>
        Value == TV_ATTRIBUTEKIND_DELPHI ? TBoldAttributeKind.bastDelphi : TBoldAttributeKind.bastBold;

    [Delphi(PortStatus.Ported)]
    public static string AttributeKindToString(TBoldAttributeKind Value) => Value switch
    {
        TBoldAttributeKind.bastBold => TV_ATTRIBUTEKIND_BOLD,
        TBoldAttributeKind.bastDelphi => TV_ATTRIBUTEKIND_DELPHI,
        _ => UnknownValue(nameof(AttributeKindToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TDelphiPropertyAccessKind StringToDelphiPropertyAccessKind(string Value) => Value switch
    {
        _ when Value == TV_DPNONE => TDelphiPropertyAccessKind.pkNone,
        _ when Value == TV_DPFIELD => TDelphiPropertyAccessKind.pkField,
        _ when Value == TV_DPPRIVATEMETHOD => TDelphiPropertyAccessKind.pkPrivateMethod,
        _ when Value == TV_DPPROTECTEDVIRTUALMETHOD => TDelphiPropertyAccessKind.pkProtectedVirtualMethod,
        _ => TDelphiPropertyAccessKind.pkNone,
    };

    /// <summary>Case-insensitive; accepts the old names "Member" and "Class".</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldOptimisticLockingMode StringToOptimisticLockingMode(string Value)
    {
        bool Is(string s) => SysUtils.CompareText(Value, s) == 0;
        if (Is(BoldDefs.DEFAULTNAME))
        {
            return TBoldOptimisticLockingMode.bolmDefault;
        }

        if (Is(TV_OPTIMISTICLOCKING_OFF))
        {
            return TBoldOptimisticLockingMode.bolmOff;
        }

        if (Is(TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS) || Is(TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS_OLDNAME))
        {
            return TBoldOptimisticLockingMode.bolmModifiedMembers;
        }

        if (Is(TV_OPTIMISTICLOCKING_ALLMEMBERS) || Is(TV_OPTIMISTICLOCKING_ALLMEMBERS_OLDNAME))
        {
            return TBoldOptimisticLockingMode.bolmAllMembers;
        }

        return Is(TV_OPTIMISTICLOCKING_TIMESTAMP) ? TBoldOptimisticLockingMode.bolmTimeStamp : TBoldOptimisticLockingMode.bolmDefault;
    }

    [Delphi(PortStatus.Ported)]
    public static string OptimisticLockingModeToString(TBoldOptimisticLockingMode Value) => Value switch
    {
        TBoldOptimisticLockingMode.bolmDefault => BoldDefs.DEFAULTNAME,
        TBoldOptimisticLockingMode.bolmOff => TV_OPTIMISTICLOCKING_OFF,
        TBoldOptimisticLockingMode.bolmModifiedMembers => TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS,
        TBoldOptimisticLockingMode.bolmAllMembers => TV_OPTIMISTICLOCKING_ALLMEMBERS,
        TBoldOptimisticLockingMode.bolmTimeStamp => TV_OPTIMISTICLOCKING_TIMESTAMP,
        _ => UnknownValue(nameof(OptimisticLockingModeToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static string DelphiPropertyAccessKindToString(TDelphiPropertyAccessKind Value) => Value switch
    {
        TDelphiPropertyAccessKind.pkNone => TV_DPNONE,
        TDelphiPropertyAccessKind.pkField => TV_DPFIELD,
        TDelphiPropertyAccessKind.pkPrivateMethod => TV_DPPRIVATEMETHOD,
        TDelphiPropertyAccessKind.pkProtectedVirtualMethod => TV_DPPROTECTEDVIRTUALMETHOD,
        _ => UnknownValue(nameof(DelphiPropertyAccessKindToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TDeleteAction StringToDeleteAction(string Value) => Value switch
    {
        _ when Value == TV_DELETEACTION_DEFAULT => TDeleteAction.daDefault,
        _ when Value == TV_DELETEACTION_ALLOW => TDeleteAction.daAllow,
        _ when Value == TV_DELETEACTION_PROHIBIT => TDeleteAction.daProhibit,
        _ when Value == TV_DELETEACTION_CASCADE => TDeleteAction.daCascade,
        _ => TDeleteAction.daDefault,
    };

    [Delphi(PortStatus.Ported)]
    public static string DeleteActionToString(TDeleteAction Value) => Value switch
    {
        TDeleteAction.daDefault => TV_DELETEACTION_DEFAULT,
        TDeleteAction.daAllow => TV_DELETEACTION_ALLOW,
        TDeleteAction.daProhibit => TV_DELETEACTION_PROHIBIT,
        TDeleteAction.daCascade => TV_DELETEACTION_CASCADE,
        _ => UnknownValue(nameof(DeleteActionToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TBoldEvolutionState StringToEvolutionState(string Value)
    {
        if (SysUtils.SameText(Value, TV_EVOLUTIONSTATE_TOBEREMOVED))
        {
            return TBoldEvolutionState.esToBeRemoved;
        }

        return SysUtils.SameText(Value, TV_EVOLUTIONSTATE_REMOVED) ? TBoldEvolutionState.esRemoved : TBoldEvolutionState.esNormal;
    }

    [Delphi(PortStatus.Ported)]
    public static string EvolutionStateToString(TBoldEvolutionState Value) => Value switch
    {
        TBoldEvolutionState.esNormal => TV_EVOLUTIONSTATE_NORMAL,
        TBoldEvolutionState.esToBeRemoved => TV_EVOLUTIONSTATE_TOBEREMOVED,
        TBoldEvolutionState.esRemoved => TV_EVOLUTIONSTATE_REMOVED,
        _ => UnknownValue(nameof(EvolutionStateToString)),
    };

    [Delphi(PortStatus.Ported)]
    public static TBoldAssociationEndDefaultRegionMode StringToDefaultRegionMode(string value) => value switch
    {
        _ when value == TV_DEFAULTREGIONMODE_ASSOCIATIONEND_DEFAULT => TBoldAssociationEndDefaultRegionMode.aedrmDefault,
        _ when value == TV_DEFAULTREGIONMODE_ASSOCIATIONEND_NONE => TBoldAssociationEndDefaultRegionMode.aedrmNone,
        _ when value == TV_DEFAULTREGIONMODE_ASSOCIATIONEND_EXISTENCE => TBoldAssociationEndDefaultRegionMode.aedrmExistence,
        _ when value == TV_DEFAULTREGIONMODE_ASSOCIATIONEND_INDEPENDENTCASCADE => TBoldAssociationEndDefaultRegionMode.aedrmIndependentCascade,
        _ when value == TV_DEFAULTREGIONMODE_ASSOCIATIONEND_CASCADE => TBoldAssociationEndDefaultRegionMode.aedrmCascade,
        _ => TBoldAssociationEndDefaultRegionMode.aedrmDefault,
    };

    [Delphi(PortStatus.Ported)]
    public static string DefaultRegionModeToString(TBoldAssociationEndDefaultRegionMode Value) => Value switch
    {
        TBoldAssociationEndDefaultRegionMode.aedrmDefault => TV_DEFAULTREGIONMODE_ASSOCIATIONEND_DEFAULT,
        TBoldAssociationEndDefaultRegionMode.aedrmNone => TV_DEFAULTREGIONMODE_ASSOCIATIONEND_NONE,
        TBoldAssociationEndDefaultRegionMode.aedrmExistence => TV_DEFAULTREGIONMODE_ASSOCIATIONEND_EXISTENCE,
        TBoldAssociationEndDefaultRegionMode.aedrmIndependentCascade => TV_DEFAULTREGIONMODE_ASSOCIATIONEND_INDEPENDENTCASCADE,
        TBoldAssociationEndDefaultRegionMode.aedrmCascade => TV_DEFAULTREGIONMODE_ASSOCIATIONEND_CASCADE,
        _ => UnknownValue(nameof(DefaultRegionModeToString)),
    };

    /// <summary>"True" or "False" in any case; anything else raises EBold.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool StringToBoolean(string Value)
    {
        if (SysUtils.SameText(Value, TV_TRUE))
        {
            return true;
        }

        if (SysUtils.SameText(Value, TV_FALSE))
        {
            return false;
        }

        UnknownValue(nameof(StringToBoolean));
        return false;
    }
}
