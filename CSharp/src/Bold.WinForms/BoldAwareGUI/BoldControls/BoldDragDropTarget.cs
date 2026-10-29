// Delphi: Source/BoldAwareGUI/BoldControls/BoldDragDropTarget.pas
// Port map: CSharp/port/units/BoldDragDropTarget.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>
/// A picture that stands for a reference handle's object: its image is the icon of the object's node description
/// (from the image list). An object dropped on it becomes the handle's value; it can be dragged to drag the object.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms PictureBox.")]
public class TBoldDropTarget : PictureBox, IBoldOCLComponent
{
    private bool FIsDropTarget;
    private bool FIsDragSource;
    private string FNodeSelectionExpression;
    private TBoldElementHandleFollower? fHandleFollower;
    private TBoldTreeFollowerController? FRepresentations;
    private ImageList? FImageList;
    private int FEmptyImageIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldDropTarget()
    {
        FRepresentations = new TBoldTreeFollowerController(this);
        FRepresentations.OnGetContextType = GetContextType;
        fHandleFollower = new TBoldElementHandleFollower(this, Representations);
        FRepresentations.AfterMakeUptoDate = AfterMakeUptoDate;
        FNodeSelectionExpression = "oclType.asstring->union(oclType.allsupertypes.asString)->union('<Default>')"; // do not localize
        AllowDrop = true;
        AfterMakeUptoDate(fHandleFollower.Follower);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDropTarget.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SetImageList(null);
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref FRepresentations);
        }

        base.Dispose(disposing);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (fHandleFollower is not null)
        {
            object match = (object?)FindForm() ?? this;
            fHandleFollower.MatchObject = match;
            fHandleFollower.Follower.MatchObject = match;
        }
    }

    /// <summary>The picture is the image of the object's icon (outside a designer).</summary>
    [Delphi(PortStatus.Ported)]
    private void AfterMakeUptoDate(TBoldFollower Follower)
    {
        if (!DesignMode && ImageList is { } images)
        {
            Image?.Dispose();
            Image = null;
            int imageIndex = GetImageIndex();
            if (imageIndex >= 0 && imageIndex < images.Images.Count)
            {
                Image = new Bitmap(images.Images[imageIndex]);
            }
        }
    }

    /// <summary>The one dragged object is of the handle's type.</summary>
    [Delphi(PortStatus.Ported)]
    private bool CanAcceptDraggedObject() =>
        BoldHandle?.StaticBoldType is { } type && DraggedObject is { } dragged && dragged.BoldType!.ConformsTo(type);

    /// <summary>A press on the picture starts dragging its object (when it is a drag source).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldDropTarget.MouseDown", Note = "The drag starts at once (Delphi's BeginDrag(True)), then DoEndDrag.")]
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (IsDragSource && e.Button == MouseButtons.Left)
        {
            DoStartDrag();
            try
            {
                if (BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0)
                {
                    DoDragDrop(new TBoldDragObject { Source = this }, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                }
            }
            finally
            {
                DoEndDrag();
            }
        }
    }

    /// <summary>The handle's object is the dragged object.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DoStartDrag()
    {
        if (BoldHandle?.Value is TBoldObject value)
        {
            BoldGUI.BoldGUIHandler().DraggedObjects.Clear();
            BoldGUI.BoldGUIHandler().DraggedObjects.Add(value);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void DoEndDrag() => BoldGUI.BoldGUIHandler().DraggedObjects.Clear();

    /// <summary>A dropped object of the handle's type becomes the handle's value.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldDropTarget.DragDrop", Note = "OnDragDrop; as in Delphi, the DragDrop event is not raised.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        if (CanAcceptDraggedObject())
        {
            BoldHandle!.Value = DraggedObject;
        }
    }

    /// <summary>Accepts an object of the handle's type (when a drop target); the DragOver event may refuse it.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldDropTarget.DragOver", Note = "OnDragOver; the DragOver event is raised only when the target accepts.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        // First set the hard accept value
        bool Accept = IsDropTarget && CanAcceptDraggedObject();
        TBoldDragSupport.Accept(drgevent, Accept);
        // Then, if we accept, invoke user code
        if (Accept)
        {
            base.OnDragOver(drgevent);
        }

        //  Make sure we only accept if hard conditions are true.
        if (!(IsDropTarget && CanAcceptDraggedObject()))
        {
            drgevent.Effect = DragDropEffects.None;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetDraggedObject()
    {
        TBoldObjectList dragged = BoldGUI.BoldGUIHandler().DraggedObjects;
        return dragged.Count == 1 ? dragged[0] : null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldReferenceHandle? GetBoldHandle() => fHandleFollower!.BoldHandle as TBoldReferenceHandle;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldReferenceHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle?.StaticBoldType;

    TBoldElementTypeInfo? IBoldOCLComponent.GetContextType() => GetContextType();

    /// <summary>
    /// The node description for the object: the first named by the node selection expression's strings, else the
    /// default one.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private TBoldNodeDescription? GetCurrentNodeDescription()
    {
        TBoldNodeDescription? result = null;
        if (Element is { } element)
        {
            using (var ie = new TBoldIndirectElement())
            {
                // this code should move to the treeview support classes (or is it already there???)
                element.EvaluateAndSubscribeToExpression(NodeSelectionExpression, fHandleFollower!.Follower.Subscriber, ie);
                if (ie.Value is TBoldList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        result = Representations.NodeDescriptions.FindByName(list[i]!.AsString);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }
                else if (ie.Value is { } value)
                {
                    result = Representations.NodeDescriptions.FindByName(value.AsString);
                }
            }

            result ??= Representations.NodeDescriptions.FindByName(Representations.DefaultNodeDescriptionName);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetElement() => fHandleFollower!.Follower.Element;

    [Delphi(PortStatus.Ported)]
    private string GetExpression() => NodeSelectionExpression;

    string IBoldOCLComponent.GetExpression() => GetExpression();

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Expression) => NodeSelectionExpression = Expression;

    void IBoldOCLComponent.SetExpression(string Expression) => SetExpression(Expression);

    /// <summary>The icon of the object's node description (the empty image without an object).</summary>
    [Delphi(PortStatus.Ported)]
    private int GetImageIndex()
    {
        int result = EmptyImageIndex;
        if (Element is { } element && GetCurrentNodeDescription() is { } NodeDescription)
        {
            using var ie = new TBoldIndirectElement();
            element.EvaluateAndSubscribeToExpression(NodeDescription.IconController.Expression, fHandleFollower!.Follower.Subscriber, ie);
            if (ie.Value is TBAInteger integer)
            {
                result = integer.AsInteger;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalVariableList? GetVariableList() => Representations.VariableList;

    TBoldExternalVariableList? IBoldOCLComponent.GetVariableList() => GetVariableList();

    /// <summary>The image list went: the drop target has none.</summary>
    [Delphi(PortStatus.Ported, Note = "The image list's Disposed event.")]
    protected void Notification(object? AComponent, EventArgs e)
    {
        if (AComponent == ImageList)
        {
            ImageList = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetEmptyImageIndex(int Value) => FEmptyImageIndex = Value;

    [Delphi(PortStatus.Ported)]
    private void SetImageList(ImageList? Value)
    {
        if (FImageList is not null)
        {
            FImageList.Disposed -= Notification;
        }

        FImageList = Value;
        if (FImageList is not null)
        {
            FImageList.Disposed += Notification;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetIsDragSource(bool Value) => FIsDragSource = Value;

    [Delphi(PortStatus.Ported)]
    private void SetIsDropTarget(bool Value) => FIsDropTarget = Value;

    [Delphi(PortStatus.Ported)]
    private void SetNodeSelectionExpression(string Value) => FNodeSelectionExpression = Value;

    [Delphi(PortStatus.Ported)]
    private void SetRepresentations(TBoldTreeFollowerController Value) => FRepresentations!.Assign(Value);

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldObject? DraggedObject => GetDraggedObject();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? Element => GetElement();

    /// <summary>An object of the handle's type can be dropped on it.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool IsDropTarget
    {
        get => FIsDropTarget;
        set => SetIsDropTarget(value);
    }

    /// <summary>A press drags its object.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool IsDragSource
    {
        get => FIsDragSource;
        set => SetIsDragSource(value);
    }

    /// <summary>The strings that name the object's node description (the first found is taken).</summary>
    [Delphi(PortStatus.Ported)]
    public string NodeSelectionExpression
    {
        get => FNodeSelectionExpression;
        set => SetNodeSelectionExpression(value);
    }

    /// <summary>The node descriptions whose icons stand for the objects.</summary>
    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldTreeFollowerController Representations
    {
        get => FRepresentations!;
        set => SetRepresentations(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldReferenceHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(null)]
    public ImageList? ImageList
    {
        get => FImageList;
        set => SetImageList(value);
    }

    /// <summary>The image without an object.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(0)]
    public int EmptyImageIndex
    {
        get => FEmptyImageIndex;
        set => SetEmptyImageIndex(value);
    }
}
