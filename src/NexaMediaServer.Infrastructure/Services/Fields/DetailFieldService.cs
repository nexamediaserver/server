// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Fields;

/// <summary>
/// Service for retrieving and managing field definitions for item detail pages.
/// </summary>
public sealed partial class DetailFieldService : IDetailFieldService
{
    private readonly IDbContextFactory<MediaServerContext> contextFactory;
    private readonly IDetailFieldDefinitionProvider definitionProvider;

    // Logger field is used by source-generated LoggerMessage methods
#pragma warning disable S1450, S4487
    private readonly ILogger<DetailFieldService> logger;
#pragma warning restore S1450, S4487

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailFieldService"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="definitionProvider">The field definition provider.</param>
    /// <param name="logger">The logger.</param>
    public DetailFieldService(
        IDbContextFactory<MediaServerContext> contextFactory,
        IDetailFieldDefinitionProvider definitionProvider,
        ILogger<DetailFieldService> logger
    )
    {
        this.contextFactory = contextFactory;
        this.definitionProvider = definitionProvider;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DetailFieldDefinition>> GetItemDetailFieldDefinitionsAsync(
        Guid metadataItemId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var itemInfo = await context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId)
            .Select(mi => new { mi.MetadataType, mi.LibrarySectionId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (itemInfo == null || itemInfo.MetadataType == MetadataType.Unknown)
        {
            return [];
        }

        this.LogRetrievingFieldDefinitions(metadataItemId, itemInfo.MetadataType);

        return await this.GetFieldDefinitionsForTypeInternalAsync(
            context,
            itemInfo.MetadataType,
            userId,
            itemInfo.LibrarySectionId,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DetailFieldDefinition>> GetFieldDefinitionsForTypeAsync(
        MetadataType metadataType,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        return await this.GetFieldDefinitionsForTypeInternalAsync(
            context,
            metadataType,
            userId,
            librarySectionId: null,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<DetailFieldConfiguration?> GetAdminFieldConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        int? libraryId = null;

        if (librarySectionId.HasValue)
        {
            libraryId = await context
                .LibrarySections.Where(section => section.Uuid == librarySectionId.Value)
                .Select(section => (int?)section.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!libraryId.HasValue)
            {
                return null;
            }
        }

        return await GetAdminFieldConfigurationInternalAsync(
            context,
            metadataType,
            libraryId,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<DetailFieldConfiguration> UpdateUserFieldConfigurationAsync(
        string userId,
        MetadataType metadataType,
        DetailFieldConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var existingConfig = await context
            .UserDetailFieldConfigurations.FirstOrDefaultAsync(
                c => c.UserId == userId && c.MetadataType == metadataType,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserDetailFieldConfiguration
            {
                UserId = userId,
                MetadataType = metadataType,
            };
            context.UserDetailFieldConfigurations.Add(existingConfig);
        }

        existingConfig.EnabledFieldTypes = configuration.EnabledFieldTypes.ToList();
        existingConfig.DisabledFieldTypes = configuration.DisabledFieldTypes.ToList();
        existingConfig.DisabledCustomFieldKeys = configuration.DisabledCustomFieldKeys.ToList();

        // Serialize field groups if provided
        existingConfig.FieldGroupsJson = configuration.FieldGroups != null
            ? JsonSerializer.Serialize(configuration.FieldGroups)
            : null;

        // Serialize field group assignments if provided
        existingConfig.FieldGroupAssignmentsJson = configuration.FieldGroupAssignments != null
            ? JsonSerializer.Serialize(configuration.FieldGroupAssignments)
            : null;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogUpdatedUserFieldConfiguration(userId, metadataType);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task<DetailFieldConfiguration> UpdateAdminFieldConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        DetailFieldConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        int? libraryId = null;

        if (librarySectionId.HasValue)
        {
            libraryId = await context
                .LibrarySections.Where(section => section.Uuid == librarySectionId.Value)
                .Select(section => (int?)section.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!libraryId.HasValue)
            {
                return configuration;
            }
        }

        var existingConfig = await context
            .DetailFieldConfigurationOverrides.FirstOrDefaultAsync(
                config =>
                    config.MetadataType == metadataType && config.LibrarySectionId == libraryId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new DetailFieldConfigurationOverride
            {
                MetadataType = metadataType,
                LibrarySectionId = libraryId,
            };

            context.DetailFieldConfigurationOverrides.Add(existingConfig);
        }

        existingConfig.EnabledFieldTypes = configuration.EnabledFieldTypes.ToList();
        existingConfig.DisabledFieldTypes = configuration.DisabledFieldTypes.ToList();
        existingConfig.DisabledCustomFieldKeys = configuration.DisabledCustomFieldKeys.ToList();

        // Serialize field groups if provided
        existingConfig.FieldGroupsJson = configuration.FieldGroups != null
            ? JsonSerializer.Serialize(configuration.FieldGroups)
            : null;

        // Serialize field group assignments if provided
        existingConfig.FieldGroupAssignmentsJson = configuration.FieldGroupAssignments != null
            ? JsonSerializer.Serialize(configuration.FieldGroupAssignments)
            : null;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogUpdatedAdminFieldConfiguration(metadataType, librarySectionId);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CustomFieldDefinition>> GetCustomFieldDefinitionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .CustomFieldDefinitions.OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CustomFieldDefinition>> GetCustomFieldDefinitionsForTypeAsync(
        MetadataType metadataType,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        // Get custom fields that are enabled and either apply to all types or include this type
        var customFields = (await context
            .CustomFieldDefinitions.Where(f => f.IsEnabled)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Where(f => IsApplicableToType(f, metadataType))
            .ToList();

        return customFields;
    }

    /// <inheritdoc/>
    public async Task<CustomFieldDefinition> CreateCustomFieldDefinitionAsync(
        string key,
        string label,
        DetailFieldWidgetType widget,
        IEnumerable<MetadataType> applicableMetadataTypes,
        int sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        // Check for duplicate key
        var existingField = await context
            .CustomFieldDefinitions.FirstOrDefaultAsync(f => f.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existingField != null)
        {
            throw new InvalidOperationException($"A custom field with key '{key}' already exists.");
        }

        var customField = new CustomFieldDefinition
        {
            Key = key,
            Label = label,
            Widget = widget,
            ApplicableMetadataTypes = applicableMetadataTypes.ToList(),
            SortOrder = sortOrder,
            IsEnabled = true,
        };

        context.CustomFieldDefinitions.Add(customField);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogCreatedCustomFieldDefinition(key, label);

        return customField;
    }

    /// <inheritdoc/>
    public async Task<CustomFieldDefinition> UpdateCustomFieldDefinitionAsync(
        int id,
        string? label,
        DetailFieldWidgetType? widget,
        IEnumerable<MetadataType>? applicableMetadataTypes,
        int? sortOrder,
        bool? isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var customField = await context
            .CustomFieldDefinitions.FindAsync([id], cancellationToken)
            .ConfigureAwait(false);

        if (customField == null)
        {
            throw new InvalidOperationException($"Custom field with ID '{id}' not found.");
        }

        if (label != null)
        {
            customField.Label = label;
        }

        if (widget.HasValue)
        {
            customField.Widget = widget.Value;
        }

        if (applicableMetadataTypes != null)
        {
            customField.ApplicableMetadataTypes = applicableMetadataTypes.ToList();
        }

        if (sortOrder.HasValue)
        {
            customField.SortOrder = sortOrder.Value;
        }

        if (isEnabled.HasValue)
        {
            customField.IsEnabled = isEnabled.Value;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogUpdatedCustomFieldDefinition(id, customField.Key);

        return customField;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCustomFieldDefinitionAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var customField = await context
            .CustomFieldDefinitions.FindAsync([id], cancellationToken)
            .ConfigureAwait(false);

        if (customField == null)
        {
            return false;
        }

        context.CustomFieldDefinitions.Remove(customField);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogDeletedCustomFieldDefinition(id, customField.Key);

        return true;
    }

    private static bool IsApplicableToType(CustomFieldDefinition field, MetadataType metadataType)
    {
        return field.ApplicableMetadataTypes.Count == 0
            || field.ApplicableMetadataTypes.Contains(metadataType);
    }

    private static async Task<DetailFieldConfiguration?> GetAdminFieldConfigurationInternalAsync(
        MediaServerContext context,
        MetadataType metadataType,
        int? librarySectionId,
        CancellationToken cancellationToken
    )
    {
        DetailFieldConfigurationOverride? adminConfig = null;

        if (librarySectionId.HasValue)
        {
            adminConfig = await context
                .DetailFieldConfigurationOverrides.AsNoTracking()
                .FirstOrDefaultAsync(
                    config =>
                        config.MetadataType == metadataType
                        && config.LibrarySectionId == librarySectionId,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        if (adminConfig == null)
        {
            adminConfig = await context
                .DetailFieldConfigurationOverrides.AsNoTracking()
                .FirstOrDefaultAsync(
                    config => config.MetadataType == metadataType && config.LibrarySectionId == null,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return adminConfig == null
            ? null
            : new DetailFieldConfiguration(
                adminConfig.EnabledFieldTypes,
                adminConfig.DisabledFieldTypes,
                adminConfig.DisabledCustomFieldKeys,
                DeserializeFieldGroups(adminConfig.FieldGroupsJson),
                DeserializeFieldGroupAssignments(adminConfig.FieldGroupAssignmentsJson)
            );
    }

    private static List<DetailFieldGroup>? DeserializeFieldGroups(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<DetailFieldGroup>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string>? DeserializeFieldGroupAssignments(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static List<DetailFieldDefinition> ApplyFieldConfiguration(
        List<DetailFieldDefinition> definitions,
        DetailFieldConfiguration configuration
    )
    {
        var disabledTypes = configuration.DisabledFieldTypes.ToHashSet();
        var disabledCustomKeys = configuration.DisabledCustomFieldKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = definitions
            .Where(definition => !disabledTypes.Contains(definition.FieldType))
            .Where(definition =>
                definition.FieldType != DetailFieldType.Custom
                || !disabledCustomKeys.Contains(definition.CustomFieldKey ?? string.Empty)
            )
            .ToList();

        if (configuration.EnabledFieldTypes.Count == 0)
        {
            return filtered.OrderBy(definition => definition.SortOrder).ToList();
        }

        var fieldsByType = filtered
            .Where(definition => definition.FieldType != DetailFieldType.Custom)
            .GroupBy(definition => definition.FieldType)
            .ToDictionary(group => group.Key, group => group.First());

        var ordered = new List<DetailFieldDefinition>();

        foreach (var fieldType in configuration.EnabledFieldTypes)
        {
            if (fieldsByType.TryGetValue(fieldType, out var definition))
            {
                ordered.Add(definition);
                fieldsByType.Remove(fieldType);
            }
        }

        ordered.AddRange(fieldsByType.Values.OrderBy(definition => definition.SortOrder));

        ordered.AddRange(
            filtered
                .Where(definition => definition.FieldType == DetailFieldType.Custom)
                .OrderBy(definition => definition.SortOrder)
        );

        return ordered;
    }

    private static Dictionary<string, string> CreateDefaultGroupAssignments(IReadOnlyList<DetailFieldDefinition> fields)
    {
        return fields
            .Where(f => !string.IsNullOrEmpty(f.GroupKey))
            .ToDictionary(
                f => GetFieldKey(f),
                f => f.GroupKey!,
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static List<DetailFieldDefinition> ApplyGroupAssignments(
        List<DetailFieldDefinition> fields,
        IReadOnlyDictionary<string, string>? assignments
    )
    {
        if (assignments == null || assignments.Count == 0)
        {
            return fields;
        }

        return fields
            .Select(f =>
            {
                var fieldKey = GetFieldKey(f);
                if (assignments.TryGetValue(fieldKey, out var groupKey))
                {
                    return f with { GroupKey = groupKey };
                }

                return f;
            })
            .ToList();
    }

    private static string GetFieldKey(DetailFieldDefinition field)
    {
        return field.FieldType == DetailFieldType.Custom
            ? $"Custom:{field.CustomFieldKey}"
            : field.FieldType.ToString();
    }

    private async Task<IReadOnlyList<DetailFieldDefinition>> GetFieldDefinitionsForTypeInternalAsync(
        MediaServerContext context,
        MetadataType metadataType,
        string userId,
        int? librarySectionId,
        CancellationToken cancellationToken
    )
    {
        // Get default fields and groups from provider
        var defaultFields = this.definitionProvider.GetDefaultFields(metadataType);
#pragma warning disable S1481 // Unused local variables should be removed - will be used when groups are returned
        var defaultGroups = this.definitionProvider.GetDefaultGroups(metadataType);
#pragma warning restore S1481

        // Get custom fields applicable to this type
        var customFields = (await context
            .CustomFieldDefinitions.Where(f => f.IsEnabled)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Where(f => IsApplicableToType(f, metadataType))
            .ToList();

        var adminConfig = await GetAdminFieldConfigurationInternalAsync(
            context,
            metadataType,
            librarySectionId,
            cancellationToken
        ).ConfigureAwait(false);

        // Get user configuration
        var userConfig = await context
            .UserDetailFieldConfigurations.FirstOrDefaultAsync(
                c => c.UserId == userId && c.MetadataType == metadataType,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Start with default fields
        var allFields = defaultFields.ToList();

        // Determine group assignments (admin overrides or defaults)
        var groupAssignments = adminConfig?.FieldGroupAssignments
            ?? CreateDefaultGroupAssignments(defaultFields);

        // Apply group assignments to fields
        allFields = ApplyGroupAssignments(allFields, groupAssignments);

        // Add custom fields as DetailFieldDefinition
        var maxSortOrder = defaultFields.Count > 0 ? defaultFields.Max(f => f.SortOrder) : 0;
        foreach (var customField in customFields)
        {
            allFields.Add(
                new DetailFieldDefinition(
                    DetailFieldType.Custom,
                    customField.Label,
                    customField.Widget,
                    maxSortOrder + customField.SortOrder,
                    customField.Key
                )
            );
        }

        if (adminConfig != null)
        {
            allFields = ApplyFieldConfiguration(allFields, adminConfig);
        }

        // Apply user configuration if present
        if (userConfig != null)
        {
            var userConfiguration = new DetailFieldConfiguration(
                userConfig.EnabledFieldTypes,
                userConfig.DisabledFieldTypes,
                userConfig.DisabledCustomFieldKeys,
                DeserializeFieldGroups(userConfig.FieldGroupsJson),
                DeserializeFieldGroupAssignments(userConfig.FieldGroupAssignmentsJson)
            );

            allFields = ApplyFieldConfiguration(allFields, userConfiguration);
        }

        return allFields.OrderBy(f => f.SortOrder).ToList();
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving field definitions for item {MetadataItemId} of type {MetadataType}"
    )]
    private partial void LogRetrievingFieldDefinitions(Guid metadataItemId, MetadataType metadataType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Updated user field configuration for user {UserId} and metadata type {MetadataType}"
    )]
    private partial void LogUpdatedUserFieldConfiguration(string userId, MetadataType metadataType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Updated admin field configuration for metadata type {MetadataType} and library {LibrarySectionId}"
    )]
    private partial void LogUpdatedAdminFieldConfiguration(
        MetadataType metadataType,
        Guid? librarySectionId
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Created custom field definition with key '{Key}' and label '{Label}'"
    )]
    private partial void LogCreatedCustomFieldDefinition(string key, string label);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Updated custom field definition with ID {Id} (key: '{Key}')"
    )]
    private partial void LogUpdatedCustomFieldDefinition(int id, string key);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleted custom field definition with ID {Id} (key: '{Key}')"
    )]
    private partial void LogDeletedCustomFieldDefinition(int id, string key);
}

