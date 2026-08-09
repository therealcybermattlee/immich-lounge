---
icon: material/history
---

# Changelog

User-facing release notes for Immich Lounge live here.

For technical release details, see [CHANGELOG.md](https://github.com/immich-lounge/immich-lounge/blob/main/CHANGELOG.md).

## Unreleased

- Slideshows now rotate through the whole library properly instead of repeating the same first photos after refreshes.
- Profiles can limit photos to a date range or a rolling window (e.g. the last 12 months) from the new Date Filter section.
- Profiles can include smart searches ("beach sunset", "dog") as content sources, powered by Immich's machine-learning search.
- Videos and live photos can now play on the Roku channel and screensaver. Enable them per profile in the companion's Media section.
- Videos play to completion before the next slide. Sound is off by default and can be enabled with the new Video Audio toggle; the screensaver always plays videos muted.
- If a video fails to load, the slideshow shows its preview image and moves on instead of getting stuck.

## 2026-04-12

- Roku channel and screensaver version 1.0.0008.
- The Roku channel now stays open when a profile has no photos and shows a clearer message with the companion URL and profile-change hint.
- Weather, clock, and display setting changes now apply on Roku automatically after a short refresh, without reshuffling the current slideshow playlist.
- Playlist refreshes and resume behavior are more stable when playlists are refreshed or fetched in batches.
- Shared Immich albums are documented as not supported yet because Immich search does not reliably return assets from shared albums.

## 2026-03-30

- Initial Release
