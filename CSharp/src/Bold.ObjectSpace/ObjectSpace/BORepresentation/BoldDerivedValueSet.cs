// Delphi: Source/ObjectSpace/BORepresentation/BoldDerivedValueSet.pas
// Port map: CSharp/port/units/BoldDerivedValueSet.md

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>Value set values taken from the objects of a class: an integer attribute and string attributes per object.</summary>
[Delphi(PortStatus.Ported)]
public class TBADerivedValueSetValueList : TBAValueSetValueList
{
    /// <summary>Adds a value with the members' strings as its representations.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void AddMembers(int Int, TBoldMember[] Members)
    {
        string[] DynArr = new string[Members.Length];
        for (int i = 0; i < Members.Length; i++)
        {
            DynArr[i] = Members[i].AsString;
        }

        Add(Int, DynArr);
    }

    /// <summary>Reads the values from all objects of the class; raises when the class or attributes are missing or wrong, or there are no objects.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBADerivedValueSetValueList.create")]
    public TBADerivedValueSetValueList(TBoldSystem System, string ClassToFollow, string IntValueAttribute, params string[] StrValueAttributes)
    {
        string ClassName = GetType().Name;
        TBoldClassTypeInfo ClassTypeInfo = System.BoldSystemTypeInfo.ClassTypeInfoByExpressionName(ClassToFollow)
            ?? throw new EBold(SysUtils.Format(sNoClassCalledX, ClassName, ClassToFollow));

        TBoldMemberRTInfo RTAttr = ClassTypeInfo.MemberRTInfoByExpressionName(IntValueAttribute)
            ?? throw new EBold(SysUtils.Format(sNoAttributeCalledX, ClassName, ClassToFollow, IntValueAttribute));
        if (RTAttr.MemberClass is null || !typeof(TBAInteger).IsAssignableFrom(RTAttr.MemberClass))
        {
            throw new EBold(SysUtils.Format(sXIsNotAnInteger, ClassName, ClassToFollow, IntValueAttribute));
        }

        int IntIndex = RTAttr.index;
        var DynArr = new TBoldMember[StrValueAttributes.Length];
        int[] IndexArr = new int[StrValueAttributes.Length];
        for (int i = 0; i < StrValueAttributes.Length; i++)
        {
            RTAttr = ClassTypeInfo.MemberRTInfoByExpressionName(StrValueAttributes[i])
                ?? throw new EBold(SysUtils.Format(sNoAttributeCalledX, ClassName, ClassToFollow, StrValueAttributes[i]));
            if (RTAttr.MemberClass is null || !typeof(TBoldAttribute).IsAssignableFrom(RTAttr.MemberClass))
            {
                throw new EBold(SysUtils.Format(sXIsNotAnAttribute, ClassName, ClassToFollow, StrValueAttributes[i]));
            }

            IndexArr[i] = RTAttr.index;
        }

        TBoldObjectList ObjectList = System.Classes(ClassTypeInfo.TopSortedIndex)!;
        if (ObjectList.Count == 0)
        {
            throw new EBold(SysUtils.Format(sNoValuesInValueSetList, ClassName, ClassTypeInfo.ExpressionName));
        }

        for (int i = 0; i < ObjectList.Count; i++)
        {
            for (int j = 0; j < StrValueAttributes.Length; j++)
            {
                DynArr[j] = ObjectList[i]!.BoldMembers(IndexArr[j]);
            }

            AddMembers(((TBAInteger)ObjectList[i]!.BoldMembers(IntIndex)).AsInteger, DynArr);
        }
    }
}
