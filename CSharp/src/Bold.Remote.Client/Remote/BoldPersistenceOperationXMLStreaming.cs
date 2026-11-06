// Built from master's Source/Persistence/SOAP/BoldPersistenceOperationXMLStreaming.pas and
// BoldPersistenceControllerSOAPAdapterCore.pas (removed from the branch with SOAP; not tracked by the port tool).
using System.Globalization;
using System.Xml;

namespace Bold;

/// <summary>
/// One call of a persistence controller, streamed as XML: the client writes the in parameters, the server reads them,
/// runs the call on its persistence controller and writes the out parameters, which the client reads back. The
/// messages are those of Delphi Bold's SOAP persistence: an element named after the operation inside a Body element,
/// and the reply in an element named after the operation with "Response", or a Fault with a faultstring.
/// </summary>
public abstract class TBoldPersistenceOperation
{
    public const string BodyNodeName = "Body";
    public const string FaultNodeName = "Fault";
    public const string FaultStringNodeName = "faultstring";
    public const string ResponseSuffix = "Response";

    protected const string BoldNodeName_ObjectIdList = "ObjectIdList";
    protected const string BoldNodeName_MemberIdList = "MemberIdList";
    protected const string BoldNodeName_FetchMode = "FetchMode";
    protected const string BoldNodeName_BoldClientID = "BoldClientID";
    protected const string BoldNodeName_ValueSpace = "ValueSpace";
    protected const string BoldNodeName_Condition = "Condition";
    protected const string BoldNodeName_TranslationList = "TranslationList";
    protected const string BoldNodeName_Precondition = "Precondition";
    protected const string BoldNodeName_Old_Values = "Old_Values";
    protected const string BoldNodeName_Timestamp = "Timestamp";
    protected const string BoldNodeName_ClockTime = "ClockTime";

    protected TBoldPersistenceOperation(TBoldDefaultXMLStreamManager Manager)
    {
        StreamManager = Manager;
    }

    protected TBoldDefaultXMLStreamManager StreamManager { get; }

    /// <summary>The operation's name, which names its element in the messages.</summary>
    public abstract string Name { get; }

    public virtual void InitParams()
    {
    }

    public virtual void FreeParams()
    {
    }

    public abstract void WriteInParams(TBoldXMLNode XMLNode);

    public abstract void WriteOutParams(TBoldXMLNode XMLNode);

    public abstract void ReadInParams(TBoldXMLNode XMLNode);

    public abstract void ReadOutParams(TBoldXMLNode XMLNode);

    public abstract void Execute(TBoldPersistenceController PersistenceController);

    /// <summary>The request for this operation, with its in parameters.</summary>
    public string WriteRequest()
    {
        var document = new XmlDocument();
        TBoldXMLNode body = StreamManager.NewRootNode(document, BodyNodeName);
        WriteInParams(body.NewSubNode(Name));
        return document.OuterXml;
    }

    /// <summary>Reads the out parameters from a reply; raises with the server's message when the reply is a fault.</summary>
    public void ReadReply(string Reply)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml(Reply);
        }
        catch (XmlException e)
        {
            throw new EBold($"{GetType().Name}: the reply is not XML ({e.Message}): {Reply}");
        }

        TBoldXMLNode body = StreamManager.GetRootNode(document, BodyNodeName);
        TBoldXMLNode? response = body.GetSubNode(Name + ResponseSuffix);
        if (response is null)
        {
            TBoldXMLNode? fault = body.GetSubNode(FaultNodeName) ?? throw new EBold($"{GetType().Name}: the reply holds no answer to {Name}.");
            TBoldXMLNode? message = fault.GetSubNode(FaultStringNodeName);
            throw new EBold(message is null ? $"{Name} failed on the server." : $"{Name} failed on the server: {message.ReadString()}");
        }

        ReadOutParams(response);
    }

    /// <summary>On the server: reads the in parameters from the request's body, runs the call and writes the reply's body.</summary>
    public void ExecuteStreamed(TBoldPersistenceController PersistenceController, TBoldXMLNode RequestBodyNode, TBoldXMLNode ReplyBodyNode)
    {
        InitParams();
        try
        {
            ReadInParams(RequestBodyNode.GetSubNode(Name) ?? throw new EBold($"{GetType().Name}: the request holds no {Name}."));
            try
            {
                Execute(PersistenceController);
                WriteOutParams(ReplyBodyNode.NewSubNode(Name + ResponseSuffix));
            }
            catch (Exception e)
            {
                ReplyBodyNode.NewSubNode(FaultNodeName).NewSubNode(FaultStringNodeName).WriteString(e.Message);
            }
        }
        finally
        {
            FreeParams();
        }
    }

    protected static void CopyTranslations(TBoldIDTranslationList? From, TBoldIDTranslationList To)
    {
        if (From is null)
        {
            return;
        }

        for (int i = 0; i < From.Count; i++)
        {
            To.AddTranslation(From.OldIds(i), From.NewIds(i));
        }
    }
}

