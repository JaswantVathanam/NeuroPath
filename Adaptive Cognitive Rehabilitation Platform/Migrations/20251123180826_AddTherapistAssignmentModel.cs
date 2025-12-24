using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdaptiveCognitiveRehabilitationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddTherapistAssignmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TherapistAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TherapistId = table.Column<int>(type: "int", nullable: false),
                    PatientUserId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TherapyFocus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedDifficultyLevel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapistAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapistAssignments_Users_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TherapistAssignments_Users_TherapistId",
                        column: x => x.TherapistId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TherapistAssignments_PatientUserId",
                table: "TherapistAssignments",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TherapistAssignments_TherapistId_PatientUserId_IsActive",
                table: "TherapistAssignments",
                columns: new[] { "TherapistId", "PatientUserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TherapistAssignments");
        }
    }
}
