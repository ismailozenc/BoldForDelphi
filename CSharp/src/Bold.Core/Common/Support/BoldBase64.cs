// Delphi: Source/Common/Support/BoldBase64.pas
// Port map: CSharp/port/units/BoldBase64.md

namespace Bold;

/// <summary>
/// Base64 (RFC 2045) on strings whose characters are bytes (0..255), as Delphi's AnsiString blobs. Result codes are the
/// BASE64_ constants.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBase64
{
    private const int AlphabetLength = 64;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private const char Pad = '=';

    private bool ffilterdecodeinput;

    [Delphi(PortStatus.Ported)]
    public TBase64()
    {
        ffilterdecodeinput = true;
    }

    [Delphi(PortStatus.Ported)]
    private static bool ValueToCharacter(byte value, ref char character)
    {
        if (value > AlphabetLength - 1)
        {
            return false;
        }

        character = Alphabet[value];
        return true;
    }

    [Delphi(PortStatus.Ported)]
    private static bool CharacterToValue(char character, ref byte value)
    {
        int p = Alphabet.IndexOf(character);
        if (p < 0)
        {
            value = 0;
            return false;
        }

        value = (byte)p;
        return true;
    }

    /// <summary>The input without characters outside the alphabet and the pad.</summary>
    [Delphi(PortStatus.Ported)]
    private static string filterLine(string InputData)
    {
        var sb = new System.Text.StringBuilder();
        byte f = 0;
        foreach (char c in InputData)
        {
            if (CharacterToValue(c, ref f) || c == Pad)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    [Delphi(PortStatus.Ported)]
    public byte EncodeData(string InputData, ref string OutputData)
    {
        var output = new System.Text.StringBuilder();
        OutputData = "";
        int inputLength = InputData.Length;
        int i = 0;
        if (inputLength == 0)
        {
            return BoldBase64.BASE64_OK;
        }

        char s = ' ';
        do
        {
            byte currentb = (byte)InputData[i++];
            inputLength--;
            if (!ValueToCharacter((byte)(currentb >> 2), ref s))
            {
                return BoldBase64.BASE64_ERROR;
            }

            output.Append(s);
            byte prevb = currentb;

            currentb = inputLength == 0 ? (byte)0 : (byte)InputData[i++];
            inputLength--;
            if (!ValueToCharacter((byte)(((prevb & 0x03) << 4) + (currentb >> 4)), ref s))
            {
                return BoldBase64.BASE64_ERROR;
            }

            output.Append(s);
            prevb = currentb;

            if (inputLength < 0)
            {
                s = Pad;
            }
            else
            {
                currentb = inputLength == 0 ? (byte)0 : (byte)InputData[i++];
                inputLength--;
                if (!ValueToCharacter((byte)(((prevb & 0x0F) << 2) + (currentb >> 6)), ref s))
                {
                    return BoldBase64.BASE64_ERROR;
                }
            }

            output.Append(s);

            if (inputLength < 0)
            {
                s = Pad;
            }
            else if (!ValueToCharacter((byte)(currentb & 0x3F), ref s))
            {
                return BoldBase64.BASE64_ERROR;
            }

            output.Append(s);
        }
        while (inputLength > 0);

        OutputData = output.ToString();
        return BoldBase64.BASE64_OK;
    }

    [Delphi(PortStatus.Ported)]
    public byte DecodeData(string InputData, ref string OutputData)
    {
        if (InputData == "")
        {
            return BoldBase64.BASE64_OK;
        }

        var output = new System.Text.StringBuilder();
        OutputData = "";
        if (FilterdecodeInput)
        {
            InputData = filterLine(InputData);
        }

        int inputLength = InputData.Length;
        if (inputLength % 4 != 0)
        {
            return BoldBase64.BASE64_LENGTH;
        }

        // i is 1-based as in Delphi.
        int i = 0;
        byte currentb = 0, prevb = 0;
        do
        {
            i++;
            if (!CharacterToValue(InputData[i - 1], ref currentb))
            {
                { OutputData = output.ToString(); return BoldBase64.BASE64_INVALID; }
            }

            i++;
            if (!CharacterToValue(InputData[i - 1], ref prevb))
            {
                { OutputData = output.ToString(); return BoldBase64.BASE64_INVALID; }
            }

            output.Append((char)(((currentb << 2) + (prevb >> 4)) & 255));

            i++;
            char s = InputData[i - 1];
            if (s == Pad)
            {
                if (i != inputLength - 1)
                {
                    { OutputData = output.ToString(); return BoldBase64.BASE64_DATALEFT; }
                }

                if (InputData[i] != Pad)
                {
                    { OutputData = output.ToString(); return BoldBase64.BASE64_PADDING; }
                }
            }
            else
            {
                if (!CharacterToValue(s, ref currentb))
                {
                    { OutputData = output.ToString(); return BoldBase64.BASE64_INVALID; }
                }

                output.Append((char)(((prevb << 4) + (currentb >> 2)) & 255));
            }

            i++;
            s = InputData[i - 1];
            if (s == Pad)
            {
                if (i != inputLength)
                {
                    { OutputData = output.ToString(); return BoldBase64.BASE64_DATALEFT; }
                }
            }
            else
            {
                if (!CharacterToValue(s, ref prevb))
                {
                    { OutputData = output.ToString(); return BoldBase64.BASE64_INVALID; }
                }

                output.Append((char)(((currentb << 6) + prevb) & 255));
            }
        }
        while (i < inputLength);

        // On failure, as in Delphi, OutputData holds what was decoded so far.
        OutputData = output.ToString();
        return BoldBase64.BASE64_OK;
    }

    [Delphi(PortStatus.Ported)]
    public bool FilterdecodeInput
    {
        get => ffilterdecodeinput;
        set => ffilterdecodeinput = value;
    }
}

/// <summary>Result codes of TBase64.</summary>
public static partial class BoldBase64
{
    [Delphi(PortStatus.Ported)] public const byte BASE64_OK = 0;
    [Delphi(PortStatus.Ported)] public const byte BASE64_ERROR = 1;
    [Delphi(PortStatus.Ported)] public const byte BASE64_INVALID = 2;
    [Delphi(PortStatus.Ported)] public const byte BASE64_LENGTH = 3;
    [Delphi(PortStatus.Ported)] public const byte BASE64_DATALEFT = 4;
    [Delphi(PortStatus.Ported)] public const byte BASE64_PADDING = 5;
}
