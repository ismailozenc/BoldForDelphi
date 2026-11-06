// Delphi: Source/UMLModel/ModelLinks/Core/BoldUMLModelLinkSupport.pas
// Port map: CSharp/port/units/BoldUMLModelLinkSupport.md

namespace Bold;

/// <summary>What links do to elements: constraints to and from text, and a leading slash as derived.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldUMLModelLinkSupport
{
    private const string CONSTRAINT_NAME_TERMINATOR = ":";
    private const string CONSTRAINT_EXPRESSION_TERMINATOR = BoldDefs.BOLDCRLF;

    /// <summary>Makes a constraint of each line, "name: body".</summary>
    [Delphi(PortStatus.Ported)]
    public static void StringToConstraints(TBoldUMLObject Element, string ConstraintString)
    {
        var allConstraints = new TStringList { Text = ConstraintString };
        for (int i = 0; i < allConstraints.Count; i++)
        {
            string aConstraint = allConstraints[i];
            int p = aConstraint.IndexOf(CONSTRAINT_NAME_TERMINATOR, StringComparison.Ordinal);
            TBoldUMLObject constraint = Element.Graph.Create("Constraint");
            constraint.AddRef("constrainedElement", Element);
            constraint.namespace_ = Element.IsKindOf("Namespace") ? Element : Element.namespace_;
            constraint.name = p >= 0 ? aConstraint[..p].Trim() : "";
            constraint.body = aConstraint[(p + 1)..].Trim();
        }
    }

    /// <summary>The constraints as text, and removes them.</summary>
    [Delphi(PortStatus.Ported)]
    public static string ConstraintsToString(TBoldUMLObject Element)
    {
        string result = ConstraintsAsString(Element);
        foreach (TBoldUMLObject constraint in Enumerable.Reverse(Element.constraint))
        {
            constraint.Delete();
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static string ConstraintsAsString(TBoldUMLObject Element) =>
        string.Concat(Element.constraint.Select(c =>
            (c.name != "" ? c.name + CONSTRAINT_NAME_TERMINATOR : "") + c.body + CONSTRAINT_EXPRESSION_TERMINATOR));

    /// <summary>An attribute named "/name" becomes a derived attribute "name".</summary>
    [Delphi(PortStatus.Ported)]
    public static void LeadingSlashToDerived(TBoldUMLObject Element)
    {
        if (Element.name.StartsWith('/') && Element.IsKindOf("Attribute"))
        {
            Element.name = Element.name[1..];
            Element.derived = true;
        }
    }
}
