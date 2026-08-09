using System.Collections.Generic;

namespace ImmichLoungeCompanion.Immich.Dtos;

public class SearchAssetsRequest
{
    public List<string>? AlbumIds { get; set; }
    public List<string>? PersonIds { get; set; }
    public List<string>? TagIds { get; set; }
    public string? TakenAfter { get; set; }         // ISO 8601 date
    public string? TakenBefore { get; set; }
    public string? Type { get; set; }               // "IMAGE" | "VIDEO" (optional filter)
    // Timeline-visible assets only. Without this, search also returns hidden
    // assets - notably live-photo motion clips, which have no thumbnails and
    // must never appear as standalone playlist entries.
    public string? Visibility { get; set; } = "timeline";
    public bool WithExif { get; set; } = true;
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 1000;
}
