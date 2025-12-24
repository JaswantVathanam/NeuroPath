using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdaptiveCognitiveRehabilitationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddGameIdAndLinkCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "GameSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LinkCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentChildRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentUserId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentChildRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkCodes_Code",
                table: "LinkCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinkCodes_UserId",
                table: "LinkCodes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_ParentUserId_StudentUserId",
                table: "ParentChildRelations",
                columns: new[] { "ParentUserId", "StudentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_StudentUserId",
                table: "ParentChildRelations",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_UserId",
                table: "ParentChildRelations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_UserId1",
                table: "ParentChildRelations",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkCodes");

            migrationBuilder.DropTable(
                name: "ParentChildRelations");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "GameSessions");
        }
    }
}
