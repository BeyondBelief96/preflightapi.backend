using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class CertificateRenewalFunction
{
    private readonly ILogger _logger;
    private readonly ICertificateRenewalService _certificateRenewalService;

    public CertificateRenewalFunction(
        ICertificateRenewalService certificateRenewalService,
        ILoggerFactory loggerFactory)
    {
        _certificateRenewalService = certificateRenewalService ?? throw new ArgumentNullException(nameof(certificateRenewalService));
        _logger = loggerFactory.CreateLogger<CertificateRenewalFunction>();
    }

    [Function("CertificateRenewalFunction")]
    [ExponentialBackoffRetry(3, "00:01:00", "00:30:00")]
    public async Task Run([TimerTrigger("0 0 13 * * *", RunOnStartup = FunctionDefaults.RunOnStartup)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("Certificate Renewal Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _certificateRenewalService.RenewCertificateIfNeededAsync(context.CancellationToken);
        _logger.LogInformation("Certificate Renewal Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}
