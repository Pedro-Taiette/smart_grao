using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Photos;
using SmartGrao.Application.Features.Sampling;
using SmartGrao.Domain.Photos;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class PhotosController : ControllerBase
{
    private const long RequestLimit = Photo.MaximumBytes + 1024 * 1024;

    [HttpPost("api/photos", Name = "UploadObservationPhoto")]
    [RequestSizeLimit(RequestLimit)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PhotoViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoViewModel>> Upload(
        [FromForm] Guid id, [FromForm] Guid inspectionId, [FromForm] Guid observationId, [FromForm] Guid? targetId,
        [FromForm] PlantOrgan organ, [FromForm] DateTimeOffset takenAt, IFormFile file,
        [FromServices] UploadPhotoCommandHandler handler, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        return Ok(await handler.HandleAsync(new UploadObservationPhoto(
            id, inspectionId, observationId, targetId, organ, takenAt, file.ContentType, file.Length, content), cancellationToken));
    }

    [HttpPost("api/photos/device", Name = "UploadDevicePhoto")]
    [RequestSizeLimit(RequestLimit)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PhotoViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhotoViewModel>> UploadFromDevice(
        [FromForm] Guid id, [FromForm] Guid cultivationId, [FromForm] PhotoSource source, [FromForm] bool isSimulated,
        [FromForm] PlantOrgan organ, [FromForm] DateTimeOffset takenAt, [FromForm] double? latitude, [FromForm] double? longitude,
        IFormFile file, [FromServices] UploadPhotoCommandHandler handler, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        return Ok(await handler.HandleAsync(new UploadDevicePhoto(
            id, cultivationId, source, isSimulated, organ, takenAt, latitude, longitude, file.ContentType, file.Length, content), cancellationToken));
    }

    [HttpGet("api/photos", Name = "GetPhotos")]
    [ProducesResponseType(typeof(IReadOnlyList<PhotoViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PhotoViewModel>>> List(
        [FromQuery] Guid? inspectionId, [FromQuery] Guid? cultivationId,
        [FromServices] GetPhotosQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(inspectionId, cultivationId, cancellationToken));

    [HttpGet("api/photos/{id:guid}/content", Name = "GetPhotoContent")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    public async Task<IActionResult> Content(Guid id, [FromServices] GetPhotoContentQueryHandler handler, CancellationToken cancellationToken)
    {
        var (content, contentType) = await handler.HandleAsync(id, cancellationToken);
        return File(content, contentType);
    }

    [HttpPost("api/cultivations/{id:guid}/directed-plan", Name = "CreateDirectedPlan")]
    [ProducesResponseType(typeof(SamplingPlanViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<SamplingPlanViewModel>> DirectedPlan(
        Guid id, [FromServices] CreateDirectedPlanCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));
}
