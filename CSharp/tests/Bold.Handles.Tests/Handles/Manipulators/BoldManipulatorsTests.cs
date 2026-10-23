// Delphi: Source/Handles/Manipulators/BoldManipulators.pas
// Port map: CSharp/port/units/BoldManipulators.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldManipulatorsTests
{
    [Test]
    [Covers(typeof(TBoldManipulator), "*")]
    [Covers("BoldManipulators", "TBoldIdStringRepresentation", "TBoldStringStringFunction")]
    public void Id_strings_name_objects_and_members_and_set_their_values()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TResidential_Building house = s.NewHouse("Elm St");
        var manipulator = new TBoldManipulator(s.Components) { Name = "Manipulator", BoldSystemHandle = s.SystemHandle };
        Assert.That(manipulator.IdStringRepresentation, Is.EqualTo(TBoldIdStringRepresentation.isrVerbose));

        string annId = manipulator.IdStringForElement(ann);
        Assert.That(annId, Does.StartWith("Person.i"), "an unsaved object has an internal id");
        Assert.That(manipulator.ElementForIdString(annId), Is.SameAs(ann));
        string nameId = manipulator.IdStringForElement(ann.M_FirstName);
        Assert.That(nameId, Is.EqualTo(annId + ":firstName"));
        Assert.That(manipulator.ElementForIdString(nameId), Is.SameAs(ann.M_FirstName));

        string id = "";
        Assert.That(manipulator.GetValueAndId(ann.M_LastName, ref id), Is.EqualTo("Smith"));
        Assert.That(id, Is.EqualTo(annId + ":lastName"));
        manipulator.SetValue(id, "Brown");
        Assert.That(ann.LastName, Is.EqualTo("Brown"));
        manipulator.SetValue(manipulator.IdStringForElement(ann.M_Home), manipulator.IdStringForElement(house));
        Assert.That(ann.Home, Is.SameAs(house), "a reference is set from the object's id string");
        Assert.That(() => manipulator.SetValue(manipulator.IdStringForElement(house.Residents), "x"), Throws.InstanceOf<EBold>());
        Assert.That(() => manipulator.SetValue(annId, "x"), Throws.InstanceOf<EBold>());
        Assert.That(manipulator.GetValueAndId(null, ref id), Is.EqualTo(""));

        manipulator.IdStringRepresentation = TBoldIdStringRepresentation.isrTerse;
        string terse = manipulator.IdStringForElement(ann.M_FirstName);
        Assert.That(terse, Does.Match(@"^i\d+:0$"), "the member's index in the terse form");
        Assert.That(manipulator.ElementForIdString(terse), Is.SameAs(ann.M_FirstName));

        var lines = new TStringList();
        lines.Add(terse + "=Anna");
        manipulator.SetFromList(lines);
        Assert.That(ann.FirstName, Is.EqualTo("Anna"));

        Assert.That(manipulator.DefaultTagForElement(ann.M_FirstName), Is.EqualTo("firstName"));
        Assert.That(manipulator.DefaultTagForElement(ann), Is.EqualTo("Person"));
        Assert.That(() => manipulator.DefaultTagForElement(new TBAString()), Throws.InstanceOf<EBold>());

        string created = manipulator.CreateObject("Person");
        Assert.That(manipulator.ElementForIdString(created), Is.InstanceOf<TPerson>());
        manipulator.DeleteObject(created);
        Assert.That(s.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("1"));

        manipulator.OnEncrypt = text => new string(text.Reverse().ToArray());
        manipulator.OnDecrypt = text => new string(text.Reverse().ToArray());
        Assert.That(manipulator.OnEncrypt, Is.Not.Null);
        Assert.That(manipulator.OnDecrypt, Is.Not.Null);
        string secret = manipulator.IdStringForElement(ann);
        Assert.That(secret, Does.EndWith("i"), "encrypted");
        Assert.That(manipulator.ElementForIdString(secret), Is.SameAs(ann));
        manipulator.SetValue(manipulator.IdStringForElement(ann.M_Home), manipulator.IdStringForElement(ann.M_FirstName));
        Assert.That(ann.Home, Is.SameAs(house), "an id string that names no object leaves the reference");
    }

    [Test]
    [Covers(typeof(TBoldManipulatorMapper), "*")]
    [Covers(typeof(TBoldManipulatorMapperCollection), "*")]
    [Covers("BoldManipulators", "TBoldManipulatorGetter", "TBoldManipulatorSetter")]
    public void Mappings_convert_values_on_the_way_in_and_out()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        var manipulator = new TBoldManipulator(s.Components);
        manipulator.BoldSystem = s.System;
        Assert.That(manipulator.BoldSystem, Is.SameAs(s.System));
        var upper = (TBoldManipulatorMapper)manipulator.Mappers.Add();
        upper.MappingName = "upper";
        upper.OnGet = e => e!.AsString.ToUpperInvariant();
        upper.OnSet = (e, value) => e!.AsString = value.ToLowerInvariant();
        Assert.That(upper.DisplayName, Is.EqualTo("upper"));
        Assert.That(manipulator.Mappers[0], Is.SameAs(upper));
        Assert.That(manipulator.Mappers.ItemByname("upper"), Is.SameAs(upper));
        Assert.That(manipulator.Mappers.Owner, Is.SameAs(manipulator));
        Assert.That(() => manipulator.Mappers.ItemByname("lower"), Throws.InstanceOf<EBold>().With.Message.Contains("Unknown mapping"));

        string id = "";
        Assert.That(manipulator.GetValueAndId(ann.M_FirstName, ref id, "upper"), Is.EqualTo("ANN"));
        Assert.That(id, Does.EndWith(":firstName[upper]"));
        Assert.That(manipulator.IdStringForElement(ann.M_FirstName, "upper"), Is.EqualTo(id));
        Assert.That(manipulator.ElementForIdString(id), Is.SameAs(ann.M_FirstName), "the mapping is stripped");
        manipulator.SetValue(id, "BOB");
        Assert.That(ann.FirstName, Is.EqualTo("bob"));

        var empty = (TBoldManipulatorMapper)manipulator.Mappers.Add();
        Assert.That(empty.GetAsString(ann), Is.EqualTo(""));
        empty.SetFromString(ann, "ignored");
        Assert.That(empty.OnGet, Is.Null);
        Assert.That(empty.OnSet, Is.Null);

        var copy = new TBoldManipulator(s.Components) { Mappers = manipulator.Mappers };
        Assert.That(copy.Mappers.Count, Is.EqualTo(2));
    }

    [Test]
    public void Without_a_system_objects_cannot_be_created_and_a_disposed_system_handle_is_forgotten()
    {
        using var s = new HandlesSample();
        var manipulator = new TBoldManipulator(s.Components) { Name = "Manipulator" };
        Assert.That(() => manipulator.CreateObject("Person"), Throws.InstanceOf<EBold>().With.Message.Contains("TBoldManipulator.CreateObject"));
        manipulator.BoldSystemHandle = s.SystemHandle;
        s.System.Discard();
        s.SystemHandle.Active = false;
        s.SystemHandle.Dispose();
        Assert.That(manipulator.BoldSystemHandle, Is.Null);
    }
}
