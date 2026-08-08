' MediaUrls.brs - Immich asset URL helpers

' Build Immich media URL from playlist entry and profile.
' Authentication uses the x-api-key header set via SetHeaders() on each Poster node
' in ApplyProfile(), or via ContentNode HttpHeaders for video playback
' (BuildVideoContentFields) - the API key is never embedded in URLs.
function BuildMediaUrl(entry as Object, profile as Object) as String
    if profile.immich = invalid then return ""
    base = profile.immich.serverUrl
    quality = "preview"
    if profile.imageQuality <> invalid then quality = profile.imageQuality

    if entry.type = "video" or entry.type = "livePhoto" then
        videoId = entry.id
        if entry.type = "livePhoto" and entry.livePhotoVideoId <> invalid then
            videoId = entry.livePhotoVideoId
        end if
        return base + "/api/assets/" + videoId + "/video/playback"
    end if

    if quality = "original" then
        return base + "/api/assets/" + entry.id + "/original"
    end if
    return base + "/api/assets/" + entry.id + "/thumbnail?size=preview"
end function

' Build the still-image URL for an entry, for use by Poster nodes.
' Video and live-photo entries render their Immich-generated preview still on the
' poster ring; the motion itself plays through the Video node (PlaybackVideo.brs).
function BuildStillMediaUrl(entry as Object, profile as Object) as String
    if profile.immich = invalid then return ""
    if entry.type = "video" or entry.type = "livePhoto" then
        return profile.immich.serverUrl + "/api/assets/" + entry.id + "/thumbnail?size=preview"
    end if
    return BuildMediaUrl(entry, profile)
end function

function BuildBackgroundMediaUrl(entry as Object, profile as Object) as String
    if profile.immich = invalid then return ""
    ' entry.id is always the displayable asset (for live photos, the still),
    ' so the blur background uses its thumbnail for every entry type.
    return profile.immich.serverUrl + "/api/assets/" + entry.id + "/thumbnail?size=thumbnail"
end function

' Choose the Video node streamFormat for a playback URL. Immich transcodes to
' progressive H.264 MP4 by default; HLS manifests are detected by extension.
function GetVideoStreamFormat(url as String) as String
    if Instr(1, LCase(url), ".m3u8") > 0 then return "hls"
    return "mp4"
end function

' Build the ContentNode field values for video playback. The API key rides in
' an HttpHeaders entry - never in the URL - so it stays out of logs.
function BuildVideoContentFields(entry as Object, profile as Object) as Object
    url = BuildMediaUrl(entry, profile)
    fields = {
        url: url
        streamFormat: GetVideoStreamFormat(url)
    }
    if profile.immich <> invalid and profile.immich.apiKey <> invalid and profile.immich.apiKey <> "" then
        fields.httpHeaders = ["x-api-key: " + profile.immich.apiKey]
    end if
    if LCase(Left(url, 8)) = "https://" then
        fields.httpCertificatesFile = "common:/certs/ca-bundle.crt"
    end if
    return fields
end function
