using Autodesk.Revit.UI;

namespace ElectricalBim.Revit;

public sealed class App : IExternalApplication
{
    public static RevitBridge? Bridge { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        const string tab = "Electrical BIM";
        try { application.CreateRibbonTab(tab); } catch { }
        var panel = application.CreateRibbonPanel(tab, "Platform");
        var assembly = typeof(App).Assembly.Location;
        panel.AddItem(new PushButtonData("ElectricalBimConnect", "Connect", assembly, typeof(ConnectCommand).FullName));
        panel.AddItem(new PushButtonData("ElectricalBimSync", "Sync Model", assembly, typeof(SyncCommand).FullName));

        Bridge = new RevitBridge();
        application.ControlledApplication.DocumentChanged += (_, _) => Bridge?.ScheduleSync();
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        Bridge?.Dispose();
        Bridge = null;
        return Result.Succeeded;
    }
}

