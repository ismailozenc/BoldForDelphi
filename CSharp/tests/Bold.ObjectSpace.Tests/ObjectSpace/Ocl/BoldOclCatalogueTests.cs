// Delphi: Source/ObjectSpace/Ocl/BoldOclSymbolImplementations.pas
// Port map: CSharp/port/units/BoldOclSymbolImplementations.md
// The M4 expression catalogue: every operation of the OCL library, evaluated on a small town, with the value Delphi
// Bold gives. Each case names the expression, the root (the system, or Ann) and the expected string.
namespace Bold.Tests;

public class BoldOclCatalogueTests
{
    private static TBoldMember M(TBoldObject o, string name) => o.BoldMemberByExpressionName(name);

    /// <summary>
    /// The town: Ann (30, salary 100.5, born 1990-12-24, wakes 07:30, married) and Bob (40) live in the House, Cy (25)
    /// and Dee (no age) in the Flat; Ann's friends are Bob and Cy, in that order.
    /// </summary>
    private sealed class Town : IDisposable
    {
        public readonly ObjectSpaceSample S = new(m => m.Classes.ItemsByName("Person")!.SetStdTV("Author", "Ann"));
        public readonly TBoldObject Ann, Bob, Cy, Dee;

        public Town()
        {
            Ann = S.NewPerson();
            Bob = S.NewPerson();
            Cy = S.NewPerson();
            Dee = S.NewPerson();
            TBoldObject house = S.NewBuilding(), flat = S.NewBuilding();
            M(house, "address").AsString = "House";
            M(flat, "address").AsString = "Flat";
            foreach ((TBoldObject p, string name, int? age, TBoldObject home) in new[] { (Ann, "Ann", (int?)30, house), (Bob, "Bob", 40, house), (Cy, "Cy", 25, flat), (Dee, "Dee", null, flat) })
            {
                M(p, "firstName").AsString = name;
                if (age is { } a)
                {
                    ((TBAInteger)M(p, "age")).AsInteger = a;
                }
                else
                {
                    ((TBAInteger)M(p, "age")).SetToNull();
                }

                ((TBoldObjectReference)M(p, "home")).BoldObject = home;
            }

            ((TBACurrency)M(Ann, "salary")).AsCurrency = 100.5m;
            ((TBADate)M(Ann, "born")).AsDate = new DateTime(1990, 12, 24);
            ((TBATime)M(Ann, "wakes")).AsTime = new DateTime(1899, 12, 30, 7, 30, 0);
            ((TBABoolean)M(Ann, "married")).AsBoolean = true;
            var friends = (TBoldObjectList)M(Ann, "friends");
            friends.Add(Bob);
            friends.Add(Cy);
        }

        public void Dispose() => S.Dispose();
    }

