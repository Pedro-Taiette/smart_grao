using SmartGrao.Application.Common;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

public sealed record SaveFarmViewModel(
    string Name,
    string City,
    string State,
    GeoCoordinateViewModel? Headquarters);

public sealed record FarmViewModel(
    Guid Id,
    string Name,
    string City,
    string State,
    GeoCoordinateViewModel? Headquarters,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static FarmViewModel From(Farm farm)
    {
        ArgumentNullException.ThrowIfNull(farm);

        var headquarters = farm.HeadquartersCoordinate;

        return new FarmViewModel(
            farm.Id.Value,
            farm.Name,
            farm.City,
            farm.State,
            headquarters is null ? null : GeoCoordinateViewModel.From(headquarters),
            farm.CreatedAt,
            farm.UpdatedAt);
    }
}
