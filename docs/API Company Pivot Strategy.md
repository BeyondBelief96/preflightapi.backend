# PreflightApi API Company Pivot Strategy

## Overview

This document outlines the strategy for pivoting PreflightApi from a frontend-serving API into a standalone aviation data API product. The core value proposition: aviation data is notoriously fragmented (FAA fixed-width text files, NOAA inconsistent APIs, ArcGIS pagination) and developers building aviation apps would pay to not deal with this themselves.

**Target customers:** EFB developers, drone operators, flight school software, FBO management platforms, aviation startups.

**Comparable companies:** AeroAPI (FlightAware), AviationStack, CheckWX. PreflightApi would carve a niche in VFR/GA-focused data which is underserved.

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

## What Goes Into an API Company

### 1. API Key Management & Tiered Access (via APIM)

Azure API Management handles API key authentication out of the box:

```
Architecture:  Client -> APIM (subscription key validation) -> Your API
```

**APIM provides:**
- Subscription keys (primary + secondary per subscription)
- Products (group APIs into tiers)
- Rate limits and quotas per product/subscription
- Key regeneration and revocation
- No custom auth middleware needed in API code

**Tiered plans (APIM Products):**

| Product | Rate Limit | APIs Included | Price |
|---------|-----------|---------------|-------|
| Free | 100 req/day | Weather, Airports only | $0 |
| Developer | 10,000 req/day | All data endpoints | $49/mo |
| Professional | 100,000 req/day | All endpoints + Navlog | $199/mo |
| Enterprise | Custom | All + SLA + support | Custom |

### 2. Rate Limiting & Throttling (via APIM)

APIM policies handle all rate limiting — no custom middleware needed:

```xml
<rate-limit-by-key calls="100" renewal-period="86400"
                   counter-key="@(context.Subscription.Id)" />
```

**APIM rate limiting dimensions:**
- Per subscription (overall daily/monthly quota)
- Per product (different limits for different tiers)
- Per operation (expensive endpoints like `/navlog/calculate` cost more)
- Burst protection via `rate-limit` and `quota` policies

### 3. Documentation

- **OpenAPI/Swagger spec** generated from controllers (Swashbuckle)
- **Interactive API docs** (Swagger UI, Redoc, or custom portal)
- **Code examples** in Python, JavaScript, C#, Go, cURL
- **Guides** for common use cases (get weather for a flight route, find nearby airports, etc.)
- **Changelog** for API versions

### 4. Billing

Reactivate existing Stripe integration with:
- Usage-based billing or fixed tier subscriptions
- Overage charges or hard cutoffs at quota
- Invoice generation
- Webhook handlers for subscription lifecycle events

### 5. Legal & Compliance

- **Terms of Service** for API usage
- **Data attribution:** FAA data is public domain; NOAA has attribution requirements
- **SLA commitments** per tier (99.9% uptime for Enterprise, best-effort for Free)
- **Privacy policy** for customer data
- **Acceptable use policy** (prevent abuse, reselling restrictions)

---

## Required Code Changes

### What APIM Handles (No Code Needed)

With APIM as the gateway, these concerns are handled via APIM policies:

- **API key authentication** — APIM subscription key validation
- **Rate limiting** — `rate-limit-by-key` and `quota-by-key` policies
- **Usage tracking** — APIM built-in analytics and reporting
- **CORS** — APIM CORS policies
- **Response caching** — APIM cache policies for static data

### What Still Needs Code Changes

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

---

## Scaling Strategy

### Current Architecture Bottlenecks

| Bottleneck | Current State | Solution |
|---|---|---|
| Database | Single PostgreSQL instance | Read replicas, connection pooling (PgBouncer), caching layer |
| No caching | Every request hits DB | Redis/Azure Cache for hot data |
| No rate limiting | Unlimited requests | Middleware + Redis-backed sliding window |
| Single API instance | One App Service | Azure App Service scale-out or AKS |
| Cron jobs share DB | Same DB, no isolation | Separate read replicas; cron writes to primary, API reads from replica |
| No pagination | Full result sets returned | Cursor-based pagination on all list endpoints |

### Phase 1 — Hundreds of Customers (~$100-200/mo additional)

**Azure App Service + Redis**

- Add Azure Cache for Redis (C0 Basic: ~$17/mo or C1 Standard: ~$85/mo)
- Cache weather data, airport data, airspace data with appropriate TTLs
- Add API key auth + rate limiting via middleware
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

**Containers + API Management**

- Azure Container Apps or AKS for fine-grained scaling per endpoint
- Database sharding or read-replica routing per region
- Azure API Management (APIM) as full gateway
- Multi-region deployment for latency-sensitive customers
- Dedicated database instances for enterprise customers

### The Azure API Management Shortcut

Azure APIM handles many API-company concerns out of the box and can be placed in front of the existing API with minimal code changes:

| Feature | APIM Handles It |
|---------|----------------|
| API key management and validation | Yes |
| Rate limiting and quotas per subscription | Yes |
| Built-in developer portal with interactive docs | Yes |
| Usage analytics and reporting | Yes |
| Request/response transformation | Yes |
| Response caching | Yes |
| API versioning | Yes |
| OAuth2 / API key auth | Yes |

