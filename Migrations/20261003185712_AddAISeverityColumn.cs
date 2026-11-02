using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAISeverityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AISeverity",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AISeverity",
                table: "Complaints");
        }
    }
}
