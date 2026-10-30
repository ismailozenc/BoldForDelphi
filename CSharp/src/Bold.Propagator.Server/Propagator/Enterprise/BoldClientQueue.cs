// Delphi: Source/Propagator/Enterprise/BoldClientQueue.pas
// Port map: CSharp/port/units/BoldClientQueue.md

namespace Bold;

/// <summary>The events waiting to be sent to one client.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientQueue(string Name) : TBoldThreadSafeStringQueue(Name)
{
    /// <summary>Takes all the events out of the queue, oldest first; null when it is empty.</summary>
    [Delphi(PortStatus.Ported, Note = "A string array where Delphi made a variant array.")]
    public string[]? AsVarArray()
    {
        Lock();
        try
        {
            if (UnsafeIsEmpty())
            {
                return null;
            }

            var result = new string[Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Dequeue();
            }

            return result;
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldClientID BoldClientID { get; set; }
}
