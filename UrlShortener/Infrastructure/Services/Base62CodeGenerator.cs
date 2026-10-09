using System.Security.Cryptography;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application;
using UrlShortener.Domain;

namespace UrlShortener.Infrastructure.Services;

/// <summary>
/// Base62 code generator (the Strategy implementation). Uses a cryptographic RNG
/// so codes are unpredictable — this is what defeats short-code enumeration (S4).
/// RandomNumberGenerator.GetInt32 is used to avoid modulo bias.
/// </summary>
public sealed class Base62CodeGenerator : ICodeGenerator
{
    private readonly int _length;

    public Base62CodeGenerator(UrlShortenerOptions options) => _length = options.CodeLength;

    public string Generate()
    {
        var alphabet = ShortCodeRules.Alphabet;
        var chars = new char[_length];
        for (var i = 0; i < _length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}
