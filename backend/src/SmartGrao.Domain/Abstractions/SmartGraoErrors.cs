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
}
