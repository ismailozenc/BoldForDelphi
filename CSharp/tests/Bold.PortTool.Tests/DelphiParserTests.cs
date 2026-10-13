using Bold.PortTool.Delphi;

namespace Bold.Tests;

public class DelphiParserTests
{
    private const string Source = """
        unit Sample;

        interface

        uses
          SysUtils, BoldDefs;

        const
          LIMIT = 10;
          NAME: string = 'x' deprecated;

        resourcestring
          sHello = 'Hello %s';

        type
          TKind = (kOne, kTwo = 5, kThree);
          TKindSet = set of TKind;
          TCount = integer;
          TNotify = procedure(Sender: TObject; const Text: string) of object;
          TFoo = class;
          EFoo = class(Exception);

          TFoo = class(TObject, IInterface)
          private
            FItems: array of Integer;
            FName: string;
            function GetItem(Index: Integer): Integer;
            procedure SetName(const Value: string);
          protected
            procedure Run; virtual; abstract;
          public
            constructor Create(Owner: TObject);
            destructor Destroy; override;
            procedure Add(Value: Integer); overload;
            procedure Add(const Value: string); overload;
            class function Make: TFoo;
            property Items[Index: Integer]: Integer read GetItem; default;
            property Name: string read FName write SetName;
          end;

        function Twice(Value: Integer): Integer;

        implementation

        type
          THelper = class
            procedure Help;
          end;

        procedure THelper.Help;
        begin
        end;

        function TFoo.GetItem(Index: Integer): Integer;
        begin
          Result := FItems[Index];
        end;

        procedure TFoo.SetName(const Value: string);
        begin
          FName := Value;
        end;

        constructor TFoo.Create(Owner: TObject);
        begin
          inherited Create;
        end;

        destructor TFoo.Destroy;
        begin
          inherited;
        end;

        procedure TFoo.Add(Value: Integer);
        begin
        end;

        procedure TFoo.Add(const Value: string);
        var
          i: Integer;
          procedure Local;
          begin
            case i of
              1: ;
            end;
          end;
        begin
          try
            Local;
          finally
          end;
        end;

        class function TFoo.Make: TFoo;
        begin
          Result := TFoo.Create(nil);
        end;

        function Twice(Value: Integer): Integer;
        begin
          Result := Value * 2;
        end;

        function Hidden: Boolean;
        begin
          Result := True;
        end;

        initialization
          Twice(1);
        finalization
          Twice(2);
        end.
        """;

    private static DelphiUnit Parse() =>
        DelphiParser.Parse(DelphiLexer.TokenizeText(Source.Replace("\r\n", "\n"), "Sample.pas"), "Sample.pas")!;

    private static DelphiSymbol Find(DelphiUnit unit, string key) => unit.Symbols.Single(s => s.Key == key);

    [Test]
    public void Reads_every_declaration_with_its_kind()
    {
        var unit = Parse();
        var expected = new[]
        {
            "LIMIT const", "NAME const", "sHello resourcestring", "TKind enum", "TKindSet set", "TCount alias", "TNotify event type",
            "EFoo class", "TFoo class", "TFoo.GetItem method", "TFoo.SetName method", "TFoo.Run method", "TFoo.Create constructor",
            "TFoo.Destroy destructor", "TFoo.Add(Integer) method", "TFoo.Add(string) method", "TFoo.Make class method",
            "TFoo.Items property", "TFoo.Name property", "Twice routine", "THelper class", "THelper.Help method", "Hidden routine",
            "initialization initialization", "finalization finalization",
        };
        Assert.That(unit.Symbols.Select(s => $"{s.Key} {s.KindText}"), Is.EquivalentTo(expected));
        Assert.That(unit.Warnings, Is.Empty);
    }

    [Test]
    public void Links_each_declaration_to_its_body()
    {
        var unit = Parse();
        Assert.Multiple(() =>
        {
            Assert.That(Find(unit, "TFoo.GetItem").ImplLine, Is.EqualTo(Source.Split('\n').ToList().FindIndex(l => l.Contains("function TFoo.GetItem")) + 1));
            Assert.That(Find(unit, "TFoo.Add(string)").ImplLine, Is.GreaterThan(Find(unit, "TFoo.Add(Integer)").ImplLine!.Value));
            Assert.That(Find(unit, "TFoo.Run").ImplLine, Is.Null, "abstract methods have no body");
            Assert.That(Find(unit, "TFoo.Run").IsAbstract, Is.True);
            Assert.That(Find(unit, "Hidden").InInterface, Is.False);
            Assert.That(Find(unit, "Twice").InInterface, Is.True);
        });
    }

    [Test]
    public void Records_uses_parents_accessors_and_parameters()
    {
        var unit = Parse();
        Assert.Multiple(() =>
        {
            Assert.That(unit.UsesInterface, Is.EqualTo(new[] { "SysUtils", "BoldDefs" }));
            Assert.That(Find(unit, "TFoo").Parent, Is.EqualTo("TObject"));
            Assert.That(Find(unit, "TFoo").Interfaces, Is.EqualTo(new[] { "IInterface" }));
            Assert.That(Find(unit, "EFoo").Parent, Is.EqualTo("Exception"));
            Assert.That(Find(unit, "TFoo.Items").IsDefaultProperty, Is.True);
            Assert.That(Find(unit, "TFoo.GetItem").AccessorOf?.Key, Is.EqualTo("TFoo.Items"));
            Assert.That(Find(unit, "TFoo.SetName").AccessorOf?.Key, Is.EqualTo("TFoo.Name"));
            Assert.That(Find(unit, "TNotify").ParameterNames, Is.EqualTo(new[] { "Sender", "Text" }));
            Assert.That(Find(unit, "TNotify").ReturnType, Is.Null);
            Assert.That(Find(unit, "Twice").ReturnType, Is.EqualTo("Integer"));
            Assert.That(Find(unit, "TKind").EnumMembers!.Select(m => $"{m.Key}={m.Value}"), Is.EqualTo(new[] { "kOne=0", "kTwo=5", "kThree=6" }));
            Assert.That(Find(unit, "TCount").TypeText, Is.EqualTo("integer"));
        });
    }

    [Test]
    public void Constant_values_can_be_evaluated()
    {
        var values = DelphiConstants.Evaluate(Parse());
        Assert.Multiple(() =>
        {
            Assert.That(values["LIMIT"], Is.EqualTo(10L));
            Assert.That(values["NAME"], Is.EqualTo("x"), "the deprecated hint is not part of the value");
            Assert.That(values["sHello"], Is.EqualTo("Hello %s"));
        });
    }

    [Test]
    public void Programs_and_packages_are_not_units()
    {
        Assert.That(DelphiParser.Parse(DelphiLexer.TokenizeText("program P; begin end."), "P.dpr"), Is.Null);
    }
}
