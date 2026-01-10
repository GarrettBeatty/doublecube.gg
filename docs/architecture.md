# Backgammon Architecture Deep Dive

## High-Level System Overview

```mermaid
flowchart TB
    subgraph Clients["Frontend Clients"]
        Browser1["Browser Tab 1"]
        Browser2["Browser Tab 2"]
        BrowserN["Browser Tab N"]
    end

    subgraph WebClient["Backgammon.WebClient (React + TypeScript)"]
        subgraph State["State Management"]
            Zustand["Zustand Store\n(gameStore)"]
            AuthCtx["AuthContext"]
            SignalRCtx["SignalRContext"]
            MatchCtx["MatchContext"]
        end

        subgraph UI["UI Layer"]
            Pages["Pages\n(Home, Game, Profile, Analysis)"]
            Components["Components\n(Board, Controls, Modals)"]
        end

        subgraph Services["Client Services"]
            SignalRSvc["signalr.service"]
            AuthSvc["auth.service"]
            ApiSvc["api.service"]
            AudioSvc["audio.service"]
        end
    end

    subgraph Server["Backgammon.Server (ASP.NET Core)"]
        subgraph API["REST API Layer"]
            AuthCtrl["AuthController\n/api/auth/*"]
            UserCtrl["UsersController\n/api/users/*"]
            FriendCtrl["FriendsController\n/api/friends/*"]
            GameCtrl["GameController\n/api/game/*"]
        end

        subgraph Hub["SignalR Hub"]
            GameHub["GameHub\n/gamehub"]
        end

        subgraph ServiceLayer["Service Layer"]
            GameSvc["GameService"]
            MatchSvc["MatchService"]
            AuthService["AuthService"]
            PlayerSvc["PlayerProfileService"]
            EloSvc["EloRatingService"]
            AiSvc["AiMoveService"]
            AnalysisSvc["AnalysisService"]
        end

        subgraph SessionMgmt["Session Management"]
            SessionMgr["GameSessionManager"]
            Sessions["GameSession[]"]
        end
    end

    subgraph Core["Backgammon.Core (Domain Logic)"]
        GameEngine["GameEngine"]
        Board["Board"]
        Match["Match"]
        DoublingCube["DoublingCube"]
    end

    subgraph Data["Data Layer"]
        subgraph DynamoDB["AWS DynamoDB"]
            Table["backgammon-local\n(Single Table Design)"]
        end

        subgraph Repos["Repositories"]
            UserRepo["DynamoDbUserRepository"]
            GameRepo["DynamoDbGameRepository"]
            MatchRepo["DynamoDbMatchRepository"]
            FriendRepo["DynamoDbFriendshipRepository"]
        end

        subgraph Cache["Caching"]
            HybridCache["HybridCache\n(Memory + Redis)"]
        end
    end

    subgraph Infrastructure["Infrastructure (Aspire)"]
        DynamoLocal["DynamoDB Local\n(Docker)"]
        Redis["Redis\n(SignalR Backplane)"]
    end

    Browser1 & Browser2 & BrowserN <--> |HTTPS/WSS| WebClient
    WebClient <--> |SignalR WebSocket| GameHub
    WebClient <--> |REST API| API

    GameHub --> ServiceLayer
    API --> ServiceLayer
    ServiceLayer --> SessionMgmt
    ServiceLayer --> Core
    ServiceLayer --> Repos

    Repos --> Table
    Repos --> HybridCache
    HybridCache --> Redis

    SessionMgr --> Sessions
    Sessions --> GameEngine
```

---

## Frontend Architecture

