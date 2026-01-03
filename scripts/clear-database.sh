#!/bin/bash
# Script to clear all games and game-related data from DynamoDB
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

echo -e "${YELLOW}🧹 Database Cleanup Script${NC}"
echo "Environment: $ENVIRONMENT"
echo ""

if [ "$ENVIRONMENT" == "prod" ]; then
    echo -e "${RED}⚠️  WARNING: You are about to clear the PRODUCTION database!${NC}"
    echo -e "${RED}This will delete all games, but keep users and friendships.${NC}"
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
echo -e "${YELLOW}Scanning for game data...${NC}"

# Function to delete items in batches
delete_items() {
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

    echo -e "  ${GREEN}✓ Deleted $deleted items${NC}"
}

# Delete games (GAME#* primary keys)
delete_items "GAME#" "all games"

# Delete matches (MATCH#* primary keys)
delete_items "MATCH#" "all matches"

# Delete player-game index entries (USER#* with SK starting with GAME#)
echo -e "${YELLOW}Deleting player-game index entries...${NC}"
player_game_items=$(aws dynamodb scan \
    --table-name "$TABLE_NAME" \
    --filter-expression "begins_with(PK, :pk_prefix) AND begins_with(SK, :sk_prefix)" \
    --expression-attribute-values '{":pk_prefix":{"S":"USER#"},":sk_prefix":{"S":"GAME#"}}' \
    --region "$REGION" \
    $ENDPOINT_ARGS \
    --output json 2>/dev/null || echo '{"Items":[]}')

pg_count=$(echo "$player_game_items" | jq -r '.Items | length')
if [ "$pg_count" -eq 0 ]; then
    echo "  No player-game entries found."
else
    echo "  Found $pg_count player-game entries to delete..."

    # Delete in batches of 25
    pg_deleted=0
    batch_size=25
    pg_keys=$(echo "$player_game_items" | jq -c '[.Items[] | {PK: .PK, SK: .SK}]')
    total_batches=$(( (pg_count + batch_size - 1) / batch_size ))

    for ((i=0; i<pg_count; i+=batch_size)); do
        # Extract batch of keys
        batch=$(echo "$pg_keys" | jq -c ".[${i}:${i}+${batch_size}]")

        # Build batch delete request
        request=$(echo "$batch" | jq -c "{\"$TABLE_NAME\": [.[] | {DeleteRequest: {Key: .}}]}")

        # Execute batch delete
        aws dynamodb batch-write-item \
            --request-items "$request" \
            --region "$REGION" \
            $ENDPOINT_ARGS \
            > /dev/null 2>&1

        pg_deleted=$((pg_deleted + $(echo "$batch" | jq -r 'length')))
        batch_num=$(( (i / batch_size) + 1 ))
        echo "    Batch $batch_num/$total_batches: Deleted $pg_deleted/$pg_count items..."
    done

    echo -e "  ${GREEN}✓ Deleted $pg_deleted player-game entries${NC}"
fi

echo ""
echo -e "${GREEN}✅ Database cleanup complete!${NC}"
echo ""
echo "What was cleared:"
echo "  • All games (completed, active, abandoned)"
echo "  • All player-game index entries"
echo ""
echo "What was preserved:"
echo "  • User accounts"
echo "  • Friendships"
echo "  • User profiles and settings"
echo ""
echo -e "${YELLOW}💡 Tip: If you have stale WebSocket connections, restart the server:${NC}"
if [ "$ENVIRONMENT" == "local" ]; then
    echo "   cd Backgammon.AppHost && dotnet run"
elif [ "$ENVIRONMENT" == "dev" ]; then
    echo "   docker compose -f docker-compose.dev.yml restart server"
else
    echo "   docker compose -f docker-compose.prod.yml restart server"
fi
