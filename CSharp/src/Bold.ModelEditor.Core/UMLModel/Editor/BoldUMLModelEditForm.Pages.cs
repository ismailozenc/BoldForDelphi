// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
// The pages of the model editor, as data: for each kind of element its fields (Delphi's Bold-aware edits, check boxes
// and combo boxes on the form, with the expressions they showed) and the lists on its tabs (Delphi's grids). The form
// builds its pages from these; the renderers of Delphi's form are the functions here.

using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm.bsrNiceCRRendererSubscribe", Note = "The UML graph tells the controller of every change; the form shows the page again.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm.bcrBooleanToCheckBoxSubscribe", Note = "The UML graph tells the controller of every change; the form shows the page again.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLModelEditForm", Symbol = "TBoldModelEditFrm.bcrGetSetSubscribe", Note = "The UML graph tells the controller of every change; the form shows the page again.")]

namespace Bold;

/// <summary>How a field shows its value.</summary>
public enum TBoldModelEditFieldKind { Text, CheckBox, Choice, EditableChoice }

/// <summary>A field of a page: one of Delphi's Bold-aware edits, check boxes and combo boxes, on the element on show.</summary>
public sealed class TBoldModelEditField(string Name, string Caption, TBoldModelEditFieldKind Kind)
{
    /// <summary>The name of Delphi's control.</summary>
    public string Name { get; } = Name;

    /// <summary>The caption of its label, or of the check box, with its hot key.</summary>
    public string Caption { get; } = Caption;

    public TBoldModelEditFieldKind Kind { get; } = Kind;

    public string Hint { get; init; } = "";

    /// <summary>Shown in the advanced mode only.</summary>
    public bool Advanced { get; init; }

    /// <summary>Never shown, as in Delphi's form.</summary>
    public bool Hidden { get; init; }

    /// <summary>The "..." button beside the field, by the name of Delphi's button.</summary>
    public string? Button { get; init; }

    /// <summary>The group box the field is in, if any.</summary>
    public string? Group { get; init; }

    public Func<TBoldUMLObject, string> Get { get; init; } = _ => "";

    /// <summary>Sets the value; none for a field that is read only.</summary>
    public Action<TBoldUMLObject, string>? Set { get; init; }

    /// <summary>The state of a check box: null for greyed.</summary>
    public Func<TBoldUMLObject, bool?> GetChecked { get; init; } = _ => null;

    public Action<TBoldUMLObject, bool>? SetChecked { get; init; }

    /// <summary>Whether the user may change it now (a check box whose tagged value is not there may not).</summary>
    public Func<TBoldUMLObject, bool> MayModify { get; init; } = _ => true;

    /// <summary>The values to choose from.</summary>
    public Func<TBoldModelEditController, TBoldUMLObject, IReadOnlyList<string>> Choices { get; init; } = (_, _) => [];

    /// <summary>Whether it shows, besides the advanced mode.</summary>
    public Func<TBoldModelEditController, TBoldUMLObject, bool> IsVisible { get; init; } = (_, _) => true;

    public Func<TBoldUMLObject, bool> IsEnabled { get; init; } = _ => true;

    public bool ReadOnly => Kind == TBoldModelEditFieldKind.CheckBox ? SetChecked is null : Set is null;

    /// <summary>Whether it shows for the element, in the mode the controller is in.</summary>
    public bool Shows(TBoldModelEditController Controller, TBoldUMLObject Element) =>
        !Hidden && (!Advanced || Controller.IsAdvancedMode) && IsVisible(Controller, Element);

    public override string ToString() => Name;
}

/// <summary>A list on a page's tabs (Delphi's grid): its rows and columns.</summary>
public sealed class TBoldModelEditGrid(string Name, string Caption, Func<TBoldUMLObject, IEnumerable<TBoldUMLObject>> Rows, IReadOnlyList<TBoldModelEditField> Columns)
{
    public string Name { get; } = Name;

    public string Caption { get; } = Caption;

    public Func<TBoldUMLObject, IEnumerable<TBoldUMLObject>> Rows { get; } = Rows;

    public IReadOnlyList<TBoldModelEditField> Columns { get; } = Columns;
}

/// <summary>A page of the editor: its fields and its lists.</summary>
public sealed class TBoldModelEditPageInfo(TBoldModelEditPage Page, string Name, string Caption, string InfoCaption, Func<TBoldUMLObject, string> Info,
    IReadOnlyList<TBoldModelEditField> Fields, IReadOnlyList<TBoldModelEditGrid> Grids)
{
    public TBoldModelEditPage Page { get; } = Page;

    /// <summary>The name of Delphi's tab sheet.</summary>
    public string Name { get; } = Name;

    public string Caption { get; } = Caption;

    /// <summary>The caption of the line that says what is on show ("Class:").</summary>
    public string InfoCaption { get; } = InfoCaption;

    /// <summary>What is on show (Delphi's info label).</summary>
    public Func<TBoldUMLObject, string> Info { get; } = Info;

    public IReadOnlyList<TBoldModelEditField> Fields { get; } = Fields;

    public IReadOnlyList<TBoldModelEditGrid> Grids { get; } = Grids;

    public TBoldModelEditField Field(string Name) => Fields.Single(f => f.Name == Name);
}

/// <summary>The pages of the model editor and the renderers of their fields.</summary>
public static class TBoldModelEditPages
{
    private static readonly Dictionary<string, string[]> Values = TBoldModelEditController.PopulateComboBoxes();

