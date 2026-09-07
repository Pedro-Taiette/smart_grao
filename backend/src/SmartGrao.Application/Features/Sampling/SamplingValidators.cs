using FluentValidation;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

public sealed class GenerateSamplingPlanViewModelValidator
    : AbstractValidator<GenerateSamplingPlanViewModel>
{
    public GenerateSamplingPlanViewModelValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.Mode).IsInEnum();

        RuleFor(x => x.SpacingMeters)
            .NotNull()
            .When(x => x.Mode == SamplingMode.Mapping)
            .WithMessage("The mapping mode needs a spacing in metres.");

        // Recusar em vez de ignorar. Aceitar um espacamento que nao tera efeito faria o cliente
        // acreditar que escolheu a densidade, quando no Monitoramento ela vem da tabela do MIP-Soja.
        RuleFor(x => x.SpacingMeters)
            .Null()
            .When(x => x.Mode == SamplingMode.Monitoring)
            .WithMessage("The monitoring mode derives its own spacing; do not send one.");

        RuleFor(x => x.SpacingMeters!.Value)
            .InclusiveBetween(SamplingSpacing.MinimumMeters, SamplingSpacing.MaximumMeters)
            .When(x => x.SpacingMeters is not null);
    }
}
