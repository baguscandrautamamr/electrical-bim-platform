using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ElectricalBim.Contracts;

namespace ElectricalBim.Revit;

public sealed class RevitBridge : IExternalEventHandler, IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:5080"), Timeout = TimeSpan.FromSeconds(20) };
    private readonly ExternalEvent _externalEvent;
    private readonly CancellationTokenSource _stop = new();
    private UIApplication? _uiApplication;
    private RemoteJob? _pendingJob;
    private int _syncScheduled;
    private const string ProjectId = "demo";
    private const string AgentId = "revit-local";

    public RevitBridge() => _externalEvent = ExternalEvent.Create(this);

    public void Attach(UIApplication application)
    {
        _uiApplication = application;
        _ = PollJobsAsync(_stop.Token);
    }

    public void ScheduleSync() => Interlocked.Exchange(ref _syncScheduled, 1);

    public void SyncNow(Document document)
    {
        var elements = CollectElectricalElements(document);
        var request = new ElementSyncRequest(AgentId, document.Title, elements, Guid.NewGuid().ToString("N"));
        _ = PostSyncAsync(request);
        Interlocked.Exchange(ref _syncScheduled, 0);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var document = app.ActiveUIDocument?.Document;
            if (document is null) return;

            if (_pendingJob is { } job)
            {
                _pendingJob = null;
                ExecuteJob(app, document, job);
            }
            else if (Interlocked.Exchange(ref _syncScheduled, 0) == 1)
            {
                SyncNow(document);
            }
        }
        catch (Exception ex) { TaskDialog.Show("Electrical BIM", ex.Message); }
    }

    public string GetName() => "Electrical BIM Revit Bridge";

    private void ExecuteJob(UIApplication app, Document document, RemoteJob job)
    {
        try
        {
            switch (job.Type.ToLowerInvariant())
            {
                case "sync-model": SyncNow(document); break;
                case "select-elements":
                    var ids = job.Payload.TryGetValue("uniqueIds", out var raw) ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries) : [];
                    var elementIds = ids.Select(document.GetElement).Where(x => x is not null).Select(x => x!.Id).ToList();
                    app.ActiveUIDocument.Selection.SetElementIds(elementIds);
                    break;
                case "update-parameter": UpdateParameter(document, job.Payload); break;
                default: throw new InvalidOperationException($"Job '{job.Type}' belum diimplementasikan pada add-in.");
            }
            _ = CompleteAsync(job.Id, true, "Completed by Revit 2025", null);
        }
        catch (Exception ex) { _ = CompleteAsync(job.Id, false, null, ex.Message); }
    }

    private static void UpdateParameter(Document document, IReadOnlyDictionary<string, string> payload)
    {
        if (!payload.TryGetValue("uniqueId", out var uniqueId) || !payload.TryGetValue("parameter", out var name) || !payload.TryGetValue("value", out var value))
            throw new InvalidOperationException("Payload requires uniqueId, parameter, and value.");
        var element = document.GetElement(uniqueId) ?? throw new InvalidOperationException("Element not found.");
        var parameter = element.LookupParameter(name) ?? throw new InvalidOperationException("Parameter not found.");
        if (parameter.IsReadOnly) throw new InvalidOperationException("Parameter is read-only.");
        using var transaction = new Transaction(document, "Electrical BIM remote update");
        transaction.Start();
        parameter.Set(value);
        transaction.Commit();
    }

    private static IReadOnlyList<BimElementDto> CollectElectricalElements(Document document)
    {
        var categories = new[] { BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_ElectricalFixtures,
            BuiltInCategory.OST_LightingFixtures, BuiltInCategory.OST_CableTray, BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_ElectricalCircuit };
        return new FilteredElementCollector(document).WhereElementIsNotElementType()
            .Where(e => e.Category is not null && categories.Contains((BuiltInCategory)e.Category.Id.Value))
            .Select(e => new BimElementDto(e.UniqueId, e.Id.Value, e.Category!.Name,
                e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString() ?? "",
                e.Name ?? "", e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString(),
                e.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM)?.AsString(),
                ReadParameters(e), DateTimeOffset.UtcNow)).ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadParameters(Element element) => element.Parameters
        .Cast<Parameter>().Where(p => p.Definition is not null && p.HasValue).Take(80)
        .GroupBy(p => p.Definition.Name).ToDictionary(g => g.Key, g => g.First().AsValueString() ?? g.First().AsString() ?? "");

    private async Task PostSyncAsync(ElementSyncRequest request)
    {
        try { await _http.PostAsJsonAsync($"/api/projects/{ProjectId}/elements/sync", request, _stop.Token); } catch { ScheduleSync(); }
    }

    private async Task PollJobsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_pendingJob is null)
                {
                    var response = await _http.GetAsync($"/api/agents/{AgentId}/jobs/next", token);
                    if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                    {
                        _pendingJob = await response.Content.ReadFromJsonAsync<RemoteJob>(cancellationToken: token);
                        if (_pendingJob is not null) _externalEvent.Raise();
                    }
                }
            }
            catch when (!token.IsCancellationRequested) { }
            await Task.Delay(TimeSpan.FromSeconds(2), token);
        }
    }

    private Task CompleteAsync(Guid jobId, bool success, string? result, string? error) =>
        _http.PostAsJsonAsync($"/api/jobs/{jobId}/complete", new CompleteJobRequest(success, result, error));

    public void Dispose() { _stop.Cancel(); _http.Dispose(); _stop.Dispose(); }
}
