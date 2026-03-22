using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "datasets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_extension",
                table: "datasets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size_bytes",
                table: "datasets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "storage_file_name",
                table: "datasets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "file_extension",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "file_size_bytes",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "storage_file_name",
                table: "datasets");
        }
    }
}
