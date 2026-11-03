using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Analysis;

namespace SmartGrao.Application.Features.Analysis;

public sealed record ReviewAssessmentViewModel(LeafLabel Label, string Reviewer, string? Notes);

public sealed class ReviewAssessmentValidator : AbstractValidator<ReviewAssessmentViewModel>
{
    public ReviewAssessmentValidator()
    {
        RuleFor(x => x.Label).IsInEnum();
        RuleFor(x => x.Reviewer).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(Assessment.MaximumNotesLength);
    }
}

/// <summary>A pessoa confirma ou corrige o modelo. A revisao e a verdade de campo do aferimento.</summary>
public sealed class ReviewAssessmentCommandHandler(ISmartGraoDbContext db, IValidator<ReviewAssessmentViewModel> validator)
{
    public async Task<AssessmentViewModel> HandleAsync(Guid assessmentId, ReviewAssessmentViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);
        var id = new AssessmentId(assessmentId);
        var reviewed = await db.Assessments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Analysis.AssessmentNotFound);
        var review = Assessment.FromReview(reviewed, model.Label, model.Reviewer, model.Notes, DateTimeOffset.UtcNow);
        db.Assessments.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        return AssessmentViewModel.From(review);
    }
}
