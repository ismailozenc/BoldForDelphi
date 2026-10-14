namespace Bold.Tests;

/// <summary>A text dump of a Mold model with every element, setting and tagged value (defaults included), sorted, for comparisons.</summary>
public static class ModelDump
{
    public static List<string> Of(TMoldModel model)
    {
        var lines = new List<string>();
        void TVs(string path, TMoldElement e)
        {
            var tags = new TStringList();
            var values = new TStringList();
            e.AddAllTaggedValues(tags, values);
            for (int i = 0; i < tags.Count; i++)
            {
                lines.Add($"{path} TV {tags[i]}={values[i]}");
            }

            lines.Add($"{path} stereotype={e.Stereotype} constraints={e.Constraints.CommaText}");
        }

        lines.Add($"model {model.Name} root={model.RootClass.Name}");
        TVs("model", model);
        for (int c = 0; c < model.Classes.Count; c++)
        {
            TMoldClass cls = model.Classes[c];
            string cp = "class " + cls.Name;
            lines.Add($"{cp} super={cls.SuperClass?.Name} persistent={cls.Persistent} abstract={cls.IsAbstract} link={cls.Association?.Name}");
            TVs(cp, cls);
            for (int a = 0; a < cls.Attributes.Count; a++)
            {
                TMoldAttribute attr = cls.Attributes[a];
                string ap = $"{cp}.{attr.Name}";
                lines.Add($"{ap} type={attr.BoldType} derived={attr.Derived} visibility={attr.Visibility} initial={attr.InitialValue}");
                TVs(ap, attr);
            }

            for (int m = 0; m < cls.Methods.Count; m++)
            {
                TMoldMethod method = cls.Methods[m];
                string mp = $"{cp}.{method.Name}()";
                lines.Add($"{mp} signature={method.Signature} returns={method.ReturnType} class={method.IsClassMethod} visibility={method.Visibility}");
                TVs(mp, method);
            }
        }

        for (int i = 0; i < model.Associations.Count; i++)
        {
            TMoldAssociation assoc = model.Associations[i];
            string ap = "association " + assoc.Name;
            lines.Add($"{ap} link={assoc.LinkClass?.Name} derived={assoc.Derived}");
            TVs(ap, assoc);
            for (int r = 0; r < assoc.Roles.Count; r++)
            {
                TMoldRole role = assoc.Roles[r];
                string rp = $"{ap}.{role.Name}";
                lines.Add($"{rp} class={role.MoldClass?.Name} multiplicity={role.Multiplicity} navigable={role.Navigable} ordered={role.Ordered} aggregation={role.Aggregation} changeability={role.Changeability} visibility={role.Visibility}");
                TVs(rp, role);
                for (int q = 0; q < role.Qualifiers.Count; q++)
                {
                    lines.Add($"{rp} qualifier {role.Qualifiers[q].Name}: {role.Qualifiers[q].BoldType}");
                }
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
