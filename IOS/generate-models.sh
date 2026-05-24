#!/bin/bash
# Generates Swift Codable models from the Tapper-generated TypeScript types.
# Run this whenever the server DTOs change:
#   1. cd Backgammon.WebClient && pnpm generate:signalr  (regenerate TypeScript)
#   2. cd IOS && ./generate-models.sh                    (regenerate Swift)

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TS_DIR="$SCRIPT_DIR/../Backgammon.WebClient/src/types/generated"
OUT="$SCRIPT_DIR/BackgammonMobile/BackgammonMobile/Models/GeneratedModels.swift"

echo "Combining TypeScript sources..."
cat \
  "$TS_DIR/Backgammon.Core.ts" \
  "$TS_DIR/Backgammon.Server.Models.ts" \
  "$TS_DIR/Backgammon.Server.Models.SignalR.ts" \
  "$TS_DIR/Backgammon.Server.Services.ts" \
  | grep -v "^import" \
  > /tmp/backgammon_combined.ts

echo "Generating Swift models..."
npx quicktype \
  --src-lang typescript \
  --lang swift \
  --access-level public \
  --swift-5-support \
  /tmp/backgammon_combined.ts \
  -o "$OUT"

echo "Done → $OUT"
