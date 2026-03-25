using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RetailForecast.Data;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RetailForecastDbContext))]
    [Migration("20260325120000_RemoveKpiDataType")]
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
