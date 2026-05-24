using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backgammon.Server.Data.Migrations;
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.CreateTable(
                name: "board_themes",
                columns: table => new
                {
                    theme_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<string>(type: "text", nullable: false),
                    author_username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    colors = table.Column<string>(type: "jsonb", nullable: false),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_themes", x => x.theme_id);
                });

            migrationBuilder.CreateTable(
                name: "daily_puzzles",
                columns: table => new
                {
                    puzzle_id = table.Column<string>(type: "text", nullable: false),
                    puzzle_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    position_sgf = table.Column<string>(type: "text", nullable: false),
                    current_player = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    dice = table.Column<string>(type: "jsonb", nullable: false),
                    board_state = table.Column<string>(type: "jsonb", nullable: false),
                    white_checkers_on_bar = table.Column<int>(type: "integer", nullable: false),
                    red_checkers_on_bar = table.Column<int>(type: "integer", nullable: false),
                    white_born_off = table.Column<int>(type: "integer", nullable: false),
                    red_born_off = table.Column<int>(type: "integer", nullable: false),
                    best_moves = table.Column<string>(type: "jsonb", nullable: false),
                    best_moves_notation = table.Column<string>(type: "text", nullable: false),
                    best_move_equity = table.Column<double>(type: "double precision", nullable: false),
                    alternative_moves = table.Column<string>(type: "jsonb", nullable: false),
                    evaluator_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    solved_count = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_puzzles", x => x.puzzle_id);
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    friend_user_id = table.Column<string>(type: "text", nullable: false),
                    friend_username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    friend_display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    initiated_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friendships", x => new { x.user_id, x.friend_user_id });
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    game_id = table.Column<string>(type: "text", nullable: false),
                    core_game = table.Column<string>(type: "jsonb", nullable: false),
                    white_player_id = table.Column<string>(type: "text", nullable: true),
                    red_player_id = table.Column<string>(type: "text", nullable: true),
                    white_user_id = table.Column<string>(type: "text", nullable: true),
                    red_user_id = table.Column<string>(type: "text", nullable: true),
                    white_player_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    red_player_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    game_started = table.Column<bool>(type: "boolean", nullable: false),
                    board_state = table.Column<string>(type: "jsonb", nullable: false),
                    white_checkers_on_bar = table.Column<int>(type: "integer", nullable: false),
                    red_checkers_on_bar = table.Column<int>(type: "integer", nullable: false),
                    white_born_off = table.Column<int>(type: "integer", nullable: false),
                    red_born_off = table.Column<int>(type: "integer", nullable: false),
                    current_player = table.Column<string>(type: "text", nullable: false),
                    die1 = table.Column<int>(type: "integer", nullable: false),
                    die2 = table.Column<int>(type: "integer", nullable: false),
                    remaining_moves = table.Column<string>(type: "jsonb", nullable: false),
                    doubling_cube_value = table.Column<int>(type: "integer", nullable: false),
                    doubling_cube_owner = table.Column<string>(type: "text", nullable: true),
                    moves = table.Column<string>(type: "jsonb", nullable: false),
                    move_count = table.Column<int>(type: "integer", nullable: false),
                    turns = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    IsAiOpponent = table.Column<bool>(type: "boolean", nullable: false),
                    is_rated = table.Column<bool>(type: "boolean", nullable: false),
                    white_rating_before = table.Column<int>(type: "integer", nullable: true),
                    red_rating_before = table.Column<int>(type: "integer", nullable: true),
                    WhiteRatingAfter = table.Column<int>(type: "integer", nullable: true),
                    RedRatingAfter = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    match_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.game_id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    match_id = table.Column<string>(type: "text", nullable: false),
                    core_match = table.Column<string>(type: "jsonb", nullable: false),
                    player1_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    player2_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    winner_id = table.Column<string>(type: "text", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    opponent_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lobby_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_open_lobby = table.Column<bool>(type: "boolean", nullable: false),
                    is_rated = table.Column<bool>(type: "boolean", nullable: false),
                    player1_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    player2_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    games_summary = table.Column<string>(type: "jsonb", nullable: false),
                    is_correspondence = table.Column<bool>(type: "boolean", nullable: false),
                    time_per_move_days = table.Column<int>(type: "integer", nullable: false),
                    turn_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_turn_player_id = table.Column<string>(type: "text", nullable: true),
                    target_score = table.Column<int>(type: "integer", nullable: false),
                    player1_id = table.Column<string>(type: "text", nullable: false),
                    player2_id = table.Column<string>(type: "text", nullable: false),
                    player1_score = table.Column<int>(type: "integer", nullable: false),
                    player2_score = table.Column<int>(type: "integer", nullable: false),
                    is_crawford_game = table.Column<bool>(type: "boolean", nullable: false),
                    HasCrawfordGameBeenPlayed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_game_id = table.Column<string>(type: "text", nullable: true),
                    game_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "puzzle_attempts",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    puzzle_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    puzzle_id = table.Column<string>(type: "text", nullable: false),
                    submitted_moves = table.Column<string>(type: "jsonb", nullable: false),
                    submitted_notation = table.Column<string>(type: "text", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    equity_loss = table.Column<double>(type: "double precision", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    solved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gave_up = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puzzle_attempts", x => new { x.user_id, x.puzzle_date });
                });

            migrationBuilder.CreateTable(
                name: "puzzle_streaks",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    best_streak = table.Column<int>(type: "integer", nullable: false),
                    last_solved_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    total_solved = table.Column<int>(type: "integer", nullable: false),
                    total_attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puzzle_streaks", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "rating_history",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    rating_change = table.Column<int>(type: "integer", nullable: false),
                    game_id = table.Column<string>(type: "text", nullable: false),
                    opponent_user_id = table.Column<string>(type: "text", nullable: true),
                    opponent_username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    won = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_history", x => new { x.user_id, x.timestamp });
                });

            migrationBuilder.CreateTable(
                name: "theme_likes",
                columns: table => new
                {
                    theme_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_theme_likes", x => new { x.theme_id, x.user_id });
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    username_normalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_normalized = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    stats = table.Column<string>(type: "jsonb", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    peak_rating = table.Column<int>(type: "integer", nullable: false),
                    rating_last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rated_games_count = table.Column<int>(type: "integer", nullable: false),
                    linked_anonymous_ids = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    banned_reason = table.Column<string>(type: "text", nullable: true),
                    banned_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    profile_privacy = table.Column<string>(type: "text", nullable: false),
                    game_history_privacy = table.Column<string>(type: "text", nullable: false),
                    friends_list_privacy = table.Column<string>(type: "text", nullable: false),
                    selected_theme_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_board_themes_author_id",
                table: "board_themes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_themes_is_default",
                table: "board_themes",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_board_themes_visibility",
                table: "board_themes",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "ix_daily_puzzles_date",
                table: "daily_puzzles",
                column: "puzzle_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_friendships_user_id",
                table: "friendships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_friendships_user_status",
                table: "friendships",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_games_created_at",
                table: "games",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_games_match_id",
                table: "games",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_games_red_user_id",
                table: "games",
                column: "red_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_games_status",
                table: "games",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_games_white_user_id",
                table: "games",
                column: "white_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_current_turn_player",
                table: "matches",
                column: "current_turn_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_is_open_lobby",
                table: "matches",
                column: "is_open_lobby");

            migrationBuilder.CreateIndex(
                name: "ix_matches_last_updated_at",
                table: "matches",
                column: "last_updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_matches_lobby_status",
                table: "matches",
                column: "lobby_status");

            migrationBuilder.CreateIndex(
                name: "ix_matches_player1_id",
                table: "matches",
                column: "player1_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_player2_id",
                table: "matches",
                column: "player2_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_status",
                table: "matches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_attempts_user_id",
                table: "puzzle_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rating_history_user_id",
                table: "rating_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rating_history_user_time",
                table: "rating_history",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_theme_likes_user_id",
                table: "theme_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_normalized",
                table: "users",
                column: "email_normalized");

            migrationBuilder.CreateIndex(
                name: "ix_users_rating",
                table: "users",
                column: "rating");

            migrationBuilder.CreateIndex(
                name: "ix_users_username_normalized",
                table: "users",
                column: "username_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username_trgm",
                table: "users",
                column: "username")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_themes");

            migrationBuilder.DropTable(
                name: "daily_puzzles");

            migrationBuilder.DropTable(
                name: "friendships");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "puzzle_attempts");

            migrationBuilder.DropTable(
                name: "puzzle_streaks");

            migrationBuilder.DropTable(
                name: "rating_history");

            migrationBuilder.DropTable(
                name: "theme_likes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
