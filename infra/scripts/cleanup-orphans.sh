#!/usr/bin/env bash
set -euo pipefail

# ─── TST Orphan Cleanup Script ──────────────────────────────────────────────
# Lists and deletes orphaned resources in the TST environment with confirmation.
# These resources are not attached to any active workload.
#
# Usage: ./cleanup-orphans.sh

RESOURCE_GROUP="rg-preflightapi-eastus-test"

echo "=== TST Orphan Cleanup ==="
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# ─── Orphaned App Service Plans (no sites attached) ─────────────────────────

ORPHAN_ASPS=(
  "ASP-rgpreflightapieastustest-ad15"
  "ASP-rgpreflightapieastustest-9665"
)

echo "--- Orphaned App Service Plans ---"
for asp in "${ORPHAN_ASPS[@]}"; do
  echo "  $asp"
done
echo ""

# ─── Orphaned User-Assigned Managed Identities ──────────────────────────────
# From earlier OIDC experiments (before switching to App Registrations)
# and previous app deployments. None are referenced by any active resource.

ORPHAN_MIS=(
  "oidc-msi-890a"
  "oidc-msi-a339"
  "oidc-msi-93df"
  "oidc-msi-baf9"
  "preflightapi-eas-id-bb3b"
  "preflightapi-eas-id-b84e"
  "az-func-prefligh-id-ab89"
)

echo "--- Orphaned User-Assigned Managed Identities ---"
for mi in "${ORPHAN_MIS[@]}"; do
  echo "  $mi"
done
echo ""

# ─── Confirm ─────────────────────────────────────────────────────────────────

read -rp "Delete all orphaned resources listed above? (y/N) " confirm
if [[ "$confirm" != "y" && "$confirm" != "Y" ]]; then
  echo "Aborted."
  exit 0
fi

echo ""
echo "Deleting orphaned App Service Plans..."
for asp in "${ORPHAN_ASPS[@]}"; do
  echo "  Deleting $asp..."
  az appservice plan delete \
    --name "$asp" \
    --resource-group "$RESOURCE_GROUP" \
    --yes \
    2>/dev/null && echo "    Done." || echo "    Not found or already deleted."
done

echo ""
echo "Deleting orphaned Managed Identities..."
for mi in "${ORPHAN_MIS[@]}"; do
  echo "  Deleting $mi..."
  az identity delete \
    --name "$mi" \
    --resource-group "$RESOURCE_GROUP" \
    2>/dev/null && echo "    Done." || echo "    Not found or already deleted."
done

echo ""
echo "Cleanup complete."
