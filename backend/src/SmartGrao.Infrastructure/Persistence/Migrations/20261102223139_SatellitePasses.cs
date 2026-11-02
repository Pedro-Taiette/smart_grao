using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SatellitePasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "satellite_passes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    scene_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    valid_fraction = table.Column<double>(type: "double precision", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ndvi_mean = table.Column<double>(type: "double precision", nullable: true),
                    ndvi_median = table.Column<double>(type: "double precision", nullable: true),
                    ndvi_p10 = table.Column<double>(type: "double precision", nullable: true),
                    ndvi_p90 = table.Column<double>(type: "double precision", nullable: true),
                    ndre_mean = table.Column<double>(type: "double precision", nullable: true),
                    ndre_median = table.Column<double>(type: "double precision", nullable: true),
                    ndre_p10 = table.Column<double>(type: "double precision", nullable: true),
                    ndre_p90 = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_satellite_passes", x => x.id);
                    table.ForeignKey(
                        name: "fk_satellite_passes_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "anomaly_zones",
                columns: table => new
                {
                    index = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    satellite_pass_id = table.Column<Guid>(type: "uuid", nullable: false),
                    geometry = table.Column<Polygon>(type: "geography(Polygon,4326)", nullable: false),
                    area_hectares = table.Column<double>(type: "double precision", nullable: false),
                    mean_drop = table.Column<double>(type: "double precision", nullable: false),
                    dropped_since_previous = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anomaly_zones", x => new { x.satellite_pass_id, x.index });
                    table.ForeignKey(
                        name: "fk_anomaly_zones_satellite_passes_satellite_pass_id",
                        column: x => x.satellite_pass_id,
                        principalTable: "satellite_passes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_satellite_passes_field_date",
                table: "satellite_passes",
                columns: new[] { "field_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anomaly_zones");

            migrationBuilder.DropTable(
                name: "satellite_passes");
        }
    }
}
