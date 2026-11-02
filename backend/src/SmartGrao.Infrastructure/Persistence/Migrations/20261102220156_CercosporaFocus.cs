using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CercosporaFocus : Migration
    {
        private const string ProtocolId = "b1000000-0000-4000-8000-000000000001";
        private const string LowerLeavesItemId = "b1000000-0000-4000-8000-000000000101";
        private const string EarLeafItemId = "b1000000-0000-4000-8000-000000000102";
        private const string CercosporaTargetId = "a1000000-0000-4000-8000-000000000012";

        private const string LowerLeavesInstructions =
            "Em cada parada, examine 10 plantas seguidas. Em cada planta, comece pelas folhas de baixo e suba "
            + "até a folha logo abaixo da espiga. Conte a planta como atacada se tiver ao menos uma lesão: pontos "
            + "ou manchas pequenas, de 1 a 3 mm, com halo amarelo visível contra a luz, ou manchas retangulares "
            + "cinza a marrom limitadas pelas nervuras. Registre o percentual (3 de 10 plantas = 30%). Lesão "
            + "pequena ainda não é diagnóstico: fotografe a folha inteira e um close da lesão contra o sol.";

        private const string EarLeafInstructions =
            "Nas mesmas 10 plantas, olhe a folha da espiga. Conte a planta como atacada se a lesão já chegou "
            + "a essa folha e registre o percentual. Fotografe a folha da espiga com a lesão.";
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "risk_crop_sequence",
                table: "cultivations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "risk_hybrid_reaction",
                table: "cultivations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "risk_tillage",
                table: "cultivations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.Sql($$"""
                INSERT INTO protocols (id, code, version, crop, name, status, created_at)
                VALUES ('{{ProtocolId}}', 'cercosporiose_deteccao', 1, 'Corn',
                        'Cercosporiose — detecção precoce', 'Published', '2026-11-02T00:00:00Z')
                ON CONFLICT DO NOTHING;

                INSERT INTO protocol_items (id, protocol_id, target_id, organ, unit, photos_requested, instructions)
                SELECT item.id, item.protocol_id, item.target_id, item.organ, item.unit, item.photos, item.instructions
                FROM (VALUES
                    ('{{LowerLeavesItemId}}'::uuid, '{{ProtocolId}}'::uuid, '{{CercosporaTargetId}}'::uuid,
                     'LowerLeaves', 'AttackedPlantPercentage', 2, '{{LowerLeavesInstructions}}'),
                    ('{{EarLeafItemId}}'::uuid, '{{ProtocolId}}'::uuid, '{{CercosporaTargetId}}'::uuid,
                     'EarLeaf', 'AttackedPlantPercentage', 1, '{{EarLeafInstructions}}')
                ) AS item(id, protocol_id, target_id, organ, unit, photos, instructions)
                WHERE EXISTS (SELECT 1 FROM protocols WHERE id = '{{ProtocolId}}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($$"""
                DELETE FROM protocol_items WHERE protocol_id = '{{ProtocolId}}'
                    AND NOT EXISTS (SELECT 1 FROM inspections WHERE protocol_id = '{{ProtocolId}}');
                DELETE FROM protocols WHERE id = '{{ProtocolId}}'
                    AND NOT EXISTS (SELECT 1 FROM inspections WHERE protocol_id = '{{ProtocolId}}');
                """);

            migrationBuilder.DropColumn(
                name: "risk_crop_sequence",
                table: "cultivations");

            migrationBuilder.DropColumn(
                name: "risk_hybrid_reaction",
                table: "cultivations");

            migrationBuilder.DropColumn(
                name: "risk_tillage",
                table: "cultivations");
        }
    }
}
