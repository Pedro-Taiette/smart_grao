using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TargetCatalogAndProtocols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitoring_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    common_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    scientific_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    crop = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    automation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monitoring_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "protocols",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    crop = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protocols", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "protocol_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organ = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reference_threshold = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    reference_source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    photos_requested = table.Column<int>(type: "integer", nullable: false),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protocol_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_protocol_items_monitoring_targets_target_id",
                        column: x => x.target_id,
                        principalTable: "monitoring_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocol_items_protocols_protocol_id",
                        column: x => x.protocol_id,
                        principalTable: "protocols",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "monitoring_targets",
                columns: new[] { "id", "automation", "code", "common_name", "created_at", "crop", "kind", "scientific_name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), "ManualRecord", "spodoptera_frugiperda", "Lagarta-do-cartucho", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Spodoptera frugiperda", null },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), "ManualRecord", "dalbulus_maidis", "Cigarrinha-do-milho", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Dalbulus maidis", null },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), "ManualRecord", "diceraeus_melacanthus", "Percevejo-barriga-verde", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Diceraeus melacanthus", null },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), "ManualRecord", "rhopalosiphum_maidis", "Pulgão-do-milho", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Rhopalosiphum maidis", null },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), "ManualRecord", "elasmopalpus_lignosellus", "Lagarta-elasmo", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Elasmopalpus lignosellus", null },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), "ManualRecord", "agrotis_ipsilon", "Lagarta-rosca", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Agrotis ipsilon", null },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), "ManualRecord", "diatraea_saccharalis", "Broca-da-cana", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Diatraea saccharalis", null },
                    { new Guid("a1000000-0000-4000-8000-000000000008"), "ManualRecord", "helicoverpa_zea", "Lagarta-da-espiga", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Helicoverpa zea", null },
                    { new Guid("a1000000-0000-4000-8000-000000000009"), "ManualRecord", "diabrotica_speciosa", "Larva-alfinete", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Diabrotica speciosa", null },
                    { new Guid("a1000000-0000-4000-8000-000000000010"), "ManualRecord", "coros", "Corós", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "Pest", "Diloboderus abderus e Phyllophaga spp.", null },
                    { new Guid("a1000000-0000-4000-8000-000000000011"), "ManualRecord", "pantoea_ananatis", "Mancha-branca", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Pantoea ananatis", null },
                    { new Guid("a1000000-0000-4000-8000-000000000012"), "ManualRecord", "cercospora_zeina", "Cercosporiose", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Cercospora zeina", null },
                    { new Guid("a1000000-0000-4000-8000-000000000013"), "ManualRecord", "exserohilum_turcicum", "Helmintosporiose", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Exserohilum turcicum", null },
                    { new Guid("a1000000-0000-4000-8000-000000000014"), "ManualRecord", "colletotrichum_graminicola", "Antracnose foliar", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Colletotrichum graminicola", null },
                    { new Guid("a1000000-0000-4000-8000-000000000015"), "ManualRecord", "bipolaris_maydis", "Mancha-de-bipolaris", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Bipolaris maydis", null },
                    { new Guid("a1000000-0000-4000-8000-000000000016"), "ManualRecord", "puccinia_polysora", "Ferrugem-polissora", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Puccinia polysora", null },
                    { new Guid("a1000000-0000-4000-8000-000000000017"), "ManualRecord", "puccinia_sorghi", "Ferrugem-comum", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Puccinia sorghi", null },
                    { new Guid("a1000000-0000-4000-8000-000000000018"), "ManualRecord", "physopella_zeae", "Ferrugem-tropical", new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Corn", "FoliarDisease", "Physopella zeae", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_monitoring_targets_code",
                table: "monitoring_targets",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monitoring_targets_crop_kind",
                table: "monitoring_targets",
                columns: new[] { "crop", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_protocol_items_protocol_id",
                table: "protocol_items",
                column: "protocol_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocol_items_target_id",
                table: "protocol_items",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocols_code_version",
                table: "protocols",
                columns: new[] { "code", "version" },
                unique: true);

            // O mesmo alvo nao se observa duas vezes no mesmo orgao dentro de uma versao.
            // NULLS NOT DISTINCT porque o orgao e nulo na contagem por armadilha: sem isso o
            // PostgreSQL trataria cada nulo como valor proprio e deixaria passar duas armadilhas
            // iguais para o mesmo alvo — justamente o caso que a checagem em memoria nao cobre sob
            // requisicoes concorrentes. SQL explicito: nao ha como declarar isso pelo modelo do EF,
            // entao esta restricao precisa ser preservada em alteracoes futuras do esquema.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ix_protocol_items_target_per_organ
                ON protocol_items (protocol_id, target_id, organ) NULLS NOT DISTINCT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "protocol_items");

            migrationBuilder.DropTable(
                name: "monitoring_targets");

            migrationBuilder.DropTable(
                name: "protocols");
        }
    }
}