    public static IEnumerable<TestCaseData> Cases()
    {
        // Comparison and arithmetic
        yield return C("1 = 1", "Y");
        yield return C("'a' = 'b'", "N");
        yield return C("1 <> 1", "N");
        yield return C("3 - 5", "-2");
        yield return C("-(3)", "-3");
        yield return C("2.5 * 2", "5");
        yield return C("7 / 2", "3.5");
        yield return C("(-2.5).abs", "2.5");
        yield return C("2.7.floor", "2");
        yield return C("3.5.round", "4");
        yield return C("'42'.strToInt", "42");
        yield return C("3.min(5)", "3");
        yield return C("3.max(5)", "5");
        yield return C("2 <= 2", "Y");
        yield return C("3 >= 4", "N");
        yield return C("3 > 2", "Y");
        yield return C("1 < 2.5", "Y");
        yield return C("7 div 2", "3");
        yield return C("7 mod 3", "1");
        // Strings
        yield return C("'abc'.length", "3");
        yield return C("'ab'.concat('cd')", "abcd");
        yield return C("'aBc'.toUpper", "ABC");
        yield return C("'aBc'.toLower", "abc");
        yield return C("'Hello'.subString(2, 4)", "ell");
        yield return C("'7'.pad(3, '0')", "007");
        yield return C("'7'.postPad(3, '0')", "700");
        // formatNumeric is Delphi's Format, so it takes a %-format.
        yield return C("1234.5.formatNumeric('%.2f')", "1234.50");
        yield return C("#2001-02-03.formatDateTime('dd.mm.yyyy')", "03.02.2001");
        yield return C("'Hello'.sqlLike('H%o')", "Y");
        yield return C("'Hello'.sqlLike('h%')", "N");
        yield return C("'Hello'.sqlLikeCaseInsensitive('h_llo')", "Y");
        yield return C("'abc123'.regExpMatch('^[a-z]+[0-9]+$')", "Y");
        yield return C("'abc'.regExpMatch('^[0-9]+$')", "N");
        // Logic
        yield return C("true or false", "Y");
        yield return C("true and false", "N");
        yield return C("not true", "N");
        yield return C("true xor true", "N");
        yield return C("false implies false", "Y");
        yield return C("if 3 > 2 then 'yes' else 'no' endif", "yes");
        // Collections
        yield return C("Sequence{1, 2, 3}->size", "3");
        yield return C("Sequence{1, 2, 3}->includes(4)", "N");
        yield return C("Sequence{1, 2, 2}->count(2)", "2");
        yield return C("Sequence{1, 2, 3}->includesAll(Sequence{1, 3})", "Y");
        yield return C("Person.allInstances->select(firstName = 'Zed')->isEmpty", "Y");
        yield return C("Person.allInstances->notEmpty", "Y");
        yield return C("Sequence{1, 2, 3, 4}->sum", "10");
        yield return C("Sequence{3, 1, 2}->maxValue", "3");
        yield return C("Sequence{3, 1, 2}->minValue", "1");
        yield return C("Sequence{1, 2, 3, 6}->average", "3");
        yield return C("Sequence{1, 2, 3}->forAll(x | x > 1)", "N");
        yield return C("Sequence{1, 2, 3}->exists(x | x > 2)", "Y");
        // including, append and prepend take an object (Delphi declares ObjectType); values are refused.
        yield return A("friends->append(self)->last.firstName", "Ann");
        yield return A("friends->prepend(self)->first.firstName", "Ann");
        yield return C("Sequence{1, 1, 2}->asSet->size", "2");
        yield return C("Sequence{1, 1, 2}->asBag->size", "3");
        yield return C("Set{1, 2}->asSequence->size", "2");
        yield return C("Sequence{1, 2, 3, 4}->subSequence(2, 3)->sum", "5");
        yield return C("Sequence{5, 6, 7}->at(3)", "7");
        yield return C("Sequence{1, 2, 3}->reject(x | x = 2)->sum", "4");
        yield return C("Sequence{1, 2, 3}->collect(x | x * 10)->sum", "60");
        yield return C("Person.allInstances->orderby(firstName)->last.firstName", "Dee");
        yield return C("Person.allInstances->orderdescending(firstName)->first.firstName", "Dee");
        // Objects and navigation
        yield return C("Person.allInstances->size", "4");
        yield return C("Person.allLoadedObjects->size", "4");
        yield return C("Building.allInstances.residents->size", "4");
        yield return C("Person.allInstances->select(age.isNull)->size", "1");
        // A null among the summed values raises, as in Delphi (XNumeric reads the value).
        yield return C("Person.allInstances->reject(age.isNull).age->sum", "95");
        yield return C("Person.emptyList->size", "0");
        yield return C("Person.allInstances->filterOnType(Person)->size", "4");
        yield return A("friends->size", "2");
        yield return A("friends->first.firstName", "Bob");
        yield return A("friends->at(2).firstName", "Cy");
        yield return A("Person.allInstances->intersection(friends)->size", "2");
        yield return A("Person.allInstances->difference(friends)->size", "2");
        yield return A("Person.allInstances->excluding(self)->size", "3");
        yield return A("friends->union(Person.allInstances->select(firstName = 'Dee'))->size", "3");
        yield return A("Person.allInstances->symmetricDifference(friends)->size", "2");
        yield return A("friends->including(self)->size", "3");
        yield return A("home.residents->includes(self)", "Y");
        yield return A("salary", "100.5");
        yield return A("salary + 1", "101.5");
        yield return A("married", "Y");
        yield return A("born.formatDateTime('yyyy-mm-dd')", "1990-12-24");
        yield return A("wakes.formatDateTime('hh:nn')", "07:30");
        // The range is given as day numbers (Delphi declares Numeric arguments).
        yield return A("born.inDateRange(#1990-01-01.dateTimeAsFloat, #1990-12-31.dateTimeAsFloat)", "Y");
        yield return A("born.inDateRange(#1991-01-01.dateTimeAsFloat, #1991-12-31.dateTimeAsFloat)", "N");
        yield return A("wakes.inTimeRange(#07:00.dateTimeAsFloat, #08:00.dateTimeAsFloat)", "Y");
        // A time literal holds its time (Delphi's always held 0).
        yield return A("#08:15.formatDateTime('hh:nn')", "08:15");
        yield return A("wakes = #07:30", "Y");
        yield return A("Person.allInstances->select(age > 26)->collect(wakes)->sumTime.formatDateTime('hh:nn')", "07:30");
        yield return A("age.asString", "30");
        // An object without a default string representation shows its member of the representation's index (1 is age).
        yield return A("self.asString", "30");
        // Types and the model
        yield return A("self.oclType.typename", "Person");
        yield return A("self.oclIsKindOf(Person)", "Y");
        yield return A("self.oclIsTypeOf(Building)", "N");
        yield return A("self.oclAsType(Person).firstName", "Ann");
        yield return A("self.safeCast(Building)->isEmpty", "Y");
        yield return C("Person.attributes->size", "9");
        // home, friendOf, friends and the two link roles to Friendship.
        yield return C("Person.associationEnds->size", "5");
        yield return C("Person.superTypes->size", "1");
        yield return C("Person.allSuperTypes->size", "1");
        // The class itself is included.
        yield return C("TownRoot.allSubClasses->size", "4");
        yield return C("Person.typename", "Person");
        yield return C("Person.constraints->size", "0");
        yield return C("#2001-02-03.dateTimeAsFloat", "36925");
        // The default format settings read M/d/yyyy.
        yield return C("'2/3/2001'.strToDate.formatDateTime('yyyy-mm-dd')", "2001-02-03");
        yield return C("'07:30'.strToTime.formatDateTime('hh:nn')", "07:30");
        yield return C("'2/3/2001 07:30'.strToDateTime.formatDateTime('yyyy-mm-dd hh:nn')", "2001-02-03 07:30");
        yield return A("firstName.stringRepresentation(1)", "Ann");
        // taggedValue reads the tagged values of the object's class.
        yield return A("self.taggedValue('Author')", "Ann");
        yield return A("self.taggedValue('Nothing')", "");
        yield return A("(1 / 0).except(5)", "5");
        yield return A("(1 + 1).except(5)", "2");
    }

    private static TestCaseData C(string ocl, string expected) => new TestCaseData(ocl, false, expected).SetName("System: " + ocl);

    private static TestCaseData A(string ocl, string expected) => new TestCaseData(ocl, true, expected).SetName("Ann: " + ocl);

    [TestCaseSource(nameof(Cases))]
    public void Operations_give_the_Delphi_values(string ocl, bool onAnn, string expected)
    {
        using var town = new Town();
        TBoldElement root = onAnn ? town.Ann : town.S.System;
        Assert.That(root.EvaluateExpressionAsString(ocl, BoldDefs.brDefault), Is.EqualTo(expected));
    }
}
