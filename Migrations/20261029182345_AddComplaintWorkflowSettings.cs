using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintWorkflowSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAnonymousComplaints",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCitizensToReopenComplaints",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNewComplaints",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableCitizenFeedback",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableDuplicateDetection",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireComplaintDescription",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireLocation",
                table: "ComplaintUploadSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowAnonymousComplaints",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "AllowCitizensToReopenComplaints",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "AllowNewComplaints",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "EnableCitizenFeedback",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "EnableDuplicateDetection",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "RequireComplaintDescription",
                table: "ComplaintUploadSettings");

            migrationBuilder.DropColumn(
                name: "RequireLocation",
                table: "ComplaintUploadSettings");
        }
    }
}
