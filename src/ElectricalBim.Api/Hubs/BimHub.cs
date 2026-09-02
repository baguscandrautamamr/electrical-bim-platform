using Microsoft.AspNetCore.SignalR;

namespace ElectricalBim.Api.Hubs;

public sealed class BimHub : Hub
{
    public Task JoinProject(string projectId) => Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    public Task LeaveProject(string projectId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    public Task JoinAgent(string agentId) => Groups.AddToGroupAsync(Context.ConnectionId, AgentGroup(agentId));

    public static string ProjectGroup(string projectId) => $"project:{projectId}";
    public static string AgentGroup(string agentId) => $"agent:{agentId}";
}

