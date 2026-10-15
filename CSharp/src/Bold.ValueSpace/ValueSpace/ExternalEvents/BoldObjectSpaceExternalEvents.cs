// Delphi: Source/ValueSpace/ExternalEvents/BoldObjectSpaceExternalEvents.pas
// Port map: CSharp/port/units/BoldObjectSpaceExternalEvents.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldObjectSpaceExternalEvents", Symbol = "TBoldEventType", Note = "Global using alias of char, in BoldObjectSpaceExternalEvents.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldObjectSpaceExternalEvents", Symbol = "TBoldExternalEvent", Note = "Global using alias of string, in BoldObjectSpaceExternalEvents.Aliases.cs.")]

namespace Bold;

/// <summary>What an external (propagated) event says changed.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldObjectSpaceSubscriptionType
{
    bsClassChanged, bsEmbeddedStateOfObjectChanged, bsNonEmbeddedStateOfObjectChanged, bsGotLocks, bsLockLost, bsObjectDeleted,
}

/// <summary>
/// Encodes and decodes the strings the propagator sends between object spaces: "C:Class", "E:id", "E:id|D",
/// "I:member:id", "L:lock" and "GotLocks".
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectSpaceExternalEvent
{
    private const char SUBSCRIPTION_DELIMITER_CHAR = ':';
    private const string EXTERNAL_EVENT_CLASSCHANGED = "C";
    private const char EXTERNAL_EVENT_EMBEDDEDSTATEOFOBJECTCHANGED = 'E';
    private const char EXTERNAL_EVENT_NONEMBEDDEDSTATEOFOBJECTCHANGED = 'I';
    private const string EXTERNAL_EVENT_EMBEDDEDSTATE_OBJECTDELETED = "D";
    private const string EXTERNAL_EVENT_LOCKLOST = "L";
    private const string EXTERNAL_EVENT_GOTLOCK = "GotLocks";

    /// <summary>Reads the object id of an object event into <paramref name="ObjectID"/> (when given); raises EBold for a bad id.</summary>
    [Delphi(PortStatus.Ported)]
    private static void GetID(TBoldExternalEvent Event, TBoldDefaultID? ObjectID)
    {
        if (ObjectID is null)
        {
            return;
        }

        string IDAsString = "";
        switch (GetEventType(Event))
        {
            case TBoldObjectSpaceSubscriptionType.bsObjectDeleted:
            case TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged:
                int p = SysUtils.Pos(BoldDefs.PROPAGATOR_PARAMETER_DELIMITER_CHAR.ToString(), Event);
                if (p != 0)
                {
                    Event = SysUtils.Copy(Event, 1, p - 1);
                }

                IDAsString = SysUtils.Copy(Event, 3, SysUtils.MaxInt);
                break;
            case TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged:
                Event = SysUtils.Copy(Event, SysUtils.Pos(SUBSCRIPTION_DELIMITER_CHAR.ToString(), Event) + 1, SysUtils.MaxInt);
                Event = SysUtils.Copy(Event, SysUtils.Pos(SUBSCRIPTION_DELIMITER_CHAR.ToString(), Event) + 1, SysUtils.MaxInt);
                IDAsString = Event;
                break;
        }

        try
        {
            ObjectID.AsInteger = SysUtils.StrToInt(IDAsString);
        }
        catch (Exception e)
        {
            throw new EBold(SysUtils.Format(ValueSpaceConst.sInvalidID, IDAsString), e);
        }
    }

    /// <summary>The text after the parameter delimiter "|", or "".</summary>
    [Delphi(PortStatus.Ported)]
    private static string GetParameter(TBoldExternalEvent Event)
    {
        int p = SysUtils.Pos(BoldDefs.PROPAGATOR_PARAMETER_DELIMITER_CHAR.ToString(), Event);
        return p != 0 ? SysUtils.Copy(Event, p + 1, SysUtils.MaxInt) : "";
    }

    /// <summary>The kind of event, from its first character (or the GotLocks prefix); raises EBold for anything else.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldObjectSpaceSubscriptionType GetEventType(TBoldExternalEvent Event)
    {
        char ExternalEvent = Event.Length > 0 ? Event[0] : ' ';
        switch (ExternalEvent)
        {
            case 'C':
                return TBoldObjectSpaceSubscriptionType.bsClassChanged;
            case EXTERNAL_EVENT_EMBEDDEDSTATEOFOBJECTCHANGED:
                return GetParameter(Event) == EXTERNAL_EVENT_EMBEDDEDSTATE_OBJECTDELETED
                    ? TBoldObjectSpaceSubscriptionType.bsObjectDeleted
                    : TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged;
            case EXTERNAL_EVENT_NONEMBEDDEDSTATEOFOBJECTCHANGED:
                return TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged;
            case 'L':
                return TBoldObjectSpaceSubscriptionType.bsLockLost;
            default:
                if (SysUtils.Pos(EXTERNAL_EVENT_GOTLOCK, Event) == 1)
                {
                    return TBoldObjectSpaceSubscriptionType.bsGotLocks;
                }

                throw new EBold(SysUtils.Format(ValueSpaceConst.sInvalidEvent, Event));
        }
    }

    /// <summary>The event string for a change; the parts a kind does not use are ignored.</summary>
    [Delphi(PortStatus.Ported)]
    public static string EncodeExternalEvent(TBoldObjectSpaceSubscriptionType SubscriptionType, string ClassName, string MemberName, string LockName,
        TBoldObjectId? ObjectID) => SubscriptionType switch
    {
        TBoldObjectSpaceSubscriptionType.bsClassChanged => EXTERNAL_EVENT_CLASSCHANGED + SUBSCRIPTION_DELIMITER_CHAR + ClassName,
        TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged =>
            EXTERNAL_EVENT_EMBEDDEDSTATEOFOBJECTCHANGED + SUBSCRIPTION_DELIMITER_CHAR.ToString() + ObjectID!.AsString,
        TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged =>
            EXTERNAL_EVENT_NONEMBEDDEDSTATEOFOBJECTCHANGED + SUBSCRIPTION_DELIMITER_CHAR.ToString() + MemberName + SUBSCRIPTION_DELIMITER_CHAR + ObjectID!.AsString,
        TBoldObjectSpaceSubscriptionType.bsLockLost => EXTERNAL_EVENT_LOCKLOST + SUBSCRIPTION_DELIMITER_CHAR + LockName,
        TBoldObjectSpaceSubscriptionType.bsGotLocks => EXTERNAL_EVENT_GOTLOCK,
        TBoldObjectSpaceSubscriptionType.bsObjectDeleted =>
            EXTERNAL_EVENT_EMBEDDEDSTATEOFOBJECTCHANGED + SUBSCRIPTION_DELIMITER_CHAR.ToString() + ObjectID!.AsString +
            BoldDefs.PROPAGATOR_PARAMETER_DELIMITER_CHAR + EXTERNAL_EVENT_EMBEDDEDSTATE_OBJECTDELETED,
        _ => "",
    };

    /// <summary>The kind of event, with the class, member, lock name or object id it carries.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldObjectSpaceSubscriptionType DecodeExternalEvent(TBoldExternalEvent Event, ref string Classname, ref string MemberName,
        ref string LockName, TBoldDefaultID? ObjectID)
    {
        TBoldObjectSpaceSubscriptionType result = GetEventType(Event);
        switch (result)
        {
            case TBoldObjectSpaceSubscriptionType.bsClassChanged:
                Classname = SysUtils.Copy(Event, (EXTERNAL_EVENT_CLASSCHANGED + SUBSCRIPTION_DELIMITER_CHAR).Length + 1, SysUtils.MaxInt);
                break;
            case TBoldObjectSpaceSubscriptionType.bsObjectDeleted:
            case TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged:
                GetID(Event, ObjectID);
                break;
            case TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged:
                string delim = SUBSCRIPTION_DELIMITER_CHAR.ToString();
                int first = SysUtils.Pos(delim, Event);
                MemberName = SysUtils.Copy(Event, first + 1, SysUtils.Pos(delim, SysUtils.Copy(Event, first + 1, SysUtils.MaxInt)) - 1);
                GetID(Event, ObjectID);
                break;
            case TBoldObjectSpaceSubscriptionType.bsLockLost:
                LockName = SysUtils.Copy(Event, (EXTERNAL_EVENT_LOCKLOST + SUBSCRIPTION_DELIMITER_CHAR).Length + 1, SysUtils.MaxInt);
                break;
        }

        return result;
    }
}
