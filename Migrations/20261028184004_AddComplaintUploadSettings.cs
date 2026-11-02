using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicGrievanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintUploadSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplaintUploadSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaximumImageSizeMb = table.Column<int>(type: "int", nullable: false),
                    AllowedImageFormats = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequireImage = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultipleImages = table.Column<bool>(type: "bit", nullable: false),
                    MaximumImagesPerComplaint = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintUploadSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintUploadSettings");
        }
    }
}
