using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgriculturalCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "crop",
                table: "fields");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "cultivation_id",
                table: "sampling_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_seasons_farms_farm_id",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cultivations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cultivar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    planted_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ended_on = table.Column<DateOnly>(type: "date", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cultivations", x => x.id);
                    table.UniqueConstraint("ak_cultivations_id_field_id", x => new { x.id, x.field_id });
                    table.CheckConstraint("ck_cultivation_dates", "ended_on IS NULL OR ended_on >= planted_on");
                    table.ForeignKey(
                        name: "fk_cultivations_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cultivations_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "growth_stage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cultivation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observed_on = table.Column<DateOnly>(type: "date", nullable: false),
                    stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_growth_stage_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_growth_stage_records_cultivations_cultivation_id",
                        column: x => x.cultivation_id,
                        principalTable: "cultivations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sampling_plans_cultivation_id_field_id",
                table: "sampling_plans",
                columns: new[] { "cultivation_id", "field_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cultivations_field_id_planted_on",
                table: "cultivations",
                columns: new[] { "field_id", "planted_on" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_cultivations_season_id",
                table: "cultivations",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_date_per_cultivation",
                table: "growth_stage_records",
                columns: new[] { "cultivation_id", "observed_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_name_per_farm",
                table: "seasons",
                columns: new[] { "farm_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sampling_plans_cultivations_cultivation_id_field_id",
                table: "sampling_plans",
                columns: new[] { "cultivation_id", "field_id" },
                principalTable: "cultivations",
                principalColumns: new[] { "id", "field_id" },
                onDelete: ReferentialAction.Restrict);

            // Inclusive dates: planting starts after the preceding cycle's closure.
            // Enforced in PostgreSQL even when concurrent requests both pass the application guard.
            migrationBuilder.Sql("""
                ALTER TABLE cultivations ADD CONSTRAINT ex_cultivations_no_overlap
                EXCLUDE USING gist (field_id WITH =, daterange(planted_on, ended_on, '[]') WITH &&);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sampling_plans_cultivations_cultivation_id_field_id",
                table: "sampling_plans");

            migrationBuilder.DropTable(
                name: "growth_stage_records");

            migrationBuilder.DropTable(
                name: "cultivations");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropIndex(
                name: "ix_sampling_plans_cultivation_id_field_id",
                table: "sampling_plans");

            migrationBuilder.DropColumn(
                name: "cultivation_id",
                table: "sampling_plans");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<string>(
                name: "crop",
                table: "fields",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Undefined");
        }
    }
}