    private static readonly string[] VisibilityKinds = ["private", "protected", "public"];
    private static readonly string[] AggregationKinds = ["none", "aggregate", "composite"];
    private static readonly string[] ChangeableKinds = ["changeable", "frozen", "addOnly"];
    private static readonly string[] ScopeKinds = ["instance", "classifier"];
    private static readonly string[] ParameterDirectionKinds = ["in", "out", "inout", "return"];

    private static List<TBoldModelEditPageInfo>? fAll;

    /// <summary>Every page, in the order of Delphi's tab sheets.</summary>
    public static IReadOnlyList<TBoldModelEditPageInfo> All => fAll ??= MakePages();

    public static TBoldModelEditPageInfo For(TBoldModelEditPage Page) => All.Single(p => p.Page == Page);

    /// <summary>A text without its line breaks, as an edit of one line shows it.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bsrNiceCRRendererGetAsString", Note = "Takes the text the expression gave.")]
    public static string bsrNiceCRRendererGetAsString(string Text) => Text.Replace(BoldDefs.BOLDCRLF, "", StringComparison.Ordinal);

    /// <summary>A tagged value as a check box: checked when true, greyed when the element does not have it.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrBooleanToCheckBoxGetAsCheckBoxState", Note = "Takes the tag where Delphi took the expression of its value.")]
    public static bool? bcrBooleanToCheckBoxGetAsCheckBoxState(TBoldUMLObject? Element, string Tag)
    {
        if (Element?.FindTaggedValue(Tag) is not { } taggedValue)
        {
            return null;
        }

        return TVIsTrue(taggedValue["value"]);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrBooleanToCheckBoxSetAsCheckBoxState", Note = "Takes the tag where Delphi took the expression of its value.")]
    public static void bcrBooleanToCheckBoxSetAsCheckBoxState(TBoldUMLObject Element, string Tag, bool NewValue) =>
        Element.EnsuredTaggedValue(Tag)["value"] = NewValue ? "True" : "False";

    /// <summary>A tagged value's check box may be changed when the element has the tagged value.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrBooleanToCheckBoxMayModify", Note = "Takes the tag where Delphi took the expression of its value.")]
    public static bool bcrBooleanToCheckBoxMayModify(TBoldUMLObject Element, string Tag) => Element.FindTaggedValue(Tag) is not null;

    /// <summary>As bcrBooleanToCheckBox, greyed without an element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrGetSetGetAsCheckBoxState")]
    public static bool? bcrGetSetGetAsCheckBoxState(TBoldUMLObject? Element, string Tag) =>
        Element is null ? null : bcrBooleanToCheckBoxGetAsCheckBoxState(Element, Tag);

    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrGetSetSetAsCheckBoxState")]
    public static void bcrGetSetSetAsCheckBoxState(TBoldUMLObject Element, string Tag, bool NewValue) =>
        bcrBooleanToCheckBoxSetAsCheckBoxState(Element, Tag, NewValue);

    /// <summary>A Delphi feature of an attribute may be changed when the attribute is of the Delphi kind.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrGetSetMayModify")]
    public static bool bcrGetSetMayModify(TBoldUMLObject Element) => Element.GetBoldTV(TAG_ATTRIBUTEKIND) == TV_ATTRIBUTEKIND_DELPHI;

    /// <summary>Checked when the element was made by boldifying.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrAutoCreatedGetAsCheckBoxState")]
    public static bool bcrAutoCreatedGetAsCheckBoxState(TBoldUMLObject? Element) => Element is not null && TBoldUMLBoldify.IsAutoCreated(Element);

    /// <summary>Unchecking keeps the element when unboldifying.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bcrAutoCreatedSetAsCheckBoxState")]
    public static void bcrAutoCreatedSetAsCheckBoxState(TBoldUMLObject? Element, bool NewValue)
    {
        if (Element is not null && !NewValue)
        {
            TBoldUMLBoldify.RemoveBoldifyTaggedValue(Element, TAG_AUTOCREATED);
        }
    }

    /// <summary>Whether an element made by boldifying shows in red.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bsrRedOnAutocreatedSetColor", Note = "Says whether to show it red; the form colours it.")]
    public static bool bsrRedOnAutocreatedSetColor(TBoldUMLObject? Element) => Element is not null && TBoldUMLBoldify.IsAutoCreated(Element);

    /// <summary>The Delphi features of an attribute show when it is of the Delphi kind.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.ShowAttribKindFeaturesRecieve", Note = "Says whether the group shows; the form shows the page again when the model changes.")]
    public static bool ShowAttribKindFeaturesRecieve(TBoldUMLObject? Attribute) =>
        Attribute is not null && Attribute.IsKindOf("Attribute") && Attribute.GetBoldTV(TAG_ATTRIBUTEKIND) == TV_ATTRIBUTEKIND_DELPHI;

    /// <summary>The persistence mappers that can store the attribute's type, after the default.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.bdhAttributePMapperNamesDeriveAndSubscribe", Note = "Gives the names; there is nothing to subscribe to.")]
    public static IReadOnlyList<string> bdhAttributePMapperNamesDeriveAndSubscribe(TBoldModelEditController Controller, TBoldUMLObject? Attribute)
    {
        if (Attribute is null)
        {
            return [];
        }

        string strStreamName = "";
        if (Controller.CurrentTypeNameDictionary?.MappingForModelName(Attribute.typeName) is { } Mapping)
        {
            strStreamName = Mapping.ExpandedContentsName;
        }

        var result = new List<string> { BoldDefs.DEFAULTNAME };
        TBoldMemberPersistenceMapperDescriptorList mappers = BoldPMapperLists.BoldMemberPersistenceMappers();
        for (int Index = 0; Index < mappers.Count; Index++)
        {
            if (mappers.Descriptors(Index).CanStore(strStreamName))
            {
                result.Add(mappers.Descriptors(Index).Name);
            }
        }

        return result;
    }

    /// <summary>The superclass is chosen when the class has one.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.pcModelEditChange", Note = "Says whether the superclass may be chosen; the form enables the combo box.")]
    public static bool pcModelEditChange(TBoldUMLObject Class) => Class.Superclass is not null;

    /// <summary>Choosing a superclass makes it the class's first parent.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldModelEditFrm.cmbSuperClassSelectChanged", Note = "Takes the chosen class by name.")]
    public static void cmbSuperClassSelectChanged(TBoldUMLObject Classifier, string SuperclassName) =>
        Classifier.SetFirstParent(ClassNamed(Classifier, SuperclassName));

    private static TBoldUMLObject? ClassNamed(TBoldUMLObject Element, string Name) =>
        Name == "" ? null : Element.model?.Classes.FirstOrDefault(c => c.name == Name);

    private static TBoldUMLObject? DataTypeNamed(TBoldUMLObject Element, string Name) =>
        Element.model is { } model ? model.AllOwnedElements.FirstOrDefault(e => e.IsKindOf("DataType") && e.name == Name) : null;

    private static IEnumerable<TBoldUMLObject> OrderByName(IEnumerable<TBoldUMLObject> Elements) => Elements.OrderBy(e => e.name, StringComparer.CurrentCulture);

    // the fields

    private static TBoldModelEditField Text(string Name, string Caption, Func<TBoldUMLObject, string> Get, Action<TBoldUMLObject, string>? Set, string Hint = "",
        bool Advanced = false, string? Button = null, Func<TBoldModelEditController, TBoldUMLObject, bool>? IsVisible = null, bool Hidden = false) =>
        new(Name, Caption, TBoldModelEditFieldKind.Text)
        {
            Get = Get,
            Set = Set,
            Hint = Hint,
            Advanced = Advanced,
            Button = Button,
            IsVisible = IsVisible ?? ((_, _) => true),
            Hidden = Hidden,
        };

    private static TBoldModelEditField TV(string Name, string Caption, string Tag, string Hint = "", bool Advanced = false, string? Button = null,
        Func<TBoldModelEditController, TBoldUMLObject, bool>? IsVisible = null, bool Hidden = false) =>
        Text(Name, Caption, e => e.GetBoldTV(Tag), (e, v) => e.SetBoldTV(Tag, v), Hint, Advanced, Button, IsVisible, Hidden);

    private static TBoldModelEditField NameField(string Name, string Hint = "", bool ReadOnly = false) =>
        Text(Name, "&Name", e => e.name, ReadOnly ? null : (e, v) => e.name = v, Hint);

    private static TBoldModelEditField Stereotype(string Name, string Caption = "&Stereotype", string Hint = "", bool Hidden = false) =>
        Text(Name, Caption, e => e.stereotypeName, (e, v) => e.stereotypeName = v, Hint, Hidden: Hidden);

    /// <summary>The number of constraints, read only, with the button of the constraint editor.</summary>
    private static TBoldModelEditField Constraints(string Name, string Caption, string Button, string Hint = "") =>
        Text(Name, Caption, e => bsrNiceCRRendererGetAsString(e.constraint.Count.ToString()), null, Hint, Advanced: true, Button: Button);

    private static TBoldModelEditField Check(string Name, string Caption, Func<TBoldUMLObject, bool?> Get, Action<TBoldUMLObject, bool>? Set, string Hint = "",
        Func<TBoldUMLObject, bool>? IsEnabled = null, string? Group = null) =>
        new(Name, Caption, TBoldModelEditFieldKind.CheckBox)
        {
            GetChecked = Get,
            SetChecked = Set,
            Hint = Hint,
            IsEnabled = IsEnabled ?? (_ => true),
            Group = Group,
        };

    /// <summary>A tagged value as a check box (Delphi's bcrBooleanToCheckBox renderer).</summary>
    private static TBoldModelEditField TVCheck(string Name, string Caption, string Tag, string Hint = "", Func<TBoldUMLObject, bool>? IsEnabled = null, string? Group = null)
    {
        string tag = BoldDefs.BOLDTVPREFIX + Tag;
        return new(Name, Caption, TBoldModelEditFieldKind.CheckBox)
        {
            GetChecked = e => bcrBooleanToCheckBoxGetAsCheckBoxState(e, tag),
            SetChecked = (e, v) => bcrBooleanToCheckBoxSetAsCheckBoxState(e, tag, v),
            MayModify = e => bcrBooleanToCheckBoxMayModify(e, tag),
            Hint = Hint,
            IsEnabled = IsEnabled ?? (_ => true),
            Group = Group,
        };
    }

    private static TBoldModelEditField Choice(string Name, string Caption, Func<TBoldUMLObject, string> Get, Action<TBoldUMLObject, string>? Set,
        Func<TBoldModelEditController, TBoldUMLObject, IReadOnlyList<string>> Choices, string Hint = "", bool Advanced = false, bool Editable = false,
        Func<TBoldModelEditController, TBoldUMLObject, bool>? IsVisible = null, string? Group = null) =>
        new(Name, Caption, Editable ? TBoldModelEditFieldKind.EditableChoice : TBoldModelEditFieldKind.Choice)
        {
            Get = Get,
            Set = Set,
            Choices = Choices,
            Hint = Hint,
            Advanced = Advanced,
            IsVisible = IsVisible ?? ((_, _) => true),
            Group = Group,
        };

    private static TBoldModelEditField TVChoice(string Name, string Caption, string Tag, string ValuesOf, string Hint = "", bool Advanced = false, string? Group = null) =>
        Choice(Name, Caption, e => e.GetBoldTV(Tag), (e, v) => e.SetBoldTV(Tag, v), (_, _) => Values[ValuesOf], Hint, Advanced, Group: Group);

    private static TBoldModelEditField EnumChoice(string Name, string Caption, string[] Names, Func<TBoldUMLObject, int> Get, Action<TBoldUMLObject, int> Set, string Hint = "") =>
        Choice(Name, Caption, e => Names[Get(e)], (e, v) => Set(e, Array.IndexOf(Names, v) is var i and >= 0 ? i : Get(e)), (_, _) => Names, Hint);

    private static TBoldModelEditField DataTypeChoice(string Name, string Caption, string Hint) =>
        Choice(Name, Caption, e => e.type_?.name ?? "", (e, v) => e.type_ = DataTypeNamed(e, v) ?? e.type_,
            (_, e) => [.. OrderByName(e.model?.AllOwnedElements.Where(t => t.IsKindOf("DataType")) ?? []).Select(t => t.name)], Hint);

    private static IReadOnlyList<string> AllClassNames(TBoldUMLObject Element, bool WithNone) =>
        [.. (WithNone ? [""] : Array.Empty<string>()), .. OrderByName(Element.model?.Classes ?? []).Select(c => c.name)];

    private static string Persistence(TBoldUMLObject Element) => Element.persistent ? "True" : "False";

    // the columns of the lists

    private static TBoldModelEditField Column(string Caption, Func<TBoldUMLObject, string> Get, Action<TBoldUMLObject, string>? Set = null) =>
        Text(Caption, Caption, Get, Set);

    private static TBoldModelEditField TVColumn(string Caption, string Tag) => Column(Caption, e => e.GetBoldTV(Tag), (e, v) => e.SetBoldTV(Tag, v));

    private static TBoldModelEditField CheckColumn(string Caption, Func<TBoldUMLObject, bool> Get, Action<TBoldUMLObject, bool>? Set = null) =>
        Check(Caption, Caption, e => Get(e), Set);

    private static TBoldModelEditField TVCheckColumn(string Caption, string Tag) => TVCheck(Caption, Caption, Tag);

    private static TBoldModelEditGrid ClassGrid(string Name, string Caption) =>
        new(Name, Caption, owner => OrderByName(owner.ownedElement.Where(e => e.IsKindOf("Class"))),
        [
            Column("Name", e => e.name, (e, v) => e.name = v),
            Column("Superclass", e => e.Superclass?.name ?? ""),
            TVColumn("File name", TAG_INCFILENAME),
            TVColumn("Table mapping", TAG_TABLEMAPPING),
            TVColumn("Delphi name", TAG_DELPHINAME),
            TVColumn("Expression name", TAG_EXPRESSIONNAME),
            TVColumn("PMapper name", TAG_PMAPPERNAME),
            TVColumn("Table name", TAG_TABLENAME),
            CheckColumn("Persistent", e => e.persistent, (e, v) => e.persistent = v),
            CheckColumn("Abstract", e => e.isAbstract, (e, v) => e.isAbstract = v),
            TVCheckColumn("Imported", TAG_IMPORTED),
        ]);

    private static string ConnectionName(TBoldUMLObject Association, int Index) =>
        Association.connection.Count > Index ? Association.connection[Index].name : "<None>";

    private static TBoldModelEditGrid AssociationGrid(string Name, string Caption) =>
        new(Name, Caption, owner => owner.ownedElement.Where(e => e.IsKindOf("Association")),
        [
            Column("Name", e => e.name, (e, v) => e.name = v),
            Column("Class", e => e.class_?.name ?? ""),
            Column("Connection 1", e => ConnectionName(e, 0)),
            Column("Connection 2", e => ConnectionName(e, 1)),
        ]);

    private static TBoldModelEditGrid PackageGrid(string Name, string Caption) =>
        new(Name, Caption, owner => OrderByName(owner.ownedElement.Where(e => e.IsKindOf("Package"))),
        [
            Column("Name", e => e.name, (e, v) => e.name = v),
        ]);

    private static List<TBoldModelEditPageInfo> MakePages()
    {
        var model = new TBoldModelEditPageInfo(TBoldModelEditPage.Model, "tabModel", "Model", "Model:", e => e.name,
        [
            NameField("tbxModelName", "The name of the model."),
            TV("tbxModelDelhpiName", "&Unit name", TAG_UNITNAME, "Filename used to save model."),
            TV("tbxModelExpressionName", "E&xpression name", TAG_EXPRESSIONNAME, "The expression name of the model. Can mostly be left unchanged."),
            TV("tbxModelPMapperName", "&PMapper name", TAG_PMAPPERNAME, Advanced: true),
            Text("tbxModelInterfaceUses", "&Interface uses", e => bsrNiceCRRendererGetAsString(e.GetBoldTV(TAG_INTERFACEUSES)), null,
                "The units required by the interface section.", Button: "btInterfaceUses"),
            Text("tbxModelImplementationUses", "Imp&lementation uses", e => bsrNiceCRRendererGetAsString(e.GetBoldTV(TAG_IMPLEMENTATIONUSES)), null,
                "The units required by the implementation section.", Button: "btImplementationUses"),
            TV("tbxModelRootClass", "Model &root class", TAG_ROOTCLASS, "The root class of the model."),
            Stereotype("tbxModelStereotype", "Stereot&ype", "Stereotype applied to model."),
            Constraints("tbxModelConstraints", "C&onstraints", "btModelConstraintEditor"),
            TV("edModelGUID", "GUI&D", TAG_GUID, "GUID used when generating IDL code.", Advanced: true),
            TV("edModelTypeLibVersion", "Type li&b version", TAG_TYPELIBVERSION, "Version number for the type lib.", Advanced: true),
            TVChoice("cmbModelOptimisticLocking", "Optimistic Loc&king", TAG_OPTIMISTICLOCKING, "bvhOptimisticLocking", Advanced: true),
            TVCheck("bcbModelUseXFiles", "U&se X files", TAG_USEXFILES),
            TVCheck("bcbModelUseTimestamp", "Use timestamp", TAG_USETIMESTAMP),
            TVCheck("bcbUseGlobalId", "Use global id", TAG_USEGLOBALID, "If checked, a GUID column will be added to the XFiles table."),
            TVCheck("bcbUseReadOnly", "Use readonly", TAG_USEREADONLY),
            TVCheck("bcbUseClockLog", "Use clock lo&g", TAG_USECLOCKLOG),
            TVCheck("bcbGenerateMultiplicityConstraints", "Use multiplicity constraints", TAG_GENERATEMULTIPLICITYCONSTRAINTS),
        ],
        [ClassGrid("BoldGrid1", "&Classes"), AssociationGrid("BoldGrid2", "&Associations"), PackageGrid("TabSheet9", "Packages")]);

        var cls = new TBoldModelEditPageInfo(TBoldModelEditPage.Class, "tabClass", "Class", "Class:", e => e.name,
        [
            NameField("tbxClassName", "Modeled name of the class."),
            TV("tbxClassDelphiName", "Delph&i name", TAG_DELPHINAME, "Template for the name used to access this class in source code.", Advanced: true),
            TV("tbxClassExpressionName", "E&xpression name", TAG_EXPRESSIONNAME, "Template for the name used to access the class from OCL.", Advanced: true),
            Choice("cmbClassPMapperName", "&PMapper name", e => e.GetBoldTV(TAG_PMAPPERNAME), (e, v) => e.SetBoldTV(TAG_PMAPPERNAME, v),
                (_, _) => ObjectPersistenceMapperNames(), Advanced: true),
            TV("tbxClassTableName", "T&able name", TAG_TABLENAME, "Template for the name used for this class' table name.", Advanced: true),
            TV("tbxClassFileName", "Include file name", TAG_INCFILENAME),
            TVChoice("cmbTableMapping", "Table mappin&g", TAG_TABLEMAPPING, "bvhTableMapping", "Controls into which table the class persistent data goes."),
            Choice("cmbSuperClass", "&Superclass", e => e.Superclass?.name ?? "", cmbSuperClassSelectChanged,
                (c, e) => [.. (c.CurrentModelIsBoldified ? Array.Empty<string>() : [""]), .. OrderByName((e.model?.Classes ?? []).Where(x => x != e)).Select(x => x.name)],
                "The immediate ancestor of this class.",
                IsVisible: (_, e) => !(e.model is { } m && TBoldUMLBoldify.IsBoldified(m) && e.generalization.Count == 0)),
            Stereotype("tbxClassStereotype", "Stereot&ype", "Stereotype applied on class, if any."),
            Constraints("tbxClassConstraint", "&Constraints", "btClassConstraintEditor", "Number of constraints specified for this class."),
            TV("tbxClassDerivationExpressions", "Derivation expressions", TAG_DERIVATIONEXPRESSIONS, "Redefinition of inherited derivation expressions.",
                Advanced: true, Button: "btShowDerivationExpressionsEditor"),
            TV("tbxClassUnitName", "Unit name", TAG_UNITNAME),
            TV("edClassDefaultStringRep", "Default string rep", TAG_DEFAULTSTRINGREPRESENTATION, Button: "btClassDefaultStringRep"),
            TV("edClassGUID", "GUID", TAG_GUID, "The GUID for this class (required for COM access)", Advanced: true),
            TVChoice("cmbClassOptimisticLocking", "Optimistic &Locking", TAG_OPTIMISTICLOCKING, "bvhOptimisticLocking",
                "Specify the optimistic locking mechanism to use for this class.", Advanced: true),
            Check("bcbClassPersistent", "Pe&rsistent", e => e.persistent, (e, v) => e.persistent = v, "Controls of this class is to be saved in persistent storage."),
            Check("bcbClassAbstract", "A&bstract", e => e.isAbstract, (e, v) => e.isAbstract = v, "Controls if this class can be instantiated."),
            TVCheck("bcbClassImported", "Importe&d", TAG_IMPORTED),
            Check("bcbRemoveSuperOnUnboldify", "Remove Superclass on unboldify", e => bcrAutoCreatedGetAsCheckBoxState(e.generalization.FirstOrDefault()),
                (e, v) => bcrAutoCreatedSetAsCheckBoxState(e.generalization.FirstOrDefault(), v)),
            Check("bcbRemoveOnUnboldify", "Remove class on unboldify", e => bcrAutoCreatedGetAsCheckBoxState(e), bcrAutoCreatedSetAsCheckBoxState),
            Check("bcbIsRootClass", "Is Root Class", e => e.model is { } m && TBoldUMLBoldify.IsBoldified(m) && e.generalization.Count == 0, null),
        ],
        [
            new("BoldGrid3", "Attrib&utes", c => c.feature.Where(f => f.IsKindOf("Attribute")),
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Type", e => e.typeName, (e, v) => e.typeName = v),
                Column("Stereotype", e => e.stereotypeName, (e, v) => e.stereotypeName = v),
                TVColumn("Length", TAG_LENGTH),
                TVColumn("Delphi name", TAG_DELPHINAME),
                TVColumn("Expression name", TAG_EXPRESSIONNAME),
                TVColumn("PMapper name", TAG_PMAPPERNAME),
                TVColumn("Column name", TAG_COLUMNNAME),
                CheckColumn("Persistent", e => e.persistent, (e, v) => e.persistent = v),
                TVCheckColumn("Allow null", TAG_ALLOWNULL),
                CheckColumn("Derived", e => e.derived, (e, v) => e.derived = v),
                TVCheckColumn("Delayed fetch", TAG_DELAYEDFETCH),
                TVColumn("Get method", "GetMethod"),
                TVColumn("Set method", "SetMethod"),
                TVColumn("Local variable", "LocalVariable"),
            ]),
            new("BoldGrid4", "Operati&ons", c => c.feature.Where(f => f.MetaClass == "Operation"),
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Owner scope", e => ScopeKinds[(int)e.ownerScope]),
                TVColumn("Delphi function type", TAG_DELPHIOPERATIONKIND),
                TVColumn("Delphi name", TAG_DELPHINAME),
                TVColumn("Expression name", TAG_EXPRESSIONNAME),
            ]),
            new("BoldGrid9", "Association ends", c => c.associationEnd.Select(e => e.otherEnd).OfType<TBoldUMLObject>(),
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Stereotype", e => e.stereotypeName, (e, v) => e.stereotypeName = v),
                CheckColumn("Navigable", e => e.isNavigable, (e, v) => e.isNavigable = v),
                CheckColumn("Multi", IsMulti),
                CheckColumn("Mandatory", IsMandatory),
                CheckColumn("Ordered", e => e.isOrdered, (e, v) => e.isOrdered = v),
                Column("Aggregation", e => AggregationKinds[(int)e.aggregation]),
                Column("Multiplicity", e => e.multiplicity, (e, v) => e.multiplicity = v),
                Column("Changeability", e => ChangeableKinds[(int)e.changeability]),
                Column("Visibility", e => VisibilityKinds[(int)e.visibility]),
            ]),
        ]);

        var attribute = new TBoldModelEditPageInfo(TBoldModelEditPage.Attribute, "tabAttribute", "Attribute", "Attribute:", e => (e.owner?.name ?? "") + "." + e.name,
        [
            NameField("tbxAttributeName", "The modeled name for this attribute."),
            TV("tbxAttributeDelphiName", "Delph&i name", TAG_DELPHINAME, Advanced: true),
            TV("tbxAttributeExpressionName", "E&xpression name", TAG_EXPRESSIONNAME, "Template for the name used to access this attribute in OCL.", Advanced: true),
            Choice("cmbAttributePMapperName", "&PMapper name", e => e.GetBoldTV(TAG_PMAPPERNAME), (e, v) => e.SetBoldTV(TAG_PMAPPERNAME, v),
                bdhAttributePMapperNamesDeriveAndSubscribe, "Persistence mapper to be used for this attribute.", Advanced: true),
            TV("tbxAttributeColumnName", "Col&umn name", TAG_COLUMNNAME, Advanced: true),
            DataTypeChoice("cmbAttributeBoldType", "Type", "The type of the attribute."),
            TV("tbxAttributeLength", "Len&gth", TAG_LENGTH),
            Stereotype("tbxAttributeStereotype", "&Stereotype", "Stereotype applied to this attribute."),
            TVChoice("cmbTVAttributeKind", "Attribute &kind", TAG_ATTRIBUTEKIND, "bvhAttributeKind", Advanced: true),
            TV("tbxAttributeDerivationOCL", "Derivation OC&L", TAG_DERIVATIONOCL, "The OCL expression used to derive this attribute.",
                Button: "btAttributeShowDerivExprEditor", IsVisible: (_, e) => e.derived),
            EnumChoice("cmbAttributeVisibility", "Visi&bility", VisibilityKinds, e => (int)e.visibility, (e, v) => e.visibility = (TVisibilityKind)v),
            Text("tbxAttributeInitialValue", "Initial value", e => e.initialValue, (e, v) => e.initialValue = v, Advanced: true),
            Constraints("tbxAttributeConstraint", "&Constraints", "btAttributeConstraintEditor", "The number of constraints for this attribute."),
            Check("bcbAttributePersistent", "Persistent", e => e.persistent, (e, v) => e.persistent = v),
            TVCheck("bcbAttributeDelayedFetch", "Dela&yed fetch", TAG_DELAYEDFETCH,
                IsEnabled: e => e.persistent || e.GetBoldTV(TAG_DELAYEDFETCH) == "True"),
            TVCheck("bcbAttributeAllowNull", "&Allow null", TAG_ALLOWNULL, "Controls if the attribute can be NULL."),
            Check("bcbAttributeDerived", "&Derived", e => e.derived, (e, v) => e.derived = v, "Controls if the attribute is derived."),
            TVCheck("bcbAttributeReverseDerive", "&Reverse derive", TAG_REVERSEDERIVE, "Controls if the attribute is reverse derived.",
                IsEnabled: e => e.derived || e.GetBoldTV(TAG_REVERSEDERIVE) == "True"),
            TVCheck("bcbDPHasField", "Delphi field", TAG_DELPHIFIELD, Group: "gbDelphiFeatures"),
            TVChoice("cmbTVDPRead", "Pr&operty read", TAG_DPREAD, "bvhDelphiProperty", "Controls the read mechanism for the attribute.", Group: "gbDelphiFeatures"),
            TVChoice("cmbTVDPWrite", "Property &write", TAG_DPWRITE, "bvhDelphiProperty", "Controls the write mechanism for the attribute.", Group: "gbDelphiFeatures"),
        ],
        []);

        var qualifier = new TBoldModelEditPageInfo(TBoldModelEditPage.Qualifier, "tabQualifier", "Qualifier", "Qualifier:", e => e.qualifiedName,
        [
            NameField("BoldEdit1", "Modeled name of the qualifier."),
            Stereotype("BoldEdit3", "&Stereotype", "Stereotype applied to the qualifier."),
            DataTypeChoice("BoldComboBox1", "&Type", "The type of the qualifier."),
            TV("BoldEdit4", "&Delphi name", TAG_DELPHINAME),
            TV("BoldEdit9", "E&xpression name", TAG_EXPRESSIONNAME, "Template for the name used to access the qualifier in OCL."),
        ],
        []);

        var operation = new TBoldModelEditPageInfo(TBoldModelEditPage.Operation, "tabOperation", "Operation", "Operation:", e => (e.owner?.name ?? "") + "." + e.name,
        [
            NameField("tbxOperationName", "The modeled name of the operation."),
            TV("tbxOperationDelphiName", "Delph&i name", TAG_DELPHINAME, Advanced: true),
            TV("tbxOperationExpressionName", "E&xpression name", TAG_EXPRESSIONNAME, "Template for OCL name (not relevant for operations).", Advanced: true),
            EnumChoice("cmbOwnerScope", "&Owner scope", ScopeKinds, e => (int)e.ownerScope, (e, v) => e.ownerScope = (TScopeKind)v),
            TVChoice("cmbDelphiFunctionType", "O&peration kind", TAG_DELPHIOPERATIONKIND, "bvhDelphiFunctionType", "Controls the directives of the operation."),
            Stereotype("tbxOperationStereotype", "&Stereotype", "The stereotype applied to the operation."),
            EnumChoice("cmbOperationVisibility", "Visi&bility", VisibilityKinds, e => (int)e.visibility, (e, v) => e.visibility = (TVisibilityKind)v),
            Constraints("tbxOperationConstraint", "&Constraints", "btOperationConstraintEditor", "The number of constraints defined for this operation."),
            TVCheck("cbOperationOverrideInAllSubclasses", "Override in all s&ubclasses", TAG_OVERRIDEINALLSUBCLASSES),
        ],
        [
            new("BoldGrid5", "&Parameters", o => o.parameter,
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Kind", e => ParameterDirectionKinds[(int)e.kind]),
                Column("Type", e => e.typeName, (e, v) => e.typeName = v),
                TVColumn("Expression name", TAG_EXPRESSIONNAME),
                TVCheckColumn("Is const", TAG_ISCONST),
            ]),
        ]);

        var association = new TBoldModelEditPageInfo(TBoldModelEditPage.Association, "tabAssociation", "Association", "Association:", e => e.name,
        [
            NameField("tbxAssociationName", "Modeled name of association."),
            Choice("cmbAssociationLinkClass", "C&lass", e => e.class_?.name ?? "", (e, v) => e.class_ = ClassNamed(e, v),
                (_, e) => AllClassNames(e, true), "Specifies the link class for this association."),
            Stereotype("tbxAssociationStereotype", "&Stereotype", "Stereotype applied to acssociation."),
            Constraints("tbxAssociationConstraint", "&Constraints", "btAssociationConstraintEditor", "Number of constraints specified for the association."),
            Check("bcbAssociationDerived", "Der&ived", e => e.derived, (e, v) => e.derived = v, "Controls if the association is derived."),
            Check("bcbPersistent", "&Persistent", e => e.persistent, (e, v) => e.persistent = v),
        ],
        [
            new("BoldGrid6", "&AssociationEnds", a => a.connection,
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Class", e => e.type_?.name ?? ""),
                TVColumn("Delphi name", TAG_DELPHINAME),
                TVColumn("Expression name", TAG_EXPRESSIONNAME),
                TVColumn("Column name", "DBName"),
                CheckColumn("Navigable", e => e.isNavigable, (e, v) => e.isNavigable = v),
                CheckColumn("Multi", IsMulti),
                CheckColumn("Ordered", e => e.isOrdered, (e, v) => e.isOrdered = v),
                CheckColumn("Mandatory", IsMandatory),
                TVCheckColumn("Embed", TAG_EMBED),
            ]),
        ]);

        var associationEnd = new TBoldModelEditPageInfo(TBoldModelEditPage.AssociationEnd, "tabAssociationEnd", "AssociationEnd", "Role:",
            e => (e.otherEnd?.type_?.name ?? "") + "." + e.name + " -> " + (e.type_?.name ?? ""),
        [
            NameField("tbxAssociationEndName", "The modeled name of the role."),
            TV("tbxAssociationEndDelphiName", "Delph&i name", TAG_DELPHINAME, "Template for the name used to access this role in source code.", Advanced: true),
            TV("tbxAssociationEndExpressionName", "E&xpression name", TAG_EXPRESSIONNAME, "Template for the name used to access this role in OCL.", Advanced: true),
            TV("tbxAssociationEndColumnName", "C&olumn name", TAG_COLUMNNAME, "Template for SQL column to store this role.", Advanced: true),
            Choice("cmbAssoEndClass", "C&lass", e => e.type_?.name ?? "", (e, v) => e.type_ = ClassNamed(e, v) ?? e.type_,
                (_, e) => AllClassNames(e, false), "Class where the role ends."),
            Stereotype("tbxAssociationEndStereotype", "&Stereotype", "Stereotype applied to this role."),
            EnumChoice("cmbAggregationKind", "A&ggregation", AggregationKinds, e => (int)e.aggregation, (e, v) => e.aggregation = (TAggregationKind)v,
                "Type of aggregation of the role."),
            EnumChoice("cmbAssoEndVisibility", "Visibility", VisibilityKinds, e => (int)e.visibility, (e, v) => e.visibility = (TVisibilityKind)v),
            EnumChoice("cmbAssoEndChangeability", "Changeability", ChangeableKinds, e => (int)e.changeability, (e, v) => e.changeability = (TChangeableKind)v,
                "Controls changeability of the role (not enforced)."),
            Constraints("tbxAssociationEndConstraint", "&Constraints", "btAssociationEndConstraintEditor", "Number of constraints defined for this role."),
            Choice("cmbMultiplicity", "Multi&plicity", e => e.multiplicity, (e, v) => e.multiplicity = v, (_, _) => Values["bvhMultiplicityValues"], Editable: true),
            TVChoice("cmbAssoEndDeleteAction", "&Delete action", TAG_DELETEACTION, "bvhDeleteActions", "Controls how deletion propagates over the role."),
            TV("tbxAssoEndDerivationOCL", "Derivation OCL", TAG_DERIVATIONOCL, "Derivation OCL expression for this role.", Button: "btAssoEndShowDeriExprEditor",
                IsVisible: (_, e) => e.association?.derived ?? false),
            Check("bcbAssociationEndNavigable", "N&avigable", e => e.isNavigable, (e, v) => e.isNavigable = v, "Controls if the role is navigable in this direction."),
            Check("bcbAssociationEndMulti", "M&ulti", e => IsMulti(e), null, "Indicates if the multiplicity if more than 1."),
            Check("bcbAssociationEndOrdered", "O&rdered", e => e.isOrdered, (e, v) => e.isOrdered = v, "Controls if order is preserved."),
            Check("bcbAssociationEndMandatory", "Mandator&y", e => IsMandatory(e), null, "Indicates if the multiplicity is one or more."),
            TVCheck("bcbAssociationEndEmbed", "Em&bed", TAG_EMBED, "Controls if the role is embedded in the owning object."),
        ],
        [
            new("BoldGrid7", "&Qualifiers", e => e.qualifier,
            [
                Column("Name", e => e.name, (e, v) => e.name = v),
                Column("Type", e => e.typeName, (e, v) => e.typeName = v),
                Column("Stereotype", e => e.stereotypeName, (e, v) => e.stereotypeName = v),
                TVColumn("Length", TAG_LENGTH),
                TVColumn("Delphi name", TAG_DELPHINAME),
                TVColumn("Expression name", TAG_EXPRESSIONNAME),
                TVColumn("PMapper name", "PMapperName"),
                TVColumn("Column name", "DBName"),
            ]),
        ]);

        var parameter = new TBoldModelEditPageInfo(TBoldModelEditPage.Parameter, "tabParameter", "Parameter", "Parameter:", e => e.name,
        [
            NameField("tbxParameterName", "Modeled name of the parameter."),
            Text("tbxParameterType", "T&ype", e => e.typeName, (e, v) => e.typeName = v, "Data type of the parameter."),
            TV("tbxParameterExpressionName", "E&xpression Name", TAG_EXPRESSIONNAME, Advanced: true),
            EnumChoice("cmbParamKind", "&Kind", ParameterDirectionKinds, e => (int)e.kind, (e, v) => e.kind = (TBoldParameterDirectionKind)v),
            Stereotype("tbxParameterStereotype", "&Stereotype", "Stereotype applied to the parameter."),
            Constraints("tbxParameterConstraint", "&Constraints", "btParameterConstraintEditor", "Number of constraints on the parameter."),
            TVCheck("bcbIsConst", "is c&onst", TAG_ISCONST, "Determines if the param should be const declared."),
        ],
        []);

        var package = new TBoldModelEditPageInfo(TBoldModelEditPage.Package, "tabPackage", "Package", "Package:", e => e.name,
        [
            NameField("tbxPackageName"),
            TV("tbxPackageExpressionName", "E&xpression name", TAG_EXPRESSIONNAME),
            Stereotype("tbxStereotypeName"),
        ],
        [ClassGrid("BoldGrid8", "&Classes"), AssociationGrid("BoldGrid10", "A&ssociations"), PackageGrid("TabSheet13", "Packages")]);

        var dataType = new TBoldModelEditPageInfo(TBoldModelEditPage.DataType, "tabDataType", "DataType", "DataType:", e => e.name,
        [
            NameField("BoldEdit5", ReadOnly: true),
            Stereotype("BoldEdit6", Hidden: true),
            TV("BoldEdit7", "&Delphi name", TAG_DELPHINAME, Hidden: true),
            TV("BoldEdit8", "E&xpression name", TAG_EXPRESSIONNAME, Hidden: true),
        ],
        []);

        return [model, cls, attribute, operation, association, associationEnd, parameter, package, dataType, qualifier];
    }

    /// <summary>The names of the object persistence mappers.</summary>
    public static IReadOnlyList<string> ObjectPersistenceMapperNames()
    {
        TBoldObjectPersistenceMapperDescriptorList mappers = BoldPMapperLists.BoldObjectPersistenceMappers();
        return [.. Enumerable.Range(0, mappers.Count).Select(i => mappers.Descriptors(i).Name)];
    }

    /// <summary>Whether an end holds many: its upper bound is over one, or it is qualified.</summary>
    public static bool IsMulti(TBoldUMLObject AssociationEnd) =>
        BoldUtils.GetUpperLimitForMultiplicity(AssociationEnd.multiplicity) > 1 || AssociationEnd.qualifier.Count > 0;

    /// <summary>Whether an end must hold one: its lower bound is over zero.</summary>
    public static bool IsMandatory(TBoldUMLObject AssociationEnd) => BoldUtils.GetLowerLimitForMultiplicity(AssociationEnd.multiplicity) > 0;
}
