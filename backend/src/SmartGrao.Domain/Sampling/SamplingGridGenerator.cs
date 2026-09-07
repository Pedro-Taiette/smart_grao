using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// Gera a malha de pontos de amostragem: grade regular sobre a caixa envolvente do talhao, recortada
/// pelo contorno.
/// <para>
/// E o nucleo compartilhado pelos dois modos, e o unico lugar onde a malha nasce. Nao sabe nada de
/// bordadura nem de contagem alvo: recebe o contorno <b>ja recuado</b> e o espacamento <b>ja
/// resolvido</b>, e devolve os pontos. As duas derivacoes de densidade entram por cima disso.
/// </para>
/// <para>
/// A grade uniforme nao e conveniencia de implementacao. A fonte da Embrapa registra como falha
/// documentada de campo justamente o oposto — pontos agrupados ao longo de um carreador, que medem
/// um lugar nao representativo. Distribuicao uniforme e a resposta a essa falha.
/// </para>
/// <para>
/// Os pontos ficam no <b>centro</b> de cada celula da grade, nao nos cantos. Duas razoes. A
/// primeira e que cada ponto passa a representar literalmente a celula em volta dele, que e o mesmo
/// raciocinio que dimensiona a bordadura em meio espacamento. A segunda e pratica: ancorados nos
/// cantos, os pontos da fileira externa caem <i>exatamente sobre</i> a divisa de um talhao
/// retangular, e <c>Contains</c> exclui a fronteira — o talhao perderia o anel inteiro de pontos,
/// justo onde o modo Mapeamento mais precisa deles.
/// </para>
/// </summary>
public static class SamplingGridGenerator
{
    /// <summary>
    /// Teto de sanidade para o tamanho da grade. Sem ele, um talhao grande com espacamento pequeno
    /// pediria milhoes de testes de contencao e prenderia a requisicao. Dez mil pontos ja e uma
    /// caminhada que ninguem faz — o limite protege o servidor, nao a agronomia.
    /// </summary>
    public const int MaximumPoints = 10_000;

    /// <summary>
    /// Pontos da malha, na ordem em que se caminha o talhao.
    /// <para>
    /// A ordem e em serpentina: a primeira fileira e percorrida da esquerda para a direita, a
    /// seguinte no sentido contrario, e assim por diante. Custa inverter uma lista e transforma a
    /// sequencia num caminho continuo — na ordem ingenua, o fim de cada fileira saltaria de volta
    /// para o outro extremo do talhao a cada linha.
    /// </para>
    /// </summary>
    public static IReadOnlyList<GeoCoordinate> Generate(Boundary boundary, SamplingSpacing spacing)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(spacing);

        var box = boundary.BoundingBox;
        var referenceLatitude = (box.MinY + box.MaxY) / 2d;

        var stepLatitude = Wgs84Geodesy.MetersToDegreesLatitude(spacing.Meters);
        var stepLongitude = Wgs84Geodesy.MetersToDegreesLongitude(spacing.Meters, referenceLatitude);

        var rows = CellsAcross(box.Height, stepLatitude);
        var columns = CellsAcross(box.Width, stepLongitude);

        if ((long)rows * columns > MaximumPoints)
            throw new DomainException(SmartGraoErrors.Sampling.GridTooDense(MaximumPoints));

        // Sobra da divisao repartida entre as duas pontas: sem isso a grade encosta numa borda da
        // caixa e deixa uma faixa vazia na oposta, o que apareceria como um talhao amostrado torto.
        var originLatitude = box.MinY + ((box.Height - (rows * stepLatitude)) / 2d);
        var originLongitude = box.MinX + ((box.Width - (columns * stepLongitude)) / 2d);

        var points = new List<GeoCoordinate>();

        for (var row = 0; row < rows; row++)
        {
            var latitude = originLatitude + ((row + 0.5) * stepLatitude);
            var rowPoints = new List<GeoCoordinate>();

            for (var column = 0; column < columns; column++)
            {
                var longitude = originLongitude + ((column + 0.5) * stepLongitude);
                var candidate = GeoCoordinate.From(latitude, longitude);

                if (boundary.Contains(candidate))
                    rowPoints.Add(candidate);
            }

            if (row % 2 == 1)
                rowPoints.Reverse();

            points.AddRange(rowPoints);
        }

        return points;
    }

    /// <summary>
    /// Quantas celulas de lado <paramref name="step"/> cabem no vao. Nunca menos de uma: um talhao
    /// menor que o espacamento pedido deve render o seu ponto central, e nao uma lista vazia.
    /// </summary>
    private static int CellsAcross(double span, double step) =>
        Math.Max(1, (int)Math.Round(span / step, MidpointRounding.AwayFromZero));
}
