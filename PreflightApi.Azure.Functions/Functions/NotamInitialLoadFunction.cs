using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class NotamInitialLoadFunction
{
    private readonly ILogger _logger;
    private readonly INotamInitialLoadCronService _notamInitialLoadService;
    private readonly PreflightApiDbContext _dbContext;

    public NotamInitialLoadFunction(
        INotamInitialLoadCronService notamInitialLoadService,
        PreflightApiDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        _notamInitialLoadService = notamInitialLoadService ?? throw new ArgumentNullException(nameof(notamInitialLoadService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = loggerFactory.CreateLogger<NotamInitialLoadFunction>();
    }

    [Function("NotamInitialLoadFunction")]
    [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
    public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
    {
        // On startup, only run if the database is empty
        //if (myTimer.IsPastDue)
        //{
        //    var hasNotams = await _dbContext.Notams.AnyAsync(context.CancellationToken);
        //    if (hasNotams)
        //    {
        //        _logger.LogInformation("NOTAM Initial Load skipped — database already populated (past due trigger)");
        //        return;
        //    }
        //}

        _logger.LogInformation("NOTAM Initial Load Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _notamInitialLoadService.LoadAllClassificationsAsync(context.CancellationToken);
        _logger.LogInformation("NOTAM Initial Load Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}
