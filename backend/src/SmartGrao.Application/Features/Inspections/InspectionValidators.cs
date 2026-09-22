using FluentValidation;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;

namespace SmartGrao.Application.Features.Inspections;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonViewModel>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Person.MaximumNameLength);
        RuleFor(x => x.Role).IsInEnum().NotEqual(PersonRole.Undefined);
    }
}

public sealed class UpdatePersonValidator : AbstractValidator<UpdatePersonViewModel>
{
    public UpdatePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Person.MaximumNameLength);
        RuleFor(x => x.Role).IsInEnum().NotEqual(PersonRole.Undefined);
    }
}

public sealed class ScheduleInspectionValidator : AbstractValidator<ScheduleInspectionViewModel>
{
    public ScheduleInspectionValidator()
    {
        RuleFor(x => x.CultivationId).NotEmpty();
        RuleFor(x => x.SamplingPlanId).NotEmpty();
        RuleFor(x => x.ProtocolId).NotEmpty();
        RuleFor(x => x.ResponsibleId).NotEmpty();
        RuleFor(x => x.ScheduledFor).NotEmpty();
    }
}

public sealed class RescheduleInspectionValidator : AbstractValidator<RescheduleInspectionViewModel>
{
    public RescheduleInspectionValidator()
    {
        RuleFor(x => x.ResponsibleId).NotEmpty();
        RuleFor(x => x.ScheduledFor).NotEmpty();
    }
}

public sealed class CancelInspectionValidator : AbstractValidator<CancelInspectionViewModel>
{
    public CancelInspectionValidator() =>
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(Inspection.MaximumReasonLength);
}

public sealed class RecordObservationValidator : AbstractValidator<RecordObservationViewModel>
{
    public RecordObservationValidator()
    {
        RuleFor(x => x.RecordedAt).NotEmpty();
        RuleFor(x => x.Location).NotNull();
        RuleFor(x => x.Location.Latitude).InclusiveBetween(-90, 90).When(x => x.Location is not null);
        RuleFor(x => x.Location.Longitude).InclusiveBetween(-180, 180).When(x => x.Location is not null);
        RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0).When(x => x.AccuracyMeters.HasValue);
        RuleFor(x => x.GrowthStage).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(Observation.MaximumNotesLength);

        // Uma parada sem nenhum alvo avaliado nao registra nada: seria uma linha dizendo que alguem
        // esteve ali e nao olhou para coisa nenhuma.
        RuleFor(x => x.Counts).NotEmpty();
        RuleForEach(x => x.Counts).ChildRules(count =>
            count.RuleFor(x => x.ProtocolItemId).NotEmpty());
    }
}
