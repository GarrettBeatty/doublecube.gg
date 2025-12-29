#!/bin/bash
set -e

echo "Creating Cosmos DB database and containers..."

# Get the Cosmos DB account name from environment
COSMOS_ACCOUNT_NAME=$(az cosmosdb list --resource-group $AZURE_RESOURCE_GROUP --query "[0].name" -o tsv)

echo "Using Cosmos DB account: $COSMOS_ACCOUNT_NAME"

# Create database (serverless mode - no throughput parameter)
az cosmosdb sql database create \
  --account-name $COSMOS_ACCOUNT_NAME \
  --resource-group $AZURE_RESOURCE_GROUP \
  --name backgammon \
  --output none || echo "Database already exists"

echo "Database 'backgammon' ready"

# Create containers
echo "Creating containers..."

# Games container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT_NAME \
  --resource-group $AZURE_RESOURCE_GROUP \
  --database-name backgammon \
  --name games \
  --partition-key-path /gameId \
  --output none || echo "Container 'games' already exists"

# Users container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT_NAME \
  --resource-group $AZURE_RESOURCE_GROUP \
  --database-name backgammon \
  --name users \
  --partition-key-path /userId \
  --output none || echo "Container 'users' already exists"

# Friendships container
az cosmosdb sql container create \
  --account-name $COSMOS_ACCOUNT_NAME \
  --resource-group $AZURE_RESOURCE_GROUP \
  --database-name backgammon \
  --name friendships \
  --partition-key-path /userId \
  --output none || echo "Container 'friendships' already exists"

echo "Cosmos DB provisioning complete!"
