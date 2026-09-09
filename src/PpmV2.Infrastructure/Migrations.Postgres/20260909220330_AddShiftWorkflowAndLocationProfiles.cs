using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PpmV2.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddShiftWorkflowAndLocationProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Location detail-page fields
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "locations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "locations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "locations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "locations",
                type: "integer",
                nullable: true);

            // Participant workflow tracking
            migrationBuilder.AddColumn<int>(
                name: "ConfirmationStatus",
                table: "shift_participants",
                type: "integer",
                nullable: false,
                defaultValue: 1); // 1 = Accepted (Coordinator direct-assign default)

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "shift_participants",
                type: "timestamp with time zone",
                nullable: true);

            // Staff work-profile assignments (Festmitarbeiter ↔ Location)
            migrationBuilder.CreateTable(
                name: "user_location_assignments",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_location_assignments", x => new { x.UserId, x.LocationId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_location_assignments_LocationId",
                table: "user_location_assignments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_user_location_assignments_UserId",
                table: "user_location_assignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_location_assignments");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatus",
                table: "shift_participants");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "shift_participants");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "locations");
        }
    }
}
