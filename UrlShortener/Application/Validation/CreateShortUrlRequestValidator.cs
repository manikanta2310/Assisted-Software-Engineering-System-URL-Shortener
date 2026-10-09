using System.Net;
using FluentValidation;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain;

namespace UrlShortener.Application.Validation;

/// <summary>
/// Validates create requests at the API edge. This is a security control, not
/// just cosmetics: the URL rules defend against open-redirect and basic SSRF
/// (threat S1), and the alias rules defend against route shadowing and injection
/// (threat S5). Each rule is independently unit-tested.
/// </summary>
public sealed class CreateShortUrlRequestValidator : AbstractValidator<CreateShortUrlRequest>
{
    public CreateShortUrlRequestValidator(UrlShortenerOptions options)
    {
        RuleFor(x => x.LongUrl)
            .NotEmpty().WithMessage("A target URL is required.")
            .MaximumLength(options.MaxUrlLength)
                .WithMessage($"URL must be at most {options.MaxUrlLength} characters.")
            .Must(BeASafeHttpUrl)
                .WithMessage("URL must be an absolute http/https address and must not point to an internal host.");

        When(x => !string.IsNullOrWhiteSpace(x.CustomAlias), () =>
        {
            RuleFor(x => x.CustomAlias!)
                .Must(ShortCodeRules.IsValidAlias)
                .WithMessage(
                    $"Alias must be {ShortCodeRules.MinLength}-{ShortCodeRules.MaxLength} characters, " +
                    "use only letters and digits, and not be a reserved word.");
        });

        When(x => x.ExpiresInDays.HasValue, () =>
        {
            RuleFor(x => x.ExpiresInDays!.Value)
                .GreaterThan(0).WithMessage("Expiry must be at least 1 day.")
                .LessThanOrEqualTo(3650).WithMessage("Expiry must be at most 3650 days.");
        });
    }

    /// <summary>
    /// Accepts only absolute http/https URLs whose host is not loopback,
    /// private, or link-local. This blocks the obvious SSRF vectors; full SSRF
    /// protection (resolving the host and re-checking the resolved IP) is a
    /// documented production item — see docs/risks.md.
    /// </summary>
    private static bool BeASafeHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (uri.IsLoopback)
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            return !IsPrivateOrReserved(ip);
        }

        // Named host: block obvious internal names.
        return !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (b[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (b[0] == 192 && b[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 link-local
            if (b[0] == 169 && b[1] == 254)
            {
                return true;
            }

            // 0.0.0.0/8
            if (b[0] == 0)
            {
                return true;
            }

            return false;
        }

        // IPv6 private / link-local ranges
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
    }
}
