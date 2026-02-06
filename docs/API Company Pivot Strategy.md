# PreflightApi API Company Pivot Strategy

## Overview

This document outlines the strategy for pivoting PreflightApi from a frontend-serving API into a standalone aviation data API product. The core value proposition: aviation data is notoriously fragmented (FAA fixed-width text files, NOAA inconsistent APIs, ArcGIS pagination) and developers building aviation apps would pay to not deal with this themselves.

**Target customers:** EFB developers, drone operators, flight school software, FBO management platforms, aviation startups.

**Comparable companies:** AeroAPI (FlightAware), AviationStack, CheckWX. PreflightApi would carve a niche in VFR/GA-focused data which is underserved.

---

## Business Model

### How It Works

PreflightApi is a **developer API platform**. We sell access to normalized aviation data via API keys. Our customers are developers building their own aviation applications — they never interact with our backend directly through a browser. Instead, they:

1. Sign up on our management portal (the frontend)
2. Choose a subscription tier and manage billing
3. Get API keys from their dashboard
4. Use those API keys in their own applications to call our API

This is the same model as OpenWeatherMap, Google Maps API, Stripe, or Twilio. No OAuth flows, no bearer tokens, no user sessions — just an API key in a request header.

### Two Separate Systems

| System | Purpose | Tech Stack | Audience |
|--------|---------|------------|----------|
| **Management Portal** (Frontend) | Marketing, docs, signup, billing, API key management | TanStack Start (full-stack React) + Clerk auth | Our customers (developers) |
| **Data API** (Backend) | Serve aviation data | .NET 8 + PostgreSQL + PostGIS, behind Azure APIM | Our customers' applications |

These systems are **completely decoupled**. The frontend never calls the data API. The data API has no concept of users, sessions, or authentication — APIM handles all of that before requests reach it.

### The Request Lifecycle

```
Developer's Application (their code, not ours)
        │
        │  API Key in header: Ocp-Apim-Subscription-Key: abc123
        │  (No bearer tokens, no OAuth, no sessions)
        │
        ▼
┌─────────────────────────────────┐
│        Azure APIM Gateway       │
│                                 │
│  1. Validate API key            │
│  2. Identify product/tier       │
│  3. Enforce rate limits/quotas  │
│  4. Cache response if possible  │
│  5. Forward to backend          │
└───────────────┬─────────────────┘
                │
                ▼
┌─────────────────────────────────┐
│       PreflightApi.API          │
│      (No auth needed here)      │
│                                 │
│  Process request, query DB,     │
│  return aviation data           │
└─────────────────────────────────┘
```

### Example API Call (What Our Customers Do)

```bash
# Get METAR for JFK — that's it, just an API key
curl -H "Ocp-Apim-Subscription-Key: their-api-key" \
     https://api.preflightapi.com/v1/metar/KJFK
```

No login, no token exchange, no OAuth dance. Their app sends an API key, gets data back.

---

## System Architecture

### Management Portal (Frontend — `preflightapi.frontend`)

**Framework:** TanStack Start (full-stack React with server functions)
**Authentication:** Clerk (for portal login only — developers managing their account)
**Hosting:** Deployed independently from the data API

The frontend is the **management portal and marketing site**. It never calls the data API directly. It serves three purposes:

1. **Marketing:** Landing page, pricing, documentation
2. **Account Management:** Signup/login via Clerk, billing via Stripe, API key management
3. **APIM Provisioning:** Server functions call the APIM Management REST API to create/manage subscriptions

#### Frontend Responsibilities

| Responsibility | How | Details |
|---------------|-----|---------|
| User signup/login | Clerk | Developers sign in to manage their account |
| Billing | Stripe | Subscription management, invoices, tier upgrades |
| API key provisioning | APIM Management REST API | Called via TanStack Start server functions |
| API key display/rotation | APIM Management REST API | Dashboard shows keys, allows regeneration |
| Usage dashboard | APIM Analytics API | Show request counts, quota usage |
| Documentation | Static/MDX content | API reference, guides, code examples |

#### Provisioning Flow (Server Functions)

When a developer signs up and selects a tier, the frontend's server functions handle the provisioning:

```
Developer signs up via Clerk
        │
        ▼
TanStack Start Server Function
        │
        ├──► Stripe: Create subscription for selected tier
        │
        ├──► APIM Management API: Create APIM user
        │
        ├──► APIM Management API: Create subscription under correct Product
        │         (generates primary + secondary API keys)
        │
        └──► Return API keys to developer's dashboard
```

#### Tier Change / Upgrade Flow

```
Developer clicks "Upgrade to Pro" on dashboard
        │
        ▼
TanStack Start Server Function
        │
        ├──► Stripe: Update subscription (prorate billing)
        │
        ├──► APIM Management API: Move subscription to new Product
        │         (API keys stay the same, rate limits change)
        │
        └──► Dashboard reflects new tier and limits
```

#### Webhook Handlers (Server Functions)

| Webhook Source | Event | Action |
|---------------|-------|--------|
| Clerk | `user.created` | Create internal user record |
| Clerk | `user.deleted` | Deactivate APIM subscription, cancel Stripe |
| Stripe | `invoice.payment_failed` | Downgrade to Free tier in APIM, notify user |
| Stripe | `customer.subscription.deleted` | Revoke APIM subscription |
| Stripe | `customer.subscription.updated` | Update APIM Product assignment |

### Data API (Backend — `preflight.api`)

**Framework:** .NET 8 ASP.NET Core
**Database:** PostgreSQL + PostGIS
**Hosting:** Azure App Service (behind APIM)

The data API is a **pure data service**. It has no concept of users, authentication, billing, or API keys. By the time a request reaches it, APIM has already validated the API key, checked rate limits, and approved the request.

#### What the API Does

- Serves normalized aviation data (weather, airports, airspace, obstacles, NOTAMs, charts)
- Runs scheduled data sync jobs (Azure Functions cron)
- Queries PostgreSQL/PostGIS for spatial data
- Integrates with external sources (NOAA, FAA, ArcGIS, Azure Blob Storage)

#### What the API Does NOT Do

- No authentication or authorization (APIM handles this)
- No rate limiting (APIM handles this)
- No user management (frontend handles this)
- No billing (frontend + Stripe handles this)
- No CORS (APIM handles this)

### Azure API Management (APIM)

APIM sits between developers' applications and the data API. It is the **single entry point** for all API traffic.

#### APIM Responsibilities

| Responsibility | APIM Feature | Details |
|---------------|-------------|---------|
| API key validation | Subscription keys | Primary + secondary keys per subscription |
| Tier enforcement | Products | Group APIs into Free/Dev/Pro/Enterprise |
| Rate limiting | Policies | `rate-limit-by-key`, `quota-by-key` |
| Response caching | Cache policies | Cache static data (airports, airspace) |
| Usage analytics | Built-in analytics | Request counts, latency, errors per subscription |
| CORS | CORS policies | Handle cross-origin requests |
| API versioning | Version sets | Route `/v1/` and future `/v2/` |

#### APIM Products (Tiers)

| Product | Rate Limit | Quota | APIs Included | Price |
|---------|-----------|-------|---------------|-------|
| Free | 10 req/min | 100 req/day | Weather, Airports only | $0 |
| Developer | 60 req/min | 10,000 req/day | All data endpoints | $49/mo |
| Professional | 300 req/min | 100,000 req/day | All endpoints + Navlog | $199/mo |
| Enterprise | Custom | Custom | All + SLA + support | Custom |

#### APIM Rate Limiting Policy Example

```xml
<policies>
  <inbound>
    <!-- Per-subscription rate limit -->
    <rate-limit-by-key calls="10" renewal-period="60"
                       counter-key="@(context.Subscription.Id)" />
    <!-- Per-subscription daily quota -->
    <quota-by-key calls="100" renewal-period="86400"
                  counter-key="@(context.Subscription.Id)" />
  </inbound>
</policies>
```

### APIM Management REST API

The frontend's server functions use this API to provision and manage subscriptions programmatically. Developers never interact with APIM directly.

