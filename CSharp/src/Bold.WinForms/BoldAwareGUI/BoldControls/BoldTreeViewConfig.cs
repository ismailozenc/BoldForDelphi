// Delphi: Source/BoldAwareGUI/BoldControls/BoldTreeViewConfig.pas
// Port map: CSharp/port/units/BoldTreeViewConfig.md

namespace Bold;

/// <summary>Configures a tree view that shows any object of a model: its attributes and, below each role, the role's objects.</summary>
public static class BoldTreeViewConfig
{
    /// <summary>The text with its second character in upper case (Delphi's s[2] := UpCase(s[2]), after the opening quote).</summary>
    private static string UpCaseSecond(string s) => s.Length > 1 ? s[..1] + char.ToUpperInvariant(s[1]) + s[2..] : s;

    /// <summary>A list part that shows the attribute as "Name: value" in an attribute node.</summary>
    [Delphi(PortStatus.Ported)]
    private static void FillListpartForAttr(TBoldAttributeRTInfo RTAttr, TBoldGenericListPart Listpart)
    {
        string s = SysUtils.Format("'%s: ' + %s.asString", RTAttr.ExpressionName, RTAttr.ExpressionName); // do not localize
        Listpart.ElementExpression = UpCaseSecond(s);
        Listpart.InterpretAsList = false;
        Listpart.ControllerExpression = "'AttributeNode'"; // do not localize
    }

    /// <summary>A list part that shows the role in a role node (made for the class that defines the role), whose children are the role's objects.</summary>
    [Delphi(PortStatus.Ported)]
    private static void FillListpartForRole(TBoldRoleRTInfo RTRole, TBoldGenericListPart Listpart, TBoldClassTypeInfo DefiningClass, TBoldTreeView TreeView)
    {
        Listpart.ElementExpression = RTRole.ExpressionName;
        string RoleName = SysUtils.Format("%s.%s", DefiningClass.ExpressionName, RTRole.ExpressionName);
        Listpart.ControllerExpression = SysUtils.Format("'%s'", RoleName);
        Listpart.InterpretAsList = false;
        if (DefiningClass == RTRole.ClassTypeInfo)
        {
            TBoldNodeDescription description = TreeView.BoldProperties.NodeDescriptions.Add();
            description.Name = RoleName;
            description.TextController.Expression = UpCaseSecond(SysUtils.Format("'%s:'", RTRole.ExpressionName));
            TBoldGenericListPart part = description.ListController.Parts.Add();
            part.ControllerExpression = "oclType"; // do not localize
            part.InterpretAsList = RTRole.IsMultiRole;
        }
    }

    /// <summary>The class's node: its string and type, a part per attribute and per navigable role.</summary>
    [Delphi(PortStatus.Ported)]
    private static void FillNodeDescriptorForClass(TBoldClassTypeInfo ClassTypeInfo, TBoldNodeDescription NodeDescriptor, TBoldTreeView TreeView)
    {
        NodeDescriptor.Name = ClassTypeInfo.ExpressionName;
        NodeDescriptor.TextController.Expression = "self.asString + ': ' + oclType.asString"; // do not localize
        for (int i = 0; i < ClassTypeInfo.AllMembers.Count; i++)
        {
            TBoldClassTypeInfo DefiningClass = ClassTypeInfo;
            while (i < DefiningClass.FirstOwnMemberIndex)
            {
                DefiningClass = DefiningClass.SuperClassTypeInfo!;
            }

            if (ClassTypeInfo.AllMembers[i].IsRole)
            {
                var Role = (TBoldRoleRTInfo)ClassTypeInfo.AllMembers[i];
                if (Role.IsNavigable)
                {
                    FillListpartForRole(Role, NodeDescriptor.ListController.Parts.Add(), DefiningClass, TreeView);
                }
            }
            else
            {
                FillListpartForAttr((TBoldAttributeRTInfo)ClassTypeInfo.AllMembers[i], NodeDescriptor.ListController.Parts.Add());
            }
        }
    }

    /// <summary>Configures the tree view for every class of the system: an attribute node, a node per class and per role.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldGenericTreeView(TBoldSystemTypeInfo SystemTypeInfo, TBoldTreeView TreeView)
    {
        TreeView.BoldProperties.NodeDescriptions.Add().Name = "AttributeNode"; // do not localize
        TreeView.BoldProperties.Parts.Add().InterpretAsList = true;
        for (int i = 0; i < SystemTypeInfo.TopSortedClasses.Count; i++)
        {
            FillNodeDescriptorForClass(SystemTypeInfo.TopSortedClasses[i], TreeView.BoldProperties.NodeDescriptions.Add(), TreeView);
        }
    }
}
