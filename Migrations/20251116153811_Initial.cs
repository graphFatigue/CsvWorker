using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsvWorker.CLI.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "trips",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tpep_pickup_datetime = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    tpep_dropoff_datetime = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    passenger_count = table.Column<short>(type: "smallint", nullable: true),
                    trip_distance = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    store_and_fwd_flag = table.Column<string>(type: "varchar(3)", nullable: false),
                    PULocationID = table.Column<int>(type: "int", nullable: true),
                    DOLocationID = table.Column<int>(type: "int", nullable: true),
                    fare_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    tip_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trips",
                schema: "dbo");
        }
    }
}
