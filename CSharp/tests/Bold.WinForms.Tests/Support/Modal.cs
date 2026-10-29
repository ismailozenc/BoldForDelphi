using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>Acts on a modal form while it shows: a timer finds the form once it is visible and runs the action.</summary>
internal static class Modal
{
    /// <summary>Starts watching for a form of the type; the action runs once, inside the modal loop.</summary>
    public static IDisposable WhenShown<T>(Action<T> action)
        where T : Form
    {
        var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            if (Application.OpenForms.OfType<T>().LastOrDefault() is { Visible: true } form)
            {
                timer.Stop();
                action(form);
            }
        };
        timer.Start();
        return timer;
    }

    /// <summary>Raises a protected event method of a control (OnClick, OnDoubleClick and the like).</summary>
    public static void Raise(Control control, string method, EventArgs e) =>
        typeof(Control).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, [e.GetType()])!.Invoke(control, [e]);
}
