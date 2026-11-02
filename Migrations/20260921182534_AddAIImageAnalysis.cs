using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAIImageAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIImageCategory",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AIImageConfidence",
                table: "Complaints",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIImageDescription",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIImageCategory",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AIImageConfidence",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AIImageDescription",
                table: "Complaints");
        }
    }
}
