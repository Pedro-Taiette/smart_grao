using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

/// <summary>
/// A escala fenologica da cultura, para a tela oferecer os estadios em vez de pedir o codigo
/// digitado. Nao consulta o banco: a escala e tabela de dominio, nao dado de uma propriedade.
/// <para>
/// A lista vazia nao e um erro — significa que nao ha escala transcrita para a cultura, e que ali o
/// estadio segue sendo texto livre.
/// </para>
/// </summary>
public sealed class GetGrowthStagesQueryHandler
{
    public Task<IReadOnlyList<GrowthStageOptionViewModel>> HandleAsync(
        Crop crop, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<GrowthStageOptionViewModel> stages = GrowthStageCatalog.For(crop)
            .Select(stage => new GrowthStageOptionViewModel(stage.Code, stage.Description))
            .ToList();

        return Task.FromResult(stages);
    }
}
