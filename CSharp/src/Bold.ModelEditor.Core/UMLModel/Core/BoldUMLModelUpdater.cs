// Delphi: Source/UMLModel/Core/BoldUMLModelUpdater.pas
// Port map: CSharp/port/units/BoldUMLModelUpdater.md

using static Bold.BoldDefaultTaggedValues;
using static Bold.BoldUMLTaggedValues;

namespace Bold;

/// <summary>Brings a model of an older Bold up to date: the old Bold.Persistent tagged value becomes persistence.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldUMLModelUpdater
{
    [Delphi(PortStatus.Ported)]
    public static void UpdateModel(TBoldUMLObject ModelToUpdate)
    {
        UpdatePersistentTVs([.. ModelToUpdate.Graph.AllInstances("Association")]);
        UpdatePersistentTVs([.. ModelToUpdate.Graph.AllInstances("Attribute")]);
    }

    [Delphi(PortStatus.Ported)]
    private static void UpdatePersistentTVs(List<TBoldUMLObject> aList)
    {
        foreach (TBoldUMLObject anElement in aList)
        {
            string oldPersistentValue = anElement.GetBoldTV(TV_PERSISTENT_OLD);
            if (oldPersistentValue != "")
            {
                if (BoldUtils.StringToBoolean(oldPersistentValue))
                {
                    if (anElement.taggedValue(TAG_PERSISTENCE) is null)
                    {
                        anElement.SetTaggedValue(TAG_PERSISTENCE, TV_PERSISTENCE_PERSISTENT);
                    }
                }
                else
                {
                    anElement.SetTaggedValue(TAG_PERSISTENCE, TV_PERSISTENCE_TRANSIENT);
                }

                anElement.DeleteTaggedValue(BoldDefs.BOLDTVPREFIX + TV_PERSISTENT_OLD);
            }
        }
    }
}
