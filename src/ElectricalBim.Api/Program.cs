using ElectricalBim.Api.Hubs;
using ElectricalBim.Api.Services;
using ElectricalBim.Contracts;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<PlatformStore>();
builder.Services.AddSingleton<BimChatService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.MapGet("/api/projects/{projectId}/elements", (string projectId, PlatformStore store) =>
    Results.Ok(store.GetElements(projectId)));

app.MapPost("/api/projects/{projectId}/elements/sync", async (string projectId, ElementSyncRequest request,
    PlatformStore store, IHubContext<BimHub> hub) =>
{
    var result = store.Sync(projectId, request);
    await hub.Clients.Group(BimHub.ProjectGroup(projectId)).SendAsync(RealtimeEvents.ElementsChanged, result);
    return Results.Ok(result);
});

app.MapPost("/api/projects/{projectId}/chat", (string projectId, ChatRequest request, BimChatService chat) =>
    Results.Ok(chat.Ask(projectId, request.Message)));

app.MapGet("/api/projects/{projectId}/jobs", (string projectId, PlatformStore store) => Results.Ok(store.GetJobs(projectId)));

app.MapPost("/api/projects/{projectId}/jobs", async (string projectId, CreateJobRequest request,
    PlatformStore store, IHubContext<BimHub> hub) =>
{
    var allowed = new[] { "sync-model", "select-elements", "update-parameter", "export-pdf", "export-ifc" };
    if (!allowed.Contains(request.Type, StringComparer.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Unsupported job type", allowed });
    var job = store.Enqueue(projectId, request);
    await hub.Clients.Group(BimHub.AgentGroup(request.AgentId)).SendAsync(RealtimeEvents.JobQueued, job);
    await hub.Clients.Group(BimHub.ProjectGroup(projectId)).SendAsync(RealtimeEvents.JobChanged, job);
    return Results.Accepted($"/api/projects/{projectId}/jobs", job);
});

app.MapGet("/api/agents/{agentId}/jobs/next", (string agentId, PlatformStore store) =>
    store.ClaimNext(agentId) is { } job ? Results.Ok(job) : Results.NoContent());

app.MapPost("/api/jobs/{jobId:guid}/complete", async (Guid jobId, CompleteJobRequest request,
    PlatformStore store, IHubContext<BimHub> hub) =>
{
    var job = store.Complete(jobId, request);
    if (job is null) return Results.NotFound();
    await hub.Clients.Group(BimHub.ProjectGroup(job.ProjectId)).SendAsync(RealtimeEvents.JobChanged, job);
    return Results.Ok(job);
});

app.MapHub<BimHub>("/hubs/bim");
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
