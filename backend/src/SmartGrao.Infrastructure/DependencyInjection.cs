using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGrao.Application.Abstractions;
using SmartGrao.Infrastructure.Persistence;

namespace SmartGrao.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionName = "SmartGrao";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"A connection string '{ConnectionName}' nao foi configurada.");

        services.AddDbContext<SmartGraoDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Liga o mapeamento entre os tipos do NetTopologySuite e as colunas geometry/geography,
                // nos dois sentidos: e o que permite gravar um Polygon e traduzir Intersects/Contains
                // para ST_Intersects/ST_Contains em vez de trazer as linhas para a memoria.
                npgsql.UseNetTopologySuite();

                // O Neon e serverless: a instancia hiberna quando ociosa e a primeira conexao depois
                // disso paga o tempo de acordar. Sem o retry, esse cold start aparece como um erro
                // transitorio na primeira requisicao do dia.
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(5), null);
            });

            // snake_case nas tabelas e colunas: e a convencao do PostgreSQL, e evita que cada
            // consulta escrita a mao no psql precise de aspas em volta de "AreaHectares".
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<ISmartGraoDbContext>(sp => sp.GetRequiredService<SmartGraoDbContext>());

        return services;
    }
}
