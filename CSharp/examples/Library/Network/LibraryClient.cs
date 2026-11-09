// A client of the library's server, such as a desk: an application with no database of its own. Its system persists
// through the server, hears from the propagator what other clients saved, and takes pessimistic locks before it changes
// a region of an object. The console program of this example has two; the WinForms desk (examples/Library/Desk) uses
// one when it is started with --server.
using System.ComponentModel;
using Bold;
using LibraryClasses;

namespace Library.Network;

/// <summary>A client of the library's server: a system on HTTP persistence, a listener for the others' changes, and locking.</summary>
public sealed class LibraryClient : IDisposable
{
    private readonly Container fComponents = new();
    public readonly string Name;
    public readonly TBoldSystemHandle SystemHandle;
    public readonly TBoldListenerHandle Listener;
    public readonly TBoldLockingHandle Locking;

    /// <summary>The objects this client changed when another client saved a change of them too.</summary>
    public readonly List<string> Conflicts = [];

    public LibraryClient(string name, string serverUrl)
    {
        Name = name;
        var model = new TBoldAbstractModel(fComponents) { Name = name };
        model.ReadModel(LibraryModel.Lines());   // the server's model

        // The server, for persistence and propagation.
        var server = new TBoldHttpClientPersistenceHandle(fComponents) { BoldModel = model, Url = serverUrl + "/persistence" };
        var propagator = new TBoldSignalRPropagatorHandle(fComponents) { Url = serverUrl + "/propagator" };
        var lockManager = new TBoldSignalRLockManagerHandle(fComponents) { Url = serverUrl + "/propagator" };

        // The system. The listener hears from the propagator what other clients saved; the event handler makes the system
        // read those objects again. The ID adder tells the server which client each request comes from.
        var typeInfo = new TBoldSystemTypeInfoHandle(fComponents) { BoldModel = model };
        SystemHandle = new TBoldSystemHandle(fComponents) { SystemTypeInfoHandle = typeInfo };
        var events = new TBoldExternalObjectSpaceEventHandler(fComponents)
        {
            BoldSystemHandle = SystemHandle,
            PropagatorHandle = propagator,
            OnConflict = changed => Conflicts.Add(changed.AsString),
        };
        Listener = new TBoldListenerHandle(fComponents) { PropagatorHandle = propagator, Dequeuer = events, ClientIdentifierString = name, PollingInterval = 100 };
        SystemHandle.PersistenceHandle = new TBoldIDAdderHandle(fComponents) { NextPersistenceHandle = server, BoldListener = Listener };
        propagator.Connected = true;
        lockManager.Connected = true;
        SystemHandle.Active = true;

        // Locking: before a member of a lock region changes (the model's Bold.RegionDefinitions), the client takes its lock.
        Locking = new TBoldLockingHandle(fComponents) { Listener = Listener, LockManager = lockManager, SystemHandle = SystemHandle };
    }

    public TBoldSystem System => SystemHandle.System!;

    /// <summary>The book of this title, as this client has it.</summary>
    public TBook? Book(string title) => System.ClassByExpressionName("Book").Cast<TBook>().FirstOrDefault(book => book.Title == title);

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        fComponents.Dispose();
    }
}
