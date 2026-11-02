using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhotosAndDirectedPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "zone_date",
                table: "sampling_plans",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cultivation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organ = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    growth_stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    accuracy_meters = table.Column<double>(type: "double precision", nullable: true),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_simulated = table.Column<bool>(type: "boolean", nullable: false),
                    taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_photos_cultivations_cultivation_id",
                        column: x => x.cultivation_id,
                        principalTable: "cultivations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_photos_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_photos_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_photos_observations_observation_id",
                        column: x => x.observation_id,
                        principalTable: "observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_photos_cultivation_id_taken_at",
                table: "photos",
                columns: new[] { "cultivation_id", "taken_at" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_field_id",
                table: "photos",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "ix_photos_inspection_id",
                table: "photos",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_photos_observation_id",
                table: "photos",
                column: "observation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropColumn(
                name: "zone_date",
                table: "sampling_plans");
        }
    }
}
