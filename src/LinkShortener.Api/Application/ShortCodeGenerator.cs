using System.Security.Cryptography;

namespace LinkShortener.Api.Application;

public sealed class ShortCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 7;

    public string Generate()
    {
        var code = new char[CodeLength];

        for (var index = 0; index < code.Length; index++)
        {
            code[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
