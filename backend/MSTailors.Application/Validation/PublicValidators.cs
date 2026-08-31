using FluentValidation;
using MSTailors.Application.Auth.Dtos;
using MSTailors.Application.Carts.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Content.Dtos;
using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Orders.Dtos;

namespace MSTailors.Application.Validation;

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ProductQueryRequestValidator : AbstractValidator<ProductQueryRequest>
{
    public ProductQueryRequestValidator()
    {
        Include(new PageRequestValidator());

        RuleFor(request => request.Q).MaximumLength(120);
        RuleFor(request => request.Size).MaximumLength(20);
        RuleFor(request => request.Color).MaximumLength(40);
        RuleFor(request => request.MinPrice).GreaterThanOrEqualTo(0).When(r => r.MinPrice.HasValue);
        RuleFor(request => request.MaxPrice).GreaterThanOrEqualTo(0).When(r => r.MaxPrice.HasValue);

        RuleFor(request => request)
            .Must(request => request.MinPrice <= request.MaxPrice)
            .When(request => request.MinPrice.HasValue && request.MaxPrice.HasValue)
            .WithName("price")
            .WithMessage("The minimum price cannot be greater than the maximum price.");
    }
}

public sealed class CreateInquiryRequestValidator : AbstractValidator<CreateInquiryRequest>
{
    public CreateInquiryRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Your name is required.")
            .MaximumLength(120);

        RuleFor(request => request.Email).Email();
        RuleFor(request => request.Phone).Phone();

        RuleFor(request => request.ServiceNeeded)
            .IsInEnum().WithMessage("Choose one of the listed services.");

        RuleFor(request => request.Message)
            .NotEmpty().WithMessage("Tell us a little about what you need.")
            .MinimumLength(10).WithMessage("Please give us a bit more detail.")
            .MaximumLength(4000);

        RuleFor(request => request.PreferredDate)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .When(request => request.PreferredDate.HasValue)
            .WithMessage("Choose a date that is not in the past.");

        // The honeypot is not validated here — a filled value is handled in the
        // service, which returns a normal-looking success so bots learn nothing.
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Email).Email();
        RuleFor(request => request.Phone).Phone();
        RuleFor(request => request.Password).Password();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Deliberately weak rules: applying password policy at login would
        // reject legacy passwords and hint at the policy to an attacker.
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Password).NotEmpty();
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(request => request.Email).Email();
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email).Email();
        RuleFor(request => request.Token).NotEmpty().MaximumLength(512);
        RuleFor(request => request.NewPassword).Password();
    }
}

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(request => request.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(request => request.Quantity)
            .InclusiveBetween(1, 99)
            .WithMessage("Quantity must be between 1 and 99.");
    }
}

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator() =>
        RuleFor(request => request.Quantity)
            .InclusiveBetween(0, 99)
            .WithMessage("Quantity must be between 0 and 99. Use 0 to remove the item.");
}

public sealed class ApplyCouponRequestValidator : AbstractValidator<ApplyCouponRequest>
{
    public ApplyCouponRequestValidator() =>
        RuleFor(request => request.Code).NotEmpty().MaximumLength(40);
}

public sealed class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    public ContactRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Email).Email();
        RuleFor(request => request.Phone).Phone();
    }
}

public sealed class ShippingAddressRequestValidator : AbstractValidator<ShippingAddressRequest>
{
    public ShippingAddressRequestValidator()
    {
        RuleFor(request => request.Line1).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Line2).MaximumLength(200);
        RuleFor(request => request.City).NotEmpty().MaximumLength(100);
        RuleFor(request => request.PostalCode).MaximumLength(20);
    }
}

public sealed class OrderLineRequestValidator : AbstractValidator<OrderLineRequest>
{
    public OrderLineRequestValidator()
    {
        RuleFor(line => line.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(line => line.Quantity).InclusiveBetween(1, 99);
    }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Contact).NotNull().SetValidator(new ContactRequestValidator());
        RuleFor(request => request.ShippingAddress).NotNull().SetValidator(new ShippingAddressRequestValidator());
        RuleFor(request => request.SpecialInstructions).MaximumLength(1000);
        RuleFor(request => request.PaymentMethod).IsInEnum();
        RuleFor(request => request.CouponCode).MaximumLength(40);

        RuleForEach(request => request.Items)
            .SetValidator(new OrderLineRequestValidator())
            .When(request => request.Items is not null);

        // Note there is no price field anywhere in this request. Totals are
        // recomputed server-side, so there is nothing for a client to forge.
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<Customers.Dtos.UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Phone).Phone();
    }
}

public sealed class AddressRequestValidator : AbstractValidator<Customers.Dtos.AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(request => request.Label).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Line1).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Line2).MaximumLength(200);
        RuleFor(request => request.City).NotEmpty().MaximumLength(100);
        RuleFor(request => request.PostalCode).MaximumLength(20);
    }
}

public sealed class AddWishlistItemRequestValidator : AbstractValidator<Customers.Dtos.AddWishlistItemRequest>
{
    public AddWishlistItemRequestValidator()
    {
        RuleFor(request => request.ProductId).ObjectId();
        RuleFor(request => request.VariantSku).MaximumLength(64);
    }
}
