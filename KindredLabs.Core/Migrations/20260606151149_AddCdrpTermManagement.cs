using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCdrpTermManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeniedAt",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RenewalRequested",
                table: "CdrpCandidates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermExpiresAt",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "DeniedAt",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "RenewalRequested",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "TermExpiresAt",
                table: "CdrpCandidates");
        }
    }
}
