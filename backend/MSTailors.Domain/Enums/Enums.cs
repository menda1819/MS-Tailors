namespace MSTailors.Domain.Enums;

public enum UserRole
{
    Customer,
    Admin
}

public enum PaymentMethod
{
    CashOnDelivery,
    Card,
    OnlineGateway
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum OrderStatus
{
    New,
    Confirmed,
    Processing,
    Packed,
    Shipped,
    Delivered,
    Cancelled,
    Returned
}

public enum CouponType
{
    Percentage,
    FixedAmount
}

public enum InquiryStatus
{
    New,
    Contacted,
    Closed
}

/// <summary>
/// The exact options offered on the inquiry form. Display strings carry spaces,
/// so each member declares its wire value explicitly rather than relying on the
/// member name (see ServiceNeededExtensions).
/// </summary>
public enum ServiceNeeded
{
    CustomSuit,
    CustomShirt,
    CustomTrousers,
    WeddingSuit,
    AlterationService,
    MeasurementAppointment,
    GeneralInquiry
}

public enum GalleryCategory
{
    Suits,
    Shirts,
    Trousers,
    WeddingWear,
    Alterations,
    Uniforms,
    TailoringWork,
    Store
}
