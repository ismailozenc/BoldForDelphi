namespace Bold.Tests;

/// <summary>
/// Fills a system with objects of every concrete persistent class, sets every stored attribute and links every
/// stored role; and compares two systems object by object, matching objects by their ids. Round trip tests use it to
/// check that a save and a reload keep everything.
/// </summary>
internal static class SystemFiller
{
    /// <summary>Attribute types that are not set: multi-language values, languages and constraints.</summary>
    private static bool Skipped(TBoldMember m) => m is TBAMLString or TBAMLValueSet or TBALanguage or TBAConstraint;

    private static bool IsStoredAttribute(TBoldMemberRTInfo m) => m.IsAttribute && m.Persistent && !m.IsDerived;

    private static bool IsStoredRole(TBoldMemberRTInfo m) =>
        m is TBoldRoleRTInfo r && r.Persistent && !r.IsDerived && r.IsNavigable && r.RoleType is TBoldRoleType.rtRole;

    /// <summary>
    /// Creates <paramref name="perClass"/> objects of each concrete persistent class that is not a link class, sets
    /// their attributes from the object's number and links each role to objects of the other end. Returns the number
    /// of attributes set.
    /// </summary>
    public static int Fill(TBoldSystem system, int perClass = 2)
    {
        TBoldSystemTypeInfo types = system.BoldSystemTypeInfo;
        var objects = new List<TBoldObject>();
        for (int c = 0; c < types.TopSortedClasses.Count; c++)
        {
            TBoldClassTypeInfo info = types.TopSortedClasses[c];
            if (!info.IsAbstract && info.Persistent && !info.IsLinkClass)
            {
                for (int n = 0; n < perClass; n++)
                {
                    objects.Add(system.CreateNewObjectByExpressionName(info.ExpressionName));
                }
            }
        }

        int set = 0;
        for (int k = 0; k < objects.Count; k++)
        {
            TBoldObject o = objects[k];
            for (int i = 0; i < o.BoldMemberCount; i++)
            {
                TBoldMemberRTInfo info = o.BoldClassTypeInfo.AllMembers[i];
                if (IsStoredAttribute(info) && !Skipped(o.BoldMembers(i)))
                {
                    SetValue((TBoldAttribute)o.BoldMembers(i), (TBoldAttributeRTInfo)info, k + 1);
                    set++;
                }
            }
        }

        for (int k = 0; k < objects.Count; k++)
        {
            TBoldObject o = objects[k];
            for (int i = 0; i < o.BoldMemberCount; i++)
            {
                if (IsStoredRole(o.BoldClassTypeInfo.AllMembers[i]))
                {
                    var role = (TBoldRoleRTInfo)o.BoldClassTypeInfo.AllMembers[i];
                    List<TBoldObject> targets = objects.Where(t => t.BoldClassTypeInfo.BoldIsA(role.ClassTypeInfoOfOtherEnd)).ToList();
                    if (targets.Count == 0)
                    {
                        continue;
                    }

                    if (o.BoldMembers(i) is TBoldObjectReference reference)
                    {
                        reference.BoldObject = targets[k % targets.Count];
                    }
                    else
                    {
                        var list = (TBoldObjectList)o.BoldMembers(i);
                        TBoldObject target = targets[k % targets.Count];
                        if (!list.Includes(target))
                        {
                            list.Add(target);
                        }
                    }
                }
            }
        }

        return set;
    }

    /// <summary>A value of the attribute's type made from <paramref name="n"/>, within its length.</summary>
    private static void SetValue(TBoldAttribute a, TBoldAttributeRTInfo info, int n)
    {
        switch (a)
        {
            case TBAString s:
                string text = n + info.ExpressionName;
                s.AsString = info.Length > 0 && text.Length > info.Length ? text[..info.Length] : text;
                break;
            case TBAInteger i:
                i.AsInteger = n;
                break;
            case TBAFloat f:
                f.AsFloat = n + 0.25;
                break;
            case TBACurrency c:
                c.AsCurrency = n + 0.5m;
                break;
            case TBADateTime dt:
                dt.AsDateTime = new DateTime(2001, 2, 3, 4, 5, 6).AddDays(n);
                break;
            case TBADate d:
                d.AsDate = new DateTime(2001, 2, 3).AddDays(n);
                break;
            case TBATime t:
                t.AsTime = new DateTime(1899, 12, 30, n % 24, 30, 0);
                break;
            case TBABoolean b:
                b.AsBoolean = n % 2 == 1;
                break;
            case TBAValueSet v:
                v.AsInteger = v.Values[n % v.Values.Count].AsInteger;
                break;
            case TBABlob blob:
                blob.AsString = "blob " + n;
                break;
            default:
                Assert.Fail($"{info.ClassTypeInfo.ExpressionName}.{info.ExpressionName}: no test value for {a.GetType().Name}");
                break;
        }
    }

