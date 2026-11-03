using FluentValidation;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class CreateSeasonValidator : AbstractValidator<CreateSeasonViewModel>
{
    public CreateSeasonValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}

public sealed class CreateCultivationValidator : AbstractValidator<CreateCultivationViewModel>
{
    public CreateCultivationValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.SeasonId).NotEmpty();
        RuleFor(x => x.Crop).IsInEnum().NotEqual(SmartGrao.Domain.Fields.Crop.Undefined);
        RuleFor(x => x.Cultivar).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PlantedOn).NotEmpty();
        RuleFor(x => x.RiskFactors!).SetValidator(new CercosporaRiskFactorsValidator()).When(x => x.RiskFactors is not null);
    }
}

public sealed class CercosporaRiskFactorsValidator : AbstractValidator<CercosporaRiskFactorsViewModel>
{
    public CercosporaRiskFactorsValidator()
    {
        RuleFor(x => x.HybridReaction).IsInEnum();
        RuleFor(x => x.CropSequence).IsInEnum();
        RuleFor(x => x.Tillage).IsInEnum();
    }
}

public sealed class RecordGrowthStageValidator : AbstractValidator<RecordGrowthStageViewModel>
{
    public RecordGrowthStageValidator()
    {
        RuleFor(x => x.ObservedOn).NotEmpty();
        RuleFor(x => x.Stage).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
