using System.Text.RegularExpressions;
using FluentValidation;

namespace MSTailors.Application.Validation;

public static partial class CommonRules
{
    /// <summary>Lowercase words separated by single hyphens. No leading/trailing hyphen.</summary>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public static partial Regex SlugPattern();

    /// <summary>
    /// Sri Lankan mobile or landline, with or without +94. Deliberately liberal
    /// about spaces and dashes — rejecting a real customer's phone number costs
    /// far more than accepting a slightly odd one.
    /// </summary>
    [GeneratedRegex(@"^(\+94|0)[0-9\s\-]{8,14}$")]
    public static partial Regex SriLankanPhonePattern();

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    public static partial Regex HexColourPattern();

    public static IRuleBuilderOptions<T, string> Slug<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("A slug is required.")
            .MaximumLength(120)
            .Must(value => SlugPattern().IsMatch(value.Trim().ToLowerInvariant()))
            .WithMessage("A slug may contain only lowercase letters, numbers and single hyphens.");

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("An email address is required.")
            .MaximumLength(254)
            .EmailAddress().WithMessage("Enter a valid email address.");

    public static IRuleBuilderOptions<T, string> Phone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("A phone number is required.")
            .Must(value => SriLankanPhonePattern().IsMatch(value.Trim()))
            .WithMessage("Enter a valid Sri Lankan phone number, for example 0771234567 or +94771234567.");

    /// <summary>
    /// Minimum twelve characters and not entirely one repeated character.
    /// Length beats composition rules for real-world strength.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("A password is required.")
            .MinimumLength(12).WithMessage("Use at least 12 characters.")
            .MaximumLength(256).WithMessage("Passwords cannot exceed 256 characters.")
            .Must(value => value.Distinct().Count() > 3)
            .WithMessage("That password is too repetitive.");

    public static IRuleBuilderOptions<T, decimal> Money<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0).WithMessage("Amount cannot be negative.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Amount is implausibly large.")
            .Must(value => decimal.Round(value, 2) == value)
            .WithMessage("Amounts may have at most two decimal places.");

    public static IRuleBuilderOptions<T, string> ObjectId<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(value => MongoDB.Bson.ObjectId.TryParse(value, out _))
            .WithMessage("That identifier is not valid.");
}
