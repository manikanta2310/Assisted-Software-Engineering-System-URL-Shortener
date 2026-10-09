using UrlShortener.Application.Interfaces;

namespace UrlShortener.UnitTests.TestDoubles;

/// <summary>Returns a predetermined sequence of codes, so collision/retry logic
/// can be exercised deterministically.</summary>
internal sealed class SequenceCodeGenerator : ICodeGenerator
{
    private readonly Queue<string> _codes;

    public SequenceCodeGenerator(params string[] codes) => _codes = new Queue<string>(codes);

    public string Generate() =>
        _codes.Count > 0 ? _codes.Dequeue() : throw new InvalidOperationException("No more codes queued.");
}
