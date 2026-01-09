# Filtering Analysis Report: Backend vs Frontend

This document analyzes where filtering happens in our lobby and correspondence game implementation, comparing backend database queries vs frontend JavaScript filtering.

## Executive Summary

Our current implementation uses a **mixed approach**:
- **Backend filtering** for data isolation (player-specific queries, status-based queries)
- **Frontend filtering** for UX features (search, rating range, match preferences) and owner exclusion

### Key Findings:
1. ✅ **Good**: Performance-critical filters (status, player ownership) are in the backend using DynamoDB indexes
2. ⚠️ **Mixed**: Some ownership filtering happens on frontend (correspondence lobbies exclude current user)
3. ⚠️ **Mixed**: Correspondence type filtering happens on frontend, not backend
4. ✅ **Good**: UX filters (search, rating range) correctly happen on frontend for instant feedback

---

## Detailed Analysis by Feature

### 1. Regular Match Lobbies (GameLobby Component)

**Backend Query** (`GetMatchLobbies` → `GetOpenLobbiesAsync`):
- **Location**: `DynamoDbMatchRepository.cs:409-431`
- **Filters Applied**:
  - ✅ `status = "WaitingForPlayers"` (via GSI3 index)
  - ✅ `isOpenLobby = true` (FilterExpression)
  - ✅ Limit to 50 results
  - ✅ Sort by most recent first
- **Returns**: ALL open lobbies (both regular and correspondence)

**Frontend Filtering** (`GameLobby.tsx:44-84`):
- **Initial Filter** (line 44-46):
  ```typescript
  .filter((lobby) => !lobby.isCorrespondence)
  ```
  - ⚠️ **Issue**: This filters out correspondence lobbies on the frontend
  - **Impact**: Backend sends unnecessary data (correspondence lobbies that get filtered out)

- **UX Filters** (line 49-84):
  - ✅ Search by username (client-side)
  - ✅ Rating range slider (client-side)
  - ✅ Match length filter (client-side)
  - ✅ Rated/casual filter (client-side)
  - ✅ Doubling cube filter (client-side)
  - **Justification**: These are UI preference filters that should be instant

**Recommendation**:
- 🔴 **OPTIMIZE**: Add `isCorrespondence = false` filter to backend query to avoid transferring unnecessary data
- ✅ **KEEP**: UX filters on frontend for instant response

---

### 2. Correspondence Lobbies (CorrespondenceLobbies Component)

**Backend Query** (`GetMatchLobbies` → `GetOpenLobbiesAsync`):
- **Location**: Same as above
- **Filters Applied**: Same as regular lobbies
- **Returns**: ALL open lobbies (both regular and correspondence)

**Frontend Filtering** (`CorrespondenceLobbies.tsx:44-45`):
```typescript
.filter((l) => l.isCorrespondence && l.creatorPlayerId !== currentPlayerId)
```

- ⚠️ **Issues**:
  1. `isCorrespondence = true` filter happens on frontend (should be backend)
  2. `creatorPlayerId !== currentPlayerId` filter happens on frontend (acceptable but could be backend)

**Recommendation**:
- 🔴 **OPTIMIZE**: Backend should support optional `isCorrespondence` filter parameter
- 🟡 **CONSIDER**: Move creator exclusion to backend (pass `currentPlayerId` param)

---

### 3. My Correspondence Games (CorrespondenceGames Component)

**Backend Query** (`GetAllCorrespondenceGamesAsync`):
- **Location**: `CorrespondenceGameService.cs:46-75`
- **Filters Applied**:

  **3a. Your Turn Games**:
  - ✅ Query GSI4: `CORRESPONDENCE_TURN#{playerId}`
  - ✅ Filter: `status = "InProgress"`
  - ✅ Sorted by deadline (earliest first)
  - **Backend Query Location**: `DynamoDbMatchRepository.cs:587-619`

  **3b. Waiting Games**:
  - ✅ Query player's matches with `status = "InProgress"`
  - ✅ Filter: `isCorrespondence = true AND currentTurnPlayerId != playerId`
  - **Backend Query Location**: `DynamoDbMatchRepository.cs:621-641`

  **3c. My Lobbies**:
  - ✅ Query player's matches with `status = "WaitingForPlayers"`
  - ⚠️ Frontend filter (line 58-60): `isCorrespondence = true AND player1Id == playerId`
  - **Backend Query Location**: Calls `GetPlayerMatchesAsync` (generic query)

**Frontend Filtering**: None (data comes pre-filtered)

**Recommendation**:
- 🟡 **CONSIDER**: The "My Lobbies" query could be more efficient with a backend filter for `isCorrespondence = true`
- ✅ **GOOD**: Most filtering happens in database using indexes

---

## Performance Analysis

### Database Queries (Backend)

