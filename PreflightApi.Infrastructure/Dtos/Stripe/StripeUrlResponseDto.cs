namespace PreflightApi.Infrastructure.Dtos.Stripe;

public record StripeUrlResponseDto
{
    public string Url { get; set; } = string.Empty;
}