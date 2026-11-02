using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityToStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ComplaintStatusHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ComplaintStatusHistories");
        }
    }
}
