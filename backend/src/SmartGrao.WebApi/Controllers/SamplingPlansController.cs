using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Sampling;

namespace SmartGrao.WebApi.Controllers;

/// <summary>
/// Planos de amostragem: a malha georreferenciada que o agronomo caminha no talhao.
/// <para>
/// Dois modos, uma rota. O <c>Monitoramento</c> deriva a quantidade de pontos da tabela do MIP-Soja
/// e descarta a bordadura; o <c>Mapeamento</c> recebe o espacamento e preserva a borda, porque ali o
/// gradiente da divisa para o interior e o proprio sinal. Ver <c>docs/amostragem.md</c>.
/// </para>
/// </summary>
[ApiController]
[Route("api/sampling-plans")]
[Produces("application/json")]
public sealed class SamplingPlansController : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetSamplingPlanById")]
    [ProducesResponseType(typeof(SamplingPlanViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SamplingPlanViewModel>> GetById(
        Guid id,
        [FromServices] GetSamplingPlanByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    /// <summary>
    /// Gera a malha para um talhao. Recusa espacamento no modo Monitoramento, onde ele nao e
    /// escolhido (422), talhao inexistente (404), talhao desativado (409) e talhao estreito demais
    /// para a bordadura (400).
    /// </summary>
    [HttpPost(Name = "GenerateSamplingPlan")]
    [ProducesResponseType(typeof(SamplingPlanViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SamplingPlanViewModel>> Generate(
        [FromBody] GenerateSamplingPlanViewModel model,
        [FromServices] GenerateSamplingPlanCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);

        return CreatedAtRoute("GetSamplingPlanById", new { id = created.Plan.Id }, created);
    }

    /// <summary>
    /// Apaga o plano e a malha junto. Diferente do talhao, um plano nao carrega historico de
    /// lavoura — e um roteiro que foi gerado e nao serviu.
    /// </summary>
    [HttpDelete("{id:guid}", Name = "DeleteSamplingPlan")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteSamplingPlanCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
