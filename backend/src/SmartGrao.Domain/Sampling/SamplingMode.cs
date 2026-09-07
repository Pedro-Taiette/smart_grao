namespace SmartGrao.Domain.Sampling;

/// <summary>
/// A pergunta que a amostragem responde — e nao um ajuste de precisao.
/// <para>
/// A literatura trata as duas perguntas com densidades separadas por um fator de 10, e e dai que
/// nasce o modo: nao existe "densidade certa" em abstrato, existe densidade certa para a decisao
/// que se quer tomar. Ver <c>docs/amostragem.md</c>.
/// </para>
/// </summary>
public enum SamplingMode
{
    /// <summary>
    /// Decidir <b>se</b> aplica. Poucos pontos bem distribuidos, alinhados a tabela do MIP-Soja,
    /// para estimar a media do talhao contra o nivel de controle. Descarta a bordadura, porque a
    /// populacao concentrada na borda contamina essa media.
    /// </summary>
    Monitoring,

    /// <summary>
    /// Decidir <b>onde</b> aplica. Malha uma ordem de grandeza mais densa, para caracterizar a
    /// distribuicao espacial da infestacao. Nao descarta a bordadura: o gradiente da borda para o
    /// interior e justamente o sinal que justifica a aplicacao em faixa.
    /// </summary>
    Mapping,
}
