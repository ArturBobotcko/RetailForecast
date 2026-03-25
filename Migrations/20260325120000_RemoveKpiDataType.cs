using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKpiDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_type",
                table: "kpis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_type",
                table: "kpis",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
