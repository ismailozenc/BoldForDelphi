// Delphi: Source/Persistence/Core/BoldPersistenceNotifier.pas
// Port map: CSharp/port/units/BoldPersistenceNotifier.md
using System.Drawing;
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>The progress notifier's progress bar, messages and animation, driven by a persistence handle's events.</summary>
[NonParallelizable]
public class BoldPersistenceProgressNotifierTests
{
    /// <summary>A GIF of two 1x1 frames: an animated image.</summary>
    private static readonly byte[] TwoFrameGif =
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
        0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00,
        0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00,
        0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00,
        0x3B,
    ];

    private static TBoldObjectIdList Ids(int count)
    {
        var ids = new TBoldObjectIdList();
        for (int i = 0; i < count; i++)
        {
            ids.Add(new TBoldDefaultID(0, true) { AsInteger = i + 1 });
        }

        return ids;
    }

    /// <summary>Runs the message loop until the condition holds (or two seconds pass).</summary>
    private static bool PumpUntil(Func<bool> condition)
    {
        var watch = global::System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.ElapsedMilliseconds < 2000)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        return condition();
    }

    [Test]
    [Covers(typeof(TBoldPersistenceProgressNotifier), "*")]
    public void The_notifier_steps_a_progress_bar_shows_what_the_layer_does_and_plays_an_animation()
    {
        string file = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.xml");
        using var s = new GuiSample(activate: false);
        using var handle = new TBoldPersistenceHandleFileXML { FileName = file, BoldModel = s.Model };
        using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
        var bar = new ProgressBar { Visible = false };
        var label = new Label();
        var status = new GroupBox();
        using var gif = new MemoryStream(TwoFrameGif);
        var animation = new PictureBox { Image = Image.FromStream(gif), Enabled = false };
        form.Controls.AddRange([bar, label, status, animation]);
        form.Show();
        using var notifier = new TBoldPersistenceProgressNotifier
        {
            ProgressBar = bar,
            MsgLabel = label,
            WinControl = status,
            Animation = animation,
            AnimationInterval = 20,
            PersistenceHandle = handle,
        };
        Assert.That(notifier.AnimationInterval, Is.EqualTo(20));
        Assert.That(notifier.MsgUpdateDatabase, Is.EqualTo(PersistenceConsts.sUpdatingDB));
        Assert.That(notifier.MsgFetchObjects, Is.EqualTo(PersistenceConsts.sFetchingObjects));
        Assert.That(notifier.MsgRetrieveIds, Is.EqualTo(PersistenceConsts.sRetrievingIDs));
        Assert.That((notifier.ProgressBar, notifier.MsgLabel, notifier.WinControl, notifier.Animation), Is.EqualTo((bar, label, (Control)status, animation)));
        Assert.That(bar.Step, Is.EqualTo(1));
        // The handle passes on the events of its controller and of the persistence mapper; here it sends them itself.
        void Send(int bpe, params object?[] args) => handle.SendExtendedEvent(handle, bpe, args);

        // An update of three objects: the bar shows and steps per object, the message says so, the animation plays.
        Send(BoldSubscription.bpeStartUpdate, Ids(3));
        Assert.That(bar.Visible, Is.True);
        Assert.That((bar.Value, bar.Maximum), Is.EqualTo((0, 3)));
        Assert.That(label.Text, Is.EqualTo(PersistenceConsts.sUpdatingDB));
        Assert.That(status.Text, Is.EqualTo(PersistenceConsts.sUpdatingDB));
        Assert.That(animation.Enabled, Is.True, "the animation plays");
        Send(BoldSubscription.bpeUpdateObject);
        Send(BoldSubscription.bpeCreateObject);
        Send(BoldSubscription.bpeDeleteObject);
        Assert.That(bar.Value, Is.EqualTo(3));
        Send(BoldSubscription.bpeEndUpdate);
        Assert.That(bar.Visible, Is.False);
        Assert.That(bar.Maximum, Is.EqualTo(0));
        Assert.That(label.Text, Is.Empty);
        Assert.That(status.Text, Is.Empty);
        Assert.That(PumpUntil(() => !animation.Enabled), Is.True, "the animation stops a little after the layer is done");

        // A fetch of two objects.
        notifier.MsgFetchObjects = "Fetching";
        Send(BoldSubscription.bpeStartFetch, Ids(2));
        Assert.That(label.Text, Is.EqualTo("Fetching"));
        Assert.That(bar.Maximum, Is.EqualTo(2));
        Send(BoldSubscription.bpeFetchObject);
        Assert.That(bar.Value, Is.EqualTo(1));
        Send(BoldSubscription.bpeEndFetch);
        Assert.That(bar.Visible, Is.False);
        Assert.That(PumpUntil(() => !animation.Enabled), Is.True);

        // Retrieving ids and fetching members play the animation.
        notifier.MsgRetrieveIds = "Ids";
        Send(BoldSubscription.bpeStartFetchID);
        Assert.That(label.Text, Is.EqualTo("Ids"));
        Assert.That(animation.Enabled, Is.True);
        Send(BoldSubscription.bpeFetchId);
        Send(BoldSubscription.bpeEndFetchID);
        Assert.That(label.Text, Is.Empty);
        Assert.That(PumpUntil(() => !animation.Enabled), Is.True);
        Send(BoldSubscription.bpeFetchMember);
        Assert.That(animation.Enabled, Is.True);

        // A still image does not play.
        animation.Enabled = false;
        animation.Image = new Bitmap(1, 1);
        Send(BoldSubscription.bpeFetchMember);
        Assert.That(animation.Enabled, Is.False, "nothing to play");

        // Controls that go are forgotten.
        bar.Dispose();
        label.Dispose();
        status.Dispose();
        animation.Dispose();
        Assert.That(notifier.ProgressBar, Is.Null);
        Assert.That(notifier.MsgLabel, Is.Null);
        Assert.That(notifier.WinControl, Is.Null);
        Assert.That(notifier.Animation, Is.Null);
        Assert.DoesNotThrow(() => Send(BoldSubscription.bpeStartUpdate, Ids(1)));
        Assert.DoesNotThrow(() => Send(BoldSubscription.bpeEndUpdate));
        File.Delete(file);
    }
}
