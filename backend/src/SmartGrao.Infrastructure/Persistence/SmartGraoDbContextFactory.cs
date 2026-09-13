using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Infrastructure.Persistence;

/// <summary>
/// Usada apenas pelas ferramentas de linha de comando do EF Core (<c>dotnet ef migrations add</c>).
/// Existe para que gerar uma migration nao exija subir a aplicacao nem alcancar o banco: a string
/// aqui e um alvo de projeto, nunca a de execucao.
/// </summary>
internal sealed class SmartGraoDbContextFactory : IDesignTimeDbContextFactory<SmartGraoDbContext>
{
    public SmartGraoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTGRAO_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=smartgrao;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<SmartGraoDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention()
            .Options;

        // As ferramentas so leem o modelo para gerar migrations — nunca salvam nada, entao nenhum
        // evento chega a ser despachado. Um despachante que nao faz nada e mais honesto aqui do que
        // montar o container inteiro da aplicacao para satisfazer um construtor.
        return new SmartGraoDbContext(options, NoDomainEventDispatcher.Instance);
    }

    private sealed class NoDomainEventDispatcher : IDomainEventDispatcher
    {
        public static readonly NoDomainEventDispatcher Instance = new();

        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