```
Base URL: https://management.azure.com/subscriptions/{azureSubscriptionId}/
          resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/
          service/{serviceName}/...

Key operations:
- PUT  /users/{userId}                         → Create APIM user
- PUT  /subscriptions/{subscriptionId}         → Create subscription (generates keys)
- POST /subscriptions/{subscriptionId}/listSecrets → Retrieve subscription keys
- POST /subscriptions/{subscriptionId}/regeneratePrimaryKey → Rotate primary key
- POST /subscriptions/{subscriptionId}/regenerateSecondaryKey → Rotate secondary key
- PATCH /subscriptions/{subscriptionId}        → Update subscription (change Product)
- DELETE /subscriptions/{subscriptionId}       → Revoke access
```

Authentication: Azure AD service principal with `API Management Service Contributor` RBAC role.

---

## Full System Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Management Portal (Frontend)                       │
│                    preflightapi.frontend                              │
│                    TanStack Start + Clerk + Stripe                    │
│                                                                      │
│  ┌─────────┐  ┌──────────┐  ┌───────────┐  ┌──────────────────────┐ │
│  │Marketing│  │  Docs    │  │ Dashboard │  │   API Key Mgmt      │ │
│  │ Pages   │  │ (guides, │  │ (usage,   │  │ (view, rotate,      │ │
│  │         │  │  ref)    │  │  billing) │  │  revoke keys)       │ │
│  └─────────┘  └──────────┘  └───────────┘  └──────────────────────┘ │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │              TanStack Start Server Functions                     │ │
│  │  - Clerk webhooks (user lifecycle)                              │ │
│  │  - Stripe webhooks (billing lifecycle)                          │ │
│  │  - APIM Management API calls (provision/manage subscriptions)   │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└───────────┬──────────────────────┬──────────────────────┬────────────┘
            │                      │                      │
            ▼                      ▼                      ▼
     ┌──────────┐           ┌──────────┐           ┌──────────┐
     │  Clerk   │           │  Stripe  │           │  APIM    │
     │  (auth)  │           │ (billing)│           │  Mgmt    │
     │          │           │          │           │  API     │
     └──────────┘           └──────────┘           └────┬─────┘
                                                        │ configures
                                                        ▼
┌──────────────────────────────────────────────────────────────────────┐
│              Azure API Management (APIM Gateway)                     │
│                                                                      │
│  Validates API keys │ Enforces rate limits │ Caches responses        │
│  Tracks usage       │ Handles CORS         │ Routes versions         │
└───────────────────────────────────┬──────────────────────────────────┘
                                    │ forwards validated requests
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     PreflightApi.API (.NET 8)                        │
│                     preflight.api                                    │
│                                                                      │
│  Pure data service — no auth, no users, no billing                   │
│                                                                      │
│  Weather │ Airports │ Airspace │ Obstacles │ NOTAMs │ Charts │ Navlog│
└───────────────────────────────────┬──────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     PostgreSQL + PostGIS                              │
│                     Azure Blob Storage (charts, diagrams)            │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Current State Assessment

### What We Have (Core Asset)

The data ingestion pipeline and normalized data layer is the most valuable and hardest-to-replicate component. After removing user-specific features, this is now a lean, focused data API.

| Category | Count | Details |
|----------|-------|---------|
| Controllers | 14 | Airport, Metar, Taf, Pirep, Airspace, Obstacle, Notam, Navlog, Performance, etc. |
| Domain Entities | 16 | Airport, Metar, Taf, Airspace, Obstacle, etc. |
| External Data Sources | 5 | NOAA, FAA NMS, FAA NASR, ArcGIS, Azure Blob |
| API Endpoints | ~50+ | Data-focused GET endpoints |
| Database | PostgreSQL + PostGIS | Full spatial data support |
| Tests | 143 | All passing |

### Data Endpoints (Revenue-Generating)

These are the core API product — normalized aviation data:

- **Weather:** METAR, TAF, PIREP, AIRMET/SIGMET, G-AIRMET
- **Airports:** FAA NASR airport data, runways, runway ends, communication frequencies
- **Airspace:** Controlled airspace boundaries (PostGIS polygons), special use airspace
- **Obstacles:** Searchable by radius, bounding box, state, OAS number
- **NOTAMs:** By airport, radius, and flight route corridor
- **Navigation:** Navlog calculation (accepts inline performance data), magnetic variation, winds aloft
- **Performance:** Crosswind component, density altitude calculations
- **Charts:** Airport diagrams, chart supplements (PDF via Azure Blob SAS URLs)

