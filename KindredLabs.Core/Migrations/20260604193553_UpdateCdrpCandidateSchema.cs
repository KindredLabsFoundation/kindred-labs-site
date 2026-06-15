using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCdrpCandidateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseToken",
                table: "CdrpCandidates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseTokenExpiry",
                table: "CdrpCandidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplementaryData",
                table: "CdrpCandidates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseToken",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "ResponseTokenExpiry",
                table: "CdrpCandidates");

            migrationBuilder.DropColumn(
                name: "SupplementaryData",
                table: "CdrpCandidates");
        }
    }
}