```mermaid
flowchart TB
    subgraph App["App.tsx (Root)"]
        AuthProvider["AuthProvider"]
        SignalRProvider["SignalRProvider"]
        MatchProvider["MatchProvider"]
        Router["React Router"]
    end

    subgraph Routes["Routes"]
        Home["/ → HomePage"]
        Game["/game/:gameId → GamePage"]
        Profile["/profile/:username → ProfilePage"]
        Analysis["/analysis/:sgf? → AnalysisPage"]
        MatchResults["/match-results/:matchId → MatchResultsPage"]
    end

    subgraph Stores["Zustand Stores"]
        GameStore["gameStore\n- currentGameState\n- myColor\n- doublingCube\n- chatMessages\n- validMoves\n- isAnalysisMode"]
        MatchStore["matchStore\n- currentMatchId"]
    end

    subgraph Contexts["React Contexts"]
        Auth["AuthContext\n- user\n- token\n- isAuthenticated\n- login()/logout()"]
        SignalR["SignalRContext\n- connection\n- isConnected\n- invoke()"]
        MatchCtxFE["MatchContext\n- currentMatch\n- currentMatchLobby"]
    end

    subgraph Services["Services Layer"]
        SignalRService["SignalRService\n- connect()\n- disconnect()\n- invoke()\n- on()"]
        AuthService2["AuthService\n- login()\n- register()\n- restoreSession()"]
        ApiService["ApiService\n- request()\n- getProfile()\n- getFriends()"]
    end

    subgraph Hooks["Custom Hooks"]
        UseSignalREvents["useSignalREvents\n- Registers all event handlers\n- Uses refs to avoid re-registration\n- Filters by gameId"]
    end

    subgraph Components["Key Components"]
        BoardSVG["BoardSVG\n(Interactive game board)"]
        GameControls["GameControls\n(Roll, End Turn, Undo)"]
        PlayerCard["PlayerCard\n(Name, Rating, Timer)"]
        DoublingCubeUI["DoublingCube\n(Visual cube)"]
        ChatPanel["ChatPanel\n(In-game chat)"]
        Modals["Modals\n(Login, Register, GameResult, Double)"]
    end

    App --> AuthProvider --> SignalRProvider --> MatchProvider --> Router
    Router --> Routes

    Routes --> Stores
    Routes --> Contexts
    Routes --> Components

    SignalR --> SignalRService
    Auth --> AuthService2
    SignalRProvider --> UseSignalREvents

    UseSignalREvents --> GameStore
    Components --> GameStore
```

---

## Backend Services Architecture

```mermaid
flowchart TB
    subgraph Hub["GameHub (SignalR)"]
        HubMethods["Hub Methods\n- JoinGame\n- RollDice\n- MakeMove\n- EndTurn\n- OfferDouble\n- CreateMatch\n- ..."]
    end

    subgraph Orchestration["Action Orchestration"]
        GameActionOrch["GameActionOrchestrator\n- RollDiceAsync()\n- MakeMoveAsync()\n- EndTurnAsync()\n- UndoLastMoveAsync()"]
    end

    subgraph GameServices["Game Services"]
        GameSvc2["GameService\n- JoinGameAsync()\n- BroadcastGameUpdateAsync()\n- BroadcastDoubleOfferAsync()"]
        MatchSvc2["MatchService\n- CreateMatchAsync()\n- CompleteGameAsync()\n- StartNextGameAsync()"]
        DoubleSvc["DoubleOfferService\n- OfferDoubleAsync()\n- AcceptDoubleAsync()\n- DeclineDoubleAsync()"]
        MoveQuerySvc["MoveQueryService\n- GetValidSourcesAsync()\n- GetValidDestinationsAsync()"]
    end

    subgraph PlayerServices["Player Services"]
        PlayerConnSvc["PlayerConnectionService\n- Track online players"]
        PlayerProfileSvc["PlayerProfileService\n- GetProfile()\n- UpdateProfile()"]
        PlayerStatsSvc["PlayerStatsService\n- UpdateStatsAfterGame()"]
        EloRatingSvc["EloRatingService\n- CalculateNewRatings()"]
    end

    subgraph AIServices["AI & Analysis"]
        AiMoveSvc["AiMoveService\n- GetBestMove()\n- ExecuteAiTurn()"]
        AnalysisSvc2["AnalysisService\n- AnalyzePosition()\n- FindBestMoves()"]
        Evaluators["Evaluators\n- HeuristicEvaluator\n- GnubgEvaluator"]
    end

    subgraph SocialServices["Social Services"]
        FriendSvc["FriendService\n- SendRequest()\n- AcceptRequest()"]
        ChatSvc["ChatService\n- SendMessage()"]
    end

    subgraph CorrespondenceServices["Correspondence"]
        CorrSvc["CorrespondenceGameService\n- CreateMatch()\n- NotifyTurn()"]
        TimeoutSvc["CorrespondenceTimeoutService\n(Background Service)"]
    end

    subgraph SessionLayer["Session Management"]
        SessionMgr2["GameSessionManager\n- CreateGame()\n- GetGameByPlayer()\n- AddPlayer()"]
        Sessions2["GameSession\n- Engine\n- WhiteConnections[]\n- RedConnections[]\n- GetState()"]
    end

    Hub --> GameActionOrch
    Hub --> GameServices
    Hub --> PlayerServices
    Hub --> SocialServices
    Hub --> AIServices
    Hub --> CorrespondenceServices

    GameActionOrch --> GameSvc2
    GameActionOrch --> SessionLayer
    GameServices --> SessionLayer

    GameSvc2 --> AiMoveSvc
    MatchSvc2 --> EloRatingSvc
```

