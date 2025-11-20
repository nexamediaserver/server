# Keyboard Shortcuts

This document lists all keyboard shortcuts available in the NexaMediaServer web client.

## Player Shortcuts

These shortcuts are available when the media player is active.

### Global (Available anytime player is active)

| Key          | Action            |
| ------------ | ----------------- |
| <kbd>F</kbd> | Toggle fullscreen |

### Maximized Player Only

These shortcuts only work when the player is maximized (full view mode).

| Key                                 | Action                  |
| ----------------------------------- | ----------------------- |
| <kbd>P</kbd> or <kbd>Spacebar</kbd> | Play/Pause              |
| <kbd>←</kbd> (Left Arrow)           | Rewind 10 seconds       |
| <kbd>→</kbd> (Right Arrow)          | Forward 10 seconds      |
| <kbd>↓</kbd> (Down Arrow)           | Skip back 10 minutes    |
| <kbd>↑</kbd> (Up Arrow)             | Jump forward 10 minutes |

## Adding New Shortcuts

To add new keyboard shortcuts:

1. **Define the shortcut** in `/src/features/player/config/keyboardShortcuts.ts`:
   - Add the handler function parameter to the `createPlayerShortcuts` function
   - Add the shortcut configuration to the array
   - Update the `PLAYER_SHORTCUTS_DOCS` object for documentation

2. **Implement the handler** in the component that uses the shortcut (e.g., `PlayerContainer.tsx`):
   - Create the handler function
   - Pass it to `createPlayerShortcuts` in the `useMemo` hook

3. **Update this documentation** with the new shortcut

## Implementation Details

The keyboard shortcuts system is centralized using:

- **`useKeyboardShortcuts` hook** (`/src/shared/hooks/useKeyboardShortcuts.ts`): Core hook for registering keyboard shortcuts
- **Configuration files**: Each feature can have its own keyboard shortcuts configuration (e.g., `/src/features/player/config/keyboardShortcuts.ts`)

### Features

- **Conditional shortcuts**: Shortcuts can have conditions that must be met for them to be active
- **Multiple keys**: A single action can be triggered by multiple keys (e.g., both P and Spacebar for play/pause)
- **Input protection**: Shortcuts are automatically disabled when typing in input fields (unless explicitly allowed)
- **Prevent default**: Shortcuts can optionally prevent default browser behavior
- **Centralized management**: All shortcuts are defined in one place, making them easy to find and modify

### Example

```typescript
import { useKeyboardShortcuts } from '@/shared/hooks'
import { createPlayerShortcuts } from '../config/keyboardShortcuts'

// In your component:
const shortcuts = useMemo(
  () =>
    createPlayerShortcuts(
      {
        toggleFullscreen: handleToggleFullscreen,
        togglePlayPause: handleTogglePlayPause,
        // ... other handlers
      },
      {
        isPlayerMaximized: () => isMaximized,
      },
    ),
  [isMaximized],
)

useKeyboardShortcuts(shortcuts, isPlayerActive)
```
