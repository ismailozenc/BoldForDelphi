// Delphi: Source/Common/Support/BoldMath.pas
// Port map: CSharp/port/units/BoldMath.md

namespace Bold;

/// <summary>Floor and the minimum and maximum of arrays, as Delphi's Math unit.</summary>
public static partial class BoldMath
{
    /// <summary>The largest integer not above <paramref name="X"/>.</summary>
    [Delphi(PortStatus.Ported, Note = "double where Delphi has Extended.")]
    public static int Floor(double X)
    {
        int result = (int)Math.Truncate(X);
        if (X - Math.Truncate(X) < 0)
        {
            result--;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static int MaxIntValue(params int[] Data)
    {
        int result = Data[0];
        for (int i = 1; i < Data.Length; i++)
        {
            if (result < Data[i])
            {
                result = Data[i];
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static double MaxValue(params double[] Data)
    {
        double result = Data[0];
        for (int i = 1; i < Data.Length; i++)
        {
            if (result < Data[i])
            {
                result = Data[i];
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static double MinValue(params double[] Data)
    {
        double result = Data[0];
        for (int i = 1; i < Data.Length; i++)
        {
            if (result > Data[i])
            {
                result = Data[i];
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static int MinIntValue(params int[] Data)
    {
        int result = Data[0];
        for (int i = 1; i < Data.Length; i++)
        {
            if (result > Data[i])
            {
                result = Data[i];
            }
        }

        return result;
    }
}