    private static string Id(TBoldObject o) => o.BoldObjectLocator!.BoldObjectID.AsString;

    /// <summary>The ids of the objects a role points to, in list order for ordered roles and sorted otherwise.</summary>
    private static List<string> Targets(TBoldMember m, bool ordered)
    {
        var ids = new List<string>();
        if (m is TBoldObjectReference reference)
        {
            if (reference.BoldObject is { } target)
            {
                ids.Add(Id(target));
            }
        }
        else
        {
            var list = (TBoldObjectList)m;
            for (int i = 0; i < list.Count; i++)
            {
                ids.Add(Id(list[i]!));
            }

            if (!ordered)
            {
                ids.Sort(StringComparer.Ordinal);
            }
        }

        return ids;
    }

    /// <summary>
    /// Asserts that <paramref name="actual"/> holds the same objects as <paramref name="expected"/>, with the same
    /// stored attribute values and the same links, leaving out the members <paramref name="ignore"/> picks. Returns
    /// the number of members compared.
    /// </summary>
    public static int AssertSame(TBoldSystem expected, TBoldSystem actual, Func<TBoldMemberRTInfo, bool>? ignore = null)
    {
        TBoldSystemTypeInfo types = expected.BoldSystemTypeInfo;
        int compared = 0;
        var problems = new List<string>();
        for (int c = 0; c < types.TopSortedClasses.Count; c++)
        {
            TBoldClassTypeInfo info = types.TopSortedClasses[c];
            if (info.IsAbstract || !info.Persistent)
            {
                continue;
            }

            var mine = new Dictionary<string, TBoldObject>();
            TBoldObjectList actualList = actual.Classes(c)!;
            for (int i = 0; i < actualList.Count; i++)
            {
                if (actualList[i]!.BoldClassTypeInfo == actual.BoldSystemTypeInfo.TopSortedClasses[c])
                {
                    mine[Id(actualList[i]!)] = actualList[i]!;
                }
            }

            TBoldObjectList expectedList = expected.Classes(c)!;
            int count = 0;
            for (int i = 0; i < expectedList.Count; i++)
            {
                TBoldObject e = expectedList[i]!;
                if (e.BoldClassTypeInfo != info)
                {
                    continue;
                }

                count++;
                if (!mine.TryGetValue(Id(e), out TBoldObject? a))
                {
                    problems.Add($"{info.ExpressionName} {Id(e)} is missing");
                    continue;
                }

                for (int m = 0; m < e.BoldMemberCount; m++)
                {
                    TBoldMemberRTInfo member = info.AllMembers[m];
                    string where = $"{info.ExpressionName} {Id(e)}.{member.ExpressionName}";
                    if (ignore?.Invoke(member) == true)
                    {
                        continue;
                    }

                    if (IsStoredAttribute(member) && !Skipped(e.BoldMembers(m)))
                    {
                        string want = e.BoldMembers(m).AsString, got = a.BoldMembers(m).AsString;
                        if (want != got)
                        {
                            problems.Add($"{where}: expected '{want}', got '{got}'");
                        }

                        compared++;
                    }
                    else if (member is TBoldRoleRTInfo role && role.Persistent && !role.IsDerived && role.IsNavigable &&
                             role.RoleType is TBoldRoleType.rtRole or TBoldRoleType.rtLinkRole)
                    {
                        List<string> want = Targets(e.BoldMembers(m), role.IsOrdered), got = Targets(a.BoldMembers(m), role.IsOrdered);
                        if (!want.SequenceEqual(got))
                        {
                            problems.Add($"{where}: expected [{string.Join(", ", want)}], got [{string.Join(", ", got)}]");
                        }

                        compared++;
                    }
                }
            }

            if (count != mine.Count)
            {
                problems.Add($"{info.ExpressionName}: expected {count} objects, got {mine.Count}");
            }
        }

        Assert.That(problems, Is.Empty);
        return compared;
    }
}
