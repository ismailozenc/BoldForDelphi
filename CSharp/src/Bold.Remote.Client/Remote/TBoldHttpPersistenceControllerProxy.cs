using System.Net;
using System.Text;

namespace Bold;

/// <summary>
/// A persistence controller whose calls run on a remote persistence server (Bold.Remote.Server) over HTTP: each call
/// is one POST of the operation's XML to the server's address, answered with the reply. What Delphi's SOAP persistence
/// controller proxy was, on HttpClient. A call raises <see cref="EBold"/> when the server cannot be reached or fails.
/// </summary>
public class TBoldHttpPersistenceControllerProxy : TBoldPersistenceController
{
    /// <summary>The media type of requests and replies.</summary>
    public const string ContentType = "application/xml";

    private readonly TBoldDefaultXMLStreamManager fStreamManager;
    private readonly HttpClient fClient;
    private readonly bool fOwnsClient;

    /// <param name="Url">The server's persistence address, as http://server:5000/persistence.</param>
    /// <param name="Client">The HttpClient to use; one is made (and disposed with the controller) when null.</param>
    public TBoldHttpPersistenceControllerProxy(TMoldModel Model, string Url, HttpClient? Client = null)
    {
        this.Url = new Uri(Url);
        fStreamManager = new TBoldDefaultXMLStreamManager(TBoldDefaultXMLStreamerRegistry.MainStreamerRegistry(), Model);
        fOwnsClient = Client is null;
        fClient = Client ?? new HttpClient();
    }

    public Uri Url { get; }

    /// <summary>How long a call waits for the server.</summary>
    public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(100);

    public override void Dispose()
    {
        if (fOwnsClient)
        {
            fClient.Dispose();
        }

        base.Dispose();
    }