/// <summary>PMFetch: the values of objects (all members, or the ones listed).</summary>
public class TBoldPMFetchOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMFetch";

    public TBoldObjectIdList? ObjectIdList { get; set; }
    public IBoldValueSpace? ValueSpace { get; set; }
    public TBoldMemberIdList? MemberIdList { get; set; }
    public int FetchMode { get; set; }
    public TBoldClientID BoldClientID { get; set; }

    public override void InitParams() => ValueSpace = new TBoldFreeStandingValueSpace();

    public override void FreeParams()
    {
        ObjectIdList = null;
        MemberIdList = null;
        ValueSpace = null;
    }

    public override void ReadInParams(TBoldXMLNode XMLNode)
    {
        ObjectIdList = XMLNode.ReadSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) as TBoldObjectIdList;
        MemberIdList = XMLNode.ReadSubNodeObject(BoldNodeName_MemberIdList, BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME) as TBoldMemberIdList;
        FetchMode = XMLNode.ReadSubNodeInteger(BoldNodeName_FetchMode);
        BoldClientID = XMLNode.ReadSubNodeInteger(BoldNodeName_BoldClientID);
    }

    public override void WriteInParams(TBoldXMLNode XMLNode)
    {
        XMLNode.WriteSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, ObjectIdList);
        XMLNode.WriteSubNodeObject(BoldNodeName_MemberIdList, BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME, MemberIdList);
        XMLNode.WriteSubNodeInteger(BoldNodeName_FetchMode, FetchMode);
        XMLNode.WriteSubNodeInteger(BoldNodeName_BoldClientID, BoldClientID);
    }

    public override void Execute(TBoldPersistenceController PersistenceController) =>
        PersistenceController.PMFetch(ObjectIdList!, ValueSpace!, MemberIdList, FetchMode, BoldClientID);

    public override void WriteOutParams(TBoldXMLNode XMLNode) =>
        StreamManager.WriteValueSpace(ValueSpace, ObjectIdList!, null, XMLNode.NewSubNode(BoldNodeName_ValueSpace));

    public override void ReadOutParams(TBoldXMLNode XMLNode)
    {
        if (XMLNode.GetSubNode(BoldNodeName_ValueSpace) is { } values)
        {
            StreamManager.ReadValueSpace(ValueSpace, values);
        }
    }
}

/// <summary>PMFetchIDListWithCondition: the ids of the objects that meet a condition.</summary>
public class TBoldPMFetchIdListOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMFetchIDListWithCondition";

    public TBoldObjectIdList? ObjectIdList { get; set; }
    public int FetchMode { get; set; }
    public TBoldCondition? Condition { get; set; }
    public TBoldClientID BoldClientID { get; set; }

    public override void InitParams() => ObjectIdList = new TBoldObjectIdList();

    public override void FreeParams()
    {
        ObjectIdList = null;
        Condition = null;
    }

    public override void ReadInParams(TBoldXMLNode XMLNode)
    {
        FetchMode = XMLNode.ReadSubNodeInteger(BoldNodeName_FetchMode);
        Condition = XMLNode.ReadSubNodeObject(BoldNodeName_Condition, "") as TBoldCondition;
        BoldClientID = XMLNode.ReadSubNodeInteger(BoldNodeName_BoldClientID);
    }

    public override void WriteInParams(TBoldXMLNode XMLNode)
    {
        XMLNode.WriteSubNodeInteger(BoldNodeName_FetchMode, FetchMode);
        XMLNode.WriteSubNodeObject(BoldNodeName_Condition, "", Condition);
        XMLNode.WriteSubNodeInteger(BoldNodeName_BoldClientID, BoldClientID);
    }

    public override void Execute(TBoldPersistenceController PersistenceController) =>
        PersistenceController.PMFetchIDListWithCondition(ObjectIdList!, new TBoldFreeStandingValueSpace(), FetchMode, Condition!, BoldClientID);

    public override void WriteOutParams(TBoldXMLNode XMLNode) =>
        XMLNode.WriteSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, ObjectIdList);

    public override void ReadOutParams(TBoldXMLNode XMLNode)
    {
        var ids = XMLNode.ReadSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) as TBoldObjectIdList;
        ObjectIdList!.Clear();
        if (ids is not null)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                ObjectIdList.Add(ids[i]);
            }
        }
    }
}

