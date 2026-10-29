// Delphi: Source/ObjectSpace/UtilsGUI/BoldOCLGraphicRTDebug.pas
// Port map: CSharp/port/units/BoldOCLGraphicRTDebug.md

using System.Windows.Forms;
using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>
/// The run-time OCL debugger with a user interface: an expression that fails is shown in the OCL editor, and the fix
/// the user makes is used from then on for that expression in that context.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOclGraphicRTDebugger : TBoldOclRTDebugger, IDisposable
{
    private TStringList? FOrgExprs;
    private TStringList? FFixedExprs;

    [Delphi(PortStatus.Ported)]
    public TBoldOclGraphicRTDebugger()
    {
        FOrgExprs = new TStringList();
        FFixedExprs = new TStringList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOclGraphicRTDebugger.Destroy")]
    public void Dispose()
    {
        FOrgExprs = null;
        FFixedExprs = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>The index of the string, comparing case.</summary>
    [Delphi(PortStatus.Ported, Symbol = "CaseSensitiveIndexOf", Note = "A routine of the unit, here a static method of the debugger.")]
    internal static int CaseSensitiveIndexOf(TStringList Strings, string Str)
    {
        for (int Result = 0; Result < Strings.Count; Result++)
        {
            if (Strings[Result] == Str)
            {
                return Result;
            }
        }

        return -1;
    }

    /// <summary>Tells the user about the failing expression and lets the user fix it in the OCL editor; true when fixed.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool AddFixFor(string Ocl, TBoldElementTypeInfo? Context, string ComponentPath, string Message)
    {
        bool result;
        BoldOcl.BoldOCLRTDebugger = null;
        try
        {
            using var FixForm = new TBoldOclPropEditForm();
            FixForm.Context = Context;
            FixForm.OclExpr = Ocl;
            FixForm.Text = sIllegalExpressionEncountered;
            FixForm.ExpressionPage.Text = SysUtils.Format(sInComponent, ComponentPath);
            Dialogs.ShowMessage(SysUtils.Format(sIncorrectMessage, Ocl, BoldDefs.BOLDCRLF, ComponentPath, BoldDefs.BOLDCRLF, Message, BoldDefs.BOLDCRLF, Context?.AsString ?? ""));
            if (ShowFixForm(FixForm) == DialogResult.OK && Ocl != FixForm.OclExpr)
            {
                int OldPos = CaseSensitiveIndexOf(OrgExprs, MatchString(Context, Ocl));
                if (OldPos != -1)
                {
                    FixedExprs[OldPos] = FixForm.OclExpr;
                }
                else
                {
                    FixedExprs.Add(FixForm.OclExpr);
                    OrgExprs.Add(MatchString(Context, Ocl));
                }

                result = true;
            }
            else
            {
                result = false;
            }
        }
        finally
        {
            BoldOcl.BoldOCLRTDebugger = this;
        }

        return result;
    }

    /// <summary>Shows the editor modally (tests replace it).</summary>
    internal Func<TBoldOclPropEditForm, DialogResult> ShowFixForm { get; set; } = form => form.ShowDialog();

    /// <summary>The fix of the expression in the context, following fixes of fixes; the expression when it has none.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi looked up the original expression again on each step, so a fixed expression looped forever; this follows the fix, and stops at a cycle.")]
    public override string GetFixFor(string Ocl, TBoldElementTypeInfo? Context)
    {
        string result = Ocl;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int pos = CaseSensitiveIndexOf(OrgExprs, MatchString(Context, Ocl));
        while (pos != -1 && seen.Add(result))
        {
            result = FixedExprs[pos];
            pos = CaseSensitiveIndexOf(OrgExprs, MatchString(Context, result));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override bool HasFixFor(string Ocl, TBoldElementTypeInfo? Context) => CaseSensitiveIndexOf(OrgExprs, MatchString(Context, Ocl)) != -1;

    /// <summary>The key of an expression in a context.</summary>
    [Delphi(PortStatus.Ported)]
    private static string MatchString(TBoldElementTypeInfo? Context, string Ocl) =>
        Context is not null ? Context.AsString + "." + Ocl : "<no context>." + Ocl; //do not localize

    [Delphi(PortStatus.Ported)]
    protected TStringList OrgExprs => FOrgExprs!;

    [Delphi(PortStatus.Ported)]
    protected TStringList FixedExprs => FFixedExprs!;
}
