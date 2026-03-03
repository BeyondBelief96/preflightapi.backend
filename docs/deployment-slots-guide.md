# Zero-Downtime Deployments with App Service Deployment Slots

## Current State

- **PRD API**: Deploys directly to the production App Service (`was-eastus-preflightapi-prd`), restarts it, and waits up to 5 minutes for it to come back. During this window APIM routes traffic to an unresponsive backend.
- **PRD Functions**: Stop → deploy → start. Acceptable because Functions are background cron jobs, not user-facing.
- **App Service Plan SKU**: B2 (Basic) — does **not** support deployment slots.

## Prerequisites

### Upgrade App Service Plan to Standard (S1)

Deployment slots require **Standard (S1) tier or higher**.

| Tier | Price (~) | Slots |
|------|-----------|-------|
| B2 (current) | ~$55/mo | 0 |
| S1 | ~$73/mo | 5 |

Update in Bicep parameters:

```
# infra/parameters/prod.bicepparam
param apiSkuName = 'S1'
param apiSkuTier = 'Standard'
```

### Azure Functions (Flex Consumption)

Flex Consumption does **not** support deployment slots. The current stop/deploy/start pattern is the correct approach and doesn't need to change.

## How Slot Swapping Works

1. New code is deployed to the **staging** slot (`was-eastus-preflightapi-prd-staging.azurewebsites.net`)
2. Azure warms up the staging slot instances
3. `az webapp deployment slot swap` atomically switches which slot serves the **production hostname**
4. The production URL (`was-eastus-preflightapi-prd.azurewebsites.net`) never changes — APIM and the frontend on Railway are completely unaffected

### APIM Is Transparent to Swaps

APIM points to `was-eastus-preflightapi-prd.azurewebsites.net`. That hostname is owned by the production slot. During a swap, Azure internally reroutes which deployment serves that hostname. APIM doesn't need any reconfiguration.

### Both Slots Share the PRD Database

Both the production and staging slots connect to the same PRD PostgreSQL database. This is intentional:

1. Migrations run first (additive schema changes)
2. New code deploys to staging (works with the new schema)
3. Old code keeps running in production (still works because migrations were backwards-compatible)
4. Swap happens — new code becomes production seamlessly

**Important**: Migrations must be **backwards-compatible** (additive only). You cannot rename or drop columns/tables that the currently-running production code still depends on. If a breaking schema change is needed, it must be split across two releases:
- Release 1: Add new column, update code to use both old and new
- Release 2: Remove old column after all code references are gone

## Pipeline Changes (main-ci-cd.yml)

### Current Flow

```
build → migrations → deploy-api (direct to production + restart) → deploy-functions
```

### Target Flow

```
build → migrations → deploy-to-staging-slot → smoke-test-staging → swap → deploy-functions
```

### Key Differences

| Step | Current | With Slots |
|------|---------|------------|
| Deploy API | `azure/webapps-deploy` to production | `azure/webapps-deploy` with `slot-name: staging` |
| Restart | `az webapp restart` (causes downtime) | Not needed — swap handles warm-up |
| Validation | Wait for production to respond | Smoke test staging slot before swap |
| Swap | N/A | `az webapp deployment slot swap` |
| Rollback | Redeploy previous version | Swap again (instant) |

### Example Deploy + Swap Steps

```yaml
- name: Deploy API to staging slot
  uses: azure/webapps-deploy@v3
  with:
    app-name: was-eastus-preflightapi-prd
    slot-name: staging
    package: ./api-publish

- name: Smoke test staging slot
  run: |
    for i in $(seq 1 20); do
      if curl -sf "https://was-eastus-preflightapi-prd-staging.azurewebsites.net/health"; then
        echo "Staging slot is healthy"
        exit 0
      fi
      sleep 5
    done
    echo "::error::Staging slot failed health check"
    exit 1

- name: Swap staging to production
  run: |
    az webapp deployment slot swap \
      --resource-group rg-eastus-preflightapi-prd \
      --name was-eastus-preflightapi-prd \
      --slot staging \
      --target-slot production
```

### APIM Policies and OpenAPI Import

These target the APIM service (not the App Service), so they remain unchanged. Deploy policies and import the OpenAPI spec after the swap, same as today.

## Bicep Changes

### Add Staging Slot to `infra/modules/app-service.bicep`

```bicep
resource stagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: webApp
  name: 'staging'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    siteConfig: webApp.properties.siteConfig
  }
}
```

Note: The staging slot inherits app settings from the parent by default. Settings that should differ per slot (e.g., logging level) can be marked as "slot sticky" via `slotConfigNames`.

### RBAC for Staging Slot

The staging slot gets its own managed identity. It needs the same RBAC roles as the production slot:
- **Storage Blob Data Contributor** on the storage account
- Any other roles the API's MI currently has

## Rollback

One of the biggest advantages of deployment slots: if something goes wrong after a swap, you can **swap back instantly**:

```bash
az webapp deployment slot swap \
  --resource-group rg-eastus-preflightapi-prd \
  --name was-eastus-preflightapi-prd \
  --slot staging \
  --target-slot production
```

This puts the previous production code back in the production slot. No rebuild, no redeploy.

**Caveat**: Rollback does not undo database migrations. If a migration was destructive (dropped a column), swapping back won't restore it. This reinforces the requirement for backwards-compatible migrations.