---

## SignalR Communication Flow

```mermaid
sequenceDiagram
    participant B1 as Browser Tab 1
    participant B2 as Browser Tab 2
    participant SR as SignalR Service
    participant Hub as GameHub
    participant Svc as GameService
    participant Sess as GameSession
    participant Engine as GameEngine

    Note over B1,Engine: Connection & Authentication
    B1->>SR: connect(token)
    SR->>Hub: OnConnectedAsync()
    Hub->>Hub: Validate JWT
    Hub-->>B1: Connected

    Note over B1,Engine: Join Game (Multi-Tab)
    B1->>Hub: JoinGame(playerId, gameId)
    Hub->>Svc: JoinGameAsync()
    Svc->>Sess: AddPlayer(playerId, connId1)
    Sess-->>Svc: Player added to WhiteConnections[]

    B2->>Hub: JoinGame(playerId, gameId)
    Hub->>Svc: JoinGameAsync()
    Svc->>Sess: AddPlayer(playerId, connId2)
    Sess-->>Svc: connId2 added to same player's set

    Hub-->>B1: GameStart(state)
    Hub-->>B2: GameStart(state)

    Note over B1,Engine: Make Move
    B1->>Hub: MakeMove(from, to)
    Hub->>Engine: ExecuteMove(from, to)
    Engine-->>Hub: Move executed
    Hub->>Svc: BroadcastGameUpdateAsync()

    loop Each connection in WhiteConnections + RedConnections
        Svc-->>B1: GameUpdate(state)
        Svc-->>B2: GameUpdate(state)
    end

    Note over B1,Engine: Doubling Cube
    B1->>Hub: OfferDouble()
    Hub->>Svc: BroadcastDoubleOfferAsync()
    Svc-->>B2: DoubleOffered(newValue)

    B2->>Hub: AcceptDouble()
    Hub->>Engine: cube.Accept()
    Hub->>Svc: BroadcastGameUpdateAsync()
    Svc-->>B1: DoubleAccepted(state)
    Svc-->>B2: GameUpdate(state)
```

---

## DynamoDB Single-Table Design

