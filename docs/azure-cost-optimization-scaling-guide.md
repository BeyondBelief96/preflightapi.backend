# Azure Infrastructure Cost Optimization & Scaling Guide

**PreflightApi Aviation Data Platform**

**February 2026**

---

## Executive Summary

- **Current estimated cost:** ~$130/month
- **Optimized cost:** ~$28-35/month (saving ~$100/month)
- **Scales from** $30/month **to** $2,000+/month with clear upgrade triggers

---

## Table of Contents

- [Current Infrastructure State](#current-infrastructure-state)
- [Phase 1: Bootstrap (0 Users)](#phase-1-bootstrap-0-users)
- [Phase 2: Early Traction (1-100 Users)](#phase-2-early-traction-1-100-users)
- [Phase 3: Growth (100-1,000 Users)](#phase-3-growth-100-1000-users)
- [Phase 4: Scale (1,000+ Users)](#phase-4-scale-1000-users)
- [Phase 5: High Scale (10,000+ Users)](#phase-5-high-scale-10000-users)
- [Scaling Decision Matrix](#scaling-decision-matrix)
- [Cost Trajectory Overview](#cost-trajectory-overview)
- [Immediate Action Items](#immediate-action-items)

---

## Current Infrastructure State

The following table summarizes the current Azure resource configuration and associated monthly costs for the PreflightApi platform.

| Resource | Current SKU | Monthly Cost | Status |
|---|---|---|---|
| PostgreSQL Flexible Server | Standard_B1ms (Burstable) | ~$12 | Optimal |
| App Service Plan (API) | B2 (2 cores, 3.5 GB) | ~$54 | Oversized |
| Function App | Flex Consumption (FC1) | ~$1-3 | Optimal |
| API Management | Developer (1 unit) | ~$49 | Oversized |
| Storage (Blob) | Standard_RAGRS | ~$1-2 | Oversized |
| Functions Storage | Standard_LRS | <$1 | Optimal |
| App Insights + Log Analytics | Pay-per-GB | ~$0-5 | OK |
| **Total** | | **~$130** | |

---

## Phase 1: Bootstrap (0 Users)

**Target: ~$28-35/month** — Minimize fixed costs, pay-per-use where possible.

### PostgreSQL Flexible Server — Keep B1ms (~$12/month)

| Setting | Current | Recommendation |
|---|---|---|
| SKU | Standard_B1ms | Keep — cheapest viable tier |
| Storage | 32 GB | Keep — minimum size |
| Backup Retention | 7 days | Keep — minimum |
| Geo-Redundant Backup | Disabled | Keep disabled |
| High Availability | Disabled | Keep disabled |

Your cron jobs run sequentially, and with zero API traffic the burstable tier will barely touch its CPU credits. PostGIS queries on NASR data are read-heavy and well within B1ms capability.

### App Service Plan (API) — Downgrade B2 → B1 (~$13/month)

**Saves ~$41/month**

| Setting | Current | Recommendation |
|---|---|---|
| SKU | B2 (2 cores, 3.5 GB) | B1 (1 core, 1.75 GB) |
| OS | Linux | Keep |
| Always On | Enabled | Keep — prevents cold starts |
| Auto-scale | N/A (Basic tier) | N/A |

Your API is a thin read layer over PostgreSQL. With zero users, 1 core and 1.75 GB RAM is more than enough. Do not use F1 (Free) — it lacks Always On, has a 60 min/day compute limit, and no custom SSL.

### Function App — Keep Flex Consumption (~$1-3/month)

Already optimal. With 15 functions running on schedules:

- 5 weather functions × 6/hour × 24 hours = 720 executions/day
- 10 NASR/daily functions × 1/day = 10 executions/day
- ~22,000 executions/month — first 400,000 are free
- You will pay only pennies for GB-seconds of compute time

### API Management — Downgrade Developer → Consumption (~$0-5/month)

**Saves ~$45/month** — This is your biggest savings opportunity.

| Setting | Current | Recommendation |
|---|---|---|
| SKU | Developer (~$49/month) | Consumption (~$3.50/1M calls) |
| SLA | None | None |
| Free Tier | N/A | First 1M calls/month free |

Trade-offs of Consumption tier:

- Cold starts (5-15 second delay after idle) — acceptable with zero users
- No built-in developer portal — host docs separately
- No virtual network support
- Existing policies (rate limiting, caching, tiered products) all work on Consumption

> **Important:** Consumption APIM handles named values differently. Test your policies after migration.

### Blob Storage — Downgrade RAGRS → LRS (~$0.50/month)

**Saves ~60% on storage costs**

| Setting | Current | Recommendation |
|---|---|---|
| Primary Storage SKU | Standard_RAGRS | Standard_LRS |
| Functions Storage | Standard_LRS | Keep |
| Access Tier | Hot | Keep (diagrams need fast access) |

Geo-redundant replication is insurance you don't need with zero users. Airport diagrams and chart supplements can be re-synced from FAA if a datacenter fails. Upgrade to GRS when you have paying customers.

### Monitoring — Cap Ingestion (~$0/month)

- Set Application Insights daily cap to 0.1 GB as a safety net
- Free allowance: 5 GB/month — with zero users you'll stay well under
- Keep Log Analytics at PerGB2018 with 30-day retention

### Phase 1 Cost Summary

| Resource | Monthly Cost |
|---|---|
| PostgreSQL B1ms | $12.41 |
| App Service B1 | $13.14 |
| Function App (Flex Consumption) | ~$1-3 |
| APIM Consumption | ~$0-5 |
| Storage (LRS + Functions) | ~$1-2 |
| App Insights / Log Analytics | ~$0 (free tier) |
| **Total** | **~$28-35/month** |

---

## Phase 2: Early Traction (1-100 Users)

**Target: ~$80-150/month** — You have paying subscribers and need production SLA.

| Resource | Change | New Cost | Priority |
|---|---|---|---|
| PostgreSQL | Keep B1ms | $12 | Low |
| App Service | Keep B1 (monitor CPU %) | $13 | Low |
| APIM | Upgrade to Basic v2 or stay Consumption | $5-170 | Medium |
| Storage | Upgrade to GRS | ~$2-3 | Medium |
| App Insights | Raise daily cap | ~$0-5 | Low |

### APIM Decision Point

The APIM tier is the critical decision at this phase:

- **If cold starts annoy users:** upgrade to Basic v2 (~$170/month) for 99.95% SLA
- **If users tolerate 5-15s first-request latency:** stay on Consumption and save $165/month
- **Alternative:** Skip APIM entirely. Implement rate limiting with ASP.NET Core middleware, caching with ResponseCaching, and API key auth in your own code. Re-add APIM when you need full gateway features.

---

## Phase 3: Growth (100-1,000 Users)

**Target: ~$280-340/month** — B1 CPU consistently above 70%, or you need auto-scaling.

| Resource | Change | New Cost | Priority |
|---|---|---|---|
| PostgreSQL | Upgrade to B2ms (2 vCores, 4 GB) | ~$25 | Medium |
| App Service | Upgrade to P0v3 (Premium, auto-scale) | ~$74 | High |
| APIM | Basic v2 | ~$170 | High |
| Storage | Standard_GRS | ~$3-5 | Low |
| App Insights | Budget ~$10/month | ~$10 | Low |

### Why P0v3 over S1

Premium v3 gives you critical scaling features at a similar price to Standard S1 (~$74 vs ~$73):

- **Auto-scale** (1-10 instances) — the critical feature for handling traffic spikes
- **Deployment slots** (blue-green deployments) for zero-downtime releases
- Better performance per dollar with newer hardware

### PostgreSQL B2ms

If queries start slowing under load, the jump to 2 vCores doubles throughput for only ~$13 more/month. The burstable tier is still appropriate at this scale.

---

## Phase 4: Scale (1,000+ Users)

**Target: ~$500-700/month** — Auto-scale regularly hitting 3+ instances, PostgreSQL CPU above 80%.

| Resource | Change | New Cost | Priority |
|---|---|---|---|
| PostgreSQL | GP_Standard_D2ds_v4 (General Purpose) | ~$125 | High |
| App Service | P1v3 (2 cores, 8 GB, auto-scale to 30) | ~$148 | High |
| APIM | Standard v2 or keep Basic v2 | ~$170-340 | Medium |
| Storage | Standard_RAGRS (restore geo-redundancy) | ~$5-10 | Medium |
| App Insights | Budget ~$30-50/month | ~$30-50 | Low |
| Redis Cache (new) | Basic C0 for response caching | ~$16 | Medium |

### Key Changes at Scale

- **General Purpose PostgreSQL:** Burstable tiers lose credits under sustained load. GP tier gives consistent performance with no credit depletion.
- **Redis Cache:** Move APIM/response caching to Azure Cache for Redis. Reduces DB load and gives consistent sub-millisecond reads for weather data.
- **Read Replicas:** If read-heavy patterns dominate (weather lookups, airport data), add a PostgreSQL read replica (~$125/month) and route read-only queries to it.

---

## Phase 5: High Scale (10,000+ Users)

**Target: ~$1,000-2,000+/month**

| Resource | Recommendation | Estimated Cost |
|---|---|---|
| PostgreSQL | GP_Standard_D4ds_v4 (4 vCores) + read replica | ~$250-500 |
| App Service | P2v3 or P3v3 with aggressive auto-scale | ~$300-600 |
| APIM | Standard v2 (multiple units) | ~$340+ |
| Redis | Standard C1+ | ~$80+ |
| CDN | Azure Front Door for static data | ~$35+ |

---

## Scaling Decision Matrix

Use these signals to determine when to upgrade each resource tier.

| Signal | Action |
|---|---|
| API response time > 500ms consistently | Scale up App Service tier |
| App Service CPU > 70% sustained | Enable auto-scale or scale up |
| PostgreSQL CPU > 80% sustained | Move from Burstable to General Purpose |
| PostgreSQL connections > 80% of max | Scale up PostgreSQL tier |
| Same data queried repeatedly | Add Redis cache layer |
| Read:Write ratio > 10:1 at scale | Add PostgreSQL read replica |
| APIM cold starts annoying users | Move from Consumption to Basic v2 |
| Deployment downtime unacceptable | Use P0v3+ for staging slots |

---

## Cost Trajectory Overview

Summary of estimated monthly costs across all scaling phases.

| Resource | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Phase 5 |
|---|---|---|---|---|---|
| PostgreSQL | $12 | $12 | $25 | $125 | $250+ |
| App Service | $13 | $13 | $74 | $148 | $300+ |
| Functions | $1-3 | $1-3 | $1-3 | $1-3 | $1-3 |
| APIM | $0-5 | $5-170 | $170 | $170-340 | $340+ |
| Storage | $1-2 | $2-3 | $3-5 | $5-10 | $10+ |
| Monitoring | $0 | $0-5 | $10 | $30-50 | $50+ |
| Redis | — | — | — | $16 | $80+ |
| **Total** | **~$30** | **~$80-150** | **~$280-340** | **~$500-700** | **~$1-2K+** |

---

## Immediate Action Items

To reduce from ~$130/month to ~$30/month, make these four Bicep changes:

### 1. Downgrade App Service Plan

In `infra/modules/app-service.bicep`, change the SKU from B2 to B1:

```bicep
sku: { name: 'B1', tier: 'Basic', size: 'B1', family: 'B', capacity: 1 }
```

### 2. Switch APIM to Consumption Tier

In `infra/modules/apim.bicep`, change the SKU:

```bicep
sku: { name: 'Consumption', capacity: 0 }
```

> **Note:** Test all APIM policies after migration. Named values and some policy features behave differently on Consumption tier.

### 3. Switch Primary Storage to LRS

In `infra/modules/storage.bicep`, change the SKU:

```bicep
sku: { name: 'Standard_LRS' }
```

### 4. Set Application Insights Daily Cap

In the Azure Portal or via Bicep, set the daily volume cap on Application Insights to 0.1 GB as a safety net against unexpected log volume.

---

> **Disclaimer:** All costs are estimates based on Azure pricing as of February 2026 for the East US region. Actual costs may vary based on usage patterns, data transfer, and Azure pricing changes. Monitor Azure Cost Management for real-time spending and set budget alerts.
