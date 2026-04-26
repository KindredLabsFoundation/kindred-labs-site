using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureModelRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FormType",
                table: "SubmissionLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FormType",
                table: "Drafts",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CdrpCandidates",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLogs_SubmittedAt",
                table: "SubmissionLogs",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_ExpiresAt",
                table: "Drafts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPeriods_IsLocked",
                table: "CommentPeriods",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_CdrpCandidates_Status",
                table: "CdrpCandidates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionLogs_SubmittedAt",
                table: "SubmissionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Drafts_ExpiresAt",
                table: "Drafts");

            migrationBuilder.DropIndex(
                name: "IX_CommentPeriods_IsLocked",
                table: "CommentPeriods");

            migrationBuilder.DropIndex(
                name: "IX_CdrpCandidates_Status",
                table: "CdrpCandidates");

            migrationBuilder.AlterColumn<int>(
                name: "FormType",
                table: "SubmissionLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "FormType",
                table: "Drafts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "CdrpCandidates",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
