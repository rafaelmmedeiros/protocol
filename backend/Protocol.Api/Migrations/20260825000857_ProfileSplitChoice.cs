using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProfileSplitChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Split",
                table: "training_profiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Split",
                table: "training_profiles");
        }
    }
}
