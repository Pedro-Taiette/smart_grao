using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class GetCultivationsQueryHandler(ISmartGraoDbContext db)
{
    /// <summary>
    /// Os ciclos de um talhão, ou os da propriedade inteira.
    /// <para>
    /// O recorte por fazenda existe para a tela de abertura: "quais talhões estão sem cultivo
    /// aberto" é uma pergunta sobre a propriedade, e respondê-la com uma consulta por talhão faria
    /// o painel disparar uma requisição por talhão cadastrado.
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<CultivationViewModel>> HandleAsync(
        Guid? fieldId = null, Guid? farmId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Cultivations.AsNoTracking().Include(x => x.Stages).AsQueryable();

        if (fieldId is { } rawField)
        {
            var id = new FieldId(rawField);
            // Talhão inexistente é 404, e não lista vazia: pedir o ciclo de algo que não existe é
            // erro de quem chama, não ausência de dado.
            if (!await db.Fields.AnyAsync(x => x.Id == id, cancellationToken))
                throw new DomainException(SmartGraoErrors.Field.NotFound);
            query = query.Where(x => x.FieldId == id);
        }

        if (farmId is { } rawFarm)
        {
            var id = new FarmId(rawFarm);
            query = query.Where(x => db.Fields.Any(field => field.Id == x.FieldId && field.FarmId == id));
        }

        var cultivations = await query.OrderByDescending(x => x.PlantedOn).ToListAsync(cancellationToken);
        return cultivations.Select(CultivationViewModel.From).ToList();
    }
}
