using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Protocol.Api.Migrations
{
    /// <inheritdoc />
    public partial class RepairZeroFolderIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A data repair, not a schema change. Rows written before the write-response envelope
            // was understood stored a folder identifier of zero: Hevy's OpenAPI document declares
            // POST /v1/routine_folders as returning the bare object and the service returns
            // { "routine_folder": { ... } }, so the identifier deserialised to the default and was
            // saved without complaint. Every routine then went to folder 0, which does not exist,
            // and Hevy refused it.
            //
            // Nulling it is what lets those weeks recover: the push treats an absent folder as one
            // to create. Forward-only (root standard 10) -- the mistake is corrected by a new
            // migration rather than by editing the one that allowed it.
            migrationBuilder.Sql(
                @"UPDATE generated_weeks SET ""HevyRoutineFolderId"" = NULL WHERE ""HevyRoutineFolderId"" <= 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing. Restoring a value that was never a real folder would restore the bug.
        }
    }
}
