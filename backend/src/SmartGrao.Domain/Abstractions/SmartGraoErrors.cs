using System.Globalization;

namespace SmartGrao.Domain.Abstractions;

/// <summary>
/// Catalogo unico de erros de dominio. Cada erro e declarado aqui uma vez — codigo estavel,
/// mensagem e <see cref="ErrorType"/> juntos — para que um ponto de chamada referencie um membro
/// em vez de repetir o par codigo/mensagem, que sempre acaba divergindo.
/// <para>
/// O <b>codigo</b> e o contrato com o frontend, que traduz por chave: snake_case, prefixado pela
/// area. A <b>mensagem</b> e o texto de desenvolvedor e nunca chega ao produtor.
/// </para>
/// </summary>
public static class SmartGraoErrors
{
    public static class Common
    {
        public static Error ValidationFailed(string message) =>
            Error.Validation("validation.failed", message);
    }

    public static class Geo
    {
        public static Error CoordinateNotFinite =>
            Error.Validation("geo.coordinate_not_finite", "A coordinate value is not a finite number.");

        public static Error LatitudeOutOfRange =>
            Error.Validation("geo.latitude_out_of_range", "Latitude must be between -90 and 90 degrees.");

        public static Error LongitudeOutOfRange =>
            Error.Validation("geo.longitude_out_of_range", "Longitude must be between -180 and 180 degrees.");

        public static Error LatitudeTooCloseToPole =>
            Error.Validation("geo.latitude_too_close_to_pole",
                "Metric conversions are not defined this close to a pole.");

        public static Error NegativeShrinkDistance =>
            Error.Validation("geo.negative_shrink_distance",
                "A boundary can only be shrunk inwards by a non-negative distance.");

        public static Error EmptyBoundary =>
            Error.Validation("geo.empty_boundary", "The boundary has no geometry.");

        public static Error InvalidPolygon =>
            Error.Validation("geo.invalid_polygon",
                "The polygon is not topologically valid (it probably self-intersects).");

        public static Error PolygonHasHoles =>
            Error.Validation("geo.polygon_has_holes", "A field boundary cannot contain interior rings.");

        public static Error InsufficientVertices =>
            Error.Validation("geo.insufficient_vertices", "A polygon needs at least three distinct vertices.");

        public static Error TooManyVertices =>
            Error.Validation("geo.too_many_vertices",
                "The polygon has more vertices than the maximum allowed.");

        public static Error UnsupportedGeoJson =>
            Error.Validation("geo.unsupported_geojson",
                "Only a GeoJSON geometry of type Polygon is accepted here.");

        public static Error MalformedGeoJson =>
            Error.Validation("geo.malformed_geojson",
                "The GeoJSON coordinates are malformed; each position must be [longitude, latitude].");
    }

    public static class Farm
    {
        public static Error NotFound =>
            Error.NotFound("farm.not_found", "Farm not found.");

        public static Error NameRequired =>
            Error.Validation("farm.name_required", "The farm needs a name.");

        public static Error NameTooLong =>
            Error.Validation("farm.name_too_long", "The farm name is longer than the maximum allowed.");

        public static Error CityRequired =>
            Error.Validation("farm.city_required", "The farm needs a municipality.");

        public static Error CityTooLong =>
            Error.Validation("farm.city_too_long", "The municipality name is longer than the maximum allowed.");

        public static Error InvalidState =>
            Error.Validation("farm.invalid_state", "The state code must be two letters.");

        public static Error HasFields =>
            Error.Conflict("farm.has_fields", "This farm still has fields; remove them before deleting it.");
    }

    public static class Field
    {
        public static Error NotFound =>
            Error.NotFound("field.not_found", "Field not found.");

        public static Error NameRequired =>
            Error.Validation("field.name_required", "The field needs a name.");

        public static Error NameTooLong =>
            Error.Validation("field.name_too_long", "The field name is longer than the maximum allowed.");

        public static Error DuplicateName =>
            Error.Conflict("field.duplicate_name", "This farm already has a field with that name.");

        public static Error AreaBelowMinimum(decimal minimumHectares) =>
            Error.BusinessRule(
                "field.area_below_minimum",
                string.Create(CultureInfo.InvariantCulture,
                    $"A field must have at least {minimumHectares} hectares."));

        public static Error AreaAboveMaximum(decimal maximumHectares) =>
            Error.BusinessRule(
                "field.area_above_maximum",
                string.Create(CultureInfo.InvariantCulture,
                    $"A field cannot exceed {maximumHectares} hectares; the outline was probably drawn wrong."));

