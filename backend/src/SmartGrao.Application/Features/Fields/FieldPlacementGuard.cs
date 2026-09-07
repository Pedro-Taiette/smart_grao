using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Application.Features.Fields;

/// <summary>
/// As duas regras que dependem dos <i>outros</i> talhoes da fazenda, e por isso nao cabem dentro do
/// agregado <see cref="Field"/>: nome unico e ausencia de sobreposicao.
/// <para>
/// Colaborador explicito, injetado em quem precisa. Antes eram metodos estaticos que o handler de
/// atualizacao tomava emprestado do de criacao — o que fazia parecer que atualizar era um caso
/// particular de criar, e prendia um ao outro sem que nada no codigo dissesse por que.
/// </para>
/// </summary>
public sealed class FieldPlacementGuard(ISmartGraoDbContext dbContext)
{
    /// <param name="ignoring">
    /// O proprio talhao, ao ser editado: sem isso ele se acusaria de repetir o proprio nome e
    /// nenhuma edicao passaria.
    /// </param>
    public async Task EnsureNameIsAvailableAsync(
        FarmId farmId,
        string name,
        FieldId? ignoring = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLowerInvariant();

        var taken = await dbContext.Fields.AnyAsync(
            field => field.FarmId == farmId &&
                     field.Name.ToLower() == normalized &&
                     (ignoring == null || field.Id != ignoring.Value),
            cancellationToken);

        if (taken)
            throw new DomainException(SmartGraoErrors.Field.DuplicateName);
    }

    /// <summary>
    /// Recusa um contorno que divida area com outro talhao da mesma fazenda.
    /// <para>
    /// Dois passos de proposito. A triagem grossa e <c>ST_Intersects</c> no banco, apoiada no indice
    /// GiST — que tambem devolve vizinhos apenas encostados na divisa, o caso normal de uma fazenda
    /// e nao um conflito. A confirmacao fina, area em comum de verdade, e do dominio, sobre os
    /// poucos candidatos que sobraram: essa distincao e regra de negocio, nao detalhe de consulta.
    /// </para>
    /// </summary>
    public async Task EnsureNoOverlapAsync(
        FarmId farmId,
        Boundary boundary,
        FieldId? ignoring = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        var candidates = await dbContext.Fields
            .AsNoTracking()
            .Where(field => field.FarmId == farmId &&
                            (ignoring == null || field.Id != ignoring.Value) &&
                            field.Geometry.Intersects(boundary.Polygon))
            .ToListAsync(cancellationToken);

        if (candidates.Any(candidate => boundary.OverlapsWith(candidate.Boundary)))
            throw new DomainException(SmartGraoErrors.Field.OverlapsAnother);
    }
}
