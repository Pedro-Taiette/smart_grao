using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Domain.Farms;

/// <summary>
/// A fazenda: o guarda-chuva sob o qual os talhoes existem.
/// <para>
/// Os talhoes <b>nao</b> ficam dentro deste agregado. Uma fazenda tem dezenas deles, cada um
/// editado isoladamente no mapa, e carregar a colecao inteira para renomear um so seria pagar caro
/// por uma invariante que nenhuma regra exige. O talhao referencia a fazenda por id.
/// </para>
/// </summary>
public sealed class Farm : AggregateRoot<FarmId>
{
    public const int MaximumNameLength = 120;
    public const int MaximumCityLength = 120;

    private Farm()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Municipio onde a sede esta registrada.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Sigla da unidade federativa, em maiusculas (ex.: "MT").</summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>
    /// Sede da fazenda, opcional. Serve para centralizar o mapa antes de existir qualquer talhao —
    /// sem ela a primeira tela abre sobre o oceano e o produtor precisa navegar ate a propria terra
    /// para comecar a desenhar.
    /// </summary>
    public Point? Headquarters { get; private set; }

    public GeoCoordinate? HeadquartersCoordinate =>
        Headquarters is null ? null : GeoCoordinate.FromCoordinate(Headquarters.Coordinate);

    public static Farm Create(string name, string city, string state, GeoCoordinate? headquarters = null) =>
        new()
        {
            Id = FarmId.New(),
            Name = ValidateName(name),
            City = ValidateCity(city),
            State = ValidateState(state),
            Headquarters = headquarters?.ToPoint(),
        };

    public void Update(string name, string city, string state)
    {
        Name = ValidateName(name);
        City = ValidateCity(city);
        State = ValidateState(state);
        MarkAsUpdated();
    }

    public void SetHeadquarters(GeoCoordinate? headquarters)
    {
        Headquarters = headquarters?.ToPoint();
        MarkAsUpdated();
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(SmartGraoErrors.Farm.NameRequired);

        var trimmed = name.Trim();

        if (trimmed.Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Farm.NameTooLong);

        return trimmed;
    }

    private static string ValidateCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException(SmartGraoErrors.Farm.CityRequired);

        var trimmed = city.Trim();

        if (trimmed.Length > MaximumCityLength)
            throw new DomainException(SmartGraoErrors.Farm.CityTooLong);

        return trimmed;
    }

    private static string ValidateState(string state)
    {
        if (string.IsNullOrWhiteSpace(state) || state.Trim().Length != 2)
            throw new DomainException(SmartGraoErrors.Farm.InvalidState);

        return state.Trim().ToUpperInvariant();
    }
}
