using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MSTailors.Infrastructure.Seeding;

namespace MSTailors.Tests.Seeding;

/// <summary>
/// Regression guard. These annotations are the only thing standing between a
/// missing Seed__AdminPassword and an admin account whose password hash is
/// derived from the empty string.
/// </summary>
public class SeedOptionsTests
{
    private static List<ValidationResult> Validate(SeedOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void An_empty_admin_password_is_rejected()
    {
        var results = Validate(new SeedOptions { AdminPassword = string.Empty });

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SeedOptions.AdminPassword)));
    }

    [Fact]
    public void A_short_admin_password_is_rejected()
    {
        var results = Validate(new SeedOptions { AdminPassword = "short" });

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SeedOptions.AdminPassword)));
    }

    [Fact]
    public void A_malformed_admin_email_is_rejected()
    {
        var results = Validate(new SeedOptions
        {
            AdminEmail = "not-an-email",
            AdminPassword = "a-sufficiently-long-password"
        });

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SeedOptions.AdminEmail)));
    }

    [Fact]
    public void A_complete_configuration_validates()
    {
        var results = Validate(new SeedOptions
        {
            AdminEmail = "admin@mstailors.lk",
            AdminPassword = "a-sufficiently-long-password"
        });

        results.Should().BeEmpty();
    }
}
