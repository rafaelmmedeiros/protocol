using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class WeekVolumeBand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 6.0, not 0 and not 8.0. Every row that predates this column was generated
            // under TD-014's target of 6.0 and *before* TD-022 created a ceiling, so a ceiling
            // equal to the target is the faithful statement that those weeks were built to stop
            // there. Writing 8.0 would assert they could buy volume above target that no code
            // producing them was capable of; writing 0 would report every muscle in them as
            // infinitely over target (ADR-029, revision of 2026-08-24).
            //
            // This is an assertion about history rather than a recovered fact. It is here rather
            // than in a data-fixing script because standard 10 makes migrations forward-only:
            // this value is what those rows will mean forever.
            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyCeilingFractionalSets",
                table: "generated_weeks",
                type: "numeric",
                nullable: false,
                defaultValue: 6.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyTargetFractionalSets",
                table: "generated_weeks",
                type: "numeric",
                nullable: false,
                defaultValue: 6.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyCeilingFractionalSets",
                table: "generated_weeks");

            migrationBuilder.DropColumn(
                name: "WeeklyTargetFractionalSets",
                table: "generated_weeks");
        }
    }
}
