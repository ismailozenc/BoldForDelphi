using System.Drawing;

namespace Bold.Tests;

/// <summary>The emulation of Delphi's GUI units the Bold-aware GUI relies on: Graphics, ActnList, Dialogs and Controls.</summary>
[NonParallelizable]
public class GuiEmulationTests
{
    [Test]
    public void Fonts_tell_of_changes_and_copy_from_each_other()
    {
        int changes = 0;
        var font = new TFont { OnChange = _ => changes++ };
        font.Size = 12;
        font.Size = 12;
        font.Style = DelphiSet<TFontStyle>.Of(TFontStyle.fsBold);
        Assert.That(changes, Is.EqualTo(2), "only a real change tells");

        var copy = new TFont();
        copy.Assign(font);
        Assert.That((copy.Size, copy.Style.Contains(TFontStyle.fsBold), copy.Name), Is.EqualTo((12, true, font.Name)));
        var brush = new TBrush { Color = Color.Red };
        var brush2 = new TBrush();
        brush2.Assign(brush);
        Assert.That(brush2.Color, Is.EqualTo(Color.Red));
        Assert.That(() => brush2.Assign(font), Throws.InstanceOf<EConvertError>());
    }

    private sealed class CountingAction : TAction
    {
        public bool Handles = true;
        public int Updates, Executions;

        public override bool HandlesTarget(object? Target) => Handles;

        public override void UpdateTarget(object? Target) => Updates++;

        public override void ExecuteTarget(object? Target) => Executions++;
    }

    [Test]
    public void Actions_update_before_they_execute_and_a_disabled_one_does_nothing()
    {
        var action = new CountingAction { Caption = "Go" };
        int shown = 0;
        action.Changed += (_, _) => shown++;
        action.Caption = "Run";
        Assert.That(shown, Is.EqualTo(1));

        Assert.That(action.Execute(), Is.True);
        Assert.That((action.Updates, action.Executions), Is.EqualTo((1, 1)), "execute updates first");
        action.Enabled = false;
        Assert.That(action.Execute(), Is.False);
        Assert.That(action.Executions, Is.EqualTo(1));

        action.Enabled = true;
        action.Handles = false;
        Assert.That(action.Execute(), Is.False, "a target it does not handle");
        int executed = 0;
        action.OnExecute = _ => executed++;
        Assert.That(action.Execute(), Is.True, "OnExecute runs for any target");
        Assert.That(executed, Is.EqualTo(1));
        action.OnUpdate = _ => action.Enabled = false;
        Assert.That(action.Execute(), Is.False, "OnUpdate disabled it");

        using var container = new System.ComponentModel.Container();
        var named = new TAction(container) { Name = "Save" };
        Assert.That(named.Name, Is.EqualTo("Save"));
        Assert.That(container.Components.Count, Is.EqualTo(1));
    }

    [Test]
    public void Message_dialogs_need_a_GUI_toolkit_to_answer()
    {
        Assert.That(Dialogs.MessageDlgHandler, Is.Null);
        Assert.That(() => Dialogs.MessageDlg("Save?", TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0),
            Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("Save?"));

        var asked = new List<string>();
        Dialogs.MessageDlgHandler = (msg, type, buttons, _) =>
        {
            asked.Add($"{type} {msg} {buttons.Contains(TMsgDlgBtn.mbNo)}");
            return Controls.mrNo;
        };
        try
        {
            Assert.That(Dialogs.MessageDlg("Save?", TMsgDlgType.mtConfirmation, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0), Is.EqualTo(Controls.mrNo));
            Dialogs.ShowMessage("Done");
            Assert.That(asked, Is.EqualTo(new[] { "mtConfirmation Save? True", "mtCustom Done False" }));
        }
        finally
        {
            Dialogs.MessageDlgHandler = null;
        }

        Assert.That((Controls.mrOk, Controls.mrCancel, Controls.mrYes, Controls.mrNo), Is.EqualTo((1, 2, 6, 7)), "Delphi's modal results");
    }

    [Test]
    public void A_dotnet_list_serves_as_strings()
    {
        var items = new List<object> { "a", 2 };
        var strings = new TListStrings(items);
        Assert.That(strings.Count, Is.EqualTo(2));
        Assert.That(strings[1], Is.EqualTo("2"));
        strings.InsertObject(0, "z", this);
        Assert.That(items, Is.EqualTo(new object[] { "z", "a", 2 }));
        Assert.That(strings.Objects(0), Is.Null, "objects are not kept");
        strings[1] = "b";
        strings.Delete(2);
        Assert.That(items, Is.EqualTo(new object[] { "z", "b" }));
        strings.Text = "x" + SysUtils.sLineBreak + "y";
        Assert.That(items, Is.EqualTo(new object[] { "x", "y" }));
        strings.Clear();
        Assert.That(strings.List, Is.Empty);

        var list = new TStringList();
        list.InsertObject(0, "one", items);
        Assert.That(list.Objects(0), Is.SameAs(items));
    }
}
