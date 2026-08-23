using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExercisePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_exclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_exclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_exclusions_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preferred_variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MovementPattern = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preferred_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_preferred_variants_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_exclusions_ExerciseId",
                table: "exercise_exclusions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_exclusions_UserId_ExerciseId",
                table: "exercise_exclusions",
                columns: new[] { "UserId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_preferred_variants_ExerciseId",
                table: "preferred_variants",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_preferred_variants_UserId_MovementPattern",
                table: "preferred_variants",
                columns: new[] { "UserId", "MovementPattern" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_exclusions");

            migrationBuilder.DropTable(
                name: "preferred_variants");
        }
    }
}