```mermaid
erDiagram
    BACKGAMMON_TABLE {
        string PK "Partition Key"
        string SK "Sort Key"
        string GSI1PK "Username Index"
        string GSI1SK "Username Index SK"
        string GSI2PK "Email Index"
        string GSI2SK "Email Index SK"
        string GSI3PK "Status Index"
        string GSI3SK "Timestamp Sort"
        string GSI4PK "Correspondence Turn"
        string GSI4SK "Deadline Sort"
    }

    USER_PROFILE {
        string PK "USER#{userId}"
        string SK "PROFILE"
        string GSI1PK "USERNAME#{normalized}"
        string GSI2PK "EMAIL#{normalized}"
        string userId
        string username
        string displayName
        string email
        string passwordHash
        int rating
        int peakRating
        map stats "wins, losses, streaks"
        string profilePrivacy
    }

    GAME {
        string PK "GAME#{gameId}"
        string SK "METADATA"
        string GSI3PK "GAME_STATUS#{status}"
        string GSI3SK "timestamp_ticks"
        string gameId
        string status
        string whitePlayerId
        string redPlayerId
        list boardState
        int die1
        int die2
        string currentPlayer
        int doublingCubeValue
        string winner
        string matchId
    }

    PLAYER_GAME_INDEX {
        string PK "USER#{playerId}"
        string SK "GAME#{reversedTimestamp}#{gameId}"
        string gameId
    }

    MATCH {
        string PK "MATCH#{matchId}"
        string SK "METADATA"
        string GSI3PK "MATCH_STATUS#{status}"
        string GSI4PK "CORRESPONDENCE_TURN#{playerId}"
        string matchId
        string player1Id
        string player2Id
        int player1Score
        int player2Score
        int targetScore
        boolean isCrawfordGame
        string currentGameId
        list gameIds
        string opponentType
        boolean isCorrespondence
    }

    FRIENDSHIP {
        string PK "USER#{userId}"
        string SK "FRIEND#{status}#{friendUserId}"
        string friendUserId
        string friendUsername
        string status "Pending|Accepted|Blocked"
        string initiatedBy
    }

    BACKGAMMON_TABLE ||--o{ USER_PROFILE : "contains"
    BACKGAMMON_TABLE ||--o{ GAME : "contains"
    BACKGAMMON_TABLE ||--o{ PLAYER_GAME_INDEX : "contains"
    BACKGAMMON_TABLE ||--o{ MATCH : "contains"
    BACKGAMMON_TABLE ||--o{ FRIENDSHIP : "contains"
```

### Key Schema Patterns

| Entity | Partition Key (PK) | Sort Key (SK) | GSI Usage |
|--------|-------------------|---------------|-----------|
| User Profile | `USER#{userId}` | `PROFILE` | GSI1 (username), GSI2 (email) |
| Game | `GAME#{gameId}` | `METADATA` | GSI3 (status queries) |
| Player-Game Index | `USER#{playerId}` | `GAME#{reversedTS}#{gameId}` | None (per-player queries) |
| Match | `MATCH#{matchId}` | `METADATA` | GSI3 (status), GSI4 (turn) |
| Player-Match Index | `USER#{playerId}` | `MATCH#{reversedTS}#{matchId}` | None |
| Friendship | `USER#{userId}` | `FRIEND#{status}#{friendId}` | None |

---

## Authentication Flow

```mermaid
sequenceDiagram
    participant Browser
    participant AuthCtx as AuthContext
    participant AuthSvc as AuthService
    participant API as REST API
    participant AuthCtrl as AuthController
    participant DB as DynamoDB
    participant SignalR as SignalRProvider

    Note over Browser,SignalR: App Initialization
    Browser->>AuthCtx: initialize()
    AuthCtx->>AuthSvc: restoreSession()
    AuthSvc->>AuthSvc: Check localStorage for token

    alt Has Token
        AuthSvc->>API: GET /api/auth/me
        API->>AuthCtrl: Validate JWT
        AuthCtrl->>DB: GetUserById()
        DB-->>AuthCtrl: User
        AuthCtrl-->>API: User data
        API-->>AuthSvc: User
        AuthSvc-->>AuthCtx: User restored
    else No Token
        AuthSvc->>API: POST /api/auth/register-anonymous
        API->>AuthCtrl: RegisterAnonymous()
        AuthCtrl->>DB: CreateUser(isAnonymous=true)
        DB-->>AuthCtrl: User
        AuthCtrl->>AuthCtrl: Generate JWT
        AuthCtrl-->>API: {token, user}
        API-->>AuthSvc: {token, user}
        AuthSvc->>AuthSvc: Store in localStorage
        AuthSvc-->>AuthCtx: Anonymous user created
    end

    AuthCtx->>AuthCtx: Set isReady = true
    AuthCtx-->>SignalR: Auth ready, can connect

    Note over Browser,SignalR: SignalR Connection
    SignalR->>SignalR: Wait for isReady
    SignalR->>API: Connect to /gamehub?access_token={jwt}
    API->>API: Validate JWT in query string
    API-->>SignalR: Connection established

    Note over Browser,SignalR: Login (Registered User)
    Browser->>AuthCtx: login(username, password)
    AuthCtx->>AuthSvc: login()
    AuthSvc->>API: POST /api/auth/login
    API->>AuthCtrl: Login()
    AuthCtrl->>DB: GetByUsername()
    AuthCtrl->>AuthCtrl: Verify BCrypt password
    AuthCtrl->>AuthCtrl: Generate JWT with displayName claim
    AuthCtrl-->>Browser: {token, user}
    AuthSvc->>AuthSvc: Update localStorage
    AuthCtx->>SignalR: Reconnect with new token
```