/// <summary>PMExactifyIds: the exact class of objects known by an id of a superclass.</summary>
public class TBoldPMExactifyIdsOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMExactifyIds";

    public TBoldObjectIdList? ObjectIdList { get; set; }
    public TBoldIDTranslationList? TranslationList { get; set; }

    public override void InitParams() => TranslationList = new TBoldIDTranslationList();

    public override void FreeParams()
    {
        ObjectIdList = null;
        TranslationList = null;
    }

    public override void ReadInParams(TBoldXMLNode XMLNode) =>
        ObjectIdList = XMLNode.ReadSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) as TBoldObjectIdList;

    public override void WriteInParams(TBoldXMLNode XMLNode) =>
        XMLNode.WriteSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, ObjectIdList);

    public override void Execute(TBoldPersistenceController PersistenceController) => PersistenceController.PMExactifyIds(ObjectIdList!, TranslationList!);

    public override void WriteOutParams(TBoldXMLNode XMLNode) =>
        XMLNode.WriteSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME, TranslationList);

    public override void ReadOutParams(TBoldXMLNode XMLNode) =>
        CopyTranslations(XMLNode.ReadSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME) as TBoldIDTranslationList,
            TranslationList!);
}

/// <summary>PMUpdate: stores changed, new and deleted objects; gives the new objects' ids and the timestamp.</summary>
public class TBoldPMUpdateOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMUpdate";

    public TBoldObjectIdList? ObjectIdList { get; set; }
    public IBoldValueSpace? ValueSpace { get; set; }
    public IBoldValueSpace? Old_Values { get; set; }
    public TBoldUpdatePrecondition? Precondition { get; set; }
    public TBoldIDTranslationList? TranslationList { get; set; }
    public TBoldTimeStampType Timestamp { get; set; }
    public TBoldClientID BoldClientID { get; set; }

    public override void InitParams()
    {
        ValueSpace = new TBoldFreeStandingValueSpace();
        Old_Values = new TBoldFreeStandingValueSpace();
        TranslationList = new TBoldIDTranslationList();
    }

    public override void FreeParams()
    {
        ObjectIdList = null;
        ValueSpace = null;
        Old_Values = null;
        TranslationList = null;
        Precondition = null;
    }

    public override void ReadInParams(TBoldXMLNode XMLNode)
    {
        ObjectIdList = XMLNode.ReadSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) as TBoldObjectIdList;
        if (XMLNode.GetSubNode(BoldNodeName_ValueSpace) is { } values)
        {
            StreamManager.ReadValueSpace(ValueSpace, values);
        }

        Precondition = XMLNode.ReadSubNodeObject(BoldNodeName_Precondition, "") as TBoldUpdatePrecondition;
        BoldClientID = XMLNode.ReadSubNodeInteger(BoldNodeName_BoldClientID);
    }

    public override void WriteInParams(TBoldXMLNode XMLNode)
    {
        XMLNode.WriteSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, ObjectIdList);
        StreamManager.WriteValueSpace(ValueSpace, ObjectIdList!, null, XMLNode.NewSubNode(BoldNodeName_ValueSpace));
        XMLNode.WriteSubNodeObject(BoldNodeName_Precondition, "", Precondition);
        XMLNode.WriteSubNodeInteger(BoldNodeName_BoldClientID, BoldClientID);
    }

    public override void Execute(TBoldPersistenceController PersistenceController)
    {
        TBoldTimeStampType timestamp = Timestamp;
        PersistenceController.PMUpdate(ObjectIdList!, ValueSpace!, Old_Values, Precondition, TranslationList, ref timestamp, BoldClientID);
        Timestamp = timestamp;
    }

    public override void WriteOutParams(TBoldXMLNode XMLNode)
    {
        StreamManager.WriteValueSpace(Old_Values, ObjectIdList!, null, XMLNode.NewSubNode(BoldNodeName_Old_Values));
        XMLNode.WriteSubNodeObject(BoldNodeName_Precondition, "", Precondition);
        XMLNode.WriteSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME, TranslationList);
        XMLNode.WriteSubNodeInteger(BoldNodeName_Timestamp, Timestamp);
    }

    public override void ReadOutParams(TBoldXMLNode XMLNode)
    {
        if (XMLNode.GetSubNode(BoldNodeName_Old_Values) is { } oldValues)
        {
            StreamManager.ReadValueSpace(Old_Values, oldValues);
        }

        if (Precondition is not null && XMLNode.ReadSubNodeObject(BoldNodeName_Precondition, "") is TBoldUpdatePrecondition outPrecondition)
        {
            Precondition.AssignOutValues(outPrecondition);
        }

        CopyTranslations(XMLNode.ReadSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME) as TBoldIDTranslationList,
            TranslationList!);
        Timestamp = XMLNode.ReadSubNodeInteger(BoldNodeName_Timestamp);
    }
}

