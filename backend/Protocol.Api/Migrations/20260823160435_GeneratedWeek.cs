using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class GeneratedWeek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_weeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Goal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    SessionDurationSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_weeks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "generated_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedWeekId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_sessions_generated_weeks_GeneratedWeekId",
                        column: x => x.GeneratedWeekId,
                        principalTable: "generated_weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generated_prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    MinReps = table.Column<int>(type: "integer", nullable: false),
                    MaxReps = table.Column<int>(type: "integer", nullable: false),
                    RepsInReserve = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_prescriptions_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_generated_prescriptions_generated_sessions_GeneratedSession~",
                        column: x => x.GeneratedSessionId,
                        principalTable: "generated_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_prescriptions_ExerciseId",
                table: "generated_prescriptions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_prescriptions_GeneratedSessionId",
                table: "generated_prescriptions",
                column: "GeneratedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_sessions_GeneratedWeekId",
                table: "generated_sessions",
                column: "GeneratedWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_weeks_UserId_GeneratedAt",
                table: "generated_weeks",
                columns: new[] { "UserId", "GeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_prescriptions");

            migrationBuilder.DropTable(
                name: "generated_sessions");

            migrationBuilder.DropTable(
                name: "generated_weeks");
        }
    }
}
