using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddResolutionReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminRemarks",
                table: "ComplaintResolutionResponses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "ComplaintResolutionResponses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ComplaintResolutionResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "ComplaintResolutionResponses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminRemarks",
                table: "ComplaintResolutionResponses");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "ComplaintResolutionResponses");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ComplaintResolutionResponses");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ComplaintResolutionResponses");
        }
    }
}
