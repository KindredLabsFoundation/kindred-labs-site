using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CdrpCandidates_AspNetUsers_UserId",
                table: "CdrpCandidates");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SubmissionLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CdrpCandidates",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLogs_UserId",
                table: "SubmissionLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CdrpCandidates_AspNetUsers_UserId",
                table: "CdrpCandidates",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionLogs_AspNetUsers_UserId",
                table: "SubmissionLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CdrpCandidates_AspNetUsers_UserId",
                table: "CdrpCandidates");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionLogs_AspNetUsers_UserId",
                table: "SubmissionLogs");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionLogs_UserId",
                table: "SubmissionLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SubmissionLogs");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CdrpCandidates",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CdrpCandidates_AspNetUsers_UserId",
                table: "CdrpCandidates",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
