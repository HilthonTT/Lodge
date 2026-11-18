using System.Text;

namespace Lodge.Infrastructure.Cryptography;

/// <summary>
/// RFC 4648 Base32 (alphabet <c>A-Z2-7</c>), as used by authenticator apps for TOTP secrets. Encoding omits
/// padding; decoding ignores case, spaces, dashes and padding.
/// </summary>
internal static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(ReadOnlySpan<byte> data)
    {
        var builder = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                builder.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            builder.Append(Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return builder.ToString();
    }

    public static bool TryDecode(string value, out byte[] bytes)
    {
        var output = new List<byte>(value.Length * 5 / 8);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in value)
        {
            if (c is ' ' or '-' or '=')
            {
                continue;
            }

            int index = Alphabet.IndexOf(char.ToUpperInvariant(c), StringComparison.Ordinal);

            if (index < 0)
            {
                bytes = [];
                return false;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        bytes = [.. output];
        return bytes.Length > 0;
    }
}
