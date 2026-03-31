using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingRunForecastHorizon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "forecast_horizon",
                table: "training_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "forecast_horizon",
                table: "training_runs");
        }
    }
}
