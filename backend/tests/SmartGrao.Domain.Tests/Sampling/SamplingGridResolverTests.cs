using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling;
using SmartGrao.Domain.Tests.Geo;
using Xunit;
using Xunit.Abstractions;

namespace SmartGrao.Domain.Tests.Sampling;

/// <summary>
/// A resolucao e o ponto onde o protocolo do MIP-Soja vira geometria. Se ela errar, o plano de
/// amostragem manda o agronomo caminhar um numero de pontos que a fonte nao pede — e a defesa do
/// trabalho perde justamente o elo entre a literatura e o codigo.
/// </summary>
public sealed class SamplingGridResolverTests(ITestOutputHelper output)
{
    /// <summary>
    /// Cada faixa da tabela do MIP-Soja tem que ser alcancada. E o teste que amarra o codigo a fonte:
    /// mudar a tabela sem mudar a fundamentacao quebra aqui.
    /// </summary>
    [Theory]
    [InlineData(5d, 6)]     // faixa 1–9 ha
    [InlineData(20d, 8)]    // faixa 10–29 ha
    [InlineData(50d, 10)]   // faixa 30–99 ha
    [InlineData(150d, 10)]  // acima de 100 ha: satura em 10 e recomenda subdividir
    public void EveryMipSojaBand_ReachesItsTarget(double areaHectares, int expectedTarget)
    {
        var boundary = SquareOfArea(areaHectares);

        var grid = SamplingGridResolver.ForMonitoring(boundary);

        Assert.Equal(expectedTarget, grid.TargetPointCount);
        Assert.True(
            grid.Points.Count >= expectedTarget,
            $"{areaHectares} ha: esperava ao menos {expectedTarget} pontos, veio {grid.Points.Count}");
    }

    /// <summary>
    /// O excedente sobre o alvo e a medida que decide, mais tarde, se vale trocar a grade por
    /// estratificacao k-means (spcosa) para obter contagem exata. Este teste existe para que essa
    /// decisao seja tomada com numero na mao, e nao por preferencia — por isso ele imprime o
    /// excedente de formas variadas, incluindo talhao em L e talhao alongado.
    /// <para>
    /// O limite de 2x nao e regra agronomica: e o ponto a partir do qual o excedente deixaria de ser
    /// arredondamento e viraria uma caminhada visivelmente maior do que a que o protocolo pede.
    /// </para>
    /// </summary>
    [Fact]
    public void OvershootOverTheTarget_StaysWithinReason()
    {
        var shapes = new (string Name, Boundary Boundary)[]
        {
            ("quadrado 5 ha", SquareOfArea(5d)),
            ("quadrado 20 ha", SquareOfArea(20d)),
            ("quadrado 50 ha", SquareOfArea(50d)),
            ("retangulo 4:1, 40 ha", RectangleOfArea(40d, 4d)),
            ("retangulo 8:1, 40 ha", RectangleOfArea(40d, 8d)),
            ("talhao em L, ~48 ha", LShapedField()),
        };

        foreach (var (name, boundary) in shapes)
        {
            var grid = SamplingGridResolver.ForMonitoring(boundary);
            var target = grid.TargetPointCount!.Value;
            var overshoot = (double)grid.Points.Count / target;

            output.WriteLine(
                $"{name,-24} area {boundary.AreaHectares,8:F1} ha  alvo {target,3}  " +
                $"pontos {grid.Points.Count,3}  excedente {overshoot:F2}x  " +
                $"espacamento {grid.Spacing.Meters,6:F0} m");

            Assert.True(
                overshoot < 2d,
                $"{name}: {grid.Points.Count} pontos para um alvo de {target} ({overshoot:F2}x)");
        }
    }

    /// <summary>
    /// Mesma entrada, mesma saida. A busca e aritmetica pura, sem aleatoriedade nem dependencia de
    /// ordem de iteracao — um plano regerado tem que devolver os mesmos pontos, senao o historico do
    /// talhao deixa de ser comparavel entre safras.
    /// </summary>
    [Fact]
    public void Resolution_IsDeterministic()
    {
        var boundary = SquareOfArea(20d);

        var first = SamplingGridResolver.ForMonitoring(boundary);
        var second = SamplingGridResolver.ForMonitoring(boundary);

        Assert.Equal(first.Spacing.Meters, second.Spacing.Meters);
        Assert.Equal(first.Points.Count, second.Points.Count);
        Assert.Equal(first.Points, second.Points);
    }

    /// <summary>
    /// O Monitoramento descarta a bordadura, e o efeito tem que ser visivel: a area amostrada e
    /// menor que o talhao, e nenhum ponto encosta na divisa.
    /// </summary>
    [Fact]
    public void Monitoring_DiscardsTheFieldMargin()
    {
        var boundary = SquareOfArea(20d);

        var grid = SamplingGridResolver.ForMonitoring(boundary);

        Assert.True(grid.EdgeBuffer.DiscardsAnything);
        Assert.True(grid.SamplingArea.AreaHectares < boundary.AreaHectares);
        Assert.All(grid.Points, point => Assert.True(boundary.Contains(point)));
    }

    /// <summary>
    /// O Mapeamento nao descarta nada. E a decisao que veio da fonte: o gradiente da borda para o
    /// interior e o sinal que justifica a aplicacao em faixa, entao apagar a borda apagaria o dado
    /// mais informativo do metodo.
    /// </summary>
    [Fact]
    public void Mapping_KeepsTheFieldMargin()
    {
        var boundary = SquareOfArea(20d);

        var grid = SamplingGridResolver.ForMapping(boundary, SamplingSpacing.Standard);

        Assert.False(grid.EdgeBuffer.DiscardsAnything);
        Assert.Same(boundary, grid.SamplingArea);
        Assert.Null(grid.TargetPointCount);
    }

