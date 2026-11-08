// What the generator knows of a marked class: Delphi's wizard kept the same in TCustomAttribute, TPropertyInfo and
// TClassInfo (BoldWACustomAttr, BoldWAClassInfo), filled from its forms; here it is read from the class's attributes.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bold.SourceGenerators
{
    /// <summary>The kind of attribute type the generator writes, by Delphi's wizard's templates.</summary>
    public enum BoldAttributeKind
    {
        /// <summary>A value set (the wizard's ValueSetTemplate).</summary>
        ValueSet,

        /// <summary>A new attribute with fields, its content interface, free-standing value and mapper (AttributeTemplate, InterfaceTemplate, MapperTemplate).</summary>
        NewAttribute,

        /// <summary>A subclass of another attribute type (SubClassedAttrTemplate).</summary>
        SubclassedAttribute,
    }

    /// <summary>How a field is stored by the generated persistence mapper.</summary>
    public enum BoldFieldStorage
    {
        None,
        Integer,
        String,
        Float,
        Currency,
        Boolean,
        DateTime,
    }

    /// <summary>A field of a new attribute type (Delphi's TPropertyInfo).</summary>
    public sealed class BoldFieldSpec
    {
        public BoldFieldSpec(string name, string typeName, BoldFieldStorage storage, bool readable, bool writable)
        {
            Name = name;
            TypeName = typeName;
            Storage = storage;
            Readable = readable;
            Writable = writable;
        }

        public string Name { get; }

        /// <summary>The field's type as C# writes it (int, string, global::System.DateTime).</summary>
        public string TypeName { get; }

        public BoldFieldStorage Storage { get; }

        public bool Readable { get; }

        public bool Writable { get; }

        /// <summary>The field's initial value: empty for strings (Bold's strings are never null), the default otherwise.</summary>
        public string InitialValue => Storage == BoldFieldStorage.String || TypeName == "string" ? "\"\"" : Storage == BoldFieldStorage.None ? "default!" : "default";
    }

    /// <summary>A value of a value set: its name and string representations.</summary>
    public sealed class BoldValueSpec
    {
        public BoldValueSpec(string name, string[] representations)
        {
            Name = name;
            Representations = representations;
        }

        public string Name { get; }

        public string[] Representations { get; }
    }

    /// <summary>A marked class, as the generator writes it (Delphi's TCustomAttribute).</summary>
    public sealed class BoldAttributeSpec
    {
        public BoldAttributeKind Kind { get; set; }

        /// <summary>The class's namespace, or empty in the global namespace.</summary>
        public string Namespace { get; set; } = "";

        public string ClassName { get; set; } = "";

        /// <summary>public or internal, as the class is declared.</summary>
        public string Accessibility { get; set; } = "public";

        public string ExpressionName { get; set; } = "";

        public string ModelName { get; set; } = "";

        public string SuperClassName { get; set; } = "";

        public string ValuePrefix { get; set; } = "";

        public List<BoldValueSpec> Values { get; } = new List<BoldValueSpec>();

        public string ContentName { get; set; } = "";

        public string MapperName { get; set; } = "";

        public bool GenerateMapper { get; set; }

        public List<BoldFieldSpec> Fields { get; } = new List<BoldFieldSpec>();

        /// <summary>The name of the generated file.</summary>
        public string HintName => (Namespace.Length == 0 ? "" : Namespace + ".") + ClassName + ".BoldAttribute.g.cs";

        /// <summary>The content interface of a new attribute.</summary>
        public string InterfaceName => "IBold" + ExpressionName + "Content";

        /// <summary>The free-standing value of a new attribute.</summary>
        public string FreeStandingName => "TBFS" + ExpressionName;

        /// <summary>The proxy that gives a new attribute as its content interface.</summary>
        public string ProxyName => ClassName + "_Proxy";

        /// <summary>The constant with the content name of a new attribute.</summary>
        public string ContentNameConstant => "ContentName_" + ExpressionName;

        /// <summary>The enumeration of a value set.</summary>
        public string EnumName => "T" + ExpressionName + "Enum";
    }

    /// <summary>Reads the specification of a marked class, reporting what keeps it from being generated.</summary>
    public static class BoldAttributeSpecs
    {
        private const string ValueSetValueAttributeName = "Bold.BoldValueSetValueAttribute";
        private const string AttributeFieldAttributeName = "Bold.BoldAttributeFieldAttribute";

        /// <summary>A value set: [BoldValueSet] with [BoldValueSetValue] on a partial subclass of TBAValueSet.</summary>
        public static BoldAttributeSpec? ReadValueSet(INamedTypeSymbol type, AttributeData attribute, Compilation compilation, List<Diagnostic> diagnostics)
        {
            BoldAttributeSpec? spec = ReadCommon(type, attribute, diagnostics);
            if (spec is null)
            {
                return null;
            }

            spec.Kind = BoldAttributeKind.ValueSet;
            spec.ValuePrefix = NamedString(attribute, "ValuePrefix") ?? "";
            bool ok = true;
            if (!DerivesFrom(type, "Bold.TBAValueSet"))
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotAValueSet, Location(type), type.Name));
                ok = false;
            }

            var names = new HashSet<string>();
            foreach (AttributeData value in type.GetAttributes())
            {
                if (value.AttributeClass?.ToDisplayString() != ValueSetValueAttributeName || value.ConstructorArguments.Length < 1)
                {
                    continue;
                }

                string name = value.ConstructorArguments[0].Value as string ?? "";
                string[] representations = value.ConstructorArguments.Length > 1 && value.ConstructorArguments[1].Kind == TypedConstantKind.Array
                    ? value.ConstructorArguments[1].Values.Select(v => v.Value as string ?? "").ToArray()
                    : new string[0];
                if (representations.Length == 0)
                {
                    representations = new[] { name };
                }

                if (!SyntaxFacts.IsValidIdentifier(spec.ValuePrefix + name))
                {
                    diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotAnIdentifier, Location(type), type.Name, spec.ValuePrefix + name));
                    ok = false;
                }
                else if (!names.Add(name))
                {
                    diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.Duplicate, Location(type), type.Name, name));
                    ok = false;
                }

                spec.Values.Add(new BoldValueSpec(name, representations));
            }

            // Every value has as many representations as the one with the most (Delphi's wizard had a column a
            // representation): a missing one is the value's name.
            int representationCount = spec.Values.Count == 0 ? 0 : spec.Values.Max(v => v.Representations.Length);
            for (int i = 0; i < spec.Values.Count; i++)
            {
                BoldValueSpec value = spec.Values[i];
                if (value.Representations.Length < representationCount)
                {
                    spec.Values[i] = new BoldValueSpec(value.Name, value.Representations.Concat(Enumerable.Repeat(value.Name, representationCount - value.Representations.Length)).ToArray());
                }
            }

            if (spec.Values.Count == 0)
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NoValues, Location(type), type.Name));
                ok = false;
            }

            spec.ContentName = "Integer";
            spec.MapperName = "TBoldPMInteger";
            return ok ? spec : null;
        }

        /// <summary>An attribute type: [BoldAttributeType] on a partial subclass of TBoldAttribute (with fields) or of another attribute type.</summary>
        public static BoldAttributeSpec? ReadAttributeType(INamedTypeSymbol type, AttributeData attribute, Compilation compilation, List<Diagnostic> diagnostics)
        {
            BoldAttributeSpec? spec = ReadCommon(type, attribute, diagnostics);
            if (spec is null)
            {
                return null;
            }

            if (!DerivesFrom(type, "Bold.TBoldAttribute"))
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotAnAttribute, Location(type), type.Name));
                return null;
            }

            bool ok = true;
            var names = new HashSet<string>();
            foreach (AttributeData field in type.GetAttributes())
            {
                if (field.AttributeClass?.ToDisplayString() != AttributeFieldAttributeName || field.ConstructorArguments.Length < 2)
                {
                    continue;
                }

                string name = field.ConstructorArguments[0].Value as string ?? "";
                var fieldType = field.ConstructorArguments[1].Value as ITypeSymbol;
                bool readable = NamedBool(field, "Readable") ?? true;
                bool writable = NamedBool(field, "Writable") ?? true;
                if (!SyntaxFacts.IsValidIdentifier(name))
                {
                    diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotAnIdentifier, Location(type), type.Name, name));
                    ok = false;
                    continue;
                }

                if (!names.Add(name))
                {
                    diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.Duplicate, Location(type), type.Name, name));
                    ok = false;
                    continue;
                }

                string typeName = fieldType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
                spec.Fields.Add(new BoldFieldSpec(name, typeName, StorageOf(fieldType), readable, writable));
            }

            spec.GenerateMapper = NamedBool(attribute, "GenerateMapper") ?? false;
            if (spec.Fields.Count > 0)
            {
                spec.Kind = BoldAttributeKind.NewAttribute;
                spec.ContentName = NamedString(attribute, "ContentName") ?? spec.ExpressionName;
                spec.MapperName = NamedString(attribute, "MapperName") ?? "TBoldPM" + spec.ExpressionName;
                if (spec.GenerateMapper)
                {
                    if (compilation.GetTypeByMetadataName("Bold.TBoldMemberDefaultMapper") is null)
                    {
                        diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NoPersistence, Location(type), type.Name));
                        ok = false;
                    }

                    foreach (BoldFieldSpec field in spec.Fields.Where(f => f.Storage == BoldFieldStorage.None))
                    {
                        diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.UnstorableField, Location(type), type.Name, field.Name, field.TypeName));
                        ok = false;
                    }
                }
            }
            else if (type.BaseType?.ToDisplayString() == "Bold.TBoldAttribute")
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NoFields, Location(type), type.Name));
                ok = false;
            }
            else
            {
                spec.Kind = BoldAttributeKind.SubclassedAttribute;
                spec.ContentName = NamedString(attribute, "ContentName") ?? "";
                spec.MapperName = NamedString(attribute, "MapperName") ?? "";
            }

            return ok ? spec : null;
        }

        /// <summary>The names and the checks every marked class has: partial and top-level.</summary>
        private static BoldAttributeSpec? ReadCommon(INamedTypeSymbol type, AttributeData attribute, List<Diagnostic> diagnostics)
        {
            bool ok = true;
            if (type.ContainingType is not null)
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.Nested, Location(type), type.Name));
                ok = false;
            }

            bool partial = type.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
            if (!partial)
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotPartial, Location(type), type.Name));
                ok = false;
            }

            string expressionName = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string ?? "" : "";
            if (!SyntaxFacts.IsValidIdentifier(expressionName))
            {
                diagnostics.Add(Diagnostic.Create(BoldAttributeDiagnostics.NotAnIdentifier, Location(type), type.Name, expressionName));
                ok = false;
            }

            if (!ok)
            {
                return null;
            }

            return new BoldAttributeSpec
            {
                Namespace = type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : "",
                ClassName = type.Name,
                Accessibility = type.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public ? "public" : "internal",
                ExpressionName = expressionName,
                ModelName = NamedString(attribute, "ModelName") ?? expressionName,
                SuperClassName = type.BaseType?.Name ?? "",
            };
        }

        private static BoldFieldStorage StorageOf(ITypeSymbol? type)
        {
            switch (type?.SpecialType)
            {
                case SpecialType.System_Int32:
                    return BoldFieldStorage.Integer;
                case SpecialType.System_String:
                    return BoldFieldStorage.String;
                case SpecialType.System_Double:
                    return BoldFieldStorage.Float;
                case SpecialType.System_Decimal:
                    return BoldFieldStorage.Currency;
                case SpecialType.System_Boolean:
                    return BoldFieldStorage.Boolean;
                case SpecialType.System_DateTime:
                    return BoldFieldStorage.DateTime;
                default:
                    return BoldFieldStorage.None;
            }
        }

        private static bool DerivesFrom(INamedTypeSymbol type, string baseName)
        {
            for (INamedTypeSymbol? t = type.BaseType; t is not null; t = t.BaseType)
            {
                if (t.ToDisplayString() == baseName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string? NamedString(AttributeData attribute, string name)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name)
                {
                    return argument.Value.Value as string;
                }
            }

            return null;
        }

        private static bool? NamedBool(AttributeData attribute, string name)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is bool value)
                {
                    return value;
                }
            }

            return null;
        }

        private static Location Location(INamedTypeSymbol type) => type.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;
    }
}
