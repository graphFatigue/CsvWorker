using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsvWorker.CLI.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndexesForSearchOptimiztion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trip_duration_seconds",
                schema: "dbo",
                table: "trips",
                type: "int",
                nullable: false,
                computedColumnSql: "DATEDIFF(SECOND, tpep_pickup_datetime, tpep_dropoff_datetime)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_trips_PULocationID",
                schema: "dbo",
                table: "trips",
                column: "PULocationID");

            migrationBuilder.CreateIndex(
                name: "IX_trips_TripDistance",
                schema: "dbo",
                table: "trips",
                column: "trip_distance");

            migrationBuilder.CreateIndex(
                name: "IX_trips_TripDurationSeconds",
                schema: "dbo",
                table: "trips",
                column: "trip_duration_seconds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trips_PULocationID",
                schema: "dbo",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "IX_trips_TripDistance",
                schema: "dbo",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "IX_trips_TripDurationSeconds",
                schema: "dbo",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "trip_duration_seconds",
                schema: "dbo",
                table: "trips");
        }
    }
}