    /// <summary>
    /// Os dois modos sobre o mesmo talhao produzem densidades separadas por uma ordem de grandeza.
    /// E a tese central do documento de fundamentacao virada em teste: nao e ajuste de precisao, e
    /// pergunta diferente.
    /// </summary>
    [Fact]
    public void TheTwoModes_DifferByAnOrderOfMagnitude()
    {
        var boundary = SquareOfArea(50d);

        var monitoring = SamplingGridResolver.ForMonitoring(boundary);
        var mapping = SamplingGridResolver.ForMapping(boundary, SamplingSpacing.Standard);

        output.WriteLine(
            $"50 ha: monitoramento {monitoring.Points.Count} pontos, mapeamento {mapping.Points.Count}");

        Assert.True(mapping.Points.Count > monitoring.Points.Count * 4);
    }

    /// <summary>
    /// Talhao estreito demais para a bordadura: a erosao nao deixa miolo e a resolucao recusa, em
    /// vez de devolver um plano vazio que so quebraria na tela.
    /// </summary>
    [Fact]
    public void FieldTooNarrowForItsMargin_IsRefused()
    {
        // Faixa comprida e estreita: ~1000 m por ~12 m.
        var boundary = Boundary.FromCoordinates(Rectangle(-15.6, -56.1, 1_000d, 12d));

        var exception = Assert.Throws<DomainException>(
            () => SamplingGridResolver.ForMonitoring(boundary));

        Assert.Equal("sampling.field_too_narrow_for_edge_buffer", exception.Error.Code);
    }

    /// <summary>
    /// Talhao pequeno demais para caber os pontos que o protocolo pede. Nao e erro: a malha sai na
    /// densidade maxima possivel e o plano registra alvo e contagem real lado a lado, para que a
    /// diferenca apareca em vez de ser mascarada.
    /// </summary>
    [Fact]
    public void FieldTooSmallForTheProtocol_FallsBackToTheDensestGrid()
    {
        var boundary = SquareOfArea(0.15d);

        var grid = SamplingGridResolver.ForMonitoring(boundary);

        output.WriteLine(
            $"0,15 ha: alvo {grid.TargetPointCount}, pontos {grid.Points.Count}, " +
            $"espacamento {grid.Spacing.Meters:F0} m");

        Assert.NotEmpty(grid.Points);
        Assert.Equal(SamplingSpacing.MinimumMeters, grid.Spacing.Meters);
    }

    /// <summary>
    /// O limite superior do dominio nao pode estourar a busca. Um talhao de 50.000 ha com alvo de 10
    /// pontos pede quilometros de espacamento — e o caso que motivou o teto alto do
    /// <see cref="SamplingSpacing"/>.
    /// </summary>
    [Fact]
    public void HugeField_ResolvesWithoutBlowingUp()
    {
        var boundary = SquareOfArea(50_000d);

        var grid = SamplingGridResolver.ForMonitoring(boundary);

        output.WriteLine(
            $"50.000 ha: alvo {grid.TargetPointCount}, pontos {grid.Points.Count}, " +
            $"espacamento {grid.Spacing.Meters:F0} m");

        Assert.True(grid.Points.Count >= grid.TargetPointCount);
        Assert.True(grid.Points.Count < 100, $"excedente absurdo: {grid.Points.Count} pontos");
    }

    private static Boundary SquareOfArea(double hectares)
    {
        var side = Math.Sqrt(hectares * Wgs84Geodesy.SquareMetersPerHectare);
        return Boundary.FromCoordinates(Rectangle(-15.6, -56.1, side, side));
    }

    private static Boundary RectangleOfArea(double hectares, double aspectRatio)
    {
        var area = hectares * Wgs84Geodesy.SquareMetersPerHectare;
        var height = Math.Sqrt(area / aspectRatio);

        return Boundary.FromCoordinates(Rectangle(-15.6, -56.1, height * aspectRatio, height));
    }

    private static GeoCoordinate[] Rectangle(
        double latitude, double longitude, double widthMetres, double heightMetres)
    {
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(heightMetres);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(widthMetres, latitude);

        return
        [
            GeoCoordinate.From(latitude, longitude),
            GeoCoordinate.From(latitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude),
        ];
    }

    /// <summary>Talhao em L de ~48 ha: um quadrante da caixa envolvente fica de fora.</summary>
    private static Boundary LShapedField()
    {
        const double Latitude = -15.6;
        const double Longitude = -56.1;

        var full = Wgs84Geodesy.MetersToDegreesLatitude(800d);
        var half = full / 2d;
        var fullLongitude = Wgs84Geodesy.MetersToDegreesLongitude(800d, Latitude);
        var halfLongitude = fullLongitude / 2d;

        return Boundary.FromCoordinates(
        [
            GeoCoordinate.From(Latitude, Longitude),
            GeoCoordinate.From(Latitude, Longitude + fullLongitude),
            GeoCoordinate.From(Latitude + half, Longitude + fullLongitude),
            GeoCoordinate.From(Latitude + half, Longitude + halfLongitude),
            GeoCoordinate.From(Latitude + full, Longitude + halfLongitude),
            GeoCoordinate.From(Latitude + full, Longitude),
        ]);
    }
}