### User-Scoped Endpoints (REMOVED)

As of 2026-02-05, all user-specific features have been removed to focus on a pure data API:

- ~~Aircraft management~~ — Removed
- ~~Aircraft documents~~ — Removed
- ~~Performance profiles~~ — Removed (NavlogService now accepts inline performance data)
- ~~Flights~~ — Removed
- ~~Weight & balance~~ — Removed
- ~~Stripe billing~~ — Removed (will use APIM subscriptions instead)
- ~~Auth0 JWT auth~~ — Removed from API (APIM handles authentication)

### Current Gaps (Addressed by APIM)

| Gap | Current State | Solution |
|-----|--------------|----------|
| No API key auth | No auth on data endpoints | **APIM subscription keys** |
| No rate limiting | Zero throttling | **APIM rate limit policies** |
| No caching layer | Every request hits PostgreSQL | **APIM response caching** + Redis for hot data |
| No pagination | Full result sets returned | Code change needed (not APIM) |
| No usage tracking | No request logging per customer | **APIM built-in analytics** |
| No API versioning | Unversioned routes | **APIM versioning** |
| No developer portal | N/A | **Custom website** (not APIM developer portal) |
| CORS configuration | Handled in API code | **APIM CORS policies** |

---

## Required Code Changes

### What APIM Handles (No Code Needed)

With APIM as the gateway, these concerns are handled via APIM policies:

- **API key authentication** — APIM subscription key validation
- **Rate limiting** — `rate-limit-by-key` and `quota-by-key` policies
- **Usage tracking** — APIM built-in analytics and reporting
- **CORS** — APIM CORS policies
- **Response caching** — APIM cache policies for static data

### What Still Needs Code Changes in the Data API

#### 1. API Versioning

Prefix all data routes with `/v1/`:

```
/api/metar/{icao}         -> /api/v1/metar/{icao}
/api/airport/{icao}       -> /api/v1/airport/{icao}
/api/airspace/by-classes  -> /api/v1/airspace/by-classes
```

Use `Asp.Versioning.Mvc` NuGet package for proper API versioning support.

#### 2. Response Standardization

Wrap all responses in a consistent envelope:

```json
{
  "data": { ... },
  "meta": {
    "requestId": "uuid",
    "timestamp": "ISO 8601",
    "version": "v1",
    "pagination": {
      "cursor": "abc123",
      "hasMore": true,
      "limit": 50
    }
  }
}
```

Note: Usage info (`requestsRemaining`, `resetAt`) can be added by APIM as response headers.

#### 3. Pagination

Add cursor-based pagination to all list endpoints. Priority endpoints:

1. `GET /api/v1/airport` (thousands of airports)
2. `GET /api/v1/obstacle/search` (currently hard-capped at 500/5000)
3. `GET /api/v1/airspace/by-classes` (large geometry payloads)
4. `GET /api/v1/pirep` (returns all PIREPs)
5. `GET /api/v1/metar/state/{stateCode}` (hundreds per state)

#### 4. Caching Layer

Add Redis caching for hot data (in addition to APIM response caching):

| Data Type | Cache TTL | Rationale |
|-----------|----------|-----------|
| METARs | 10 minutes | Matches cron refresh cycle |
| TAFs | 30 minutes | Updates less frequently |
| Airports | 24 hours | NASR data changes on 56-day cycle |
| Airspace | 24 hours | Rarely changes |
| Obstacles | 24 hours | Rarely changes |
| Comm Frequencies | 24 hours | Rarely changes |
| NOTAMs | 5 minutes | Time-sensitive |
| PIREPs | 5 minutes | Time-sensitive |

#### 5. Remove Remaining Auth Code

With APIM handling all access control, the data API should be stripped of:

- Clerk JWT authentication handlers (in progress — switching from Auth0 to Clerk currently in working tree)
- ConditionalAuthHandler/ConditionalAuthAttribute
- Any CORS middleware (APIM handles this)
- Any auth-related NuGet packages

The API will be accessible only through APIM (network-level restriction via Azure VNET or IP whitelisting).

### What Needs to Be Built in the Frontend

#### 1. APIM Integration Service

