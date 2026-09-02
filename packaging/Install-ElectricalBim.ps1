$ErrorActionPreference = 'Stop'
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$addinsRoot = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2025'
$pluginRoot = Join-Path $addinsRoot 'ElectricalBim'
New-Item -ItemType Directory -Force -Path $pluginRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'ElectricalBim.Revit.dll') -Destination $pluginRoot -Force
Copy-Item -LiteralPath (Join-Path $source 'ElectricalBim.Contracts.dll') -Destination $pluginRoot -Force
$assemblyPath = Join-Path $pluginRoot 'ElectricalBim.Revit.dll'
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Electrical BIM Platform</Name>
    <Assembly>$assemblyPath</Assembly>
    <AddInId>6F6289A9-8B4E-48A1-9242-7F6D6A0C5101</AddInId>
    <FullClassName>ElectricalBim.Revit.App</FullClassName>
    <VendorId>EBIM</VendorId>
    <VendorDescription>Electrical BIM Platform for Revit 2025</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
Set-Content -LiteralPath (Join-Path $addinsRoot 'ElectricalBim.addin') -Value $manifest -Encoding utf8
Write-Host 'Electrical BIM Platform installed for Revit 2025.' -ForegroundColor Green
Write-Host 'Open Revit, then use Electrical BIM > Connect.'

