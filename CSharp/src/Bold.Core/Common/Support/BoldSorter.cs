// Delphi: Source/Common/Support/BoldSorter.pas
// Port map: CSharp/port/units/BoldSorter.md

namespace Bold;

/// <summary>Compares the items at two positions: negative, zero or positive.</summary>
[Delphi(PortStatus.Ported)]
public delegate int TBoldSortCompare(int Index1, int Index2);

/// <summary>Swaps the items at two positions.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSortExchange(int Index1, int Index2);

/// <summary>Quicksort over positions, through compare and exchange callbacks.</summary>
public static partial class BoldSorter
{
    /// <summary>Sorts positions <paramref name="FirstIndex"/> to <paramref name="LastIndex"/>, tracking the pivot through exchanges as Delphi does.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldSort(int FirstIndex, int LastIndex, TBoldSortCompare Compare, TBoldSortExchange Exchange)
    {
        int I;
        do
        {
            I = FirstIndex;
            int J = LastIndex;
            int P = (FirstIndex + LastIndex) >> 1;
            do
            {
                while (Compare(I, P) < 0)
                {
                    I++;
                }

                while (Compare(J, P) > 0)
                {
                    J--;
                }

                if (I <= J)
                {
                    Exchange(I, J);
                    if (P == I)
                    {
                        P = J;
                    }
                    else if (P == J)
                    {
                        P = I;
                    }

                    I++;
                    J--;
                }
            }
            while (I <= J);

            if (FirstIndex < J)
            {
                BoldSort(FirstIndex, J, Compare, Exchange);
            }

            FirstIndex = I;
        }
        while (I < LastIndex);
    }
}
