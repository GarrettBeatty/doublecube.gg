using System.Text.Json;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backgammon.Server.Data;

/// <summary>
/// EF Core database context for all relational data.
/// Replaces the DynamoDB single-table design with proper relational tables.
/// Complex nested objects are stored as JSONB for flexibility.
/// </summary>
public class BackgammonDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of <see cref="BackgammonDbContext"/>.
    /// </summary>
    public BackgammonDbContext(DbContextOptions<BackgammonDbContext> options)
        : base(options)
    {
    }

    /// <summary>Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Games table (both active and completed).</summary>
    public DbSet<Game> Games => Set<Game>();

    /// <summary>Matches table.</summary>
    public DbSet<Match> Matches => Set<Match>();

    /// <summary>Friendships table (stores both directions of each friendship).</summary>
    public DbSet<Friendship> Friendships => Set<Friendship>();

    /// <summary>Daily puzzles table.</summary>
    public DbSet<DailyPuzzle> DailyPuzzles => Set<DailyPuzzle>();

    /// <summary>User puzzle attempts table.</summary>
    public DbSet<PuzzleAttempt> PuzzleAttempts => Set<PuzzleAttempt>();

    /// <summary>User puzzle streaks table.</summary>
    public DbSet<PuzzleStreakInfo> PuzzleStreaks => Set<PuzzleStreakInfo>();

    /// <summary>Rating history entries table.</summary>
    public DbSet<RatingHistoryEntry> RatingHistory => Set<RatingHistoryEntry>();

    /// <summary>Board themes table.</summary>
    public DbSet<BoardTheme> BoardThemes => Set<BoardTheme>();

    /// <summary>Theme likes (user-theme join table).</summary>
    public DbSet<ThemeLike> ThemeLikes => Set<ThemeLike>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prevent EF from discovering Backgammon.Core domain types as entity tables.
        // These are stored as JSONB via value converters on the entity properties above.
        modelBuilder.Ignore<Core.Board>();
        modelBuilder.Ignore<Core.Dice>();
        modelBuilder.Ignore<Core.DoublingCube>();
        modelBuilder.Ignore<Core.Game>();
        modelBuilder.Ignore<Core.GameEngine>();
        modelBuilder.Ignore<Core.GameHistory>();
        modelBuilder.Ignore<Core.GameRecord>();
        modelBuilder.Ignore<Core.GameResult>();
        modelBuilder.Ignore<Core.Match>();
        modelBuilder.Ignore<Core.Move>();
        modelBuilder.Ignore<Core.Player>();
        modelBuilder.Ignore<Core.Point>();
        modelBuilder.Ignore<Core.TimeControlConfig>();
        modelBuilder.Ignore<Core.PlayerTimeState>();
        modelBuilder.Ignore<Core.TurnSnapshot>();

        ConfigureUsers(modelBuilder);
        ConfigureGames(modelBuilder);
        ConfigureMatches(modelBuilder);
        ConfigureFriendships(modelBuilder);
        ConfigureDailyPuzzles(modelBuilder);
        ConfigurePuzzleAttempts(modelBuilder);
        ConfigurePuzzleStreaks(modelBuilder);
        ConfigureRatingHistory(modelBuilder);
        ConfigureBoardThemes(modelBuilder);
    }

    private static ValueConverter<T, string> JsonConverter<T>() =>
        new(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<T>(v, JsonOptions)!);

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).HasColumnName("user_id");
            entity.Ignore(u => u.Id); // DynamoDB artifact; userId is the PK

            entity.Property(u => u.Username).HasColumnName("username").HasMaxLength(20).IsRequired();
            entity.Property(u => u.UsernameNormalized).HasColumnName("username_normalized").HasMaxLength(20).IsRequired();
            entity.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(u => u.EmailNormalized).HasColumnName("email_normalized").HasMaxLength(256);
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(u => u.LastSeenAt).HasColumnName("last_seen_at");
            entity.Property(u => u.IsAnonymous).HasColumnName("is_anonymous");
            entity.Property(u => u.IsActive).HasColumnName("is_active");
            entity.Property(u => u.IsBanned).HasColumnName("is_banned");
            entity.Property(u => u.BannedReason).HasColumnName("banned_reason");
            entity.Property(u => u.BannedUntil).HasColumnName("banned_until");
            entity.Property(u => u.Rating).HasColumnName("rating");
            entity.Property(u => u.PeakRating).HasColumnName("peak_rating");
            entity.Property(u => u.RatedGamesCount).HasColumnName("rated_games_count");
            entity.Property(u => u.RatingLastUpdatedAt).HasColumnName("rating_last_updated_at");
            entity.Property(u => u.ProfilePrivacy).HasColumnName("profile_privacy").HasConversion<string>();
            entity.Property(u => u.GameHistoryPrivacy).HasColumnName("game_history_privacy").HasConversion<string>();
            entity.Property(u => u.FriendsListPrivacy).HasColumnName("friends_list_privacy").HasConversion<string>();
            entity.Property(u => u.SelectedThemeId).HasColumnName("selected_theme_id");

            // JSONB columns for complex nested types (value converters prevent EF from treating them as navigations)
            entity.Property(u => u.Stats).HasColumnName("stats")
                .HasConversion(JsonConverter<UserStats>()).HasColumnType("jsonb");
            entity.Property(u => u.LinkedAnonymousIds).HasColumnName("linked_anonymous_ids")
                .HasConversion(JsonConverter<List<string>>()).HasColumnType("jsonb");

            // Unique indexes (replaces DynamoDB GSIs)
            entity.HasIndex(u => u.UsernameNormalized).IsUnique().HasDatabaseName("ix_users_username_normalized");
            entity.HasIndex(u => u.EmailNormalized).HasDatabaseName("ix_users_email_normalized");

            // Leaderboard index
            entity.HasIndex(u => u.Rating).HasDatabaseName("ix_users_rating");

            // Full-text search index on username (replaces OpenSearch need)
            entity.HasIndex(u => u.Username).HasDatabaseName("ix_users_username_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });
    }

    private static void ConfigureGames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");

            // GameId delegates to CoreGame, so we map it via property shadow
            entity.HasKey(g => g.GameId);
            entity.Property(g => g.GameId).HasColumnName("game_id");

            entity.Ignore(g => g.Id); // DynamoDB artifact
            entity.Ignore(g => g.WinType);
            entity.Ignore(g => g.Winner);
            entity.Ignore(g => g.Stakes);
            entity.Ignore(g => g.IsCrawfordGame);
            entity.Ignore(g => g.GameSgf);

            entity.Property(g => g.WhitePlayerId).HasColumnName("white_player_id");
            entity.Property(g => g.RedPlayerId).HasColumnName("red_player_id");
            entity.Property(g => g.WhiteUserId).HasColumnName("white_user_id");
            entity.Property(g => g.RedUserId).HasColumnName("red_user_id");
            entity.Property(g => g.WhitePlayerName).HasColumnName("white_player_name").HasMaxLength(100);
            entity.Property(g => g.RedPlayerName).HasColumnName("red_player_name").HasMaxLength(100);
            entity.Property(g => g.GameStarted).HasColumnName("game_started");
            entity.Property(g => g.WhiteCheckersOnBar).HasColumnName("white_checkers_on_bar");
            entity.Property(g => g.RedCheckersOnBar).HasColumnName("red_checkers_on_bar");
            entity.Property(g => g.WhiteBornOff).HasColumnName("white_born_off");
            entity.Property(g => g.RedBornOff).HasColumnName("red_born_off");
            entity.Property(g => g.CurrentPlayer).HasColumnName("current_player");
            entity.Property(g => g.Die1).HasColumnName("die1");
            entity.Property(g => g.Die2).HasColumnName("die2");
            entity.Property(g => g.RemainingMoves).HasColumnName("remaining_moves")
                .HasConversion(JsonConverter<List<int>>()).HasColumnType("jsonb");
            entity.Property(g => g.DoublingCubeValue).HasColumnName("doubling_cube_value");
            entity.Property(g => g.DoublingCubeOwner).HasColumnName("doubling_cube_owner");
            entity.Property(g => g.Moves).HasColumnName("moves")
                .HasConversion(JsonConverter<List<string>>()).HasColumnType("jsonb");
            entity.Property(g => g.MoveCount).HasColumnName("move_count");
            entity.Property(g => g.Turns).HasColumnName("turns")
                .HasConversion(JsonConverter<List<TurnSnapshotDto>>()).HasColumnType("jsonb");
            entity.Property(g => g.CreatedAt).HasColumnName("created_at");
            entity.Property(g => g.LastUpdatedAt).HasColumnName("last_updated_at");
            entity.Property(g => g.CompletedAt).HasColumnName("completed_at");
            entity.Property(g => g.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(g => g.IsRated).HasColumnName("is_rated");
            entity.Property(g => g.WhiteRatingBefore).HasColumnName("white_rating_before");
            entity.Property(g => g.RedRatingBefore).HasColumnName("red_rating_before");

            // Status and MatchId are computed from CoreGame
            entity.Property(g => g.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(g => g.MatchId).HasColumnName("match_id");

            // Board state stored as JSONB for reconstruction
            entity.Property(g => g.BoardState).HasColumnName("board_state")
                .HasConversion(JsonConverter<List<PointStateDto>>()).HasColumnType("jsonb");

            // Full CoreGame stored as JSONB for complete game reconstruction
            entity.Property(g => g.CoreGame).HasColumnName("core_game")
                .HasConversion(JsonConverter<Core.Game>()).HasColumnType("jsonb");

            // Indexes for common queries
            entity.HasIndex(g => g.MatchId).HasDatabaseName("ix_games_match_id");
            entity.HasIndex(g => g.WhiteUserId).HasDatabaseName("ix_games_white_user_id");
            entity.HasIndex(g => g.RedUserId).HasDatabaseName("ix_games_red_user_id");
            entity.HasIndex(g => g.Status).HasDatabaseName("ix_games_status");
            entity.HasIndex(g => g.CreatedAt).HasDatabaseName("ix_games_created_at");
        });
    }

    private static void ConfigureMatches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(m => m.MatchId);
            entity.Property(m => m.MatchId).HasColumnName("match_id");

            entity.Property(m => m.Player1Id).HasColumnName("player1_id");
            entity.Property(m => m.Player2Id).HasColumnName("player2_id");
            entity.Property(m => m.Player1Name).HasColumnName("player1_name").HasMaxLength(100);
            entity.Property(m => m.Player2Name).HasColumnName("player2_name").HasMaxLength(100);
            entity.Property(m => m.Player1DisplayName).HasColumnName("player1_display_name").HasMaxLength(100);
            entity.Property(m => m.Player2DisplayName).HasColumnName("player2_display_name").HasMaxLength(100);
            entity.Property(m => m.WinnerId).HasColumnName("winner_id");
            entity.Property(m => m.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(m => m.OpponentType).HasColumnName("opponent_type").HasMaxLength(20);
            entity.Property(m => m.LobbyStatus).HasColumnName("lobby_status").HasMaxLength(30);
            entity.Property(m => m.IsOpenLobby).HasColumnName("is_open_lobby");
            entity.Property(m => m.IsRated).HasColumnName("is_rated");
            entity.Property(m => m.IsCorrespondence).HasColumnName("is_correspondence");
            entity.Property(m => m.TimePerMoveDays).HasColumnName("time_per_move_days");
            entity.Property(m => m.TurnDeadline).HasColumnName("turn_deadline");
            entity.Property(m => m.CurrentTurnPlayerId).HasColumnName("current_turn_player_id");
            entity.Property(m => m.LastUpdatedAt).HasColumnName("last_updated_at");

            // Computed from CoreMatch
            entity.Property(m => m.CreatedAt).HasColumnName("created_at");
            entity.Property(m => m.TargetScore).HasColumnName("target_score");
            entity.Property(m => m.Player1Score).HasColumnName("player1_score");
            entity.Property(m => m.Player2Score).HasColumnName("player2_score");
            entity.Property(m => m.IsCrawfordGame).HasColumnName("is_crawford_game");
            entity.Property(m => m.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(m => m.CurrentGameId).HasColumnName("current_game_id");
            entity.Property(m => m.GameIds).HasColumnName("game_ids")
                .HasConversion(JsonConverter<List<string>>()).HasColumnType("jsonb");

            // JSONB for game summaries and full match state
            entity.Property(m => m.GamesSummary).HasColumnName("games_summary")
                .HasConversion(JsonConverter<List<MatchGameSummary>>()).HasColumnType("jsonb");
            entity.Property(m => m.CoreMatch).HasColumnName("core_match")
                .HasConversion(JsonConverter<Core.Match>()).HasColumnType("jsonb");

            entity.HasIndex(m => m.Player1Id).HasDatabaseName("ix_matches_player1_id");
            entity.HasIndex(m => m.Player2Id).HasDatabaseName("ix_matches_player2_id");
            entity.HasIndex(m => m.Status).HasDatabaseName("ix_matches_status");
            entity.HasIndex(m => m.LobbyStatus).HasDatabaseName("ix_matches_lobby_status");
            entity.HasIndex(m => m.IsOpenLobby).HasDatabaseName("ix_matches_is_open_lobby");
            entity.HasIndex(m => m.CurrentTurnPlayerId).HasDatabaseName("ix_matches_current_turn_player");
            entity.HasIndex(m => m.LastUpdatedAt).HasDatabaseName("ix_matches_last_updated_at");
        });
    }

    private static void ConfigureFriendships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");

            // Composite PK: each friendship creates one row per direction
            entity.HasKey(f => new { f.UserId, f.FriendUserId });
            entity.Property(f => f.UserId).HasColumnName("user_id");
            entity.Property(f => f.FriendUserId).HasColumnName("friend_user_id");

            entity.Ignore(f => f.Id); // DynamoDB artifact

            entity.Property(f => f.FriendUsername).HasColumnName("friend_username").HasMaxLength(20);
            entity.Property(f => f.FriendDisplayName).HasColumnName("friend_display_name").HasMaxLength(50);
            entity.Property(f => f.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(f => f.CreatedAt).HasColumnName("created_at");
            entity.Property(f => f.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(f => f.InitiatedBy).HasColumnName("initiated_by");

            entity.HasIndex(f => f.UserId).HasDatabaseName("ix_friendships_user_id");
            entity.HasIndex(f => new { f.UserId, f.Status }).HasDatabaseName("ix_friendships_user_status");
        });
    }

    private static void ConfigureDailyPuzzles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyPuzzle>(entity =>
        {
            entity.ToTable("daily_puzzles");
            entity.HasKey(p => p.PuzzleId);
            entity.Property(p => p.PuzzleId).HasColumnName("puzzle_id");

            entity.Property(p => p.PuzzleDate).HasColumnName("puzzle_date").HasMaxLength(10);
            entity.Property(p => p.PositionSgf).HasColumnName("position_sgf");
            entity.Property(p => p.CurrentPlayer).HasColumnName("current_player").HasMaxLength(10);
            entity.Property(p => p.Dice).HasColumnName("dice")
                .HasConversion(JsonConverter<int[]>()).HasColumnType("jsonb");
            entity.Property(p => p.BoardState).HasColumnName("board_state")
                .HasConversion(JsonConverter<List<PointStateDto>>()).HasColumnType("jsonb");
            entity.Property(p => p.WhiteCheckersOnBar).HasColumnName("white_checkers_on_bar");
            entity.Property(p => p.RedCheckersOnBar).HasColumnName("red_checkers_on_bar");
            entity.Property(p => p.WhiteBornOff).HasColumnName("white_born_off");
            entity.Property(p => p.RedBornOff).HasColumnName("red_born_off");
            entity.Property(p => p.BestMoves).HasColumnName("best_moves")
                .HasConversion(JsonConverter<List<MoveDto>>()).HasColumnType("jsonb");
            entity.Property(p => p.BestMovesNotation).HasColumnName("best_moves_notation");
            entity.Property(p => p.BestMoveEquity).HasColumnName("best_move_equity");
            entity.Property(p => p.AlternativeMoves).HasColumnName("alternative_moves")
                .HasConversion(JsonConverter<List<AlternativeMove>>()).HasColumnType("jsonb");
            entity.Property(p => p.EvaluatorType).HasColumnName("evaluator_type").HasMaxLength(20);
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.SolvedCount).HasColumnName("solved_count");
            entity.Property(p => p.AttemptCount).HasColumnName("attempt_count");

            entity.HasIndex(p => p.PuzzleDate).IsUnique().HasDatabaseName("ix_daily_puzzles_date");
        });
    }

    private static void ConfigurePuzzleAttempts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PuzzleAttempt>(entity =>
        {
            entity.ToTable("puzzle_attempts");
            entity.HasKey(a => new { a.UserId, a.PuzzleDate });
            entity.Property(a => a.UserId).HasColumnName("user_id");
            entity.Property(a => a.PuzzleDate).HasColumnName("puzzle_date").HasMaxLength(10);
            entity.Property(a => a.PuzzleId).HasColumnName("puzzle_id");
            entity.Property(a => a.SubmittedMoves).HasColumnName("submitted_moves")
                .HasConversion(JsonConverter<List<MoveDto>>()).HasColumnType("jsonb");
            entity.Property(a => a.SubmittedNotation).HasColumnName("submitted_notation");
            entity.Property(a => a.IsCorrect).HasColumnName("is_correct");
            entity.Property(a => a.EquityLoss).HasColumnName("equity_loss");
            entity.Property(a => a.AttemptCount).HasColumnName("attempt_count");
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            entity.Property(a => a.SolvedAt).HasColumnName("solved_at");
            entity.Property(a => a.GaveUp).HasColumnName("gave_up");

            entity.HasIndex(a => a.UserId).HasDatabaseName("ix_puzzle_attempts_user_id");
        });
    }

    private static void ConfigurePuzzleStreaks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PuzzleStreakInfo>(entity =>
        {
            entity.ToTable("puzzle_streaks");
            entity.HasKey(s => s.UserId);
            entity.Property(s => s.UserId).HasColumnName("user_id");
            entity.Property(s => s.CurrentStreak).HasColumnName("current_streak");
            entity.Property(s => s.BestStreak).HasColumnName("best_streak");
            entity.Property(s => s.LastSolvedDate).HasColumnName("last_solved_date").HasMaxLength(10);
            entity.Property(s => s.TotalSolved).HasColumnName("total_solved");
            entity.Property(s => s.TotalAttempts).HasColumnName("total_attempts");
        });
    }

    private static void ConfigureRatingHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RatingHistoryEntry>(entity =>
        {
            entity.ToTable("rating_history");
            entity.HasKey(r => new { r.UserId, r.Timestamp });
            entity.Property(r => r.UserId).HasColumnName("user_id");
            entity.Property(r => r.Timestamp).HasColumnName("timestamp");
            entity.Property(r => r.Rating).HasColumnName("rating");
            entity.Property(r => r.RatingChange).HasColumnName("rating_change");
            entity.Property(r => r.GameId).HasColumnName("game_id");
            entity.Property(r => r.OpponentUserId).HasColumnName("opponent_user_id");
            entity.Property(r => r.OpponentUsername).HasColumnName("opponent_username").HasMaxLength(20);
            entity.Property(r => r.Won).HasColumnName("won");

            entity.HasIndex(r => r.UserId).HasDatabaseName("ix_rating_history_user_id");
            entity.HasIndex(r => new { r.UserId, r.Timestamp }).HasDatabaseName("ix_rating_history_user_time");
        });
    }

    private static void ConfigureBoardThemes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardTheme>(entity =>
        {
            entity.ToTable("board_themes");
            entity.HasKey(t => t.ThemeId);
            entity.Property(t => t.ThemeId).HasColumnName("theme_id");
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(t => t.Description).HasColumnName("description");
            entity.Property(t => t.AuthorId).HasColumnName("author_id");
            entity.Property(t => t.AuthorUsername).HasColumnName("author_username").HasMaxLength(20);
            entity.Property(t => t.Visibility).HasColumnName("visibility").HasConversion<string>();
            entity.Property(t => t.IsDefault).HasColumnName("is_default");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            entity.Property(t => t.UsageCount).HasColumnName("usage_count");
            entity.Property(t => t.LikeCount).HasColumnName("like_count");
            entity.Property(t => t.Colors).HasColumnName("colors")
                .HasConversion(JsonConverter<ThemeColors>()).HasColumnType("jsonb");
            entity.Property(t => t.ThumbnailUrl).HasColumnName("thumbnail_url");

            entity.HasIndex(t => t.AuthorId).HasDatabaseName("ix_board_themes_author_id");
            entity.HasIndex(t => t.IsDefault).HasDatabaseName("ix_board_themes_is_default");
            entity.HasIndex(t => t.Visibility).HasDatabaseName("ix_board_themes_visibility");
        });

        modelBuilder.Entity<ThemeLike>(entity =>
        {
            entity.ToTable("theme_likes");
            entity.HasKey(l => new { l.ThemeId, l.UserId });
            entity.Property(l => l.ThemeId).HasColumnName("theme_id");
            entity.Property(l => l.UserId).HasColumnName("user_id");
            entity.Property(l => l.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(l => l.UserId).HasDatabaseName("ix_theme_likes_user_id");
        });
    }
}
