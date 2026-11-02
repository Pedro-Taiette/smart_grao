using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartGrao.Application.Features.Risk;
using SmartGrao.Domain.Fields;
using SmartGrao.Infrastructure.Persistence;

namespace SmartGrao.Infrastructure.Weather;

/// <summary>Atualiza o clima de todo cultivo de milho em andamento, sem ninguem pedir.</summary>
internal sealed class WeatherRefreshWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<WeatherRefreshWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = configuration.GetValue("Weather:RefreshIntervalHours", 6);
        if (hours <= 0) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        do
        {
            await RefreshAllAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        List<Guid> ids;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartGraoDbContext>();
            ids = await db.Cultivations.AsNoTracking()
                .Where(x => x.Crop == Crop.Corn && x.EndedOn == null)
                .Select(x => x.Id.Value).ToListAsync(cancellationToken);
        }

        foreach (var id in ids)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<RefreshWeatherCommandHandler>().HandleAsync(id, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Falha ao atualizar o clima do cultivo {CultivationId}", id);
            }
        }
        logger.LogInformation("Clima atualizado para {Count} cultivos de milho", ids.Count);
    }
}
