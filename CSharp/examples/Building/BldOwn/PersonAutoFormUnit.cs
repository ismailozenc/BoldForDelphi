// The form persons show in (Delphi: examples/Delphi/Compound/Building/PersonAutoFormUnit.pas), plugged into the auto
// forms by the data module's TBoldPlaceableAFP.
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Bold;
using static BldOwn.Datamod;

namespace BldOwn;

/// <summary>A person's names, assets and marriage, and the buildings the person owns; it goes when the person goes.</summary>
public class TPersonAutoForm : Form
{
    private readonly Container components = new();

    public readonly TBoldEdit BoldEdit1;
    public readonly TBoldEdit beFirstName;
    public readonly TBoldEdit BoldEdit7;
    public readonly TBoldCheckBox bcbIsMarried;
    public readonly TBoldGrid bgrOwnedBuildings;

    /// <summary>The person (the pluggable form provider sets it).</summary>
    public readonly TBoldReferenceHandle brhPerson;
    public readonly TBoldListHandle blhOwnedBuildings;

    public TPersonAutoForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(307, 380);
        Text = "Person Detail";
        StartPosition = FormStartPosition.CenterScreen;

        brhPerson = new TBoldReferenceHandle(components)
        {
            Name = "brhPerson",
            StaticSystemHandle = DataModule1.BoldSystemHandle1,
            StaticValueTypeName = "Person",
            OnObjectDeleted = brhPersonObjectDeleted,
            OnValueDestroyed = brhPersonObjectDeleted,
        };
        blhOwnedBuildings = new TBoldListHandle(components) { Name = "blhOwnedBuildings", RootHandle = brhPerson, Expression = "ownedBuildings" };

        beFirstName = new TBoldEdit { Name = "beFirstName", Bounds = new Rectangle(8, 23, 121, 21) };
        beFirstName.BoldHandle = brhPerson;
        beFirstName.BoldProperties.Expression = "firstName";
        BoldEdit1 = new TBoldEdit { Name = "BoldEdit1", Bounds = new Rectangle(8, 67, 121, 21) };
        BoldEdit1.BoldHandle = brhPerson;
        BoldEdit1.BoldProperties.Expression = "lastName";
        BoldEdit7 = new TBoldEdit { Name = "BoldEdit7", Bounds = new Rectangle(136, 23, 121, 21) };
        BoldEdit7.BoldHandle = brhPerson;
        BoldEdit7.BoldProperties.Expression = "assets";
        bcbIsMarried = new TBoldCheckBox { Name = "bcbIsMarried", Text = "Is Married", Bounds = new Rectangle(138, 67, 90, 17) };
        bcbIsMarried.BoldHandle = brhPerson;
        bcbIsMarried.BoldProperties.Expression = "isMarried";
        bgrOwnedBuildings = new TBoldGrid
        {
            Name = "bgrOwnedBuildings",
            Bounds = new Rectangle(8, 136, 249, 121),
            AddNewAtEnd = false,
            BoldAutoColumns = false,
            BoldShowConstraints = false,
        };
        bgrOwnedBuildings.BoldHandle = blhOwnedBuildings;
        bgrOwnedBuildings.BoldProperties.NilElementMode = TBoldNilElementMode.neNone;
        foreach ((string expression, string title) in new[] { ("zipCode", "ZipCode"), ("address", "Address"), ("owners->size", "# owners") })
        {
            TBoldGridColumn column = bgrOwnedBuildings.AddColumn();
            column.BoldProperties.Expression = expression;
            column.Title.Caption = title;
            column.Width = 64;
        }

        Controls.AddRange(
        [
            new Label { Text = "FirstName", Bounds = new Rectangle(8, 7, 80, 13) },
            new Label { Text = "LastName", Bounds = new Rectangle(8, 52, 80, 13) },
            new Label { Text = "Assets", Bounds = new Rectangle(136, 7, 80, 13) },
            new Label { Text = "Owned Buildings:", Bounds = new Rectangle(9, 116, 120, 13) },
            new Label { Text = "This form is installed by the PlaceableAFP-component on the data module", Bounds = new Rectangle(8, 272, 214, 30) },
            beFirstName, BoldEdit1, BoldEdit7, bcbIsMarried, bgrOwnedBuildings,
        ]);
        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>When the object is deleted or destroyed, we release the form.</summary>
    private void brhPersonObjectDeleted(object? Sender) => BoldOwnedComponents.Release(this);
}
