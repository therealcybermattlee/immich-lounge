using ImmichLoungeCompanion.Models;

namespace ImmichLoungeCompanion.Services;

public class ProfileValidator : IProfileValidator
{
    public string? Validate(Profile profile)
    {
        if (!IsMediaTypesValid(profile.MediaTypes))
        {
            return "At least one of photos, videos, or livePhotos must be true.";
        }

        if (!IsTransitionEffectValid(profile.Slideshow.TransitionEffect))
        {
            return "Invalid transitionEffect. Must be fade, none, slide, zoom, or random.";
        }

        if (!IsPhotoMotionValid(profile.Slideshow.PhotoMotion))
        {
            return "Invalid photoMotion. Must be none or kenBurns.";
        }

        if (!IsBackgroundEffectValid(profile.Display.BackgroundEffect))
        {
            return "Invalid backgroundEffect. Must be none, blur, or ambilight.";
        }

        if (!IsWeatherUnitValid(profile.Display.WeatherUnit))
        {
            return "Invalid weatherUnit. Must be celsius or fahrenheit.";
        }

        if (!IsSlideshowValid(profile.Slideshow))
        {
            return "intervalSeconds must be 3–3600; refreshIntervalMinutes must be 5–1440.";
        }

        if (!IsImageQualityValid(profile.ImageQuality))
        {
            return "Invalid imageQuality. Must be preview or original.";
        }

        if (!IsQualityValid(profile.Quality))
        {
            return "Minimum file size must be 0 KB or greater.";
        }

        var dateFilterError = ValidateDateFilter(profile.DateFilter);
        if (dateFilterError != null)
        {
            return dateFilterError;
        }

        return profile.ValidateAssetFilter();
    }

    private static string? ValidateDateFilter(DateFilter? filter)
    {
        if (filter == null)
        {
            return null;
        }

        if (filter.Type == "range")
        {
            if (string.IsNullOrWhiteSpace(filter.From) && string.IsNullOrWhiteSpace(filter.To))
            {
                return "Date filter range needs a from or to date.";
            }

            if (!IsIsoDateOrEmpty(filter.From) || !IsIsoDateOrEmpty(filter.To))
            {
                return "Date filter dates must use the yyyy-MM-dd format.";
            }

            if (System.DateOnly.TryParse(filter.From, out var from) &&
                System.DateOnly.TryParse(filter.To, out var to) &&
                from > to)
            {
                return "Date filter from date must not be after the to date.";
            }

            return null;
        }

        if (filter.Type == "rolling")
        {
            if (filter.Amount is not >= 1)
            {
                return "Rolling date filter amount must be 1 or greater.";
            }

            return filter.Unit is "days" or "weeks" or "months" or "years"
                ? null
                : "Rolling date filter unit must be days, weeks, months, or years.";
        }

        return "Invalid dateFilter type. Must be range or rolling.";
    }

    private static bool IsIsoDateOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        System.DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);

    private static bool IsMediaTypesValid(MediaTypes m) => m.Photos || m.Videos || m.LivePhotos;
    private static bool IsTransitionEffectValid(string e) =>
        e is "fade" or "none" or "slide" or "zoom" or "random";
    private static bool IsPhotoMotionValid(string e) => e is "none" or "kenBurns";
    private static bool IsBackgroundEffectValid(string e) => e is "none" or "blur" or "ambilight";
    private static bool IsWeatherUnitValid(string unit) => unit is "celsius" or "fahrenheit";
    private static bool IsImageQualityValid(string q) => q is "preview" or "original";
    private static bool IsQualityValid(QualitySettings quality) =>
        quality.MinFileSizeKb is null or >= 0;
    private static bool IsSlideshowValid(SlideshowSettings s) =>
        s.IntervalSeconds is >= 3 and <= 3600 &&
        s.RefreshIntervalMinutes is >= 5 and <= 1440;
}
