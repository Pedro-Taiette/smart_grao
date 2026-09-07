using SmartGrao.Application.Common;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed record CreateFieldViewModel(
    Guid FarmId,
    string Name,
    Crop Crop,
    GeoJsonPolygon Boundary);

/// <summary>
/// Atualizacao do talhao. O contorno vem junto porque, na tela, renomear e arrastar um vertice sao
/// a mesma acao de salvar: separar em dois endpoints exigiria do frontend saber o que mudou.
/// </summary>
public sealed record UpdateFieldViewModel(
    string Name,
    Crop Crop,
    GeoJsonPolygon Boundary);

public sealed record FieldViewModel(
    Guid Id,
    Guid FarmId,
    string Name,
    Crop Crop,
    decimal AreaHectares,
    double PerimeterMeters,
    GeoCoordinateViewModel Center,
    int VertexCount,
    bool Active,
    GeoJsonPolygon Boundary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static FieldViewModel From(Field field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var boundary = field.Boundary;

        return new FieldViewModel(
            field.Id.Value,
            field.FarmId.Value,
            field.Name,
            field.Crop,
            field.AreaHectares,
            Math.Round(field.PerimeterMeters, 2),
            GeoCoordinateViewModel.From(boundary.Center),
            boundary.VertexCount,
            field.Active,
            GeoJsonPolygon.From(boundary),
            field.CreatedAt,
            field.UpdatedAt);
    }
}
