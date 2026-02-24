using Clerk.Net.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services
{
    public class ClerkUserService : IClerkUserService
    {
        private readonly ClerkApiClient _clerkApiClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ClerkUserService> _logger;

        private const string CacheKey = "clerk-user-emails";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
        private const int PageSize = 500;

        public ClerkUserService(
            ClerkApiClient clerkApiClient,
            IMemoryCache cache,
            ILogger<ClerkUserService> logger)
        {
            _clerkApiClient = clerkApiClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IReadOnlyList<string>> GetAllUserEmailsAsync(CancellationToken ct = default)
        {
            try
            {
                var cached = await _cache.GetOrCreateAsync(CacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                    return await FetchAllEmailsAsync(ct);
                });

                return cached ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user emails from Clerk");
                return [];
            }
        }

        private async Task<IReadOnlyList<string>> FetchAllEmailsAsync(CancellationToken ct)
        {
            var emails = new List<string>();
            var offset = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var currentOffset = offset;
                var users = await _clerkApiClient.Users.GetAsync(q =>
                {
                    q.QueryParameters.Limit = PageSize;
                    q.QueryParameters.Offset = currentOffset;
                }, ct);

                if (users == null || users.Count == 0)
                    break;

                foreach (var user in users)
                {
                    if (user.EmailAddresses == null || user.PrimaryEmailAddressId == null)
                        continue;

                    var primary = user.EmailAddresses
                        .FirstOrDefault(e => e.Id == user.PrimaryEmailAddressId);

                    if (primary?.EmailAddressProp != null)
                    {
                        emails.Add(primary.EmailAddressProp);
                    }
                }

                if (users.Count < PageSize)
                    break;

                offset += PageSize;
            }

            _logger.LogInformation("Fetched {Count} user emails from Clerk", emails.Count);
            return emails;
        }
    }
}
