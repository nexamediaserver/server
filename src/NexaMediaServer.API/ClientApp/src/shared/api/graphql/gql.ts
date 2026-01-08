/* eslint-disable */
import * as types from './graphql'
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core'

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
  '\n  mutation AnalyzeItem($itemId: ID!) {\n    analyzeItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n': typeof types.AnalyzeItemDocument
  '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n': typeof types.LibrarySectionsListDocument
  '\n  mutation StartLibraryScan($librarySectionId: ID!) {\n    startLibraryScan(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n      scanId\n    }\n  }\n': typeof types.StartLibraryScanDocument
  '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n': typeof types.RemoveLibrarySectionDocument
  '\n  mutation UpdateMetadataItem($input: UpdateMetadataItemInput!) {\n    updateMetadataItem(input: $input) {\n      success\n      error\n      item {\n        id\n        title\n        titleSort\n        originalTitle\n        summary\n        tagline\n        contentRating\n        year\n        originallyAvailableAt\n        genres\n        tags\n        lockedFields\n        externalIds {\n          provider\n          value\n        }\n        extraFields {\n          key\n          value\n        }\n      }\n    }\n  }\n': typeof types.UpdateMetadataItemDocument
  '\n  mutation LockMetadataFields($input: LockMetadataFieldsInput!) {\n    lockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n': typeof types.LockMetadataFieldsDocument
  '\n  mutation UnlockMetadataFields($input: UnlockMetadataFieldsInput!) {\n    unlockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n': typeof types.UnlockMetadataFieldsDocument
  '\n  query MetadataItemForEdit($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      metadataType\n      title\n      titleSort\n      originalTitle\n      summary\n      tagline\n      contentRating\n      year\n      originallyAvailableAt\n      genres\n      tags\n      lockedFields\n      externalIds {\n        provider\n        value\n      }\n      extraFields {\n        key\n        value\n      }\n      thumbUri\n      thumbHash\n    }\n  }\n': typeof types.MetadataItemForEditDocument
  '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n': typeof types.RefreshLibraryMetadataDocument
  '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n': typeof types.RefreshItemMetadataDocument
  '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n': typeof types.PromoteItemDocument
  '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n': typeof types.UnpromoteItemDocument
  '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n': typeof types.SearchDocument
  '\n  mutation RestartServer {\n    restartServer\n  }\n': typeof types.RestartServerDocument
  '\n  query ServerSettings {\n    serverSettings {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n': typeof types.ServerSettingsDocument
  '\n  mutation UpdateServerSettings($input: UpdateServerSettingsInput!) {\n    updateServerSettings(input: $input) {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n': typeof types.UpdateServerSettingsDocument
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n': typeof types.OnMetadataItemUpdatedDocument
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n': typeof types.OnJobNotificationDocument
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n': typeof types.ServerInfoDocument
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n': typeof types.AddLibrarySectionDocument
  '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n': typeof types.AvailableMetadataAgentsDocument
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n': typeof types.FileSystemRootsDocument
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n': typeof types.BrowseDirectoryDocument
  '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n': typeof types.LibrarySectionDocument
  '\n  query LibrarySectionBrowseOptions($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      availableRootItemTypes {\n        displayName\n        metadataTypes\n      }\n      availableSortFields {\n        key\n        displayName\n        requiresUserData\n      }\n    }\n  }\n': typeof types.LibrarySectionBrowseOptionsDocument
  '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n    $skip: Int\n    $take: Int\n    $order: [ItemSortInput!]\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataTypes: $metadataTypes\n        skip: $skip\n        take: $take\n        order: $order\n      ) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewCount\n          viewOffset\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n': typeof types.LibrarySectionChildrenDocument
  '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataTypes: $metadataTypes) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n': typeof types.LibrarySectionLetterIndexDocument
  '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.HomeHubDefinitionsDocument
  '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.LibraryDiscoverHubDefinitionsDocument
  '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.ItemDetailHubDefinitionsDocument
  '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      index\n      length\n      viewCount\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n      parent {\n        id\n        index\n        title\n      }\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n    }\n  }\n': typeof types.HubItemsDocument
  '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n': typeof types.HubPeopleDocument
  '\n  query MetadataItemChildren($itemId: ID!, $skip: Int, $take: Int) {\n    metadataItem(id: $itemId) {\n      id\n      librarySectionId\n      children(skip: $skip, take: $take) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          metadataType\n          title\n          year\n          index\n          length\n          viewCount\n          viewOffset\n          thumbUri\n          thumbHash\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n': typeof types.MetadataItemChildrenDocument
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewCount\n      viewOffset\n      isPromoted\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n      extraFields {\n        key\n        value\n      }\n    }\n  }\n': typeof types.MediaDocument
  '\n  query ItemDetailFieldDefinitions($itemId: ID!) {\n    itemDetailFieldDefinitions(itemId: $itemId) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n': typeof types.ItemDetailFieldDefinitionsDocument
  '\n  query FieldDefinitionsForType($metadataType: MetadataType!) {\n    fieldDefinitionsForType(metadataType: $metadataType) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n': typeof types.FieldDefinitionsForTypeDocument
  '\n  query CustomFieldDefinitions {\n    customFieldDefinitions {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n': typeof types.CustomFieldDefinitionsDocument
  '\n  mutation CreateCustomFieldDefinition(\n    $input: CreateCustomFieldDefinitionInput!\n  ) {\n    createCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n': typeof types.CreateCustomFieldDefinitionDocument
  '\n  mutation UpdateCustomFieldDefinition(\n    $input: UpdateCustomFieldDefinitionInput!\n  ) {\n    updateCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n': typeof types.UpdateCustomFieldDefinitionDocument
  '\n  mutation DeleteCustomFieldDefinition($id: ID!) {\n    deleteCustomFieldDefinition(id: $id)\n  }\n': typeof types.DeleteCustomFieldDefinitionDocument
  '\n  mutation UpdateDetailFieldConfiguration(\n    $input: UpdateDetailFieldConfigurationInput!\n  ) {\n    updateDetailFieldConfiguration(input: $input) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n    }\n  }\n': typeof types.UpdateDetailFieldConfigurationDocument
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n': typeof types.ContentSourceDocument
  '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.DecidePlaybackDocument
  '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.PlaybackHeartbeatDocument
  '\n  query PlaylistChunk($input: PlaylistChunkInput!) {\n    playlistChunk(input: $input) {\n      playlistGeneratorId\n      items {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      currentIndex\n      totalCount\n      hasMore\n      shuffle\n      repeat\n    }\n  }\n': typeof types.PlaylistChunkDocument
  '\n  mutation PlaylistNext($input: PlaylistNavigateInput!) {\n    playlistNext(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n': typeof types.PlaylistNextDocument
  '\n  mutation PlaylistPrevious($input: PlaylistNavigateInput!) {\n    playlistPrevious(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n': typeof types.PlaylistPreviousDocument
  '\n  mutation PlaylistJump($input: PlaylistJumpInput!) {\n    playlistJump(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n': typeof types.PlaylistJumpDocument
  '\n  mutation PlaylistSetShuffle($input: PlaylistModeInput!) {\n    playlistSetShuffle(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n': typeof types.PlaylistSetShuffleDocument
  '\n  mutation PlaylistSetRepeat($input: PlaylistModeInput!) {\n    playlistSetRepeat(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n': typeof types.PlaylistSetRepeatDocument
  '\n  mutation DecidePlaybackNavigation($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.DecidePlaybackNavigationDocument
  '\n  mutation ResumePlayback($input: PlaybackResumeInput!) {\n    resumePlayback(input: $input) {\n      playbackSessionId\n      currentItemId\n      playlistGeneratorId\n      playheadMs\n      state\n      capabilityProfileVersion\n      capabilityVersionMismatch\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n    }\n  }\n': typeof types.ResumePlaybackDocument
  '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n      playlistIndex\n      playlistTotalCount\n      shuffle\n      repeat\n      currentItemId\n      currentItemMetadataType\n      currentItemTitle\n      currentItemOriginalTitle\n      currentItemParentTitle\n      currentItemThumbUrl\n      currentItemParentThumbUrl\n    }\n  }\n': typeof types.StartPlaybackDocument
  '\n  mutation StopPlayback($input: PlaybackStopInput!) {\n    stopPlayback(input: $input) {\n      success\n    }\n  }\n': typeof types.StopPlaybackDocument
  '\n  query HubConfiguration($input: HubConfigurationScopeInput!) {\n    hubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n': typeof types.HubConfigurationDocument
  '\n  mutation UpdateHubConfiguration($input: UpdateHubConfigurationInput!) {\n    updateHubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n': typeof types.UpdateHubConfigurationDocument
  '\n  query AdminDetailFieldConfiguration(\n    $input: DetailFieldConfigurationScopeInput!\n  ) {\n    adminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n': typeof types.AdminDetailFieldConfigurationDocument
  '\n  mutation UpdateAdminDetailFieldConfiguration(\n    $input: UpdateAdminDetailFieldConfigurationInput!\n  ) {\n    updateAdminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n': typeof types.UpdateAdminDetailFieldConfigurationDocument
  '\n  query AdminLibrarySectionsList {\n    librarySections(first: 50, order: [{ name: ASC }]) {\n      edges {\n        node {\n          id\n          name\n          type\n        }\n      }\n    }\n  }\n': typeof types.AdminLibrarySectionsListDocument
}
const documents: Documents = {
  '\n  mutation AnalyzeItem($itemId: ID!) {\n    analyzeItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n':
    types.AnalyzeItemDocument,
  '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n':
    types.LibrarySectionsListDocument,
  '\n  mutation StartLibraryScan($librarySectionId: ID!) {\n    startLibraryScan(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n      scanId\n    }\n  }\n':
    types.StartLibraryScanDocument,
  '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n':
    types.RemoveLibrarySectionDocument,
  '\n  mutation UpdateMetadataItem($input: UpdateMetadataItemInput!) {\n    updateMetadataItem(input: $input) {\n      success\n      error\n      item {\n        id\n        title\n        titleSort\n        originalTitle\n        summary\n        tagline\n        contentRating\n        year\n        originallyAvailableAt\n        genres\n        tags\n        lockedFields\n        externalIds {\n          provider\n          value\n        }\n        extraFields {\n          key\n          value\n        }\n      }\n    }\n  }\n':
    types.UpdateMetadataItemDocument,
  '\n  mutation LockMetadataFields($input: LockMetadataFieldsInput!) {\n    lockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n':
    types.LockMetadataFieldsDocument,
  '\n  mutation UnlockMetadataFields($input: UnlockMetadataFieldsInput!) {\n    unlockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n':
    types.UnlockMetadataFieldsDocument,
  '\n  query MetadataItemForEdit($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      metadataType\n      title\n      titleSort\n      originalTitle\n      summary\n      tagline\n      contentRating\n      year\n      originallyAvailableAt\n      genres\n      tags\n      lockedFields\n      externalIds {\n        provider\n        value\n      }\n      extraFields {\n        key\n        value\n      }\n      thumbUri\n      thumbHash\n    }\n  }\n':
    types.MetadataItemForEditDocument,
  '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n':
    types.RefreshLibraryMetadataDocument,
  '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n':
    types.RefreshItemMetadataDocument,
  '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n':
    types.PromoteItemDocument,
  '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n':
    types.UnpromoteItemDocument,
  '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n':
    types.SearchDocument,
  '\n  mutation RestartServer {\n    restartServer\n  }\n':
    types.RestartServerDocument,
  '\n  query ServerSettings {\n    serverSettings {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n':
    types.ServerSettingsDocument,
  '\n  mutation UpdateServerSettings($input: UpdateServerSettingsInput!) {\n    updateServerSettings(input: $input) {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n':
    types.UpdateServerSettingsDocument,
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n':
    types.OnMetadataItemUpdatedDocument,
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n':
    types.OnJobNotificationDocument,
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n':
    types.ServerInfoDocument,
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n':
    types.AddLibrarySectionDocument,
  '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n':
    types.AvailableMetadataAgentsDocument,
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n':
    types.FileSystemRootsDocument,
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n':
    types.BrowseDirectoryDocument,
  '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n':
    types.LibrarySectionDocument,
  '\n  query LibrarySectionBrowseOptions($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      availableRootItemTypes {\n        displayName\n        metadataTypes\n      }\n      availableSortFields {\n        key\n        displayName\n        requiresUserData\n      }\n    }\n  }\n':
    types.LibrarySectionBrowseOptionsDocument,
  '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n    $skip: Int\n    $take: Int\n    $order: [ItemSortInput!]\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataTypes: $metadataTypes\n        skip: $skip\n        take: $take\n        order: $order\n      ) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewCount\n          viewOffset\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n':
    types.LibrarySectionChildrenDocument,
  '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataTypes: $metadataTypes) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n':
    types.LibrarySectionLetterIndexDocument,
  '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.HomeHubDefinitionsDocument,
  '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.LibraryDiscoverHubDefinitionsDocument,
  '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.ItemDetailHubDefinitionsDocument,
  '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      index\n      length\n      viewCount\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n      parent {\n        id\n        index\n        title\n      }\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n    }\n  }\n':
    types.HubItemsDocument,
  '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n':
    types.HubPeopleDocument,
  '\n  query MetadataItemChildren($itemId: ID!, $skip: Int, $take: Int) {\n    metadataItem(id: $itemId) {\n      id\n      librarySectionId\n      children(skip: $skip, take: $take) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          metadataType\n          title\n          year\n          index\n          length\n          viewCount\n          viewOffset\n          thumbUri\n          thumbHash\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n':
    types.MetadataItemChildrenDocument,
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewCount\n      viewOffset\n      isPromoted\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n      extraFields {\n        key\n        value\n      }\n    }\n  }\n':
    types.MediaDocument,
  '\n  query ItemDetailFieldDefinitions($itemId: ID!) {\n    itemDetailFieldDefinitions(itemId: $itemId) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n':
    types.ItemDetailFieldDefinitionsDocument,
  '\n  query FieldDefinitionsForType($metadataType: MetadataType!) {\n    fieldDefinitionsForType(metadataType: $metadataType) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n':
    types.FieldDefinitionsForTypeDocument,
  '\n  query CustomFieldDefinitions {\n    customFieldDefinitions {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n':
    types.CustomFieldDefinitionsDocument,
  '\n  mutation CreateCustomFieldDefinition(\n    $input: CreateCustomFieldDefinitionInput!\n  ) {\n    createCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n':
    types.CreateCustomFieldDefinitionDocument,
  '\n  mutation UpdateCustomFieldDefinition(\n    $input: UpdateCustomFieldDefinitionInput!\n  ) {\n    updateCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n':
    types.UpdateCustomFieldDefinitionDocument,
  '\n  mutation DeleteCustomFieldDefinition($id: ID!) {\n    deleteCustomFieldDefinition(id: $id)\n  }\n':
    types.DeleteCustomFieldDefinitionDocument,
  '\n  mutation UpdateDetailFieldConfiguration(\n    $input: UpdateDetailFieldConfigurationInput!\n  ) {\n    updateDetailFieldConfiguration(input: $input) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n    }\n  }\n':
    types.UpdateDetailFieldConfigurationDocument,
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n':
    types.ContentSourceDocument,
  '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n':
    types.DecidePlaybackDocument,
  '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n':
    types.PlaybackHeartbeatDocument,
  '\n  query PlaylistChunk($input: PlaylistChunkInput!) {\n    playlistChunk(input: $input) {\n      playlistGeneratorId\n      items {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      currentIndex\n      totalCount\n      hasMore\n      shuffle\n      repeat\n    }\n  }\n':
    types.PlaylistChunkDocument,
  '\n  mutation PlaylistNext($input: PlaylistNavigateInput!) {\n    playlistNext(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n':
    types.PlaylistNextDocument,
  '\n  mutation PlaylistPrevious($input: PlaylistNavigateInput!) {\n    playlistPrevious(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n':
    types.PlaylistPreviousDocument,
  '\n  mutation PlaylistJump($input: PlaylistJumpInput!) {\n    playlistJump(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n':
    types.PlaylistJumpDocument,
  '\n  mutation PlaylistSetShuffle($input: PlaylistModeInput!) {\n    playlistSetShuffle(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n':
    types.PlaylistSetShuffleDocument,
  '\n  mutation PlaylistSetRepeat($input: PlaylistModeInput!) {\n    playlistSetRepeat(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n':
    types.PlaylistSetRepeatDocument,
  '\n  mutation DecidePlaybackNavigation($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n':
    types.DecidePlaybackNavigationDocument,
  '\n  mutation ResumePlayback($input: PlaybackResumeInput!) {\n    resumePlayback(input: $input) {\n      playbackSessionId\n      currentItemId\n      playlistGeneratorId\n      playheadMs\n      state\n      capabilityProfileVersion\n      capabilityVersionMismatch\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n    }\n  }\n':
    types.ResumePlaybackDocument,
  '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n      playlistIndex\n      playlistTotalCount\n      shuffle\n      repeat\n      currentItemId\n      currentItemMetadataType\n      currentItemTitle\n      currentItemOriginalTitle\n      currentItemParentTitle\n      currentItemThumbUrl\n      currentItemParentThumbUrl\n    }\n  }\n':
    types.StartPlaybackDocument,
  '\n  mutation StopPlayback($input: PlaybackStopInput!) {\n    stopPlayback(input: $input) {\n      success\n    }\n  }\n':
    types.StopPlaybackDocument,
  '\n  query HubConfiguration($input: HubConfigurationScopeInput!) {\n    hubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n':
    types.HubConfigurationDocument,
  '\n  mutation UpdateHubConfiguration($input: UpdateHubConfigurationInput!) {\n    updateHubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n':
    types.UpdateHubConfigurationDocument,
  '\n  query AdminDetailFieldConfiguration(\n    $input: DetailFieldConfigurationScopeInput!\n  ) {\n    adminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n':
    types.AdminDetailFieldConfigurationDocument,
  '\n  mutation UpdateAdminDetailFieldConfiguration(\n    $input: UpdateAdminDetailFieldConfigurationInput!\n  ) {\n    updateAdminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n':
    types.UpdateAdminDetailFieldConfigurationDocument,
  '\n  query AdminLibrarySectionsList {\n    librarySections(first: 50, order: [{ name: ASC }]) {\n      edges {\n        node {\n          id\n          name\n          type\n        }\n      }\n    }\n  }\n':
    types.AdminLibrarySectionsListDocument,
}

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation AnalyzeItem($itemId: ID!) {\n    analyzeItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation AnalyzeItem($itemId: ID!) {\n    analyzeItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation StartLibraryScan($librarySectionId: ID!) {\n    startLibraryScan(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n      scanId\n    }\n  }\n',
): (typeof documents)['\n  mutation StartLibraryScan($librarySectionId: ID!) {\n    startLibraryScan(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n      scanId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateMetadataItem($input: UpdateMetadataItemInput!) {\n    updateMetadataItem(input: $input) {\n      success\n      error\n      item {\n        id\n        title\n        titleSort\n        originalTitle\n        summary\n        tagline\n        contentRating\n        year\n        originallyAvailableAt\n        genres\n        tags\n        lockedFields\n        externalIds {\n          provider\n          value\n        }\n        extraFields {\n          key\n          value\n        }\n      }\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateMetadataItem($input: UpdateMetadataItemInput!) {\n    updateMetadataItem(input: $input) {\n      success\n      error\n      item {\n        id\n        title\n        titleSort\n        originalTitle\n        summary\n        tagline\n        contentRating\n        year\n        originallyAvailableAt\n        genres\n        tags\n        lockedFields\n        externalIds {\n          provider\n          value\n        }\n        extraFields {\n          key\n          value\n        }\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation LockMetadataFields($input: LockMetadataFieldsInput!) {\n    lockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n',
): (typeof documents)['\n  mutation LockMetadataFields($input: LockMetadataFieldsInput!) {\n    lockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UnlockMetadataFields($input: UnlockMetadataFieldsInput!) {\n    unlockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n',
): (typeof documents)['\n  mutation UnlockMetadataFields($input: UnlockMetadataFieldsInput!) {\n    unlockMetadataFields(input: $input) {\n      success\n      error\n      lockedFields\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query MetadataItemForEdit($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      metadataType\n      title\n      titleSort\n      originalTitle\n      summary\n      tagline\n      contentRating\n      year\n      originallyAvailableAt\n      genres\n      tags\n      lockedFields\n      externalIds {\n        provider\n        value\n      }\n      extraFields {\n        key\n        value\n      }\n      thumbUri\n      thumbHash\n    }\n  }\n',
): (typeof documents)['\n  query MetadataItemForEdit($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      metadataType\n      title\n      titleSort\n      originalTitle\n      summary\n      tagline\n      contentRating\n      year\n      originallyAvailableAt\n      genres\n      tags\n      lockedFields\n      externalIds {\n        provider\n        value\n      }\n      extraFields {\n        key\n        value\n      }\n      thumbUri\n      thumbHash\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n',
): (typeof documents)['\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RestartServer {\n    restartServer\n  }\n',
): (typeof documents)['\n  mutation RestartServer {\n    restartServer\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ServerSettings {\n    serverSettings {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n',
): (typeof documents)['\n  query ServerSettings {\n    serverSettings {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateServerSettings($input: UpdateServerSettingsInput!) {\n    updateServerSettings(input: $input) {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateServerSettings($input: UpdateServerSettingsInput!) {\n    updateServerSettings(input: $input) {\n      serverName\n      maxStreamingBitrate\n      preferH265\n      allowRemuxing\n      allowHEVCEncoding\n      dashVideoCodec\n      dashAudioCodec\n      dashSegmentDurationSeconds\n      enableToneMapping\n      userPreferredAcceleration\n      allowedTags\n      blockedTags\n      genreMappings {\n        key\n        value\n      }\n      logLevel\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n',
): (typeof documents)['\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n',
): (typeof documents)['\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n',
): (typeof documents)['\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n',
): (typeof documents)['\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n',
): (typeof documents)['\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n',
): (typeof documents)['\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n',
): (typeof documents)['\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionBrowseOptions($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      availableRootItemTypes {\n        displayName\n        metadataTypes\n      }\n      availableSortFields {\n        key\n        displayName\n        requiresUserData\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionBrowseOptions($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      availableRootItemTypes {\n        displayName\n        metadataTypes\n      }\n      availableSortFields {\n        key\n        displayName\n        requiresUserData\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n    $skip: Int\n    $take: Int\n    $order: [ItemSortInput!]\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataTypes: $metadataTypes\n        skip: $skip\n        take: $take\n        order: $order\n      ) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewCount\n          viewOffset\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n    $skip: Int\n    $take: Int\n    $order: [ItemSortInput!]\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataTypes: $metadataTypes\n        skip: $skip\n        take: $take\n        order: $order\n      ) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewCount\n          viewOffset\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataTypes: $metadataTypes) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataTypes: [MetadataType!]!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataTypes: $metadataTypes) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      index\n      length\n      viewCount\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n      parent {\n        id\n        index\n        title\n      }\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n    }\n  }\n',
): (typeof documents)['\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      index\n      length\n      viewCount\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n      parent {\n        id\n        index\n        title\n      }\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n',
): (typeof documents)['\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query MetadataItemChildren($itemId: ID!, $skip: Int, $take: Int) {\n    metadataItem(id: $itemId) {\n      id\n      librarySectionId\n      children(skip: $skip, take: $take) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          metadataType\n          title\n          year\n          index\n          length\n          viewCount\n          viewOffset\n          thumbUri\n          thumbHash\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n',
): (typeof documents)['\n  query MetadataItemChildren($itemId: ID!, $skip: Int, $take: Int) {\n    metadataItem(id: $itemId) {\n      id\n      librarySectionId\n      children(skip: $skip, take: $take) {\n        items {\n          id\n          isPromoted\n          librarySectionId\n          metadataType\n          title\n          year\n          index\n          length\n          viewCount\n          viewOffset\n          thumbUri\n          thumbHash\n          primaryPerson {\n            id\n            title\n            metadataType\n          }\n          persons {\n            id\n            title\n            metadataType\n          }\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewCount\n      viewOffset\n      isPromoted\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n      extraFields {\n        key\n        value\n      }\n    }\n  }\n',
): (typeof documents)['\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewCount\n      viewOffset\n      isPromoted\n      primaryPerson {\n        id\n        title\n        metadataType\n      }\n      persons {\n        id\n        title\n        metadataType\n      }\n      extraFields {\n        key\n        value\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ItemDetailFieldDefinitions($itemId: ID!) {\n    itemDetailFieldDefinitions(itemId: $itemId) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n',
): (typeof documents)['\n  query ItemDetailFieldDefinitions($itemId: ID!) {\n    itemDetailFieldDefinitions(itemId: $itemId) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query FieldDefinitionsForType($metadataType: MetadataType!) {\n    fieldDefinitionsForType(metadataType: $metadataType) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n',
): (typeof documents)['\n  query FieldDefinitionsForType($metadataType: MetadataType!) {\n    fieldDefinitionsForType(metadataType: $metadataType) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n      groupKey\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query CustomFieldDefinitions {\n    customFieldDefinitions {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n',
): (typeof documents)['\n  query CustomFieldDefinitions {\n    customFieldDefinitions {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation CreateCustomFieldDefinition(\n    $input: CreateCustomFieldDefinitionInput!\n  ) {\n    createCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n',
): (typeof documents)['\n  mutation CreateCustomFieldDefinition(\n    $input: CreateCustomFieldDefinitionInput!\n  ) {\n    createCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateCustomFieldDefinition(\n    $input: UpdateCustomFieldDefinitionInput!\n  ) {\n    updateCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateCustomFieldDefinition(\n    $input: UpdateCustomFieldDefinitionInput!\n  ) {\n    updateCustomFieldDefinition(input: $input) {\n      id\n      key\n      label\n      widget\n      applicableMetadataTypes\n      sortOrder\n      isEnabled\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation DeleteCustomFieldDefinition($id: ID!) {\n    deleteCustomFieldDefinition(id: $id)\n  }\n',
): (typeof documents)['\n  mutation DeleteCustomFieldDefinition($id: ID!) {\n    deleteCustomFieldDefinition(id: $id)\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateDetailFieldConfiguration(\n    $input: UpdateDetailFieldConfigurationInput!\n  ) {\n    updateDetailFieldConfiguration(input: $input) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateDetailFieldConfiguration(\n    $input: UpdateDetailFieldConfigurationInput!\n  ) {\n    updateDetailFieldConfiguration(input: $input) {\n      key\n      fieldType\n      label\n      widget\n      sortOrder\n      customFieldKey\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n',
): (typeof documents)['\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query PlaylistChunk($input: PlaylistChunkInput!) {\n    playlistChunk(input: $input) {\n      playlistGeneratorId\n      items {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      currentIndex\n      totalCount\n      hasMore\n      shuffle\n      repeat\n    }\n  }\n',
): (typeof documents)['\n  query PlaylistChunk($input: PlaylistChunkInput!) {\n    playlistChunk(input: $input) {\n      playlistGeneratorId\n      items {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      currentIndex\n      totalCount\n      hasMore\n      shuffle\n      repeat\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaylistNext($input: PlaylistNavigateInput!) {\n    playlistNext(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaylistNext($input: PlaylistNavigateInput!) {\n    playlistNext(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaylistPrevious($input: PlaylistNavigateInput!) {\n    playlistPrevious(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaylistPrevious($input: PlaylistNavigateInput!) {\n    playlistPrevious(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaylistJump($input: PlaylistJumpInput!) {\n    playlistJump(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaylistJump($input: PlaylistJumpInput!) {\n    playlistJump(input: $input) {\n      success\n      currentItem {\n        itemEntryId\n        itemId\n        index\n        served\n        title\n        metadataType\n        durationMs\n        playbackUrl\n        thumbUri\n        parentTitle\n        subtitle\n        primaryPerson {\n          id\n          title\n          metadataType\n        }\n      }\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaylistSetShuffle($input: PlaylistModeInput!) {\n    playlistSetShuffle(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaylistSetShuffle($input: PlaylistModeInput!) {\n    playlistSetShuffle(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaylistSetRepeat($input: PlaylistModeInput!) {\n    playlistSetRepeat(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaylistSetRepeat($input: PlaylistModeInput!) {\n    playlistSetRepeat(input: $input) {\n      success\n      shuffle\n      repeat\n      currentIndex\n      totalCount\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation DecidePlaybackNavigation($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation DecidePlaybackNavigation($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      nextItemTitle\n      nextItemOriginalTitle\n      nextItemParentTitle\n      nextItemThumbUrl\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation ResumePlayback($input: PlaybackResumeInput!) {\n    resumePlayback(input: $input) {\n      playbackSessionId\n      currentItemId\n      playlistGeneratorId\n      playheadMs\n      state\n      capabilityProfileVersion\n      capabilityVersionMismatch\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n    }\n  }\n',
): (typeof documents)['\n  mutation ResumePlayback($input: PlaybackResumeInput!) {\n    resumePlayback(input: $input) {\n      playbackSessionId\n      currentItemId\n      playlistGeneratorId\n      playheadMs\n      state\n      capabilityProfileVersion\n      capabilityVersionMismatch\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n      playlistIndex\n      playlistTotalCount\n      shuffle\n      repeat\n      currentItemId\n      currentItemMetadataType\n      currentItemTitle\n      currentItemOriginalTitle\n      currentItemParentTitle\n      currentItemThumbUrl\n      currentItemParentThumbUrl\n    }\n  }\n',
): (typeof documents)['\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n      playlistIndex\n      playlistTotalCount\n      shuffle\n      repeat\n      currentItemId\n      currentItemMetadataType\n      currentItemTitle\n      currentItemOriginalTitle\n      currentItemParentTitle\n      currentItemThumbUrl\n      currentItemParentThumbUrl\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation StopPlayback($input: PlaybackStopInput!) {\n    stopPlayback(input: $input) {\n      success\n    }\n  }\n',
): (typeof documents)['\n  mutation StopPlayback($input: PlaybackStopInput!) {\n    stopPlayback(input: $input) {\n      success\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HubConfiguration($input: HubConfigurationScopeInput!) {\n    hubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n',
): (typeof documents)['\n  query HubConfiguration($input: HubConfigurationScopeInput!) {\n    hubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateHubConfiguration($input: UpdateHubConfigurationInput!) {\n    updateHubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateHubConfiguration($input: UpdateHubConfigurationInput!) {\n    updateHubConfiguration(input: $input) {\n      enabledHubTypes\n      disabledHubTypes\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query AdminDetailFieldConfiguration(\n    $input: DetailFieldConfigurationScopeInput!\n  ) {\n    adminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n',
): (typeof documents)['\n  query AdminDetailFieldConfiguration(\n    $input: DetailFieldConfigurationScopeInput!\n  ) {\n    adminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UpdateAdminDetailFieldConfiguration(\n    $input: UpdateAdminDetailFieldConfigurationInput!\n  ) {\n    updateAdminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n',
): (typeof documents)['\n  mutation UpdateAdminDetailFieldConfiguration(\n    $input: UpdateAdminDetailFieldConfigurationInput!\n  ) {\n    updateAdminDetailFieldConfiguration(input: $input) {\n      metadataType\n      librarySectionId\n      enabledFieldTypes\n      disabledFieldTypes\n      disabledCustomFieldKeys\n      fieldGroups {\n        groupKey\n        label\n        layoutType\n        sortOrder\n        isCollapsible\n      }\n      fieldGroupAssignments {\n        key\n        value\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query AdminLibrarySectionsList {\n    librarySections(first: 50, order: [{ name: ASC }]) {\n      edges {\n        node {\n          id\n          name\n          type\n        }\n      }\n    }\n  }\n',
): (typeof documents)['\n  query AdminLibrarySectionsList {\n    librarySections(first: 50, order: [{ name: ASC }]) {\n      edges {\n        node {\n          id\n          name\n          type\n        }\n      }\n    }\n  }\n']

export function graphql(source: string) {
  return (documents as any)[source] ?? {}
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> =
  TDocumentNode extends DocumentNode<infer TType, any> ? TType : never
