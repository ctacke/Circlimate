using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Circlimate.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    city_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    city_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    oldest_data_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    newest_data_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_temperature_c = table.Column<double>(type: "double precision", nullable: true),
                    max_temperature_c = table.Column<double>(type: "double precision", nullable: true),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.city_id);
                });

            migrationBuilder.CreateTable(
                name: "temperature_data",
                columns: table => new
                {
                    temperature_data_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    record_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_temperature_c = table.Column<double>(type: "double precision", nullable: false),
                    min_temperature_c = table.Column<double>(type: "double precision", nullable: false),
                    provider_id = table.Column<int>(type: "integer", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temperature_data", x => x.temperature_data_id);
                    table.ForeignKey(
                        name: "FK_temperature_data_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_cities_name",
                table: "cities",
                column: "city_name");

            migrationBuilder.CreateIndex(
                name: "uq_city_location",
                table: "cities",
                columns: new[] { "city_name", "latitude", "longitude" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_temperature_city_date",
                table: "temperature_data",
                columns: new[] { "city_id", "record_date" });

            migrationBuilder.CreateIndex(
                name: "idx_temperature_provider",
                table: "temperature_data",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "uq_city_date_provider",
                table: "temperature_data",
                columns: new[] { "city_id", "record_date", "provider_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "temperature_data");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
