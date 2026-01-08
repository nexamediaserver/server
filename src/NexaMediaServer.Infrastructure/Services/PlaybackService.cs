// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Playback;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

using IO = System.IO;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Default playback orchestration service (skeleton implementation).
/// </summary>
public partial class PlaybackService : IPlaybackService
{
    private readonly IBifService bifService;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IFfmpegCapabilities ffmpegCapabilities;
    private readonly IGopIndexService gopIndexService;
    private readonly ILogger<PlaybackService> logger;
    private readonly IApplicationPaths paths;
    private readonly TranscodeOptions transcodeOptions;

    private readonly Action<ILogger, string, Exception?> logDashDeleteFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(logDashDeleteFailed)),
            "Failed to delete DASH cache at {Directory}"
        );

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating DbContext instances.</param>
    /// <param name="bifService">BIF service for trickplay discovery.</param>
    /// <param name="ffmpegCapabilities">FFmpeg capabilities for hardware acceleration checks.</param>
    /// <param name="gopIndexService">GoP index service for seek optimization.</param>
    /// <param name="logger">Typed logger.</param>
    /// <param name="transcodeOptions">Transcode configuration binding.</param>
    /// <param name="paths">Application path provider.</param>
    public PlaybackService(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IBifService bifService,
        IFfmpegCapabilities ffmpegCapabilities,
        IGopIndexService gopIndexService,
        ILogger<PlaybackService> logger,
        IOptions<TranscodeOptions> transcodeOptions,
        IApplicationPaths paths
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.bifService = bifService;
        this.ffmpegCapabilities = ffmpegCapabilities;
        this.gopIndexService = gopIndexService;
        this.logger = logger;
        this.paths = paths;
        this.transcodeOptions = transcodeOptions.Value;
    }

    /// <inheritdoc />
    public async Task<CapabilityProfile> UpsertCapabilityProfileAsync(
        int sessionId,
        CapabilityProfileInput input,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var latest = await db
            .CapabilityProfiles.Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        int nextVersion = input.Version ?? (latest?.Version + 1 ?? 1);

        // Try to find an existing profile with the target version
        var profile = await db
            .CapabilityProfiles.Where(p => p.SessionId == sessionId && p.Version == nextVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            // Only create a new profile if one doesn't exist with this version
            profile = new CapabilityProfile { SessionId = sessionId, Version = nextVersion };
            await db.CapabilityProfiles.AddAsync(profile, cancellationToken);
        }

        profile.DeviceId = input.DeviceId;
        profile.Name = input.Name;
        profile.Capabilities = input.Capabilities ?? this.CreateDefaultCapabilities();
        profile.DeclaredAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <inheritdoc />
    public async Task<PlaybackStartResponse> StartPlaybackAsync(
        int sessionId,
        PlaybackStartRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var profile = await db
            .CapabilityProfiles.Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            profile = new CapabilityProfile
            {
                SessionId = sessionId,
                Version = 1,
                Capabilities = this.CreateDefaultCapabilities(),
                DeclaredAt = DateTime.UtcNow,
            };
            await db.CapabilityProfiles.AddAsync(profile, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var metadata = await db
            .MetadataItems.Include(m => m.Parent)
            .FirstOrDefaultAsync(m => m.Id == request.MetadataItemId, cancellationToken);

        if (metadata == null)
        {
            throw new InvalidOperationException("Metadata item not found for playback start.");
        }

        // Resolve the first playable item if the requested item is a container
        var (playableItem, originator) = await ResolvePlayableItemAsync(
            db,
            metadata,
            request.OriginatorMetadataItemId,
            cancellationToken
        );

        // Determine the starting index for container-based playlists
        int startIndex = 0;
        if (originator != null)
        {
            // Find the index of the playable item within the originator's children
            var siblings = await db
                .MetadataItems.Where(m =>
                    m.ParentId == originator.Id && m.DeletedAt == null
                )
                .OrderBy(m => m.Index ?? int.MaxValue)
                .ThenBy(m => m.Title)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            startIndex = siblings.IndexOf(playableItem.Id);
            if (startIndex < 0)
            {
                startIndex = 0;
            }
        }

        var playbackSession = new PlaybackSession
        {
            SessionId = sessionId,
            CapabilityProfile = profile,
            CapabilityProfileId = profile.Id,
            CurrentMetadataItem = playableItem,
            CurrentMetadataItemId = playableItem.Id,
            Originator = request.Originator,
            LastHeartbeatAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays),
            State = "playing",
        };

        PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
            db,
            playableItem.Id,
            profile,
            cancellationToken
        );

        playbackSession.CurrentMediaPartId = plan.MediaPartId;

        // Create the playlist seed based on the request
        // Use the resolved originator for container-based playlists
        var seed = new PlaylistSeed
        {
            Type = request.PlaylistType,
            OriginatorId = originator?.Id ?? playableItem.Id,
            StartIndex = startIndex,
            Shuffle = request.Shuffle,
            Repeat = request.Repeat,
        };

        var generator = new PlaylistGenerator
        {
            PlaybackSession = playbackSession,
            SeedJson = JsonSerializer.Serialize(seed),
            Cursor = startIndex,
            Shuffle = request.Shuffle,
            Repeat = request.Repeat,
            ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays),
            ChunkSize = PlaybackDefaults.PlaylistChunkSize,
        };

        // Materialize the initial playlist item
        var initialItem = new PlaylistGeneratorItem
        {
            PlaylistGenerator = generator,
            MetadataItemId = playableItem.Id,
            SortOrder = startIndex,
        };

        await db.PlaybackSessions.AddAsync(playbackSession, cancellationToken);
        await db.PlaylistGenerators.AddAsync(generator, cancellationToken);
        await db.PlaylistGeneratorItems.AddAsync(initialItem, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Load the playable metadata with parent info for client display
        var playableMetadata = await db
            .MetadataItems.Include(m => m.Parent)
                .ThenInclude(p => p!.Parent)
            .FirstAsync(m => m.Id == playableItem.Id, cancellationToken);

        // Calculate total count for the playlist
        int totalCount = await GetPlaylistTotalCountAsync(db, seed, cancellationToken);

        return new PlaybackStartResponse
        {
            CurrentMetadataItemId = playableMetadata.Uuid,
            CurrentMetadataItemUuid = playableMetadata.Uuid,
            CurrentItemMetadataType = playableMetadata.MetadataType,
            CurrentItemOriginalTitle = playableMetadata.OriginalTitle,
            CurrentItemParentThumbUrl = playableMetadata.Parent?.ThumbUri,
            CurrentItemParentTitle = ResolveParentTitle(playableMetadata),
            CurrentItemThumbUrl = playableMetadata.ThumbUri,
            CurrentItemTitle = playableMetadata.Title,
            PlaybackSessionId = playbackSession.PlaybackSessionId,
            PlaylistGeneratorId = generator.PlaylistGeneratorId,
            CapabilityProfileVersion = profile.Version,
            StreamPlanJson = plan.StreamPlanJson,
            PlaybackUrl = plan.PlaybackUrl,
            TrickplayUrl = plan.TrickplayUrl,
            DurationMs = plan.DurationMs,
            PlaylistIndex = startIndex,
            PlaylistTotalCount = totalCount,
            Shuffle = request.Shuffle,
            Repeat = request.Repeat,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackHeartbeatResponse> HeartbeatAsync(
        int sessionId,
        PlaybackHeartbeatRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.CapabilityProfile)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            LogMissingPlaybackSession(this.logger, request.PlaybackSessionId);
            throw new InvalidOperationException(
                $"Playback session {request.PlaybackSessionId} not found."
            );
        }

        playback.PlayheadMs = request.PlayheadMs;
        playback.State = request.State;
        playback.LastHeartbeatAt = DateTime.UtcNow;
        playback.ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);
        if (request.MediaPartId.HasValue)
        {
            playback.CurrentMediaPartId = request.MediaPartId;
        }

        await this.UpdateProgressAsync(db, playback, request.PlayheadMs, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        // Return the capability version from the already-loaded profile
        return new PlaybackHeartbeatResponse
        {
            PlaybackSessionId = request.PlaybackSessionId,
            CapabilityProfileVersion = playback.CapabilityProfile.Version,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackDecisionResponse> DecideAsync(
        int sessionId,
        PlaybackDecisionRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.CapabilityProfile)
            .Include(p => p.CurrentMetadataItem)
            .Include(p => p.PlaylistGenerator)
            .ThenInclude(g => g!.Items)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            throw new InvalidOperationException("Playback session not found for decision.");
        }

        playback.PlayheadMs = request.ProgressMs;
        playback.LastHeartbeatAt = DateTime.UtcNow;
        playback.ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);

        // For items that don't track progress (photos, tracks, clips, etc.),
        // increment view count on any playback decision, but only once per item view
        var currentMetadataType = await db
            .MetadataItems.AsNoTracking()
            .Where(m => m.Id == playback.CurrentMetadataItemId)
            .Select(m => m.MetadataType)
            .FirstOrDefaultAsync(cancellationToken);

        if (!ShouldTrackProgress(currentMetadataType))
        {
            await this.UpdateViewCountForNonProgressItemAsync(db, playback, cancellationToken);
        }

        // Handle explicit navigation requests (next/previous)
        if (string.Equals(request.Status, "next", StringComparison.OrdinalIgnoreCase))
        {
            var generator = playback.PlaylistGenerator;
            if (generator != null)
            {
                // Reset ViewOffset for the current item if it's a non-progress type
                // This allows it to be counted again if we navigate back to it later
                if (!ShouldTrackProgress(currentMetadataType))
                {
                    var currentSetting = await db.MetadataItemSettings.FirstOrDefaultAsync(
                        s => s.UserId == playback.Session.UserId && s.MetadataItemId == playback.CurrentMetadataItemId,
                        cancellationToken
                    );
                    if (currentSetting != null)
                    {
                        currentSetting.ViewOffset = 0;
                    }
                }

                // Mark current item as served
                var currentItem = generator.Items.FirstOrDefault(i => i.SortOrder == generator.Cursor);
                if (currentItem != null)
                {
                    currentItem.Served = true;
                }

                var nextItem = await GetNextPlaylistItemAsync(db, generator, cancellationToken);
                if (nextItem != null)
                {
                    generator.Cursor++;
                    playback.CurrentMetadataItemId = nextItem.MetadataItemId;
                    playback.CurrentMetadataItem = (await db
                        .MetadataItems.FirstOrDefaultAsync(
                            m => m.Id == nextItem.MetadataItemId,
                            cancellationToken
                        ))!;
                    playback.PlayheadMs = 0;

                    PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
                        db,
                        nextItem.MetadataItemId,
                        playback.CapabilityProfile,
                        cancellationToken
                    );

                    playback.CurrentMediaPartId = plan.MediaPartId;
                    await db.SaveChangesAsync(cancellationToken);

                    // Load parent for album/parent title
                    var nextMetadata = await db.MetadataItems
                        .Include(m => m.Parent)
                            .ThenInclude(p => p!.Parent)
                        .FirstOrDefaultAsync(m => m.Id == nextItem.MetadataItemId, cancellationToken);

                    return new PlaybackDecisionResponse
                    {
                        Action = "next",
                        StreamPlanJson = plan.StreamPlanJson,
                        NextMetadataItemId = nextItem.MetadataItemId,
                        NextMetadataItemUuid = nextMetadata?.Uuid,
                        NextItemTitle = nextMetadata?.Title,
                        NextItemOriginalTitle = nextMetadata?.OriginalTitle,
                        NextItemParentTitle = ResolveParentTitle(nextMetadata),
                        NextItemThumbUrl = nextMetadata?.ThumbUri,
                        PlaybackUrl = plan.PlaybackUrl,
                        TrickplayUrl = plan.TrickplayUrl,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }

                // No next item, return stop
                await db.SaveChangesAsync(cancellationToken);
                return new PlaybackDecisionResponse
                {
                    Action = "stop",
                    StreamPlanJson = "{}",
                    NextMetadataItemId = null,
                    NextMetadataItemUuid = null,
                    PlaybackUrl = string.Empty,
                    TrickplayUrl = null,
                    CapabilityProfileVersion = playback.CapabilityProfile.Version,
                };
            }
        }

        if (string.Equals(request.Status, "previous", StringComparison.OrdinalIgnoreCase))
        {
            var generator = playback.PlaylistGenerator;
            if (generator != null)
            {
                // Reset ViewOffset for the current item if it's a non-progress type
                // This allows it to be counted again if we navigate back to it later
                if (!ShouldTrackProgress(currentMetadataType))
                {
                    var currentSetting = await db.MetadataItemSettings.FirstOrDefaultAsync(
                        s => s.UserId == playback.Session.UserId && s.MetadataItemId == playback.CurrentMetadataItemId,
                        cancellationToken
                    );
                    if (currentSetting != null)
                    {
                        currentSetting.ViewOffset = 0;
                    }
                }

                var previousItem = GetPreviousPlaylistItem(generator);
                if (previousItem != null)
                {
                    generator.Cursor--;
                    playback.CurrentMetadataItemId = previousItem.MetadataItemId;
                    playback.CurrentMetadataItem = (await db
                        .MetadataItems.FirstOrDefaultAsync(
                            m => m.Id == previousItem.MetadataItemId,
                            cancellationToken
                        ))!;
                    playback.PlayheadMs = 0;

                    PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
                        db,
                        previousItem.MetadataItemId,
                        playback.CapabilityProfile,
                        cancellationToken
                    );

                    playback.CurrentMediaPartId = plan.MediaPartId;
                    await db.SaveChangesAsync(cancellationToken);

                    // Load parent for album/parent title
                    var prevMetadata = await db.MetadataItems
                        .Include(m => m.Parent)
                            .ThenInclude(p => p!.Parent)
                        .FirstOrDefaultAsync(m => m.Id == previousItem.MetadataItemId, cancellationToken);

                    return new PlaybackDecisionResponse
                    {
                        Action = "previous",
                        StreamPlanJson = plan.StreamPlanJson,
                        NextMetadataItemId = previousItem.MetadataItemId,
                        NextMetadataItemUuid = prevMetadata?.Uuid,
                        NextItemTitle = prevMetadata?.Title,
                        NextItemOriginalTitle = prevMetadata?.OriginalTitle,
                        NextItemParentTitle = ResolveParentTitle(prevMetadata),
                        NextItemThumbUrl = prevMetadata?.ThumbUri,
                        PlaybackUrl = plan.PlaybackUrl,
                        TrickplayUrl = plan.TrickplayUrl,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }

                // No previous item, stay on current
                await db.SaveChangesAsync(cancellationToken);
                return new PlaybackDecisionResponse
                {
                    Action = "stay",
                    StreamPlanJson = "{}",
                    NextMetadataItemId = playback.CurrentMetadataItemId,
                    NextMetadataItemUuid = playback.CurrentMetadataItem?.Uuid,
                    PlaybackUrl = string.Empty,
                    TrickplayUrl = null,
                    CapabilityProfileVersion = playback.CapabilityProfile.Version,
                };
            }
        }

        if (string.Equals(request.Status, "jump", StringComparison.OrdinalIgnoreCase) && request.JumpIndex.HasValue)
        {
            var generator = playback.PlaylistGenerator;
            if (generator != null)
            {
                int targetIndex = request.JumpIndex.Value;

                // Validate the target index
                int totalCount = generator.Items.Count;
                if (targetIndex < 0 || targetIndex >= totalCount)
                {
                    return new PlaybackDecisionResponse
                    {
                        Action = "stay",
                        StreamPlanJson = "{}",
                        NextMetadataItemId = playback.CurrentMetadataItemId,
                        NextMetadataItemUuid = playback.CurrentMetadataItem?.Uuid,
                        PlaybackUrl = string.Empty,
                        TrickplayUrl = null,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }

                // Reset ViewOffset for the current item if it's a non-progress type
                if (!ShouldTrackProgress(currentMetadataType))
                {
                    var currentSetting = await db.MetadataItemSettings.FirstOrDefaultAsync(
                        s => s.UserId == playback.Session.UserId && s.MetadataItemId == playback.CurrentMetadataItemId,
                        cancellationToken
                    );
                    if (currentSetting != null)
                    {
                        currentSetting.ViewOffset = 0;
                    }
                }

                // Find the item at the target index
                var targetItem = generator.Items.FirstOrDefault(i => i.SortOrder == targetIndex);
                if (targetItem != null)
                {
                    // Update cursor to the target index
                    generator.Cursor = targetIndex;
                    playback.CurrentMetadataItemId = targetItem.MetadataItemId;
                    playback.CurrentMetadataItem = (await db
                        .MetadataItems.FirstOrDefaultAsync(
                            m => m.Id == targetItem.MetadataItemId,
                            cancellationToken
                        ))!;
                    playback.PlayheadMs = 0;

                    PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
                        db,
                        targetItem.MetadataItemId,
                        playback.CapabilityProfile,
                        cancellationToken
                    );

                    playback.CurrentMediaPartId = plan.MediaPartId;
                    await db.SaveChangesAsync(cancellationToken);

                    // Load parent for album/parent title
                    var targetMetadata = await db.MetadataItems
                        .Include(m => m.Parent)
                            .ThenInclude(p => p!.Parent)
                        .FirstOrDefaultAsync(m => m.Id == targetItem.MetadataItemId, cancellationToken);

                    return new PlaybackDecisionResponse
                    {
                        Action = "jump",
                        StreamPlanJson = plan.StreamPlanJson,
                        NextMetadataItemId = targetItem.MetadataItemId,
                        NextMetadataItemUuid = targetMetadata?.Uuid,
                        NextItemTitle = targetMetadata?.Title,
                        NextItemOriginalTitle = targetMetadata?.OriginalTitle,
                        NextItemParentTitle = ResolveParentTitle(targetMetadata),
                        NextItemThumbUrl = targetMetadata?.ThumbUri,
                        PlaybackUrl = plan.PlaybackUrl,
                        TrickplayUrl = plan.TrickplayUrl,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }

                // Target item not found, stay on current
                await db.SaveChangesAsync(cancellationToken);
                return new PlaybackDecisionResponse
                {
                    Action = "stay",
                    StreamPlanJson = "{}",
                    NextMetadataItemId = playback.CurrentMetadataItemId,
                    NextMetadataItemUuid = playback.CurrentMetadataItem?.Uuid,
                    PlaybackUrl = string.Empty,
                    TrickplayUrl = null,
                    CapabilityProfileVersion = playback.CapabilityProfile.Version,
                };
            }
        }

        if (string.Equals(request.Status, "ended", StringComparison.OrdinalIgnoreCase))
        {
            await this.UpdateViewCountAsync(db, playback, cancellationToken);

            // Mark current item as served and try to advance to next item
            var generator = playback.PlaylistGenerator;
            if (generator != null)
            {
                // Mark current item as served
                var currentItem = generator.Items.FirstOrDefault(i => i.SortOrder == generator.Cursor);
                if (currentItem != null)
                {
                    currentItem.Served = true;
                }

                // Try to get the next item
                var nextItem = await GetNextPlaylistItemAsync(db, generator, cancellationToken);
                if (nextItem != null)
                {
                    // Advance cursor and update current metadata item
                    generator.Cursor++;
                    playback.CurrentMetadataItemId = nextItem.MetadataItemId;
                    playback.CurrentMetadataItem = (await db
                        .MetadataItems.FirstOrDefaultAsync(
                            m => m.Id == nextItem.MetadataItemId,
                            cancellationToken
                        ))!;
                    playback.PlayheadMs = 0;

                    // Build stream plan for the next item
                    PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
                        db,
                        nextItem.MetadataItemId,
                        playback.CapabilityProfile,
                        cancellationToken
                    );

                    playback.CurrentMediaPartId = plan.MediaPartId;
                    await db.SaveChangesAsync(cancellationToken);

                    // Load parent for album/parent title
                    var nextMetadata = await db.MetadataItems
                        .Include(m => m.Parent)
                            .ThenInclude(p => p!.Parent)
                        .FirstOrDefaultAsync(m => m.Id == nextItem.MetadataItemId, cancellationToken);

                    return new PlaybackDecisionResponse
                    {
                        Action = "next",
                        StreamPlanJson = plan.StreamPlanJson,
                        NextMetadataItemId = nextItem.MetadataItemId,
                        NextMetadataItemUuid = nextMetadata?.Uuid,
                        NextItemTitle = nextMetadata?.Title,
                        NextItemOriginalTitle = nextMetadata?.OriginalTitle,
                        NextItemParentTitle = ResolveParentTitle(nextMetadata),
                        NextItemThumbUrl = nextMetadata?.ThumbUri,
                        PlaybackUrl = plan.PlaybackUrl,
                        TrickplayUrl = plan.TrickplayUrl,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }
                else
                {
                    // End of playlist
                    await db.SaveChangesAsync(cancellationToken);

                    return new PlaybackDecisionResponse
                    {
                        Action = "stop",
                        StreamPlanJson = "{}",
                        NextMetadataItemId = null,
                        NextMetadataItemUuid = null,
                        PlaybackUrl = string.Empty,
                        TrickplayUrl = null,
                        CapabilityProfileVersion = playback.CapabilityProfile.Version,
                    };
                }
            }
        }

        // Build stream plan for the current/next item
        PlaybackPlan currentPlan = await this.BuildPlaybackPlanAsync(
            db,
            playback.CurrentMetadataItemId,
            playback.CapabilityProfile,
            cancellationToken
        );

        playback.CurrentMediaPartId = currentPlan.MediaPartId;

        // Persist all changes including CurrentMediaPartId update
        await db.SaveChangesAsync(cancellationToken);

        return new PlaybackDecisionResponse
        {
            Action = "continue",
            StreamPlanJson = currentPlan.StreamPlanJson,
            NextMetadataItemId = playback.CurrentMetadataItemId,
            NextMetadataItemUuid = playback.CurrentMetadataItem?.Uuid,
            PlaybackUrl = currentPlan.PlaybackUrl,
            TrickplayUrl = currentPlan.TrickplayUrl,
            CapabilityProfileVersion = playback.CapabilityProfile.Version,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackResumeResponse?> ResumeAsync(
        int sessionId,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.PlaylistGenerator)
            .Include(p => p.CurrentMetadataItem)
            .Include(p => p.CapabilityProfile)
            .Include(p => p.CurrentMediaPart)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == playbackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            return null;
        }

        if (playback.CurrentMetadataItem == null)
        {
            return null;
        }

        if (playback.CapabilityProfile == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (
            playback.ExpiresAt <= now
            || (playback.PlaylistGenerator != null && playback.PlaylistGenerator.ExpiresAt <= now)
        )
        {
            await this.RemovePlaybackSessionAsync(
                db,
                playback,
                deleteDashCache: true,
                cancellationToken
            );
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        var capabilityProfile = playback.CapabilityProfile;

        PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
            db,
            playback.CurrentMetadataItem.Id,
            capabilityProfile,
            cancellationToken
        );

        playback.CurrentMediaPartId = plan.MediaPartId;
        playback.LastHeartbeatAt = now;
        playback.ExpiresAt = now.AddDays(PlaybackDefaults.ExpiryDays);

        await db.SaveChangesAsync(cancellationToken);

        // Return all needed data to avoid redundant queries in the GraphQL layer
        return new PlaybackResumeResponse
        {
            Session = playback,
            CurrentMetadataItemUuid = playback.CurrentMetadataItem?.Uuid ?? Guid.Empty,
            PlaylistGeneratorId = playback.PlaylistGenerator?.PlaylistGeneratorId ?? Guid.Empty,
            CapabilityProfileVersion = capabilityProfile.Version,
            StreamPlanJson = plan.StreamPlanJson,
            PlaybackUrl = plan.PlaybackUrl,
            TrickplayUrl = plan.TrickplayUrl,
            DurationMs = plan.DurationMs,
            PlayheadMs = playback.PlayheadMs,
            State = playback.State,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackSeekResponse> SeekAsync(
        int sessionId,
        PlaybackSeekRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Verify the playback session belongs to this session
        var playback = await db
            .PlaybackSessions.Include(p => p.CurrentMetadataItem)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            throw new InvalidOperationException(
                $"Playback session {request.PlaybackSessionId} not found or does not belong to session {sessionId}."
            );
        }

        // Lookup the media part to get the metadata UUID and part index
        var mediaPart = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == request.MediaPartId, cancellationToken);

        if (mediaPart?.MediaItem?.MetadataItem == null)
        {
            // No metadata, return the original target as fallback
            return new PlaybackSeekResponse
            {
                KeyframeMs = request.TargetMs,
                GopDurationMs = 0,
                HasGopIndex = false,
                OriginalTargetMs = request.TargetMs,
            };
        }

        var metadataUuid = mediaPart.MediaItem.MetadataItem.Uuid;

        // Determine part index
        int partIndex = await this.ResolvePartIndexAsync(db, mediaPart, cancellationToken);

        // Find nearest keyframe using GoP index
        var keyframe = await this.gopIndexService.GetNearestKeyframeAsync(
            metadataUuid,
            partIndex,
            request.TargetMs,
            cancellationToken
        );

        if (keyframe == null)
        {
            // No GoP index available, return the original target
            return new PlaybackSeekResponse
            {
                KeyframeMs = request.TargetMs,
                GopDurationMs = 0,
                HasGopIndex = false,
                OriginalTargetMs = request.TargetMs,
            };
        }

        this.LogSeekToKeyframe(request.PlaybackSessionId, request.TargetMs, keyframe.PtsMs);

        return new PlaybackSeekResponse
        {
            KeyframeMs = keyframe.PtsMs,
            GopDurationMs = keyframe.DurationMs,
            HasGopIndex = true,
            OriginalTargetMs = request.TargetMs,
        };
    }

    /// <inheritdoc />
    public async Task<bool> StopAsync(
        int sessionId,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.PlaylistGenerator)
            .Include(p => p.CurrentMediaPart)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == playbackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            return false;
        }

        await this.RemovePlaybackSessionAsync(
            db,
            playback,
            deleteDashCache: true,
            cancellationToken
        );

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resolves the parent title for a metadata item.
    /// For tracks, returns the album (Parent.Parent) instead of the medium (Parent).
    /// For other types, returns the direct parent title.
    /// </summary>
    private static string? ResolveParentTitle(MetadataItem? item)
    {
        if (item?.Parent == null)
        {
            return null;
        }

        // For tracks, the Parent is AlbumMedium (Disc 1, etc.)
        // We want the album name, which is Parent.Parent (AlbumRelease)
        if (item.MetadataType == MetadataType.Track && item.Parent.Parent != null)
        {
            return item.Parent.Parent.Title;
        }

        return item.Parent.Title;
    }

    /// <summary>
    /// Gets the hardware encoder name for the given codec and acceleration type.
    /// </summary>
    private static string? GetHardwareEncoder(string codec, HardwareAccelerationKind kind)
    {
        return kind switch
        {
            HardwareAccelerationKind.Vaapi => codec switch
            {
                "h264" => "h264_vaapi",
                "hevc" => "hevc_vaapi",
                "av1" => "av1_vaapi",
                _ => null
            },
            HardwareAccelerationKind.Qsv => codec switch
            {
                "h264" => "h264_qsv",
                "hevc" => "hevc_qsv",
                "av1" => "av1_qsv",
                _ => null
            },
            HardwareAccelerationKind.Nvenc => codec switch
            {
                "h264" => "h264_nvenc",
                "hevc" => "hevc_nvenc",
                "av1" => "av1_nvenc",
                _ => null
            },
            HardwareAccelerationKind.Amf => codec switch
            {
                "h264" => "h264_amf",
                "hevc" => "hevc_amf",
                "av1" => "av1_amf",
                _ => null
            },
            HardwareAccelerationKind.VideoToolbox => codec switch
            {
                "h264" => "h264_videotoolbox",
                "hevc" => "hevc_videotoolbox",
                _ => null
            },
            HardwareAccelerationKind.Rkmpp => codec switch
            {
                "h264" => "h264_rkmpp",
                "hevc" => "hevc_rkmpp",
                _ => null
            },
            HardwareAccelerationKind.V4L2M2M => codec switch
            {
                "h264" => "h264_v4l2m2m",
                "hevc" => "hevc_v4l2m2m",
                _ => null
            },
            _ => null
        };
    }

    private static async Task<List<int>> ResolvePlaylistItemsAsync(
        MediaServerContext db,
        PlaylistSeed seed,
        int skip,
        int take,
        CancellationToken cancellationToken
    )
    {
        IQueryable<MetadataItem> query;

        switch (seed.Type.ToLowerInvariant())
        {
            case "single":
                return skip == 0 && seed.OriginatorId.HasValue ? [seed.OriginatorId.Value] : [];

            case "explicit":
                return seed.Items?.Skip(skip).Take(take).ToList() ?? [];

            case "album":
            case "season":
            case "container":
                if (!seed.OriginatorId.HasValue)
                {
                    return [];
                }

                query = db
                    .MetadataItems.Where(m => m.ParentId == seed.OriginatorId.Value && m.DeletedAt == null)
                    .OrderBy(m => m.Index ?? int.MaxValue)
                    .ThenBy(m => m.Title);
                break;

            case "show":
                if (!seed.OriginatorId.HasValue)
                {
                    return [];
                }

                var seasonIds = await db
                    .MetadataItems.Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.Season
                        && m.DeletedAt == null
                    )
                    .OrderBy(m => m.Index ?? int.MaxValue)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                query = db
                    .MetadataItems.Where(m =>
                        seasonIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Episode
                        && m.DeletedAt == null
                    )
                    .OrderBy(m => m.Parent!.Index ?? int.MaxValue)
                    .ThenBy(m => m.Index ?? int.MaxValue);
                break;

            default:
                return [];
        }

        return await query.Select(m => m.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    private static async Task<int> GetPlaylistTotalCountAsync(
        MediaServerContext db,
        PlaylistSeed seed,
        CancellationToken cancellationToken
    )
    {
        switch (seed.Type.ToLowerInvariant())
        {
            case "single":
                return seed.OriginatorId.HasValue ? 1 : 0;

            case "explicit":
                return seed.Items?.Count ?? 0;

            case "album":
            case "season":
            case "container":
                if (!seed.OriginatorId.HasValue)
                {
                    return 0;
                }

                return await db
                    .MetadataItems.Where(m => m.ParentId == seed.OriginatorId.Value && m.DeletedAt == null)
                    .CountAsync(cancellationToken);

            case "show":
                if (!seed.OriginatorId.HasValue)
                {
                    return 0;
                }

                var showSeasonIds = await db
                    .MetadataItems.Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.Season
                        && m.DeletedAt == null
                    )
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                return await db
                    .MetadataItems.Where(m =>
                        showSeasonIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Episode
                        && m.DeletedAt == null
                    )
                    .CountAsync(cancellationToken);

            default:
                return 1;
        }
    }

    private static async Task<PlaylistGeneratorItem?> GetNextPlaylistItemAsync(
        MediaServerContext db,
        PlaylistGenerator generator,
        CancellationToken cancellationToken
    )
    {
        int nextIndex = generator.Cursor + 1;

        // First, check if we have a materialized item at the next index
        var nextItem = generator.Items.FirstOrDefault(i => i.SortOrder == nextIndex);
        if (nextItem != null)
        {
            return nextItem;
        }

        // Need to materialize more items
        var seed = JsonSerializer.Deserialize<PlaylistSeed>(generator.SeedJson) ?? new PlaylistSeed();
        var nextItemIds = await ResolvePlaylistItemsAsync(db, seed, nextIndex, 1, cancellationToken);

        if (nextItemIds.Count == 0)
        {
            // Check if repeat is enabled
            if (generator.Repeat)
            {
                // Reset to beginning
                generator.Cursor = -1; // Will be incremented to 0
                var firstItem = generator.Items.FirstOrDefault(i => i.SortOrder == 0);
                if (firstItem != null)
                {
                    // Reset served status for repeat
                    foreach (var item in generator.Items)
                    {
                        item.Served = false;
                    }

                    return firstItem;
                }
            }

            return null;
        }

        // Create and save the new item
        var newItem = new PlaylistGeneratorItem
        {
            PlaylistGeneratorId = generator.Id,
            MetadataItemId = nextItemIds[0],
            SortOrder = nextIndex,
        };

        await db.PlaylistGeneratorItems.AddAsync(newItem, cancellationToken);
        generator.Items.Add(newItem);

        return newItem;
    }

    private static PlaylistGeneratorItem? GetPreviousPlaylistItem(PlaylistGenerator generator)
    {
        int previousIndex = generator.Cursor - 1;
        if (previousIndex < 0)
        {
            return null;
        }

        // Previous items must already be materialized since we've played them
        return generator.Items.FirstOrDefault(i => i.SortOrder == previousIndex);
    }

    /// <summary>
    /// Resolves the first playable item from a metadata item.
    /// If the item itself is playable (has media parts), it is returned directly.
    /// If the item is a container, it recursively finds the first playable child.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="item">The metadata item to resolve.</param>
    /// <param name="explicitOriginatorId">Optional explicit originator ID from the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (playable item, originator for playlist generation).</returns>
    private static async Task<(MetadataItem PlayableItem, MetadataItem? Originator)> ResolvePlayableItemAsync(
        MediaServerContext db,
        MetadataItem item,
        int? explicitOriginatorId,
        CancellationToken cancellationToken
    )
    {
        // Check if the item itself has media parts (is directly playable)
        bool hasMediaParts = await db
            .MediaParts.AnyAsync(
                p => p.MediaItem.MetadataItemId == item.Id,
                cancellationToken
            );

        if (hasMediaParts)
        {
            // Item is directly playable
            // If explicit originator was provided, use it; otherwise use the item's parent
            MetadataItem? originator = null;
            if (explicitOriginatorId.HasValue)
            {
                originator = await db.MetadataItems.FindAsync(
                    new object[] { explicitOriginatorId.Value },
                    cancellationToken
                );
            }
            else if (item.ParentId.HasValue)
            {
                originator = await db.MetadataItems.FindAsync(
                    new object[] { item.ParentId.Value },
                    cancellationToken
                );
            }

            return (item, originator);
        }

        // Item is a container - find the first playable child
        // We need to traverse the hierarchy to find a playable item
        var firstPlayableChild = await FindFirstPlayableChildAsync(db, item.Id, cancellationToken);

        if (firstPlayableChild == null)
        {
            throw new InvalidOperationException(
                $"No playable items found within container '{item.Title}' (ID: {item.Id})."
            );
        }

        // Determine the originator based on the hierarchy
        // The originator should be the direct parent of the playable item for playlist purposes
        MetadataItem? playlistOriginator = null;
        if (firstPlayableChild.ParentId.HasValue)
        {
            playlistOriginator = await db.MetadataItems.FindAsync(
                new object[] { firstPlayableChild.ParentId.Value },
                cancellationToken
            );
        }

        return (firstPlayableChild, playlistOriginator);
    }

    /// <summary>
    /// Recursively finds the first playable child within a container hierarchy.
    /// </summary>
    private static async Task<MetadataItem?> FindFirstPlayableChildAsync(
        MediaServerContext db,
        int parentId,
        CancellationToken cancellationToken
    )
    {
        // Get immediate children ordered by index/title
        var children = await db
            .MetadataItems.Where(m => m.ParentId == parentId && m.DeletedAt == null)
            .OrderBy(m => m.Index ?? int.MaxValue)
            .ThenBy(m => m.Title)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            // Check if this child has media parts
            bool hasMediaParts = await db
                .MediaParts.AnyAsync(
                    p => p.MediaItem.MetadataItemId == child.Id,
                    cancellationToken
                );

            if (hasMediaParts)
            {
                return child;
            }

            // Recursively check this child's children
            var playableDescendant = await FindFirstPlayableChildAsync(
                db,
                child.Id,
                cancellationToken
            );

            if (playableDescendant != null)
            {
                return playableDescendant;
            }
        }

        return null;
    }

    private static bool ShouldTrackProgress(MetadataType metadataType)
    {
        return metadataType switch
        {
            MetadataType.Track => false,
            MetadataType.Photo => false,
            MetadataType.Picture => false,
            MetadataType.GameRelease => false,
            MetadataType.Trailer => false,
            MetadataType.Clip => false,
            MetadataType.BehindTheScenes => false,
            MetadataType.DeletedScene => false,
            MetadataType.Featurette => false,
            MetadataType.Interview => false,
            MetadataType.Scene => false,
            MetadataType.ShortForm => false,
            MetadataType.ExtraOther => false,
            _ => true,
        };
    }

    private async Task<int> ResolvePartIndexAsync(
        MediaServerContext db,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        _ = this.logger;

        var partIds = await db
            .MediaParts.Where(p => p.MediaItemId == part.MediaItemId)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var index = partIds.FindIndex(id => id == part.Id);
        return index < 0 ? 0 : index;
    }

    private async Task RemovePlaybackSessionAsync(
        MediaServerContext db,
        PlaybackSession playback,
        bool deleteDashCache,
        CancellationToken cancellationToken
    )
    {
        if (deleteDashCache && playback.CurrentMediaPart != null)
        {
            await this.TryDeleteDashCachesAsync(
                db,
                playback.CurrentMediaPart,
                playback.PlaybackSessionId,
                cancellationToken
            );
        }

        if (playback.PlaylistGenerator != null)
        {
            db.PlaylistGenerators.Remove(playback.PlaylistGenerator);
        }

        db.PlaybackSessions.Remove(playback);
    }

    private async Task TryDeleteDashCachesAsync(
        MediaServerContext db,
        MediaPart part,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    )
    {
        var metadataItem = part.MediaItem?.MetadataItem
            ?? await db
                .MediaParts.Where(p => p.Id == part.Id)
                .Select(p => p.MediaItem!.MetadataItem)
                .FirstOrDefaultAsync(cancellationToken);

        if (metadataItem == null)
        {
            return;
        }

        bool hasOtherActiveSessions = await db.PlaybackSessions.AnyAsync(
            p =>
                p.PlaybackSessionId != playbackSessionId
                && p.CurrentMediaPartId == part.Id
                && p.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (hasOtherActiveSessions)
        {
            return;
        }

        int partIndex = await this.ResolvePartIndexAsync(db, part, cancellationToken);
        string metadataUuid = metadataItem.Uuid.ToString("N", CultureInfo.InvariantCulture);

        var targets = new[]
        {
            Path.Combine(
                this.paths.CacheDirectory,
                "dash",
                metadataUuid,
                partIndex.ToString(CultureInfo.InvariantCulture)
            ),
            Path.Combine(
                this.paths.CacheDirectory,
                "dash-seek",
                metadataUuid,
                partIndex.ToString(CultureInfo.InvariantCulture)
            ),
        };

        foreach (string directory in targets)
        {
            try
            {
                if (IO.Directory.Exists(directory))
                {
                    IO.Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                this.logDashDeleteFailed(this.logger, directory, ex);
            }
        }
    }

    private async Task UpdateProgressAsync(
        MediaServerContext db,
        PlaybackSession playback,
        long playheadMs,
        CancellationToken cancellationToken
    )
    {
        _ = this.logger;

        var userId = playback.Session.UserId;
        var metadataId = playback.CurrentMetadataItemId;

        var setting = await db.MetadataItemSettings.FirstOrDefaultAsync(
            s => s.UserId == userId && s.MetadataItemId == metadataId,
            cancellationToken
        );

        if (setting == null)
        {
            setting = new MetadataItemSetting { UserId = userId, MetadataItemId = metadataId };
            await db.MetadataItemSettings.AddAsync(setting, cancellationToken);
        }

        // Get the metadata item to check type and duration
        var metadataItem = await db
            .MetadataItems.AsNoTracking()
            .Where(m => m.Id == metadataId)
            .Select(m => new { m.MetadataType, m.Duration })
            .FirstOrDefaultAsync(cancellationToken);

        if (metadataItem == null)
        {
            return;
        }

        // Check if this is a metadata type that should not track progress (viewOffset)
        // These types only track view counts, not playback position
        bool shouldTrackProgress = ShouldTrackProgress(metadataItem.MetadataType);

        if (!shouldTrackProgress)
        {
            // For non-progress-tracking types, only update LastViewedAt
            // View count will be incremented when playback ends via UpdateViewCountAsync
            setting.LastViewedAt = DateTime.UtcNow;
            return;
        }

        if (metadataItem.Duration != null && metadataItem.Duration > 0)
        {
            // Convert duration from seconds to milliseconds
            long durationMs = metadataItem.Duration.Value * 1000L;
            long watchedThresholdMs = (long)(durationMs * 0.9);

            // Check if playhead is in the "watched" zone (>= 90%)
            if (playheadMs >= watchedThresholdMs)
            {
                // Only increment view count if this is the first time crossing the threshold
                // in this viewing session (ViewOffset < 90% indicates we're crossing for the first time)
                if (setting.ViewOffset < watchedThresholdMs)
                {
                    setting.ViewCount += 1;
                    setting.ViewOffset = 0;
                    setting.LastViewedAt = DateTime.UtcNow;
                }

                // else: Already crossed threshold in this session, ignore further updates
                // while remaining above 90%
                return;
            }
        }

        // Normal progress tracking when below 90% threshold
        setting.ViewOffset = (int)Math.Min(int.MaxValue, playheadMs);
        setting.LastViewedAt = DateTime.UtcNow;
    }

    private async Task UpdateViewCountForNonProgressItemAsync(
        MediaServerContext db,
        PlaybackSession playback,
        CancellationToken cancellationToken
    )
    {
        _ = this.logger;

        var userId = playback.Session.UserId;
        var metadataId = playback.CurrentMetadataItemId;

        var setting = await db.MetadataItemSettings.FirstOrDefaultAsync(
            s => s.UserId == userId && s.MetadataItemId == metadataId,
            cancellationToken
        );

        if (setting == null)
        {
            setting = new MetadataItemSetting { UserId = userId, MetadataItemId = metadataId };
            await db.MetadataItemSettings.AddAsync(setting, cancellationToken);
            setting.ViewCount = 1;
            setting.ViewOffset = 1; // Mark as counted with a non-zero value
            setting.LastViewedAt = DateTime.UtcNow;
            return;
        }

        // For non-progress items, only increment if ViewOffset is 0
        // ViewOffset=0 means we haven't counted this view yet in the current playback session
        // ViewOffset=1 means we already counted it
        if (setting.ViewOffset == 0)
        {
            setting.ViewCount += 1;
            setting.ViewOffset = 1; // Mark as counted
            setting.LastViewedAt = DateTime.UtcNow;
        }
    }

    private async Task UpdateViewCountAsync(
        MediaServerContext db,
        PlaybackSession playback,
        CancellationToken cancellationToken
    )
    {
        _ = this.logger;

        var userId = playback.Session.UserId;
        var metadataId = playback.CurrentMetadataItemId;

        var setting = await db.MetadataItemSettings.FirstOrDefaultAsync(
            s => s.UserId == userId && s.MetadataItemId == metadataId,
            cancellationToken
        );

        if (setting == null)
        {
            setting = new MetadataItemSetting { UserId = userId, MetadataItemId = metadataId };
            await db.MetadataItemSettings.AddAsync(setting, cancellationToken);
        }

        setting.ViewCount += 1;
        setting.ViewOffset = 0;
        setting.LastViewedAt = DateTime.UtcNow;
    }

    private bool SupportsDirectPlay(
        PlaybackCapabilities capabilities,
        string container,
        string? videoCodec,
        string? audioCodec,
        string mediaType
    )
    {
        foreach (var profile in capabilities.DirectPlayProfiles)
        {
            if (!this.MatchesMediaType(profile.Type, mediaType))
            {
                continue;
            }

            if (!this.MatchesCsv(container, profile.Container))
            {
                continue;
            }

            bool videoOk = profile.VideoCodec is null || this.MatchesCsv(videoCodec, profile.VideoCodec);
            bool audioOk =
                profile.AudioCodec is null
                || (string.IsNullOrWhiteSpace(audioCodec)
                    && string.Equals(mediaType, "Audio", StringComparison.OrdinalIgnoreCase))
                || this.MatchesCsv(audioCodec, profile.AudioCodec);
            if (videoOk && audioOk)
            {
                return true;
            }
        }

        return false;
    }

    private bool SupportsDirectStream(
        PlaybackCapabilities capabilities,
        string container,
        string? videoCodec,
        string? audioCodec,
        string mediaType,
        out string remuxContainer,
        out string remuxProtocol
    )
    {
        remuxContainer = container;
        remuxProtocol = capabilities.SupportsDash ? "dash" : "progressive";

        foreach (var profile in capabilities.TranscodingProfiles)
        {
            if (!this.MatchesMediaType(profile.Type, mediaType))
            {
                continue;
            }

            if (!this.MatchesCsv(container, profile.Container))
            {
                continue;
            }

            bool videoOk = profile.VideoCodec is null || this.MatchesCsv(videoCodec, profile.VideoCodec);
            bool audioOk =
                profile.AudioCodec is null
                || (string.IsNullOrWhiteSpace(audioCodec)
                    && string.Equals(mediaType, "Audio", StringComparison.OrdinalIgnoreCase))
                || this.MatchesCsv(audioCodec, profile.AudioCodec);
            if (videoOk && audioOk)
            {
                remuxContainer = profile.Container ?? remuxContainer;
                remuxProtocol = profile.Protocol ?? remuxProtocol;
                return true;
            }
        }

        return false;
    }

    private bool MatchesCsv(string? value, string? csv)
    {
        _ = this.transcodeOptions;

        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = csv.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return tokens.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesMediaType(string? profileType, string mediaType)
    {
        _ = this.transcodeOptions;

        if (string.IsNullOrWhiteSpace(profileType))
        {
            return true;
        }

        return string.Equals(profileType, mediaType, StringComparison.OrdinalIgnoreCase);
    }

    private PlaybackStreamPlan PlanImageStream(
        string filePath,
        int mediaPartId,
        PlaybackCapabilities? capabilities
    )
    {
        _ = this.transcodeOptions;

        var caps = capabilities ?? new PlaybackCapabilities();
        var sourceExt = PlaybackFormatSelector.NormalizeExtension(Path.GetExtension(filePath));
        var targetFormat = PlaybackFormatSelector.ChooseImageFormat(
            sourceExt,
            caps.SupportedImageFormats
        );

        bool passthrough = string.Equals(
            sourceExt,
            targetFormat,
            StringComparison.OrdinalIgnoreCase
        );

        var queryBuilder = new StringBuilder();
        queryBuilder.Append("uri=");
        queryBuilder.Append(Uri.EscapeDataString(filePath));

        if (!passthrough)
        {
            queryBuilder.Append("&format=");
            queryBuilder.Append(Uri.EscapeDataString(targetFormat));
        }

        var directUrl = $"/api/v1/images/transcode?{queryBuilder}";

        return new PlaybackStreamPlan
        {
            Mode = passthrough ? PlaybackMode.DirectPlay : PlaybackMode.Transcode,
            MediaType = "Photo",
            Protocol = "progressive",
            MediaPartId = mediaPartId,
            Container = targetFormat,
            DirectUrl = directUrl,
            CopyVideo = true,
            CopyAudio = true,
            UseHardwareAcceleration = false,
            EnableToneMapping = false,
        };
    }

    private PlaybackStreamPlan PlanStream(
        string filePath,
        int mediaPartId,
        PlaybackCapabilities? capabilities,
        MediaCodecInfo? codecInfo,
        string mediaType
    )
    {
        string extension = Path.GetExtension(filePath);
        string normalizedExt = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.TrimStart('.').ToLowerInvariant();

        var caps = capabilities ?? new PlaybackCapabilities();
        var mediaTypeNormalized = string.IsNullOrWhiteSpace(mediaType) ? "Video" : mediaType;

        // Use pre-analyzed codec info from the database when available
        string? videoCodec = codecInfo?.VideoCodec;
        string? audioCodec = codecInfo?.AudioCodec;
        int? videoIndex = codecInfo?.VideoStreamIndex;
        int? audioIndex = codecInfo?.AudioStreamIndex;

        bool directPlay = this.SupportsDirectPlay(
            caps,
            normalizedExt,
            videoCodec,
            audioCodec,
            mediaTypeNormalized
        );
        if (directPlay)
        {
            return new PlaybackStreamPlan
            {
                Mode = PlaybackMode.DirectPlay,
                MediaType = mediaTypeNormalized,
                Protocol = "progressive",
                MediaPartId = mediaPartId,
                Container = normalizedExt,
                DirectUrl = $"/api/v1/media/part/{mediaPartId}/file.{normalizedExt}",
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                VideoStreamIndex = videoIndex,
                AudioStreamIndex = audioIndex,
                CopyVideo = videoIndex.HasValue,
                CopyAudio = audioIndex.HasValue,
                UseHardwareAcceleration = false,
                EnableToneMapping = false,
            };
        }

        string remuxContainer;
        string remuxProtocol;
        if (
            this.SupportsDirectStream(
                caps,
                normalizedExt,
                videoCodec,
                audioCodec,
                mediaTypeNormalized,
                out remuxContainer,
                out remuxProtocol
            )
        )
        {
            // DirectStream copies video/audio so hardware acceleration is not applicable
            return new PlaybackStreamPlan
            {
                Mode = PlaybackMode.DirectStream,
                MediaType = mediaTypeNormalized,
                Protocol = remuxProtocol,
                MediaPartId = mediaPartId,
                Container = remuxContainer,
                RemuxUrl = $"/api/v1/playback/part/{mediaPartId}/remux.{remuxContainer}",
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                VideoStreamIndex = videoIndex,
                AudioStreamIndex = audioIndex,
                CopyVideo = videoIndex.HasValue,
                CopyAudio = audioIndex.HasValue,
                UseHardwareAcceleration = false,
                EnableToneMapping =
                    string.Equals(mediaTypeNormalized, "Video", StringComparison.OrdinalIgnoreCase)
                    && this.ShouldApplyToneMapping(caps),
            };
        }

        bool isAudio = string.Equals(mediaTypeNormalized, "Audio", StringComparison.OrdinalIgnoreCase);
        if (isAudio && MediaFileExtensions.IsAudio($".{normalizedExt}"))
        {
            return new PlaybackStreamPlan
            {
                Mode = PlaybackMode.DirectPlay,
                MediaType = mediaTypeNormalized,
                Protocol = "progressive",
                MediaPartId = mediaPartId,
                Container = normalizedExt,
                DirectUrl = $"/api/v1/media/part/{mediaPartId}/file.{normalizedExt}",
                AudioCodec = audioCodec,
                AudioStreamIndex = audioIndex,
                CopyVideo = false,
                CopyAudio = audioIndex.HasValue,
                UseHardwareAcceleration = false,
                EnableToneMapping = false,
            };
        }

        bool includeVideo = string.Equals(
            mediaTypeNormalized,
            "Video",
            StringComparison.OrdinalIgnoreCase
        );

        // Check if hardware encoding is actually available (not just configured)
        bool useHwAccel = false;
        var hwAccelKind = this.transcodeOptions.EffectiveAcceleration;
        if (includeVideo && hwAccelKind != HardwareAccelerationKind.None)
        {
            var targetCodec = this.transcodeOptions.DashVideoCodec ?? "h264";
            var hwEncoder = GetHardwareEncoder(targetCodec, hwAccelKind);
            useHwAccel = hwEncoder != null && this.ffmpegCapabilities.SupportsEncoder(hwEncoder);
        }

        return new PlaybackStreamPlan
        {
            Mode = PlaybackMode.Transcode,
            MediaType = mediaTypeNormalized,
            Protocol = "dash",
            MediaPartId = mediaPartId,
            Container = "mp4",
            ManifestUrl = $"/api/v1/playback/part/{mediaPartId}/dash/manifest.mpd",
            VideoCodec = includeVideo ? this.transcodeOptions.DashVideoCodec : null,
            AudioCodec = this.transcodeOptions.DashAudioCodec,
            VideoStreamIndex = includeVideo ? videoIndex : null,
            AudioStreamIndex = audioIndex,
            CopyVideo = false,
            CopyAudio = false,
            UseHardwareAcceleration = useHwAccel,
            EnableToneMapping =
                includeVideo && this.ShouldApplyToneMapping(caps),
        };
    }

    private bool ShouldApplyToneMapping(PlaybackCapabilities capabilities)
    {
        if (!this.transcodeOptions.EnableToneMapping)
        {
            return false;
        }

        if (!capabilities.AllowToneMapping)
        {
            return false;
        }

        return !capabilities.SupportsHdr;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Instance-bound for future dependency access"
    )]
    private PlaybackCapabilities CreateDefaultCapabilities()
    {
        return new PlaybackCapabilities
        {
            MaxStreamingBitrate = 60_000_000,
            MaxStaticBitrate = 100_000_000,
            MusicStreamingTranscodingBitrate = 384_000,
            DirectPlayProfiles =
            [
                new DirectPlayProfile
                {
                    Type = "Audio",
                    Container = "mp3",
                    AudioCodec = "mp3",
                },
                new DirectPlayProfile
                {
                    Type = "Audio",
                    Container = "flac",
                    AudioCodec = "flac",
                },
                new DirectPlayProfile
                {
                    Type = "Audio",
                    Container = "aac,m4a",
                    AudioCodec = "aac",
                },
                new DirectPlayProfile
                {
                    Type = "Audio",
                    Container = "ogg,webm",
                    AudioCodec = "opus",
                },
                new DirectPlayProfile
                {
                    Type = "Audio",
                    Container = "wav",
                    AudioCodec = null,
                },
                new DirectPlayProfile
                {
                    Type = "Video",
                    Container = "mp4,m4v",
                    VideoCodec = "h264",
                    AudioCodec = "aac,mp3",
                },
            ],
            TranscodingProfiles =
            [
                new TranscodingProfile
                {
                    Type = "Audio",
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Context = "Streaming",
                    Protocol = "http",
                },
                new TranscodingProfile
                {
                    Type = "Video",
                    Container = "mp4",
                    VideoCodec = "h264",
                    AudioCodec = "aac,mp3",
                    Context = "Streaming",
                    Protocol = "hls",
                    MaxAudioChannels = "2",
                },
            ],
            ResponseProfiles =
            [
                new ResponseProfile
                {
                    Type = "Video",
                    Container = "m4v",
                    MimeType = "video/mp4",
                },
            ],
            SubtitleProfiles =
            [
                new SubtitleProfile
                {
                    Format = "vtt",
                    Method = "External",
                    Languages = [],
                },
            ],
            SupportedImageFormats = new List<string> { "jpg", "jpeg", "png", "webp" },
            SupportsDash = false,
            SupportsHls = true,
            AllowToneMapping = true,
        };
    }

    private async Task<PlaybackPlan> BuildPlaybackPlanAsync(
        MediaServerContext db,
        int metadataItemId,
        CapabilityProfile? capabilityProfile,
        CancellationToken cancellationToken
    )
    {
        // Optimized query: fetch media part + codec info in a single round-trip
        // Include both MediaItem.Duration (from file analysis) and MetadataItem.Duration (from external metadata)
        var projection = await db
            .MediaParts.Where(p => p.MediaItem.MetadataItemId == metadataItemId)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.File,
                MetadataUuid = p.MediaItem.MetadataItem.Uuid,
                MetadataType = p.MediaItem.MetadataItem.MetadataType,
                p.MediaItem.VideoCodec,
                AudioCodec = p.MediaItem.AudioCodecs.FirstOrDefault(),
                MediaItemDuration = p.MediaItem.Duration,
                MetadataItemDurationSeconds = p.MediaItem.MetadataItem.Duration,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (projection == null)
        {
            throw new InvalidOperationException(
                $"No media parts available for metadata item {metadataItemId}."
            );
        }

        string mediaType = PlaybackFormatSelector.ResolveMediaType(
            Path.GetExtension(projection.File),
            projection.MetadataType
        );

        string? trickplayUrl = null;
        if (string.Equals(mediaType, "Video", StringComparison.OrdinalIgnoreCase))
        {
            var bifPath = this.bifService.GetBifPath(projection.MetadataUuid, partIndex: 0);
            if (File.Exists(bifPath))
            {
                trickplayUrl = $"/api/v1/images/trickplay/{projection.Id}.vtt";
            }
        }

        // Use cached codec info from the database instead of calling FFProbe
        int? videoIndex = string.IsNullOrWhiteSpace(projection.VideoCodec) ? null : 0;
        int? audioIndex = null;
        if (!string.IsNullOrWhiteSpace(projection.AudioCodec))
        {
            audioIndex = videoIndex.HasValue ? 1 : 0;
        }

        var codecInfo = new MediaCodecInfo(
            projection.VideoCodec,
            projection.AudioCodec,
            videoIndex,
            audioIndex
        );

        PlaybackStreamPlan plan;
        if (string.Equals(mediaType, "Photo", StringComparison.OrdinalIgnoreCase))
        {
            plan = this.PlanImageStream(
                projection.File,
                projection.Id,
                capabilityProfile?.Capabilities
            );
        }
        else
        {
            plan = this.PlanStream(
                projection.File,
                projection.Id,
                capabilityProfile?.Capabilities,
                codecInfo,
                mediaType
            );
        }

        string playbackUrl = plan.DirectUrl ?? plan.RemuxUrl ?? plan.ManifestUrl ?? string.Empty;
        string streamPlanJson = JsonSerializer.Serialize(plan);

        // Convert duration to milliseconds, preferring MediaItem.Duration (from file analysis)
        // and falling back to MetadataItem.Duration (from external metadata, in seconds)
        long? durationMs = null;
        if (projection.MediaItemDuration.HasValue)
        {
            durationMs = (long)projection.MediaItemDuration.Value.TotalMilliseconds;
        }
        else if (projection.MetadataItemDurationSeconds.HasValue)
        {
            durationMs = projection.MetadataItemDurationSeconds.Value * 1000L;
        }

        return new PlaybackPlan(
            projection.Id,
            playbackUrl,
            streamPlanJson,
            trickplayUrl,
            durationMs
        );
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Seek in session {PlaybackSessionId}: target={TargetMs}ms -> keyframe={KeyframeMs}ms"
    )]
    private partial void LogSeekToKeyframe(Guid playbackSessionId, long targetMs, long keyframeMs);

    /// <summary>
    /// Holds pre-analyzed codec information from the database.
    /// </summary>
    /// <param name="VideoCodec">The video codec.</param>
    /// <param name="AudioCodec">The audio codec.</param>
    /// <param name="VideoStreamIndex">The video stream index.</param>
    /// <param name="AudioStreamIndex">The audio stream index.</param>
    private sealed record MediaCodecInfo(
        string? VideoCodec,
        string? AudioCodec,
        int? VideoStreamIndex,
        int? AudioStreamIndex
    );

    private sealed record PlaybackPlan(
        int MediaPartId,
        string PlaybackUrl,
        string StreamPlanJson,
        string? TrickplayUrl,
        long? DurationMs
    );
}