**Pricing:**
- Developer tier: ~$50/mo (non-production, no SLA)
- Basic tier: ~$150/mo
- Standard tier: ~$700/mo (includes developer portal)

This could be a quick-win approach: put APIM in front of your existing API and get 80% of API company infrastructure without rewriting auth, rate limiting, or building a developer portal.

---

## Custom Website + APIM Architecture

### Why Not Use APIM's Built-in Developer Portal?

APIM includes a developer portal for self-service signup, but it has limitations:
- Generic look and feel (hard to customize branding)
- Separate domain from your marketing site
- Limited control over user experience
- Doesn't integrate with your marketing/landing page

**Our approach:** Build a custom website for marketing, signup, and key management, using the APIM Management REST API behind the scenes.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Custom Website                            │
│  (Marketing, Pricing, Docs, Signup, Dashboard, Key Management)  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Website Backend                             │
│    (Handles user auth, calls APIM Management API, Stripe)       │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
        ┌──────────┐    ┌──────────┐    ┌──────────┐
        │  APIM    │    │  Stripe  │    │ Database │
        │  Mgmt    │    │  (billing)│    │ (users)  │
        │  API     │    │          │    │          │
        └──────────┘    └──────────┘    └──────────┘
```

### Customer Signup Flow

1. Customer visits your marketing website
2. Customer signs up (creates account in your database)
3. Customer selects a pricing tier
4. Website backend calls Stripe to set up subscription
5. Website backend calls APIM Management API to create:
   - APIM User (linked to your customer)
   - APIM Subscription to the appropriate Product
6. Customer receives their API keys (primary + secondary)
7. Customer uses keys to call your API through APIM

### APIM Management REST API

The APIM Management API lets you programmatically manage everything:

```
Base URL: https://management.azure.com/subscriptions/{subscriptionId}/
          resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/
          service/{serviceName}/...

Key operations:
- POST /users                    → Create APIM user
- POST /subscriptions            → Create subscription (generates keys)
- GET  /subscriptions/{id}/keys  → Retrieve subscription keys
- POST /subscriptions/{id}/regenerateKey → Rotate keys
- DELETE /subscriptions/{id}     → Revoke access
```

Authentication: Azure AD with appropriate RBAC roles (API Management Service Contributor).

### What the Website Builds

**Website includes:**
- Landing page and marketing content
- Pricing page with tier comparison
- User signup/login (your own auth, not APIM)
- Dashboard showing usage (pulls from APIM analytics API)
- API key management (view, regenerate, revoke)
- Stripe integration for billing
- API documentation (can import from APIM OpenAPI spec)

**Website does NOT build:**
- Custom auth middleware (APIM handles key validation)
- Rate limiting middleware (APIM policies)
- Usage tracking (APIM analytics)
- API gateway functionality (APIM)

### API Simplification

With APIM handling gateway concerns, the API codebase becomes simpler:

**Remove from API:**
- Auth0 JWT authentication (APIM validates keys)
- CORS configuration (APIM CORS policies)
- Rate limiting middleware (APIM policies)
- Usage tracking middleware (APIM analytics)

**Keep in API:**
- Core business logic
- Data access layer
- External service integrations (NOAA, FAA, etc.)
- Caching (Redis) for hot data

---

## Implementation Roadmap

### Milestone 1: API Cleanup & APIM Setup (Weeks 1-2)

- [x] Remove user-specific features (Aircraft, Flight, WeightBalance, Stripe)
- [x] Refactor NavlogService to accept inline performance data
- [x] Remove Auth0 JWT authentication from API
- [ ] Remove CORS configuration from API (APIM will handle)
- [ ] API versioning — prefix all data routes with `/v1/`
- [ ] Add cursor-based pagination to all list endpoints
- [ ] Deploy APIM instance (Developer tier for testing)
- [ ] Import API into APIM from OpenAPI spec
- [ ] Configure APIM Products (Free, Developer, Professional, Enterprise)
- [ ] Set up APIM rate limit and quota policies per product

### Milestone 2: Infrastructure (Weeks 3-4)

- [ ] Add Redis caching layer with appropriate TTLs
- [ ] Standardize response envelope format
- [ ] Add proper error response format with error codes
- [ ] Configure APIM caching policies for static/semi-static data
- [ ] Configure APIM CORS policies
- [ ] Set up APIM to call backend API
- [ ] Test end-to-end: Client → APIM → API → Database

### Milestone 3: Custom Website (Weeks 5-7)

- [ ] Set up website project (Next.js or similar)
- [ ] Build landing page and marketing content
- [ ] Build pricing page with tier comparison
- [ ] Implement user signup/login (own auth system)
- [ ] Integrate Stripe for billing
- [ ] Integrate APIM Management API for subscription creation
- [ ] Build dashboard showing API keys and usage
- [ ] Build API key management (view, regenerate, revoke)
- [ ] Import and display API documentation

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
