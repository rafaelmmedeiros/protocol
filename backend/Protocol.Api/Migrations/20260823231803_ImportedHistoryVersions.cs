using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class ImportedHistoryVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_performed_workouts_UserId_ExternalWorkoutId",
                table: "performed_workouts");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "performed_workouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "performed_workouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "hevy_workout_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExternalWorkoutId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ExternallyUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MappingFailure = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hevy_workout_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_performed_workouts_UserId_ExternalWorkoutId_Version",
                table: "performed_workouts",
                columns: new[] { "UserId", "ExternalWorkoutId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hevy_workout_snapshots_UserId_ExternalWorkoutId_Version",
                table: "hevy_workout_snapshots",
                columns: new[] { "UserId", "ExternalWorkoutId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hevy_workout_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_performed_workouts_UserId_ExternalWorkoutId_Version",
                table: "performed_workouts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "performed_workouts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "performed_workouts");

            migrationBuilder.CreateIndex(
                name: "IX_performed_workouts_UserId_ExternalWorkoutId",
                table: "performed_workouts",
                columns: new[] { "UserId", "ExternalWorkoutId" });
        }
    }
}
