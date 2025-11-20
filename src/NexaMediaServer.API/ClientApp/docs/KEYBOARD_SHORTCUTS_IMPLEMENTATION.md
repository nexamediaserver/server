# Keyboard Shortcuts Implementation

## Overview

The keyboard shortcuts system provides a centralized way to manage keyboard shortcuts across the application. This makes it easy to:

- Document all available shortcuts
- Avoid conflicts between shortcuts
- Add/remove/modify shortcuts in one place
- Conditionally enable/disable shortcuts based on application state

## Architecture

### Core Components

1. **`useKeyboardShortcuts` Hook** (`/src/shared/hooks/useKeyboardShortcuts.ts`)
   - Core hook that registers global keyboard event listeners
   - Handles keyboard events and dispatches to the appropriate handlers
   - Automatically disables shortcuts when typing in input fields
   - Supports conditional shortcuts

2. **Configuration Files** (e.g., `/src/features/player/config/keyboardShortcuts.ts`)
   - Define shortcuts for specific features
   - Centralize shortcut definitions to make them easy to find and modify
   - Export documentation-friendly structures

3. **Feature Components** (e.g., `PlayerContainer.tsx`)
   - Implement handler functions
   - Configure and register shortcuts using the hook

### Key Features

- **Conditional Shortcuts**: Execute only when specific conditions are met
- **Multiple Keys per Action**: Support multiple keys triggering the same action (e.g., P and Spacebar)
- **Input Protection**: Automatically disabled when user is typing in input fields
- **Prevent Default**: Can optionally prevent browser default behaviors
- **TypeScript Support**: Full type safety for configuration

## Usage Example

### 1. Create a Configuration File

```typescript
// src/features/myfeature/config/keyboardShortcuts.ts
import type { KeyboardShortcut } from '@/shared/hooks'

export function createMyFeatureShortcuts(
  handlers: {
    doSomething: () => void
    doSomethingElse: () => void
  },
  conditions: {
    isActive: () => boolean
  },
): KeyboardShortcut[] {
  return [
    {
      description: 'Do something',
      handler: handlers.doSomething,
      key: 's',
    },
    {
      condition: conditions.isActive,
      description: 'Do something else (when active)',
      handler: handlers.doSomethingElse,
      key: ['e', 'Enter'],
    },
  ]
}
```

### 2. Use in Component

```typescript
// MyComponent.tsx
import { useMemo } from 'react'
import { useKeyboardShortcuts } from '@/shared/hooks'
import { createMyFeatureShortcuts } from './config/keyboardShortcuts'

export function MyComponent() {
  const [isActive, setIsActive] = useState(false)

  const handleDoSomething = () => {
    console.log('Doing something!')
  }

  const handleDoSomethingElse = () => {
    console.log('Doing something else!')
  }

  // Configure shortcuts
  const shortcuts = useMemo(
    () =>
      createMyFeatureShortcuts(
        {
          doSomething: handleDoSomething,
          doSomethingElse: handleDoSomethingElse,
        },
        {
          isActive: () => isActive,
        },
      ),
    [isActive],
  )

  // Register shortcuts
  useKeyboardShortcuts(shortcuts, true)

  return <div>My Component</div>
}
```

## Best Practices

1. **Keep shortcuts centralized**: Define all shortcuts for a feature in one configuration file
2. **Use conditions wisely**: Only execute shortcuts when they make sense contextually
3. **Document shortcuts**: Update the main KEYBOARD_SHORTCUTS.md when adding new shortcuts
4. **Test edge cases**: Ensure shortcuts don't interfere with input fields or other browser functionality
5. **Use descriptive keys**: Choose intuitive keyboard shortcuts that users can easily remember

## Testing

When testing components with keyboard shortcuts:

```typescript
import { fireEvent } from '@testing-library/react'

// Simulate a keydown event
fireEvent.keyDown(window, { key: 'f' })
```

## Adding New Shortcuts

See [KEYBOARD_SHORTCUTS.md](./KEYBOARD_SHORTCUTS.md) for the current list of shortcuts and instructions on adding new ones.
