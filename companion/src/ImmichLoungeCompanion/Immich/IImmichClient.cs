using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImmichLoungeCompanion.Immich.Dtos;
using ImmichLoungeCompanion.Models;

namespace ImmichLoungeCompanion.Immich;

public interface IImmichClient
{
    /// <summary>Tests connectivity; returns image/video counts on success.</summary>
    Task<(bool Ok, int ImageCount, int VideoCount, string? Error)> TestConnectionAsync(ImmichSettings settings);
    Task<List<ImmichAlbum>> GetAlbumsAsync(ImmichSettings settings);
    Task<List<ImmichPerson>> GetPeopleAsync(ImmichSettings settings);
    Task<List<ImmichTag>> GetTagsAsync(ImmichSettings settings);
    /// <summary>Fetches ALL pages for a single source. Applies date filter if provided.</summary>
    Task<List<ImmichAsset>> SearchAssetsAllPagesAsync(ImmichSettings settings, SearchAssetsRequest request);
    /// <summary>Fetches ALL pages of an Immich smart (ML text) search.</summary>
    Task<List<ImmichAsset>> SmartSearchAllPagesAsync(ImmichSettings settings, SmartSearchRequest request);
    Task<List<ImmichMemory>> GetMemoriesAsync(ImmichSettings settings, DateOnly date);
}
