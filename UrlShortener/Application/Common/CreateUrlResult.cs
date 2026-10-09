using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Common;

public enum CreateStatus
{
    Created,
    AliasAlreadyTaken,
}

/// <summary>
/// Outcome of a create request. A taken custom alias is an expected outcome, not
/// an exceptional one, so it is modelled as a result (like <see cref="ResolveResult"/>)
/// rather than thrown — the controller maps it to 409.
/// </summary>
public sealed record CreateUrlResult(CreateStatus Status, ShortUrlResponse? Response)
{
    public static CreateUrlResult Created(ShortUrlResponse response) =>
        new(CreateStatus.Created, response);

    public static readonly CreateUrlResult AliasAlreadyTaken =
        new(CreateStatus.AliasAlreadyTaken, null);
}
