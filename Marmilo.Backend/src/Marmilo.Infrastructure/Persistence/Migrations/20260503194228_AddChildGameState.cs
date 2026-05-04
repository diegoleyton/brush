using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmilo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChildGameState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "child_game_states",
                columns: table => new
                {
                    child_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coins_balance = table.Column<int>(type: "integer", nullable: false),
                    brush_session_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    pending_reward = table.Column<bool>(type: "boolean", nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false),
                    pet_state_json = table.Column<string>(type: "jsonb", nullable: false),
                    room_state_json = table.Column<string>(type: "jsonb", nullable: false),
                    inventory_state_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_child_game_states", x => x.child_profile_id);
                    table.ForeignKey(
                        name: "FK_child_game_states_child_profiles_child_profile_id",
                        column: x => x.child_profile_id,
                        principalTable: "child_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO child_game_states
                    (child_profile_id, coins_balance, brush_session_duration_minutes, pending_reward, muted, pet_state_json, room_state_json, inventory_state_json, created_at, updated_at)
                SELECT
                    id,
                    0,
                    2,
                    FALSE,
                    FALSE,
                    '{}'::jsonb,
                    '{}'::jsonb,
                    '{}'::jsonb,
                    NOW(),
                    NOW()
                FROM child_profiles
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM child_game_states
                    WHERE child_game_states.child_profile_id = child_profiles.id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "child_game_states");
        }
    }
}
