// Delphi: Source/Persistence/Propagation/BoldPropagatorUtils.pas
// Port map: CSharp/port/units/BoldPropagatorUtils.md

namespace Bold;

/// <summary>Reconnecting a client to the propagator.</summary>
public static class BoldPropagatorUtils
{
    /// <summary>The subscriptions for an object in memory: its embedded state, and each non-embedded member that is current or modified.</summary>
    [Delphi(PortStatus.Ported)]
    private static void GetActiveObjectSubscriptions(TBoldObject Obj, TStringList Subscriptions)
    {
        if (!Obj.BoldPersistent)
        {
            return;
        }

        TBoldObjectId Id = Obj.BoldObjectLocator!.BoldObjectID;
        Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "", Id));
        for (int i = 0; i < Obj.BoldMemberCount; i++)
        {
            TBoldMemberRTInfo MemberRTInfo = Obj.BoldClassTypeInfo.AllMembers[i];
            if (!MemberRTInfo.IsStoredInObject && Obj.BoldMemberAssigned(i) &&
                Obj.BoldMembers(i).BoldPersistenceState is TBoldValuePersistenceState.bvpsCurrent or TBoldValuePersistenceState.bvpsModified)
            {
                Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "",
                    MemberRTInfo.ExpressionName, "", Id));
            }
        }
    }

    /// <summary>The subscriptions for everything in memory: each object, and each class that is loaded.</summary>
    [Delphi(PortStatus.Ported)]
    private static void GetActiveSubscriptions(TBoldSystem System, TStringList Subscriptions)
    {
        using (TBoldLocatorListTraverser Traverser = System.Locators.CreateTraverser())
        {
            while (!Traverser.EndOfList)
            {
                if (Traverser.locator.BoldObject is { } obj)
                {
                    GetActiveObjectSubscriptions(obj, Subscriptions);
                }

                Traverser.Next();
            }
        }

        TBoldClassTypeInfoList TopSortedClasses = System.BoldSystemTypeInfo.TopSortedClasses;
        for (int i = 0; i < TopSortedClasses.Count; i++)
        {
            if (System.Classes(i)?.BoldPersistenceState is TBoldValuePersistenceState.bvpsTransient or TBoldValuePersistenceState.bvpsCurrent
                or TBoldValuePersistenceState.bvpsModified)
            {
                Subscriptions.Add(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsClassChanged,
                    TopSortedClasses[i].ExpressionName, "", "", null));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void SendSubscriptions(TStringList Subscriptions, TBoldSnooper Snooper, TBoldClientID ClientId)
    {
        Snooper.Subscriptions.AddStrings(Subscriptions);
        Snooper.TransmitEvents(ClientId);
    }

    /// <summary>
    /// Reconnects to the propagator, registers the listener again and subscribes to everything in memory; true when the
    /// listener is connected again.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Takes the propagator handle where Delphi took its COM connection handle.")]
    public static bool ReConnectToPropagator(TBoldSystemHandle SystemHandle, TBoldListenerHandle ListenerHandle, TBoldAbstractPropagatorHandle PropagatorHandle,
        TBoldSnooperHandle SnooperHandle)
    {
        bool result = false;
        PropagatorHandle.Connected = false;
        PropagatorHandle.Connected = true;
        if (PropagatorHandle.Connected)
        {
            ListenerHandle.StartListenerThread();
            if (ListenerHandle.Connected)
            {
                var Subscriptions = new TStringList();
                GetActiveSubscriptions(SystemHandle.System!, Subscriptions);
                var Snooper = (TBoldSnooper)SnooperHandle.PersistenceController;
                SendSubscriptions(Subscriptions, Snooper, ListenerHandle.BoldClientID);
                result = true;
            }
        }

        return result;
    }
}
