using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Analysis;
using SmartGrao.Domain.Analysis;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class AnalysisController : ControllerBase
{
    [HttpPost("api/photos/{id:guid}/analysis", Name = "RequeueAnalysis")]
    [ProducesResponseType(typeof(AnalysisJobViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalysisJobViewModel>> Requeue(
        Guid id, [FromServices] RequeueAnalysisCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpGet("api/analysis-jobs", Name = "GetAnalysisJobs")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalysisJobViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnalysisJobViewModel>>> Jobs(
        [FromQuery] AnalysisJobStatus? status, [FromServices] GetAnalysisJobsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(status, cancellationToken));

    [HttpGet("api/assessments/{id:guid}/heatmap", Name = "GetAssessmentHeatmap")]
    [Produces("image/png")]
    public async Task<IActionResult> Heatmap(Guid id, [FromServices] GetHeatmapQueryHandler handler, CancellationToken cancellationToken)
        => File(await handler.HandleAsync(id, cancellationToken), "image/png");

    [HttpPost("api/assessments/{id:guid}/review", Name = "ReviewAssessment")]
    [ProducesResponseType(typeof(AssessmentViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssessmentViewModel>> Review(
        Guid id, [FromBody] ReviewAssessmentViewModel model, [FromServices] ReviewAssessmentCommandHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));
}