Server functions that wrap the APIM Management REST API:

- `createApimUser(clerkUserId, email)` — Create APIM user linked to Clerk user
- `createSubscription(apimUserId, productId)` — Create subscription, return API keys
- `getSubscriptionKeys(subscriptionId)` — Retrieve current API keys
- `regenerateKey(subscriptionId, keyType)` — Rotate primary or secondary key
- `updateSubscription(subscriptionId, newProductId)` — Change tier
- `deleteSubscription(subscriptionId)` — Revoke access

#### 2. Stripe Integration

- Checkout session creation for tier selection
- Webhook handlers for payment lifecycle
- Customer portal for self-service billing management
- Sync tier changes to APIM (Stripe webhook → update APIM Product)

#### 3. Dashboard Pages

- **API Keys page:** View primary/secondary keys, copy to clipboard, regenerate
- **Usage page:** Request counts, quota remaining, usage over time (from APIM analytics)
- **Billing page:** Current plan, upgrade/downgrade, invoices (Stripe Customer Portal)
- **Getting Started page:** Quick-start guide with user's actual API key pre-filled in examples

---

## Scaling Strategy

### Current Architecture Bottlenecks

| Bottleneck | Current State | Solution |
|---|---|---|
| Database | Single PostgreSQL instance | Read replicas, connection pooling (PgBouncer), caching layer |
| No caching | Every request hits DB | Redis/Azure Cache for hot data |
| No rate limiting | Unlimited requests | APIM rate limit policies |
| Single API instance | One App Service | Azure App Service scale-out or AKS |
| Cron jobs share DB | Same DB, no isolation | Separate read replicas; cron writes to primary, API reads from replica |
| No pagination | Full result sets returned | Cursor-based pagination on all list endpoints |

### Phase 1 — Hundreds of Customers (~$100-200/mo additional)

**Azure App Service + Redis**

- Add Azure Cache for Redis (C0 Basic: ~$17/mo or C1 Standard: ~$85/mo)
- Cache weather data, airport data, airspace data with appropriate TTLs
- Add response caching headers (ETag, Cache-Control)
- Add pagination to all list endpoints
- Scale App Service to S2 or P1v3 if needed

### Phase 2 — Thousands of Customers (~$500-1,500/mo)

**Scale Out + Read Replicas**

- Azure App Service auto-scaling (2-4 instances behind load balancer)
- PostgreSQL read replicas (Azure Flexible Server supports this natively)
- PgBouncer for connection pooling (built into Azure Flexible Server)
- Move Azure Functions cron jobs to their own App Service Plan to isolate write load
- CDN for static/semi-static data (airport info, airspace boundaries)
- Upgrade Redis to Standard tier for replication

### Phase 3 — Enterprise Scale (~$2,000+/mo)

**Containers + Multi-Region**

- Azure Container Apps or AKS for fine-grained scaling per endpoint
- Database sharding or read-replica routing per region
- Multi-region deployment for latency-sensitive customers
- Dedicated database instances for enterprise customers

---

## Implementation Roadmap

### Milestone 1: API Cleanup & APIM Setup (Weeks 1-2)

- [x] Remove user-specific features (Aircraft, Flight, WeightBalance, Stripe)
- [x] Refactor NavlogService to accept inline performance data
- [x] Remove Auth0 JWT authentication from API
- [x] Remove remaining auth code from API (Clerk handlers, CORS middleware)
- [x] API versioning — prefix all data routes with `/v1/`
- [x] Add cursor-based pagination to all list endpoints
- [x] Deploy API to Azure App Service
- [x] Deploy APIM instance (Developer tier for testing)
- [ ] Import API into APIM from OpenAPI spec
- [ ] Configure APIM Products (Free, Developer, Professional, Enterprise)
- [ ] Set up APIM rate limit and quota policies per product
- [ ] Restrict API to only accept traffic from APIM (IP whitelist or VNET)

### Milestone 2: Infrastructure (Weeks 3-4)

- [ ] Add Redis caching layer with appropriate TTLs
- [ ] Standardize response envelope format
- [ ] Add proper error response format with error codes
- [ ] Configure APIM caching policies for static/semi-static data
- [ ] Configure APIM CORS policies
- [ ] Test end-to-end: Client → APIM → API → Database

