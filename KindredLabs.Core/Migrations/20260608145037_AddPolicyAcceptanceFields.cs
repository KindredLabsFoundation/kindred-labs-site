using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredLabs.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyAcceptanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyPolicyAcceptedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyVersion",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsOfServiceAcceptedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsOfServiceVersion",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyPolicyAcceptedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyVersion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsOfServiceAcceptedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsOfServiceVersion",
                table: "AspNetUsers");
        }
    }
}
