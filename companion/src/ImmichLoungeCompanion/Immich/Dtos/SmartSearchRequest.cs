namespace ImmichLoungeCompanion.Immich.Dtos;

// POST /api/search/smart — Immich ML text search ("beach", "dog", ...).
// Response shape matches SearchAssetsResponse.
public class SmartSearchRequest
{
    public string Query { get; set; } = "";
    public string? TakenAfter { get; set; }         // ISO 8601 date
    public string? TakenBefore { get; set; }
    public string? Visibility { get; set; } = "timeline";
    public bool WithExif { get; set; } = true;
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 1000;
}
