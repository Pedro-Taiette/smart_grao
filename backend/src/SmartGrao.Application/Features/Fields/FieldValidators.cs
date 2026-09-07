using FluentValidation;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class CreateFieldViewModelValidator : AbstractValidator<CreateFieldViewModel>
{
    public CreateFieldViewModelValidator()
    {
        RuleFor(x => x.FarmId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Field.MaximumNameLength);
        RuleFor(x => x.Crop).IsInEnum();
        RuleFor(x => x.Boundary).NotNull().SetValidator(new GeoJsonPolygonValidator());
    }
}

public sealed class UpdateFieldViewModelValidator : AbstractValidator<UpdateFieldViewModel>
{
    public UpdateFieldViewModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Field.MaximumNameLength);
        RuleFor(x => x.Crop).IsInEnum();
        RuleFor(x => x.Boundary).NotNull().SetValidator(new GeoJsonPolygonValidator());
    }
}

/// <summary>
/// So a forma do documento. A validade topologica — auto-interseccao, fechamento do anel, area
/// minima — e do <see cref="Domain.Geo.Boundary"/>, que e por onde todo poligono passa venha de
/// onde vier.
/// </summary>
public sealed class GeoJsonPolygonValidator : AbstractValidator<GeoJsonPolygon>
{
    public GeoJsonPolygonValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => string.Equals(type, GeoJsonPolygon.ExpectedType, StringComparison.OrdinalIgnoreCase))
            .WithMessage("The geometry type must be 'Polygon'.");

        RuleFor(x => x.Coordinates).NotEmpty();
    }
}
