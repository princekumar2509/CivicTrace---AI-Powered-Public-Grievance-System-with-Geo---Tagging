using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddResolutionEscalationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAutomaticOverdueDetection",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEscalationNotifications",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableResolutionProof",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EscalationAfterDeadlineHours",
                table: "ComplaintUploadSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HighPriorityResolutionHours",
                table: "ComplaintUploadSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LowPriorityResolutionHours",
                table: "ComplaintUploadSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MediumPriorityResolutionHours",
                table: "ComplaintUploadSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAutomaticOverdueDetection",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "EnableEscalationNotifications",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "EnableResolutionProof",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "EscalationAfterDeadlineHours",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "HighPriorityResolutionHours",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "LowPriorityResolutionHours",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "MediumPriorityResolutionHours",
                table: "ComplaintUploadSettings");
        }
    }
}
