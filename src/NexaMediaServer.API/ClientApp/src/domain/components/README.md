# Domain Components

This directory contains **domain-specific React components** that represent
media-server business concepts. These are distinct from:

- `@/shared/components/ui` - Generic UI primitives (Button, Dialog, etc.)
- `@/shared/components` - Generic compound components (ConfirmDialog, etc.)
- `@/features/*/components` - Feature-specific implementations

## Guidelines

### What Belongs Here

- Components tied to media-server domain concepts (Item, Library, Playback)
- Components that multiple features need to render domain entities
- Components that depend on domain types from `@/shared/api/graphql`

### What Doesn't Belong Here

- Generic UI components → `@/shared/components/ui`
- Feature-specific components → `@/features/*/components`
- Layout components → `@/shared/components` or feature routes

## Component Structure

Each component should be organized as:

```
ComponentName/
├── ComponentName.tsx    # Main implementation
├── ComponentName.test.tsx # Tests (colocated)
└── index.ts             # Barrel export
```

## Current Components

### `ItemProgress`

Renders playback progress as a progress bar based on `viewOffset` and `length`.
Used in item cards across library, detail views, and continue watching.

### `MetadataTypeIcon`

Renders an appropriate icon based on an item's `metadataType`.
Used in search results, item headers, and library views.
