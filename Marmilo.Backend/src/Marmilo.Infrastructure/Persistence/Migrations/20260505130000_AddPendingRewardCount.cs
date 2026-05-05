using Marmilo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmilo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MarmiloDbContext))]
    [Migration("20260505130000_AddPendingRewardCount")]
    public partial class AddPendingRewardCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pending_reward_count",
                table: "child_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE child_game_states
                SET pending_reward_count = CASE
                    WHEN pending_reward THEN 1
                    ELSE 0
                END;
                """);

            migrationBuilder.DropColumn(
                name: "pending_reward",
                table: "child_game_states");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pending_reward",
                table: "child_game_states",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE child_game_states
                SET pending_reward = CASE
                    WHEN pending_reward_count > 0 THEN TRUE
                    ELSE FALSE
                END;
                """);

            migrationBuilder.DropColumn(
                name: "pending_reward_count",
                table: "child_game_states");
        }
    }
}
