namespace PreflightApi.Infrastructure.Services;

public interface IMagneticVariationService
{
    Task<double> GetMagneticVariation(double latitude, double longitude, CancellationToken ct = default);
}
