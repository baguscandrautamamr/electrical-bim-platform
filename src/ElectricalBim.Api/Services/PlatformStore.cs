using System.Collections.Concurrent;
using ElectricalBim.Contracts;

namespace ElectricalBim.Api.Services;

public sealed class PlatformStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BimElementDto>> _elements = new();
    private readonly ConcurrentDictionary<Guid, RemoteJob> _jobs = new();

    public IReadOnlyCollection<BimElementDto> GetElements(string projectId) =>
        _elements.TryGetValue(projectId, out var project)
            ? project.Values.OrderBy(x => x.Category).ThenBy(x => x.ElementId).ToArray()
            : Array.Empty<BimElementDto>();

    public ElementSyncResult Sync(string projectId, ElementSyncRequest request)
    {
        var project = _elements.GetOrAdd(projectId, _ => new());
        foreach (var element in request.Elements)
            project[element.UniqueId] = element with { UpdatedAt = DateTimeOffset.UtcNow };
        return new ElementSyncResult(request.Elements.Count, DateTimeOffset.UtcNow);
    }

    public RemoteJob Enqueue(string projectId, CreateJobRequest request)
    {
        var job = new RemoteJob(Guid.NewGuid(), projectId, request.AgentId, request.Type,
            request.Payload ?? new Dictionary<string, string>(), JobStatus.Queued, DateTimeOffset.UtcNow);
        _jobs[job.Id] = job;
        return job;
    }

    public RemoteJob? ClaimNext(string agentId)
    {
        foreach (var candidate in _jobs.Values.Where(x => x.AgentId == agentId && x.Status == JobStatus.Queued).OrderBy(x => x.CreatedAt))
        {
            var claimed = candidate with { Status = JobStatus.Running, StartedAt = DateTimeOffset.UtcNow };
            if (_jobs.TryUpdate(candidate.Id, claimed, candidate)) return claimed;
        }
        return null;
    }

    public RemoteJob? Complete(Guid id, CompleteJobRequest request)
    {
        while (_jobs.TryGetValue(id, out var current))
        {
            var completed = current with {
                Status = request.Success ? JobStatus.Completed : JobStatus.Failed,
                FinishedAt = DateTimeOffset.UtcNow,
                Result = request.Result,
                Error = request.Error
            };
            if (_jobs.TryUpdate(id, completed, current)) return completed;
        }
        return null;
    }

    public IReadOnlyCollection<RemoteJob> GetJobs(string projectId) =>
        _jobs.Values.Where(x => x.ProjectId == projectId).OrderByDescending(x => x.CreatedAt).ToArray();
}

