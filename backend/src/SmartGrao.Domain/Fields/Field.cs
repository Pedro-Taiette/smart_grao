using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields.Events;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Domain.Fields;

/// <summary>
/// O talhao: a unidade de manejo, o pedaco de terra que se planta, se amostra e se pulveriza como
/// um todo. E o agregado central do Pilar 1 e a ancora espacial de todo o resto do sistema.
/// </summary>
public sealed class Field : AggregateRoot<FieldId>
{
    public const int MaximumNameLength = 80;

    /// <summary>
    /// Abaixo disso o desenho e um clique acidental, nao um talhao. Meio hectare ja seria grande
    /// demais para um canteiro experimental, entao o corte fica em 0,1 ha (mil metros quadrados).
    /// </summary>
    public const decimal MinimumAreaHectares = 0.1m;

    /// <summary>
    /// Teto de sanidade: a maior fazenda do pais nao chega a isso, entao uma area assim significa
    /// contorno invertido, lat/lon trocadas ou um vertice arrastado para o outro hemisferio —
    /// falhas que so aparecem como um numero absurdo.
    /// </summary>
    public const decimal MaximumAreaHectares = 50_000m;

    private Boundary? _boundary;

    private Field()
    {
    }

    public FarmId FarmId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Crop Crop { get; private set; }

    /// <summary>
    /// O contorno, como o PostGIS o guarda: <c>geography(Polygon,4326)</c>.
    /// <para>
    /// Exposto como <see cref="Polygon"/> e nao como o objeto de valor porque e assim que o EF Core
    /// traduz <c>ST_Intersects</c> e <c>ST_Contains</c> para SQL. Envolve-lo num conversor de valor
    /// deixaria o predicado espacial intraduzivel e empurraria a checagem de sobreposicao — e
    /// depois a amostragem inteira do Pilar 2 — para a memoria da aplicacao. A validacao nao se
    /// perde: nada entra aqui sem passar por <see cref="Geo.Boundary"/>.
    /// </para>
    /// </summary>
    public Polygon Geometry { get; private set; } = null!;

    /// <summary>
    /// Area geodesica em hectares, gravada no momento em que o contorno e definido.
    /// <para>
    /// Denormalizada de proposito. E o numero que aparece em toda lista e todo relatorio, e
    /// recalcula-lo a cada leitura — ou pedi-lo ao <c>ST_Area</c> a cada linha — cobraria uma ida ao
    /// banco por talhao para reproduzir um valor que so muda quando o contorno muda.
    /// </para>
    /// </summary>
    public decimal AreaHectares { get; private set; }

    public double PerimeterMeters { get; private set; }

    public bool Active { get; private set; } = true;

    /// <summary>O contorno como objeto de valor, com as medidas derivadas. Nao e mapeado.</summary>
    public Boundary Boundary => _boundary ??= Boundary.FromPolygon(Geometry);

    public static Field Create(FarmId farmId, string name, Crop crop, Boundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        ValidateArea(boundary.AreaHectares);
        ValidateCrop(crop);

        var field = new Field
        {
            Id = FieldId.New(),
            FarmId = farmId,
            Name = ValidateName(name),
            Crop = crop,
        };

        field.ApplyBoundary(boundary);
        field.RaiseDomainEvent(new FieldCreatedEvent(field.Id, farmId, field.AreaHectares));

        return field;
    }

    public void Rename(string name)
    {
        Name = ValidateName(name);
        MarkAsUpdated();
    }

    public void ChangeCrop(Crop crop)
    {
        ValidateCrop(crop);
        Crop = crop;
        MarkAsUpdated();
    }

    /// <summary>
    /// Redesenha o contorno. A area e reescrita junto — as duas coisas nunca se separam, que e a
    /// razao de nao existir um setter publico para <see cref="AreaHectares"/>.
    /// </summary>
    public void Redraw(Boundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ValidateArea(boundary.AreaHectares);

        var previousArea = AreaHectares;
        ApplyBoundary(boundary);
        MarkAsUpdated();

        RaiseDomainEvent(new FieldRedrawnEvent(Id, previousArea, AreaHectares));
    }

    /// <summary>
    /// Tira o talhao de operacao sem apaga-lo. Um talhao que ja recebeu amostragens e diagnosticos
    /// carrega o historico da lavoura; excluir a linha levaria junto o registro de que a ferrugem
    /// apareceu ali em duas safras seguidas.
    /// </summary>
    public void Deactivate()
    {
        Active = false;
        MarkAsUpdated();
    }

    public void Reactivate()
    {
        Active = true;
        MarkAsUpdated();
    }

    private void ApplyBoundary(Boundary boundary)
    {
        _boundary = boundary;
        Geometry = boundary.Polygon;
        AreaHectares = boundary.AreaHectares;
        PerimeterMeters = boundary.PerimeterMeters;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(SmartGraoErrors.Field.NameRequired);

        var trimmed = name.Trim();

        if (trimmed.Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Field.NameTooLong);

        return trimmed;
    }

    private static void ValidateArea(decimal areaHectares)
    {
        if (areaHectares < MinimumAreaHectares)
            throw new DomainException(SmartGraoErrors.Field.AreaBelowMinimum(MinimumAreaHectares));

        if (areaHectares > MaximumAreaHectares)
            throw new DomainException(SmartGraoErrors.Field.AreaAboveMaximum(MaximumAreaHectares));
    }

    private static void ValidateCrop(Crop crop)
    {
        if (!Enum.IsDefined(crop))
            throw new DomainException(SmartGraoErrors.Field.UnknownCrop);
    }
}
