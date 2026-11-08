// The Bold attribute generator, which takes the place of Delphi's attribute wizard (Source/ObjectSpace/IDE/AttributeWizard,
// decision DECIDE-3). The wizard asked in its forms what kind of attribute to make and wrote a Delphi unit from one of
// its templates (BoldWAdmTemplates); the generator reads the same answers from the attributes on a partial class
// (BoldValueSetAttribute, BoldAttributeTypeAttribute and their value and field attributes, in Bold.ObjectSpace) and adds
// the rest of the class to the compilation when the project builds.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bold.SourceGenerators
{
    /// <summary>Writes Bold attribute types from the attributes on their partial classes.</summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class BoldAttributeGenerator : IIncrementalGenerator
    {
        /// <summary>The attribute that marks a value set type.</summary>
        public const string ValueSetAttributeName = "Bold.BoldValueSetAttribute";

        /// <summary>The attribute that marks an attribute type (new or subclassed).</summary>
        public const string AttributeTypeAttributeName = "Bold.BoldAttributeTypeAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<GeneratorResult> valueSets = context.SyntaxProvider.ForAttributeWithMetadataName(
                ValueSetAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => Generate(ctx, ct, isValueSet: true));
            IncrementalValuesProvider<GeneratorResult> attributeTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeTypeAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => Generate(ctx, ct, isValueSet: false));
            context.RegisterSourceOutput(valueSets, Emit);
            context.RegisterSourceOutput(attributeTypes, Emit);
        }

        private static GeneratorResult Generate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken, bool isValueSet)
        {
            var result = new GeneratorResult();
            if (context.TargetSymbol is not INamedTypeSymbol type || context.Attributes.Length == 0)
            {
                return result;
            }

            cancellationToken.ThrowIfCancellationRequested();
            BoldAttributeSpec? spec = isValueSet
                ? BoldAttributeSpecs.ReadValueSet(type, context.Attributes[0], context.SemanticModel.Compilation, result.Diagnostics)
                : BoldAttributeSpecs.ReadAttributeType(type, context.Attributes[0], context.SemanticModel.Compilation, result.Diagnostics);
            if (spec is not null)
            {
                result.HintName = spec.HintName;
                result.Source = BoldAttributeCode.Write(spec);
            }

            return result;
        }

        private static void Emit(SourceProductionContext context, GeneratorResult result)
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (result.Source is not null)
            {
                context.AddSource(result.HintName, result.Source);
            }
        }

        /// <summary>What one marked class gives: its source, or the diagnostics that say why there is none.</summary>
        private sealed class GeneratorResult
        {
            public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

            public string HintName { get; set; } = "";

            public string? Source { get; set; }
        }
    }

    /// <summary>The diagnostics of the generator: a marked class it cannot complete.</summary>
    public static class BoldAttributeDiagnostics
    {
        private const string Category = "Bold.SourceGenerators";

        public static readonly DiagnosticDescriptor NotPartial = new DiagnosticDescriptor(
            "BOLDGEN001", "Attribute type is not partial", "{0} must be partial: the Bold attribute generator writes the rest of it", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NotAValueSet = new DiagnosticDescriptor(
            "BOLDGEN002", "Value set does not derive from TBAValueSet", "{0} has [BoldValueSet] and must derive from TBAValueSet", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NoValues = new DiagnosticDescriptor(
            "BOLDGEN003", "Value set has no values", "{0} has no values: give each with [BoldValueSetValue]", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NotAnAttribute = new DiagnosticDescriptor(
            "BOLDGEN004", "Attribute type does not derive from TBoldAttribute", "{0} has [BoldAttributeType] and must derive from TBoldAttribute or another attribute type", Category,
            DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NoFields = new DiagnosticDescriptor(
            "BOLDGEN005", "New attribute type has no fields", "{0} derives from TBoldAttribute and has no fields: give them with [BoldAttributeField], or derive from an attribute type",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor UnstorableField = new DiagnosticDescriptor(
            "BOLDGEN006", "Field cannot be stored", "The field {1} of {0} is a {2}, which the generated persistence mapper cannot store (int, string, double, decimal, bool or DateTime)",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NoPersistence = new DiagnosticDescriptor(
            "BOLDGEN007", "No persistence for the mapper", "{0} asks for a persistence mapper, but the project does not reference Bold.Persistence.Sql", Category,
            DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Nested = new DiagnosticDescriptor(
            "BOLDGEN008", "Attribute type is nested", "{0} is declared in another type: attribute types must be top-level", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NotAnIdentifier = new DiagnosticDescriptor(
            "BOLDGEN009", "Name is not an identifier", "{1}, a name in {0}, must be a C# identifier", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Duplicate = new DiagnosticDescriptor(
            "BOLDGEN010", "Name is given twice", "{0} names {1} twice", Category, DiagnosticSeverity.Error, true);

        /// <summary>All of them, for the tests.</summary>
        public static ImmutableArray<DiagnosticDescriptor> All { get; } = ImmutableArray.Create(
            NotPartial, NotAValueSet, NoValues, NotAnAttribute, NoFields, UnstorableField, NoPersistence, Nested, NotAnIdentifier, Duplicate);
    }
}
