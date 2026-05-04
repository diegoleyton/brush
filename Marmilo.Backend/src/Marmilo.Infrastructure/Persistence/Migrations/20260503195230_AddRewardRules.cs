using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmilo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reward_rule_id",
                table: "currency_ledger",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reward_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    currency_amount = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_reward_rules_families_family_id",
                        column: x => x.family_id,
                        principalTable: "families",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_currency_ledger_reward_rule_id",
                table: "currency_ledger",
                column: "reward_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_reward_rules_family_id",
                table: "reward_rules",
                column: "family_id");

            migrationBuilder.AddForeignKey(
                name: "FK_currency_ledger_reward_rules_reward_rule_id",
                table: "currency_ledger",
                column: "reward_rule_id",
                principalTable: "reward_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_currency_ledger_reward_rules_reward_rule_id",
                table: "currency_ledger");

            migrationBuilder.DropTable(
                name: "reward_rules");

            migrationBuilder.DropIndex(
                name: "IX_currency_ledger_reward_rule_id",
                table: "currency_ledger");

            migrationBuilder.DropColumn(
                name: "reward_rule_id",
                table: "currency_ledger");
        }
    }
}
