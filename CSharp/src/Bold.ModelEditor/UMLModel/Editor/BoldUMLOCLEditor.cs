// Delphi: Source/UMLModel/Editor/BoldUMLOCLEditor.pas
// Port map: CSharp/port/units/BoldUMLOCLEditor.md

using System.Windows.Forms;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLOCLEditor", Symbol = "finalization", Note = "The editor and its type info handle go with the process.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLOCLEditor", Symbol = "initialization", Note = "The section is empty.")]

namespace Bold;

/// <summary>Edits an OCL expression of the model in the OCL editor, in the context of a class or an attribute's type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLOclEditor : IDisposable
{
    private static TBoldUMLOclEditor? G_UMLOCLEditor;
    private readonly TBoldSystemTypeInfoHandle fSystemTypeInfohandle;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLOclEditor()
    {
        fSystemTypeInfohandle = new TBoldSystemTypeInfoHandle { UseGeneratedCode = false };
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLOclEditor.Destroy")]
    public void Dispose()
    {
        fSystemTypeInfohandle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>The OCL type of the context: a class's own, or the type an attribute's type name maps to.</summary>
    public TBoldElementTypeInfo? ContextType(TBoldModel BoldModel, TBoldUMLObject Context)
    {
        fSystemTypeInfohandle.BoldModel = BoldModel;
        TBoldSystemTypeInfo systemTypeInfo = fSystemTypeInfohandle.StaticSystemTypeInfo!;
        if (Context.IsKindOf("Class"))
        {
            string expressionName = BoldModel.MoldModel.Classes.ItemsByName(Context.name)?.ExpandedExpressionName ?? Context.name;
            return systemTypeInfo.ElementTypeInfoByExpressionName(expressionName);
        }

        if (Context.IsKindOf("Attribute"))
        {
            TBoldTypeNameMapping? Mapping = BoldModel.TypeNameDictionary.MappingForModelName(Context.typeName);
            return Mapping is null ? null : systemTypeInfo.ElementTypeInfoByExpressionName(Mapping.ExpressionName);
        }

        throw new EBold($"Unsupported context type for OCL-expression: {Context.MetaClass}");
    }

    /// <summary>The expression as the user edited it, or the old one when the user cancelled.</summary>
    [Delphi(PortStatus.Ported)]
    public string EditOcl(TBoldModel BoldModel, TBoldUMLObject Context, string OldExpression)
    {
        using var EditForm = new TBoldOclPropEditForm();
        EditForm.Context = ContextType(BoldModel, Context);
        EditForm.OclExpr = OldExpression;
        return EditForm.ShowDialog() == DialogResult.OK ? EditForm.OclExpr : OldExpression;
    }

    [Delphi(PortStatus.Ported)]
    public void DiscardRuntimeModel()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "BoldUMLOclEditor_")]
    public static TBoldUMLOclEditor BoldUMLOclEditor_() => G_UMLOCLEditor ??= new TBoldUMLOclEditor();
}
