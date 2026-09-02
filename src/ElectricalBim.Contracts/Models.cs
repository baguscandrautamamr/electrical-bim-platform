namespace ElectricalBim.Contracts;

public sealed record BimElementDto(
    string UniqueId,
    long ElementId,
    string Category,
    string Family,
    string Type,
    string? Level,
    string? System,
    IReadOnlyDictionary<string, string> Parameters,
    DateTimeOffset UpdatedAt);

public sealed record ElementSyncRequest(
    string AgentId,
    string ModelName,
    IReadOnlyList<BimElementDto> Elements,
    string? CorrelationId = null);

public sealed record ElementSyncResult(int Accepted, DateTimeOffset SyncedAt);

public sealed record ChatRequest(string Message);
public sealed record ChatResponse(string Answer, IReadOnlyList<string> ElementUniqueIds, object? Data = null);

public enum JobStatus { Queued, Running, Completed, Failed }

public sealed record CreateJobRequest(
    string AgentId,
    string Type,
    IReadOnlyDictionary<string, string>? Payload = null);

public sealed record RemoteJob(
    Guid Id,
    string ProjectId,
    string AgentId,
    string Type,
    IReadOnlyDictionary<string, string> Payload,
    JobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? FinishedAt = null,
    string? Result = null,
    string? Error = null);

public sealed record CompleteJobRequest(bool Success, string? Result = null, string? Error = null);

public static class RealtimeEvents
{
    public const string ElementsChanged = "elements.changed";
    public const string JobQueued = "jobs.queued";
    public const string JobChanged = "jobs.changed";
}
