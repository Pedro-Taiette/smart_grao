using FluentValidation;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;

namespace SmartGrao.Application.Features.Inspections;

/// <summary>
/// Início e conclusão usam o relógio do servidor, ao contrário do horário de cada parada, que vem do
/// cliente. A parada precisa da hora em que a pessoa esteve no ponto — inclusive sem sinal; abrir e
/// fechar a visita são atos que só existem quando há conexão, e aí o relógio confiável é o do
/// servidor.
/// </summary>
public sealed class StartInspectionCommandHandler(ISmartGraoDbContext db)
{
    public async Task<InspectionViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inspection = await db.LoadInspectionAsync(id, cancellationToken);

        inspection.Start(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}

public sealed class CompleteInspectionCommandHandler(ISmartGraoDbContext db)
{
    public async Task<InspectionViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inspection = await db.LoadInspectionAsync(id, cancellationToken);

        inspection.Complete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}

public sealed class CancelInspectionCommandHandler(
    ISmartGraoDbContext db, IValidator<CancelInspectionViewModel> validator)
{
    public async Task<InspectionViewModel> HandleAsync(
        Guid id, CancelInspectionViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var inspection = await db.LoadInspectionAsync(id, cancellationToken);

        inspection.Cancel(model.Reason);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}
