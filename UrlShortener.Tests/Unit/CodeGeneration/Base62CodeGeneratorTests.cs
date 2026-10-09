using Shouldly;
using UrlShortener.Application;
using UrlShortener.Domain;
using UrlShortener.Infrastructure.Services;
using Xunit;

namespace UrlShortener.UnitTests.CodeGeneration;

public sealed class Base62CodeGeneratorTests
{
    [Fact]
    public void Generate_returns_code_of_configured_length()
    {
        var generator = new Base62CodeGenerator(new UrlShortenerOptions { CodeLength = 7 });

        generator.Generate().Length.ShouldBe(7);
    }

    [Fact]
    public void Generate_uses_only_base62_characters()
    {
        var generator = new Base62CodeGenerator(new UrlShortenerOptions { CodeLength = 10 });

        for (var i = 0; i < 100; i++)
        {
            var code = generator.Generate();
            code.ShouldAllBe(c => ShortCodeRules.Alphabet.Contains(c));
        }
    }

    [Fact]
    public void Generate_produces_varied_output()
    {
        var generator = new Base62CodeGenerator(new UrlShortenerOptions { CodeLength = 8 });
        var produced = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            produced.Add(generator.Generate());
        }

        // With 62^8 keyspace, 1000 draws should almost never collide.
        produced.Count.ShouldBeGreaterThan(990);
    }
}
