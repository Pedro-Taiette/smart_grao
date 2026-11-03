using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnalysisQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_jobs_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    label = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: true),
                    cercosporiose_probability = table.Column<double>(type: "double precision", nullable: true),
                    healthy_probability = table.Column<double>(type: "double precision", nullable: true),
                    other_spots_probability = table.Column<double>(type: "double precision", nullable: true),
                    model_version = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    heatmap_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_of = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessments_assessments_review_of",
                        column: x => x.review_of,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessments_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_photo_id",
                table: "analysis_jobs",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_queue",
                table: "analysis_jobs",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_photo_id_assessed_at",
                table: "assessments",
                columns: new[] { "photo_id", "assessed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_review_of",
                table: "assessments",
                column: "review_of");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_jobs");

            migrationBuilder.DropTable(
                name: "assessments");
        }
    }
}
