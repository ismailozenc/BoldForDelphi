// Delphi: Source/UMLModel/Editor/BoldUMLModelEditForm.pas
// Port map: CSharp/port/units/BoldUMLModelEditForm.md
using System.ComponentModel;
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelEditFormPagesTests
{
    private Container fComponents = null!;
    private TBoldModel fModel = null!;
    private TBoldModelEditController fEditor = null!;

    [SetUp]
    public void SetUp()
    {
        fComponents = new Container();
        fModel = EditorSamples.ModelComponent(fComponents);
        fEditor = new TBoldModelEditController { ModelHandle = fModel };
    }

    [TearDown]
    public void TearDown()
    {
        fEditor.Dispose();
        fComponents.Dispose();
    }

    private TBoldUMLObject Model => fEditor.CurrentModel!;

    private TBoldUMLObject Class(string Name) => Model.Classes.Single(c => c.name == Name);

    /// <summary>An element of each page's kind.</summary>
    private TBoldUMLObject SampleOf(TBoldModelEditPage Page)
    {
        TBoldUMLObject person = Class("Person");
        TBoldUMLObject end = Model.Associations.First(a => a.connection.Count == 2).connection[0];
        switch (Page)
        {
            case TBoldModelEditPage.Model:
                return Model;
            case TBoldModelEditPage.Class:
                return person;
            case TBoldModelEditPage.Attribute:
                return person.feature.First(f => f.IsKindOf("Attribute"));
            case TBoldModelEditPage.Qualifier:
                fEditor.CurrentElement = end;
                return fEditor.NewQualifier();
            case TBoldModelEditPage.Operation:
                fEditor.CurrentElement = person;
                TBoldUMLObject operation = fEditor.NewOperation();
                fEditor.CurrentElement = operation;
                fEditor.NewParameter();
                return operation;
            case TBoldModelEditPage.Parameter:
                fEditor.CurrentElement = person;
                fEditor.CurrentElement = fEditor.NewOperation();
                return fEditor.NewParameter();
            case TBoldModelEditPage.Association:
                return end.association!;
            case TBoldModelEditPage.AssociationEnd:
                return end;
            case TBoldModelEditPage.Package:
                return EditorSamples.NewElement(Model, "Package", "Sales");
            case TBoldModelEditPage.DataType:
                return Model.AllOwnedElements.First(e => e.IsKindOf("DataType"));
            default:
                throw new ArgumentOutOfRangeException(nameof(Page));
        }
    }

    [Test]
    public void Every_field_reads_and_writes_its_element()
    {
        Assert.That(TBoldModelEditPages.All.Select(p => p.Page), Is.EquivalentTo(Enum.GetValues<TBoldModelEditPage>().Where(p => p != TBoldModelEditPage.None)));
        foreach (TBoldModelEditPageInfo page in TBoldModelEditPages.All)
        {
            TBoldUMLObject element = SampleOf(page.Page);
            Assert.That(TBoldModelEditController.PageFor(element), Is.EqualTo(page.Page));
            Assert.That(page.Info(element), Is.Not.Empty, page.Name);
            Assert.That(page.Fields.Select(f => f.Name), Is.Unique, page.Name);
            foreach (TBoldModelEditField field in page.Fields)
            {
                string where = page.Name + "." + field.Name;
                if (field.Kind == TBoldModelEditFieldKind.CheckBox)
                {
                    bool? state = field.GetChecked(element);
                    if (field.SetChecked is not null && field.MayModify(element) && field.Name != "bcbRemoveSuperOnUnboldify" && field.Name != "bcbRemoveOnUnboldify")
                    {
                        field.SetChecked(element, !(state ?? false));
                        Assert.That(field.GetChecked(element), Is.EqualTo(!(state ?? false)), where);
                        field.SetChecked(element, state ?? false);
                    }

                    continue;
                }

                string value = field.Get(element);
                IReadOnlyList<string> choices = field.Choices(fEditor, element);
                if (field.Kind == TBoldModelEditFieldKind.Choice)
                {
                    Assert.That(choices, Is.Not.Empty, where);
                    if (field.Set is not null && value != "" && field.Name != "cmbAttributePMapperName" && field.Name != "cmbClassPMapperName")
                    {
                        Assert.That(choices, Does.Contain(value), where);
                    }
                }

                if (field.Set is not null)
                {
                    string newValue = field.Kind == TBoldModelEditFieldKind.Text || field.Kind == TBoldModelEditFieldKind.EditableChoice
                        ? (field.Name.Contains("Multiplicity") ? "1..*" : "Changed")
                        : choices.Last(c => c != value || choices.Count == 1);
                    field.Set(element, newValue);
                    Assert.That(field.Get(element), Is.EqualTo(newValue), where);
                    field.Set(element, value);
                }
            }

            foreach (TBoldModelEditGrid grid in page.Grids)
            {
                foreach (TBoldUMLObject row in grid.Rows(element))
                {
                    foreach (TBoldModelEditField column in grid.Columns)
                    {
                        _ = column.Kind == TBoldModelEditFieldKind.CheckBox ? column.GetChecked(row)?.ToString() : column.Get(row);
                    }
                }
            }
        }
    }

    [Test]
    public void Advanced_fields_show_in_the_advanced_mode_and_some_fields_as_the_element_says()
    {
        TBoldModelEditPageInfo attributePage = TBoldModelEditPages.For(TBoldModelEditPage.Attribute);
        TBoldUMLObject attribute = Class("Person").feature.First(f => f.IsKindOf("Attribute"));
        TBoldModelEditField delphiName = attributePage.Field("tbxAttributeDelphiName"), derivation = attributePage.Field("tbxAttributeDerivationOCL");
        Assert.That(delphiName.Shows(fEditor, attribute), Is.False);
        fEditor.IsAdvancedMode = true;
        Assert.That(delphiName.Shows(fEditor, attribute), Is.True);
        attribute.derived = false;
        Assert.That(derivation.Shows(fEditor, attribute), Is.False);
        attribute.derived = true;
        Assert.That(derivation.Shows(fEditor, attribute), Is.True);
        Assert.That(TBoldModelEditPages.For(TBoldModelEditPage.DataType).Field("BoldEdit6").Shows(fEditor, attribute), Is.False, "never shown");
        Assert.That(TBoldModelEditPages.For(TBoldModelEditPage.DataType).Field("BoldEdit5").ReadOnly, Is.True);

        TBoldModelEditField reverse = attributePage.Field("bcbAttributeReverseDerive"), delayed = attributePage.Field("bcbAttributeDelayedFetch");
        Assert.That(reverse.IsEnabled(attribute), Is.True);
        attribute.derived = false;
        attribute.SetBoldTV(TAG_REVERSEDERIVE, "False");
        Assert.That(reverse.IsEnabled(attribute), Is.False);
        attribute.persistent = false;
        attribute.SetBoldTV(TAG_DELAYEDFETCH, "False");
        Assert.That(delayed.IsEnabled(attribute), Is.False);
        attribute.persistent = true;
        Assert.That(delayed.IsEnabled(attribute), Is.True);

        TBoldUMLObject end = Model.Associations.First().connection[0];
        TBoldModelEditField endDerivation = TBoldModelEditPages.For(TBoldModelEditPage.AssociationEnd).Field("tbxAssoEndDerivationOCL");
        end.association!.derived = false;
        Assert.That(endDerivation.Shows(fEditor, end), Is.False);
        end.association.derived = true;
        Assert.That(endDerivation.Shows(fEditor, end), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldModelEditPages), nameof(TBoldModelEditPages.bcrBooleanToCheckBoxGetAsCheckBoxState), nameof(TBoldModelEditPages.bcrBooleanToCheckBoxSetAsCheckBoxState),
        nameof(TBoldModelEditPages.bcrBooleanToCheckBoxMayModify), nameof(TBoldModelEditPages.bcrGetSetGetAsCheckBoxState), nameof(TBoldModelEditPages.bcrGetSetSetAsCheckBoxState),
        nameof(TBoldModelEditPages.bcrGetSetMayModify), nameof(TBoldModelEditPages.ShowAttribKindFeaturesRecieve), nameof(TBoldModelEditPages.bsrNiceCRRendererGetAsString))]
    public void A_tagged_value_shows_as_a_check_box()
    {
        TBoldUMLObject attribute = Class("Person").feature.First(f => f.IsKindOf("Attribute"));
        const string tag = "Bold." + TAG_DELPHIFIELD;
        attribute.DeleteTaggedValue(tag);
        Assert.That(TBoldModelEditPages.bcrBooleanToCheckBoxGetAsCheckBoxState(attribute, tag), Is.Null, "greyed without the tagged value");
        Assert.That(TBoldModelEditPages.bcrBooleanToCheckBoxMayModify(attribute, tag), Is.False);
        TBoldModelEditPages.bcrBooleanToCheckBoxSetAsCheckBoxState(attribute, tag, true);
        Assert.That(attribute.GetTaggedValue(tag), Is.EqualTo("True"));
        Assert.That(TBoldModelEditPages.bcrBooleanToCheckBoxGetAsCheckBoxState(attribute, tag), Is.True);
        Assert.That(TBoldModelEditPages.bcrBooleanToCheckBoxMayModify(attribute, tag), Is.True);
        TBoldModelEditPages.bcrGetSetSetAsCheckBoxState(attribute, tag, false);
        Assert.That(TBoldModelEditPages.bcrGetSetGetAsCheckBoxState(attribute, tag), Is.False);
        Assert.That(TBoldModelEditPages.bcrGetSetGetAsCheckBoxState(null, tag), Is.Null);

        attribute.SetBoldTV(TAG_ATTRIBUTEKIND, TV_ATTRIBUTEKIND_BOLD);
        Assert.That(TBoldModelEditPages.bcrGetSetMayModify(attribute) || TBoldModelEditPages.ShowAttribKindFeaturesRecieve(attribute), Is.False);
        attribute.SetBoldTV(TAG_ATTRIBUTEKIND, TV_ATTRIBUTEKIND_DELPHI);
        Assert.That(TBoldModelEditPages.bcrGetSetMayModify(attribute) && TBoldModelEditPages.ShowAttribKindFeaturesRecieve(attribute), Is.True);
        Assert.That(TBoldModelEditPages.ShowAttribKindFeaturesRecieve(Class("Person")), Is.False);
        Assert.That(TBoldModelEditPages.bsrNiceCRRendererGetAsString("BoldDefs,\r\nBoldUtils"), Is.EqualTo("BoldDefs,BoldUtils"));
    }

    [Test]
    [Covers(typeof(TBoldModelEditPages), nameof(TBoldModelEditPages.bcrAutoCreatedGetAsCheckBoxState), nameof(TBoldModelEditPages.bcrAutoCreatedSetAsCheckBoxState),
        nameof(TBoldModelEditPages.bsrRedOnAutocreatedSetColor), nameof(TBoldModelEditPages.pcModelEditChange), nameof(TBoldModelEditPages.cmbSuperClassSelectChanged))]
    public void Boldify_s_elements_are_marked_and_the_superclass_chosen()
    {
        TBoldUMLObject root = TBoldUMLBoldify.GetRootClass(Model)!;
        TBoldUMLObject person = Class("Person");
        Assert.That(TBoldModelEditPages.bcrAutoCreatedGetAsCheckBoxState(person.generalization.First()), Is.EqualTo(TBoldUMLBoldify.IsAutoCreated(person.generalization.First())));
        Assert.That(TBoldModelEditPages.bcrAutoCreatedGetAsCheckBoxState(null) || TBoldModelEditPages.bsrRedOnAutocreatedSetColor(null), Is.False);
        TBoldUMLBoldify.UnBoldify(Model);
        new TBoldUMLBoldify().Boldify(Model);
        root = TBoldUMLBoldify.GetRootClass(Model)!;
        TBoldUMLObject autoCreated = Model.Classes.Concat(Model.AllOwnedElements.Where(e => e.IsKindOf("Generalization"))).FirstOrDefault(TBoldUMLBoldify.IsAutoCreated)
            ?? root;
        if (TBoldUMLBoldify.IsAutoCreated(autoCreated))
        {
            Assert.That(TBoldModelEditPages.bcrAutoCreatedGetAsCheckBoxState(autoCreated) && TBoldModelEditPages.bsrRedOnAutocreatedSetColor(autoCreated), Is.True);
            TBoldModelEditPages.bcrAutoCreatedSetAsCheckBoxState(autoCreated, true);
            Assert.That(TBoldUMLBoldify.IsAutoCreated(autoCreated), Is.True, "checking does nothing");
            TBoldModelEditPages.bcrAutoCreatedSetAsCheckBoxState(autoCreated, false);
            Assert.That(TBoldUMLBoldify.IsAutoCreated(autoCreated), Is.False, "unchecked, it stays when unboldifying");
        }

        Assert.That(TBoldModelEditPages.pcModelEditChange(person), Is.True);
        Assert.That(TBoldModelEditPages.pcModelEditChange(root), Is.False);
        TBoldModelEditPages.cmbSuperClassSelectChanged(person, "Building");
        Assert.That(person.Superclass, Is.SameAs(Class("Building")));
        TBoldModelEditField superclass = TBoldModelEditPages.For(TBoldModelEditPage.Class).Field("cmbSuperClass");
        Assert.That(superclass.Choices(fEditor, person), Does.Not.Contain("Person").And.Not.Contain(""), "a boldified model's class has a superclass");
        Assert.That(superclass.Shows(fEditor, root), Is.False, "the root class has none");
        fEditor.Boldifymodel();
        Assert.That(superclass.Choices(fEditor, person)[0], Is.EqualTo(""));
        TBoldModelEditPages.cmbSuperClassSelectChanged(person, "");
        Assert.That(person.Superclass, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldModelEditPages), nameof(TBoldModelEditPages.bdhAttributePMapperNamesDeriveAndSubscribe))]
    public void Choices_come_from_the_mappers_and_the_model()
    {
        TBoldUMLObject attribute = Class("Person").feature.Single(f => f.name == "FirstName");
        IReadOnlyList<string> names = TBoldModelEditPages.bdhAttributePMapperNamesDeriveAndSubscribe(fEditor, attribute);
        Assert.That(names[0], Is.EqualTo(BoldDefs.DEFAULTNAME));
        Assert.That(names, Does.Contain(nameof(TBoldPMString)));
        Assert.That(TBoldModelEditPages.bdhAttributePMapperNamesDeriveAndSubscribe(fEditor, null), Is.Empty);
        Assert.That(TBoldModelEditPages.ObjectPersistenceMapperNames(), Does.Contain(BoldDefs.DEFAULTNAME));

        TBoldUMLObject end = Model.Associations.First().connection[0];
        end.multiplicity = "1..*";
        Assert.That(TBoldModelEditPages.IsMulti(end) && TBoldModelEditPages.IsMandatory(end), Is.True);
        end.multiplicity = "0..1";
        Assert.That(TBoldModelEditPages.IsMulti(end) || TBoldModelEditPages.IsMandatory(end), Is.False);
        end.AddRef("qualifier", Model.Graph.Create("Attribute"));
        Assert.That(TBoldModelEditPages.IsMulti(end), Is.True, "qualified");

        TBoldModelEditPageInfo classPage = TBoldModelEditPages.For(TBoldModelEditPage.Class);
        Assert.That(classPage.Grids.Select(g => g.Caption), Is.EqualTo(new[] { "Attrib&utes", "Operati&ons", "Association ends" }));
        Assert.That(classPage.Grids[0].Rows(Class("Person")).Select(r => r.name), Does.Contain("FirstName"));
        TBoldModelEditGrid associations = TBoldModelEditPages.For(TBoldModelEditPage.Model).Grids[1];
        Assert.That(associations.Rows(Model).Count(), Is.EqualTo(Model.ownedElement.Count(e => e.IsKindOf("Association"))));
        Assert.That(associations.Columns.Select(c => c.Caption), Is.EqualTo(new[] { "Name", "Class", "Connection 1", "Connection 2" }));
        TBoldUMLObject lonely = EditorSamples.NewElement(Model, "Association", "Lonely");
        Assert.That(associations.Columns[2].Get(lonely), Is.EqualTo("<None>"));
    }
}