/// <summary>ReserveNewIds: database ids for new objects before they are saved.</summary>
public class TBoldPMReserveNewIdsOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "ReserveNewIds";

    public TBoldObjectIdList? ObjectIdList { get; set; }
    public TBoldIDTranslationList? TranslationList { get; set; }

    public override void InitParams() => TranslationList = new TBoldIDTranslationList();

    public override void FreeParams()
    {
        ObjectIdList = null;
        TranslationList = null;
    }

    public override void ReadInParams(TBoldXMLNode XMLNode) =>
        ObjectIdList = XMLNode.ReadSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME) as TBoldObjectIdList;

    public override void WriteInParams(TBoldXMLNode XMLNode) =>
        XMLNode.WriteSubNodeObject(BoldNodeName_ObjectIdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, ObjectIdList);

    public override void Execute(TBoldPersistenceController PersistenceController) =>
        PersistenceController.ReserveNewIds(new TBoldFreeStandingValueSpace(), ObjectIdList!, TranslationList!);

    public override void WriteOutParams(TBoldXMLNode XMLNode) =>
        XMLNode.WriteSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME, TranslationList);

    public override void ReadOutParams(TBoldXMLNode XMLNode) =>
        CopyTranslations(XMLNode.ReadSubNodeObject(BoldNodeName_TranslationList, BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME) as TBoldIDTranslationList,
            TranslationList!);
}

/// <summary>PMTimestampForTime: the database timestamp at a time. Times go as ISO 8601, where Delphi used the locale's format.</summary>
public class TBoldPMTimestampForTimeOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMTimestampForTime";

    public TDateTime ClockTime { get; set; }
    public TBoldTimeStampType Timestamp { get; set; }

    public override void ReadInParams(TBoldXMLNode XMLNode) => ClockTime = BoldRemoteTime.Parse(XMLNode.ReadSubNodeString(BoldNodeName_ClockTime));

    public override void WriteInParams(TBoldXMLNode XMLNode) => XMLNode.WriteSubNodeString(BoldNodeName_ClockTime, BoldRemoteTime.Format(ClockTime));

    public override void Execute(TBoldPersistenceController PersistenceController)
    {
        TBoldTimeStampType timestamp = Timestamp;
        PersistenceController.PMTimestampForTime(ClockTime, ref timestamp);
        Timestamp = timestamp;
    }

    public override void WriteOutParams(TBoldXMLNode XMLNode) => XMLNode.WriteSubNodeInteger(BoldNodeName_Timestamp, Timestamp);

    public override void ReadOutParams(TBoldXMLNode XMLNode) => Timestamp = XMLNode.ReadSubNodeInteger(BoldNodeName_Timestamp);
}

/// <summary>PMTimeForTimestamp: the time of a database timestamp.</summary>
public class TBoldPMTimeForTimestampOperation(TBoldDefaultXMLStreamManager Manager) : TBoldPersistenceOperation(Manager)
{
    public override string Name => "PMTimeForTimestamp";

    public TDateTime ClockTime { get; set; }
    public TBoldTimeStampType Timestamp { get; set; }

