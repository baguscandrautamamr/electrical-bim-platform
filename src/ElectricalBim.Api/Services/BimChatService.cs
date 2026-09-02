using ElectricalBim.Contracts;

namespace ElectricalBim.Api.Services;

public sealed class BimChatService(PlatformStore store)
{
    public ChatResponse Ask(string projectId, string message)
    {
        var elements = store.GetElements(projectId);
        var text = message.Trim().ToLowerInvariant();

        if (elements.Count == 0)
            return new("Model belum tersinkron. Hubungkan add-in Revit lalu jalankan sync.", Array.Empty<string>());

        if (text.Contains("panel"))
        {
            var panels = elements.Where(x => x.Category.Contains("Electrical Equipment", StringComparison.OrdinalIgnoreCase)).ToArray();
            return new($"Ditemukan {panels.Length} panel/electrical equipment.", panels.Select(x => x.UniqueId).ToArray(),
                panels.Select(x => new { x.ElementId, x.Family, x.Type, x.Level }));
        }

        if (text.Contains("circuit") || text.Contains("sirkuit"))
        {
            var circuits = elements.Where(x => x.Category.Contains("Electrical Circuit", StringComparison.OrdinalIgnoreCase)).ToArray();
            return new($"Ditemukan {circuits.Length} circuit.", circuits.Select(x => x.UniqueId).ToArray());
        }

        if (text.Contains("kategori") || text.Contains("ringkas") || text.Contains("summary"))
        {
            var summary = elements.GroupBy(x => x.Category).OrderByDescending(x => x.Count())
                .Select(x => new { Category = x.Key, Count = x.Count() }).ToArray();
            return new($"Model memiliki {elements.Count} elemen dalam {summary.Length} kategori.", Array.Empty<string>(), summary);
        }

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = elements.Where(x => tokens.Any(t => t.Length > 2 &&
            (x.Category.Contains(t, StringComparison.OrdinalIgnoreCase) || x.Family.Contains(t, StringComparison.OrdinalIgnoreCase) ||
             x.Type.Contains(t, StringComparison.OrdinalIgnoreCase) || (x.Level?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false))))
            .Take(100).ToArray();

        return matches.Length > 0
            ? new($"Saya menemukan {matches.Length} elemen yang cocok.", matches.Select(x => x.UniqueId).ToArray())
            : new("Belum ada kecocokan. Coba tanya: 'ringkas kategori', 'berapa panel', atau 'berapa circuit'.", Array.Empty<string>());
    }
}

