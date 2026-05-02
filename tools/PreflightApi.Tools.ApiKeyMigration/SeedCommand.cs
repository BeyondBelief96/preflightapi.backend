using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;

namespace PreflightApi.Tools.ApiKeyMigration;

internal static class SeedCommand
{
    public static async Task<int> RunAsync(Dictionary<string, string> opts)
    {
        var connectionString = opts.Required("connection-string");
        var ownerId = opts.Required("owner-id");
        var name = opts.Optional("name") ?? "Local dev key";
        var tierName = opts.Optional("tier") ?? nameof(SubscriptionTier.StudentPilot);
        var stripeCustomerId = opts.Optional("stripe-customer-id");
        var stripeSubscriptionId = opts.Optional("stripe-subscription-id");

        if (!Enum.TryParse<SubscriptionTier>(tierName, out var tier))
        {
            Console.Error.WriteLine($"unknown tier '{tierName}'. valid: StudentPilot, PrivatePilot, CommercialPilot");
            return 1;
        }

        var rawKey = KeyHashing.GeneratePfaKey();
        var prefix = rawKey[..12];
        var keyHash = KeyHashing.Sha256(rawKey);
        var now = DateTime.UtcNow;
        var quotaResetAt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        await using var ctx = DbContextFactory.Create(connectionString);
        ctx.ApiKeys.Add(new ApiKey
        {
            OwnerId = ownerId,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            Prefix = prefix,
            KeyHash = keyHash,
            Name = name,
            Tier = tier,
            IsActive = true,
            CreatedAt = now,
            MonthlyRequestCount = 0,
            QuotaResetAt = quotaResetAt
        });
        await ctx.SaveChangesAsync();

        Console.WriteLine($"Created {tier} key for {ownerId}");
        Console.WriteLine($"Prefix: {prefix}");
        Console.WriteLine();
        Console.WriteLine("Raw key (shown once — copy now):");
        Console.WriteLine(rawKey);
        return 0;
    }
}
