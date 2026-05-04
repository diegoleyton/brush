using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmilo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    created_by_parent_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_ledger", x => x.id);
                    table.ForeignKey(
                        name: "FK_currency_ledger_child_profiles_child_profile_id",
                        column: x => x.child_profile_id,
                        principalTable: "child_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_currency_ledger_families_family_id",
                        column: x => x.family_id,
                        principalTable: "families",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_currency_ledger_parent_users_created_by_parent_user_id",
                        column: x => x.created_by_parent_user_id,
                        principalTable: "parent_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_currency_ledger_child_profile_id",
                table: "currency_ledger",
                column: "child_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_currency_ledger_created_by_parent_user_id",
                table: "currency_ledger",
                column: "created_by_parent_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_currency_ledger_family_id",
                table: "currency_ledger",
                column: "family_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency_ledger");
        }
    }
}
