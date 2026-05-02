using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using Stripe;

namespace PreflightApi.Tools.ApiKeyMigration;

internal static class MigrateCommand
{
    public static async Task<int> RunAsync(Dictionary<string, string> opts)
    {
        var connectionString = opts.Required("connection-string");
        var subscriptionId   = opts.Required("subscription-id");
        var resourceGroup    = opts.Required("resource-group");
        var apimName         = opts.Required("apim-name");
        var clerkSecret      = opts.Optional("clerk-secret-key");
        var stripeSecret     = opts.Optional("stripe-secret-key");
        var dryRun           = opts.Flag("dry-run");

        var priceMap = new Dictionary<string, SubscriptionTier>(StringComparer.OrdinalIgnoreCase);
        if (opts.Optional("price-private-pilot") is { } pPriv)    priceMap[pPriv] = SubscriptionTier.PrivatePilot;
        if (opts.Optional("price-commercial-pilot") is { } pComm) priceMap[pComm] = SubscriptionTier.CommercialPilot;

        if (stripeSecret != null) StripeConfiguration.ApiKey = stripeSecret;

        Console.WriteLine($"=> Enumerating APIM subscriptions: {apimName}");
        var apimSubs = await EnumerateApimSubscriptionsAsync(subscriptionId, resourceGroup, apimName);
        Console.WriteLine($"   Found {apimSubs.Count} subscriptions");

        if (apimSubs.Count == 0) return 0;

        using var clerkHttp = new HttpClient { BaseAddress = new Uri("https://api.clerk.com/v1/") };
        if (clerkSecret != null)
            clerkHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clerkSecret);

        await using var ctx = DbContextFactory.Create(connectionString);

        // Pre-fetch existing key hashes so re-runs are idempotent
        var existingHashes = await ctx.ApiKeys
            .AsNoTracking()
            .Select(k => k.KeyHash)
            .ToHashSetAsync();

        int imported = 0, skippedDup = 0, skippedNoKey = 0, errors = 0;

        foreach (var sub in apimSubs)
        {
            try
            {
                if (string.IsNullOrEmpty(sub.PrimaryKey))
                {
                    Console.WriteLine($"   skip {sub.DisplayName}: no primary key");
                    skippedNoKey++;
                    continue;
                }

                var keyHash = KeyHashing.Sha256(sub.PrimaryKey);
                if (existingHashes.Contains(keyHash))
                {
                    skippedDup++;
                    continue;
                }

                var ownerId = sub.DisplayName ?? sub.Name;
                string? stripeCustomerId = null;
                if (clerkSecret != null)
                    stripeCustomerId = await TryGetStripeCustomerIdAsync(clerkHttp, ownerId);

                SubscriptionTier tier = SubscriptionTier.StudentPilot;
                string? stripeSubscriptionId = null;
                if (stripeCustomerId != null && stripeSecret != null)
                    (tier, stripeSubscriptionId) = await ResolveTierAsync(stripeCustomerId, priceMap);

                var prefix = sub.PrimaryKey.Length >= 12 ? sub.PrimaryKey[..12] : sub.PrimaryKey;
                var now = DateTime.UtcNow;
                var entity = new ApiKey
                {
                    OwnerId = ownerId,
                    StripeCustomerId = stripeCustomerId,
                    StripeSubscriptionId = stripeSubscriptionId,
                    Prefix = prefix,
                    KeyHash = keyHash,
                    Name = "Migrated from APIM",
                    Tier = tier,
                    IsActive = true,
                    CreatedAt = now,
                    MonthlyRequestCount = 0,
                    QuotaResetAt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
                };

                if (dryRun)
                {
                    Console.WriteLine($"   [dry-run] would import {ownerId} → {tier}");
                }
                else
                {
                    ctx.ApiKeys.Add(entity);
                    await ctx.SaveChangesAsync();
                    Console.WriteLine($"   imported {ownerId} → {tier}");
                }
                imported++;
            }
            catch (Exception ex)
            {
                errors++;
                Console.Error.WriteLine($"   ERROR on {sub.DisplayName}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: imported={imported} skipped-dup={skippedDup} skipped-no-key={skippedNoKey} errors={errors}");
        return errors > 0 ? 2 : 0;
    }

    private record ApimSubscription(string Name, string? DisplayName, string? PrimaryKey);

    private static async Task<List<ApimSubscription>> EnumerateApimSubscriptionsAsync(
        string subscriptionId, string resourceGroup, string apimName)
    {
        var armClient = new ArmClient(new DefaultAzureCredential());
        var apimResourceId = ApiManagementServiceResource.CreateResourceIdentifier(
            subscriptionId, resourceGroup, apimName);
        var apim = armClient.GetApiManagementServiceResource(apimResourceId);

        // Enumerate ALL subscriptions on the APIM service regardless of product.
        // displayName is conventionally the Clerk user ID; primaryKey is the raw
        // header value users currently send.
        var result = new List<ApimSubscription>();
        await foreach (var sub in apim.GetApiManagementSubscriptions().GetAllAsync())
        {
            var keysResp = await sub.GetSecretsAsync();
            result.Add(new ApimSubscription(
                Name: sub.Data.Name,
                DisplayName: sub.Data.DisplayName,
                PrimaryKey: keysResp.Value.PrimaryKey));
        }
        return result;
    }

    private static async Task<string?> TryGetStripeCustomerIdAsync(HttpClient clerkHttp, string clerkUserId)
    {
        try
        {
            using var resp = await clerkHttp.GetAsync($"users/{clerkUserId}");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("private_metadata", out var meta)) return null;
            if (!meta.TryGetProperty("stripeCustomerId", out var sc) || sc.ValueKind != JsonValueKind.String) return null;
            return sc.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(SubscriptionTier tier, string? subscriptionId)> ResolveTierAsync(
        string stripeCustomerId, Dictionary<string, SubscriptionTier> priceMap)
    {
        try
        {
            var subs = await new SubscriptionService().ListAsync(new SubscriptionListOptions
            {
                Customer = stripeCustomerId,
                Status = "active",
                Limit = 1
            });

            var active = subs.Data.FirstOrDefault();
            if (active == null) return (SubscriptionTier.StudentPilot, null);

            var priceId = active.Items?.Data?.FirstOrDefault()?.Price?.Id;
            if (priceId != null && priceMap.TryGetValue(priceId, out var tier))
                return (tier, active.Id);

            return (SubscriptionTier.StudentPilot, active.Id);
        }
        catch
        {
            return (SubscriptionTier.StudentPilot, null);
        }
    }
}
