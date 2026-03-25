using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RetailForecast.Data;

#nullable disable

namespace RetailForecast.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RetailForecastDbContext))]
    [Migration("20260325170000_AddTrainingRunMlFields")]
    public partial class AddTrainingRunMlFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "training_runs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_job_id",
                table: "training_runs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_message",
                table: "training_runs");

            migrationBuilder.DropColumn(
                name: "external_job_id",
                table: "training_runs");
        }
    }
}