    public override void ReadInParams(TBoldXMLNode XMLNode) => Timestamp = XMLNode.ReadSubNodeInteger(BoldNodeName_Timestamp);

    public override void WriteInParams(TBoldXMLNode XMLNode) => XMLNode.WriteSubNodeInteger(BoldNodeName_Timestamp, Timestamp);

    public override void Execute(TBoldPersistenceController PersistenceController)
    {
        TDateTime clockTime = ClockTime;
        PersistenceController.PMTimeForTimestamp(Timestamp, ref clockTime);
        ClockTime = clockTime;
    }

    public override void WriteOutParams(TBoldXMLNode XMLNode) => XMLNode.WriteSubNodeString(BoldNodeName_ClockTime, BoldRemoteTime.Format(ClockTime));

    public override void ReadOutParams(TBoldXMLNode XMLNode) => ClockTime = BoldRemoteTime.Parse(XMLNode.ReadSubNodeString(BoldNodeName_ClockTime));
}

internal static class BoldRemoteTime
{
    public static string Format(TDateTime Value) => Value.ToString("o", CultureInfo.InvariantCulture);

    public static TDateTime Parse(string Value) => DateTime.Parse(Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

/// <summary>
/// The server's side of remote persistence: answers a request with the reply of the persistence controller, as
/// Delphi's SOAP adapter core did. Calls are made one at a time, since a persistence controller is not thread-safe.
/// </summary>
public class TBoldRemotePersistenceAdapterCore
{
    private readonly TBoldDefaultXMLStreamManager fStreamManager;
    private readonly object fLock = new();

    public TBoldRemotePersistenceAdapterCore(TMoldModel Model)
    {
        fStreamManager = new TBoldDefaultXMLStreamManager(TBoldDefaultXMLStreamerRegistry.MainStreamerRegistry(), Model)
        {
            PersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsModified, TBoldValuePersistenceState.bvpsCurrent,
                TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsTransient),
            IgnorePersistenceState = false,
            PersistenceStatesToOverwrite = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid, TBoldValuePersistenceState.bvpsCurrent),
        };
    }

    /// <summary>The operation a request is for.</summary>
    public TBoldPersistenceOperation OperationFor(string Name) => Name switch
    {
        "PMFetch" => new TBoldPMFetchOperation(fStreamManager),
        "PMFetchIDListWithCondition" => new TBoldPMFetchIdListOperation(fStreamManager),
        "PMExactifyIds" => new TBoldPMExactifyIdsOperation(fStreamManager),
        "PMUpdate" => new TBoldPMUpdateOperation(fStreamManager),
        "ReserveNewIds" => new TBoldPMReserveNewIdsOperation(fStreamManager),
        "PMTimeForTimestamp" => new TBoldPMTimeForTimestampOperation(fStreamManager),
        "PMTimestampForTime" => new TBoldPMTimestampForTimeOperation(fStreamManager),
        _ => throw new EBold($"{GetType().Name}: unknown operation {Name}."),
    };

    /// <summary>The reply to a request; a request that cannot be read gets a fault.</summary>
    public string Get(string Request, TBoldPersistenceController PersistenceController)
    {
        var replyDocument = new XmlDocument();
        TBoldXMLNode replyBody = fStreamManager.NewRootNode(replyDocument, TBoldPersistenceOperation.BodyNodeName);
        try
        {
            var requestDocument = new XmlDocument();
            requestDocument.LoadXml(Request);
            TBoldXMLNode requestBody = fStreamManager.GetRootNode(requestDocument, TBoldPersistenceOperation.BodyNodeName);
            XmlElement operation = requestBody.XMLDomElement.ChildNodes.OfType<XmlElement>().FirstOrDefault()
                ?? throw new EBold($"{GetType().Name}: the request holds no operation.");
            TBoldPersistenceOperation op = OperationFor(operation.LocalName);
            lock (fLock)
            {
                op.ExecuteStreamed(PersistenceController, requestBody, replyBody);
            }
        }
        catch (Exception e) when (e is EBold or XmlException)
        {
            replyBody.NewSubNode(TBoldPersistenceOperation.FaultNodeName).NewSubNode(TBoldPersistenceOperation.FaultStringNodeName).WriteString(e.Message);
        }

        return replyDocument.OuterXml;
    }
}
