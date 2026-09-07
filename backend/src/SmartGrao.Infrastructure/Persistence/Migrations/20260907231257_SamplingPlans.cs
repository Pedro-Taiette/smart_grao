using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SamplingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sampling_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    spacing_meters = table.Column<double>(type: "double precision", nullable: false),
                    edge_buffer_meters = table.Column<double>(type: "double precision", nullable: false),
                    target_point_count = table.Column<int>(type: "integer", nullable: true),
                    field_area_hectares = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    subdivision_recommended = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sampling_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_sampling_plans_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sampling_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampling_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sampling_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_sampling_points_sampling_plans_sampling_plan_id",
                        column: x => x.sampling_plan_id,
                        principalTable: "sampling_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sampling_plans_field_recent_first",
                table: "sampling_plans",
                columns: new[] { "field_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_sampling_points_location_gist",
                table: "sampling_points",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_sampling_points_sequence_per_plan",
                table: "sampling_points",
                columns: new[] { "sampling_plan_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sampling_points");

            migrationBuilder.DropTable(
                name: "sampling_plans");
        }
    }
}
