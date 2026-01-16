#!/bin/bash
# Script to clear all data from DynamoDB EXCEPT user profiles and themes
# Usage:
#   Local: ./clear-database.sh local
#   Dev: ./clear-database.sh dev
#   Production: ./clear-database.sh prod

set -e

ENVIRONMENT=${1:-local}

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Database Cleanup Script${NC}"
echo "Environment: $ENVIRONMENT"
echo ""

if [ "$ENVIRONMENT" == "prod" ]; then
    echo -e "${RED}WARNING: You are about to clear the PRODUCTION database!${NC}"
    echo -e "${RED}This will delete ALL data except user profiles and themes.${NC}"
    read -p "Type 'DELETE PRODUCTION' to confirm: " confirmation
    if [ "$confirmation" != "DELETE PRODUCTION" ]; then
        echo "Cancelled."
        exit 1
    fi

    TABLE_NAME="backgammon-prod"
    ENDPOINT_ARGS=""
    REGION="us-east-1"
elif [ "$ENVIRONMENT" == "dev" ]; then
    echo "Clearing dev database..."
    TABLE_NAME="backgammon-dev"
    ENDPOINT_ARGS=""
    REGION="us-east-1"
else
    echo "Clearing local database..."
    TABLE_NAME="backgammon-local"
    ENDPOINT_ARGS="--endpoint-url http://localhost:8000"
    REGION="us-east-1"
fi

echo ""
echo -e "${YELLOW}Scanning for data to delete...${NC}"

# Function to delete items by PK prefix
delete_by_pk_prefix() {
    local pk_pattern=$1
    local description=$2

    echo -e "${YELLOW}Deleting $description...${NC}"

    # Scan for items matching the pattern
    items=$(aws dynamodb scan \
        --table-name "$TABLE_NAME" \
        --filter-expression "begins_with(PK, :pk_prefix)" \
        --expression-attribute-values "{\":pk_prefix\":{\"S\":\"$pk_pattern\"}}" \
        --region "$REGION" \
        $ENDPOINT_ARGS \
        --output json 2>/dev/null || echo '{"Items":[]}')

    count=$(echo "$items" | jq -r '.Items | length')

    if [ "$count" -eq 0 ]; then
        echo "  No items found."
        return
    fi

    echo "  Found $count items to delete..."

    # Delete in batches of 25 (DynamoDB batch limit)
    deleted=0
    batch_size=25
    keys=$(echo "$items" | jq -c '[.Items[] | {PK: .PK, SK: .SK}]')
    total_batches=$(( (count + batch_size - 1) / batch_size ))

    for ((i=0; i<count; i+=batch_size)); do
        # Extract batch of keys
        batch=$(echo "$keys" | jq -c ".[${i}:${i}+${batch_size}]")

        # Build batch delete request
        request=$(echo "$batch" | jq -c "{\"$TABLE_NAME\": [.[] | {DeleteRequest: {Key: .}}]}")

        # Execute batch delete
        aws dynamodb batch-write-item \
            --request-items "$request" \
            --region "$REGION" \
            $ENDPOINT_ARGS \
            > /dev/null 2>&1

        deleted=$((deleted + $(echo "$batch" | jq -r 'length')))
        batch_num=$(( (i / batch_size) + 1 ))
        echo "    Batch $batch_num/$total_batches: Deleted $deleted/$count items..."
    done

    echo -e "  ${GREEN}Deleted $deleted items${NC}"
}

# Function to delete USER# items with specific SK prefix (preserving PROFILE)
delete_user_items_by_sk() {
    local sk_prefix=$1
    local description=$2

    echo -e "${YELLOW}Deleting $description...${NC}"

    # Scan for USER# items with specific SK prefix
    items=$(aws dynamodb scan \
        --table-name "$TABLE_NAME" \
        --filter-expression "begins_with(PK, :pk_prefix) AND begins_with(SK, :sk_prefix)" \
        --expression-attribute-values "{\":pk_prefix\":{\"S\":\"USER#\"},\":sk_prefix\":{\"S\":\"$sk_prefix\"}}" \
        --region "$REGION" \
        $ENDPOINT_ARGS \
        --output json 2>/dev/null || echo '{"Items":[]}')

    count=$(echo "$items" | jq -r '.Items | length')

    if [ "$count" -eq 0 ]; then
        echo "  No items found."
        return
    fi

    echo "  Found $count items to delete..."

    # Delete in batches of 25
    deleted=0
    batch_size=25
    keys=$(echo "$items" | jq -c '[.Items[] | {PK: .PK, SK: .SK}]')
    total_batches=$(( (count + batch_size - 1) / batch_size ))

    for ((i=0; i<count; i+=batch_size)); do
        # Extract batch of keys
        batch=$(echo "$keys" | jq -c ".[${i}:${i}+${batch_size}]")

        # Build batch delete request
        request=$(echo "$batch" | jq -c "{\"$TABLE_NAME\": [.[] | {DeleteRequest: {Key: .}}]}")

        # Execute batch delete
        aws dynamodb batch-write-item \
            --request-items "$request" \
            --region "$REGION" \
            $ENDPOINT_ARGS \
            > /dev/null 2>&1

        deleted=$((deleted + $(echo "$batch" | jq -r 'length')))
        batch_num=$(( (i / batch_size) + 1 ))
        echo "    Batch $batch_num/$total_batches: Deleted $deleted/$count items..."
    done

    echo -e "  ${GREEN}Deleted $deleted items${NC}"
}

# Delete all games (GAME#* primary keys)
delete_by_pk_prefix "GAME#" "all games"

# Delete all matches (MATCH#* primary keys)
delete_by_pk_prefix "MATCH#" "all matches"

# Delete player-game index entries (USER#* with SK starting with GAME#)
delete_user_items_by_sk "GAME#" "player-game index entries"

# Delete player-match index entries (USER#* with SK starting with MATCH#)
delete_user_items_by_sk "MATCH#" "player-match index entries"

# Delete friendships (USER#* with SK starting with FRIEND#)
delete_user_items_by_sk "FRIEND#" "friendships"

# Delete rating history (USER#* with SK starting with RATING#)
delete_user_items_by_sk "RATING#" "rating history entries"

# Delete puzzle completions (USER#* with SK starting with PUZZLE#)
delete_user_items_by_sk "PUZZLE#" "puzzle completion records"

# Delete daily puzzles (PUZZLE#* primary keys)
delete_by_pk_prefix "PUZZLE#" "daily puzzles"

echo ""
echo -e "${GREEN}Database cleanup complete!${NC}"
echo ""
echo "What was cleared:"
echo "  - All games (completed, active, abandoned)"
echo "  - All matches (including lobbies)"
echo "  - All player-game index entries"
echo "  - All player-match index entries"
echo "  - All friendships"
echo "  - All rating history"
echo "  - All puzzle data"
echo ""
echo "What was preserved:"
echo "  - User accounts (profiles)"
echo "  - Board themes"
echo ""
echo -e "${YELLOW}Note: User stats (wins/losses/rating) are stored in the profile and preserved.${NC}"
echo -e "${YELLOW}You may want to reset stats manually if needed.${NC}"
echo ""
echo -e "${YELLOW}Tip: If you have stale WebSocket connections, restart the server:${NC}"
if [ "$ENVIRONMENT" == "local" ]; then
    echo "   cd Backgammon.AppHost && dotnet run"
elif [ "$ENVIRONMENT" == "dev" ]; then
    echo "   docker compose -f docker-compose.dev.yml restart server"
else
    echo "   docker compose -f docker-compose.prod.yml restart server"
fi
