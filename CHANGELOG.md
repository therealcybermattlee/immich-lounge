# Changelog

Technical release notes for Immich Lounge live here.

For a shorter user-facing summary, see [docs/website/changelog.md](./docs/website/changelog.md).

## Unreleased

- Fixed date-filtered playlists never building: Immich rejects bare yyyy-MM-dd values for takenAfter/takenBefore with HTTP 400, so date bounds are now sent as full ISO 8601 datetimes.
- Fixed slideshows replaying the same playlist head: shuffled playlist rebuilds now take a fresh shuffle whenever library content changed, instead of preserving a frozen order that every version-reset offset snapped back to. Unchanged content still keeps a stable order and playlist version.
- Added a Date Filter section to the profile editor exposing the existing range and rolling date filters, with new validation.
- Added Immich smart (machine-learning) text search as a content source: enter search terms in the profile editor and matching assets join the playlist. Uses POST /api/search/smart with all-page pagination and respects the profile date filter.
- Added video and live-photo playback on the Roku channel and screensaver: a dedicated Video node in PlaybackCanvas plays motion above the poster ring and below the overlay/clock/weather layers, and the slideshow advances when playback finishes instead of on the slide timer.
- Video playback authenticates with the Immich API key via ContentNode HTTP headers (never in URLs) and falls back to the asset's preview still with the normal slide interval on playback errors or a 20-second buffering watchdog timeout.
- The screensaver always plays videos muted; the channel follows the profile's Video Audio toggle (muted by default).
- Re-enabled the Videos, Live Photos, and Video Audio media-type toggles in the companion profile editor; editor saves no longer force these flags off.
- Fixed blurred background posters for video entries to load the asset thumbnail instead of the unplayable video stream URL.

## 2026-04-12

- Bumped Roku channel and screensaver manifests to version 1.0.0008.
- Kept the Roku channel open when the selected profile has no photos, with a clearer empty-state screen and profile-change hint.
- Added a Roku metadata-only profile refresh that periodically applies display, clock, and weather configuration changes without replacing the current playlist.
- Added playlist window versioning so Roku refreshes and batched playlist fetches can avoid reshuffling or jumping unexpectedly when the companion cache is still current.
- Improved Roku playlist resume state by tracking playlist version and preserving the currently displayed asset when possible after refresh.
- Documented that shared Immich albums are not currently supported because Immich search does not reliably return assets from shared albums.
- Kept shared-album retrieval helpers in the companion codebase, but disabled shared album listing until upstream Immich search support is available.

## 2026-03-30

- Initial Release