        /// <summary>
        /// Dois talhoes da mesma fazenda dividindo area. Nao e detalhe cosmetico: a amostragem do
        /// Pilar 2 contaria o mesmo ponto duas vezes, e a recomendacao de aplicacao sairia dobrada
        /// na faixa sobreposta.
        /// </summary>
        public static Error OverlapsAnother =>
            Error.Conflict("field.overlaps_another",
                "This outline overlaps another field of the same farm.");

        public static Error UnknownCrop =>
            Error.Validation("field.unknown_crop", "That crop is not in the catalog.");
    }

    public static class Cultivation
    {
        public static Error ConcurrentChange => Error.Conflict("cultivation.concurrent_change", "The cultivation changed during this operation. Reload and try again.");
        public static Error FarmHasSeasons => Error.Conflict("cultivation.farm_has_seasons", "This farm has seasons and cannot be deleted.");
        public static Error SeasonNotFound => Error.NotFound("cultivation.season_not_found", "Season not found.");
        public static Error InvalidSeasonName => Error.Validation("cultivation.invalid_season_name", "A season name of up to 80 characters is required.");
        public static Error DuplicateSeason => Error.Conflict("cultivation.duplicate_season", "This farm already has that season.");
        public static Error NotFound => Error.NotFound("cultivation.not_found", "Cultivation not found.");
        public static Error WrongFarm => Error.Validation("cultivation.wrong_farm", "The season and field must belong to the same farm.");
        public static Error InvalidCultivar => Error.Validation("cultivation.invalid_cultivar", "A cultivar of up to 120 characters is required.");
        public static Error InvalidDate => Error.Validation("cultivation.invalid_date", "Dates must fall within the crop cycle and preserve recorded history.");
        public static Error InvalidStage => Error.Validation("cultivation.invalid_stage", "A stage of up to 32 characters and notes of up to 1000 characters are accepted.");

        /// <summary>
        /// Milho e soja tem escala fenologica transcrita, e fora dela o codigo nao diz nada: dois
        /// ciclos so se comparam — e um modelo so se alimenta — quando o mesmo estadio chega
        /// sempre com o mesmo codigo.
        /// </summary>
        public static Error StageNotInScale => Error.Validation("cultivation.stage_not_in_scale", "That stage code is not in the phenological scale of this crop.");
        public static Error DuplicateStageDate => Error.Conflict("cultivation.duplicate_stage_date", "A stage has already been recorded on this date.");
        public static Error OverlappingCycle => Error.Conflict("cultivation.overlapping_cycle", "Crop cycles in a field cannot overlap. Close the current cycle first.");
        public static Error Closed => Error.Conflict("cultivation.closed", "This cultivation is closed.");
        public static Error HasHistory => Error.Conflict("cultivation.has_history", "This field has cultivation or sampling history; deactivate it instead.");
        public static Error WrongField => Error.Validation("cultivation.wrong_field", "The cultivation does not belong to this field.");
        public static Error UnsupportedMonitoring => Error.Validation("cultivation.unsupported_monitoring", "MIP-Soja monitoring is only available for soybean cultivations.");
    }

    public static class Protocol
    {
        public static Error TargetNotFound => Error.NotFound("protocol.target_not_found", "Monitoring target not found.");
        public static Error InvalidTargetCode => Error.Validation("protocol.invalid_target_code", "A target code uses lowercase letters, digits and inner underscores, up to 60 characters.");
        public static Error InvalidTargetName => Error.Validation("protocol.invalid_target_name", "A common and a scientific name of up to 120 characters are required.");
        public static Error UnknownTargetKind => Error.Validation("protocol.unknown_target_kind", "A target is either a pest or a foliar disease.");
        public static Error DuplicateTargetCode => Error.Conflict("protocol.duplicate_target_code", "That target code is already in the catalog.");
        public static Error UnknownAutomation => Error.Validation("protocol.unknown_automation", "That automation capability is not in the catalog.");

        /// <summary>
        /// A escada da automacao existe para que o catalogo amplo nao vire promessa de diagnostico.
        /// Pular a revisao humana anularia exatamente essa distincao.
        /// </summary>
        public static Error AutomationSkipsValidation =>
            Error.BusinessRule("protocol.automation_skips_validation",
                "A target must go through human-reviewed validation before automation is enabled.");

