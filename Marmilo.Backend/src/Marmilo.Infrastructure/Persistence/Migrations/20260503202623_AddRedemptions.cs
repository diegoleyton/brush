using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmilo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRedemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by_parent_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_redemptions_child_profiles_child_profile_id",
                        column: x => x.child_profile_id,
                        principalTable: "child_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_redemptions_families_family_id",
                        column: x => x.family_id,
                        principalTable: "families",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_redemptions_market_items_market_item_id",
                        column: x => x.market_item_id,
                        principalTable: "market_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_redemptions_parent_users_resolved_by_parent_user_id",
                        column: x => x.resolved_by_parent_user_id,
                        principalTable: "parent_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_child_profile_id",
                table: "redemptions",
                column: "child_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_family_id",
                table: "redemptions",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_market_item_id",
                table: "redemptions",
                column: "market_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_resolved_by_parent_user_id",
                table: "redemptions",
                column: "resolved_by_parent_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redemptions");
        }
    }
}
