namespace UrlShortener.Application.Interfaces;

/// <summary>
/// Strategy for producing a short-code candidate. Pure: it only generates a
/// string of the configured length. Uniqueness is the service's concern (it
/// retries against the repository), which keeps this component trivially testable
/// and swappable (Base62 today, a hash or Snowflake-style scheme tomorrow).
/// </summary>
public interface ICodeGenerator
{
    string Generate();
}