        public static Error ConcurrentChange => Error.Conflict("protocol.concurrent_change", "The protocol changed during this operation. Reload and try again.");
        public static Error NotFound => Error.NotFound("protocol.not_found", "Protocol not found.");
        public static Error InvalidCode => Error.Validation("protocol.invalid_code", "A protocol code uses lowercase letters, digits and inner underscores, up to 60 characters.");
        public static Error InvalidName => Error.Validation("protocol.invalid_name", "A protocol name of up to 120 characters is required.");
        public static Error DuplicateVersion => Error.Conflict("protocol.duplicate_version", "That protocol version already exists.");
        public static Error NotDraft => Error.Conflict("protocol.not_draft", "A published protocol is immutable; open the next version instead.");
        public static Error NotPublished => Error.Conflict("protocol.not_published", "This protocol is not published.");
        public static Error EmptyProtocol => Error.BusinessRule("protocol.empty", "A protocol needs at least one target before it can be published.");

        /// <summary>
        /// O criterio da fase 2: uma vistoria de milho nao pode herdar o MIP-Soja por omissao.
        /// A barreira fica no modelo, e nao numa tela que alguem lembre de conferir.
        /// </summary>
        public static Error TargetFromAnotherCrop =>
            Error.Validation("protocol.target_from_another_crop", "This target belongs to another crop.");

        public static Error DuplicateItem => Error.Conflict("protocol.duplicate_item", "This protocol already observes that target on that organ.");
        public static Error ItemNotFound => Error.NotFound("protocol.item_not_found", "Protocol item not found.");
        public static Error UnknownUnit => Error.Validation("protocol.unknown_unit", "That counting unit is not in the catalog.");
        public static Error OrganRequired => Error.Validation("protocol.organ_required", "This counting unit needs the plant organ being observed.");
        public static Error OrganNotApplicable => Error.Validation("protocol.organ_not_applicable", "A trap count does not observe a plant organ.");
        public static Error ReferenceLevelWithoutSource => Error.Validation("protocol.reference_level_without_source", "A reference level needs a source of up to 300 characters.");
        public static Error ReferenceLevelNotApplicable => Error.Validation("protocol.reference_level_not_applicable", "A presence record has no reference level.");
        public static Error ReferenceLevelOutOfRange => Error.Validation("protocol.reference_level_out_of_range", "The reference level falls outside the range of its counting unit.");
        public static Error InvalidInstructions => Error.Validation("protocol.invalid_instructions", "Instructions of up to 2000 characters are required.");

        public static Error TooManyPhotos(int maximum) =>
            Error.Validation("protocol.too_many_photos",
                string.Create(CultureInfo.InvariantCulture,
                    $"Between 0 and {maximum} photos can be requested for a target."));
    }

    public static class Inspection
    {
        // ── Pessoa ───────────────────────────────────────────────────────────
        public static Error PersonNotFound => Error.NotFound("inspection.person_not_found", "Person not found.");
        public static Error InvalidPersonName => Error.Validation("inspection.invalid_person_name", "A name of up to 120 characters is required.");
        public static Error UnknownRole => Error.Validation("inspection.unknown_role", "That role is not in the catalog.");
        public static Error PersonInactive => Error.Conflict("inspection.person_inactive", "This person is no longer on the team.");
        public static Error PersonFromAnotherFarm => Error.Validation("inspection.person_from_another_farm", "This person belongs to another farm.");
        public static Error PersonHasHistory => Error.Conflict("inspection.person_has_history", "This person has inspection history; deactivate instead of deleting.");

        // ── Agendamento ──────────────────────────────────────────────────────
        public static Error NotFound => Error.NotFound("inspection.not_found", "Inspection not found.");
        public static Error ConcurrentChange => Error.Conflict("inspection.concurrent_change", "The inspection changed during this operation. Reload and try again.");
        public static Error InvalidDate => Error.Validation("inspection.invalid_date", "The scheduled date must fall within the crop cycle.");
        public static Error PlanFromAnotherCultivation => Error.Validation("inspection.plan_from_another_cultivation", "The sampling plan does not belong to this cultivation.");
        public static Error ProtocolNotPublished => Error.Conflict("inspection.protocol_not_published", "Only a published protocol version can be used in the field.");
        public static Error ProtocolFromAnotherCrop => Error.Validation("inspection.protocol_from_another_crop", "The protocol belongs to another crop.");

        // ── Execucao ─────────────────────────────────────────────────────────
        public static Error NotScheduled => Error.Conflict("inspection.not_scheduled", "This inspection already started; it cannot be rescheduled.");
        public static Error NotInProgress => Error.Conflict("inspection.not_in_progress", "This inspection is not in progress.");
        public static Error AlreadyFinished => Error.Conflict("inspection.already_finished", "This inspection is already finished.");
        public static Error InvalidTiming => Error.Validation("inspection.invalid_timing", "The completion time cannot precede the start.");

