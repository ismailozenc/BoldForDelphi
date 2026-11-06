// The model editor as a program. Delphi's editor ran in the IDE on a model component of the form being designed; here
// it edits a model component of its own, read from the file given on the command line (a .bld or an XMI file).

using System.Windows.Forms;

namespace Bold;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        BoldWinForms.Initialize();
        TBoldUMLBldLink.EnsureRegistered();
        using var model = new TBoldModel();
        TModelEdit editor = TModelEdit.UMLModelEditor();
        TBoldModelEditFrm form = editor.ShowEditFormForBoldModel(model);
        if (args.Length > 0)
        {
            string fileName = Path.GetFullPath(args[0]);
            if (!File.Exists(fileName))
            {
                Dialogs.ShowMessage($"There is no file {fileName}.");
            }
            else if (form.Controller.OpenFile(fileName))
            {
                form.Text = $"{form.Text} - {Path.GetFileName(fileName)}";
            }
            else
            {
                Dialogs.ShowMessage($"Could not read the model in {fileName}; the log says why.");
            }

            form.RefreshAll();
        }

        Application.Run(form);
        TModelEdit.Finalize_();
        return 0;
    }
}