---

## Game Session Lifecycle

```mermaid
stateDiagram-v2
    [*] --> WaitingForPlayer: CreateGame()

    WaitingForPlayer --> InProgress: 2nd player joins
    WaitingForPlayer --> InProgress: AI game starts

    state InProgress {
        [*] --> OpeningRoll
        OpeningRoll --> PlayerTurn: Higher roll wins

        state PlayerTurn {
            [*] --> WaitingForRoll
            WaitingForRoll --> HasDice: RollDice()
            HasDice --> Moving: GetValidMoves()
            Moving --> Moving: MakeMove()
            Moving --> WaitingForEndTurn: No moves left
            WaitingForEndTurn --> [*]: EndTurn()
        }

        PlayerTurn --> DoubleOffered: OfferDouble()
        DoubleOffered --> PlayerTurn: AcceptDouble()
        DoubleOffered --> Completed: DeclineDouble()

        PlayerTurn --> Completed: All checkers born off
    }

    InProgress --> Completed: Game over
    InProgress --> Abandoned: AbandonGame()
    InProgress --> Abandoned: Timeout (90 days)

    Completed --> [*]: Session cleanup (5 min)
    Abandoned --> [*]: Session cleanup

    note right of InProgress
        Multi-tab support:
        - HashSet of connections per player
        - All tabs receive broadcasts
        - Any tab can make moves
    end note
```

---

## Match Play Flow

```mermaid
flowchart TB
    subgraph Creation["Match Creation"]
        CreateMatch["CreateMatch(config)"]
        CreateMatch --> SaveMatch["Save to DynamoDB"]
        SaveMatch --> CreateFirstGame["Create First Game"]
        CreateFirstGame --> NotifyPlayers["Broadcast MatchCreated"]
    end

    subgraph GamePlay["Individual Games"]
        JoinGame["Players Join Game"]
        JoinGame --> PlayGame["Play Game"]
        PlayGame --> GameOver["Game Completes"]
    end

    subgraph Completion["Game Completion"]
        GameOver --> UpdateScore["Update Match Score"]
        UpdateScore --> CheckCrawford{"Crawford Rule?"}
        CheckCrawford -->|"Score = Target-1"| SetCrawford["Set IsCrawfordGame"]
        CheckCrawford -->|"Otherwise"| CheckWinner
        SetCrawford --> CheckWinner
        CheckWinner{"Match Won?"}
        CheckWinner -->|"Yes"| MatchComplete["Complete Match"]
        CheckWinner -->|"No"| NextGame["Start Next Game"]
    end

    subgraph NextGameFlow["Next Game"]
        NextGame --> CreateNextGame["Create New GameSession"]
        CreateNextGame --> BroadcastContinue["Broadcast MatchContinued"]
        BroadcastContinue --> JoinGame
    end

    subgraph MatchCompleteFlow["Match Complete"]
        MatchComplete --> UpdateElo["Update ELO Ratings"]
        UpdateElo --> UpdateStats["Update Player Stats"]
        UpdateStats --> SaveFinal["Save Final Match State"]
        SaveFinal --> BroadcastComplete["Broadcast MatchCompleted"]
    end

    NotifyPlayers --> JoinGame
```

