using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ElectricalBim.Revit;

[Transaction(TransactionMode.Manual)]
public sealed class ConnectCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (App.Bridge is null) return Result.Failed;
        App.Bridge.Attach(commandData.Application);
        TaskDialog.Show("Electrical BIM", "Connected to http://localhost:5080\nAgent: revit-local\nProject: demo");
        return Result.Succeeded;
    }
}

[Transaction(TransactionMode.Manual)]
public sealed class SyncCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (App.Bridge is null) return Result.Failed;
        App.Bridge.Attach(commandData.Application);
        App.Bridge.SyncNow(commandData.Application.ActiveUIDocument.Document);
        return Result.Succeeded;
    }
}

