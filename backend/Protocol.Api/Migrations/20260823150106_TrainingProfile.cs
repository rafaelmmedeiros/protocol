using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrainingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Goal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    SessionDurationSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_profiles_UserId",
                table: "training_profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_profiles");
        }
    }
}
