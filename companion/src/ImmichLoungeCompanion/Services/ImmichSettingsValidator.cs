using System;
using System.Net;
using ImmichLoungeCompanion.Models;

namespace ImmichLoungeCompanion.Services;

public class ImmichSettingsValidator : IImmichSettingsValidator
{
    public string? Validate(ImmichSettings? settings)
    {
        if (settings == null)
        {
            return "Immich settings are required.";
        }

        var serverUrl = settings.ServerUrl?.Trim() ?? "";
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri) ||
            uri is not { Scheme: "http" or "https" } ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return "Immich server URL must be a valid http or https URL.";
        }

        if (IsBlockedHost(uri.Host))
        {
            return "Immich server URL must use a LAN-reachable host or IP. Do not use localhost, loopback, or wildcard addresses.";
        }

        return null;
    }

    private static bool IsBlockedHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var ip))
        {
            return false;
        }

        return IPAddress.IsLoopback(ip) || IsWildcard(ip);
    }

    private static bool IsWildcard(IPAddress ip) =>
        ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any);
}
