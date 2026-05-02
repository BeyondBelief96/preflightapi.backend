namespace PreflightApi.Tools.ApiKeyMigration;

internal static class Usage
{
    public static void Print()
    {
        Console.WriteLine("""
            preflightapi-keys — API key migration / dev seeding tool

            USAGE:
              preflightapi-keys migrate --connection-string <pg> --resource-group <rg> --apim-name <name> [options]
              preflightapi-keys seed    --connection-string <pg> --owner-id <clerk-id> [options]

            COMMANDS:
              migrate    Enumerate APIM subscriptions and import them into api_keys
              seed       Insert a single test API key into the local DB (raw key printed once)

            COMMON OPTIONS:
              --connection-string <pg>      PostgreSQL connection string (required)

            MIGRATE OPTIONS:
              --resource-group <name>       Azure resource group containing the APIM (required)
              --apim-name <name>            APIM service name (required)
              --subscription-id <guid>      Azure subscription ID (required)
              --clerk-secret-key <key>      Clerk Backend API secret. If set, fetches stripeCustomerId
                                            per user from Clerk privateMetadata for tier resolution.
              --stripe-secret-key <key>     Stripe secret API key. If set with --clerk-secret-key,
                                            looks up active subscription and assigns the matching tier.
              --price-private-pilot <id>    Stripe Price ID → PrivatePilot tier
              --price-commercial-pilot <id> Stripe Price ID → CommercialPilot tier
              --dry-run                     Log what would be inserted without writing to DB

            SEED OPTIONS:
              --owner-id <clerk-id>         Clerk user ID to attach the test key to (required)
              --tier <name>                 StudentPilot | PrivatePilot | CommercialPilot
                                            (default: StudentPilot)
              --name <label>                Display name for the key (default: "Local dev key")
              --stripe-customer-id <id>     Optional, recorded on the row
              --stripe-subscription-id <id> Optional, recorded on the row

            EXAMPLES:
              # Local dev seeding (path of least resistance for Bruno testing)
              preflightapi-keys seed \
                --connection-string "Host=localhost;Database=preflightapi;Username=preflightapi_admin;Password=localdev" \
                --owner-id user_local123 \
                --tier CommercialPilot \
                --name "Local dev key"

              # Production cutover (silent — preserves existing key values)
              preflightapi-keys migrate \
                --connection-string "Host=...;Database=preflightapi;..." \
                --subscription-id 00000000-0000-0000-0000-000000000000 \
                --resource-group rg-eastus-preflightapi-prd \
                --apim-name apim-eastus-preflightapi-prd \
                --clerk-secret-key sk_live_xxx \
                --stripe-secret-key sk_live_yyy \
                --price-private-pilot price_xxx \
                --price-commercial-pilot price_yyy \
                --dry-run
            """);
    }
}