### Milestone 3: Frontend — Management Portal (Weeks 5-7)

- [ ] Build landing page and marketing content
- [ ] Build pricing page with tier comparison
- [ ] Implement Clerk authentication (signup/login for portal)
- [ ] Build APIM integration service (server functions calling APIM Management API)
- [ ] Build Stripe integration (checkout, webhooks, customer portal)
- [ ] Build provisioning flow: Clerk signup → Stripe subscription → APIM subscription → API keys
- [ ] Build dashboard: API keys page (view, copy, regenerate)
- [ ] Build dashboard: Usage page (APIM analytics)
- [ ] Build dashboard: Billing page (Stripe Customer Portal embed)
- [ ] Build API documentation pages
- [ ] Build webhook handlers (Clerk user events, Stripe billing events)

### Milestone 4: Launch Prep (Weeks 8-9)

- [ ] Terms of Service and legal docs
- [ ] Set up status page (e.g., Atlassian Statuspage)
- [ ] Write code examples in Python, JavaScript, cURL
- [ ] Create getting-started guides for common use cases
- [ ] Upgrade APIM to Standard tier (production SLA)
- [ ] Configure Azure App Service auto-scaling
- [ ] Beta launch with select customers

### Milestone 5: Growth (Ongoing)

- [ ] Add new data sources (TFRs, runway closures, fuel prices, FBO info)
- [ ] WebSocket/SSE for real-time weather updates
- [ ] Batch/bulk endpoints for high-volume customers
- [ ] SDKs in popular languages (Python, JavaScript, Go)
- [ ] Geographic expansion (international aviation data)
- [ ] Set up PostgreSQL read replica (when scale demands)

---

## Revenue Projections (Rough Estimates)

| Customers | Avg Revenue/Customer | MRR | Annual |
|-----------|---------------------|-----|--------|
| 10 (beta) | $100 | $1,000 | $12,000 |
| 50 | $120 | $6,000 | $72,000 |
| 200 | $130 | $26,000 | $312,000 |
| 500 | $140 | $70,000 | $840,000 |

Infrastructure costs at 500 customers would be roughly $2,000-5,000/mo depending on usage patterns, giving healthy margins.

---

## Key Risks

| Risk | Mitigation |
|------|-----------|
| FAA/NOAA change data formats or APIs | Abstract data sources behind interfaces (already done); monitor for changes |
| Competitors with more data | Focus on VFR/GA niche; superior DX and data quality |
| High infrastructure costs at scale | Aggressive caching; usage-based pricing covers costs |
| Legal IP concerns from former employer | Consult attorney; document independent creation; consider clean-room rewrite of any contested components |
| Single cloud vendor lock-in | ICloudStorageService abstraction already exists; keep infrastructure portable |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-05 | Document pivot strategy | Evaluate feasibility of API company model |
| 2026-02-05 | Remove user-specific features | Focus on pure data API; removed Aircraft, Flight, WeightBalance, Stripe, Auth0 to simplify codebase (~90+ files deleted) |
| 2026-02-05 | Refactor NavlogService | Accept inline `NavlogPerformanceDataDto` instead of fetching from DB; no user profiles needed |
| 2026-02-05 | Use Azure API Management | APIM handles API keys, rate limiting, caching, CORS, analytics out of the box; avoids building custom middleware |
| 2026-02-05 | Custom website over APIM developer portal | APIM's built-in portal is generic; custom website allows full branding, marketing integration, and better UX |
| 2026-02-05 | Use APIM Management REST API | Programmatically create subscriptions and keys from custom website backend; customer never sees APIM directly |
| 2026-02-05 | TanStack Start for frontend | Full-stack React framework with server functions; handles Clerk auth, Stripe webhooks, and APIM Management API calls without needing a separate backend service |
| 2026-02-05 | API key-only access model | Developers use API keys only (no bearer tokens/OAuth) to call the data API; same model as OpenWeatherMap, Google Maps API; Clerk auth is only for the management portal |
| 2026-02-05 | Frontend handles all provisioning | APIM subscription creation, Stripe billing, and API key management all handled by TanStack Start server functions; data API has zero knowledge of users or billing |
