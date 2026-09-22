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
