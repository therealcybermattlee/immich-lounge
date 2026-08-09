' PlaybackVideo.brs - shared video/live-photo playback helpers
'
' Poster-under-video design: video and live-photo entries commit their Immich
' preview still through the normal poster ring, then the shared Video node in
' PlaybackCanvas plays the motion on top of it. When playback finishes the
' slideshow advances; on any failure (error state or buffering watchdog) the
' already-visible still takes over with the regular slide interval so the
' slideshow never hangs.

sub StartVideoForEntryForScene(ctx as Object, entry as Object)
    if ctx.videoPlayer = invalid then return
    if ctx.profile = invalid then return

    fields = BuildVideoContentFields(entry, ctx.profile)
    if fields.url = "" then
        HandleVideoFailureForScene(ctx, "no playback url")
        return
    end if

    content = CreateObject("roSGNode", "ContentNode")
    content.url = fields.url
    content.streamFormat = fields.streamFormat
    if fields.httpHeaders <> invalid then content.HttpHeaders = fields.httpHeaders
    if fields.httpCertificatesFile <> invalid then content.HttpCertificatesFile = fields.httpCertificatesFile

    ctx.videoActive = true
    ctx.videoEntryId = ValueOrDefault(entry.id, "")
    if ctx.progressBar <> invalid then ctx.progressBar.visible = false

    player = ctx.videoPlayer

    ' Match the video surface to the committed still's fitted layout so
    ' portrait and non-16:9 videos keep their aspect ratio (pillarboxed like
    ' the still) instead of stretching to fill the screen.
    still = ctx.mainPosters[ctx.ringCurrent]
    if still <> invalid and still.width > 0 and still.height > 0 then
        player.width = still.width
        player.height = still.height
        player.translation = still.translation
    else
        player.width = 1920
        player.height = 1080
        player.translation = [0, 0]
    end if
    player.observeField("state", "OnVideoState")
    player.content = content
    player.mute = GetVideoMuteForScene(ctx)
    player.loop = false
    ctx.videoWatchdogTimer.control = "stop"
    ctx.videoWatchdogTimer.control = "start"
    player.control = "play"
    LogDebug(PlaybackLogScope(ctx), "StartVideo id=" + ctx.videoEntryId + " format=" + fields.streamFormat)
end sub

' Screensavers always play muted; the channel follows the profile's videoAudio toggle.
function GetVideoMuteForScene(ctx as Object) as Boolean
    if ctx.top.isScreensaver then return true
    if ctx.profile <> invalid and ctx.profile.mediaTypes <> invalid then
        return not ValueOrDefault(ctx.profile.mediaTypes.videoAudio, false)
    end if
    return true
end function

sub StopVideoForScene(ctx as Object)
    if ctx.videoPlayer = invalid then return
    ctx.videoWatchdogTimer.control = "stop"
    if not ctx.videoActive then return
    ctx.videoActive = false
    ctx.videoEntryId = ""
    player = ctx.videoPlayer
    player.unobserveField("state")
    player.control = "stop"
    player.visible = false
    player.content = invalid
end sub

sub OnVideoStateForScene(ctx as Object)
    if not ctx.videoActive then return
    state = ctx.videoPlayer.state
    LogDebug(PlaybackLogScope(ctx), "VideoState=" + state + " id=" + ctx.videoEntryId)

    if state = "playing" then
        ctx.videoPlayer.visible = true
        ctx.videoWatchdogTimer.control = "stop"
    else if state = "buffering" then
        ctx.videoWatchdogTimer.control = "stop"
        ctx.videoWatchdogTimer.control = "start"
    else if state = "finished" then
        StopVideoForScene(ctx)
        if not ctx.paused then AdvanceToNextForScene(ctx)
    else if state = "error" then
        errorCode = ctx.videoPlayer.errorCode
        HandleVideoFailureForScene(ctx, "error code=" + errorCode.ToStr())
    end if
end sub

sub OnVideoWatchdogForScene(ctx as Object)
    if not ctx.videoActive then return
    if ctx.paused then return
    if ctx.videoPlayer.state = "playing" then return
    HandleVideoFailureForScene(ctx, "buffering timeout")
end sub

' Fall back to the entry's committed still and keep the slideshow moving with
' the regular photo interval.
sub HandleVideoFailureForScene(ctx as Object, reason as String)
    LogDebug(PlaybackLogScope(ctx), "Video playback failed (" + reason + ") id=" + ctx.videoEntryId + " - falling back to still")
    StopVideoForScene(ctx)
    if ctx.progressBar <> invalid then ctx.progressBar.visible = ctx.showTimer
    if ctx.paused then return
    ctx.slideStartTime = UpTime(0)
    ctx.slideDuration = ctx.intervalSeconds
    ctx.slideTimer.duration = ctx.intervalSeconds
    ctx.slideTimer.control = "start"
    ctx.progressTimer.control = "start"
end sub

sub PauseVideoForScene(ctx as Object)
    if not ctx.videoActive then return
    ctx.videoWatchdogTimer.control = "stop"
    ctx.videoPlayer.control = "pause"
end sub

sub ResumeVideoForScene(ctx as Object)
    if not ctx.videoActive then return
    ctx.videoPlayer.control = "resume"
    if ctx.videoPlayer.state <> "playing" then
        ctx.videoWatchdogTimer.control = "stop"
        ctx.videoWatchdogTimer.control = "start"
    end if
end sub
