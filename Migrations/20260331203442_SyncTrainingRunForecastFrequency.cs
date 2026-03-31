using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    public partial class SyncTrainingRunForecastFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "forecast_frequency",
                table: "training_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "forecast_frequency",
                table: "training_runs");
        }
    }
}
