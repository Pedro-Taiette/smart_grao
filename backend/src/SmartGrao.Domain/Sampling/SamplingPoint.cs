using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// Um ponto da malha: onde o agronomo para, estende o pano-de-batida e — no Pilar 2 — tira a foto
/// que vai virar diagnostico.
/// <para>
/// Entidade filha, nao raiz de agregado. Um ponto nao existe sem o plano que o gerou, e o plano
/// inteiro so faz sentido como conjunto: metade da malha nao mede metade do talhao, mede coisa
/// nenhuma.
/// </para>
/// </summary>
public sealed class SamplingPoint : Entity<SamplingPointId>
{
    private SamplingPoint()
    {
    }

    public SamplingPlanId SamplingPlanId { get; private set; }

    /// <summary>
    /// Posicao na caminhada, comecando em 1. Nao e enfeite: a malha sai em serpentina justamente
    /// para que seguir a sequencia seja um caminho continuo pelo talhao.
    /// </summary>
    public int Sequence { get; private set; }

    /// <summary>
    /// A coordenada, como o PostGIS a guarda: <c>geography(Point,4326)</c>.
    /// <para>
    /// Mapeada direto, e nao pelo objeto de valor, pela mesma razao do contorno do talhao: e assim
    /// que o EF Core traduz os predicados espaciais para SQL. Na coleta em campo, "qual o ponto mais
    /// proximo de onde estou" precisa virar <c>ST_DWithin</c> no banco, e nao uma varredura de todos
    /// os pontos na memoria da aplicacao.
    /// </para>
    /// </summary>
    public Point Location { get; private set; } = null!;

    /// <summary>A coordenada como objeto de valor. Nao e mapeada.</summary>
    public GeoCoordinate Coordinate => GeoCoordinate.FromCoordinate(Location.Coordinate);

    internal static SamplingPoint Create(SamplingPlanId planId, int sequence, GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);

        return new SamplingPoint
        {
            Id = SamplingPointId.New(),
            SamplingPlanId = planId,
            Sequence = sequence,
            Location = coordinate.ToPoint(),
        };
    }
}
