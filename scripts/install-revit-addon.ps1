param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\ElectricalBim.Revit\ElectricalBim.Revit.csproj'
dotnet build $project -c $Configuration
$dll = Join-Path $repoRoot "src\ElectricalBim.Revit\bin\$Configuration\net8.0-windows\ElectricalBim.Revit.dll"
$manifestTemplate = Join-Path $repoRoot 'src\ElectricalBim.Revit\ElectricalBim.addin'
$destination = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2025'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$manifest = (Get-Content -Raw $manifestTemplate).Replace('__ASSEMBLY_PATH__', $dll)
Set-Content -Path (Join-Path $destination 'ElectricalBim.addin') -Value $manifest -Encoding utf8
Write-Host "Installed Electrical BIM add-in for Revit 2025: $dll"