| Query | Index Used | Filter Type | Efficiency |
|-------|-----------|-------------|------------|
| Open Lobbies | GSI3 (status-based) | Key condition + filter | ⚡ Fast |
| Your Turn Games | GSI4 (turn-based) | Key condition + filter | ⚡ Fast |
| Waiting Games | Player-based query | Client-side filter | ⚠️ Medium |
| My Lobbies | Player-based query | Client-side filter | ⚠️ Medium |

### Network Transfer

**Current State**:
- `GetMatchLobbies` returns ~50 items (regular + correspondence)
- Frontend filters out half the data (correspondence for GameLobby, regular for CorrespondenceLobbies)
- **Waste**: ~25 unnecessary items transferred per query

**If Optimized**:
- Backend returns only relevant type (regular OR correspondence)
- Frontend receives ~25 items per query
- **Savings**: 50% reduction in data transfer

---

## Recommendations Priority

### 🔴 High Priority (Performance Impact)

1. **Add `isCorrespondence` parameter to `GetOpenLobbiesAsync`**
   - **Location**: `DynamoDbMatchRepository.cs:409`
   - **Change**: Add optional parameter `bool? isCorrespondence = null`
   - **Filter**: Add to FilterExpression when provided
   - **Impact**: 50% reduction in network transfer for lobby queries

2. **Update GameHub to pass filter parameter**
   - **Location**: `GameHub.cs:1026`
   - **Change**: Support query parameter (e.g., `/lobbies?type=regular` or `/lobbies?type=correspondence`)
   - **Impact**: Clients get only relevant lobbies

### 🟡 Medium Priority (Code Clarity)

3. **Create separate backend methods**
   - Create `GetRegularLobbiesAsync()` and `GetCorrespondenceLobbiesAsync()`
   - More explicit API design
   - Easier to optimize independently

4. **Move My Lobbies filtering to backend**
   - **Location**: `CorrespondenceGameService.cs:58-60`
   - **Change**: Create dedicated repository method instead of filtering in-memory
   - **Impact**: Slight performance improvement, better separation of concerns

### ✅ Keep As-Is (Correct Pattern)

5. **Frontend UX filters (search, rating, preferences)**
   - These should stay on frontend for instant user feedback
   - No server round-trip needed for UI state changes

---

## Pattern Guidelines Going Forward

### ✅ Filter on Backend When:
- Data isolation (player-specific, private data)
- Performance-critical (reduces network transfer)
- Database can use indexes (fast query)
- Security requirement (user shouldn't see data)

### ✅ Filter on Frontend When:
- UX features (search, sliders, toggles)
- Instant feedback required
- Frequently changing user preferences
- Post-processing display logic

### ⚠️ Avoid:
- Filtering data on frontend that user shouldn't have access to (security issue)
- Transferring large datasets to filter on frontend (performance issue)
- Backend sorting/filtering for UX preferences (unnecessary server load)

---

## Code Locations Reference

### Backend
- `DynamoDbMatchRepository.cs:409-431` - GetOpenLobbiesAsync
- `DynamoDbMatchRepository.cs:587-619` - GetCorrespondenceMatchesForTurnAsync
- `DynamoDbMatchRepository.cs:621-641` - GetCorrespondenceMatchesWaitingAsync
- `CorrespondenceGameService.cs:46-75` - GetAllCorrespondenceGamesAsync
- `GameHub.cs:1022-1051` - GetMatchLobbies endpoint

### Frontend
- `GameLobby.tsx:44-84` - Regular lobby filtering
- `CorrespondenceLobbies.tsx:44-45` - Correspondence lobby filtering
- `CorrespondenceGames.tsx:87-97` - Uses pre-filtered backend data
- `useMatchLobbies.ts:12-33` - Lobby data fetching

---

## Implementation Plan (Optional)

If you want to optimize based on recommendations:

```typescript
// Backend: GameHub.cs
public async Task<List<object>> GetMatchLobbies(string? lobbyType = null)
{
    var lobbies = await _matchService.GetOpenLobbiesAsync(
        isCorrespondence: lobbyType == "correspondence" ? true :
                         lobbyType == "regular" ? false : null
    );
    // ... rest of mapping
}

// Frontend: CorrespondenceLobbies.tsx
const allLobbies = await invoke<CorrespondenceLobby[]>(
    HubMethods.GetMatchLobbies,
    "correspondence" // Pass filter parameter
);
// No need to filter for isCorrespondence anymore

// Frontend: GameLobby.tsx (via useMatchLobbies)
const matchLobbies = await invoke<MatchLobby[]>(
    'GetMatchLobbies',
    "regular" // Pass filter parameter
);
// No need to filter for !isCorrespondence anymore
```

This would eliminate all the frontend filtering for `isCorrespondence` and reduce data transfer by 50%.