        /// <summary>
        /// Vistoria sem nenhuma parada registrada nao e vistoria concluida: e vistoria que nao
        /// aconteceu, e para isso existe o cancelamento com motivo.
        /// </summary>
        public static Error NoObservations => Error.BusinessRule("inspection.no_observations", "Record at least one observation before completing the inspection.");

        public static Error CancellationNeedsReason => Error.Validation("inspection.cancellation_needs_reason", "A reason of up to 500 characters is required to cancel.");

        // ── Observacao ───────────────────────────────────────────────────────
        public static Error ObservationNotFound => Error.NotFound("inspection.observation_not_found", "Observation not found.");
        public static Error PointFromAnotherPlan => Error.Validation("inspection.point_from_another_plan", "That point does not belong to this inspection's sampling plan.");
        public static Error DuplicatePointObservation => Error.Conflict("inspection.duplicate_point_observation", "This point has already been recorded in this inspection.");
        public static Error InvalidAccuracy => Error.Validation("inspection.invalid_accuracy", "The GPS accuracy must be a non-negative number of metres.");
        public static Error InvalidNotes => Error.Validation("inspection.invalid_notes", "Notes of up to 1000 characters are accepted.");

        // ── Contagem ─────────────────────────────────────────────────────────
        public static Error UnknownCountTarget => Error.Validation("inspection.unknown_count_target", "Every count must point at a target of the protocol.");
        public static Error CountFromAnotherProtocol => Error.Validation("inspection.count_from_another_protocol", "That target is not in this inspection's protocol version.");
        public static Error DuplicateCount => Error.Conflict("inspection.duplicate_count", "That target was counted twice in the same observation.");
        public static Error PresenceTakesNoValue => Error.Validation("inspection.presence_takes_no_value", "A presence record carries no count.");
        public static Error CountValueRequired => Error.Validation("inspection.count_value_required", "This counting unit needs a value.");
        public static Error CountValueOutOfRange => Error.Validation("inspection.count_value_out_of_range", "The value falls outside the range of its counting unit.");
    }

    public static class Sampling
    {
        public static Error NotFound =>
            Error.NotFound("sampling.not_found", "Sampling plan not found.");

        /// <summary>
        /// O talhao saiu de operacao. Gerar uma caminhada para ele mandaria alguem a campo por uma
        /// area que ninguem esta manejando.
        /// </summary>
        public static Error FieldIsInactive =>
            Error.Conflict("sampling.field_is_inactive",
                "This field is inactive; reactivate it before planning a sampling round.");

        public static Error UnknownMode =>
            Error.Validation("sampling.unknown_mode", "That sampling mode is not in the catalog.");

        public static Error EdgeBufferNotFinite =>
            Error.Validation("sampling.edge_buffer_not_finite", "The edge buffer is not a finite number.");

        public static Error EdgeBufferNegative =>
            Error.Validation("sampling.edge_buffer_negative", "The edge buffer cannot be negative.");

        public static Error SpacingNotFinite =>
            Error.Validation("sampling.spacing_not_finite", "The spacing is not a finite number.");

        public static Error SpacingOutOfRange(double minimumMeters, double maximumMeters) =>
            Error.Validation(
                "sampling.spacing_out_of_range",
                string.Create(CultureInfo.InvariantCulture,
                    $"The spacing must be between {minimumMeters} and {maximumMeters} metres."));

        public static Error GridTooDense(int maximumPoints) =>
            Error.BusinessRule(
                "sampling.grid_too_dense",
                string.Create(CultureInfo.InvariantCulture,
                    $"That spacing would produce more than {maximumPoints} points for this field."));

        /// <summary>
        /// A bordadura descartada pelo modo Monitoramento consumiu o talhao inteiro — ou o partiu em
        /// pedacos soltos, no caso de um contorno em ampulheta. Nao ha miolo onde amostrar.
        /// </summary>
        public static Error FieldTooNarrowForEdgeBuffer =>
            Error.BusinessRule("sampling.field_too_narrow_for_edge_buffer",
                "Discarding the field margin leaves no area to sample; the field is too narrow.");

        /// <summary>
        /// O contorno nao acomodou nenhum ponto da malha. Acontece com talhao muito estreito e
        /// espacamento grande: a grade existe, mas todos os candidatos caem fora do poligono.
        /// </summary>
        public static Error NoPointsFitTheField =>
            Error.BusinessRule("sampling.no_points_fit_the_field",
                "No grid point falls inside this field at the requested spacing.");
    }
}
