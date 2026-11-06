using System.Drawing;
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>No message dialog shows during the tests: one the test does not answer fails it.</summary>
[SetUpFixture]
public class EditorDialogsSetup
{
    [OneTimeSetUp]
    public void NoDialogs()
    {
        BoldWinForms.Initialize();
        Dialogs.MessageDlgHandler = (msg, _, _, _) => throw new InvalidOperationException("A dialog the test did not expect: " + msg);
    }
}

/// <summary>The model editor's form in the tests: shown off the screen, with the plug-ins, as a user sees it.</summary>
internal static class EditorForms
{
    /// <summary>The editor's form on the model component, shown off the screen.</summary>
    public static TBoldModelEditFrm Open(TBoldModel model)
    {
        var form = new TBoldModelEditFrm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000) };
        form.ModelHandle = model;
        form.PlugInList = TModelEdit.UMLModelEditor().PlugInList;
        form.Show();
        Application.DoEvents();
        return form;
    }

    /// <summary>The node at the path of captions below the root ("Logical View", "Person").</summary>
    public static TreeNode Node(TBoldModelEditFrm form, params string[] path)
    {
        TreeNode node = form.BoldTreeView1.Nodes[0];
        foreach (string caption in path)
        {
            node = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == caption)
                ?? throw new AssertionException($"No node {caption} below {node.Text}; there are {string.Join(", ", Texts(node))}");
        }

        return node;
    }

    /// <summary>The class "Customer" of a small model (EditorSamples.Shop).</summary>
    public static TBoldUMLObject Customer() => EditorSamples.Shop().Classes.Single(c => c.name == "Customer");

    /// <summary>Closes the forms a test left open.</summary>
    public static void CloseAll()
    {
        foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
        {
            form.Dispose();
        }
    }

    /// <summary>The captions of the nodes below a node.</summary>
    public static string[] Texts(TreeNode node) => [.. node.Nodes.Cast<TreeNode>().Select(n => n.Text)];

    /// <summary>Selects the node, as a click on it does.</summary>
    public static void Select(TBoldModelEditFrm form, TreeNode node)
    {
        form.BoldTreeView1.SelectedNode = node;
        Application.DoEvents();
    }

    /// <summary>Selects the node of the element.</summary>
    public static void Select(TBoldModelEditFrm form, TBoldUMLObject element)
    {
        form.JumpToElement(form, element);
        Application.DoEvents();
    }

    /// <summary>The control of the name (a field of a page, a list on a page's tabs).</summary>
    public static T Field<T>(TBoldModelEditFrm form, string name)
        where T : Control => (T)form.Controls.Find(name, true).Single();

    /// <summary>Raises a protected event method of a control or of its class (OnCellDoubleClick, OnSelectionChangeCommitted and the like).</summary>
    public static void Raise(Control control, string method, EventArgs e) =>
        (control.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, [e.GetType()])
            ?? throw new MissingMethodException(control.GetType().Name, method)).Invoke(control, [e]);
}
