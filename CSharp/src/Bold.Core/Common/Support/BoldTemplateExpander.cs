// Delphi: Source/Common/Support/BoldTemplateExpander.pas
// Port map: CSharp/port/units/BoldTemplateExpander.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldTemplateExpander", Symbol = "TBoldVariableFlags", Note = "A DelphiSet<int> of the bits 0 to 31.")]

namespace Bold;

/// <summary>A named template variable with a value and flags.</summary>
[Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariable")]
public class TBoldTemplateVariable : TBoldMemoryManagedObject
{
    private readonly string fName;
    private string fValue;
    private readonly DelphiSet<int> fFlags;

    [Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariable.Create")]
    public TBoldTemplateVariable(string name, string value, DelphiSet<int> Flags)
    {
        fFlags = Flags;
        fName = name;
        fValue = value;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariable.Name")] public string Name => fName;

    [Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariable.Value")]
    public string Value
    {
        get => fValue;
        set => fValue = value;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariable.Flags")] public DelphiSet<int> Flags => fFlags;
}

/// <summary>The variables of a template, found by name ignoring case.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTemplateVariables : TBoldUnOrderedIndexableList
{
    private static int IX_TemplateVariables = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldTemplateVariables()
    {
        BoldIndexableList.SetIndexVariable(ref IX_TemplateVariables, AddIndex(new TBoldTemplatevariableIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldTemplateVariable? GetVariableByName(string name) =>
        (TBoldTemplateVariable?)((TBoldTemplatevariableIndex)Indexes(IX_TemplateVariables)!).FindByString(name);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected TBoldTemplateVariable? VariableByName(string name) => GetVariableByName(name);

    /// <summary>Adds the variable unless one with the name exists.</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(string Name, string Value, DelphiSet<int> Flags)
    {
        if (VariableByName(Name) is null)
        {
            ForceAdd(Name, Value, Flags);
        }
    }

    /// <summary>Adds the variable even if one with the name exists (the first one added is the one found).</summary>
    [Delphi(PortStatus.Ported)]
    public void ForceAdd(string Name, string Value, DelphiSet<int> Flags) => base.Add(new TBoldTemplateVariable(Name, Value, Flags));

    /// <summary>Changes the value of an existing variable; does nothing when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    public void Change(string Name, string Value)
    {
        TBoldTemplateVariable? Variable = VariableByName(Name);
        if (Variable is not null)
        {
            Variable.Value = Value;
        }
    }

    /// <summary>Changes the value, adding the variable when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetVariable(string Name, string Value)
    {
        TBoldTemplateVariable? variable = VariableByName(Name);
        if (variable is not null)
        {
            variable.Value = Value;
        }
        else
        {
            ForceAdd(Name, Value, DelphiSet<int>.Empty);
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool Exists(string Name) => VariableByName(Name) is not null;

    /// <summary>Expands the text with these variables, after updating DATETIME, DATE and TIME.</summary>
    [Delphi(PortStatus.Ported)]
    public string ExpandString(string Source)
    {
        Change("DATETIME", SysUtils.DateTimeToStr(DateTime.Now));
        Change("DATE", SysUtils.DateToStr(DateTime.Now));
        Change("TIME", SysUtils.TimeToStr(DateTime.Now));
        return BoldTemplateExpander.BoldExpandTemplate(Source, this);
    }

    [Delphi(PortStatus.Ported)]
    public void Remove(string Name)
    {
        TBoldTemplateVariable? variable = VariableByName(Name);
        if (variable is not null)
        {
            base.Remove(variable);
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetValue(string Name) => VariableByName(Name)?.Value ?? "";

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public string Values(string Name) => GetValue(Name);

    /// <summary>Sets DATETIME, DATE and TIME to now.</summary>
    [Delphi(PortStatus.Ported)]
    internal void InitializeDateTimeMacros()
    {
        SetVariable("DATETIME", SysUtils.DateTimeToStr(DateTime.Now));
        SetVariable("DATE", SysUtils.DateToStr(DateTime.Now));
        SetVariable("TIME", SysUtils.TimeToStr(DateTime.Now));
    }

    [Delphi(PortStatus.Ported)]
    private sealed class TBoldTemplatevariableIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldTemplatevariableIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldTemplateVariable)Item!).Name;
    }
}

/// <summary>A template text with a file name, a description and its variables; expanding it gives the text to write.</summary>
[Delphi(PortStatus.Ported, Note = "A System.ComponentModel.Component.")]
public class TBoldTemplateHolder : System.ComponentModel.Component
{
    private readonly TStringList fTemplate;
    private readonly TStringList fExpandedTemplate;
    private readonly TBoldTemplateVariables fVariables;
    private string FDescription = "";
    private string FFileName = "";

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldTemplateHolder(System.ComponentModel.IContainer? aOwner = null)
    {
        aOwner?.Add(this);
        fVariables = new TBoldTemplateVariables();
        fTemplate = new TStringList();
        fTemplate.OnChange = TemplateChanged;
        fExpandedTemplate = new TStringList();
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldTemplateHolder.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    /// <summary>Expands the template with the variables, trimmed, into the last expansion.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExpandTemplate() => fExpandedTemplate.Text = SysUtils.Trim(Variables.ExpandString(Template.Text));

    [Delphi(PortStatus.Ported)]
    private void SetTemplate(TStringList Value) => fTemplate.Assign(Value);

    [Delphi(PortStatus.Ported)]
    private TStringList GetExpandedTemplate()
    {
        ExpandTemplate();
        return fExpandedTemplate;
    }

    [Delphi(PortStatus.Ported)]
    private string GetExpandedFileName() => BoldTemplateExpander.BoldExpandTemplate(FileName, Variables);

    [Delphi(PortStatus.Ported)]
    private void SetDescription(string Value) => FDescription = Value;

    [Delphi(PortStatus.Ported)]
    private void SetFileName(string Value) => FFileName = Value;

    /// <summary>Drops trailing empty lines when the template changes.</summary>
    [Delphi(PortStatus.Ported)]
    private void TemplateChanged(object? sender)
    {
        while (fTemplate.Count > 0 && SysUtils.Trim(fTemplate[fTemplate.Count - 1]) == "")
        {
            fTemplate.Delete(fTemplate.Count - 1);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldTemplateVariables Variables => fVariables;
    [Delphi(PortStatus.Ported)] public TStringList ExpandedTemplate => GetExpandedTemplate();
    [Delphi(PortStatus.Ported)] public TStringList LastExpansion => fExpandedTemplate;
    [Delphi(PortStatus.Ported)] public string ExpandedFileName => GetExpandedFileName();

    [Delphi(PortStatus.Ported)]
    public TStringList Template
    {
        get => fTemplate;
        set => SetTemplate(value);
    }

    [Delphi(PortStatus.Ported)]
    public string FileName
    {
        get => FFileName;
        set => SetFileName(value);
    }

    [Delphi(PortStatus.Ported)]
    public string Description
    {
        get => FDescription;
        set => SetDescription(value);
    }
}

/// <summary>A list of template holders.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTemplateList : TList
{
    [Delphi(PortStatus.Ported)]
    private TBoldTemplateHolder GetItems(int index) => (TBoldTemplateHolder)base[index]!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldTemplateList.items")]
    public new TBoldTemplateHolder this[int index] => GetItems(index);
}

/// <summary>Unit-level constants and routines of BoldTemplateExpander.</summary>
public static class BoldTemplateExpander
{
    [Delphi(PortStatus.Ported)] public const int bvfReadOnly = 0;
    [Delphi(PortStatus.Ported)] public const int bvfPersistent = 1;

    /// <summary>The template value of a Boolean: '0' or '1'.</summary>
    [Delphi(PortStatus.Ported, Note = "An array indexed by Boolean in Delphi.")]
    public static string BoldBooleanToTemplateVar(bool Value) => Value ? "1" : "0";

    private const string MacroBegin = "$(";
    private const string MacroEnd = ")";
    private const string MacroModifiers = ",+-<>?";

    /// <summary>
    /// Expands the macros of the template language in <paramref name="Source"/>:
    /// <list type="bullet">
    /// <item><c>$(NAME)</c> is the variable's value, itself expanded; inside a loop, a name not found with the loop
    /// suffix (NAME.0.1) is looked up in the outer scopes (NAME.0, NAME).</item>
    /// <item><c>$(+NAME)</c> and <c>$(-NAME)</c> are the value in upper and lower case; <c>$(&lt;20NAME)</c> and
    /// <c>$(&gt;20NAME)</c> pad it right or left to 20 characters.</item>
    /// <item><c>$(,NAME:text)</c> gives the text unless the value is 0; <c>$(?NAME:yes,no)</c> gives yes when the
    /// value is 1, else no.</item>
    /// <item><c>$(LOOPNAME)</c> ... <c>$(ENDLOOPNAME)</c> repeats the text NAME times, with the loop index added to
    /// the names inside.</item>
    /// <item><c>$(CASENAME)</c> ... <c>$(ENDCASENAME)</c> keeps the lines tagged <c>value|</c> whose value is the
    /// variable's (or the ELSE lines).</item>
    /// <item><c>$(MACROSTART NAME)</c> ... <c>$(MACROEND NAME)</c> defines a variable; <c>$(CRLF)</c> is a line break.</item>
    /// </list>
    /// A backslash at the end of a line joins it with the next.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "A justification macro with no name ($(<20)) gives an empty value; Delphi read past the end of the name.")]
    public static string BoldExpandTemplate(string Source, TBoldTemplateVariables Variables, string MacroNamePad = "")
    {
        string GetVarvalue(string MacroName)
        {
            string result = Variables.Values(MacroName);
            while (result == "" && SysUtils.Pos(".", MacroName) != 0)
            {
                // The variable might belong to an outer loop scope.
                MacroName = MacroName[..MacroName.LastIndexOf('.')];
                result = Variables.Values(MacroName);
            }

            return result;
        }

        Variables.InitializeDateTimeMacros();
        string TempSource = Source;
        var Result = new System.Text.StringBuilder();
        int Mb = SysUtils.Pos(MacroBegin, TempSource);
        while (Mb > 0)
        {
            string Temp = SysUtils.Copy(TempSource, Mb + 2, SysUtils.MaxInt);
            int Me = SysUtils.Pos(MacroEnd, Temp);
            if (Me <= 0)
            {
                break;
            }

            Result.Append(SysUtils.Copy(TempSource, 1, Mb - 1));
            // The name pad is added to ?-macros later.
            string MacroName = Temp[0] is not ('?' or ',') ? SysUtils.Copy(Temp, 1, Me - 1) + MacroNamePad : SysUtils.Copy(Temp, 1, Me - 1);
            if (MacroName.Length > 0 && MacroModifiers.Contains(MacroName[0]))
            {
                char Modifier = MacroName[0];
                MacroName = SysUtils.Copy(MacroName, 2, SysUtils.MaxInt);
                switch (Modifier)
                {
                    case ',':
                    {
                        // Separation
                        string Value = SysUtils.Copy(MacroName, SysUtils.Pos(":", MacroName) + 1, SysUtils.MaxInt);
                        MacroName = SysUtils.Copy(MacroName, 1, SysUtils.Pos(":", MacroName) - 1);
                        if (GetVarvalue(MacroName + MacroNamePad) != "0")
                        {
                            Result.Append(Value);
                        }

                        break;
                    }

                    case '+':
                        Result.Append(SysUtils.AnsiUpperCase(GetVarvalue(MacroName)));
                        break;
                    case '-':
                        Result.Append(SysUtils.AnsiLowerCase(GetVarvalue(MacroName)));
                        break;
                    case '<':
                    case '>':
                    {
                        string Number = "";
                        while (MacroName.Length > 0 && MacroName[0] is >= '0' and <= '9')
                        {
                            Number += MacroName[0];
                            MacroName = SysUtils.Copy(MacroName, 2, SysUtils.MaxInt);
                        }

                        int J = SysUtils.StrToIntDef(Number, 0);
                        string MacroValue = GetVarvalue(MacroName);
                        string Pad = J > 0 && J > MacroValue.Length
                            ? Modifier == '<' ? MacroValue + SysUtils.StringOfChar(' ', J - MacroValue.Length) : SysUtils.StringOfChar(' ', J - MacroValue.Length) + MacroValue
                            : MacroValue;
                        Result.Append(Pad);
                        break;
                    }

                    case '?':
                    {
                        // If: $(?varname:true,false)
                        int ColonPos = SysUtils.Pos(":", MacroName);
                        int CommaPos = SysUtils.Pos(",", MacroName);
                        if (GetVarvalue(SysUtils.Copy(MacroName, 1, ColonPos - 1) + MacroNamePad) == "1")
                        {
                            Result.Append(SysUtils.Copy(MacroName, ColonPos + 1, CommaPos - ColonPos - 1));
                        }
                        else
                        {
                            Result.Append(SysUtils.Copy(MacroName, CommaPos + 1, SysUtils.MaxInt));
                        }

                        break;
                    }
                }
            }
            else
            {
                int EndLoopPos = 0;
                if (SysUtils.Pos("LOOP", MacroName) == 1)
                {
                    MacroName = SysUtils.Copy(MacroName, 5, SysUtils.MaxInt);
                    string MatchMacroName = MacroName;
                    if (SysUtils.Pos(".", MatchMacroName) != 0)
                    {
                        MatchMacroName = MatchMacroName[..MatchMacroName.IndexOf('.')];
                    }

                    MatchMacroName = SysUtils.UpperCase(MacroBegin + "ENDLOOP" + MatchMacroName + MacroEnd);
                    EndLoopPos = SysUtils.Pos(MatchMacroName, SysUtils.UpperCase(Temp));
                    if (EndLoopPos == 0)
                    {
                        throw new Exception(SysUtils.Format(BoldCommonConst.sUnterminatedLoop, MacroName));
                    }

                    string LoopText = SysUtils.Copy(Temp, Me + 1, EndLoopPos - Me - 1);
                    Me = EndLoopPos + MatchMacroName.Length - 1;
                    int OldLoopValue = SysUtils.StrToIntDef(GetVarvalue(MacroName), 0);
                    for (int i = 0; i < OldLoopValue; i++)
                    {
                        Variables.SetVariable(MacroName, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        Result.Append(BoldExpandTemplate(LoopText, Variables, MacroNamePad + "." + i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    }

                    Variables.SetVariable(MacroName, OldLoopValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else if (SysUtils.Pos("CASE", MacroName) == 1)
                {
                    MacroName = SysUtils.Copy(MacroName, 5, SysUtils.MaxInt);
                    string MatchMacroName = MacroName;
                    if (SysUtils.Pos(".", MatchMacroName) != 0)
                    {
                        MatchMacroName = MatchMacroName[..MatchMacroName.IndexOf('.')];
                    }

                    MatchMacroName = SysUtils.UpperCase(MacroBegin + "ENDCASE" + MatchMacroName + MacroEnd);
                    EndLoopPos = SysUtils.Pos(MatchMacroName, SysUtils.UpperCase(Temp));
                    if (EndLoopPos == 0)
                    {
                        throw new Exception(SysUtils.Format(BoldCommonConst.sUnterminatedCase, MacroName));
                    }

                    var CaseText = new TStringList { Text = SysUtils.Trim(SysUtils.Copy(Temp, Me + 1, EndLoopPos - Me - 1)) };
                    string CaseValue = SysUtils.UpperCase(GetVarvalue(MacroName));
                    string ElseValue = "";
                    for (int i = CaseText.Count - 1; i >= 0; i--)
                    {
                        int BarPos = SysUtils.Pos("|", CaseText[i]);
                        if (BarPos != 0)
                        {
                            string CurrentCaseTag = SysUtils.UpperCase(BoldExpandTemplate(SysUtils.UpperCase(SysUtils.Trim(SysUtils.Copy(CaseText[i], 1, BarPos - 1))), Variables, MacroNamePad));
                            if (CurrentCaseTag != CaseValue)
                            {
                                if (CurrentCaseTag == "ELSE")
                                {
                                    ElseValue = SysUtils.Copy(CaseText[i], BarPos + 1, SysUtils.MaxInt) + BoldDefs.BOLDCRLF + ElseValue;
                                }

                                CaseText.Delete(i);
                            }
                            else
                            {
                                CaseText[i] = SysUtils.Copy(CaseText[i], BarPos + 1, SysUtils.MaxInt);
                                if (SysUtils.Trim(CaseText[i]) == "\\")
                                {
                                    CaseText.Delete(i);
                                }
                            }
                        }
                        else if (SysUtils.Trim(CaseText[i]) == "\\")
                        {
                            CaseText.Delete(i);
                        }
                    }

                    if (CaseText.Count == 0)
                    {
                        CaseText.Text = ElseValue;
                    }

                    Me = EndLoopPos + MatchMacroName.Length - 1;
                    Result.Append(BoldExpandTemplate(CaseText.Text, Variables, MacroNamePad));
                }
                else if (SysUtils.Pos("MACROSTART", MacroName) == 1)
                {
                    MacroName = SysUtils.Trim(SysUtils.Copy(MacroName, 11, SysUtils.MaxInt));
                    string MatchMacroName = MacroName;
                    if (SysUtils.Pos(".", MatchMacroName) != 0)
                    {
                        MatchMacroName = MatchMacroName[..MatchMacroName.IndexOf('.')];
                    }

                    MatchMacroName = SysUtils.UpperCase(MacroBegin + "MACROEND " + MatchMacroName + MacroEnd);
                    EndLoopPos = SysUtils.Pos(MatchMacroName, SysUtils.UpperCase(Temp));
                    if (EndLoopPos == 0)
                    {
                        throw new Exception(SysUtils.Format(BoldCommonConst.sUnterminatedMacro, MacroName));
                    }

                    string MacroText = SysUtils.Copy(Temp, Me + 1, EndLoopPos - Me - 1);
                    Me = EndLoopPos + MatchMacroName.Length - 1;
                    Variables.SetVariable(MacroName, MacroText);
                }

                if (SysUtils.Pos("CRLF", SysUtils.UpperCase(MacroName)) == 1)
                {
                    // In loops CRLF becomes CRLF.0.
                    Result.Append(BoldDefs.BOLDCRLF);
                }
                else if (EndLoopPos == 0)
                {
                    string value = Variables.Values(MacroName);
                    while (value == "" && SysUtils.Pos(".", MacroName) != 0)
                    {
                        // The variable seems to belong to an outer loop scope.
                        MacroName = MacroName[..MacroName.LastIndexOf('.')];
                        value = Variables.Values(MacroName);
                    }

                    Result.Append(BoldExpandTemplate(value, Variables, MacroNamePad));
                }
            }

            TempSource = SysUtils.Copy(Temp, Me + 1, SysUtils.MaxInt);
            Mb = SysUtils.Pos(MacroBegin, TempSource);
        }

        Result.Append(TempSource);
        string text = Result.ToString();
        int p = text.IndexOf("\\" + BoldDefs.BOLDCRLF, StringComparison.Ordinal);
        while (p >= 0)
        {
            text = text.Remove(p, 3);
            p = text.IndexOf("\\" + BoldDefs.BOLDCRLF, StringComparison.Ordinal);
        }

        return text;
    }
}
