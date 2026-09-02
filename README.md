# Electrical BIM Platform

MVP platform yang menghubungkan Autodesk Revit 2025 dengan dashboard web secara realtime. Fitur awal:

- Sinkronisasi snapshot elemen elektrikal dari Revit ke web melalui SignalR.
- BIM Chat berbasis perintah deterministik untuk query panel, circuit, beban, dan status elemen.
- Remote Job Queue untuk `sync-model`, `select-elements`, `update-parameter`, dan pekerjaan ekspor.
- Revit add-in menggunakan `ExternalEvent`, sehingga perubahan model selalu dijalankan pada thread Revit yang benar.

## Arsitektur

```text
Revit 2025 Add-in <-> ASP.NET Core API + SignalR <-> Web Dashboard
                              |
                       In-memory MVP store
```

MVP memakai penyimpanan in-memory agar dapat dijalankan tanpa layanan tambahan. Untuk produksi, ganti store dengan PostgreSQL/Supabase dan gunakan antrean persisten.

## Menjalankan API dan dashboard

Prasyarat: .NET SDK 8.

```powershell
dotnet run --project src/ElectricalBim.Api/ElectricalBim.Api.csproj
```

Buka `http://localhost:5080` untuk dashboard.

## Revit 2025 add-in

Revit 2025 menggunakan .NET 8. Project add-in mencari DLL API di lokasi default:

`C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll`

Build dan instal:

```powershell
dotnet build src/ElectricalBim.Revit/ElectricalBim.Revit.csproj -c Release
./scripts/install-revit-addon.ps1 -Configuration Release
```

Setelah Revit dibuka, gunakan ribbon **Electrical BIM > Connect**. URL server default adalah `http://localhost:5080`.

Paket add-in siap instal tersedia di [`artifacts/ElectricalBim-Revit2025-MVP.zip`](artifacts/ElectricalBim-Revit2025-MVP.zip). Ekstrak ZIP di PC yang memiliki Revit 2025, lalu jalankan `Install-ElectricalBim.ps1`.

## Endpoint utama

- `GET /api/health`
- `GET /api/projects/{projectId}/elements`
- `POST /api/projects/{projectId}/elements/sync`
- `POST /api/projects/{projectId}/chat`
- `POST /api/projects/{projectId}/jobs`
- `GET /api/agents/{agentId}/jobs/next`
- `POST /api/jobs/{jobId}/complete`
- SignalR hub: `/hubs/bim`

## Keamanan produksi

Sebelum dipasang ke internet, tambahkan OIDC/JWT, pembatasan tenant/project, TLS, audit log persisten, idempotency key, rate limiting, secret manager, dan validasi allowlist untuk jenis remote job.