    /// <summary>Sends the operation's request and reads its reply.</summary>
    protected virtual void RemoteExecute(TBoldPersistenceOperation Operation)
    {
        string request = Operation.WriteRequest();
        string reply;
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, Url) { Content = new StringContent(request, Encoding.UTF8, ContentType) };
            using var timeOut = new CancellationTokenSource(TimeOut);
            // The synchronous Send: no continuation waits for a GUI thread's synchronization context.
            using HttpResponseMessage response = fClient.Send(message, timeOut.Token);
            using var reader = new StreamReader(response.Content.ReadAsStream(timeOut.Token), Encoding.UTF8);
            reply = reader.ReadToEnd();
            if (response.StatusCode != HttpStatusCode.OK && !reply.Contains("<" + TBoldPersistenceOperation.BodyNodeName, StringComparison.Ordinal))
            {
                throw new EBold($"{GetType().Name}: {Operation.Name} failed: the server at {Url} answered {(int)response.StatusCode} {response.ReasonPhrase}.");
            }
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or IOException)
        {
            throw new EBold($"{GetType().Name}: {Operation.Name} failed: the server at {Url} could not be reached ({e.Message}).");
        }

        Operation.ReadReply(reply);
    }

    /// <summary>Gives the ids of objects known by a superclass their exact class, here and in the value space.</summary>
    protected void ExactifyIds(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        if (ObjectIdList.Count == 0)
        {
            return;
        }

        bool allExact = true;
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            allExact &= ObjectIdList[i].TopSortedIndexExact;
        }

        if (allExact)
        {
            return;
        }

        var translationList = new TBoldIDTranslationList();
        PMExactifyIds(ObjectIdList, translationList);
        ValueSpace.ExactifyIDs(translationList);
        ObjectIdList.ExactifyIds(translationList);
    }

    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        RemoteExecute(new TBoldPMExactifyIdsOperation(fStreamManager) { ObjectIdList = ObjectIdList, TranslationList = TranslationList });

    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID)
    {
        SendExtendedEvent(BoldSubscription.bpeStartFetch, [ObjectIdList, MemberIdList]);
        try
        {
            ExactifyIds(ObjectIdList, ValueSpace);
            RemoteExecute(new TBoldPMFetchOperation(fStreamManager)
            {
                ObjectIdList = ObjectIdList,
                ValueSpace = ValueSpace,
                MemberIdList = MemberIdList,
                FetchMode = FetchMode,
                BoldClientID = BoldClientID,
            });
        }
        finally
        {
            SendEvent(BoldSubscription.bpeEndFetch);
        }
    }

    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID)
    {
        SendExtendedEvent(BoldSubscription.bpeStartFetchID, [Condition]);
        try
        {
            RemoteExecute(new TBoldPMFetchIdListOperation(fStreamManager)
            {
                ObjectIdList = ObjectIdList,
                FetchMode = FetchMode,
                Condition = Condition,
                BoldClientID = BoldClientID,
            });
        }
        finally
        {
            SendEvent(BoldSubscription.bpeEndFetchID);
        }
    }

    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        SendExtendedEvent(BoldSubscription.bpeStartUpdate, [ObjectIdList, ValueSpace]);
        try
        {
            var op = new TBoldPMUpdateOperation(fStreamManager)
            {
                ObjectIdList = ObjectIdList,
                ValueSpace = ValueSpace,
                Old_Values = Old_Values,
                Precondition = Precondition,
                TranslationList = TranslationList ?? new TBoldIDTranslationList(),
                BoldClientID = BoldClientID,
            };
            RemoteExecute(op);
            if (Precondition is null || !Precondition.Failed)
            {
                TimeStamp = op.Timestamp;
                ValueSpace.ApplytranslationList(op.TranslationList!);
            }
        }
        finally
        {
            SendEvent(BoldSubscription.bpeEndUpdate);
        }
    }

    /// <summary>Not offered by the server, as in Delphi's SOAP proxy: nothing is done.</summary>
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
    }

    /// <summary>Not offered by the server, as in Delphi's SOAP proxy: nothing is done.</summary>
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList)
    {
    }

    /// <summary>Not offered by the server, as in Delphi's SOAP proxy: nothing is done.</summary>
    public override void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList)
    {
    }

    public override void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        RemoteExecute(new TBoldPMReserveNewIdsOperation(fStreamManager) { ObjectIdList = ObjectIdList, TranslationList = TranslationList });

    public override void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp)
    {
        var op = new TBoldPMTimestampForTimeOperation(fStreamManager) { ClockTime = ClockTime };
        RemoteExecute(op);
        Timestamp = op.Timestamp;
    }

    public override void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime)
    {
        var op = new TBoldPMTimeForTimestampOperation(fStreamManager) { Timestamp = Timestamp };
        RemoteExecute(op);
        ClockTime = op.ClockTime;
    }
}

/// <summary>
/// A persistence handle for a remote persistence server (Bold.Remote.Server) at <see cref="Url"/>, where Delphi had
/// its SOAP and HTTP client persistence handles. The system's model must be the server's.
/// </summary>
public class TBoldHttpClientPersistenceHandle : TBoldPersistenceHandle
{
    private TBoldAbstractModel? fBoldModel;

    public TBoldHttpClientPersistenceHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>The server's persistence address, as http://server:5000/persistence.</summary>
    public string Url { get; set; } = "";

    /// <summary>The model the values are streamed by; the server's model.</summary>
    public TBoldAbstractModel? BoldModel
    {
        get => fBoldModel;
        set
        {
            if (value != fBoldModel)
            {
                ReleasePersistenceController();
                fBoldModel = value;
            }
        }
    }

    /// <summary>The HttpClient to use; one is made for each persistence controller when null.</summary>
    public HttpClient? HttpClient { get; set; }

    protected override TBoldPersistenceController CreatePersistenceController()
    {
        if (BoldModel is null)
        {
            throw new EBold($"{GetType().Name} {Name}: BoldModel is not set.");
        }

        if (string.IsNullOrEmpty(Url))
        {
            throw new EBold($"{GetType().Name} {Name}: Url is not set.");
        }

        return new TBoldHttpPersistenceControllerProxy(BoldModel.MoldModel, Url, HttpClient);
    }
}
