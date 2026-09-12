using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PpmV2.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddShiftAndUserIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_users_Status",
                table: "users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_Status_StartAtUtc",
                table: "shifts",
                columns: new[] { "Status", "StartAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Status",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_shifts_Status_StartAtUtc",
                table: "shifts");
        }
    }
}
