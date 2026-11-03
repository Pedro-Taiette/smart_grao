using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Photos;

namespace SmartGrao.Domain.Analysis;

public readonly record struct AnalysisJobId(Guid Value) : IStronglyTypedId
{
    public static AnalysisJobId New() => new(Guid.CreateVersion7());
}

public enum AnalysisJobStatus
{
    Pending = 0,
    Running,
    Done,
    Failed,
}

/// <summary>Uma foto na fila de analise. A fila e a tabela: o worker reserva com FOR UPDATE SKIP LOCKED.</summary>
public sealed class AnalysisJob : AggregateRoot<AnalysisJobId>
{
    public const int MaximumAttempts = 5;
    public const int MaximumErrorLength = 1000;

    private AnalysisJob() { }

    public PhotoId PhotoId { get; private set; }

    public AnalysisJobStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public static AnalysisJob For(PhotoId photoId, DateTimeOffset now) => new()
    {
        Id = AnalysisJobId.New(), PhotoId = photoId, Status = AnalysisJobStatus.Pending, NextAttemptAt = now.ToUniversalTime(),
    };

    public void Succeed(DateTimeOffset now)
    {
        Status = AnalysisJobStatus.Done;
        Attempts++;
        LastError = null;
        FinishedAt = now.ToUniversalTime();
        MarkAsUpdated();
    }

    /// <summary>Espera 30 s, 1, 2 e 4 minutos entre as tentativas; na quinta falha, desiste e registra.</summary>
    public void Fail(string error, DateTimeOffset now)
    {
        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "erro sem mensagem" : error.Trim()[..Math.Min(error.Trim().Length, MaximumErrorLength)];
        if (Attempts >= MaximumAttempts)
        {
            Status = AnalysisJobStatus.Failed;
            FinishedAt = now.ToUniversalTime();
        }
        else
        {
            Status = AnalysisJobStatus.Pending;
            NextAttemptAt = now.ToUniversalTime().AddSeconds(30 * Math.Pow(2, Attempts - 1));
        }
        MarkAsUpdated();
    }

    /// <summary>Volta para a fila, por exemplo depois de trocar o modelo.</summary>
    public void Retry(DateTimeOffset now)
    {
        Status = AnalysisJobStatus.Pending;
        Attempts = 0;
        LastError = null;
        FinishedAt = null;
        NextAttemptAt = now.ToUniversalTime();
        MarkAsUpdated();
    }
}
