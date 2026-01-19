---
sidebar_position: 2
---

# REST API Reference

HTTP endpoints for user management, authentication, and data queries.

## Authentication

### Register User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "player1",
  "email": "player1@example.com",
  "password": "securePassword123",
  "displayName": "Player One"
}
```

Response:
```json
{
  "success": true,
  "token": "eyJhbG...",
  "user": {
    "userId": "uuid",
    "username": "player1",
    "displayName": "Player One"
  }
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "player1",
  "password": "securePassword123"
}
```

### Register Anonymous User

```http
POST /api/auth/register-anonymous
Content-Type: application/json

{
  "playerId": "uuid",
  "displayName": "Guest Player"
}
```

### Get Current User

```http
GET /api/auth/me
Authorization: Bearer <token>
```

## Users

### Get User by ID

```http
GET /api/users/{userId}
```

### Update Profile

```http
PUT /api/users/profile
Authorization: Bearer <token>
Content-Type: application/json

{
  "displayName": "New Display Name",
  "email": "newemail@example.com",
  "profilePrivacy": "Public"
}
```

### Search Users

```http
GET /api/users/search?q=player
Authorization: Bearer <token>
```

## Friends

### Get Friends List

```http
GET /api/friends
Authorization: Bearer <token>
```

### Get Friend Requests

```http
GET /api/friends/requests
Authorization: Bearer <token>
```

### Send Friend Request

```http
POST /api/friends/request/{toUserId}
Authorization: Bearer <token>
```

### Accept Friend Request

```http
POST /api/friends/accept/{friendUserId}
Authorization: Bearer <token>
```

### Decline Friend Request

```http
POST /api/friends/decline/{friendUserId}
Authorization: Bearer <token>
```

### Remove Friend

```http
DELETE /api/friends/{friendUserId}
Authorization: Bearer <token>
```

## Games

### List Games

```http
GET /api/games
```

Response:
```json
{
  "activeGames": [...],
  "waitingGames": [...]
}
```

### Get Game by ID

```http
GET /api/game/{gameId}
```

### Get Player's Active Games

```http
GET /api/player/{playerId}/active-games
```

### Get Player's Game History

```http
GET /api/player/{playerId}/games?limit=20&skip=0
```

### Get Player Statistics

```http
GET /api/player/{playerId}/stats
```

## Themes

### List Public Themes

```http
GET /api/themes?limit=50&cursor=xyz
```

### Get Default Themes

```http
GET /api/themes/defaults
```

### Get Theme by ID

```http
GET /api/themes/{themeId}
```

### Create Theme

```http
POST /api/themes
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "My Theme",
  "description": "A custom board theme",
  "visibility": "Public",
  "colors": {
    "boardBackground": "#8B4513",
    "lightPoint": "#DEB887",
    "darkPoint": "#654321",
    "whiteChecker": "#FFFFFF",
    "redChecker": "#8B0000"
  }
}
```

### Update Theme

```http
PUT /api/themes/{themeId}
Authorization: Bearer <token>
```

### Delete Theme

```http
DELETE /api/themes/{themeId}
Authorization: Bearer <token>
```

### Like/Unlike Theme

```http
POST /api/themes/{themeId}/like
DELETE /api/themes/{themeId}/like
Authorization: Bearer <token>
```

## Bots and Evaluators

### List Available Bots

```http
GET /api/bots
```

### List Available Evaluators

```http
GET /api/evaluators
```

## Statistics

### Server Statistics

```http
GET /stats
```

### Database Statistics

```http
GET /api/stats/db
```

## Health

### Health Check

```http
GET /health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-18T12:00:00Z"
}
```
