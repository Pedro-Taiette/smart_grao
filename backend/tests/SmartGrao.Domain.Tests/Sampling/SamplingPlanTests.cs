using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling;
using SmartGrao.Domain.Sampling.Events;
using Xunit;

namespace SmartGrao.Domain.Tests.Sampling;

/// <summary>
/// O agregado e o documento fechado do que foi decidido: quantos pontos, com que espacamento, com
/// que bordadura e sobre que talhao. E o que a defesa do trabalho vai abrir para mostrar que a regra
/// implementada e a regra da fonte.
/// </summary>
public sealed class SamplingPlanTests
{
    [Fact]
    public void MonitoringPlan_RecordsHowItWasDerived()
    {
        var boundary = SquareOfArea(20d);

        var plan = SamplingPlan.ForMonitoring(FieldId.New(), boundary);

        Assert.Equal(SamplingMode.Monitoring, plan.Mode);
        Assert.Equal(8, plan.TargetPointCount);
        Assert.True(plan.PointCount >= 8);
        Assert.True(plan.SpacingMeters > 0);
        Assert.True(plan.EdgeBufferMeters > 0);
        Assert.False(plan.SubdivisionRecommended);
    }

    /// <summary>
    /// No Mapeamento nao existe contagem alvo: a pergunta e onde aplicar, e a resposta e a
    /// distribuicao, nao um numero de paradas.
    /// </summary>
    [Fact]
    public void MappingPlan_HasNoTargetAndDiscardsNoMargin()
    {
        var boundary = SquareOfArea(20d);

        var plan = SamplingPlan.ForMapping(FieldId.New(), boundary, SamplingSpacing.Standard);

        Assert.Equal(SamplingMode.Mapping, plan.Mode);
        Assert.Null(plan.TargetPointCount);
        Assert.Equal(0d, plan.EdgeBufferMeters);
        Assert.Equal(100d, plan.SpacingMeters);
        Assert.False(plan.FallsShortOfTarget);
    }

    /// <summary>
    /// A sequencia e continua e comeca em 1: e o roteiro da caminhada, e um furo nela seria um ponto
    /// que o agronomo procura e nao encontra.
    /// </summary>
    [Fact]
    public void Points_AreSequentiallyNumberedFromOne()
    {
        var plan = SamplingPlan.ForMonitoring(FieldId.New(), SquareOfArea(20d));

        Assert.Equal(
            Enumerable.Range(1, plan.PointCount),
            plan.Points.Select(point => point.Sequence));
    }

    /// <summary>
    /// Cada ponto guarda a geometria que o PostGIS vai receber, com o SRID certo. Sem o 4326 a
    /// insercao na coluna geography e recusada.
    /// </summary>
    [Fact]
    public void Points_CarryGeographyReadyGeometry()
    {
        var plan = SamplingPlan.ForMonitoring(FieldId.New(), SquareOfArea(20d));

        Assert.All(plan.Points, point =>
        {
            Assert.Equal(Srid.Wgs84, point.Location.SRID);
            Assert.Equal(point.Location.Y, point.Coordinate.Latitude);
            Assert.Equal(point.Location.X, point.Coordinate.Longitude);
        });
    }

    /// <summary>
    /// A area do talhao e congelada no plano. Depois de um redesenho do contorno, e ela que permite
    /// perceber que a malha ficou defasada sem carregar o talhao junto.
    /// </summary>
    [Fact]
    public void Plan_SnapshotsTheFieldAreaAtGenerationTime()
    {
        var boundary = SquareOfArea(20d);

        var plan = SamplingPlan.ForMonitoring(FieldId.New(), boundary);

        Assert.Equal(boundary.AreaHectares, plan.FieldAreaHectares);
    }

    /// <summary>
    /// Acima de 100 ha o protocolo manda subdividir em vez de amostrar mais. O plano ainda e gerado,
    /// mas carrega a marca — e ela e dado estruturado, nao texto, para que a evolucao para uma
    /// sugestao de subdivisao de verdade nao quebre o contrato.
    /// </summary>
    [Fact]
    public void LargeField_IsFlaggedForSubdivision()
    {
        var plan = SamplingPlan.ForMonitoring(FieldId.New(), SquareOfArea(150d));

        Assert.True(plan.SubdivisionRecommended);
        Assert.Equal(10, plan.TargetPointCount);
    }

    /// <summary>O Mapeamento nao opina sobre subdivisao: a regra e da tabela do MIP-Soja.</summary>
    [Fact]
    public void MappingPlan_NeverFlagsSubdivision()
    {
        var plan = SamplingPlan.ForMapping(FieldId.New(), SquareOfArea(150d), SamplingSpacing.Standard);

        Assert.False(plan.SubdivisionRecommended);
    }

    /// <summary>
    /// Talhao pequeno demais para o protocolo: o plano nasce mesmo assim, e a diferenca entre alvo e
    /// contagem fica explicita em vez de mascarada.
    /// </summary>
    [Fact]
    public void PlanOnATinyField_AdmitsItFallsShort()
    {
        var plan = SamplingPlan.ForMonitoring(FieldId.New(), SquareOfArea(0.15d));

        Assert.True(plan.FallsShortOfTarget);
        Assert.True(plan.PointCount < plan.TargetPointCount);
        Assert.NotEmpty(plan.Points);
    }

    [Fact]
    public void GeneratingAPlan_RaisesTheDomainEvent()
    {
        var fieldId = FieldId.New();

        var plan = SamplingPlan.ForMonitoring(fieldId, SquareOfArea(20d));

        var raised = Assert.Single(plan.DomainEvents);
        var generated = Assert.IsType<SamplingPlanGeneratedEvent>(raised);

        Assert.Equal(plan.Id, generated.SamplingPlanId);
        Assert.Equal(fieldId, generated.FieldId);
        Assert.Equal(SamplingMode.Monitoring, generated.Mode);
        Assert.Equal(plan.PointCount, generated.PointCount);
    }

    /// <summary>Todo ponto pertence ao plano que o gerou — a chave estrangeira nasce preenchida.</summary>
    [Fact]
    public void EveryPoint_BelongsToItsPlan()
    {
        var plan = SamplingPlan.ForMonitoring(FieldId.New(), SquareOfArea(20d));

        Assert.All(plan.Points, point => Assert.Equal(plan.Id, point.SamplingPlanId));
    }

    private static Boundary SquareOfArea(double hectares)
    {
        const double Latitude = -15.6;
        const double Longitude = -56.1;

        var side = Math.Sqrt(hectares * Wgs84Geodesy.SquareMetersPerHectare);
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(side);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(side, Latitude);

        return Boundary.FromCoordinates(
        [
            GeoCoordinate.From(Latitude, Longitude),
            GeoCoordinate.From(Latitude, Longitude + deltaLongitude),
            GeoCoordinate.From(Latitude + deltaLatitude, Longitude + deltaLongitude),
            GeoCoordinate.From(Latitude + deltaLatitude, Longitude),
        ]);
    }
}
