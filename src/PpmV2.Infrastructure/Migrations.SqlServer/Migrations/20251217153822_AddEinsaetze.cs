using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PpmV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEinsaetze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Einsaetze",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Einsaetze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Einsaetze_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EinsatzParticipants",
                columns: table => new
                {
                    EinsatzId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EinsatzParticipants", x => new { x.EinsatzId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EinsatzParticipants_Einsaetze_EinsatzId",
                        column: x => x.EinsatzId,
                        principalTable: "Einsaetze",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Einsaetze_LocationId",
                table: "Einsaetze",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EinsatzParticipants_EinsatzId",
                table: "EinsatzParticipants",
                column: "EinsatzId");

            migrationBuilder.CreateIndex(
                name: "IX_EinsatzParticipants_EinsatzId_Role",
                table: "EinsatzParticipants",
                columns: new[] { "EinsatzId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_EinsatzParticipants_UserId",
                table: "EinsatzParticipants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EinsatzParticipants");

            migrationBuilder.DropTable(
                name: "Einsaetze");
        }
    }
}
