using MSTailors.Domain.Enums;

namespace MSTailors.Application.Content.Dtos;

public sealed class CreateInquiryRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public ServiceNeeded ServiceNeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime? PreferredDate { get; init; }

    /// <summary>
    /// Honeypot. Hidden from real users with CSS, so anything that fills it in
    /// is a bot. Named plausibly on purpose — "honeypot" would be a giveaway.
    /// </summary>
    public string? Website { get; init; }
}
