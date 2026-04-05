using ImmichLoungeCompanion.Models;

namespace ImmichLoungeCompanion.Services;

public interface IImmichSettingsValidator
{
    string? Validate(ImmichSettings? settings);
}
