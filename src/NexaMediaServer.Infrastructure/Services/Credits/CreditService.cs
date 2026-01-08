// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services.Metadata;

namespace NexaMediaServer.Infrastructure.Services.Credits;

/// <summary>
/// Service responsible for managing person and group credits (cast, crew, artists)
/// associated with metadata items.
/// </summary>
public sealed partial class CreditService : ICreditService
{
    private readonly MediaServerContext dbContext;
    private readonly ILockedFieldEnforcer lockedFieldEnforcer;
    private readonly ILogger<CreditService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreditService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for direct EF Core access.</param>
    /// <param name="lockedFieldEnforcer">Service for checking and enforcing field locks.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    public CreditService(
        MediaServerContext dbContext,
        ILockedFieldEnforcer lockedFieldEnforcer,
        ILogger<CreditService> logger)
    {
        this.dbContext = dbContext;
        this.lockedFieldEnforcer = lockedFieldEnforcer;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> UpsertCreditsAsync(
        MetadataItem owner,
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        IEnumerable<string>? overrideFields = null,
        CancellationToken cancellationToken = default
    )
    {
        // Check if credits are locked (Cast, Crew, or combined Credits lock)
        var creditsLocked = this.lockedFieldEnforcer.IsFieldLocked(
            owner,
            MetadataFieldNames.Credits,
            overrideFields
        );
        var castLocked = this.lockedFieldEnforcer.IsFieldLocked(
            owner,
            MetadataFieldNames.Cast,
            overrideFields
        );
        var crewLocked = this.lockedFieldEnforcer.IsFieldLocked(
            owner,
            MetadataFieldNames.Crew,
            overrideFields
        );

        // If all credits are locked, skip processing entirely
        if (creditsLocked || (castLocked && crewLocked))
        {
            this.LogCreditsLocked(owner.Uuid);
            return false;
        }

        var normalized = NormalizeCredits(people, groups, castLocked, crewLocked);
        if (normalized.PersonCredits.Count == 0 && normalized.GroupCredits.Count == 0)
        {
            return false;
        }

        this.LogUpsertingCredits(
            owner.Uuid,
            normalized.PersonCredits.Count,
            normalized.GroupCredits.Count
        );

        var personMap = await this.FetchExistingMetadataAsync(
                normalized.PersonNames,
                MetadataType.Person,
                cancellationToken
            )
            .ConfigureAwait(false);

        var groupMap = await this.FetchExistingMetadataAsync(
                normalized.GroupNames,
                MetadataType.Group,
                cancellationToken
            )
            .ConfigureAwait(false);

        var newMetadata = CreateMissingMetadata(
            normalized.PersonCredits,
            normalized.GroupCredits,
            owner,
            personMap,
            groupMap
        );

        var changes = await this.SaveNewMetadataAsync(
                newMetadata,
                personMap,
                groupMap,
                cancellationToken
            )
            .ConfigureAwait(false);

        var relationCandidates = BuildRelationCandidates(
            normalized.PersonCredits,
            normalized.GroupCredits,
            owner,
            personMap,
            groupMap
        );

        if (relationCandidates.Count == 0)
        {
            return changes;
        }

        var relationsInserted = await this.InsertMissingRelationsAsync(
                relationCandidates,
                cancellationToken
            )
            .ConfigureAwait(false);

        return changes || relationsInserted;
    }

    private static (
        List<PersonCredit> PersonCredits,
        List<GroupCredit> GroupCredits,
        List<string> PersonNames,
        List<string> GroupNames
    ) NormalizeCredits(
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        bool castLocked = false,
        bool crewLocked = false)
    {
        var personCredits = (people ?? Enumerable.Empty<PersonCredit>()).ToList();
        var groupCredits = (groups ?? Enumerable.Empty<GroupCredit>()).ToList();

        if (groupCredits.Count > 0)
        {
            var memberCredits = groupCredits
                .SelectMany(g => g.Members ?? Array.Empty<PersonCredit>())
                .ToList();

            if (memberCredits.Count > 0)
            {
                personCredits.AddRange(memberCredits);
            }
        }

        personCredits = personCredits
            .Where(c => c.Person is not null && !string.IsNullOrWhiteSpace(c.Person.Title))
            .ToList();

        groupCredits = groupCredits
            .Where(c => c.Group is not null && !string.IsNullOrWhiteSpace(c.Group.Title))
            .ToList();

        // Filter out locked credit types
        // Cast = PersonPerformsInVideo, Crew = PersonContributesCrewToVideo and other contribution types
        if (castLocked)
        {
            personCredits = personCredits
                .Where(c => c.RelationType != RelationType.PersonPerformsInVideo)
                .ToList();
        }

        if (crewLocked)
        {
            personCredits = personCredits
                .Where(c => c.RelationType == RelationType.PersonPerformsInVideo)
                .ToList();
            groupCredits = new List<GroupCredit>(); // All groups are crew (studios, networks)
        }

        var personNames = personCredits
            .Select(c => c.Person.Title.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupNames = groupCredits
            .Select(c => c.Group.Title.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (personCredits, groupCredits, personNames, groupNames);
    }

    private static List<MetadataItem> CreateMissingMetadata(
        List<PersonCredit> personCredits,
        List<GroupCredit> groupCredits,
        MetadataItem owner,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap
    )
    {
        var newMetadata = new List<MetadataItem>();

        foreach (var person in personCredits.Select(credit => credit.Person!))
        {
            var name = person.Title.Trim();
            if (personMap.ContainsKey(name))
            {
                continue;
            }

            var dto = person with
            {
                Title = name,
                SortTitle = string.IsNullOrWhiteSpace(person.SortTitle) ? name : person.SortTitle,
                LibrarySectionId = owner.LibrarySectionId,
            };

            var entity = MetadataItemMapper.MapToEntity(dto);
            personMap[name] = entity;
            newMetadata.Add(entity);
        }

        foreach (var group in groupCredits.Select(credit => credit.Group!))
        {
            var name = group.Title.Trim();
            if (groupMap.ContainsKey(name))
            {
                continue;
            }

            var dto = group with
            {
                Title = name,
                SortTitle = string.IsNullOrWhiteSpace(group.SortTitle) ? name : group.SortTitle,
                LibrarySectionId = owner.LibrarySectionId,
            };

            var entity = MetadataItemMapper.MapToEntity(dto);
            groupMap[name] = entity;
            newMetadata.Add(entity);
        }

        return newMetadata;
    }

    private static List<MetadataRelation> BuildRelationCandidates(
        List<PersonCredit> personCredits,
        List<GroupCredit> groupCredits,
        MetadataItem owner,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap
    )
    {
        var relationCandidates = new List<MetadataRelation>();

        foreach (var credit in groupCredits)
        {
            var groupName = credit.Group.Title.Trim();
            if (!groupMap.TryGetValue(groupName, out var group))
            {
                continue;
            }

            relationCandidates.Add(
                new MetadataRelation
                {
                    MetadataItemId = group.Id,
                    RelatedMetadataItemId = owner.Id,
                    RelationType = credit.RelationType,
                    Text = credit.Text,
                }
            );

            if (credit.Members is null)
            {
                continue;
            }

            foreach (var member in credit.Members)
            {
                var memberName = member.Person.Title.Trim();
                if (!personMap.TryGetValue(memberName, out var memberEntity))
                {
                    continue;
                }

                relationCandidates.Add(
                    new MetadataRelation
                    {
                        MetadataItemId = memberEntity.Id,
                        RelatedMetadataItemId = group.Id,
                        RelationType = member.RelationType,
                        Text = member.Text,
                    }
                );
            }
        }

        foreach (var credit in personCredits)
        {
            var personName = credit.Person.Title.Trim();
            if (!personMap.TryGetValue(personName, out var person))
            {
                continue;
            }

            relationCandidates.Add(
                new MetadataRelation
                {
                    MetadataItemId = person.Id,
                    RelatedMetadataItemId = owner.Id,
                    RelationType = credit.RelationType,
                    Text = credit.Text,
                }
            );
        }

        return relationCandidates;
    }

    private async Task<Dictionary<string, MetadataItem>> FetchExistingMetadataAsync(
        List<string> names,
        MetadataType type,
        CancellationToken cancellationToken
    )
    {
        var map = new Dictionary<string, MetadataItem>(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0)
        {
            return map;
        }

        var query = this
            .dbContext.MetadataItems.AsNoTracking()
            .Where(m => m.MetadataType == type)
            .Where(m => names.Contains(EF.Functions.Collate(m.Title, "NOCASE")));

        var existing = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var meta in existing)
        {
            var name = (meta.Title ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                map[name] = meta;
            }
        }

        return map;
    }

    private async Task<bool> SaveNewMetadataAsync(
        List<MetadataItem> newMetadata,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap,
        CancellationToken cancellationToken
    )
    {
        if (newMetadata.Count == 0)
        {
            return false;
        }

        await this.dbContext.MetadataItems.AddRangeAsync(newMetadata, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        foreach (var meta in newMetadata)
        {
            var name = (meta.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (meta.MetadataType == MetadataType.Person)
            {
                personMap[name] = meta;
            }
            else if (meta.MetadataType == MetadataType.Group)
            {
                groupMap[name] = meta;
            }
        }

        this.LogCreatedMetadataEntities(newMetadata.Count);
        return true;
    }

    private async Task<bool> InsertMissingRelationsAsync(
        IReadOnlyCollection<MetadataRelation> relationCandidates,
        CancellationToken cancellationToken
    )
    {
        var sourceIds = relationCandidates.Select(r => r.MetadataItemId).Distinct().ToList();
        var targetIds = relationCandidates.Select(r => r.RelatedMetadataItemId).Distinct().ToList();
        var relationTypes = relationCandidates.Select(r => r.RelationType).Distinct().ToList();

        var existingRelations = await this
            .dbContext.MetadataRelations.AsNoTracking()
            .Where(r => sourceIds.Contains(r.MetadataItemId))
            .Where(r => targetIds.Contains(r.RelatedMetadataItemId))
            .Where(r => relationTypes.Contains(r.RelationType))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingKeys = new HashSet<RelationKey>(RelationKeyComparer.Instance);
        foreach (var rel in existingRelations)
        {
            existingKeys.Add(
                new RelationKey(
                    rel.MetadataItemId,
                    rel.RelatedMetadataItemId,
                    rel.RelationType,
                    rel.Text
                )
            );
        }

        var toInsert = new List<MetadataRelation>();
        foreach (var candidate in relationCandidates)
        {
            var key = new RelationKey(
                candidate.MetadataItemId,
                candidate.RelatedMetadataItemId,
                candidate.RelationType,
                candidate.Text
            );

            if (existingKeys.Add(key))
            {
                toInsert.Add(candidate);
            }
        }

        if (toInsert.Count == 0)
        {
            return false;
        }

        await this.dbContext.MetadataRelations.AddRangeAsync(toInsert, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        this.LogCreatedRelations(toInsert.Count);
        return true;
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Upserting credits for {MetadataItemUuid}: {PersonCount} people, {GroupCount} groups"
    )]
    private partial void LogUpsertingCredits(
        Guid metadataItemUuid,
        int personCount,
        int groupCount
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created {Count} new metadata entities for credits"
    )]
    private partial void LogCreatedMetadataEntities(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created {Count} new metadata relations")]
    private partial void LogCreatedRelations(int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Credits locked for metadata item {MetadataItemUuid}, skipping upsert"
    )]
    private partial void LogCreditsLocked(Guid metadataItemUuid);
    #endregion

    private sealed record RelationKey(
        int SourceId,
        int TargetId,
        RelationType RelationType,
        string? Text
    );

    private sealed class RelationKeyComparer : IEqualityComparer<RelationKey>
    {
        public static readonly RelationKeyComparer Instance = new();

        public bool Equals(RelationKey? x, RelationKey? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.SourceId == y.SourceId
                && x.TargetId == y.TargetId
                && x.RelationType == y.RelationType
                && string.Equals(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(RelationKey obj)
        {
            var hash = default(HashCode);
            hash.Add(obj.SourceId);
            hash.Add(obj.TargetId);
            hash.Add(obj.RelationType);
            hash.Add(obj.Text, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }
}
