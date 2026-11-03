using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartGrao.Application.Features.Analysis;
using SmartGrao.Infrastructure.Persistence;

namespace SmartGrao.Infrastructure.Analytics;

/// <summary>
/// Consome a fila de analises. A reserva e um UPDATE com FOR UPDATE SKIP LOCKED: dois workers nunca
/// pegam a mesma foto, e a fila participa das transacoes do dominio sem outro servico.
/// </summary>
internal sealed class AnalysisWorker(IServiceScopeFactory scopes, ILogger<AnalysisWorker> logger) : BackgroundService
{
    private const string Claim = """
        UPDATE analysis_jobs SET status = 'Running', updated_at = now()
        WHERE id = (
            SELECT id FROM analysis_jobs
            WHERE status = 'Pending' AND next_attempt_at <= now()
            ORDER BY next_attempt_at
            LIMIT 1
            FOR UPDATE SKIP LOCKED)
        RETURNING id AS "Value"
        """;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var id = await ClaimAsync(stoppingToken);
                if (id is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ProcessAnalysisJobCommandHandler>().HandleAsync(id.Value, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Falha no worker de analises");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<Guid?> ClaimAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGraoDbContext>();
        var ids = await db.Database.SqlQueryRaw<Guid>(Claim).ToListAsync(cancellationToken);
        return ids.Count == 0 ? null : ids[0];
    }

    /// <summary>Tarefa que ficou "Running" porque a API caiu no meio volta para a fila.</summary>
    private async Task RecoverAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGraoDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE analysis_jobs SET status = 'Pending' WHERE status = 'Running' AND updated_at < now() - interval '10 minutes'",
            cancellationToken);
    }
}
