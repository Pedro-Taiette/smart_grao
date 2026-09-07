using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        return new SmartGraoDbContext(options);
    }
}
