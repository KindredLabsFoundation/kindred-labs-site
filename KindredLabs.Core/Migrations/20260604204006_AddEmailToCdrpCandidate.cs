using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToCdrpCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CdrpCandidates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CdrpCandidates_Email",
                table: "CdrpCandidates",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CdrpCandidates_Email",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CdrpCandidates");
        }
    }
}
