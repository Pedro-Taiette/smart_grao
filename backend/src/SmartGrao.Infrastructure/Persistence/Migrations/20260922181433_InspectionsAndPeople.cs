using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InspectionsAndPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                    table.ForeignKey(
                        name: "fk_people_farms_farm_id",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cultivation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampling_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsible_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scheduled_for = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspections", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspections_cultivations_cultivation_id",
                        column: x => x.cultivation_id,
                        principalTable: "cultivations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_people_responsible_id",
                        column: x => x.responsible_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_protocols_protocol_id",
                        column: x => x.protocol_id,
                        principalTable: "protocols",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_sampling_plans_sampling_plan_id",
                        column: x => x.sampling_plan_id,
                        principalTable: "sampling_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampling_point_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    accuracy_meters = table.Column<double>(type: "double precision", nullable: true),
                    growth_stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_observations", x => x.id);
                    table.ForeignKey(
                        name: "fk_observations_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_observations_sampling_points_sampling_point_id",
                        column: x => x.sampling_point_id,
                        principalTable: "sampling_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "target_counts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    detected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_target_counts", x => x.id);
                    table.ForeignKey(
                        name: "fk_target_counts_monitoring_targets_target_id",
                        column: x => x.target_id,
                        principalTable: "monitoring_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_target_counts_observations_observation_id",
                        column: x => x.observation_id,
                        principalTable: "observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_target_counts_protocol_items_protocol_item_id",
                        column: x => x.protocol_item_id,
                        principalTable: "protocol_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_cultivation_id_scheduled_for",
                table: "inspections",
                columns: new[] { "cultivation_id", "scheduled_for" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_field_id",
                table: "inspections",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_protocol_id",
                table: "inspections",
                column: "protocol_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_responsible_id_status",
                table: "inspections",
                columns: new[] { "responsible_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_sampling_plan_id",
                table: "inspections",
                column: "sampling_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_observations_point_per_inspection",
                table: "observations",
                columns: new[] { "inspection_id", "sampling_point_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_observations_sampling_point_id",
                table: "observations",
                column: "sampling_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_farm_id_name",
                table: "people",
                columns: new[] { "farm_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_target_counts_item_per_observation",
                table: "target_counts",
                columns: new[] { "observation_id", "protocol_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_target_counts_protocol_item_id",
                table: "target_counts",
                column: "protocol_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_target_counts_target_id_detected",
                table: "target_counts",
                columns: new[] { "target_id", "detected" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "target_counts");

            migrationBuilder.DropTable(
                name: "observations");

            migrationBuilder.DropTable(
                name: "inspections");

            migrationBuilder.DropTable(
                name: "people");
        }
    }
}
