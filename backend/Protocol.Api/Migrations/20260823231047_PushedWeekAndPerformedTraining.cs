using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class PushedWeekAndPerformedTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HevyRoutineFolderId",
                table: "generated_weeks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HevyRoutineId",
                table: "generated_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "performed_workouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExternalWorkoutId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalRoutineId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExternalTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExternallyUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performed_workouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "performed_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalTemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performed_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_performed_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_performed_exercises_performed_workouts_PerformedWorkoutId",
                        column: x => x.PerformedWorkoutId,
                        principalTable: "performed_workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performed_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    Reps = table.Column<double>(type: "double precision", nullable: true),
                    RepsInReserve = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performed_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_performed_sets_performed_exercises_PerformedExerciseId",
                        column: x => x.PerformedExerciseId,
                        principalTable: "performed_exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_sessions_HevyRoutineId",
                table: "generated_sessions",
                column: "HevyRoutineId");

            migrationBuilder.CreateIndex(
                name: "IX_performed_exercises_ExerciseId",
                table: "performed_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_performed_exercises_PerformedWorkoutId",
                table: "performed_exercises",
                column: "PerformedWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_performed_sets_PerformedExerciseId",
                table: "performed_sets",
                column: "PerformedExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_performed_workouts_ExternalRoutineId",
                table: "performed_workouts",
                column: "ExternalRoutineId");

            migrationBuilder.CreateIndex(
                name: "IX_performed_workouts_UserId_ExternalWorkoutId",
                table: "performed_workouts",
                columns: new[] { "UserId", "ExternalWorkoutId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "performed_sets");

            migrationBuilder.DropTable(
                name: "performed_exercises");

            migrationBuilder.DropTable(
                name: "performed_workouts");

            migrationBuilder.DropIndex(
                name: "IX_generated_sessions_HevyRoutineId",
                table: "generated_sessions");

            migrationBuilder.DropColumn(
                name: "HevyRoutineFolderId",
                table: "generated_weeks");

            migrationBuilder.DropColumn(
                name: "HevyRoutineId",
                table: "generated_sessions");
        }
    }
}
