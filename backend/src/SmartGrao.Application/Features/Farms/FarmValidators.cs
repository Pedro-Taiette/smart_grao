using FluentValidation;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

/// <summary>
/// Checagens de forma sobre o que chegou pela rede. As regras de negocio de verdade continuam no
/// agregado — este validador existe para que um corpo obviamente vazio vire um 422 legivel em vez
/// de percorrer a aplicacao inteira ate esbarrar numa invariante.
/// </summary>
public sealed class SaveFarmViewModelValidator : AbstractValidator<SaveFarmViewModel>
{
    public SaveFarmViewModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Farm.MaximumNameLength);
        RuleFor(x => x.City).NotEmpty().MaximumLength(Farm.MaximumCityLength);
        RuleFor(x => x.State).NotEmpty().Length(2);

        When(x => x.Headquarters is not null, () =>
        {
            RuleFor(x => x.Headquarters!.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Headquarters!.Longitude).InclusiveBetween(-180, 180);
        });
    }
}
