namespace MSTailors.Domain.Enums;

/// <summary>
/// Display labels for enums whose UI text contains spaces.
/// </summary>
/// <remarks>
/// The stable PascalCase member name is what gets stored in MongoDB. The label
/// is presentation only — renaming a label must never change stored data, which
/// is exactly why the two are kept apart.
/// </remarks>
public static class EnumDisplayExtensions
{
    public static string ToDisplayName(this ServiceNeeded value) => value switch
    {
        ServiceNeeded.CustomSuit => "Custom Suit",
        ServiceNeeded.CustomShirt => "Custom Shirt",
        ServiceNeeded.CustomTrousers => "Custom Trousers",
        ServiceNeeded.WeddingSuit => "Wedding Suit",
        ServiceNeeded.AlterationService => "Alteration Service",
        ServiceNeeded.MeasurementAppointment => "Measurement Appointment",
        ServiceNeeded.GeneralInquiry => "General Inquiry",
        _ => value.ToString()
    };

    public static string ToDisplayName(this GalleryCategory value) => value switch
    {
        GalleryCategory.WeddingWear => "Wedding Wear",
        GalleryCategory.TailoringWork => "Tailoring Work",
        _ => value.ToString()
    };
}
