using FluentValidation;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class CreateMonitoringTargetValidator : AbstractValidator<CreateMonitoringTargetViewModel>
{
    public CreateMonitoringTargetValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(MonitoringTarget.MaximumCodeLength);
        RuleFor(x => x.CommonName).NotEmpty().MaximumLength(MonitoringTarget.MaximumNameLength);
        RuleFor(x => x.ScientificName).NotEmpty().MaximumLength(MonitoringTarget.MaximumNameLength);
        RuleFor(x => x.Kind).IsInEnum().NotEqual(TargetKind.Undefined);
        RuleFor(x => x.Crop).IsInEnum().NotEqual(Crop.Undefined);
    }
}

public sealed class ChangeAutomationValidator : AbstractValidator<ChangeAutomationViewModel>
{
    public ChangeAutomationValidator() => RuleFor(x => x.Automation).IsInEnum();
}

public sealed class CreateProtocolValidator : AbstractValidator<CreateProtocolViewModel>
{
    public CreateProtocolValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(Protocol.MaximumCodeLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Protocol.MaximumNameLength);
        RuleFor(x => x.Crop).IsInEnum().NotEqual(Crop.Undefined);
    }
}

public sealed class RenameProtocolValidator : AbstractValidator<RenameProtocolViewModel>
{
    public RenameProtocolValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Protocol.MaximumNameLength);
}

public sealed class AddProtocolItemValidator : AbstractValidator<AddProtocolItemViewModel>
{
    public AddProtocolItemValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Organ).IsInEnum().NotEqual(PlantOrgan.Undefined).When(x => x.Organ.HasValue);
        RuleFor(x => x.Unit).IsInEnum().NotEqual(CountUnit.Undefined);
        RuleFor(x => x.PhotosRequested).InclusiveBetween(0, ProtocolItem.MaximumPhotosRequested);
        RuleFor(x => x.Instructions).NotEmpty().MaximumLength(ProtocolItem.MaximumInstructionsLength);
        RuleFor(x => x.ReferenceLevel!.Source)
            .NotEmpty().MaximumLength(ReferenceLevel.MaximumSourceLength)
            .When(x => x.ReferenceLevel is not null);
    }
}
