# Domain Layer (`@/domain`)

This layer contains **business domain concepts** that are shared across multiple features.
It provides a clean abstraction over media-server specific logic, separating it from
generic UI components (`@/shared`) and feature implementations (`@/features`).

## Architecture Principles

1. **Domain-Specific, Not Generic UI**
   - Components here represent media-server concepts (items, libraries, playback progress)
   - Generic UI primitives stay in `@/shared/components/ui`
   - Feature-specific implementations stay in `@/features/*`

2. **Single Source of Truth**
   - Types, constants, and utilities for domain concepts live here
   - Features import from `@/domain`, never from each other
   - This prevents circular dependencies and coupling between features

3. **Stable API Surface**
   - Each subdirectory exports via a barrel file (`index.ts`)
   - Internal implementation details are not exported
   - Breaking changes should be documented

## Directory Structure

```
domain/
├── components/         # Domain-specific React components
│   ├── ItemProgress/   # Playback progress display
│   └── MetadataTypeIcon/ # Icon by metadata type
│
├── entities/          # Entity-specific utilities and types
│   ├── item/          # Item-related helpers
│   ├── library/       # Library section helpers
│   └── metadata/      # Metadata processing
│
├── constants/         # Domain constants (routes, sizes, etc.)
│
├── types/            # Shared TypeScript types
│
└── utils/            # Domain-specific utilities
```

## Usage

Import domain concepts using the `@/domain` alias:

```tsx
// Good - import from domain layer
import { ItemProgress, MetadataTypeIcon } from '@/domain/components'
import { getImageUrl, IMAGE_SIZES } from '@/domain/entities/item'

// Bad - cross-feature imports create coupling
import { something } from '@/features/library' // Don't do this from another feature
```

## Adding New Domain Concepts

1. Create a directory under the appropriate category
2. Include component/utility files and a local `index.ts`
3. Export from the parent barrel file
4. Add tests alongside the implementation
5. Document the purpose in this README if it's a new category
