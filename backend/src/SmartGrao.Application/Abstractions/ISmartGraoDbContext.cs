using Microsoft.EntityFrameworkCore;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Abstractions;

/// <summary>
/// A superficie de persistencia de que a aplicacao depende. Uma interface fina sobre o
/// <c>DbContext</c> do EF Core — que <i>ja e</i> a unidade de trabalho — entao os casos de uso
/// consultam os conjuntos com LINQ comum mais os operadores assincronos do EF e confirmam tudo com
/// <see cref="SaveChangesAsync"/>.
/// <para>
/// Sem um repositorio por agregado: um repositorio sobre o EF acrescenta uma camada que quase so
/// encaminha chamadas e obriga cada nova forma de consulta a virar um metodo novo na interface —
/// e a consulta espacial de sobreposicao, que e a mais interessante do Pilar 1, era exatamente a
/// que mais sofria com isso.
/// </para>
/// <para>
/// A interface vive na Application e nao no Domain porque quem precisa dela sao os casos de uso;
/// o Domain segue sem saber que persistencia existe.
/// </para>
/// </summary>
public interface ISmartGraoDbContext
{
    DbSet<Farm> Farms { get; }

    DbSet<Field> Fields { get; }

    /// <summary>
    /// Os planos de amostragem. Nao ha <c>DbSet</c> para <c>SamplingPoint</c>: o ponto so existe
    /// dentro do plano que o gerou, e expo-lo como conjunto proprio convidaria a consultas que
    /// atravessam a fronteira do agregado.
    /// </summary>
    DbSet<SamplingPlan> SamplingPlans { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