---

## Infrastructure (Aspire Orchestration)

```mermaid
flowchart TB
    subgraph Aspire["Backgammon.AppHost"]
        Orchestrator["Aspire Orchestrator"]
    end

    subgraph Resources["Managed Resources"]
        DynamoDBLocal["DynamoDB Local\n(Docker Container)\nPort: 8000"]
        RedisCache["Redis\n(SignalR Backplane)\nPort: 6379"]
    end

    subgraph Services["Application Services"]
        Server["Backgammon.Server\n(ASP.NET Core)\nPort: 5000"]
        WebClient["Backgammon.WebClient\n(Vite Dev Server)\nPort: 3000"]
    end

    subgraph ServiceDiscovery["Service Discovery"]
        Config["/api/config endpoint\nReturns service URLs"]
    end

    Orchestrator --> |"1. Start"| DynamoDBLocal
    Orchestrator --> |"2. Start"| RedisCache
    Orchestrator --> |"3. Start (WaitFor DB+Redis)"| Server
    Orchestrator --> |"4. Start (WaitFor Server)"| WebClient

    Server --> |"Connection String"| DynamoDBLocal
    Server --> |"Connection String"| RedisCache
    WebClient --> |"Fetch"| Config
    Config --> |"Server URL"| WebClient

    Server --> |"HybridCache L2"| RedisCache
    Server --> |"SignalR Backplane"| RedisCache
```

---

## Key File Locations

### Backend (Backgammon.Server)
| File | Purpose |
|------|---------|
| `Hubs/GameHub.cs` | SignalR hub - all real-time methods |
| `Program.cs` | DI setup, middleware, routes |
| `Services/GameService.cs` | Game creation, joining, broadcasting |
| `Services/GameActionOrchestrator.cs` | Move execution pattern |
| `Services/GameSession.cs` | Session state + multi-tab support |
| `Services/MatchService.cs` | Match lifecycle management |
| `Services/AuthService.cs` | JWT + BCrypt authentication |
| `Services/DynamoDb/*.cs` | Repository implementations |
| `Controllers/*.cs` | REST API endpoints |

### Frontend (Backgammon.WebClient)
| File | Purpose |
|------|---------|
| `src/App.tsx` | Root component + providers |
| `src/contexts/SignalRContext.tsx` | SignalR connection management |
| `src/contexts/AuthContext.tsx` | Authentication state |
| `src/hooks/useSignalREvents.ts` | Event handler registration |
| `src/stores/gameStore.ts` | Zustand game state |
| `src/services/signalr.service.ts` | SignalR service layer |
| `src/services/auth.service.ts` | Auth API calls |
| `src/pages/*.tsx` | Route pages |
| `src/components/game/*.tsx` | Game UI components |

### Core (Backgammon.Core)
| File | Purpose |
|------|---------|
| `GameEngine.cs` | Game rules + logic |
| `Board.cs` | Board representation |
| `Match.cs` | Match scoring + Crawford |
| `DoublingCube.cs` | Cube logic |
| `Move.cs` | Move representation |

---

## Summary

This architecture implements a **real-time multiplayer backgammon** application with:

1. **Frontend**: React + TypeScript + Vite with Zustand for state management
2. **Real-time**: SignalR WebSocket with multi-tab support and automatic reconnection
3. **Backend**: ASP.NET Core with clean service layer separation
4. **Domain**: Pure game logic in Backgammon.Core (no dependencies)
5. **Persistence**: DynamoDB single-table design with 4 GSIs for efficient queries
6. **Auth**: JWT + BCrypt with anonymous user support
7. **Infrastructure**: .NET Aspire orchestration with DynamoDB Local and Redis
