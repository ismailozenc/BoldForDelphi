// Delphi: Source/MoldModel/Bld/BoldBld.pas
// Port map: CSharp/port/units/BoldBld.md

using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldBld", Symbol = "typetableIndex", Note = "The bounds of the type table array; the C# array has its own.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldBld", Symbol = "TReader.Destroy", Note = "Freed the current token; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldBld", Symbol = "TElementClass", Note = "Each element reader is a singleton object; the type table holds those objects instead of class references.")]

namespace Bold;

/// <summary>Raised when a .bld file cannot be parsed; the message ends with the line and position.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldBLDParseError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>
/// Reads and writes models in Bold's .bld format: a VERSION line, then nested blocks such as (Model "name" ... (Classes
/// (Class ...)) (Associations ...)). The reader accepts versions 1 to 19; the writer writes version 19.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TMoldBLDRW
{
    private const string LINKEXTENSION = ".bld";
    private const string LINKDESC = "Bold Proprietary Format";
    internal const int CURRENTVERSION = 19;
    private const char SPACE = ' ';
    private const char TAB = '\t';

    /// <summary>The file extension and description of the format, for file dialogs.</summary>
    public static (string Extension, string Description) FileFormat => (LINKEXTENSION, LINKDESC);

    /// <summary>Reads a model from a .bld file (Windows-1252 unless it has a byte order mark).</summary>
    [Delphi(PortStatus.Ported)]
    public static TMoldModel ModelFromFile(string filename)
    {
        var sl = new TStringList();
        sl.LoadFromFile(filename);
        return StringsToModel(sl);
    }

    [Delphi(PortStatus.Ported)]
    public static void ModelToFile(TMoldModel Model, string filename)
    {
        var sl = new TStringList();
        ModelToStrings(Model, sl);
        sl.SaveToFile(filename);
    }

    /// <summary>Writes the model as .bld lines into <paramref name="s"/>, which is cleared first.</summary>
    [Delphi(PortStatus.Ported)]
    public static void ModelToStrings(TMoldModel? Model, TStrings s)
    {
        if (Model is null)
        {
            return;
        }

        var theWriter = new TWriter(s);
        theWriter.Put(SysUtils.Format("VERSION %d", CURRENTVERSION));
        TModel.Instance.Write(Model, theWriter);
    }

    /// <summary>Reads a model from .bld lines; no lines give an empty model named New_Model.</summary>
    [Delphi(PortStatus.Ported)]
    public static TMoldModel StringsToModel(TStrings s)
    {
        if (s.Count == 0)
        {
            return new TMoldModel(null, "New_Model");
        }

        var reader = new TReader(s);
        try
        {
            reader.CurrentToken = new TToken();
            reader.GetCharacter();
            reader.GetToken();
            reader.FFormatVersion = reader.TryKeyword("VERSION") ? reader.GetInteger() : 1;
            return (TMoldModel)TElement.CreateAndRead(null, reader);
        }
        catch (EBoldBLDParseError e)
        {
            throw new EBoldBLDParseError(SysUtils.Format(BoldMoldConsts.sErrorOnPos, e.Message, reader.LineNumber, reader.Position), e);
        }
    }

    /// <summary>Writes .bld lines with tab indentation, one value per line.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TWriter")]
    private sealed class TWriter : TBoldMemoryManagedObject
    {
        private readonly TStrings modelAsStrings;
        private int line;
        private int indent;

        [Delphi(PortStatus.Ported, Symbol = "TWriter.Create")]
        public TWriter(TStrings s)
        {
            modelAsStrings = s;
            s.Clear();
            s.Add("");
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.DoIndent")]
        private void DoIndent()
        {
            modelAsStrings.Add("");
            line++;
            for (int i = 0; i < indent; i++)
            {
                Put(TAB.ToString());
            }
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.PutStartBlock")]
        public void PutStartBlock(string keyword)
        {
            DoIndent();
            Put("(" + keyword);
            indent++;
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.PutEndBlock")]
        public void PutEndBlock()
        {
            indent--;
            DoIndent();
            Put(")");
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.Put")]
        public void Put(string s) => modelAsStrings[line] = modelAsStrings[line] + s;

        /// <summary>A quoted string: '"' and '\' are escaped with '\', CR is written \c and LF \l.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TWriter.PutQuotedString")]
        public void PutQuotedString(string s)
        {
            DoIndent();
            var sb = new System.Text.StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c is '"' or '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c == BoldDefs.BOLDCR)
                {
                    sb.Append("\\c");
                }
                else if (c == BoldDefs.BOLDLF)
                {
                    sb.Append("\\l");
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
            Put(sb.ToString());
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.Putinteger")]
        public void PutInteger(int i)
        {
            DoIndent();
            Put(SysUtils.IntToStr(i));
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.PutBoolean")]
        public void PutBoolean(bool b)
        {
            DoIndent();
            Put(b ? "TRUE" : "FALSE");
        }

        [Delphi(PortStatus.Ported, Symbol = "TWriter.PutClassReference")]
        public void PutClassReference(TMoldClass? aClass) => PutQuotedString(aClass is not null ? aClass.Name : "<NONE>");
    }

    [Delphi(PortStatus.Ported, Symbol = "BLDTokenKind")]
    private enum BLDTokenKind { LPAR, RPAR, KEYWORD, QSTRING, INTVALUE, ENDOFFILE }

    [Delphi(PortStatus.Ported, Symbol = "TToken")]
    private sealed class TToken : TBoldMemoryManagedObject
    {
        public BLDTokenKind Kind;
        public string StringValue = "";
        public int IntegerValue;
    }

    /// <summary>Reads .bld tokens: parentheses, keywords (letters), quoted strings with escapes, and unsigned integers.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TReader")]
    private sealed class TReader : TBoldMemoryManagedObject
    {
        private readonly TStrings modelAsStrings;
        internal int FFormatVersion;
        private int fLineNumber;
        private string fLine = "";
        private int fLineLength;
        private bool fEOS;
        internal int Position;
        internal TToken CurrentToken = null!;
        private char CurrentCharacter;
        internal TMoldModel? CurrentModel;

        [Delphi(PortStatus.Ported, Symbol = "TReader.Create")]
        public TReader(TStrings s)
        {
            modelAsStrings = s;
            fEOS = modelAsStrings.Count == 0;
            Position = 1;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.EOS")]
        private bool EOS => fEOS;

        [Delphi(PortStatus.Ported, Symbol = "TReader.LineNumber")]
        internal int LineNumber => fLineNumber;

        [Delphi(PortStatus.Ported, Symbol = "TReader.Line")]
        private string Line => fLine;

        [Delphi(PortStatus.Ported, Symbol = "TReader.FormatVersion")]
        public int FormatVersion => FFormatVersion;

        [Delphi(PortStatus.Ported, Symbol = "TReader.Skip")]
        private void Skip()
        {
            while (!EOS && CurrentCharacter is SPACE or TAB)
            {
                GetCharacter();
            }
        }

        /// <summary>The next character; the end of a line reads as a space.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TReader.GetCharacter")]
        internal void GetCharacter()
        {
            if (EOS)
            {
                CurrentCharacter = ' ';
            }
            else if (Position > fLineLength)
            {
                if (LineNumber >= modelAsStrings.Count)
                {
                    fEOS = true;
                }
                else
                {
                    fLine = modelAsStrings[LineNumber];
                    fLineLength = Line.Length;
                    fLineNumber++;
                }

                Position = 1;
                CurrentCharacter = ' ';
            }
            else
            {
                CurrentCharacter = Line[Position - 1];
                Position++;
            }
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.GetCharacterNotEof")]
        private void GetCharacterNotEof()
        {
            if (EOS)
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sUnexpectedEOF);
            }

            GetCharacter();
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.NextCharacter")]
        private char NextCharacter()
        {
            GetCharacter();
            return CurrentCharacter;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.GetToken")]
        internal void GetToken()
        {
            Skip();
            TToken t = CurrentToken;
            if (EOS)
            {
                t.Kind = BLDTokenKind.ENDOFFILE;
                return;
            }

            char c = CurrentCharacter;
            if (c == '(')
            {
                t.Kind = BLDTokenKind.LPAR;
                GetCharacter();
            }
            else if (c == ')')
            {
                t.Kind = BLDTokenKind.RPAR;
                GetCharacter();
            }
            else if (c is >= '0' and <= '9')
            {
                t.Kind = BLDTokenKind.INTVALUE;
                t.IntegerValue = c - '0';
                while (NextCharacter() is >= '0' and <= '9')
                {
                    t.IntegerValue = t.IntegerValue * 10 + CurrentCharacter - '0';
                }
            }
            else if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
            {
                t.Kind = BLDTokenKind.KEYWORD;
                string lineWithToken = fLine;
                int startIndex = Position;
                int foundLength = 1;
                while (NextCharacter() is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                {
                    foundLength++;
                }

                t.StringValue = SysUtils.Copy(lineWithToken, startIndex - 1, foundLength);
            }
            else if (c == '"')
            {
                t.Kind = BLDTokenKind.QSTRING;
                GetCharacterNotEof();
                var sb = new System.Text.StringBuilder();
                while (CurrentCharacter != '"')
                {
                    if (CurrentCharacter == '\\')
                    {
                        GetCharacterNotEof();
                        if (CurrentCharacter == 'c')
                        {
                            CurrentCharacter = BoldDefs.BOLDCR;
                        }
                        else if (CurrentCharacter == 'l')
                        {
                            CurrentCharacter = BoldDefs.BOLDLF;
                        }
                    }

                    sb.Append(CurrentCharacter);
                    GetCharacterNotEof();
                }

                GetCharacter();
                t.StringValue = sb.ToString();
            }
            else
            {
                throw new EBoldBLDParseError(SysUtils.Format(BoldMoldConsts.sBadCharacter, SysUtils.IntToStr(c)));
            }
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.Eat")]
        internal void Eat(BLDTokenKind t)
        {
            if (CurrentToken.Kind != t)
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sSyntaxError);
            }

            GetToken();
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.EatKeyword")]
        private void EatKeyword(string keyw)
        {
            if (!TryKeyword(keyw))
            {
                throw new EBoldBLDParseError(SysUtils.Format(BoldMoldConsts.sAKeywordExpected, keyw));
            }
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.EatStartBlock")]
        internal void EatStartBlock(string keyw)
        {
            Eat(BLDTokenKind.LPAR);
            EatKeyword(keyw);
        }

        /// <summary>Eats the keyword (case-sensitive) when it is the current token.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TReader.TryKeyword")]
        internal bool TryKeyword(string keyw)
        {
            if (CurrentToken.Kind == BLDTokenKind.KEYWORD && CurrentToken.StringValue == keyw)
            {
                GetToken();
                return true;
            }

            return false;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.GetQuotedString")]
        internal string GetQuotedString()
        {
            string result = CurrentToken.StringValue;
            if (CurrentToken.Kind != BLDTokenKind.QSTRING)
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sQuotedStringExpected);
            }

            GetToken();
            return result;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.GetKeyword")]
        internal string GetKeyword()
        {
            string result = CurrentToken.StringValue;
            if (CurrentToken.Kind != BLDTokenKind.KEYWORD)
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sKeyWordTokenExpected);
            }

            GetToken();
            return result;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.Getinteger")]
        internal int GetInteger()
        {
            int result = CurrentToken.IntegerValue;
            if (CurrentToken.Kind != BLDTokenKind.INTVALUE)
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sIntegerExpected);
            }

            GetToken();
            return result;
        }

        [Delphi(PortStatus.Ported, Symbol = "TReader.GetBoolean")]
        internal bool GetBoolean()
        {
            bool result = false;
            if (CurrentToken.Kind == BLDTokenKind.KEYWORD && CurrentToken.StringValue == "TRUE")
            {
                result = true;
            }
            else if (CurrentToken.Kind != BLDTokenKind.KEYWORD || CurrentToken.StringValue != "FALSE")
            {
                throw new EBoldBLDParseError(BoldMoldConsts.sBooleanExpected);
            }

            GetToken();
            return result;
        }

        /// <summary>A class name; "&lt;NONE&gt;" is nil, an unknown name creates the class.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TReader.GetClassReference")]
        internal TMoldClass? GetClassReference()
        {
            string name = GetQuotedString();
            return name == "<NONE>" ? null : CurrentModel!.GetClassByName(name);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TElementClassRecord")]
    private readonly record struct TElementClassRecord(TElement ElementClass, TMoldElementClass MoldElementClass, string Name);

    /// <summary>Reads and writes one kind of element. The base writes the name; subclasses add their fields.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TElement")]
    private class TElement : TBoldMemoryManagedObject
    {
        [Delphi(PortStatus.Ported, Symbol = "TElement.Write")]
        public virtual void Write(TMoldElement element, TWriter w) => w.PutQuotedString(element.Name);

        [Delphi(PortStatus.Ported, Symbol = "TElement.Read")]
        public virtual void Read(TMoldElement element, TReader r)
        {
            // Do nothing
        }

        /// <summary>Reads "(Kind "name" ...)": classes are found or created by name, other elements are created in the parent.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TElement.CreateAndRead")]
        public static TMoldElement CreateAndRead(TMoldElement? parent, TReader r)
        {
            r.Eat(BLDTokenKind.LPAR);
            TElementClassRecord typeInfo = TypeTableByName(r.GetKeyword());
            string name = r.GetQuotedString();
            TMoldElement result = typeInfo.MoldElementClass == typeof(TMoldClass)
                ? ((TMoldModel)parent!).GetClassByName(name)
                : (TMoldElement)Activator.CreateInstance(typeInfo.MoldElementClass, parent, name)!;
            typeInfo.ElementClass.Read(result, r);
            return result;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TModel")]
    private sealed class TModel : TElement
    {
        public static readonly TModel Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TModel.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var model = (TMoldModel)element;
            w.PutStartBlock("Model");
            w.PutQuotedString(model.Name);
            w.PutQuotedString(model.RootClass.Name);
            w.PutQuotedString(model.Stereotype);
            w.PutQuotedString(model.Constraints.CommaText);
            w.PutQuotedString(model.NonDefaultTaggedValuesCommaText);
            w.PutStartBlock("Classes");
            TElementList.Write(model.Classes, w);
            w.PutEndBlock();
            w.PutStartBlock("Associations");
            TElementList.Write(model.Associations, w);
            w.PutEndBlock();
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TModel.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var model = (TMoldModel)element;
            base.Read(element, r);
            int v = r.FormatVersion;
            if (v >= 6 && v < 19)
            {
                model.SetBoldTV(TAG_INTERFACEUSES, r.GetQuotedString());
                model.SetBoldTV(TAG_IMPLEMENTATIONUSES, r.GetQuotedString());
            }

            if (v >= 11)
            {
                model.RootClass.Name = r.GetQuotedString();
            }

            if (v >= 12 && v < 19)
            {
                model.SetBoldTV(TAG_PMAPPERNAME, r.GetQuotedString());
            }

            if (v >= 13 && v < 19)
            {
                r.GetQuotedString(); // legacy, was Model.DatabaseName
            }

            if (v >= 15 && v < 19)
            {
                model.SetBoldTV(TAG_USEXFILES, BoldUtils.BooleanToString(r.GetBoolean()));
                model.SetBoldTV(TAG_USETIMESTAMP, BoldUtils.BooleanToString(r.GetBoolean()));
                model.SetBoldTV(TAG_USEGLOBALID, BoldUtils.BooleanToString(r.GetBoolean()));
                model.SetBoldTV(TAG_USEREADONLY, BoldUtils.BooleanToString(r.GetBoolean()));
            }

            if (v >= 16)
            {
                model.Stereotype = r.GetQuotedString();
                model.Constraints.CommaText = r.GetQuotedString();
            }

            if (v >= 17)
            {
                model.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            r.EatStartBlock("Classes");
            r.CurrentModel = model;
            TElementList.Read(model, r);
            r.Eat(BLDTokenKind.RPAR);
            r.EatStartBlock("Associations");
            TElementList.Read(model, r);
            r.Eat(BLDTokenKind.RPAR);
            r.Eat(BLDTokenKind.RPAR);
            r.Eat(BLDTokenKind.ENDOFFILE);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TElementList")]
    private static class TElementList
    {
        [Delphi(PortStatus.Ported, Symbol = "TElementList.Write")]
        public static void Write(TMoldElementList list, TWriter w)
        {
            for (int i = 0; i < list.Count; i++)
            {
                TypeTableByMoldElement(list[i]).ElementClass.Write(list[i], w);
            }
        }

        /// <summary>Reads elements while blocks follow; each element links itself into the right list of the parent.</summary>
        [Delphi(PortStatus.Ported, Symbol = "TElementList.Read")]
        public static void Read(TMoldElement parent, TReader r)
        {
            while (r.CurrentToken.Kind == BLDTokenKind.LPAR)
            {
                TElement.CreateAndRead(parent, r);
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TPClass")]
    private sealed class TPClass : TElement
    {
        public static readonly TPClass Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TPClass.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var c = (TMoldClass)element;
            w.PutStartBlock("Class");
            base.Write(element, w);
            w.PutClassReference(c.SuperClass);
            w.PutBoolean(c.Persistent);
            w.PutBoolean(c.IsAbstract);
            w.PutQuotedString(c.Stereotype);
            w.PutQuotedString(c.Constraints.CommaText);
            w.PutQuotedString(c.NonDefaultTaggedValuesCommaText);
            w.PutStartBlock("Attributes");
            TElementList.Write(c.Attributes, w);
            w.PutEndBlock();
            w.PutStartBlock("Methods");
            TElementList.Write(c.Methods, w);
            w.PutEndBlock();
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TPClass.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var c = (TMoldClass)element;
            int v = r.FormatVersion;
            if (v < 19)
            {
                c.SetBoldTV(TAG_TABLENAME, r.GetQuotedString());
                c.SetBoldTV(TAG_TABLEMAPPING, TBoldTaggedValueSupport.TableMappingToString((TTableMapping)r.GetInteger()));
                r.GetInteger(); // was the class id
            }

            c.SuperClass = r.GetClassReference();

            // Older versions may leave the superclass nil where it should be the root class.
            if (c.SuperClass is null && element != c.Model.RootClass)
            {
                c.SuperClass = c.Model.RootClass;
            }

            if (v >= 3)
            {
                c.Persistent = r.GetBoolean();
                c.IsAbstract = r.GetBoolean();
                if (v < 19)
                {
                    c.SetBoldTV(TAG_IMPORTED, BoldUtils.BooleanToString(r.GetBoolean()));
                    c.SetBoldTV(TAG_DELPHINAME, r.GetQuotedString());
                    if (v >= 7)
                    {
                        c.SetBoldTV(TAG_EXPRESSIONNAME, r.GetQuotedString());
                    }
                }
            }
            else
            {
                c.SetBoldTV(TAG_DELPHINAME, c.TableName);
            }

            if (v >= 5 && v < 19)
            {
                c.SetBoldTV(TAG_INCFILENAME, r.GetQuotedString());
            }

            if (v >= 12 && v < 19)
            {
                c.SetBoldTV(TAG_PMAPPERNAME, r.GetQuotedString());
            }

            if (v >= 16)
            {
                c.Stereotype = r.GetQuotedString();
                c.Constraints.CommaText = r.GetQuotedString();
            }

            if (v >= 17)
            {
                c.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            r.EatStartBlock("Attributes");
            TElementList.Read(element, r);
            r.Eat(BLDTokenKind.RPAR);
            r.EatStartBlock("Methods");
            TElementList.Read(element, r);
            r.Eat(BLDTokenKind.RPAR);
            r.Eat(BLDTokenKind.RPAR);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TAttribute")]
    private sealed class TAttribute : TElement
    {
        public static readonly TAttribute Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TAttribute.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var a = (TMoldAttribute)element;
            w.PutStartBlock("Attribute");
            base.Write(element, w);
            w.PutQuotedString(a.BoldType);
            w.PutBoolean(a.Derived);
            w.PutQuotedString(a.Stereotype);
            w.PutQuotedString(a.Constraints.CommaText);
            w.PutInteger((int)a.Visibility);
            w.PutQuotedString(a.InitialValue);
            w.PutQuotedString(a.NonDefaultTaggedValuesCommaText);
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TAttribute.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var a = (TMoldAttribute)element;
            base.Read(element, r);
            int v = r.FormatVersion;
            if (v < 19)
            {
                a.SetBoldTV(TAG_COLUMNNAME, r.GetQuotedString());
            }

            a.BoldType = r.GetQuotedString();
            if (v >= 3)
            {
                if (v < 19)
                {
                    a.SetBoldTV(TAG_ALLOWNULL, BoldUtils.BooleanToString(r.GetBoolean()));
                    a.SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, r.GetBoolean()
                        ? BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT
                        : BoldUMLTaggedValues.TV_PERSISTENCE_TRANSIENT);
                }

                a.Derived = r.GetBoolean();
                if (v >= 8 && v < 19)
                {
                    a.SetBoldTV(TAG_DELAYEDFETCH, BoldUtils.BooleanToString(r.GetBoolean()));
                }

                if (v < 19)
                {
                    a.SetBoldTV(TAG_LENGTH, SysUtils.IntToStr(r.GetInteger()));
                    a.SetBoldTV(TAG_DELPHINAME, r.GetQuotedString());
                    if (v >= 10)
                    {
                        a.SetBoldTV(TAG_PMAPPERNAME, r.GetQuotedString());
                    }
                }

                if (v == 15)
                {
                    r.GetBoolean();
                    r.GetBoolean();
                    r.GetBoolean();
                    r.GetInteger();
                }

                if (v >= 16)
                {
                    if (v < 19)
                    {
                        a.SetBoldTV(TAG_ATTRIBUTEKIND, TBoldTaggedValueSupport.AttributeKindToString((TBoldAttributeKind)r.GetInteger()));
                    }

                    a.Stereotype = r.GetQuotedString();
                    a.Constraints.CommaText = r.GetQuotedString();
                    a.Visibility = (TVisibilityKind)r.GetInteger();
                    if (v < 19)
                    {
                        r.GetQuotedString(); // was DelphiField, removed in version 18
                        a.SetBoldTV(TAG_DPREAD, TBoldTaggedValueSupport.DelphiPropertyAccessKindToString((TDelphiPropertyAccessKind)r.GetInteger()));
                        a.SetBoldTV(TAG_DPWRITE, TBoldTaggedValueSupport.DelphiPropertyAccessKindToString((TDelphiPropertyAccessKind)r.GetInteger()));
                        r.GetQuotedString(); // was DerivationOCL
                    }

                    a.InitialValue = r.GetQuotedString();
                }
                else
                {
                    a.Visibility = TVisibilityKind.vkPublic;
                }

                if (v >= 17)
                {
                    a.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
                }

                if (v >= 18 && v < 19)
                {
                    a.SetBoldTV(TAG_DELPHIFIELD, BoldUtils.BooleanToString(r.GetBoolean()));
                }
            }
            else
            {
                a.SetBoldTV(TAG_DELPHINAME, a.ColumnName);
            }

            r.Eat(BLDTokenKind.RPAR);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TMethod")]
    private sealed class TMethod : TElement
    {
        public static readonly TMethod Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TMethod.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var m = (TMoldMethod)element;
            w.PutStartBlock("Method");
            base.Write(element, w);
            w.PutQuotedString(m.Signature);
            w.PutBoolean(m.IsClassMethod);
            w.PutQuotedString(m.ReturnType);
            w.PutQuotedString(m.Stereotype);
            w.PutInteger((int)m.Visibility);
            w.PutQuotedString(m.Constraints.CommaText);
            w.PutQuotedString(m.NonDefaultTaggedValuesCommaText);
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TMethod.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var m = (TMoldMethod)element;
            base.Read(element, r);
            int v = r.FormatVersion;
            m.Signature = r.GetQuotedString();
            if (v < 19)
            {
                r.GetBoolean(); // was IsFunction
            }

            m.IsClassMethod = r.GetBoolean();
            if (v >= 3)
            {
                m.ReturnType = r.GetQuotedString();
                if (v < 19)
                {
                    m.SetBoldTV(TAG_DELPHIOPERATIONKIND, TBoldTaggedValueSupport.DelphiFunctionTypeToString((TDelphiFunctionType)r.GetInteger()));
                    m.SetBoldTV(TAG_DELPHINAME, r.GetQuotedString());
                }
            }

            if (v >= 16)
            {
                m.Stereotype = r.GetQuotedString();
                m.Visibility = (TVisibilityKind)r.GetInteger();
                m.Constraints.CommaText = r.GetQuotedString();
            }
            else
            {
                m.Visibility = TVisibilityKind.vkPublic;
            }

            if (v >= 17)
            {
                m.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            if (v >= 18 && v < 19)
            {
                m.SetBoldTV(TAG_OVERRIDEINALLSUBCLASSES, BoldUtils.BooleanToString(r.GetBoolean()));
            }

            r.Eat(BLDTokenKind.RPAR);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TRole")]
    private sealed class TRole : TElement
    {
        public static readonly TRole Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TRole.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var role = (TMoldRole)element;
            w.PutStartBlock("Role");
            base.Write(element, w);
            w.PutBoolean(role.Navigable);
            w.PutBoolean(role.Ordered);
            w.PutClassReference(role.MoldClass);
            w.PutQuotedString(role.Stereotype);
            w.PutQuotedString(role.Multiplicity);
            w.PutQuotedString(role.Constraints.CommaText);
            w.PutInteger((int)role.Aggregation);
            w.PutInteger((int)role.Visibility);
            w.PutInteger((int)role.Changeability);
            w.PutQuotedString(role.NonDefaultTaggedValuesCommaText);
            w.PutStartBlock("Qualifiers");
            TElementList.Write(role.Qualifiers, w);
            w.PutEndBlock();
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TRole.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var role = (TMoldRole)element;
            base.Read(element, r);
            int v = r.FormatVersion;
            if (v < 19)
            {
                role.SetBoldTV(TAG_COLUMNNAME, r.GetQuotedString());
            }

            if (v < 16)
            {
                role.Multiplicity = BoldBld.BooleanToMultiplicityString(r.GetBoolean());
            }

            role.Navigable = r.GetBoolean();
            role.Ordered = v >= 2 ? r.GetBoolean() : role.Multi && role.Navigable;
            role.MoldClass = r.GetClassReference();
            if (v >= 3)
            {
                if (v < 19)
                {
                    role.SetBoldTV(TAG_DELPHINAME, r.GetQuotedString());
                }
            }
            else
            {
                role.SetBoldTV(TAG_DELPHINAME, role.ColumnName);
            }

            if (v >= 4 && v < 19)
            {
                r.GetBoolean(); // legacy, was Mandatory
            }

            if (v < 19)
            {
                // Embed came in version 9 with default True.
                role.SetBoldTV(TAG_EMBED, BoldUtils.BooleanToString(v < 9 || r.GetBoolean()));
            }

            if (v >= 12 && v < 19)
            {
                role.SetBoldTV(TAG_PMAPPERNAME, r.GetQuotedString());
            }

            if (v >= 16)
            {
                role.Stereotype = r.GetQuotedString();
                role.Multiplicity = r.GetQuotedString();
                role.Constraints.CommaText = r.GetQuotedString();
                role.Aggregation = (TAggregationKind)r.GetInteger();
                role.Visibility = (TVisibilityKind)r.GetInteger();
                role.Changeability = (TChangeableKind)r.GetInteger();
            }
            else
            {
                role.Visibility = TVisibilityKind.vkPublic;
            }

            if (v >= 17)
            {
                role.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            if (v >= 14)
            {
                r.EatStartBlock("Qualifiers");
                TElementList.Read(element, r);
                r.Eat(BLDTokenKind.RPAR);
            }

            r.Eat(BLDTokenKind.RPAR);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TQualifier")]
    private sealed class TQualifier : TElement
    {
        public static readonly TQualifier Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TQualifier.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var q = (TMoldQualifier)element;
            w.PutStartBlock("Qualifier");
            base.Write(element, w);
            w.PutQuotedString(q.BoldType);
            w.PutQuotedString(q.NonDefaultTaggedValuesCommaText);
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TQualifier.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var q = (TMoldQualifier)element;
            base.Read(element, r);
            q.BoldType = r.GetQuotedString();
            if (r.FormatVersion < 19)
            {
                q.SetBoldTV(TAG_DELPHINAME, r.GetQuotedString());
            }

            if (r.FormatVersion >= 19)
            {
                q.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            r.Eat(BLDTokenKind.RPAR);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TAssociation")]
    private sealed class TAssociation : TElement
    {
        public static readonly TAssociation Instance = new();

        [Delphi(PortStatus.Ported, Symbol = "TAssociation.Write")]
        public override void Write(TMoldElement element, TWriter w)
        {
            var a = (TMoldAssociation)element;
            w.PutStartBlock("Association");
            base.Write(element, w);
            w.PutClassReference(a.LinkClass);
            w.PutQuotedString(a.Stereotype);
            w.PutQuotedString(a.Constraints.CommaText);
            w.PutQuotedString(a.NonDefaultTaggedValuesCommaText);
            w.PutBoolean(a.Derived);
            w.PutStartBlock("Roles");
            TElementList.Write(a.Roles, w);
            w.PutEndBlock();
            w.PutEndBlock();
        }

        [Delphi(PortStatus.Ported, Symbol = "TAssociation.Read")]
        public override void Read(TMoldElement element, TReader r)
        {
            var a = (TMoldAssociation)element;
            base.Read(element, r);
            int v = r.FormatVersion;
            if (v >= 2)
            {
                a.LinkClass = r.GetClassReference();
            }

            if (v >= 16)
            {
                a.Stereotype = r.GetQuotedString();
                a.Constraints.CommaText = r.GetQuotedString();
            }

            // Older versions did not store persistence for associations.
            a.SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);
            if (v >= 17)
            {
                a.NonDefaultTaggedValuesCommaText = r.GetQuotedString();
            }

            if (v >= 19)
            {
                a.Derived = r.GetBoolean();
            }

            r.EatStartBlock("Roles");
            TElementList.Read(element, r);
            r.Eat(BLDTokenKind.RPAR);
            r.Eat(BLDTokenKind.RPAR);
        }
    }

    private static readonly TElementClassRecord[] TypeTable =
    [
        new(TModel.Instance, typeof(TMoldModel), "Model"),
        new(TPClass.Instance, typeof(TMoldClass), "Class"),
        new(TAttribute.Instance, typeof(TMoldAttribute), "Attribute"),
        new(TMethod.Instance, typeof(TMoldMethod), "Method"),
        new(TAssociation.Instance, typeof(TMoldAssociation), "Association"),
        new(TRole.Instance, typeof(TMoldRole), "Role"),
        new(TQualifier.Instance, typeof(TMoldQualifier), "Qualifier"),
    ];

    [Delphi(PortStatus.Ported, Symbol = "TypeTableByMoldElement")]
    private static TElementClassRecord TypeTableByMoldElement(TMoldElement moldElement)
    {
        foreach (TElementClassRecord record in TypeTable)
        {
            if (record.MoldElementClass.IsInstanceOfType(moldElement))
            {
                return record;
            }
        }

        throw new EBoldInternal("TypeTableByMoldElement: moldElementClass not found in typetable");
    }

    [Delphi(PortStatus.Ported, Symbol = "TypeTableByName")]
    private static TElementClassRecord TypeTableByName(string Name)
    {
        foreach (TElementClassRecord record in TypeTable)
        {
            if (record.Name == Name)
            {
                return record;
            }
        }

        throw new EBoldInternal(SysUtils.Format("TypeTableByName: %s not found in typetable", Name));
    }
}

/// <summary>Unit-level routines of BoldBld.</summary>
public static partial class BoldBld
{
    /// <summary>"*" for a multi end, "1" otherwise; for files before version 16, which stored a boolean.</summary>
    [Delphi(PortStatus.Ported)]
    public static string BooleanToMultiplicityString(bool Value) => Value ? "*" : "1";
}
